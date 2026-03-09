import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import productosApi from "../api/productos.api";
import ProductCard from "../components/ProductCard";
import "./Catalogo.css";
import { notifyError, notifySuccess } from "@/shared/ui/sweetAlert";
import carritosApi from "@/features/carrito/api/carrito.api";

const GENERO_PALABRAS_CLAVE = [
  ["ciencia ficcion", "Ciencia ficcion"],
  ["ciencia ficción", "Ciencia ficcion"],
  ["ficcion", "Ficcion"],
  ["ficción", "Ficcion"],
  ["novela", "Novela"],
  ["terror", "Terror"],
  ["romance", "Romance"],
  ["fantasia", "Fantasia"],
  ["fantasía", "Fantasia"],
  ["historia", "Historia"],
  ["historico", "Historico"],
  ["histórico", "Historico"],
  ["biografia", "Biografia"],
  ["biografía", "Biografia"],
  ["poesia", "Poesia"],
  ["poesía", "Poesia"],
  ["infantil", "Infantil"],
  ["juvenil", "Juvenil"],
  ["aventura", "Aventura"],
  ["drama", "Drama"],
  ["policial", "Policial"],
  ["misterio", "Misterio"],
  ["autoayuda", "Autoayuda"],
  ["clasico", "Clasico"],
  ["clásico", "Clasico"],
];

function normalizarGenero(valor) {
  return (valor || "").trim().toLowerCase();
}

function capitalizar(valor) {
  if (!valor) return valor;
  return valor.charAt(0).toUpperCase() + valor.slice(1);
}

function resolverGenero(producto) {
  const generoDirecto = (producto?.genero || producto?.categoria || "").trim();
  if (generoDirecto) return capitalizar(generoDirecto);

  const descripcion = (producto?.descripcion || "").trim();
  if (!descripcion) return "Sin genero";

  const explicitMatch = descripcion.match(/(?:genero|género|categoria|categoría)\s*:\s*([^\-|,.;]+)/i);
  if (explicitMatch?.[1]) return capitalizar(explicitMatch[1].trim().toLowerCase());

  const descNorm = descripcion.toLowerCase();
  const keyword = GENERO_PALABRAS_CLAVE.find(([term]) => descNorm.includes(term));
  if (keyword) return keyword[1];

  return "Sin genero";
}

function obtenerPrecioOrdenable(producto) {
  const usaPromo =
    producto?.tienePromocionActiva &&
    producto?.precioFinal != null &&
    Number(producto.precioFinal) < Number(producto.precio);
  const base = usaPromo ? producto.precioFinal : producto.precio;
  return Number(base) || 0;
}

export default function Catalogo() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [q, setQ] = useState("");
  const [ordenPrecio, setOrdenPrecio] = useState("relevancia");
  const [genero, setGenero] = useState("todos");
  const [detalleLibro, setDetalleLibro] = useState(null);

  const totalPromos = useMemo(() => items.filter((x) => x.tienePromocionActiva).length, [items]);

  const generosDisponibles = useMemo(() => {
    const counts = new Map();
    for (const p of items) {
      const generoResuelto = resolverGenero(p);
      const key = normalizarGenero(generoResuelto);
      const prev = counts.get(key);
      if (prev) {
        prev.count += 1;
      } else {
        counts.set(key, { key, label: generoResuelto, count: 1 });
      }
    }

    return Array.from(counts.values()).sort((a, b) => a.label.localeCompare(b.label, "es"));
  }, [items]);

  const filtrados = useMemo(() => {
    const term = q.trim().toLowerCase();
    let list = items.filter((p) => {
      const n = (p.nombre || "").toLowerCase();
      const matchTexto = !term || n.includes(term);
      if (!matchTexto) return false;

      if (genero === "todos") return true;
      return normalizarGenero(resolverGenero(p)) === genero;
    });

    if (ordenPrecio === "precio-asc") {
      list = [...list].sort((a, b) => obtenerPrecioOrdenable(a) - obtenerPrecioOrdenable(b));
    } else if (ordenPrecio === "precio-desc") {
      list = [...list].sort((a, b) => obtenerPrecioOrdenable(b) - obtenerPrecioOrdenable(a));
    }

    return list;
  }, [items, q, genero, ordenPrecio]);

  const load = useCallback(
    async (silent = false) => {
      try {
        if (!silent) setLoading(true);
        setError("");
        const { data } = await productosApi.obtenerCatalogo();
        setItems(Array.isArray(data) ? data : []);
        return true;
      } catch {
        setError("No se pudo cargar el catalogo.");
        return false;
      } finally {
        if (!silent) setLoading(false);
      }
    },
    [],
  );

  useEffect(() => {
    let alive = true;

    const safeLoad = async () => {
      if (!alive) return;
      await load();
    };

    safeLoad();

    const interval = setInterval(async () => {
      if (!alive) return;
      await load(true);
    }, 30000);

    return () => {
      alive = false;
      clearInterval(interval);
    };
  }, [load]);

  useEffect(() => {
    if (genero === "todos") return;
    const existe = generosDisponibles.some((g) => g.key === genero);
    if (!existe) setGenero("todos");
  }, [genero, generosDisponibles]);

  const agregar = async (productoId) => {
    try {
      await carritosApi.agregarItem({ productoId, cantidad: 1 });
      await notifySuccess("Producto agregado", "Se agrego al carrito correctamente.");
    } catch {
      await notifyError("No se pudo agregar", "Intenta nuevamente en unos segundos.");
    }
  };

  const abrirDetalle = (producto) => {
    setDetalleLibro(producto);
  };

  const cerrarDetalle = () => {
    setDetalleLibro(null);
  };

  const parsearDetalleLibro = (producto) => {
    const descripcionRaw = (producto?.descripcion || "").trim();
    const editorial = (producto?.editorial || "").trim();
    const generoResuelto = resolverGenero(producto);

    const autorMatch = descripcionRaw.match(/(?:autor|autora)\s*:\s*([^|,.]+)/i);
    const isbnMatch = descripcionRaw.match(/isbn(?:-13|-10)?\s*:\s*([0-9xX\-]+)/i);

    const limpiar = (texto) =>
      (texto || "")
        .replace(/(?:editorial)\s*:\s*[^.]+\.?/gi, "")
        .replace(/(?:genero|género|categoria|categoría)\s*:\s*[^.]+\.?/gi, "")
        .replace(/(?:autor|autora)\s*:\s*[^.]+\.?/gi, "")
        .replace(/isbn(?:-13|-10)?\s*:\s*[0-9xX\-]+\.?/gi, "")
        .replace(/\s{2,}/g, " ")
        .trim();

    const sinopsis = limpiar(descripcionRaw);

    return {
      autor: (producto?.autor || autorMatch?.[1] || "").trim() || "No informado",
      genero: generoResuelto || "No informado",
      isbn: (producto?.isbn || isbnMatch?.[1] || "").trim() || "No informado",
      editorial: editorial || "No informada",
      sinopsis: sinopsis || "Sinopsis no disponible.",
    };
  };

  return (
    <main className="catalogPage">
      <header className="catalogHero">
        <div className="catalogHero__content">
          <h1 className="catalogHero__title">Compra online y retira en sucursal</h1>
          <p className="catalogHero__subtitle">
            Elegi tus libros, pagalos desde la web y hace seguimiento hasta que el retiro presencial este listo.
          </p>

          <div className="catalogHero__actions">
            <Link className="catalogHero__btn" to="/carrito">
              Ir al carrito
            </Link>

            <Link className="catalogHero__btn catalogHero__btn--ghost" to="/mis-ventas">
              Mis compras
            </Link>

            <Link className="catalogHero__btn catalogHero__btn--ghost" to="/mis-retiros">
              Estado de retiro
            </Link>
          </div>
        </div>

        <div className="catalogHero__media" aria-hidden="true">
          <div className="catalogHero__panel">
            <p className="catalogHero__hint">
              Cuando la compra se confirma, podes ver el seguimiento en la seccion "Mis compras".
            </p>
          </div>
        </div>
      </header>

      <section className="catalogBar">
        <div className="catalogBar__left">
          <h2 className="catalogBar__heading">Catalogo</h2>
        </div>

        <div className="catalogBar__right">
          <input
            className="catalogBar__search"
            placeholder="Buscar por nombre..."
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />

          <select className="catalogBar__select" value={genero} onChange={(e) => setGenero(e.target.value)}>
            <option value="todos">Todos los generos</option>
            {generosDisponibles.map((g) => (
              <option key={g.key} value={g.key}>
                {g.label} ({g.count})
              </option>
            ))}
          </select>

          <select
            className="catalogBar__select"
            value={ordenPrecio}
            onChange={(e) => setOrdenPrecio(e.target.value)}
          >
            <option value="relevancia">Orden por defecto</option>
            <option value="precio-asc">Precio: menor a mayor</option>
            <option value="precio-desc">Precio: mayor a menor</option>
          </select>

        </div>
      </section>

      {loading ? <p className="catalogState">Cargando catalogo...</p> : null}
      {error ? <p className="catalogState catalogState--error">{error}</p> : null}

      {!loading && !error ? (
        <section className="catalogGrid">
          {filtrados.map((p) => (
            <ProductCard key={p.id} producto={p} onAgregar={agregar} onVerDetalle={abrirDetalle} />
          ))}
        </section>
      ) : null}

      {detalleLibro ? (
        <DetalleLibroModal
          producto={detalleLibro}
          onClose={cerrarDetalle}
          onAgregar={agregar}
          detalle={parsearDetalleLibro(detalleLibro)}
        />
      ) : null}
    </main>
  );
}

function DetalleLibroModal({ producto, onClose, onAgregar, detalle }) {
  useEffect(() => {
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = prevOverflow;
    };
  }, []);

  const apiBase = (import.meta.env.VITE_API_BASE_URL || "https://localhost:7248").replace(/\/$/, "");
  const imagen = (producto?.imagenUrl || "").trim();
  const imageSrc = !imagen
    ? ""
    : (imagen.startsWith("http://") || imagen.startsWith("https://") || imagen.startsWith("data:"))
      ? imagen
      : `${apiBase}${imagen.startsWith("/") ? "" : "/"}${imagen}`;

  const mostrarPromo =
    !!producto?.tienePromocionActiva &&
    producto?.precioFinal != null &&
    Number(producto.precioFinal) < Number(producto.precio);

  return (
    <div className="catalogModal__overlay" role="dialog" aria-modal="true">
      <div className="catalogModal">
        <button className="catalogModal__close" type="button" onClick={onClose} aria-label="Cerrar detalle">
          ×
        </button>

        <div className="catalogModal__grid">
          <div className="catalogModal__imageWrap">
            {imageSrc ? <img className="catalogModal__image" src={imageSrc} alt={producto?.nombre} /> : <div className="catalogModal__fallback">Sin imagen</div>}
          </div>

          <div className="catalogModal__body">
            <h3 className="catalogModal__title">{producto?.nombre}</h3>

            <div className="catalogModal__meta">
              <p><strong>Autor:</strong> {detalle.autor}</p>
              <p><strong>Genero:</strong> {detalle.genero}</p>
              <p><strong>Editorial:</strong> {detalle.editorial}</p>
              <p><strong>ISBN:</strong> {detalle.isbn}</p>
            </div>

            <div className="catalogModal__synopsis">
              <strong>Sinopsis</strong>
              <p>{detalle.sinopsis}</p>
            </div>

            <div className="catalogModal__footer">
              <div className="catalogModal__price">
                {mostrarPromo ? (
                  <>
                    <span className="catalogModal__priceOld">${Number(producto?.precio).toFixed(2)}</span>
                    <span>${Number(producto?.precioFinal).toFixed(2)} ARS</span>
                  </>
                ) : (
                  <span>${Number(producto?.precio).toFixed(2)} ARS</span>
                )}
              </div>

              <button
                className="catalogModal__addBtn"
                type="button"
                disabled={(producto?.stock ?? 0) <= 0}
                onClick={() => onAgregar?.(producto?.id)}
              >
                {(producto?.stock ?? 0) <= 0 ? "Sin stock" : "Agregar al carrito"}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

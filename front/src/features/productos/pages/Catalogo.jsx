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
      const d = (p.descripcion || "").toLowerCase();
      const matchTexto = !term || n.includes(term) || d.includes(term);
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

  const recargarCatalogo = async () => {
    const ok = await load(true);
    if (ok) {
      await notifySuccess("Catalogo actualizado", "Se actualizaron stock, precios y filtros de genero.");
    } else {
      await notifyError("No se pudo actualizar", "Intenta nuevamente en unos segundos.");
    }
  };

  const agregar = async (productoId) => {
    try {
      await carritosApi.agregarItem({ productoId, cantidad: 1 });
      await notifySuccess("Producto agregado", "Se agrego al carrito correctamente.");
    } catch {
      await notifyError("No se pudo agregar", "Intenta nuevamente en unos segundos.");
    }
  };

  return (
    <main className="catalogPage">
      <header className="catalogHero">
        <div className="catalogHero__content">
          <p className="catalogHero__tag">LIBRERIA COMPUMAX</p>
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
            <article className="catalogHero__stat">
              <span className="catalogHero__statLabel">Libros disponibles</span>
              <strong className="catalogHero__statValue">{items.length}</strong>
            </article>

            <article className="catalogHero__stat">
              <span className="catalogHero__statLabel">Promociones activas</span>
              <strong className="catalogHero__statValue">{totalPromos}</strong>
            </article>

            <article className="catalogHero__stat">
              <span className="catalogHero__statLabel">Modalidad</span>
              <strong className="catalogHero__statValue">Retiro presencial</strong>
            </article>

            <p className="catalogHero__hint">
              Cuando la compra se confirma, podes ver el seguimiento en la seccion "Mis compras".
            </p>
          </div>
        </div>
      </header>

      <section className="catalogBar">
        <div className="catalogBar__left">
          <h2 className="catalogBar__heading">Catalogo</h2>
          <span className="catalogBar__count">{filtrados.length} productos</span>
          <span className="catalogBar__count">{generosDisponibles.length} generos</span>
        </div>

        <div className="catalogBar__right">
          <input
            className="catalogBar__search"
            placeholder="Buscar por nombre o descripcion..."
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

          <button type="button" className="catalogBar__refresh" onClick={recargarCatalogo}>
            Actualizar
          </button>
        </div>
      </section>

      {loading ? <p className="catalogState">Cargando catalogo...</p> : null}
      {error ? <p className="catalogState catalogState--error">{error}</p> : null}

      {!loading && !error ? (
        <section className="catalogGrid">
          {filtrados.map((p) => (
            <ProductCard key={p.id} producto={p} onAgregar={agregar} />
          ))}
        </section>
      ) : null}
    </main>
  );
}

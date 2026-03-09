import { useEffect, useMemo, useState } from "react";
import {
  crearPromocion,
  desactivarPromocion,
  desactivarPromocionPorEditorial,
  desactivarPromocionPorGenero,
  listarProductos,
  listarPromociones,
} from "../api/adminCompras.api";
import { confirmAction } from "@/shared/ui/sweetAlert";
import "../styles/ComprasCommon.css";
import "./PromocionesCompras.css";

const GENERO_PALABRAS_CLAVE = [
  ["ciencia ficcion", "Ciencia ficcion"],
  ["ficcion", "Ficcion"],
  ["novela", "Novela"],
  ["terror", "Terror"],
  ["romance", "Romance"],
  ["fantasia", "Fantasia"],
  ["historia", "Historia"],
  ["historico", "Historico"],
  ["biografia", "Biografia"],
  ["poesia", "Poesia"],
  ["infantil", "Infantil"],
  ["juvenil", "Juvenil"],
  ["aventura", "Aventura"],
  ["drama", "Drama"],
  ["policial", "Policial"],
  ["misterio", "Misterio"],
  ["autoayuda", "Autoayuda"],
  ["clasico", "Clasico"],
];
const SIN_EDITORIAL_VALUE = "__sin_editorial__";

function normalizarTexto(valor) {
  return (valor ?? "")
    .toString()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

function capitalizar(valor) {
  if (!valor) return valor;
  return valor.charAt(0).toUpperCase() + valor.slice(1);
}

function resolverImagenPromo(url) {
  if (!url) return "";
  if (/^https?:\/\//i.test(url) || url.startsWith("data:image")) return url;
  const apiBase = (import.meta.env.VITE_API_BASE_URL || "https://localhost:7248").replace(/\/$/, "");
  return `${apiBase}${url.startsWith("/") ? "" : "/"}${url}`;
}

function extraerValorDeDescripcion(descripcion, campo) {
  const texto = (descripcion ?? "").toString();
  if (!texto.trim()) return "";

  const regex = campo === "genero"
    ? /(?:genero|género)\s*:\s*([^.]+)/i
    : /editorial\s*:\s*([^.]+)/i;
  const match = texto.match(regex);
  return match?.[1] ? capitalizar(match[1].trim().toLowerCase()) : "";
}

function detectarGeneroDeProducto(producto) {
  const generoDesc = extraerValorDeDescripcion(producto?.descripcion, "genero");
  if (generoDesc) return generoDesc;

  const descripcion = (producto?.descripcion || "").trim();
  if (!descripcion) return "";

  const descNorm = normalizarTexto(descripcion);
  const keyword = GENERO_PALABRAS_CLAVE.find(([term]) => descNorm.includes(term));
  return keyword ? keyword[1] : "";
}

function validarPromo({ porcentajeDescuento, montoDescuento, fechaInicio, fechaFin }) {
  const porcentaje = porcentajeDescuento ? Number(porcentajeDescuento) : null;
  const monto = montoDescuento ? Number(montoDescuento) : null;
  const tienePorcentaje = porcentaje != null && Number.isFinite(porcentaje) && porcentaje > 0;
  const tieneMonto = monto != null && Number.isFinite(monto) && monto > 0;

  if (tienePorcentaje && tieneMonto) {
    return { error: "Elegi un solo tipo de descuento: porcentaje o monto fijo." };
  }
  if (!tienePorcentaje && !tieneMonto) {
    return { error: "Debes indicar un descuento valido mayor a 0." };
  }
  if (porcentaje != null && porcentaje <= 0) {
    return { error: "El porcentaje debe ser mayor a 0." };
  }
  if (monto != null && monto <= 0) {
    return { error: "El monto fijo debe ser mayor a 0." };
  }
  if (fechaInicio && fechaFin && fechaFin < fechaInicio) {
    return { error: "La fecha fin no puede ser menor que la fecha inicio." };
  }

  return { porcentaje, monto, error: "" };
}

function confirmarQuita(texto) {
  return confirmAction({
    title: "Confirmar quita",
    text: texto,
    confirmText: "Quitar",
    cancelText: "Cancelar",
    icon: "warning",
  });
}

export default function PromocionesCompras() {
  const [items, setItems] = useState([]);
  const [productos, setProductos] = useState([]);
  const [msg, setMsg] = useState("");
  const [err, setErr] = useState("");
  const [savingLibro, setSavingLibro] = useState(false);
  const [savingGenero, setSavingGenero] = useState(false);

  const [bookForm, setBookForm] = useState({
    editorial: "",
    productoId: "",
    porcentajeDescuento: "",
    montoDescuento: "",
    fechaInicio: "",
    fechaFin: "",
    activa: true,
  });

  const [genreForm, setGenreForm] = useState({
    genero: "",
    porcentajeDescuento: "",
    montoDescuento: "",
    fechaInicio: "",
    fechaFin: "",
    activa: true,
  });

  const cargar = () => listarPromociones({ activas: true }).then(setItems).catch(() => setItems([]));

  useEffect(() => {
    cargar();
    listarProductos().then(setProductos).catch(() => setProductos([]));
  }, []);

  const productosEnriquecidos = useMemo(() => {
    return (productos ?? []).map((p) => ({
      ...p,
      _genero: detectarGeneroDeProducto(p),
      _editorial: (p.editorial ?? "").trim() || extraerValorDeDescripcion(p.descripcion, "editorial"),
    }));
  }, [productos]);

  const generosDisponibles = useMemo(() => {
    const map = new Map();
    for (const p of productosEnriquecidos) {
      if (!p._genero) continue;
      const key = normalizarTexto(p._genero);
      if (!map.has(key)) map.set(key, p._genero);
    }
    return Array.from(map.values()).sort((a, b) => a.localeCompare(b, "es"));
  }, [productosEnriquecidos]);

  const editorialesDisponibles = useMemo(() => {
    const map = new Map();
    for (const p of productosEnriquecidos) {
      const editorial = (p._editorial ?? "").trim();
      if (!editorial) {
        if (!map.has(SIN_EDITORIAL_VALUE)) map.set(SIN_EDITORIAL_VALUE, "Sin editorial");
        continue;
      }
      const key = normalizarTexto(editorial);
      if (!map.has(key)) map.set(key, editorial);
    }
    return Array.from(map.values()).sort((a, b) => a.localeCompare(b, "es"));
  }, [productosEnriquecidos]);

  const librosFiltrados = useMemo(() => {
    const editorial = bookForm.editorial === SIN_EDITORIAL_VALUE
      ? SIN_EDITORIAL_VALUE
      : normalizarTexto(bookForm.editorial);

    return productosEnriquecidos.filter((p) => {
      const editorialProd = normalizarTexto(p._editorial);
      const editorialOk = !editorial
        || (editorial === SIN_EDITORIAL_VALUE && !editorialProd)
        || (editorial !== SIN_EDITORIAL_VALUE && editorialProd === editorial);
      return editorialOk;
    });
  }, [productosEnriquecidos, bookForm.editorial]);

  const libroSeleccionado = useMemo(() => {
    if (!bookForm.productoId) return null;
    return productosEnriquecidos.find((p) => String(p.id) === String(bookForm.productoId)) ?? null;
  }, [productosEnriquecidos, bookForm.productoId]);

  const generoPorProductoId = useMemo(() => {
    const map = new Map();
    for (const p of productosEnriquecidos) {
      map.set(String(p.id), p._genero || "");
    }
    return map;
  }, [productosEnriquecidos]);

  const resumenGenero = useMemo(() => {
    const genero = normalizarTexto(genreForm.genero);
    if (!genero) return { cantidad: 0, editoriales: [], ejemplos: [] };

    const libros = productosEnriquecidos.filter((p) => normalizarTexto(p._genero) === genero);
    const editoriales = Array.from(new Set(libros.map((l) => l._editorial).filter(Boolean))).slice(0, 4);
    const ejemplos = libros.map((l) => l.nombre).slice(0, 5);

    return { cantidad: libros.length, editoriales, ejemplos };
  }, [productosEnriquecidos, genreForm.genero]);

  async function submitPromoLibro(e) {
    e.preventDefault();
    setErr("");
    setMsg("");

    const aplicaLibro = Boolean(bookForm.productoId);
    const aplicaEditorial = Boolean(bookForm.editorial);
    if (!aplicaLibro && !aplicaEditorial) {
      setErr("Selecciona un libro, una editorial o ambos.");
      return;
    }

    const validacion = validarPromo(bookForm);
    if (validacion.error) {
      setErr(validacion.error);
      return;
    }

    setSavingLibro(true);
    try {
      const objetivos = new Map();

      if (aplicaLibro) {
        const libro = productosEnriquecidos.find((p) => String(p.id) === String(bookForm.productoId));
        if (!libro) {
          setErr("El libro seleccionado no existe.");
          return;
        }
        objetivos.set(Number(libro.id), libro);
      }

      if (aplicaEditorial) {
        if (!librosFiltrados.length) {
          setErr("No hay libros para la editorial seleccionada.");
          return;
        }
        for (const p of librosFiltrados) {
          objetivos.set(Number(p.id), p);
        }
      }

      await Promise.all(Array.from(objetivos.values()).map((p) => crearPromocion({
          productoId: Number(p.id),
          genero: null,
          porcentajeDescuento: validacion.porcentaje,
          montoDescuento: validacion.monto,
          fechaInicio: bookForm.fechaInicio,
          fechaFin: bookForm.fechaFin,
          activa: true,
        })));

      const total = objetivos.size;
      const partes = [];
      if (aplicaLibro) partes.push("libro");
      if (aplicaEditorial) partes.push("editorial");
      setMsg(`Promocion por ${partes.join(" y ")} asignada a ${total} libro(s).`);

      setBookForm({
        editorial: "",
        productoId: "",
        porcentajeDescuento: "",
        montoDescuento: "",
        fechaInicio: "",
        fechaFin: "",
        activa: true,
      });
      await cargar();
    } catch (error) {
      setErr(error?.response?.data?.error ?? "No se pudo crear la promocion por libro.");
    } finally {
      setSavingLibro(false);
    }
  }

  async function submitPromoGenero(e) {
    e.preventDefault();
    setErr("");
    setMsg("");

    if (!genreForm.genero) {
      setErr("Selecciona un genero para crear la promocion por genero.");
      return;
    }

    const validacion = validarPromo(genreForm);
    if (validacion.error) {
      setErr(validacion.error);
      return;
    }

    setSavingGenero(true);
    try {
      await crearPromocion({
        productoId: null,
        genero: genreForm.genero,
        porcentajeDescuento: validacion.porcentaje,
        montoDescuento: validacion.monto,
        fechaInicio: genreForm.fechaInicio,
        fechaFin: genreForm.fechaFin,
        activa: true,
      });
      setMsg("Promocion por genero asignada con exito.");
      setGenreForm({
        genero: "",
        porcentajeDescuento: "",
        montoDescuento: "",
        fechaInicio: "",
        fechaFin: "",
        activa: true,
      });
      await cargar();
    } catch (error) {
      setErr(error?.response?.data?.error ?? "No se pudo crear la promocion por genero.");
    } finally {
      setSavingGenero(false);
    }
  }

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Promociones</h1>
          <p className="cmuted">Creacion separada y consistente: por libro o por genero.</p>
        </div>
      </header>

      <section className="ccard ccard__pad promoCreateSection">
        <div className="promoCreateGrid">
          <article className="promoCreateCard">
            <h3 className="promoCreateTitle">Promocion por libro</h3>
            <p className="cmuted">Puedes aplicar por libro, por editorial o por ambos en un solo guardado.</p>

            <form className="agForm" onSubmit={submitPromoLibro}>
              <label>Editorial (opcional)
                <select
                  value={bookForm.editorial}
                  onChange={(e) => setBookForm((p) => ({ ...p, editorial: e.target.value }))}
                >
                  <option value="">No aplicar por editorial</option>
                  {editorialesDisponibles.map((ed) => {
                    const value = ed === "Sin editorial" ? SIN_EDITORIAL_VALUE : ed;
                    return <option key={value} value={value}>{ed}</option>;
                  })}
                </select>
              </label>

              <label>Libro seleccionado (opcional)
                <select value={bookForm.productoId} onChange={(e) => setBookForm((p) => ({ ...p, productoId: e.target.value }))}>
                  <option value="">No aplicar por libro</option>
                  {librosFiltrados.map((p) => (
                    <option key={p.id} value={p.id}>{p.nombre}</option>
                  ))}
                </select>
              </label>

              {libroSeleccionado ? (
                <div className="promoInfoBox">
                  <strong>{libroSeleccionado.nombre}</strong>
                  <span>Genero: {libroSeleccionado._genero || "Sin genero"}</span>
                  <span>Editorial: {libroSeleccionado._editorial || "Sin editorial"}</span>
                  <span>Precio: ${Number(libroSeleccionado.precio ?? 0).toFixed(2)} | Stock: {libroSeleccionado.stock ?? 0}</span>
                </div>
              ) : null}

              {bookForm.editorial ? (
                <div className="promoInfoBox">
                  <strong>Editorial: {bookForm.editorial === SIN_EDITORIAL_VALUE ? "Sin editorial" : bookForm.editorial}</strong>
                  <span>Libros alcanzados: {librosFiltrados.length}</span>
                </div>
              ) : null}

              <div className="agFormRow">
                <label>Porcentaje
                  <input
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    value={bookForm.porcentajeDescuento}
                    disabled={Boolean(bookForm.montoDescuento)}
                    onChange={(e) => setBookForm((p) => ({
                      ...p,
                      porcentajeDescuento: e.target.value,
                      montoDescuento: e.target.value ? "" : p.montoDescuento,
                    }))}
                  />
                </label>
                <label>Monto fijo
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={bookForm.montoDescuento}
                    disabled={Boolean(bookForm.porcentajeDescuento)}
                    onChange={(e) => setBookForm((p) => ({
                      ...p,
                      montoDescuento: e.target.value,
                      porcentajeDescuento: e.target.value ? "" : p.porcentajeDescuento,
                    }))}
                  />
                </label>
              </div>

              <div className="agFormRow">
                <label>Inicio
                  <input type="date" value={bookForm.fechaInicio} onChange={(e) => setBookForm((p) => ({ ...p, fechaInicio: e.target.value }))} />
                </label>
                <label>Fin
                  <input
                    type="date"
                    min={bookForm.fechaInicio || undefined}
                    value={bookForm.fechaFin}
                    onChange={(e) => setBookForm((p) => ({ ...p, fechaFin: e.target.value }))}
                  />
                </label>
              </div>

              <button className="btn btn--primary" type="submit" disabled={savingLibro}>
                {savingLibro ? "Guardando..." : "Guardar promo por libro/editorial"}
              </button>
            </form>
          </article>

          <article className="promoCreateCard">
            <h3 className="promoCreateTitle">Promocion por genero</h3>
            <p className="cmuted">Seleccionas 1 genero y se aplica a todos los libros de ese genero.</p>

            <form className="agForm" onSubmit={submitPromoGenero}>
              <label>Genero
                <select value={genreForm.genero} onChange={(e) => setGenreForm((p) => ({ ...p, genero: e.target.value }))}>
                  <option value="">Seleccionar genero</option>
                  {generosDisponibles.map((g) => (
                    <option key={g} value={g}>{g}</option>
                  ))}
                </select>
              </label>

              {genreForm.genero ? (
                <div className="promoInfoBox">
                  <strong>{genreForm.genero}</strong>
                  <span>Libros alcanzados: {resumenGenero.cantidad}</span>
                  <span>Editoriales: {resumenGenero.editoriales.length ? resumenGenero.editoriales.join(", ") : "Sin datos"}</span>
                  <span>Ejemplos: {resumenGenero.ejemplos.length ? resumenGenero.ejemplos.join(" | ") : "Sin libros en este genero"}</span>
                </div>
              ) : null}

              <div className="agFormRow">
                <label>Porcentaje
                  <input
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    value={genreForm.porcentajeDescuento}
                    disabled={Boolean(genreForm.montoDescuento)}
                    onChange={(e) => setGenreForm((p) => ({
                      ...p,
                      porcentajeDescuento: e.target.value,
                      montoDescuento: e.target.value ? "" : p.montoDescuento,
                    }))}
                  />
                </label>
                <label>Monto fijo
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={genreForm.montoDescuento}
                    disabled={Boolean(genreForm.porcentajeDescuento)}
                    onChange={(e) => setGenreForm((p) => ({
                      ...p,
                      montoDescuento: e.target.value,
                      porcentajeDescuento: e.target.value ? "" : p.porcentajeDescuento,
                    }))}
                  />
                </label>
              </div>

              <div className="agFormRow">
                <label>Inicio
                  <input type="date" value={genreForm.fechaInicio} onChange={(e) => setGenreForm((p) => ({ ...p, fechaInicio: e.target.value }))} />
                </label>
                <label>Fin
                  <input
                    type="date"
                    min={genreForm.fechaInicio || undefined}
                    value={genreForm.fechaFin}
                    onChange={(e) => setGenreForm((p) => ({ ...p, fechaFin: e.target.value }))}
                  />
                </label>
              </div>

              <button className="btn btn--primary" type="submit" disabled={savingGenero}>
                {savingGenero ? "Guardando..." : "Guardar promo por genero"}
              </button>
            </form>
          </article>
        </div>

        {msg ? <p className="agOk">{msg}</p> : null}
        {err ? <p className="agErr">{err}</p> : null}
      </section>

      <section className="ccard ccard__pad">
        <div className="promoLoadedHead">
          <h3 className="promoLoadedTitle">Promociones cargadas</h3>
          <span className="promoLoadedCount">{(items ?? []).length} activas</span>
        </div>

        {(items ?? []).length === 0 ? (
          <div className="promoEmpty">
            <strong>No hay promociones activas.</strong>
            <p>Crea una promocion por libro o genero desde los bloques superiores.</p>
          </div>
        ) : (
          <div className="promoLoadedGrid">
            {(items ?? []).map((item) => (
              <article className="promoCard" key={item.id}>
                {(() => {
                  const generoAccion = item.genero || (item.productoId ? (generoPorProductoId.get(String(item.productoId)) || "") : "");
                  return (
                    <>
                <div className="promoCard__top">
                  <div className="promoCard__titleWrap">
                    {item.productoImagenUrl ? (
                      <img
                        className="promoCard__thumb"
                        src={resolverImagenPromo(item.productoImagenUrl)}
                        alt={item.productoNombre || item.nombre}
                      />
                    ) : null}
                    <h4 className="promoCard__name">{item.productoNombre || item.nombre}</h4>
                  </div>
                </div>

                <div className="promoCard__meta">
                  {item.productoNombre ? <span className="promoPill">Libro: {item.productoNombre}</span> : null}
                  {item.productoEditorial ? <span className="promoPill">Editorial: {item.productoEditorial}</span> : null}
                  {generoAccion ? <span className="promoPill promoPill--alt">Genero: {generoAccion}</span> : null}
                </div>

                <div className="promoCard__actions">
                  <button
                    className="btn btn--ghost btn--sm"
                    onClick={async () => {
                      if (!await confirmarQuita("Quieres quitar esta promocion?")) return;
                      await desactivarPromocion(item.id);
                      setMsg("Promocion quitada.");
                      await cargar();
                    }}
                  >
                    Quitar
                  </button>

                  {item.productoEditorial ? (
                    <button
                      className="btn btn--ghost btn--sm"
                      onClick={async () => {
                        if (!await confirmarQuita(`Quieres quitar todas las promociones de la editorial "${item.productoEditorial}"?`)) return;
                        await desactivarPromocionPorEditorial(item.productoEditorial);
                        setMsg("Promociones por editorial quitadas.");
                        await cargar();
                      }}
                    >
                      Quitar por editorial
                    </button>
                  ) : null}

                  {generoAccion ? (
                    <button
                      className="btn btn--ghost btn--sm"
                      onClick={async () => {
                        if (!await confirmarQuita(`Quieres quitar todas las promociones del genero "${generoAccion}"?`)) return;
                        await desactivarPromocionPorGenero(generoAccion);
                        setMsg("Promociones por genero quitadas.");
                        await cargar();
                      }}
                    >
                      Quitar por genero
                    </button>
                  ) : null}
                </div>
                    </>
                  );
                })()}
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}

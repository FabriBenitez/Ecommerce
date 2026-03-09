import { useEffect, useMemo, useState } from "react";
import {
  actualizarProducto,
  crearProducto,
  eliminarProducto,
  importarProductosCsv,
  listarProductos,
  listarProveedores,
  obtenerStockMinimo,
} from "../api/adminCompras.api";
import StockBadge from "../components/StockBadge";
import { confirmAction } from "@/shared/ui/sweetAlert";
import "../styles/ComprasCommon.css";
import "./StockInventario.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });
const tiposSugeridos = [
  "Aventura",
  "Ciencia ficcion",
  "Fantasia",
  "Novela",
  "Infantil",
  "Historia",
  "Terror",
  "Romance",
  "Autoayuda",
  "Biografia",
];

function resolverImagenUrl(url) {
  if (!url) return "";
  if (url.startsWith("data:image/")) return url;
  if (/^https?:\/\//i.test(url)) return url;

  const base = import.meta.env.VITE_API_BASE_URL || "https://localhost:7248";
  const origenApi = new URL(base).origin;
  return `${origenApi}${url.startsWith("/") ? "" : "/"}${url}`;
}

function extraerMetadata(descripcion, campo) {
  const texto = (descripcion ?? "").toString();
  if (!texto.trim()) return "";
  const patron = campo === "genero" ? /(?:genero|g[ée]nero)\s*:\s*([^.]+)/i : /editorial\s*:\s*([^.]+)/i;
  const match = texto.match(patron);
  return match?.[1]?.trim() ?? "";
}

function limpiarMetadataDescripcion(descripcion) {
  return (descripcion ?? "")
    .toString()
    .replace(/^\s*editorial\s*:\s*[^.]+\.?\s*/i, "")
    .replace(/^\s*(?:genero|g[ée]nero)\s*:\s*[^.]+\.?\s*/i, "")
    .trim();
}

function descomponerDescripcion(producto) {
  const editorial = (producto.editorial ?? "").trim() || extraerMetadata(producto.descripcion, "editorial");
  const genero = extraerMetadata(producto.descripcion, "genero");
  const sinopsis = limpiarMetadataDescripcion(producto.descripcion);
  return { editorial, genero, sinopsis };
}

export default function StockInventario() {
  const [minStock, setMinStock] = useState(10);
  const [productos, setProductos] = useState([]);
  const [proveedores, setProveedores] = useState([]);
  const [q, setQ] = useState("");
  const [soloBajo, setSoloBajo] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");
  const [eliminandoId, setEliminandoId] = useState(0);
  const [guardandoId, setGuardandoId] = useState(0);
  const [editandoId, setEditandoId] = useState(0);

  const [nuevo, setNuevo] = useState({
    nombre: "",
    tipo: "",
    editorial: "",
    descripcion: "",
    precio: "",
    stock: "",
    imagenFile: null,
    proveedorId: "",
  });

  const [edicion, setEdicion] = useState({
    nombre: "",
    tipo: "",
    editorial: "",
    descripcion: "",
    precio: "",
    stock: "",
    imagenFile: null,
    proveedorId: "",
  });

  const [file, setFile] = useState(null);
  const [proveedorImportId, setProveedorImportId] = useState("");

  const cargar = async () => {
    const [data, min, provs] = await Promise.all([listarProductos(), obtenerStockMinimo(), listarProveedores(true)]);
    setProductos(Array.isArray(data) ? data : []);
    setMinStock(Number(min || 10));
    setProveedores(Array.isArray(provs) ? provs : []);
  };

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        await cargar();
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el inventario.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, []);

  const filtrados = useMemo(() => {
    const qq = q.trim().toLowerCase();
    return (productos ?? [])
      .filter((p) => {
        const name = (p.nombre ?? "").toLowerCase();
        const prov = (p.proveedorNombre ?? "").toLowerCase();
        const okText = !qq || name.includes(qq) || prov.includes(qq) || String(p.id).includes(qq);
        if (!okText) return false;
        if (!soloBajo) return true;
        return Number(p.stockDisponible ?? p.stock ?? 0) <= minStock;
      })
      .sort((a, b) => Number(a.stockDisponible ?? a.stock ?? 0) - Number(b.stockDisponible ?? b.stock ?? 0));
  }, [productos, q, soloBajo, minStock]);

  const countBajo = useMemo(
    () => (productos ?? []).filter((p) => Number(p.stockDisponible ?? p.stock ?? 0) <= minStock).length,
    [productos, minStock],
  );

  const crearLibro = async () => {
    try {
      setError("");
      setMsg("");
      if (!nuevo.nombre?.trim()) {
        setError("El nombre es obligatorio.");
        return;
      }
      if (!nuevo.tipo?.trim()) {
        setError("El genero del libro es obligatorio.");
        return;
      }
      if (!nuevo.editorial?.trim()) {
        setError("La editorial es obligatoria.");
        return;
      }
      if (!nuevo.descripcion?.trim()) {
        setError("La sinopsis es obligatoria.");
        return;
      }
      if (!String(nuevo.precio ?? "").trim() || Number(nuevo.precio) <= 0) {
        setError("El precio es obligatorio y debe ser mayor a 0.");
        return;
      }
      if (!String(nuevo.stock ?? "").trim() || Number(nuevo.stock) < 0) {
        setError("El stock inicial es obligatorio y no puede ser negativo.");
        return;
      }
      if (!nuevo.imagenFile) {
        setError("La imagen del libro es obligatoria.");
        return;
      }

      const descripcionNormalizada = nuevo.descripcion.trim();
      const tipoNormalizado = nuevo.tipo.trim();
      const editorialNormalizada = nuevo.editorial.trim();
      const descripcionConTipo = `Editorial: ${editorialNormalizada}. Genero: ${tipoNormalizado}. ${descripcionNormalizada}`;

      const ok = await confirmAction({
        title: "Crear libro",
        text: `Se creara el libro "${nuevo.nombre.trim()}" en inventario.`,
        confirmText: "Si, crear",
        cancelText: "Cancelar",
        icon: "warning",
      });
      if (!ok) return;

      await crearProducto({
        nombre: nuevo.nombre.trim(),
        descripcion: descripcionConTipo,
        editorial: editorialNormalizada,
        precio: Number(nuevo.precio || 0),
        stock: Number(nuevo.stock || 0),
        imagenFile: nuevo.imagenFile,
        proveedorId: nuevo.proveedorId ? Number(nuevo.proveedorId) : undefined,
      });

      setMsg("Libro creado correctamente.");
      setNuevo({
        nombre: "",
        tipo: "",
        editorial: "",
        descripcion: "",
        precio: "",
        stock: "",
        imagenFile: null,
        proveedorId: "",
      });
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? "No se pudo crear el libro.");
    }
  };

  const importar = async () => {
    if (!file) return;
    const ok = await confirmAction({
      title: "Importar archivo",
      text: `Se importaran libros desde "${file.name}".`,
      confirmText: "Si, importar",
      cancelText: "Cancelar",
      icon: "warning",
    });
    if (!ok) return;

    try {
      setError("");
      setMsg("");
      const resp = await importarProductosCsv(file, proveedorImportId ? Number(proveedorImportId) : undefined);
      setMsg(`Importacion completa. Creados: ${resp?.creados ?? 0}.`);
      setFile(null);
      await cargar();
    } catch (e) {
      const backendData = e?.response?.data;
      const backendMessage = backendData?.error
        ?? backendData?.title
        ?? (typeof backendData === "string" ? backendData : null)
        ?? "No se pudo importar el archivo.";
      console.error("Error importacion productos:", backendData ?? e);
      setError(backendMessage);
    }
  };

  const iniciarEdicion = (p) => {
    const metadata = descomponerDescripcion(p);
    setEditandoId(Number(p.id));
    setEdicion({
      nombre: p.nombre ?? "",
      tipo: metadata.genero ?? "",
      editorial: metadata.editorial ?? "",
      descripcion: metadata.sinopsis ?? "",
      precio: String(p.precio ?? ""),
      stock: String(p.stock ?? ""),
      imagenFile: null,
      proveedorId: p.proveedorId ? String(p.proveedorId) : "",
    });
  };

  const cancelarEdicion = () => {
    setEditandoId(0);
    setEdicion({
      nombre: "",
      tipo: "",
      editorial: "",
      descripcion: "",
      precio: "",
      stock: "",
      imagenFile: null,
      proveedorId: "",
    });
  };

  const guardarEdicion = async (productoId) => {
    try {
      setError("");
      setMsg("");
      if (!edicion.nombre?.trim()) {
        setError("El nombre es obligatorio.");
        return;
      }
      if (!edicion.tipo?.trim()) {
        setError("El genero del libro es obligatorio.");
        return;
      }
      if (!edicion.editorial?.trim()) {
        setError("La editorial es obligatoria.");
        return;
      }
      if (!edicion.descripcion?.trim()) {
        setError("La sinopsis es obligatoria.");
        return;
      }
      if (!String(edicion.precio ?? "").trim() || Number(edicion.precio) <= 0) {
        setError("El precio debe ser mayor a 0.");
        return;
      }
      if (!String(edicion.stock ?? "").trim() || Number(edicion.stock) < 0) {
        setError("El stock no puede ser negativo.");
        return;
      }

      const descripcionConTipo = `Editorial: ${edicion.editorial.trim()}. Genero: ${edicion.tipo.trim()}. ${edicion.descripcion.trim()}`;
      const ok = await confirmAction({
        title: "Guardar cambios",
        text: `Se actualizara el libro "${edicion.nombre.trim()}".`,
        confirmText: "Si, guardar",
        cancelText: "Cancelar",
        icon: "warning",
      });
      if (!ok) return;

      setGuardandoId(Number(productoId));
      await actualizarProducto(productoId, {
        nombre: edicion.nombre.trim(),
        descripcion: descripcionConTipo,
        editorial: edicion.editorial.trim(),
        precio: Number(edicion.precio),
        stock: Number(edicion.stock),
        imagenFile: edicion.imagenFile ?? undefined,
        proveedorId: edicion.proveedorId ? Number(edicion.proveedorId) : undefined,
      });
      setMsg("Libro actualizado correctamente.");
      cancelarEdicion();
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo actualizar el libro.");
    } finally {
      setGuardandoId(0);
    }
  };

  const eliminarLibro = async (p) => {
    const ok = await confirmAction({
      title: "Eliminar libro",
      text: `Se eliminara "${p.nombre}". Esta accion no se puede deshacer.`,
      confirmText: "Si, eliminar",
      cancelText: "Cancelar",
      icon: "warning",
    });
    if (!ok) return;

    try {
      setError("");
      setMsg("");
      setEliminandoId(Number(p.id || 0));
      await eliminarProducto(p.id);
      setMsg(`Libro "${p.nombre}" eliminado correctamente.`);
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo eliminar el libro.");
    } finally {
      setEliminandoId(0);
    }
  };

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Stock e inventario</h1>
          <p className="cmuted">
            Stock minimo: <b>{minStock}</b> - Libros con stock bajo: <b>{countBajo}</b>
          </p>
        </div>
      </header>

      <section className="ccard ccard__pad">
        <div className="toolbar">
          <label className="field">
            <span>Buscar</span>
            <input
              className="cinput"
              placeholder="Por nombre, proveedor o #id..."
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
          </label>

          <label className="field" style={{ alignSelf: "end" }}>
            <span>&nbsp;</span>
            <button
              className={`btn ${soloBajo ? "btn--primary" : "btn--ghost"}`}
              type="button"
              onClick={() => setSoloBajo((v) => !v)}
            >
              {soloBajo ? "Mostrando stock bajo" : "Ver solo stock bajo"}
            </button>
          </label>
        </div>
      </section>

      <section className="stockForms">
        <article className="ccard ccard__pad stockCard">
          <h3 className="stockCard__title">Carga manual de libros</h3>
          <p className="stockCard__desc">Alta individual con imagen desde tu PC. Todos los campos son obligatorios.</p>

          <div className="stockCard__grid">
            <label>
              <span>Nombre</span>
              <input value={nuevo.nombre} onChange={(e) => setNuevo((p) => ({ ...p, nombre: e.target.value }))} />
            </label>

            <label>
              <span>Genero</span>
              <input
                list="tipos-libro"
                placeholder="Ej: Aventura, Fantasia..."
                value={nuevo.tipo}
                onChange={(e) => setNuevo((p) => ({ ...p, tipo: e.target.value }))}
              />
              <datalist id="tipos-libro">
                {tiposSugeridos.map((tipo) => <option key={tipo} value={tipo} />)}
              </datalist>
            </label>

            <label>
              <span>Editorial</span>
              <input
                placeholder="Ej: Planeta, Sudamericana..."
                value={nuevo.editorial}
                onChange={(e) => setNuevo((p) => ({ ...p, editorial: e.target.value }))}
              />
            </label>

            <label>
              <span>Sinopsis</span>
              <input value={nuevo.descripcion} onChange={(e) => setNuevo((p) => ({ ...p, descripcion: e.target.value }))} />
            </label>

            <label>
              <span>Precio</span>
              <input type="number" value={nuevo.precio} onChange={(e) => setNuevo((p) => ({ ...p, precio: e.target.value }))} />
            </label>

            <label>
              <span>Stock inicial</span>
              <input type="number" value={nuevo.stock} onChange={(e) => setNuevo((p) => ({ ...p, stock: e.target.value }))} />
            </label>

            <label>
              <span>Proveedor asociado (opcional)</span>
              <select value={nuevo.proveedorId} onChange={(e) => setNuevo((p) => ({ ...p, proveedorId: e.target.value }))}>
                <option value="">Sin asociar</option>
                {(proveedores ?? []).map((prov) => (
                  <option key={prov.id} value={prov.id}>
                    {prov.razonSocial}
                  </option>
                ))}
              </select>
            </label>

            <label className="stockCard__file">
              <span>Imagen</span>
              <input type="file" accept="image/*" onChange={(e) => setNuevo((p) => ({ ...p, imagenFile: e.target.files?.[0] ?? null }))} />
            </label>
          </div>

          <div className="stockCard__actions">
            <button className="btn btn--primary" type="button" onClick={crearLibro}>Guardar libro</button>
          </div>
        </article>

        <article className="ccard ccard__pad stockCard">
          <h3 className="stockCard__title">Reposicion masiva por archivo</h3>
          <p className="stockCard__desc">
            Carga un solo archivo <b>.xlsx</b> o <b>.csv</b>. La imagen debe informarse en la columna <code>imagenBase64</code>; no insertes imagenes pegadas dentro de la hoja.
          </p>

          <div className="stockHelp">
            <div><b>Columnas obligatorias:</b> nombre, descripcion, precio, stock, imagenBase64, genero, editorial</div>
            <div><b>Formatos admitidos en imagenBase64:</b> URL <code>http/https</code>, <code>data:image/png;base64,...</code> o base64 puro.</div>
          </div>

          <label>
            <span>Proveedor para asociar los libros importados (opcional)</span>
            <select value={proveedorImportId} onChange={(e) => setProveedorImportId(e.target.value)}>
              <option value="">Sin asociar</option>
              {(proveedores ?? []).map((prov) => (
                <option key={prov.id} value={prov.id}>
                  {prov.razonSocial}
                </option>
              ))}
            </select>
          </label>

          <label className="stockCard__file stockCard__file--full">
            <span>Archivo de importacion</span>
            <input type="file" accept=".xlsx,.csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,text/csv" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
          </label>

          <div className="stockCard__actions">
            <button className="btn btn--ghost" type="button" onClick={importar}>Importar archivo</button>
          </div>
        </article>
      </section>

      {msg ? (
        <section className="ccard ccard__pad">
          <p className="csuccess">{msg}</p>
        </section>
      ) : null}
      {loading ? <section className="ccard ccard__pad">Cargando...</section> : null}
      {error ? <section className="ccard ccard__pad cerror">{error}</section> : null}

      {!loading && !error ? (
        <section className="ccard ccard__pad stockTableSection">
          <div className="stockTableHead">
            <h3 className="stockTableHead__title">Listado de stock</h3>
            <p className="stockTableHead__sub">
              Estado visual por nivel de reposicion para facilitar lectura operativa.
            </p>
          </div>
          <div className="tableWrap">
            <table className="ctable stockTable">
              <thead>
                <tr>
                  <th>Imagen</th>
                  <th>Producto</th>
                  <th>Proveedor</th>
                  <th className="right">Precio</th>
                  <th className="right">Stock</th>
                  <th className="right">Disponible</th>
                  <th>Detalles</th>
                  <th className="right">Estado</th>
                  <th className="right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {(filtrados ?? []).map((p) => {
                  const enEdicion = Number(editandoId) === Number(p.id);
                  return (
                    <tr key={p.id}>
                      <td>
                        {p.imagenUrl ? (
                          <img
                            className="stockTable__img"
                            src={resolverImagenUrl(p.imagenUrl)}
                            alt={p.nombre ?? `Libro #${p.id}`}
                            loading="lazy"
                          />
                        ) : (
                          <div className="stockTable__imgFallback">Sin imagen</div>
                        )}
                      </td>
                      <td>
                        {enEdicion ? (
                          <div className="stockEditGrid">
                            <input value={edicion.nombre} onChange={(e) => setEdicion((prev) => ({ ...prev, nombre: e.target.value }))} placeholder="Nombre" />
                            <input value={edicion.tipo} onChange={(e) => setEdicion((prev) => ({ ...prev, tipo: e.target.value }))} placeholder="Genero" />
                            <input value={edicion.editorial} onChange={(e) => setEdicion((prev) => ({ ...prev, editorial: e.target.value }))} placeholder="Editorial" />
                            <input value={edicion.descripcion} onChange={(e) => setEdicion((prev) => ({ ...prev, descripcion: e.target.value }))} placeholder="Sinopsis" />
                            <input type="file" accept="image/*" onChange={(e) => setEdicion((prev) => ({ ...prev, imagenFile: e.target.files?.[0] ?? null }))} />
                          </div>
                        ) : (
                          <>
                            <div className="strong">{p.nombre ?? "(sin nombre)"}</div>
                            {p.descripcion ? <div className="stockTable__desc">{p.descripcion}</div> : <div className="stockTable__desc">Sin descripcion.</div>}
                          </>
                        )}
                      </td>
                      <td>
                        {enEdicion ? (
                          <select value={edicion.proveedorId} onChange={(e) => setEdicion((prev) => ({ ...prev, proveedorId: e.target.value }))}>
                            <option value="">Sin asociar</option>
                            {(proveedores ?? []).map((prov) => (
                              <option key={prov.id} value={prov.id}>
                                {prov.razonSocial}
                              </option>
                            ))}
                          </select>
                        ) : (p.proveedorNombre || "-")}
                      </td>
                      <td className="right strong">
                        {enEdicion ? (
                          <input type="number" value={edicion.precio} onChange={(e) => setEdicion((prev) => ({ ...prev, precio: e.target.value }))} />
                        ) : money.format(Number(p.precio ?? 0))}
                      </td>
                      <td className="right strong">
                        {enEdicion ? (
                          <input type="number" value={edicion.stock} onChange={(e) => setEdicion((prev) => ({ ...prev, stock: e.target.value }))} />
                        ) : (p.stock ?? 0)}
                      </td>
                      <td className="right strong">{p.stockDisponible ?? p.stock ?? 0}</td>
                      <td>
                        <div className="stockTable__detailItem">
                          <b>Promocion:</b> {p.tienePromocionActiva ? (p.promocionNombre ?? "Activa") : "No"}
                        </div>
                        <div className="stockTable__detailItem">
                          <b>Precio final:</b> {money.format(Number(p.precioFinal ?? p.precio ?? 0))}
                        </div>
                      </td>
                      <td className="right">
                        <StockBadge stock={p.stockDisponible ?? p.stock} min={minStock} />
                      </td>
                      <td className="right">
                        <div className="stockActionsStack">
                          {enEdicion ? (
                            <>
                              <button
                                className="btn btn--sm"
                                type="button"
                                disabled={guardandoId === p.id}
                                onClick={() => guardarEdicion(p.id)}
                              >
                                {guardandoId === p.id ? "Guardando..." : "Guardar"}
                              </button>
                              <button className="btn btn--sm btn--ghost" type="button" onClick={cancelarEdicion}>
                                Cancelar
                              </button>
                            </>
                          ) : (
                            <button className="btn btn--sm btn--ghost" type="button" onClick={() => iniciarEdicion(p)}>
                              Editar
                            </button>
                          )}
                          <button
                            className="btn btn--sm stockTable__deleteBtn"
                            type="button"
                            disabled={eliminandoId === p.id || enEdicion}
                            onClick={() => eliminarLibro(p)}
                          >
                            {eliminandoId === p.id ? "Eliminando..." : "Eliminar"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
                {(filtrados ?? []).length === 0 ? (
                  <tr>
                    <td colSpan="9" className="emptyRow">No hay productos para mostrar.</td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
    </main>
  );
}

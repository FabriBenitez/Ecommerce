import { useEffect, useMemo, useState } from "react";
import { crearProducto, importarProductosCsv, listarProductos, obtenerStockMinimo } from "../api/adminCompras.api";
import StockBadge from "../components/StockBadge";
import "../styles/ComprasCommon.css";
import "./StockInventario.css";

export default function StockInventario() {
  const [minStock, setMinStock] = useState(10);
  const [productos, setProductos] = useState([]);
  const [q, setQ] = useState("");
  const [soloBajo, setSoloBajo] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");
  const [nuevo, setNuevo] = useState({ nombre: "", descripcion: "", precio: "", stock: "", imagenFile: null });
  const [file, setFile] = useState(null);

  const cargar = async () => {
    const [data, min] = await Promise.all([listarProductos(), obtenerStockMinimo()]);
    setProductos(Array.isArray(data) ? data : []);
    setMinStock(Number(min || 10));
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
        const desc = (p.descripcion ?? "").toLowerCase();
        const okText = !qq || name.includes(qq) || desc.includes(qq) || String(p.id).includes(qq);
        if (!okText) return false;
        if (!soloBajo) return true;
        return Number(p.stock ?? 0) <= minStock;
      })
      .sort((a, b) => Number(a.stock ?? 0) - Number(b.stock ?? 0));
  }, [productos, q, soloBajo, minStock]);

  const countBajo = useMemo(
    () => (productos ?? []).filter((p) => Number(p.stock ?? 0) <= minStock).length,
    [productos, minStock],
  );

  const crearLibro = async () => {
    try {
      setError("");
      setMsg("");
      if (!nuevo.imagenFile) {
        setError("La imagen del libro es obligatoria.");
        return;
      }

      await crearProducto({
        nombre: nuevo.nombre,
        descripcion: nuevo.descripcion || null,
        precio: Number(nuevo.precio || 0),
        stock: Number(nuevo.stock || 0),
        imagenFile: nuevo.imagenFile,
      });

      setMsg("Libro creado correctamente.");
      setNuevo({ nombre: "", descripcion: "", precio: "", stock: "", imagenFile: null });
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? "No se pudo crear el libro.");
    }
  };

  const importar = async () => {
    if (!file) return;
    try {
      setError("");
      setMsg("");
      const resp = await importarProductosCsv(file);
      setMsg(`Importacion completa. Creados: ${resp?.creados ?? 0}.`);
      setFile(null);
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? "No se pudo importar el archivo.");
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
              placeholder="Por nombre, descripcion o #id..."
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
          <p className="stockCard__desc">Alta individual con imagen desde tu PC.</p>

          <div className="stockCard__grid">
            <label>
              <span>Nombre</span>
              <input value={nuevo.nombre} onChange={(e) => setNuevo((p) => ({ ...p, nombre: e.target.value }))} />
            </label>

            <label>
              <span>Descripcion</span>
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
            Carga un solo archivo <b>.xlsx</b> o <b>.csv</b>. La imagen debe venir embebida en la columna <code>imagenBase64</code>.
          </p>

          <div className="stockHelp">
            <div><b>Columnas obligatorias:</b> nombre, descripcion, precio, stock, imagenBase64</div>
            <div><b>Tip:</b> tambien sirve formato data URI: <code>data:image/png;base64,...</code></div>
          </div>

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
        <section className="ccard">
          <div className="tableWrap">
            <table className="ctable" style={{ minWidth: 820 }}>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Producto</th>
                  <th className="right">Stock</th>
                  <th className="right">Estado</th>
                </tr>
              </thead>
              <tbody>
                {(filtrados ?? []).map((p) => (
                  <tr key={p.id}>
                    <td className="mono">#{p.id}</td>
                    <td>
                      <div className="strong">{p.nombre ?? "(sin nombre)"}</div>
                      {p.descripcion ? <div className="cmuted" style={{ marginTop: 4 }}>{p.descripcion}</div> : null}
                    </td>
                    <td className="right strong">{p.stock ?? 0}</td>
                    <td className="right">
                      <StockBadge stock={p.stock} min={minStock} />
                    </td>
                  </tr>
                ))}
                {(filtrados ?? []).length === 0 ? (
                  <tr>
                    <td colSpan="4" className="emptyRow">No hay productos para mostrar.</td>
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

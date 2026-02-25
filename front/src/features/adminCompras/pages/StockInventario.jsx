import { useEffect, useMemo, useState } from "react";
import { listarProductos } from "../api/adminCompras.api";
import StockBadge from "../components/StockBadge";
import "../styles/ComprasCommon.css";

export default function StockInventario() {
  const MIN_STOCK = 10;

  const [productos, setProductos] = useState([]);
  const [q, setQ] = useState("");
  const [soloBajo, setSoloBajo] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        const data = await listarProductos();
        if (!alive) return;
        setProductos(Array.isArray(data) ? data : []);
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el inventario.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
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
        return Number(p.stock ?? 0) <= MIN_STOCK;
      })
      .sort((a, b) => Number(a.stock ?? 0) - Number(b.stock ?? 0));
  }, [productos, q, soloBajo]);

  const countBajo = useMemo(() => (productos ?? []).filter((p) => Number(p.stock ?? 0) <= MIN_STOCK).length, [productos]);

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Stock e inventario</h1>
          <p className="cmuted">
            Stock mínimo: <b>{MIN_STOCK}</b> · Libros con stock bajo: <b>{countBajo}</b>
          </p>
        </div>
      </header>

      <section className="ccard ccard__pad">
        <div className="toolbar">
          <label className="field">
            <span>Buscar</span>
            <input
              className="cinput"
              placeholder="Por nombre, descripción o #id…"
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

      {loading ? <section className="ccard ccard__pad">Cargando…</section> : null}
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
                      <StockBadge stock={p.stock} min={MIN_STOCK} />
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
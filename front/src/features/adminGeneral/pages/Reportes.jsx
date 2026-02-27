import { useEffect, useState } from "react";
import { reporteCaja, reporteStock, reporteVentas } from "../api/adminGeneral.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

export default function Reportes() {
  const [ventas, setVentas] = useState(null);
  const [stock, setStock] = useState([]);
  const [caja, setCaja] = useState(null);
  const [tab, setTab] = useState("ventas");

  useEffect(() => {
    const hasta = new Date();
    const desde = new Date();
    desde.setDate(desde.getDate() - 7);

    reporteVentas({
      desde: desde.toISOString(),
      hasta: hasta.toISOString(),
    })
      .then(setVentas)
      .catch(() => setVentas(null));

    reporteStock().then(setStock).catch(() => setStock([]));
    reporteCaja().then(setCaja).catch(() => setCaja(null));
  }, []);

  return (
    <>
      <section className="agPageHead">
        <h1>Reportes de Gestion</h1>
        <p>Analisis de ventas, stock critico y flujo de caja.</p>
      </section>

      <section className="agSection">
        <div className="agSection__head">
          <div className="agQuickList" style={{ gridAutoFlow: "column", gap: 8 }}>
            <button className={`agBtn ${tab === "ventas" ? "" : "agBtn--ghost"}`} onClick={() => setTab("ventas")}>Ventas</button>
            <button className={`agBtn ${tab === "stock" ? "" : "agBtn--ghost"}`} onClick={() => setTab("stock")}>Stock Critico</button>
            <button className={`agBtn ${tab === "caja" ? "" : "agBtn--ghost"}`} onClick={() => setTab("caja")}>Caja Diaria</button>
          </div>
        </div>

        {tab === "ventas" ? (
          <div className="agGrid">
            <div className="agKpis">
              <article className="agKpi"><label>Total vendido</label><strong>{money.format(ventas?.totalVendido ?? 0)}</strong></article>
              <article className="agKpi"><label>Ordenes</label><strong>{ventas?.cantidadVentas ?? 0}</strong></article>
              <article className="agKpi"><label>Desde</label><strong>{ventas?.desde ? new Date(ventas.desde).toLocaleDateString("es-AR") : "-"}</strong></article>
              <article className="agKpi"><label>Hasta</label><strong>{ventas?.hasta ? new Date(ventas.hasta).toLocaleDateString("es-AR") : "-"}</strong></article>
            </div>
            <article className="agCard agCard--soft">
              <h3>Tendencia de Ventas</h3>
              <p>Reporte resumido para el periodo seleccionado.</p>
              <div style={{ height: 180, borderRadius: 10, background: "linear-gradient(180deg,#ebf9f0,#f9fffb)", border: "1px solid #d4eedf" }} />
            </article>
          </div>
        ) : null}

        {tab === "stock" ? (
          <article className="agCard">
            <h3>Items con stock menor o igual a 10 unidades</h3>
            <div className="agTableWrap">
              <table className="agTable">
                <thead><tr><th>Producto</th><th>Stock</th><th>Estado</th></tr></thead>
                <tbody>
                  {stock.length === 0 ? (
                    <tr><td colSpan={3}>No hay items criticos.</td></tr>
                  ) : stock.map((s) => (
                    <tr key={s.productoId}>
                      <td>{s.producto}</td>
                      <td>{s.stockActual}</td>
                      <td><span className={`agBadge ${s.stockActual <= 3 ? "agBadge--danger" : "agBadge--warn"}`}>{s.stockActual <= 3 ? "Agotado pronto" : "Bajo stock"}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </article>
        ) : null}

        {tab === "caja" ? (
          <article className="agCard">
            <h3>Caja del dia</h3>
            <div className="agKpis" style={{ gridTemplateColumns: "repeat(3,minmax(140px,1fr))" }}>
              <article className="agKpi"><label>Ingresos</label><strong>{money.format(caja?.ingresos ?? 0)}</strong></article>
              <article className="agKpi"><label>Egresos</label><strong>{money.format(caja?.egresos ?? 0)}</strong></article>
              <article className="agKpi agKpi--ok"><label>Saldo neto</label><strong>{money.format(caja?.saldoNeto ?? 0)}</strong></article>
            </div>
          </article>
        ) : null}
      </section>
    </>
  );
}

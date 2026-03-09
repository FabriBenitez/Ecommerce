import { useEffect, useState } from "react";
import { adminComprasDashboard } from "../api/adminCompras.api";
import "./ComprasDashboard.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function proveedorText(value) {
  if (!value) return "-";
  if (typeof value === "string") return value;
  if (typeof value === "object") {
    const razon = value.razonSocial ?? value.nombre ?? "";
    const cuit = value.cuit ? ` (${value.cuit})` : "";
    return razon ? `${razon}${cuit}` : `Proveedor #${value.proveedorId ?? "-"}`;
  }
  return String(value);
}

function estadoCompraLabel(e) {
  const map = { 1: "Pendiente", 2: "Confirmada", 3: "Cancelada" };
  return map[e] ?? String(e ?? "");
}

function estadoCompraClass(e) {
  const map = {
    1: "cDashLast__status cDashLast__status--pending",
    2: "cDashLast__status cDashLast__status--ok",
    3: "cDashLast__status cDashLast__status--cancel",
  };
  return map[e] ?? "cDashLast__status";
}

export default function ComprasDashboard() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        const resp = await adminComprasDashboard();
        if (!alive) return;
        setData(resp);
      } catch (e) {
        if (!alive) return;
        const status = e?.response?.status;
        if (status === 401) setError("Sesion expirada o no autenticada. Volve a iniciar sesion.");
        else if (status === 403) setError("No tenes permisos para ver este dashboard.");
        else if (status === 404) setError("Endpoint de dashboard no encontrado en backend.");
        else setError(e?.response?.data?.error ?? e?.message ?? "No se pudo cargar el dashboard.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, []);

  return (
    <div className="cDash">
      <header className="ccard ccard__pad cDashHero">
        <h1 className="ctitle">Gestion de stock y abastecimiento.</h1>
      </header>

      {loading ? <div className="ccard ccard__pad">Cargando...</div> : null}
      {error ? <div className="ccard ccard__pad" style={{ color: "#b00020" }}>{error}</div> : null}

      {!loading && !error && data ? (
        <>
          <section className="cDashTiles">
            <article className="ccard ccard__pad cTile cTile--accent">
              <div className="cTile__label">Productos con stock bajo</div>
              <div className="cTile__value">{data.stockBajoCount}</div>
            </article>

            <article className="ccard ccard__pad cTile">
              <div className="cTile__label">Compras pendientes</div>
              <div className="cTile__value">{data.comprasPendientesCount}</div>
            </article>

            <article className="ccard ccard__pad cTile">
              <div className="cTile__label">Stock minimo</div>
              <div className="cTile__value">{data.stockMinimo}</div>
            </article>
          </section>

          <section className="ccard ccard__pad">
            <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "center" }}>
              <h2 className="sectionTitle" style={{ margin: 0 }}>Ultimas 5 Compras Confirmadas</h2>
              <a className="cbtn cbtn--ghost" href="/compras/historial" style={{ textDecoration: "none" }}>
                Ver todas
              </a>
            </div>

            <div className="cDashLast">
              <div className="cDashLast__head">
                <span>Proveedor</span>
                <span>Fecha</span>
                <span className="right">Total</span>
                <span>Estado</span>
              </div>

              {(data.ultimasConfirmadas ?? []).map((c) => (
                <article key={c.id} className="cDashLast__row">
                  <div className="cDashLast__cell cDashLast__provider">
                    <span className="cDashLast__label">Proveedor</span>
                    <b>{proveedorText(c.proveedor)}</b>
                  </div>

                  <div className="cDashLast__cell cDashLast__date">
                    <span className="cDashLast__label">Fecha</span>
                    <span>{new Date(c.fecha).toLocaleString("es-AR")}</span>
                  </div>

                  <div className="cDashLast__cell cDashLast__amount right">
                    <span className="cDashLast__label">Total</span>
                    <b>{money.format(c.total)}</b>
                  </div>

                  <div className="cDashLast__cell cDashLast__state">
                    <span className="cDashLast__label">Estado</span>
                    <span className={estadoCompraClass(c.estadoCompra)}>
                      {estadoCompraLabel(c.estadoCompra)}
                    </span>
                  </div>
                </article>
              ))}

              {(data.ultimasConfirmadas ?? []).length === 0 ? (
                <div className="cDashLast__empty">No hay compras confirmadas recientes.</div>
              ) : null}
            </div>
          </section>
        </>
      ) : null}
    </div>
  );
}

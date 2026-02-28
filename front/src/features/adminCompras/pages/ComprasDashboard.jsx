import { useEffect, useState } from "react";
import { adminComprasDashboard } from "../api/adminCompras.api";

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
        <h1 className="ctitle">Dashboard Operativo</h1>
        <p className="cmuted" style={{ marginTop: 6 }}>
          Gestion centralizada de stock y abastecimiento.
        </p>
      </header>

      {loading ? <div className="ccard ccard__pad">Cargando...</div> : null}
      {error ? <div className="ccard ccard__pad" style={{ color: "#b00020" }}>{error}</div> : null}

      {!loading && !error && data ? (
        <>
          <section className="cDashTiles">
            <article className="ccard ccard__pad cTile cTile--accent">
              <div className="cTile__label">Productos con stock bajo</div>
              <div className="cTile__value">{data.stockBajoCount}</div>
              <div className="cTile__meta">Stock &lt; {data.stockMinimo}</div>
            </article>

            <article className="ccard ccard__pad cTile">
              <div className="cTile__label">Compras pendientes</div>
              <div className="cTile__value">{data.comprasPendientesCount}</div>
              <div className="cTile__meta">En espera de confirmacion</div>
            </article>

            <article className="ccard ccard__pad cTile">
              <div className="cTile__label">Stock minimo</div>
              <div className="cTile__value">{data.stockMinimo}</div>
              <div className="cTile__meta">Parametro operativo vigente</div>
            </article>
          </section>

          <section className="ccard ccard__pad">
            <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "center" }}>
              <h2 className="sectionTitle" style={{ margin: 0 }}>Ultimas 5 Compras Confirmadas</h2>
              <a className="cbtn cbtn--ghost" href="/compras/historial" style={{ textDecoration: "none" }}>
                Ver todas
              </a>
            </div>

            <div className="tableWrap" style={{ marginTop: 12 }}>
              <table className="ctable">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Proveedor</th>
                    <th>Fecha</th>
                    <th className="right">Total</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {(data.ultimasConfirmadas ?? []).map((c) => (
                    <tr key={c.id}>
                      <td>#{c.id}</td>
                      <td>{proveedorText(c.proveedor)}</td>
                      <td>{new Date(c.fecha).toLocaleString("es-AR")}</td>
                      <td className="right strong">{money.format(c.total)}</td>
                      <td>{String(c.estadoCompra)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      ) : null}
    </div>
  );
}

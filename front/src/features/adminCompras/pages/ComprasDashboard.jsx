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
        if (status === 401) setError("Sesión expirada o no autenticada. Volvé a iniciar sesión.");
        else if (status === 403) setError("No tenés permisos para ver este dashboard.");
        else if (status === 404) setError("Endpoint de dashboard no encontrado en backend.");
        else setError(e?.response?.data?.error ?? e?.message ?? "No se pudo cargar el dashboard.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
  }, []);

  return (
    <div className="cgrid" style={{ gap: 14 }}>
      <header className="ccard ccard__pad">
        <h1 className="ctitle">Dashboard Operativo</h1>
        <p className="cmuted" style={{ marginTop: 6 }}>
          Gestión centralizada de stock y abastecimiento.
        </p>
      </header>

      {loading ? <div className="ccard ccard__pad">Cargando…</div> : null}
      {error ? <div className="ccard ccard__pad" style={{ color: "#b00020" }}>{error}</div> : null}

      {!loading && !error && data ? (
        <>
          <section className="cgrid cgrid--3">
            <div className="ccard ccard__pad">
              <div className="cmuted">Productos con stock bajo</div>
              <div style={{ fontSize: 28, fontWeight: 900, marginTop: 6 }}>
                {data.stockBajoCount}
              </div>
              <div className="cmuted" style={{ marginTop: 6 }}>
                Stock &lt; {data.stockMinimo}
              </div>
            </div>

            <div className="ccard ccard__pad">
              <div className="cmuted">Compras pendientes</div>
              <div style={{ fontSize: 28, fontWeight: 900, marginTop: 6 }}>
                {data.comprasPendientesCount}
              </div>
              <div className="cmuted" style={{ marginTop: 6 }}>
                En espera de confirmación
              </div>
            </div>

            <div className="ccard ccard__pad">
              <div className="cmuted">Atajos</div>
              <div style={{ display: "flex", gap: 10, marginTop: 12, flexWrap: "wrap" }}>
                <a className="cbtn cbtn--ghost" href="/compras/inventario" style={{ textDecoration: "none" }}>
                  Inventario
                </a>
                <a className="cbtn cbtn--ghost" href="/compras/proveedores" style={{ textDecoration: "none" }}>
                  Proveedores
                </a>
                <a className="cbtn cbtn--primary" href="/compras/seguimiento" style={{ textDecoration: "none" }}>
                  Compras
                </a>
              </div>
            </div>
          </section>

          <section className="ccard ccard__pad">
            <div style={{ display: "flex", justifyContent: "space-between", gap: 12, alignItems: "center" }}>
              <h2 className="sectionTitle" style={{ margin: 0 }}>Últimas 5 Compras Confirmadas</h2>
              <a className="cbtn cbtn--ghost" href="/compras/historial" style={{ textDecoration: "none" }}>
                Ver todas
              </a>
            </div>

            <div className="tableWrap" style={{ marginTop: 12 }}>
              <table className="itemsTable">
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


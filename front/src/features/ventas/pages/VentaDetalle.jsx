import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ventaPorId } from "../api/ventas.api";
import "./VentaDetalle.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function estadoLabel(e) {
  return typeof e === "string" ? e : `Estado ${e}`;
}
function canalLabel(c) {
  return typeof c === "string" ? c : `Canal ${c}`;
}

export default function VentaDetalle() {
  const { id } = useParams();
  const [venta, setVenta] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        const data = await ventaPorId(id);
        if (!alive) return;
        setVenta(data);
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el detalle de la venta.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
  }, [id]);

  return (
    <main className="ventaDetallePage">
      <header className="ventaDetalleTop">
        <Link className="backLink" to="/mis-ventas">← Volver</Link>
        <div className="ventaDetalleTitle">
          <h1>Venta #{id}</h1>
          <p>Detalle de compra</p>
        </div>
      </header>

      {loading ? <p className="state">Cargando…</p> : null}
      {error ? <p className="state state--error">{error}</p> : null}

      {!loading && !error && venta ? (
        <>
          <section className="summaryCard">
            <div className="summaryGrid">
              <div>
                <span className="k">Fecha</span>
                <span className="v">{new Date(venta.fecha).toLocaleString("es-AR")}</span>
              </div>
              <div>
                <span className="k">Estado</span>
                <span className="v">{estadoLabel(venta.estadoVenta)}</span>
              </div>
              <div>
                <span className="k">Canal</span>
                <span className="v">{canalLabel(venta.canal)}</span>
              </div>
              <div>
                <span className="k">Total</span>
                <span className="v v--money">{money.format(venta.total)}</span>
              </div>
            </div>
          </section>

          <section className="itemsCard">
            <h2 className="sectionTitle">Items</h2>

            <div className="tableWrap">
              <table className="itemsTable">
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th className="right">Cant.</th>
                    <th className="right">Precio</th>
                    <th className="right">Subtotal</th>
                  </tr>
                </thead>
                <tbody>
                  {(venta.detalles ?? []).map((d) => (
                    <tr key={`${d.productoId}-${d.nombreProducto}`}>
                      <td>
                        <div className="prodName">{d.nombreProducto}</div>
                        <div className="prodMeta mono">ID: {d.productoId}</div>
                      </td>
                      <td className="right">{d.cantidad}</td>
                      <td className="right">{money.format(d.precioUnitario)}</td>
                      <td className="right strong">{money.format(d.subtotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="totalRow">
              <span>Total</span>
              <span className="strong">{money.format(venta.total)}</span>
            </div>
          </section>

          <section className="entregaCard">
            <h2 className="sectionTitle">Entrega</h2>

            <div className="entregaGrid">
              <div>
                <span className="k">Nombre</span>
                <span className="v">{venta.nombreEntrega ?? "-"}</span>
              </div>
              <div>
                <span className="k">Teléfono</span>
                <span className="v">{venta.telefonoEntrega ?? "-"}</span>
              </div>
              <div className="col2">
                <span className="k">Dirección</span>
                <span className="v">{venta.direccionEntrega ?? "-"}</span>
              </div>
              <div>
                <span className="k">Ciudad</span>
                <span className="v">{venta.ciudad ?? "-"}</span>
              </div>
              <div>
                <span className="k">Provincia</span>
                <span className="v">{venta.provincia ?? "-"}</span>
              </div>
              <div>
                <span className="k">Código postal</span>
                <span className="v">{venta.codigoPostal ?? "-"}</span>
              </div>
              <div className="col2">
                <span className="k">Observaciones</span>
                <span className="v">{venta.observaciones ?? "-"}</span>
              </div>
            </div>
          </section>
        </>
      ) : null}
    </main>
  );
}

import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ventasApi } from "@/features/ventas/api/ventas.api.js";
import "./Ventas.css";

export default function VentaDetalle() {
  const { id } = useParams();

  const [venta, setVenta] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const run = async () => {
      try {
        setError("");
        setLoading(true);
        const { data } = await ventasApi.obtenerVenta(id);
        setVenta(data);
      } catch (e) {
        setError(e?.response?.data ?? "No se pudo cargar la venta.");
      } finally {
        setLoading(false);
      }
    };
    run();
  }, [id]);

  return (
    <main className="ventasPage">
      <header className="ventasHeader">
        <h1 className="ventasHeader__title">Detalle de venta</h1>
        <p className="ventasHeader__subtitle">Información de la compra y sus items.</p>
      </header>

      {loading ? <p className="ventasInfo">Cargando...</p> : null}
      {error ? <p className="ventasError">{error}</p> : null}

      {venta ? (
        <section className="ventaDetalle">
          <div className="ventaDetalle__grid">
            <div className="ventaBox">
              <p className="ventaBox__title">Resumen</p>
              <div className="ventaBox__row"><span>Venta</span><b>#{venta.id}</b></div>
              <div className="ventaBox__row"><span>Fecha</span><b>{new Date(venta.fecha).toLocaleString()}</b></div>
              <div className="ventaBox__row"><span>Estado</span><b>{venta.estadoVenta}</b></div>
              <div className="ventaBox__row"><span>Canal</span><b>{venta.canal}</b></div>
              <div className="ventaBox__row"><span>Total</span><b>${venta.total}</b></div>
            </div>

            <div className="ventaBox">
              <p className="ventaBox__title">Entrega</p>
              <div className="ventaBox__row"><span>Nombre</span><b>{venta.nombreEntrega ?? "-"}</b></div>
              <div className="ventaBox__row"><span>Teléfono</span><b>{venta.telefonoEntrega ?? "-"}</b></div>
              <div className="ventaBox__row"><span>Dirección</span><b>{venta.direccionEntrega ?? "-"}</b></div>
              <div className="ventaBox__row"><span>Ciudad</span><b>{venta.ciudad ?? "-"}</b></div>
              <div className="ventaBox__row"><span>Provincia</span><b>{venta.provincia ?? "-"}</b></div>
              <div className="ventaBox__row"><span>CP</span><b>{venta.codigoPostal ?? "-"}</b></div>
            </div>
          </div>

          {venta.observaciones ? (
            <div className="ventaObs">
              <b>Observaciones:</b> {venta.observaciones}
            </div>
          ) : null}

          <div className="ventaItems">
            <h2 className="ventaItems__title">Items</h2>

            {venta.detalles?.length ? (
              <div className="ventaTableWrap">
                <table className="ventaTable">
                  <thead>
                    <tr>
                      <th>Producto</th>
                      <th>Cant.</th>
                      <th>Precio</th>
                      <th>Subtotal</th>
                    </tr>
                  </thead>
                  <tbody>
                    {venta.detalles.map((d, idx) => (
                      <tr key={idx}>
                        <td>{d.nombreProducto}</td>
                        <td>{d.cantidad}</td>
                        <td>${d.precioUnitario}</td>
                        <td>${d.subtotal}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p className="ventasInfo">No hay detalles para esta venta.</p>
            )}
          </div>

          <div className="ventaActions">
            <Link className="ventasBtn ventasBtn--ghost" to="/ventas/mis-ventas">
              ← Volver
            </Link>
            <Link className="ventasBtn" to="/catalogo">
              Ir al catálogo
            </Link>
          </div>
        </section>
      ) : null}
    </main>
  );
}

import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ventaPorId } from "../api/ventas.api";
import "./Comprobante.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function estadoLabel(e) {
  return ({ 1: "Pendiente", 2: "Pagada", 3: "Cancelada" }[e] ?? String(e ?? ""));
}
function canalLabel(c) {
  return ({ 1: "Web", 2: "Presencial" }[c] ?? String(c ?? ""));
}

function formatFecha(iso) {
  try { return new Date(iso).toLocaleString("es-AR"); }
  catch { return iso ?? "-"; }
}

export default function Comprobante() {
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
        setError(e?.message ?? "No se pudo cargar el comprobante.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
  }, [id]);

  const totalItems = useMemo(() => {
    const arr = venta?.detalles ?? [];
    return arr.reduce((acc, d) => acc + (Number(d?.cantidad) || 0), 0);
  }, [venta]);

  return (
    <main className="ticketPage">
      <div className="ticketNoPrint ticketTopbar">
        <Link className="ticketBack" to={`/ventas/${id}`}>← Volver al detalle</Link>

        <div className="ticketActions">
          <button className="ticketBtn" onClick={() => window.print()}>
            Imprimir / Guardar PDF
          </button>
        </div>
      </div>

      {loading ? <p className="ticketState">Cargando…</p> : null}
      {error ? <p className="ticketState ticketState--error">{error}</p> : null}

      {!loading && !error && venta ? (
        <section className="ticket">
          <header className="ticketHeader">
            <div className="ticketBrand">
              <div className="ticketLogo" aria-hidden="true" />
              <div>
                <h1 className="ticketTitle">Comprobante de compra</h1>
                <p className="ticketSub">Librería (demo)</p>
              </div>
            </div>

            <div className="ticketMeta">
              <div><span>N°</span><strong>#{venta.id}</strong></div>
              <div><span>Fecha</span><strong>{formatFecha(venta.fecha)}</strong></div>
              <div><span>Canal</span><strong>{canalLabel(venta.canal)}</strong></div>
              <div><span>Estado</span><strong>{estadoLabel(venta.estadoVenta)}</strong></div>
            </div>
          </header>

          <div className="ticketDivider" />

          <div className="ticketGrid">
            <div className="ticketBlock">
              <h2>Entrega</h2>
              <p><strong>Nombre:</strong> {venta.nombreEntrega ?? "-"}</p>
              <p><strong>Tel:</strong> {venta.telefonoEntrega ?? "-"}</p>
              <p><strong>Dirección:</strong> {venta.direccionEntrega ?? "-"}</p>
              <p><strong>Ciudad:</strong> {venta.ciudad ?? "-"}</p>
              <p><strong>Provincia:</strong> {venta.provincia ?? "-"}</p>
              <p><strong>CP:</strong> {venta.codigoPostal ?? "-"}</p>
              <p><strong>Obs:</strong> {venta.observaciones ?? "-"}</p>
            </div>

            <div className="ticketBlock">
              <h2>Resumen</h2>
              <p><strong>Items:</strong> {totalItems}</p>
              <p><strong>Total:</strong> <span className="ticketMoney">{money.format(venta.total)}</span></p>

              {/* Si querés mostrar refs de MP cuando existan */}
              {venta.mercadoPagoPaymentId ? (
                <p><strong>MP PaymentId:</strong> {venta.mercadoPagoPaymentId}</p>
              ) : null}
              {venta.mercadoPagoEstado ? (
                <p><strong>MP Estado:</strong> {venta.mercadoPagoEstado}</p>
              ) : null}
            </div>
          </div>

          <div className="ticketDivider" />

          <div className="ticketTableWrap">
            <table className="ticketTable">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th className="r">Cant.</th>
                  <th className="r">Precio</th>
                  <th className="r">Subt.</th>
                </tr>
              </thead>
              <tbody>
                {(venta.detalles ?? []).map((d) => (
                  <tr key={`${d.productoId}-${d.nombreProducto}`}>
                    <td>{d.nombreProducto}</td>
                    <td className="r">{d.cantidad}</td>
                    <td className="r">{money.format(d.precioUnitario)}</td>
                    <td className="r"><strong>{money.format(d.subtotal)}</strong></td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <td colSpan={3} className="r">TOTAL</td>
                  <td className="r"><strong>{money.format(venta.total)}</strong></td>
                </tr>
              </tfoot>
            </table>
          </div>

          <footer className="ticketFooter">
            <p>Gracias por tu compra 🤝</p>
            <p className="ticketTiny">Este comprobante es válido como constancia (proyecto demo).</p>
          </footer>
        </section>
      ) : null}
    </main>
  );
}

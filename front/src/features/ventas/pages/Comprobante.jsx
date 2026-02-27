import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ventaPorId } from "../api/ventas.api";
import { obtenerDatosFactura } from "@/features/adminGeneral/api/adminGeneral.api";
import "@/features/adminVentas/pages/Comprobante.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });
const medioMap = { 1: "Efectivo", 2: "Debito", 3: "Credito", 4: "Transferencia", 5: "Nota credito" };
const defaultDatosFactura = {
  nombreComercial: "LIBRERIA X",
  tituloComprobante: "Comprobante de venta",
  direccion: "Calle Falsa 123 - CABA",
  telefono: "+54 11 4567-8901",
  email: "contacto@libreriax.com",
  mensajeAgradecimiento: "Gracias por su compra en Libreria X",
};

export default function Comprobante() {
  const { id } = useParams();
  const [venta, setVenta] = useState(null);
  const [datosFactura, setDatosFactura] = useState(defaultDatosFactura);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setErr("");
        setLoading(true);
        const data = await ventaPorId(id);
        if (!alive) return;
        setVenta(data);

        try {
          const datos = await obtenerDatosFactura();
          if (!alive) return;
          setDatosFactura(datos ?? defaultDatosFactura);
        } catch {
          if (!alive) return;
          setDatosFactura(defaultDatosFactura);
        }
      } catch (e) {
        if (!alive) return;
        setErr(e?.message ?? "No se pudo cargar comprobante.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, [id]);

  const tieneNotaCredito = (venta?.pagos ?? []).some((p) => p.medioPago === 5);

  return (
    <div className="compPage">
      <div className="compActions no-print">
        <Link className="compBtn compBtnGhost" to={`/ventas/${id}`}>Volver</Link>
        <button className="compBtn" onClick={() => window.print()}>Imprimir</button>
      </div>

      {loading ? <p className="compState">Cargando...</p> : null}
      {err ? <p className="compState compStateError">{err}</p> : null}

      {!loading && !err && venta ? (
        <section className="compCard">
          <header className="compHeader">
            <div className="compBrand">
              <div className="compLogo">{datosFactura.nombreComercial}</div>
              <p>{datosFactura.tituloComprobante}</p>
              <p>{datosFactura.direccion}</p>
              <p>Tel: {datosFactura.telefono}</p>
              <p>Email: {datosFactura.email}</p>
            </div>

            <div className="compMeta">
              <span className="compBadge">COMPROBANTE ORIGINAL</span>
              <h2>Venta #{venta.id}</h2>
              <p>Fecha: {new Date(venta.fecha).toLocaleString("es-AR")}</p>
              <p>DNI Cliente: {venta.clienteDni ?? "-"}</p>
              <p>{venta.clienteNombre ?? "-"}</p>
            </div>
          </header>

          <div className="compTableWrap">
            <table className="compTable">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th className="right">Cant.</th>
                  <th className="right">Precio unit.</th>
                  <th className="right">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                {(venta.detalles ?? []).map((d) => (
                  <tr key={`${d.productoId}-${d.nombreProducto}`}>
                    <td className="prodName">{d.nombreProducto}</td>
                    <td className="right">{d.cantidad}</td>
                    <td className="right">{money.format(d.precioUnitario)}</td>
                    <td className="right strong">{money.format(d.subtotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="compFooter">
            <div className="compNotes">
              {tieneNotaCredito ? (
                <div className="compNote">
                  NC aplicada: parte del monto fue cubierto con Nota de Credito.
                </div>
              ) : (
                <div className="compNoteNeutral">
                  Pago sin Nota de Credito aplicada.
                </div>
              )}

              <div className="compMetodo">
                MEDIO DE PAGO:
                <span>
                  {(venta.pagos ?? [])
                    .map((p) => medioMap[p.medioPago] ?? `Medio ${p.medioPago}`)
                    .join(" / ")}
                </span>
              </div>
            </div>

            <div className="compTotals">
              <div className="line">
                <span>Total bruto</span>
                <strong>{money.format(venta.total)}</strong>
              </div>
              <div className="line total">
                <span>TOTAL</span>
                <strong>{money.format(venta.total)}</strong>
              </div>
            </div>
          </div>

          <div className="compPayments">
            <h3>Detalle de pagos</h3>
            {(venta.pagos ?? []).length === 0 ? (
              <div className="muted">Sin pagos informados.</div>
            ) : (
              <ul>
                {venta.pagos.map((p, idx) => (
                  <li key={idx}>
                    {medioMap[p.medioPago] ?? `Medio ${p.medioPago}`} - <b>{money.format(p.monto)}</b>
                    {p.referencia ? ` (ref: ${p.referencia})` : ""}
                  </li>
                ))}
              </ul>
            )}
          </div>

          <p className="compThanks">{datosFactura.mensajeAgradecimiento}</p>
        </section>
      ) : null}
    </div>
  );
}

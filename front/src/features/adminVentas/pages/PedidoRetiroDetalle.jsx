import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { retiroDetalle, cambiarEstadoRetiro } from "../api/adminVentas.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });
const retiroMap = { 1: "Pendiente", 2: "Preparando", 3: "Listo para retirar", 4: "Entregado" };

export default function PedidoRetiroDetalle() {
  const { ventaId } = useParams();
  const [venta, setVenta] = useState(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  const load = async () => {
    setErr("");
    setLoading(true);
    try {
      const data = await retiroDetalle(ventaId);
      setVenta(data);
    } catch (e) {
      setErr(e?.message ?? "No se pudo cargar el pedido.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [ventaId]);

  const setEstado = async (nuevo) => {
    await cambiarEstadoRetiro(ventaId, nuevo);
    await load();
  };

  return (
    <div>
      <div className="aRow" style={{ justifyContent: "space-between", marginBottom: 10 }}>
        <div>
          <h1 className="aTitle" style={{ marginBottom: 4 }}>Pedido #{ventaId}</h1>
          <p className="aSub" style={{ marginBottom: 0 }}>Detalle + cambio de estado del retiro.</p>
        </div>
        <Link className="aBtnGhost" to="/admin/retiros">Volver</Link>
      </div>

      {loading ? <p>Cargando...</p> : null}
      {err ? <p style={{ color: "#b00020" }}>{err}</p> : null}

      {!loading && !err && venta ? (
        <div className="aGrid2">
          <section className="aCard">
            <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Productos</h3>
            <div className="aTableWrap">
              <table className="aTable" style={{ minWidth: 520 }}>
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th className="aRight">Cant.</th>
                    <th className="aRight">Precio</th>
                    <th className="aRight">Subtotal</th>
                  </tr>
                </thead>
                <tbody>
                  {(venta.detalles ?? []).map((d) => (
                    <tr key={`${d.productoId}-${d.nombreProducto}`}>
                      <td style={{ fontWeight: 900 }}>{d.nombreProducto}</td>
                      <td className="aRight">{d.cantidad}</td>
                      <td className="aRight">{money.format(d.precioUnitario)}</td>
                      <td className="aRight" style={{ fontWeight: 900 }}>{money.format(d.subtotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="aRow" style={{ justifyContent: "flex-end", marginTop: 10 }}>
              <span style={{ fontWeight: 900 }}>Total: {money.format(venta.total)}</span>
            </div>
          </section>

          <aside className="aCard">
            <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Estado retiro</h3>
            <div style={{ marginBottom: 8, color: "#666", fontSize: 13 }}>
              Actual: <b>{retiroMap[venta.estadoRetiro] ?? `Estado ${venta.estadoRetiro}`}</b>
            </div>

            <div className="aRow">
              <button className="aBtnGhost" onClick={() => setEstado(1)}>Pendiente</button>
              <button className="aBtnGhost" onClick={() => setEstado(2)}>Preparando</button>
              <button className="aBtnGhost" onClick={() => setEstado(3)}>Listo</button>
              <button className="aBtn" onClick={() => setEstado(4)}>Entregado</button>
            </div>

            <hr style={{ border: 0, borderTop: "1px solid #eee", margin: "14px 0" }} />

            <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Cliente</h3>
            <div style={{ fontSize: 14 }}>
              <div><b>{venta.clienteNombre ?? "-"}</b></div>
              <div style={{ color: "#666" }}>{venta.clienteDni ?? "-"}</div>
            </div>
          </aside>
        </div>
      ) : null}
    </div>
  );
}

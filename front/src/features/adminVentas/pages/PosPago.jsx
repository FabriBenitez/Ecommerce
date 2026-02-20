import { useMemo, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { crearVentaPresencial } from "../api/adminVentas.api";
import "./PosPago.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

const MEDIOS = [
  { id: 1, label: "Efectivo" },
  { id: 2, label: "Debito" },
  { id: 3, label: "Credito" },
  { id: 4, label: "Transferencia" },
  { id: 5, label: "Nota de credito" },
];

const medioLabel = (medioPago) => MEDIOS.find((m) => m.id === medioPago)?.label ?? `Medio ${medioPago}`;

export default function PosPago() {
  const { state } = useLocation();
  const navigate = useNavigate();
  const venta = state;

  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [pagos, setPagos] = useState([]);

  const totalPagos = useMemo(
    () => pagos.reduce((acc, p) => acc + Number(p.monto || 0), 0),
    [pagos]
  );
  const diff = useMemo(() => (venta?.total ?? 0) - totalPagos, [venta, totalPagos]);

  if (!venta) {
    return (
      <div className="aCard">
        <p>Sesion invalida. Volve a Mostrador.</p>
        <Link className="aBtn" to="/admin/pos">Ir a POS</Link>
      </div>
    );
  }

  const estaSeleccionado = (medioPago) => pagos.some((p) => p.medioPago === medioPago);

  const toggleMedio = (medioPago) => {
    setError("");
    setPagos((prev) => {
      const exists = prev.some((p) => p.medioPago === medioPago);
      if (exists) return prev.filter((p) => p.medioPago !== medioPago);
      return [...prev, { medioPago, monto: 0, referencia: "" }];
    });
  };

  const setPagoPorMedio = (medioPago, patch) => {
    setPagos((prev) => prev.map((p) => (p.medioPago === medioPago ? { ...p, ...patch } : p)));
  };

  const confirmar = async () => {
    setError("");

    if (pagos.length === 0) {
      setError("Selecciona al menos un medio de pago.");
      return;
    }

    if (Math.abs(diff) > 0.0001) {
      setError("La suma de pagos debe coincidir con el total.");
      return;
    }

    const ncUsada = pagos
      .filter((p) => Number(p.medioPago) === 5)
      .reduce((acc, p) => acc + Number(p.monto || 0), 0);
    if (ncUsada > (venta.notaCredito ?? 0)) {
      setError("Nota de credito insuficiente.");
      return;
    }

    try {
      setLoading(true);

      const payload = {
        clienteDni: venta.clienteDni,
        clienteNombre: venta.clienteNombre,
        items: venta.items.map((i) => ({ productoId: i.productoId, cantidad: i.cantidad })),
        pagos: pagos.map((p) => ({
          medioPago: Number(p.medioPago),
          monto: Number(p.monto),
          referencia: p.referencia?.trim() || null,
        })),
        observaciones: null,
      };

      const resp = await crearVentaPresencial(payload);
      navigate(`/admin/facturas/${resp?.ventaId}`);
    } catch (e) {
      const apiError = e?.response?.data;
      setError(typeof apiError === "string" ? apiError : apiError?.error ?? "No se pudo registrar la venta.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="aGrid2">
      <section className="aCard">
        <div className="aRow" style={{ justifyContent: "space-between", marginBottom: 10 }}>
          <div>
            <h1 className="aTitle">Validacion de pago</h1>
            <p className="aSub">Selecciona medios y completa montos debajo de cada uno.</p>
          </div>
          <Link className="aBtnGhost" to="/admin/pos">Volver al carrito</Link>
        </div>

        <div className="ppMetodoGrid">
          {MEDIOS.map((m) => {
            const active = estaSeleccionado(m.id);
            return (
              <button
                key={m.id}
                type="button"
                className={`ppMetodoBtn ${active ? "isActive" : ""}`}
                onClick={() => toggleMedio(m.id)}
              >
                <span className="ppMetodoLabel">{m.label}</span>
              </button>
            );
          })}
        </div>

        <div className="ppRows">
          {pagos.map((p) => (
            <div key={p.medioPago} className="ppRow">
              <div className="ppRowHead">
                <span className="ppRowName">{medioLabel(p.medioPago)}</span>
              </div>

              <div className="ppRowFields">
                <label className="ppField">
                  <span>Monto</span>
                  <input
                    className="aInput"
                    type="number"
                    min="0"
                    step="0.01"
                    value={p.monto}
                    onChange={(e) => setPagoPorMedio(p.medioPago, { monto: e.target.value })}
                    placeholder="0.00"
                  />
                </label>

                {p.medioPago !== 1 ? (
                  <label className="ppField">
                    <span>Referencia (opcional)</span>
                    <input
                      className="aInput"
                      value={p.referencia ?? ""}
                      onChange={(e) => setPagoPorMedio(p.medioPago, { referencia: e.target.value })}
                      placeholder="Ej: ultimos 4 digitos / nro op"
                    />
                  </label>
                ) : null}
              </div>
            </div>
          ))}
        </div>

        <div className="ppSummary">
          <div className="ppLine"><span>Total venta</span><strong>{money.format(venta.total)}</strong></div>
          <div className="ppLine"><span>Total pagos</span><strong>{money.format(totalPagos)}</strong></div>
          <div className={`ppLine ${Math.abs(diff) < 0.0001 ? "ok" : "err"}`}>
            <span>Diferencia</span>
            <strong>{money.format(diff)}</strong>
          </div>
        </div>

        {error ? <p className="ppError">{error}</p> : null}

        <button className="aBtn ppConfirmBtn" onClick={confirmar} disabled={loading}>
          {loading ? "Confirmando..." : "Confirmar y registrar venta"}
        </button>
      </section>

      <aside className="aCard">
        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Resumen de venta</h3>

        <div className="aTableWrap">
          <table className="aTable" style={{ minWidth: 520 }}>
            <thead>
              <tr>
                <th>Producto</th>
                <th className="aRight">Cant.</th>
                <th className="aRight">Precio</th>
                <th className="aRight">Sub</th>
              </tr>
            </thead>
            <tbody>
              {venta.items.map((i) => (
                <tr key={i.productoId}>
                  <td style={{ fontWeight: 900 }}>{i.nombre}</td>
                  <td className="aRight">{i.cantidad}</td>
                  <td className="aRight">{money.format(i.precio)}</td>
                  <td className="aRight" style={{ fontWeight: 900 }}>{money.format(i.precio * i.cantidad)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </aside>
    </div>
  );
}

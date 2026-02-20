import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { retiroDetalle, registrarDevolucion, notaCreditoPorDni } from "../api/adminVentas.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

export default function Devolucion() {
  const { ventaId } = useParams();
  const [venta, setVenta] = useState(null);
  const [dni, setDni] = useState("");
  const [saldoNC, setSaldoNC] = useState(0);
  const [items, setItems] = useState([]);
  const [monto, setMonto] = useState(0);
  const [motivo, setMotivo] = useState("");
  const [generarNC, setGenerarNC] = useState(true);

  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");
  const [ok, setOk] = useState("");

  const load = async () => {
    setErr("");
    setOk("");
    setLoading(true);
    try {
      const data = await retiroDetalle(ventaId);
      setVenta(data);
      setDni(data?.clienteDni ?? "");
      const mapped = (data?.detalles ?? []).map((d) => ({
        productoId: d.productoId,
        nombre: d.nombreProducto,
        max: d.cantidad,
        cantidadDev: 0,
      }));
      setItems(mapped);
      setMonto(Number(data?.total ?? 0));
    } catch (e) {
      setErr(e?.message ?? "No se pudo cargar venta.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [ventaId]);

  const totalSeleccionado = useMemo(() => items.reduce((a, i) => a + (i.cantidadDev || 0), 0), [items]);

  const verNC = async () => {
    if (!dni.trim()) return;
    const data = await notaCreditoPorDni(dni.trim());
    setSaldoNC(Number(data?.saldoDisponible ?? 0));
  };

  const setCant = (productoId, v) => {
    setItems((prev) =>
      prev.map((i) => {
        if (i.productoId !== productoId) return i;
        const n = Math.max(0, Math.min(i.max, Number(v || 0)));
        return { ...i, cantidadDev: n };
      })
    );
  };

  const confirmar = async () => {
    setErr("");
    setOk("");

    const payload = {
      clienteDni: dni.trim(),
      motivo: motivo.trim() || null,
      generarNotaCredito: generarNC,
      montoDevolver: Number(monto),
      items: items.filter((i) => i.cantidadDev > 0).map((i) => ({ productoId: i.productoId, cantidad: i.cantidadDev })),
    };

    if (payload.items.length === 0) {
      setErr("Selecciona al menos 1 item a devolver.");
      return;
    }
    if (payload.montoDevolver <= 0) {
      setErr("Monto a devolver invalido.");
      return;
    }

    try {
      const resp = await registrarDevolucion(ventaId, payload);
      setOk(`Devolucion registrada. NC id: ${resp?.notaCreditoId ?? 0}`);
      await load();
    } catch (e) {
      setErr(e?.message ?? "No se pudo registrar devolucion.");
    }
  };

  return (
    <div>
      <div className="aRow" style={{ justifyContent: "space-between", marginBottom: 10 }}>
        <div>
          <h1 className="aTitle" style={{ marginBottom: 4 }}>Devolucion - Venta #{ventaId}</h1>
          <p className="aSub" style={{ marginBottom: 0 }}>Restock + Nota de credito o reintegro.</p>
        </div>
        <Link className="aBtnGhost" to="/admin/facturas">Volver</Link>
      </div>

      {loading ? <p>Cargando...</p> : null}
      {err ? <p style={{ color: "#b00020" }}>{err}</p> : null}
      {ok ? <p style={{ color: "#0a6a33" }}>{ok}</p> : null}

      {!loading && venta ? (
        <div className="aGrid2">
          <section className="aCard">
            <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Items a devolver</h3>

            <div className="aTableWrap">
              <table className="aTable" style={{ minWidth: 640 }}>
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th className="aRight">Comprado</th>
                    <th className="aRight">Devolver</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((i) => (
                    <tr key={i.productoId}>
                      <td style={{ fontWeight: 900 }}>{i.nombre}</td>
                      <td className="aRight">{i.max}</td>
                      <td className="aRight">
                        <input
                          className="aInput"
                          style={{ maxWidth: 140 }}
                          type="number"
                          value={i.cantidadDev}
                          onChange={(e) => setCant(i.productoId, e.target.value)}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div style={{ marginTop: 10, color: "#666", fontSize: 13 }}>
              Items seleccionados: <b>{totalSeleccionado}</b>
            </div>
          </section>

          <aside className="aCard">
            <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Resumen devolucion</h3>

            <div style={{ display: "grid", gap: 10 }}>
              <input className="aInput" placeholder="DNI cliente" value={dni} onChange={(e) => setDni(e.target.value)} />
              <button className="aBtnGhost" onClick={verNC}>Ver nota de credito</button>
              <div style={{ fontSize: 13, color: "#0a6a33" }}>
                Saldo NC: <b>{money.format(saldoNC)}</b>
              </div>

              <input className="aInput" type="number" value={monto} onChange={(e) => setMonto(e.target.value)} placeholder="Monto a devolver" />

              <input className="aInput" value={motivo} onChange={(e) => setMotivo(e.target.value)} placeholder="Motivo (opcional)" />

              <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13 }}>
                <input type="checkbox" checked={generarNC} onChange={(e) => setGenerarNC(e.target.checked)} />
                Generar Nota de Credito (si no, reintegro)
              </label>

              <button className="aBtn" onClick={confirmar}>Confirmar devolucion</button>
            </div>
          </aside>
        </div>
      ) : null}
    </div>
  );
}

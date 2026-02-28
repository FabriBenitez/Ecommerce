import { useEffect, useState } from "react";
import { abrirCaja, cerrarCaja, movimientosCaja, reporteCaja, reporteVentas, resumenCaja } from "../api/adminGeneral.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

export default function CajaAdmin() {
  const [resumen, setResumen] = useState(null);
  const [movimientos, setMovimientos] = useState([]);
  const [monto, setMonto] = useState("");
  const [err, setErr] = useState("");
  const [msg, setMsg] = useState("");
  const [diario, setDiario] = useState(null);
  const [ventasDia, setVentasDia] = useState(null);

  const cargar = async () => {
    const hoy = new Date();
    const iso = hoy.toISOString().slice(0, 10);
    const desde = `${iso}T00:00:00`;
    const hasta = `${iso}T23:59:59`;

    const [r, m, c, v] = await Promise.all([
      resumenCaja(),
      movimientosCaja({ desde, hasta }),
      reporteCaja({ fecha: iso }),
      reporteVentas({ desde, hasta }),
    ]);
    setResumen(r);
    setMovimientos(m);
    setDiario(c);
    setVentasDia(v);
  };

  useEffect(() => {
    cargar().catch(() => {
      setResumen(null);
      setMovimientos([]);
    });
  }, []);

  const onAbrir = async () => {
    setErr("");
    setMsg("");
    try {
      await abrirCaja({ saldoInicial: Number(monto || 0), observaciones: "Apertura desde panel AdminGeneral" });
      setMsg("Caja abierta.");
      setMonto("");
      await cargar();
    } catch (error) {
      setErr(error?.response?.data?.error ?? error?.response?.data ?? "No se pudo abrir caja.");
    }
  };

  const onCerrar = async () => {
    setErr("");
    setMsg("");
    try {
      await cerrarCaja({ saldoFinal: Number(monto || 0), observaciones: "Cierre desde panel AdminGeneral" });
      setMsg("Caja cerrada.");
      setMonto("");
      await cargar();
    } catch (error) {
      setErr(error?.response?.data?.error ?? error?.response?.data ?? "No se pudo cerrar caja.");
    }
  };

  return (
    <>
      <section className="agPageHead">
        <h1>Control de Caja</h1>
        <p>Gestion administrativa diaria de movimientos e ingresos.</p>
      </section>

      <section className="agSection agGrid agGrid--2">
        <article className="agCard">
          <h3>{resumen?.abierta ? "Caja Principal - Turno Activo" : "Caja cerrada"}</h3>
          <div className="agKpis" style={{ gridTemplateColumns: "1fr 1fr", marginBottom: 12 }}>
            <div className="agKpi"><label>Balance inicial</label><strong>{money.format(resumen?.saldoInicial ?? 0)}</strong></div>
            <div className="agKpi agKpi--ok"><label>Balance actual</label><strong>{money.format(resumen?.saldoActual ?? 0)}</strong></div>
          </div>

          <div className="agFormRow">
            <label>Monto para apertura/cierre<input value={monto} onChange={(e) => setMonto(e.target.value)} /></label>
            <div style={{ display: "flex", gap: 8, alignItems: "end" }}>
              <button className="agBtn" onClick={onAbrir} type="button">Abrir Caja</button>
              <button className="agBtn agBtn--ghost" onClick={onCerrar} type="button">Cerrar Caja</button>
            </div>
          </div>

          {msg ? <p className="agOk">{msg}</p> : null}
          {err ? <p className="agErr">{err}</p> : null}

          <h3 style={{ marginTop: 18 }}>Movimientos del Dia</h3>
          <div className="agTableWrap">
            <table className="agTable">
              <thead>
                <tr>
                  <th>Hora</th>
                  <th>Tipo</th>
                  <th>Concepto</th>
                  <th>Monto</th>
                </tr>
              </thead>
              <tbody>
                {movimientos.length === 0 ? (
                  <tr><td colSpan={4}>Sin movimientos.</td></tr>
                ) : (
                  movimientos.map((m) => (
                    <tr key={m.id}>
                      <td>{new Date(m.fecha).toLocaleTimeString("es-AR", { hour: "2-digit", minute: "2-digit" })}</td>
                      <td>
                        <span className={`agBadge ${m.tipo === 1 ? "agBadge--ok" : "agBadge--danger"}`}>
                          {m.tipo === 1 ? "Ingreso" : "Egreso"}
                        </span>
                      </td>
                      <td>{m.concepto}</td>
                      <td>{money.format(m.monto)}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </article>

        <article className="agCard agCard--soft">
          <h3>Resumen Diario</h3>
          <div className="agList">
            <div className="agItem"><div className="agItem__icon">V</div><div><h4>Ventas del dia</h4></div><div className="agItem__value">{ventasDia?.cantidadVentas ?? 0}</div></div>
            <div className="agItem"><div className="agItem__icon">$</div><div><h4>Total Ingresos</h4></div><div className="agItem__value">{money.format(resumen?.ingresos ?? 0)}</div></div>
            <div className="agItem"><div className="agItem__icon">-</div><div><h4>Total Egresos</h4></div><div className="agItem__value">{money.format(resumen?.egresos ?? 0)}</div></div>
            <div className="agItem"><div className="agItem__icon">=</div><div><h4>Neto del dia</h4></div><div className="agItem__value">{money.format(diario?.saldoNeto ?? ((resumen?.ingresos ?? 0) - (resumen?.egresos ?? 0)))}</div></div>
          </div>
          <div className="agHint" style={{ marginTop: 12 }}>
            No olvides realizar el arqueo de caja fisico antes del cierre formal.
          </div>
        </article>
      </section>
    </>
  );
}

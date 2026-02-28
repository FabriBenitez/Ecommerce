import { useMemo, useEffect, useState } from "react";
import { abrirCaja, cerrarCaja, historialCajaDiaria, movimientosCaja, reporteCaja, reporteVentas, resumenCaja } from "../api/adminGeneral.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function getErrorMessage(error, fallback) {
  const data = error?.response?.data;
  if (!data) return fallback;
  if (typeof data === "string") return data;
  if (typeof data?.error === "string") return data.error;
  if (typeof data?.title === "string") return data.title;

  const errors = data?.errors;
  if (errors && typeof errors === "object") {
    const first = Object.values(errors).find((v) => Array.isArray(v) && v.length > 0);
    if (Array.isArray(first) && typeof first[0] === "string") return first[0];
  }

  return fallback;
}

function dayKey(value) {
  return new Date(value).toISOString().slice(0, 10);
}

function buildDailyFromMovimientos(movs, dias) {
  const hoy = new Date();
  const keys = [];
  for (let i = dias - 1; i >= 0; i -= 1) {
    const d = new Date(hoy);
    d.setDate(hoy.getDate() - i);
    keys.push(dayKey(d));
  }

  const map = new Map(keys.map((k) => [k, { fecha: k, ingresos: 0, egresos: 0, movimientos: 0 }]));

  for (const m of movs ?? []) {
    const k = dayKey(m.fecha);
    const row = map.get(k);
    if (!row) continue;
    const monto = Number(m.monto ?? 0);
    if (Number(m.tipo) === 1) row.ingresos += monto;
    else row.egresos += monto;
    row.movimientos += 1;
  }

  return keys
    .map((k, idx) => {
      const r = map.get(k);
      return {
        cajaId: -1 * (idx + 1),
        fecha: r.fecha,
        fechaApertura: null,
        fechaCierre: null,
        abierta: false,
        saldoInicial: 0,
        ingresos: r.ingresos,
        egresos: r.egresos,
        saldoEsperado: r.ingresos - r.egresos,
        saldoFinal: null,
        diferenciaCierre: null,
        cantidadMovimientos: r.movimientos,
      };
    })
    .reverse();
}

export default function CajaAdmin() {
  const [resumen, setResumen] = useState(null);
  const [movimientos, setMovimientos] = useState([]);
  const [monto, setMonto] = useState("");
  const [err, setErr] = useState("");
  const [msg, setMsg] = useState("");
  const [diario, setDiario] = useState(null);
  const [ventasDia, setVentasDia] = useState(null);
  const [historial, setHistorial] = useState([]);
  const [diasHistorial, setDiasHistorial] = useState(15);
  const [historialSemanal, setHistorialSemanal] = useState([]);

  async function safeCall(fn, fallback) {
    try {
      return await fn();
    } catch {
      return fallback;
    }
  }

  const cargar = async () => {
    const hoy = new Date();
    const iso = hoy.toISOString().slice(0, 10);
    const desde = `${iso}T00:00:00`;
    const hasta = `${iso}T23:59:59`;
    const inicioHistorial = new Date(hoy);
    inicioHistorial.setDate(hoy.getDate() - (diasHistorial - 1));
    const desdeHistorial = `${dayKey(inicioHistorial)}T00:00:00`;
    const hastaHistorial = `${iso}T23:59:59`;
    const inicioSemanal = new Date(hoy);
    inicioSemanal.setDate(hoy.getDate() - 6);
    const desdeSemanal = `${dayKey(inicioSemanal)}T00:00:00`;
    const hastaSemanal = `${iso}T23:59:59`;

    const [r, mHoy, mHistorial, mSemanal, c, v, hApi, hsApi] = await Promise.all([
      safeCall(() => resumenCaja(), null),
      safeCall(() => movimientosCaja({ desde, hasta }), []),
      safeCall(() => movimientosCaja({ desde: desdeHistorial, hasta: hastaHistorial }), []),
      safeCall(() => movimientosCaja({ desde: desdeSemanal, hasta: hastaSemanal }), []),
      safeCall(() => reporteCaja({ fecha: iso }), null),
      safeCall(() => reporteVentas({ desde, hasta }), null),
      safeCall(() => historialCajaDiaria({ dias: diasHistorial }), null),
      safeCall(() => historialCajaDiaria({ dias: 7 }), null),
    ]);
    setResumen(r);
    setMovimientos(mHoy);
    setDiario(c);
    setVentasDia(v);
    setHistorial(Array.isArray(hApi) && hApi.length > 0 ? hApi : buildDailyFromMovimientos(mHistorial, diasHistorial));
    setHistorialSemanal(Array.isArray(hsApi) && hsApi.length > 0 ? hsApi : buildDailyFromMovimientos(mSemanal, 7));
  };

  const resumenSemanal = useMemo(() => {
    const hoy = new Date();
    const keys = [];
    for (let i = 6; i >= 0; i -= 1) {
      const d = new Date(hoy);
      d.setDate(hoy.getDate() - i);
      keys.push(dayKey(d));
    }

    const base = keys.map((k) => ({
      key: k,
      fecha: k,
      cajas: 0,
      ingresos: 0,
      egresos: 0,
      neto: 0,
      movimientos: 0,
    }));

    const index = new Map(base.map((r) => [r.key, r]));
    for (const caja of historialSemanal) {
      const k = dayKey(caja.fecha);
      const row = index.get(k);
      if (!row) continue;
      row.cajas += 1;
      row.ingresos += Number(caja.ingresos ?? 0);
      row.egresos += Number(caja.egresos ?? 0);
      row.movimientos += Number(caja.cantidadMovimientos ?? 0);
    }

    return base.map((r) => ({ ...r, neto: r.ingresos - r.egresos }));
  }, [historialSemanal]);

  useEffect(() => {
    cargar().catch(() => {
      setResumen(null);
      setMovimientos([]);
      setHistorial([]);
    });
  }, [diasHistorial]);

  const onAbrir = async () => {
    setErr("");
    setMsg("");
    try {
      await abrirCaja({ saldoInicial: Number(monto || 0), observaciones: "Apertura desde panel AdminGeneral" });
      setMsg("Caja abierta.");
      setMonto("");
      await cargar();
    } catch (error) {
      setErr(getErrorMessage(error, "No se pudo abrir caja."));
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
      setErr(getErrorMessage(error, "No se pudo cerrar caja."));
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

      <section className="agSection">
        <article className="agCard">
          <div className="agSection__head">
            <h3 style={{ margin: 0 }}>Historial de Caja Diaria</h3>
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <label style={{ fontSize: 13, color: "#3e5a73" }}>Periodo</label>
              <select value={diasHistorial} onChange={(e) => setDiasHistorial(Number(e.target.value))}>
                <option value={7}>7 dias</option>
                <option value={15}>15 dias</option>
                <option value={30}>30 dias</option>
              </select>
            </div>
          </div>
          <div className="agTableWrap">
            <table className="agTable">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Estado</th>
                  <th>Apertura</th>
                  <th>Cierre</th>
                  <th>Inicial</th>
                  <th>Ingresos</th>
                  <th>Egresos</th>
                  <th>Esperado</th>
                  <th>Final</th>
                  <th>Diferencia</th>
                  <th>Movs.</th>
                </tr>
              </thead>
              <tbody>
                {historial.length === 0 ? (
                  <tr><td colSpan={11}>Sin cajas registradas en el periodo.</td></tr>
                ) : (
                  historial.map((caja) => (
                    <tr key={caja.cajaId}>
                      <td>{new Date(caja.fecha).toLocaleDateString("es-AR")}</td>
                      <td>
                        <span className={`agBadge ${caja.abierta ? "agBadge--warn" : "agBadge--ok"}`}>
                          {caja.abierta ? "Abierta" : "Cerrada"}
                        </span>
                      </td>
                      <td>{caja.fechaApertura ? new Date(caja.fechaApertura).toLocaleTimeString("es-AR", { hour: "2-digit", minute: "2-digit" }) : "-"}</td>
                      <td>{caja.fechaCierre ? new Date(caja.fechaCierre).toLocaleTimeString("es-AR", { hour: "2-digit", minute: "2-digit" }) : "-"}</td>
                      <td>{money.format(caja.saldoInicial ?? 0)}</td>
                      <td>{money.format(caja.ingresos ?? 0)}</td>
                      <td>{money.format(caja.egresos ?? 0)}</td>
                      <td>{money.format(caja.saldoEsperado ?? 0)}</td>
                      <td>{caja.saldoFinal == null ? "-" : money.format(caja.saldoFinal)}</td>
                      <td>{caja.diferenciaCierre == null ? "-" : money.format(caja.diferenciaCierre)}</td>
                      <td>{caja.cantidadMovimientos ?? 0}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </article>
      </section>

      <section className="agSection">
        <article className="agCard">
          <div className="agSection__head">
            <h3 style={{ margin: 0 }}>Historial Semanal Diario</h3>
            <span style={{ fontSize: 13, color: "#6f8296" }}>Ultimos 7 dias</span>
          </div>
          <div className="agTableWrap">
            <table className="agTable">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Cajas</th>
                  <th>Ingresos</th>
                  <th>Egresos</th>
                  <th>Neto</th>
                  <th>Movs.</th>
                </tr>
              </thead>
              <tbody>
                {resumenSemanal.map((d) => (
                  <tr key={d.key}>
                    <td>{new Date(`${d.fecha}T00:00:00`).toLocaleDateString("es-AR")}</td>
                    <td>{d.cajas}</td>
                    <td>{money.format(d.ingresos)}</td>
                    <td>{money.format(d.egresos)}</td>
                    <td>{money.format(d.neto)}</td>
                    <td>{d.movimientos}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>
      </section>
    </>
  );
}

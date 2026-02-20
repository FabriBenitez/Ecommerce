import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { historialPresencial, retirosList } from "../api/adminVentas.api";
import "./AdminDashboard.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

const RETIRO_STATUS = {
  1: { label: "Pendiente", cls: "pending" },
  2: { label: "Preparando", cls: "preparing" },
  3: { label: "Listo", cls: "ready" },
  4: { label: "Entregado", cls: "done" },
};

const VENTA_STATUS = {
  1: { label: "Pendiente", cls: "pending" },
  2: { label: "Pagada", cls: "paid" },
  3: { label: "Cancelada", cls: "cancelled" },
};

export default function AdminDashboard() {
  const [retiros, setRetiros] = useState([]);
  const [historial, setHistorial] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setLoading(true);
        setErr("");
        const [retirosData, historialData] = await Promise.all([
          retirosList().catch(() => []),
          historialPresencial().catch(() => []),
        ]);
        if (!alive) return;
        setRetiros(retirosData ?? []);
        setHistorial(historialData ?? []);
      } catch (e) {
        if (!alive) return;
        setErr(e?.message ?? "No se pudo cargar el panel.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, []);

  const kpis = useMemo(() => {
    const pendientes = retiros.filter((r) => r.estadoRetiro === 1).length;
    const listos = retiros.filter((r) => r.estadoRetiro === 3).length;

    const hoy = new Date();
    const isToday = (f) => new Date(f).toDateString() === hoy.toDateString();
    const ventasHoy = historial.filter((v) => isToday(v.fecha) && Number(v.estadoVenta) === 2);
    const recaudadoHoy = ventasHoy.reduce((acc, v) => acc + Number(v.total ?? 0), 0);

    return {
      pendientes,
      listos,
      ventasHoyCount: ventasHoy.length,
      recaudadoHoy,
    };
  }, [retiros, historial]);

  const actividad = useMemo(() => {
    const retirosAct = (retiros ?? []).map((r) => ({
      id: `r-${r.ventaId}`,
      fecha: r.fecha,
      title: `Pedido #${r.ventaId}`,
      sub: `${r.clienteNombre ?? "Cliente"} • Retiro en tienda`,
      status: RETIRO_STATUS[r.estadoRetiro] ?? { label: `Estado ${r.estadoRetiro}`, cls: "pending" },
      to: `/admin/retiros/${r.ventaId}`,
    }));

    const ventasAct = (historial ?? []).map((v) => ({
      id: `v-${v.id}`,
      fecha: v.fecha,
      title: `Venta POS #${v.id}`,
      sub: `${v.clienteNombre ?? "Sin nombre"} • Mostrador`,
      status: VENTA_STATUS[v.estadoVenta] ?? { label: `Estado ${v.estadoVenta}`, cls: "pending" },
      to: `/admin/facturas/${v.id}`,
    }));

    return [...retirosAct, ...ventasAct]
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha))
      .slice(0, 6);
  }, [retiros, historial]);

  return (
    <div className="dashPage">
      <h1 className="dashTitle">Panel de ventas</h1>
      <p className="dashDate">{new Date().toLocaleString("es-AR")}</p>

      {err ? <p className="dashError">{err}</p> : null}

      <section className="dashTopCards">
        <article className="dashTopCard">
          <div className="dashTopTag">Pedidos Online</div>
          <h3>Gestionar ordenes del sitio y retiros pendientes</h3>
          <Link className="dashLink" to="/admin/retiros">Ir a pedidos</Link>
        </article>

        <article className="dashTopCard">
          <div className="dashTopTag">Mostrador</div>
          <h3>Punto de venta fisico para atencion presencial</h3>
          <Link className="dashLink" to="/admin/pos">Abrir terminal</Link>
        </article>
      </section>

      <section className="dashKpis">
        <article className="dashKpiCard">
          <p className="dashKpiLabel">Pendientes</p>
          <p className="dashKpiValue">{loading ? "..." : kpis.pendientes}</p>
        </article>

        <article className="dashKpiCard">
          <p className="dashKpiLabel">Listos para retirar</p>
          <p className="dashKpiValue">{loading ? "..." : kpis.listos}</p>
        </article>

        <article className="dashKpiCard isRevenue">
          <p className="dashKpiLabel">Ventas del dia</p>
          <p className="dashKpiValue money">{loading ? "..." : money.format(kpis.recaudadoHoy)}</p>
          <p className="dashKpiSub">{loading ? "" : `${kpis.ventasHoyCount} ventas pagadas`}</p>
        </article>
      </section>

      <section className="aCard">
        <div className="dashActivityHead">
          <h2>Actividad reciente</h2>
          <Link to="/admin/facturas">Ver todo</Link>
        </div>

        <div className="dashActivityList">
          {actividad.map((item) => (
            <Link key={item.id} className="dashActivityRow" to={item.to}>
              <div className="dashActivityMain">
                <p className="dashActivityTitle">{item.title}</p>
                <p className="dashActivitySub">{item.sub}</p>
              </div>
              <span className={`dashStatus ${item.status.cls}`}>{item.status.label}</span>
            </Link>
          ))}
          {!actividad.length && !loading ? (
            <p className="dashEmpty">Sin actividad para mostrar.</p>
          ) : null}
        </div>
      </section>
    </div>
  );
}

import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { listarCompras, confirmarCompra, listarProveedores } from "../api/adminCompras.api";
import "../styles/ComprasCommon.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function proveedorText(value) {
  if (!value) return "-";
  if (typeof value === "string") return value;
  if (typeof value === "object") {
    const razon = value.razonSocial ?? value.nombre ?? "";
    const cuit = value.cuit ? ` (${value.cuit})` : "";
    return razon ? `${razon}${cuit}` : `Proveedor #${value.proveedorId ?? "-"}`;
  }
  return String(value);
}

function estadoLabel(e) {
  const map = { 1: "Pendiente", 2: "Confirmada", 3: "Cancelada" };
  return map[e] ?? String(e ?? "");
}

export default function SeguimientoPedidos() {
  const [loading, setLoading] = useState(true);
  const [compras, setCompras] = useState([]);
  const [proveedores, setProveedores] = useState([]);

  const [q, setQ] = useState("");
  const [estado, setEstado] = useState("all"); // all | 1 | 2 | 3
  const [proveedorId, setProveedorId] = useState("all");

  const [error, setError] = useState("");
  const [busyId, setBusyId] = useState(null);

  const cargar = async () => {
    const provIdParam = proveedorId === "all" ? undefined : Number(proveedorId);
    const [list, provs] = await Promise.all([
      listarCompras(provIdParam),
      listarProveedores(undefined),
    ]);
    setCompras(Array.isArray(list) ? list : []);
    setProveedores(Array.isArray(provs) ? provs : []);
  };

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setError("");
        setLoading(true);
        await cargar();
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el seguimiento de compras.");
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [proveedorId]);

  const filtradas = useMemo(() => {
    const qq = q.trim().toLowerCase();

    return (compras ?? [])
      .filter((c) => {
        if (estado === "all") return true;
        return String(c.estadoCompra) === String(estado);
      })
      .filter((c) => {
        if (!qq) return true;
        const id = String(c.id ?? "");
        const prov = proveedorText(c.proveedor).toLowerCase();
        return id.includes(qq) || prov.includes(qq);
      })
      .sort((a, b) => new Date(b.fecha) - new Date(a.fecha));
  }, [compras, q, estado]);

  const stats = useMemo(() => {
    const pend = (compras ?? []).filter((c) => c.estadoCompra === 1).length;
    const conf = (compras ?? []).filter((c) => c.estadoCompra === 2).length;
    const canc = (compras ?? []).filter((c) => c.estadoCompra === 3).length;
    return { pend, conf, canc };
  }, [compras]);

  const onConfirmar = async (id) => {
    try {
      setError("");
      setBusyId(id);
      await confirmarCompra(id);
      await cargar();
    } catch (e) {
      setError(e?.message ?? "No se pudo confirmar la compra.");
    } finally {
      setBusyId(null);
    }
  };

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Seguimiento de Compras</h1>
          <p className="cmuted">Compras pendientes por recibir, confirmación de llegada e impacto en stock.</p>
        </div>

        <div className="cactions">
          <Link className="btn btn--primary" to="/compras/nueva">Nueva compra</Link>
          <button className="btn btn--ghost" onClick={cargar} type="button">Refrescar</button>
        </div>
      </header>

      {error ? (
        <section className="ccard ccard__pad">
          <p className="cerror">{error}</p>
        </section>
      ) : null}

      <section className="grid3">
        <div className="ccard ccard__pad">
          <span className="k">Pendientes</span>
          <span className="v strong">{stats.pend}</span>
        </div>
        <div className="ccard ccard__pad">
          <span className="k">Confirmadas</span>
          <span className="v strong">{stats.conf}</span>
        </div>
        <div className="ccard ccard__pad">
          <span className="k">Canceladas</span>
          <span className="v strong">{stats.canc}</span>
        </div>
      </section>

      <section className="ccard ccard__pad">
        <div className="toolbar">
          <label className="field">
            <span>Buscar</span>
            <input
              className="cinput"
              placeholder="Por #compra o proveedor…"
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
          </label>

          <label className="field">
            <span>Estado</span>
            <select className="cinput" value={estado} onChange={(e) => setEstado(e.target.value)}>
              <option value="all">Todos</option>
              <option value="1">Pendiente</option>
              <option value="2">Confirmada</option>
              <option value="3">Cancelada</option>
            </select>
          </label>

          <label className="field">
            <span>Proveedor</span>
            <select className="cinput" value={proveedorId} onChange={(e) => setProveedorId(e.target.value)}>
              <option value="all">Todos</option>
              {(proveedores ?? []).map((p) => (
                <option key={p.id} value={p.id}>{p.razonSocial}</option>
              ))}
            </select>
          </label>
        </div>
      </section>

      {loading ? (
        <section className="ccard ccard__pad">Cargando…</section>
      ) : (
        <section className="ccard">
          <div className="tableWrap">
            <table className="ctable" style={{ minWidth: 920 }}>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Fecha</th>
                  <th>Proveedor</th>
                  <th>Estado</th>
                  <th className="right">Total</th>
                  <th className="right">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {(filtradas ?? []).map((c) => {
                  const esPendiente = c.estadoCompra === 1;
                  return (
                    <tr key={c.id}>
                      <td className="mono">#{c.id}</td>
                      <td>{c.fecha ? new Date(c.fecha).toLocaleString("es-AR") : "-"}</td>
                      <td className="strong">{proveedorText(c.proveedor)}</td>
                      <td>{estadoLabel(c.estadoCompra)}</td>
                      <td className="right strong">{money.format(c.total ?? 0)}</td>
                      <td className="right">
                        <Link className="btn btn--ghost btn--sm" to={`/compras/${c.id}`}>
                          Ver
                        </Link>

                        <button
                          className={`btn btn--sm ${esPendiente ? "btn--primary" : "btn--ghost"}`}
                          disabled={!esPendiente || busyId === c.id}
                          onClick={() => onConfirmar(c.id)}
                          type="button"
                          style={{ marginLeft: 8 }}
                          title={esPendiente ? "Confirma la llegada e ingresa stock" : "Solo disponible si está Pendiente"}
                        >
                          {busyId === c.id ? "Confirmando…" : "Confirmar llegada"}
                        </button>
                      </td>
                    </tr>
                  );
                })}

                {(filtradas ?? []).length === 0 ? (
                  <tr>
                    <td colSpan="6" className="emptyRow">No hay compras para mostrar.</td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </main>
  );
}

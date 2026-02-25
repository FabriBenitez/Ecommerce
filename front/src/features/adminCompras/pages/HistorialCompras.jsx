import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { listarCompras, listarProveedores } from "../api/adminCompras.api";
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

function estadoCompraLabel(e) {
  // Ajustá si tu enum difiere
  const map = { 1: "Pendiente", 2: "Confirmada", 3: "Cancelada" };
  return map[e] ?? String(e ?? "");
}

export default function HistorialCompras() {
  const [compras, setCompras] = useState([]);
  const [proveedores, setProveedores] = useState([]);

  const [proveedorId, setProveedorId] = useState("");
  const [q, setQ] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);

        const [prov, comp] = await Promise.all([
          listarProveedores(),
          listarCompras(),
        ]);

        if (!alive) return;
        setProveedores(Array.isArray(prov) ? prov : []);
        setCompras(Array.isArray(comp) ? comp : []);
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el historial.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
  }, []);

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        const data = await listarCompras(proveedorId ? Number(proveedorId) : undefined);
        if (!alive) return;
        setCompras(Array.isArray(data) ? data : []);
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo filtrar compras.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
  }, [proveedorId]);

  const filtradas = useMemo(() => {
    const qq = q.trim().toLowerCase();
    if (!qq) return compras ?? [];
    return (compras ?? []).filter((c) => {
      const prov = proveedorText(c.proveedor).toLowerCase();
      const id = String(c.id ?? "");
      return prov.includes(qq) || id.includes(qq);
    });
  }, [compras, q]);

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Historial de compras</h1>
          <p className="cmuted">Buscá y entrá al detalle para confirmar o ver factura.</p>
        </div>

        <div className="cactions">
          <Link className="btn btn--primary" to="/compras/nueva">Nueva compra</Link>
        </div>
      </header>

      <section className="ccard ccard__pad">
        <div className="toolbar">
          <label className="field">
            <span>Proveedor</span>
            <select className="cinput" value={proveedorId} onChange={(e) => setProveedorId(e.target.value)}>
              <option value="">Todos</option>
              {(proveedores ?? []).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.razonSocial ?? p.razon ?? `Proveedor #${p.id}`}
                </option>
              ))}
            </select>
          </label>

          <label className="field">
            <span>Buscar</span>
            <input
              className="cinput"
              placeholder="Por proveedor o #id…"
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
          </label>
        </div>
      </section>

      {loading ? <section className="ccard ccard__pad">Cargando…</section> : null}
      {error ? <section className="ccard ccard__pad cerror">{error}</section> : null}

      {!loading && !error ? (
        <section className="ccard">
          <div className="tableWrap">
            <table className="ctable">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Fecha</th>
                  <th>Proveedor</th>
                  <th>Estado</th>
                  <th className="right">Total</th>
                  <th className="right">Acción</th>
                </tr>
              </thead>
              <tbody>
                {(filtradas ?? []).map((c) => (
                  <tr key={c.id}>
                    <td className="mono">#{c.id}</td>
                    <td>{c.fecha ? new Date(c.fecha).toLocaleString("es-AR") : "-"}</td>
                    <td>{proveedorText(c.proveedor)}</td>
                    <td>{estadoCompraLabel(c.estadoCompra)}</td>
                    <td className="right strong">{money.format(c.total ?? 0)}</td>
                    <td className="right">
                      <Link className="btn btn--ghost btn--sm" to={`/compras/${c.id}`}>
                        Ver detalle
                      </Link>
                    </td>
                  </tr>
                ))}
                {(filtradas ?? []).length === 0 ? (
                  <tr>
                    <td colSpan="6" className="emptyRow">No hay compras para mostrar.</td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
    </main>
  );
}

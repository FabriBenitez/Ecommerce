import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { historialPresencial } from "../api/adminVentas.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });
const estadoMap = { 1: "Pendiente", 2: "Pagada", 3: "Preparado", 4: "ListoParaRetirar", 5: "Cancelada" };

export default function HistorialFacturas() {
  const [rows, setRows] = useState([]);
  const [q, setQ] = useState("");
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setLoading(true);
        const data = await historialPresencial();
        if (!alive) return;
        setRows(data ?? []);
      } catch (e) {
        if (!alive) return;
        setErr(e?.message ?? "No se pudo cargar historial.");
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, []);

  const filtrados = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return rows;
    return rows.filter(
      (r) =>
        String(r.id ?? "").includes(s) ||
        String(r.clienteDni ?? "").toLowerCase().includes(s) ||
        String(r.clienteNombre ?? "").toLowerCase().includes(s)
    );
  }, [rows, q]);

  return (
    <div>
      <h1 className="aTitle">Historial de facturas y ventas</h1>
      <p className="aSub">Incluye ventas presenciales.</p>

      <section className="aCard" style={{ marginBottom: 14 }}>
        <input className="aInput" placeholder="Buscar por ID, DNI o nombre..." value={q} onChange={(e) => setQ(e.target.value)} />
      </section>

      {loading ? <p>Cargando...</p> : null}
      {err ? <p style={{ color: "#b00020" }}>{err}</p> : null}

      {!loading && !err ? (
        <section className="aCard">
          <div className="aTableWrap">
            <table className="aTable">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Fecha</th>
                  <th>DNI / Cliente</th>
                  <th className="aRight">Total</th>
                  <th>Estado</th>
                  <th className="aRight">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filtrados.map((r) => (
                  <tr key={r.id}>
                    <td style={{ fontWeight: 900 }}>#{r.id}</td>
                    <td>{new Date(r.fecha).toLocaleString("es-AR")}</td>
                    <td>
                      <div style={{ fontWeight: 900 }}>{r.clienteNombre ?? "-"}</div>
                      <div style={{ color: "#666", fontSize: 12 }}>{r.clienteDni ?? "-"}</div>
                    </td>
                    <td className="aRight" style={{ fontWeight: 900 }}>{money.format(r.total)}</td>
                    <td>
                      <span className={r.estadoVenta === 2 ? "aBadge aBadge--green" : r.estadoVenta === 5 ? "aBadge aBadge--gray" : "aBadge aBadge--yellow"}>
                        {estadoMap[r.estadoVenta] ?? `Estado ${r.estadoVenta}`}
                      </span>
                    </td>
                    <td className="aRight">
                      <Link className="aBtnGhost" to={`/admin/facturas/${r.id}`}>Ver</Link>{" "}
                      <Link className="aBtnGhost" to={`/admin/devolucion/${r.id}`}>Devolucion</Link>
                    </td>
                  </tr>
                ))}
                {filtrados.length === 0 ? (
                  <tr>
                    <td colSpan={6} style={{ color: "#666" }}>Sin resultados.</td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
    </div>
  );
}

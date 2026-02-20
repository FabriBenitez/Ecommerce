import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { retirosList, cambiarEstadoRetiro } from "../api/adminVentas.api";
import "./PedidosRetiro.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

const ESTADOS = [
  { id: 0, label: "Todos" },
  { id: 1, label: "Pendiente" },
  { id: 2, label: "Preparando" },
  { id: 3, label: "Listo" },
  { id: 4, label: "Entregado" },
];

const RETIRO_LABEL = {
  1: "Pendiente",
  2: "Preparando",
  3: "Listo",
  4: "Entregado",
};

function initials(nombre) {
  const n = String(nombre ?? "").trim();
  if (!n) return "CL";
  const parts = n.split(" ").filter(Boolean);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

function nextAction(estadoRetiro) {
  if (estadoRetiro === 1) return { next: 2, label: "Marcar preparando" };
  if (estadoRetiro === 2) return { next: 3, label: "Marcar listo" };
  if (estadoRetiro === 3) return { next: 4, label: "Entregar pedido" };
  return null;
}

export default function PedidosRetiro() {
  const [rows, setRows] = useState([]);
  const [q, setQ] = useState("");
  const [estado, setEstado] = useState(0);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");
  const [updatingId, setUpdatingId] = useState(null);

  const load = async () => {
    setErr("");
    setLoading(true);
    try {
      const data = await retirosList(estado === 0 ? {} : { estado });
      setRows(data ?? []);
    } catch (e) {
      setErr(e?.message ?? "No se pudo cargar retiros.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [estado]);

  const filtrados = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return rows;

    return rows.filter((r) => {
      return (
        String(r.ventaId ?? "").toLowerCase().includes(s) ||
        String(r.clienteDni ?? "").toLowerCase().includes(s) ||
        String(r.clienteNombre ?? "").toLowerCase().includes(s)
      );
    });
  }, [rows, q]);

  const pendientesCount = useMemo(
    () => rows.filter((r) => r.estadoRetiro === 1).length,
    [rows]
  );

  const actualizarEstado = async (ventaId, nuevoEstado) => {
    try {
      setUpdatingId(ventaId);
      await cambiarEstadoRetiro(ventaId, nuevoEstado);
      await load();
    } catch (e) {
      setErr(e?.response?.data?.error ?? e?.message ?? "No se pudo actualizar el estado.");
    } finally {
      setUpdatingId(null);
    }
  };

  return (
    <div className="prPage">
      <header className="prHeader">
        <div>
          <h1 className="prTitle">Gestion de Pedidos Online Retiro</h1>
          <p className="prSub">• {pendientesCount} pedidos pendientes de retiro hoy</p>
        </div>
        <button className="aBtn prRefresh" onClick={load}>Refrescar lista</button>
      </header>

      <section className="prToolbar aCard">
        <div className="prTabs" role="tablist" aria-label="Filtrar por estado">
          {ESTADOS.map((e) => (
            <button
              key={e.id}
              className={`prTab ${estado === e.id ? "isActive" : ""}`}
              onClick={() => setEstado(e.id)}
            >
              {e.label}
            </button>
          ))}
        </div>

        <div className="prSearchWrap">
          <input
            className="aInput"
            placeholder="Buscar por cliente, libro o ID de pedido..."
            value={q}
            onChange={(ev) => setQ(ev.target.value)}
          />
        </div>
      </section>

      {loading ? <p className="prMsg">Cargando...</p> : null}
      {err ? <p className="prMsg prErr">{err}</p> : null}

      {!loading && !err ? (
        <section className="aCard">
          <div className="aTableWrap">
            <table className="aTable prTable">
              <thead>
                <tr>
                  <th>ID Pedido</th>
                  <th>Fecha y hora</th>
                  <th>Cliente</th>
                  <th className="aRight">Total</th>
                  <th>Estado</th>
                  <th className="aRight">Acciones</th>
                </tr>
              </thead>

              <tbody>
                {filtrados.map((r) => {
                  const action = nextAction(r.estadoRetiro);
                  const estadoLabel = RETIRO_LABEL[r.estadoRetiro] ?? `Estado ${r.estadoRetiro}`;

                  return (
                    <tr key={r.ventaId}>
                      <td className="prId">#ORD-{r.ventaId}</td>
                      <td>{r.fecha ? new Date(r.fecha).toLocaleString("es-AR") : "-"}</td>
                      <td>
                        <div className="prClient">
                          <span className="prAvatar">{initials(r.clienteNombre)}</span>
                          <div>
                            <div className="prClientName">{r.clienteNombre ?? "-"}</div>
                            <div className="prClientDni">{r.clienteDni ?? "-"}</div>
                          </div>
                        </div>
                      </td>
                      <td className="aRight prTotal">{money.format(Number(r.total ?? 0))}</td>
                      <td>
                        <span className={`prStatus s${r.estadoRetiro}`}>{estadoLabel}</span>
                      </td>
                      <td className="aRight">
                        <div className="prActions">
                          {action ? (
                            <button
                              className="aBtn prActionBtn"
                              onClick={() => actualizarEstado(r.ventaId, action.next)}
                              disabled={updatingId === r.ventaId}
                            >
                              {updatingId === r.ventaId ? "Actualizando..." : action.label}
                            </button>
                          ) : (
                            <span className="prDone">Completado</span>
                          )}
                          <Link className="aBtnGhost prDetailBtn" to={`/admin/retiros/${r.ventaId}`}>
                            Ver detalle
                          </Link>
                        </div>
                      </td>
                    </tr>
                  );
                })}

                {filtrados.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="prEmpty">Sin resultados para el filtro actual.</td>
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

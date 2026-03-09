import { useEffect, useMemo, useState } from "react";
import { actualizarEstadoPedidoSenia, listarPedidosSenia } from "../api/adminCompras.api";
import { confirmAction } from "@/shared/ui/sweetAlert";
import "../styles/ComprasCommon.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

const ESTADO_PEDIDO = {
  1: "Pendiente compra",
  2: "En compra",
  3: "Pedido recibido",
};

const ESTADO_RESERVA = {
  1: "Pendiente anticipo",
  2: "Señada",
  3: "Pagada",
  4: "Vencida",
  5: "Cancelada",
};

export default function PedidosSeniaCompras() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [savingId, setSavingId] = useState(null);
  const [selectedId, setSelectedId] = useState(null);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");

  const pendientes = useMemo(
    () => (items ?? []).filter((x) => Number(x.estado) !== 3).length,
    [items],
  );

  const selected = useMemo(
    () => (items ?? []).find((x) => x.id === selectedId) ?? null,
    [items, selectedId],
  );

  const cargar = async () => {
    const data = await listarPedidosSenia();
    const list = Array.isArray(data) ? data : [];
    setItems(list);
    if (list.length > 0 && !selectedId) setSelectedId(list[0].id);
  };

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setLoading(true);
        setError("");
        await cargar();
      } catch (e) {
        if (!alive) return;
        setError(e?.response?.data?.error ?? e?.message ?? "No se pudieron cargar pedidos por seña.");
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, []);

  const cambiarEstado = async (id, estado) => {
    const texto = Number(estado) === 2
      ? `El pedido #${id} pasara a "En compra".`
      : `El pedido #${id} pasara a "Pedido recibido".`;
    const ok = await confirmAction({
      title: "Actualizar estado",
      text: texto,
      confirmText: "Si, continuar",
      cancelText: "Cancelar",
      icon: "warning",
    });
    if (!ok) return;

    try {
      setSavingId(id);
      setError("");
      setMsg("");
      await actualizarEstadoPedidoSenia(id, estado);
      setMsg(`Pedido #${id} actualizado.`);
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo actualizar el estado.");
    } finally {
      setSavingId(null);
    }
  };

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Pedidos por señas</h1>
          <p className="cmuted">Se generan cuando AdminVentas registra una seña presencial.</p>
        </div>
        <span className="cbadge">{pendientes} pendientes</span>
      </header>

      <section className="ccard ccard__pad" style={{ marginBottom: 14 }}>
        {loading ? <p>Cargando...</p> : null}
        {error ? <p style={{ color: "#b00020" }}>{error}</p> : null}
        {msg ? <p style={{ color: "#0a6a33" }}>{msg}</p> : null}

        <div className="aTableWrap">
          <table className="aTable" style={{ minWidth: 900 }}>
            <thead>
              <tr>
                <th>Pedido #</th>
                <th>Cliente</th>
                <th className="aRight">Total</th>
                <th className="aRight">Seña</th>
                <th className="aRight">Saldo</th>
                <th>Estado pedido</th>
                <th>Estado reserva</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {(items ?? []).map((r) => (
                <tr
                  key={r.id}
                  onClick={() => setSelectedId(r.id)}
                  style={{
                    cursor: "pointer",
                    background: selectedId === r.id ? "#f8fafc" : "transparent",
                  }}
                >
                  <td>#{r.id}</td>
                  <td>{r.clienteNombre || "-"} ({r.clienteDni || "-"})</td>
                  <td className="aRight">{money.format(r.total)}</td>
                  <td className="aRight">{money.format(r.montoSenia)}</td>
                  <td className="aRight">{money.format(r.saldoPendiente)}</td>
                  <td>{ESTADO_PEDIDO[r.estado] ?? r.estado}</td>
                  <td>{ESTADO_RESERVA[r.estadoReserva] ?? r.estadoReserva}</td>
                  <td>
                    <div className="aRow" style={{ gap: 8 }}>
                      {Number(r.estado) === 1 ? (
                        <button
                          className="aBtnGhost"
                          disabled={savingId === r.id}
                          onClick={(e) => {
                            e.stopPropagation();
                            cambiarEstado(r.id, 2);
                          }}
                        >
                          Marcar en compra
                        </button>
                      ) : null}

                      {Number(r.estado) !== 3 ? (
                        <button
                          className="aBtnGhost"
                          disabled={savingId === r.id}
                          onClick={(e) => {
                            e.stopPropagation();
                            cambiarEstado(r.id, 3);
                          }}
                        >
                          Pedido recibido
                        </button>
                      ) : null}
                    </div>
                  </td>
                </tr>
              ))}
              {(items ?? []).length === 0 ? (
                <tr><td colSpan="8" style={{ textAlign: "center", color: "#666" }}>No hay pedidos por seña.</td></tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>

      <section className="ccard ccard__pad">
        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Detalle para compra</h3>
        {!selected ? (
          <p style={{ color: "#666" }}>Selecciona un pedido para ver su información.</p>
        ) : (
          <>
            <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))" }}>
              <div><span className="cmuted">Pedido:</span> <b>#{selected.id}</b></div>
              <div><span className="cmuted">Reserva:</span> <b>#{selected.reservaId}</b></div>
              <div><span className="cmuted">Cliente:</span> <b>{selected.clienteNombre || "-"}</b></div>
              <div><span className="cmuted">DNI:</span> <b>{selected.clienteDni || "-"}</b></div>
              <div><span className="cmuted">Teléfono:</span> <b>{selected.clienteTelefono || "-"}</b></div>
              <div><span className="cmuted">Cantidad de items:</span> <b>{selected.cantidadItems ?? 0}</b></div>
              <div><span className="cmuted">Total:</span> <b>{money.format(selected.total)}</b></div>
              <div><span className="cmuted">Seña:</span> <b>{money.format(selected.montoSenia)}</b></div>
              <div><span className="cmuted">Saldo:</span> <b>{money.format(selected.saldoPendiente)}</b></div>
              <div><span className="cmuted">Estado pedido:</span> <b>{ESTADO_PEDIDO[selected.estado] ?? selected.estado}</b></div>
              <div><span className="cmuted">Estado reserva:</span> <b>{ESTADO_RESERVA[selected.estadoReserva] ?? selected.estadoReserva}</b></div>
              <div style={{ gridColumn: "1 / -1" }}>
                <span className="cmuted">Detalle de libros:</span> <b>{selected.resumenItems || "-"}</b>
              </div>
            </div>

          </>
        )}
      </section>
    </main>
  );
}

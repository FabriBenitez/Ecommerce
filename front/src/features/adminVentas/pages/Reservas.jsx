import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  cancelarReserva,
  completarPagoReserva,
  detalleReserva,
  listarReservas,
  resumenLibrosSenia,
} from "../api/adminVentas.api";
import { confirmAction } from "@/shared/ui/sweetAlert";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

const ESTADOS = {
  1: "Pendiente anticipo",
  2: "Señada",
  3: "Pagada",
  4: "Vencida",
  5: "Cancelada",
};
const ESTADO_PEDIDO = {
  1: "Pendiente compra",
  2: "En compra",
  3: "Pedido recibido",
};

const MEDIOS = [
  { id: 1, label: "Efectivo" },
  { id: 2, label: "Debito" },
  { id: 3, label: "Credito" },
  { id: 4, label: "Transferencia" },
  { id: 5, label: "Nota de credito" },
];

function medioLabel(id) {
  return MEDIOS.find((m) => m.id === Number(id))?.label ?? `Medio ${id}`;
}

function estadoPedidoMeta(estadoPedidoSenia) {
  const estado = Number(estadoPedidoSenia);
  if (estado === 3) {
    return {
      label: "Pedido recibido",
      hint: "Contactar al cliente para coordinar entrega/pago.",
      style: { background: "#dcfce7", color: "#166534", border: "1px solid #86efac" },
    };
  }
  if (estado === 2) {
    return {
      label: "En compra",
      hint: "Compra en curso por Admin Compras.",
      style: { background: "#fef9c3", color: "#854d0e", border: "1px solid #fde047" },
    };
  }
  return {
    label: "Pendiente compra",
    hint: "Aun no fue tomado por Admin Compras.",
    style: { background: "#e2e8f0", color: "#334155", border: "1px solid #cbd5e1" },
  };
}

function telefonoWhatsAppLink(telefono) {
  if (!telefono) return null;
  const soloDigitos = String(telefono).replace(/\D/g, "");
  if (!soloDigitos) return null;
  return `https://wa.me/${soloDigitos}`;
}

export default function Reservas() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");
  const [estado, setEstado] = useState("");
  const [dni, setDni] = useState("");
  const [reservaSel, setReservaSel] = useState(null);
  const [pagos, setPagos] = useState([{ medioPago: 1, monto: "", referencia: "" }]);
  const [librosResumen, setLibrosResumen] = useState([]);

  async function cargar() {
    const [dataReservas, dataLibros] = await Promise.all([
      listarReservas({
        estado: estado ? Number(estado) : undefined,
        dni: dni?.trim() || undefined,
      }),
      resumenLibrosSenia(),
    ]);
    setItems(Array.isArray(dataReservas) ? dataReservas : []);
    setLibrosResumen(Array.isArray(dataLibros) ? dataLibros : []);
  }

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setLoading(true);
        setError("");
        await cargar();
      } catch (e) {
        if (!alive) return;
        setError(e?.response?.data?.error ?? e?.message ?? "No se pudo cargar reservas.");
      } finally {
        if (alive) setLoading(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, []);

  const totalPagos = useMemo(
    () => pagos.reduce((acc, p) => acc + Number(p.monto || 0), 0),
    [pagos],
  );
  const pendientesContacto = useMemo(
    () => (items ?? []).filter((r) => Number(r.estadoPedidoSenia) === 3 && Number(r.estado) === 2).length,
    [items],
  );

  const abrirDetalle = async (id) => {
    try {
      setError("");
      const data = await detalleReserva(id);
      setReservaSel(data);
      setPagos([{ medioPago: 1, monto: "", referencia: "" }]);
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo abrir la reserva.");
    }
  };

  const completar = async () => {
    if (!reservaSel) return;
    try {
      setError("");
      setMsg("");
      await completarPagoReserva(reservaSel.id, {
        pagos: pagos.map((p) => ({
          medioPago: Number(p.medioPago),
          monto: Number(p.monto || 0),
          referencia: p.referencia?.trim() || null,
        })),
      });
      setMsg(`Reserva #${reservaSel.id} completada.`);
      setReservaSel(null);
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo completar el pago.");
    }
  };

  const cancelar = async (id) => {
    const ok = await confirmAction({
      title: "Cancelar reserva",
      text: `Se cancelara la reserva #${id}.`,
      confirmText: "Cancelar reserva",
      cancelText: "Volver",
      icon: "warning",
    });
    if (!ok) return;

    const motivo = window.prompt("Motivo de cancelacion (opcional):", "") ?? "";
    try {
      setError("");
      setMsg("");
      await cancelarReserva(id, motivo);
      setMsg(`Reserva #${id} cancelada.`);
      if (reservaSel?.id === id) setReservaSel(null);
      await cargar();
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo cancelar la reserva.");
    }
  };

  return (
    <div className="aGrid2">
      <section className="aCard">
        <div className="aRow" style={{ justifyContent: "space-between", marginBottom: 10 }}>
          <div>
            <h1 className="aTitle">Señas presenciales</h1>
            <p className="aSub">Gestion de reservas y saldo pendiente.</p>
          </div>
          <Link className="aBtn" to="/admin/reservas/nueva">
            Generar seña
          </Link>
        </div>

        <div
          style={{
            marginBottom: 12,
            border: "1px solid #bfdbfe",
            background: "#eff6ff",
            color: "#1e3a8a",
            borderRadius: 12,
            padding: "10px 12px",
            fontWeight: 700,
          }}
        >
          Pedidos listos para contactar cliente: {pendientesContacto}
        </div>

        <div className="aRow" style={{ gap: 10, marginBottom: 12 }}>
          <select className="aInput" style={{ maxWidth: 220 }} value={estado} onChange={(e) => setEstado(e.target.value)}>
            <option value="">Todos los estados</option>
            <option value="2">Señada</option>
            <option value="3">Pagada</option>
            <option value="4">Vencida</option>
            <option value="5">Cancelada</option>
          </select>
          <input className="aInput" style={{ maxWidth: 220 }} placeholder="DNI cliente" value={dni} onChange={(e) => setDni(e.target.value)} />
          <button className="aBtnGhost" onClick={cargar}>Filtrar</button>
        </div>

        {loading ? <p>Cargando...</p> : null}
        {error ? <p style={{ color: "#b00020" }}>{error}</p> : null}
        {msg ? <p style={{ color: "#0a6a33" }}>{msg}</p> : null}

        <div className="aTableWrap">
          <table className="aTable" style={{ minWidth: 760 }}>
            <thead>
              <tr>
                <th>#</th>
                <th>Cliente</th>
                <th>Telefono</th>
                <th>Estado</th>
                <th>Pedido compras</th>
                <th className="aRight">Total</th>
                <th className="aRight">Seña</th>
                <th className="aRight">Saldo</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {(items ?? []).map((r) => (
                <tr key={r.id}>
                  <td>#{r.id}</td>
                  <td>{r.clienteNombre || "-"} ({r.clienteDni || "-"})</td>
                  <td>{r.clienteTelefono || "-"}</td>
                  <td>{ESTADOS[r.estado] ?? r.estado}</td>
                  <td>
                    {(() => {
                      const meta = estadoPedidoMeta(r.estadoPedidoSenia);
                      return (
                        <span
                          title={meta.hint}
                          style={{
                            display: "inline-flex",
                            alignItems: "center",
                            padding: "4px 10px",
                            borderRadius: 999,
                            fontWeight: 800,
                            fontSize: 12,
                            whiteSpace: "nowrap",
                            ...meta.style,
                          }}
                        >
                          {meta.label}
                        </span>
                      );
                    })()}
                  </td>
                  <td className="aRight">{money.format(r.total)}</td>
                  <td className="aRight">{money.format(r.montoSenia)}</td>
                  <td className="aRight">{money.format(r.saldoPendiente)}</td>
                  <td>
                    <div className="aRow" style={{ gap: 8 }}>
                      <button className="aBtnGhost" onClick={() => abrirDetalle(r.id)}>Ver</button>
                      {Number(r.estadoPedidoSenia) === 3 && Number(r.estado) === 2 && r.clienteTelefono ? (
                        <a
                          className="aBtnGhost"
                          href={telefonoWhatsAppLink(r.clienteTelefono)}
                          target="_blank"
                          rel="noreferrer"
                        >
                          Contactar
                        </a>
                      ) : null}
                      {Number(r.estado) === 2 ? (
                        <button className="aBtnGhost" onClick={() => cancelar(r.id)}>Cancelar</button>
                      ) : null}
                    </div>
                  </td>
                </tr>
              ))}
              {(items ?? []).length === 0 ? (
                <tr><td colSpan="9" style={{ textAlign: "center", color: "#666" }}>No hay reservas.</td></tr>
              ) : null}
            </tbody>
          </table>
        </div>

        <h3 style={{ margin: "18px 0 10px", fontWeight: 900 }}>Libros pedidos en señas</h3>
        <div className="aTableWrap">
          <table className="aTable" style={{ minWidth: 760 }}>
            <thead>
              <tr>
                <th>Libro</th>
                <th className="aRight">Total pedido</th>
                <th className="aRight">Reservas</th>
                <th className="aRight">Con stock</th>
                <th className="aRight">A conseguir</th>
              </tr>
            </thead>
            <tbody>
              {(librosResumen ?? []).map((r) => (
                <tr key={r.nombreLibro}>
                  <td>{r.nombreLibro}</td>
                  <td className="aRight">{r.cantidadTotal}</td>
                  <td className="aRight">{r.cantidadReservas}</td>
                  <td className="aRight">{r.cantidadConStock}</td>
                  <td className="aRight">{r.cantidadAConseguir}</td>
                </tr>
              ))}
              {(librosResumen ?? []).length === 0 ? (
                <tr><td colSpan="5" style={{ textAlign: "center", color: "#666" }}>No hay libros en señas activas.</td></tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>

      <aside className="aCard">
        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Detalle reserva</h3>
        {!reservaSel ? <p style={{ color: "#666" }}>Selecciona una reserva.</p> : (
          <>
            <p style={{ margin: 0 }}><b>Reserva #{reservaSel.id}</b></p>
            <p style={{ margin: "6px 0" }}>
              Cliente: {reservaSel.clienteNombre || "-"} ({reservaSel.clienteDni || "-"})
            </p>
            <p style={{ margin: "6px 0" }}>
              Telefono: <b>{reservaSel.clienteTelefono || "-"}</b>
            </p>
            <p style={{ margin: "6px 0" }}>
              Estado: <b>{ESTADOS[reservaSel.estado] ?? reservaSel.estado}</b>
            </p>
            <p style={{ margin: "6px 0" }}>
              Pedido compras: <b>{ESTADO_PEDIDO[reservaSel.estadoPedidoSenia] ?? "-"}</b>
            </p>
            {(() => {
              const meta = estadoPedidoMeta(reservaSel.estadoPedidoSenia);
              const wa = telefonoWhatsAppLink(reservaSel.clienteTelefono);
              return (
                <div
                  style={{
                    margin: "8px 0",
                    borderRadius: 10,
                    padding: "8px 10px",
                    fontWeight: 700,
                    ...meta.style,
                  }}
                >
                  {meta.hint}
                  {Number(reservaSel.estadoPedidoSenia) === 3 && wa ? (
                    <a
                      href={wa}
                      target="_blank"
                      rel="noreferrer"
                      style={{ marginLeft: 10, fontWeight: 900, color: "#0f172a" }}
                    >
                      Contactar ahora
                    </a>
                  ) : null}
                </div>
              );
            })()}
            <p style={{ margin: "6px 0" }}>Saldo pendiente: <b>{money.format(reservaSel.saldoPendiente)}</b></p>

            <div className="aTableWrap">
              <table className="aTable" style={{ minWidth: 400 }}>
                <thead>
                  <tr>
                    <th>Libro</th>
                    <th className="aRight">Cant.</th>
                    <th className="aRight">Sub</th>
                  </tr>
                </thead>
                <tbody>
                  {(reservaSel.items ?? []).map((it) => (
                    <tr key={it.productoId}>
                      <td>{it.nombreProducto}</td>
                      <td className="aRight">{it.cantidad}</td>
                      <td className="aRight">{money.format(it.subtotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {Number(reservaSel.estado) === 2 && Number(reservaSel.saldoPendiente || 0) > 0 ? (
              <>
                <h4 style={{ margin: "14px 0 8px" }}>Completar pago</h4>
                <div style={{ display: "grid", gap: 8 }}>
                  {pagos.map((p, idx) => (
                    <div key={`${idx}-${p.medioPago}`} className="aRow" style={{ gap: 8 }}>
                      <select className="aInput" value={p.medioPago} onChange={(e) => setPagos((arr) => arr.map((x, i) => i === idx ? { ...x, medioPago: Number(e.target.value) } : x))}>
                        {MEDIOS.map((m) => <option key={m.id} value={m.id}>{m.label}</option>)}
                      </select>
                      <input className="aInput" type="number" min="0" step="0.01" value={p.monto} onChange={(e) => setPagos((arr) => arr.map((x, i) => i === idx ? { ...x, monto: e.target.value } : x))} placeholder="Monto" />
                      <input className="aInput" value={p.referencia || ""} onChange={(e) => setPagos((arr) => arr.map((x, i) => i === idx ? { ...x, referencia: e.target.value } : x))} placeholder={`Ref ${medioLabel(p.medioPago)}`} />
                    </div>
                  ))}
                </div>
                <div className="aRow" style={{ gap: 8, marginTop: 8 }}>
                  <button className="aBtnGhost" onClick={() => setPagos((arr) => [...arr, { medioPago: 1, monto: "", referencia: "" }])}>+ Pago</button>
                  {pagos.length > 1 ? <button className="aBtnGhost" onClick={() => setPagos((arr) => arr.slice(0, -1))}>Quitar</button> : null}
                </div>
                <p style={{ marginTop: 8 }}>Total cargado: <b>{money.format(totalPagos)}</b></p>
                <button className="aBtn" onClick={completar}>Completar pago y generar venta</button>
              </>
            ) : null}
          </>
        )}
      </aside>
    </div>
  );
}









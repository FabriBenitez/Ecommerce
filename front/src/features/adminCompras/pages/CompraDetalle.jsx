import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  obtenerCompra,
  confirmarCompra,
  registrarFacturaProveedor,
  listarProductos,
} from "../api/adminCompras.api";
import CompraItemsTable from "../components/CompraItemsTable";
import "../styles/ComprasCommon.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function estadoCompraLabel(e) {
  // Ajustá si tu enum en backend es distinto:
  // 1 Pendiente, 2 Confirmada, 3 Cancelada (ejemplo)
  const map = { 1: "Pendiente", 2: "Confirmada", 3: "Cancelada" };
  return map[e] ?? String(e ?? "");
}

export default function CompraDetalle() {
  const { id } = useParams();

  const [compra, setCompra] = useState(null);
  const [productos, setProductos] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  // Factura
  const [factNumero, setFactNumero] = useState("");
  const [factFecha, setFactFecha] = useState(() => new Date().toISOString().slice(0, 10));
  const [factMonto, setFactMonto] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);

        const [c, prods] = await Promise.all([obtenerCompra(id), listarProductos()]);

        if (!alive) return;
        setCompra(c);
        setProductos(Array.isArray(prods) ? prods : []);
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el detalle de la compra.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
  }, [id]);

  const productosById = useMemo(() => {
    const map = {};
    (productos ?? []).forEach((p) => { map[p.id] = p; });
    return map;
  }, [productos]);

  const items = useMemo(() => {
    // ⚠️ Esto depende de cómo devuelva tu backend el detalle:
    // esperable: compra.items = [{ productoId, cantidad, costoUnitario }]
    return compra?.items ?? compra?.detalles ?? [];
  }, [compra]);

  const proveedorLabel = useMemo(() => {
    const p = compra?.proveedor;
    if (!p) return compra?.proveedorNombre ?? "-";
    if (typeof p === "string") return p;
    if (typeof p === "object") {
      const razon = p.razonSocial ?? p.nombre ?? "";
      const cuit = p.cuit ? ` (${p.cuit})` : "";
      const fallbackId = p.proveedorId ? `Proveedor #${p.proveedorId}` : "-";
      return razon ? `${razon}${cuit}` : fallbackId;
    }
    return "-";
  }, [compra]);

  const total = useMemo(() => {
    // Si backend ya trae compra.total, lo usamos; si no, lo calculamos.
    if (typeof compra?.total === "number") return compra.total;
    return (items ?? []).reduce((acc, it) => acc + (it.cantidad * it.costoUnitario), 0);
  }, [compra, items]);

  const isConfirmada = useMemo(() => {
    // Ajustá según tu enum EstadoCompra real
    return compra?.estadoCompra === 2 || compra?.estadoCompra === "Confirmada";
  }, [compra]);

  async function onConfirmar() {
    if (!window.confirm("¿Confirmar compra? Esto debería aumentar stock y no ser editable.")) return;

    setBusy(true);
    try {
      await confirmarCompra(id);
      const updated = await obtenerCompra(id);
      setCompra(updated);
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo confirmar la compra.");
    } finally {
      setBusy(false);
    }
  }

  async function onRegistrarFactura(e) {
    e.preventDefault();

    const monto = Number(factMonto);
    if (!factNumero.trim()) return alert("Ingresá número de factura.");
    if (!factFecha) return alert("Ingresá fecha de factura.");
    if (!Number.isFinite(monto) || monto <= 0) return alert("Monto inválido.");

    setBusy(true);
    try {
      await registrarFacturaProveedor(id, {
        numero: factNumero.trim(),
        fecha: factFecha,
        monto,
      });

      // recargar detalle
      const updated = await obtenerCompra(id);
      setCompra(updated);

      // limpiar
      setFactNumero("");
      setFactMonto("");
    } catch (e2) {
      setError(e2?.response?.data?.error ?? e2?.message ?? "No se pudo registrar la factura.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <div className="breadRow">
            <Link className="backLink" to="/compras/historial">← Historial</Link>
            <span className="pill">Compra #{id}</span>
          </div>

          <h1 className="ctitle">Detalle de compra</h1>
          <p className="cmuted">
            Estado: <b>{estadoCompraLabel(compra?.estadoCompra)}</b> · Total: <b>{money.format(total)}</b>
          </p>
        </div>

        <div className="cactions">
          <button className="btn btn--primary" disabled={busy || loading || isConfirmada} onClick={onConfirmar}>
            {isConfirmada ? "Confirmada" : (busy ? "Procesando…" : "Confirmar compra")}
          </button>
        </div>
      </header>

      {loading ? <section className="ccard ccard__pad">Cargando…</section> : null}
      {error ? <section className="ccard ccard__pad cerror">{error}</section> : null}

      {!loading && !error && compra ? (
        <>
          <section className="ccard ccard__pad">
            <div className="grid3">
              <div className="kv">
                <span className="k">Fecha</span>
                <span className="v">{compra.fecha ? new Date(compra.fecha).toLocaleString("es-AR") : "-"}</span>
              </div>
              <div className="kv">
                <span className="k">Proveedor</span>
                <span className="v">{proveedorLabel}</span>
              </div>
              <div className="kv">
                <span className="k">Total</span>
                <span className="v strong">{money.format(total)}</span>
              </div>
            </div>
          </section>

          <CompraItemsTable
            items={items}
            productosById={productosById}
            onRemove={() => {}}
            onChange={() => {}}
          />

          {/* Factura proveedor */}
          <section className="ccard ccard__pad">
            <div className="rowHead">
              <h2 className="ctitle2">Factura proveedor</h2>
              <span className="cmuted">Registro interno (no fiscal)</span>
            </div>

            <form className="grid3" onSubmit={onRegistrarFactura}>
              <label className="field">
                <span>Número</span>
                <input className="cinput" value={factNumero} onChange={(e) => setFactNumero(e.target.value)} />
              </label>

              <label className="field">
                <span>Fecha</span>
                <input className="cinput" type="date" value={factFecha} onChange={(e) => setFactFecha(e.target.value)} />
              </label>

              <label className="field">
                <span>Monto</span>
                <input className="cinput" type="number" min="0" step="0.01" value={factMonto} onChange={(e) => setFactMonto(e.target.value)} />
              </label>

              <div className="field">
                <span>&nbsp;</span>
                <button className="btn btn--primary" disabled={busy}>
                  {busy ? "Guardando…" : "Registrar factura"}
                </button>
              </div>
            </form>

            {/* Si tu backend devuelve factura(s), mostramos algo simple */}
            {compra.factura ? (
              <div className="miniCard">
                <div className="kv">
                  <span className="k">Número</span>
                  <span className="v">{compra.factura.numero}</span>
                </div>
                <div className="kv">
                  <span className="k">Fecha</span>
                  <span className="v">{new Date(compra.factura.fecha).toLocaleDateString("es-AR")}</span>
                </div>
                <div className="kv">
                  <span className="k">Monto</span>
                  <span className="v strong">{money.format(compra.factura.monto)}</span>
                </div>
              </div>
            ) : null}
          </section>
        </>
      ) : null}
    </main>
  );
}

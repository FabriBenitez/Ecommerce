import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { confirmAction } from "@/shared/ui/sweetAlert";
import productosApi from "@/features/productos/api/productos.api";
import { crearReservaPresencial, notaCreditoPorDni } from "../api/adminVentas.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });
const MAX_DNI = 20;
const MAX_NOMBRE = 200;
const MAX_TELEFONO = 50;

const MEDIOS = [
  { id: 1, label: "Efectivo" },
  { id: 2, label: "Debito" },
  { id: 3, label: "Credito" },
  { id: 4, label: "Transferencia" },
  { id: 5, label: "Nota de credito" },
];

function normalizeImageUrl(raw) {
  let value = (raw ?? "").trim();
  if (!value) return "";
  value = value.replace(/^"+|"+$/g, "");
  if (/^data:image\//i.test(value)) {
    const headers = [...value.matchAll(/data:image\/[a-z0-9.+-]+;base64,/ig)];
    if (headers.length) {
      const lastHeader = headers[headers.length - 1];
      const prefix = lastHeader[0];
      let payload = value.slice((lastHeader.index ?? 0) + prefix.length);
      payload = payload.replace(/["'\s]/g, "");
      payload = payload.replace(/[^A-Za-z0-9+/=]/g, "");
      if (!payload) return "";
      const mod = payload.length % 4;
      if (mod !== 0) payload += "=".repeat(4 - mod);
      value = `${prefix}${payload}`;
    }
  }
  return value;
}

function resolveImageSrc(imagenUrl) {
  const normalized = normalizeImageUrl(imagenUrl);
  if (!normalized) return null;
  if (
    normalized.startsWith("http://")
    || normalized.startsWith("https://")
    || normalized.startsWith("data:")
    || normalized.startsWith("blob:")
  ) return normalized;

  const apiBase = (import.meta.env.VITE_API_BASE_URL || "https://localhost:7248").replace(/\/$/, "");
  return `${apiBase}${normalized.startsWith("/") ? "" : "/"}${normalized}`;
}

function getPromoTag(p) {
  if (!p?.tienePromocionActiva) return "Sin promocion";
  if (p?.porcentajeDescuento) return `Promo ${Number(p.porcentajeDescuento).toFixed(0)}%`;
  if (p?.montoDescuento) return `Promo ${money.format(Number(p.montoDescuento || 0))}`;
  return "Con promocion";
}

export default function SeniaGenerar() {
  const navigate = useNavigate();

  const [catalogo, setCatalogo] = useState([]);
  const [loadingCatalogo, setLoadingCatalogo] = useState(true);
  const [errorCatalogo, setErrorCatalogo] = useState("");

  const [dni, setDni] = useState("");
  const [nombre, setNombre] = useState("");
  const [telefono, setTelefono] = useState("");
  const [q, setQ] = useState("");
  const [nc, setNc] = useState(null);

  const [pagos, setPagos] = useState([]);
  const [cart, setCart] = useState([]);
  const [manualNombre, setManualNombre] = useState("");
  const [manualPrecio, setManualPrecio] = useState("");
  const [manualCant, setManualCant] = useState("1");

  const [msg, setMsg] = useState("");
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setLoadingCatalogo(true);
        setErrorCatalogo("");
        const { data } = await productosApi.obtenerCatalogo();
        if (!alive) return;
        setCatalogo(Array.isArray(data) ? data : []);
      } catch {
        if (!alive) return;
        setErrorCatalogo("No se pudo cargar el catalogo.");
      } finally {
        if (alive) setLoadingCatalogo(false);
      }
    })();
    return () => {
      alive = false;
    };
  }, []);

  const total = useMemo(() => cart.reduce((acc, i) => acc + i.precio * i.cantidad, 0), [cart]);
  const totalPagos = useMemo(() => pagos.reduce((acc, p) => acc + Number(p.monto || 0), 0), [pagos]);
  const saldo = useMemo(() => total - totalPagos, [total, totalPagos]);

  const catalogoFiltrado = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return catalogo;
    return (catalogo ?? []).filter((p) => {
      const nombreProd = String(p.nombre ?? "").toLowerCase();
      return nombreProd.includes(s) || String(p.id ?? "").includes(s);
    });
  }, [catalogo, q]);

  const agregarProducto = (p, esAConseguir) => {
    const stockDisponible = Number(p.stockDisponible ?? p.stock ?? 0);
    if (!esAConseguir && stockDisponible <= 0) {
      setMsg("Sin stock para agregar como disponible.");
      return;
    }
    setMsg("");
    setCart((prev) => {
      const idx = prev.findIndex(
        (x) => !x.esManual && x.productoId === p.id && x.esAConseguir === esAConseguir,
      );
      if (idx === -1) {
        return [
          ...prev,
          {
            lineId: `${p.id}-${esAConseguir ? "ac" : "stk"}`,
            productoId: p.id,
            nombre: p.nombre,
            precio: Number(p.precio ?? 0),
            cantidad: 1,
            esAConseguir,
            esManual: false,
          },
        ];
      }
      const next = [...prev];
      next[idx] = { ...next[idx], cantidad: next[idx].cantidad + 1 };
      return next;
    });
  };

  const agregarManualAConseguir = () => {
    const cant = Math.max(1, Number(manualCant || 1));
    const precio = Number(manualPrecio || 0);
    if (!manualNombre.trim()) return setMsg("Ingresa el nombre del libro a conseguir.");
    if (!(precio > 0)) return setMsg("Ingresa un precio pactado valido.");

    setMsg("");
    setCart((prev) => [
      ...prev,
      {
        lineId: `manual-${Date.now()}-${Math.random()}`,
        productoId: null,
        nombre: manualNombre.trim(),
        precio,
        cantidad: cant,
        esAConseguir: true,
        esManual: true,
      },
    ]);

    setManualNombre("");
    setManualPrecio("");
    setManualCant("1");
  };

  const cambiarCant = (lineId, delta) => {
    setCart((prev) =>
      prev
        .map((i) => (i.lineId === lineId ? { ...i, cantidad: Math.max(1, i.cantidad + delta) } : i))
        .filter((i) => i.cantidad > 0),
    );
  };

  const quitarItem = (lineId) => {
    setCart((prev) => prev.filter((i) => i.lineId !== lineId));
  };

  const toggleMedio = (medioPago) => {
    setPagos((prev) => {
      const exists = prev.some((p) => p.medioPago === medioPago);
      if (exists) return prev.filter((p) => p.medioPago !== medioPago);
      return [...prev, { medioPago, monto: "", referencia: "" }];
    });
  };

  const setPagoPorMedio = (medioPago, patch) => {
    setPagos((prev) => prev.map((p) => (p.medioPago === medioPago ? { ...p, ...patch } : p)));
  };

  const verificarNC = async () => {
    const dniClean = dni.replace(/\D/g, "").slice(0, MAX_DNI);
    if (!dniClean) return setMsg("Ingresa DNI para verificar nota de credito.");
    const data = await notaCreditoPorDni(dniClean);
    setNc(data);
  };

  const registrarSenia = async () => {
    setMsg("");

    const dniClean = dni.replace(/\D/g, "").slice(0, MAX_DNI);
    const nombreClean = nombre.trim().slice(0, MAX_NOMBRE);
    const telefonoClean = telefono.trim().slice(0, MAX_TELEFONO);

    if (cart.length === 0) return setMsg("Agrega al menos un libro a la seña.");
    if (!dniClean) return setMsg("El DNI es obligatorio.");
    if (!nombreClean) return setMsg("El nombre es obligatorio.");
    if (!telefonoClean) return setMsg("El telefono es obligatorio para contactar al cliente.");
    if (pagos.length === 0) return setMsg("Selecciona al menos un medio de pago.");
    if (totalPagos <= 0) return setMsg("La seña debe ser mayor a cero.");
    if (totalPagos >= total) return setMsg("La seña debe ser menor al total.");

    const ncUsada = pagos
      .filter((p) => Number(p.medioPago) === 5)
      .reduce((acc, p) => acc + Number(p.monto || 0), 0);
    if (ncUsada > Number(nc?.saldoDisponible ?? 0)) return setMsg("Nota de credito insuficiente.");

    const ok = await confirmAction({
      title: "Registrar seña",
      text: "Se registrara la seña y, si aplica, quedara pendiente para compras.",
      confirmText: "Registrar",
      cancelText: "Cancelar",
      icon: "warning",
    });
    if (!ok) return;

    try {
      setSaving(true);
      const payload = {
        clienteDni: dniClean,
        clienteNombre: nombreClean,
        clienteTelefono: telefonoClean,
        observaciones: "Seña generada desde modulo Señas.",
        items: cart.map((i) => ({
          productoId: i.productoId,
          nombreManual: i.esManual ? i.nombre : null,
          precioUnitarioManual: i.esManual ? Number(i.precio) : null,
          cantidad: Number(i.cantidad),
          esAConseguir: Boolean(i.esAConseguir),
        })),
        pagosAnticipo: pagos.map((p) => ({
          medioPago: Number(p.medioPago),
          monto: Number(p.monto || 0),
          referencia: p.referencia?.trim() || null,
        })),
      };

      const resp = await crearReservaPresencial(payload);
      navigate("/admin/reservas", { state: { reservaIdDestacada: resp?.reservaId } });
    } catch (e) {
      const apiError = e?.response?.data;
      setMsg(typeof apiError === "string" ? apiError : apiError?.error ?? "No se pudo registrar la seña.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="aGrid2">
      <section className="aCard">
        <div className="aRow" style={{ justifyContent: "space-between", marginBottom: 10 }}>
          <div>
            <h1 className="aTitle">Generar seña</h1>
            <p className="aSub">Busca libros, agrega items a conseguir y registra anticipo.</p>
          </div>
          <Link className="aBtnGhost" to="/admin/reservas">Volver a señas</Link>
        </div>

        <div className="aCard" style={{ boxShadow: "none", marginBottom: 12 }}>
          <h3 style={{ margin: "0 0 8px", fontWeight: 900 }}>Libro no registrado (A conseguir)</h3>
          <div className="aRow" style={{ gap: 8 }}>
            <input className="aInput" placeholder="Nombre del libro" value={manualNombre} onChange={(e) => setManualNombre(e.target.value)} />
            <input className="aInput" type="number" min="0" step="0.01" placeholder="Precio pactado" value={manualPrecio} onChange={(e) => setManualPrecio(e.target.value)} />
            <input className="aInput" type="number" min="1" step="1" placeholder="Cant" value={manualCant} onChange={(e) => setManualCant(e.target.value)} style={{ maxWidth: 110 }} />
            <button className="aBtnGhost" onClick={agregarManualAConseguir}>Agregar a conseguir</button>
          </div>
        </div>

        <input
          className="aInput"
          style={{ marginBottom: 12 }}
          placeholder="Buscar libro por nombre o ID..."
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />

        {msg ? <p style={{ color: "#b00020", marginTop: 0 }}>{msg}</p> : null}
        {loadingCatalogo ? <p>Cargando catalogo...</p> : null}
        {errorCatalogo ? <p style={{ color: "#b00020" }}>{errorCatalogo}</p> : null}

        <div className="posCatalogGrid">
          {catalogoFiltrado.map((p) => {
            const imageSrc = resolveImageSrc(p.imagenUrl);
            return (
              <div key={p.id} className="aCard posCard" style={{ boxShadow: "none" }}>
                <div className="posCard__media">
                  {imageSrc ? <img src={imageSrc} alt={p.nombre} loading="lazy" className="posCard__img" /> : <div className="posCard__imgFallback">Sin imagen</div>}
                </div>
                <div className="posCard__title">{p.nombre}</div>
                <div className={`posCard__meta ${p.tienePromocionActiva ? "isPromo" : ""}`}>{getPromoTag(p)}</div>
                <div className="posCard__meta posCard__meta--stock">Disponible: <b>{p.stockDisponible ?? p.stock}</b></div>
                <div className="posCard__footer" style={{ flexDirection: "column", alignItems: "stretch", gap: 8 }}>
                  <div className="posCard__price">{money.format(Number(p.precio ?? 0))}</div>
                  <div className="aRow" style={{ gap: 8 }}>
                    <button className="aBtn posCard__btn" onClick={() => agregarProducto(p, false)} disabled={Number(p.stockDisponible ?? p.stock ?? 0) <= 0}>Agregar</button>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      <aside className="aCard">
        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Cliente</h3>
        <div style={{ display: "grid", gap: 10 }}>
          <input className="aInput" placeholder="DNI (obligatorio)" value={dni} onChange={(e) => setDni(e.target.value.replace(/\D/g, "").slice(0, MAX_DNI))} maxLength={MAX_DNI} />
          <input className="aInput" placeholder="Nombre (obligatorio)" value={nombre} onChange={(e) => setNombre(e.target.value.slice(0, MAX_NOMBRE))} maxLength={MAX_NOMBRE} />
          <input className="aInput" placeholder="Telefono (obligatorio)" value={telefono} onChange={(e) => setTelefono(e.target.value.replace(/[^\d+\-()\s]/g, "").slice(0, MAX_TELEFONO))} maxLength={MAX_TELEFONO} />
          <button className="aBtnGhost" onClick={verificarNC}>Verificar nota de credito</button>
          {nc ? <div style={{ fontSize: 13, color: "#0a6a33" }}>NC: <b>{nc.saldoDisponible}</b></div> : null}
        </div>

        <hr style={{ border: 0, borderTop: "1px solid #eee", margin: "14px 0" }} />

        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Carrito de seña</h3>
        {cart.length === 0 ? <p style={{ color: "#666" }}>Vacio</p> : null}

        <div style={{ display: "grid", gap: 10 }}>
          {cart.map((i) => (
            <div key={i.lineId} className="aCard" style={{ boxShadow: "none" }}>
              <div style={{ fontWeight: 900 }}>{i.nombre}</div>
              <div style={{ fontSize: 12, color: i.esAConseguir ? "#b45309" : "#0a6a33", marginTop: 4 }}>
                {i.esAConseguir ? "A conseguir" : "Con stock"}
              </div>
              <div className="aRow" style={{ justifyContent: "space-between", marginTop: 8 }}>
                <div className="aRow">
                  <button className="aBtnGhost" onClick={() => cambiarCant(i.lineId, -1)}>-</button>
                  <div style={{ width: 30, textAlign: "center", fontWeight: 900 }}>{i.cantidad}</div>
                  <button className="aBtnGhost" onClick={() => cambiarCant(i.lineId, +1)}>+</button>
                  <button className="aBtnGhost" onClick={() => quitarItem(i.lineId)}>Quitar</button>
                </div>
                <div style={{ fontWeight: 900 }}>{money.format(i.precio * i.cantidad)}</div>
              </div>
            </div>
          ))}
        </div>

        <hr style={{ border: 0, borderTop: "1px solid #eee", margin: "14px 0" }} />
        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Pagos de seña</h3>

        <div className="ppMetodoGrid" style={{ marginBottom: 8 }}>
          {MEDIOS.map((m) => {
            const active = pagos.some((p) => p.medioPago === m.id);
            return (
              <button key={m.id} type="button" className={`ppMetodoBtn ${active ? "isActive" : ""}`} onClick={() => toggleMedio(m.id)}>
                <span className="ppMetodoLabel">{m.label}</span>
              </button>
            );
          })}
        </div>

        <div className="ppRows">
          {pagos.map((p) => (
            <div key={p.medioPago} className="ppRow">
              <div className="ppRowHead"><span className="ppRowName">{MEDIOS.find((x) => x.id === p.medioPago)?.label}</span></div>
              <div className="ppRowFields">
                <label className="ppField">
                  <span>Monto</span>
                  <input className="aInput" type="number" min="0" step="0.01" value={p.monto} onChange={(e) => setPagoPorMedio(p.medioPago, { monto: e.target.value })} />
                </label>
                {p.medioPago !== 1 ? (
                  <label className="ppField">
                    <span>Referencia</span>
                    <input className="aInput" value={p.referencia ?? ""} onChange={(e) => setPagoPorMedio(p.medioPago, { referencia: e.target.value })} />
                  </label>
                ) : null}
              </div>
            </div>
          ))}
        </div>

        <div className="ppSummary" style={{ marginTop: 12 }}>
          <div className="ppLine"><span>Total</span><strong>{money.format(total)}</strong></div>
          <div className="ppLine"><span>Seña cobrada</span><strong>{money.format(totalPagos)}</strong></div>
          <div className={`ppLine ${saldo > 0 ? "err" : "ok"}`}><span>Saldo pendiente</span><strong>{money.format(saldo)}</strong></div>
        </div>

        <button className="aBtn" style={{ width: "100%", marginTop: 12 }} onClick={registrarSenia} disabled={saving}>
          {saving ? "Guardando..." : "Registrar seña"}
        </button>
      </aside>
    </div>
  );
}

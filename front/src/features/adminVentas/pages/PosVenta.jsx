import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { notaCreditoPorDni } from "../api/adminVentas.api";
import productosApi from "@/features/productos/api/productos.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });
const MAX_DNI = 20;
const MAX_NOMBRE = 200;
const MAX_TELEFONO = 50;

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

function getIsbn(p) {
  const directo =
    p?.isbn
    ?? p?.ISBN
    ?? p?.codigoIsbn
    ?? p?.codigoISBN
    ?? p?.isbn13
    ?? p?.ISBN13
    ?? p?.codigoBarras
    ?? p?.ean;

  if (directo != null && String(directo).trim()) return String(directo).trim();

  const desc = String(p?.descripcion ?? "");
  const match = desc.match(/(?:isbn|isbn-13|isbn13|ean)\s*[:#-]?\s*([0-9xX\- ]{9,20})/i);
  if (match?.[1]) return match[1].replace(/\s+/g, "").trim();

  return "-";
}

export default function PosVenta() {
  const navigate = useNavigate();
  const { state } = useLocation();
  const draftVenta = state?.draftVenta;

  const [catalogo, setCatalogo] = useState([]);
  const [loadingCatalogo, setLoadingCatalogo] = useState(true);
  const [errorCatalogo, setErrorCatalogo] = useState("");

  const [dni, setDni] = useState(draftVenta?.clienteDni ?? "");
  const [nombre, setNombre] = useState(draftVenta?.clienteNombre ?? "");
  const [telefono, setTelefono] = useState(draftVenta?.clienteTelefono ?? "");
  const [q, setQ] = useState("");
  const [nc, setNc] = useState(
    draftVenta?.notaCredito != null ? { saldoDisponible: Number(draftVenta.notaCredito || 0) } : null
  );
  const [msg, setMsg] = useState("");
  const [cart, setCart] = useState(
    Array.isArray(draftVenta?.items)
      ? draftVenta.items.map((i) => ({
        productoId: i.productoId,
        nombre: i.nombre,
        precio: Number(i.precio ?? 0),
        cantidad: Number(i.cantidad ?? 1),
      }))
      : []
  );

  useEffect(() => {
    let alive = true;

    const cargarCatalogo = async () => {
      try {
        setLoadingCatalogo(true);
        setErrorCatalogo("");
        const { data } = await productosApi.obtenerCatalogo();
        if (!alive) return;
        setCatalogo(Array.isArray(data) ? data : []);
      } catch {
        if (!alive) return;
        setErrorCatalogo("No se pudo cargar el catalogo real.");
      } finally {
        if (alive) setLoadingCatalogo(false);
      }
    };

    cargarCatalogo();
    return () => {
      alive = false;
    };
  }, []);

  const total = useMemo(() => cart.reduce((acc, i) => acc + i.precio * i.cantidad, 0), [cart]);
  const catalogoFiltrado = useMemo(() => {
    const s = q.trim().toLowerCase();
    if (!s) return catalogo;

    return (catalogo ?? []).filter((p) => {
      const nombreProd = String(p.nombre ?? "").toLowerCase();
      return nombreProd.includes(s) || String(p.id ?? "").includes(s);
    });
  }, [catalogo, q]);

  const agregar = (p) => {
    const stockDisponible = Number(p.stockDisponible ?? p.stock ?? 0);
    if (stockDisponible <= 0) {
      setMsg("Sin stock");
      return;
    }
    setMsg("");
    setCart((prev) => {
      const idx = prev.findIndex((x) => x.productoId === p.id);
      if (idx === -1) return [...prev, { productoId: p.id, nombre: p.nombre, precio: p.precio, cantidad: 1 }];
      const next = [...prev];
      next[idx] = { ...next[idx], cantidad: next[idx].cantidad + 1 };
      return next;
    });
  };

  const cambiarCant = (productoId, delta) => {
    setCart((prev) =>
      prev
        .map((i) => (i.productoId === productoId ? { ...i, cantidad: Math.max(1, i.cantidad + delta) } : i))
        .filter((i) => i.cantidad > 0)
    );
  };

  const quitarItem = (productoId) => {
    setCart((prev) => prev.filter((i) => i.productoId !== productoId));
  };

  const verificarNC = async () => {
    setMsg("");
    const dniClean = dni.replace(/\D/g, "").slice(0, MAX_DNI);
    if (!dniClean) {
      setMsg("Ingresa DNI");
      return;
    }
    const data = await notaCreditoPorDni(dniClean);
    setNc(data);
  };

  const continuarPago = () => {
    if (cart.length === 0) {
      setMsg("Agrega al menos 1 producto");
      return;
    }
    const dniClean = dni.replace(/\D/g, "").slice(0, MAX_DNI);
    const nombreClean = nombre.trim().slice(0, MAX_NOMBRE);
    const telefonoClean = telefono.trim().slice(0, MAX_TELEFONO);
    if (!dniClean) {
      setMsg("El DNI es obligatorio.");
      return;
    }
    if (!nombreClean) {
      setMsg("El nombre es obligatorio.");
      return;
    }
    if (!telefonoClean) {
      setMsg("El telefono es obligatorio.");
      return;
    }
    navigate("/admin/pos/pago", {
      state: {
        clienteDni: dniClean,
        clienteNombre: nombreClean,
        clienteTelefono: telefonoClean,
        items: cart.map((i) => ({ productoId: i.productoId, cantidad: i.cantidad, nombre: i.nombre, precio: i.precio })),
        total,
        notaCredito: nc?.saldoDisponible ?? 0,
      },
    });
  };

  return (
    <div className="aGrid2">
      <section className="aCard">
        <h1 className="aTitle">Mostrador</h1>
        <p className="aSub">Busca y agrega libros al carrito (POS).</p>
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
        {!loadingCatalogo && !errorCatalogo && catalogoFiltrado.length === 0 ? (
          <p style={{ color: "#666" }}>No hay libros que coincidan con la busqueda.</p>
        ) : null}

        <div className="posCatalogGrid">
          {catalogoFiltrado.map((p) => {
            const imageSrc = resolveImageSrc(p.imagenUrl);
            return (
              <div key={p.id} className="aCard posCard" style={{ boxShadow: "none" }}>
                <div className="posCard__media">
                  {imageSrc ? (
                    <img
                      src={imageSrc}
                      alt={p.nombre}
                      loading="lazy"
                      className="posCard__img"
                    />
                  ) : (
                    <div className="posCard__imgFallback">
                      Sin imagen
                    </div>
                  )}
                </div>
                <div className="posCard__title">{p.nombre}</div>
                <div className="posCard__meta">
                  ISBN: <b>{getIsbn(p)}</b>
                </div>
                <div className={`posCard__meta ${p.tienePromocionActiva ? "isPromo" : ""}`}>
                  {getPromoTag(p)}
                </div>
                <div className="posCard__meta posCard__meta--stock">
                  Disponible: <b>{p.stockDisponible ?? p.stock}</b>
                </div>
                <div className="posCard__footer">
                  <div className="posCard__price">
                    {p.tienePromocionActiva && p.precioFinal != null && Number(p.precioFinal) < Number(p.precio)
                      ? money.format(Number(p.precioFinal))
                      : money.format(Number(p.precio ?? 0))}
                  </div>
                  <button className="aBtn posCard__btn" onClick={() => agregar(p)} disabled={Number(p.stockDisponible ?? p.stock ?? 0) <= 0}>
                    Agregar
                  </button>
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
          {nc ? (
            <div style={{ fontSize: 13, color: "#0a6a33" }}>
              NC: <b>{nc.saldoDisponible}</b>
            </div>
          ) : null}
        </div>

        <hr style={{ border: 0, borderTop: "1px solid #eee", margin: "14px 0" }} />

        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Carrito</h3>
        {cart.length === 0 ? <p style={{ color: "#666" }}>Vacio</p> : null}

        <div style={{ display: "grid", gap: 10 }}>
          {cart.map((i) => (
            <div key={i.productoId} className="aCard" style={{ boxShadow: "none" }}>
              <div style={{ fontWeight: 900 }}>{i.nombre}</div>
              <div className="aRow" style={{ justifyContent: "space-between", marginTop: 8 }}>
                <div className="aRow">
                  <button className="aBtnGhost" onClick={() => cambiarCant(i.productoId, -1)}>-</button>
                  <div style={{ width: 30, textAlign: "center", fontWeight: 900 }}>{i.cantidad}</div>
                  <button className="aBtnGhost" onClick={() => cambiarCant(i.productoId, +1)}>+</button>
                  <button className="aBtnGhost" onClick={() => quitarItem(i.productoId)}>Quitar</button>
                </div>
                <div style={{ fontWeight: 900 }}>
                  {new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" }).format(i.precio * i.cantidad)}
                </div>
              </div>
            </div>
          ))}
        </div>

        <div className="aRow" style={{ justifyContent: "space-between", marginTop: 14 }}>
          <div style={{ fontWeight: 900 }}>TOTAL</div>
          <div style={{ fontWeight: 900 }}>
            {new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" }).format(total)}
          </div>
        </div>

        <button className="aBtn" style={{ width: "100%", marginTop: 12 }} onClick={continuarPago}>
          Continuar a pago
        </button>
      </aside>
    </div>
  );
}

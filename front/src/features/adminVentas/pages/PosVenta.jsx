import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { notaCreditoPorDni } from "../api/adminVentas.api";
import productosApi from "@/features/productos/api/productos.api";

export default function PosVenta() {
  const navigate = useNavigate();

  const [catalogo, setCatalogo] = useState([]);
  const [loadingCatalogo, setLoadingCatalogo] = useState(true);
  const [errorCatalogo, setErrorCatalogo] = useState("");

  const [dni, setDni] = useState("");
  const [nombre, setNombre] = useState("");
  const [nc, setNc] = useState(null);
  const [msg, setMsg] = useState("");
  const [cart, setCart] = useState([]);

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

  const agregar = (p) => {
    if (p.stock <= 0) {
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

  const verificarNC = async () => {
    setMsg("");
    if (!dni.trim()) {
      setMsg("Ingresa DNI");
      return;
    }
    const data = await notaCreditoPorDni(dni.trim());
    setNc(data);
  };

  const continuarPago = () => {
    if (cart.length === 0) {
      setMsg("Agrega al menos 1 producto");
      return;
    }
    navigate("/admin/pos/pago", {
      state: {
        clienteDni: dni.trim() || null,
        clienteNombre: nombre.trim() || null,
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

        {msg ? <p style={{ color: "#b00020", marginTop: 0 }}>{msg}</p> : null}

        {loadingCatalogo ? <p>Cargando catalogo...</p> : null}
        {errorCatalogo ? <p style={{ color: "#b00020" }}>{errorCatalogo}</p> : null}

        <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(0, 1fr))", gap: 12 }}>
          {catalogo.map((p) => (
            <div key={p.id} className="aCard" style={{ boxShadow: "none" }}>
              <div style={{ fontWeight: 900, marginBottom: 6 }}>{p.nombre}</div>
              <div style={{ color: "#666", fontSize: 13, marginBottom: 8 }}>
                Stock: <b>{p.stock}</b>
              </div>
              <div className="aRow" style={{ justifyContent: "space-between" }}>
                <div style={{ fontWeight: 900 }}>
                  {new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" }).format(p.precio)}
                </div>
                <button className="aBtn" onClick={() => agregar(p)} disabled={p.stock <= 0}>
                  Agregar
                </button>
              </div>
            </div>
          ))}
        </div>
      </section>

      <aside className="aCard">
        <h3 style={{ margin: "0 0 10px", fontWeight: 900 }}>Cliente</h3>

        <div style={{ display: "grid", gap: 10 }}>
          <input className="aInput" placeholder="DNI (opcional)" value={dni} onChange={(e) => setDni(e.target.value)} />
          <input className="aInput" placeholder="Nombre (opcional)" value={nombre} onChange={(e) => setNombre(e.target.value)} />

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

import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import {
  listarProveedores,
  listarProductos,
  crearCompra,
  listarProductosProveedor,
} from "../api/adminCompras.api";
import CompraItemsTable from "../components/CompraItemsTable";
import { confirmAction, notifyWarning } from "@/shared/ui/sweetAlert";
import "./NuevaCompra.css";

export default function NuevaCompra() {
  const navigate = useNavigate();
  const [sp] = useSearchParams();

  const [proveedores, setProveedores] = useState([]);
  const [productos, setProductos] = useState([]);
  const [productosProveedor, setProductosProveedor] = useState([]);

  const [proveedorId, setProveedorId] = useState(Number(sp.get("proveedorId") || 0));
  const [productoId, setProductoId] = useState(0);
  const [cantidad, setCantidad] = useState(1);
  const [costoUnitario, setCostoUnitario] = useState(0);

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [msg, setMsg] = useState("");

  const cargarProductosProveedor = async (pid) => {
    if (!pid) {
      setProductosProveedor([]);
      return;
    }
    const data = await listarProductosProveedor(pid);
    setProductosProveedor(Array.isArray(data) ? data : []);
  };

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setError("");
        setLoading(true);

        const [prov, prods] = await Promise.all([listarProveedores(true), listarProductos()]);

        if (!alive) return;

        setProveedores(Array.isArray(prov) ? prov : []);
        setProductos(Array.isArray(prods) ? prods : []);

        const pidInicial = proveedorId || (Array.isArray(prov) && prov.length ? prov[0].id : 0);
        if (pidInicial && pidInicial !== proveedorId) setProveedorId(pidInicial);
        if (pidInicial) await cargarProductosProveedor(pidInicial);
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar proveedores/productos.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        if (!proveedorId) {
          setProductosProveedor([]);
          return;
        }
        const data = await listarProductosProveedor(proveedorId);
        if (!alive) return;
        setProductosProveedor(Array.isArray(data) ? data : []);
      } catch {
        if (!alive) return;
        setProductosProveedor([]);
      }
    })();
    return () => {
      alive = false;
    };
  }, [proveedorId]);

  const productosById = useMemo(() => {
    const map = {};
    (productos ?? []).forEach((p) => {
      map[p.id] = p;
    });
    return map;
  }, [productos]);

  const costosByProductoId = useMemo(() => {
    const map = {};
    (productosProveedor ?? []).forEach((p) => {
      map[p.productoId] = Number(p.costoUnitario ?? 0);
    });
    return map;
  }, [productosProveedor]);

  const total = useMemo(() => {
    return (items ?? []).reduce((acc, it) => acc + it.cantidad * it.costoUnitario, 0);
  }, [items]);

  async function addItem() {
    const pid = Number(productoId);
    const qty = Number(cantidad);
    const cost = Number(costoUnitario || costosByProductoId[pid] || 0);

    if (!pid) {
      await notifyWarning("Dato requerido", "Selecciona un producto asociado al proveedor.");
      return;
    }
    if (qty <= 0) {
      await notifyWarning("Cantidad invalida", "Ingresa una cantidad mayor a 0.");
      return;
    }
    if (cost <= 0) {
      await notifyWarning("Costo invalido", "El costo del item debe ser mayor a 0.");
      return;
    }

    setItems((arr) => [...arr, { productoId: pid, cantidad: qty, costoUnitario: cost }]);
    setProductoId(0);
    setCantidad(1);
    setCostoUnitario(0);
  }

  function removeItem(idx) {
    setItems((arr) => arr.filter((_, i) => i !== idx));
  }

  function changeItem(idx, next) {
    setItems((arr) => arr.map((it, i) => (i === idx ? next : it)));
  }

  async function submit() {
    if (!proveedorId) {
      await notifyWarning("Dato requerido", "Selecciona un proveedor.");
      return;
    }
    if (!items.length) {
      await notifyWarning("Items requeridos", "Agrega al menos un item.");
      return;
    }

    for (const it of items) {
      if (!it.productoId) {
        await notifyWarning("Items invalidos", "Hay items sin producto.");
        return;
      }
      if (it.cantidad <= 0) {
        await notifyWarning("Items invalidos", "Hay items con cantidad invalida.");
        return;
      }
      if (it.costoUnitario <= 0) {
        await notifyWarning("Items invalidos", "Hay items con costo invalido.");
        return;
      }
    }

    const ok = await confirmAction({
      title: "Crear compra",
      text: `Se creara la compra con ${items.length} item(s) por un total de ${total.toFixed(2)}.`,
      confirmText: "Si, crear",
      cancelText: "Cancelar",
      icon: "warning",
    });
    if (!ok) return;

    setSaving(true);
    try {
      const resp = await crearCompra({
        proveedorId,
        items,
      });

      const id = resp?.id;
      if (!id) throw new Error("No se recibio id de compra.");

      navigate(`/compras/${id}`);
    } catch (e) {
      setError(e?.response?.data?.error ?? e?.message ?? "No se pudo crear la compra.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Nueva compra</h1>
          <p className="cmuted">
            Proceso: 1) Seleccionar proveedor, 2) Cargar items de compra.
          </p>
        </div>

        <div className="cactions">
          <button className="btn btn--ghost" onClick={() => navigate("/compras/historial")}>
            Ver historial
          </button>
          <button className="btn btn--primary" disabled={saving || loading} onClick={submit}>
            {saving ? "Guardando..." : `Crear compra (${total.toFixed(2)})`}
          </button>
        </div>
      </header>

      {loading ? <section className="ccard ccard__pad">Cargando...</section> : null}
      {error ? <section className="ccard ccard__pad cerror">{error}</section> : null}
      {msg ? <section className="ccard ccard__pad csuccess">{msg}</section> : null}

      {!loading && !error ? (
        <>
          <section className="ccard ccard__pad ncSection ncSection--provider">
            <div className="grid2 ncProviderGrid">
              <label className="field">
                <span>Proveedor</span>
                <select
                  className="cinput"
                  value={proveedorId || ""}
                  onChange={(e) => {
                    const pid = Number(e.target.value);
                    setProveedorId(pid);
                    setProductoId(0);
                    setItems([]);
                  }}
                >
                  <option value="" disabled>
                    Seleccionar...
                  </option>
                  {proveedores.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.razonSocial} ({p.cuit ?? p.CUIT})
                    </option>
                  ))}
                </select>
              </label>

              <div className="field">
                <span>Accion</span>
                <div className="rowInline ncActions">
                  <button className="btn btn--ghost ncBtnGhost" onClick={() => navigate("/compras/inventario")}>
                    Asociar libros a proveedor
                  </button>
                  <button className="btn btn--ghost ncBtnGhost" onClick={() => navigate("/compras/proveedores")}>
                    Ir a proveedores
                  </button>
                </div>
              </div>
            </div>
          </section>

          <section className="ccard ccard__pad">
            <h2 className="ctitle2">1) Agregar item</h2>

            <div className="grid4">
              <label className="field">
                <span>Producto</span>
                <select
                  className="cinput"
                  value={productoId}
                  onChange={(e) => {
                    const pid = Number(e.target.value);
                    setProductoId(pid);
                    setCostoUnitario(Number(costosByProductoId[pid] || 0));
                  }}
                >
                  <option value={0}>Seleccionar...</option>
                  {productosProveedor.map((p) => (
                    <option key={p.productoId} value={p.productoId}>
                      {p.nombre} (Stock: {p.stockActual})
                    </option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>Cantidad</span>
                <input className="cinput" type="number" min="1" value={cantidad} onChange={(e) => setCantidad(e.target.value)} />
              </label>

              <label className="field">
                <span>Costo unitario</span>
                <input className="cinput" type="number" min="0.01" step="0.01" value={costoUnitario} readOnly />
              </label>

              <div className="field">
                <span>&nbsp;</span>
                <button className="btn btn--primary" onClick={addItem} disabled={!productosProveedor.length}>
                  Agregar
                </button>
              </div>
            </div>
            {!productosProveedor.length ? (
              <p className="cmuted" style={{ marginTop: 10 }}>
                Este proveedor todavia no tiene libros asociados.
              </p>
            ) : null}
          </section>

          <CompraItemsTable items={items} productosById={productosById} onRemove={removeItem} onChange={changeItem} bloquearCosto />
        </>
      ) : null}
    </main>
  );
}

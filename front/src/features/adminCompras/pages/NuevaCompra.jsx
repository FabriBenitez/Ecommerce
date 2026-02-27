import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { listarProveedores, listarProductos, crearCompra } from "../api/adminCompras.api";
import CompraItemsTable from "../components/CompraItemsTable";
import { notifyWarning } from "@/shared/ui/sweetAlert";

export default function NuevaCompra() {
  const navigate = useNavigate();
  const [sp] = useSearchParams();

  const [proveedores, setProveedores] = useState([]);
  const [productos, setProductos] = useState([]);

  const [proveedorId, setProveedorId] = useState(Number(sp.get("proveedorId") || 0));
  const [productoId, setProductoId] = useState(0);
  const [cantidad, setCantidad] = useState(1);
  const [costoUnitario, setCostoUnitario] = useState(0);

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setError("");
        setLoading(true);

        const [prov, prods] = await Promise.all([
          listarProveedores(true),
          listarProductos(),
        ]);

        if (!alive) return;

        setProveedores(Array.isArray(prov) ? prov : []);
        setProductos(Array.isArray(prods) ? prods : []);

        // si no vino proveedorId por query, setear el primero activo
        if (!proveedorId && Array.isArray(prov) && prov.length) {
          setProveedorId(prov[0].id);
        }
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar proveedores/productos.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const productosById = useMemo(() => {
    const map = {};
    (productos ?? []).forEach((p) => { map[p.id] = p; });
    return map;
  }, [productos]);

  const total = useMemo(() => {
    return (items ?? []).reduce((acc, it) => acc + (it.cantidad * it.costoUnitario), 0);
  }, [items]);

  async function addItem() {
    const pid = Number(productoId);
    const qty = Number(cantidad);
    const cost = Number(costoUnitario);

    if (!pid) { await notifyWarning("Dato requerido", "Selecciona un producto."); return; }
    if (qty <= 0) { await notifyWarning("Cantidad invalida", "Ingresa una cantidad mayor a 0."); return; }
    if (cost < 0) { await notifyWarning("Costo invalido", "El costo no puede ser negativo."); return; }

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
    if (!proveedorId) { await notifyWarning("Dato requerido", "Selecciona un proveedor."); return; }
    if (!items.length) { await notifyWarning("Items requeridos", "Agrega al menos un item."); return; }

    // validación rápida
    for (const it of items) {
      if (!it.productoId) { await notifyWarning("Items invalidos", "Hay items sin producto."); return; }
      if (it.cantidad <= 0) { await notifyWarning("Items invalidos", "Hay items con cantidad invalida."); return; }
      if (it.costoUnitario < 0) { await notifyWarning("Items invalidos", "Hay items con costo invalido."); return; }
    }

    setSaving(true);
    try {
      const resp = await crearCompra({
        proveedorId,
        items,
      });

      const id = resp?.id;
      if (!id) throw new Error("No se recibió id de compra.");

      navigate(`/compras/${id}`);
    } catch (e) {
      setError(e?.message ?? "No se pudo crear la compra.");
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
            Cargá items y guardá la compra como <b>Pendiente</b>. Se confirma cuando llega mercadería.
          </p>
        </div>

        <div className="cactions">
          <button className="btn btn--ghost" onClick={() => navigate("/compras/historial")}>
            Ver historial
          </button>
          <button className="btn btn--primary" disabled={saving || loading} onClick={submit}>
            {saving ? "Guardando…" : `Crear compra (${total.toFixed(2)})`}
          </button>
        </div>
      </header>

      {loading ? <section className="ccard ccard__pad">Cargando…</section> : null}
      {error ? <section className="ccard ccard__pad cerror">{error}</section> : null}

      {!loading && !error ? (
        <>
          <section className="ccard ccard__pad">
            <div className="grid2">
              <label className="field">
                <span>Proveedor</span>
                <select
                  className="cinput"
                  value={proveedorId || ""}
                  onChange={(e) => setProveedorId(Number(e.target.value))}
                >
                  <option value="" disabled>Seleccionar…</option>
                  {proveedores.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.razonSocial} ({p.cuit ?? p.CUIT})
                    </option>
                  ))}
                </select>
              </label>

              <div className="field">
                <span>Acción</span>
                <div className="rowInline">
                  <button className="btn btn--ghost" onClick={() => navigate("/compras/proveedores")}>
                    Ir a proveedores
                  </button>
                </div>
              </div>
            </div>
          </section>

          <section className="ccard ccard__pad">
            <h2 className="ctitle2">Agregar item</h2>

            <div className="grid4">
              <label className="field">
                <span>Producto</span>
                <select className="cinput" value={productoId} onChange={(e) => setProductoId(Number(e.target.value))}>
                  <option value={0}>Seleccionar…</option>
                  {productos.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombre} (Stock: {p.stock})
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
                <input className="cinput" type="number" min="0" step="0.01" value={costoUnitario} onChange={(e) => setCostoUnitario(e.target.value)} />
              </label>

              <div className="field">
                <span>&nbsp;</span>
                <button className="btn btn--primary" onClick={addItem}>Agregar</button>
              </div>
            </div>
          </section>

          <CompraItemsTable
            items={items}
            productosById={productosById}
            onRemove={removeItem}
            onChange={changeItem}
          />
        </>
      ) : null}
    </main>
  );
}

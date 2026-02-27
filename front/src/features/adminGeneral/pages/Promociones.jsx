import { useEffect, useState } from "react";
import { crearPromocion, listarProductosAdmin, listarPromociones } from "../api/adminGeneral.api";

export default function Promociones() {
  const [items, setItems] = useState([]);
  const [productos, setProductos] = useState([]);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState("");
  const [err, setErr] = useState("");
  const [form, setForm] = useState({
    nombre: "",
    descripcion: "",
    productoId: "",
    porcentajeDescuento: "",
    montoDescuento: "",
    fechaInicio: "",
    fechaFin: "",
    activa: true,
  });

  const cargar = () => listarPromociones().then(setItems).catch(() => setItems([]));

  useEffect(() => {
    cargar();
    listarProductosAdmin().then(setProductos).catch(() => setProductos([]));
  }, []);

  const onSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setErr("");
    setMsg("");
    try {
      await crearPromocion({
        ...form,
        productoId: form.productoId ? Number(form.productoId) : null,
        porcentajeDescuento: form.porcentajeDescuento ? Number(form.porcentajeDescuento) : null,
        montoDescuento: form.montoDescuento ? Number(form.montoDescuento) : null,
      });
      setMsg("Promocion creada.");
      setForm({
        nombre: "",
        descripcion: "",
        productoId: "",
        porcentajeDescuento: "",
        montoDescuento: "",
        fechaInicio: "",
        fechaFin: "",
        activa: true,
      });
      await cargar();
    } catch (error) {
      setErr(error?.response?.data?.error ?? "No se pudo crear la promocion.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <section className="agPageHead">
        <h1>Gestion de Promociones</h1>
        <p>Configura y supervisa los descuentos activos en la tienda.</p>
      </section>

      <section className="agSection agSplit">
        <article className="agCard">
          <h3>Crear Nueva Promocion</h3>
          <form className="agForm" onSubmit={onSubmit}>
            <label>Libro afectado
              <select value={form.productoId} onChange={(e) => setForm((p) => ({ ...p, productoId: e.target.value }))}>
                <option value="">Seleccionar categoria o libro...</option>
                {productos.map((p) => (
                  <option key={p.id} value={p.id}>{p.nombre}</option>
                ))}
              </select>
            </label>
            <label>Nombre
              <input value={form.nombre} onChange={(e) => setForm((p) => ({ ...p, nombre: e.target.value }))} />
            </label>
            <label>Descripcion
              <input value={form.descripcion} onChange={(e) => setForm((p) => ({ ...p, descripcion: e.target.value }))} />
            </label>
            <div className="agFormRow">
              <label>Porcentaje
                <input type="number" value={form.porcentajeDescuento} onChange={(e) => setForm((p) => ({ ...p, porcentajeDescuento: e.target.value }))} />
              </label>
              <label>Monto fijo
                <input type="number" value={form.montoDescuento} onChange={(e) => setForm((p) => ({ ...p, montoDescuento: e.target.value }))} />
              </label>
            </div>
            <div className="agFormRow">
              <label>Inicio
                <input type="date" value={form.fechaInicio} onChange={(e) => setForm((p) => ({ ...p, fechaInicio: e.target.value }))} />
              </label>
              <label>Fin
                <input type="date" value={form.fechaFin} onChange={(e) => setForm((p) => ({ ...p, fechaFin: e.target.value }))} />
              </label>
            </div>
            <label>Estado
              <select value={form.activa ? "1" : "0"} onChange={(e) => setForm((p) => ({ ...p, activa: e.target.value === "1" }))}>
                <option value="1">Activa</option>
                <option value="0">Inactiva</option>
              </select>
            </label>
            <button className="agBtn" type="submit" disabled={saving}>{saving ? "Guardando..." : "Guardar Promocion"}</button>
          </form>
          {msg ? <p className="agOk">{msg}</p> : null}
          {err ? <p className="agErr">{err}</p> : null}
        </article>

        <article className="agCard agCard--soft">
          <div className="agSection__head"><h3>Promociones Listadas</h3><span>{items.length} total</span></div>
          <div className="agList">
            {items.length === 0 ? (
              <p>Sin promociones cargadas.</p>
            ) : (
              items.map((item) => (
                <div className="agItem" key={item.id}>
                  <div className="agItem__icon">%</div>
                  <div>
                    <h4>{item.nombre}</h4>
                    <p>
                      {item.productoNombre ? `Aplica a: ${item.productoNombre} - ` : ""}
                      {new Date(item.fechaInicio).toLocaleDateString("es-AR")} - {new Date(item.fechaFin).toLocaleDateString("es-AR")}
                    </p>
                  </div>
                  <div className="agItem__value">
                    <span className={`agBadge ${item.activa ? "agBadge--ok" : "agBadge--warn"}`}>{item.activa ? "Activa" : "Inactiva"}</span>
                  </div>
                </div>
              ))
            )}
          </div>
        </article>
      </section>
    </>
  );
}

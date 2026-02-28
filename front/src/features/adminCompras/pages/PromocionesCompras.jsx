import { useEffect, useMemo, useState } from "react";
import {
  crearPromocion,
  desactivarPromocion,
  desactivarPromocionPorGenero,
  desactivarPromocionPorProducto,
  listarProductos,
  listarPromociones,
} from "../api/adminCompras.api";
import "../styles/ComprasCommon.css";

const GENERO_PALABRAS_CLAVE = [
  ["ciencia ficcion", "Ciencia ficcion"],
  ["ciencia ficción", "Ciencia ficcion"],
  ["ficcion", "Ficcion"],
  ["ficción", "Ficcion"],
  ["novela", "Novela"],
  ["terror", "Terror"],
  ["romance", "Romance"],
  ["fantasia", "Fantasia"],
  ["fantasía", "Fantasia"],
  ["historia", "Historia"],
  ["historico", "Historico"],
  ["histórico", "Historico"],
  ["biografia", "Biografia"],
  ["biografía", "Biografia"],
  ["poesia", "Poesia"],
  ["poesía", "Poesia"],
  ["infantil", "Infantil"],
  ["juvenil", "Juvenil"],
  ["aventura", "Aventura"],
  ["drama", "Drama"],
  ["policial", "Policial"],
  ["misterio", "Misterio"],
  ["autoayuda", "Autoayuda"],
  ["clasico", "Clasico"],
  ["clásico", "Clasico"],
];

function capitalizar(valor) {
  if (!valor) return valor;
  return valor.charAt(0).toUpperCase() + valor.slice(1);
}

function detectarGeneroDeProducto(producto) {
  const descripcion = (producto?.descripcion || "").trim();
  if (!descripcion) return "";

  const explicitMatch = descripcion.match(/(?:genero|género|categoria|categoría)\s*:\s*([^\-|,.;]+)/i);
  if (explicitMatch?.[1]) return capitalizar(explicitMatch[1].trim().toLowerCase());

  const descNorm = descripcion.toLowerCase();
  const keyword = GENERO_PALABRAS_CLAVE.find(([term]) => descNorm.includes(term));
  return keyword ? keyword[1] : "";
}

export default function PromocionesCompras() {
  const [items, setItems] = useState([]);
  const [productos, setProductos] = useState([]);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState("");
  const [err, setErr] = useState("");
  const [search, setSearch] = useState("");
  const [form, setForm] = useState({
    productoId: "",
    genero: "",
    porcentajeDescuento: "",
    montoDescuento: "",
    fechaInicio: "",
    fechaFin: "",
    activa: true,
  });

  const cargar = () => listarPromociones().then(setItems).catch(() => setItems([]));

  useEffect(() => {
    cargar();
    listarProductos().then(setProductos).catch(() => setProductos([]));
  }, []);

  const productosFiltrados = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return productos;
    return productos.filter((p) =>
      (p.nombre ?? "").toLowerCase().includes(q) || String(p.id).includes(q)
    );
  }, [productos, search]);

  const generosDisponibles = useMemo(() => {
    const map = new Map();

    for (const p of productos) {
      const genero = detectarGeneroDeProducto(p);
      if (!genero) continue;
      const key = genero.toLowerCase();
      if (!map.has(key)) map.set(key, genero);
    }

    for (const item of items) {
      const genero = (item?.genero ?? "").trim();
      if (!genero) continue;
      const key = genero.toLowerCase();
      if (!map.has(key)) map.set(key, capitalizar(genero));
    }

    return Array.from(map.values()).sort((a, b) => a.localeCompare(b, "es"));
  }, [productos, items]);

  const onSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setErr("");
    setMsg("");
    try {
      await crearPromocion({
        ...form,
        productoId: form.productoId ? Number(form.productoId) : null,
        genero: form.genero.trim() || null,
        porcentajeDescuento: form.porcentajeDescuento ? Number(form.porcentajeDescuento) : null,
        montoDescuento: form.montoDescuento ? Number(form.montoDescuento) : null,
      });
      setMsg("Promocion creada.");
      setForm({
        productoId: "",
        genero: "",
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
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Promociones</h1>
          <p className="cmuted">Crear y quitar promociones por libro o genero.</p>
        </div>
      </header>

      <section className="ccard ccard__pad">
        <form className="agForm" onSubmit={onSubmit}>
          <div className="agFormRow">
            <label>Buscar libro
              <input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Buscar por nombre o ID"
              />
            </label>
            <label>Libro (opcional)
              <select value={form.productoId} onChange={(e) => setForm((p) => ({ ...p, productoId: e.target.value }))}>
                <option value="">Sin libro especifico</option>
                {productosFiltrados.map((p) => (
                  <option key={p.id} value={p.id}>{p.nombre}</option>
                ))}
              </select>
            </label>
          </div>
          <label>Genero (si no aplica por libro)
            <select
              value={form.genero}
              onChange={(e) => setForm((p) => ({ ...p, genero: e.target.value }))}
              disabled={Boolean(form.productoId)}
            >
              <option value="">Sin genero especifico</option>
              {generosDisponibles.map((g) => (
                <option key={g} value={g}>{g}</option>
              ))}
            </select>
          </label>
          {!generosDisponibles.length ? <p className="cmuted">No hay generos detectados en libros cargados.</p> : null}
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
          <button className="btn btn--primary" type="submit" disabled={saving}>
            {saving ? "Guardando..." : "Guardar promocion"}
          </button>
        </form>
        {msg ? <p className="agOk">{msg}</p> : null}
        {err ? <p className="agErr">{err}</p> : null}
      </section>

      <section className="ccard ccard__pad">
        <h3>Promociones cargadas</h3>
        {(items ?? []).map((item) => (
          <div className="agItem" key={item.id}>
            <div>
              <strong>{item.nombre}</strong>
              <p className="cmuted">
                {item.productoNombre ? `Libro: ${item.productoNombre}` : ""} {item.genero ? `Genero: ${item.genero}` : ""}
              </p>
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              <button className="btn btn--ghost btn--sm" onClick={() => desactivarPromocion(item.id).then(cargar)}>
                Quitar
              </button>
              {item.productoId ? (
                <button className="btn btn--ghost btn--sm" onClick={() => desactivarPromocionPorProducto(item.productoId).then(cargar)}>
                  Quitar por libro
                </button>
              ) : null}
              {item.genero ? (
                <button className="btn btn--ghost btn--sm" onClick={() => desactivarPromocionPorGenero(item.genero).then(cargar)}>
                  Quitar por genero
                </button>
              ) : null}
            </div>
          </div>
        ))}
      </section>
    </main>
  );
}

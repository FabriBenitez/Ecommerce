import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { listarProveedores, actualizarProveedor, listarCompras } from "../api/adminCompras.api";
import "../styles/ComprasCommon.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function estadoCompraLabel(e) {
  const map = { 1: "Pendiente", 2: "Confirmada", 3: "Cancelada" };
  return map[e] ?? String(e ?? "");
}

export default function ProveedorDetalle() {
  const { id } = useParams();
  const proveedorId = Number(id);

  const [prov, setProv] = useState(null);
  const [compras, setCompras] = useState([]);

  const [form, setForm] = useState({
    razonSocial: "",
    cuit: "",
    email: "",
    telefono: "",
    activo: true,
  });

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [ok, setOk] = useState("");

  const cargar = async () => {
    const [provs, comps] = await Promise.all([
      listarProveedores(), // no hay GET por id en backend, filtramos
      listarCompras(proveedorId),
    ]);

    const found = (provs ?? []).find((p) => Number(p.id) === proveedorId) ?? null;

    setProv(found);
    setCompras(Array.isArray(comps) ? comps : []);

    if (found) {
      setForm({
        razonSocial: found.razonSocial ?? "",
        cuit: found.cuit ?? found.CUIT ?? "",
        email: found.email ?? "",
        telefono: found.telefono ?? "",
        activo: !!found.activo,
      });
    }
  };

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        await cargar();
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el proveedor.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [proveedorId]);

  const onChange = (k) => (e) => {
    const v = e.target.type === "checkbox" ? e.target.checked : e.target.value;
    setForm((prev) => ({ ...prev, [k]: v }));
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    setOk("");
    setError("");

    if (!form.razonSocial.trim()) return setError("Razón social es obligatoria.");
    if (!form.cuit.trim()) return setError("CUIT es obligatorio.");

    try {
      setSaving(true);

      await actualizarProveedor(proveedorId, {
        razonSocial: form.razonSocial.trim(),
        cuit: form.cuit.trim(),
        email: form.email?.trim() || null,
        telefono: form.telefono?.trim() || null,
        activo: !!form.activo,
      });

      setOk("Cambios guardados ✅");
      await cargar();
    } catch (e2) {
      setError(e2?.message ?? "No se pudo actualizar el proveedor.");
    } finally {
      setSaving(false);
      setTimeout(() => setOk(""), 2500);
    }
  };

  const totalComprado = useMemo(
    () => (compras ?? []).reduce((acc, c) => acc + Number(c.total ?? 0), 0),
    [compras]
  );

  if (loading) {
    return <main className="cpage"><section className="ccard ccard__pad">Cargando…</section></main>;
  }

  if (!prov) {
    return (
      <main className="cpage">
        <section className="ccard ccard__pad">
          <p className="cerror">Proveedor no encontrado.</p>
          <Link className="btn btn--ghost" to="/compras/proveedores">Volver</Link>
        </section>
      </main>
    );
  }

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <Link className="btn btn--ghost" to="/compras/proveedores">← Volver</Link>
          <h1 className="ctitle" style={{ marginTop: 10 }}>{prov.razonSocial}</h1>
          <p className="cmuted">CUIT: <span className="mono">{prov.cuit ?? prov.CUIT ?? "-"}</span></p>
        </div>

        <div className="cactions">
          <Link className="btn btn--primary" to="/compras/nueva">Nueva compra</Link>
        </div>
      </header>

      <section className="grid2">
        <section className="ccard ccard__pad">
          <h2 className="csubtitle">Editar proveedor</h2>

          {error ? <p className="cerror">{error}</p> : null}
          {ok ? <p className="csuccess">{ok}</p> : null}

          <form className="cform" onSubmit={onSubmit}>
            <label className="field">
              <span>Razón social *</span>
              <input className="cinput" value={form.razonSocial} onChange={onChange("razonSocial")} />
            </label>

            <label className="field">
              <span>CUIT *</span>
              <input className="cinput" value={form.cuit} onChange={onChange("cuit")} />
            </label>

            <label className="field">
              <span>Email</span>
              <input className="cinput" type="email" value={form.email} onChange={onChange("email")} />
            </label>

            <label className="field">
              <span>Teléfono</span>
              <input className="cinput" value={form.telefono} onChange={onChange("telefono")} />
            </label>

            <label className="field field--check">
              <input type="checkbox" checked={form.activo} onChange={onChange("activo")} />
              <span>Activo</span>
            </label>

            <div className="formActions">
              <button className="btn btn--primary" disabled={saving}>
                {saving ? "Guardando…" : "Guardar cambios"}
              </button>
            </div>
          </form>
        </section>

        <section className="ccard ccard__pad">
          <h2 className="csubtitle">Resumen</h2>
          <div className="kv">
            <div><span className="k">Compras</span><span className="v strong">{(compras ?? []).length}</span></div>
            <div><span className="k">Total comprado</span><span className="v strong">{money.format(totalComprado)}</span></div>
            <div><span className="k">Activo</span><span className="v">{prov.activo ? "Sí" : "No"}</span></div>
          </div>
        </section>
      </section>

      <section className="ccard">
        <div className="ccard__pad">
          <h2 className="csubtitle">Compras de este proveedor</h2>
          <p className="cmuted">Entrá al detalle para ver ítems / factura / confirmar.</p>
        </div>

        <div className="tableWrap">
          <table className="ctable" style={{ minWidth: 820 }}>
            <thead>
              <tr>
                <th>#</th>
                <th>Fecha</th>
                <th>Estado</th>
                <th className="right">Total</th>
                <th className="right">Acción</th>
              </tr>
            </thead>
            <tbody>
              {(compras ?? []).map((c) => (
                <tr key={c.id}>
                  <td className="mono">#{c.id}</td>
                  <td>{c.fecha ? new Date(c.fecha).toLocaleString("es-AR") : "-"}</td>
                  <td>{estadoCompraLabel(c.estadoCompra)}</td>
                  <td className="right strong">{money.format(c.total ?? 0)}</td>
                  <td className="right">
                    <Link className="btn btn--ghost btn--sm" to={`/compras/${c.id}`}>
                      Ver compra
                    </Link>
                  </td>
                </tr>
              ))}

              {(compras ?? []).length === 0 ? (
                <tr>
                  <td colSpan="5" className="emptyRow">Todavía no hay compras para este proveedor.</td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}
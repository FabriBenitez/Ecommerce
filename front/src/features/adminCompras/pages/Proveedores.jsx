import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { listarProveedores, crearProveedor } from "../api/adminCompras.api";
import { confirmAction } from "@/shared/ui/sweetAlert";
import "../styles/ComprasCommon.css";
import "./Proveedores.css";

export default function Proveedores() {
  const [proveedores, setProveedores] = useState([]);
  const [q, setQ] = useState("");
  const [soloActivos, setSoloActivos] = useState(false);

  const [showForm, setShowForm] = useState(false);
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
    const data = await listarProveedores(soloActivos ? true : undefined);
    setProveedores(Array.isArray(data) ? data : []);
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
        setError(e?.message ?? "No se pudo cargar proveedores.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [soloActivos]);

  const filtrados = useMemo(() => {
    const qq = q.trim().toLowerCase();
    if (!qq) return proveedores ?? [];
    return (proveedores ?? []).filter((p) => {
      const rs = (p.razonSocial ?? "").toLowerCase();
      const cuit = (p.cuit ?? p.CUIT ?? "").toLowerCase();
      return rs.includes(qq) || cuit.includes(qq) || String(p.id ?? "").includes(qq);
    });
  }, [proveedores, q]);

  const onChange = (k) => (e) => {
    const raw = e.target.type === "checkbox" ? e.target.checked : e.target.value;
    const v = k === "cuit" && typeof raw === "string" ? raw.replace(/\D/g, "") : raw;
    setForm((prev) => ({ ...prev, [k]: v }));
  };

  const resetForm = () => {
    setForm({ razonSocial: "", cuit: "", email: "", telefono: "", activo: true });
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    setOk("");
    setError("");

    if (!form.razonSocial.trim()) return setError("Razon social es obligatoria.");
    if (!form.cuit.trim()) return setError("CUIT es obligatorio.");

    const okConfirm = await confirmAction({
      title: "Crear proveedor",
      text: `Se creara el proveedor "${form.razonSocial.trim()}".`,
      confirmText: "Si, crear",
      cancelText: "Cancelar",
      icon: "warning",
    });
    if (!okConfirm) return;

    try {
      setSaving(true);

      await crearProveedor({
        razonSocial: form.razonSocial.trim(),
        cuit: form.cuit.trim(),
        email: form.email?.trim() || null,
        telefono: form.telefono?.trim() || null,
        activo: !!form.activo,
      });

      setOk("Proveedor creado correctamente.");
      resetForm();
      setShowForm(false);
      await cargar();
    } catch (e2) {
      setError(e2?.message ?? "No se pudo crear el proveedor.");
    } finally {
      setSaving(false);
      setTimeout(() => setOk(""), 2500);
    }
  };

  return (
    <main className="cpage">
      <header className="cpage__head">
        <div>
          <h1 className="ctitle">Proveedores</h1>
          <p className="cmuted">Alta, edicion y acceso al detalle.</p>
        </div>

        <div className="cactions">
          <button className="btn btn--primary" type="button" onClick={() => setShowForm((v) => !v)}>
            {showForm ? "Cerrar" : "Nuevo proveedor"}
          </button>
        </div>
      </header>

      <section className="ccard ccard__pad">
        <div className="toolbar">
          <label className="field">
            <span>Buscar</span>
            <input
              className="cinput"
              placeholder="Por razon social, CUIT o #id..."
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
          </label>

          <label className="field" style={{ alignSelf: "end" }}>
            <span>&nbsp;</span>
            <button
              type="button"
              className={`btn ${soloActivos ? "btn--primary" : "btn--ghost"}`}
              onClick={() => setSoloActivos((v) => !v)}
            >
              {soloActivos ? "Mostrando activos" : "Ver solo activos"}
            </button>
          </label>
        </div>
      </section>

      {showForm ? (
        <section className="ccard ccard__pad">
          <h2 className="csubtitle">Nuevo proveedor</h2>

          {error ? <p className="cerror">{error}</p> : null}
          {ok ? <p className="csuccess">{ok}</p> : null}

          <form className="cform" onSubmit={onSubmit}>
            <label className="field">
              <span>Razon social *</span>
              <input className="cinput" value={form.razonSocial} onChange={onChange("razonSocial")} />
            </label>

            <label className="field">
              <span>CUIT *</span>
              <input className="cinput" value={form.cuit} onChange={onChange("cuit")} inputMode="numeric" />
            </label>

            <label className="field">
              <span>Email</span>
              <input className="cinput" type="email" value={form.email} onChange={onChange("email")} />
            </label>

            <label className="field">
              <span>Telefono</span>
              <input className="cinput" value={form.telefono} onChange={onChange("telefono")} />
            </label>

            <label className="field field--check">
              <input type="checkbox" checked={form.activo} onChange={onChange("activo")} />
              <span>Activo</span>
            </label>

            <div className="formActions">
              <button className="btn btn--primary" disabled={saving}>
                {saving ? "Guardando..." : "Crear proveedor"}
              </button>
              <button
                className="btn btn--ghost"
                type="button"
                onClick={() => {
                  resetForm();
                  setShowForm(false);
                  setError("");
                }}
              >
                Cancelar
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {loading ? <section className="ccard ccard__pad">Cargando...</section> : null}

      {!loading ? (
        <section className="ccard">
          <div className="tableWrap">
            <table className="ctable proveedoresTable" style={{ minWidth: 760 }}>
              <thead>
                <tr>
                  <th>Razon social</th>
                  <th>CUIT</th>
                  <th>Activo</th>
                  <th className="right">Accion</th>
                </tr>
              </thead>
              <tbody>
                {(filtrados ?? []).map((p) => (
                  <tr key={p.id}>
                    <td>
                      <div className="proveedoresTable__rs">{p.razonSocial ?? "-"}</div>
                    </td>
                    <td><span className="proveedoresTable__cuit">{p.cuit ?? p.CUIT ?? "-"}</span></td>
                    <td>
                      <span className={`proveedoresTable__estado ${p.activo ? "is-on" : "is-off"}`}>
                        {p.activo ? "Activo" : "Inactivo"}
                      </span>
                    </td>
                    <td className="right">
                      <Link className="btn btn--ghost btn--sm proveedoresTable__action" to={`/compras/proveedores/${p.id}`}>
                        Ver / Editar
                      </Link>
                    </td>
                  </tr>
                ))}

                {(filtrados ?? []).length === 0 ? (
                  <tr>
                    <td colSpan="4" className="emptyRow">No hay proveedores.</td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
    </main>
  );
}



import { useEffect, useMemo, useState } from "react";
import { actualizarUsuarioInterno, listarUsuariosAdmin } from "../api/adminGeneral.api";

const ROLES = ["AdminVentas", "AdminCompras"];

export default function UsuariosAdmin() {
  const [usuarios, setUsuarios] = useState([]);
  const [rolFiltro, setRolFiltro] = useState("AdminVentas");
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({ email: "", dni: "", rol: "AdminVentas", nuevaPassword: "" });
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState("");
  const [err, setErr] = useState("");

  const cargar = async () => {
    const data = await listarUsuariosAdmin();
    setUsuarios(data ?? []);
  };

  useEffect(() => {
    cargar().catch(() => setUsuarios([]));
  }, []);

  const filtrados = useMemo(() => {
    return usuarios
      .filter((u) => (u.roles ?? []).some((r) => ROLES.includes(r)))
      .filter((u) => (u.roles ?? []).includes(rolFiltro));
  }, [usuarios, rolFiltro]);

  const comenzarEdicion = (u) => {
    const rol = (u.roles ?? []).find((r) => ROLES.includes(r)) ?? "AdminVentas";
    setEditing(u);
    setForm({
      email: u.email ?? "",
      dni: u.dni ?? "",
      rol,
      nuevaPassword: "",
    });
    setErr("");
    setMsg("");
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    if (!editing) return;

    setSaving(true);
    setErr("");
    setMsg("");
    try {
      await actualizarUsuarioInterno(editing.id, {
        email: form.email,
        dni: form.dni || null,
        rol: form.rol,
        nuevaPassword: form.nuevaPassword || null,
      });
      setMsg("Usuario actualizado.");
      setEditing(null);
      setForm({ email: "", dni: "", rol: "AdminVentas", nuevaPassword: "" });
      await cargar();
    } catch (error) {
      setErr(error?.response?.data?.error ?? "No se pudo actualizar.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <section className="agPageHead">
        <h1>Gestion de Personal</h1>
        <p>Solo administradores de Ventas y Compras.</p>
      </section>

      <section className="agSection">
        <div className="agSection__head">
          <div className="agQuickList" style={{ gridAutoFlow: "column", gap: 8 }}>
            {ROLES.map((r) => (
              <button
                key={r}
                className={`agBtn ${rolFiltro === r ? "" : "agBtn--ghost"}`}
                onClick={() => setRolFiltro(r)}
                type="button"
              >
                {r}
              </button>
            ))}
          </div>
        </div>

        <article className="agCard">
          <div className="agTableWrap">
            <table className="agTable">
              <thead>
                <tr>
                  <th>Usuario</th>
                  <th>DNI</th>
                  <th>Rol</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {filtrados.length === 0 ? (
                  <tr><td colSpan={4}>Sin administradores para este rol.</td></tr>
                ) : (
                  filtrados.map((u) => (
                    <tr key={u.id}>
                      <td>{u.email}</td>
                      <td>{u.dni ?? "-"}</td>
                      <td>
                        <span className="agBadge agBadge--ok">
                          {(u.roles ?? []).find((r) => ROLES.includes(r)) ?? "-"}
                        </span>
                      </td>
                      <td>
                        <button className="agBtn agBtn--ghost" type="button" onClick={() => comenzarEdicion(u)}>
                          Editar
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </article>
      </section>

      <section className="agSection">
        <article className="agCard">
          <h3>Editar Usuario</h3>
          {!editing ? (
            <p>Selecciona un usuario de la tabla para editar.</p>
          ) : (
            <form className="agForm" onSubmit={onSubmit}>
              <div className="agFormRow">
                <label>Usuario (email)
                  <input value={form.email} onChange={(e) => setForm((p) => ({ ...p, email: e.target.value }))} />
                </label>
                <label>DNI
                  <input value={form.dni} onChange={(e) => setForm((p) => ({ ...p, dni: e.target.value }))} />
                </label>
              </div>
              <div className="agFormRow">
                <label>Rol
                  <select value={form.rol} onChange={(e) => setForm((p) => ({ ...p, rol: e.target.value }))}>
                    {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                  </select>
                </label>
                <label>Nueva contraseña (opcional)
                  <input type="password" value={form.nuevaPassword} onChange={(e) => setForm((p) => ({ ...p, nuevaPassword: e.target.value }))} />
                </label>
              </div>
              <div style={{ display: "flex", gap: 8 }}>
                <button className="agBtn" type="submit" disabled={saving}>{saving ? "Guardando..." : "Guardar cambios"}</button>
                <button className="agBtn agBtn--ghost" type="button" onClick={() => setEditing(null)}>Cancelar</button>
              </div>
            </form>
          )}

          {msg ? <p className="agOk">{msg}</p> : null}
          {err ? <p className="agErr">{err}</p> : null}
        </article>
      </section>
    </>
  );
}

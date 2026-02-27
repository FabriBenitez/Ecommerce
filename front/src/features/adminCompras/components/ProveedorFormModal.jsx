import { useEffect, useState } from "react";
import { notifyWarning } from "@/shared/ui/sweetAlert";

const empty = {
  razonSocial: "",
  cuit: "",
  email: "",
  telefono: "",
  activo: true,
};

export default function ProveedorFormModal({ open, onClose, initial, onSubmit }) {
  const [form, setForm] = useState(empty);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    setForm({
      razonSocial: initial?.razonSocial ?? "",
      cuit: initial?.cuit ?? initial?.CUIT ?? "",
      email: initial?.email ?? "",
      telefono: initial?.telefono ?? "",
      activo: typeof initial?.activo === "boolean" ? initial.activo : true,
      id: initial?.id,
    });
  }, [open, initial]);

  if (!open) return null;

  const set = (k) => (e) => setForm((s) => ({ ...s, [k]: e.target.value }));
  const setBool = (k) => (e) => setForm((s) => ({ ...s, [k]: e.target.checked }));

  async function handleSubmit(e) {
    e.preventDefault();
    if (!form.razonSocial.trim()) {
      await notifyWarning("Dato requerido", "Razon social obligatoria.");
      return;
    }
    if (!form.cuit.trim()) {
      await notifyWarning("Dato requerido", "CUIT obligatorio.");
      return;
    }

    setSaving(true);
    try {
      await onSubmit({
        razonSocial: form.razonSocial.trim(),
        cuit: form.cuit.trim(),
        email: form.email?.trim() || null,
        telefono: form.telefono?.trim() || null,
        activo: !!form.activo,
      });
      onClose();
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modalOverlay" onMouseDown={onClose}>
      <div className="modalCard" onMouseDown={(e) => e.stopPropagation()}>
        <div className="modalHead">
          <h3 className="modalTitle">{initial?.id ? "Editar proveedor" : "Nuevo proveedor"}</h3>
          <button className="btn btn--ghost" onClick={onClose}>✕</button>
        </div>

        <form onSubmit={handleSubmit} className="modalBody">
          <div className="grid2">
            <label className="field">
              <span>Razón Social</span>
              <input className="cinput" value={form.razonSocial} onChange={set("razonSocial")} />
            </label>

            <label className="field">
              <span>CUIT</span>
              <input className="cinput" value={form.cuit} onChange={set("cuit")} />
            </label>

            <label className="field">
              <span>Email</span>
              <input className="cinput" value={form.email} onChange={set("email")} />
            </label>

            <label className="field">
              <span>Teléfono</span>
              <input className="cinput" value={form.telefono} onChange={set("telefono")} />
            </label>
          </div>

          <label className="ctoggle" style={{ marginTop: 10 }}>
            <input type="checkbox" checked={form.activo} onChange={setBool("activo")} />
            <span>Proveedor activo</span>
          </label>

          <div className="modalFoot">
            <button type="button" className="btn btn--ghost" onClick={onClose}>
              Cancelar
            </button>
            <button className="btn btn--primary" disabled={saving}>
              {saving ? "Guardando…" : "Guardar"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

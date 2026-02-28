import { useEffect, useState } from "react";
import { actualizarDatosFactura, obtenerDatosFactura } from "../api/adminGeneral.api";

const initial = {
  nombreComercial: "",
  tituloComprobante: "",
  direccion: "",
  telefono: "",
  email: "",
  mensajeAgradecimiento: "",
};

export default function FacturaDatosAdmin() {
  const [form, setForm] = useState(initial);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState("");
  const [err, setErr] = useState("");

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        setLoading(true);
        const data = await obtenerDatosFactura();
        if (!alive) return;
        setForm({
          nombreComercial: data?.nombreComercial ?? "",
          tituloComprobante: data?.tituloComprobante ?? "",
          direccion: data?.direccion ?? "",
          telefono: data?.telefono ?? "",
          email: data?.email ?? "",
          mensajeAgradecimiento: data?.mensajeAgradecimiento ?? "",
        });
      } catch {
        if (!alive) return;
        setErr("No se pudieron cargar los datos de factura.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, []);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    setSaving(true);
    setMsg("");
    setErr("");
    try {
      const data = await actualizarDatosFactura(form);
      setForm({
        nombreComercial: data?.nombreComercial ?? "",
        tituloComprobante: data?.tituloComprobante ?? "",
        direccion: data?.direccion ?? "",
        telefono: data?.telefono ?? "",
        email: data?.email ?? "",
        mensajeAgradecimiento: data?.mensajeAgradecimiento ?? "",
      });
      setMsg("Datos actualizados.");
    } catch {
      setErr("No se pudieron guardar los cambios.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <section className="agCard"><p>Cargando...</p></section>;

  return (
    <>
      <section className="agPageHead">
        <h1>Datos globales de factura</h1>
        <p>Configura la informacion legal y comercial que se imprime en comprobantes.</p>
      </section>

      <section className="agSection">
        <article className="agCard" style={{ maxWidth: 980, margin: "0 auto" }}>
          <h3 style={{ marginBottom: 16 }}>Configuracion de comprobantes</h3>
          <form className="agForm" onSubmit={onSubmit}>
            <div className="agFormRow">
              <label>Razon social
                <input name="nombreComercial" value={form.nombreComercial} onChange={onChange} />
              </label>
              <label>Titulo comprobante
                <input name="tituloComprobante" value={form.tituloComprobante} onChange={onChange} />
              </label>
            </div>
            <label>Direccion legal
              <input name="direccion" value={form.direccion} onChange={onChange} />
            </label>
            <div className="agFormRow">
              <label>Telefono de contacto
                <input name="telefono" value={form.telefono} onChange={onChange} />
              </label>
              <label>Email administrativo
                <input name="email" value={form.email} onChange={onChange} />
              </label>
            </div>
            <label>Pie de comprobante / mensaje final
              <textarea name="mensajeAgradecimiento" value={form.mensajeAgradecimiento} onChange={onChange} />
            </label>
            <div style={{ display: "flex", gap: 8, justifyContent: "flex-end" }}>
              <button className="agBtn agBtn--ghost" type="button" onClick={() => setForm(initial)}>Restablecer</button>
              <button className="agBtn" type="submit" disabled={saving}>{saving ? "Guardando..." : "Guardar cambios"}</button>
            </div>
          </form>
          {msg ? <p className="agOk">{msg}</p> : null}
          {err ? <p className="agErr">{err}</p> : null}
        </article>

        <div className="agHint" style={{ marginTop: 12, maxWidth: 980, marginInline: "auto" }}>
          Los cambios impactan en comprobantes de venta presencial y ecommerce.
        </div>
      </section>
    </>
  );
}

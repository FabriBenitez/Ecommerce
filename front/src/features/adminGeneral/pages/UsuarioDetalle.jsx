import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { cambiarPasswordUsuarioAdmin, detalleUsuarioAdmin } from "../api/adminGeneral.api";

export default function UsuarioDetalle() {
  const { id } = useParams();
  const [usuario, setUsuario] = useState(null);
  const [password, setPassword] = useState("");
  const [msg, setMsg] = useState("");
  const [err, setErr] = useState("");

  useEffect(() => {
    if (!id) return;
    detalleUsuarioAdmin(id).then(setUsuario).catch(() => setUsuario(null));
  }, [id]);

  return (
    <>
      <section className="agPageHead">
        <h1>Detalle de Usuario</h1>
        <p>Informacion ampliada del perfil interno.</p>
      </section>
      <section className="agSection">
        <article className="agCard">
          {!usuario ? (
            <p>Sin datos.</p>
          ) : (
            <div className="agForm">
              <div className="agFormRow">
                <label>Nombre<input value={usuario.nombreCompleto ?? ""} readOnly /></label>
                <label>Email<input value={usuario.email ?? ""} readOnly /></label>
              </div>
              <div className="agFormRow">
                <label>DNI<input value={usuario.dni ?? "-"} readOnly /></label>
                <label>Telefono<input value={usuario.telefono ?? "-"} readOnly /></label>
              </div>
              <div className="agFormRow">
                <label>Estado<input value={usuario.activo ? "Activo" : "Inactivo"} readOnly /></label>
                <label>Ultimo acceso<input value={usuario.ultimoLogin ? new Date(usuario.ultimoLogin).toLocaleString("es-AR") : "-"} readOnly /></label>
              </div>
              <div className="agFormRow">
                <label>Nueva password
                  <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Minimo 6 caracteres"
                  />
                </label>
                <div style={{ display: "flex", alignItems: "end" }}>
                  <button
                    className="agBtn"
                    type="button"
                    onClick={async () => {
                      try {
                        setErr("");
                        setMsg("");
                        await cambiarPasswordUsuarioAdmin(id, password);
                        setMsg("Password actualizada.");
                        setPassword("");
                      } catch (e) {
                        setErr(e?.response?.data?.error ?? "No se pudo cambiar la password.");
                      }
                    }}
                  >
                    Cambiar password
                  </button>
                </div>
              </div>
              {msg ? <p className="agOk">{msg}</p> : null}
              {err ? <p className="agErr">{err}</p> : null}
            </div>
          )}
        </article>
      </section>
    </>
  );
}

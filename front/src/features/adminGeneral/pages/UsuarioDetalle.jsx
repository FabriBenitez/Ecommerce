import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { detalleUsuarioAdmin } from "../api/adminGeneral.api";

export default function UsuarioDetalle() {
  const { id } = useParams();
  const [usuario, setUsuario] = useState(null);

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
            </div>
          )}
        </article>
      </section>
    </>
  );
}

import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import { confirmAction } from "@/shared/ui/sweetAlert";

function cx(...c) {
  return c.filter(Boolean).join(" ");
}

export default function AdminGeneralNavbar() {
  const { usuario, logout } = useAuth();
  const navigate = useNavigate();

  const onLogout = async () => {
    const confirmar = await confirmAction({
      title: "Cerrar sesion",
      text: "Vas a salir del sistema. Queres continuar?",
      confirmText: "Si, salir",
      cancelText: "No, seguir",
    });
    if (!confirmar) return;
    logout();
    navigate("/login");
  };

  return (
    <header className="agNav">
      <div className="agNav__inner">
        <div className="agNav__brand">
          <span className="agNav__brandIcon">📗</span>
          <span>Administrador General</span>
        </div>
        <nav className="agNav__links" aria-label="Admin General">
          <NavLink to="/admin-general" className={({ isActive }) => cx("agNav__link", isActive && "isActive")}>Inicio</NavLink>
          <NavLink to="/admin-general/caja" className={({ isActive }) => cx("agNav__link", isActive && "isActive")}>Caja</NavLink>
          <NavLink to="/admin-general/usuarios" className={({ isActive }) => cx("agNav__link", isActive && "isActive")}>Usuarios</NavLink>
          <NavLink to="/admin-general/reportes" className={({ isActive }) => cx("agNav__link", isActive && "isActive")}>Reportes</NavLink>
          <NavLink to="/admin-general/factura" className={({ isActive }) => cx("agNav__link", isActive && "isActive")}>Datos factura</NavLink>
        </nav>
        <div className="agNav__right">
          <button className="agNav__logout" type="button" onClick={onLogout}>
            Salir
          </button>
        </div>
      </div>
    </header>
  );
}

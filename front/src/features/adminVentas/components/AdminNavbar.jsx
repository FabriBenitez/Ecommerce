import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import { confirmAction } from "@/shared/ui/sweetAlert";
import "./AdminNavbar.css";

function cx(...c) {
  return c.filter(Boolean).join(" ");
}

export default function AdminNavbar() {
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
    <header className="aNav">
      <div className="aNav__inner">
        <div className="aNav__brand" onClick={() => navigate("/admin")} role="button" tabIndex={0}>
          <span className="aNav__dot" /> AdminVentas
        </div>

        <div className="aNav__searchWrap">
          <input className="aNav__search" placeholder="Buscar pedidos, libros..." />
        </div>

        <nav className="aNav__links" aria-label="Admin">
          <NavLink to="/admin" className={({ isActive }) => cx("aNav__link", isActive && "isActive")}>Dashboard</NavLink>
          <NavLink to="/admin/retiros" className={({ isActive }) => cx("aNav__link", isActive && "isActive")}>Pedidos retiro</NavLink>
          <NavLink to="/admin/pos" className={({ isActive }) => cx("aNav__link", isActive && "isActive")}>Mostrador</NavLink>
          <NavLink to="/admin/facturas" className={({ isActive }) => cx("aNav__link", isActive && "isActive")}>Facturas</NavLink>
        </nav>

        <div className="aNav__right">
          <span className="aNav__user" title={usuario?.email ?? ""}>{usuario?.email ?? "Admin"}</span>
          <button className="aNav__btn" onClick={onLogout}>Salir</button>
        </div>
      </div>
    </header>
  );
}

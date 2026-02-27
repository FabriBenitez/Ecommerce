import { NavLink, useNavigate } from "react-router-dom";
import { useMemo, useState } from "react";
import { useAuth } from "@/shared/auth/useAuth";
import { confirmAction } from "@/shared/ui/sweetAlert";
import "../styles/ComprasNavbar.css";
import "../styles/ComprasCommon.css";

function cx(...classes) {
  return classes.filter(Boolean).join(" ");
}

export default function ComprasNavbar() {
  const { usuario, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const email = useMemo(() => usuario?.email ?? "admin", [usuario]);

  const handleLogout = async () => {
    const confirmar = await confirmAction({
      title: "Cerrar sesion",
      text: "Vas a salir del sistema. Queres continuar?",
      confirmText: "Si, salir",
      cancelText: "No, seguir",
    });
    if (!confirmar) return;
    setOpen(false);
    logout();
    navigate("/login");
  };

  const linkClass = ({ isActive }) =>
    cx("cnav__link", isActive && "cnav__link--active");

  return (
    <header className="cnav">
      <div className="cnav__inner">
        <div className="cnav__left">
          <button
            className="cnav__burger"
            type="button"
            onClick={() => setOpen((s) => !s)}
            aria-label="Abrir menu"
            aria-expanded={open ? "true" : "false"}
          >
            {"\u2630"}
          </button>

          <NavLink to="/compras" className="cnav__brand">
            AdminCompras
          </NavLink>

          <nav className="cnav__links" aria-label="Navegacion Admin Compras">
            <NavLink to="/compras" className={linkClass} end>
              Dashboard
            </NavLink>
            <NavLink to="/compras/inventario" className={linkClass}>
              Inventario
            </NavLink>
            <NavLink to="/compras/proveedores" className={linkClass}>
              Proveedores
            </NavLink>
            <NavLink to="/compras/seguimiento" className={linkClass}>
              Compras
            </NavLink>
            <NavLink to="/compras/historial" className={linkClass}>
              Historial
            </NavLink>
          </nav>
        </div>

        <div className="cnav__right">
          <div className="cnav__searchWrap">
            <input
              className="cnav__search"
              placeholder="Buscar productos, ordenes..."
              onKeyDown={(e) => {
                if (e.key === "Enter") e.currentTarget.blur();
              }}
            />
          </div>

          <span className="cnav__user" title={email}>
            {email}
          </span>

          <button className="cbtn cbtn--primary cnav__logoutTop" onClick={handleLogout}>
            Logout
          </button>
        </div>
      </div>

      <div className={cx("cnav__drawer", open && "cnav__drawer--open")}>
        <nav className="cnav__drawerLinks" aria-label="Menu movil Admin Compras">
          <NavLink to="/compras" className={linkClass} end onClick={() => setOpen(false)}>
            Dashboard
          </NavLink>
          <NavLink to="/compras/inventario" className={linkClass} onClick={() => setOpen(false)}>
            Inventario
          </NavLink>
          <NavLink to="/compras/proveedores" className={linkClass} onClick={() => setOpen(false)}>
            Proveedores
          </NavLink>
          <NavLink to="/compras/seguimiento" className={linkClass} onClick={() => setOpen(false)}>
            Compras
          </NavLink>
          <NavLink to="/compras/historial" className={linkClass} onClick={() => setOpen(false)}>
            Historial
          </NavLink>

          <button className="cbtn cbtn--primary cnav__drawerBtn" onClick={handleLogout}>
            Logout
          </button>
        </nav>
      </div>
    </header>
  );
}

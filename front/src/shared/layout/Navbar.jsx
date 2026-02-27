import { NavLink, Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import { hasRole } from "@/shared/auth/roles";
import { confirmAction } from "@/shared/ui/sweetAlert";
import "./Navbar.css";

function cx(...classes) {
  return classes.filter(Boolean).join(" ");
}

export default function Navbar() {
  const { usuario, logout, isAuth } = useAuth();
  const navigate = useNavigate();
  const isAdminVentas = hasRole(usuario, "AdminVentas");
  const isAdminCompras = hasRole(usuario, "AdminCompras");
  const isAdminGeneral = hasRole(usuario, "AdminGeneral");
  const homeTo = isAdminGeneral ? "/admin-general" : isAdminCompras ? "/compras" : isAdminVentas ? "/admin" : "/catalogo";

  const handleLogout = async () => {
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
    <header className="nav">
      <div className="nav__inner">
        <Link className="nav__brand" to={homeTo}>
          Libreria
        </Link>

        <nav className="nav__links" aria-label="Principal">
          {!isAdminVentas && !isAdminCompras && !isAdminGeneral ? (
            <NavLink
              to="/catalogo"
              className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
            >
              Catalogo
            </NavLink>
          ) : null}

          {isAuth ? (
            <>
              {isAdminGeneral ? (
                <NavLink
                  to="/admin-general"
                  className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                >
                  Admin General
                </NavLink>
              ) : isAdminCompras ? (
                <NavLink
                  to="/compras"
                  className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                >
                  Compras
                </NavLink>
              ) : isAdminVentas ? (
                <>
                  <NavLink
                    to="/admin"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Dashboard
                  </NavLink>
                  <NavLink
                    to="/admin/retiros"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Retiros
                  </NavLink>
                  <NavLink
                    to="/admin/pos"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Mostrador
                  </NavLink>
                  <NavLink
                    to="/admin/facturas"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Facturas
                  </NavLink>
                </>
              ) : (
                <>
                  <NavLink
                    to="/promociones"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Promociones
                  </NavLink>
                  <NavLink
                    to="/carrito"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Carrito
                  </NavLink>

                  <NavLink
                    to="/mis-ventas"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Mis compras
                  </NavLink>
                  <NavLink
                    to="/mis-retiros"
                    className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
                  >
                    Retiros
                  </NavLink>
                </>
              )}
            </>
          ) : null}
        </nav>

        <div className="nav__right">
          {isAuth ? (
            <>
              <span className="nav__user" title={usuario?.email ?? ""}>
                {usuario?.email ?? "Usuario"}
              </span>
              <button className="nav__btn" onClick={handleLogout}>
                Salir
              </button>
            </>
          ) : (
            <div className="nav__auth">
              <NavLink
                to="/login"
                className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
              >
                Login
              </NavLink>
              <NavLink to="/registro" className="nav__btn nav__btn--ghost">
                Registro
              </NavLink>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}

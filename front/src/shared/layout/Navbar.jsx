import { NavLink, Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import "./Navbar.css";

function cx(...classes) {
  return classes.filter(Boolean).join(" ");
}

export default function Navbar() {
  const { usuario, logout, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <header className="nav">
      <div className="nav__inner">
        <Link className="nav__brand" to="/catalogo">
          Librería
        </Link>

        <nav className="nav__links" aria-label="Principal">
          <NavLink
            to="/catalogo"
            className={({ isActive }) => cx("nav__link", isActive && "nav__link--active")}
          >
            Catálogo
          </NavLink>

          {isAuthenticated ? (
            <>
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
            </>
          ) : null}
        </nav>

        <div className="nav__right">
          {isAuthenticated ? (
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


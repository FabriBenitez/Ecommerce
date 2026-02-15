import { Link } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import "./Navbar.css";

export default function Navbar() {
  const { usuario, logout } = useAuth();

  return (
    <header className="nav">
      <div className="nav__inner">
        <Link className="nav__brand" to="/catalogo">Librería</Link>

        <nav className="nav__links">
          <Link className="nav__link" to="/catalogo">Catálogo</Link>
          <Link className="nav__link" to="/carrito">Carrito</Link>

          {usuario ? (
            <>
              <Link className="nav__link" to="/ventas/mis-ventas">Mis compras</Link>
              <button className="nav__btn" onClick={logout}>Salir</button>
            </>
          ) : (
            <Link className="nav__btn nav__btn--link" to="/login">Ingresar</Link>
          )}
        </nav>
      </div>
    </header>
  );
}

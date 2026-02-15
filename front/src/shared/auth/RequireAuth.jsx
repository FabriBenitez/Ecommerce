import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../auth/useAuth";

export default function RequireAuth() {
  const { isAuth, cargandoPerfil } = useAuth();
  const location = useLocation();

  if (cargandoPerfil) return <div style={{ padding: 20 }}>Cargando...</div>;

  if (!isAuth) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}

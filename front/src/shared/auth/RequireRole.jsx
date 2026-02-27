import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import { getUserRoles, hasRole } from "@/shared/auth/roles";

export default function RequireRole({ role }) {
  const { isAuth, cargandoPerfil, usuario } = useAuth();
  const location = useLocation();

  if (cargandoPerfil) return <div style={{ padding: 20 }}>Cargando...</div>;
  if (!isAuth) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (!hasRole(usuario, role)) {
    const roles = getUserRoles(usuario);
    if (roles.includes("AdminVentas")) return <Navigate to="/admin" replace />;
    if (roles.includes("AdminCompras")) return <Navigate to="/compras" replace />;
    if (roles.includes("AdminGeneral")) return <Navigate to="/admin-general" replace />;
    return <Navigate to="/sin-acceso" replace />;
  }

  return <Outlet />;
}

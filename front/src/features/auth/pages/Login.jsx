import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import { getUserRoles } from "@/shared/auth/roles";
import AuthCard from "../components/AuthCard";
import LoginForm from "../components/LoginForm";
import "./AuthPage.css";

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();

  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const handleLogin = async ({ email, password }) => {
    try {
      setError(null);
      setLoading(true);

      await login({ email, password });
      const roles = getUserRoles();

      if (roles.includes("AdminVentas")) {
        navigate("/admin");
      } else if (roles.includes("AdminCompras")) {
        navigate("/compras");
      } else if (roles.includes("Cliente")) {
        navigate("/catalogo");
      } else {
        navigate("/sin-acceso");
      }
    } catch (e) {
      const apiError = e?.response?.data;
      const msg =
        typeof apiError === "string"
          ? apiError
          : apiError?.error ?? e?.message ?? "No se pudo iniciar sesion.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="authPage">
      <div className="authPage__container">
        <AuthCard
          title="Iniciar sesion"
          subtitle="Ingresa con tu email y contrasena para comprar y ver tus ventas."
        >
          <LoginForm onSubmit={handleLogin} isLoading={loading} error={error} />
        </AuthCard>
      </div>
    </main>
  );
}

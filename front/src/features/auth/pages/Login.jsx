import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
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

      // Después lo podés mandar a /catalogo o /carrito
      navigate("/catalogo");
    } catch (e) {
      setError(e?.message ?? "No se pudo iniciar sesión.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="authPage">
      <div className="authPage__container">
        <AuthCard
          title="Iniciar sesión"
          subtitle="Ingresá con tu email y contraseña para comprar y ver tus ventas."
        >
          <LoginForm onSubmit={handleLogin} isLoading={loading} error={error} />
        </AuthCard>
      </div>
    </main>
  );
}

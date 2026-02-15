import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "@/shared/auth/useAuth";
import AuthCard from "../components/AuthCard";
import "./AuthPage.css";
import "../components/LoginForm.css"; // reutilizamos estilos base

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [nombreCompleto, setNombreCompleto] = useState("");
  const [telefono, setTelefono] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e) => {
    e.preventDefault();
    setError(null);

    try {
      setLoading(true);

      // Ajustá nombres EXACTOS según tu RegisterDto backend
      await register({ nombreCompleto, telefono, email, password });

      navigate("/login");
    } catch {
      setError("No se pudo registrar (email existente o error).");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="authPage">
      <div className="authPage__container">
        <AuthCard
          title="Crear cuenta"
          subtitle="Registrate para poder comprar, pagar y seguir tus pedidos."
        >
          <form className="authForm" onSubmit={onSubmit}>
            <div className="authForm__field">
              <label className="authForm__label">Nombre completo</label>
              <input
                className="authForm__input"
                value={nombreCompleto}
                onChange={(e) => setNombreCompleto(e.target.value)}
                placeholder="Tu nombre y apellido"
                required
              />
            </div>

            <div className="authForm__field">
              <label className="authForm__label">Teléfono</label>
              <input
                className="authForm__input"
                value={telefono}
                onChange={(e) => setTelefono(e.target.value)}
                placeholder="Ej: 11 1234 5678"
              />
            </div>

            <div className="authForm__field">
              <label className="authForm__label">Email</label>
              <input
                className="authForm__input"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="tu@email.com"
                required
              />
            </div>

            <div className="authForm__field">
              <label className="authForm__label">Contraseña</label>
              <input
                className="authForm__input"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
              />
            </div>

            {error ? <p className="authForm__error">{error}</p> : null}

            <button className="authForm__button" disabled={loading}>
              {loading ? "Creando cuenta..." : "Crear cuenta"}
            </button>

            <div className="authForm__links">
              <span />
              <Link className="authForm__link" to="/login">Ya tengo cuenta</Link>
            </div>
          </form>
        </AuthCard>
      </div>
    </main>
  );
}

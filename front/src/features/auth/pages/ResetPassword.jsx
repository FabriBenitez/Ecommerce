import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import AuthCard from "../components/AuthCard";
import authApi from "../api/auth.api";
import "./AuthPage.css";
import "../components/LoginForm.css";
import "./ResetPassword.css";

export default function ResetPassword() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const emailParam = params.get("email") ?? "";
  const tokenParam = params.get("token") ?? "";

  const [email, setEmail] = useState(emailParam);
  const [token, setToken] = useState(tokenParam);
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const hasLinkData = Boolean(emailParam && tokenParam);

  const onSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setMessage("");

    if (newPassword.length < 6) {
      setError("La nueva contraseña debe tener al menos 6 caracteres.");
      return;
    }
    if (newPassword !== confirmPassword) {
      setError("Las contraseñas no coinciden.");
      return;
    }

    try {
      setLoading(true);
      const { data } = await authApi.resetPassword({
        email,
        token,
        newPassword,
        confirmPassword,
      });
      setMessage(data?.message ?? "Contraseña actualizada correctamente.");
      setTimeout(() => navigate("/login"), 1200);
    } catch (err) {
      const apiError = err?.response?.data;
      const msg = typeof apiError === "string"
        ? apiError
        : apiError?.error ?? err?.message ?? "No se pudo restablecer la contraseña.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="authPage authPage--reset">
      <div className="authPage__container">
        <AuthCard
          title="Nueva contraseña"
          subtitle="Defini una clave nueva para recuperar el acceso a tu cuenta."
        >
          <form className="authForm authForm--reset" onSubmit={onSubmit}>
            {hasLinkData ? (
              <div className="resetMeta">
                <p className="resetMeta__title">Solicitud detectada</p>
                <div className="resetMeta__row">
                  <span className="resetMeta__label">Cuenta</span>
                  <span className="resetMeta__value" title={email}>{email}</span>
                </div>
              </div>
            ) : null}

            {!hasLinkData ? (
              <div className="authForm__field">
                <label className="authForm__label" htmlFor="email">Email</label>
                <input
                  id="email"
                  className="authForm__input"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>
            ) : null}

            {!hasLinkData ? (
              <div className="authForm__field">
                <label className="authForm__label" htmlFor="token">Token</label>
                <input
                  id="token"
                  className="authForm__input"
                  value={token}
                  onChange={(e) => setToken(e.target.value)}
                  required
                />
              </div>
            ) : null}

            <div className="authForm__field">
              <label className="authForm__label" htmlFor="newPassword">Nueva contraseña</label>
              <input
                id="newPassword"
                className="authForm__input"
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Minimo 6 caracteres"
                required
              />
            </div>

            <div className="authForm__field">
              <label className="authForm__label" htmlFor="confirmPassword">Confirmar contraseña</label>
              <input
                id="confirmPassword"
                className="authForm__input"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Repite tu nueva contraseña"
                required
              />
            </div>

            {error ? <p className="authForm__error">{error}</p> : null}
            {message ? <p className="resetSuccess">{message}</p> : null}

            <button className="authForm__button authForm__button--reset" disabled={loading}>
              {loading ? "Guardando..." : "Actualizar contraseña"}
            </button>

            <div className="authForm__links">
              <span />
              <Link className="authForm__link" to="/login">Volver al login</Link>
            </div>
          </form>
        </AuthCard>
      </div>
    </main>
  );
}

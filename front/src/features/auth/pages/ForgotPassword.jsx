import { useState } from "react";
import { Link } from "react-router-dom";
import AuthCard from "../components/AuthCard";
import authApi from "../api/auth.api";
import "./AuthPage.css";
import "../components/LoginForm.css";
import "./ForgotPassword.css";

export default function ForgotPassword() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [devResetLink, setDevResetLink] = useState("");
  const showMessage = Boolean(message) && !message.toLowerCase().includes("smtp no configurado");

  const onSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setMessage("");
    setDevResetLink("");

    try {
      setLoading(true);
      const { data } = await authApi.forgotPassword({ email });
      setMessage(data?.message ?? "Si el correo existe, enviamos instrucciones para recuperar la cuenta.");
      setDevResetLink(data?.devResetLink ?? "");
    } catch (err) {
      const apiError = err?.response?.data;
      const msg = typeof apiError === "string"
        ? apiError
        : apiError?.error ?? err?.message ?? "No se pudo iniciar el recupero.";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="authPage">
      <div className="authPage__container">
        <AuthCard
          title="Recuperar contrasena"
          subtitle="Ingresa tu correo y te enviaremos un enlace para restablecerla."
        >
          <form className="authForm" onSubmit={onSubmit}>
            <div className="authForm__field">
              <label className="authForm__label" htmlFor="email">Email</label>
              <input
                id="email"
                className="authForm__input"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="tu@email.com"
                required
              />
            </div>

            {error ? <p className="authForm__error">{error}</p> : null}
            {showMessage ? <p className="authForm__label">{message}</p> : null}
            {devResetLink ? (
              <div className="fpDevLink">
                <p className="fpDevLink__title">Link de recupero listo</p>
                <a className="fpDevLink__btn" href={devResetLink}>
                  Abrir recupero
                </a>
              </div>
            ) : null}

            <button className="authForm__button" disabled={loading}>
              {loading ? "Enviando..." : "Enviar enlace"}
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

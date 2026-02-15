import { useState } from "react";
import { Link } from "react-router-dom";
import "./LoginForm.css";

export default function LoginForm({ onSubmit, isLoading = false, error = null }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = (e) => {
    e.preventDefault();
    onSubmit?.({ email, password });
  };

  return (
    <form className="authForm" onSubmit={handleSubmit}>
      <div className="authForm__field">
        <label className="authForm__label" htmlFor="email">Email</label>
        <input
          id="email"
          className="authForm__input"
          type="email"
          autoComplete="email"
          placeholder="tu@email.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
      </div>

      <div className="authForm__field">
        <label className="authForm__label" htmlFor="password">Contraseña</label>
        <input
          id="password"
          className="authForm__input"
          type="password"
          autoComplete="current-password"
          placeholder="••••••••"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
      </div>

      {error ? <p className="authForm__error">{error}</p> : null}

      <button className="authForm__button" disabled={isLoading}>
        {isLoading ? "Ingresando..." : "Entrar"}
      </button>

      <div className="authForm__links">
        <Link className="authForm__link" to="/registro">Crear cuenta</Link>
        <Link className="authForm__link" to="/recuperar">¿Olvidaste tu contraseña?</Link>
      </div>
    </form>
  );
}

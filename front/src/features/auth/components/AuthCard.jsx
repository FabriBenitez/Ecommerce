import "./AuthCard.css";

export default function AuthCard({ title, subtitle, children }) {
  return (
    <section className="authCard" aria-label={title}>
      <header className="authCard__header">
        <h1 className="authCard__title">{title}</h1>
        {subtitle ? <p className="authCard__subtitle">{subtitle}</p> : null}
      </header>

      <div className="authCard__content">{children}</div>
    </section>
  );
}

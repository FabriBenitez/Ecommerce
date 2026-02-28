import { useState } from "react";
import "./CheckoutForm.css";

export default function CheckoutForm({
  onSubmit,
  isLoading = false,
  error = null,
  initial = {},
}) {
  const [clienteDni, setClienteDni] = useState(initial.clienteDni ?? "");
  const [observaciones, setObservaciones] = useState(initial.observaciones ?? "");

  const handleSubmit = (e) => {
    e.preventDefault();
    onSubmit?.({
      clienteDni,
      observaciones: observaciones || null,
    });
  };

  return (
    <form className="checkoutForm" onSubmit={handleSubmit}>
      <header className="checkoutForm__header">
        <h1 className="checkoutForm__title">Checkout</h1>
        <p className="checkoutForm__subtitle">
          Retiro presencial: solo DNI y observaciones para generar el link de pago.
        </p>
      </header>

      <div className="checkoutForm__grid">
        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="clienteDni">DNI cliente</label>
          <input
            id="clienteDni"
            className="checkoutForm__input"
            value={clienteDni}
            onChange={(e) => setClienteDni(e.target.value)}
            placeholder="30123456"
            required
          />
        </div>

        <div className="checkoutForm__field checkoutForm__field--full">
          <label className="checkoutForm__label" htmlFor="observaciones">Observaciones (opcional)</label>
          <textarea
            id="observaciones"
            className="checkoutForm__textarea"
            value={observaciones}
            onChange={(e) => setObservaciones(e.target.value)}
            placeholder="Ej: retiro por la tarde"
            rows={3}
          />
        </div>
      </div>

      {error ? <p className="checkoutForm__error">{error}</p> : null}

      <button className="checkoutForm__button" disabled={isLoading}>
        {isLoading ? "Generando pago..." : "Generar link de pago"}
      </button>
    </form>
  );
}

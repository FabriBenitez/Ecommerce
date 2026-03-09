import { useState } from "react";
import "./CheckoutForm.css";

const MAX_DNI = 20;
const MAX_NOMBRE = 200;
const MAX_TELEFONO = 50;

export default function CheckoutForm({
  onSubmit,
  isLoading = false,
  error = null,
  initial = {},
}) {
  const [clienteDni, setClienteDni] = useState(initial.clienteDni ?? "");
  const [clienteNombre, setClienteNombre] = useState(initial.clienteNombre ?? "");
  const [clienteTelefono, setClienteTelefono] = useState(initial.clienteTelefono ?? "");
  const [observaciones, setObservaciones] = useState(initial.observaciones ?? "");

  const handleSubmit = (e) => {
    e.preventDefault();
    onSubmit?.({
      clienteDni: clienteDni.replace(/\D/g, "").slice(0, MAX_DNI),
      nombreEntrega: clienteNombre.trim().slice(0, MAX_NOMBRE),
      telefonoEntrega: clienteTelefono.trim().slice(0, MAX_TELEFONO),
      observaciones: observaciones || null,
    });
  };

  return (
    <form className="checkoutForm" onSubmit={handleSubmit}>
      <header className="checkoutForm__header">
        <h1 className="checkoutForm__title">Checkout</h1>
        <p className="checkoutForm__subtitle">
          Ingrese sus datos para el retiro presencial. DNI, nombre completo y telefono son obligatorios.
        </p>
      </header>

      <div className="checkoutForm__grid">
        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="clienteDni">Ingrese su DNI *</label>
          <input
            id="clienteDni"
            className="checkoutForm__input"
            value={clienteDni}
            onChange={(e) => setClienteDni(e.target.value.replace(/\D/g, "").slice(0, MAX_DNI))}
            placeholder="Ingrese su DNI"
            maxLength={MAX_DNI}
            required
          />
        </div>

        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="clienteNombre">Nombre completo *</label>
          <input
            id="clienteNombre"
            className="checkoutForm__input"
            value={clienteNombre}
            onChange={(e) => setClienteNombre(e.target.value.slice(0, MAX_NOMBRE))}
            placeholder="Ingrese su nombre completo"
            maxLength={MAX_NOMBRE}
            required
          />
        </div>

        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="clienteTelefono">Telefono *</label>
          <input
            id="clienteTelefono"
            className="checkoutForm__input"
            value={clienteTelefono}
            onChange={(e) => setClienteTelefono(e.target.value.replace(/[^\d+\-()\s]/g, "").slice(0, MAX_TELEFONO))}
            placeholder="Ingrese su telefono"
            maxLength={MAX_TELEFONO}
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

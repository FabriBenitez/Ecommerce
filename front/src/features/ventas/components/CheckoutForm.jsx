import { useState } from "react";
import "./CheckoutForm.css";

export default function CheckoutForm({
  onSubmit,
  isLoading = false,
  error = null,
  initial = {},
}) {
  const [nombreEntrega, setNombreEntrega] = useState(initial.nombreEntrega ?? "");
  const [telefonoEntrega, setTelefonoEntrega] = useState(initial.telefonoEntrega ?? "");
  const [direccionEntrega, setDireccionEntrega] = useState(initial.direccionEntrega ?? "");
  const [ciudad, setCiudad] = useState(initial.ciudad ?? "");
  const [provincia, setProvincia] = useState(initial.provincia ?? "");
  const [codigoPostal, setCodigoPostal] = useState(initial.codigoPostal ?? "");
  const [observaciones, setObservaciones] = useState(initial.observaciones ?? "");

  const handleSubmit = (e) => {
    e.preventDefault();
    onSubmit?.({
      nombreEntrega,
      telefonoEntrega,
      direccionEntrega,
      ciudad,
      provincia,
      codigoPostal,
      observaciones: observaciones || null,
    });
  };

  return (
    <form className="checkoutForm" onSubmit={handleSubmit}>
      <header className="checkoutForm__header">
        <h1 className="checkoutForm__title">Checkout</h1>
        <p className="checkoutForm__subtitle">
          Completá los datos de entrega y generá el link de pago.
        </p>
      </header>

      <div className="checkoutForm__grid">
        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="nombreEntrega">Nombre entrega</label>
          <input
            id="nombreEntrega"
            className="checkoutForm__input"
            value={nombreEntrega}
            onChange={(e) => setNombreEntrega(e.target.value)}
            placeholder="Nombre y apellido"
            required
          />
        </div>

        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="telefonoEntrega">Teléfono</label>
          <input
            id="telefonoEntrega"
            className="checkoutForm__input"
            value={telefonoEntrega}
            onChange={(e) => setTelefonoEntrega(e.target.value)}
            placeholder="11 1234-5678"
            required
          />
        </div>

        <div className="checkoutForm__field checkoutForm__field--full">
          <label className="checkoutForm__label" htmlFor="direccionEntrega">Dirección</label>
          <input
            id="direccionEntrega"
            className="checkoutForm__input"
            value={direccionEntrega}
            onChange={(e) => setDireccionEntrega(e.target.value)}
            placeholder="Calle 123, piso/depto"
            required
          />
        </div>

        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="ciudad">Ciudad</label>
          <input
            id="ciudad"
            className="checkoutForm__input"
            value={ciudad}
            onChange={(e) => setCiudad(e.target.value)}
            placeholder="Ciudad"
            required
          />
        </div>

        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="provincia">Provincia</label>
          <input
            id="provincia"
            className="checkoutForm__input"
            value={provincia}
            onChange={(e) => setProvincia(e.target.value)}
            placeholder="Provincia"
            required
          />
        </div>

        <div className="checkoutForm__field">
          <label className="checkoutForm__label" htmlFor="codigoPostal">Código postal</label>
          <input
            id="codigoPostal"
            className="checkoutForm__input"
            value={codigoPostal}
            onChange={(e) => setCodigoPostal(e.target.value)}
            placeholder="C1000"
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
            placeholder="Ej: tocar timbre, horario, referencias…"
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

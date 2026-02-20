import { useState } from "react";
import { Link } from "react-router-dom";
import { ventasApi } from "../api/ventas.api";
import CheckoutForm from "../components/CheckoutForm";
import "../pages/Checkout.css";



export default function Checkout() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const [resultado, setResultado] = useState(null);
  // resultado esperado:
  // { ventaId, preferenceId, urlPago, total }

  const onSubmit = async (payload) => {
    try {
      setError("");
      setLoading(true);
      setResultado(null);

      const { data } = await ventasApi.checkoutWeb(payload);
      setResultado(data);
    } catch (e) {
      // si backend devuelve mensaje, lo mostramos
      const validationErrors = e?.response?.data?.errors;
      const validationMsg = validationErrors
        ? Object.values(validationErrors).flat().join(" | ")
        : null;
      const msg =
        validationMsg ||
        e?.response?.data?.message ||
        e?.response?.data ||
        e?.message ||
        "No se pudo generar el checkout.";
      setError(String(msg));
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="checkoutPage">
      <div className="checkoutPage__topbar">
        <Link className="checkoutPage__link" to="/carrito">← Volver al carrito</Link>
        <Link className="checkoutPage__link" to="/catalogo">Catálogo</Link>
      </div>

      <div className="checkoutPage__layout">
        <section className="checkoutPage__main">
          <CheckoutForm onSubmit={onSubmit} isLoading={loading} error={error} />
        </section>

        <aside className="checkoutPage__aside">
          <div className="checkoutAsideCard">
            <h2 className="checkoutAsideCard__title">Pago</h2>

            {!resultado ? (
              <p className="checkoutAsideCard__text">
                Generá el link y luego vas a poder abrir Mercado Pago.
              </p>
            ) : (
              <>
                <div className="checkoutAsideCard__row">
                  <span>Venta</span>
                  <strong>#{resultado.ventaId}</strong>
                </div>

                <div className="checkoutAsideCard__row">
                  <span>Total</span>
                  <strong>${Number(resultado.total ?? 0).toFixed(2)} ARS</strong>
                </div>

                <a
                  className="checkoutAsideCard__payBtn"
                  href={resultado.urlPago}
                  target="_blank"
                  rel="noreferrer"
                >
                  Pagar con Mercado Pago
                </a>

                <p className="checkoutAsideCard__hint">
                  Se abre en una pestaña nueva.
                </p>

              </>
            )}
          </div>
        </aside>
      </div>
    </main>
  );
}

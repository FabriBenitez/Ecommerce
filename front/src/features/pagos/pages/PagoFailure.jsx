import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ventasApi } from "@/features/ventas/api/ventas.api";
import "./PagoResultado.css";

export default function PagoFailure() {
  const [searchParams] = useSearchParams();
  const ventaIdQuery = searchParams.get("ventaId");

  const [venta, setVenta] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const run = async () => {
      try {
        setError("");
        setLoading(true);

        const ventaId = ventaIdQuery || localStorage.getItem("ultimaVentaId");
        if (!ventaId) return; // failure puede venir sin venta

        const { data } = await ventasApi.obtenerVenta(ventaId);
        setVenta(data);
      } catch {
        // si falla, no pasa nada: mostramos mensaje genérico
      } finally {
        setLoading(false);
      }
    };

    run();
  }, [ventaIdQuery]);

  return (
    <main className="pagoResultado">
      <section className="pagoResultado__card">
        <h1 className="pagoResultado__title">❌ Pago no completado</h1>
        <p className="pagoResultado__subtitle">
          El pago fue cancelado o rechazado. Podés intentarlo de nuevo desde el checkout.
        </p>

        {loading ? <p className="pagoResultado__info">Cargando...</p> : null}
        {error ? <p className="pagoResultado__error">{error}</p> : null}

        {venta ? (
          <div className="pagoResultado__box">
            <p><b>Venta:</b> #{venta.id}</p>
            <p><b>Estado actual:</b> {venta.estadoVenta}</p>
            <p><b>Total:</b> ${venta.total}</p>
          </div>
        ) : null}

        <div className="pagoResultado__actions">
          <Link className="pagoResultado__btn" to="/checkout">
            Volver al checkout
          </Link>
          <Link className="pagoResultado__btn pagoResultado__btn--ghost" to="/catalogo">
            Volver al catálogo
          </Link>
        </div>
      </section>
    </main>
  );
}

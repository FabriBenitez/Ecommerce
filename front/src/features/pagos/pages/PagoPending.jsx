import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ventasApi } from "@/features/ventas/api/ventas.api";
import "./PagoResultado.css";

export default function PagoPending() {
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
        if (!ventaId) {
          setError("No se encontró el ID de la venta.");
          return;
        }

        const { data } = await ventasApi.obtenerVenta(ventaId);
        setVenta(data);
      } catch (e) {
        setError(e?.response?.data ?? "No se pudo consultar la venta.");
      } finally {
        setLoading(false);
      }
    };

    run();
  }, [ventaIdQuery]);

  return (
    <main className="pagoResultado">
      <section className="pagoResultado__card">
        <h1 className="pagoResultado__title">⏳ Pago pendiente</h1>
        <p className="pagoResultado__subtitle">
          El pago todavía no quedó aprobado. Puede tardar unos minutos (o quedar pendiente).
        </p>

        {loading ? <p className="pagoResultado__info">Consultando venta...</p> : null}
        {error ? <p className="pagoResultado__error">{error}</p> : null}

        {venta ? (
          <div className="pagoResultado__box">
            <p><b>Venta:</b> #{venta.id}</p>
            <p><b>Estado:</b> {venta.estadoVenta}</p>
            <p><b>Total:</b> ${venta.total}</p>

            <div className="pagoResultado__actions">
              <Link className="pagoResultado__btn" to={`/ventas/${venta.id}`}>
                Ver detalle
              </Link>
              <Link className="pagoResultado__btn pagoResultado__btn--ghost" to="/catalogo">
                Volver al catálogo
              </Link>
            </div>
          </div>
        ) : null}
      </section>
    </main>
  );
}

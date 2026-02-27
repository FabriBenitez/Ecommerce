import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { listarPromocionesCliente } from "../api/promocionesCliente.api";
import "./PromocionesCliente.css";

export default function PromocionesCliente() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        const data = await listarPromocionesCliente();
        if (!alive) return;
        setItems(Array.isArray(data) ? data : []);
      } catch (e) {
        if (!alive) return;
        const status = e?.response?.status;
        if (status === 404) {
          setError("No se encontraron promociones para cliente (endpoint no disponible). Reinicia backend.");
        } else if (status === 401 || status === 403) {
          setError("No autorizado para ver promociones. Cierra sesion e inicia nuevamente.");
        } else {
          setError("No se pudieron cargar las promociones. Si acabas de actualizar backend, reinicialo e inicia sesion nuevamente.");
        }
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, []);

  return (
    <main className="promoPage">
      <header className="promoHead">
        <h1>Promociones para vos</h1>
        <p>Ofertas activas disponibles para tu cuenta.</p>
      </header>

      <div className="promoActions">
        <Link to="/catalogo" className="promoBtn">
          Ver catalogo
        </Link>
      </div>

      {loading ? <p className="promoState">Cargando promociones...</p> : null}
      {error ? <p className="promoState promoState--error">{error}</p> : null}

      {!loading && !error ? (
        items.length === 0 ? (
          <section className="promoEmpty">
            <p>No hay promociones activas por ahora.</p>
          </section>
        ) : (
          <section className="promoGrid">
            {items.map((p) => (
              <article key={p.id} className="promoCard">
                <h3>{p.nombre}</h3>
                <p className="promoName">{p.productoNombre ? `Aplica a: ${p.productoNombre}` : "Promocion general"}</p>
                <p className="promoDate">
                  {new Date(p.fechaInicio).toLocaleDateString("es-AR")} - {new Date(p.fechaFin).toLocaleDateString("es-AR")}
                </p>
              </article>
            ))}
          </section>
        )
      ) : null}
    </main>
  );
}

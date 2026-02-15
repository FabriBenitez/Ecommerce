import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ventasApi } from "@/features/ventas/api/ventas.api.js";
import "./Ventas.css";

export default function MisVentas() {
  const [ventas, setVentas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const run = async () => {
      try {
        setError("");
        setLoading(true);
        const { data } = await ventasApi.misVentas();
        setVentas(Array.isArray(data) ? data : []);
      } catch (e) {
        setError(e?.response?.data ?? "No se pudieron cargar tus ventas.");
      } finally {
        setLoading(false);
      }
    };
    run();
  }, []);

  return (
    <main className="ventasPage">
      <header className="ventasHeader">
        <h1 className="ventasHeader__title">Mis compras</h1>
        <p className="ventasHeader__subtitle">Historial de ventas web (y futuras presenciales si aplica).</p>
      </header>

      {loading ? <p className="ventasInfo">Cargando...</p> : null}
      {error ? <p className="ventasError">{error}</p> : null}

      {!loading && !error && ventas.length === 0 ? (
        <div className="ventasEmpty">
          <p>No tenés compras registradas.</p>
          <Link className="ventasBtn" to="/catalogo">Ir al catálogo</Link>
        </div>
      ) : null}

      {ventas.length > 0 ? (
        <section className="ventasList">
          {ventas.map((v) => (
            <article key={v.id} className="ventaCard">
              <div className="ventaCard__top">
                <div>
                  <p className="ventaCard__id">Venta #{v.id}</p>
                  <p className="ventaCard__date">
                    {new Date(v.fecha).toLocaleString()}
                  </p>
                </div>

                <span className={`ventaBadge ventaBadge--${String(v.estadoVenta).toLowerCase()}`}>
                  {v.estadoVenta}
                </span>
              </div>

              <div className="ventaCard__bottom">
                <p className="ventaCard__total">
                  Total: <b>${v.total}</b>
                </p>

                <p className="ventaCard__canal">
                  Canal: <b>{v.canal}</b>
                </p>

                <Link className="ventaCard__link" to={`/ventas/${v.id}`}>
                  Ver detalle →
                </Link>
              </div>
            </article>
          ))}
        </section>
      ) : null}
    </main>
  );
}

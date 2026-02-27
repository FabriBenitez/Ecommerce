import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { misVentas } from "../api/ventas.api";
import "./MisRetiros.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

const RETIRO_LABEL = {
  1: "Pendiente",
  2: "Preparando",
  3: "Listo para retirar",
  4: "Entregado",
};

export default function MisRetiros() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setError("");
        setLoading(true);
        const data = await misVentas();
        if (!alive) return;
        setItems(Array.isArray(data) ? data : []);
      } catch (e) {
        if (!alive) return;
        setError(e?.message ?? "No se pudo cargar el estado de retiros.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, []);

  const retiros = useMemo(
    () => items.filter((v) => Number(v.estadoVenta) === 2),
    [items]
  );
  const listos = useMemo(
    () => retiros.filter((v) => Number(v.estadoRetiro) === 3),
    [retiros]
  );

  return (
    <main className="misRetirosPage">
      <header className="misRetirosHead">
        <h1>Mis retiros</h1>
        <p>Seguimiento de pedidos para retirar en tienda.</p>
      </header>

      {listos.length > 0 ? (
        <section className="retiroReady">
          Tenes {listos.length} pedido{listos.length > 1 ? "s" : ""} listo{listos.length > 1 ? "s" : ""} para retirar.
        </section>
      ) : null}

      {loading ? <p className="misRetirosState">Cargando...</p> : null}
      {error ? <p className="misRetirosState misRetirosState--error">{error}</p> : null}

      {!loading && !error ? (
        retiros.length === 0 ? (
          <section className="misRetirosEmpty">
            <p>No tenes pedidos para retiro por ahora.</p>
            <Link className="misRetirosLink" to="/catalogo">
              Ir al catalogo
            </Link>
          </section>
        ) : (
          <section className="misRetirosCard">
            <div className="misRetirosTableWrap">
              <table className="misRetirosTable">
                <thead>
                  <tr>
                    <th>Venta</th>
                    <th>Fecha</th>
                    <th>Total</th>
                    <th>Estado retiro</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {retiros.map((v) => (
                    <tr key={v.id}>
                      <td>#{v.id}</td>
                      <td>{new Date(v.fecha).toLocaleString("es-AR")}</td>
                      <td>{money.format(v.total)}</td>
                      <td>
                        <span className={`retiroBadge s${v.estadoRetiro}`}>
                          {RETIRO_LABEL[v.estadoRetiro] ?? `Estado ${v.estadoRetiro}`}
                        </span>
                      </td>
                      <td>
                        <Link className="misRetirosLink" to={`/ventas/${v.id}`}>
                          Ver detalle
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )
      ) : null}
    </main>
  );
}

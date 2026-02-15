import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { misVentas } from "../api/ventas.api";
import "./MisVentas.css";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

function estadoLabel(e) {
  // si tu enum llega como número, podés mapearlo acá. Si llega como string, mostrala directo.
  return typeof e === "string" ? e : `Estado ${e}`;
}

function canalLabel(c) {
  return typeof c === "string" ? c : `Canal ${c}`;
}

export default function MisVentas() {
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
        setError(e?.message ?? "No se pudo cargar Mis Ventas.");
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => { alive = false; };
  }, []);

  return (
    <main className="misVentasPage">
      <header className="misVentasHeader">
        <h1 className="misVentasHeader__title">Mis compras</h1>
        <p className="misVentasHeader__subtitle">Historial de tus ventas web.</p>
      </header>

      {loading ? <p className="misVentasState">Cargando…</p> : null}
      {error ? <p className="misVentasState misVentasState--error">{error}</p> : null}

      {!loading && !error ? (
        items.length === 0 ? (
          <section className="misVentasEmpty">
            <p>No tenés compras todavía.</p>
            <Link className="misVentasLink" to="/catalogo">Ir al catálogo</Link>
          </section>
        ) : (
          <section className="misVentasCard">
            <div className="misVentasTableWrap">
              <table className="misVentasTable">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Fecha</th>
                    <th>Total</th>
                    <th>Estado</th>
                    <th>Canal</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((v) => (
                    <tr key={v.id}>
                      <td className="mono">#{v.id}</td>
                      <td>{new Date(v.fecha).toLocaleString("es-AR")}</td>
                      <td className="money">{money.format(v.total)}</td>
                      <td>{estadoLabel(v.estadoVenta)}</td>
                      <td>{canalLabel(v.canal)}</td>
                      <td className="right">
                        <Link className="btnLink" to={`/ventas/${v.id}`}>
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

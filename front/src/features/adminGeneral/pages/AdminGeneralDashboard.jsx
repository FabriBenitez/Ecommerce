import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { reporteStock, resumenCaja } from "../api/adminGeneral.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

export default function AdminGeneralDashboard() {
  const [caja, setCaja] = useState(null);
  const [stock, setStock] = useState([]);

  useEffect(() => {
    resumenCaja().then(setCaja).catch(() => setCaja(null));
    reporteStock().then(setStock).catch(() => setStock([]));
  }, []);

  const saldoActual = useMemo(() => (caja?.saldoActual ?? 0), [caja]);
  const ingresos = useMemo(() => (caja?.ingresos ?? 0), [caja]);
  const egresos = useMemo(() => (caja?.egresos ?? 0), [caja]);
  const stockRows = stock.slice(0, 4);

  return (
    <>
      <section className="agPageHead">
        <h1>Dashboard General</h1>
        <p>Monitoreo en tiempo real del sistema y la libreria.</p>
      </section>

      <section className="agSection">
        <div className="agSection__head">
          <h2>Estado de Caja Hoy</h2>
        </div>
        <div className="agKpis">
          <article className="agKpi">
            <label>Estado actual</label>
            <strong>{caja?.abierta ? "Abierta" : "Cerrada"}</strong>
          </article>
          <article className="agKpi">
            <label>Saldo inicial</label>
            <strong>{money.format(caja?.saldoInicial ?? 0)}</strong>
          </article>
          <article className="agKpi agKpi--accent">
            <label>Ingresos / Egresos</label>
            <strong>{money.format(ingresos - egresos)}</strong>
          </article>
          <article className="agKpi agKpi--ok">
            <label>Saldo actual</label>
            <strong>{money.format(saldoActual)}</strong>
          </article>
        </div>
      </section>

      <section className="agSection">
        <div className="agGrid agGrid--2">
          <article className="agCard">
            <div className="agSection__head">
              <h3>Alertas de Stock Bajo</h3>
              <Link to="/admin-general/reportes" className="agNav__link isActive">Ver inventario completo</Link>
            </div>
            <div className="agTableWrap">
              <table className="agTable">
                <thead>
                  <tr>
                    <th>Libro / Articulo</th>
                    <th>Stock</th>
                    <th>Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {stockRows.length === 0 ? (
                    <tr>
                      <td colSpan={3}>Sin alertas de stock.</td>
                    </tr>
                  ) : (
                    stockRows.map((item) => (
                      <tr key={item.productoId}>
                        <td>{item.producto}</td>
                        <td>{item.stockActual}</td>
                        <td>
                          <span className={`agBadge ${item.stockActual <= 3 ? "agBadge--danger" : "agBadge--warn"}`}>
                            {item.stockActual <= 3 ? "Critico" : "Bajo"}
                          </span>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </article>

          <article className="agCard agCard--soft">
            <h3>Accesos Rapidos</h3>
            <div className="agQuickList">
              <Link className="agQuick" to="/admin-general/usuarios">
                <div className="agQuick__icon">+</div>
                <div><h4>Crear Usuario</h4><p>Nuevo personal o admin</p></div>
              </Link>
              <Link className="agQuick" to="/admin-general/caja">
                <div className="agQuick__icon">$</div>
                <div><h4>Abrir Caja</h4><p>Registrar inicio de jornada</p></div>
              </Link>
              <Link className="agQuick" to="/admin-general/promociones">
                <div className="agQuick__icon">%</div>
                <div><h4>Nueva Promo</h4><p>Configurar descuentos</p></div>
              </Link>
              <Link className="agQuick" to="/admin-general/factura">
                <div className="agQuick__icon">#</div>
                <div><h4>Configurar factura</h4><p>Datos globales de comprobante</p></div>
              </Link>
            </div>
          </article>
        </div>
      </section>
    </>
  );
}

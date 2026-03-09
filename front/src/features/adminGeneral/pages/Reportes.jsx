import { useEffect, useMemo, useState } from "react";
import { reporteCaja, reporteEjecutivo, reporteStock, reporteVentas } from "../api/adminGeneral.api";

const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

const PERIODOS = [
  { id: 7, label: "7 dias" },
  { id: 30, label: "30 dias" },
  { id: 90, label: "90 dias" },
];

function inicioDia(value) {
  const d = new Date(value);
  d.setHours(0, 0, 0, 0);
  return d;
}

function finDia(value) {
  const d = new Date(value);
  d.setHours(23, 59, 59, 999);
  return d;
}

export default function Reportes() {
  const [dias, setDias] = useState(30);
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState("");
  const [info, setInfo] = useState("");

  useEffect(() => {
    let alive = true;

    (async () => {
      try {
        setLoading(true);
        setErr("");
        setInfo("");

        const hasta = finDia(new Date());
        const desde = inicioDia(new Date());
        desde.setDate(desde.getDate() - dias + 1);

        let resp;
        try {
          resp = await reporteEjecutivo({
            desde: desde.toISOString(),
            hasta: hasta.toISOString(),
          });
        } catch (e) {
          if (e?.response?.status !== 404) throw e;

          const [ventas, stock, caja] = await Promise.all([
            reporteVentas({ desde: desde.toISOString(), hasta: hasta.toISOString() }),
            reporteStock(),
            reporteCaja({ fecha: hasta.toISOString() }),
          ]);

          resp = {
            desde: desde.toISOString(),
            hasta: hasta.toISOString(),
            generadoEn: new Date().toISOString(),
            rentabilidad: {
              totalVendido: Number(ventas?.totalVendido ?? 0),
              totalComprado: 0,
              margenBrutoEstimado: Number(ventas?.totalVendido ?? 0),
              ticketPromedio: Number(ventas?.cantidadVentas ?? 0) > 0 ? Number(ventas?.totalVendido ?? 0) / Number(ventas?.cantidadVentas ?? 1) : 0,
              cantidadVentas: Number(ventas?.cantidadVentas ?? 0),
              crecimientoPorcentualVsPeriodoAnterior: 0,
            },
            canal: {
              ventasWeb: 0,
              ventasPresencial: 0,
              porcentajeWeb: 0,
              porcentajePresencial: 0,
            },
            caja: {
              ingresos: Number(caja?.ingresos ?? 0),
              egresos: Number(caja?.egresos ?? 0),
              totalCompras: Number(caja?.totalCompras ?? 0),
              totalNotasCredito: Number(caja?.totalNotasCredito ?? 0),
              cantidadNotasCredito: Number(caja?.cantidadNotasCredito ?? 0),
              saldoNeto: Number(caja?.saldoNeto ?? 0),
              medioPagoPredominante: "No disponible",
            },
            inventario: {
              stockMinimoConfigurado: 10,
              valorTotalInventario: 0,
              productosStockBajo: Array.isArray(stock) ? stock.length : 0,
              productosSinRotacion60Dias: 0,
              topProductosVendidos: [],
              productosSinRotacionDetalle: [],
            },
            proveedores: {
              totalInvertido: 0,
              frecuenciaCompras: 0,
              proveedorMayorVolumen: null,
              ranking: [],
            },
          };

          setInfo("Se muestra una version reducida porque el endpoint ejecutivo aun no esta disponible en backend.");
        }

        if (!alive) return;
        setData(resp ?? null);
      } catch (e) {
        if (!alive) return;
        setErr(e?.response?.data?.error ?? e?.message ?? "No se pudo cargar el reporte ejecutivo.");
        setData(null);
      } finally {
        if (alive) setLoading(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, [dias]);

  const crecimientoClass = useMemo(() => {
    const value = Number(data?.rentabilidad?.crecimientoPorcentualVsPeriodoAnterior ?? 0);
    if (value > 0) return "agBadge agBadge--ok";
    if (value < 0) return "agBadge agBadge--danger";
    return "agBadge agBadge--warn";
  }, [data]);

  const descargarReporte = () => {
    if (!data) return;
    const payload = {
      generadoEn: new Date().toISOString(),
      tipo: "Reporte Ejecutivo Integral",
      reporte: data,
    };
    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: "application/json;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `reporte-ejecutivo-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <>
      <section className="agPageHead">
        <h1>Dashboard Ejecutivo Integral</h1>
        <p>Vista gerencial para decisiones sobre rentabilidad, canales, caja, inventario y proveedores.</p>
      </section>

      <section className="agSection">
        <div className="agSection__head">
          <div className="agExecToolbar">
            {PERIODOS.map((p) => (
              <button
                key={p.id}
                className={`agBtn ${dias === p.id ? "" : "agBtn--ghost"}`}
                onClick={() => setDias(p.id)}
              >
                {p.label}
              </button>
            ))}
          </div>
          <div className="agExecToolbar">
            <button className="agBtn agBtn--ghost" onClick={descargarReporte} disabled={!data || loading}>
              Guardar reporte
            </button>
            <button className="agBtn" onClick={() => window.print()} disabled={loading}>
              Imprimir
            </button>
          </div>
        </div>

        {loading ? <article className="agCard">Cargando reporte ejecutivo...</article> : null}
        {err ? <article className="agCard agErr">{err}</article> : null}
        {info ? <article className="agCard agHint">{info}</article> : null}

        {!loading && !err && data ? (
          <div className="agGrid agExecStack">
            <section className="agKpis">
              <article className="agKpi">
                <label>Total vendido</label>
                <strong>{money.format(data.rentabilidad?.totalVendido ?? 0)}</strong>
              </article>
              <article className="agKpi">
                <label>Total comprado</label>
                <strong>{money.format(data.rentabilidad?.totalComprado ?? 0)}</strong>
              </article>
              <article className="agKpi agKpi--accent">
                <label>Margen bruto estimado</label>
                <strong>{money.format(data.rentabilidad?.margenBrutoEstimado ?? 0)}</strong>
              </article>
              <article className="agKpi agKpi--ok">
                <label>Ticket promedio</label>
                <strong>{money.format(data.rentabilidad?.ticketPromedio ?? 0)}</strong>
              </article>
            </section>

            <section className="agGrid agExecGrid2">
              <article className="agCard">
                <h3>1. Rentabilidad del periodo</h3>
                <div className="agExecList">
                  <div className="agExecItem">
                    <span>Cantidad de ventas</span>
                    <b>{data.rentabilidad?.cantidadVentas ?? 0}</b>
                  </div>
                  <div className="agExecItem">
                    <span>Crecimiento vs periodo anterior</span>
                    <b><span className={crecimientoClass}>{Number(data.rentabilidad?.crecimientoPorcentualVsPeriodoAnterior ?? 0).toFixed(2)}%</span></b>
                  </div>
                </div>
              </article>

              <article className="agCard">
                <h3>2. Analisis por canal</h3>
                <div className="agExecList">
                  <div className="agExecItem">
                    <span>Ventas web</span>
                    <b>{money.format(data.canal?.ventasWeb ?? 0)} ({Number(data.canal?.porcentajeWeb ?? 0).toFixed(1)}%)</b>
                  </div>
                  <div className="agExecItem">
                    <span>Ventas presencial</span>
                    <b>{money.format(data.canal?.ventasPresencial ?? 0)} ({Number(data.canal?.porcentajePresencial ?? 0).toFixed(1)}%)</b>
                  </div>
                </div>
              </article>
            </section>

            <section className="agGrid agExecGrid2">
              <article className="agCard">
                <h3>3. Flujo de caja resumido</h3>
                <div className="agKpis agExecMiniKpi">
                  <article className="agKpi">
                    <label>Ingresos</label>
                    <strong>{money.format(data.caja?.ingresos ?? 0)}</strong>
                  </article>
                  <article className="agKpi">
                    <label>Egresos</label>
                    <strong>{money.format(data.caja?.egresos ?? 0)}</strong>
                  </article>
                  <article className="agKpi">
                    <label>Compras</label>
                    <strong>{money.format(data.caja?.totalCompras ?? 0)}</strong>
                  </article>
                  <article className="agKpi">
                    <label>Notas credito (cant.)</label>
                    <strong>{data.caja?.cantidadNotasCredito ?? 0}</strong>
                  </article>
                  <article className="agKpi agKpi--ok">
                    <label>Saldo neto</label>
                    <strong>{money.format(data.caja?.saldoNeto ?? 0)}</strong>
                  </article>
                </div>
                <p className="agExecFoot">Medio de pago predominante: <b>{data.caja?.medioPagoPredominante ?? "Sin datos"}</b></p>
              </article>

              <article className="agCard">
                <h3>4. Inventario estrategico</h3>
                <div className="agExecList">
                  <div className="agExecItem">
                    <span>Valor total del inventario</span>
                    <b>{money.format(data.inventario?.valorTotalInventario ?? 0)}</b>
                  </div>
                  <div className="agExecItem">
                    <span>
                      Productos con stock bajo ({"<="} {data.inventario?.stockMinimoConfigurado ?? 10})
                    </span>
                    <b>{data.inventario?.productosStockBajo ?? 0}</b>
                  </div>
                  <div className="agExecItem">
                    <span>Productos sin rotacion (+60 dias)</span>
                    <b>{data.inventario?.productosSinRotacion60Dias ?? 0}</b>
                  </div>
                </div>
              </article>
            </section>

            <section className="agGrid agExecGrid2">
              <article className="agCard">
                <h3>Top productos vendidos</h3>
                <div className="agTableWrap">
                  <table className="agTable agTable--fit">
                    <thead>
                      <tr>
                        <th>Producto</th>
                        <th>Cantidad</th>
                        <th>Total vendido</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(data.inventario?.topProductosVendidos ?? []).length === 0 ? (
                        <tr><td colSpan={3}>Sin ventas para el periodo seleccionado.</td></tr>
                      ) : (data.inventario?.topProductosVendidos ?? []).map((p) => (
                        <tr key={p.productoId}>
                          <td>{p.producto}</td>
                          <td>{p.cantidadVendida}</td>
                          <td>{money.format(p.totalVendido)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </article>

              <article className="agCard">
                <h3>Sin rotacion (detalle)</h3>
                <div className="agTableWrap">
                  <table className="agTable agTable--fit">
                    <thead>
                      <tr>
                        <th>Producto</th>
                        <th>Dias sin venta</th>
                        <th>Stock</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(data.inventario?.productosSinRotacionDetalle ?? []).length === 0 ? (
                        <tr><td colSpan={3}>No hay productos sin rotacion mayor a 60 dias.</td></tr>
                      ) : (data.inventario?.productosSinRotacionDetalle ?? []).map((p) => (
                        <tr key={p.productoId}>
                          <td>{p.producto}</td>
                          <td>{p.diasSinVenta}</td>
                          <td>{p.stockActual}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </article>
            </section>

            <section className="agCard">
              <h3>5. Proveedores</h3>
              <div className="agExecList agExecProviderSummary">
                <div className="agExecItem">
                  <span>Total invertido en compras</span>
                  <b>{money.format(data.proveedores?.totalInvertido ?? 0)}</b>
                </div>
                <div className="agExecItem">
                  <span>Frecuencia de compras</span>
                  <b>{data.proveedores?.frecuenciaCompras ?? 0}</b>
                </div>
                <div className="agExecItem">
                  <span>Proveedor con mayor volumen</span>
                  <b>{data.proveedores?.proveedorMayorVolumen?.proveedor ?? "Sin datos"}</b>
                </div>
              </div>

              <div className="agTableWrap">
                <table className="agTable agTable--fit">
                  <thead>
                    <tr>
                      <th>Proveedor</th>
                      <th>Frecuencia</th>
                      <th>Total invertido</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(data.proveedores?.ranking ?? []).length === 0 ? (
                      <tr><td colSpan={3}>No hay compras registradas para el periodo.</td></tr>
                    ) : (data.proveedores?.ranking ?? []).map((p) => (
                      <tr key={p.proveedorId}>
                        <td>{p.proveedor}</td>
                        <td>{p.frecuenciaCompras}</td>
                        <td>{money.format(p.totalInvertido)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          </div>
        ) : null}
      </section>
    </>
  );
}

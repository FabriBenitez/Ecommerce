const money = new Intl.NumberFormat("es-AR", { style: "currency", currency: "ARS" });

export default function CompraItemsTable({ items, productosById, onRemove, onChange }) {
  const total = (items ?? []).reduce((acc, it) => acc + (it.cantidad * it.costoUnitario), 0);

  return (
    <section className="ccard ccard__pad">
      <div className="rowHead">
        <h2 className="ctitle2">Items de compra</h2>
        <div className="cmuted">Total: <b>{money.format(total)}</b></div>
      </div>

      <div className="tableWrap">
        <table className="invTable">
          <thead>
            <tr>
              <th>Producto</th>
              <th className="right">Cantidad</th>
              <th className="right">Costo unit.</th>
              <th className="right">Subtotal</th>
              <th className="right">Acción</th>
            </tr>
          </thead>
          <tbody>
            {(items ?? []).map((it, idx) => {
              const p = productosById[it.productoId];
              const subtotal = it.cantidad * it.costoUnitario;

              return (
                <tr key={`${it.productoId}-${idx}`}>
                  <td className="invName">{p?.nombre ?? `Producto #${it.productoId}`}</td>

                  <td className="right">
                    <input
                      className="cinput cinput--sm"
                      type="number"
                      min="1"
                      value={it.cantidad}
                      onChange={(e) => onChange(idx, { ...it, cantidad: Number(e.target.value || 0) })}
                    />
                  </td>

                  <td className="right">
                    <input
                      className="cinput cinput--sm"
                      type="number"
                      min="0"
                      step="0.01"
                      value={it.costoUnitario}
                      onChange={(e) => onChange(idx, { ...it, costoUnitario: Number(e.target.value || 0) })}
                    />
                  </td>

                  <td className="right strong">{money.format(subtotal)}</td>

                  <td className="right">
                    <button className="btn btn--ghost" onClick={() => onRemove(idx)}>
                      Quitar
                    </button>
                  </td>
                </tr>
              );
            })}

            {(items ?? []).length === 0 ? (
              <tr>
                <td colSpan={5} className="invEmpty">Todavía no agregaste items.</td>
              </tr>
            ) : null}
          </tbody>
        </table>
      </div>
    </section>
  );
}
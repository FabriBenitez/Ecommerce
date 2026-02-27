import "./ProductCard.css";

export default function ProductCard({ producto, onAgregar }) {
  const {
    id,
    nombre,
    descripcion,
    imagenUrl,
    precio,
    stock,
    tienePromocionActiva,
    promocionNombre,
    precioFinal,
    porcentajeDescuento,
    montoDescuento,
  } = producto;

  const agotado = stock <= 0;
  const mostrarPromo = !!tienePromocionActiva && precioFinal != null && Number(precioFinal) < Number(precio);
  const etiquetaPromo = porcentajeDescuento
    ? `-${Number(porcentajeDescuento).toFixed(0)}%`
    : montoDescuento
      ? `-$${Number(montoDescuento).toFixed(2)}`
      : "Promo";

  return (
    <article className="productCard">
      <div className="productCard__imageWrap">
        {imagenUrl ? (
          <img className="productCard__img" src={imagenUrl} alt={nombre} loading="lazy" />
        ) : (
          <div className="productCard__imgFallback">Sin imagen</div>
        )}

        {agotado ? <span className="productCard__badge">Sin stock</span> : null}
        {mostrarPromo ? <span className="productCard__badge productCard__badge--promo">{etiquetaPromo}</span> : null}
      </div>

      <div className="productCard__body">
        <h3 className="productCard__title" title={nombre}>
          {nombre}
        </h3>

        {descripcion ? (
          <p className="productCard__desc" title={descripcion}>
            {descripcion}
          </p>
        ) : (
          <p className="productCard__desc productCard__desc--muted">—</p>
        )}

        <div className="productCard__footer">
          <div className="productCard__price">
            {mostrarPromo ? (
              <>
                <span className="productCard__priceOld">${Number(precio).toFixed(2)}</span>{" "}
                ${Number(precioFinal).toFixed(2)}
              </>
            ) : (
              <>${Number(precio).toFixed(2)}</>
            )}
            <span className="productCard__currency"> ARS</span>
          </div>

          <button
            className="productCard__btn"
            disabled={agotado}
            onClick={() => onAgregar?.(id)}
            type="button"
          >
            {agotado ? "Agotado" : "Agregar"}
          </button>
        </div>
        {mostrarPromo ? (
          <p className="productCard__promoName">
            {promocionNombre || "Promocion activa"}
          </p>
        ) : null}
      </div>
    </article>
  );
}

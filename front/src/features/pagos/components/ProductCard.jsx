import "./ProductCard.css";

export default function ProductCard({ producto, onAgregar }) {
  const { id, nombre, descripcion, imagenUrl, precio, stock } = producto;

  const agotado = stock <= 0;

  return (
    <article className="productCard">
      <div className="productCard__imageWrap">
        {imagenUrl ? (
          <img className="productCard__img" src={imagenUrl} alt={nombre} loading="lazy" />
        ) : (
          <div className="productCard__imgFallback">Sin imagen</div>
        )}

        {agotado ? <span className="productCard__badge">Sin stock</span> : null}
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
            ${Number(precio).toFixed(2)}
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
      </div>
    </article>
  );
}

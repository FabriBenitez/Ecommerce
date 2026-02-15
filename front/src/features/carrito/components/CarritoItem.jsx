import "./CarritoItem.css";

export default function CarritoItem({ item, onInc, onDec, onRemove, busy = false }) {
  // Ajustá estos campos si tu DTO devuelve otros nombres
  const id = item.id ?? item.itemId;
  const nombre = item.nombreProducto ?? item.productoNombre ?? item.producto?.nombre ?? "Producto";
  const imagenUrl = item.imagenUrl ?? item.productoImagenUrl ?? item.producto?.imagenUrl ?? "";
  const precio = Number(item.precioUnitario ?? item.precio ?? item.productoPrecio ?? 0);
  const cantidad = Number(item.cantidad ?? 0);

  const subtotal = precio * cantidad;

  return (
    <article className="cartItem">
      <div className="cartItem__imgWrap">
        {imagenUrl ? (
          <img className="cartItem__img" src={imagenUrl} alt={nombre} loading="lazy" />
        ) : (
          <div className="cartItem__imgFallback">Sin imagen</div>
        )}
      </div>

      <div className="cartItem__info">
        <h3 className="cartItem__title">{nombre}</h3>
        <p className="cartItem__meta">
          <span className="cartItem__price">${precio.toFixed(2)} ARS</span>
          <span className="cartItem__dot">•</span>
          <span className="cartItem__subtotal">Subtotal: ${subtotal.toFixed(2)}</span>
        </p>

        <div className="cartItem__actions">
          <div className="cartItem__qty">
            <button
              className="cartItem__qtyBtn"
              type="button"
              onClick={() => onDec?.(id, cantidad)}
              disabled={busy || cantidad <= 1}
              aria-label="Disminuir cantidad"
            >
              –
            </button>
            <span className="cartItem__qtyValue">{cantidad}</span>
            <button
              className="cartItem__qtyBtn"
              type="button"
              onClick={() => onInc?.(id, cantidad)}
              disabled={busy}
              aria-label="Aumentar cantidad"
            >
              +
            </button>
          </div>

          <button
            className="cartItem__remove"
            type="button"
            onClick={() => onRemove?.(id)}
            disabled={busy}
          >
            Quitar
          </button>
        </div>
      </div>
    </article>
  );
}

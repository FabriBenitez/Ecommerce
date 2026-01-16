import './Carrito.css';

export default function Carrito({ items, total, onPagar }) {
  return (
    <div className="carrito-container">
      <h2>🛒 Tu Carrito</h2>
      {items.length === 0 ? (
        <p>No hay productos aún.</p>
      ) : (
        <>
          <div className="carrito-lista">
            {items.map(item => (
              <div key={item.id} className="carrito-item">
                <span>{item.nombre} (x{item.cantidad})</span>
                <strong>${(item.precio * item.cantidad).toFixed(2)}</strong>
              </div>
            ))}
          </div>
          <div className="carrito-total">
            <h3>Total: ${total.toFixed(2)}</h3>
            <button className="btn-pagar" onClick={onPagar}>
              Pagar con Mercado Pago
            </button>
          </div>
        </>
      )}
    </div>
  );
}
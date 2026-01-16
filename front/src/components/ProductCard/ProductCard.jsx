import './ProductCard.css';

export default function ProductoCard({ libro, onAgregar }) {
  return (
    <div className="producto-card">
      <img 
        src={libro.imagenUrl} 
        alt={libro.nombre} 
        className="producto-img"
        onError={(e) => { e.target.src = 'https://via.placeholder.com/150?text=Sin+Imagen' }}
      />
      <div className="producto-info">
        <h3>{libro.nombre}</h3>
        <p className="descripcion">{libro.descripcion}</p>
        <p className="precio">${libro.precio}</p>
        <button className="btn-agregar" onClick={() => onAgregar(libro)}>
          Agregar al carrito
        </button>
      </div>
    </div>
  );
}
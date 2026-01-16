import { useState, useEffect } from 'react';
import api from './api/api';
import ProductoCard from './components/ProductCard/ProductCard';
import Carrito from './components/Carrito/Carrito';
import { crearPreferenciaPago } from './api/pagoService';
import './App.css';

function App() {
  const [libros, setLibros] = useState([]);
  const [carrito, setCarrito] = useState([]);

  useEffect(() => {
    api.get('/productos')
      .then(res => setLibros(res.data))
      .catch(err => console.error("Error cargando productos", err));
  }, []);

  const agregarAlCarrito = (libro) => {
    setCarrito(prev => {
      const existe = prev.find(i => i.id === libro.id);
      if (existe) {
        return prev.map(i => i.id === libro.id ? { ...i, cantidad: i.cantidad + 1 } : i);
      }
      return [...prev, { ...libro, cantidad: 1 }];
    });
  };

  const manejarPago = async () => {
    if (carrito.length === 0) {
      alert("El carrito está vacío");
      return;
    }

    try {
      // 1. Llamamos al backend para obtener la URL de Mercado Pago
      const urlMercadoPago = await crearPreferenciaPago(carrito);
      
      // 2. Redirigimos al usuario a la pasarela de pago
      window.location.href = urlMercadoPago; 
      
    } catch (error) {
      console.error("Detalles del error:", error); // <-- Al usar la variable aquí, el aviso desaparece
      alert("Hubo un error al procesar el pago. Intenta de nuevo.");
    }
  };

  const total = carrito.reduce((acc, item) => acc + (item.precio * item.cantidad), 0);

  return (
    <div className="app-layout">
      <main className="productos-section">
        <h1>📚 Librería Online</h1>
        <div className="productos-grid">
          {libros.map(l => (
            <ProductoCard key={l.id} libro={l} onAgregar={agregarAlCarrito} />
          ))}
        </div>
      </main>
      <aside className="carrito-section">
        <Carrito items={carrito} total={total} onPagar={manejarPago} />
      </aside>
    </div>
  );
}

export default App;

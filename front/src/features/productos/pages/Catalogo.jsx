import { useEffect, useMemo, useState } from "react";
import productosApi from "../api/productos.api";
import ProductCard from "../components/ProductCard";
import "./Catalogo.css";

import carritosApi from "@/features/carrito/api/carrito.api"; // si todavía no existe te lo paso abajo

export default function Catalogo() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [q, setQ] = useState("");

  const filtrados = useMemo(() => {
    const term = q.trim().toLowerCase();
    if (!term) return items;
    return items.filter((p) => {
      const n = (p.nombre || "").toLowerCase();
      const d = (p.descripcion || "").toLowerCase();
      return n.includes(term) || d.includes(term);
    });
  }, [items, q]);

  useEffect(() => {
    let alive = true;

    async function load() {
      try {
        setLoading(true);
        setError("");
        const { data } = await productosApi.obtenerCatalogo();
        if (!alive) return;
        setItems(Array.isArray(data) ? data : []);
      } catch (error) {
        console.error("Error al cargar el catálogo:", error);
        if (!alive) return;
        setError("No se pudo cargar el catálogo.");
      } finally {
        if (alive) setLoading(false);
      }
    }

    load();
    return () => {
      alive = false;
    };
  }, []);

  const agregar = async (productoId) => {
    try {
      // Ajustá el DTO según tu backend: AgregarCarritoItemDto
      await carritosApi.agregarItem({ productoId, cantidad: 1 });
      alert("Producto agregado al carrito ✅");
    } catch {
      alert("No se pudo agregar al carrito.");
    }
  };

  return (
    <main className="catalogPage">
      <header className="catalogHero">
        <div className="catalogHero__content">
          <p className="catalogHero__tag">NOVEDADES</p>
          <h1 className="catalogHero__title">Top Picks</h1>
          <p className="catalogHero__subtitle">
            Elegí tus libros y comprá online. Retiro en librería.
          </p>

          <div className="catalogHero__actions">
            <a className="catalogHero__btn" href="/carrito">Ir al carrito</a>
            <a className="catalogHero__btn catalogHero__btn--ghost" href="/mis-ventas">
              Mis compras
            </a>
          </div>
        </div>

        <div className="catalogHero__media" aria-hidden="true">
          <div className="catalogHero__mock" />
        </div>
      </header>

      <section className="catalogBar">
        <div className="catalogBar__left">
          <h2 className="catalogBar__heading">Catálogo</h2>
          <span className="catalogBar__count">{filtrados.length} productos</span>
        </div>

        <div className="catalogBar__right">
          <input
            className="catalogBar__search"
            placeholder="Buscar por nombre o descripción…"
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
        </div>
      </section>

      {loading ? <p className="catalogState">Cargando catálogo…</p> : null}
      {error ? <p className="catalogState catalogState--error">{error}</p> : null}

      {!loading && !error ? (
        <section className="catalogGrid">
          {filtrados.map((p) => (
            <ProductCard key={p.id} producto={p} onAgregar={agregar} />
          ))}
        </section>
      ) : null}
    </main>
  );
}

import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import carritosApi from "../api/carrito.api";
import CarritoItem from "../components/CarritoItem";
import "../components/Carrito.css";

export default function Carrito() {
  const navigate = useNavigate();

  const [carrito, setCarrito] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busyItemId, setBusyItemId] = useState(null);
  const [error, setError] = useState("");

  const items = carrito?.items ?? carrito?.carritoItems ?? [];
  const sinDatosIniciales = !carrito && !!error;

  const resolverMensajeError = (err, fallback) => {
    const raw =
      err?.response?.data?.error ??
      err?.response?.data?.title ??
      (typeof err?.response?.data === "string" ? err.response.data : "") ??
      err?.message ??
      "";

    const msg = String(raw).toLowerCase();
    if (msg.includes("stock")) {
      return "No hay mas stock disponible para esa cantidad.";
    }
    if (msg.includes("cantidad")) {
      return "La cantidad solicitada no es valida.";
    }
    return fallback;
  };

  const total = useMemo(() => {
    return items.reduce((acc, it) => {
      const precio = Number(it.precioUnitario ?? it.precio ?? 0);
      const cant = Number(it.cantidad ?? 0);
      return acc + precio * cant;
    }, 0);
  }, [items]);

  const cargar = async () => {
    try {
      setLoading(true);
      setError("");
      const { data } = await carritosApi.obtenerActual();
      setCarrito(data);
    } catch {
      setError("No se pudo cargar el carrito.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    cargar();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const inc = async (itemId, cantidadActual) => {
    try {
      setBusyItemId(itemId);
      await carritosApi.actualizarCantidad(itemId, { cantidad: cantidadActual + 1 });
      setError("");
      await cargar();
    } catch (err) {
      setError(resolverMensajeError(err, "No se pudo actualizar la cantidad."));
    } finally {
      setBusyItemId(null);
    }
  };

  const dec = async (itemId, cantidadActual) => {
    try {
      if (cantidadActual <= 1) return;
      setBusyItemId(itemId);
      await carritosApi.actualizarCantidad(itemId, { cantidad: cantidadActual - 1 });
      setError("");
      await cargar();
    } catch (err) {
      setError(resolverMensajeError(err, "No se pudo actualizar la cantidad."));
    } finally {
      setBusyItemId(null);
    }
  };

  const remove = async (itemId) => {
    try {
      setBusyItemId(itemId);
      await carritosApi.eliminarItem(itemId);
      setError("");
      await cargar();
    } catch (err) {
      setError(resolverMensajeError(err, "No se pudo eliminar el item."));
    } finally {
      setBusyItemId(null);
    }
  };

  const irCheckout = () => {
    // la página Checkout la armamos después (ya la tenés creada)
    navigate("/checkout");
  };

  return (
    <main className="cartPage">
      <header className="cartHeader">
        <div className="cartHeader__left">
          <h1 className="cartHeader__title">Carrito</h1>
          <p className="cartHeader__subtitle">Revisá tus productos antes de pagar.</p>
        </div>

        <div className="cartHeader__right">
          <Link className="cartHeader__link" to="/catalogo">
            Seguir comprando
          </Link>
        </div>
      </header>

      {loading ? <p className="cartState">Cargando carrito…</p> : null}
      {error ? <p className="cartState cartState--error">{error}</p> : null}

      {!loading ? (
        <div className="cartLayout">
          <section className="cartList">
            {sinDatosIniciales ? (
              <div className="cartEmpty">
                <p className="cartEmpty__title">No pudimos cargar tu carrito.</p>
                <p className="cartEmpty__text">Intenta nuevamente en unos segundos o segui comprando.</p>
                <Link className="cartEmpty__btn" to="/catalogo">
                  Ir al catalogo
                </Link>
              </div>
            ) : items.length === 0 ? (
              <div className="cartEmpty">
                <p className="cartEmpty__title">Tu carrito está vacío 🧾</p>
                <p className="cartEmpty__text">Volvé al catálogo y agregá productos.</p>
                <Link className="cartEmpty__btn" to="/catalogo">
                  Ir al catálogo
                </Link>
              </div>
            ) : (
              items.map((it) => (
                <CarritoItem
                  key={it.id ?? it.itemId}
                  item={it}
                  onInc={inc}
                  onDec={dec}
                  onRemove={remove}
                  busy={busyItemId === (it.id ?? it.itemId)}
                />
              ))
            )}
          </section>

          <aside className="cartSummary">
            <div className="cartSummary__card">
              <h2 className="cartSummary__title">Resumen</h2>

              <div className="cartSummary__row">
                <span>Items</span>
                <strong>{items.length}</strong>
              </div>

              <div className="cartSummary__row cartSummary__row--total">
                <span>Total</span>
                <strong>${total.toFixed(2)} ARS</strong>
              </div>

              <button
                className="cartSummary__cta"
                type="button"
                onClick={irCheckout}
                disabled={items.length === 0}
              >
                Ir a checkout
              </button>

              <p className="cartSummary__hint">
                En checkout se genera el link de Mercado Pago para retiro presencial en tienda.
              </p>
            </div>
          </aside>
        </div>
      ) : null}
    </main>
  );
}

import { createBrowserRouter, Navigate } from "react-router-dom";

// Auth
import Login from "@/features/auth/pages/Login";
import Register from "@/features/auth/pages/Register";

// Feature pages (ajustá imports si cambiaste rutas)
import Catalogo from "@/features/productos/pages/Catalogo";
import Carrito from "@/features/carrito/pages/Carrito";
import Checkout from "@/features/ventas/pages/Checkout";
import PagoResultado from "@/features/ventas/pages/PagoResultado";
import RequireAuth from "@/shared/auth/RequireAuth";

import PagoSuccess from "@/features/pagos/pages/PagoSuccess";
import PagoPending from "@/features/pagos/pages/PagoPending";
import PagoFailure from "@/features/pagos/pages/PagoFailure";

import MisVentas from "@/features/ventas/pages/MisVentas";
import VentaDetalle from "@/features/ventas/pages/VentaDetalle";


export const router = createBrowserRouter([
  { path: "/", element: <Navigate to="/catalogo" replace /> },

  // Públicas
  { path: "/login", element: <Login /> },
  { path: "/registro", element: <Register /> },

  // Si todavía no hiciste recuperar, dejalo así
  { path: "/recuperar", element: <div style={{ padding: 20 }}>Recuperar password (pendiente)</div> },

  // Privadas
  {
    element: <RequireAuth />,
    children: [
      { path: "/catalogo", element: <Catalogo /> },
      { path: "/carrito", element: <Carrito /> },
      { path: "/checkout", element: <Checkout /> },
      { path: "/mis-ventas", element: <MisVentas /> },
      { path: "/venta/:id", element: <VentaDetalle /> },

      { path: "/pago/success", element: <PagoSuccess /> },
      { path: "/pago/pending", element: <PagoPending />},
      { path: "/pago/failure", element: <PagoFailure />},

      {path: "/ventas/mis-ventas",element: <MisVentas />},
      {path: "/ventas/:id",element: <VentaDetalle />},

      // back_urls de MP (success / failure / pending)
      { path: "/pago/:resultado", element: <PagoResultado /> },
    ],
  },

  { path: "*", element: <div style={{ padding: 20 }}>404</div> },
]);

import { createBrowserRouter, Navigate } from "react-router-dom";
import AppLayout from "@/shared/layout/Navbar";;

// Auth
import Login from "@/features/auth/pages/Login";
import Register from "@/features/auth/pages/Register";

// Feature pages
import Catalogo from "@/features/productos/pages/Catalogo";
import Carrito from "@/features/carrito/pages/Carrito";
import Checkout from "@/features/ventas/pages/Checkout";
import MisVentas from "@/features/ventas/pages/MisVentas";
import VentaDetalle from "@/features/ventas/pages/VentaDetalle";

import RequireAuth from "@/shared/auth/RequireAuth";

// Pagos
import PagoResultado from "@/features/ventas/pages/PagoResultado";
import PagoSuccess from "@/features/pagos/pages/PagoSuccess";
import PagoPending from "@/features/pagos/pages/PagoPending";
import PagoFailure from "@/features/pagos/pages/PagoFailure";

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: "/", element: <Navigate to="/catalogo" replace /> },

      // Públicas
      { path: "/login", element: <Login /> },
      { path: "/registro", element: <Register /> },
      {
        path: "/recuperar",
        element: <div style={{ padding: 20 }}>Recuperar password (pendiente)</div>,
      },

      // Privadas
      {
        element: <RequireAuth />,
        children: [
          { path: "/catalogo", element: <Catalogo /> },
          { path: "/carrito", element: <Carrito /> },
          { path: "/checkout", element: <Checkout /> },

          // Ventas
          { path: "/mis-ventas", element: <MisVentas /> },
          { path: "/ventas/:id", element: <VentaDetalle /> },

          // back_urls / pantallas de resultado
          { path: "/pago/success", element: <PagoSuccess /> },
          { path: "/pago/pending", element: <PagoPending /> },
          { path: "/pago/failure", element: <PagoFailure /> },
          { path: "/pago/:resultado", element: <PagoResultado /> },
        ],
      },

      { path: "*", element: <div style={{ padding: 20 }}>404</div> },
    ],
  },
]);

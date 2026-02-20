import { createBrowserRouter, Navigate } from "react-router-dom";
import AppLayout from "@/shared/layout/pages/AppLayuout";
import Comprobante from "@/features/ventas/pages/Comprobante";

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
import RequireRole from "@/shared/auth/RequireRole";

// Pagos
import PagoResultado from "@/features/ventas/pages/PagoResultado";
import PagoSuccess from "@/features/pagos/pages/PagoSuccess";
import PagoPending from "@/features/pagos/pages/PagoPending";
import PagoFailure from "@/features/pagos/pages/PagoFailure";

// AdminVentas
import AdminShell from "@/features/adminVentas/components/AdminShell";
import AdminDashboard from "@/features/adminVentas/pages/AdminDashboard";
import PedidosRetiro from "@/features/adminVentas/pages/PedidosRetiro";
import PedidoRetiroDetalle from "@/features/adminVentas/pages/PedidoRetiroDetalle";
import PosVenta from "@/features/adminVentas/pages/PosVenta";
import PosPago from "@/features/adminVentas/pages/PosPago";
import HistorialFacturas from "@/features/adminVentas/pages/HistorialFacturas";
import Devolucion from "@/features/adminVentas/pages/Devolucion";
import AdminComprobante from "@/features/adminVentas/pages/Comprobante";

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
          {
            element: <RequireRole role="Cliente" />,
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
              { path: "/comprobante/:id", element: <Comprobante /> }
            ],
          },
        ],
      },
      {
        element: <RequireRole role="AdminVentas" />,
        children: [
          {
            element: <AdminShell />,
            children: [
              { path: "/admin", element: <AdminDashboard /> },
              { path: "/admin/retiros", element: <PedidosRetiro /> },
              { path: "/admin/retiros/:ventaId", element: <PedidoRetiroDetalle /> },
              { path: "/admin/pos", element: <PosVenta /> },
              { path: "/admin/pos/pago", element: <PosPago /> },
              { path: "/admin/facturas", element: <HistorialFacturas /> },
              { path: "/admin/facturas/:ventaId", element: <AdminComprobante /> },
              { path: "/admin/devolucion/:ventaId", element: <Devolucion /> },
            ],
          },
        ],
      },
      { path: "/sin-acceso", element: <div style={{ padding: 20 }}>No tenes acceso a este modulo.</div> },

      { path: "*", element: <div style={{ padding: 20 }}>404</div> },
    ],
  },
]);

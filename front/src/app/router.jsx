import { createBrowserRouter, Navigate } from "react-router-dom";
import AppLayout from "@/shared/layout/pages/AppLayuout";
import Comprobante from "@/features/ventas/pages/Comprobante";

// Auth
import Login from "@/features/auth/pages/Login";
import Register from "@/features/auth/pages/Register";

// Cliente
import Catalogo from "@/features/productos/pages/Catalogo";
import PromocionesCliente from "@/features/productos/pages/PromocionesCliente";
import Carrito from "@/features/carrito/pages/Carrito";
import Checkout from "@/features/ventas/pages/Checkout";
import MisVentas from "@/features/ventas/pages/MisVentas";
import MisRetiros from "@/features/ventas/pages/MisRetiros";
import VentaDetalle from "@/features/ventas/pages/VentaDetalle";

// Guards
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

// AdminCompras
import ComprasShell from "@/features/adminCompras/components/ComprasShell";
import ComprasDashboard from "@/features/adminCompras/pages/ComprasDashboard";
import StockInventario from "@/features/adminCompras/pages/StockInventario";
import Proveedores from "@/features/adminCompras/pages/Proveedores";
import ProveedorDetalle from "@/features/adminCompras/pages/ProveedorDetalle";
import NuevaCompra from "@/features/adminCompras/pages/NuevaCompra";
import CompraDetalle from "@/features/adminCompras/pages/CompraDetalle";
import SeguimientoPedidos from "@/features/adminCompras/pages/SeguimientoPedidos";
import HistorialCompras from "@/features/adminCompras/pages/HistorialCompras";

// AdminGeneral
import AdminGeneralShell from "@/features/adminGeneral/components/AdminGeneralShell";
import AdminGeneralDashboard from "@/features/adminGeneral/pages/AdminGeneralDashboard";
import UsuariosAdmin from "@/features/adminGeneral/pages/UsuariosAdmin";
import UsuarioDetalle from "@/features/adminGeneral/pages/UsuarioDetalle";
import FacturaDatosAdmin from "@/features/adminGeneral/pages/FacturaDatosAdmin";
import Promociones from "@/features/adminGeneral/pages/Promociones";
import CajaAdmin from "@/features/adminGeneral/pages/CajaAdmin";
import Reportes from "@/features/adminGeneral/pages/Reportes";

export const router = createBrowserRouter([
  {
    element: <AppLayout />,
    children: [
      { path: "/", element: <Navigate to="/catalogo" replace /> },

      // Públicas
      { path: "/login", element: <Login /> },
      { path: "/registro", element: <Register /> },
      { path: "/recuperar", element: <div style={{ padding: 20 }}>Recuperar password (pendiente)</div> },

      // Privadas (TODO lo que requiera token acá adentro)
      {
        element: <RequireAuth />,
        children: [
          // ✅ Cliente
          {
            element: <RequireRole role="Cliente" />,
            children: [
              { path: "/catalogo", element: <Catalogo /> },
              { path: "/promociones", element: <PromocionesCliente /> },
              { path: "/carrito", element: <Carrito /> },
              { path: "/checkout", element: <Checkout /> },

              { path: "/mis-ventas", element: <MisVentas /> },
              { path: "/mis-retiros", element: <MisRetiros /> },
              { path: "/ventas/:id", element: <VentaDetalle /> },

              { path: "/pago/success", element: <PagoSuccess /> },
              { path: "/pago/pending", element: <PagoPending /> },
              { path: "/pago/failure", element: <PagoFailure /> },
              { path: "/pago/:resultado", element: <PagoResultado /> },

              { path: "/comprobante/:id", element: <Comprobante /> },
            ],
          },

          // ✅ AdminVentas
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

          // ✅ AdminCompras
          {
            element: <RequireRole role="AdminCompras" />,
            children: [
              {
                element: <ComprasShell />,
                children: [
                  { path: "/compras", element: <ComprasDashboard /> },
                  { path: "/compras/inventario", element: <StockInventario /> },

                  { path: "/compras/proveedores", element: <Proveedores /> },
                  { path: "/compras/proveedores/:id", element: <ProveedorDetalle /> },

                  { path: "/compras/nueva", element: <NuevaCompra /> },
                  { path: "/compras/seguimiento", element: <SeguimientoPedidos /> },
                  { path: "/compras/historial", element: <HistorialCompras /> },

                  { path: "/compras/:id", element: <CompraDetalle /> },
                ],
              },
            ],
          },

          // ✅ AdminGeneral
          {
            element: <RequireRole role="AdminGeneral" />,
            children: [
              {
                element: <AdminGeneralShell />,
                children: [
                  { path: "/admin-general", element: <AdminGeneralDashboard /> },
                  { path: "/admin-general/usuarios", element: <UsuariosAdmin /> },
                  { path: "/admin-general/usuarios/:id", element: <UsuarioDetalle /> },
                  { path: "/admin-general/factura", element: <FacturaDatosAdmin /> },
                  { path: "/admin-general/promociones", element: <Promociones /> },
                  { path: "/admin-general/caja", element: <CajaAdmin /> },
                  { path: "/admin-general/reportes", element: <Reportes /> },
                ],
              },
            ],
          },
        ],
      },

      { path: "/sin-acceso", element: <div style={{ padding: 20 }}>No tenes acceso a este modulo.</div> },
      { path: "*", element: <div style={{ padding: 20 }}>404</div> },
    ],
  },
]);

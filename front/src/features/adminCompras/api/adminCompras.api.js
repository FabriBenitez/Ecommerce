import http from "@/shared/api/http";

// Dashboard
export async function adminComprasDashboard(stockMinimo = 10) {
  const { data } = await http.get(`/api/admin-compras/dashboard?stockMinimo=${stockMinimo}`);
  return data;
}

// Inventario
export async function getProductosCatalogo() {
  const { data } = await http.get("/api/productos");
  return data;
}

export async function listarProductos() {
  const { data } = await http.get("/api/productos");
  return data;
}

// Proveedores
export async function listarProveedores(activos) {
  const params = {};
  if (typeof activos === "boolean") params.activos = activos;
  const { data } = await http.get("/api/proveedores", { params });
  return data;
}

export async function crearProveedor(payload) {
  const { data } = await http.post("/api/proveedores", payload);
  return data;
}

export async function actualizarProveedor(id, payload) {
  const { data } = await http.put(`/api/proveedores/${id}`, payload);
  return data;
}

export async function proveedorDetalle(id) {
  const { data } = await http.get(`/api/proveedores/${id}`);
  return data;
}

// Compras
export async function crearCompra(payload) {
  const { data } = await http.post("/api/compras", payload);
  return data;
}

export async function obtenerCompra(id) {
  const { data } = await http.get(`/api/compras/${id}`);
  if (data && typeof data === "object") {
    const p = data.proveedor;
    if (p && typeof p === "object") {
      const razon = p.razonSocial ?? p.nombre ?? "";
      const cuit = p.cuit ? ` (${p.cuit})` : "";
      data.proveedor = razon ? `${razon}${cuit}` : `Proveedor #${p.proveedorId ?? ""}`.trim();
      data.proveedorObj = p;
    }
  }
  return data;
}

export async function confirmarCompra(id) {
  const { data } = await http.post(`/api/compras/${id}/confirmar`);
  return data;
}

export async function registrarFacturaProveedor(id, payload) {
  const { data } = await http.post(`/api/compras/${id}/factura`, payload);
  return data;
}

export async function listarCompras(proveedorId) {
  const { data } = await http.get("/api/compras", {
    params: proveedorId ? { proveedorId } : undefined,
  });
  return data;
}

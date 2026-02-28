import http from "@/shared/api/http";

// Dashboard
export async function adminComprasDashboard(stockMinimo) {
  const query = typeof stockMinimo === "number" ? `?stockMinimo=${stockMinimo}` : "";
  const { data } = await http.get(`/api/admin-compras/dashboard${query}`);
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

export async function crearProducto(payload) {
  const form = new FormData();
  form.append("nombre", payload.nombre ?? "");
  form.append("descripcion", payload.descripcion ?? "");
  form.append("precio", String(payload.precio ?? 0));
  form.append("stock", String(payload.stock ?? 0));
  if (payload.imagenFile) form.append("imagen", payload.imagenFile);
  const { data } = await http.post("/api/productos", form);
  return data;
}

export async function importarProductosCsv(file) {
  const form = new FormData();
  form.append("file", file);
  const { data } = await http.post("/api/productos/import-csv", form);
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

export async function obtenerStockMinimo() {
  const { data } = await http.get("/api/configuracion/stock-minimo");
  return Number(data?.stockMinimo ?? 10);
}

// Promociones
export async function listarPromociones(params) {
  const { data } = await http.get("/api/promociones", { params });
  return data ?? [];
}

export async function crearPromocion(payload) {
  const { data } = await http.post("/api/promociones", payload);
  return data;
}

export async function desactivarPromocion(id) {
  const { data } = await http.delete(`/api/promociones/${id}`);
  return data;
}

export async function desactivarPromocionPorProducto(productoId) {
  const { data } = await http.delete(`/api/promociones/by-producto/${productoId}`);
  return data;
}

export async function desactivarPromocionPorGenero(genero) {
  const { data } = await http.delete("/api/promociones/by-genero", { params: { genero } });
  return data;
}

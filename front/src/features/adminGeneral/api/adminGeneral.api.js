import http from "@/shared/api/http";

export async function listarUsuariosAdmin() {
  const { data } = await http.get("/api/admin-general/usuarios");
  return data ?? [];
}

export async function detalleUsuarioAdmin(id) {
  const { data } = await http.get(`/api/admin-general/usuarios/${id}`);
  return data;
}

export async function listarRolesAdmin() {
  const { data } = await http.get("/api/admin-general/roles");
  return data ?? [];
}

export async function listarPromociones(params) {
  const { data } = await http.get("/api/promociones", { params });
  return data ?? [];
}

export async function crearPromocion(payload) {
  const { data } = await http.post("/api/promociones", payload);
  return data;
}

export async function listarProductosAdmin() {
  const { data } = await http.get("/api/productos");
  return data ?? [];
}

export async function resumenCaja() {
  const { data } = await http.get("/api/caja/resumen");
  return data;
}

export async function movimientosCaja(params) {
  const { data } = await http.get("/api/caja/movimientos", { params });
  return data ?? [];
}

export async function abrirCaja(payload) {
  const { data } = await http.post("/api/caja/abrir", payload);
  return data;
}

export async function cerrarCaja(payload) {
  const { data } = await http.post("/api/caja/cerrar", payload);
  return data;
}

export async function reporteVentas(params) {
  const { data } = await http.get("/api/reportes/ventas", { params });
  return data;
}

export async function reporteStock() {
  const { data } = await http.get("/api/reportes/stock");
  return data ?? [];
}

export async function reporteCaja(params) {
  const { data } = await http.get("/api/reportes/caja", { params });
  return data;
}

export async function obtenerDatosFactura() {
  const { data } = await http.get("/api/facturacion-config");
  return data;
}

export async function actualizarDatosFactura(payload) {
  const { data } = await http.put("/api/facturacion-config", payload);
  return data;
}

export async function crearUsuarioInterno(payload) {
  const { data } = await http.post("/api/admin-general/usuarios", payload);
  return data;
}

export async function cambiarActivoUsuario(id, activo) {
  const { data } = await http.put(`/api/admin-general/usuarios/${id}/activo`, { activo });
  return data;
}

export async function actualizarUsuarioInterno(id, payload) {
  const { data } = await http.put(`/api/admin-general/usuarios/${id}`, payload);
  return data ?? null;
}

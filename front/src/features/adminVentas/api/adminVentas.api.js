import http from "@/shared/api/http";

export async function retirosList({ estado } = {}) {
  const { data } = await http.get("/api/retiros", {
    params: estado ? { estado } : undefined,
  });
  return data ?? [];
}

export async function retiroDetalle(ventaId) {
  const { data } = await http.get(`/api/ventas/${ventaId}`);
  return data;
}

export async function cambiarEstadoRetiro(ventaId, estadoRetiro) {
  const { data } = await http.put(`/api/retiros/${ventaId}/estado`, { estadoRetiro });
  return data ?? null;
}

export async function crearVentaPresencial(payload) {
  const { data } = await http.post("/api/ventas-presenciales", payload);
  return data;
}

export async function historialPresencial(params) {
  const { data } = await http.get("/api/ventas-presenciales/historial", { params });
  return data ?? [];
}

export async function registrarDevolucion(ventaId, payload) {
  const { data } = await http.post(`/api/ventas-presenciales/${ventaId}/devolucion`, payload);
  return data;
}

export async function notaCreditoPorDni(dni) {
  const { data } = await http.get(`/api/notas-credito/${dni}`);
  return data;
}

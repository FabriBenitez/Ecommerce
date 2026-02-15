import http from "@/shared/api/http";

export const ventasApi = {
  checkoutWeb: (payload) => http.post("/api/ventas/web/checkout", payload),
  obtenerVenta: (id) => http.get(`/api/ventas/${id}`),
  obtenerMisVentas: () => http.get("/api/ventas/mis-ventas"),
};

export async function misVentas() {
  const { data } = await ventasApi.obtenerMisVentas();
  return data; // MisVentasItemDto[]
}

export async function ventaPorId(id) {
  const { data } = await ventasApi.obtenerVenta(id);
  return data; // VentaDto
}

export default ventasApi;

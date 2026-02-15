import api from "@/shared/api/http";

export const ventasApi = {
  checkoutWeb: (payload) => api.post("/api/ventas/checkout-web", payload),
  obtenerVenta: (id) => api.get(`/api/ventas/${id}`),
  misVentas: () => api.get(`/api/ventas/mis-ventas`),
};


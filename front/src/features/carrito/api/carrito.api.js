import http from "@/shared/api/http";

const carritosApi = {
  obtenerActual: () => http.get("/api/carritos/actual"),
  agregarItem: (payload) => http.post("/api/carritos/items", payload), // { productoId, cantidad }
  actualizarCantidad: (itemId, payload) => http.put(`/api/carritos/items/${itemId}`, payload), // { cantidad }
  eliminarItem: (itemId) => http.delete(`/api/carritos/items/${itemId}`),
};

export default carritosApi;

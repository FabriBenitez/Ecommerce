import http from "@/shared/api/http";

const productosApi = {
  obtenerCatalogo: () => http.get("/api/productos"),
};

export default productosApi;

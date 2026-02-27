import http from "@/shared/api/http";

export async function listarPromocionesCliente() {
  const { data } = await http.get("/api/promociones/cliente");
  return data ?? [];
}

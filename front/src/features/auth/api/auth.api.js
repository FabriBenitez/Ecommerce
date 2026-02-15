import http from "@/shared/api/http";

const authApi = {
  login: (payload) => http.post("/api/auth/login", payload),
  register: (payload) => http.post("/api/auth/register", payload),
  perfil: () => http.get("/api/auth/perfil"),
  refresh: (payload) => http.post("/api/auth/refresh", payload),
};

export default authApi;

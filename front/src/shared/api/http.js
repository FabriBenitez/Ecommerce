import axios from "axios";
import { getTokens, clearTokens } from "@/shared/auth/tokenStorage";

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "https://localhost:7248",
});

http.interceptors.request.use((config) => {
  const tokens = getTokens();
  if (tokens?.accessToken) {
    config.headers.Authorization = `Bearer ${tokens.accessToken}`;
  }
  return config;
});

http.interceptors.response.use(
  (r) => r,
  async (err) => {
    // si querés, acá después metemos refresh token automático.
    // por ahora: si 401 -> limpiar sesión
    if (err?.response?.status === 401) {
      clearTokens();
    }
    return Promise.reject(err);
  }
);

export default http;

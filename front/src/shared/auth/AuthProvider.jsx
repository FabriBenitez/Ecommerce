import { useCallback, useEffect, useMemo, useState } from "react";
import { AuthContext } from "./auth.context";
import { getTokens, setTokens, clearTokens } from "./tokenStorage";
import authApi from "@/features/auth/api/auth.api";

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(null);
  const [cargandoPerfil, setCargandoPerfil] = useState(true);

  const cargarPerfil = useCallback(async () => {
    try {
      const { data } = await authApi.perfil();
      setUsuario(data);
    } catch {
      setUsuario(null);
    } finally {
      setCargandoPerfil(false);
    }
  }, []);

  useEffect(() => {
    const tokens = getTokens();
    if (!tokens?.accessToken) {
      setCargandoPerfil(false);
      return;
    }
    cargarPerfil();
  }, [cargarPerfil]);

  const login = useCallback(
    async ({ email, password }) => {
      const { data } = await authApi.login({ email, password });
      setTokens(data);
      await cargarPerfil();
    },
    [cargarPerfil]
  );

  const register = useCallback(async (payload) => {
    await authApi.register(payload);
  }, []);

  const logout = useCallback(() => {
    clearTokens();
    setUsuario(null);
  }, []);

  const value = useMemo(
    () => ({
      usuario,
      isAuth: !!usuario,
      cargandoPerfil,
      login,
      register,
      logout,
    }),
    [usuario, cargandoPerfil, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

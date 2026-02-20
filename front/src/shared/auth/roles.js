import { getTokens } from "@/shared/auth/tokenStorage";

function decodeJwtPayload(token) {
  try {
    const payloadPart = token?.split(".")?.[1];
    if (!payloadPart) return null;
    const base64 = payloadPart.replace(/-/g, "+").replace(/_/g, "/");
    const normalized = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
    return JSON.parse(atob(normalized));
  } catch {
    return null;
  }
}

export function getUserRoles(usuario) {
  const fromUser = usuario?.roles ?? usuario?.role ?? [];
  const rolesUserArray = Array.isArray(fromUser) ? fromUser : [fromUser].filter(Boolean);

  const tokens = getTokens();
  const payload = decodeJwtPayload(tokens?.accessToken);
  const claim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
  const fromToken = payload?.[claim] ?? payload?.role ?? [];
  const rolesTokenArray = Array.isArray(fromToken) ? fromToken : [fromToken].filter(Boolean);

  const merged = [...rolesUserArray, ...rolesTokenArray];
  return [...new Set(merged)];
}

export function hasRole(usuario, role) {
  return getUserRoles(usuario).includes(role);
}

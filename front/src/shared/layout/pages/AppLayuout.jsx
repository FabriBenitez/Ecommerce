import { Outlet, useLocation } from "react-router-dom";
import Navbar from "@/shared/layout/Navbar";

export default function AppLayout() {
  const location = useLocation();
  const hideNavbar =
    location.pathname.startsWith("/admin") ||
    location.pathname.startsWith("/compras");

  return (
    <>
      {!hideNavbar ? <Navbar /> : null}
      <Outlet />
    </>
  );
}

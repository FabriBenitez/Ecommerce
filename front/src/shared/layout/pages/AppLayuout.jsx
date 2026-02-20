import { Outlet, useLocation } from "react-router-dom";
import Navbar from "@/shared/layout/Navbar";

export default function AppLayout() {
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith("/admin");

  return (
    <>
      {!isAdminRoute ? <Navbar /> : null}
      <Outlet />
    </>
  );
}

import { Outlet, useLocation } from "react-router-dom";
import AdminNavbar from "./AdminNavbar";
import "./AdminShell.css";
import "./AdminCommon.css";

export default function AdminShell() {
  const { pathname } = useLocation();
  const isComprobanteRoute = /^\/admin\/facturas\/[^/]+\/?$/.test(pathname);

  return (
    <div className="adminShell">
      {!isComprobanteRoute ? <AdminNavbar /> : null}
      <main className="adminMain">
        <Outlet />
      </main>
    </div>
  );
}

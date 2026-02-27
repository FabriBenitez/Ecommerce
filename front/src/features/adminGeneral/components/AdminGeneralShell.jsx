import { Outlet } from "react-router-dom";
import AdminGeneralNavbar from "./AdminGeneralNavbar";
import "./AdminGeneralCommon.css";

export default function AdminGeneralShell() {
  return (
    <div className="agShell">
      <AdminGeneralNavbar />
      <main className="agMain">
        <Outlet />
      </main>
    </div>
  );
}

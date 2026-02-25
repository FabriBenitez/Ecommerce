import { Outlet } from "react-router-dom";
import ComprasNavbar from "./ComprasNavbar";
import "../styles/ComprasShell.css";
import "../styles/ComprasCommon.css";

export default function ComprasShell() {
  return (
    <div className="comprasShell">
      <ComprasNavbar />
      <main className="comprasMain" role="main">
        <Outlet />
      </main>
    </div>
  );
}
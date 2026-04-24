import { Outlet } from "react-router-dom";
import FloatingDockComponent from "./floating-dock-component";
import NoiseBackgroundIcon from "./noise-background-logo";
import MainBackround from "./main-backroudn";

export default function PortfolioLayout() {
  return (
    <div className="relative w-full min-h-screen overflow-hidden bg-white dark:bg-[#0B0B0F]">
      <MainBackround />

      <div className="absolute left-12 top-12 w-40 h-40">
        <NoiseBackgroundIcon />
      </div>

      <div className="relative z-10">
        <div className="flex justify-center py-4">
          <FloatingDockComponent />
        </div>

        <main>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
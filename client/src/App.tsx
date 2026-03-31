import React, { useState, useEffect, useCallback } from 'react';
import { MacbookScroll } from "@/components/ui/macbook-scroll";
import icon from "../public/icon.png";

import FloatingDockDemo from "./components/floating-dock-demo" 

function App() {
  return (
    <div>
      <FloatingDockDemo/>
      <div className="w-full overflow-hidden bg-white dark:bg-[#0B0B0F]">

      <MacbookScroll
        title={
          ""
        }
        badge={
          <img src={icon} className="w-40 h-30"  alt="icon" />
        }
        src={`/linear.webp`}
        showGradient={false}
      />
    </div>
    </div>
    )
}

export default App

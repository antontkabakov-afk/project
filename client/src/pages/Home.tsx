import React, { useState, useEffect, useCallback } from 'react';

import { MacbookScroll } from "@/components/ui/macbook-scroll";
import {NoiseBackground} from "@/components/ui/noise-background";

import FloatingDockComponent from "../components/floating-dock-component" 

import icon from "../../public/icon.png";
import { Link } from 'react-router';

function Home() {
  return (
  <div className="relative w-full bg-white dark:bg-[#0B0B0F]">
    <NoiseBackground
      containerClassName="absolute left-12 top-12 w-40 h-40 rounded-full overflow-hidden flex items-center justify-center"
      gradientColors={[
        "rgb(255, 100, 150)",
        "rgb(100, 150, 255)",
        "rgb(255, 200, 100)",
      ]}
    >
      <img
        src={icon}
        alt="side"
        className="w-full h-full object-cover"
      />
    </NoiseBackground>

    <div className="flex justify-center py-4">
      <FloatingDockComponent />
    </div>

    <div className="absolute top-12 right-10 mr-10 flex flex-col gap-8 z-10">
      <Link to="/login">
        <button className="shadow-[inset_0_0_0_2px_#616467] px-8 py-4 rounded-full font-bold text-white tracking-widest uppercase transform hover:scale-110 hover:bg-[#616467] transition-colors duration-200">
          Log in
        </button>
      </Link>
      <button className="shadow-[inset_0_0_0_2px_#616467] px-8 py-4 rounded-full font-bold text-white tracking-widest uppercase transform hover:scale-110 hover:bg-[#616467] transition-colors duration-200">
        Sing up
      </button>
    </div>

    <div className="w-full overflow-hidden">
      <MacbookScroll
        title={""}
        badge={
          <img src={icon} className="w-20 h-30" alt="icon" />
        }
        src={`/linear.webp`}
        showGradient={false}
      />
    </div>
  </div>
);
}

export default Home

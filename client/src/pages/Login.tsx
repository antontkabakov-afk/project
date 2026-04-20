import React from 'react';
import { Link } from 'react-router';

import FloatingDockComponent from "../components/floating-dock-component";
import NoiseBackgroundIcon from "../components/noise-background-logo";
import MainBackround from "../components/main-backroudn";

export default function Login() {
  return (
    <div className="relative w-full min-h-screen overflow-hidden bg-white dark:bg-[#0B0B0F]">
       <MainBackround/>
       <div className='absolute left-12 top-12 w-40 h-40'>
               <NoiseBackgroundIcon />
        </div>
        <div className="relative z-10">

         <div className="flex justify-center py-4">
           <FloatingDockComponent />
         </div>
         <div className="absolute top-12 right-10 mr-10 flex flex-col gap-8">
           <Link to="/login">
             <button className="px-8 py-4 rounded-full font-bold text-white tracking-widest uppercase transform hover:scale-110 bg-[#00F5C8] transition-colors duration-200 shadow-2xl shadow-[#00F5C8]/[0.5]">
               Log in
             </button>
           </Link>

           <Link to="/signup">
            <button className="shadow-[inset_0_0_0_2px_#616467] px-8 py-4 rounded-full font-bold text-white tracking-widest uppercase transform hover:scale-110 hover:bg-[#00F5C8] transition-colors duration-200 hover:shadow-2xl hover:shadow-[#00F5C8]/[0.5]">
              Sign up
            </button>
          </Link>
         </div>
       </div>
       <div className="relative z-10 flex items-center justify-center min-h-screen">

      {/* Login Card */}
      <div className="w-[360px] bg-[#0F172A]/80 backdrop-blur-xl border border-white/10 rounded-2xl shadow-2xl p-6 text-white">

        {/* Logo */}
        <div className="flex items-center gap-4 mb-6">
          <div className='w-20 h-20'>
            <NoiseBackgroundIcon />
          </div>
          <h1 className="text-xl font-semibold">Crypto Tracker</h1>
        </div>

        {/* Inputs */}
        <div className="flex flex-col gap-4">

          <input
            type="text"
            placeholder="Email"
            className="w-full px-4 py-3 rounded-lg bg-[#020617]/80 border border-white/10 focus:outline-none focus:border-[#00F5C8]"
          />

          <input
            type="password"
            placeholder="Password"
            className="w-full px-4 py-3 rounded-lg bg-[#020617]/80 border border-white/10 focus:outline-none focus:border-[#00F5C8]"
          />

          {/* Login Button (placeholder) */}
          <button className="mt-2 py-3 rounded-lg font-bold tracking-widest uppercase bg-[#00F5C8] text-black hover:brightness-110 transition">
            Login
          </button>

          {/* Links */}
          <div className="text-center text-sm text-white/60">
            <button className="hover:text-[#00F5C8] transition">
              Forgot Password?
            </button>
          </div>

          <div className="text-center text-sm text-white/60">
            Don’t have an account?{" "}
            <Link to="/signup">
              <button className="text-[#00F5C8] hover:underline">
                Sign up
              </button>
            </Link>
          </div>
        </div>
      </div>  
    </div>
    </div>
  );
}
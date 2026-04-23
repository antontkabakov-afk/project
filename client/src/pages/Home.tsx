import { useEffect, useState } from 'react';
import { Link, Navigate } from 'react-router-dom'; 
import {logout, refresh} from"../api/auth";

import { MacbookScroll } from "@/components/ui/macbook-scroll";
import FloatingDockComponent from "../components/floating-dock-component";
import NoiseBackgroundIcon from "../components/noise-background-logo";
import MainBackround from "../components/main-backroudn";


import icon from "../../public/icon.png";

export default function Home() {
  const [isLogin, setIsLogin] = useState(false);
  const [isLogout, setIsLogout] = useState(false);
  const [userName, setUserName] = useState("");
  
  useEffect(() => {
    const run = async () => {
      const result = await refresh();
      console.log("refresh:", result);

      if (result?.isSuccess) {
        setIsLogin(true);
        setUserName(result.username);
      }
    };
    run();
  }, []);

  async function handleLogout() {  
    try {
      const result = await logout();
      console.log("logout in:", result);
      if (result) {
        setIsLogout(true);
      }
    } catch (err) {
      console.error(err);
    }
  }

  if (isLogout) {
    return <Navigate to="/login" replace />;
  }

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

        <div className="absolute top-12 right-10 mr-10 flex flex-col gap-8 items-end">
          {isLogin ? (
            <>
              <div className="flex items-center gap-3">
                <div className="px-4 py-2 rounded-full bg-white/5 border border-white/10 text-white text-sm font-medium tracking-wide backdrop-blur-md">
                  Hello {userName}
                </div>
          
                <button
                  className="shadow-[inset_0_0_0_2px_#616467] px-8 py-4 rounded-full font-bold text-white tracking-widest uppercase transform hover:scale-110 hover:bg-[#00F5C8] transition-colors duration-200 hover:shadow-2xl hover:shadow-[#00F5C8]/[0.5]"
                  onClick={handleLogout}
                >
                  Log out
                </button>
              </div>
            </>
          ) : (
            <>
              <Link to="/login">
                <button className="shadow-[inset_0_0_0_2px_#616467] px-8 py-4 rounded-full font-bold text-white tracking-widest uppercase transform hover:scale-110 hover:bg-[#00F5C8] transition-colors duration-200 hover:shadow-2xl hover:shadow-[#00F5C8]/[0.5]">
                  Log in
                </button>
              </Link>
          
              <Link to="/signup">
                <button className="shadow-[inset_0_0_0_2px_#616467] px-8 py-4 rounded-full font-bold text-white tracking-widest uppercase transform hover:scale-110 hover:bg-[#00F5C8] transition-colors duration-200 hover:shadow-2xl hover:shadow-[#00F5C8]/[0.5]">
                  Sign up
                </button>
              </Link>
            </>
          )}
        </div>

        <div className="w-full overflow-hidden">
          <MacbookScroll
            title={""}
            badge={<img src={icon} className="w-20 h-30" alt="icon" />}
            src={`/linear.webp`}
            showGradient={false}
          />
        </div>

      </div>
    </div>
  );
}

import { useEffect, useState } from "react";
import { Navigate, Outlet } from "react-router-dom";

import { refresh } from "@/api/auth";

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

export default function RequireAuth() {
  const [authStatus, setAuthStatus] = useState<AuthStatus>("loading");

  useEffect(() => {
    let isMounted = true;

    const run = async () => {
      try {
        const result = await refresh();

        if (!isMounted) {
          return;
        }

        setAuthStatus(result?.isSuccess ? "authenticated" : "unauthenticated");
      } catch {
        if (isMounted) {
          setAuthStatus("unauthenticated");
        }
      }
    };

    run();

    return () => {
      isMounted = false;
    };
  }, []);

  if (authStatus === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-[#0B0B0F] px-6 text-sm text-white/70">
        Checking session...
      </div>
    );
  }

  if (authStatus === "unauthenticated") {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}

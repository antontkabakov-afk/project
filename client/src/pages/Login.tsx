import { type FormEvent, useState } from "react";
import { Link, Navigate } from "react-router-dom";

import { getApiErrorMessage } from "@/api/client";
import { login } from "@/api/auth";
import FloatingDockComponent from "../components/floating-dock-component";
import MainBackround from "../components/main-backroudn";
import NoiseBackgroundIcon from "../components/noise-background-logo";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [shouldRedirect, setShouldRedirect] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage("");

    if (!email.trim() || !password) {
      setErrorMessage("Email and password are required.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await login(email.trim(), password);

      if (result?.isSuccess) {
        setShouldRedirect(true);
      }
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  }

  if (shouldRedirect) {
    return <Navigate to="/wallet" replace />;
  }

  return (
    <div className="relative w-full min-h-screen overflow-hidden bg-white dark:bg-[#0B0B0F]">
      <MainBackround />
      <div className="absolute left-12 top-12 h-40 w-40">
        <NoiseBackgroundIcon />
      </div>
      <div className="relative z-10">
        <div className="flex justify-center py-4">
          <FloatingDockComponent />
        </div>
        <div className="absolute top-12 right-10 mr-10 flex flex-col gap-8">
          <Link to="/login">
            <button className="rounded-full bg-[#00F5C8] px-8 py-4 font-bold uppercase tracking-widest text-black transition-colors duration-200 hover:scale-110 hover:brightness-110">
              Log in
            </button>
          </Link>

          <Link to="/signup">
            <button className="rounded-full px-8 py-4 font-bold uppercase tracking-widest text-white shadow-[inset_0_0_0_2px_#616467] transition-colors duration-200 hover:scale-110 hover:bg-[#00F5C8] hover:text-black hover:shadow-2xl hover:shadow-[#00F5C8]/[0.5]">
              Sign up
            </button>
          </Link>
        </div>
      </div>
      <div className="relative z-10 flex min-h-screen items-center justify-center">
        <div className="w-[360px] rounded-2xl border border-white/10 bg-[#0F172A]/80 p-6 text-white shadow-2xl backdrop-blur-xl">
          <div className="mb-6 flex items-center gap-4">
            <div className="h-20 w-20">
              <NoiseBackgroundIcon />
            </div>
            <h1 className="text-xl font-semibold">Crypto Tracker</h1>
          </div>

          <form className="flex flex-col gap-4" onSubmit={handleLogin}>
            <input
              autoComplete="email"
              className="w-full rounded-lg border border-white/10 bg-[#020617]/80 px-4 py-3 focus:border-[#00F5C8] focus:outline-none"
              onChange={(event) => setEmail(event.target.value)}
              placeholder="Email"
              type="email"
              value={email}
            />

            <input
              autoComplete="current-password"
              className="w-full rounded-lg border border-white/10 bg-[#020617]/80 px-4 py-3 focus:border-[#00F5C8] focus:outline-none"
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Password"
              type="password"
              value={password}
            />

            {errorMessage ? (
              <p className="text-sm text-rose-300">{errorMessage}</p>
            ) : null}

            <button
              className="mt-2 rounded-lg bg-[#00F5C8] py-3 font-bold uppercase tracking-widest text-black transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
              disabled={isSubmitting}
              type="submit"
            >
              {isSubmitting ? "Signing in..." : "Login"}
            </button>

            <div className="text-center text-sm text-white/60">
              Don&apos;t have an account?{" "}
              <Link to="/signup">
                <span className="text-[#00F5C8] hover:underline">Sign up</span>
              </Link>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

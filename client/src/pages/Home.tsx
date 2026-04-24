import { useEffect, useState } from "react";
import { Link, Navigate } from "react-router-dom";
import {
  Activity,
  ArrowRight,
  BarChart3,
  Clock3,
  Database,
  type LucideIcon,
  ShieldCheck,
  Wallet2,
} from "lucide-react";

import { getApiErrorMessage } from "@/api/client";
import { logout, refresh } from "@/api/auth";
import DashboardCard from "@/components/dashboard-card";
import FloatingDockComponent from "@/components/floating-dock-component";
import MainBackround from "@/components/main-backroudn";
import NoiseBackgroundIcon from "@/components/noise-background-logo";
import { MacbookScroll } from "@/components/ui/macbook-scroll";



const featureCards = [
  {
    title: "Month-first history",
    description:
      "Timeline and statistics now start on the last 30 days so the important movement is visible immediately.",
    icon: Clock3,
  },
  {
    title: "Stored portfolio state",
    description:
      "Every history point comes from an immutable database snapshot instead of a transient frontend cache.",
    icon: Database,
  },
  {
    title: "Live crypto pricing",
    description:
      "Tracked market assets stay current through backend price pulls and fall back gracefully when an upstream API has issues.",
    icon: Activity,
  },
];

const spotlightRows = [
  "Wallet connection and snapshot capture flow through one authenticated dashboard.",
  "History, statistics, and asset pricing read from backend endpoints instead of frontend mock state.",
  "Docker Compose runs the full stack behind one local entrypoint at http://localhost:8080.",
];

export default function Home() {
  const [isLogin, setIsLogin] = useState(false);
  const [isLogout, setIsLogout] = useState(false);
  const [userName, setUserName] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    const run = async () => {
      try {
        const result = await refresh();

        if (!isMounted || !result?.isSuccess) {
          return;
        }

        setIsLogin(true);
        setUserName(result.username);
      } catch (error) {
        if (isMounted) {
          setErrorMessage(getApiErrorMessage(error));
        }
      }
    };

    run();

    return () => {
      isMounted = false;
    };
  }, []);

  async function handleLogout() {
    setErrorMessage("");

    try {
      const result = await logout();

      if (result) {
        setIsLogin(false);
        setUserName("");
        setIsLogout(true);
      }
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    }
  }

  if (isLogout) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="relative min-h-screen overflow-hidden bg-[#04050A] text-white">
      <MainBackround />

      <div className="pointer-events-none absolute inset-0 overflow-hidden">
        <div className="absolute left-[-10%] top-24 h-72 w-72 rounded-full bg-[#00F5C8]/10 blur-3xl" />
        <div className="absolute bottom-0 right-[-8%] h-80 w-80 rounded-full bg-sky-500/10 blur-3xl" />
      </div>

      <div className="absolute left-8 top-8 z-10 h-28 w-28 md:left-12 md:top-10 md:h-36 md:w-36">
        <NoiseBackgroundIcon />
      </div>

      <div className="relative z-10 mx-auto max-w-7xl px-6 pb-20 pt-4">
        <div className="flex justify-center">
          <FloatingDockComponent />
        </div>

        {errorMessage ? (
          <div className="mx-auto -mt-4 max-w-2xl rounded-2xl border border-rose-400/25 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">
            {errorMessage}
          </div>
        ) : null}

        <section className="mt-10 overflow-hidden rounded-[36px] border border-white/10 bg-[#060B16]/80 px-4 pb-6 pt-8 shadow-[0_30px_90px_rgba(2,6,23,0.5)] backdrop-blur-xl md:px-8">
              
          <div className=" w-full overflow-hidden">
            <MacbookScroll
              title="Wallet data, stored snapshots..."
              badge={<img alt="logo" className="h-20 w-20" src="/icon.png" />}
              showGradient={false}
              src="/macScreen.png"
            />
          </div>
        </section>

        <section className="mt-6 grid gap-8 lg:grid-cols-[minmax(0,1.15fr)_minmax(340px,0.85fr)] lg:items-start">
          <div className="pt-10 md:pt-16">
            <div className="inline-flex items-center gap-2 rounded-full border border-[#00F5C8]/20 bg-[#00F5C8]/10 px-4 py-2 text-xs font-semibold uppercase tracking-[0.35em] text-[#8CFFE8]">
              <ShieldCheck className="h-4 w-4" />
              Last 30 days first
            </div>

            <h1 className="mt-6 max-w-4xl text-5xl font-semibold leading-[0.95] text-white md:text-7xl">
              See what your wallet actually did this month.
            </h1>

            <p className="mt-6 max-w-2xl text-base leading-8 text-white/70 md:text-lg">
              Crypto Tracker combines live wallet hydration, stored portfolio snapshots,
              and backend market pricing into a cleaner month-first dashboard for
              assets, history, and performance.
            </p>

            <div className="mt-8 flex flex-wrap items-center gap-4">
              {isLogin ? (
                <>
                  <div className="rounded-full border border-white/10 bg-white/5 px-5 py-3 text-sm font-medium tracking-wide text-white/90 backdrop-blur-md">
                    Welcome back, {userName || "builder"}
                  </div>
                  <Link
                    className="inline-flex items-center gap-3 rounded-full bg-[#00F5C8] px-7 py-4 text-sm font-bold uppercase tracking-[0.3em] text-black transition hover:scale-[1.02] hover:brightness-110"
                    to="/wallet"
                  >
                    Open dashboard
                    <ArrowRight className="h-4 w-4" />
                  </Link>
                  <button
                    className="rounded-full border border-white/15 bg-white/[0.04] px-7 py-4 text-sm font-bold uppercase tracking-[0.3em] text-white transition hover:border-[#00F5C8]/40 hover:text-[#00F5C8]"
                    onClick={handleLogout}
                    type="button"
                  >
                    Log out
                  </button>
                </>
              ) : (
                <>
                  <Link
                    className="inline-flex items-center gap-3 rounded-full bg-[#00F5C8] px-7 py-4 text-sm font-bold uppercase tracking-[0.3em] text-black transition hover:scale-[1.02] hover:brightness-110"
                    to="/signup"
                  >
                    Start tracking
                    <ArrowRight className="h-4 w-4" />
                  </Link>
                  <Link
                    className="rounded-full border border-white/15 bg-white/[0.04] px-7 py-4 text-sm font-bold uppercase tracking-[0.3em] text-white transition hover:border-[#00F5C8]/40 hover:text-[#00F5C8]"
                    to="/login"
                  >
                    Log in
                  </Link>
                </>
              )}
            </div>

            <div className="mt-10 grid gap-4 sm:grid-cols-3">
              <HeroStatCard
                eyebrow="Focus"
                value="30D"
                description="History and statistics now default to the latest month."
              />
              <HeroStatCard
                eyebrow="Tracking"
                value="Wallet + Prices"
                description="One place for live asset pricing and stored holdings snapshots."
              />
              <HeroStatCard
                eyebrow="Runtime"
                value="Docker"
                description="Frontend, API, and PostgreSQL run as one local stack."
              />
            </div>
          </div>

          <div className="relative pt-12 lg:pt-20">
            <div className="absolute inset-0 rounded-[36px] bg-gradient-to-br from-[#00F5C8]/10 via-sky-500/5 to-transparent blur-2xl" />

            <DashboardCard className="relative overflow-hidden rounded-[36px] border-white/15 bg-[#07101F]/85 p-0">
              <div className="border-b border-white/10 px-7 py-6">
                <p className="text-xs font-semibold uppercase tracking-[0.35em] text-[#8CFFE8]">
                  System brief
                </p>
                <h2 className="mt-3 text-2xl font-semibold text-white">
                  Built for clearer monthly readouts
                </h2>
                <p className="mt-3 text-sm leading-7 text-white/65">
                  The app is now tuned to show the last month first, while keeping the
                  full archive one click away for longer-term review.
                </p>
              </div>

              <div className="grid gap-4 px-7 py-6">
                <SignalRow
                  icon={Wallet2}
                  label="Wallet capture"
                  value="Authenticated snapshot flow"
                />
                <SignalRow
                  icon={BarChart3}
                  label="Performance lens"
                  value="30-day default with all-time fallback"
                />
                <SignalRow
                  icon={Activity}
                  label="Market state"
                  value="Live backend prices with resilient fallbacks"
                />
              </div>

              <div className="border-t border-white/10 bg-white/[0.03] px-7 py-6">
                <p className="text-xs font-semibold uppercase tracking-[0.35em] text-white/45">
                  Why it feels better
                </p>
                <div className="mt-4 grid gap-3">
                  {spotlightRows.map((row) => (
                    <div
                      className="rounded-2xl border border-white/10 bg-[#020617]/60 px-4 py-4 text-sm leading-7 text-white/70"
                      key={row}
                    >
                      {row}
                    </div>
                  ))}
                </div>
              </div>
            </DashboardCard>
          </div>
        </section>

        <section className="mt-10 grid gap-6 md:grid-cols-3">
          {featureCards.map((feature) => {
            const Icon = feature.icon;

            return (
              <DashboardCard className="rounded-[30px] border-white/10 bg-[#091323]/70" key={feature.title}>
                <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-[#00F5C8]/12 text-[#00F5C8]">
                  <Icon className="h-5 w-5" />
                </div>
                <h3 className="mt-5 text-xl font-semibold text-white">{feature.title}</h3>
                <p className="mt-3 text-sm leading-7 text-white/65">{feature.description}</p>
              </DashboardCard>
            );
          })}
        </section>

        
      </div>
    </div>
  );
}

interface HeroStatCardProps {
  eyebrow: string;
  value: string;
  description: string;
}

function HeroStatCard({ eyebrow, value, description }: HeroStatCardProps) {
  return (
    <div className="rounded-[28px] border border-white/10 bg-white/[0.03] px-5 py-5 shadow-[0_20px_60px_rgba(2,6,23,0.25)] backdrop-blur-md">
      <p className="text-xs font-semibold uppercase tracking-[0.35em] text-white/45">
        {eyebrow}
      </p>
      <p className="mt-4 text-2xl font-semibold text-white">{value}</p>
      <p className="mt-3 text-sm leading-6 text-white/60">{description}</p>
    </div>
  );
}

interface SignalRowProps {
  icon: LucideIcon;
  label: string;
  value: string;
}

function SignalRow({ icon: Icon, label, value }: SignalRowProps) {
  return (
    <div className="flex items-center justify-between rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-4">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-[#00F5C8]/12 text-[#00F5C8]">
          <Icon className="h-4 w-4" />
        </div>
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-white/45">{label}</p>
          <p className="mt-1 text-sm text-white/85">{value}</p>
        </div>
      </div>
    </div>
  );
}

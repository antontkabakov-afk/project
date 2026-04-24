import type { ReactNode } from "react";

interface DashboardCardProps {
  children: ReactNode;
  className?: string;
}

export default function DashboardCard({
  children,
  className,
}: DashboardCardProps) {
  return (
    <section
      className={[
        "rounded-[28px] border border-white/10 bg-[#0F172A]/75 p-6 text-white shadow-[0_25px_80px_rgba(2,6,23,0.5)] backdrop-blur-xl",
        className,
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {children}
    </section>
  );
}

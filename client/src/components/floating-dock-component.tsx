import { FloatingDock } from "@/components/ui/floating-dock";
import { ChartPie, Coins, House, Landmark, ScanSearch } from "lucide-react";

export default function FloatingDockComponent() {
  const links = [
    {
      title: "Home",
      icon: <House className="h-full w-full" />,
      href: "/",
      end: true,
    },
    {
      title: "Wallet",
      icon: <Landmark className="h-full w-full" />,
      href: "/wallet",
    },
    {
      title: "Assets",
      icon: <Coins className="h-full w-full" />,
      href: "/assets",
    },
    {
      title: "History",
      icon: <ScanSearch className="h-full w-full" />,
      href: "/history",
    },
    {
      title: "Statistics",
      icon: <ChartPie className="h-full w-full" />,
      href: "/statistics",
    },
  ];
  return (
    <div className="flex h-[10rem] w-full items-center justify-center">
      <FloatingDock items={links} desktopClassName="mx-0" />
    </div>
  );
}

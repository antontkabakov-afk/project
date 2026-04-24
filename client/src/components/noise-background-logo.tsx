import { NoiseBackground } from "@/components/ui/noise-background";

import icon from "../../public/icon.png";

export default function NoiseBackgroundIcon() {
  return (
    <NoiseBackground
          containerClassName=" rounded-full overflow-hidden flex items-center justify-center"
          gradientColors={[
            "rgb(0, 255, 200)", 
            "rgb(0, 220, 255)", 
            "rgb(100, 255, 180)"
          ]}
        >
          <img
            src={icon}
            alt="side"
            className="w-full h-full object-cover"
          />
        </NoiseBackground>
  );
}

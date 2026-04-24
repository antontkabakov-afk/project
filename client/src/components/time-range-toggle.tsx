export type TimeRangeOption = "30d" | "all";

interface TimeRangeToggleProps {
  value: TimeRangeOption;
  onChange: (value: TimeRangeOption) => void;
}

const options: Array<{ label: string; value: TimeRangeOption }> = [
  { label: "Last 30D", value: "30d" },
  { label: "All time", value: "all" },
];

export default function TimeRangeToggle({
  value,
  onChange,
}: TimeRangeToggleProps) {
  return (
    <div className="inline-flex rounded-full border border-white/10 bg-white/[0.03] p-1">
      {options.map((option) => {
        const isActive = option.value === value;

        return (
          <button
            className={[
              "rounded-full px-4 py-2 text-xs font-semibold uppercase tracking-[0.3em] transition",
              isActive
                ? "bg-[#00F5C8] text-black shadow-[0_10px_30px_rgba(0,245,200,0.2)]"
                : "text-white/60 hover:text-white",
            ].join(" ")}
            key={option.value}
            onClick={() => onChange(option.value)}
            type="button"
          >
            {option.label}
          </button>
        );
      })}
    </div>
  );
}

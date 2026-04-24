"use client";

import { useState, useRef, useEffect, useId } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { cn } from "@/lib/utils";

/**
 * HELPER COMPONENT: GooeyFilter
 * This MUST be in the same file or imported.
 */
function GooeyFilter({ filterId, blur }: { filterId: string; blur: number }) {
  return (
    <svg className="absolute hidden h-0 w-0" aria-hidden>
      <defs>
        <filter id={filterId} x="-50%" y="-50%" width="200%" height="200%">
          <feGaussianBlur in="SourceGraphic" stdDeviation={blur} result="blur" />
          <feColorMatrix
            in="blur"
            type="matrix"
            values="1 0 0 0 0  0 1 0 0 0  0 0 1 0 0  0 0 0 20 -10"
            result="goo"
          />
          <feComposite in="SourceGraphic" in2="goo" operator="atop" />
        </filter>
      </defs>
    </svg>
  );
}

/**
 * HELPER COMPONENT: SearchIcon
 */
function SearchIcon({ layoutId }: { layoutId: string }) {
  return (
    <motion.svg
      layoutId={layoutId}
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth={2}
      className="size-4 shrink-0"
    >
      <circle cx="11" cy="11" r="8" />
      <path d="m21 21-4.3-4.3" />
    </motion.svg>
  );
}

const transition = { 
  type: "spring" as const, 
  stiffness: 260, 
  damping: 20, 
  mass: 0.8 
};

export function GooeyInput({
  placeholder = "Search...",
  className,
  collapsedWidth = 48,
  expandedWidth = 350,
  gooeyBlur = 5,
}: {
  placeholder?: string;
  className?: string;
  collapsedWidth?: number;
  expandedWidth?: number;
  gooeyBlur?: number;
}) {
  const reactId = useId();
  const safeId = reactId.replace(/:/g, "");
  const filterId = `gooey-filter-${safeId}`;
  const iconLayoutId = `gooey-input-icon-${safeId}`;
  
  const inputRef = useRef<HTMLInputElement>(null);
  const [isExpanded, setIsExpanded] = useState(false);
  const [value, setValue] = useState("");

  useEffect(() => {
    if (isExpanded) {
      inputRef.current?.focus();
    }
  }, [isExpanded]);

  const surfaceClass = "bg-white text-black shadow-xl ring-1 ring-black/5 dark:bg-neutral-900 dark:text-white dark:ring-white/10";

  return (
  <div className={cn("relative flex items-center justify-center", className)}>
    <GooeyFilter filterId={filterId} blur={gooeyBlur} />
    
    {/* CHANGED: h-12 here instead of h-16 matches the dock height better */}
    <div className="relative flex h-12 items-center" style={{ filter: `url(#${filterId})` }}>
      <motion.div
        layout
        initial={false}
        animate={{ 
          width: isExpanded ? expandedWidth : collapsedWidth,
          paddingLeft: isExpanded ? 52 : 0,
          paddingRight: isExpanded ? 16 : 0,
        }}
        transition={transition}
        onClick={() => {
          if (!isExpanded) {
            setIsExpanded(true);
          }
        }}
        className={cn(
          "flex h-12 items-center rounded-full overflow-hidden shrink-0 cursor-pointer", 
          !isExpanded && "justify-center",
          surfaceClass
        )}
      >
        <AnimatePresence mode="wait">
          {!isExpanded && (
            <motion.div
              key="collapsed-icon"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center justify-center"
            >
              <SearchIcon layoutId={iconLayoutId} />
            </motion.div>
          )}
        </AnimatePresence>
        {isExpanded && (
            <motion.input
          ref={inputRef}
          layout
          type="text"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onBlur={() => !value && setIsExpanded(false)}
          placeholder={placeholder}
          animate={{ opacity: isExpanded ? 1 : 0 }}
          className="bg-transparent outline-none flex-1 min-w-0 text-sm placeholder:text-neutral-500"
        />
          )}
        
      </motion.div>

      <motion.div
        initial={false}
        animate={{ 
          scale: isExpanded ? 1 : 0, 
          opacity: isExpanded ? 1 : 0,
        }}
        transition={transition}
        // CHANGED: absolute top-0 ensures it stays perfectly aligned with the parent
        className={cn(
          "absolute top-0 left-0 size-12 flex items-center justify-center rounded-full shrink-0 z-10 pointer-events-none", 
          surfaceClass
        )}
      >
        <SearchIcon layoutId={iconLayoutId} />
      </motion.div>
    </div>
  </div>
);
}

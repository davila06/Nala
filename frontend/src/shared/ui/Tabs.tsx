import { type ReactNode } from "react";
import { AnimatePresence, motion } from "framer-motion";

// ── Types ─────────────────────────────────────────────────────────────────────

export interface TabItem {
  id: string;
  label: string;
  /** Optional icon: emoji, SVG element, or text */
  icon?: ReactNode;
  /** Show a numeric badge (e.g. unread count) */
  badge?: number;
  disabled?: boolean;
}

type TabVariant = "pills" | "underline" | "boxed";

interface TabsProps {
  tabs: TabItem[];
  activeId: string;
  onChange: (id: string) => void;
  variant?: TabVariant;
  children?: ReactNode;
  /** If provided, renders tab panels with AnimatePresence */
  panels?: Record<string, ReactNode>;
  className?: string;
}

// ── Styles per variant ────────────────────────────────────────────────────────

const WRAPPER_CLS: Record<TabVariant, string> = {
  pills: "flex gap-1 rounded-2xl bg-sand-100 p-1.5",
  underline: "flex gap-0 border-b border-sand-200",
  boxed: "grid border-b border-sand-200",
};

function tabCls(
  variant: TabVariant,
  isActive: boolean,
  isDisabled: boolean,
): string {
  const base =
    "relative flex items-center justify-center gap-1.5 text-sm font-semibold transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400";
  const disabled = "opacity-40 cursor-not-allowed";

  const variants: Record<TabVariant, { active: string; inactive: string }> = {
    pills: {
      active:
        "rounded-xl bg-[var(--color-surface)] px-4 py-2.5 text-sand-900 shadow-sm",
      inactive:
        "rounded-xl px-4 py-2.5 text-sand-500 hover:bg-[var(--color-surface)]/60 hover:text-sand-800",
    },
    underline: {
      active:
        "px-4 pb-3 pt-2 text-brand-600 border-b-2 border-brand-500 -mb-px",
      inactive:
        "px-4 pb-3 pt-2 text-sand-500 hover:text-sand-800 border-b-2 border-transparent -mb-px",
    },
    boxed: {
      active:
        "px-3 py-3 text-sand-900 bg-[var(--color-surface)] border-b-2 border-brand-500",
      inactive:
        "px-3 py-3 text-sand-500 hover:bg-[var(--color-surface)]/60 hover:text-sand-800 border-b-2 border-transparent",
    },
  };

  return [
    base,
    isActive ? variants[variant].active : variants[variant].inactive,
    isDisabled ? disabled : "",
  ].join(" ");
}

// ── Tabs component ────────────────────────────────────────────────────────────

export function Tabs({
  tabs,
  activeId,
  onChange,
  variant = "pills",
  panels,
  className = "",
}: TabsProps) {
  const wrapperStyle =
    variant === "boxed"
      ? { gridTemplateColumns: `repeat(${tabs.length}, minmax(0, 1fr))` }
      : undefined;

  return (
    <div className={className}>
      {/* Tab bar */}
      <div
        role="tablist"
        aria-label="Navegación por pestañas"
        className={WRAPPER_CLS[variant]}
        style={wrapperStyle}
      >
        {tabs.map((tab) => {
          const isActive = tab.id === activeId;
          return (
            <button
              key={tab.id}
              role="tab"
              type="button"
              id={`tab-${tab.id}`}
              aria-selected={isActive}
              aria-controls={`panel-${tab.id}`}
              disabled={tab.disabled}
              onClick={() => !tab.disabled && onChange(tab.id)}
              className={tabCls(variant, isActive, !!tab.disabled)}
            >
              {tab.icon && <span aria-hidden="true">{tab.icon}</span>}
              {tab.label}
              {tab.badge != null && tab.badge > 0 && (
                <span className="ml-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-danger-500 px-1 text-[10px] font-bold text-white">
                  {tab.badge > 99 ? "99+" : tab.badge}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* Animated panels (optional) */}
      {panels && (
        <AnimatePresence mode="wait" initial={false}>
          {tabs.map((tab) =>
            tab.id === activeId ? (
              <motion.div
                key={tab.id}
                id={`panel-${tab.id}`}
                role="tabpanel"
                aria-labelledby={`tab-${tab.id}`}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -4 }}
                transition={{ duration: 0.18, ease: [0.4, 0, 0.2, 1] }}
              >
                {panels[tab.id]}
              </motion.div>
            ) : null,
          )}
        </AnimatePresence>
      )}
    </div>
  );
}

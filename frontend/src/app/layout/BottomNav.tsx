import { NavLink, useLocation } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import { HeartPulse, MapPin, PawPrint, Users } from "lucide-react";

interface NavItem {
  to: string;
  label: string;
  icon: (active: boolean) => React.ReactNode;
}

const NAV_ITEMS: NavItem[] = [
  {
    to: "/dashboard",
    label: "Mascota",
    icon: (active) => <PawPrint className="h-5 w-5" fill={active ? "currentColor" : "none"} aria-hidden="true" />,
  },
  {
    to: "/map",
    label: "Encontrar",
    icon: (active) => <MapPin className="h-5 w-5" fill={active ? "currentColor" : "none"} aria-hidden="true" />,
  },
  {
    to: "/salud",
    label: "Salud",
    icon: (active) => <HeartPulse className="h-5 w-5" fill={active ? "currentColor" : "none"} aria-hidden="true" />,
  },
  {
    to: "/red",
    label: "Red",
    icon: (active) => <Users className="h-5 w-5" fill={active ? "currentColor" : "none"} aria-hidden="true" />,
  },
];

/** Mobile-only bottom navigation bar. Hidden on md+ screens. */
export function BottomNav() {
  const location = useLocation();

  return (
    <nav
      aria-label="Navegación inferior"
      className="fixed inset-x-0 bottom-0 z-40 border-t border-sand-200 bg-surface/95 backdrop-blur-sm md:hidden"
      style={{ paddingBottom: "env(safe-area-inset-bottom)" }}
    >
      <div className="flex h-16 items-center justify-around">
        {NAV_ITEMS.map((item) => {
          const isActive = location.pathname === item.to || (item.to !== "/" && location.pathname.startsWith(item.to));

          return (
            <NavLink
              key={item.to}
              to={item.to}
              aria-label={item.label}
              aria-current={isActive ? "page" : undefined}
              className="relative flex flex-1 flex-col items-center justify-center gap-0.5 py-1"
            >
              {/* Sliding pill indicator */}
              <AnimatePresence>
                {isActive && (
                  <motion.span
                    layoutId="bottom-nav-indicator"
                    className="absolute inset-x-2 top-0 h-0.5 rounded-full bg-brand-500"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    transition={{ type: "spring", stiffness: 500, damping: 40 }}
                  />
                )}
              </AnimatePresence>

              {/* Icon with scale spring on active */}
              <motion.span
                animate={{ scale: isActive ? 1.15 : 1 }}
                transition={{ type: "spring", stiffness: 400, damping: 28 }}
                className={isActive ? "text-brand-600" : "text-sand-500"}
              >
                {item.icon(isActive)}
              </motion.span>

              <span
                className={[
                  "text-[10px] font-semibold leading-none transition-colors",
                  isActive ? "text-brand-600" : "text-sand-400",
                ].join(" ")}
              >
                {item.label}
              </span>
            </NavLink>
          );
        })}
      </div>
    </nav>
  );
}

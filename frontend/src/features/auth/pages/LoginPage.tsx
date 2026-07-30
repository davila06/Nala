import { useState, useEffect, useRef } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useLogin } from "../hooks/useAuth";
import { Button } from "@/shared/ui/Button";
import { Input } from "@/shared/ui/Input";
import { Alert } from "@/shared/ui/Alert";

// ── Count-up hook ─────────────────────────────────────────────────────────────

function useCountUp(target: number, duration = 1800, start = false) {
  const [value, setValue] = useState(0);
  useEffect(() => {
    if (!start) return;
    const startTime = performance.now();
    const tick = (now: number) => {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      // Ease-out cubic
      const eased = 1 - Math.pow(1 - progress, 3);
      setValue(Math.round(eased * target));
      if (progress < 1) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }, [target, duration, start]);
  return value;
}

// ── Ambient floating paw prints ───────────────────────────────────────────────

const PAWS = [
  { left: "8%", animDur: "7s", size: "1.4rem", delay: "0s", opacity: 0.12 },
  { left: "22%", animDur: "9s", size: "1rem", delay: "1.2s", opacity: 0.08 },
  { left: "50%", animDur: "11s", size: "1.8rem", delay: "0.5s", opacity: 0.1 },
  { left: "70%", animDur: "8s", size: "1.2rem", delay: "2.1s", opacity: 0.09 },
  { left: "88%", animDur: "10s", size: "1.5rem", delay: "3.4s", opacity: 0.07 },
];

function AmbientPaws() {
  return (
    <>
      {PAWS.map((p, i) => (
        <span
          key={i}
          aria-hidden="true"
          style={{
            position: "absolute",
            left: p.left,
            bottom: "-2rem",
            fontSize: p.size,
            opacity: p.opacity,
            animation: `float-bob ${p.animDur} ease-in-out ${p.delay} infinite`,
            userSelect: "none",
            pointerEvents: "none",
          }}
        >
          🐾
        </span>
      ))}
    </>
  );
}

// ── Animated stat item ────────────────────────────────────────────────────────

interface StatItemProps {
  end: number;
  suffix: string;
  label: string;
  started: boolean;
}

function StatItem({ end, suffix, label, started }: StatItemProps) {
  const count = useCountUp(end, 1600, started);
  return (
    <div>
      <p className="font-display text-2xl font-semibold text-brand-400">
        {count.toLocaleString("es-CR")}
        {suffix}
      </p>
      <p className="text-xs text-trust-300 mt-0.5">{label}</p>
    </div>
  );
}

// ── Brand panel ───────────────────────────────────────────────────────────────

function BrandPanel() {
  const panelRef = useRef<HTMLDivElement>(null);
  const [statsStarted, setStatsStarted] = useState(false);

  // Start count-up when the panel becomes visible
  useEffect(() => {
    const el = panelRef.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) setStatsStarted(true);
      },
      { threshold: 0.3 },
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <div
      ref={panelRef}
      className="hidden lg:flex flex-col justify-between bg-trust-900 bg-topo px-12 py-14 text-white overflow-hidden"
      aria-hidden="true"
      style={{ position: "relative" }}
    >
      {/* Ambient paw prints */}
      <AmbientPaws />

      {/* Logo */}
      <div className="flex items-center gap-3 relative z-10">
        <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-brand-500 text-xl">
          🐾
        </span>
        <span className="font-display text-2xl font-semibold tracking-tight">
          PawTrack CR
        </span>
      </div>

      {/* Central copy */}
      <div className="space-y-6 relative z-10">
        <p className="font-display text-4xl leading-snug font-medium text-balance">
          Cada mascota merece volver
          <br />
          <em className="not-italic text-brand-400">a casa.</em>
        </p>
        <p className="text-trust-200 text-base leading-relaxed max-w-sm">
          Identidad digital, seguimiento en tiempo real y una red comunitaria de
          rescate para mascotas.
        </p>
      </div>

      {/* Animated stats */}
      <div className="flex gap-8 relative z-10">
        <StatItem
          end={12000}
          suffix="+"
          label="mascotas registradas"
          started={statsStarted}
        />
        <StatItem
          end={94}
          suffix=" %"
          label="tasa de recuperación"
          started={statsStarted}
        />
        <StatItem
          end={480}
          suffix="+"
          label="aliados verificados"
          started={statsStarted}
        />
      </div>
    </div>
  );
}

// ── Login form ───────────────────────────────────────────────────────────────

export default function LoginPage() {
  const { mutate: login, isPending, error } = useLogin();
  const [searchParams] = useSearchParams();
  const justRegistered = searchParams.get("registered") === "true";

  const [form, setForm] = useState({ email: "", password: "" });
  const [showPassword, setShowPassword] = useState(false);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    login({ email: form.email, password: form.password });
  }

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[1fr_520px]">
      <BrandPanel />

      {/* Form panel */}
      <div className="flex min-h-dvh flex-col items-center justify-center px-6 py-12 lg:px-12 bg-surface">
        {/* Mobile logo */}
        <div className="mb-10 flex items-center gap-2.5 lg:hidden">
          <span
            className="flex h-9 w-9 items-center justify-center rounded-2xl bg-brand-500 text-lg text-white"
            aria-hidden="true"
          >
            🐾
          </span>
          <span className="font-display text-xl font-semibold text-sand-900">
            PawTrack CR
          </span>
        </div>

        <div className="w-full max-w-sm">
          <div className="mb-8">
            <h1 className="font-display text-3xl font-semibold text-sand-900 text-balance">
              Bienvenido de vuelta
            </h1>
            <p className="mt-2 text-sm text-sand-500">
              Ingresa para acceder al panel de tu mascota.
            </p>
          </div>

          {justRegistered && (
            <Alert variant="success" className="mb-6">
              Cuenta creada exitosamente. Revisa tu correo para verificarla.
            </Alert>
          )}

          {error && (
            <Alert variant="error" className="mb-6">
              Credenciales incorrectas. Verifica tu correo y contraseña.
            </Alert>
          )}

          <form onSubmit={handleSubmit} noValidate className="space-y-5">
            <Input
              label="Correo electrónico"
              type="email"
              id="email"
              autoComplete="email"
              inputMode="email"
              required
              autoFocus
              placeholder="tu@correo.com"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />

            <div>
              <Input
                label="Contraseña"
                type={showPassword ? "text" : "password"}
                id="password"
                autoComplete="current-password"
                required
                placeholder="••••••••"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                className="mt-1.5 rounded px-1 py-2 text-xs text-sand-500 hover:text-sand-700 transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                aria-label="Alternar visibilidad"
                aria-pressed={showPassword}
              >
                {showPassword ? "Ocultar" : "Mostrar"}
              </button>
            </div>

            <div className="flex items-center justify-end">
              <Link
                to="/forgot-password"
                className="rounded text-xs text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                ¿Olvidaste tu contraseña?
              </Link>
            </div>

            <Button type="submit" loading={isPending} fullWidth size="lg">
              {isPending ? "Ingresando…" : "Ingresar"}
            </Button>
          </form>

          <p className="mt-8 text-center text-sm text-sand-500">
            ¿No tienes cuenta?{" "}
            <Link
              to="/register"
              className="rounded font-semibold text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
            >
              Regístrate gratis
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

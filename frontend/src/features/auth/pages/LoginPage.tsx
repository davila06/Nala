import { useState, useEffect, useRef } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useLogin } from "../hooks/useAuth";
import { useRecoveryOverview } from "@/features/lost-pets/hooks/useRecoveryStats";
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

// ── Eye icon SVG (show/hide password) ────────────────────────────────────────

function EyeIcon({ open }: { open: boolean }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      className="h-4 w-4"
      aria-hidden="true"
    >
      {open ? (
        <>
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94"
          />
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19"
          />
          <line x1="1" y1="1" x2="23" y2="23" />
        </>
      ) : (
        <>
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"
          />
          <circle cx="12" cy="12" r="3" />
        </>
      )}
    </svg>
  );
}

// ── Error message extractor ───────────────────────────────────────────────────

function extractLoginError(err: unknown): string {
  if (!err) return "";
  const axiosErr = err as {
    response?: { data?: { detail?: string; title?: string }; status?: number };
  };
  const status = axiosErr.response?.status;
  const detail = axiosErr.response?.data?.detail;

  if (status === 423)
    return "Cuenta bloqueada temporalmente por múltiples intentos fallidos. Intenta en 15 minutos.";
  if (detail?.toLowerCase().includes("locked"))
    return "Cuenta bloqueada temporalmente. Intenta en 15 minutos.";
  if (detail?.toLowerCase().includes("verified"))
    return "Debes verificar tu correo antes de iniciar sesión. Revisa tu bandeja de entrada.";
  if (status === 401) return "Correo o contraseña incorrectos.";
  return "No se pudo iniciar sesión. Intenta de nuevo.";
}

// ── Brand panel ───────────────────────────────────────────────────────────────

function BrandPanel() {
  const panelRef = useRef<HTMLDivElement>(null);
  const [statsStarted, setStatsStarted] = useState(false);
  const { data: overview } = useRecoveryOverview();

  // Real stats when available, fallback to seed values while loading
  const totalReunited = overview?.recoveredCount ?? 0;
  const recoveryPct = overview
    ? Math.round(overview.overallRecoveryRate * 100)
    : 0;
  const totalReports = overview?.totalReports ?? 0;

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
      <AmbientPaws />

      <div className="flex items-center gap-3 relative z-10">
        <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-brand-500 text-xl">
          🐾
        </span>
        <span className="font-display text-2xl font-semibold tracking-tight">
          PawTrack CR
        </span>
      </div>

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

      <div className="flex gap-8 relative z-10">
        {totalReports > 0 ? (
          <>
            <StatItem
              end={totalReports}
              suffix="+"
              label="casos atendidos"
              started={statsStarted}
            />
            <StatItem
              end={recoveryPct}
              suffix=" %"
              label="tasa de recuperación"
              started={statsStarted}
            />
            <StatItem
              end={totalReunited}
              suffix="+"
              label="mascotas reunidas"
              started={statsStarted}
            />
          </>
        ) : (
          <>
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
          </>
        )}
      </div>
    </div>
  );
}

// ── Login form ───────────────────────────────────────────────────────────────

export default function LoginPage() {
  const [searchParams] = useSearchParams();
  const returnTo = searchParams.get("return") ?? undefined;
  const justRegistered = searchParams.get("registered") === "true";

  const { mutate: login, isPending, error } = useLogin(returnTo);

  // J — Restore remembered email from last session
  const [form, setForm] = useState(() => ({
    email: localStorage.getItem("pawtrack:lastEmail") ?? "",
    password: "",
  }));
  const [showPassword, setShowPassword] = useState(false);
  const [emailTouched, setEmailTouched] = useState(
    () => !!localStorage.getItem("pawtrack:lastEmail"),
  );
  // M — Debounce validation: only show error after 400ms of inactivity post-blur
  const emailDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [emailValidNow, setEmailValidNow] = useState(false);

  const emailInvalid =
    emailValidNow &&
    emailTouched &&
    form.email.length > 0 &&
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email);
  const errorMsg = extractLoginError(error);

  const handleEmailBlur = () => {
    setEmailTouched(true);
    if (emailDebounceRef.current) clearTimeout(emailDebounceRef.current);
    emailDebounceRef.current = setTimeout(() => setEmailValidNow(true), 400);
  };

  const handleEmailChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, email: e.target.value });
    setEmailValidNow(false); // reset until debounce fires again
  };

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    // J — Persist email for next visit
    localStorage.setItem("pawtrack:lastEmail", form.email);
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
            <Alert
              variant="error"
              className="mb-6"
              id="login-error"
              role="alert"
            >
              {errorMsg}
            </Alert>
          )}

          <form onSubmit={handleSubmit} noValidate className="space-y-5">
            <div>
              <Input
                label="Correo electrónico"
                type="email"
                id="email"
                autoComplete="email"
                inputMode="email"
                autoCapitalize="none"
                autoCorrect="off"
                spellCheck={false}
                required
                autoFocus
                placeholder="tu@correo.com"
                value={form.email}
                onChange={handleEmailChange}
                onBlur={handleEmailBlur}
                aria-describedby={
                  emailInvalid
                    ? "email-error"
                    : error
                      ? "login-error"
                      : undefined
                }
                aria-invalid={emailInvalid || undefined}
              />
              {emailInvalid && (
                <p
                  id="email-error"
                  className="mt-1.5 text-xs text-danger-600"
                  role="alert"
                >
                  Ingresa un correo electrónico válido.
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="password"
                className="mb-1.5 block text-sm font-medium text-sand-700"
              >
                Contraseña
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  autoCapitalize="none"
                  autoCorrect="off"
                  spellCheck={false}
                  required
                  placeholder="••••••••"
                  value={form.password}
                  onChange={(e) =>
                    setForm({ ...form, password: e.target.value })
                  }
                  aria-describedby={error ? "login-error" : undefined}
                  className="block w-full rounded-xl border border-sand-300 bg-surface py-2.5 pl-3.5 pr-10 text-sm text-sand-900 shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200 placeholder:text-sand-400"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  tabIndex={-1}
                  aria-label={
                    showPassword ? "Ocultar contraseña" : "Mostrar contraseña"
                  }
                  aria-pressed={showPassword}
                  className="absolute inset-y-0 right-0 flex items-center px-3 text-sand-400 hover:text-sand-700 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-inset rounded-r-xl"
                >
                  <EyeIcon open={showPassword} />
                </button>
              </div>
            </div>

            <div className="flex items-center justify-end">
              <Link
                to="/forgot-password"
                className="rounded text-xs text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              >
                ¿Olvidaste tu contraseña?
              </Link>
            </div>

            <Button
              type="submit"
              loading={isPending}
              disabled={emailInvalid}
              fullWidth
              size="lg"
            >
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

          {/* L — Escape hatch for users who don't want to log in */}
          <p className="mt-4 text-center">
            <Link
              to="/map"
              className="text-xs text-sand-400 hover:text-sand-600 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 rounded"
            >
              Explorar el mapa público sin cuenta
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}

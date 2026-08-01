import { useState, useEffect, useRef, useCallback } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { motion, AnimatePresence } from "framer-motion";
import { createPortal } from "react-dom";
import { useLogin, useForgotPassword } from "../hooks/useAuth";
import { useRecoveryOverview } from "@/features/lost-pets/hooks/useRecoveryStats";
import { Button } from "@/shared/ui/Button";
import { Input } from "@/shared/ui/Input";
import { Alert } from "@/shared/ui/Alert";

// ── Tilt hook — tracks mouse position relative to an element ─────────────────

function useTilt(maxAngle: number) {
  const [tilt, setTilt] = useState({ x: 0, y: 0 });

  const onMove = useCallback(
    (e: React.MouseEvent<HTMLElement>) => {
      const rect = e.currentTarget.getBoundingClientRect();
      const cx = (e.clientX - rect.left) / rect.width - 0.5;
      const cy = (e.clientY - rect.top) / rect.height - 0.5;
      setTilt({ x: -cy * maxAngle, y: cx * maxAngle });
    },
    [maxAngle],
  );

  const onLeave = useCallback(() => setTilt({ x: 0, y: 0 }), []);

  return { tilt, onMove, onLeave };
}

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

// ── Holographic stat card — holo-card CSS + mouse-tracked tilt ────────────────

interface HoloStatItemProps {
  end: number;
  suffix: string;
  label: string;
  started: boolean;
}

function HoloStatItem({ end, suffix, label, started }: HoloStatItemProps) {
  const count = useCountUp(end, 1600, started);
  const { tilt, onMove, onLeave } = useTilt(8);

  return (
    <div
      className="holo-card flex-1 rounded-2xl px-4 py-3 bg-white/5 border border-white/10 select-none cursor-default"
      onMouseMove={onMove}
      onMouseLeave={onLeave}
      style={{
        transform: `perspective(600px) rotateX(${tilt.x}deg) rotateY(${tilt.y}deg)`,
        transition: "transform 0.08s linear, box-shadow 0.1s linear",
        willChange: "transform",
      }}
    >
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

// ── Error helpers ─────────────────────────────────────────────────────────────

type AxiosLike = {
  response?: { data?: { detail?: string; title?: string }; status?: number };
};

function extractLoginError(err: unknown): string {
  if (!err) return "";
  const e = err as AxiosLike;
  const status = e.response?.status;
  const detail = e.response?.data?.detail;
  if (status === 423)
    return "Cuenta bloqueada temporalmente por múltiples intentos fallidos. Intenta en 15 minutos.";
  if (detail?.toLowerCase().includes("locked"))
    return "Cuenta bloqueada temporalmente. Intenta en 15 minutos.";
  if (detail?.toLowerCase().includes("verified"))
    return "Debes verificar tu correo antes de iniciar sesión.";
  if (status === 401) return "Correo o contraseña incorrectos.";
  return "No se pudo iniciar sesión. Intenta de nuevo.";
}

function isVerifiedError(err: unknown): boolean {
  return (
    (err as AxiosLike | null)?.response?.data?.detail
      ?.toLowerCase()
      .includes("verified") === true
  );
}

// ── Brand panel ───────────────────────────────────────────────────────────────

function BrandPanel() {
  const panelRef = useRef<HTMLDivElement>(null);
  const [statsStarted, setStatsStarted] = useState(false);
  const { data: overview } = useRecoveryOverview();

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

      {/* Holographic stat cards — mouse-tracked 3D tilt */}
      <div className="flex gap-3 relative z-10">
        {totalReunited > 0 ? (
          <>
            <HoloStatItem
              end={totalReports}
              suffix="+"
              label="casos atendidos"
              started={statsStarted}
            />
            <HoloStatItem
              end={recoveryPct}
              suffix=" %"
              label="tasa de recuperación"
              started={statsStarted}
            />
            <HoloStatItem
              end={totalReunited}
              suffix="+"
              label="mascotas reunidas"
              started={statsStarted}
            />
          </>
        ) : (
          <>
            <HoloStatItem
              end={12000}
              suffix="+"
              label="mascotas registradas"
              started={statsStarted}
            />
            <HoloStatItem
              end={94}
              suffix=" %"
              label="tasa de recuperación"
              started={statsStarted}
            />
            <HoloStatItem
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

// ── Verify email modal — 3D Framer Motion entrance (rotateX perspective) ─────

interface VerifyEmailModalProps {
  email: string;
  onClose: () => void;
}

function VerifyEmailModal({ email, onClose }: VerifyEmailModalProps) {
  const contentRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const prev = document.activeElement as HTMLElement | null;
    contentRef.current?.focus();
    return () => prev?.focus();
  }, []);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [onClose]);

  return createPortal(
    <>
      {/* Backdrop — click outside to dismiss */}
      <motion.div
        className="fixed inset-0 z-50 bg-zinc-900/60 backdrop-blur-sm"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.22 }}
        onClick={onClose}
        aria-hidden="true"
      />
      {/* Modal wrapper — pointer-events-none lets backdrop receive clicks */}
      <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-4 pointer-events-none">
        <motion.div
          ref={contentRef}
          role="dialog"
          aria-modal="true"
          aria-labelledby="verify-modal-title"
          tabIndex={-1}
          className="w-full max-w-md rounded-3xl bg-white shadow-2xl p-8 outline-none pointer-events-auto"
          initial={{ opacity: 0, y: 36, rotateX: -14, scale: 0.93 }}
          animate={{ opacity: 1, y: 0, rotateX: 0, scale: 1 }}
          exit={{ opacity: 0, y: 20, rotateX: 8, scale: 0.96 }}
          transition={{ type: "spring", stiffness: 270, damping: 22 }}
          style={{ transformPerspective: 900 }}
        >
          <motion.div
            className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-warn-100"
            initial={{ scale: 0, rotate: -20 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{
              delay: 0.12,
              type: "spring",
              stiffness: 360,
              damping: 20,
            }}
          >
            <span
              className="text-3xl"
              style={{
                display: "inline-block",
                animation: "float-bob 3s ease-in-out infinite",
              }}
            >
              📧
            </span>
          </motion.div>

          <h2
            id="verify-modal-title"
            className="font-display text-2xl font-semibold text-sand-900 text-center"
          >
            Confirma tu correo
          </h2>
          <p className="mt-3 text-sm text-sand-500 text-center leading-relaxed">
            La cuenta{" "}
            <strong className="font-semibold text-sand-700 break-all">
              {email}
            </strong>{" "}
            aún no ha sido verificada. Revisa tu bandeja de entrada o la carpeta
            de spam para el enlace que te enviamos al registrarte.
          </p>

          <div className="mt-7 flex flex-col gap-3">
            <Button type="button" fullWidth size="lg" onClick={onClose}>
              Entendido
            </Button>
            <Link
              to="/register"
              className="block w-full rounded-xl py-2 text-center text-sm text-sand-400 hover:text-sand-600 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
              onClick={onClose}
            >
              Crear una cuenta nueva en su lugar
            </Link>
          </div>
        </motion.div>
      </div>
    </>,
    document.body,
  );
}

// ── Inline forgot-password form — flip-card back face ────────────────────────

interface InlineForgotFormProps {
  initialEmail: string;
  onBack: () => void;
}

function InlineForgotForm({ initialEmail, onBack }: InlineForgotFormProps) {
  const { mutate: forgotPassword, isPending, isSuccess } = useForgotPassword();
  const [email, setEmail] = useState(initialEmail);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    forgotPassword({ email });
  }

  return (
    <div className="w-full">
      <button
        type="button"
        onClick={onBack}
        aria-label="Volver al formulario de inicio de sesión"
        className="mb-6 inline-flex items-center gap-1.5 rounded-lg text-sm text-sand-500 hover:text-sand-800 transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
      >
        ← Volver a ingresar
      </button>

      <AnimatePresence mode="wait" initial={false}>
        {isSuccess ? (
          <motion.div
            key="success"
            initial={{ opacity: 0, scale: 0.95, y: 12 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            transition={{ type: "spring", stiffness: 290, damping: 26 }}
            className="flex flex-col items-center text-center gap-6"
          >
            <motion.div
              initial={{ scale: 0, rotate: -15 }}
              animate={{ scale: 1, rotate: 0 }}
              transition={{
                delay: 0.1,
                type: "spring",
                stiffness: 380,
                damping: 22,
              }}
              className="flex h-20 w-20 items-center justify-center rounded-full bg-rescue-100 shadow-lg shadow-rescue-200"
            >
              <span
                className="text-4xl"
                style={{
                  display: "inline-block",
                  animation: "float-bob 3s ease-in-out infinite",
                }}
              >
                📬
              </span>
            </motion.div>
            <div>
              <h2 className="font-display text-2xl font-bold text-sand-900">
                ¡Revisa tu correo!
              </h2>
              <p className="mt-2 text-sm text-sand-500 leading-relaxed">
                Si el correo está registrado recibirás un enlace en unos
                minutos. Revisa también tu carpeta de spam.
              </p>
            </div>
            <Button variant="secondary" fullWidth onClick={onBack}>
              ← Volver a iniciar sesión
            </Button>
          </motion.div>
        ) : (
          <motion.div
            key="form"
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ type: "spring", stiffness: 290, damping: 26 }}
          >
            <div className="mb-8">
              <h2 className="font-display text-3xl font-semibold text-sand-900">
                Recuperar contraseña
              </h2>
              <p className="mt-2 text-sm text-sand-500">
                Te enviaremos un enlace seguro a tu correo registrado.
              </p>
            </div>
            <form onSubmit={handleSubmit} noValidate className="space-y-5">
              <Input
                label="Correo electrónico"
                type="email"
                id="forgot-email"
                autoComplete="email"
                inputMode="email"
                autoCapitalize="none"
                autoCorrect="off"
                spellCheck={false}
                required
                placeholder="tu@correo.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoFocus
              />
              <Button type="submit" loading={isPending} fullWidth size="lg">
                Enviar enlace de recuperación
              </Button>
            </form>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

// ── Login page ────────────────────────────────────────────────────────────────

export default function LoginPage() {
  const [searchParams] = useSearchParams();
  const returnTo = searchParams.get("return") ?? undefined;
  const justRegistered = searchParams.get("registered") === "true";

  const { mutate: login, isPending, error } = useLogin(returnTo);
  const [showForgot, setShowForgot] = useState(false);
  const [showVerifyModal, setShowVerifyModal] = useState(false);

  const [form, setForm] = useState(() => ({
    email: localStorage.getItem("pawtrack:lastEmail") ?? "",
    password: "",
  }));
  const [showPassword, setShowPassword] = useState(false);
  const [emailTouched, setEmailTouched] = useState(
    () => !!localStorage.getItem("pawtrack:lastEmail"),
  );
  const emailDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [emailValidNow, setEmailValidNow] = useState(false);

  const {
    tilt: formTilt,
    onMove: onFormMove,
    onLeave: onFormLeave,
  } = useTilt(4);

  const emailInvalid =
    emailValidNow &&
    emailTouched &&
    form.email.length > 0 &&
    !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email);

  const errorMsg = extractLoginError(error);
  const verifyError = isVerifiedError(error);

  useEffect(() => {
    if (verifyError) setShowVerifyModal(true);
  }, [verifyError]);

  const handleEmailBlur = () => {
    setEmailTouched(true);
    if (emailDebounceRef.current) clearTimeout(emailDebounceRef.current);
    emailDebounceRef.current = setTimeout(() => setEmailValidNow(true), 400);
  };

  const handleEmailChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, email: e.target.value });
    setEmailValidNow(false);
  };

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
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

        {/* Outer tilt — flat perspective ±4°; no preserve-3d so the inner
            flip scene creates its own independent stacking context */}
        <div
          className="w-full max-w-sm"
          onMouseMove={onFormMove}
          onMouseLeave={onFormLeave}
          style={{
            transform: `perspective(900px) rotateX(${formTilt.x}deg) rotateY(${formTilt.y}deg)`,
            transition: "transform 0.12s linear",
            willChange: "transform",
          }}
        >
          {/* Flip scene — own perspective, independent from outer tilt */}
          <div style={{ perspective: "1400px" }}>
            <AnimatePresence mode="wait" initial={false}>
              {!showForgot ? (
                <motion.div
                  key="login"
                  initial={{ opacity: 0, rotateY: -9 }}
                  animate={{ opacity: 1, rotateY: 0 }}
                  exit={{ opacity: 0, rotateY: 9 }}
                  transition={{ duration: 0.42, ease: [0.34, 1.1, 0.64, 1] }}
                >
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
                      Cuenta creada exitosamente. Revisa tu correo para
                      verificarla.
                    </Alert>
                  )}

                  {error && !verifyError && (
                    <Alert
                      variant="error"
                      className="mb-6"
                      id="login-error"
                      role="alert"
                    >
                      {errorMsg}
                    </Alert>
                  )}

                  <form
                    onSubmit={handleSubmit}
                    noValidate
                    className="space-y-5"
                  >
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
                            : error && !verifyError
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
                          aria-describedby={
                            error && !verifyError ? "login-error" : undefined
                          }
                          className="block w-full rounded-xl border border-sand-300 bg-surface py-2.5 pl-3.5 pr-10 text-sm text-sand-900 shadow-sm outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-200 placeholder:text-sand-400"
                        />
                        <button
                          type="button"
                          onClick={() => setShowPassword((v) => !v)}
                          tabIndex={-1}
                          aria-label={
                            showPassword
                              ? "Ocultar contraseña"
                              : "Mostrar contraseña"
                          }
                          aria-pressed={showPassword}
                          className="absolute inset-y-0 right-0 flex items-center px-3 text-sand-400 hover:text-sand-700 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 focus-visible:ring-inset rounded-r-xl"
                        >
                          <EyeIcon open={showPassword} />
                        </button>
                      </div>
                    </div>

                    <div className="flex items-center justify-end">
                      {/* Triggers rotateY flip — no navigation */}
                      <button
                        type="button"
                        onClick={() => setShowForgot(true)}
                        className="rounded text-xs text-brand-600 hover:text-brand-700 hover:underline transition-base focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
                      >
                        ¿Olvidaste tu contraseña?
                      </button>
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

                  <p className="mt-2 text-center">
                    <a
                      href="/precios.html"
                      className="text-xs text-sand-400 hover:text-brand-600 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 rounded"
                    >
                      Ver planes y precios →
                    </a>
                  </p>

                  <p className="mt-2 text-center">
                    <Link
                      to="/map"
                      className="text-xs text-sand-400 hover:text-sand-600 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400 rounded"
                    >
                      Explorar el mapa público sin cuenta
                    </Link>
                  </p>
                </motion.div>
              ) : (
                <motion.div
                  key="forgot"
                  initial={{ opacity: 0, rotateY: 9 }}
                  animate={{ opacity: 1, rotateY: 0 }}
                  exit={{ opacity: 0, rotateY: -9 }}
                  transition={{ duration: 0.42, ease: [0.34, 1.1, 0.64, 1] }}
                >
                  <InlineForgotForm
                    initialEmail={form.email}
                    onBack={() => setShowForgot(false)}
                  />
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>

      {/* AnimatePresence in parent so portalled modal gets exit animations */}
      <AnimatePresence>
        {showVerifyModal && (
          <VerifyEmailModal
            email={form.email}
            onClose={() => setShowVerifyModal(false)}
          />
        )}
      </AnimatePresence>
    </div>
  );
}

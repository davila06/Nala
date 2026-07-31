import { Link } from "react-router-dom";
import { motion } from "framer-motion";

const PACKAGES = [
  {
    name: "Básica",
    price: "₡150,000",
    period: "/año",
    color: "border-sand-200",
    badge: "bg-sand-100 text-sand-600",
    features: [
      "Portal control animal municipal",
      "Mapa de mascotas perdidas por cantón",
      "Registro de animales capturados",
      "Reportes mensuales en PDF",
      "Soporte por email",
    ],
    cta: "Solicitar Básica",
  },
  {
    name: "Full",
    price: "₡300,000",
    period: "/año",
    color: "border-trust-400",
    badge: "bg-trust-100 text-trust-700",
    popular: true,
    features: [
      "Todo lo del plan Básica",
      "API de consulta pública",
      "Estadísticas de recuperación por cantón",
      "Dashboard de control animal en tiempo real",
      "SLA de disponibilidad 99.5%",
      "Soporte prioritario telefónico",
    ],
    cta: "Solicitar Full",
  },
  {
    name: "Red Regional",
    price: "₡500,000",
    period: "/año",
    color: "border-rescue-400",
    badge: "bg-rescue-100 text-rescue-700",
    features: [
      "Todo lo del plan Full",
      "Múltiples cantones bajo un solo contrato",
      "Integración con PANI y SENASA",
      "Capacitación presencial al equipo",
      "Customización de marca",
      "Gerente de cuenta dedicado",
    ],
    cta: "Solicitar Red Regional",
  },
];

const STATS = [
  { icon: "🏛️", value: "82", label: "municipalidades en CR" },
  { icon: "🐾", value: "14,000+", label: "mascotas en el sistema" },
  { icon: "🔍", value: "68%", label: "tasa de recuperación" },
  { icon: "⚡", value: "< 72h", label: "tiempo promedio de reunificación" },
];

export default function MunicipalityPortalPage() {
  return (
    <div className="animate-fade-in-up">
      {/* Hero */}
      <section className="border-b border-sand-200 bg-linear-to-br from-trust-50 via-surface to-rescue-50 px-4 py-16 text-center">
        <p className="text-xs font-semibold uppercase tracking-[0.4em] text-trust-600">
          Sector público
        </p>
        <h1 className="mt-3 font-display text-3xl font-black tracking-tight text-sand-950 sm:text-4xl">
          PawTrack para Municipalidades
        </h1>
        <p className="mx-auto mt-4 max-w-xl text-base text-sand-600">
          Herramienta oficial de control animal para cantones costarricenses.
          Gestión de animales capturados, reportes al SENASA y coordinación con
          dueños de mascotas en tiempo real.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <a
            href="mailto:municipios@pawtrack.cr?subject=Solicitud%20PawTrack%20Municipal"
            className="inline-flex items-center gap-2 rounded-2xl bg-trust-600 px-6 py-3 text-sm font-bold text-white shadow-sm transition hover:bg-trust-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400"
          >
            Solicitar demostración
          </a>
          <Link
            to="/map"
            className="inline-flex items-center gap-2 rounded-2xl border border-sand-300 bg-surface px-6 py-3 text-sm font-semibold text-sand-700 transition hover:border-sand-400 hover:text-sand-900"
          >
            Ver mapa público →
          </Link>
        </div>
      </section>

      {/* Stats */}
      <section className="border-b border-sand-200 bg-surface px-4 py-10">
        <div className="mx-auto grid max-w-4xl grid-cols-2 gap-6 sm:grid-cols-4">
          {STATS.map((s, i) => (
            <motion.div
              key={s.label}
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.07 }}
              className="text-center"
            >
              <p className="text-2xl" aria-hidden="true">{s.icon}</p>
              <p className="mt-1 text-2xl font-black tabular-nums text-sand-900">{s.value}</p>
              <p className="text-xs text-sand-500">{s.label}</p>
            </motion.div>
          ))}
        </div>
      </section>

      {/* Packages */}
      <section className="bg-surface-warm px-4 py-12">
        <div className="mx-auto max-w-4xl">
          <h2 className="mb-2 text-center text-2xl font-black text-sand-900">
            Planes institucionales
          </h2>
          <p className="mb-8 text-center text-sm text-sand-500">
            Facturación anual · Incluye acceso sin límite de usuarios ·
            Contrato adaptado a requerimientos de Hacienda
          </p>
          <div className="grid gap-5 sm:grid-cols-3">
            {PACKAGES.map((pkg, i) => (
              <motion.div
                key={pkg.name}
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.07 }}
                className={[
                  "relative flex flex-col rounded-2xl border-2 bg-surface p-5",
                  pkg.color,
                  pkg.popular ? "shadow-lg" : "",
                ].join(" ")}
              >
                {pkg.popular && (
                  <span className="absolute -top-3 left-1/2 -translate-x-1/2 rounded-full bg-trust-600 px-3 py-0.5 text-[10px] font-bold uppercase tracking-widest text-white">
                    Más contratado
                  </span>
                )}
                <div className="mb-4">
                  <span className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold ${pkg.badge}`}>
                    {pkg.name}
                  </span>
                  <p className="mt-2 text-2xl font-extrabold text-sand-900">{pkg.price}</p>
                  <p className="text-xs text-sand-400">{pkg.period}</p>
                </div>
                <ul className="mb-5 flex-1 space-y-2">
                  {pkg.features.map((f) => (
                    <li key={f} className="flex items-start gap-2 text-xs text-sand-700">
                      <span className="mt-0.5 shrink-0 text-rescue-600" aria-hidden="true">✓</span>
                      {f}
                    </li>
                  ))}
                </ul>
                <a
                  href={`mailto:municipios@pawtrack.cr?subject=${encodeURIComponent(`Solicitar ${pkg.name} PawTrack Municipal`)}`}
                  className={[
                    "block rounded-xl py-2.5 text-center text-xs font-bold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-trust-400",
                    pkg.popular
                      ? "bg-trust-600 text-white hover:bg-trust-700"
                      : "border border-sand-300 text-sand-700 hover:border-sand-400 hover:text-sand-900",
                  ].join(" ")}
                >
                  {pkg.cta}
                </a>
              </motion.div>
            ))}
          </div>
        </div>
      </section>

      {/* Benefits for public admin */}
      <section className="border-t border-sand-200 bg-surface px-4 py-12">
        <div className="mx-auto max-w-3xl">
          <h2 className="mb-6 text-center text-xl font-black text-sand-900">
            ¿Por qué PawTrack Municipal?
          </h2>
          <div className="grid gap-4 sm:grid-cols-2">
            {[
              { icon: "📋", title: "Cumplimiento normativo", body: "Reportes automáticos para SENASA, PANI y contraloras según la Ley 7451 de Bienestar Animal." },
              { icon: "🗺️", title: "Mapa en tiempo real", body: "Visualiza casos activos, capturas, y puntos de concentración por barrio y distrito." },
              { icon: "📱", title: "Sin papel", body: "Digitalizacion completa del registro de animales. Acceso desde cualquier dispositivo municipal." },
              { icon: "🔗", title: "Integración con dueños", body: "Cuando un animal capturado tiene placa QR, el dueño recibe una notificación inmediata." },
            ].map((b) => (
              <div key={b.title} className="rounded-2xl border border-sand-200 p-4">
                <p className="text-2xl" aria-hidden="true">{b.icon}</p>
                <p className="mt-2 text-sm font-bold text-sand-900">{b.title}</p>
                <p className="mt-1 text-xs text-sand-500">{b.body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="bg-trust-900 px-4 py-12 text-center text-white">
        <h2 className="font-display text-2xl font-black">
          ¿Su cantón quiere unirse a la red?
        </h2>
        <p className="mx-auto mt-3 max-w-md text-sm text-trust-100/90">
          Escríbanos a{" "}
          <a href="mailto:municipios@pawtrack.cr" className="font-bold text-white underline hover:no-underline">
            municipios@pawtrack.cr
          </a>{" "}
          y coordinamos una demostración sin costo con su equipo de bienestar animal.
        </p>
      </section>
    </div>
  );
}

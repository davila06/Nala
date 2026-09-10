import { Helmet } from "react-helmet-async";
import { Link } from "react-router-dom";
import { useAuthStore } from "@/features/auth/store/authStore";

interface BusinessOption {
  icon: string;
  title: string;
  description: string;
  bullets: string[];
  cta: string;
  to: string;
  accent: string;
}

const options: BusinessOption[] = [
  {
    icon: "🏥",
    title: "Clínica veterinaria",
    description:
      "Aparece en el directorio y mapa público, escanea QR/microchip y emite certificados verificables.",
    bullets: [
      "Directorio y mapa público",
      "Escaneo QR y RFID de pacientes",
      "Certificados PDF verificables (plan Partner)",
    ],
    cta: "Registrar mi clínica",
    to: "/clinica/registro",
    accent: "border-trust-200 bg-trust-50 hover:border-trust-300",
  },
  {
    icon: "🛍️",
    title: "Tienda de mascotas",
    description:
      "Publica tu catálogo, recibe pedidos con SINPE y aparece en el directorio y mapa de tiendas.",
    bullets: [
      "Directorio y mapa público",
      "Catálogo de productos",
      "Pedidos in-app con SINPE (plan Plus)",
    ],
    cta: "Registrar mi tienda",
    to: "/tienda/registro",
    accent: "border-rescue-200 bg-rescue-50 hover:border-rescue-300",
  },
  {
    icon: "🐾",
    title: "Servicio para mascotas",
    description:
      "Adiestradores, groomers, hoteles, guarderías, paseadores y fotógrafos: directorio, catálogo y reservas.",
    bullets: [
      "Directorio y mapa público",
      "Catálogo, disponibilidad y reservas",
      "30 días de prueba gratis del plan Verificado",
    ],
    cta: "Registrar mi servicio",
    to: "/servicio/registro",
    accent: "border-brand-200 bg-brand-50 hover:border-brand-300",
  },
  {
    icon: "🤝",
    title: "Aliado (refugio, ONG, seguridad)",
    description:
      "Únete a la red de aliados que amplifica alertas de mascotas perdidas en tu zona de cobertura.",
    bullets: [
      "Bandeja de alertas por zona",
      "Dashboard de impacto y respuesta",
      "Requiere primero una cuenta de dueño de mascota",
    ],
    cta: "Aplicar como aliado",
    to: "/allies/panel",
    accent: "border-warn-200 bg-warn-50 hover:border-warn-300",
  },
];

export default function BusinessRegistrationHubPage() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  return (
    <>
      <Helmet>
        <title>Registra tu negocio · PawTrack CR</title>
      </Helmet>
      <main className="mx-auto max-w-5xl px-4 py-12">
        <nav className="mb-8 flex flex-wrap gap-x-5 gap-y-2 text-sm font-semibold">
          <Link
            to="/register"
            className="text-brand-600 transition-colors hover:text-brand-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Volver
          </Link>
          <Link
            to={isAuthenticated ? "/dashboard" : "/login"}
            className="text-sand-600 transition-colors hover:text-sand-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-400"
          >
            Ir al inicio
          </Link>
        </nav>
        <div className="text-center">
          <p className="text-xs font-bold uppercase tracking-wide text-brand-600">
            Para negocios
          </p>
          <h1 className="mt-2 font-display text-3xl font-semibold text-ink-900 sm:text-4xl">
            ¿Tienes un negocio para mascotas?
          </h1>
          <p className="mx-auto mt-3 max-w-2xl text-sm text-sand-600">
            Elige el perfil que mejor describe tu negocio u organización. Todos
            los registros son gratis para empezar; algunas funciones avanzadas
            requieren un plan pago.
          </p>
        </div>

        <div className="mt-10 grid gap-5 sm:grid-cols-2">
          {options.map((option) => {
            const isAllyAndAnon =
              option.to === "/allies/panel" && !isAuthenticated;
            const linkTo = isAllyAndAnon
              ? `/login?return=${encodeURIComponent(option.to)}`
              : option.to;
            return (
              <Link
                key={option.title}
                to={linkTo}
                className={`group flex flex-col rounded-2xl border-2 p-6 shadow-sm transition-base hover:-translate-y-0.5 hover:shadow-md ${option.accent}`}
              >
                <span aria-hidden="true" className="text-4xl">
                  {option.icon}
                </span>
                <h2 className="mt-3 font-display text-lg font-semibold text-ink-900">
                  {option.title}
                </h2>
                <p className="mt-1.5 text-sm text-sand-600">
                  {option.description}
                </p>
                <ul className="mt-3 space-y-1 text-xs text-sand-500">
                  {option.bullets.map((bullet) => (
                    <li key={bullet} className="flex items-start gap-1.5">
                      <span
                        aria-hidden="true"
                        className="mt-0.5 text-rescue-600"
                      >
                        ✓
                      </span>
                      {bullet}
                    </li>
                  ))}
                </ul>
                <span className="mt-4 inline-flex items-center gap-1 text-sm font-semibold text-brand-700 group-hover:text-brand-800">
                  {isAllyAndAnon ? "Inicia sesión para aplicar" : option.cta}
                  <span aria-hidden="true">→</span>
                </span>
              </Link>
            );
          })}
        </div>

        <div className="mt-10 space-y-1 text-center text-sm text-sand-500">
          <p>
            ¿Eres dueño de mascota?{" "}
            <Link
              to="/register"
              className="font-semibold text-brand-600 hover:underline"
            >
              Crea tu cuenta gratis
            </Link>
          </p>
          <p>
            ¿Ya tienes cuenta?{" "}
            <Link
              to="/login"
              className="font-semibold text-brand-600 hover:underline"
            >
              Iniciar sesión
            </Link>
          </p>
        </div>
      </main>
    </>
  );
}

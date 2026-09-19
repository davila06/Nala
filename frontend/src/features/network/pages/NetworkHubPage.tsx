import { Building2, CalendarHeart, HeartHandshake, ShoppingBag, Stethoscope } from "lucide-react";
import { Link } from "react-router-dom";

const destinations = [
  { to: "/clinicas", label: "Clínicas", detail: "Atención y expedientes", icon: Stethoscope },
  { to: "/servicios", label: "Servicios", detail: "Cuidadores y especialistas", icon: HeartHandshake },
  { to: "/adopciones", label: "Adopciones", detail: "Animales que buscan hogar", icon: Building2 },
  { to: "/tiendas", label: "Tiendas", detail: "Comercios verificados", icon: ShoppingBag },
  { to: "/campanas-castracion", label: "Campañas", detail: "Jornadas de castración", icon: CalendarHeart },
];

export default function NetworkHubPage() {
  return (
    <main className="mx-auto max-w-5xl px-4 py-8">
      <header className="mb-6">
        <p className="text-xs font-semibold uppercase text-brand-600">Red</p>
        <h1 className="font-display text-2xl font-semibold text-sand-900">Cuidado cerca de ti</h1>
      </header>
      <div className="grid gap-3 sm:grid-cols-2">
        {destinations.map(({ to, label, detail, icon: Icon }) => (
          <Link
            key={to}
            to={to}
            className="flex items-center gap-4 border-b border-sand-200 bg-white px-4 py-5 transition hover:bg-sand-50 sm:rounded-lg sm:border"
          >
            <Icon className="h-6 w-6 text-brand-600" aria-hidden="true" />
            <div>
              <p className="font-semibold text-sand-900">{label}</p>
              <p className="text-sm text-sand-500">{detail}</p>
            </div>
          </Link>
        ))}
      </div>
    </main>
  );
}

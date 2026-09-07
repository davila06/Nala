import { Helmet } from "react-helmet-async";
import { Link } from "react-router-dom";

export default function ServiceProviderPendingPage() {
  return (
    <main className="flex min-h-dvh items-center justify-center bg-sand-50 px-4">
      <Helmet>
        <title>Solicitud enviada · PawTrack CR</title>
      </Helmet>
      <section className="max-w-md space-y-5 text-center">
        <p aria-hidden="true" className="text-5xl">
          🐾
        </p>
        <div>
          <h1 className="font-display text-3xl font-semibold text-ink-900">
            Solicitud enviada
          </h1>
          <p className="mt-2 text-sm leading-relaxed text-sand-600">
            Verificaremos tu perfil antes de publicarlo. Revisa tu correo para
            confirmar la cuenta.
          </p>
        </div>
        <Link
          to="/login"
          className="inline-block rounded-lg bg-brand-600 px-5 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
        >
          Ir al inicio de sesion
        </Link>
      </section>
    </main>
  );
}

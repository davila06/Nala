import { Helmet } from "react-helmet-async";
import { Link } from "react-router-dom";

export default function StorePendingPage() {
  return (
    <div className="flex min-h-dvh items-center justify-center bg-sand-50 px-4">
      <Helmet><title>Solicitud enviada — PawTrack CR</title></Helmet>
      <div className="max-w-md text-center space-y-6">
        <div className="text-6xl" aria-hidden="true">🛒</div>
        <div>
          <h1 className="font-display text-2xl font-bold text-sand-900">¡Solicitud enviada!</h1>
          <p className="mt-2 text-sand-500 leading-relaxed">
            Verificaremos tu tienda en menos de 48 horas. Recibirás un correo de confirmación cuando sea aprobada.
          </p>
        </div>
        <div className="rounded-2xl border border-sand-200 bg-surface p-5 space-y-2 text-sm text-sand-700">
          <p>📧 Revisa tu correo para verificar tu cuenta.</p>
          <p>🗓️ Revisión: menos de 48 horas.</p>
          <p>🛒 Tras la aprobación podrás agregar productos y recibir pedidos.</p>
        </div>
        <Link
          to="/login"
          className="inline-block rounded-xl bg-rescue-600 px-6 py-3 text-sm font-semibold text-white hover:bg-rescue-700 transition-colors"
        >
          Ir al inicio de sesión
        </Link>
      </div>
    </div>
  );
}

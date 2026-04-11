import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-6 bg-sand-50 px-6 text-center animate-fade-in-up">
      <div className="select-none text-7xl" aria-hidden="true">🐾</div>
      <div>
        <p className="text-xs font-bold uppercase tracking-[0.3em] text-sand-400">Error 404</p>
        <h1 className="mt-2 font-display text-3xl font-bold text-sand-900">
          ¡Esta página se escapó!
        </h1>
        <p className="mt-3 max-w-xs text-sm text-sand-500">
          No encontramos la página que buscas. Puede que la URL sea incorrecta o la sección ya no
          exista.
        </p>
      </div>
      <Link
        to="/dashboard"
        className="rounded-xl bg-brand-500 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-brand-600 transition-base"
      >
        ← Volver al inicio
      </Link>
    </div>
  )
}

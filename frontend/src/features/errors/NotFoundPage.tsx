import { Link } from 'react-router-dom'

// Floating paw prints scattered around
const PAW_POSITIONS = [
  { top: '12%', left: '8%',  size: '1.6rem', delay: '0s',   dur: '6s',  rot: '-15deg' },
  { top: '18%', left: '82%', size: '1.2rem', delay: '1.1s', dur: '8s',  rot: '20deg' },
  { top: '72%', left: '5%',  size: '1rem',   delay: '0.5s', dur: '7s',  rot: '-8deg' },
  { top: '80%', left: '88%', size: '1.4rem', delay: '2.2s', dur: '9s',  rot: '12deg' },
  { top: '45%', left: '92%', size: '0.9rem', delay: '1.8s', dur: '6.5s',rot: '-25deg' },
  { top: '60%', left: '3%',  size: '1.1rem', delay: '3s',   dur: '7.5s',rot: '18deg' },
]

export default function NotFoundPage() {
  return (
    <div className="relative flex min-h-screen flex-col items-center justify-center gap-8 overflow-hidden bg-sand-50 px-6 text-center">
      {/* Ambient scattered paw prints */}
      {PAW_POSITIONS.map((p, i) => (
        <span
          key={i}
          aria-hidden="true"
          style={{
            position: 'absolute',
            top: p.top,
            left: p.left,
            fontSize: p.size,
            transform: `rotate(${p.rot})`,
            opacity: 0.12,
            animation: `float-bob ${p.dur} ease-in-out ${p.delay} infinite`,
            userSelect: 'none',
            pointerEvents: 'none',
          }}
        >
          🐾
        </span>
      ))}

      {/* Main illustration — animated lost pet */}
      <div className="relative" aria-hidden="true">
        {/* Question mark orbiting the pet */}
        <span
          style={{
            position: 'absolute',
            top: '-0.5rem',
            right: '-1rem',
            fontSize: '1.8rem',
            animation: 'float-bob 3s ease-in-out 0.3s infinite',
          }}
        >
          ❓
        </span>

        {/* Main pet emoji with bounce */}
        <div
          style={{ animation: 'float-bob 4s ease-in-out infinite' }}
          className="text-8xl select-none"
        >
          🐕
        </div>

        {/* Dashed trail dots */}
        <div className="mt-2 flex justify-center gap-2" aria-hidden="true">
          {[0, 1, 2].map((i) => (
            <span
              key={i}
              className="inline-block h-2 w-2 rounded-full bg-sand-300"
              style={{ animation: `pulse-soft 1.4s ease-in-out ${i * 0.3}s infinite` }}
            />
          ))}
        </div>
      </div>

      {/* Copy */}
      <div className="relative z-10">
        <p className="text-xs font-bold uppercase tracking-[0.3em] text-sand-400">Error 404</p>
        <h1 className="mt-2 font-display text-3xl font-bold text-sand-900">
          Esta página también se perdió
        </h1>
        <p className="mt-3 max-w-xs text-sm text-sand-500 leading-relaxed">
          No encontramos lo que buscas. Tal vez la URL cambió,{' '}
          <br className="hidden sm:block" />
          o nunca existió.
        </p>
      </div>

      {/* Actions */}
      <div className="relative z-10 flex flex-col items-center gap-3">
        <Link
          to="/dashboard"
          className="rounded-xl bg-brand-500 px-6 py-3 text-sm font-semibold text-white shadow-md shadow-brand-200 hover:bg-brand-600 transition-all hover:-translate-y-0.5"
        >
          ← Volver al inicio
        </Link>
        <Link
          to="/map"
          className="rounded-xl border border-sand-200 field-input px-5 py-2.5 text-sm font-semibold text-sand-700 hover:bg-sand-50 transition-colors"
        >
          Ver mapa público
        </Link>
      </div>
    </div>
  )
}


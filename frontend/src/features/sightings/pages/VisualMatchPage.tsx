import { Link } from 'react-router-dom'
import { VisualMatchPanel } from '../components/VisualMatchPanel'

export default function VisualMatchPage() {
  return (
    <div className="min-h-screen bg-sand-50 animate-fade-in-up">
      {/* Back link */}
      <div className="border-b border-sand-200 bg-white px-4 py-3">
        <Link
          to="/map"
          className="inline-flex items-center gap-1 text-sm text-sand-500 hover:text-sand-900"
        >
          ← Volver al mapa
        </Link>
      </div>

      <VisualMatchPanel />
    </div>
  )
}


import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { VisualMatchPanel } from '../components/VisualMatchPanel'

export default function VisualMatchPage() {
  return (
    <div className="min-h-screen bg-sand-50 animate-fade-in-up">
      {/* Header */}
      <div className="border-b border-sand-200 bg-white/95 backdrop-blur-sm sticky top-0 z-10">
        <div className="flex items-center justify-between px-4 py-3 max-w-2xl mx-auto">
          <Link
            to="/map"
            className="inline-flex items-center gap-1 text-sm text-sand-500 hover:text-sand-900 transition-colors"
          >
            ← Volver al mapa
          </Link>
          <span className="text-xs font-semibold text-trust-600 bg-trust-50 rounded-full px-2.5 py-0.5">
            IA de reconocimiento visual
          </span>
        </div>
      </div>

      {/* Hero banner */}
      <motion.div
        initial={{ opacity: 0, y: -8 }}
        animate={{ opacity: 1, y: 0 }}
        className="border-b border-sand-200 bg-gradient-to-r from-trust-900 to-trust-800 px-4 py-6 text-center text-white"
      >
        <p className="text-3xl mb-2" aria-hidden="true">🔍</p>
        <h1 className="font-display text-xl font-bold">Buscar por foto</h1>
        <p className="mt-1 text-sm text-trust-200 max-w-xs mx-auto leading-snug">
          Sube una foto de la mascota que encontraste y la IA buscará coincidencias entre los reportes de mascotas perdidas.
        </p>
      </motion.div>

      <VisualMatchPanel />
    </div>
  )
}


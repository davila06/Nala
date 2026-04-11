import { Component, type ErrorInfo, type ReactNode } from 'react'
import { Link } from 'react-router-dom'

interface Props {
  children?: ReactNode
}

interface State {
  hasError: boolean
}

/**
 * React class-based error boundary that catches unhandled render errors and
 * shows a friendly fallback UI instead of a blank screen.
 */
export default class AppErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  override componentDidCatch(error: Error, info: ErrorInfo) {
    // In production this would pipe to Application Insights / Sentry.
    console.error('[AppErrorBoundary]', error, info.componentStack)
  }

  override render() {
    if (this.state.hasError) {
      return (
        <div className="flex min-h-screen flex-col items-center justify-center gap-6 bg-sand-50 px-6 text-center animate-fade-in-up">
          <div className="select-none text-7xl" aria-hidden="true">😿</div>
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.3em] text-danger-500">
              Error inesperado
            </p>
            <h1 className="mt-2 font-display text-3xl font-bold text-sand-900">
              Algo salió mal
            </h1>
            <p className="mt-3 max-w-xs text-sm text-sand-500">
              Ocurrió un error al cargar esta pantalla. Por favor vuelve al inicio o recarga la
              aplicación.
            </p>
          </div>
          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => this.setState({ hasError: false })}
              className="rounded-xl border border-sand-300 bg-white px-5 py-2.5 text-sm font-semibold text-sand-700 hover:bg-sand-50 transition-base"
            >
              Reintentar
            </button>
            <Link
              to="/dashboard"
              onClick={() => this.setState({ hasError: false })}
              className="rounded-xl bg-brand-500 px-5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-brand-600 transition-base"
            >
              ← Inicio
            </Link>
          </div>
        </div>
      )
    }

    return this.props.children
  }
}

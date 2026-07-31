import { Component, type ErrorInfo, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { trackException } from "@/shared/lib/telemetry";

interface Props {
  children?: ReactNode;
}

interface State {
  hasError: boolean;
}

/**
 * React class-based error boundary that catches unhandled render errors and
 * shows a friendly fallback UI instead of a blank screen.
 */
export default class AppErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  override componentDidCatch(error: Error, info: ErrorInfo) {
    trackException(error, {
      componentStack: info.componentStack ?? undefined,
      source: "AppErrorBoundary",
    });
  }

  override render() {
    if (this.state.hasError) {
      return (
        <div className="relative flex min-h-screen flex-col items-center justify-center gap-8 overflow-hidden bg-surface-warm px-6 text-center">
          {/* Ambient scattered paw prints */}
          {[
            { top: "10%", left: "6%", size: "1.4rem", delay: "0s" },
            { top: "20%", left: "85%", size: "1rem", delay: "1s" },
            { top: "75%", left: "4%", size: "1.2rem", delay: "2s" },
            { top: "80%", left: "88%", size: "0.9rem", delay: "0.5s" },
          ].map((p, i) => (
            <span
              key={i}
              aria-hidden="true"
              style={{
                position: "absolute",
                top: p.top,
                left: p.left,
                fontSize: p.size,
                opacity: 0.1,
                animation: `float-bob 7s ease-in-out ${p.delay} infinite`,
                userSelect: "none",
                pointerEvents: "none",
              }}
            >
              🐾
            </span>
          ))}

          {/* Illustration */}
          <div className="relative" aria-hidden="true">
            <div
              style={{
                animation: "float-bob 4s ease-in-out infinite",
                fontSize: "5rem",
              }}
            >
              😿
            </div>
            <span
              style={{
                position: "absolute",
                top: "-0.5rem",
                right: "-1rem",
                fontSize: "1.5rem",
                animation: "float-bob 3.5s ease-in-out 0.5s infinite",
              }}
            >
              ⚠️
            </span>
          </div>

          {/* Copy */}
          <div className="relative z-10 max-w-sm">
            <p className="text-xs font-bold uppercase tracking-[0.3em] text-danger-500">
              Error inesperado
            </p>
            <h1 className="mt-2 font-display text-3xl font-bold text-sand-900">
              Algo salió mal
            </h1>
            <p className="mt-3 text-sm text-sand-500 leading-relaxed">
              Ocurrió un error al cargar esta pantalla. No te preocupes — tus
              mascotas están seguras. Puedes reintentar o volver al inicio.
            </p>
          </div>

          {/* Actions */}
          <div className="relative z-10 flex flex-col items-center gap-3 w-full max-w-xs">
            <button
              type="button"
              onClick={() => this.setState({ hasError: false })}
              className="w-full rounded-xl border-2 border-brand-200 field-input px-5 py-3 text-sm font-semibold text-brand-700 hover:bg-brand-50 transition-all hover:-translate-y-0.5"
            >
              🔄 Reintentar
            </button>
            <Link
              to="/dashboard"
              onClick={() => this.setState({ hasError: false })}
              className="w-full rounded-xl bg-brand-500 px-5 py-3 text-center text-sm font-semibold text-white shadow-md shadow-brand-200 hover:bg-brand-600 transition-all hover:-translate-y-0.5"
            >
              ← Volver al inicio
            </Link>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

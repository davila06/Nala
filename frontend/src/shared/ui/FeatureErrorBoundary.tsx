import { Component, type ErrorInfo, type ReactNode } from "react";
import { trackException } from "@/shared/lib/telemetry";

interface Props {
  children: ReactNode;
  featureName?: string;
}

interface State {
  hasError: boolean;
  errorMessage?: string;
}

/**
 * Lightweight error boundary scoped to a single feature/page.
 * Falls back to an inline error card instead of crashing the whole app.
 */
export class FeatureErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, errorMessage: error.message };
  }

  override componentDidCatch(error: Error, info: ErrorInfo) {
    trackException(error, {
      componentStack: info.componentStack ?? undefined,
      source: `FeatureErrorBoundary:${this.props.featureName ?? "unknown"}`,
    });
  }

  override render() {
    if (!this.state.hasError) return this.props.children;

    return (
      <div className="mx-auto max-w-lg px-4 py-12 text-center space-y-4">
        <p className="text-4xl">⚠️</p>
        <p className="text-base font-semibold text-ink-800">
          Algo salió mal
          {this.props.featureName ? ` en ${this.props.featureName}` : ""}
        </p>
        <p className="text-sm text-sand-500">
          Intenta recargar la página. Si el problema persiste, contacta soporte.
        </p>
        <button
          onClick={() => {
            this.setState({ hasError: false, errorMessage: undefined });
            window.location.reload();
          }}
          className="inline-block rounded-xl bg-brand-500 px-5 py-2 text-sm font-semibold text-white hover:bg-brand-600 transition-colors"
        >
          Recargar
        </button>
      </div>
    );
  }
}

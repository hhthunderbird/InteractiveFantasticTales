import { Component, type ReactNode } from 'react';

interface Props { children: ReactNode; }
interface State { hasError: boolean; error: Error | null; }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="h-screen w-screen bg-[#1a1a2e] flex items-center justify-center">
          <div className="text-center max-w-lg px-8">
            <div className="text-6xl mb-6">⚠️</div>
            <h1 className="text-2xl font-bold text-white mb-2">Erro Inesperado</h1>
            <p className="text-[#a0a0b0] mb-4 text-sm">
              Ocorreu um erro ao renderizar o editor.
            </p>
            <pre className="text-left text-xs text-[#ef4444] bg-[#0f3460] rounded p-3 mb-4 overflow-auto max-h-40">
              {this.state.error?.message}
            </pre>
            <button
              onClick={() => this.setState({ hasError: false, error: null })}
              className="px-6 py-2 bg-[#e94560] text-white rounded-lg font-semibold hover:bg-[#d63850] transition-colors"
            >
              Tentar Novamente
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}

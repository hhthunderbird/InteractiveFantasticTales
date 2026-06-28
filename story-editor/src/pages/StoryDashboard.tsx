import { useEffect, useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import { useStories } from '../hooks/useStories';
import { useEditorStore } from '../stores/editor-store';
import { createEmptyStory, importStoryFromJson } from '../lib/json-handler';
import { isLocalMode } from '../lib/local-mode';

export function StoryDashboard() {
  const { user, loading: authLoading, error: authError, login, logout, clearError: clearAuthError } = useAuth();
  const { stories, loading: storiesLoading, error: storiesError, clearError: clearStoriesError, listStories, loadStory, saveStory, deleteStory } = useStories(user?.uid ?? null);
  const setStory = useEditorStore((s) => s.setStory);
  const [saveMsg, setSaveMsg] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    if (user) listStories();
  }, [user, listStories]);

  const handleCreateNew = async () => {
    setActionLoading(true);
    try {
      const story = createEmptyStory();
      story.metadata.id = `story-${Date.now()}`;
      story.metadata.author = { name: user?.displayName ?? '', email: user?.email ?? '' };
      if (user) {
        const ok = await saveStory(story.metadata.id, story);
        if (ok) {
          setSaveMsg('História criada e salva na nuvem.');
          setStory(story);
        } else {
          setSaveMsg('História criada localmente (erro ao salvar na nuvem).');
          setStory(story);
        }
      } else {
        setStory(story);
      }
    } finally {
      setActionLoading(false);
    }
  };

  const handleOpenStory = async (storyId: string) => {
    setActionLoading(true);
    const result = await loadStory(storyId);
    setActionLoading(false);
    if (result.data) {
      setStory(result.data);
    } else {
      setSaveMsg(result.error ?? 'Erro ao carregar história.');
    }
  };

  const handleImport = () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = async (e) => {
      setActionLoading(true);
      const file = (e.target as HTMLInputElement).files?.[0];
      if (!file) { setActionLoading(false); return; }
      try {
        const text = await file.text();
        const story = importStoryFromJson(text);
        if (!story.metadata.id) story.metadata.id = `story-${Date.now()}`;
        if (user) {
          const ok = await saveStory(story.metadata.id, story);
          setSaveMsg(ok
            ? 'História importada e salva na nuvem.'
            : 'História importada localmente (erro ao salvar na nuvem).');
        }
        setStory(story);
      } catch (err: any) {
        setSaveMsg(`Erro ao importar: ${err.message}`);
      } finally {
        setActionLoading(false);
      }
    };
    input.click();
  };

  const handleDelete = async (storyId: string) => {
    if (!confirm('Tem certeza que deseja excluir esta história?')) return;
    const ok = await deleteStory(storyId);
    if (ok) listStories();
    else setSaveMsg('Erro ao excluir história.');
  };

  if (authLoading) {
    return (
      <div className="h-screen w-screen bg-[#1a1a2e] flex items-center justify-center">
        <div className="text-[#a0a0b0] text-lg">Carregando...</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="h-screen w-screen bg-[#1a1a2e] flex items-center justify-center">
        <div className="text-center max-w-lg px-8">
          <div className="text-6xl mb-6">📖</div>
          <h1 className="text-3xl font-bold text-white mb-2">Interactive Fantastic Tales</h1>
          <p className="text-[#a0a0b0] mb-8 text-sm leading-relaxed">
            Ferramenta de edição de histórias interativas. Crie aventuras ramificadas com
            editor visual de grafo, auditoria automática de fluxo e preview inline.
          </p>

          {authError && (
            <div className="mb-4 p-3 bg-[#ef444420] border border-[#ef4444] rounded text-sm text-[#ef4444]">
              {authError}
              <button onClick={clearAuthError} className="ml-2 hover:underline">✕</button>
            </div>
          )}

          <button
            onClick={login}
            disabled={actionLoading}
            className="w-full py-3 bg-white text-gray-900 rounded-lg font-semibold hover:bg-gray-200 transition-colors flex items-center justify-center gap-2 disabled:opacity-50"
          >
            {actionLoading ? (
              <span className="inline-block w-4 h-4 border-2 border-gray-900 border-t-transparent rounded-full animate-spin" />
            ) : (
              <span className="text-xl">G</span>
            )}
            {actionLoading ? 'Entrando...' : 'Entrar com Google'}
          </button>

          <div className="mt-6 text-xs text-[#6b7280]">
            Suas histórias são salvas automaticamente na nuvem.
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="h-screen w-screen bg-[#1a1a2e] flex flex-col overflow-hidden">
      <div className="flex items-center justify-between p-4 bg-[#16213e] border-b border-[#2a2a4a] shrink-0">
        <div className="flex items-center gap-3">
          <span className="text-2xl">📖</span>
          <h1 className="text-lg font-bold text-white">Interactive Fantastic Tales</h1>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-xs text-[#a0a0b0]">{user.displayName ?? user.email}</span>
          <button
            onClick={logout}
            className="text-xs text-[#a0a0b0] hover:text-white transition-colors"
          >
            Sair
          </button>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto p-6">
        {isLocalMode() && (
          <div className="mb-4 p-3 bg-[#f59e0b20] border border-[#f59e0b] rounded text-sm text-[#f59e0b]">
            ⚠️ <strong>Modo Local</strong> — Histórias salvas no navegador (localStorage).
            Dados NÃO são enviados para a nuvem.
          </div>
        )}

        {(saveMsg || storiesError || authError) && (
          <div className={`mb-4 p-3 border rounded text-sm ${
            (storiesError || authError)
              ? 'bg-[#ef444420] border-[#ef4444] text-[#ef4444]'
              : saveMsg?.includes('erro')
                ? 'bg-[#ef444420] border-[#ef4444] text-[#ef4444]'
                : 'bg-[#10b98120] border-[#10b981] text-[#10b981]'
          }`}>
            {storiesError ?? authError ?? saveMsg}
            <button
              onClick={() => { setSaveMsg(null); clearStoriesError(); clearAuthError(); }}
              className="ml-2 hover:underline"
            >✕</button>
          </div>
        )}

        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-white">Minhas Histórias</h2>
          <div className="flex gap-2">
            <button
              onClick={handleCreateNew}
              disabled={actionLoading}
              className="px-4 py-2 bg-[#e94560] text-white rounded-lg text-sm font-semibold hover:bg-[#d63850] transition-colors disabled:opacity-50"
            >
              {actionLoading ? 'Criando...' : '+ Nova Aventura'}
            </button>
            <button
              onClick={handleImport}
              disabled={actionLoading}
              className="px-4 py-2 bg-[#0f3460] text-[#e0e0e0] rounded-lg text-sm font-semibold hover:bg-[#1a4a7a] transition-colors disabled:opacity-50"
            >
              Importar JSON
            </button>
          </div>
        </div>

        {storiesLoading ? (
          <div className="text-center text-[#6b7280] py-12">
            <span className="inline-block w-5 h-5 border-2 border-[#6b7280] border-t-transparent rounded-full animate-spin mr-2 align-middle" />
            Carregando histórias...
          </div>
        ) : stories.length === 0 ? (
          <div className="text-center py-12">
            <div className="text-4xl mb-4">📚</div>
            <p className="text-[#a0a0b0] text-sm mb-2">Nenhuma história ainda.</p>
            <p className="text-[#6b7280] text-xs">Crie uma nova aventura ou importe um arquivo JSON.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {stories.map((story) => (
              <div
                key={story.id}
                className="bg-[#16213e] border border-[#2a2a4a] rounded-lg p-4 hover:border-[#e94560] transition-colors group"
              >
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <h3 className="text-white font-semibold text-sm truncate">{story.title}</h3>
                    <p className="text-[10px] text-[#6b7280] mt-0.5">v{story.version}</p>
                  </div>
                  <button
                    onClick={() => handleDelete(story.id)}
                    className="text-[#6b7280] hover:text-[#ef4444] text-xs opacity-0 group-hover:opacity-100 transition-opacity"
                  >
                    ✕
                  </button>
                </div>
                <p className="text-[10px] text-[#6b7280] mb-3">
                  Atualizado {story.updatedAt.toLocaleDateString('pt-BR')}
                </p>
                <button
                  onClick={() => handleOpenStory(story.id)}
                  disabled={actionLoading}
                  className="w-full py-2 bg-[#0f3460] text-[#e0e0e0] rounded text-sm hover:bg-[#1a4a7a] transition-colors disabled:opacity-50"
                >
                  Abrir
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

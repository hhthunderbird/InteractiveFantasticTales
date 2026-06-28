import { useState, useEffect, useCallback, useRef } from 'react';
import { useEditorStore } from '../../stores/editor-store';
import { SECTION_TYPE_LABELS, type SectionType } from '../../types/story';
import type { SectionData } from '../../types/story';
import { downloadJson } from '../../lib/json-handler';
import { useAutoSave } from '../../hooks/useAutoSave';
import { useStyleStore } from '../../stores/style-store';

export function Toolbar() {
  const story = useEditorStore((s) => s.story);
  const clearStory = useEditorStore((s) => s.clearStory);
  const updateMetadata = useEditorStore((s) => s.updateMetadata);
  const isDirty = useEditorStore((s) => s.isDirty);
  const viewMode = useEditorStore((s) => s.viewMode);
  const setViewMode = useEditorStore((s) => s.setViewMode);
  const toggleAudit = useEditorStore((s) => s.toggleAudit);
  const triggerLayout = useEditorStore((s) => s.triggerLayout);
  const { manualSave } = useAutoSave();
  const toggleStylePanel = useStyleStore((s) => s.toggleStylePanel);

  const [title, setTitle] = useState('');
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    if (story) setTitle(story.metadata.title);
  }, [story?.metadata.title]);

  const debouncedUpdate = useCallback((val: string) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      const s = useEditorStore.getState().story;
      if (s && val !== s.metadata.title) {
        updateMetadata({ title: val });
      }
    }, 400);
  }, [updateMetadata]);

  if (!story) return null;

  const handleTitleChange = (val: string) => {
    setTitle(val);
    debouncedUpdate(val);
  };

  const getNextSectionId = (): number => {
    const ids = Object.keys(story.sections).map(Number);
    return ids.length > 0 ? Math.max(...ids) + 1 : 1;
  };

  const addSection = (type: SectionType) => {
    const id = getNextSectionId();
    const section: SectionData = {
      id,
      type,
      text: '',
      choices: type === 'narrative' ? [] : undefined,
      combat: type === 'combat' ? {
        enemyName: 'Inimigo',
        enemySkill: 5,
        enemyStamina: 6,
        victoryTarget: 0,
        defeatTarget: 0,
        fleeTarget: 0,
        allowFlee: true,
        lootOnVictory: [],
      } : undefined,
      test: type === 'test' ? {
        attribute: 'skill',
        difficulty: 8,
        successTarget: 0,
        failTarget: 0,
      } : undefined,
      itemGate: type === 'itemGate' ? { item: '', hasItemTarget: 0, noItemTarget: 0 } : undefined,
      random: type === 'random' ? { outcomes: [] } : undefined,
      ending: type === 'ending' ? { type: 'neutral' } : undefined,
    };
    useEditorStore.getState().addSection(section);
    setDropdownOpen(false);
  };

  const handleSave = async () => {
    setSaving(true);
    await manualSave();
    if (story) downloadJson(story);
    setSaving(false);
  };

  return (
    <div className="flex items-center justify-between h-12 px-4 bg-[#16213e] border-b border-[#2a2a4a] shrink-0">
      <div className="flex items-center gap-3">
        <button
          onClick={clearStory}
          className="text-[#a0a0b0] hover:text-white transition-colors"
          title="Voltar ao início"
        >
          ←
        </button>
        <span className="text-lg">📖</span>
        <input
          className="bg-transparent text-white font-semibold text-lg border-b border-transparent hover:border-[#e94560] focus:border-[#e94560] focus:outline-none px-1 w-64"
          value={title}
          onChange={(e) => handleTitleChange(e.target.value)}
          onBlur={() => updateMetadata({ title })}
          placeholder="Título da História"
        />
        {isDirty && <span className="text-[#f59e0b] text-xs">● não salvo</span>}
      </div>

      <div className="flex items-center gap-2">
        <div className="flex bg-[#0f3460] rounded-lg p-0.5">
          {(['graph', 'sheet', 'text', 'preview'] as const).map((mode) => (
            <button
              key={mode}
              onClick={() => setViewMode(mode)}
              className={`px-3 py-1 text-xs rounded-md transition-colors ${
                viewMode === mode ? 'bg-[#e94560] text-white' : 'text-[#a0a0b0] hover:text-white'
              }`}
            >
              {mode === 'graph' ? 'Grafo' : mode === 'sheet' ? 'Planilha' : mode === 'text' ? 'Texto' : 'Preview'}
            </button>
          ))}
        </div>

        <div className="h-6 w-px bg-[#2a2a4a]" />

        <div className="relative">
          <button
            onClick={() => setDropdownOpen(!dropdownOpen)}
            className="px-3 py-1 text-xs bg-[#e94560] text-white rounded-md hover:bg-[#d63850] transition-colors"
          >
            + Nova Seção
          </button>
          {dropdownOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setDropdownOpen(false)} />
              <div className="absolute top-full right-0 mt-1 bg-[#16213e] border border-[#2a2a4a] rounded-lg shadow-xl z-50 min-w-[160px]">
                {(Object.keys(SECTION_TYPE_LABELS) as SectionType[]).map((type) => (
                  <button
                    key={type}
                    onClick={() => addSection(type)}
                    className="block w-full text-left px-4 py-2 text-sm text-[#e0e0e0] hover:bg-[#0f3460] transition-colors"
                  >
                    {SECTION_TYPE_LABELS[type]}
                  </button>
                ))}
              </div>
            </>
          )}
        </div>

        <button
          onClick={toggleAudit}
          className="px-3 py-1 text-xs bg-[#0f3460] text-[#e0e0e0] rounded-md hover:bg-[#1a4a7a] transition-colors"
        >
          Auditoria
        </button>

        <button
          onClick={toggleStylePanel}
          className="px-3 py-1 text-xs bg-[#0f3460] text-[#e0e0e0] rounded-md hover:bg-[#1a4a7a] transition-colors"
          title="Estilos e Grupos"
        >
          🎨 Estilos
        </button>

        <button
          onClick={triggerLayout}
          className="px-3 py-1 text-xs bg-[#0f3460] text-[#e0e0e0] rounded-md hover:bg-[#1a4a7a] transition-colors"
          title="Reorganizar nós do grafo"
        >
          Organizar
        </button>

        <button
          onClick={handleSave}
          disabled={saving}
          className="px-3 py-1 text-xs bg-[#10b981] text-white rounded-md hover:bg-[#059669] transition-colors disabled:opacity-50"
        >
          {saving ? 'Salvando...' : 'Salvar'}
        </button>
      </div>
    </div>
  );
}

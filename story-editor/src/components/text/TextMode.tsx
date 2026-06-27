import { useEditorStore } from '../../stores/editor-store';

export function TextMode() {
  const story = useEditorStore((s) => s.story);
  const selectSection = useEditorStore((s) => s.selectSection);
  const updateSection = useEditorStore((s) => s.updateSection);
  const selectedSectionId = useEditorStore((s) => s.selectedSectionId);

  if (!story) return null;

  const section = selectedSectionId ? story.sections[selectedSectionId] : null;

  const handleTextChange = (text: string) => {
    if (selectedSectionId) updateSection(selectedSectionId, { text });
  };

  const rawText = section
    ? `=== Seção #${section.id} [${section.type}] ===\n\n${section.text}\n\n` +
      (section.choices ? section.choices.map((c) => `→ [${c.targetSection}] ${c.text}`).join('\n') : '')
    : 'Selecione uma seção no painel esquerdo para editar.';

  return (
    <div className="h-full flex bg-[#1a1a2e]">
      <div className="w-48 bg-[#16213e] border-r border-[#2a2a4a] overflow-y-auto shrink-0">
        <div className="p-2 text-[10px] text-[#6b7280] uppercase">Seções</div>
        {Object.values(story.sections).map((s) => (
          <button
            key={s.id}
            onClick={() => selectSection(s.id)}
            className={`w-full text-left px-3 py-1.5 text-xs truncate transition-colors ${
              s.id === selectedSectionId
                ? 'bg-[#0f3460] text-white'
                : 'text-[#a0a0b0] hover:bg-[#0f3460]/50'
            }`}
          >
            <span className="text-[10px] text-[#6b7280] mr-1">#{s.id}</span>
            {s.text ? s.text.substring(0, 30) : '(sem texto)'}
          </button>
        ))}
      </div>

      <div className="flex-1 p-4">
        {section ? (
          <textarea
            className="w-full h-full bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded p-4 font-mono text-sm resize-none focus:outline-none focus:border-[#e94560]"
            value={section.text}
            onChange={(e) => handleTextChange(e.target.value)}
            placeholder={`[Seção #${section.id} - ${section.type}]\n\nDigite o texto narrativo aqui...`}
          />
        ) : (
          <div className="h-full flex items-center justify-center text-[#6b7280] text-sm">
            {rawText}
          </div>
        )}
      </div>

      {section && (
        <div className="w-64 bg-[#16213e] border-l border-[#2a2a4a] p-3 overflow-y-auto shrink-0">
          <h3 className="text-xs font-semibold text-white mb-3">Preview</h3>
          <div className="text-sm text-[#a0a0b0] whitespace-pre-wrap leading-relaxed">
            {section.text || '(sem texto)'}
          </div>
          {section.choices && section.choices.length > 0 && (
            <div className="mt-4 space-y-2">
              {section.choices.map((choice, i) => (
                <div key={i} className="bg-[#0f3460] rounded px-3 py-2 text-xs text-[#e0e0e0]">
                  <span className="text-[#6b7280]">→ [{choice.targetSection}]</span> {choice.text}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

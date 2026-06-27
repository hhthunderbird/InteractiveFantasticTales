import { useEditorStore } from '../../stores/editor-store';

export function StatusBar() {
  const story = useEditorStore((s) => s.story);
  const isDirty = useEditorStore((s) => s.isDirty);
  const selectedId = useEditorStore((s) => s.selectedSectionId);

  const sectionCount = story ? Object.keys(story.sections).length : 0;

  return (
    <div className="h-7 bg-[#16213e] border-t border-[#2a2a4a] flex items-center px-4 text-[10px] text-[#6b7280] shrink-0">
      <span>v{story?.metadata?.version ?? '0.0.0'}</span>
      <span className="mx-2">|</span>
      <span>{sectionCount} seções</span>
      <span className="mx-2">|</span>
      <span>Selecionado: {selectedId ?? '—'}</span>
      <span className="mx-2">|</span>
      <span className={isDirty ? 'text-[#f59e0b]' : 'text-[#10b981]'}>
        {isDirty ? '● Alterações não salvas' : '● Salvo'}
      </span>
    </div>
  );
}

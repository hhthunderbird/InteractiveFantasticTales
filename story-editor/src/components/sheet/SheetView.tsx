import { useState } from 'react';
import { useEditorStore } from '../../stores/editor-store';
import { SECTION_TYPE_LABELS, SECTION_TYPE_COLORS, type SectionType } from '../../types/story';

export function SheetView() {
  const story = useEditorStore((s) => s.story);
  const selectSection = useEditorStore((s) => s.selectSection);
  const selectedSectionId = useEditorStore((s) => s.selectedSectionId);
  const [filter, setFilter] = useState<SectionType | 'all'>('all');
  const [search, setSearch] = useState('');

  if (!story) return null;

  const sections = Object.values(story.sections)
    .filter((s) => filter === 'all' || s.type === filter)
    .filter((s) => !search || s.text.toLowerCase().includes(search.toLowerCase()) || String(s.id).includes(search));

  return (
    <div className="h-full flex flex-col bg-[#1a1a2e]">
      <div className="p-3 bg-[#16213e] border-b border-[#2a2a4a] flex items-center gap-3">
        <select
          className="bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs"
          value={filter}
          onChange={(e) => setFilter(e.target.value as SectionType | 'all')}
        >
          <option value="all">Todos os tipos</option>
          {(Object.keys(SECTION_TYPE_LABELS) as SectionType[]).map((t) => (
            <option key={t} value={t}>{SECTION_TYPE_LABELS[t]}</option>
          ))}
        </select>
        <input
          className="flex-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs focus:outline-none focus:border-[#e94560]"
          placeholder="Buscar por texto ou ID..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <span className="text-[10px] text-[#6b7280]">{sections.length} seções</span>
      </div>

      <div className="flex-1 overflow-y-auto">
        <table className="w-full text-xs">
          <thead className="sticky top-0 bg-[#16213e] text-[#6b7280]">
            <tr>
              <th className="text-left px-3 py-2 w-16">ID</th>
              <th className="text-left px-3 py-2 w-24">Tipo</th>
              <th className="text-left px-3 py-2">Texto</th>
              <th className="text-left px-3 py-2 w-20">Destinos</th>
            </tr>
          </thead>
          <tbody>
            {sections.map((section) => {
              const targets: number[] = [];
              if (section.choices) section.choices.forEach((c) => targets.push(c.targetSection));
              if (section.combat) {
                targets.push(section.combat.victoryTarget, section.combat.defeatTarget);
                if (section.combat.allowFlee) targets.push(section.combat.fleeTarget);
              }
              if (section.test) targets.push(section.test.successTarget, section.test.failTarget);
              if (section.itemGate) targets.push(section.itemGate.hasItemTarget, section.itemGate.noItemTarget);
              if (section.random) section.random.outcomes.forEach((o) => targets.push(o.targetSection));

              const isSelected = section.id === selectedSectionId;
              const color = SECTION_TYPE_COLORS[section.type];

              return (
                <tr
                  key={section.id}
                  onClick={() => selectSection(section.id)}
                  className={`cursor-pointer border-b border-[#1a1a2e] hover:bg-[#0f3460]/50 transition-colors ${
                    isSelected ? 'bg-[#0f3460]' : ''
                  }`}
                >
                  <td className="px-3 py-2 text-[#6b7280] font-mono">#{section.id}</td>
                  <td className="px-3 py-2">
                    <span className="px-1.5 py-0.5 rounded text-[10px]" style={{ background: color + '30', color }}>
                      {SECTION_TYPE_LABELS[section.type]}
                    </span>
                  </td>
                  <td className="px-3 py-2 text-[#a0a0b0] truncate max-w-xs">
                    {section.text ? section.text.substring(0, 80) + (section.text.length > 80 ? '...' : '') : '(sem texto)'}
                  </td>
                  <td className="px-3 py-2 text-[#6b7280]">
                    {targets.filter((t) => t > 0).join(', ') || '—'}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

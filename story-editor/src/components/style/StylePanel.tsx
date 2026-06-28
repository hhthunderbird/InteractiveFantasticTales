import { useState } from 'react';
import { useStyleStore } from '../../stores/style-store';
import { useEditorStore } from '../../stores/editor-store';
import type { NodeStyle, NodeGroup } from '../../types/story';

export function StylePanel() {
  const {
    styles, groups, sectionStyles, highlightedStyleIds, stylePanelOpen,
    addStyle, removeStyle, applyStyle,
    toggleHighlight, clearHighlights,
    addGroup, removeGroup, addNodesToGroup, removeNodesFromGroup,
    toggleStylePanel,
  } = useStyleStore();
  const selectedSectionId = useEditorStore((s) => s.selectedSectionId);
  const story = useEditorStore((s) => s.story);

  const [editingStyle, setEditingStyle] = useState<NodeStyle | null>(null);
  const [editingGroup, setEditingGroup] = useState<NodeGroup | null>(null);
  const [tab, setTab] = useState<'styles' | 'groups'>('styles');

  if (!stylePanelOpen || !story) return null;

  return (
    <div className="w-72 bg-[#16213e] border-l border-[#2a2a4a] flex flex-col shrink-0 overflow-hidden text-xs">
      <div className="flex items-center justify-between p-3 border-b border-[#2a2a4a]">
        <div className="flex gap-1">
          <button onClick={() => setTab('styles')} className={`px-2 py-0.5 rounded text-xs ${tab === 'styles' ? 'bg-[#e94560] text-white' : 'text-[#a0a0b0]'}`}>Estilos</button>
          <button onClick={() => setTab('groups')} className={`px-2 py-0.5 rounded text-xs ${tab === 'groups' ? 'bg-[#e94560] text-white' : 'text-[#a0a0b0]'}`}>Grupos</button>
        </div>
        <button onClick={toggleStylePanel} className="text-[#6b7280] hover:text-white">✕</button>
      </div>

      <div className="flex-1 overflow-y-auto p-2 space-y-2">
        {tab === 'styles' && (
          <>
            <div className="flex items-center justify-between">
              <span className="text-[10px] text-[#6b7280] uppercase">Estilos Salvos</span>
              <button
                onClick={() => setEditingStyle({
                  id: crypto.randomUUID().slice(0, 8), name: 'Novo Estilo',
                  color: '#3b82f6', borderColor: '#3b82f6', borderWidth: 2, borderStyle: 'solid',
                  backgroundColor: '', fontSize: 'medium', locked: false,
                })}
                className="text-[10px] bg-[#0f3460] text-[#e0e0e0] px-1.5 py-0.5 rounded hover:bg-[#1a4a7a]"
              >+ Novo</button>
            </div>

            {styles.filter((s) => s.id !== '__default__').length === 0 && (
              <div className="text-[10px] text-[#6b7280] text-center py-4">Nenhum estilo salvo ainda.</div>
            )}

            {styles.filter((s) => s.id !== '__default__').map((style) => {
              const isHighlighted = highlightedStyleIds.includes(style.id);
              const styledSectionCount = Object.values(sectionStyles).filter((s) => s === style.id).length;

              return (
                <div key={style.id} className="bg-[#0f3460] rounded p-2 space-y-1.5">
                  <div className="flex items-center gap-2">
                    <div className="w-4 h-4 rounded border-2"
                      style={{ background: style.backgroundColor || style.color + '30', borderColor: style.color }} />
                    <span className="text-[#e0e0e0] flex-1 truncate">{style.name}</span>
                    <span className="text-[10px] text-[#6b7280]">{styledSectionCount}</span>
                  </div>

                  <div className="flex gap-1">
                    {selectedSectionId && (
                      <button onClick={() => applyStyle(style.id, [selectedSectionId])}
                        className="flex-1 text-[10px] bg-[#1a1a2e] text-[#a0a0b0] px-1 py-0.5 rounded hover:text-white">
                        Aplicar
                      </button>
                    )}
                    <button onClick={() => toggleHighlight(style.id)}
                      className={`flex-1 text-[10px] px-1 py-0.5 rounded ${isHighlighted ? 'bg-[#f59e0b] text-black' : 'bg-[#1a1a2e] text-[#a0a0b0]'}`}>
                      {isHighlighted ? 'Destaque ON' : 'Destacar'}
                    </button>
                    <button onClick={() => removeStyle(style.id)}
                      className="text-[10px] bg-[#1a1a2e] text-[#ef4444] px-1 py-0.5 rounded hover:bg-[#ef444420]">✕</button>
                  </div>
                </div>
              );
            })}

            {highlightedStyleIds.length > 0 && (
              <button onClick={clearHighlights} className="w-full text-[10px] text-[#f59e0b] hover:underline py-1">
                Limpar todos os destaques
              </button>
            )}

            {editingStyle && (
              <StyleEditor
                style={editingStyle}
                onSave={(s) => { addStyle(s); setEditingStyle(null); }}
                onCancel={() => setEditingStyle(null)}
              />
            )}
          </>
        )}

        {tab === 'groups' && (
          <>
            <div className="flex items-center justify-between">
              <span className="text-[10px] text-[#6b7280] uppercase">Grupos</span>
              <button
                onClick={() => setEditingGroup({
                  id: crypto.randomUUID().slice(0, 8), title: 'Novo Grupo',
                  color: '#3b82f6', borderStyle: 'dashed', borderWidth: 2,
                  visible: true, nodeIds: selectedSectionId ? [selectedSectionId] : [], collapsed: false,
                })}
                className="text-[10px] bg-[#0f3460] text-[#e0e0e0] px-1.5 py-0.5 rounded hover:bg-[#1a4a7a]"
              >+ Novo</button>
            </div>

            {groups.length === 0 && (
              <div className="text-[10px] text-[#6b7280] text-center py-4">Nenhum grupo criado.</div>
            )}

            {groups.map((group) => (
              <div key={group.id} className="bg-[#0f3460] rounded p-2 space-y-1.5">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-sm border-2"
                    style={{ borderColor: group.color, borderStyle: group.borderStyle }} />
                  <span className="text-[#e0e0e0] flex-1 truncate">{group.title}</span>
                  <span className="text-[10px] text-[#6b7280]">{group.nodeIds.length}n</span>
                </div>

                <div className="flex gap-1">
                  {selectedSectionId && (
                    <>
                      {group.nodeIds.includes(selectedSectionId) ? (
                        <button onClick={() => removeNodesFromGroup(group.id, [selectedSectionId])}
                          className="flex-1 text-[10px] bg-[#1a1a2e] text-[#ef4444] px-1 py-0.5 rounded hover:bg-[#ef444420]">
                          Remover
                        </button>
                      ) : (
                        <button onClick={() => addNodesToGroup(group.id, [selectedSectionId])}
                          className="flex-1 text-[10px] bg-[#1a1a2e] text-[#a0a0b0] px-1 py-0.5 rounded hover:text-white">
                          Adicionar
                        </button>
                      )}
                    </>
                  )}
                  <button onClick={() => removeGroup(group.id)}
                    className="text-[10px] bg-[#1a1a2e] text-[#ef4444] px-1 py-0.5 rounded hover:bg-[#ef444420]">✕</button>
                </div>
              </div>
            ))}

            {editingGroup && (
              <GroupEditor
                group={editingGroup}
                onSave={(g) => { addGroup(g); setEditingGroup(null); }}
                onCancel={() => setEditingGroup(null)}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}

function StyleEditor({ style, onSave, onCancel }: { style: NodeStyle; onSave: (s: NodeStyle) => void; onCancel: () => void }) {
  const [s, setS] = useState(style);
  return (
    <div className="absolute inset-0 z-50 bg-black/60 flex items-center justify-center" onClick={onCancel}>
      <div className="bg-[#16213e] border border-[#2a2a4a] rounded-lg p-4 w-64 space-y-2" onClick={(e) => e.stopPropagation()}>
        <h3 className="text-sm font-semibold text-white">Editar Estilo</h3>
        <input className="w-full bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={s.name} onChange={(e) => setS({ ...s, name: e.target.value })} placeholder="Nome" />
        <div className="flex gap-2">
          <label className="text-[10px] text-[#6b7280]">Cor</label>
          <input type="color" className="w-8 h-6 rounded" value={s.color} onChange={(e) => setS({ ...s, color: e.target.value })} />
          <label className="text-[10px] text-[#6b7280]">Fundo</label>
          <input type="color" className="w-8 h-6 rounded" value={s.backgroundColor || '#000000'} onChange={(e) => setS({ ...s, backgroundColor: e.target.value })} />
        </div>
        <div className="flex gap-2">
          <label className="text-[10px] text-[#6b7280]">Borda</label>
          <select className="bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1 py-0.5 text-[10px]" value={s.borderStyle} onChange={(e) => setS({ ...s, borderStyle: e.target.value as any })}>
            <option value="solid">Sólida</option>
            <option value="dashed">Tracejada</option>
            <option value="dotted">Pontilhada</option>
          </select>
          <label className="text-[10px] text-[#6b7280]">Esp.</label>
          <input type="number" min="1" max="6" className="w-8 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1 py-0.5 text-[10px]" value={s.borderWidth} onChange={(e) => setS({ ...s, borderWidth: +e.target.value })} />
        </div>
        <div className="flex gap-2">
          <button onClick={() => onSave(s)} className="flex-1 py-1 bg-[#10b981] text-white rounded text-xs">Salvar</button>
          <button onClick={onCancel} className="flex-1 py-1 bg-[#0f3460] text-[#a0a0b0] rounded text-xs">Cancelar</button>
        </div>
      </div>
    </div>
  );
}

function GroupEditor({ group, onSave, onCancel }: { group: NodeGroup; onSave: (g: NodeGroup) => void; onCancel: () => void }) {
  const [g, setG] = useState(group);
  return (
    <div className="absolute inset-0 z-50 bg-black/60 flex items-center justify-center" onClick={onCancel}>
      <div className="bg-[#16213e] border border-[#2a2a4a] rounded-lg p-4 w-64 space-y-2" onClick={(e) => e.stopPropagation()}>
        <h3 className="text-sm font-semibold text-white">Editar Grupo</h3>
        <input className="w-full bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={g.title} onChange={(e) => setG({ ...g, title: e.target.value })} placeholder="Título" />
        <input type="color" className="w-full h-8 rounded" value={g.color} onChange={(e) => setG({ ...g, color: e.target.value })} />
        <select className="w-full bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={g.borderStyle} onChange={(e) => setG({ ...g, borderStyle: e.target.value as any })}>
          <option value="solid">Sólida</option>
          <option value="dashed">Tracejada</option>
          <option value="dotted">Pontilhada</option>
        </select>
        <div className="flex gap-2">
          <button onClick={() => onSave(g)} className="flex-1 py-1 bg-[#10b981] text-white rounded text-xs">Salvar</button>
          <button onClick={onCancel} className="flex-1 py-1 bg-[#0f3460] text-[#a0a0b0] rounded text-xs">Cancelar</button>
        </div>
      </div>
    </div>
  );
}

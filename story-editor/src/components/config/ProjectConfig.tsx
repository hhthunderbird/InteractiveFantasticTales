import { useState } from 'react';
import { useProjectConfigStore } from '../../stores/project-config-store';
import { useEditorStore } from '../../stores/editor-store';
import { ITEM_TYPE_LABELS, type ItemType, type ItemTemplate, type EnemyTemplate } from '../../types/story';

type Tab = 'items' | 'enemies' | 'rules' | 'external';

export function ProjectConfig() {
  const story = useEditorStore((s) => s.story);
  const {
    items, enemies, rules, externalAssets,
    addItem, updateItem, removeItem,
    addEnemy, updateEnemy, removeEnemy,
    updateRules, addCustomAttribute, updateCustomAttribute, removeCustomAttribute,
    addExternalAsset, removeExternalAsset,
  } = useProjectConfigStore();
  const [tab, setTab] = useState<Tab>('items');

  if (!story) return null;

  const tabs: { key: Tab; label: string; count: number; icon: string }[] = [
    { key: 'items', label: 'Itens', count: items.length, icon: '🎒' },
    { key: 'enemies', label: 'Inimigos', count: enemies.length, icon: '👹' },
    { key: 'rules', label: 'Regras', count: rules.customAttributes.length, icon: '⚙️' },
    { key: 'external', label: 'Arquivos', count: externalAssets.length, icon: '📁' },
  ];

  return (
    <div className="h-full flex flex-col bg-[#1a1a2e]">
      <div className="p-3 bg-[#16213e] border-b border-[#2a2a4a]">
        <h3 className="text-sm font-semibold text-white">⚙️ Configuração do Projeto</h3>
        <p className="text-[10px] text-[#6b7280] mt-1">Gerencie itens, inimigos, regras e assets do projeto.</p>
      </div>

      <div className="flex border-b border-[#2a2a4a] bg-[#16213e]">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`flex items-center gap-1.5 px-4 py-2 text-xs transition-colors border-b-2 ${
              tab === t.key ? 'border-[#e94560] text-white' : 'border-transparent text-[#6b7280] hover:text-[#a0a0b0]'
            }`}
          >
            <span>{t.icon}</span> {t.label}
            {t.count > 0 && <span className="text-[10px] opacity-60">({t.count})</span>}
          </button>
        ))}
      </div>

      <div className="flex-1 overflow-y-auto p-4">
        {tab === 'items' && (
          <ItemsTab items={items} addItem={addItem} updateItem={updateItem} removeItem={removeItem} />
        )}
        {tab === 'enemies' && (
          <EnemiesTab enemies={enemies} addEnemy={addEnemy} updateEnemy={updateEnemy} removeEnemy={removeEnemy} />
        )}
        {tab === 'rules' && (
          <RulesTab rules={rules} updateRules={updateRules}
            addCustomAttribute={addCustomAttribute} updateCustomAttribute={updateCustomAttribute} removeCustomAttribute={removeCustomAttribute} />
        )}
        {tab === 'external' && (
          <ExternalTab assets={externalAssets} addAsset={addExternalAsset} removeAsset={removeExternalAsset}
            storyId={story.metadata.id} />
        )}
      </div>
    </div>
  );
}

function ItemsTab({ items, addItem, updateItem, removeItem }: {
  items: ItemTemplate[]; addItem: (i: ItemTemplate) => void; updateItem: (id: string, d: Partial<ItemTemplate>) => void; removeItem: (id: string) => void;
}) {
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<ItemTemplate>({ id: '', name: '', type: 'misc', description: '', effects: {}, tags: [] });

  const handleAdd = () => {
    if (!form.name.trim()) return;
    addItem({ ...form, id: crypto.randomUUID().slice(0, 8) });
    setForm({ id: '', name: '', type: 'misc', description: '', effects: {}, tags: [] });
    setShowForm(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const text = e.dataTransfer.getData('text/plain');
    if (text) setForm((f) => ({ ...f, name: text, description: text }));
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h4 className="text-xs font-semibold text-white">Itens do Jogo</h4>
        <button onClick={() => setShowForm(!showForm)}
          className="text-xs bg-[#0f3460] text-[#e0e0e0] px-2 py-1 rounded hover:bg-[#1a4a7a]">+ Novo Item</button>
      </div>

      {showForm && (
        <div className="bg-[#0f3460] rounded p-3 space-y-2"
          onDragOver={(e) => e.preventDefault()} onDrop={handleDrop}>
          <input className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Nome do item" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          <div className="flex gap-2">
            <select className="flex-1 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as ItemType })}>
              {(Object.entries(ITEM_TYPE_LABELS) as [ItemType, string][]).map(([k, v]) => (<option key={k} value={k}>{v}</option>))}
            </select>
          </div>
          <textarea className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs h-16 resize-none" placeholder="Descrição" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          <div className="flex gap-2">
            <input className="flex-1 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Efeitos (ex: stamina=6)" value={Object.entries(form.effects).map(([k, v]) => `${k}=${v}`).join(', ')} onChange={(e) => {
              const eff: Record<string, number> = {};
              e.target.value.split(',').forEach((p) => {
                const [k, v] = p.split('=').map((s) => s.trim());
                if (k && !isNaN(+v)) eff[k] = +v;
              });
              setForm({ ...form, effects: eff });
            }} />
          </div>
          <input className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Tags (separadas por vírgula)" value={form.tags.join(', ')} onChange={(e) => setForm({ ...form, tags: e.target.value.split(',').map((s) => s.trim()).filter(Boolean) })} />
          <div className="flex gap-2">
            <button onClick={handleAdd} className="flex-1 py-1 bg-[#10b981] text-white rounded text-xs">Salvar</button>
            <button onClick={() => setShowForm(false)} className="flex-1 py-1 bg-[#1a1a2e] text-[#a0a0b0] rounded text-xs">Cancelar</button>
          </div>
        </div>
      )}

      {items.length === 0 ? (
        <div className="text-center text-[10px] text-[#6b7280] py-8">
          Nenhum item registrado. Arraste o nome de um item para cá ou clique em "+ Novo Item".
        </div>
      ) : (
        <div className="space-y-1">
          {items.map((item) => (
            <div key={item.id} className="bg-[#0f3460] rounded p-2 flex items-center gap-2 group">
              <span className="text-xs text-[#6b7280] w-16">{ITEM_TYPE_LABELS[item.type]}</span>
              <span className="text-xs text-[#e0e0e0] flex-1 truncate">{item.name}</span>
              {item.effects && Object.keys(item.effects).length > 0 && (
                <span className="text-[10px] text-[#10b981]">{Object.entries(item.effects).map(([k, v]) => `${v >= 0 ? '+' : ''}${v} ${k}`).join(', ')}</span>
              )}
              <button onClick={() => { const name = prompt('Nome:', item.name); if (name) updateItem(item.id, { name }); }}
                className="text-[10px] text-[#6b7280] hover:text-white opacity-0 group-hover:opacity-100 px-1">✏️</button>
              <button onClick={() => removeItem(item.id)}
                className="text-[10px] text-[#ef4444] hover:text-white opacity-0 group-hover:opacity-100 px-1">✕</button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function EnemiesTab({ enemies, addEnemy, updateEnemy, removeEnemy }: {
  enemies: EnemyTemplate[]; addEnemy: (e: EnemyTemplate) => void; updateEnemy: (id: string, d: Partial<EnemyTemplate>) => void; removeEnemy: (id: string) => void;
}) {
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<EnemyTemplate>({ id: '', name: '', skill: 5, stamina: 6, description: '', loot: [], tags: [] });

  const handleAdd = () => {
    if (!form.name.trim()) return;
    addEnemy({ ...form, id: crypto.randomUUID().slice(0, 8) });
    setForm({ id: '', name: '', skill: 5, stamina: 6, description: '', loot: [], tags: [] });
    setShowForm(false);
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h4 className="text-xs font-semibold text-white">Inimigos & NPCs</h4>
        <button onClick={() => setShowForm(!showForm)}
          className="text-xs bg-[#0f3460] text-[#e0e0e0] px-2 py-1 rounded hover:bg-[#1a4a7a]">+ Novo Inimigo</button>
      </div>

      {showForm && (
        <div className="bg-[#0f3460] rounded p-3 space-y-2">
          <input className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Nome" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          <div className="grid grid-cols-2 gap-2">
            <div><label className="text-[10px] text-[#6b7280]">SKILL</label><input type="number" className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={form.skill} onChange={(e) => setForm({ ...form, skill: +e.target.value })} /></div>
            <div><label className="text-[10px] text-[#6b7280]">STAMINA</label><input type="number" className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={form.stamina} onChange={(e) => setForm({ ...form, stamina: +e.target.value })} /></div>
          </div>
          <textarea className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs h-12 resize-none" placeholder="Descrição" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          <div className="flex gap-2">
            <button onClick={handleAdd} className="flex-1 py-1 bg-[#10b981] text-white rounded text-xs">Salvar</button>
            <button onClick={() => setShowForm(false)} className="flex-1 py-1 bg-[#1a1a2e] text-[#a0a0b0] rounded text-xs">Cancelar</button>
          </div>
        </div>
      )}

      {enemies.length === 0 ? (
        <div className="text-center text-[10px] text-[#6b7280] py-8">Nenhum inimigo registrado.</div>
      ) : (
        <div className="space-y-1">
          {enemies.map((enemy) => (
            <div key={enemy.id} className="bg-[#0f3460] rounded p-2 flex items-center gap-2 group">
              <span className="text-xs text-[#e0e0e0] flex-1 truncate">{enemy.name}</span>
              <span className="text-[10px] text-[#6b7280]">SK {enemy.skill} ST {enemy.stamina}</span>
              <button onClick={() => { const name = prompt('Nome:', enemy.name); if (name) updateEnemy(enemy.id, { name }); }}
                className="text-[10px] text-[#6b7280] hover:text-white opacity-0 group-hover:opacity-100 px-1">✏️</button>
              <button onClick={() => removeEnemy(enemy.id)}
                className="text-[10px] text-[#ef4444] hover:text-white opacity-0 group-hover:opacity-100 px-1">✕</button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function RulesTab({ rules, updateRules, addCustomAttribute, updateCustomAttribute, removeCustomAttribute }: {
  rules: import('../../types/story').GameRules;
  updateRules: (d: Partial<import('../../types/story').GameRules>) => void;
  addCustomAttribute: (a: import('../../types/story').CustomAttribute) => void;
  updateCustomAttribute: (k: string, d: Partial<import('../../types/story').CustomAttribute>) => void;
  removeCustomAttribute: (k: string) => void;
}) {
  return (
    <div className="space-y-4">
      <h4 className="text-xs font-semibold text-white">Fórmulas de Jogo</h4>
      <div className="grid grid-cols-2 gap-3">
        <div>
          <label className="text-[10px] text-[#6b7280]">Fórmula de Combate (ataque)</label>
          <input className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={rules.combatFormula} onChange={(e) => updateRules({ combatFormula: e.target.value })} />
          <span className="text-[10px] text-[#6b7280]">Ex: 2d6+skill</span>
        </div>
        <div>
          <label className="text-[10px] text-[#6b7280]">Dano por Round</label>
          <input className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={rules.damageFormula} onChange={(e) => updateRules({ damageFormula: e.target.value })} />
          <span className="text-[10px] text-[#6b7280]">Ex: 2</span>
        </div>
        <div>
          <label className="text-[10px] text-[#6b7280]">Fórmula de Teste de Sorte</label>
          <input className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={rules.luckTestFormula} onChange={(e) => updateRules({ luckTestFormula: e.target.value })} />
          <span className="text-[10px] text-[#6b7280]">Ex: 2d6</span>
        </div>
      </div>

      <div className="border-t border-[#2a2a4a] pt-4">
        <div className="flex items-center justify-between mb-2">
          <h4 className="text-xs font-semibold text-white">Atributos Customizados</h4>
          <button onClick={() => addCustomAttribute({ key: '', label: 'Novo Atributo', dice: '1d6', min: 1, max: 6, description: '' })}
            className="text-xs bg-[#0f3460] text-[#e0e0e0] px-2 py-1 rounded hover:bg-[#1a4a7a]">+ Atributo</button>
        </div>
        {rules.customAttributes.length === 0 ? (
          <div className="text-center text-[10px] text-[#6b7280] py-4">Use atributos customizados para adicionar novas mecânicas (ex: MAGIA, HONRA).</div>
        ) : (
          <div className="space-y-2">
            {rules.customAttributes.map((attr) => (
              <div key={attr.key} className="bg-[#0f3460] rounded p-2 space-y-1">
                <div className="flex items-center gap-2">
                  <input className="flex-1 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Key (ex: magic)" value={attr.key} onChange={(e) => updateCustomAttribute(attr.key, { key: e.target.value })} />
                  <input className="flex-1 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Rótulo" value={attr.label} onChange={(e) => updateCustomAttribute(attr.key, { label: e.target.value })} />
                </div>
                <div className="flex items-center gap-2">
                  <input className="w-20 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Dados" value={attr.dice} onChange={(e) => updateCustomAttribute(attr.key, { dice: e.target.value })} />
                  <span className="text-[10px] text-[#6b7280]">Min</span>
                  <input type="number" className="w-16 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={attr.min} onChange={(e) => updateCustomAttribute(attr.key, { min: +e.target.value })} />
                  <span className="text-[10px] text-[#6b7280]">Max</span>
                  <input type="number" className="w-16 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" value={attr.max} onChange={(e) => updateCustomAttribute(attr.key, { max: +e.target.value })} />
                  <button onClick={() => removeCustomAttribute(attr.key)} className="text-[#ef4444] text-xs hover:underline">✕</button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function ExternalTab({ assets, addAsset, removeAsset, storyId }: {
  assets: import('../../types/story').ExternalAsset[];
  addAsset: (a: import('../../types/story').ExternalAsset) => void;
  removeAsset: (id: string) => void;
  storyId: string;
}) {
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (!files) return;
    for (const file of Array.from(files)) {
      const type = file.type.startsWith('image/') ? 'image' as const :
        file.type.startsWith('audio/') ? 'audio' as const :
        file.name.endsWith('.txt') ? 'text' as const : 'other' as const;
      addAsset({
        id: crypto.randomUUID().slice(0, 8),
        name: file.name,
        type,
        path: `stories/${storyId}/assets/${file.name}`,
        size: formatSize(file.size),
        tags: [],
      });
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h4 className="text-xs font-semibold text-white">Arquivos Externos</h4>
        <button onClick={() => document.getElementById('ext-file-input')?.click()}
          className="text-xs bg-[#0f3460] text-[#e0e0e0] px-2 py-1 rounded hover:bg-[#1a4a7a]">+ Upload</button>
        <input id="ext-file-input" type="file" multiple className="hidden" onChange={(e) => {
          const files = e.target.files;
          if (!files) return;
          for (const file of Array.from(files)) {
            const type = file.type.startsWith('image/') ? 'image' as const :
              file.type.startsWith('audio/') ? 'audio' as const :
              file.name.endsWith('.txt') ? 'text' as const : 'other' as const;
            addAsset({
              id: crypto.randomUUID().slice(0, 8),
              name: file.name,
              type,
              path: `stories/${storyId}/assets/${file.name}`,
              size: formatSize(file.size),
              tags: [],
            });
          }
        }} />
      </div>

      <div
        className="border-2 border-dashed border-[#2a2a4a] rounded-lg p-6 text-center cursor-pointer hover:border-[#e94560] transition-colors"
        onDragOver={(e) => e.preventDefault()}
        onDrop={handleDrop}
        onClick={() => document.getElementById('ext-file-input')?.click()}
      >
        <div className="text-2xl mb-1">📁</div>
        <p className="text-xs text-[#a0a0b0]">Arraste arquivos ou clique para upload</p>
        <p className="text-[10px] text-[#6b7280]">PNG, WebP, MP3, OGG, TXT</p>
      </div>

      {assets.length === 0 ? (
        <div className="text-center text-[10px] text-[#6b7280] py-4">Nenhum arquivo externo registrado.</div>
      ) : (
        <div className="space-y-1">
          {assets.map((asset) => (
            <div key={asset.id} className="bg-[#0f3460] rounded p-2 flex items-center gap-2 group">
              <span className="text-xs">{asset.type === 'image' ? '🖼️' : asset.type === 'audio' ? '🔊' : asset.type === 'text' ? '📝' : '📄'}</span>
              <span className="text-xs text-[#e0e0e0] flex-1 truncate">{asset.name}</span>
              <span className="text-[10px] text-[#6b7280]">{asset.size}</span>
              <button onClick={() => navigator.clipboard.writeText(asset.path)}
                className="text-[10px] text-[#3b82f6] hover:text-white opacity-0 group-hover:opacity-100 px-1">📋</button>
              <button onClick={() => removeAsset(asset.id)}
                className="text-[10px] text-[#ef4444] hover:text-white opacity-0 group-hover:opacity-100 px-1">✕</button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

import { useEditorStore } from '../../stores/editor-store';
import { SECTION_TYPE_LABELS, type SectionType, type ChoiceData, type ConditionData } from '../../types/story';

export function InspectorPanel() {
  const story = useEditorStore((s) => s.story);
  const selectedId = useEditorStore((s) => s.selectedSectionId);
  const updateSection = useEditorStore((s) => s.updateSection);

  if (!story || selectedId === null) {
    return (
      <div className="w-80 bg-[#16213e] border-l border-[#2a2a4a] flex items-center justify-center shrink-0">
        <div className="text-[#6b7280] text-sm text-center px-4">
          Selecione um nó no grafo para editar
        </div>
      </div>
    );
  }

  const section = story.sections[selectedId];
  if (!section) return null;

  const handleTextChange = (text: string) => updateSection(selectedId, { text });
  const handleTypeChange = (type: SectionType) => updateSection(selectedId, { type });

  return (
    <div className="w-80 bg-[#16213e] border-l border-[#2a2a4a] flex flex-col shrink-0 overflow-hidden">
      <Header sectionId={selectedId} />
      <div className="flex-1 overflow-y-auto p-3 space-y-4">
        <TypeSelector value={section.type} onChange={handleTypeChange} />
        <TextEditor value={section.text} onChange={handleTextChange} />

        {section.type === 'narrative' && <ChoicesEditor sectionId={selectedId} choices={section.choices ?? []} />}
        {section.type === 'combat' && <CombatEditor sectionId={selectedId} combat={section.combat} />}
        {section.type === 'test' && <TestEditor sectionId={selectedId} test={section.test} />}
        {section.type === 'itemGate' && <ItemGateEditor sectionId={selectedId} gate={section.itemGate} />}
        {section.type === 'random' && <RandomEditor sectionId={selectedId} random={section.random} />}
        {section.type === 'ending' && <EndingEditor sectionId={selectedId} ending={section.ending} />}

        <OnEnterEditor sectionId={selectedId} onEnter={section.onEnter} />
        <PresentationEditor sectionId={selectedId} presentation={section.presentation} />
      </div>
    </div>
  );
}

function Header({ sectionId }: { sectionId: number }) {
  const story = useEditorStore((s) => s.story);
  const removeSection = useEditorStore((s) => s.removeSection);
  const isStart = story?.metadata.startSection === sectionId;

  return (
    <div className="p-3 border-b border-[#2a2a4a]">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-white">
          Seção #{sectionId}
          {isStart && <span className="ml-2 text-[10px] bg-[#e94560] text-white px-1.5 py-0.5 rounded">INÍCIO</span>}
        </h3>
        <div className="flex items-center gap-2">
          <span className="text-[10px] text-[#6b7280]">ID: {sectionId}</span>
          {!isStart && (
            <button
              onClick={() => { if (confirm(`Excluir seção #${sectionId}?`)) removeSection(sectionId); }}
              className="text-[10px] text-[#ef4444] hover:text-white px-1.5 py-0.5 rounded hover:bg-[#ef444420]"
              title="Excluir seção"
            >
              🗑️
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function TypeSelector({ value, onChange }: { value: SectionType; onChange: (t: SectionType) => void }) {
  return (
    <div>
      <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Tipo</label>
      <select
        className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1.5 text-sm focus:outline-none focus:border-[#e94560]"
        value={value}
        onChange={(e) => onChange(e.target.value as SectionType)}
      >
        {(Object.keys(SECTION_TYPE_LABELS) as SectionType[]).map((t) => (
          <option key={t} value={t}>{SECTION_TYPE_LABELS[t]}</option>
        ))}
      </select>
    </div>
  );
}

function TextEditor({ value, onChange }: { value: string; onChange: (t: string) => void }) {
  return (
    <div>
      <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Texto</label>
      <textarea
        className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1.5 text-sm focus:outline-none focus:border-[#e94560] resize-none h-28"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="Digite o texto narrativo desta seção..."
      />
    </div>
  );
}

function ChoicesEditor({ sectionId, choices }: { sectionId: number; choices: ChoiceData[] }) {
  const updateSection = useEditorStore((s) => s.updateSection);

  const setChoices = (c: ChoiceData[]) => updateSection(sectionId, { choices: c });

  const handleAdd = () => setChoices([...choices, { id: crypto.randomUUID().slice(0, 8), text: 'Nova escolha', targetSection: 1, conditions: [] }]);

  const handleUpdate = (index: number, data: Partial<ChoiceData>) => {
    const updated = choices.map((c, i) => (i === index ? { ...c, ...data } : c));
    setChoices(updated);
  };

  const handleRemove = (index: number) => setChoices(choices.filter((_, i) => i !== index));

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Escolhas</label>
        <button onClick={handleAdd} className="text-xs bg-[#0f3460] text-[#e0e0e0] px-2 py-0.5 rounded hover:bg-[#1a4a7a]">+</button>
      </div>
      {choices.map((choice, i) => (
        <div key={choice.id ?? i} className="bg-[#0f3460] rounded p-2 space-y-2">
          <div className="flex items-center gap-1">
            <span className="text-[10px] text-[#6b7280]">#{i + 1}</span>
            <button onClick={() => handleRemove(i)} className="text-[#ef4444] text-xs ml-auto hover:underline">Remover</button>
          </div>
          <input
            className="w-full bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-sm focus:outline-none focus:border-[#e94560]"
            value={choice.text}
            onChange={(e) => handleUpdate(i, { text: e.target.value })}
            placeholder="Texto da escolha"
          />
          <div className="flex items-center gap-2">
            <span className="text-xs text-[#a0a0b0]">→ Seção</span>
            <input
              type="number"
              className="w-20 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-sm focus:outline-none focus:border-[#e94560]"
              value={choice.targetSection}
              onChange={(e) => handleUpdate(i, { targetSection: +e.target.value })}
            />
          </div>
          <ConditionsEditor choiceIndex={i} conditions={choice.conditions} onUpdate={(conds) => handleUpdate(i, { conditions: conds })} />
        </div>
      ))}
    </div>
  );
}

function ConditionsEditor({ choiceIndex, conditions, onUpdate }: { choiceIndex: number; conditions: ConditionData[]; onUpdate: (c: ConditionData[]) => void }) {
  const handleAdd = () => onUpdate([...conditions, { type: 'hasItem', key: '', op: '==', value: '' }]);
  const handleUpdate = (index: number, data: Partial<ConditionData>) => {
    onUpdate(conditions.map((c, i) => (i === index ? { ...c, ...data } : c)));
  };
  const handleRemove = (index: number) => onUpdate(conditions.filter((_, i) => i !== index));

  return (
    <div className="space-y-2 pl-2 border-l border-[#2a2a4a]">
      <div className="flex items-center justify-between">
        <span className="text-[10px] text-[#6b7280]">Condições</span>
        <button onClick={handleAdd} className="text-[10px] bg-[#1a1a2e] text-[#a0a0b0] px-1.5 py-0.5 rounded hover:bg-[#2a2a4a]">+</button>
      </div>
      {conditions.map((cond, i) => (
        <div key={i} className="bg-[#1a1a2e] rounded p-1.5 space-y-1">
          <div className="flex items-center gap-1">
            <span className="text-[10px] text-[#6b7280]">Cond #{i + 1}</span>
            <button onClick={() => handleRemove(i)} className="text-[#ef4444] text-[10px] ml-auto">Remover</button>
          </div>
          <select className="w-full bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1.5 py-1 text-[11px]" value={cond.type} onChange={(e) => handleUpdate(i, { type: e.target.value as ConditionData['type'] })}>
            <option value="hasItem">Tem Item</option>
            <option value="hasFlag">Tem Flag</option>
            <option value="skill">Skill</option>
            <option value="stamina">Stamina</option>
            <option value="luck">Luck</option>
            <option value="gold">Ouro</option>
            <option value="counter">Contador</option>
          </select>
          <div className="flex gap-1">
            <input className="flex-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1.5 py-1 text-[11px]" placeholder="Key" value={cond.key} onChange={(e) => handleUpdate(i, { key: e.target.value })} />
            <select className="w-16 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1 py-1 text-[11px]" value={cond.op} onChange={(e) => handleUpdate(i, { op: e.target.value as ConditionData['op'] })}>
              <option value="==">==</option>
              <option value="!=">!=</option>
              <option value=">=">&gt;=</option>
              <option value="<=">&lt;=</option>
              <option value=">">&gt;</option>
              <option value="<">&lt;</option>
            </select>
            <input className="w-16 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1.5 py-1 text-[11px]" placeholder="Valor" value={String(cond.value)} onChange={(e) => handleUpdate(i, { value: isNaN(+e.target.value) ? e.target.value : +e.target.value })} />
          </div>
        </div>
      ))}
    </div>
  );
}

function CombatEditor({ sectionId, combat }: { sectionId: number; combat?: import('../../types/story').CombatData }) {
  const updateSection = useEditorStore((s) => s.updateSection);
  const data = combat ?? { enemyName: '', enemySkill: 5, enemyStamina: 6, victoryTarget: 0, defeatTarget: 0, fleeTarget: 0, allowFlee: true, lootOnVictory: [] };

  const update = (partial: Partial<typeof data>) => updateSection(sectionId, { combat: { ...data, ...partial } });

  return (
    <div className="space-y-3">
      <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Combate</label>
      <NumberField label="SKILL do Inimigo" value={data.enemySkill} onChange={(v) => update({ enemySkill: v })} />
      <NumberField label="STAMINA do Inimigo" value={data.enemyStamina} onChange={(v) => update({ enemyStamina: v })} />
      <TextField label="Nome do Inimigo" value={data.enemyName} onChange={(v) => update({ enemyName: v })} />
      <div className="flex items-center gap-2">
        <input type="checkbox" checked={data.allowFlee} onChange={(e) => update({ allowFlee: e.target.checked })} className="accent-[#e94560]" />
        <span className="text-xs text-[#a0a0b0]">Permitir fuga</span>
      </div>
      <div className="grid grid-cols-3 gap-2">
        <NumberField label="Vitória →" value={data.victoryTarget} onChange={(v) => update({ victoryTarget: v })} color="#10b981" />
        <NumberField label="Derrota →" value={data.defeatTarget} onChange={(v) => update({ defeatTarget: v })} color="#ef4444" />
        <NumberField label="Fuga →" value={data.fleeTarget} onChange={(v) => update({ fleeTarget: v })} color="#f59e0b" />
      </div>
      <div>
        <label className="text-xs text-[#a0a0b0]">Loot ao Vencer (itens separados por vírgula)</label>
        <input
          className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-sm focus:outline-none focus:border-[#e94560]"
          value={data.lootOnVictory?.join(', ') ?? ''}
          onChange={(e) => update({ lootOnVictory: e.target.value.split(',').map((s) => s.trim()).filter(Boolean) })}
          placeholder="espada, poção de cura"
        />
      </div>
    </div>
  );
}

function TestEditor({ sectionId, test }: { sectionId: number; test?: import('../../types/story').TestData }) {
  const updateSection = useEditorStore((s) => s.updateSection);
  const data = test ?? { attribute: 'skill' as const, difficulty: 8, successTarget: 0, failTarget: 0 };

  const update = (partial: Partial<typeof data>) => updateSection(sectionId, { test: { ...data, ...partial } });

  return (
    <div className="space-y-3">
      <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Teste</label>
      <div>
        <label className="text-xs text-[#a0a0b0]">Atributo</label>
        <select className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-sm" value={data.attribute} onChange={(e) => update({ attribute: e.target.value as typeof data.attribute })}>
          <option value="skill">Habilidade</option>
          <option value="luck">Sorte</option>
          <option value="custom">Personalizado</option>
        </select>
      </div>
      <NumberField label="Dificuldade" value={data.difficulty} onChange={(v) => update({ difficulty: v })} />
      <div className="grid grid-cols-2 gap-2">
        <NumberField label="Sucesso →" value={data.successTarget} onChange={(v) => update({ successTarget: v })} color="#10b981" />
        <NumberField label="Falha →" value={data.failTarget} onChange={(v) => update({ failTarget: v })} color="#ef4444" />
      </div>
    </div>
  );
}

function ItemGateEditor({ sectionId, gate }: { sectionId: number; gate?: import('../../types/story').ItemGateData }) {
  const updateSection = useEditorStore((s) => s.updateSection);
  const data = gate ?? { item: '', hasItemTarget: 0, noItemTarget: 0 };

  const update = (partial: Partial<typeof data>) => updateSection(sectionId, { itemGate: { ...data, ...partial } });

  return (
    <div className="space-y-3">
      <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Item Gate</label>
      <TextField label="Item verificado" value={data.item} onChange={(v) => update({ item: v })} />
      <div className="grid grid-cols-2 gap-2">
        <NumberField label="Tem item →" value={data.hasItemTarget} onChange={(v) => update({ hasItemTarget: v })} color="#10b981" />
        <NumberField label="Não tem →" value={data.noItemTarget} onChange={(v) => update({ noItemTarget: v })} color="#ef4444" />
      </div>
    </div>
  );
}

function RandomEditor({ sectionId, random }: { sectionId: number; random?: import('../../types/story').RandomData }) {
  const updateSection = useEditorStore((s) => s.updateSection);
  const outcomes = random?.outcomes ?? [];

  const handleAdd = () => updateSection(sectionId, { random: { outcomes: [...outcomes, { targetSection: 0, weight: 0.5 }] } });
  const handleRemove = (index: number) => updateSection(sectionId, { random: { outcomes: outcomes.filter((_, i) => i !== index) } });
  const handleUpdate = (index: number, data: { targetSection?: number; weight?: number }) => {
    const updated = outcomes.map((o, i) => (i === index ? { ...o, ...data } : o));
    updateSection(sectionId, { random: { outcomes: updated } });
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Resultados Aleatórios</label>
        <button onClick={handleAdd} className="text-xs bg-[#0f3460] text-[#e0e0e0] px-2 py-0.5 rounded hover:bg-[#1a4a7a]">+</button>
      </div>
      {outcomes.map((outcome, i) => (
        <div key={i} className="bg-[#0f3460] rounded p-2 flex items-center gap-2">
          <span className="text-[10px] text-[#6b7280]">#{i + 1}</span>
          <input
            type="number"
            className="w-16 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1.5 py-1 text-xs"
            value={outcome.targetSection}
            onChange={(e) => handleUpdate(i, { targetSection: +e.target.value })}
            placeholder="Seção"
          />
          <span className="text-xs text-[#a0a0b0]">Peso</span>
          <input
            type="number"
            step="0.1"
            min="0"
            max="1"
            className="w-16 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-1.5 py-1 text-xs"
            value={outcome.weight}
            onChange={(e) => handleUpdate(i, { weight: +e.target.value })}
          />
          <button onClick={() => handleRemove(i)} className="text-[#ef4444] text-xs ml-auto">✕</button>
        </div>
      ))}
    </div>
  );
}

function EndingEditor({ sectionId, ending }: { sectionId: number; ending?: import('../../types/story').EndingData }) {
  const updateSection = useEditorStore((s) => s.updateSection);
  const type = ending?.type ?? 'neutral';

  return (
    <div className="space-y-3">
      <label className="text-[10px] text-[#6b7280] uppercase tracking-wider">Final</label>
      <div>
        <label className="text-xs text-[#a0a0b0]">Tipo de Final</label>
        <select
          className="w-full mt-1 bg-[#0f3460] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-sm"
          value={type}
          onChange={(e) => updateSection(sectionId, { ending: { type: e.target.value as 'victory' | 'defeat' | 'neutral' } })}
        >
          <option value="victory">Vitória</option>
          <option value="defeat">Derrota</option>
          <option value="neutral">Neutro</option>
        </select>
      </div>
    </div>
  );
}

function OnEnterEditor({ sectionId, onEnter }: { sectionId: number; onEnter?: import('../../types/story').OnEnterData }) {
  const updateSection = useEditorStore((s) => s.updateSection);
  const data = onEnter ?? {};

  const update = (partial: Partial<typeof data>) => updateSection(sectionId, { onEnter: { ...data, ...partial } });

  return (
    <details className="space-y-3 bg-[#0f3460] rounded p-3">
      <summary className="text-[10px] text-[#6b7280] uppercase tracking-wider cursor-pointer">Ao Entrar (Efeitos)</summary>
      <div className="mt-3 space-y-2">
        <TextField label="Adicionar Itens (vírgula)" value={data.addItems?.join(', ') ?? ''} onChange={(v) => update({ addItems: v.split(',').map((s) => s.trim()).filter(Boolean) })} />
        <TextField label="Remover Itens (vírgula)" value={data.removeItems?.join(', ') ?? ''} onChange={(v) => update({ removeItems: v.split(',').map((s) => s.trim()).filter(Boolean) })} />
        <NumberField label="Modificar Ouro (+/-)" value={data.modifyGold ?? 0} onChange={(v) => update({ modifyGold: v })} />
        <NumberField label="Modificar Stamina (+/-)" value={data.modifyStamina ?? 0} onChange={(v) => update({ modifyStamina: v })} />
        <NumberField label="Modificar Sorte (+/-)" value={data.modifyLuck ?? 0} onChange={(v) => update({ modifyLuck: v })} />
      </div>
    </details>
  );
}

function PresentationEditor({ sectionId, presentation }: { sectionId: number; presentation?: import('../../types/story').PresentationData }) {
  const updateSection = useEditorStore((s) => s.updateSection);
  const data = presentation ?? {};

  const update = (partial: Partial<typeof data>) => updateSection(sectionId, { presentation: { ...data, ...partial } });

  return (
    <details className="space-y-3 bg-[#0f3460] rounded p-3">
      <summary className="text-[10px] text-[#6b7280] uppercase tracking-wider cursor-pointer">Apresentação (Assets)</summary>
      <div className="mt-3 space-y-2">
        <TextField label="Ilustração (path)" value={data.illustration ?? ''} onChange={(v) => update({ illustration: v || undefined })} placeholder="illustrations/cave.webp" />
        <TextField label="Som Ambiente (path)" value={data.ambientSound ?? ''} onChange={(v) => update({ ambientSound: v || undefined })} placeholder="audio/ambient/cave.ogg" />
        <div>
          <label className="text-xs text-[#a0a0b0]">Narração</label>
          <div className="grid grid-cols-2 gap-2 mt-1">
            <input className="bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Voz" value={data.narration?.voice ?? ''} onChange={(e) => update({ narration: { ...data.narration, voice: e.target.value, speed: data.narration?.speed ?? 1, emphasis: data.narration?.emphasis ?? [] } })} />
            <input type="number" step="0.1" min="0.5" max="2" className="bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-xs" placeholder="Velocidade" value={data.narration?.speed ?? 1} onChange={(e) => update({ narration: { ...data.narration, voice: data.narration?.voice ?? '', speed: +e.target.value, emphasis: data.narration?.emphasis ?? [] } })} />
          </div>
        </div>
      </div>
    </details>
  );
}

function TextField({ label, value, onChange, placeholder }: { label: string; value: string; onChange: (v: string) => void; placeholder?: string }) {
  return (
    <div>
      <label className="text-xs text-[#a0a0b0]">{label}</label>
      <input className="w-full mt-1 bg-[#1a1a2e] text-[#e0e0e0] border border-[#2a2a4a] rounded px-2 py-1 text-sm focus:outline-none focus:border-[#e94560]" value={value} onChange={(e) => onChange(e.target.value)} placeholder={placeholder} />
    </div>
  );
}

function NumberField({ label, value, onChange, color }: { label: string; value: number; onChange: (v: number) => void; color?: string }) {
  const textColor = color === '#10b981' ? 'text-[#10b981]' : color === '#ef4444' ? 'text-[#ef4444]' : color === '#f59e0b' ? 'text-[#f59e0b]' : 'text-[#e0e0e0]';
  return (
    <div>
      <label className="text-xs text-[#a0a0b0]">{label}</label>
      <input type="number" className={`w-full mt-1 bg-[#1a1a2e] border border-[#2a2a4a] rounded px-2 py-1 text-sm focus:outline-none focus:border-[#e94560] ${textColor}`} value={value} onChange={(e) => onChange(+e.target.value)} />
    </div>
  );
}

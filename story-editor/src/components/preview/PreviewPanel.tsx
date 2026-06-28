import { useState, useCallback } from 'react';
import { useEditorStore } from '../../stores/editor-store';
import type { SectionData, ChoiceData } from '../../types/story';

interface PreviewState {
  currentSectionId: number;
  history: number[];
  character: {
    skill: number;
    stamina: number;
    maxStamina: number;
    luck: number;
    maxLuck: number;
    gold: number;
    inventory: string[];
    flags: Record<string, boolean | number>;
    counters: Record<string, number>;
  };
  characterSnapshots: Map<number, CharacterSnapshot>;
  combatState: CombatState | null;
  combatSectionText: string | null;
  message: string | null;
}

interface CharacterSnapshot {
  inventory: string[];
  flags: Record<string, boolean | number>;
  counters: Record<string, number>;
  gold: number;
  stamina: number;
  maxStamina: number;
  luck: number;
  maxLuck: number;
  skill: number;
}

function snapshotChar(char: PreviewState['character']): CharacterSnapshot {
  return {
    inventory: [...char.inventory],
    flags: { ...char.flags },
    counters: { ...char.counters },
    gold: char.gold,
    stamina: char.stamina,
    maxStamina: char.maxStamina,
    luck: char.luck,
    maxLuck: char.maxLuck,
    skill: char.skill,
  };
}

function restoreChar(snap: CharacterSnapshot): PreviewState['character'] {
  return {
    inventory: [...snap.inventory],
    flags: { ...snap.flags },
    counters: { ...snap.counters },
    gold: snap.gold,
    stamina: snap.stamina,
    maxStamina: snap.maxStamina,
    luck: snap.luck,
    maxLuck: snap.maxLuck,
    skill: snap.skill,
  };
}

interface CombatState {
  enemyName: string;
  enemySkill: number;
  enemyStamina: number;
  enemyMaxStamina: number;
  phase: 'fighting' | 'result';
  rounds: string[];
  victoryTarget: number;
  defeatTarget: number;
  fleeTarget: number;
  allowFlee: boolean;
}

function createInitialState(story: import('../../types/story').StoryData): PreviewState {
  const attr = story.characterCreation.attributes;
  const skill = rollDice(attr.skill?.dice ?? '1d6+6');
  const stamina = rollDice(attr.stamina?.dice ?? '2d6+12');
  const luck = rollDice(attr.luck?.dice ?? '1d6+6');

  const flags: Record<string, boolean | number> = {};
  for (const [key, def] of Object.entries(story.flags ?? {})) {
    flags[key] = def.default;
  }

  return {
    currentSectionId: story.metadata.startSection,
    history: [story.metadata.startSection],
    character: {
      skill,
      stamina,
      maxStamina: stamina,
      luck,
      maxLuck: luck,
      gold: story.characterCreation.startingGold,
      inventory: [...story.characterCreation.startingItems],
      flags,
      counters: {},
    },
    characterSnapshots: new Map(),
    combatState: null,
    combatSectionText: null,
    message: null,
  };
}

function rollDice(expr: string): number {
  const match = expr.match(/(\d+)d(\d+)([+-]\d+)?/);
  if (!match) return 0;
  const count = parseInt(match[1]);
  const sides = parseInt(match[2]);
  const mod = match[3] ? parseInt(match[3]) : 0;
  let total = 0;
  for (let i = 0; i < count; i++) total += Math.floor(Math.random() * sides) + 1;
  return total + mod;
}

function roll2d6(): number {
  return Math.floor(Math.random() * 6) + 1 + Math.floor(Math.random() * 6) + 1;
}

export function PreviewPanel() {
  const story = useEditorStore((s) => s.story);
  const [state, setState] = useState<PreviewState | null>(null);
  const [platform, setPlatform] = useState<'mobile' | 'tablet' | 'pc'>('mobile');
  const [accessibilityMode, setAccessibilityMode] = useState(false);

  const startPreview = useCallback(() => {
    if (!story) return;
    setState(createInitialState(story));
  }, [story]);

  const resetPreview = useCallback(() => {
    setState(null);
  }, []);

  if (!story) return null;

  if (!state) {
    return (
      <div className="h-full flex flex-col items-center justify-center bg-[#1a1a2e] p-8">
        <div className="text-center">
          <div className="text-5xl mb-4">🎮</div>
          <h2 className="text-xl font-bold text-white mb-2">Preview da História</h2>
          <p className="text-[#a0a0b0] text-sm mb-6">
            Teste sua história como um jogador. Navegue pelas seções, faça escolhas e veja o fluxo em ação.
          </p>
          <div className="flex gap-3 justify-center mb-6">
            {(['mobile', 'tablet', 'pc'] as const).map((p) => (
              <button
                key={p}
                onClick={() => setPlatform(p)}
                className={`px-3 py-1.5 text-xs rounded ${
                  platform === p ? 'bg-[#e94560] text-white' : 'bg-[#0f3460] text-[#a0a0b0]'
                }`}
              >
                {p === 'mobile' ? '📱' : p === 'tablet' ? '📋' : '🖥️'} {p === 'mobile' ? 'Mobile' : p === 'tablet' ? 'Tablet' : 'PC'}
              </button>
            ))}
          </div>
          <button
            onClick={startPreview}
            className="px-8 py-3 bg-[#10b981] text-white rounded-lg font-semibold hover:bg-[#059669] transition-colors"
          >
            ▶ Iniciar Preview
          </button>
        </div>
      </div>
    );
  }

  const section = story.sections[state.currentSectionId];
  const isMobile = platform === 'mobile';
  const isTablet = platform === 'tablet';

  const goToSection = (targetId: number) => {
    setState((s) => {
      if (!s) return s;
      const targetSection = story.sections[targetId];
      if (!targetSection) return s;
      const charClone = restoreChar(snapshotChar(s.character));
      evalOnEnter(targetSection, charClone);
      const snapshots = new Map(s.characterSnapshots);
      snapshots.set(s.currentSectionId, snapshotChar(s.character));
      return {
        ...s,
        currentSectionId: targetId,
        history: [...s.history, targetId],
        character: charClone,
        characterSnapshots: snapshots,
        combatState: null,
        combatSectionText: null,
        message: null,
      };
    });
  };

  const handleChoice = (choice: ChoiceData) => {
    if (!evaluateConditions(choice.conditions, state)) return;
    const targetSection = story.sections[choice.targetSection];
    if (!targetSection) return;

    if (targetSection.type === 'combat' && targetSection.combat) {
      setState((s) => {
        if (!s) return s;
        const charClone = restoreChar(snapshotChar(s.character));
        evalOnEnter(targetSection, charClone);
        return {
          ...s,
          combatSectionText: targetSection.text,
          character: charClone,
          combatState: {
            enemyName: targetSection.combat!.enemyName,
            enemySkill: targetSection.combat!.enemySkill,
            enemyStamina: targetSection.combat!.enemyStamina,
            enemyMaxStamina: targetSection.combat!.enemyStamina,
            phase: 'fighting' as const,
            rounds: [],
            victoryTarget: targetSection.combat!.victoryTarget,
            defeatTarget: targetSection.combat!.defeatTarget,
            fleeTarget: targetSection.combat!.fleeTarget,
            allowFlee: targetSection.combat!.allowFlee,
          },
        };
      });
    } else {
      goToSection(choice.targetSection);
    }
  };

  const startCombat = (section: SectionData) => {
    if (!section.combat) return;
    setState((s) => {
      if (!s) return s;
      const charClone = restoreChar(snapshotChar(s.character));
      evalOnEnter(section, charClone);
      return {
        ...s,
        combatSectionText: section.text,
        character: charClone,
        combatState: {
          enemyName: section.combat!.enemyName,
          enemySkill: section.combat!.enemySkill,
          enemyStamina: section.combat!.enemyStamina,
          enemyMaxStamina: section.combat!.enemyStamina,
          phase: 'fighting' as const,
          rounds: [],
          victoryTarget: section.combat!.victoryTarget,
          defeatTarget: section.combat!.defeatTarget,
          fleeTarget: section.combat!.fleeTarget,
          allowFlee: section.combat!.allowFlee,
        },
      };
    });
  };

  const fightRound = () => {
    setState((s) => {
      if (!s || !s.combatState || s.combatState.phase !== 'fighting') return s;
      const cs = { ...s.combatState, rounds: [...s.combatState.rounds] };
      const char = { ...s.character };

      const playerRoll = roll2d6() + char.skill;
      const enemyRoll = roll2d6() + cs.enemySkill;

      if (playerRoll > enemyRoll) {
        cs.enemyStamina -= 2;
        cs.rounds.push(`Você ${playerRoll} vs ${enemyRoll} — ${cs.enemyName} perde 2 STAMINA (${Math.max(0, cs.enemyStamina)})`);
      } else if (enemyRoll > playerRoll) {
        char.stamina -= 2;
        cs.rounds.push(`${cs.enemyName} ${enemyRoll} vs você ${playerRoll} — Você perde 2 STAMINA (${Math.max(0, char.stamina)})`);
      } else {
        cs.rounds.push(`Empate ${playerRoll} a ${enemyRoll} — ninguém se fere neste round.`);
      }

      if (cs.enemyStamina <= 0) {
        cs.phase = 'result';
        cs.rounds.push(`🏆 Vitória! ${cs.enemyName} foi derrotado.`);
        return { ...s, combatState: cs, character: char, message: 'victory' };
      }
      if (char.stamina <= 0) {
        cs.phase = 'result';
        cs.rounds.push('💀 Você foi derrotado...');
        return { ...s, combatState: cs, character: char, message: 'defeat' };
      }

      return { ...s, combatState: cs, character: char };
    });
  };

  const fleeCombat = () => {
    setState((s) => {
      if (!s || !s.combatState) return s;
      const char = { ...s.character, luck: Math.max(0, s.character.luck - 1) };
      const test = roll2d6();
      const success = test <= char.luck;
      return {
        ...s,
        combatState: {
          ...s.combatState,
          phase: 'result',
          rounds: [...s.combatState.rounds, success
            ? `🏃 Fuga bem sucedida! (Teste de SORTE: ${test} ≤ ${char.luck})`
            : `❌ Falha na fuga! (Teste de SORTE: ${test} > ${char.luck})`],
        },
        character: char,
        message: success ? 'flee_success' : 'flee_fail',
      };
    });
  };

  const resolveCombatResult = () => {
    if (!state.combatState) return;
    if (state.message === 'victory') goToSection(state.combatState.victoryTarget);
    else if (state.message === 'defeat') goToSection(state.combatState.defeatTarget);
    else if (state.message === 'flee_success') goToSection(state.combatState.fleeTarget);
  };

  const handleTest = (section: SectionData) => {
    if (!section.test) return;
    const attrValue = section.test.attribute === 'skill'
      ? state.character.skill
      : section.test.attribute === 'luck'
        ? state.character.luck
        : 7;
    const roll = roll2d6();
    const total = roll + attrValue;
    const success = total >= section.test.difficulty;
    setState((s) => {
      if (!s) return s;
      return {
        ...s,
        message: success
          ? `🎲 Sucesso! ${total} ≥ ${section.test!.difficulty}`
          : `🎲 Falha! ${total} < ${section.test!.difficulty}`,
      };
    });
    setTimeout(() => {
      if (success) goToSection(section.test!.successTarget);
      else goToSection(section.test!.failTarget);
    }, 800);
  };

  const handleItemGate = (section: SectionData) => {
    if (!section.itemGate) return;
    const hasItem = state.character.inventory.includes(section.itemGate.item);
    goToSection(hasItem ? section.itemGate.hasItemTarget : section.itemGate.noItemTarget);
  };

  const handleRandom = (section: SectionData) => {
    if (!section.random) return;
    const totalWeight = section.random.outcomes.reduce((s, o) => s + o.weight, 0);
    let roll = Math.random() * totalWeight;
    for (const outcome of section.random.outcomes) {
      roll -= outcome.weight;
      if (roll <= 0) {
        goToSection(outcome.targetSection);
        return;
      }
    }
  };

  const stepBack = () => {
    setState((s) => {
      if (!s || s.history.length <= 1) return s;
      const newHistory = s.history.slice(0, -1);
      const prevSectionId = newHistory[newHistory.length - 1];
      const snap = s.characterSnapshots.get(prevSectionId);
      const character = snap ? restoreChar(snap) : s.character;
      return {
        ...s,
        currentSectionId: prevSectionId,
        history: newHistory,
        character,
        combatState: null,
        combatSectionText: null,
        message: null,
      };
    });
  };

  const renderSection = () => {
    if (!section) {
      return (
        <div className="p-4 text-center text-[#ef4444]">
          Seção #{state.currentSectionId} não encontrada!
        </div>
      );
    }

    return (
      <div className="space-y-4">
        <div className="flex items-center justify-between text-xs text-[#6b7280]">
          <span>Seção #{section.id}</span>
          <span className="bg-[#0f3460] px-2 py-0.5 rounded">{section.type}</span>
        </div>

        {section.presentation?.illustration && (
          <div className="bg-[#0f3460] rounded h-32 flex items-center justify-center text-[#6b7280] text-xs">
            🖼️ {section.presentation.illustration}
          </div>
        )}

        <div className={`text-sm leading-relaxed whitespace-pre-wrap ${accessibilityMode ? 'text-2xl leading-relaxed' : ''}`}>
          {section.text || '(sem texto)'}
        </div>

        {state.message && (
          <div className="bg-[#0f3460] rounded p-2 text-xs text-[#f59e0b] text-center">{state.message}</div>
        )}

        {state.combatState ? (
          <div className="space-y-3">
            {state.combatSectionText && (
              <div className="text-sm text-[#e0e0e0] italic border-l-2 border-[#ef4444] pl-3 whitespace-pre-wrap">
                {state.combatSectionText}
              </div>
            )}
            <div className="bg-[#0f3460] rounded p-3 space-y-2">
              <div className="flex justify-between text-sm">
                <span>Você: SKILL {state.character.skill} STAM {state.character.stamina}/{state.character.maxStamina}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span>{state.combatState.enemyName}: SKILL {state.combatState.enemySkill} STAM {Math.max(0, state.combatState.enemyStamina)}/{state.combatState.enemyMaxStamina}</span>
              </div>
              <div className="h-2 bg-[#1a1a2e] rounded overflow-hidden">
                <div className="h-full bg-[#10b981] transition-all" style={{ width: `${Math.max(0, state.combatState.enemyStamina / state.combatState.enemyMaxStamina * 100)}%` }} />
              </div>
            </div>

            {state.combatState.rounds.length > 0 && (
              <div className="bg-[#0f3460] rounded p-2 max-h-32 overflow-y-auto space-y-1">
                {state.combatState.rounds.map((r, i) => (
                  <div key={i} className="text-xs text-[#a0a0b0]">{r}</div>
                ))}
              </div>
            )}

            {state.combatState.phase === 'fighting' ? (
              <div className="flex gap-2">
                <button onClick={fightRound} className="flex-1 py-2 bg-[#e94560] text-white rounded text-sm font-semibold hover:bg-[#d63850]">
                  ⚔️ Atacar
                </button>
                {state.combatState.allowFlee && (
                  <button onClick={fleeCombat} className="flex-1 py-2 bg-[#f59e0b] text-black rounded text-sm font-semibold hover:bg-[#d97706]">
                    🏃 Fugir
                  </button>
                )}
              </div>
            ) : (
              <button onClick={resolveCombatResult} className="w-full py-2 bg-[#3b82f6] text-white rounded text-sm font-semibold">
                Continuar
              </button>
            )}
          </div>
        ) : section.choices ? (
          <div className="space-y-2">
            {section.choices.map((choice, i) => {
              const conditionsMet = evaluateConditions(choice.conditions, state);
              return (
                <button
                  key={choice.id ?? i}
                  onClick={() => handleChoice(choice)}
                  disabled={!conditionsMet}
                  className={`w-full text-left p-3 rounded text-sm transition-colors ${
                    conditionsMet
                      ? 'bg-[#0f3460] text-[#e0e0e0] hover:bg-[#1a4a7a] border border-[#2a2a4a]'
                      : 'bg-[#0a0a1a] text-[#4a4a5a] border border-[#1a1a2e] cursor-not-allowed'
                  } ${accessibilityMode ? 'text-xl p-4 border-2' : ''}`}
                >
                  {conditionsMet ? '▶' : '🔒'} {choice.text}
                </button>
              );
            })}
          </div>
        ) : section.type === 'combat' && section.combat ? (
          <button onClick={() => startCombat(section)} className="w-full py-3 bg-[#e94560] text-white rounded text-sm font-semibold">
            ⚔️ Iniciar Combate
          </button>
        ) : section.type === 'test' && section.test ? (
          <button onClick={() => handleTest(section)} className="w-full py-3 bg-[#10b981] text-white rounded text-sm font-semibold">
            🎲 Realizar Teste
          </button>
        ) : section.type === 'itemGate' && section.itemGate ? (
          <button onClick={() => handleItemGate(section)} className="w-full py-3 bg-[#f59e0b] text-black rounded text-sm font-semibold">
            🔑 Verificar Item
          </button>
        ) : section.type === 'random' ? (
          <button onClick={() => handleRandom(section)} className="w-full py-3 bg-[#8b5cf6] text-white rounded text-sm font-semibold">
            🔀 Resultado Aleatório
          </button>
        ) : section.type === 'ending' ? (
          <div className={`text-center py-4 ${section.ending?.type === 'victory' ? 'text-[#10b981]' : section.ending?.type === 'defeat' ? 'text-[#ef4444]' : 'text-[#f59e0b]'} text-lg font-bold`}>
            {section.ending?.type === 'victory' ? '🏆 Vitória!' : section.ending?.type === 'defeat' ? '💀 Derrota' : '🏁 Fim'}
          </div>
        ) : null}

        {state.history.length > 1 && (
          <button onClick={stepBack} className="text-xs text-[#6b7280] hover:text-white mt-2">
            ↩ Voltar um passo
          </button>
        )}
      </div>
    );
  };

  const maxWidth = isMobile ? 'max-w-sm' : isTablet ? 'max-w-xl' : 'max-w-2xl';

  return (
    <div className={`h-full flex flex-col ${accessibilityMode ? 'bg-black' : 'bg-[#1a1a2e]'}`}>
      <div className="flex items-center justify-between p-3 bg-[#16213e] border-b border-[#2a2a4a] shrink-0">
        <div className="flex items-center gap-2">
          <button onClick={resetPreview} className="text-[#a0a0b0] hover:text-white text-sm">← Sair</button>
          <span className="text-xs text-[#6b7280]">|</span>
          <span className="text-sm text-white">{story.metadata.title}</span>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setAccessibilityMode(!accessibilityMode)}
            className={`text-xs px-2 py-1 rounded ${accessibilityMode ? 'bg-[#f59e0b] text-black' : 'bg-[#0f3460] text-[#a0a0b0]'}`}
          >
            {accessibilityMode ? '♿ Alto Contraste ON' : 'Acessibilidade'}
          </button>
          <div className="flex bg-[#0f3460] rounded p-0.5">
            {(['mobile', 'tablet', 'pc'] as const).map((p) => (
              <button
                key={p}
                onClick={() => setPlatform(p)}
                className={`px-2 py-0.5 text-xs rounded ${platform === p ? 'bg-[#e94560] text-white' : 'text-[#6b7280]'}`}
              >
                {p === 'mobile' ? '📱' : p === 'tablet' ? '📋' : '🖥️'}
              </button>
            ))}
          </div>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto">
        <div className={`mx-auto px-4 py-6 ${maxWidth}`}>
          {renderSection()}
        </div>
      </div>

      <div className="p-3 bg-[#16213e] border-t border-[#2a2a4a] flex items-center gap-4 text-xs text-[#6b7280] shrink-0">
        <span>SKILL: {state.character.skill}</span>
        <span>STAM: {state.character.stamina}/{state.character.maxStamina}</span>
        <span>LUCK: {state.character.luck}/{state.character.maxLuck}</span>
        <span>💰 {state.character.gold}</span>
        <span className="ml-auto">Passo {state.history.length}</span>
      </div>
    </div>
  );
}

function evaluateConditions(conditions: import('../../types/story').ConditionData[], state: PreviewState): boolean {
  if (!conditions || conditions.length === 0) return true;
  return conditions.every((cond) => {
    const char = state.character;
    let actual: number | boolean = 0;
    switch (cond.type) {
      case 'hasItem': actual = char.inventory.includes(cond.key); break;
      case 'hasFlag': actual = Boolean(char.flags[cond.key]); break;
      case 'skill': actual = char.skill; break;
      case 'stamina': actual = char.stamina; break;
      case 'luck': actual = char.luck; break;
      case 'gold': actual = char.gold; break;
      case 'counter': actual = char.counters[cond.key] ?? 0; break;
      default: return true;
    }
    const val = typeof cond.value === 'string' ? cond.value : cond.value;
    switch (cond.op) {
      case '==': return actual == val;
      case '!=': return actual != val;
      case '>=': return Number(actual) >= Number(val);
      case '<=': return Number(actual) <= Number(val);
      case '>': return Number(actual) > Number(val);
      case '<': return Number(actual) < Number(val);
      default: return true;
    }
  });
}

function evalOnEnter(section: SectionData | undefined, char: PreviewState['character']) {
  if (!section) return;
  const oe = section.onEnter;
  if (!oe) return;
  if (oe.addItems) char.inventory = [...new Set([...char.inventory, ...oe.addItems])];
  if (oe.removeItems) char.inventory = char.inventory.filter((i) => !(oe.removeItems as string[]).includes(i));
  if (oe.setFlags) Object.assign(char.flags, oe.setFlags);
  if (oe.modifyGold) char.gold += oe.modifyGold;
  if (oe.modifyStamina) char.stamina = Math.min(char.maxStamina, Math.max(0, char.stamina + oe.modifyStamina));
  if (oe.modifyLuck) char.luck = Math.min(char.maxLuck, Math.max(0, char.luck + oe.modifyLuck));
}

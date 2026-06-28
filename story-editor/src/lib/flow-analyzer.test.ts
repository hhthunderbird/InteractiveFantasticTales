import { describe, it, expect } from 'vitest';
import { analyzeFlow, getFlowStats } from './flow-analyzer';
import { createEmptyStory } from './json-handler';
import type { StoryData } from '../types/story';

function makeStory(overrides?: Partial<StoryData>): StoryData {
  const base = createEmptyStory();
  base.metadata.startSection = 1;
  return { ...base, ...overrides } as StoryData;
}

function n(id: number, text: string, choices?: { text: string; target: number; conditions?: any[] }[]) {
  return { id, type: 'narrative' as const, text, choices: choices?.map((c) => ({ text: c.text, targetSection: c.target, conditions: c.conditions ?? [] })) ?? [] };
}

function e(id: number, type: 'victory' | 'defeat' | 'neutral') {
  return { id, type: 'ending' as const, text: 'Fim.', ending: { type } };
}

function c(id: number, text: string, skill: number, stamina: number, victory: number, defeat: number, flee = 0) {
  return { id, type: 'combat' as const, text, combat: { enemyName: 'Inimigo', enemySkill: skill, enemyStamina: stamina, victoryTarget: victory, defeatTarget: defeat, fleeTarget: flee, allowFlee: flee > 0, lootOnVictory: [] as string[] } };
}

function t(id: number, text: string, attr: 'skill' | 'luck', diff: number, success: number, fail: number) {
  return { id, type: 'test' as const, text, test: { attribute: attr, difficulty: diff, successTarget: success, failTarget: fail } };
}

function ig(id: number, text: string, item: string, hasTarget: number, noTarget: number) {
  return { id, type: 'itemGate' as const, text, itemGate: { item, hasItemTarget: hasTarget, noItemTarget: noTarget } };
}

function r(id: number, text: string, outcomes: { targetSection: number; weight: number }[]) {
  return { id, type: 'random' as const, text, random: { outcomes } };
}

describe('FlowAnalyzer — Expanded Suite', () => {
  describe('basic validation', () => {
    it('valid minimal story (1 narrative → victory ending)', () => {
      const issues = analyzeFlow(makeStory({ sections: { 1: n(1, 'Oi', [{ text: 'Ir', target: 2 }]), 2: e(2, 'victory') } }));
      expect(issues.filter((i) => i.severity === 'error')).toHaveLength(0);
    });

    it('empty story with only start section → dead end + no victory', () => {
      const issues = analyzeFlow(makeStory({ sections: { 1: n(1, '', []) } }));
      expect(issues.find((i) => i.code === 'DEAD_END')).toBeDefined();
      expect(issues.find((i) => i.code === 'NO_VICTORY')).toBeDefined();
    });

    it('story with only ending section as start', () => {
      const issues = analyzeFlow(makeStory({
        metadata: { ...createEmptyStory().metadata, startSection: 99 },
        sections: { 99: e(99, 'victory') },
      }));
      expect(issues.filter((i) => i.severity === 'error')).toHaveLength(0);
    });
  });

  describe('combat edge cases', () => {
    it('flee target NOT included when allowFlee=false', () => {
      const s = makeStory({ sections: { 1: c(1, 'Luta', 5, 6, 2, 3, 0), 2: e(2, 'victory'), 3: e(3, 'defeat') } });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'DANGLING_REF' && i.message.includes('fleeTarget'))).toBeUndefined();
    });

    it('combat with all targets = 0 → all dangling refs', () => {
      const s = makeStory({ sections: { 1: c(1, 'Luta', 5, 6, 0, 0, 0) } });
      const issues = analyzeFlow(s);
      const danglings = issues.filter((i) => i.code === 'DANGLING_REF');
      expect(danglings.length).toBeGreaterThanOrEqual(2);
    });

    it('combat victory loot on enter items used', () => {
      const s = makeStory({
        sections: {
          1: c(1, 'Luta', 5, 6, 2, 3, 0),
          2: { id: 2, type: 'narrative' as const, text: '', choices: [], onEnter: { addItems: ['espada'] } },
          3: e(3, 'defeat'),
          4: ig(4, 'Porta', 'espada', 5, 6),
          5: e(5, 'victory'),
          6: e(6, 'defeat'),
        },
      });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'UNUSED_ITEM')).toBeUndefined();
    });
  });

  describe('random outcomes', () => {
    it('random with valid outcomes', () => {
      const s = makeStory({ sections: { 1: r(1, 'Sorte', [{ targetSection: 2, weight: 0.5 }, { targetSection: 3, weight: 0.5 }]), 2: e(2, 'victory'), 3: e(3, 'defeat') } });
      const issues = analyzeFlow(s);
      expect(issues.filter((i) => i.severity === 'error')).toHaveLength(0);
    });

    it('random with zero outcomes → error', () => {
      const issues = analyzeFlow(makeStory({ sections: { 1: r(1, '', []) } }));
      expect(issues.find((i) => i.code === 'RANDOM_NO_OUTCOMES')).toBeDefined();
    });

    it('random with dangling outcome target', () => {
      const s = makeStory({ sections: { 1: r(1, 'Sorte', [{ targetSection: 999, weight: 1 }]) } });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'DANGLING_REF' && i.message.includes('random'))).toBeDefined();
    });
  });

  describe('item gates', () => {
    it('gate without source', () => {
      const s = makeStory({ sections: { 1: n(1, 'Porta', [{ text: 'Tentar', target: 2 }]), 2: ig(2, 'Verificando', 'chave_ouro', 3, 4), 3: e(3, 'victory'), 4: e(4, 'defeat') } });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'GATE_WITHOUT_SOURCE')).toBeDefined();
    });

    it('gate with source (from starting items)', () => {
      const s = makeStory({
        characterCreation: { attributes: {}, startingGold: 0, startingItems: ['chave_ouro'], startingProvisions: 0 },
        sections: { 1: n(1, 'Porta', [{ text: 'Abrir', target: 2 }]), 2: ig(2, 'Verificando', 'chave_ouro', 3, 4), 3: e(3, 'victory'), 4: e(4, 'defeat') },
      });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'GATE_WITHOUT_SOURCE')).toBeUndefined();
    });

    it('gate with source from combat loot', () => {
      const s = makeStory({
        sections: {
          1: { id: 1, type: 'combat' as const, text: '', combat: { enemyName: 'Goblin', enemySkill: 5, enemyStamina: 6, victoryTarget: 2, defeatTarget: 99, fleeTarget: 0, allowFlee: false, lootOnVictory: ['chave'] } },
          2: n(2, 'Achou chave', [{ text: 'Usar', target: 3 }]),
          3: ig(3, 'Porta trancada', 'chave', 4, 5),
          4: e(4, 'victory'),
          5: e(5, 'defeat'),
          99: e(99, 'defeat'),
        },
      });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'GATE_WITHOUT_SOURCE')).toBeUndefined();
    });
  });

  describe('flags', () => {
    it('flag set but never read', () => {
      const s = makeStory({ sections: { 1: { id: 1, type: 'narrative' as const, text: '', choices: [{ text: 'Ir', targetSection: 2, conditions: [] }], onEnter: { setFlags: { found_secret: true } } }, 2: e(2, 'victory') } });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'UNUSED_FLAG' && i.message.includes('found_secret'))).toBeDefined();
    });

    it('flag read but never set (and not in story.flags)', () => {
      const s = makeStory({
        flags: {},
        sections: { 1: n(1, 'Escolha', [{ text: 'Ir', target: 2, conditions: [{ type: 'hasFlag' as const, key: 'blessed', op: '==' as const, value: 1 }] }]), 2: e(2, 'victory') },
      });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'FLAG_NEVER_SET')).toBeDefined();
    });

    it('flag in story.flags is considered set', () => {
      const s = makeStory({
        flags: { blessed: { type: 'boolean', default: false } },
        sections: { 1: n(1, 'Escolha', [{ text: 'Ir', target: 2, conditions: [{ type: 'hasFlag' as const, key: 'blessed', op: '==' as const, value: 1 }] }]), 2: e(2, 'victory') },
      });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'FLAG_NEVER_SET')).toBeUndefined();
    });
  });

  describe('complex scenarios', () => {
    it('diamond: two paths converge', () => {
      const s = makeStory({ sections: { 1: n(1, 'Bifurcação', [{ text: 'Esq', target: 2 }, { text: 'Dir', target: 3 }]), 2: n(2, 'Esq', [{ text: 'Continuar', target: 4 }]), 3: n(3, 'Dir', [{ text: 'Continuar', target: 4 }]), 4: e(4, 'victory') } });
      const issues = analyzeFlow(s);
      expect(issues.filter((i) => i.severity === 'error')).toHaveLength(0);
      expect(issues.find((i) => i.code === 'DUPLICATE_CHOICE')).toBeUndefined();
    });

    it('cycle with test exit → no loop error', () => {
      const s = makeStory({ sections: { 1: n(1, 'Entrada', [{ text: 'Tentar', target: 2 }]), 2: t(2, 'Teste de força', 'skill', 10, 3, 1), 3: e(3, 'victory') } });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'INFINITE_LOOP')).toBeUndefined();
    });

    it('cycle with item gate exit → no loop error', () => {
      const s = makeStory({
        characterCreation: { attributes: {}, startingGold: 0, startingItems: ['chave'], startingProvisions: 0 },
        sections: { 1: n(1, 'Porta', [{ text: 'Tentar', target: 2 }]), 2: ig(2, 'Trancada', 'chave', 3, 1), 3: e(3, 'victory') },
      });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'INFINITE_LOOP')).toBeUndefined();
    });

    it('self-loop → infinite loop error', () => {
      const s = makeStory({ sections: { 1: n(1, 'Loop', [{ text: 'De novo', target: 1 }]) } });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'INFINITE_LOOP')).toBeDefined();
    });

    it('two disconnected component — one reachable, one orphan', () => {
      const s = makeStory({ sections: { 1: n(1, 'Início', [{ text: 'Ir', target: 2 }]), 2: e(2, 'victory'), 99: n(99, 'Órfã', [{ text: 'Voltar', target: 100 }]), 100: e(100, 'victory') } });
      const issues = analyzeFlow(s);
      expect(issues.find((i) => i.code === 'UNREACHABLE')).toBeDefined();
      expect(issues.find((i) => i.code === 'ORPHAN_NODE' && i.sectionId === 99)).toBeDefined();
      expect(issues.find((i) => i.code === 'DISCONNECTED_ISLANDS')).toBeDefined();
    });

    it('missing start section → handled gracefully', () => {
      const s = makeStory({
        metadata: { ...createEmptyStory().metadata, startSection: 1 },
        sections: { 2: n(2, 'Órfã', []), 3: e(3, 'victory') },
      });
      expect(() => analyzeFlow(s)).not.toThrow();
    });

    it('deeply nested: 50-section chain', () => {
      const sections: any = {};
      for (let i = 1; i < 50; i++) sections[i] = n(i, `Passo ${i}`, [{ text: 'Avançar', target: i + 1 }]);
      sections[50] = e(50, 'victory');
      const s = makeStory({ sections });
      const issues = analyzeFlow(s);
      expect(issues.filter((i) => i.severity === 'error')).toHaveLength(0);
    });

    it('very large story: 400 sections (FF standard)', () => {
      const sections: any = {};
      for (let i = 1; i < 400; i++) sections[i] = n(i, `S${i}`, [{ text: '→', target: i + 1 }]);
      sections[400] = e(400, 'victory');
      const s = makeStory({ sections });
      const start = performance.now();
      const issues = analyzeFlow(s);
      const elapsed = performance.now() - start;
      expect(issues.filter((i) => i.severity === 'error')).toHaveLength(0);
      expect(elapsed).toBeLessThan(1000);
    });
  });

  describe('stats', () => {
    it('correct stats for branching story', () => {
      const stats = getFlowStats(makeStory({
        sections: {
          1: n(1, 'Início', [{ text: 'A', target: 2 }, { text: 'B', target: 3 }, { text: 'C', target: 4 }]),
          2: e(2, 'victory'),
          3: e(3, 'defeat'),
          4: c(4, 'Luta', 5, 6, 5, 6, 0),
          5: e(5, 'victory'),
          6: e(6, 'defeat'),
        },
      }));
      expect(stats.totalSections).toBe(6);
      expect(stats.victoryEndings).toBe(2);
      expect(stats.defeatEndings).toBe(2);
      expect(stats.combatCount).toBe(1);
      expect(stats.totalChoices).toBe(3);
      expect(stats.maxDepth).toBe(3);
    });
  });
});

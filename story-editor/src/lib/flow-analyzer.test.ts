import { describe, it, expect } from 'vitest';
import { analyzeFlow, getFlowStats } from './flow-analyzer';
import { createEmptyStory } from './json-handler';
import type { StoryData } from '../types/story';

function makeStory(overrides?: Partial<StoryData>): StoryData {
  const base = createEmptyStory();
  base.metadata.startSection = 1;
  return { ...base, ...overrides };
}

function narrative(id: number, text: string, choices?: { text: string; target: number; conditions?: any[] }[]) {
  return {
    id,
    type: 'narrative' as const,
    text,
    choices: choices?.map((c) => ({ text: c.text, targetSection: c.target, conditions: c.conditions ?? [] })) ?? [],
  };
}

function ending(id: number, endingType: 'victory' | 'defeat' | 'neutral', text = 'Fim.') {
  return {
    id,
    type: 'ending' as const,
    text,
    ending: { type: endingType },
  };
}

function combat(id: number, text: string, enemy: { name: string; skill: number; stamina: number }, targets: { victory: number; defeat: number; flee?: number }) {
  return {
    id,
    type: 'combat' as const,
    text,
    combat: {
      enemyName: enemy.name,
      enemySkill: enemy.skill,
      enemyStamina: enemy.stamina,
      victoryTarget: targets.victory,
      defeatTarget: targets.defeat,
      fleeTarget: targets.flee ?? 0,
      allowFlee: targets.flee != null,
      lootOnVictory: [] as string[],
    },
  };
}

describe('FlowAnalyzer', () => {
  describe('analyzeFlow — healthy story', () => {
    it('should return no errors for a minimal valid story', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Você entra na caverna.', [
            { text: 'Esquerda', target: 2 },
            { text: 'Direita', target: 3 },
          ]),
          2: ending(2, 'victory', 'Tesouro!'),
          3: ending(3, 'defeat', 'Morte.'),
        },
      });

      const issues = analyzeFlow(story);
      const errors = issues.filter((i) => i.severity === 'error');
      expect(errors).toHaveLength(0);
    });
  });

  describe('detectDeadEnds', () => {
    it('should flag narrative section with no choices', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Você entra.', []),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'DEAD_END')).toBeDefined();
    });

    it('should NOT flag ending sections', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Ir', target: 2 }]),
          2: ending(2, 'victory'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'DEAD_END')).toBeUndefined();
    });
  });

  describe('detectOrphanNodes', () => {
    it('should flag sections that are never referenced', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Ir', target: 2 }]),
          2: ending(2, 'victory'),
          99: narrative(99, 'Órfã', [{ text: 'Ir', target: 1 }]),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'ORPHAN_NODE' && i.sectionId === 99)).toBeDefined();
    });

    it('should NOT flag the start section', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Ir', target: 2 }]),
          2: ending(2, 'victory'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'ORPHAN_NODE' && i.sectionId === 1)).toBeUndefined();
    });
  });

  describe('detectUnreachableNodes', () => {
    it('should flag sections unreachable from start', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Ir', target: 2 }]),
          2: ending(2, 'victory'),
          3: narrative(3, 'Só alcançável de 99'),
          99: narrative(99, 'Órfã', [{ text: 'Ir', target: 3 }]),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'UNREACHABLE' && i.sectionId === 3)).toBeDefined();
      expect(issues.find((i) => i.code === 'UNREACHABLE' && i.sectionId === 99)).toBeDefined();
    });
  });

  describe('detectNoVictoryPath', () => {
    it('should flag when no victory ending is reachable', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Ir', target: 2 }]),
          2: ending(2, 'defeat'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'NO_VICTORY')).toBeDefined();
    });

    it('should NOT flag when a victory ending exists', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [
            { text: 'Esquerda', target: 2 },
            { text: 'Direita', target: 3 },
          ]),
          2: ending(2, 'victory'),
          3: ending(3, 'defeat'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'NO_VICTORY')).toBeUndefined();
    });
  });

  describe('detectLoops', () => {
    it('should flag infinite loop without exit condition', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Entrar', target: 2 }]),
          2: narrative(2, 'Loop', [{ text: 'Voltar', target: 1 }]),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'INFINITE_LOOP')).toBeDefined();
    });

    it('should NOT flag loop with test exit', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Entrar', target: 2 }]),
          2: {
            id: 2, type: 'test' as const, text: 'Teste',
            test: { attribute: 'skill' as const, difficulty: 8, successTarget: 3, failTarget: 1 },
          },
          3: ending(3, 'victory'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'INFINITE_LOOP')).toBeUndefined();
    });
  });

  describe('detectUnusedItems', () => {
    it('should flag items given but never checked', () => {
      const story = makeStory({
        sections: {
          1: {
            id: 1, type: 'narrative' as const,
            text: 'Você encontra uma chave.',
            choices: [{ text: 'Pegar', targetSection: 2, conditions: [] }],
            onEnter: { addItems: ['chave_misteriosa'] },
          },
          2: ending(2, 'victory'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'UNUSED_ITEM')).toBeDefined();
    });
  });

  describe('detectGatesWithoutSource', () => {
    it('should flag item gates checking items never given', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Porta trancada.', [{ text: 'Usar chave', target: 2 }]),
          2: {
            id: 2, type: 'itemGate' as const, text: 'Verificando chave...',
            itemGate: { item: 'chave_de_ouro', hasItemTarget: 3, noItemTarget: 4 },
          },
          3: ending(3, 'victory'),
          4: ending(4, 'defeat'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'GATE_WITHOUT_SOURCE')).toBeDefined();
    });
  });

  describe('detectUnusedFlags', () => {
    it('should flag flags set but never read', () => {
      const story = makeStory({
        sections: {
          1: {
            id: 1, type: 'narrative' as const,
            text: 'Você encontra um mapa.',
            choices: [{ text: 'Seguir', targetSection: 2, conditions: [] }],
            onEnter: { setFlags: { found_map: true } },
          },
          2: ending(2, 'victory'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'UNUSED_FLAG')).toBeDefined();
    });

    it('should flag flags read but never set', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Escolha:', [
            { text: 'Ir', target: 2, conditions: [{ type: 'hasFlag', key: 'blessed', op: '==', value: 1 }] },
            { text: 'Voltar', target: 3 },
          ]),
          2: ending(2, 'victory'),
          3: ending(3, 'defeat'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'FLAG_NEVER_SET')).toBeDefined();
    });
  });

  describe('detectRandomWithoutOutcomes', () => {
    it('should flag random sections with no outcomes', () => {
      const story = makeStory({
        sections: {
          1: { id: 1, type: 'random' as const, text: '', random: { outcomes: [] } },
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'RANDOM_NO_OUTCOMES')).toBeDefined();
    });
  });

  describe('detectDuplicateChoices', () => {
    it('should flag two choices pointing to same section', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Escolha:', [
            { text: 'Opção A', target: 2 },
            { text: 'Opção B', target: 2 },
          ]),
          2: ending(2, 'victory'),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'DUPLICATE_CHOICE')).toBeDefined();
    });
  });

  describe('dangling references', () => {
    it('should flag choices pointing to non-existent sections', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [{ text: 'Ir para 404', target: 404 }]),
        },
      });

      const issues = analyzeFlow(story);
      expect(issues.find((i) => i.code === 'DANGLING_REF')).toBeDefined();
    });
  });

  describe('getFlowStats', () => {
    it('should return correct stats for a simple story', () => {
      const story = makeStory({
        sections: {
          1: narrative(1, 'Início', [
            { text: 'A', target: 2 },
            { text: 'B', target: 3 },
          ]),
          2: ending(2, 'victory'),
          3: ending(3, 'defeat'),
        },
      });

      const stats = getFlowStats(story);
      expect(stats.totalSections).toBe(3);
      expect(stats.narrativeCount).toBe(1);
      expect(stats.endingCount).toBe(2);
      expect(stats.victoryEndings).toBe(1);
      expect(stats.defeatEndings).toBe(1);
      expect(stats.maxDepth).toBe(2);
      expect(stats.totalChoices).toBe(2);
    });
  });
});

import { describe, it, expect } from 'vitest';
import { analyzeFlow, getFlowStats } from './flow-analyzer';
import { createEmptyStory } from './json-handler';
import type { StoryData } from '../types/story';

function makeStory(overrides?: Partial<StoryData>): StoryData {
  const base = createEmptyStory();
  base.metadata.startSection = 1;
  return { ...base, ...overrides };
}

describe('Exploratory Test Suite — Intentando Quebrar a Aplicação', () => {
  
  // 1. Caso de borda: startSection apontando para ID inexistente
  it('Deve lidar com startSection inexistente sem quebrar a execução', () => {
    const story = makeStory({
      metadata: {
        ...createEmptyStory().metadata,
        startSection: 9999, // Não existe
      },
      sections: {
        1: { id: 1, type: 'narrative', text: 'Início', choices: [] }
      }
    });

    const issues = analyzeFlow(story);
    expect(issues).toBeDefined();
    // Deve identificar a seção 1 como órfã e inalcançável
    expect(issues.some(i => i.code === 'UNREACHABLE')).toBe(true);
    expect(issues.some(i => i.code === 'ORPHAN_NODE')).toBe(true);

    // Estatísticas de fluxo também devem rodar sem crashar
    const stats = getFlowStats(story);
    expect(stats.maxDepth).toBe(0);
  });

  // 2. Caso de borda: Multiplos Loops sobrepostos e complexos
  it('Deve detectar loops sobrepostos complexos sem entrar em loop infinito no analisador', () => {
    const story = makeStory({
      sections: {
        1: { id: 1, type: 'narrative', text: 'Sec 1', choices: [{ text: 'Para 2', targetSection: 2 }] },
        2: { id: 2, type: 'narrative', text: 'Sec 2', choices: [{ text: 'Para 3', targetSection: 3 }, { text: 'Para 1', targetSection: 1 }] },
        3: { id: 3, type: 'narrative', text: 'Sec 3', choices: [{ text: 'Para 2', targetSection: 2 }, { text: 'Para 4', targetSection: 4 }] },
        4: { id: 4, type: 'ending', text: 'Fim Vitória', ending: { type: 'victory' } }
      }
    });

    const issues = analyzeFlow(story);
    expect(issues).toBeDefined();
    const loops = issues.filter(i => i.code === 'INFINITE_LOOP');
    expect(loops.length).toBeGreaterThan(0);
  });

  // 3. Caso de borda: Estruturas de dados corrompidas ou tipos mistos
  it('Deve tratar dados de escolhas corrompidas (ex: targetSection nulo ou negativo)', () => {
    const story = makeStory({
      sections: {
        1: {
          id: 1,
          type: 'narrative',
          text: 'Início',
          choices: [
            { text: 'Target nulo', targetSection: null as any },
            { text: 'Target negativo', targetSection: -5 },
            { text: 'Target indefinido', targetSection: undefined as any }
          ]
        }
      }
    });

    const issues = analyzeFlow(story);
    // Deve registrar avisos de referência quebrada (DANGLING_REF) ou erros
    const dangling = issues.filter(i => i.code === 'DANGLING_REF');
    expect(dangling.length).toBe(3);
  });

  // 4. Caso de performance/hang: Grafo em árvore binária (Vulnerabilidade de DFS exponencial)
  // Nota: Vamos criar uma árvore moderada de profundidade 15 para testar sem travar a execução do teste real
  it('Verifica o tempo de processamento de caminhos exponenciais no getFlowStats', () => {
    const sections: Record<number, any> = {};
    const depth = 15; // 2^15 = 32,768 caminhos possíveis
    
    for (let i = 1; i <= depth; i++) {
      sections[i] = {
        id: i,
        type: 'narrative',
        text: `Nivel ${i}`,
        choices: [
          { text: 'Ramo A', targetSection: i + 1 },
          { text: 'Ramo B', targetSection: i + 1 }
        ]
      };
    }
    // O último nó é o final de vitória
    sections[depth + 1] = {
      id: depth + 1,
      type: 'ending',
      text: 'Vitória!',
      ending: { type: 'victory' }
    };

    const story = makeStory({ sections });
    
    const startTime = Date.now();
    const stats = getFlowStats(story);
    const duration = Date.now() - startTime;

    console.log(`DFS em árvore binária de profundidade ${depth} processou ${stats.totalSections} seções em ${duration}ms`);
    expect(stats.totalSections).toBe(depth + 1);
    expect(stats.maxDepth).toBe(depth + 1);
    // Garante que o teste não excedeu o tempo razoável
    expect(duration).toBeLessThan(1000); 
  });
});

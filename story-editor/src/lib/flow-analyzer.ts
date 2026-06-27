import type { StoryData, SectionData, AuditIssue, FlowStats } from '../types/story';

interface GraphNode {
  id: number;
  section: SectionData;
  children: number[];
  parents: number[];
}

function isValidTarget(target: number | null | undefined): target is number {
  return target != null && target > 0;
}

function buildGraph(story: StoryData): Map<number, GraphNode> {
  const graph = new Map<number, GraphNode>();

  for (const section of Object.values(story.sections)) {
    graph.set(section.id, {
      id: section.id,
      section,
      children: [],
      parents: [],
    });
  }

  const danglingRefs: AuditIssue[] = [];

  for (const section of Object.values(story.sections)) {
    const node = graph.get(section.id)!;

    if (section.choices) {
      for (const choice of section.choices) {
        if (!isValidTarget(choice.targetSection) || !graph.has(choice.targetSection)) {
          danglingRefs.push({
            severity: 'warning',
            code: 'DANGLING_REF',
            message: `Seção ${section.id} tem escolha "${choice.text.substring(0, 30)}" apontando para seção ${choice.targetSection} que não existe.`,
            sectionId: section.id,
            suggestion: 'Corrija o targetSection ou crie a seção de destino.',
          });
          continue;
        }
        node.children.push(choice.targetSection);
        graph.get(choice.targetSection)!.parents.push(section.id);
      }
    }

    if (section.combat) {
      const targets: [number, string][] = [
        [section.combat.victoryTarget, 'victoryTarget'],
        [section.combat.defeatTarget, 'defeatTarget'],
      ];
      if (section.combat.allowFlee) {
        targets.push([section.combat.fleeTarget, 'fleeTarget']);
      }
      for (const [target, label] of targets) {
        if (!isValidTarget(target) || !graph.has(target)) {
          danglingRefs.push({
            severity: 'warning',
            code: 'DANGLING_REF',
            message: `Seção ${section.id} (combate) tem ${label}=${target} que não existe.`,
            sectionId: section.id,
            suggestion: 'Corrija o target ou crie a seção de destino.',
          });
          continue;
        }
        node.children.push(target);
        graph.get(target)!.parents.push(section.id);
      }
    }

    if (section.test) {
      for (const [target, label] of [[section.test.successTarget, 'successTarget'], [section.test.failTarget, 'failTarget']] as [number, string][]) {
        if (!isValidTarget(target) || !graph.has(target)) {
          danglingRefs.push({
            severity: 'warning',
            code: 'DANGLING_REF',
            message: `Seção ${section.id} (teste) tem ${label}=${target} que não existe.`,
            sectionId: section.id,
            suggestion: 'Corrija o target ou crie a seção de destino.',
          });
          continue;
        }
        node.children.push(target);
        graph.get(target)!.parents.push(section.id);
      }
    }

    if (section.itemGate) {
      for (const [target, label] of [[section.itemGate.hasItemTarget, 'hasItemTarget'], [section.itemGate.noItemTarget, 'noItemTarget']] as [number, string][]) {
        if (!isValidTarget(target) || !graph.has(target)) {
          danglingRefs.push({
            severity: 'warning',
            code: 'DANGLING_REF',
            message: `Seção ${section.id} (itemGate) tem ${label}=${target} que não existe.`,
            sectionId: section.id,
            suggestion: 'Corrija o target ou crie a seção de destino.',
          });
          continue;
        }
        node.children.push(target);
        graph.get(target)!.parents.push(section.id);
      }
    }

    if (section.random) {
      for (const outcome of section.random.outcomes) {
        if (!isValidTarget(outcome.targetSection) || !graph.has(outcome.targetSection)) {
          danglingRefs.push({
            severity: 'warning',
            code: 'DANGLING_REF',
            message: `Seção ${section.id} (random) tem outcome.targetSection=${outcome.targetSection} que não existe.`,
            sectionId: section.id,
            suggestion: 'Corrija o target ou crie a seção de destino.',
          });
          continue;
        }
        node.children.push(outcome.targetSection);
        graph.get(outcome.targetSection)!.parents.push(section.id);
      }
    }
  }

  (graph as any).__danglingRefs = danglingRefs;
  return graph;
}

function detectDeadEnds(graph: Map<number, GraphNode>): AuditIssue[] {
  const issues: AuditIssue[] = [];
  for (const node of graph.values()) {
    if (node.section.type !== 'ending' && node.children.length === 0) {
      issues.push({
        severity: 'error',
        code: 'DEAD_END',
        message: `Seção ${node.id} é um beco sem saída — sem escolhas e não é um final.`,
        sectionId: node.id,
        suggestion: 'Adicione escolhas ou marque esta seção como tipo "ending".',
      });
    }
  }
  return issues;
}

function detectOrphanNodes(graph: Map<number, GraphNode>, startSection: number): AuditIssue[] {
  const issues: AuditIssue[] = [];
  for (const node of graph.values()) {
    if (node.id !== startSection && node.parents.length === 0) {
      issues.push({
        severity: 'warning',
        code: 'ORPHAN_NODE',
        message: `Seção ${node.id} é órfã — nunca é referenciada por nenhuma outra seção.`,
        sectionId: node.id,
        suggestion: 'Conecte esta seção a partir de uma escolha, combate, teste ou item gate.',
      });
    }
  }
  return issues;
}

function detectUnreachableNodes(graph: Map<number, GraphNode>, startSection: number): AuditIssue[] {
  const visited = new Set<number>();
  const queue = [startSection];

  while (queue.length > 0) {
    const id = queue.shift()!;
    if (visited.has(id)) continue;
    visited.add(id);
    const node = graph.get(id);
    if (node) {
      for (const child of node.children) {
        if (!visited.has(child)) queue.push(child);
      }
    }
  }

  const issues: AuditIssue[] = [];
  for (const node of graph.values()) {
    if (!visited.has(node.id)) {
      issues.push({
        severity: 'warning',
        code: 'UNREACHABLE',
        message: `Seção ${node.id} é inalcançável a partir da seção inicial ${startSection}.`,
        sectionId: node.id,
        suggestion: 'Verifique as conexões que levam a esta seção.',
      });
    }
  }
  return issues;
}

function detectNoVictoryPath(graph: Map<number, GraphNode>, startSection: number): AuditIssue[] {
  const visited = new Set<number>();
  const queue = [startSection];
  let hasVictory = false;

  while (queue.length > 0) {
    const id = queue.shift()!;
    if (visited.has(id)) continue;
    visited.add(id);
    const node = graph.get(id);
    if (node) {
      if (node.section.type === 'ending' && node.section.ending?.type === 'victory') {
        hasVictory = true;
        break;
      }
      for (const child of node.children) {
        if (!visited.has(child)) queue.push(child);
      }
    }
  }

  if (!hasVictory) {
    return [{
      severity: 'error',
      code: 'NO_VICTORY',
      message: 'Nenhum caminho leva a um final de vitória.',
      sectionId: startSection,
      suggestion: 'Garanta que pelo menos um final do tipo "victory" seja alcançável.',
    }];
  }
  return [];
}

function detectLoops(graph: Map<number, GraphNode>): AuditIssue[] {
  const issues: AuditIssue[] = [];
  const WHITE = 0, GRAY = 1, BLACK = 2;
  const color = new Map<number, number>();

  for (const id of graph.keys()) color.set(id, WHITE);

  function dfs(id: number, path: number[]): void {
    color.set(id, GRAY);
    path.push(id);
    const node = graph.get(id);
    if (node) {
      for (const child of node.children) {
        const c = color.get(child);
        if (c === GRAY) {
          const cycleStart = path.indexOf(child);
          const cycle = path.slice(cycleStart);
          const hasExit = cycle.some((cid) => {
            const sn = graph.get(cid);
            return sn?.section.type === 'test' || sn?.section.type === 'itemGate' || sn?.section.type === 'random';
          });
          if (!hasExit) {
            issues.push({
              severity: 'error',
              code: 'INFINITE_LOOP',
              message: `Loop infinito detectado: ${cycle.join(' → ')} → ${child}. Sem condição de saída.`,
              sectionId: child,
              suggestion: 'Adicione um teste, item gate ou condição aleatória para quebrar o ciclo.',
            });
          }
        } else if (c === WHITE) {
          dfs(child, [...path]);
        }
      }
    }
    path.pop();
    color.set(id, BLACK);
  }

  for (const id of graph.keys()) {
    if (color.get(id) === WHITE) dfs(id, []);
  }
  return issues;
}

function detectUnusedItems(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  const usedItems = new Set<string>();

  for (const section of Object.values(story.sections)) {
    if (section.itemGate) usedItems.add(section.itemGate.item);
    if (section.choices) {
      for (const choice of section.choices) {
        for (const cond of choice.conditions ?? []) {
          if (cond.type === 'hasItem') usedItems.add(cond.key);
        }
      }
    }
  }

  for (const section of Object.values(story.sections)) {
    if (section.onEnter?.addItems) {
      for (const item of section.onEnter.addItems) {
        if (!usedItems.has(item) && !issues.find((i) => i.sectionId === section.id && i.code === 'UNUSED_ITEM')) {
          issues.push({
            severity: 'warning',
            code: 'UNUSED_ITEM',
            message: `Item "${item}" é adicionado na seção ${section.id} mas nunca é verificado.`,
            sectionId: section.id,
          });
        }
      }
    }
  }

  return issues;
}

function detectEmptySections(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  for (const section of Object.values(story.sections)) {
    if (section.type === 'narrative' && (!section.text || section.text.trim() === '')) {
      issues.push({
        severity: 'warning',
        code: 'EMPTY_SECTION',
        message: `Seção ${section.id} é narrativa mas não tem texto.`,
        sectionId: section.id,
      });
    }
    if (section.type === 'combat' && (!section.text || section.text.trim() === '')) {
      issues.push({
        severity: 'info',
        code: 'EMPTY_COMBAT_TEXT',
        message: `Seção ${section.id} (combate) não tem texto introdutório. Considere adicionar uma descrição.`,
        sectionId: section.id,
      });
    }
  }
  return issues;
}

function detectUnusedFlags(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  const readFlags = new Set<string>();
  const writtenFlags = new Set<string>();

  for (const section of Object.values(story.sections)) {
    if (section.choices) {
      for (const choice of section.choices) {
        for (const cond of choice.conditions ?? []) {
          if (cond.type === 'hasFlag') readFlags.add(cond.key);
        }
      }
    }
    if (section.onEnter?.setFlags) {
      for (const key of Object.keys(section.onEnter.setFlags)) {
        writtenFlags.add(key);
      }
    }
  }

  for (const flag of writtenFlags) {
    if (!readFlags.has(flag)) {
      issues.push({
        severity: 'warning',
        code: 'UNUSED_FLAG',
        message: `Flag "${flag}" é definida mas nunca é lida em nenhuma condição.`,
        sectionId: 0,
        suggestion: 'Use esta flag em uma condição hasFlag ou remova a definição.',
      });
    }
  }

  for (const flag of readFlags) {
    if (!writtenFlags.has(flag) && !story.flags?.[flag]) {
      issues.push({
        severity: 'warning',
        code: 'FLAG_NEVER_SET',
        message: `Flag "${flag}" é verificada em condições mas nunca é definida via onEnter.setFlags.`,
        sectionId: 0,
        suggestion: 'Defina esta flag em um onEnter de alguma seção ou adicione-a em characterCreation.flags.',
      });
    }
  }

  return issues;
}

function detectGatesWithoutSource(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  const itemsGiven = new Set<string>(story.characterCreation.startingItems);

  for (const section of Object.values(story.sections)) {
    if (section.onEnter?.addItems) {
      for (const item of section.onEnter.addItems) itemsGiven.add(item);
    }
    if (section.combat?.lootOnVictory) {
      for (const item of section.combat.lootOnVictory) itemsGiven.add(item);
    }
  }

  for (const section of Object.values(story.sections)) {
    if (section.itemGate && !itemsGiven.has(section.itemGate.item)) {
      issues.push({
        severity: 'warning',
        code: 'GATE_WITHOUT_SOURCE',
        message: `Seção ${section.id} verifica item "${section.itemGate.item}" que nunca é dado ao jogador.`,
        sectionId: section.id,
        suggestion: 'Garanta que este item seja adicionado ao inventário em alguma seção via onEnter.addItems ou lootOnVictory.',
      });
    }
  }

  return issues;
}

function detectRandomWithoutOutcomes(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  for (const section of Object.values(story.sections)) {
    if (section.type === 'random' && (!section.random || section.random.outcomes.length === 0)) {
      issues.push({
        severity: 'error',
        code: 'RANDOM_NO_OUTCOMES',
        message: `Seção ${section.id} é do tipo "random" mas não tem outcomes definidos.`,
        sectionId: section.id,
        suggestion: 'Adicione pelo menos um outcome com targetSection e weight.',
      });
    }
  }
  return issues;
}

function detectMissingLootTargets(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  for (const section of Object.values(story.sections)) {
    if (section.combat?.lootOnVictory) {
      for (const item of section.combat.lootOnVictory) {
        if (!item || item.trim() === '') {
          issues.push({
            severity: 'info',
            code: 'EMPTY_LOOT',
            message: `Seção ${section.id} (combate) tem um item de loot vazio.`,
            sectionId: section.id,
          });
        }
      }
    }
  }
  return issues;
}

function detectDisconnectedIslands(graph: Map<number, GraphNode>, startSection: number): AuditIssue[] {
  const issues: AuditIssue[] = [];
  const visited = new Set<number>();
  const queue = [startSection];

  while (queue.length > 0) {
    const id = queue.shift()!;
    if (visited.has(id)) continue;
    visited.add(id);
    const node = graph.get(id);
    if (node) for (const child of node.children) if (!visited.has(child)) queue.push(child);
  }

  const connectedSize = visited.size;
  const totalSize = graph.size;
  if (connectedSize < totalSize) {
    issues.push({
      severity: 'warning',
      code: 'DISCONNECTED_ISLANDS',
      message: `${totalSize - connectedSize} seções (${((totalSize - connectedSize) / totalSize * 100).toFixed(0)}%) estão em ilhas desconectadas da seção inicial.`,
      sectionId: startSection,
      suggestion: 'Conecte estas seções ao fluxo principal ou revise as referências.',
    });
  }

  return issues;
}

function detectDuplicateChoices(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  for (const section of Object.values(story.sections)) {
    if (section.choices && section.choices.length >= 2) {
      const targets = section.choices.map((c) => c.targetSection);
      const seen = new Set<number>();
      for (const target of targets) {
        if (seen.has(target)) {
          issues.push({
            severity: 'info',
            code: 'DUPLICATE_CHOICE',
            message: `Seção ${section.id} tem múltiplas escolhas apontando para a seção ${target}.`,
            sectionId: section.id,
          });
        }
        seen.add(target);
      }
    }
  }
  return issues;
}

export function analyzeFlow(story: StoryData): AuditIssue[] {
  const graph = buildGraph(story);
  const startId = story.metadata.startSection;
  const danglingRefs: AuditIssue[] = (graph as any).__danglingRefs ?? [];

  return [
    ...danglingRefs,
    ...detectDeadEnds(graph),
    ...detectOrphanNodes(graph, startId),
    ...detectLoops(graph),
    ...detectUnreachableNodes(graph, startId),
    ...detectDisconnectedIslands(graph, startId),
    ...detectNoVictoryPath(graph, startId),
    ...detectUnusedItems(story),
    ...detectUnusedFlags(story),
    ...detectGatesWithoutSource(story),
    ...detectRandomWithoutOutcomes(story),
    ...detectMissingLootTargets(story),
    ...detectEmptySections(story),
    ...detectDuplicateChoices(story),
  ];
}

export function getFlowStats(story: StoryData): FlowStats {
  const graph = buildGraph(story);
  const sections = Object.values(story.sections);
  const endings = sections.filter((s) => s.type === 'ending');
  const victoryEndings = endings.filter((e) => e.ending?.type === 'victory');
  const defeatEndings = endings.filter((e) => e.ending?.type === 'defeat');
  const randomSections = sections.filter((s) => s.type === 'random');
  const itemGateSections = sections.filter((s) => s.type === 'itemGate');

  let maxDepth = 0;
  let totalPathCount = 0;
  function dfs(id: number, depth: number, visited: Set<number>) {
    if (visited.has(id)) return;
    visited.add(id);
    if (depth > maxDepth) maxDepth = depth;
    const node = graph.get(id);
    if (node) {
      if (node.section.type === 'ending') totalPathCount++;
      for (const child of node.children) {
        dfs(child, depth + 1, new Set(visited));
      }
    }
  }
  if (graph.has(story.metadata.startSection)) {
    dfs(story.metadata.startSection, 1, new Set());
  }

  const allReferenced = new Set<number>();
  for (const node of graph.values()) {
    for (const child of node.children) allReferenced.add(child);
  }
  const unreferencedCount = sections.filter((s) => s.id !== story.metadata.startSection && !allReferenced.has(s.id)).length;

  return {
    totalSections: sections.length,
    narrativeCount: sections.filter((s) => s.type === 'narrative').length,
    combatCount: sections.filter((s) => s.type === 'combat').length,
    testCount: sections.filter((s) => s.type === 'test').length,
    endingCount: endings.length,
    victoryEndings: victoryEndings.length,
    defeatEndings: defeatEndings.length,
    maxDepth,
    totalChoices: sections.reduce((sum, s) => sum + (s.choices?.length ?? 0), 0),
    randomCount: randomSections.length,
    itemGateCount: itemGateSections.length,
    unreferencedCount,
    totalItems: Object.keys(story.items ?? {}).length,
    totalFlags: Object.keys(story.flags ?? {}).length,
  };
}

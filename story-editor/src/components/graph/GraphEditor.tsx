import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  MarkerType,
  useReactFlow,
} from 'reactflow';
import type { Node, Edge, Connection } from 'reactflow';
import 'reactflow/dist/style.css';
import { useEditorStore } from '../../stores/editor-store';
import { useStyleStore } from '../../stores/style-store';
import { SECTION_TYPE_COLORS, SECTION_TYPE_LABELS } from '../../types/story';
import { GroupOverlay } from './GroupOverlay';
import { SectionNode } from './SectionNode';

const nodeTypes = { sectionNode: SectionNode };

type Adjacency = Map<number, number[]>;

function buildAdjacency(
  story: NonNullable<ReturnType<typeof useEditorStore.getState>['story']>,
): Adjacency {
  const adj: Adjacency = new Map();
  for (const section of Object.values(story.sections)) {
    const children: number[] = [];
    if (section.choices) section.choices.forEach((c) => {
      if (c.targetSection > 0 && story.sections[c.targetSection]) children.push(c.targetSection);
    });
    if (section.combat) {
      if (section.combat.victoryTarget > 0 && story.sections[section.combat.victoryTarget])
        children.push(section.combat.victoryTarget);
      if (section.combat.defeatTarget > 0 && story.sections[section.combat.defeatTarget])
        children.push(section.combat.defeatTarget);
      if (section.combat.allowFlee && section.combat.fleeTarget > 0 && story.sections[section.combat.fleeTarget])
        children.push(section.combat.fleeTarget);
    }
    if (section.test) {
      if (section.test.successTarget > 0 && story.sections[section.test.successTarget])
        children.push(section.test.successTarget);
      if (section.test.failTarget > 0 && story.sections[section.test.failTarget])
        children.push(section.test.failTarget);
    }
    if (section.itemGate) {
      if (section.itemGate.hasItemTarget > 0 && story.sections[section.itemGate.hasItemTarget])
        children.push(section.itemGate.hasItemTarget);
      if (section.itemGate.noItemTarget > 0 && story.sections[section.itemGate.noItemTarget])
        children.push(section.itemGate.noItemTarget);
    }
    if (section.random) {
      section.random.outcomes.forEach((o) => {
        if (o.targetSection > 0 && story.sections[o.targetSection])
          children.push(o.targetSection);
      });
    }
    adj.set(section.id, children);
  }
  return adj;
}

export function treeLayout(
  story: NonNullable<ReturnType<typeof useEditorStore.getState>['story']>,
): Map<number, { x: number; y: number }> {
  const adj = buildAdjacency(story);
  const positions = new Map<number, { x: number; y: number }>();
  const startId = story.metadata.startSection;

  const H_SPACING = 280;
  const V_SPACING = 140;

  const column: Map<number, number> = new Map();

  function dfs(id: number, depth: number, visited: Set<number>): number {
    if (visited.has(id)) return 0;
    visited.add(id);

    const children = (adj.get(id) ?? []).filter((c) => !visited.has(c));

    if (children.length === 0) {
      const col = column.get(depth) ?? 0;
      positions.set(id, { x: col * H_SPACING, y: depth * V_SPACING });
      column.set(depth, col + 1);
      return 1;
    }

    let totalLeafSpan = 0;
    const childColumns: number[] = [];
    for (const child of children) {
      const leafCount = dfs(child, depth + 1, new Set(visited));
      totalLeafSpan += leafCount;
      childColumns.push(leafCount);
    }

    let runningCol = 0;
    for (let i = 0; i < children.length; i++) {
      const childId = children[i];
      const childPos = positions.get(childId)!;
      const offset = runningCol + (childColumns[i] - 1) * 0.5;
      childPos.x = (column.get(depth + 1)! - childColumns[i] + offset) * H_SPACING;
      runningCol += childColumns[i];
    }

    const avgX = children.reduce((s, c) => s + positions.get(c)!.x, 0) / children.length;
    const col = Math.round(avgX / H_SPACING);
    positions.set(id, { x: col * H_SPACING, y: depth * V_SPACING });

    return totalLeafSpan || 1;
  }

  dfs(startId, 0, new Set());

  let freeRow = 2;
  for (const section of Object.values(story.sections)) {
    if (!positions.has(section.id)) {
      positions.set(section.id, { x: 0, y: freeRow * V_SPACING });
      freeRow++;
    }
  }

  return positions;
}

export function GraphEditor() {
  const story = useEditorStore((s) => s.story);
  const selectSection = useEditorStore((s) => s.selectSection);
  const selectedSectionId = useEditorStore((s) => s.selectedSectionId);
  const sectionCount = story ? Object.keys(story.sections).length : 0;
  const prevCount = useRef(sectionCount);
  const reactFlowInstance = useReactFlow();

  const { initialNodes, initialEdges } = useMemo(() => {
    if (!story) return { initialNodes: [], initialEdges: [] };

    const layout = treeLayout(story);
    const nodes: Node[] = [];
    const edges: Edge[] = [];

    const addEdge = (
      sourceId: number,
      target: number | undefined,
      label: string,
      color: string,
      key: string,
    ) => {
      if (target == null || target === 0 || !story.sections[target]) return;
      edges.push({
        id: `e${sourceId}-${target}-${key}`,
        source: `${sourceId}`,
        target: `${target}`,
        label,
        style: { stroke: color, strokeWidth: 2 },
        markerEnd: { type: MarkerType.ArrowClosed, color },
        labelStyle: { fill: '#a0a0b0', fontSize: 10 },
        labelBgStyle: { fill: '#1a1a2e' },
      });
    };

    for (const section of Object.values(story.sections)) {
      const isStart = section.id === story.metadata.startSection;
      const isSelected = section.id === selectedSectionId;
      const color = SECTION_TYPE_COLORS[section.type];
      const preview = section.text
        ? section.text.substring(0, 60) + (section.text.length > 60 ? '...' : '')
        : '(sem texto)';

      const pos = layout.get(section.id) ?? { x: 0, y: 0 };
      const style = useStyleStore.getState().getSectionStyle(section.id);
      const locked = useStyleStore.getState().isLocked(section.id);
      const effectiveColor = style.id !== '__default__' ? style.color : color;
      const effectiveBg = style.backgroundColor || (style.id !== '__default__' ? style.color + '20' : color + '20');

      nodes.push({
        id: `${section.id}`,
        type: 'sectionNode',
        position: pos,
        draggable: !locked,
        data: {
          id: section.id,
          type: section.type,
          label: SECTION_TYPE_LABELS[section.type],
          preview,
          color: effectiveColor,
          bgColor: effectiveBg,
          borderColor: style.borderColor !== 'transparent' ? style.borderColor : effectiveColor,
          borderStyle: style.borderStyle,
          borderWidth: style.borderWidth,
          isStart,
          isSelected,
          locked,
          styleId: style.id,
        },
      });

      if (section.choices) {
        section.choices.forEach((choice, idx) => {
          addEdge(section.id, choice.targetSection,
            choice.text.substring(0, 30) + (choice.text.length > 30 ? '...' : ''),
            '#3b82f6', `choice-${idx}`);
        });
      }

      if (section.combat) {
        addEdge(section.id, section.combat.victoryTarget, 'Vitória', '#10b981', 'victory');
        addEdge(section.id, section.combat.defeatTarget, 'Derrota', '#ef4444', 'defeat');
        if (section.combat.allowFlee) {
          addEdge(section.id, section.combat.fleeTarget, 'Fuga', '#f59e0b', 'flee');
        }
      }

      if (section.test) {
        addEdge(section.id, section.test.successTarget, 'Sucesso', '#10b981', 'success');
        addEdge(section.id, section.test.failTarget, 'Falha', '#ef4444', 'fail');
      }

      if (section.itemGate) {
        addEdge(section.id, section.itemGate.hasItemTarget, `Tem "${section.itemGate.item}"`, '#10b981', 'hasItem');
        addEdge(section.id, section.itemGate.noItemTarget, `Não tem "${section.itemGate.item}"`, '#ef4444', 'noItem');
      }

      if (section.random) {
        section.random.outcomes.forEach((outcome, idx) => {
          addEdge(section.id, outcome.targetSection,
            `${(outcome.weight * 100).toFixed(0)}%`, '#8b5cf6', `random-${idx}`);
        });
      }
    }

    return { initialNodes: nodes, initialEdges: edges };
  }, [story, selectedSectionId]);

  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);

  useEffect(() => {
    setNodes(initialNodes);
    setEdges(initialEdges);

    if (sectionCount !== prevCount.current && sectionCount > 1) {
      prevCount.current = sectionCount;
      setTimeout(() => reactFlowInstance.fitView({ padding: 0.3, duration: 300 }), 50);
    }
  }, [initialNodes, initialEdges, setNodes, setEdges, sectionCount, reactFlowInstance]);

  const onConnect = useCallback(
    (params: Connection) => {
      if (!params.source || !params.target) return;
      const sourceId = Number(params.source);
      const targetId = Number(params.target);

      const currentStory = useEditorStore.getState().story;
      if (!currentStory) return;

      const sourceSection = currentStory.sections[sourceId];
      if (!sourceSection) return;

      if (sourceSection.type === 'combat' || sourceSection.type === 'test' ||
          sourceSection.type === 'itemGate' || sourceSection.type === 'random' ||
          sourceSection.type === 'ending') {
        return;
      }

      const existingChoices = sourceSection.choices ?? [];
      const alreadyConnected = existingChoices.some((c) => c.targetSection === targetId);

      if (alreadyConnected) return;

      useEditorStore.getState().updateSection(sourceId, {
        choices: [...existingChoices, {
          id: crypto.randomUUID().slice(0, 8),
          text: `Ir para seção ${targetId}`,
          targetSection: targetId,
          conditions: [],
        }],
      });
    },
    [],
  );

  const onEdgesDelete = useCallback(
    (deletedEdges: Edge[]) => {
      const currentStory = useEditorStore.getState().story;
      if (!currentStory) return;

      for (const edge of deletedEdges) {
        const sourceId = Number(edge.source);
        const targetId = Number(edge.target);
        const section = currentStory.sections[sourceId];
        if (!section?.choices) continue;

        const nodeIds = new Set(edge.id.split('-').filter(Boolean));
        if (nodeIds.has('choice')) {
          const choiceIdx = parseInt(edge.id.split('-').pop() ?? '0');
          const filtered = section.choices.filter((_, i) => i !== choiceIdx || section.choices![i]?.targetSection !== targetId);
          if (filtered.length !== section.choices.length) {
            useEditorStore.getState().updateSection(sourceId, { choices: filtered });
          }
        }
      }
    },
    [],
  );

  const onNodeClick = useCallback(
    (_: React.MouseEvent, node: Node) => selectSection(Number(node.id)),
    [selectSection],
  );

  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; nodeId?: number } | null>(null);

  const getNextSectionId = () => {
    const currentStory = useEditorStore.getState().story;
    if (!currentStory) return 1;
    const ids = Object.keys(currentStory.sections).map(Number);
    return ids.length > 0 ? Math.max(...ids) + 1 : 1;
  };

  const addNarrativeSection = (atPosition?: { x: number; y: number }) => {
    const id = getNextSectionId();
    const section = {
      id, type: 'narrative' as const, text: '',
      choices: [],
    };
    useEditorStore.getState().addSection(section);
    if (atPosition) setContextMenu(null);
  };

  const duplicateSection = (sectionId: number) => {
    const currentStory = useEditorStore.getState().story;
    if (!currentStory) return;
    const original = currentStory.sections[sectionId];
    if (!original) return;
    const newId = getNextSectionId();
    useEditorStore.getState().addSection({ ...original, id: newId });
    setContextMenu(null);
  };

  const onPaneContextMenu = useCallback(
    (e: React.MouseEvent | MouseEvent) => {
      e.preventDefault();
      setContextMenu({ x: (e as React.MouseEvent).clientX ?? (e as MouseEvent).clientX, y: (e as React.MouseEvent).clientY ?? (e as MouseEvent).clientY });
    },
    [],
  );

  const onNodeContextMenu = useCallback(
    (e: React.MouseEvent, node: Node) => {
      e.preventDefault();
      setContextMenu({ x: e.clientX, y: e.clientY, nodeId: Number(node.id) });
    },
    [],
  );

  const onPaneClick = useCallback(() => setContextMenu(null), []);

  const onDoubleClickPane = useCallback(
    (_: React.MouseEvent) => {
      addNarrativeSection();
    },
    [],
  );

  const onKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'n' && !e.ctrlKey && !e.metaKey && !e.altKey) {
      e.preventDefault();
      addNarrativeSection();
      return;
    }
    if ((e.key === 'Delete' || e.key === 'Backspace') && selectedSectionId !== null) {
      if (selectedSectionId === story?.metadata.startSection) return;
      if (confirm(`Excluir seção #${selectedSectionId}?`)) {
        useEditorStore.getState().removeSection(selectedSectionId);
      }
    }
    if ((e.key === 's' || e.key === 'S') && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      useEditorStore.getState().triggerLayout();
    }
  }, [selectedSectionId, story?.metadata.startSection]);

  return (
    <div className="flex-1 bg-[#1a1a2e] relative" onKeyDown={onKeyDown} tabIndex={0}>
      {story && Object.keys(story.sections).length === 1 && (
        <div className="absolute top-4 left-1/2 -translate-x-1/2 z-10 bg-[#0f3460] border border-[#3b82f6] rounded-lg px-5 py-3 text-sm text-white shadow-lg pointer-events-none max-w-sm text-center">
          <p className="font-semibold mb-1">👋 Bem-vindo ao Editor!</p>
          <p className="text-xs text-[#a0a0b0] leading-relaxed">
            Clique em <span className="text-[#e94560] font-bold">+ Nova Seção</span> ou pressione <span className="text-[#e94560] font-bold">N</span> para adicionar seções.
            Clique com <span className="text-[#3b82f6]">botão direito</span> no grafo para mais opções.
          </p>
        </div>
      )}

      {contextMenu && (
        <div
          className="absolute z-50 bg-[#16213e] border border-[#2a2a4a] rounded-lg shadow-xl py-1 min-w-[180px]"
          style={{ left: contextMenu.x, top: contextMenu.y }}
        >
          {contextMenu.nodeId ? (
            <>
              <button onClick={() => { selectSection(contextMenu.nodeId!); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">✏️ Editar</button>
              <button onClick={() => { duplicateSection(contextMenu.nodeId!); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">📋 Duplicar</button>
              <button onClick={() => { useStyleStore.getState().toggleLock(contextMenu.nodeId!); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">
                {useStyleStore.getState().isLocked(contextMenu.nodeId!) ? '🔓 Desbloquear' : '🔒 Bloquear'}
              </button>
              <div className="border-t border-[#2a2a4a] my-1" />
              <div className="px-3 py-0.5 text-[10px] text-[#6b7280] uppercase">Aplicar Estilo</div>
              {useStyleStore.getState().styles.filter((s) => s.id !== '__default__').slice(0, 5).map((style) => (
                <button key={style.id} onClick={() => { useStyleStore.getState().applyStyle(style.id, [contextMenu.nodeId!]); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">
                  <span className="inline-block w-2.5 h-2.5 rounded-sm mr-2 border" style={{ background: style.backgroundColor || style.color + '30', borderColor: style.color }} />
                  {style.name}
                </button>
              ))}
              <button onClick={() => { useStyleStore.getState().removeSectionStyle(contextMenu.nodeId!); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#6b7280] hover:bg-[#0f3460]">Limpar estilo</button>
              {contextMenu.nodeId !== story?.metadata.startSection && (
                <>
                  <div className="border-t border-[#2a2a4a] my-1" />
                  <button onClick={() => { if (confirm('Excluir?')) { useEditorStore.getState().removeSection(contextMenu.nodeId!); setContextMenu(null); } }} className="block w-full text-left px-3 py-1.5 text-xs text-[#ef4444] hover:bg-[#0f3460]">🗑️ Excluir</button>
                </>
              )}
            </>
          ) : (
            <>
              <div className="px-3 py-1 text-[10px] text-[#6b7280] uppercase">Nova Seção</div>
              <button onClick={() => addNarrativeSection()} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">📖 Narrativa</button>
              <button onClick={() => { const id = getNextSectionId(); useEditorStore.getState().addSection({ id, type: 'combat' as const, text: '', combat: { enemyName: 'Inimigo', enemySkill: 5, enemyStamina: 6, victoryTarget: 0, defeatTarget: 0, fleeTarget: 0, allowFlee: true, lootOnVictory: [] } }); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">⚔️ Combate</button>
              <button onClick={() => { const id = getNextSectionId(); useEditorStore.getState().addSection({ id, type: 'test' as const, text: '', test: { attribute: 'skill', difficulty: 8, successTarget: 0, failTarget: 0 } }); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">🎲 Teste</button>
              <button onClick={() => { const id = getNextSectionId(); useEditorStore.getState().addSection({ id, type: 'itemGate' as const, text: '', itemGate: { item: '', hasItemTarget: 0, noItemTarget: 0 } }); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">🔑 Item Gate</button>
              <button onClick={() => { const id = getNextSectionId(); useEditorStore.getState().addSection({ id, type: 'random' as const, text: '', random: { outcomes: [] } }); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">🔀 Aleatório</button>
              <button onClick={() => { const id = getNextSectionId(); useEditorStore.getState().addSection({ id, type: 'ending' as const, text: '', ending: { type: 'neutral' } }); setContextMenu(null); }} className="block w-full text-left px-3 py-1.5 text-xs text-[#e0e0e0] hover:bg-[#0f3460]">🏁 Final</button>
            </>
          )}
        </div>
      )}

      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onNodeClick={onNodeClick}
        onNodeContextMenu={onNodeContextMenu}
        onPaneContextMenu={onPaneContextMenu}
        onPaneClick={onPaneClick}
        onDoubleClick={onDoubleClickPane}
        onEdgesDelete={onEdgesDelete}
        deleteKeyCode={null}
        nodeTypes={nodeTypes}
        fitView
        fitViewOptions={{ padding: 0.3 }}
        attributionPosition="bottom-left"
        defaultEdgeOptions={{
          style: { stroke: '#3b82f6', strokeWidth: 2 },
          markerEnd: { type: MarkerType.ArrowClosed, color: '#3b82f6' },
        }}
      >
        <Background color="#2a2a4a" gap={20} />
        <Controls className="bg-[#16213e] border-[#2a2a4a]" />
        <MiniMap
          style={{ background: '#16213e' }}
          maskColor="rgba(0,0,0,0.6)"
          nodeColor={(n) => (n.data as { color: string })?.color ?? '#6b7280'}
        />
        <div className="absolute bottom-4 left-4 z-10">
          <button
            onClick={() => reactFlowInstance.fitView({ padding: 0.3, duration: 300 })}
            className="px-3 py-1.5 text-xs bg-[#16213e] border border-[#2a2a4a] text-[#a0a0b0] rounded-md hover:bg-[#0f3460] hover:text-white transition-colors"
          >
            🔍 Centralizar
          </button>
        </div>
        <GroupOverlay />
      </ReactFlow>
    </div>
  );
}

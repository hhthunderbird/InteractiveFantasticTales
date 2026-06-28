import { useCallback, useEffect, useMemo, useRef } from 'react';
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
import { SECTION_TYPE_COLORS, SECTION_TYPE_LABELS } from '../../types/story';
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

      nodes.push({
        id: `${section.id}`,
        type: 'sectionNode',
        position: pos,
        data: {
          id: section.id,
          type: section.type,
          label: SECTION_TYPE_LABELS[section.type],
          preview,
          color,
          isStart,
          isSelected,
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

  const onConnect = useCallback((_params: Connection) => {}, []);

  const onNodeClick = useCallback(
    (_: React.MouseEvent, node: Node) => selectSection(Number(node.id)),
    [selectSection],
  );

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if ((e.key === 'Delete' || e.key === 'Backspace') && selectedSectionId !== null) {
      if (selectedSectionId === story?.metadata.startSection) return;
      if (confirm(`Excluir seção #${selectedSectionId}?`)) {
        useEditorStore.getState().removeSection(selectedSectionId);
      }
    }
  }, [selectedSectionId, story?.metadata.startSection]);

  return (
    <div className="flex-1 bg-[#1a1a2e] relative" onKeyDown={handleKeyDown} tabIndex={0}>
      {story && Object.keys(story.sections).length === 1 && (
        <div className="absolute top-4 left-1/2 -translate-x-1/2 z-10 bg-[#0f3460] border border-[#3b82f6] rounded-lg px-5 py-3 text-sm text-white shadow-lg pointer-events-none max-w-sm text-center">
          <p className="font-semibold mb-1">👋 Bem-vindo ao Editor!</p>
          <p className="text-xs text-[#a0a0b0] leading-relaxed">
            Clique em <span className="text-[#e94560] font-bold">+ Nova Seção</span> para adicionar trechos da história.
            Depois, clique nos nós para editar texto e escolhas no painel direito.
            Conecte as seções definindo os <span className="text-[#3b82f6]">números de destino</span> nas escolhas.
          </p>
        </div>
      )}

      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onNodeClick={onNodeClick}
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
      </ReactFlow>
    </div>
  );
}

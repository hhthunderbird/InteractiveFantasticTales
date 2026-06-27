import { useCallback, useEffect, useMemo } from 'react';
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  useNodesState,
  useEdgesState,
  MarkerType,
} from 'reactflow';
import type { Node, Edge, Connection } from 'reactflow';
import 'reactflow/dist/style.css';
import { useEditorStore } from '../../stores/editor-store';
import { SECTION_TYPE_COLORS, SECTION_TYPE_LABELS } from '../../types/story';
import { SectionNode } from './SectionNode';

const nodeTypes = { sectionNode: SectionNode };

export function GraphEditor() {
  const story = useEditorStore((s) => s.story);
  const selectSection = useEditorStore((s) => s.selectSection);
  const selectedSectionId = useEditorStore((s) => s.selectedSectionId);

  const { initialNodes, initialEdges } = useMemo(() => {
    if (!story) return { initialNodes: [], initialEdges: [] };

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

      nodes.push({
        id: `${section.id}`,
        type: 'sectionNode',
        position: { x: 0, y: 0 },
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
  }, [initialNodes, initialEdges, setNodes, setEdges]);

  const onConnect = useCallback(
    (_params: Connection) => {
    },
    [],
  );

  const onNodeClick = useCallback(
    (_: React.MouseEvent, node: Node) => {
      selectSection(Number(node.id));
    },
    [selectSection],
  );

  return (
    <div className="flex-1 bg-[#1a1a2e]">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onConnect={onConnect}
        onNodeClick={onNodeClick}
        nodeTypes={nodeTypes}
        fitView
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
      </ReactFlow>
    </div>
  );
}

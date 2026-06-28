import { useNodes, useViewport } from 'reactflow';
import { useStyleStore } from '../../stores/style-store';

export function GroupOverlay() {
  const nodes = useNodes();
  const { x: vpX, y: vpY, zoom } = useViewport();
  const groups = useStyleStore((s) => s.groups);

  const visibleGroups = groups.filter((g) => g.visible && g.nodeIds.length > 0);

  if (visibleGroups.length === 0) return null;

  return (
    <svg className="absolute inset-0 pointer-events-none z-5" width="100%" height="100%">
      {visibleGroups.map((group) => {
        const groupNodes = nodes.filter((n) => group.nodeIds.includes(Number(n.id)));
        if (groupNodes.length === 0) return null;

        const xs = groupNodes.map((n) => n.position.x);
        const ys = groupNodes.map((n) => n.position.y);
        const minX = Math.min(...xs) - 20;
        const minY = Math.min(...ys) - 40;
        const maxX = Math.max(...xs) + 220;
        const maxY = Math.max(...ys) + 100;
        const width = maxX - minX;
        const height = maxY - minY;

        const screenX = (minX + vpX) * zoom;
        const screenY = (minY + vpY) * zoom;
        const screenW = width * zoom;
        const screenH = height * zoom;

        return (
          <g key={group.id}>
            <rect
              x={screenX}
              y={screenY}
              width={screenW}
              height={screenH}
              fill={group.color + '10'}
              stroke={group.color}
              strokeWidth={group.borderWidth}
              strokeDasharray={group.borderStyle === 'dashed' ? '8,4' : group.borderStyle === 'dotted' ? '2,4' : 'none'}
              rx={8}
              ry={8}
            />
            <text
              x={screenX + 8}
              y={screenY - 8}
              fill={group.color}
              fontSize={11 * zoom}
              fontWeight="bold"
            >
              {group.title}
            </text>
          </g>
        );
      })}
    </svg>
  );
}

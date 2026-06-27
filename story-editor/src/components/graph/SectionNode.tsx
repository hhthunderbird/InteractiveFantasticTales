import { Handle, Position } from 'reactflow';
import { SECTION_TYPE_ICONS, type SectionType } from '../../types/story';

interface SectionNodeData {
  id: number;
  type: SectionType;
  label: string;
  preview: string;
  color: string;
  isStart: boolean;
  isSelected: boolean;
}

export function SectionNode({ data }: { data: SectionNodeData }) {
  const icon = SECTION_TYPE_ICONS[data.type];

  return (
    <>
      <Handle type="target" position={Position.Top} className="!bg-[#a0a0b0]" />
      <div
        className={`px-3 py-2 rounded-lg border-2 min-w-[180px] max-w-[220px] cursor-pointer transition-all ${
          data.isSelected ? 'border-white shadow-lg shadow-white/20 scale-105' : 'border-transparent hover:border-white/30'
        }`}
        style={{ background: data.color + '20', borderColor: data.isSelected ? '#fff' : data.color }}
      >
        <div className="flex items-center gap-1.5 mb-1">
          <span className="text-xs">{icon}</span>
          <span className="text-xs font-bold" style={{ color: data.color }}>
            {data.label}
          </span>
          <span className="text-[10px] text-[#6b7280] ml-auto">#{data.id}</span>
          {data.isStart && <span className="text-[10px] bg-[#e94560] text-white px-1 rounded">INÍCIO</span>}
        </div>
        <div className="text-[11px] text-[#a0a0b0] leading-tight line-clamp-2">{data.preview}</div>
      </div>
      <Handle type="source" position={Position.Bottom} className="!bg-[#a0a0b0]" />
    </>
  );
}

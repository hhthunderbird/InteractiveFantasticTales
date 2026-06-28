import { Handle, Position } from 'reactflow';
import { SECTION_TYPE_ICONS, type SectionType } from '../../types/story';

interface SectionNodeData {
  id: number;
  type: SectionType;
  label: string;
  preview: string;
  color: string;
  bgColor: string;
  borderColor: string;
  borderStyle: string;
  borderWidth: number;
  isStart: boolean;
  isSelected: boolean;
  locked: boolean;
  styleId: string;
}

const HANDLE_SIZE = 10;

const handleStyle: React.CSSProperties = {
  width: HANDLE_SIZE,
  height: HANDLE_SIZE,
  border: '2px solid #64748b',
  background: '#1e293b',
  borderRadius: '50%',
  cursor: 'crosshair',
  zIndex: 10,
};

export function SectionNode({ data }: { data: SectionNodeData }) {
  const effBg = data.bgColor || data.color + '20';
  const effBorderColor = data.isSelected ? '#fff' : data.borderColor;

  return (
    <>
      <Handle
        type="target"
        position={Position.Top}
        style={handleStyle}
        className="!border-[#64748b] hover:!border-[#3b82f6] hover:!bg-[#3b82f6] transition-colors"
      />
      <div
        className={`px-3 py-2 min-w-[180px] max-w-[220px] cursor-pointer transition-all ${
          data.isSelected ? 'shadow-lg shadow-white/20 scale-105' : 'hover:border-white/30'
        } ${data.borderStyle === 'dashed' ? 'border-dashed' : data.borderStyle === 'dotted' ? 'border-dotted' : ''}`}
        style={{
          background: effBg,
          borderColor: effBorderColor,
          borderWidth: data.borderWidth,
          borderStyle: data.borderStyle as any,
          borderRadius: '0.5rem',
        }}
      >
        <div className="flex items-center gap-1.5 mb-1">
          <span className="text-xs">{SECTION_TYPE_ICONS[data.type]}</span>
          <span className="text-xs font-bold" style={{ color: data.color }}>
            {data.label}
          </span>
          <span className="text-[10px] text-[#6b7280] ml-auto">#{data.id}</span>
          {data.isStart && <span className="text-[10px] bg-[#e94560] text-white px-1 rounded">INÍCIO</span>}
          {data.locked && <span className="text-[10px]">🔒</span>}
        </div>
        <div className="text-[11px] text-[#a0a0b0] leading-tight line-clamp-2">{data.preview}</div>
      </div>
      <Handle
        type="source"
        position={Position.Bottom}
        style={handleStyle}
        className="!border-[#64748b] hover:!border-[#e94560] hover:!bg-[#e94560] transition-colors"
      />
    </>
  );
}

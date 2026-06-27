import { useMemo } from 'react';
import { analyzeFlow, getFlowStats } from '../../lib/flow-analyzer';
import { useEditorStore } from '../../stores/editor-store';
import type { AuditIssue } from '../../types/story';

const severityColors: Record<string, string> = {
  error: 'text-[#ef4444] bg-[#ef444420]',
  warning: 'text-[#f59e0b] bg-[#f59e0b20]',
  info: 'text-[#3b82f6] bg-[#3b82f620]',
};

const severityLabels: Record<string, string> = {
  error: 'Erro',
  warning: 'Aviso',
  info: 'Info',
};

export function AuditPanel() {
  const story = useEditorStore((s) => s.story);
  const selectSection = useEditorStore((s) => s.selectSection);
  const auditVisible = useEditorStore((s) => s.auditVisible);

  if (!story || !auditVisible) return null;

  const issues = useMemo(() => analyzeFlow(story), [story]);
  const stats = useMemo(() => getFlowStats(story), [story]);
  const errors = issues.filter((i) => i.severity === 'error');
  const warnings = issues.filter((i) => i.severity === 'warning');
  const infos = issues.filter((i) => i.severity === 'info');

  return (
    <div className="w-80 bg-[#16213e] border-l border-[#2a2a4a] flex flex-col shrink-0 overflow-hidden">
      <div className="p-3 border-b border-[#2a2a4a]">
        <h3 className="text-sm font-semibold text-white">Auditoria de Fluxo</h3>
      </div>

      <div className="p-3 border-b border-[#2a2a4a] grid grid-cols-2 gap-2 text-xs">
        <div className="bg-[#0f3460] rounded p-2">
          <div className="text-[#a0a0b0]">Seções</div>
          <div className="text-white font-bold text-lg">{stats.totalSections}</div>
        </div>
        <div className="bg-[#0f3460] rounded p-2">
          <div className="text-[#a0a0b0]">Finais</div>
          <div className="text-white font-bold text-lg">
            {stats.victoryEndings}V / {stats.defeatEndings}D
          </div>
        </div>
        <div className="bg-[#0f3460] rounded p-2">
          <div className="text-[#a0a0b0]">Prof. Máx.</div>
          <div className="text-white font-bold text-lg">{stats.maxDepth}</div>
        </div>
        <div className="bg-[#0f3460] rounded p-2">
          <div className="text-[#a0a0b0]">Combates</div>
          <div className="text-white font-bold text-lg">{stats.combatCount}</div>
        </div>
        <div className="bg-[#0f3460] rounded p-2">
          <div className="text-[#a0a0b0]">Testes</div>
          <div className="text-white font-bold text-lg">{stats.testCount}</div>
        </div>
        <div className="bg-[#0f3460] rounded p-2">
          <div className="text-[#a0a0b0]">Não Ref.</div>
          <div className={`font-bold text-lg ${stats.unreferencedCount > 0 ? 'text-[#f59e0b]' : 'text-white'}`}>
            {stats.unreferencedCount}
          </div>
        </div>
      </div>

      <div className="flex gap-2 px-3 py-2 text-xs border-b border-[#2a2a4a]">
        <span className="text-[#ef4444]">{errors.length} erros</span>
        <span className="text-[#f59e0b]">{warnings.length} avisos</span>
        <span className="text-[#3b82f6]">{infos.length} infos</span>
      </div>

      <div className="flex-1 overflow-y-auto p-2 space-y-1">
        {issues.length === 0 ? (
          <div className="text-[#10b981] text-sm text-center py-4">✅ Nenhum problema encontrado</div>
        ) : (
          issues.map((issue, i) => (
            <IssueCard key={i} issue={issue} onClick={() => selectSection(issue.sectionId)} />
          ))
        )}
      </div>
    </div>
  );
}

function IssueCard({ issue, onClick }: { issue: AuditIssue; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className={`w-full text-left p-2 rounded text-xs transition-colors hover:opacity-80 ${severityColors[issue.severity]}`}
    >
      <div className="flex items-center gap-1 mb-0.5">
        <span className="font-bold uppercase">{severityLabels[issue.severity]}</span>
        <span className="opacity-60">— Seção {issue.sectionId}</span>
        <span className="ml-auto opacity-60">{issue.code}</span>
      </div>
      <div className="opacity-90">{issue.message}</div>
      {issue.suggestion && <div className="mt-1 opacity-60 italic">{issue.suggestion}</div>}
    </button>
  );
}

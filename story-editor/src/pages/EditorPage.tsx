import { useEditorStore } from '../stores/editor-store';
import { Toolbar } from '../components/layout/Toolbar';
import { StatusBar } from '../components/layout/StatusBar';
import { GraphEditor } from '../components/graph/GraphEditor';
import { InspectorPanel } from '../components/inspector/InspectorPanel';
import { AuditPanel } from '../components/audit/AuditPanel';
import { PreviewPanel } from '../components/preview/PreviewPanel';
import { SheetView } from '../components/sheet/SheetView';
import { TextMode } from '../components/text/TextMode';
import { StylePanel } from '../components/style/StylePanel';

export function EditorPage() {
  const viewMode = useEditorStore((s) => s.viewMode);

  if (viewMode === 'preview') {
    return (
      <div className="h-screen w-screen flex flex-col overflow-hidden">
        <Toolbar />
        <div className="flex-1 flex overflow-hidden">
          <PreviewPanel />
        </div>
        <StatusBar />
      </div>
    );
  }

  if (viewMode === 'sheet') {
    return (
      <div className="h-screen w-screen flex flex-col overflow-hidden">
        <Toolbar />
        <div className="flex-1 flex overflow-hidden">
          <div className="flex-1 flex">
            <SheetView />
          </div>
          <AuditPanel />
        </div>
        <StatusBar />
      </div>
    );
  }

  if (viewMode === 'text') {
    return (
      <div className="h-screen w-screen flex flex-col overflow-hidden">
        <Toolbar />
        <div className="flex-1 flex overflow-hidden">
          <TextMode />
        </div>
        <StatusBar />
      </div>
    );
  }

  return (
    <div className="h-screen w-screen flex flex-col overflow-hidden">
      <Toolbar />
      <div className="flex-1 flex overflow-hidden">
        <div className="flex-1 flex">
          <GraphEditor />
        </div>
        <InspectorPanel />
        <AuditPanel />
        <StylePanel />
      </div>
      <StatusBar />
    </div>
  );
}

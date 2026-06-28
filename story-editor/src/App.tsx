import { useEditorStore } from './stores/editor-store';
import { StoryDashboard } from './pages/StoryDashboard';
import { EditorPage } from './pages/EditorPage';
import { ErrorBoundary } from './components/common/ErrorBoundary';
import { HelpPanel } from './components/common/HelpPanel';

function App() {
  const story = useEditorStore((s) => s.story);

  return (
    <ErrorBoundary>
      {!story ? <StoryDashboard /> : <EditorPage />}
      {story && <HelpPanel />}
    </ErrorBoundary>
  );
}

export default App;

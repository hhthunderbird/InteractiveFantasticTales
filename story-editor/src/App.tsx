import { useEditorStore } from './stores/editor-store';
import { StoryDashboard } from './pages/StoryDashboard';
import { EditorPage } from './pages/EditorPage';
import { ErrorBoundary } from './components/common/ErrorBoundary';

function App() {
  const story = useEditorStore((s) => s.story);

  return (
    <ErrorBoundary>
      {!story ? <StoryDashboard /> : <EditorPage />}
    </ErrorBoundary>
  );
}

export default App;

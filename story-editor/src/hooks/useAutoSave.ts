import { useEffect, useRef, useCallback } from 'react';
import { useEditorStore } from '../stores/editor-store';
import { useStories } from './useStories';
import { useAuth } from './useAuth';

export function useAutoSave() {
  const isDirty = useEditorStore((s) => s.isDirty);
  const setDirty = useEditorStore((s) => s.setDirty);
  const { user } = useAuth();
  const { saveStory } = useStories(user?.uid ?? null);
  const timerRef = useRef<ReturnType<typeof setTimeout>>();
  const isDirtyRef = useRef(isDirty);
  isDirtyRef.current = isDirty;

  useEffect(() => {
    if (!isDirty || !user) return;

    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(async () => {
      const currentStory = useEditorStore.getState().story;
      if (!currentStory || !user || !isDirtyRef.current) return;
      const ok = await saveStory(currentStory.metadata.id, currentStory);
      if (ok) setDirty(false);
    }, 2000);

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [isDirty, user]);

  const manualSave = useCallback(async () => {
    const currentStory = useEditorStore.getState().story;
    if (!currentStory || !user) return false;
    if (timerRef.current) clearTimeout(timerRef.current);
    const ok = await saveStory(currentStory.metadata.id, currentStory);
    if (ok) setDirty(false);
    return ok;
  }, [user, saveStory, setDirty]);

  return { manualSave };
}

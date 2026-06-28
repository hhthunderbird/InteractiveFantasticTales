import { useState, useCallback, useRef, useEffect } from 'react';
import { isLocalMode } from '../lib/local-mode';
import type { StoryData } from '../types/story';

interface StoryListItem {
  id: string;
  title: string;
  authorName: string;
  updatedAt: Date;
  version: string;
}

const LOCAL_STORAGE_KEY = 'ift-local-stories';

function getLocalStories(): Record<string, { meta: StoryListItem; data: StoryData }> {
  try {
    return JSON.parse(localStorage.getItem(LOCAL_STORAGE_KEY) ?? '{}');
  } catch { return {}; }
}

function saveLocalStories(data: Record<string, any>) {
  localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(data));
}

async function firestoreListStories(userId: string): Promise<StoryListItem[]> {
  const { getDb } = await import('../lib/firebase');
  const { collection, query, orderBy, getDocs } = await import('firebase/firestore');
  const db = getDb();
  const q = query(collection(db, 'users', userId, 'stories'), orderBy('updatedAt', 'desc'));
  const snap = await getDocs(q);
  const items: StoryListItem[] = [];
  snap.forEach((d) => {
    const data = d.data();
    items.push({
      id: d.id, title: data.title ?? 'Sem título',
      authorName: data.authorName ?? '', updatedAt: data.updatedAt?.toDate?.() ?? new Date(),
      version: data.version ?? '0.0.0',
    });
  });
  return items;
}

async function firestoreSaveStory(userId: string, storyId: string, story: StoryData) {
  const { getDb } = await import('../lib/firebase');
  const { doc, setDoc, Timestamp } = await import('firebase/firestore');
  await setDoc(doc(getDb(), 'users', userId, 'stories', storyId), {
    title: story.metadata.title, authorName: story.metadata.author?.name ?? '',
    version: story.metadata.version, updatedAt: Timestamp.now(), data: story,
  });
}

async function firestoreLoadStory(userId: string, storyId: string): Promise<StoryData | null> {
  const { getDb } = await import('../lib/firebase');
  const { doc, getDoc } = await import('firebase/firestore');
  const snap = await getDoc(doc(getDb(), 'users', userId, 'stories', storyId));
  return snap.exists() ? snap.data().data as StoryData : null;
}

async function firestoreDeleteStory(userId: string, storyId: string) {
  const { getDb } = await import('../lib/firebase');
  const { doc, deleteDoc } = await import('firebase/firestore');
  await deleteDoc(doc(getDb(), 'users', userId, 'stories', storyId));
}

export function useStories(userId: string | null) {
  const [stories, setStories] = useState<StoryListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const mountedRef = useRef(true);

  useEffect(() => { return () => { mountedRef.current = false; }; }, []);

  const clearError = useCallback(() => setError(null), []);

  const listStories = useCallback(async () => {
    if (!userId) return;
    setLoading(true); setError(null);
    try {
      if (isLocalMode()) {
        const local = getLocalStories();
        const items = Object.values(local).map((v) => v.meta).sort((a, b) => b.updatedAt.getTime() - a.updatedAt.getTime());
        if (mountedRef.current) setStories(items);
      } else {
        const items = await firestoreListStories(userId);
        if (mountedRef.current) setStories(items);
      }
    } catch (e: any) {
      if (mountedRef.current) setError(e.message);
    } finally {
      if (mountedRef.current) setLoading(false);
    }
  }, [userId]);

  const loadStory = useCallback(async (storyId: string): Promise<{ data: StoryData | null; error?: string }> => {
    if (!userId) return { data: null, error: 'Não autenticado.' };
    try {
      if (isLocalMode()) {
        const local = getLocalStories();
        const entry = local[storyId];
        return entry ? { data: entry.data } : { data: null, error: 'História não encontrada.' };
      }
      const data = await firestoreLoadStory(userId, storyId);
      return data ? { data } : { data: null, error: 'História não encontrada.' };
    } catch (e: any) {
      return { data: null, error: e.message };
    }
  }, [userId]);

  const saveStory = useCallback(async (storyId: string, story: StoryData): Promise<boolean> => {
    if (!userId) return false;
    setError(null);
    try {
      if (isLocalMode()) {
        const local = getLocalStories();
        local[storyId] = {
          meta: {
            id: storyId, title: story.metadata.title,
            authorName: story.metadata.author?.name ?? '', version: story.metadata.version,
            updatedAt: new Date(),
          },
          data: story,
        };
        saveLocalStories(local);
      } else {
        await firestoreSaveStory(userId, storyId, story);
      }
      return true;
    } catch (e: any) { setError(e.message); return false; }
  }, [userId]);

  const deleteStory = useCallback(async (storyId: string): Promise<boolean> => {
    if (!userId) return false;
    setError(null);
    try {
      if (isLocalMode()) {
        const local = getLocalStories();
        delete local[storyId];
        saveLocalStories(local);
      } else {
        await firestoreDeleteStory(userId, storyId);
      }
      return true;
    } catch (e: any) { setError(e.message); return false; }
  }, [userId]);

  return { stories, loading, error, clearError, listStories, loadStory, saveStory, deleteStory };
}

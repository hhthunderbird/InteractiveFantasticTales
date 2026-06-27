import { useState, useCallback, useRef } from 'react';
import {
  collection,
  doc,
  getDocs,
  getDoc,
  setDoc,
  deleteDoc,
  query,
  orderBy,
  Timestamp,
} from 'firebase/firestore';
import { getDb } from '../lib/firebase';
import type { StoryData } from '../types/story';

interface StoryListItem {
  id: string;
  title: string;
  authorName: string;
  updatedAt: Date;
  version: string;
}

export function useStories(userId: string | null) {
  const [stories, setStories] = useState<StoryListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const mountedRef = useRef(true);

  const clearError = useCallback(() => setError(null), []);

  const listStories = useCallback(async () => {
    if (!userId) return;
    setLoading(true);
    setError(null);
    try {
      const db = getDb();
      const q = query(
        collection(db, 'users', userId, 'stories'),
        orderBy('updatedAt', 'desc'),
      );
      const snap = await getDocs(q);
      if (!mountedRef.current) return;
      const items: StoryListItem[] = [];
      snap.forEach((d) => {
        const data = d.data();
        items.push({
          id: d.id,
          title: data.title ?? 'Sem título',
          authorName: data.authorName ?? '',
          updatedAt: data.updatedAt?.toDate?.() ?? new Date(),
          version: data.version ?? '0.0.0',
        });
      });
      setStories(items);
    } catch (e: any) {
      if (mountedRef.current) setError(e.message);
    } finally {
      if (mountedRef.current) setLoading(false);
    }
  }, [userId]);

  const loadStory = useCallback(async (storyId: string): Promise<{ data: StoryData | null; error?: string }> => {
    if (!userId) return { data: null, error: 'Não autenticado.' };
    try {
      const db = getDb();
      const ref = doc(db, 'users', userId, 'stories', storyId);
      const snap = await getDoc(ref);
      if (!snap.exists()) return { data: null, error: 'História não encontrada.' };
      return { data: snap.data().data as StoryData };
    } catch (e: any) {
      return { data: null, error: e.message };
    }
  }, [userId]);

  const saveStory = useCallback(async (storyId: string, story: StoryData): Promise<boolean> => {
    if (!userId) return false;
    setError(null);
    try {
      const db = getDb();
      const ref = doc(db, 'users', userId, 'stories', storyId);
      await setDoc(ref, {
        title: story.metadata.title,
        authorName: story.metadata.author?.name ?? '',
        version: story.metadata.version,
        updatedAt: Timestamp.now(),
        data: story,
      });
      return true;
    } catch (e: any) {
      setError(e.message);
      return false;
    }
  }, [userId]);

  const deleteStory = useCallback(async (storyId: string): Promise<boolean> => {
    if (!userId) return false;
    setError(null);
    try {
      const db = getDb();
      const ref = doc(db, 'users', userId, 'stories', storyId);
      await deleteDoc(ref);
      return true;
    } catch (e: any) {
      setError(e.message);
      return false;
    }
  }, [userId]);

  return { stories, loading, error, clearError, listStories, loadStory, saveStory, deleteStory };
}

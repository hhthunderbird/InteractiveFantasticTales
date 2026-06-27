import { create } from 'zustand';
import type { StoryData, StoryMetadata, SectionData } from '../types/story';

interface EditorState {
  story: StoryData | null;
  selectedSectionId: number | null;
  isDirty: boolean;
  auditVisible: boolean;
  previewVisible: boolean;
  viewMode: 'graph' | 'sheet' | 'text' | 'preview';

  setStory: (story: StoryData) => void;
  clearStory: () => void;
  updateMetadata: (meta: Partial<StoryMetadata>) => void;
  addSection: (section: SectionData) => void;
  updateSection: (id: number, data: Partial<SectionData>) => void;
  removeSection: (id: number) => void;
  selectSection: (id: number | null) => void;
  setDirty: (dirty: boolean) => void;
  toggleAudit: () => void;
  togglePreview: () => void;
  setViewMode: (mode: EditorState['viewMode']) => void;
  getSection: (id: number) => SectionData | undefined;
}

export const useEditorStore = create<EditorState>((set, get) => ({
  story: null,
  selectedSectionId: null,
  isDirty: false,
  auditVisible: false,
  previewVisible: false,
  viewMode: 'graph',

  setStory: (story) => set({ story, isDirty: false, selectedSectionId: story.metadata.startSection }),

  clearStory: () => set({ story: null, selectedSectionId: null, isDirty: false, auditVisible: false, previewVisible: false }),

  updateMetadata: (meta) => {
    const story = get().story;
    if (!story) return;
    set({
      story: { ...story, metadata: { ...story.metadata, ...meta } },
      isDirty: true,
    });
  },

  addSection: (section) => {
    const story = get().story;
    if (!story) return;
    set({
      story: {
        ...story,
        sections: { ...story.sections, [section.id]: section },
      },
      isDirty: true,
    });
  },

  updateSection: (id, data) => {
    const story = get().story;
    if (!story || !story.sections[id]) return;
    set({
      story: {
        ...story,
        sections: {
          ...story.sections,
          [id]: { ...story.sections[id], ...data },
        },
      },
      isDirty: true,
    });
  },

  removeSection: (id) => {
    const story = get().story;
    if (!story) return;

    const cleaned: Record<number, SectionData> = {};
    for (const [key, section] of Object.entries(story.sections)) {
      const sid = Number(key);
      if (sid === id) continue;
      const clean = { ...section };

      if (clean.choices) {
        clean.choices = clean.choices.map((c) => ({
          ...c,
          targetSection: c.targetSection === id ? 0 : c.targetSection,
        }));
      }
      if (clean.combat) {
        const c = clean.combat;
        clean.combat = {
          ...c,
          victoryTarget: c.victoryTarget === id ? 0 : c.victoryTarget,
          defeatTarget: c.defeatTarget === id ? 0 : c.defeatTarget,
          fleeTarget: c.fleeTarget === id ? 0 : c.fleeTarget,
        };
      }
      if (clean.test) {
        const t = clean.test;
        clean.test = {
          ...t,
          successTarget: t.successTarget === id ? 0 : t.successTarget,
          failTarget: t.failTarget === id ? 0 : t.failTarget,
        };
      }
      if (clean.itemGate) {
        const g = clean.itemGate;
        clean.itemGate = {
          ...g,
          hasItemTarget: g.hasItemTarget === id ? 0 : g.hasItemTarget,
          noItemTarget: g.noItemTarget === id ? 0 : g.noItemTarget,
        };
      }
      if (clean.random) {
        clean.random = {
          outcomes: clean.random.outcomes.map((o) => ({
            ...o,
            targetSection: o.targetSection === id ? 0 : o.targetSection,
          })),
        };
      }
      cleaned[sid] = clean;
    }

    set({
      story: { ...story, sections: cleaned },
      isDirty: true,
      selectedSectionId: get().selectedSectionId === id ? null : get().selectedSectionId,
    });
  },

  selectSection: (id) => set({ selectedSectionId: id }),
  setDirty: (dirty) => set({ isDirty: dirty }),
  toggleAudit: () => set((s) => ({ auditVisible: !s.auditVisible })),
  togglePreview: () => set((s) => ({ previewVisible: !s.previewVisible })),
  setViewMode: (mode) => set({ viewMode: mode }),

  getSection: (id) => get().story?.sections[id],
}));

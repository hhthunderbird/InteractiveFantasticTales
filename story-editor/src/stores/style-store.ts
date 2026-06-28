import { create } from 'zustand';
import type { NodeStyle, NodeGroup } from '../types/story';
import { DEFAULT_NODE_STYLE } from '../types/story';

interface StyleState {
  styles: NodeStyle[];
  groups: NodeGroup[];
  sectionStyles: Record<number, string>;
  sectionLocks: Record<number, boolean>;
  highlightedStyleIds: string[];
  stylePanelOpen: boolean;

  addStyle: (style: NodeStyle) => void;
  updateStyle: (id: string, data: Partial<NodeStyle>) => void;
  removeStyle: (id: string) => void;
  applyStyle: (styleId: string, sectionIds: number[]) => void;
  removeSectionStyle: (sectionId: number) => void;
  getSectionStyle: (sectionId: number) => NodeStyle;
  toggleHighlight: (styleId: string) => void;
  clearHighlights: () => void;

  addGroup: (group: NodeGroup) => void;
  updateGroup: (id: string, data: Partial<NodeGroup>) => void;
  removeGroup: (id: string) => void;
  addNodesToGroup: (groupId: string, nodeIds: number[]) => void;
  removeNodesFromGroup: (groupId: string, nodeIds: number[]) => void;

  toggleLock: (sectionId: number) => void;
  isLocked: (sectionId: number) => boolean;

  toggleStylePanel: () => void;
  getGroupsForNode: (sectionId: number) => NodeGroup[];
  getHighlightedSectionIds: () => number[];
}

export const useStyleStore = create<StyleState>((set, get) => ({
  styles: [DEFAULT_NODE_STYLE],
  groups: [],
  sectionStyles: {},
  sectionLocks: {},
  highlightedStyleIds: [],
  stylePanelOpen: false,

  addStyle: (style) => set((s) => ({ styles: [...s.styles, style] })),
  updateStyle: (id, data) => set((s) => ({
    styles: s.styles.map((st) => (st.id === id ? { ...st, ...data } : st)),
  })),
  removeStyle: (id) => {
    if (id === '__default__') return;
    set((s) => ({
      styles: s.styles.filter((st) => st.id !== id),
      sectionStyles: Object.fromEntries(
        Object.entries(s.sectionStyles).filter(([, styleId]) => styleId !== id),
      ),
    }));
  },
  applyStyle: (styleId, sectionIds) => set((s) => {
    const map = { ...s.sectionStyles };
    for (const sid of sectionIds) map[sid] = styleId;
    return { sectionStyles: map };
  }),
  removeSectionStyle: (sectionId) => set((s) => {
    const { [sectionId]: _, ...rest } = s.sectionStyles;
    return { sectionStyles: rest };
  }),
  getSectionStyle: (sectionId) => {
    const { styles, sectionStyles } = get();
    const styleId = sectionStyles[sectionId];
    if (!styleId) return DEFAULT_NODE_STYLE;
    return styles.find((s) => s.id === styleId) ?? DEFAULT_NODE_STYLE;
  },

  toggleHighlight: (styleId) => set((s) => ({
    highlightedStyleIds: s.highlightedStyleIds.includes(styleId)
      ? s.highlightedStyleIds.filter((id) => id !== styleId)
      : [...s.highlightedStyleIds, styleId],
  })),
  clearHighlights: () => set({ highlightedStyleIds: [] }),

  addGroup: (group) => set((s) => ({ groups: [...s.groups, group] })),
  updateGroup: (id, data) => set((s) => ({
    groups: s.groups.map((g) => (g.id === id ? { ...g, ...data } : g)),
  })),
  removeGroup: (id) => set((s) => ({ groups: s.groups.filter((g) => g.id !== id) })),
  addNodesToGroup: (groupId, nodeIds) => set((s) => ({
    groups: s.groups.map((g) =>
      g.id === groupId ? { ...g, nodeIds: [...new Set([...g.nodeIds, ...nodeIds])] } : g,
    ),
  })),
  removeNodesFromGroup: (groupId, nodeIds) => set((s) => ({
    groups: s.groups.map((g) =>
      g.id === groupId ? { ...g, nodeIds: g.nodeIds.filter((n) => !nodeIds.includes(n)) } : g,
    ),
  })),

  toggleLock: (sectionId) => set((s) => ({
    sectionLocks: { ...s.sectionLocks, [sectionId]: !s.sectionLocks[sectionId] },
  })),
  isLocked: (sectionId) => get().sectionLocks[sectionId] ?? false,

  toggleStylePanel: () => set((s) => ({ stylePanelOpen: !s.stylePanelOpen })),

  getGroupsForNode: (sectionId) => get().groups.filter((g) => g.nodeIds.includes(sectionId)),

  getHighlightedSectionIds: () => {
    const { highlightedStyleIds, sectionStyles } = get();
    if (highlightedStyleIds.length === 0) return [];
    return Object.entries(sectionStyles)
      .filter(([, styleId]) => highlightedStyleIds.includes(styleId))
      .map(([sectionId]) => Number(sectionId));
  },
}));

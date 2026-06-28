import { describe, it, expect, beforeEach } from 'vitest';
import { useStyleStore } from '../stores/style-store';

describe('Style Store', () => {
  beforeEach(() => {
    useStyleStore.setState({
      styles: [{ id: '__default__', name: 'Padrão', color: '#a0a0b0', borderColor: 'transparent', borderWidth: 2, borderStyle: 'solid', backgroundColor: '', fontSize: 'medium' }],
      groups: [],
      sectionStyles: {},
      sectionLocks: {},
      highlightedStyleIds: [],
      stylePanelOpen: false,
    });
  });

  describe('styles CRUD', () => {
    it('adds a new style', () => {
      useStyleStore.getState().addStyle({ id: 's1', name: 'Destaque', color: '#e94560', borderColor: '#e94560', borderWidth: 3, borderStyle: 'solid', backgroundColor: '', fontSize: 'medium' });
      expect(useStyleStore.getState().styles).toHaveLength(2);
    });

    it('prevents removing __default__ style', () => {
      useStyleStore.getState().removeStyle('__default__');
      expect(useStyleStore.getState().styles).toHaveLength(1);
      expect(useStyleStore.getState().styles[0].id).toBe('__default__');
    });

    it('removing style cleans sectionStyles map', () => {
      useStyleStore.getState().addStyle({ id: 's1', name: 'Destaque', color: '#e94560', borderColor: '#e94560', borderWidth: 3, borderStyle: 'solid', backgroundColor: '', fontSize: 'medium' });
      useStyleStore.getState().applyStyle('s1', [1, 2, 3]);
      useStyleStore.getState().removeStyle('s1');
      expect(useStyleStore.getState().sectionStyles[1]).toBeUndefined();
      expect(useStyleStore.getState().sectionStyles[2]).toBeUndefined();
    });
  });

  describe('applyStyle / removeSectionStyle', () => {
    it('applies style to multiple sections', () => {
      useStyleStore.getState().addStyle({ id: 's1', name: 'Destaque', color: '#e94560', borderColor: '#e94560', borderWidth: 3, borderStyle: 'solid', backgroundColor: '', fontSize: 'medium' });
      useStyleStore.getState().applyStyle('s1', [1, 5, 10]);
      expect(useStyleStore.getState().sectionStyles[1]).toBe('s1');
      expect(useStyleStore.getState().sectionStyles[5]).toBe('s1');
      expect(useStyleStore.getState().sectionStyles[10]).toBe('s1');
    });

    it('getSectionStyle returns default for unset sections', () => {
      const style = useStyleStore.getState().getSectionStyle(999);
      expect(style.id).toBe('__default__');
    });

    it('getSectionStyle returns custom style for set sections', () => {
      useStyleStore.getState().addStyle({ id: 's1', name: 'Destaque', color: '#e94560', borderColor: '#e94560', borderWidth: 3, borderStyle: 'solid', backgroundColor: '', fontSize: 'medium' });
      useStyleStore.getState().applyStyle('s1', [42]);
      const style = useStyleStore.getState().getSectionStyle(42);
      expect(style.id).toBe('s1');
      expect(style.color).toBe('#e94560');
    });
  });

  describe('highlight', () => {
    it('toggles highlight on and off', () => {
      useStyleStore.getState().addStyle({ id: 's1', name: 'Destaque', color: '#e94560', borderColor: '#e94560', borderWidth: 3, borderStyle: 'solid', backgroundColor: '', fontSize: 'medium' });
      useStyleStore.getState().toggleHighlight('s1');
      expect(useStyleStore.getState().highlightedStyleIds).toContain('s1');
      useStyleStore.getState().toggleHighlight('s1');
      expect(useStyleStore.getState().highlightedStyleIds).not.toContain('s1');
    });

    it('clearHighlights removes all', () => {
      useStyleStore.getState().toggleHighlight('s1');
      useStyleStore.getState().toggleHighlight('s2');
      useStyleStore.getState().clearHighlights();
      expect(useStyleStore.getState().highlightedStyleIds).toHaveLength(0);
    });

    it('getHighlightedSectionIds returns matching sections', () => {
      useStyleStore.getState().addStyle({ id: 's1', name: 'Destaque', color: '#e94560', borderColor: '#e94560', borderWidth: 3, borderStyle: 'solid', backgroundColor: '', fontSize: 'medium' });
      useStyleStore.getState().applyStyle('s1', [1, 3, 5]);
      useStyleStore.getState().toggleHighlight('s1');
      const ids = useStyleStore.getState().getHighlightedSectionIds();
      expect(ids).toContain(1);
      expect(ids).toContain(3);
      expect(ids).toContain(5);
    });
  });

  describe('groups', () => {
    it('adds and removes groups', () => {
      useStyleStore.getState().addGroup({ id: 'g1', title: 'Grupo A', color: '#3b82f6', borderStyle: 'dashed', borderWidth: 2, visible: true, nodeIds: [], collapsed: false });
      expect(useStyleStore.getState().groups).toHaveLength(1);
      useStyleStore.getState().removeGroup('g1');
      expect(useStyleStore.getState().groups).toHaveLength(0);
    });

    it('addNodesToGroup adds unique nodes', () => {
      useStyleStore.getState().addGroup({ id: 'g1', title: 'Grupo A', color: '#3b82f6', borderStyle: 'dashed', borderWidth: 2, visible: true, nodeIds: [1], collapsed: false });
      useStyleStore.getState().addNodesToGroup('g1', [1, 2, 3, 1]);
      expect(useStyleStore.getState().groups[0].nodeIds).toEqual([1, 2, 3]);
    });

    it('removeNodesFromGroup', () => {
      useStyleStore.getState().addGroup({ id: 'g1', title: 'Grupo A', color: '#3b82f6', borderStyle: 'dashed', borderWidth: 2, visible: true, nodeIds: [1, 2, 3], collapsed: false });
      useStyleStore.getState().removeNodesFromGroup('g1', [2]);
      expect(useStyleStore.getState().groups[0].nodeIds).toEqual([1, 3]);
    });

    it('getGroupsForNode returns matching groups', () => {
      useStyleStore.getState().addGroup({ id: 'g1', title: 'Grupo A', color: '#3b82f6', borderStyle: 'dashed', borderWidth: 2, visible: true, nodeIds: [1, 2], collapsed: false });
      useStyleStore.getState().addGroup({ id: 'g2', title: 'Grupo B', color: '#10b981', borderStyle: 'solid', borderWidth: 2, visible: true, nodeIds: [2, 3], collapsed: false });
      const groups = useStyleStore.getState().getGroupsForNode(2);
      expect(groups).toHaveLength(2);
    });
  });

  describe('locks', () => {
    it('toggles lock', () => {
      expect(useStyleStore.getState().isLocked(1)).toBe(false);
      useStyleStore.getState().toggleLock(1);
      expect(useStyleStore.getState().isLocked(1)).toBe(true);
      useStyleStore.getState().toggleLock(1);
      expect(useStyleStore.getState().isLocked(1)).toBe(false);
    });
  });

  describe('stylePanel', () => {
    it('toggles panel visibility', () => {
      expect(useStyleStore.getState().stylePanelOpen).toBe(false);
      useStyleStore.getState().toggleStylePanel();
      expect(useStyleStore.getState().stylePanelOpen).toBe(true);
    });
  });
});

import { describe, it, expect, beforeEach } from 'vitest';
import { useEditorStore } from '../stores/editor-store';
import { createEmptyStory } from '../lib/json-handler';
import type { StoryData } from '../types/story';

function freshStory(): StoryData {
  const s = createEmptyStory();
  s.metadata.id = 'test';
  s.metadata.startSection = 1;
  s.sections[1] = { id: 1, type: 'narrative' as const, text: 'Início', choices: [] };
  return s;
}

describe('Editor Store', () => {
  beforeEach(() => {
    useEditorStore.setState({
      story: null,
      selectedSectionId: null,
      isDirty: false,
      auditVisible: false,
      previewVisible: false,
      viewMode: 'graph',
      layoutVersion: 0,
    });
  });

  describe('setStory / clearStory', () => {
    it('setStory loads story and selects start section', () => {
      const story = freshStory();
      useEditorStore.getState().setStory(story);
      const state = useEditorStore.getState();
      expect(state.story).not.toBeNull();
      expect(state.selectedSectionId).toBe(1);
      expect(state.isDirty).toBe(false);
    });

    it('clearStory resets everything', () => {
      useEditorStore.getState().setStory(freshStory());
      useEditorStore.getState().clearStory();
      const state = useEditorStore.getState();
      expect(state.story).toBeNull();
      expect(state.selectedSectionId).toBeNull();
      expect(state.isDirty).toBe(false);
    });
  });

  describe('addSection', () => {
    it('adds section and marks dirty', () => {
      useEditorStore.getState().setStory(freshStory());
      useEditorStore.getState().addSection({ id: 2, type: 'narrative' as const, text: 'Nova', choices: [] });
      const state = useEditorStore.getState();
      expect(state.story?.sections[2]).toBeDefined();
      expect(state.isDirty).toBe(true);
    });

    it('overwrites existing section with same ID', () => {
      useEditorStore.getState().setStory(freshStory());
      useEditorStore.getState().addSection({ id: 1, type: 'ending' as const, text: 'Sobrescrito', ending: { type: 'neutral' } });
      expect(useEditorStore.getState().story?.sections[1].type).toBe('ending');
    });
  });

  describe('updateSection', () => {
    it('updates section fields', () => {
      useEditorStore.getState().setStory(freshStory());
      useEditorStore.getState().updateSection(1, { text: 'Atualizado', choices: [{ id: 'c1', text: 'Ir', targetSection: 2, conditions: [] }] });
      const sec = useEditorStore.getState().story?.sections[1];
      expect(sec?.text).toBe('Atualizado');
      expect(sec?.choices).toHaveLength(1);
    });

    it('ignores update for non-existent section', () => {
      useEditorStore.getState().setStory(freshStory());
      useEditorStore.getState().updateSection(999, { text: 'Nope' });
      expect(useEditorStore.getState().story?.sections[999]).toBeUndefined();
    });

    it('preserves other fields on partial update', () => {
      const story = freshStory();
      story.sections[1].onEnter = { modifyGold: 5 };
      useEditorStore.getState().setStory(story);
      useEditorStore.getState().updateSection(1, { text: 'Novo texto' });
      expect(useEditorStore.getState().story?.sections[1].onEnter?.modifyGold).toBe(5);
    });
  });

  describe('removeSection', () => {
    it('removes section and cleans backreferences', () => {
      const story = freshStory();
      story.sections[1].choices = [{ id: 'c1', text: 'Ir', targetSection: 2, conditions: [] }];
      story.sections[2] = { id: 2, type: 'ending' as const, text: 'Fim', ending: { type: 'victory' } };
      story.sections[3] = { id: 3, type: 'combat' as const, text: 'Luta', combat: { enemyName: 'Goblin', enemySkill: 5, enemyStamina: 6, victoryTarget: 2, defeatTarget: 2, fleeTarget: 0, allowFlee: false, lootOnVictory: [] } };
      useEditorStore.getState().setStory(story);

      useEditorStore.getState().removeSection(2);

      const secs = useEditorStore.getState().story?.sections;
      expect(secs?.[2]).toBeUndefined();
      expect(secs?.[1].choices?.[0].targetSection).toBe(0); // cleaned
      expect(secs?.[3].combat?.victoryTarget).toBe(0); // cleaned
      expect(secs?.[3].combat?.defeatTarget).toBe(0); // cleaned
    });

    it('removing start section deselects it', () => {
      const story = freshStory();
      story.sections[1].choices = [{ id: 'c1', text: 'Ir', targetSection: 2, conditions: [] }];
      story.sections[2] = { id: 2, type: 'ending' as const, text: 'Fim', ending: { type: 'victory' } };
      useEditorStore.getState().setStory(story);
      useEditorStore.getState().selectSection(2);
      useEditorStore.getState().removeSection(2);
      expect(useEditorStore.getState().selectedSectionId).toBeNull();
    });
  });

  describe('selectSection', () => {
    it('selects and deselects', () => {
      useEditorStore.getState().setStory(freshStory());
      useEditorStore.getState().selectSection(1);
      expect(useEditorStore.getState().selectedSectionId).toBe(1);
      useEditorStore.getState().selectSection(null);
      expect(useEditorStore.getState().selectedSectionId).toBeNull();
    });
  });

  describe('viewModes', () => {
    it('toggles through all modes', () => {
      const modes = ['graph', 'sheet', 'text', 'preview', 'project'] as const;
      for (const mode of modes) {
        useEditorStore.getState().setViewMode(mode);
        expect(useEditorStore.getState().viewMode).toBe(mode);
      }
    });
  });

  describe('toggleAudit / togglePreview', () => {
    it('toggles audit panel', () => {
      expect(useEditorStore.getState().auditVisible).toBe(false);
      useEditorStore.getState().toggleAudit();
      expect(useEditorStore.getState().auditVisible).toBe(true);
      useEditorStore.getState().toggleAudit();
      expect(useEditorStore.getState().auditVisible).toBe(false);
    });
  });

  describe('triggerLayout', () => {
    it('increments layoutVersion', () => {
      expect(useEditorStore.getState().layoutVersion).toBe(0);
      useEditorStore.getState().triggerLayout();
      expect(useEditorStore.getState().layoutVersion).toBe(1);
      useEditorStore.getState().triggerLayout();
      expect(useEditorStore.getState().layoutVersion).toBe(2);
    });
  });

  describe('dirty flag', () => {
    it('setStory resets dirty', () => {
      useEditorStore.getState().setStory(freshStory());
      useEditorStore.getState().addSection({ id: 2, type: 'narrative' as const, text: 'Nova', choices: [] });
      expect(useEditorStore.getState().isDirty).toBe(true);
      useEditorStore.getState().setStory(freshStory());
      expect(useEditorStore.getState().isDirty).toBe(false);
    });

    it('setDirty can be manually controlled', () => {
      useEditorStore.getState().setDirty(true);
      expect(useEditorStore.getState().isDirty).toBe(true);
      useEditorStore.getState().setDirty(false);
      expect(useEditorStore.getState().isDirty).toBe(false);
    });
  });
});

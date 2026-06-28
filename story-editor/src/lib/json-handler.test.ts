import { describe, it, expect } from 'vitest';
import { exportStoryToJson, importStoryFromJson, createEmptyStory } from './json-handler';

describe('JSON Handler', () => {
  describe('createEmptyStory', () => {
    it('creates a valid story with required fields', () => {
      const story = createEmptyStory();
      expect(story.formatVersion).toBe('1.0');
      expect(story.metadata.startSection).toBe(1);
      expect(story.sections[1]).toBeDefined();
      expect(story.sections[1].type).toBe('narrative');
    });

    it('has default character attributes', () => {
      const story = createEmptyStory();
      expect(story.characterCreation.attributes.skill).toBeDefined();
      expect(story.characterCreation.attributes.skill.dice).toBe('1d6+6');
    });
  });

  describe('exportStoryToJson', () => {
    it('produces valid JSON string', () => {
      const story = createEmptyStory();
      const json = exportStoryToJson(story);
      expect(() => JSON.parse(json)).not.toThrow();
    });

    it('is idempotent (export → import → export)', () => {
      const story = createEmptyStory();
      story.metadata.title = 'Teste';
      const json1 = exportStoryToJson(story);
      const imported = importStoryFromJson(json1);
      const json2 = exportStoryToJson(imported);
      expect(json1).toBe(json2);
    });

    it('round-trips complex nested data', () => {
      const story = createEmptyStory();
      story.sections[2] = {
        id: 2, type: 'combat', text: 'Luta!',
        combat: { enemyName: 'Dragão', enemySkill: 10, enemyStamina: 20, victoryTarget: 3, defeatTarget: 4, fleeTarget: 0, allowFlee: false, lootOnVictory: ['espada_lendaria'] },
        onEnter: { addItems: ['poção'], setFlags: { derrotou_dragao: true } },
        presentation: { narration: { voice: 'deep', speed: 0.8, emphasis: ['Dragão', 'lendária'] } },
      };
      story.sections[3] = { id: 3, type: 'ending', text: 'Vitória!', ending: { type: 'victory' } };
      story.sections[4] = { id: 4, type: 'ending', text: 'Derrota.', ending: { type: 'defeat' } };

      const json = exportStoryToJson(story);
      const imported = importStoryFromJson(json);

      expect(imported.sections[2].combat?.enemyName).toBe('Dragão');
      expect(imported.sections[2].presentation?.narration?.voice).toBe('deep');
      expect(imported.sections[2].onEnter?.setFlags?.derrotou_dragao).toBe(true);
    });
  });

  describe('importStoryFromJson — malformed data', () => {
    it('rejects missing formatVersion', () => {
      expect(() => importStoryFromJson('{"metadata":{},"sections":{}}')).toThrow('formatVersion');
    });

    it('rejects missing metadata', () => {
      expect(() => importStoryFromJson('{"formatVersion":"1.0","sections":{}}')).toThrow('metadata');
    });

    it('rejects missing sections', () => {
      expect(() => importStoryFromJson('{"formatVersion":"1.0","metadata":{}}')).toThrow('sections');
    });

    it('rejects empty JSON object', () => {
      expect(() => importStoryFromJson('{}')).toThrow();
    });

    it('rejects non-JSON string', () => {
      expect(() => importStoryFromJson('not json at all')).toThrow();
    });

    it('rejects null', () => {
      expect(() => importStoryFromJson('null')).toThrow();
    });

    it('rejects array instead of object', () => {
      expect(() => importStoryFromJson('[1,2,3]')).toThrow();
    });

    it('accepts sections as object (Record format)', () => {
      const json = JSON.stringify({
        formatVersion: '1.0',
        metadata: { id: 'x', title: 'T', author: { name: 'A', email: 'a@b.com' }, version: '1', language: 'pt', genre: [], description: '', coverImage: '', startSection: 1, estimatedDuration: '', tags: [] },
        characterCreation: { attributes: {}, startingGold: 0, startingItems: [], startingProvisions: 0 },
        flags: {},
        items: {},
        sections: { "1": { id: 1, type: 'narrative', text: 'Olá', choices: [] } },
      });
      const story = importStoryFromJson(json);
      expect(story.sections[1].text).toBe('Olá');
    });

    it('handles sections with numeric keys', () => {
      const json = JSON.stringify({
        formatVersion: '1.0',
        metadata: { id: 'x', title: 'T', author: { name: 'A', email: 'a@b.com' }, version: '1', language: 'pt', genre: [], description: '', coverImage: '', startSection: 42, estimatedDuration: '', tags: [] },
        characterCreation: { attributes: {}, startingGold: 0, startingItems: [], startingProvisions: 0 },
        flags: {},
        items: {},
        sections: { "42": { id: 42, type: 'narrative', text: 'Começa aqui', choices: [] } },
      });
      const story = importStoryFromJson(json);
      expect(story.sections[42]).toBeDefined();
      expect(story.metadata.startSection).toBe(42);
    });
  });

  describe('large stories', () => {
    it('handles 400-section export/import', () => {
      const story = createEmptyStory();
      story.sections = {};
      for (let i = 1; i <= 400; i++) {
        story.sections[i] = { id: i, type: 'narrative' as const, text: `Seção ${i}`, choices: i < 400 ? [{ text: '→', targetSection: i + 1, conditions: [] }] : [] };
      }
      story.sections[400] = { id: 400, type: 'ending' as const, text: 'Fim', ending: { type: 'victory' } };

      const json = exportStoryToJson(story);
      const imported = importStoryFromJson(json);

      expect(Object.keys(imported.sections)).toHaveLength(400);
      expect(imported.sections[1].text).toBe('Seção 1');
      expect(imported.sections[400].ending?.type).toBe('victory');
    });
  });
});

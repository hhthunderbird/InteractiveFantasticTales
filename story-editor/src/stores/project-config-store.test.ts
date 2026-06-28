import { describe, it, expect, beforeEach } from 'vitest';
import { useProjectConfigStore } from '../stores/project-config-store';

describe('Project Config Store', () => {
  beforeEach(() => {
    useProjectConfigStore.getState().resetConfig();
  });

  describe('items', () => {
    it('adds and retrieves items', () => {
      useProjectConfigStore.getState().addItem({ id: 'i1', name: 'Espada Longa', type: 'weapon', description: 'Uma espada afiada', effects: { skill: 2 }, tags: ['arma', 'rara'] });
      expect(useProjectConfigStore.getState().items).toHaveLength(1);
      expect(useProjectConfigStore.getState().getItem('i1')?.name).toBe('Espada Longa');
    });

    it('updates item fields', () => {
      useProjectConfigStore.getState().addItem({ id: 'i1', name: 'Espada', type: 'weapon', description: '', effects: {}, tags: [] });
      useProjectConfigStore.getState().updateItem('i1', { description: 'Muito afiada', effects: { skill: 3 } });
      const item = useProjectConfigStore.getState().getItem('i1');
      expect(item?.description).toBe('Muito afiada');
      expect(item?.effects.skill).toBe(3);
    });

    it('removes items', () => {
      useProjectConfigStore.getState().addItem({ id: 'i1', name: 'Espada', type: 'weapon', description: '', effects: {}, tags: [] });
      useProjectConfigStore.getState().removeItem('i1');
      expect(useProjectConfigStore.getState().items).toHaveLength(0);
      expect(useProjectConfigStore.getState().getItem('i1')).toBeUndefined();
    });

    it('getItem returns undefined for unknown id', () => {
      expect(useProjectConfigStore.getState().getItem('unknown')).toBeUndefined();
    });

    it('multiple items of different types', () => {
      const store = useProjectConfigStore.getState();
      store.addItem({ id: 'w1', name: 'Espada', type: 'weapon', description: '', effects: {}, tags: [] });
      store.addItem({ id: 'p1', name: 'Poção de Cura', type: 'consumable', description: '', effects: { stamina: 6 }, tags: [] });
      store.addItem({ id: 'k1', name: 'Chave de Prata', type: 'key', description: '', effects: {}, tags: [] });
      expect(useProjectConfigStore.getState().items).toHaveLength(3);
    });
  });

  describe('enemies', () => {
    it('adds and retrieves enemies', () => {
      useProjectConfigStore.getState().addEnemy({ id: 'e1', name: 'Dragão', skill: 12, stamina: 20, description: 'Temível dragão vermelho', loot: ['i1'], tags: ['chefe', 'dragão'] });
      expect(useProjectConfigStore.getState().enemies).toHaveLength(1);
      expect(useProjectConfigStore.getState().getEnemy('e1')?.skill).toBe(12);
    });

    it('updates enemy fields', () => {
      useProjectConfigStore.getState().addEnemy({ id: 'e1', name: 'Goblin', skill: 4, stamina: 5, description: '', loot: [], tags: [] });
      useProjectConfigStore.getState().updateEnemy('e1', { stamina: 8, loot: ['poção'] });
      const enemy = useProjectConfigStore.getState().getEnemy('e1');
      expect(enemy?.stamina).toBe(8);
      expect(enemy?.loot).toContain('poção');
    });

    it('removes enemies', () => {
      useProjectConfigStore.getState().addEnemy({ id: 'e1', name: 'Goblin', skill: 4, stamina: 5, description: '', loot: [], tags: [] });
      useProjectConfigStore.getState().removeEnemy('e1');
      expect(useProjectConfigStore.getState().enemies).toHaveLength(0);
    });
  });

  describe('rules', () => {
    it('default rules are set', () => {
      const rules = useProjectConfigStore.getState().rules;
      expect(rules.combatFormula).toBe('2d6+skill');
      expect(rules.damageFormula).toBe('2');
      expect(rules.luckTestFormula).toBe('2d6');
    });

    it('updates rules', () => {
      useProjectConfigStore.getState().updateRules({ combatFormula: '3d6+skill', damageFormula: '3' });
      expect(useProjectConfigStore.getState().rules.combatFormula).toBe('3d6+skill');
    });

    it('adds custom attributes', () => {
      useProjectConfigStore.getState().addCustomAttribute({ key: 'magic', label: 'Magia', dice: '2d6', min: 2, max: 12, description: 'Poder mágico' });
      expect(useProjectConfigStore.getState().rules.customAttributes).toHaveLength(1);
      expect(useProjectConfigStore.getState().rules.customAttributes[0].key).toBe('magic');
    });

    it('updates custom attribute', () => {
      useProjectConfigStore.getState().addCustomAttribute({ key: 'magic', label: 'Magia', dice: '2d6', min: 2, max: 12, description: '' });
      useProjectConfigStore.getState().updateCustomAttribute('magic', { min: 3, label: 'Poder Mágico' });
      const attr = useProjectConfigStore.getState().rules.customAttributes[0];
      expect(attr.min).toBe(3);
      expect(attr.label).toBe('Poder Mágico');
    });

    it('removes custom attribute', () => {
      useProjectConfigStore.getState().addCustomAttribute({ key: 'magic', label: 'Magia', dice: '2d6', min: 2, max: 12, description: '' });
      useProjectConfigStore.getState().removeCustomAttribute('magic');
      expect(useProjectConfigStore.getState().rules.customAttributes).toHaveLength(0);
    });

    it('resetConfig clears everything', () => {
      useProjectConfigStore.getState().addItem({ id: 'i1', name: 'Test', type: 'misc', description: '', effects: {}, tags: [] });
      useProjectConfigStore.getState().addEnemy({ id: 'e1', name: 'Test', skill: 1, stamina: 1, description: '', loot: [], tags: [] });
      useProjectConfigStore.getState().addCustomAttribute({ key: 'test', label: 'Test', dice: '1d6', min: 1, max: 6, description: '' });
      useProjectConfigStore.getState().resetConfig();
      expect(useProjectConfigStore.getState().items).toHaveLength(0);
      expect(useProjectConfigStore.getState().enemies).toHaveLength(0);
      expect(useProjectConfigStore.getState().rules.customAttributes).toHaveLength(0);
    });
  });

  describe('external assets', () => {
    it('adds and removes assets', () => {
      useProjectConfigStore.getState().addExternalAsset({ id: 'a1', name: 'bg.png', type: 'image', path: 'assets/bg.png', size: '1.2 MB', tags: ['background'] });
      expect(useProjectConfigStore.getState().externalAssets).toHaveLength(1);
      useProjectConfigStore.getState().removeExternalAsset('a1');
      expect(useProjectConfigStore.getState().externalAssets).toHaveLength(0);
    });
  });
});

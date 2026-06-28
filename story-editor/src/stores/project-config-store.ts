import { create } from 'zustand';
import type { ItemTemplate, EnemyTemplate, GameRules, CustomAttribute, ExternalAsset } from '../types/story';

const DEFAULT_RULES: GameRules = {
  combatFormula: '2d6+skill',
  damageFormula: '2',
  luckTestFormula: '2d6',
  customAttributes: [],
};

interface ProjectConfigState {
  items: ItemTemplate[];
  enemies: EnemyTemplate[];
  rules: GameRules;
  externalAssets: ExternalAsset[];

  addItem: (item: ItemTemplate) => void;
  updateItem: (id: string, data: Partial<ItemTemplate>) => void;
  removeItem: (id: string) => void;
  getItem: (id: string) => ItemTemplate | undefined;

  addEnemy: (enemy: EnemyTemplate) => void;
  updateEnemy: (id: string, data: Partial<EnemyTemplate>) => void;
  removeEnemy: (id: string) => void;
  getEnemy: (id: string) => EnemyTemplate | undefined;

  updateRules: (data: Partial<GameRules>) => void;
  addCustomAttribute: (attr: CustomAttribute) => void;
  updateCustomAttribute: (key: string, data: Partial<CustomAttribute>) => void;
  removeCustomAttribute: (key: string) => void;

  addExternalAsset: (asset: ExternalAsset) => void;
  removeExternalAsset: (id: string) => void;

  resetConfig: () => void;
}

export const useProjectConfigStore = create<ProjectConfigState>((set, get) => ({
  items: [],
  enemies: [],
  rules: { ...DEFAULT_RULES },
  externalAssets: [],

  addItem: (item) => set((s) => ({ items: [...s.items, item] })),
  updateItem: (id, data) => set((s) => ({
    items: s.items.map((i) => (i.id === id ? { ...i, ...data } : i)),
  })),
  removeItem: (id) => set((s) => ({ items: s.items.filter((i) => i.id !== id) })),
  getItem: (id) => get().items.find((i) => i.id === id),

  addEnemy: (enemy) => set((s) => ({ enemies: [...s.enemies, enemy] })),
  updateEnemy: (id, data) => set((s) => ({
    enemies: s.enemies.map((e) => (e.id === id ? { ...e, ...data } : e)),
  })),
  removeEnemy: (id) => set((s) => ({ enemies: s.enemies.filter((e) => e.id !== id) })),
  getEnemy: (id) => get().enemies.find((e) => e.id === id),

  updateRules: (data) => set((s) => ({ rules: { ...s.rules, ...data } })),
  addCustomAttribute: (attr) => set((s) => ({
    rules: { ...s.rules, customAttributes: [...s.rules.customAttributes, attr] },
  })),
  updateCustomAttribute: (key, data) => set((s) => ({
    rules: {
      ...s.rules,
      customAttributes: s.rules.customAttributes.map((a) => (a.key === key ? { ...a, ...data } : a)),
    },
  })),
  removeCustomAttribute: (key) => set((s) => ({
    rules: {
      ...s.rules,
      customAttributes: s.rules.customAttributes.filter((a) => a.key !== key),
    },
  })),

  addExternalAsset: (asset) => set((s) => ({ externalAssets: [...s.externalAssets, asset] })),
  removeExternalAsset: (id) => set((s) => ({ externalAssets: s.externalAssets.filter((a) => a.id !== id) })),

  resetConfig: () => set({ items: [], enemies: [], rules: { ...DEFAULT_RULES }, externalAssets: [] }),
}));

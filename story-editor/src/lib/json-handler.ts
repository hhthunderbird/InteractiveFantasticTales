import type { StoryData } from '../types/story';

export function exportStoryToJson(story: StoryData): string {
  return JSON.stringify(story, null, 2);
}

export function importStoryFromJson(json: string): StoryData {
  const data = JSON.parse(json) as StoryData;

  if (!data.formatVersion) throw new Error('Formato inválido: formatVersion ausente');
  if (!data.metadata) throw new Error('Formato inválido: metadata ausente');
  if (!data.sections) throw new Error('Formato inválido: sections ausente');

  return data;
}

export function downloadJson(story: StoryData, filename?: string) {
  const json = exportStoryToJson(story);
  const blob = new Blob([json], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename ?? `${story.metadata.id}.json`;
  a.click();
  URL.revokeObjectURL(url);
}

export function createEmptyStory(): StoryData {
  return {
    formatVersion: '1.0',
    metadata: {
      id: '',
      title: 'Nova Aventura',
      author: { name: '', email: '' },
      version: '0.1.0',
      language: 'pt-BR',
      genre: [],
      description: '',
      coverImage: '',
      startSection: 1,
      estimatedDuration: '',
      tags: [],
    },
    characterCreation: {
      attributes: {
        skill: { label: 'Habilidade', dice: '1d6+6', min: 7, max: 12 },
        stamina: { label: 'Vigor', dice: '2d6+12', min: 14, max: 24 },
        luck: { label: 'Sorte', dice: '1d6+6', min: 7, max: 12 },
      },
      startingGold: 0,
      startingItems: [],
      startingProvisions: 10,
    },
    flags: {},
    items: {},
    sections: {
      1: {
        id: 1,
        type: 'narrative',
        text: 'Sua aventura começa aqui! Escreva o texto da primeira cena.\n\nDica: use o botão "+ Nova Seção" na barra superior para criar mais trechos da história. Conecte-os definindo os números de destino nas escolhas de cada seção.',
        choices: [],
      },
    },
  };
}

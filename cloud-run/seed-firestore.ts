import * as admin from 'firebase-admin';
import { FieldValue } from 'firebase-admin/firestore';

if (admin.apps.length === 0) {
  const serviceAccount = require('../firebase-key.json');
  admin.initializeApp({
    credential: admin.credential.cert(serviceAccount),
  });
}

const db = admin.firestore();

// ── Stories (catálogo estendido — 15 histórias) ────────────────────

interface StoryData {
  metadata: {
    title: string;
    authorName: string;
    authorId: string;
    description: string;
    language: string;
    genre: string[];
    tags: string[];
    difficulty: string;
    estimatedDurationMinutes: number;
    isPublished: boolean;
    ageRating: string;
    formatVersion: string;
    publishedAt: Date;
    updatedAt: Date;
  };
  stats: {
    wordCount: number;
    sectionCount: number;
    endingCount: number;
    averageRating: number;
    ratingCount: number;
    purchaseCount: number;
    playCount: number;
    completionRate: number;
  };
  pricing: {
    type: string;
    priceTier: string;
    subscriptionRequired: boolean;
  };
  storage: {
    rootPath: string;
    thumbnailSmallPath: string;
    thumbnailLargePath: string;
    sha256Hash: string;
  };
}

const stories: Array<StoryData & { id: string }> = [
  // ── FREE ──────────────────────────────────────────────
  {
    id: 'demo',
    metadata: {
      title: 'O Labirinto do Arquimago',
      authorName: 'Helena Verne',
      authorId: 'seed-author-helena',
      description: 'Um jovem aprendiz de mago deve atravessar o labirinto vivo de um arquimago recluso. Cada corredor muda de forma, e cada sala esconde um desafio arcano diferente.',
      language: 'pt-BR',
      genre: ['fantasia', 'aventura'],
      tags: ['fantasia', 'masmorra', 'mago', 'iniciante'],
      difficulty: 'beginner',
      estimatedDurationMinutes: 45,
      isPublished: true,
      ageRating: 'everyone',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-03-15'),
      updatedAt: new Date('2026-06-01'),
    },
    stats: { wordCount: 12000, sectionCount: 13, endingCount: 3, averageRating: 4.2, ratingCount: 156, purchaseCount: 0, playCount: 892, completionRate: 0.54 },
    pricing: { type: 'free', priceTier: 'free', subscriptionRequired: false },
    storage: { rootPath: 'stories/demo/', thumbnailSmallPath: 'thumbnails/stories/demo_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/demo_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'star-portal',
    metadata: {
      title: 'O Portal das Estrelas',
      authorName: 'Lucas Nogueira',
      authorId: 'seed-author-lucas',
      description: 'Um portal interestelar é descoberto nas ruínas de uma civilização antiga. Explore mundos desconhecidos e faça alianças com espécies alienígenas.',
      language: 'pt-BR',
      genre: ['ficcao-cientifica', 'aventura'],
      tags: ['ficcao-cientifica', 'portal', 'exploracao', 'aliens'],
      difficulty: 'beginner',
      estimatedDurationMinutes: 80,
      isPublished: true,
      ageRating: 'everyone',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-15'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 16000, sectionCount: 50, endingCount: 4, averageRating: 4.0, ratingCount: 67, purchaseCount: 0, playCount: 445, completionRate: 0.62 },
    pricing: { type: 'free', priceTier: 'free', subscriptionRequired: false },
    storage: { rootPath: 'stories/star-portal/', thumbnailSmallPath: 'thumbnails/stories/star_portal_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/star_portal_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'demo-en',
    metadata: {
      title: 'The Archmage\'s Labyrinth',
      authorName: 'Helena Verne',
      authorId: 'seed-author-helena',
      description: 'A young wizard apprentice must navigate the living labyrinth of a reclusive archmage. Each corridor shifts, each room hides a different arcane challenge.',
      language: 'en',
      genre: ['fantasy', 'adventure'],
      tags: ['fantasy', 'dungeon', 'wizard', 'beginner'],
      difficulty: 'beginner',
      estimatedDurationMinutes: 45,
      isPublished: true,
      ageRating: 'everyone',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-20'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 12000, sectionCount: 13, endingCount: 3, averageRating: 4.5, ratingCount: 34, purchaseCount: 0, playCount: 120, completionRate: 0.51 },
    pricing: { type: 'free', priceTier: 'free', subscriptionRequired: false },
    storage: { rootPath: 'stories/demo-en/', thumbnailSmallPath: 'thumbnails/stories/demo_en_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/demo_en_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  // ── TIER 1 (R$ 4,99) ──────────────────────────────────
  {
    id: 'forest-of-doom',
    metadata: {
      title: 'Floresta da Perdição',
      authorName: 'Camila Soares',
      authorId: 'seed-author-camila',
      description: 'Uma floresta amaldiçoada esconde o segredo de uma civilização perdida. Criaturas sombrias, árvores que sussurram e rios que encantam.',
      language: 'pt-BR',
      genre: ['fantasia', 'aventura'],
      tags: ['fantasia', 'floresta', 'aventura', 'maldicao'],
      difficulty: 'beginner',
      estimatedDurationMinutes: 130,
      isPublished: true,
      ageRating: 'teen',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-04-20'),
      updatedAt: new Date('2026-05-10'),
    },
    stats: { wordCount: 28000, sectionCount: 85, endingCount: 5, averageRating: 4.1, ratingCount: 98, purchaseCount: 134, playCount: 320, completionRate: 0.45 },
    pricing: { type: 'priced', priceTier: 'tier_1', subscriptionRequired: false },
    storage: { rootPath: 'stories/forest-of-doom/', thumbnailSmallPath: 'thumbnails/stories/forest_of_doom_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/forest_of_doom_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'pirate-shores',
    metadata: {
      title: 'As Costas do Pirata',
      authorName: 'Diego Almeida',
      authorId: 'seed-author-diego',
      description: 'Navegue pelos mares do Caribe em busca do tesouro perdido do Capitão Barbarossa. Enfrente tempestades, motins e criaturas marinhas.',
      language: 'pt-BR',
      genre: ['aventura'],
      tags: ['pirata', 'mar', 'tesouro', 'navio'],
      difficulty: 'beginner',
      estimatedDurationMinutes: 100,
      isPublished: true,
      ageRating: 'teen',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-05-01'),
      updatedAt: new Date('2026-06-10'),
    },
    stats: { wordCount: 22000, sectionCount: 65, endingCount: 4, averageRating: 4.3, ratingCount: 47, purchaseCount: 78, playCount: 190, completionRate: 0.58 },
    pricing: { type: 'priced', priceTier: 'tier_1', subscriptionRequired: false },
    storage: { rootPath: 'stories/pirate-shores/', thumbnailSmallPath: 'thumbnails/stories/pirate_shores_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/pirate_shores_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  // ── TIER 2 (R$ 9,99) ──────────────────────────────────
  {
    id: 'mountain-of-fire',
    metadata: {
      title: 'A Montanha do Mago de Fogo',
      authorName: 'Rafael Torres',
      authorId: 'seed-author-rafael',
      description: 'Nas profundezas da Montanha de Cinzas, um mago de fogo forja um exército de dragões para conquistar os reinos livres. Você é o único que pode detê-lo.',
      language: 'pt-BR',
      genre: ['fantasia', 'aventura'],
      tags: ['fantasia', 'aventura', 'dragao', 'epico'],
      difficulty: 'intermediate',
      estimatedDurationMinutes: 210,
      isPublished: true,
      ageRating: 'teen',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-04-01'),
      updatedAt: new Date('2026-06-15'),
    },
    stats: { wordCount: 48000, sectionCount: 142, endingCount: 8, averageRating: 4.7, ratingCount: 342, purchaseCount: 256, playCount: 580, completionRate: 0.31 },
    pricing: { type: 'priced', priceTier: 'tier_2', subscriptionRequired: false },
    storage: { rootPath: 'stories/mountain-of-fire/', thumbnailSmallPath: 'thumbnails/stories/mountain_of_fire_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/mountain_of_fire_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'vampire-crypts',
    metadata: {
      title: 'As Criptas do Vampiro',
      authorName: 'Isabela Dantas',
      authorId: 'seed-author-isabela',
      description: 'Um castelo gótico esconde criptas ancestrais onde um vampiro milenar dorme. Explore as catacumbas e sobreviva aos horrores da lua cheia.',
      language: 'pt-BR',
      genre: ['horror'],
      tags: ['horror', 'vampiro', 'gotico', 'terror'],
      difficulty: 'intermediate',
      estimatedDurationMinutes: 170,
      isPublished: true,
      ageRating: 'teen',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-01'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 38000, sectionCount: 110, endingCount: 7, averageRating: 4.5, ratingCount: 203, purchaseCount: 189, playCount: 410, completionRate: 0.37 },
    pricing: { type: 'priced', priceTier: 'tier_2', subscriptionRequired: false },
    storage: { rootPath: 'stories/vampire-crypts/', thumbnailSmallPath: 'thumbnails/stories/vampire_crypts_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/vampire_crypts_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'samurai-honor',
    metadata: {
      title: 'A Honra do Samurai',
      authorName: 'Takeshi Yamamoto',
      authorId: 'seed-author-takeshi',
      description: 'No Japão feudal, um samurai desonrado busca redenção. Suas escolhas entre o código bushido e a sobrevivência definem seu destino.',
      language: 'pt-BR',
      genre: ['aventura'],
      tags: ['samurai', 'japao', 'honra', 'acao'],
      difficulty: 'intermediate',
      estimatedDurationMinutes: 190,
      isPublished: true,
      ageRating: 'mature',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-05-15'),
      updatedAt: new Date('2026-06-20'),
    },
    stats: { wordCount: 42000, sectionCount: 125, endingCount: 9, averageRating: 4.6, ratingCount: 112, purchaseCount: 145, playCount: 310, completionRate: 0.28 },
    pricing: { type: 'priced', priceTier: 'tier_2', subscriptionRequired: false },
    storage: { rootPath: 'stories/samurai-honor/', thumbnailSmallPath: 'thumbnails/stories/samurai_honor_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/samurai_honor_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  // ── TIER 3 (R$ 14,99) ──────────────────────────────────
  {
    id: 'city-of-thieves',
    metadata: {
      title: 'Cidade dos Ladrões',
      authorName: 'Vitor Marques',
      authorId: 'seed-author-vitor',
      description: 'Nas ruas nebulosas de Ferropolis, guildas de ladrões disputam o controle da cidade a vapor. Um detetive infiltrado precisa desvendar uma conspiração.',
      language: 'pt-BR',
      genre: ['steampunk', 'misterio'],
      tags: ['steampunk', 'crime', 'misterio', 'detetive'],
      difficulty: 'advanced',
      estimatedDurationMinutes: 300,
      isPublished: true,
      ageRating: 'mature',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-05-01'),
      updatedAt: new Date('2026-06-20'),
    },
    stats: { wordCount: 72000, sectionCount: 200, endingCount: 12, averageRating: 4.9, ratingCount: 521, purchaseCount: 312, playCount: 670, completionRate: 0.22 },
    pricing: { type: 'priced', priceTier: 'tier_3', subscriptionRequired: false },
    storage: { rootPath: 'stories/city-of-thieves/', thumbnailSmallPath: 'thumbnails/stories/city_of_thieves_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/city_of_thieves_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'colony-mars',
    metadata: {
      title: 'Colônia Marte 2147',
      authorName: 'Ana Ribeiro',
      authorId: 'seed-author-ana',
      description: 'A primeira colônia humana em Marte enfrenta uma crise existencial. Como comandante, suas decisões determinarão a sobrevivência de 500 colonos.',
      language: 'pt-BR',
      genre: ['ficcao-cientifica'],
      tags: ['marte', 'sobrevivencia', 'colonia', 'dilema'],
      difficulty: 'advanced',
      estimatedDurationMinutes: 280,
      isPublished: true,
      ageRating: 'mature',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-10'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 65000, sectionCount: 185, endingCount: 10, averageRating: 4.8, ratingCount: 89, purchaseCount: 98, playCount: 230, completionRate: 0.19 },
    pricing: { type: 'priced', priceTier: 'tier_3', subscriptionRequired: false },
    storage: { rootPath: 'stories/colony-mars/', thumbnailSmallPath: 'thumbnails/stories/colony_mars_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/colony_mars_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  // ── TIER 4 (R$ 19,99) ──────────────────────────────────
  {
    id: 'eldritch-academy',
    metadata: {
      title: 'Academia Eldritch',
      authorName: 'Fernanda Castro',
      authorId: 'seed-author-fernanda',
      description: 'Em uma academia secreta de ocultismo, você descobre que nem todos os professores são humanos. Mistério lovecraftiano com múltiplas linhas temporais.',
      language: 'pt-BR',
      genre: ['horror', 'misterio'],
      tags: ['horror', 'lovecraft', 'academia', 'misterio'],
      difficulty: 'advanced',
      estimatedDurationMinutes: 360,
      isPublished: true,
      ageRating: 'mature',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-25'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 90000, sectionCount: 280, endingCount: 15, averageRating: 4.9, ratingCount: 56, purchaseCount: 67, playCount: 140, completionRate: 0.12 },
    pricing: { type: 'priced', priceTier: 'tier_4', subscriptionRequired: false },
    storage: { rootPath: 'stories/eldritch-academy/', thumbnailSmallPath: 'thumbnails/stories/eldritch_academy_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/eldritch_academy_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'en-enchanted-forest',
    metadata: {
      title: 'The Enchanted Forest',
      authorName: 'Emily Clarke',
      authorId: 'seed-author-emily',
      description: 'A dark fairy tale where every wish comes with a price. Navigate a forest where trees remember your name and animals speak in riddles.',
      language: 'en',
      genre: ['fantasy', 'adventure'],
      tags: ['fairy-tale', 'dark', 'magic', 'riddles'],
      difficulty: 'intermediate',
      estimatedDurationMinutes: 220,
      isPublished: true,
      ageRating: 'teen',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-18'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 50000, sectionCount: 150, endingCount: 8, averageRating: 4.4, ratingCount: 41, purchaseCount: 23, playCount: 85, completionRate: 0.25 },
    pricing: { type: 'priced', priceTier: 'tier_4', subscriptionRequired: false },
    storage: { rootPath: 'stories/en-enchanted-forest/', thumbnailSmallPath: 'thumbnails/stories/en/enchanted_forest_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/en/enchanted_forest_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  // ── TIER 5 (R$ 29,99) ──────────────────────────────────
  {
    id: 'solaris-chronicles',
    metadata: {
      title: 'Crônicas de Solaris',
      authorName: 'Rafael Torres',
      authorId: 'seed-author-rafael',
      description: 'Uma saga épica em três atos através do império de Solaris. Guerras, intrigas políticas e magia ancestral em uma narrativa de mais de 500 seções.',
      language: 'pt-BR',
      genre: ['fantasia', 'aventura'],
      tags: ['epico', 'fantasia', 'imperio', 'guerra'],
      difficulty: 'advanced',
      estimatedDurationMinutes: 600,
      isPublished: true,
      ageRating: 'mature',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-28'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 150000, sectionCount: 510, endingCount: 22, averageRating: 4.9, ratingCount: 28, purchaseCount: 34, playCount: 52, completionRate: 0.04 },
    pricing: { type: 'priced', priceTier: 'tier_5', subscriptionRequired: false },
    storage: { rootPath: 'stories/solaris-chronicles/', thumbnailSmallPath: 'thumbnails/stories/solaris_chronicles_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/solaris_chronicles_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
  {
    id: 'es-portal-estrellas',
    metadata: {
      title: 'El Portal de las Estrellas',
      authorName: 'María García',
      authorId: 'seed-author-maria',
      description: 'Un portal interestelar descubierto en las ruinas de una civilización antigua. Explora mundos desconocidos y descubre el misterio del Portal.',
      language: 'es',
      genre: ['ficcao-cientifica', 'aventura'],
      tags: ['ciencia-ficcion', 'portal', 'exploracion', 'aliens'],
      difficulty: 'beginner',
      estimatedDurationMinutes: 80,
      isPublished: true,
      ageRating: 'everyone',
      formatVersion: '1.0.0',
      publishedAt: new Date('2026-06-22'),
      updatedAt: new Date('2026-06-28'),
    },
    stats: { wordCount: 16000, sectionCount: 50, endingCount: 4, averageRating: 4.3, ratingCount: 19, purchaseCount: 12, playCount: 48, completionRate: 0.60 },
    pricing: { type: 'priced', priceTier: 'tier_5', subscriptionRequired: false },
    storage: { rootPath: 'stories/es-portal-estrellas/', thumbnailSmallPath: 'thumbnails/stories/es/portal_estrellas_cover_128.webp', thumbnailLargePath: 'thumbnails/stories/es/portal_estrellas_cover_512.webp', sha256Hash: '0'.repeat(64) },
  },
];

// ── User Data (2 mock users with progress + ratings) ────────────────

const mockUsers = [
  {
    userId: 'mock_user_ftales_dev1',
    profile: {
      displayName: 'Aventureiro Teste',
      createdAt: admin.firestore.Timestamp.fromDate(new Date('2026-04-01')),
      ownedStoryIds: ['demo', 'star-portal', 'forest-of-doom', 'mountain-of-fire', 'city-of-thieves'],
      accessibilityPrefs: { fontSize: 'medium', highContrast: false, dyslexicFont: false },
    },
    progress: [
      { storyId: 'demo', owned: true, lastPlayedAt: new Date('2026-06-30'), lastSectionId: 7, completionPercent: 0.54, hasActiveGame: false, sessionsPlayed: 3, totalPlaytimeSeconds: 2400, sectionsVisited: 9, uniqueSectionsVisited: 7, combatsWon: 2, combatsLost: 1, rewindsUsed: 4, endingsFoundCount: 1 },
      { storyId: 'star-portal', owned: true, lastPlayedAt: new Date('2026-06-15'), lastSectionId: 2, completionPercent: 0.15, hasActiveGame: true, activeSaveSlot: 0, sessionsPlayed: 1, totalPlaytimeSeconds: 600, sectionsVisited: 3 },
      { storyId: 'forest-of-doom', owned: true, lastPlayedAt: new Date('2026-05-20'), lastSectionId: 45, completionPercent: 0.72, hasActiveGame: false, sessionsPlayed: 4, totalPlaytimeSeconds: 5400, sectionsVisited: 52, combatsWon: 5, combatsLost: 2, rewindsUsed: 3, endingsFoundCount: 2 },
      { storyId: 'mountain-of-fire', owned: true, lastPlayedAt: new Date('2026-06-28'), lastSectionId: 120, completionPercent: 0.85, hasActiveGame: true, activeSaveSlot: 1, sessionsPlayed: 8, totalPlaytimeSeconds: 14400, sectionsVisited: 115, combatsWon: 14, combatsLost: 4, rewindsUsed: 12, endingsFoundCount: 5 },
      { storyId: 'city-of-thieves', owned: true, lastPlayedAt: new Date('2026-06-25'), lastSectionId: 50, completionPercent: 0.25, hasActiveGame: false, sessionsPlayed: 2, totalPlaytimeSeconds: 3600, sectionsVisited: 48 },
      { storyId: 'demo-en', owned: true, lastPlayedAt: new Date('2026-06-10'), lastSectionId: 10, completionPercent: 0.80, hasActiveGame: false, sessionsPlayed: 1, totalPlaytimeSeconds: 1800 },
    ],
    ratings: [
      { storyId: 'demo', score: 4, review: 'Ótima introdução ao sistema! Ideal para iniciantes.', createdAt: new Date('2026-04-10') },
      { storyId: 'mountain-of-fire', score: 5, review: 'Épico! As escolhas realmente importam. Já joguei 3 vezes e cada run foi diferente.', createdAt: new Date('2026-05-05') },
      { storyId: 'forest-of-doom', score: 4, review: 'Muito atmosférica. A floresta parece viva.', createdAt: new Date('2026-05-22') },
    ],
  },
  {
    userId: 'mock_user_ftales_dev2',
    profile: {
      displayName: 'Leitora Voraz',
      createdAt: admin.firestore.Timestamp.fromDate(new Date('2026-05-15')),
      ownedStoryIds: ['demo', 'vampire-crypts', 'samurai-honor', 'eldritch-academy'],
      accessibilityPrefs: { fontSize: 'large', highContrast: true, dyslexicFont: true },
    },
    progress: [
      { storyId: 'demo', owned: true, lastPlayedAt: new Date('2026-05-20'), lastSectionId: 13, completionPercent: 1.0, hasActiveGame: false, sessionsPlayed: 1, totalPlaytimeSeconds: 2700, sectionsVisited: 13, endingsFoundCount: 1 },
      { storyId: 'vampire-crypts', owned: true, lastPlayedAt: new Date('2026-06-28'), lastSectionId: 55, completionPercent: 0.50, hasActiveGame: true, activeSaveSlot: 0, sessionsPlayed: 3, totalPlaytimeSeconds: 5100, sectionsVisited: 52, combatsWon: 4, combatLost: 3, rewindsUsed: 5, endingsFoundCount: 2 },
      { storyId: 'samurai-honor', owned: true, lastPlayedAt: new Date('2026-06-20'), lastSectionId: 80, completionPercent: 0.64, hasActiveGame: false, sessionsPlayed: 5, totalPlaytimeSeconds: 9000, sectionsVisited: 78, combatsWon: 9, combatsLost: 3, rewindsUsed: 8, endingsFoundCount: 4 },
      { storyId: 'eldritch-academy', owned: true, lastPlayedAt: new Date('2026-06-28'), lastSectionId: 15, completionPercent: 0.05, hasActiveGame: true, activeSaveSlot: 1, sessionsPlayed: 1, totalPlaytimeSeconds: 1200, sectionsVisited: 14 },
    ],
    ratings: [
      { storyId: 'demo', score: 4, review: 'Bem legal para começar!', createdAt: new Date('2026-05-21') },
      { storyId: 'vampire-crypts', score: 5, review: 'Arrepiante! A atmosfera gótica é incrível. As ilustrações dão medo de verdade.', createdAt: new Date('2026-06-05') },
      { storyId: 'samurai-honor', score: 4, review: 'Sistema de honra muito bem implementado. Escolhas difíceis.', createdAt: new Date('2026-06-22') },
      { storyId: 'mountain-of-fire', score: 5, review: 'Melhor gamebook que já joguei. 142 seções de pura aventura!', createdAt: new Date('2026-06-15') },
    ],
  },
];

// ── Run Seeder ─────────────────────────────────────────────

async function seed(): Promise<void> {
  console.log(`[Seed] Semeando ${stories.length} histórias...`);

  // Stories
  const storyBatch = db.batch();
  for (const story of stories) {
    const ref = db.collection('stories').doc(story.id);
    storyBatch.set(ref, {
      ...story,
      metadata: {
        ...story.metadata,
        publishedAt: admin.firestore.Timestamp.fromDate(story.metadata.publishedAt),
        updatedAt: admin.firestore.Timestamp.fromDate(story.metadata.updatedAt),
      },
    }, { merge: true });
    console.log(`  + ${story.id}: ${story.metadata.title} [${story.metadata.language}]`);
  }
  await storyBatch.commit();

  // Users + progress + ratings
  for (const user of mockUsers) {
    console.log(`\n[Seed] User: ${user.userId}`);

    // Profile
    await db.collection('users').doc(user.userId).set({
      ...user.profile,
      updatedAt: FieldValue.serverTimestamp(),
    }, { merge: true });
    console.log(`  + profile: ${user.profile.displayName}`);

    // Progress
    for (const progress of user.progress) {
      await db.collection('users').doc(user.userId)
        .collection('storyProgress').doc(progress.storyId)
        .set({
          ...progress,
          lastPlayedAt: admin.firestore.Timestamp.fromDate(progress.lastPlayedAt),
          purchasedAt: admin.firestore.Timestamp.fromDate(new Date('2026-04-01')),
        }, { merge: true });
      console.log(`  + progress: ${progress.storyId} (${Math.round(progress.completionPercent * 100)}%)`);
    }

    // Ratings
    for (const rating of user.ratings) {
      await db.collection('stories').doc(rating.storyId)
        .collection('ratings').doc(user.userId)
        .set({
          userId: user.userId,
          score: rating.score,
          review: rating.review,
          createdAt: admin.firestore.Timestamp.fromDate(rating.createdAt),
        });
      console.log(`  + rating: ${rating.storyId} → ${rating.score}★`);
    }
  }

  console.log(`\n[Seed] Concluído! ${stories.length} histórias, ${mockUsers.length} usuários mock.`);
}

seed().catch(err => {
  console.error('[Seed] Erro:', err);
  process.exit(1);
});

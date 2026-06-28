export type SectionType = 'narrative' | 'combat' | 'test' | 'itemGate' | 'random' | 'ending';

export const SECTION_TYPE_LABELS: Record<SectionType, string> = {
  narrative: 'Narrativa',
  combat: 'Combate',
  test: 'Teste',
  itemGate: 'Item Gate',
  random: 'Aleatório',
  ending: 'Final',
};

export const SECTION_TYPE_COLORS: Record<SectionType, string> = {
  narrative: '#3b82f6',
  combat: '#ef4444',
  test: '#10b981',
  itemGate: '#f59e0b',
  random: '#8b5cf6',
  ending: '#6b7280',
};

export const SECTION_TYPE_ICONS: Record<SectionType, string> = {
  narrative: '📖',
  combat: '⚔️',
  test: '🎲',
  itemGate: '🔑',
  random: '🔀',
  ending: '🏁',
};

export interface StoryMetadata {
  id: string;
  title: string;
  author: { name: string; email: string };
  version: string;
  language: string;
  genre: string[];
  description: string;
  coverImage: string;
  startSection: number;
  estimatedDuration: string;
  tags: string[];
}

export interface CharacterCreationData {
  attributes: Record<string, AttributeDef>;
  startingGold: number;
  startingItems: string[];
  startingProvisions: number;
}

export interface AttributeDef {
  label: string;
  dice: string;
  min: number;
  max: number;
}

export interface FlagDefinition {
  type: 'boolean' | 'counter';
  default: boolean | number;
}

export interface ItemDefinition {
  type: string;
  description: string;
  effects?: Record<string, number>;
}

export interface ChoiceData {
  id?: string;
  text: string;
  targetSection: number;
  conditions: ConditionData[];
}

export interface ConditionData {
  type: 'hasItem' | 'hasFlag' | 'skill' | 'stamina' | 'luck' | 'gold' | 'counter';
  key: string;
  op: '==' | '!=' | '>=' | '<=' | '>' | '<';
  value: number | string;
}

export interface CombatData {
  enemyName: string;
  enemySkill: number;
  enemyStamina: number;
  victoryTarget: number;
  defeatTarget: number;
  fleeTarget: number;
  allowFlee: boolean;
  lootOnVictory: string[];
}

export interface TestData {
  attribute: 'skill' | 'luck' | 'custom';
  difficulty: number;
  successTarget: number;
  failTarget: number;
}

export interface ItemGateData {
  item: string;
  hasItemTarget: number;
  noItemTarget: number;
}

export interface RandomData {
  outcomes: { targetSection: number; weight: number }[];
}

export interface EndingData {
  type: 'victory' | 'defeat' | 'neutral';
}

export interface OnEnterData {
  addItems?: string[];
  removeItems?: string[];
  setFlags?: Record<string, boolean | number>;
  modifyGold?: number;
  modifyStamina?: number;
  modifyLuck?: number;
}

export interface PresentationData {
  illustration?: string;
  ambientSound?: string;
  narration?: NarrationConfig;
  haptics?: HapticsConfig;
}

export interface NarrationConfig {
  voice: string;
  speed: number;
  emphasis: string[];
}

export interface HapticPattern {
  type: 'light' | 'medium' | 'heavy' | 'double' | 'rhythm';
  intensity: number;
  duration: number;
}

export interface HapticsConfig {
  onEnter?: HapticPattern;
  onCombat?: HapticPattern;
  onDiscovery?: HapticPattern;
  onDanger?: HapticPattern;
}

export interface SectionData {
  id: number;
  type: SectionType;
  text: string;
  choices?: ChoiceData[];
  combat?: CombatData;
  test?: TestData;
  itemGate?: ItemGateData;
  random?: RandomData;
  ending?: EndingData;
  onEnter?: OnEnterData;
  presentation?: PresentationData;
}

export interface StoryData {
  formatVersion: string;
  metadata: StoryMetadata;
  characterCreation: CharacterCreationData;
  flags: Record<string, FlagDefinition>;
  items: Record<string, ItemDefinition>;
  sections: Record<number, SectionData>;
}

export interface AuditIssue {
  severity: 'error' | 'warning' | 'info';
  code: string;
  message: string;
  sectionId: number;
  suggestion?: string;
}

export interface FlowStats {
  totalSections: number;
  narrativeCount: number;
  combatCount: number;
  testCount: number;
  endingCount: number;
  victoryEndings: number;
  defeatEndings: number;
  maxDepth: number;
  totalChoices: number;
  randomCount: number;
  itemGateCount: number;
  unreferencedCount: number;
  totalItems: number;
  totalFlags: number;
}

export interface NodeStyle {
  id: string;
  name: string;
  color: string;
  borderColor: string;
  borderWidth: number;
  borderStyle: 'solid' | 'dashed' | 'dotted';
  backgroundColor: string;
  fontSize: 'small' | 'medium' | 'large';
  iconBadge?: string;
  locked?: boolean;
}

export interface NodeGroup {
  id: string;
  title: string;
  color: string;
  borderStyle: 'solid' | 'dashed' | 'dotted';
  borderWidth: number;
  visible: boolean;
  nodeIds: number[];
  collapsed: boolean;
}

export interface SectionStyleMap {
  sectionStyles: Record<number, string>;
}

export interface StyleLibrary {
  styles: NodeStyle[];
  groups: NodeGroup[];
  sectionStyleMap: Record<number, string>;
}

export const DEFAULT_NODE_STYLE: NodeStyle = {
  id: '__default__',
  name: 'Padrão',
  color: '#a0a0b0',
  borderColor: 'transparent',
  borderWidth: 2,
  borderStyle: 'solid',
  backgroundColor: '',
  fontSize: 'medium',
  locked: false,
};

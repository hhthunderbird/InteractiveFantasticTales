# Interactive Fantastic Tales — Arquitetura Técnica (v2)

> Stack: Unity (Player) + React/TypeScript (Editor Web) + Google Cloud (Backend)

---

## 1. Visão Geral da Arquitetura

```
┌──────────────────────────────────────────────────────────┐
│                    Google Cloud                          │
│  ┌─────────────┐  ┌────────────┐  ┌──────────────────┐  │
│  │ Cloud Run   │  │ Firestore  │  │ Cloud Storage    │  │
│  │ (Editor API)│  │ (DB Tempo  │  │ (Assets: imagens,│  │
│  │             │  │  Real)     │  │  sons, histórias)│  │
│  └──────┬──────┘  └─────┬──────┘  └────────┬─────────┘  │
│         │               │                  │             │
│  ┌──────┴───────────────┴──────────────────┴──────────┐  │
│  │              Firebase Auth (Google Identity)        │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
           │                          │
           ▼                          ▼
┌─────────────────────┐    ┌─────────────────────┐
│   Story Editor      │    │   Player App        │
│   (React SPA)       │    │   (Unity C#)        │
│                     │    │                     │
│   editor.ift.com    │    │   PC + Mobile       │
│   Web Browser       │    │   Windows/Mac/Linux │
│                     │    │   Android / iOS     │
└─────────────────────┘    └─────────────────────┘
           │                          │
           └──────────┬───────────────┘
                      ▼
              ┌───────────────┐
              │ story.json    │
              │ (Formato      │
              │  Padronizado) │
              └───────────────┘
```

---

## 2. Player App — Unity (C#)

### 2.1 Estrutura de Diretórios

```
Assets/
├── _Game/
│   ├── Core/
│   │   ├── GameEngine.cs           # Máquina de estados principal
│   │   ├── StoryLoader.cs          # Carrega JSON → StoryData
│   │   ├── SectionProcessor.cs     # Processa seção atual → ações
│   │   ├── DiceSystem.cs           # Rolagem de dados (2d6, 1d6, etc.)
│   │   ├── ConditionEvaluator.cs   # Avalia condições (hasItem, hasFlag, etc.)
│   │   └── Character.cs            # Modelo do personagem + inventário
│   ├── UI/
│   │   ├── StoryPanel.cs           # Painel de texto narrativo
│   │   ├── ChoiceButtons.cs        # Botões de escolha (1-4)
│   │   ├── CharacterSheetUI.cs     # Ficha do personagem (overlay)
│   │   ├── InventoryUI.cs          # Inventário (overlay)
│   │   ├── CombatUI.cs             # Interface de combate
│   │   ├── MapOverlay.cs           # Mapa de exploração
│   │   ├── TimelineUI.cs           # Linha do tempo (rewind)
│   │   ├── BookmarkUI.cs           # Gerenciador de bookmarks
│   │   └── AccessibilitySettings.cs # Painel de config de acessibilidade
│   ├── Audio/
│   │   ├── NarrationManager.cs     # TTS engine wrapper
│   │   ├── SFXManager.cs           # Efeitos sonoros
│   │   └── MusicManager.cs         # Trilha sonora adaptativa
│   ├── Input/
│   │   ├── SwipeHandler.cs         # Gestos swipe (4 direções)
│   │   ├── VoiceInputManager.cs    # Comandos de voz
│   │   ├── KeyboardHandler.cs      # Atalhos de teclado
│   │   └── ScreenReaderBridge.cs   # Integração com screen reader nativo
│   ├── Haptics/
│   │   └── HapticManager.cs        # Vibração (mobile) + padrões
│   └── Save/
│       ├── SaveManager.cs          # Save/Load local
│       ├── BookmarkSystem.cs       # Bookmarks
│       └── CloudSaveClient.cs      # Sincronização cloud
│
├── _Data/
│   └── Stories/                    # *.json de histórias
│
└── _Shared/
    └── Models/
        ├── StoryData.cs
        ├── SectionData.cs
        ├── ChoiceData.cs
        ├── ConditionData.cs
        └── PresentationData.cs
```

### 2.2 Modelos de Dados Core (C#)

```csharp
// --- StoryData.cs ---
[Serializable]
public class StoryData
{
    public string formatVersion;
    public StoryMetadata metadata;
    public CharacterCreationData characterCreation;
    public Dictionary<string, FlagDefinition> flags;
    public Dictionary<string, ItemDefinition> items;
    public Dictionary<string, SectionData> sections;
}

// --- SectionData.cs ---
public enum SectionType { Narrative, Combat, Test, ItemGate, Random, Ending }

[Serializable]
public class SectionData
{
    public int id;
    public SectionType type;
    public string text;
    public ChoiceData[] choices;
    public CombatData combat;
    public TestData test;
    public ItemGateData itemGate;
    public RandomData random;
    public EndingData ending;
    public OnEnterData onEnter;
    public PresentationData presentation;
}

// --- ChoiceData.cs ---
[Serializable]
public class ChoiceData
{
    public string text;
    public int targetSection;
    public ConditionData[] conditions;
}

// --- CombatData.cs ---
[Serializable]
public class CombatData
{
    public string enemyName;
    public int enemySkill;
    public int enemyStamina;
    public int victoryTarget;
    public int defeatTarget;
    public int fleeTarget;
    public bool allowFlee = true;
    public string[] lootOnVictory;
}

// --- Character.cs ---
[Serializable]
public class Character
{
    public int skill, stamina, luck;
    public int maxStamina, maxLuck;
    public int gold, provisions;
    public List<string> inventory;
    public HashSet<string> flags;
    public Dictionary<string, int> counters;
    
    public void ModifyStamina(int delta) { ... }
    public void ModifyLuck(int delta) { ... }
    public bool TestSkill(int difficulty) { ... }
    public bool TestLuck() { ... }
    public bool HasItem(string item) { ... }
    public bool HasFlag(string flag) { ... }
    public int GetCounter(string key) { ... }
}
```

### 2.3 GameEngine (Máquina de Estados)

```csharp
public enum GameState 
{ 
    Loading, Narrative, ChoicePending, CombatActive, 
    TestActive, ItemGateCheck, Transitioning, GameOver, Paused 
}

public class GameEngine : MonoBehaviour
{
    public GameState CurrentState { get; private set; }
    
    private StoryData story;
    private SectionData currentSection;
    private Character player;
    private Stack<GameSnapshot> timeline; // Para rewind
    
    public void LoadStory(string jsonPath);
    public void GoToSection(int sectionId);
    public void MakeChoice(int choiceIndex);
    public void RewindTo(int timelineIndex);
    
    public event Action<SectionData> OnSectionChanged;
    public event Action<Character> OnCharacterUpdated;
    public event Action<GameState> OnStateChanged;
}
```

### 2.4 NarrationManager (TTS)

```csharp
public class NarrationManager : MonoBehaviour
{
    public bool AutoNarrate { get; set; } = true;
    public float Speed { get; set; } = 1f;
    public string Voice { get; set; } = "default";
    
    public void Speak(string text, string voice = null, float? speed = null);
    public void Stop();
    public void Pause();
    public void Resume();
    
    public event Action OnNarrationStart;
    public event Action OnNarrationComplete;
    public event Action<string> OnWordSpoken; // Para highlight de texto sincronizado
}
```

### 2.5 VoiceInputManager

```csharp
public class VoiceInputManager : MonoBehaviour
{
    private Dictionary<string, VoiceAction> commands = new()
    {
        { "escolher um|opção um|primeira", VoiceAction.Select1 },
        { "escolher dois|opção dois|segunda", VoiceAction.Select2 },
        { "escolher três|opção três|terceira", VoiceAction.Select3 },
        { "escolher quatro|opção quatro|quarta", VoiceAction.Select4 },
        { "repetir|ler de novo", VoiceAction.Repeat },
        { "voltar|retornar", VoiceAction.GoBack },
        { "ficha|status|personagem", VoiceAction.ShowCharacter },
        { "inventário|itens|mochila", VoiceAction.ShowInventory },
        { "mapa", VoiceAction.ShowMap },
        { "pausar|parar", VoiceAction.Pause },
        { "continuar|seguir", VoiceAction.Resume }
    };
    
    public event Action<VoiceAction> OnCommandRecognized;
}
```

---

## 3. Story Editor — React SPA (Web)

### 3.1 Stack Tecnológica

```
Frontend:  React 18 + TypeScript + Vite
Grafo:     ReactFlow (nós + arestas customizáveis)
Estado:    Zustand (gerenciamento de estado leve)
UI:        Tailwind CSS + shadcn/ui
Editor:    TipTap (editor de texto rico) / CodeMirror (modo texto)
Backend:   Firebase (Auth, Firestore, Storage) + Cloud Run (APIs)
Hospedagem: Firebase Hosting + Cloud CDN
```

### 3.2 Estrutura de Diretórios (Editor)

```
editor/
├── src/
│   ├── App.tsx
│   ├── main.tsx
│   ├── components/
│   │   ├── layout/
│   │   │   ├── Toolbar.tsx
│   │   │   ├── StatusBar.tsx
│   │   │   └── Sidebar.tsx
│   │   ├── graph/
│   │   │   ├── GraphEditor.tsx        # Container ReactFlow
│   │   │   ├── SectionNode.tsx        # Nó customizado por tipo
│   │   │   ├── ChoiceEdge.tsx         # Aresta customizada
│   │   │   └── Minimap.tsx
│   │   ├── inspector/
│   │   │   ├── InspectorPanel.tsx     # Painel de propriedades
│   │   │   ├── TextEditor.tsx         # Editor de texto da seção
│   │   │   ├── ChoiceEditor.tsx       # Editor de escolhas
│   │   │   ├── CombatEditor.tsx       # Editor de combate
│   │   │   ├── TestEditor.tsx         # Editor de teste
│   │   │   └── PresentationEditor.tsx # Ilustração, som, narração, hápticos
│   │   ├── audit/
│   │   │   ├── AuditPanel.tsx         # Lista de problemas
│   │   │   └── AuditBadge.tsx         # Badge no nó (cor por severidade)
│   │   ├── preview/
│   │   │   ├── PreviewPanel.tsx       # Simulação inline
│   │   │   ├── MobilePreview.tsx      # Preview mobile
│   │   │   └── AccessibilityPreview.tsx # Preview acessibilidade
│   │   ├── assets/
│   │   │   ├── AssetManager.tsx       # Upload/gerenciar assets
│   │   │   └── AssetPicker.tsx        # Selecionar asset existente
│   │   └── common/
│   │       ├── Modal.tsx
│   │       ├── Dropdown.tsx
│   │       └── SearchInput.tsx
│   ├── hooks/
│   │   ├── useStoryData.ts            # Hook principal de dados
│   │   ├── useFlowAnalyzer.ts         # Auditoria
│   │   ├── useCollaboration.ts        # Tempo real (Firestore)
│   │   └── useAutoSave.ts
│   ├── lib/
│   │   ├── story-model.ts             # Tipos TypeScript (espelho C#)
│   │   ├── flow-analyzer.ts           # Algoritmos de auditoria
│   │   ├── json-exporter.ts           # Exporta StoryData → JSON
│   │   ├── json-importer.ts           # Importa JSON → StoryData
│   │   ├── graph-layout.ts            # Auto-layout do grafo
│   │   └── firebase.ts                # Config Firebase
│   ├── stores/
│   │   └── editor-store.ts            # Zustand store global
│   └── types/
│       └── index.ts
├── public/
├── package.json
├── vite.config.ts
└── tailwind.config.js
```

### 3.3 Modelos TypeScript (Espelho do C#)

```typescript
// types/story.ts
export interface StoryData {
  formatVersion: string;
  metadata: StoryMetadata;
  characterCreation: CharacterCreationData;
  flags: Record<string, FlagDefinition>;
  items: Record<string, ItemDefinition>;
  sections: Record<string, SectionData>;
}

export type SectionType = 'narrative' | 'combat' | 'test' | 'itemGate' | 'random' | 'ending';

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

export interface HapticsConfig {
  onEnter?: HapticPattern;
  onCombat?: HapticPattern;
  onDiscovery?: HapticPattern;
  onDanger?: HapticPattern;
}

export interface HapticPattern {
  type: 'light' | 'medium' | 'heavy' | 'double' | 'rhythm';
  intensity: number;   // 0.0 - 1.0
  duration: number;    // ms
}
```

### 3.4 Componentes-Chave

#### GraphEditor (ReactFlow)
```typescript
// Nós coloridos por tipo
const nodeColors: Record<SectionType, string> = {
  narrative: '#3B82F6',  // azul
  combat: '#EF4444',     // vermelho
  test: '#10B981',       // verde
  itemGate: '#F59E0B',   // amarelo
  random: '#8B5CF6',     // roxo
  ending: '#6B7280',     // cinza
};

// Aresta com tooltip mostrando texto da escolha
// Drag para criar conexão entre portas
// Duplo clique no nó → abre Inspector
// Delete key → remove nó selecionado
```

#### FlowAnalyzer
```typescript
// lib/flow-analyzer.ts
export interface AuditIssue {
  severity: 'error' | 'warning' | 'info';
  code: string;
  message: string;
  sectionId: number;
  suggestion?: string;
}

export function analyzeFlow(story: StoryData): AuditIssue[] {
  const issues: AuditIssue[] = [];
  const graph = buildGraph(story);
  
  issues.push(...detectDeadEnds(graph, story));
  issues.push(...detectOrphanNodes(graph, story));
  issues.push(...detectLoops(graph, story));
  issues.push(...detectUnreachableNodes(graph, story));
  issues.push(...detectNoVictoryPath(graph, story));
  issues.push(...detectUnusedItems(story));
  issues.push(...detectUnusedFlags(story));
  issues.push(...detectGatesWithoutSource(story));
  issues.push(...detectEmptySections(story));
  issues.push(...detectDuplicateChoices(story));
  
  return issues;
}
```

#### PreviewPanel (Simulação)
```typescript
// Roda a história inline como engine simplificado
// Mantém CharacterState (stats, inventário, flags)
// Renderiza seção atual com escolhas
// Permite "voltar" como rewind
// Modos: Mobile (retrato), Tablet, PC (paisagem)
// Modo acessibilidade: alto contraste, fonte grande
```

---

## 4. Backend — Google Cloud

### 4.1 Serviços

| Serviço | Uso |
|---------|-----|
| **Firebase Auth** | Autenticação (Google Identity) |
| **Cloud Firestore** | Banco de dados em tempo real (histórias, colaboração) |
| **Cloud Storage** | Assets (imagens, sons, arquivos JSON publicados) |
| **Cloud Run** | API serverless (validação, export, analytics) |
| **Firebase Hosting** | Hospedagem do editor SPA |
| **Cloud CDN** | Cache de assets estáticos |

### 4.2 Firestore — Estrutura de Dados

```
stories/{storyId}
  ├── metadata (map)
  │   ├── title: string
  │   ├── authorId: string
  │   ├── version: string
  │   ├── createdAt: timestamp
  │   ├── updatedAt: timestamp
  │   └── publishedAt: timestamp | null
  ├── data (map) — StoryData completo
  ├── collaborators (subcollection)
  │   └── {userId}
  │       ├── role: "owner" | "editor" | "viewer"
  │       └── joinedAt: timestamp
  └── versions (subcollection)
      └── {versionId}
          ├── number: int
          ├── data: StoryData (snapshot)
          ├── createdAt: timestamp
          └── message: string
```

### 4.3 Colaboração em Tempo Real

```
Firestore onSnapshot() listeners:
  - Cada editor recebe atualizações em tempo real
  - Operational Transform simples: último write vence com merge
  - Indicador de presença: "Fulano está editando a seção 42"
  - Lock otimista por seção (evita conflitos de edição simultânea)
```

---

## 5. Fluxo de Dados Completo

```
┌──────────────────────────────────────────────────────────┐
│                     STORY LIFECYCLE                       │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  1. CREATE                                               │
│     Escritor → Editor Web → Firestore (draft)            │
│                                                          │
│  2. EDIT                                                 │
│     Editor Web → Firestore (tempo real, colaborativo)     │
│     Assets → Cloud Storage (imagens, sons)               │
│     Flow Analyzer → feedback em tempo real               │
│                                                          │
│  3. PREVIEW                                              │
│     PreviewPanel → engine inline → teste de jogabilidade │
│                                                          │
│  4. PUBLISH                                              │
│     Editor Web → Cloud Run API → validação →             │
│     → Firestore (published = true)                       │
│     → Cloud Storage (story.json + assets publicados)     │
│                                                          │
│  5. PLAY                                                 │
│     Player App → download story.json + assets            │
│     → cache local → jogo offline                         │
│     → Cloud Save (progresso do jogador)                  │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

---

## 6. APIs (Cloud Run)

### 6.1 Story API

```
GET    /api/stories                    Lista histórias publicadas
GET    /api/stories/:id                Detalhes da história
GET    /api/stories/:id/download       Download story.json + assets (zip)
POST   /api/stories                    Criar nova história (autenticado)
PUT    /api/stories/:id                Atualizar história (autor/editor)
DELETE /api/stories/:id                Deletar história (autor)
POST   /api/stories/:id/publish        Publicar (validação + deploy assets)
POST   /api/stories/:id/validate       Validar história (sem publicar)
GET    /api/stories/:id/audit          Relatório de auditoria
POST   /api/stories/:id/import         Importar de Twine/Ink/JSON
```

### 6.2 Player API

```
POST   /api/player/save               Salvar progresso (cloud)
GET    /api/player/saves               Listar saves do jogador
DELETE /api/player/saves/:id           Deletar save
GET    /api/player/stats               Estatísticas do jogador
```

---

## 7. Considerações Técnicas

### 7.1 Por que Unity para o Player?
- **Cross-platform nativo**: Windows, Mac, Linux, Android, iOS com uma codebase
- **TTS nativo**: System.Speech (Windows), AVSpeechSynthesizer (iOS), TextToSpeech (Android)
- **Screen reader nativo**: integração com Accessibility APIs de cada plataforma
- **Hápticos**: suporte nativo a vibração em Android/iOS
- **UI flexível**: uGUI + TextMeshPro para fontes avançadas
- **Build único**: Mesmo projeto gera builds para todas as plataformas

### 7.2 Por que React + Firebase para o Editor?
- **Web-first**: Zero instalação; acessível de qualquer navegador
- **ReactFlow**: Biblioteca madura para editores de grafo (usada por n8n, Langflow, etc.)
- **Firebase**: Tempo real nativo para colaboração; serverless; escala automática
- **Google Identity**: Autenticação familiar para escritores (conta Google)
- **CDN + Hosting**: Distribuição global com cache edge

### 7.3 Por que JSON como Formato de História?
- **Human-readable**: Escritores podem inspecionar e versionar
- **Portable**: Funciona em qualquer engine/lang (Unity C#, TypeScript, Python)
- **Versionável**: Git diff funciona naturalmente
- **Extensível**: Novos campos não quebram compatibilidade
- **Inspirado em**: Ink JSON export, Yarn Spinner JSON, Twine HTML

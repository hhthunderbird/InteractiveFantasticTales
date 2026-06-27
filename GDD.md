# Interactive Fantastic Tales — Game Design Document (v2)

> Atualizado com pesquisa de campo, crítica de adaptações existentes, e foco em acessibilidade + editor web

---

## 1. Visão do Produto

**Interactive Fantastic Tales** é uma plataforma de **gamebooks digitais interativos** composta por:

| Componente | Público | Descrição |
|------------|---------|-----------|
| **Player App** (Unity) | Jogadores | Lê, ouve, decide — joga aventuras interativas com narração, acessibilidade total |
| **Story Editor** (Web) | Escritores | Cria histórias ramificadas com editor visual de grafo, preview inline, auditoria automática |
| **Story Format** (JSON) | Devs/Escritores | Formato padronizado para importação/exportação de histórias |

### Diferenciais Fundamentais

1. **Acessibilidade Total** — Screen reader, TTS, voz, swipe, alto contraste (não existe em nenhum gamebook digital)
2. **Editor Web Colaborativo** — Grafo visual + auditoria de fluxo + preview inline (não existe em nenhuma ferramenta de IF)
3. **Narração + Hápticos** — Conteúdo multissensorial configurável por seção
4. **Flow Analyzer** — Auditoria estática profissional de integridade narrativa
5. **Cross-Platform Verdadeiro** — Web + PC + Mobile com UI nativa por plataforma

---

## 2. Mecânicas de Jogo (Player App)

### 2.1 Sistema de Personagem

```
Atributos Clássicos (FF):
  SKILL   1d6+6  (7-12)  — Combate e testes de habilidade
  STAMINA 2d6+12 (14-24) — Vida; chegou a 0 = morte
  LUCK    1d6+6  (7-12)  — Testes de sorte; diminui 1 após cada uso

Atributos Expandidos (opcionais por história):
  GOLD      — Moeda para comércio
  MAGIC     — Pontos de magia (histórias com sistema mágico)
  HONOR     — Reputação (opposed-pair: Honorável/Desonroso)
  FEAR      — Medo (opposed-pair: Corajoso/Temeroso)

Recursos:
  PROVISIONS — Comida (restaura 4 STAMINA)
  POTIONS    — Poções diversas (cura, força, sorte)
```

### 2.2 Fluxo de Jogo

```
         ┌──────────────┐
         │  Tela Inicial │
         └──────┬───────┘
                ▼
         ┌──────────────┐
         │ Criação Person│ ← Rolagem de dados + nome
         └──────┬───────┘
                ▼
         ┌──────────────┐
    ┌───→│ Seção Atual   │←───┐
    │    │ (Ler + Ouvir) │    │
    │    └──────┬───────┘    │
    │           ▼             │
    │    ┌──────────────┐    │
    │    │ Escolha/Tipo  │    │
    │    └──────┬───────┘    │
    │           ▼             │
    │    ┌─────────────────┐ │
    │    │ Resolver         │ │
    │    │ • Narrativa →    │ │
    │    │ • Combate        │─┘
    │    │ • Teste          │
    │    │ • Item Gate      │
    │    │ • Random         │
    │    │ • Ending         │
    │    └─────────────────┘
    │
    └── Rewind disponível a qualquer momento
```

### 2.3 Tipos de Seção

| Tipo | Descrição | UI |
|------|-----------|----|
| **Narrative** | Texto + 1-4 escolhas | Painel de texto + botões |
| **Combat** | Inimigo encontrado | Tela de combate com animação |
| **Test** | Teste de atributo contra dificuldade | Animação de dados + resultado |
| **Item Gate** | "Se tiver item X, vá para Y" | Verificação automática |
| **Random** | Resultado aleatório (dados) | Animação de dados |
| **Ending** | Final (vitória/derrota) | Tela de conclusão + stats |

### 2.4 Sistema de Combate

```
Turnos automáticos (engine resolve):
  1. Jogador rola 2d6 + SKILL
  2. Inimigo rola 2d6 + SKILL_inimigo
  3. Perdedor perde 2 STAMINA
  4. Se STAMINA <= 0 → fim do combate
  5. Opção de FUGA: teste de LUCK

Modos de combate:
  - Clássico: engine resolve tudo (FF Classics)
  - Tátil: swipe/drag para escolher postura (Sorcery!)
  - Rápido: resultado instantâneo (veteranos)
```

### 2.5 Sistema de Rewind

```
Linha do tempo visual:
  [Criar Personagem] → [Seção 1] → [Seção 42] → [Combate Goblin] → [Seção 87] → ...
  
  Jogador pode clicar em qualquer ponto para voltar
  Estados do personagem são restaurados
  Opção de "pular texto já lido" ao avançar novamente
```

### 2.6 Salvamento

- **Auto-save**: A cada transição de seção
- **Save manual**: 5 slots por história
- **Bookmarks**: Ilimitados, nomeáveis, com preview de texto
- **Cloud save**: Sincronização entre dispositivos (Google Play / iCloud / Steam Cloud)

---

## 3. Interface do Jogador

### 3.1 Layout Principal (Modo Retrato Mobile / Paisagem PC)

```
┌──────────────────────────────────┐
│  ≡ Menu   Título da História   ⋮ │  Header compacto
├──────────────────────────────────┤
│                                  │
│  ┌────────────────────────────┐  │
│  │                            │  │
│  │     Área de Ilustração     │  │  Imagem da seção (opcional)
│  │                            │  │
│  └────────────────────────────┘  │
│                                  │
│  "Você está na entrada de uma    │
│   caverna escura. O vento uiva   │  Texto narrativo
│   lá dentro, trazendo consigo    │  (scrollável, fonte ajustável)
│   o cheiro de enxofre..."        │
│                                  │
├──────────────────────────────────┤
│  ▶ Entrar na caverna       →    │  Escolha 1
│  ▶ Voltar para a vila      ←    │  Escolha 2
│  ▶ Examinar os arredores   ↑    │  Escolha 3
├──────────────────────────────────┤
│ 🔊  ⚙️  📜  🎒  🗺️  💾  ↩️     │  Barra de ferramentas
└──────────────────────────────────┘

Legenda da barra:
  🔊 = Narração (play/pause/velocidade)
  ⚙️  = Configurações (áudio, acessibilidade, tema)
  📜 = Ficha do personagem (stats, atributos)
  🎒 = Inventário (itens, ouro, provisões)
  🗺️  = Mapa de exploração (caminhos percorridos)
  💾 = Save/Load/Bookmarks
  ↩️  = Rewind (linha do tempo)
```

### 3.2 Modos de Acessibilidade

#### Modo Alto Contraste
```
┌──────────────────────────────────┐
│                                  │  Fundo preto #000000
│  "Você está na entrada de uma    │  
│   caverna escura. O vento uiva   │  Texto amarelo #FFD700
│   lá dentro..."                  │  Fonte 2x maior
│                                  │
│  ▶ Entrar na caverna             │  Botão branco, borda 4px
│  ▶ Voltar para a vila            │  Texto preto no botão
│                                  │
└──────────────────────────────────┘
```

#### Modos de Interação
| Modo | Como Funciona | Para Quem |
|------|---------------|-----------|
| **Touch** | Toque nos botões | Padrão |
| **Swipe** | Swipe up/down/left/right mapeado para escolhas 1-4 | Baixa visão, uma mão |
| **Voz** | Comandos: "escolher um", "repetir", "voltar" | Baixa visão, sem uso das mãos |
| **Teclado** | Teclas 1-4 para escolhas, setas, atalhos | PC, power users |

#### Comandos de Voz
```
"escolher um" / "opção um" / "primeira"
"escolher dois" / "opção dois" / "segunda"
"escolher três" / "opção três" / "terceira"
"escolher quatro" / "opção quatro" / "quarta"
"repetir" / "ler de novo"
"voltar" / "retornar"
"ficha" / "status" / "personagem"
"inventário" / "itens" / "mochila"
"mapa"
"pausar" / "parar"
"continuar" / "seguir"
```

### 3.3 Configurações de Acessibilidade

| Configuração | Opções |
|-------------|--------|
| **Tamanho de fonte** | Pequeno / Médio / Grande / Muito Grande / Gigante |
| **Família de fonte** | Serif / Sans-serif / OpenDyslexic |
| **Tema** | Claro / Escuro / Sépia / Alto Contraste |
| **Narração** | Ligada / Desligada / Auto (lê ao chegar) |
| **Voz TTS** | Masculina / Feminina / Neutra (múltiplas opções) |
| **Velocidade narração** | 0.5x / 0.75x / 1x / 1.25x / 1.5x / 2x |
| **Leitor de tela** | Integração nativa (TalkBack, VoiceOver, NVDA) |
| **Vibração** | Ligada / Desligada / Intensidade |

---

## 4. Story Editor (Web App — Google Cloud)

### 4.1 Visão Geral

Ferramenta **100% web** acessada via navegador, hospedada no Google Cloud.

- **URL**: `editor.interactivefantastictales.com`
- **Autenticação**: Google Identity (conta Google)
- **Stack**: React + TypeScript + Google Cloud Firestore + Cloud Storage + Cloud Run

### 4.2 Funcionalidades Core

| Funcionalidade | Descrição |
|----------------|-----------|
| **Editor de Grafo** | Nós (seções) conectados por arestas (escolhas); drag, zoom, pan, minimapa |
| **Editor de Texto** | Markdown rico; preview em tempo real do que o jogador verá |
| **Preview/Simulação** | "Rodar" a história inline como se fosse o jogador |
| **Asset Manager** | Upload de imagens (.png/.webp), sons (.mp3/.ogg), config de hápticos |
| **Narração Config** | Por seção: voz, velocidade, ênfases |
| **Hápticos Config** | Por evento: padrão de vibração, intensidade, duração |
| **Flow Analyzer** | Auditoria automática de integridade narrativa |
| **Templates** | Seções pré-definidas (combate, teste, item gate) |
| **Versionamento** | Histórico de versões com diff visual no grafo |
| **Export/Import** | JSON padronizado; import de Twine/Ink |

### 4.3 Layout do Editor

```
┌─────────────────────────────────────────────────────────┐
│  🏠 Home   História: "A Montanha..."   v1.2.3   [Salvar]│  Toolbar
├───────────────────┬─────────────────────────────────────┤
│                   │                                     │
│   Editor Visual   │    Painel de Propriedades           │
│   (Grafo)         │                                     │
│                   │  ID: 42                             │
│   [1]──→[42]      │  Tipo: [Combate ▼]                  │
│    │     │  \     │  Texto:                             │
│    │     │   [87] │  ┌─────────────────────────────┐    │
│    │     │        │  │ Um goblin salta das sombras! │    │
│    │     │        │  │                             │    │
│    ▼     ▼        │  └─────────────────────────────┘    │
│   [83]  [400]     │                                     │
│                   │  Inimigo: [Goblin ▼]                │
│                   │  Vitória → [87]                     │
│                   │  Derrota → [400]                    │
│                   │  Fuga    → [1]                      │
│                   │                                     │
│                   │  📷 Ilustração: [goblin.png]        │
│                   │  🔊 Som ambiente: [cave_wind.mp3]   │
│                   │  🎤 Narração: [voz_masculina]       │
│                   │  📳 Háptico: [padrão_combate]       │
│                   │                                     │
├───────────────────┴─────────────────────────────────────┤
│  Auditoria:  ⚠️ 2 avisos  │  Preview  │  Estatísticas   │  Status bar
└─────────────────────────────────────────────────────────┘
```

### 4.4 Modos de Visualização

| Modo | Descrição |
|------|-----------|
| **Grafo** | Visual principal; nós + arestas; cores por tipo |
| **Planilha** | Tabela de todas as seções; filtrável, ordenável |
| **Texto** | Editor focado em texto; preview ao lado |
| **Preview** | Simulação como jogador; mobile/tablet/PC |

### 4.5 Flow Analyzer — Auditoria Automática

| Problema Detectado | Severidade | Exemplo |
|-------------------|------------|---------|
| Dead End sem flag de Ending | 🔴 Erro | Seção 55 tem 0 escolhas e type != Ending |
| Nó órfão (nunca referenciado) | 🟡 Aviso | Seção 203 existe mas nenhuma escolha aponta para ela |
| Loop sem condição de saída | 🔴 Erro | Ciclo [42→87→42] sem Test/ItemGate |
| Caminho inalcançável | 🟠 Alerta | BFS do nó inicial não alcança seção 312 |
| Nenhum caminho para final de vitória | 🔴 Erro | Nenhum Ending type=victory é alcançável |
| Item nunca usado | 🟡 Aviso | "Poção Azul" é coletada mas nunca verificada |
| Flag nunca lida | 🟡 Aviso | "found_map" é setada mas nunca usada em condição |
| Item Gate sem fonte | 🟠 Alerta | Gate verifica "Chave de Prata" que nunca é dada |
| Escolhas duplicadas | 🔵 Info | Duas escolhas apontam para mesma seção 87 |
| Seção sem texto | 🟡 Aviso | Seção 99 type=Narrative mas text está vazio |

---

## 5. Formato de História (JSON Schema)

### 5.1 Estrutura

```json
{
  "formatVersion": "1.0",
  "metadata": {
    "id": "mountain-of-fire",
    "title": "A Montanha do Mago de Fogo",
    "author": { "name": "Autor", "email": "autor@email.com" },
    "version": "1.0.0",
    "language": "pt-BR",
    "genre": ["fantasy"],
    "description": "Uma aventura nas profundezas da montanha...",
    "coverImage": "cover.png",
    "startSection": 1,
    "estimatedDuration": "2-4 horas",
    "tags": ["fantasia", "masmorra", "dragão"]
  },
  "characterCreation": {
    "attributes": {
      "skill": { "label": "Habilidade", "dice": "1d6+6", "min": 7, "max": 12 },
      "stamina": { "label": "Vigor", "dice": "2d6+12", "min": 14, "max": 24 },
      "luck": { "label": "Sorte", "dice": "1d6+6", "min": 7, "max": 12 }
    },
    "startingGold": 0,
    "startingItems": ["espada", "lanterna"],
    "startingProvisions": 10
  },
  "flags": {
    "found_map": { "type": "boolean", "default": false },
    "orcs_killed": { "type": "counter", "default": 0 }
  },
  "items": {
    "espada": { "type": "weapon", "description": "Uma espada curta de aço" },
    "lanterna": { "type": "tool", "description": "Ilumina áreas escuras" },
    "poção_de_cura": { "type": "consumable", "effects": { "stamina": 6 } }
  },
  "sections": {
    "1": {
      "id": 1,
      "type": "narrative",
      "text": "Você está na entrada de uma caverna escura. O vento uiva lá dentro, trazendo o cheiro de enxofre. À sua direita, uma trilha íngreme leva de volta à vila de Stonebridge.",
      "choices": [
        { "text": "Entrar na caverna", "targetSection": 42, "conditions": [] },
        { "text": "Voltar para a vila", "targetSection": 83, "conditions": [] }
      ],
      "presentation": {
        "illustration": "illustrations/cave_entrance.webp",
        "ambientSound": "audio/ambient/cave_wind.ogg",
        "narration": {
          "voice": "male_narrator",
          "speed": 1.0,
          "emphasis": ["vento uiva", "cheiro de enxofre"]
        },
        "haptics": {
          "onEnter": { "pattern": "soft_pulse", "duration": 200 }
        }
      }
    }
  }
}
```

---

## 6. Roadmap de Desenvolvimento

### Fase 0 — Setup (Semanas 1-2)
- [ ] Projeto Unity criado (Player App)
- [ ] Projeto React + Firebase iniciado (Story Editor)
- [ ] Google Cloud Project configurado
- [ ] CI/CD pipelines (GitHub Actions)
- [ ] Definição do JSON Schema final

### Fase 1 — Core MVP (Semanas 3-8)
- [ ] **Player**: GameEngine + SectionManager + Character + DiceSystem
- [ ] **Player**: UI básica (texto + escolhas + ficha)
- [ ] **Player**: Combate automático
- [ ] **Player**: Save/Load local
- [ ] **Editor**: CRUD de seções (texto + escolhas)
- [ ] **Editor**: Grafo visual básico (nós + arestas)
- [ ] **Editor**: Export JSON
- [ ] **Player**: Carrega JSON exportado
- [ ] **Conteúdo**: História demo de 25-30 seções

### Fase 2 — Narrativa e Imersão (Semanas 9-14)
- [ ] **Player**: Narração TTS (Unity TTS + Web Speech API)
- [ ] **Player**: Ilustrações por seção
- [ ] **Player**: Mapa de exploração
- [ ] **Player**: Bookmarks nomeáveis
- [ ] **Player**: Rewind/Timeline
- [ ] **Player**: Efeitos sonoros + música ambiente
- [ ] **Editor**: Preview/Simulação inline
- [ ] **Editor**: Asset Manager (upload imagens/sons)
- [ ] **Editor**: Config de narração por seção

### Fase 3 — Acessibilidade (Semanas 15-20)
- [ ] **Player**: Screen reader nativo
- [ ] **Player**: Modo swipe
- [ ] **Player**: Controle por voz
- [ ] **Player**: Alto contraste + temas
- [ ] **Player**: Fontes ajustáveis + OpenDyslexic
- [ ] **Player**: Navegação completa por teclado
- [ ] **Player**: Hápticos (mobile)
- [ ] **Editor**: Preview de acessibilidade

### Fase 4 — Editor Avançado (Semanas 21-26)
- [ ] **Editor**: Flow Analyzer completo
- [ ] **Editor**: Colaboração em tempo real (Firestore)
- [ ] **Editor**: Histórico de versões
- [ ] **Editor**: Templates de seção
- [ ] **Editor**: Preview multi-plataforma
- [ ] **Editor**: Config de hápticos
- [ ] **Editor**: Modo planilha + modo texto

### Fase 5 — Multi-plataforma (Semanas 27-32)
- [ ] **Player**: Build PC (Windows/Mac/Linux)
- [ ] **Player**: Build Android
- [ ] **Player**: Build iOS
- [ ] **Player**: Cloud save
- [ ] **Player**: UI adaptativa por plataforma
- [ ] **Player**: Integração Steam (PC)
- [ ] **Player**: Google Play / App Store

### Fase 6 — Polimento e Escala (Semanas 33+)
- [ ] Múltiplas histórias com menu/estante
- [ ] Sistema de DLC/In-App Purchases
- [ ] Analytics de caminhos percorridos
- [ ] Localização (inglês, espanhol)
- [ ] Community Workshop (importar histórias)
- [ ] Achievements
- [ ] Modo multiplayer/co-op (avaliação)

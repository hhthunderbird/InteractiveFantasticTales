# AUDITORIA CRÍTICA DE SEGURANÇA & ARQUITETURA

## Interactive Fantastic Tales — Story Delivery System

**Data da auditoria:** 30/06/2026  
**Escopo:** Sistema completo de entrega de histórias — Firestore, Cloud Storage, Cloud Run, IAP, offline sync, privacidade  
**Severidade:** 🔴 Crítico | 🟡 Alta | 🟢 Média | 🔵 Observação

---

## 1. AUDITORIA DE SEGURANÇA

### 1.1 Regras de Segurança do Firestore — Ausentes

**Problema:** O relatório NÃO propõe regras de segurança para o Firestore. A seção de riscos menciona "Firestore security rules" como mitigação para vazamento de dados, mas não especifica nenhuma regra concreta para as coleções: `stories`, `users`, `purchases`, `storyProgress/saves`, `achievements`, `ratings`.

**Impacto:** Sem regras, na configuração padrão do Firestore, ou o banco é completamente aberto (acesso mundial de leitura/escrita) ou completamente fechado (ninguém acessa). O primeiro cenário é catastrófico — qualquer pessoa pode ler os dados de todos os usuários, modificar saves, ou adulterar compras. O segundo paralisa o app.

**Regras propostas:**

```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {

    // ==================== STORIES ====================
    match /stories/{storyId} {
      // Leitura: qualquer usuário autenticado pode ler metadados de histórias publicadas
      allow read: if request.auth != null
                  && resource.data.metadata.isPublished == true;

      // Escrita: apenas o autor ou colaboradores com role "owner"/"editor"
      allow write: if request.auth != null
                   && request.auth.uid == resource.data.metadata.authorId;

      // Preview sections: acessível livremente para histórias publicadas
      // (já coberto pelo allow read acima, mas o preview pode ser lido mesmo sem login)
      match /previewSections/{sectionId} {
        allow read: if true; // preview público
      }

      // Ratings subcoleção
      match /ratings/{userId} {
        allow read: if request.auth != null;
        allow create: if request.auth != null
                      && request.auth.uid == userId
                      && request.resource.data.score >= 1
                      && request.resource.data.score <= 5;
        allow update: if request.auth != null
                      && request.auth.uid == userId;
        allow delete: if false; // ratings não podem ser deletados (anti-fraude)
      }

      // Colaboradores
      match /collaborators/{collabId} {
        allow read, write: if request.auth != null
                           && request.auth.uid == resource.data.authorId;
      }

      // Versões
      match /versions/{versionId} {
        allow read: if request.auth != null
                    && get(/databases/$(database)/documents/stories/$(storyId))
                       .data.metadata.isPublished == true;
        allow write: if request.auth != null
                     && request.auth.uid == resource.data.authorId;
      }
    }

    // ==================== USERS ====================
    match /users/{userId} {
      // Cada usuário só lê/escreve seu próprio documento
      allow read, write: if request.auth != null
                         && request.auth.uid == userId;

      // ownedStoryIds: só pode adicionar via arrayUnion
      // (escrita é permitida em write acima)
      // Validação adicional: não pode remover stories via client
      // (remoção só via Cloud Function de delete account)

      // Story Progress
      match /storyProgress/{storyId} {
        allow read, write: if request.auth != null
                           && request.auth.uid == userId;

        // owned só pode ser setado como true pelo backend (Cloud Run)
        // Cliente NUNCA pode setar owned=true diretamente
        allow create: if request.auth != null
                      && request.auth.uid == userId
                      && request.resource.data.owned == false; // cliente só cria com false

        // Saves
        match /saves/{slotId} {
          allow read, write: if request.auth != null
                             && request.auth.uid == userId;
        }

        // Bookmarks
        match /bookmarks/{bookmarkId} {
          allow read, write: if request.auth != null
                             && request.auth.uid == userId;
        }
      }
    }

    // ==================== PURCHASES ====================
    match /purchases/{purchaseId} {
      // Usuário pode ler suas próprias compras
      allow read: if request.auth != null
                  && request.auth.uid == resource.data.userId;

      // Apenas Cloud Run (backend) pode escrever compras
      // Usando token de serviço admin
      allow write: if false; // Bloqueia client SDK completamente
      // Na prática: Cloud Run usa Firebase Admin SDK (bypass rules)
    }

    // ==================== ACHIEVEMENTS ====================
    match /achievements/{achievementId} {
      // Definições de achievements são públicas (leitura)
      allow read: if request.auth != null;
      // Só backend pode criar/modificar
      allow write: if false;
    }
  }
}
```

**O que está faltando:**
- ❌ **Proteção de `ownedStoryIds`**: O cliente pode setar `owned=true` diretamente se a regra de write permitir modificação do array. Solução: usar Cloud Function que recebe o evento `onCreate` de `purchases/{id}` e faz o `arrayUnion` no backend com Admin SDK.
- ❌ **Rate limiting**: Não há limite de leituras por usuário. Um atacante pode fazer scraping do catálogo inteiro (200 histórias × milhares de requisições) e inflacionar custos. Solução: Cloud Armor no Cloud Run ou Firebase App Check.
- ❌ **Validação de tamanho de campos**: As regras não validam que `review` tem no máximo 500 caracteres, que `displayName` não contém scripts, etc.

### 1.2 Validação de Compras (IAP) — Insuficiente

**Problema:** O fluxo proposto é:

```
Unity Client → Cloud Run (/api/purchases/verify) → Google Play Developer API
```

O relatório diz: "Cloud Run valida receipt com Google/Apple". Mas **não especifica COMO** essa validação previne ataques.

**Ataques não mitigados pelo fluxo atual:**

| Ataque | Descrição | Status |
|--------|-----------|--------|
| **Replay Attack** | Mesmo token de compra válido enviado múltiplas vezes para usuários diferentes | ❌ Não mitigado |
| **Receipt Reuse** | Recibo do Android reutilizado no iOS (cross-store) | ❌ Não mitigado |
| **MITM** | Atacante intercepta POST /api/purchases/verify e modifica userId | ⚠️ Parcialmente (TLS) |
| **Purchase Token Farming** | Atacante gera tokens falsos e envia para Cloud Run | ⚠️ Depende da validação server-side |
| **Race Condition** | Duas requisições simultâneas com mesmo token → entitlements duplicados | ❌ Não mitigado |
| **Free Trial Abuse** | Criar conta nova para cada trial de subscription | ❌ Não mitigado |

**Solução completa:**

```csharp
// Cloud Run — POST /api/purchases/verify
[HttpPost("api/purchases/verify")]
public async Task<IActionResult> VerifyPurchase([FromBody] VerifyRequest request)
{
    // 1. Autenticar — Firebase Auth token no header
    var authToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
    var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(authToken);
    var userId = decodedToken.Uid;

    // 2. Verificar se o userId do token JWT bate com o userId do body
    //    (previne MITM que modifica o body)
    if (request.UserId != userId)
        return Unauthorized("Token/userId mismatch");

    // 3. Validar recibo com a Store (Google Play / App Store)
    var validationResult = await ValidateReceiptWithStore(request);
    if (!validationResult.IsValid)
        return BadRequest("Invalid receipt");

    // 4. IDEMPOTÊNCIA: Verificar se esse purchaseToken JÁ FOI USADO
    //    Usar transação Firestore com o token como chave de idempotência
    var existingPurchase = await db.Collection("purchases")
        .WhereEqualTo("storeToken", request.PurchaseToken)
        .WhereEqualTo("store", request.Store)
        .GetSnapshotAsync();

    if (existingPurchase.Documents.Any())
    {
        var existing = existingPurchase.Documents.First();
        if (existing.GetValue<string>("userId") != userId)
        {
            // TOKEN JÁ USADO POR OUTRO USUÁRIO → FRAUDE
            await LogFraudAttempt(userId, request.PurchaseToken);
            return BadRequest("Token already consumed");
        }
        // Mesmo usuário recomprando → idempotente, retorna sucesso
        return Ok(new { success = true, alreadyOwned = true });
    }

    // 5. Verificar idempotência por orderId
    if (!string.IsNullOrEmpty(validationResult.OrderId))
    {
        var existingByOrder = await db.Collection("purchases")
            .WhereEqualTo("storeOrderId", validationResult.OrderId)
            .GetSnapshotAsync();
        if (existingByOrder.Documents.Any())
            return Ok(new { success = true, alreadyOwned = true });
    }

    // 6. Registrar compra EM TRANSAÇÃO
    await db.RunTransactionAsync(async transaction =>
    {
        var purchaseRef = db.Collection("purchases").Document();
        var userRef = db.Collection("users").Document(userId);
        var progressRef = userRef.Collection("storyProgress").Document(request.StoryId);

        transaction.Set(purchaseRef, new
        {
            userId = userId,
            storyId = request.StoryId,
            type = "iap",
            store = request.Store,
            storeToken = request.PurchaseToken,
            storeOrderId = validationResult.OrderId,
            price = validationResult.Price,
            currency = validationResult.Currency,
            purchasedAt = FieldValue.ServerTimestamp,
            expiresAt = (Timestamp?)null,
            restoredFromDeviceId = (string?)null
        });

        // arrayUnion previne duplicação mesmo em race condition
        transaction.Update(userRef, new Dictionary<string, object>
        {
            ["ownedStoryIds"] = FieldValue.ArrayUnion(request.StoryId)
        });

        transaction.Set(progressRef, new
        {
            owned = true,
            purchasedAt = FieldValue.ServerTimestamp,
            purchaseType = "iap",
            purchaseToken = request.PurchaseToken
        }, SetOptions.MergeAll);
    });

    // 7. Agendar Acknowledge/Consume do token no Google Play
    //    (após sucesso da transação no Firestore)
    await AcknowledgePurchase(request.PurchaseToken, request.Store);

    return Ok(new { success = true });
}
```

### 1.3 Autenticação das APIs do Cloud Run

**Problema:** O relatório lista endpoints REST (`/api/purchases/verify`, `/api/player/save`, `/api/user/export`) mas **não especifica o mecanismo de autenticação**. 

**O que está em jogo:**
- `POST /api/purchases/verify` — se não autenticado, qualquer pessoa pode forjar entitlements
- `POST /api/player/save` — se não autenticado, atacante pode sobrescrever saves de outros usuários
- `GET /api/user/export` — expõe todos os dados GDPR do usuário

**Solução:**

```
┌─────────────────────────────────────────────────────────────┐
│              AUTENTICAÇÃO DAS APIs CLOUD RUN                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Todas as APIs exigem Firebase Auth ID Token no header:     │
│                                                             │
│  Authorization: Bearer {firebase_id_token}                  │
│                                                             │
│  Fluxo:                                                     │
│  1. Unity Client obtém ID token via FirebaseAuth SDK        │
│     string token = await FirebaseAuth.DefaultInstance       │
│         .CurrentUser.TokenAsync(forceRefresh: false);       │
│                                                             │
│  2. Inclui token em todas as chamadas HTTP                  │
│                                                             │
│  3. Cloud Run: middleware valida token                      │
│     - FirebaseAdmin SDK: VerifyIdTokenAsync()               │
│     - Token expirado (~1h) → 401 Unauthorized               │
│     - Token inválido → 401 Unauthorized                     │
│     - userId do token ≠ userId do body → 403 Forbidden     │
│                                                             │
│  4. Rate limit por userId (100 req/min) usando Memorystore  │
│                                                             │
│  Cloud Run IAM:                                             │
│    - Allow unauthenticated: FALSE                           │
│    - Ingress: HTTPS + Cloud CDN                             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Threat model adicional:**
| Ameaça | Mitigação |
|--------|-----------|
| Token replay | TLS + token expira em 1h |
| MITM modifica body | userId extraído do token JWT (não do body) |
| Token refresh infinito | Client limita refreshes a cada 50 min |
| API Key exposta no APK | Não usar API Key; usar Firebase Auth |
| Cloud Run DDoS | Cloud Armor + rate limiting + budget alerts |

### 1.4 Dados em Repouso — Cloud Storage

**Problema crítico:** Os arquivos de história (`story.json`) e assets (ilustrações, áudios) no Cloud Storage podem estar configurados como **publicamente acessíveis** para permitir download pelo app. Se for o caso, qualquer pessoa com o link pode baixar histórias pagas sem autorização.

**O relatório não especifica o mecanismo de controle de acesso para assets de histórias pagas.**

**Solução: Signed URLs com tempo de expiração**

```
┌─────────────────────────────────────────────────────────────┐
│         CONTROLE DE ACESSO A ASSETS (Cloud Storage)          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Configuração do Bucket:                                    │
│  - Acesso público: DESABILITADO (uniform bucket-level)     │
│  - IAM: Nenhum allUsers / allAuthenticatedUsers             │
│                                                             │
│  Fluxo de Download:                                         │
│  1. Unity Client → Cloud Run: POST /api/stories/{id}/assets │
│     Body: { "assetPaths": ["illustrations/cave.webp", ...]} │
│     Header: Authorization: Bearer {token}                   │
│                                                             │
│  2. Cloud Run verifica:                                     │
│     a. Firebase Auth (userId)                               │
│     b. Entitlement: user possui a história?                 │
│        - Verifica users/{userId}/storyProgress/{storyId}    │
│          onde owned == true                                 │
│        - OU subscription ativa                              │
│        - OU pricing.type == "free"                          │
│     c. Se autorizado: gera Signed URLs (expira em 15 min)   │
│     d. Se NÃO autorizado: 403 Forbidden                     │
│                                                             │
│  3. Signed URL:                                             │
│     https://storage.googleapis.com/bucket/...?              │
│       X-Goog-Algorithm=GOOG4-RSA-SHA256&                    │
│       X-Goog-Credential=...&                                │
│       X-Goog-Date=...&                                      │
│       X-Goog-Expires=900&                                   │
│       X-Goog-SignedHeaders=host&                            │
│       X-Goog-Signature=...                                  │
│                                                             │
│  4. Client usa URL para download (15 min de validade)       │
│                                                             │
│  Otimização:                                                │
│  - Thumbnails (cover_128.webp) podem ser públicos            │
│    (marketing, loja precisa mostrar antes da compra)        │
│  - Preview sections (primeiras 5) → signed URL também       │
│  - Assets da história completa → signed URL sempre          │
│                                                             │
│  Cache Edge (Cloud CDN):                                    │
│  - CDN configurado com signed URL verification              │
│  - Cache de 24h para assets versionados                     │
│  - Invalidação quando versão muda                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

```csharp
// Cloud Run — POST /api/stories/{storyId}/assets
[HttpPost("api/stories/{storyId}/assets")]
public async Task<IActionResult> GetAssetUrls(
    string storyId, 
    [FromBody] AssetUrlRequest request)
{
    var userId = GetUserIdFromAuthToken(); // extraído do JWT

    // Verificar entitlement
    var hasAccess = await VerifyEntitlement(userId, storyId);
    if (!hasAccess)
        return Forbid();

    // Auditar acesso (anti-abuse: limite de URLs por minuto)
    var recentRequests = await GetRecentAssetRequests(userId);
    if (recentRequests > 30) // máximo 30 URLs por minuto
        return StatusCode(429, "Too many requests");

    // Gerar signed URLs com expiração de 15 minutos
    var urls = new Dictionary<string, string>();
    foreach (var assetPath in request.AssetPaths)
    {
        var storagePath = $"stories/{storyId}/v{request.Version}/assets/{assetPath}";
        var signedUrl = await GenerateSignedUrl(storagePath, TimeSpan.FromMinutes(15));
        urls[assetPath] = signedUrl;
    }

    // Registrar acesso para auditoria
    await RecordAssetAccess(userId, storyId, request.AssetPaths.Length);

    return Ok(new { urls, expiresIn = 900 });
}
```

### 1.5 Client-Side Tampering (Root/Android)

**Problema:** Em dispositivos Android com root ou iOS com jailbreak, o jogador pode:
- Modificar arquivos de save local (PlayerPrefs, JSON)
- Modificar `ownedStoryIds` localmente
- Modificar character stats (SKILL=999, GOLD=99999)
- Bypassar checagem de versão
- Injetar saves modificados na nuvem

**O que previne? NADA no relatório atual.**

**Mitigações em camadas:**

```
┌─────────────────────────────────────────────────────────────┐
│         DEFESA EM PROFUNDIDADE CONTRA TAMPERING              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  CAMADA 1 — Integrity Check (obrigatória):                  │
│  - Firebase App Check: Play Integrity (Android) /           │
│    Device Check (iOS) — atesta que o request vem de um       │
│    app genuíno rodando em dispositivo não-rootado           │
│  - App Check token é enviado em todo request ao Firestore   │
│    e Cloud Run                                              │
│  - Firestore security rules: request.app_check.token != null│
│                                                             │
│  CAMADA 2 — Server Authority (obrigatória):                 │
│  - Cloud NUNCA confia em dados enviados pelo cliente         │
│  - Save data: Cloud Run valida consistência do save:        │
│    a. SKILL está entre min e max da configuração?           │
│    b. STAMINA ≤ MAX_STAMINA?                                │
│    c. GOLD é plausível (não > 10.000 em história level 1)?  │
│    d. Flags setadas são válidas para esta história?         │
│    e. SectionId existe na história?                         │
│  - Se inconsistente: rejeitar save, logar evento de         │
│    segurança, restaurar último save válido                  │
│                                                             │
│  CAMADA 3 — Checksum Local (defesa):                        │
│  - HMAC-SHA256 do save local com chave derivada do          │
│    Firebase Instance ID (não é impossível quebrar, mas      │
│    aumenta custo do ataque)                                 │
│  - Verificar checksum ao carregar save local                │
│  - Se quebrado: ignorar save local, baixar cloud save       │
│                                                             │
│  CAMADA 4 — Server-Side Progress (recomendada):             │
│  - Stats críticos (achievements, endings encontrados)       │
│    são computados server-side via Cloud Function            │
│  - Cliente reporta eventos (section_entered, ending_reached)│
│  - Cloud Function agrega e persiste                         │
│  - Cliente NUNCA escreve achievements diretamente           │
│                                                             │
│  CAMADA 5 — Play Integrity para IAP (crítica):              │
│  - Antes de validar compra, Cloud Run verifica que o        │
│    request vem de dispositivo íntegro                       │
│  - Play Integrity Standard Request (gratuito)               │
│  - Se dispositivo é root/jailbreak: IAP recusado            │
│    (usuário legítimo pode jogar, mas comprar é bloqueado)   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 1.6 Ataques de Multi-Device Sync

**Cenários de abuso possíveis:**

| Ataque | Descrição | Mitigação |
|--------|-----------|-----------|
| **Item Duplication** | Device A e B offline → ambos compram item → sync → 2 itens | Transaction no Firestore: usar `arrayUnion`; itens com quantidade usam `FieldValue.Increment(-1)` atômico |
| **Gold Duplication** | Dispositivo A gasta 50 gold, B gasta os mesmos 50 gold antes do sync | Operações de inventário devem passar por Cloud Run com transação atômica |
| **Achievement Reset** | Device A apaga dados locais, sync sobrescreve achievements cloud | Achievements são computed server-side (Cloud Function); nunca deletados |
| **Save Slot Overflow** | Criar 100 saves simultâneos de 2 dispositivos | Security rule: `allow create: if get(/databases/$(database)/documents/users/$(userId)/storyProgress/$(storyId)/saves).size() < 5` (não funciona em regras — precisa de Cloud Function) |
| **Purchase Token Reuse** | Mesma conta Google logada em 2 dispositivos, ambos tentam restore | Idempotência por storeToken (já coberto na seção 1.2) |

**Solução para Gold/Inventory: Server-authoritative economy**

```csharp
// Cloud Run — POST /api/game/action
// Toda operação que altera atributos do personagem passa pelo server
[HttpPost("api/game/{storyId}/action")]
public async Task<IActionResult> PerformAction(string storyId, [FromBody] GameAction action)
{
    var userId = GetUserIdFromAuthToken();

    // Transação atômica no Firestore
    await db.RunTransactionAsync(async transaction =>
    {
        var saveRef = db.Collection("users").Document(userId)
            .Collection("storyProgress").Document(storyId)
            .Collection("saves").Document(action.Slot.ToString());

        var saveSnapshot = await transaction.GetSnapshotAsync(saveRef);
        var currentChar = saveSnapshot.GetValue<Dictionary<string, object>>("characterSnapshot");

        // Validar consistência
        if (!IsActionValid(action, currentChar))
            throw new InvalidGameActionException("Invalid action");

        // Aplicar mudanças atomicamente
        transaction.Update(saveRef, new Dictionary<string, object>
        {
            ["characterSnapshot.gold"] = FieldValue.Increment(action.GoldDelta),
            ["characterSnapshot.stamina"] = FieldValue.Increment(action.StaminaDelta),
            // ...
            ["sectionId"] = action.NewSectionId,
            ["sectionEnteredAt"] = FieldValue.ServerTimestamp,
            ["updatedAt"] = FieldValue.ServerTimestamp
        });
    });

    return Ok();
}
```

🟡 **Prioridade:** Alta — MVP pode começar com economia client-side + validação server-side de limites, mas precisa migrar para server-authoritative antes do lançamento público.

---

## 2. AUDITORIA DO MODELO DE DADOS

### 2.1 Anti-Padrões no Firestore

**Problema #1: `previewSections[]` como array no documento `stories/{id}`**

O relatório propõe armazenar 3-5 primeiras seções completas (texto + choices + presentation) diretamente no documento Firestore. Cada seção com texto narrativo de 500-1000 caracteres + choices + URLs de assets pode ocupar 2-5 KB. Com 5 seções = 10-25 KB por documento. Se o catálogo tem 200 histórias, isso adiciona 2-5 MB de dados que são baixados em TODA listagem de catálogo.

🟡 **Solução:** Mover `previewSections` para uma subcoleção `stories/{id}/previewSections/{sectionId}`. Carregar apenas na tela de detalhes, não na listagem. OU armazenar apenas o texto da primeira seção (preview teaser de 300 chars) no documento principal.

**Problema #2: `ownedStoryIds[]` no documento `users/{id}`**

O array de IDs de histórias possuídas cresce para sempre. Se um usuário comprar 500 histórias (cenário extremo), o array ocupa ~10 KB. O documento `users/{id}` já contém `profile`, `stats`, `accessibilityPrefs`, e `devices`. Risco de estourar 1 MiB.

🟢 **Solução:** Manter `ownedStoryIds` para acesso rápido (até ~200 strings é seguro). Para heavy users, migrar para subcoleção `users/{id}/ownedStories/{storyId}` com dados mínimos. A query da biblioteca já consulta `storyProgress` onde `owned == true`, então `ownedStoryIds` é um cache de conveniência.

**Problema #3: Query de catálogo com filtro composto sem índice**

```csharp
db.Collection("stories")
  .WhereEqualTo("metadata.isPublished", true)
  .WhereArrayContains("metadata.genre", "fantasy")
  .OrderByDescending("stats.purchaseCount")
  .Limit(20)
```

Esta query requer um **índice composto** que **não é criado automaticamente** pelo Firestore. O SDK retorna erro e a query falha.

🔴 **Solução:** Criar índices compostos explicitamente no `firestore.indexes.json`:

```json
{
  "indexes": [
    {
      "collectionGroup": "stories",
      "queryScope": "COLLECTION",
      "fields": [
        { "fieldPath": "metadata.isPublished", "order": "ASCENDING" },
        { "fieldPath": "metadata.genre", "arrayConfig": "CONTAINS" },
        { "fieldPath": "stats.purchaseCount", "order": "DESCENDING" }
      ]
    },
    {
      "collectionGroup": "stories",
      "queryScope": "COLLECTION",
      "fields": [
        { "fieldPath": "metadata.isPublished", "order": "ASCENDING" },
        { "fieldPath": "metadata.language", "order": "ASCENDING" },
        { "fieldPath": "stats.purchaseCount", "order": "DESCENDING" }
      ]
    },
    {
      "collectionGroup": "stories",
      "queryScope": "COLLECTION",
      "fields": [
        { "fieldPath": "metadata.isPublished", "order": "ASCENDING" },
        { "fieldPath": "pricing.type", "order": "ASCENDING" },
        { "fieldPath": "stats.purchaseCount", "order": "DESCENDING" }
      ]
    }
  ],
  "fieldOverrides": []
}
```

**Problema #4: `ListenToLibraryProgress` — listener em tempo real em subcoleção**

O listener proposto escuta TODAS as mudanças em `users/{userId}/storyProgress` onde `owned == true`. Isso é eficiente para poucas histórias, mas se o usuário tem 100 histórias, o listener carrega 100 documentos em memória no cliente.

🟢 **Solução:** Na tela de Library, usar `GetSnapshotAsync` (one-time fetch) em vez de listener. A biblioteca só precisa de dados em tempo real se o usuário está jogando em outro dispositivo simultaneamente — o que não é o caso comum. Usar listener apenas na tela de jogo ativo para o save do slot atual.

### 2.2 Schema Evolution (Versão do Formato de História)

**Problema:** O GDD define `formatVersion: "1.0"` no JSON da história. O que acontece quando o schema evolui (ex: adicionar novo SectionType `puzzle`, ou campo `voiceEmotion` no `PresentationData`)?

**Cenários:**

| Mudança | Impacto | Estratégia |
|---------|---------|------------|
| Adicionar campo opcional (forward compat) | App antigo ignora campo novo | ✅ Sem migração necessária |
| Adicionar novo SectionType | App antigo não entende o tipo → crash ou seção pulada | ❌ Precisa de version gate |
| Remover campo | App antigo tenta ler campo que não existe → null ref | ❌ Precisa de fallback |
| Mudar estrutura de dados (ex: `flags` de array para objeto) | App antigo não consegue parse | ❌ Precisa de migração |

🟡 **Solução: Version Gate + Migration Layer**

```csharp
public class StoryLoader
{
    private const string MIN_APP_VERSION_FOR_FORMAT = "1.0.0";

    public async Task<StoryData> LoadStory(string json)
    {
        var story = JsonConvert.DeserializeObject<StoryData>(json);

        // Version gate: app antigo não consegue ler formato novo
        if (!IsFormatSupported(story.formatVersion, AppVersion.Current))
        {
            throw new FormatVersionException(
                $"Esta história requer a versão {GetMinAppVersion(story.formatVersion)} " +
                $"do app. Você está na versão {AppVersion.Current}."
            );
        }

        // Migration layer: migrar campos deprecated para novo formato
        story = MigrationPipeline.Migrate(story, story.formatVersion, "latest");

        // Fallback: campos ausentes recebem defaults
        story = ApplyDefaults(story);

        return story;
    }
}

public static class MigrationPipeline
{
    private static readonly Dictionary<string, Func<StoryData, StoryData>> Migrations = new()
    {
        ["1.0"] = (s) => {
            // Se não tem presentation, criar default
            foreach (var section in s.sections.Values)
            {
                section.presentation ??= new PresentationData();
            }
            return s;
        },
        ["2.0"] = (s) => {
            // Migrar flags de string[] para Dictionary<string, FlagData>
            // ...
            return s;
        }
    };

    public static StoryData Migrate(StoryData story, string fromVersion, string toVersion)
    {
        var versions = Migrations.Keys.OrderBy(v => new Version(v)).ToList();
        foreach (var version in versions)
        {
            if (new Version(version) > new Version(fromVersion) &&
                new Version(version) <= new Version(toVersion))
            {
                story = Migrations[version](story);
            }
        }
        return story;
    }
}
```

### 2.3 Story Versioning vs Player Saves

**Problema:** Se o autor atualiza a história (v1.0 → v1.1: correção de typo na seção 87), o jogador tem save na seção 87. O que acontece?

**Cenários detalhados:**

| Cenário | Comportamento atual | Comportamento desejado |
|---------|-------------------|----------------------|
| Typo corrigido na seção 87 | Texto do cache local (v1.0) mostra o typo | App detecta v1.1 disponível, pergunta se quer atualizar |
| Seção 87 removida na v2.0 | Save aponta para seção inexistente → null ref | App detecta seção órfã, move jogador para seção anterior ou start |
| Seção 87 vira combate (era narrativa) | Engine processa seção com tipo errado | App respeita nova definição da seção (ao atualizar) |
| Escolha removida na seção 42 | Timeline contém escolha que não existe mais | Rewind funciona com base nos IDs, não nos índices |
| Nova seção inserida entre 42 e 87 | Numeração sequencial quebra se IDs são índices | Usar IDs estáveis (GUID ou id semântico), nunca reordenar |

🟡 **Solução:**

```csharp
public class SectionNavigationValidator
{
    public static NavigationResult ValidateSaveCompatibility(
        SaveData save, StoryData oldVersion, StoryData newVersion)
    {
        var currentSectionId = save.sectionId;

        // Caso 1: Seção existe na nova versão → OK
        if (newVersion.sections.ContainsKey(currentSectionId.ToString()))
            return NavigationResult.Ok;

        // Caso 2: Seção removida → encontrar caminho de fallback
        // Percorrer timeline salva de trás para frente
        foreach (var entry in save.timeline.Reverse())
        {
            if (newVersion.sections.ContainsKey(entry.sectionId.ToString()))
            {
                return NavigationResult.Redirect(
                    entry.sectionId,
                    $"A seção {currentSectionId} foi removida. " +
                    "Você foi movido para a última seção válida."
                );
            }
        }

        // Caso 3: Nada funciona → reiniciar
        return NavigationResult.Restart(
            "A história foi significativamente alterada. " +
            "Seu progresso anterior não é compatível."
        );
    }
}

public enum NavigationResultType { Ok, Redirect, Restart }
```

### 2.4 Raça de Condição em Compras (Billing Entitlement)

**Problema:** O fluxo de compra tem um gap entre validação do recibo (passo 6) e escrita do entitlement (passo 7):

```
6. Validar com Google Play Developer API  ← SUCESSO
   [CRASH AQUI: Cloud Run reinicia, ou rede cai]
7. Registrar em purchases/{id}            ← NUNCA EXECUTA
8. Atualizar users/{uid}/ownedStoryIds    ← NUNCA EXECUTA
```

Usuário pagou, Google cobrou, mas entitlement nunca foi registrado.

🔴 **Solução: Idempotência + Retry + Reconciliation**

```
┌─────────────────────────────────────────────────────────────┐
│         FLUXO DE COMPRA IDEMPOTENTE (COMPLETO)               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Cloud Run — POST /api/purchases/verify                     │
│                                                             │
│  1. Receber purchaseToken + userId + storyId                │
│                                                             │
│  2. Verificar cache de idempotência (Redis/Memorystore):    │
│     key = "purchase:{purchaseToken}"                        │
│     Se existe → retornar resultado cacheado imediatamente    │
│                                                             │
│  3. Validar com Google Play (idempotente, pode repetir)     │
│                                                             │
│  4. Se válido:                                              │
│     a. Escrever purchases/{id} com { storeToken, status:    │
│        "validated", consumed: false }                       │
│     b. Escrever entitlement no user (arrayUnion + progress) │
│     c. Marcar purchases/{id}.consumed = true                │
│     d. Escrever cache de idempotência (TTL=30 dias)         │
│                                                             │
│  5. Se falha em 4b (entitlement):                            │
│     → purchases/{id} já existe com status "validated"        │
│     → Job de reconciliação (cron a cada 5 min) verifica      │
│       compras com consumed=false e concede entitlement      │
│                                                             │
│  6. Se falha em 4a (purchase doc):                          │
│     → Retry com exponential backoff (3 tentativas)           │
│     → Se todas falham: retornar 503 + "tente novamente"      │
│     → Google Play: a transação já foi confirmada,            │
│       mas o entitlement não foi concedido                    │
│     → Próxima vez que usuário abrir app:                    │
│       GET /api/purchases/pending → reconciliação automática  │
│                                                             │
│  7. Reconciliation job (cron, Cloud Scheduler):              │
│     Para cada purchase com consumed=false e idade > 5 min:  │
│       → Revalidar token com Google Play                     │
│       → Conceder entitlement se ainda válido                 │
│       → Marcar consumed=true                                │
│       → Alertar se token expirou (compra paga sem delivery) │
│                                                             │
│  Google Play Acknowledge:                                    │
│  - Só chamar Acknowledge APÓS entitlement concedido          │
│  - Se não der acknowledge em 3 dias: Google reembolsa        │
│    automaticamente                                          │
│  - Timer de 72h para acknowledge é o SLA máximo              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

```csharp
// Reconciliation Job (Cloud Scheduler → Cloud Run)
[HttpPost("api/internal/reconcile-purchases")]
public async Task<IActionResult> ReconcilePurchases()
{
    // Buscar compras "pending" (validadas mas não entregues)
    var pendingPurchases = await db.Collection("purchases")
        .WhereEqualTo("status", "validated")
        .WhereEqualTo("consumed", false)
        .GetSnapshotAsync();

    foreach (var doc in pendingPurchases.Documents)
    {
        var purchaseToken = doc.GetValue<string>("storeToken");
        var userId = doc.GetValue<string>("userId");
        var storyId = doc.GetValue<string>("storyId");

        // Re-validar com Google Play
        var validation = await ValidateReceiptWithStore(purchaseToken);
        if (validation.IsValid)
        {
            // Conceder entitlement (idempotente)
            await GrantEntitlement(userId, storyId);

            // Marcar como consumido
            await doc.Reference.UpdateAsync(new Dictionary<string, object>
            {
                ["consumed"] = true,
                ["deliveredAt"] = FieldValue.ServerTimestamp
            });

            // Acknowledge com Google Play
            await AcknowledgePurchase(purchaseToken);
        }
        else
        {
            // Token expirado/inválido → alerta de operação!
            await LogAlert($"Purchase {doc.Id} has expired without delivery!");

            // Marcar para investigação manual
            await doc.Reference.UpdateAsync(new Dictionary<string, object>
            {
                ["status"] = "needs_investigation",
                ["error"] = "Token expired before entitlement delivery"
            });
        }
    }

    return Ok(new { reconciled = pendingPurchases.Documents.Count });
}
```

---

## 3. AUDITORIA DE OFFLINE/SYNC

### 3.1 Algoritmo de Merge para Conflitos de Save

**Problema:** O relatório diz "Last-write-wins com timestamp; mostrar 'conflito detectado'". Isso é **insuficiente para dados de jogo**. Uma história de 4 horas com decisões ramificadas não pode simplesmente descartar um save inteiro porque o timestamp é mais antigo.

**Cenário real de conflito:**

```
Dispositivo A (celular):                     Dispositivo B (tablet):
Jogou da seção 1 → 42 → 87                  Jogou da seção 1 → 42 → 83
Ganhou combate: +10 gold                    Perdeu combate: -2 stamina
Coletou "poção azul"                        Coletou "chave de prata"
Save às 14:00 (horário local)               Save às 14:05 (horário local)
```

Com last-write-wins, o save do dispositivo B (14:05) sobrescreve o do dispositivo A (14:00). O jogador perde o gold, a poção azul, e o progresso até a seção 87. Isso é inaceitável.

🟡 **Solução: Three-Way Merge com Resolução Manual (quando necessário)**

```
┌─────────────────────────────────────────────────────────────┐
│          ALGORITMO DE MERGE DE SAVES (3-WAY)                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Estrutura: Cada save tem um version vector (não timestamp)│
│                                                             │
│  {                                                          │
│    "baseVersion": "abc123",     // versão base comum        │
│    "deviceVersion": "def456",   // versão no device X       │
│    "serverVersion": "ghi789",   // versão no servidor       │
│    "deviceId": "phone_01",                                  │
│    "characterSnapshot": { ... },                            │
│    "sectionId": 87,                                         │
│    "timeline": [ ... ]                                      │
│  }                                                          │
│                                                             │
│  PASSO 1: Detectar conflito                                 │
│  ┌─────────────────────────────────────────────────┐       │
│  │ Servidor tem: { baseVersion: "abc123",          │       │
│  │                 serverVersion: "def456" }        │       │
│  │                                                  │       │
│  │ Device A envia: { baseVersion: "abc123",        │       │
│  │                   deviceVersion: "ghi789" }      │       │
│  │                                                  │       │
│  │ Se serverVersion == deviceVersion → sem conflito │       │
│  │ Se serverVersion != deviceVersion:               │       │
│  │   Ambos derivam de abc123 → CONFLITO             │       │
│  │   Device A deriva de def456 → fast-forward (ok)  │       │
│  └─────────────────────────────────────────────────┘       │
│                                                             │
│  PASSO 2: Auto-merge (campos independentes)                 │
│  ┌─────────────────────────────────────────────────┐       │
│  │ Campos que podem ser merged automaticamente:    │       │
│  │  - inventory: union dos arrays                  │       │
│  │  - flags: OR lógico (true vence false)          │       │
│  │  - counters: MAX (maior valor vence)            │       │
│  │  - gold: SUM dos deltas (se base é conhecida)   │       │
│  │  - stamina: campo problemático (ver abaixo)     │       │
│  │  - sectionId: conflito (ver abaixo)             │       │
│  └─────────────────────────────────────────────────┘       │
│                                                             │
│  PASSO 3: Campos com conflito requerem decisão              │
│  ┌─────────────────────────────────────────────────┐       │
│  │ sectionId: Device A na seção 87, B na 83         │       │
│  │  → NÃO pode resolver automaticamente             │       │
│  │  → Mostrar UI de conflito ao usuário:            │       │
│  │                                                  │       │
│  │  ┌────────────────────────────────────────┐     │       │
│  │  │  ⚠️ Conflito de Save Detectado         │     │       │
│  │  │                                        │     │       │
│  │  │  Seu progresso foi salvo em 2          │     │       │
│  │  │  dispositivos diferentes.              │     │       │
│  │  │                                        │     │       │
│  │  │  📱 Este dispositivo:                  │     │       │
│  │  │     Seção 87 | 45 min jogados          │     │       │
│  │  │     Gold: 120 | Flags: 5               │     │       │
│  │  │     [Usar esta versão]                 │     │       │
│  │  │                                        │     │       │
│  │  │  💻 Outro dispositivo:                 │     │       │
│  │  │     Seção 83 | 30 min jogados          │     │       │
│  │  │     Gold: 90  | Flags: 3               │     │       │
│  │  │     [Usar esta versão]                 │     │       │
│  │  └────────────────────────────────────────┘     │       │
│  └─────────────────────────────────────────────────┘       │
│                                                             │
│  PASSO 4: Merge bem-sucedido                                │
│  ┌─────────────────────────────────────────────────┐       │
│  │ Novo baseVersion = hash(merge)                   │       │
│  │ Salvar versão merged no servidor                 │       │
│  │ Sync para todos os dispositivos                  │       │
│  └─────────────────────────────────────────────────┘       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

```csharp
public class SaveMerger
{
    public MergeResult Merge(SaveSnapshot baseSnapshot, SaveSnapshot server, SaveSnapshot device)
    {
        var result = new MergeResult();
        var conflicts = new List<SaveConflict>();

        // --- Auto-merge: campos de inventário ---
        // Inventory: união
        result.Character.Inventory = server.Character.Inventory
            .Union(device.Character.Inventory)
            .Distinct()
            .ToList();

        // Flags: OR lógico (true vence false)
        result.Character.Flags = new Dictionary<string, bool>();
        foreach (var key in server.Character.Flags.Keys
            .Union(device.Character.Flags.Keys))
        {
            var serverVal = server.Character.Flags.GetValueOrDefault(key, false);
            var deviceVal = device.Character.Flags.GetValueOrDefault(key, false);
            result.Character.Flags[key] = serverVal || deviceVal;
        }

        // Counters: MAX
        result.Character.Counters = new Dictionary<string, int>();
        foreach (var key in server.Character.Counters.Keys
            .Union(device.Character.Counters.Keys))
        {
            var serverVal = server.Character.Counters.GetValueOrDefault(key, 0);
            var deviceVal = device.Character.Counters.GetValueOrDefault(key, 0);
            result.Character.Counters[key] = Math.Max(serverVal, deviceVal);
        }

        // Gold: somar deltas se base é conhecida
        if (baseSnapshot != null)
        {
            var serverGoldDelta = server.Character.Gold - baseSnapshot.Character.Gold;
            var deviceGoldDelta = device.Character.Gold - baseSnapshot.Character.Gold;
            result.Character.Gold = baseSnapshot.Character.Gold
                + serverGoldDelta + deviceGoldDelta;
        }
        else
        {
            result.Character.Gold = Math.Max(server.Character.Gold, device.Character.Gold);
        }

        // Stamina: MAX entre os dois (favorável ao jogador)
        result.Character.Stamina = Math.Max(server.Character.Stamina,
                                             device.Character.Stamina);

        // --- Campo com conflito: sectionId ---
        if (server.SectionId != device.SectionId)
        {
            conflicts.Add(new SaveConflict
            {
                Field = "sectionId",
                ServerValue = server,
                DeviceValue = device,
                Resolution = ConflictResolution.RequiresUserChoice
            });
        }

        // --- Timeline: merge por union de eventos ordenados ---
        result.Timeline = server.Timeline
            .Union(device.Timeline, new TimelineEntryComparer())
            .OrderBy(e => e.Timestamp)
            .ToList();

        // --- Auto-resolve se possível ---
        if (!conflicts.Any(c => c.Resolution == ConflictResolution.RequiresUserChoice))
        {
            result.Resolved = true;
            result.Strategy = "auto-merge";
        }
        else
        {
            result.Resolved = false;
            result.Conflicts = conflicts;
            result.Strategy = "requires-user-choice";
        }

        return result;
    }
}
```

### 3.2 Clock Skew — Timestamps

**Problema:** Se o dispositivo A está em São Paulo (UTC-3) e o dispositivo B em Londres (UTC+0), usando `DateTime.Now` para `sectionEnteredAt`, os timestamps ficam inconsistentes. O relatório usa `timestamp` do Firestore em alguns campos e `DateTime` local em outros.

🟡 **Solução:**

```
┌─────────────────────────────────────────────────────────────┐
│         POLÍTICA DE TIMESTAMPS (OBRIGATÓRIA)                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  REGRA 1: Todo timestamp no Firestore usa                    │
│           FieldValue.ServerTimestamp (UTC, precisão de ms)  │
│                                                             │
│  REGRA 2: Timestamps locais (offline) são armazenados       │
│           como UTC (DateTime.UtcNow)                        │
│                                                             │
│  REGRA 3: Ao sincronizar, o server timestamp sempre         │
│           sobrescreve o timestamp local do mesmo evento     │
│                                                             │
│  REGRA 4: Para ordenação (ex: timeline), usar o             │
│           timestamp do servidor como autoritativo            │
│                                                             │
│  REGRA 5: "LastWriteWins" usa o server timestamp,           │
│           NUNCA o clock do dispositivo                      │
│                                                             │
│  Client:                                                    │
│    saveData.updatedAt = DateTime.UtcNow; // local, para UI  │
│    // Ao enviar para servidor:                              │
│    saveRef.UpdateAsync(new {                                 │
│        sectionEnteredAt = FieldValue.ServerTimestamp,       │
│        updatedAt = FieldValue.ServerTimestamp               │
│    });                                                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 Partial Sync Failure — Rollback

**Problema:** Cloud save do slot 1 sobe com sucesso, mas save do slot 2 falha (rede caiu no meio). O servidor tem slot 1 atualizado e slot 2 desatualizado. O cliente acha que ambos foram salvos. Estado inconsistente.

🟡 **Solução: Batch Write com Atomicidade**

```csharp
public async Task<bool> SyncAllSaves(string userId, string storyId,
    Dictionary<int, SaveData> savesToSync)
{
    var batch = db.StartBatch();

    foreach (var (slot, saveData) in savesToSync)
    {
        var saveRef = db.Collection("users").Document(userId)
            .Collection("storyProgress").Document(storyId)
            .Collection("saves").Document(slot.ToString());

        batch.Set(saveRef, new
        {
            slot = slot,
            sectionId = saveData.SectionId,
            characterSnapshot = saveData.CharacterSnapshot,
            timeline = saveData.Timeline,
            playtimeAtSave = saveData.PlaytimeAtSave,
            updatedAt = FieldValue.ServerTimestamp,
            createdAt = FieldValue.ServerTimestamp, // só usado se for criação
            name = saveData.Name,
        }, SetOptions.MergeAll);
    }

    try
    {
        await batch.CommitAsync();
        return true; // TODOS os saves sincronizados atomicamente
    }
    catch (FirebaseFirestoreException ex)
    {
        // Batch inteiro falhou — nenhum save foi escrito parcialmente
        Debug.LogError($"Sync failed: {ex.Message}");
        return false; // Cliente tenta novamente no próximo ciclo
    }
}
```

**Firestore Batch Write** é atômico: ou todos os writes são aplicados, ou nenhum é. Isso resolve o problema de partial sync.

### 3.4 Compra Offline

**Problema:** O relatório menciona Unity IAP com Google Play Billing. Google Play Billing **requer conexão com internet** para processar pagamentos (óbvio). Mas o que acontece se o usuário está offline e tenta comprar?

🟢 **Comportamento esperado:**

```
┌─────────────────────────────────────────────────────────────┐
│         UX PARA COMPRA OFFLINE                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Google Play Billing NÃO funciona offline.                  │
│  O SDK retorna BillingResponseCode.ServiceUnavailable.     │
│                                                             │
│  UX correta:                                                │
│                                                             │
│  1. Usuário clica "Comprar"                                 │
│  2. Cliente verifica conectividade ANTES de iniciar fluxo   │
│  3. Se offline:                                             │
│     ┌────────────────────────────────────────┐             │
│     │  📡 Sem Conexão                        │             │
│     │                                        │             │
│     │  Compras requerem conexão com a        │             │
│     │  internet. Conecte-se para continuar.  │             │
│     │                                        │             │
│     │  [Tentar Novamente] [Depois]           │             │
│     └────────────────────────────────────────┘             │
│                                                             │
│  4. NÃO mostrar "Comprar" como disponível offline           │
│     → Botão esmaecido ou com badge "requer internet"        │
│                                                             │
│  5. Wishlist offline: usuário pode "salvar para depois"     │
│     histórias que quer comprar quando tiver conexão         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 3.5 Cache Poisoning (story.json corrompido)

**Problema:** Se um `story.json` corrompido entra no cache local (download truncado, bit flip no storage, SD card corrompido), o jogo pode carregar dados inválidos, causar crashes, ou pior: mostrar escolhas erradas que levam a estados inválidos do jogo.

🟡 **Solução: Hash Chain de Integridade**

```csharp
public class StoryIntegrityVerifier
{
    // Ao baixar story.json:
    public async Task<DownloadResult> DownloadAndVerifyStory(
        string storyId, StorageReference storyRef)
    {
        var localPath = GetCachedStoryPath(storyId);
        var tempPath = localPath + ".download";

        // 1. Baixar para arquivo temporário
        await storyRef.GetFileAsync(tempPath);

        // 2. Verificar SHA-256 contra o metadata do Firestore
        var localHash = ComputeSHA256(tempPath);
        var metadataDoc = await db.Collection("stories").Document(storyId).GetSnapshotAsync();
        var expectedHash = metadataDoc.GetValue<string>("storage.sha256Hash");

        if (localHash != expectedHash)
        {
            // Corrompido ou adulterado
            File.Delete(tempPath);
            await LogIntegrityFailure(storyId, "hash_mismatch");
            return DownloadResult.Corrupted(expectedHash, localHash);
        }

        // 3. Verificar JSON parse (integridade estrutural)
        try
        {
            var json = File.ReadAllText(tempPath);
            var story = JsonConvert.DeserializeObject<StoryData>(json);

            // 4. Verificar integridade semântica
            var validation = ValidateStoryData(story);
            if (!validation.IsValid)
            {
                File.Delete(tempPath);
                await LogIntegrityFailure(storyId, $"semantic:{validation.Error}");
                return DownloadResult.InvalidStructure(validation.Error);
            }

            // 5. Substituir arquivo antigo atomicamente
            if (File.Exists(localPath)) File.Delete(localPath);
            File.Move(tempPath, localPath);

            // 6. Salvar hash local para verificações futuras rápidas
            SaveLocalIntegrityMetadata(storyId, expectedHash, DateTime.UtcNow);

            return DownloadResult.Success;
        }
        catch (JsonException ex)
        {
            File.Delete(tempPath);
            await LogIntegrityFailure(storyId, $"parse_error:{ex.Message}");
            return DownloadResult.ParseError(ex.Message);
        }
    }

    // Ao carregar story do cache:
    public async Task<LoadResult> LoadCachedStory(string storyId)
    {
        var localPath = GetCachedStoryPath(storyId);
        if (!File.Exists(localPath))
            return LoadResult.NotCached;

        // Verificação rápida: hash match
        var metadata = LoadLocalIntegrityMetadata(storyId);
        if (metadata != null)
        {
            var currentHash = ComputeSHA256(localPath);
            if (currentHash != metadata.Hash)
            {
                // Cache corrompido desde a última verificação
                File.Delete(localPath);
                return LoadResult.Corrupted;
            }
        }

        // Parse e validação semântica (só na carga, não em todo acesso)
        var json = File.ReadAllText(localPath);
        var story = JsonConvert.DeserializeObject<StoryData>(json);
        var validation = ValidateStoryData(story);
        if (!validation.IsValid)
        {
            File.Delete(localPath);
            return LoadResult.InvalidStructure(validation.Error);
        }

        return LoadResult.Success(story);
    }

    private StoryValidationResult ValidateStoryData(StoryData story)
    {
        // Verificações estruturais
        if (story.sections == null || story.sections.Count == 0)
            return StoryValidationResult.Fail("Story has no sections");

        if (story.metadata?.startSection == null)
            return StoryValidationResult.Fail("Story has no start section");

        if (!story.sections.ContainsKey(story.metadata.startSection.ToString()))
            return StoryValidationResult.Fail($"Start section {story.metadata.startSection} not found");

        // Verificar referências circulares (todas as escolhas apontam para seções existentes)
        foreach (var (sectionId, section) in story.sections)
        {
            if (section.type == "ending") continue;

            if (section.choices != null)
            {
                foreach (var choice in section.choices)
                {
                    if (!story.sections.ContainsKey(choice.targetSection.ToString()))
                        return StoryValidationResult.Fail(
                            $"Section {sectionId}: choice targets non-existent section {choice.targetSection}");
                }
            }

            if (section.type == "narrative" && (section.choices == null || section.choices.Length == 0))
                return StoryValidationResult.Fail($"Narrative section {sectionId} has no choices and is not an ending");
        }

        return StoryValidationResult.Ok;
    }
}
```

---

## 4. PRIVACIDADE & COMPLIANCE

### 4.1 GDPR — Checklist de Conformidade

**Status do relatório:** Parcialmente coberto. O relatório menciona "Delete Account" e "Data Export", mas tem lacunas.

| Requisito GDPR | Status no Relatório | Ação Necessária |
|----------------|-------------------|-----------------|
| **Right to Access (Art. 15)** | ❌ Não mencionado | Criar GET /api/user/data → retorna todos os dados do usuário em JSON legível |
| **Right to Rectification (Art. 16)** | ❌ Não mencionado | Usuário pode editar profile via app; histórico de compras não é editável |
| **Right to Erasure (Art. 17)** | ⚠️ Parcial | Relatório propõe soft-delete com 7 dias. OK. Mas `purchases` são anonimizadas, não deletadas. Isso é correto (obrigação fiscal). Precisa documentar legal basis para retenção. |
| **Right to Portability (Art. 20)** | ✅ Existente | GET /api/user/export → ZIP. Formato JSON é adequado. |
| **Consent (Art. 7)** | ❌ Ausente | O app coleta analytics (Firebase Analytics + custom events). Precisa de consentimento explícito antes de qualquer tracking. |
| **Data Minimization (Art. 5)** | ⚠️ Parcial | O `storyProgress` armazena `purchaseToken` (token da loja). Isso é PII indireto. Minimizar: armazenar apenas hash. |
| **Privacy by Design (Art. 25)** | ❌ Não mencionado | DPIA (Data Protection Impact Assessment) necessário antes do lançamento. |
| **Data Breach Notification (Art. 33)** | ❌ Ausente | Sem plano de resposta a incidentes de segurança documentado. |
| **DPO Appointment** | ❌ Ausente | Para um app que processa dados de crianças (gamebook), um DPO pode ser obrigatório dependendo da escala. |

🟡 **Ação: Privacy Policy + Consent Flow**

```
┌─────────────────────────────────────────────────────────────┐
│         FLUXO DE CONSENTIMENTO (PRIMEIRO LAUNCH)             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Tela 1: Boas-vindas                                        │
│  ┌────────────────────────────────────────┐                │
│  │  Bem-vindo ao Interactive Fantastic    │                │
│  │  Tales!                                │                │
│  │                                        │                │
│  │  Antes de começar, precisamos da sua   │                │
│  │  autorização para:                     │                │
│  │                                        │                │
│  │  ☐ Analytics (eventos de jogo anônimos)│                │
│  │    Para melhorar o app e recomendar    │                │
│  │    histórias.                          │                │
│  │                                        │                │
│  │  ☐ Crashlytics (relatórios de erro)    │                │
│  │    Essencial para corrigir bugs.       │                │
│  │                                        │                │
│  │  ☐ Personalização (recomendações)     │                │
│  │    Baseado nas histórias que você joga.│                │
│  │                                        │                │
│  │  [Ver Política de Privacidade]         │                │
│  │                                        │                │
│  │  [Continuar]                           │                │
│  └────────────────────────────────────────┘                │
│                                                             │
│  Consentimento granular:                                    │
│  - Analytics: opcional (default: desligado)                 │
│  - Crashlytics: legítimo interesse (sempre ligado)          │
│  - Personalização: opcional (default: desligado)            │
│  - Tudo pode ser alterado em Settings → Privacidade         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 COPPA / Crianças

**Problema crítico:** O GDD define o produto como "gamebooks digitais interativos" com "fantasy tales". O gênero gamebook/livro-jogo é historicamente infantojuvenil ( Fighting Fantasy original é para 9-14 anos). Se o app é **direcionado a crianças** (ou atrai crianças), COPPA se aplica.

🔴 **Análise de risco COPPA:**

| Fator | Evidência | Risco |
|-------|-----------|-------|
| Tema | Fantasia, dragões, aventuras — apelo infantil | ALTO |
| Visual | Ilustrações estilo livro de fantasia | ALTO |
| UI | Modo swipe, modo voz — controles simplificados que crianças podem usar | MÉDIO |
| Monetização | IAP dentro do app — COPPA restringe | ALTO |
| Dados | Firebase Analytics coleta eventos de jogo | ALTO |
| Auth | Login Google — crianças < 13 não podem ter conta Google (ToS) | MÉDIO |

**Opções:**

```
OPÇÃO A: App não é direcionado a crianças (age-gate)
  - Tela de idade no primeiro launch
  - Se < 13 (ou < 16 para GDPR): modo limitado
    → Sem IAP
    → Sem analytics além do essencial
    → Sem login Google obrigatório (anon apenas)
    → Sem dados comportamentais
    → Conteúdo filtrado (sem histórias com tag "mature")

OPÇÃO B: App é direcionado a crianças (COPPA-compliant)
  - Precisa de consentimento parental verificável
  - Zero tracking comportamental
  - IAP com PIN parental
  - COPPA Safe Harbor certification
  - Muito mais complexo legalmente

RECOMENDAÇÃO: OPÇÃO A com age-gate robusto
```

🟡 **Implementação do Age Gate:**

```csharp
public class AgeGateScreen : MonoBehaviour
{
    [SerializeField] private TMP_InputField yearInput;
    [SerializeField] private int minimumAge = 13; // COPPA threshold

    public void OnContinue()
    {
        if (!int.TryParse(yearInput.text, out int birthYear))
        {
            ShowError("Ano inválido");
            return;
        }

        int age = DateTime.UtcNow.Year - birthYear;
        if (age < minimumAge)
        {
            // Modo infantil
            PlayerPrefs.SetString("account_type", "child");
            PlayerPrefs.SetInt("birth_year", birthYear);

            // Desabilitar tracking
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(false);

            // Desabilitar IAP
            IAPManager.Instance.DisablePurchases();

            // Filtrar conteúdo
            StoryFilter.EnableChildSafeMode();
        }
        else
        {
            PlayerPrefs.SetString("account_type", "adult");
            PlayerPrefs.SetInt("birth_year", birthYear);

            // Mostrar consentimento GDPR
            ConsentScreen.Show();
        }

        ScreenNavigator.GoToMainHub();
    }
}
```

### 4.3 Analytics Privacy — PII Detection

**Problema:** O relatório define eventos customizados como:

```csharp
story_search_performed  // params: { query, resultCount }
choice_made             // params: { storyId, sectionId, choiceIndex, choiceText }
```

🟡 **Risco:** `query` (texto de busca) e `choiceText` (texto da escolha) podem conter PII se o usuário digitar "meu nome é João da Silva" na busca, ou se o texto da escolha contiver informação pessoal. Firebase Analytics **proíbe PII** nos eventos.

**Solução:**

```csharp
public class AnalyticsService
{
    // Sanitização antes de enviar qualquer evento
    public void LogSearchPerformed(string query, int resultCount)
    {
        var sanitizedQuery = SanitizeForAnalytics(query, maxLength: 50);
        FirebaseAnalytics.LogEvent("story_search_performed",
            new Parameter("query_hash", HashForAnalytics(query)), // hash, não plaintext
            new Parameter("query_length", query.Length),
            new Parameter("result_count", resultCount)
        );
    }

    public void LogChoiceMade(string storyId, int sectionId, int choiceIndex, string choiceText)
    {
        // NUNCA enviar choiceText para analytics — pode conter PII
        FirebaseAnalytics.LogEvent("choice_made",
            new Parameter("story_id", storyId),
            new Parameter("section_id", sectionId),
            new Parameter("choice_index", choiceIndex),
            new Parameter("choice_text_length", choiceText?.Length ?? 0)
            // choiceText NÃO é enviado
        );
    }

    // Política de retenção de dados no Firebase Analytics:
    // Configurar para 14 meses (mínimo GDPR-friendly)
    // firebaseAnalytics.SetDataRetentionDuration(14 meses);
}
```

### 4.4 Data Retention após Account Deletion

**Problema:** O relatório propõe soft-delete de 7 dias + hard delete. Mas não especifica o que acontece com saves quando a **subscription expira** (não quando a conta é deletada).

🟢 **Matriz de retenção:**

| Evento | Dados de Jogo | Dados de Compra | Dados de Perfil |
|--------|--------------|-----------------|-----------------|
| **Subscription expira** | Mantidos (histórias compradas separadamente) | Mantidos | Mantidos |
| **Histórias exclusivas de subscription** | Inacessíveis, mas mantidos (reativados se reassinar) | N/A | N/A |
| **Conta deletada** | Deletados após 7 dias | Anonimizados (userId → hash) | Deletados após 7 dias |
| **Inatividade (2 anos)** | N/A (Firestore não cobra por storage ocioso) | N/A | Pode anonimizar analytics |
| **Solicitação GDPR** | Deletados em 30 dias | Anonimizados | Deletados |

---

## 5. CUSTO & PERFORMANCE

### 5.1 Cenários de Pico de Custo

**Problema:** A estimativa do relatório é otimista demais:

> "10.000 usuários × 30 MB médio = 300 GB → Custo: 300 GB × $0.12/GB = $36.00"

**Cenário de viralização não considerado:**

```
┌─────────────────────────────────────────────────────────────┐
│         CENÁRIO DE PICO DE CUSTO (VIRAL)                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Cenário: 100.000 downloads em 24h                          │
│                                                             │
│  Cloud Storage Egress (download de assets):                 │
│    - 100k × 30 MB = 3 TB                                    │
│    - Primeiros 1 TB: $0.12/GB = $120                        │
│    - Próximos 9 TB: $0.11/GB = $990                         │
│    - Total egress: ~$1,230/dia (!!!)
│                                                             │
│  Firestore Reads (catálogo + library + saves):               │
│    - 100k × 50 reads/sessão = 5M reads                     │
│    - Custo: 5M × $0.06/100k = $3.00 (baixo)                │
│                                                             │
│  Cloud Run (verificação de compra):                          │
│    - 5% convertem (5k compras)                              │
│    - Custo: $0.50 (baixo)                                   │
│                                                             │
│  CUSTO TOTAL (1 dia viral): ~$1,234                         │
│  CUSTO TOTAL (1 mês nesse ritmo): ~$37,000                  │
│                                                             │
│  Isso quebra o modelo de negócio se a receita média         │
│  por usuário for < $0.37.                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

🔴 **Mitigações essenciais:**

```
┌─────────────────────────────────────────────────────────────┐
│         ESTRATÉGIA DE OTIMIZAÇÃO DE CUSTOS                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. CLOUD CDN (OBRIGATÓRIO ANTES DO LANÇAMENTO):            │
│     - Configurar CDN na frente do Cloud Storage             │
│     - Cache de assets por 30 dias (versionados por hash)    │
│     - Egress do CDN é MAIS BARATO que egress direto:        │
│       - Cloud CDN egress: $0.02-0.08/GB vs $0.12/GB        │
│     - Cache hit de 80% reduz custo em 60%                   │
│                                                             │
│  2. BUNDLE DE ASSETS (OPCIONAL):                            │
│     - Em vez de 30 arquivos separados, empacotar em         │
│       AssetBundle (Unity) ou .zip por história              │
│     - 1 download de 30 MB vs 30 downloads de 1 MB          │
│     - Reduz overhead de HTTP e retries                      │
│                                                             │
│  3. DOWNLOAD SOB DEMANDA (CRÍTICO):                         │
│     - Não baixar TODOS os assets de uma vez                 │
│     - Fase 3 (story.json) → usuário já pode jogar           │
│     - Fase 4 (assets): baixar apenas assets visíveis        │
│       + prefetch das próximas 3 seções                     │
│     - Usuário que joga 20 min (10 seções) baixa             │
│       10 ilustrações, não 100                                │
│     - Reduz egress por usuário em 60-80%                    │
│                                                             │
│  4. THUMBNAILS EM CDN (SEMPRE):                             │
│     - thumbnails/cover_128.webp → Cloud CDN, cache 7 dias  │
│     - Tamanho otimizado: WebP lossy, 128×128                │
│     - 200 thumbnails × 10 KB = 2 MB no total                │
│                                                             │
│  5. COMPRESSÃO AGRESSIVA:                                   │
│     - Áudio: OGG Vorbis em vez de MP3 (melhor compressão)   │
│     - Imagens: WebP em vez de PNG (perda mínima, 40% menor) │
│     - story.json: Brotli compression no CDN (70% menor)     │
│                                                             │
│  6. BUDGET ALERT (OBRIGATÓRIO):                             │
│     - Google Cloud Budget Alert:                             │
│       - 50% do orçamento mensal → email                     │
│       - 80% → SMS + PagerDuty                               │
│       - 100% → desabilitar downloads automáticos,           │
│         manter apenas jogo offline                          │
│     - Cloud Function para monitorar egress/dia              │
│                                                             │
│  7. ESTRATÉGIA ALTERNATIVA (PLANO B):                       │
│     - Se o custo de egress se tornar proibitivo:            │
│       - Migrar assets para distribuição via CDN próprio      │
│         (BunnyCDN: $0.01/GB, 10x mais barato)              │
│       - Ou: modelo de download via torrent para PC           │
│         (com seedbox dos assets oficiais)                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Otimização de Queries — Catálogo

**Problema:** A query do catálogo:

```csharp
db.Collection("stories")
  .WhereEqualTo("metadata.isPublished", true)
  .OrderByDescending("stats.purchaseCount")
  .Limit(20)
```

Carrega 20 documentos inteiros (incluindo `previewSections`, `pricing`, `storage`, `stats`). Se cada documento tem ~30 KB (com preview sections), isso são 600 KB para a tela de catálogo.

🟡 **Otimização:**

```csharp
// Usar projection query para carregar apenas os campos necessários
public async Task<List<StoryCardData>> GetCatalogCards(string genre = null, int limit = 20)
{
    var query = db.Collection("stories")
        .WhereEqualTo("metadata.isPublished", true);

    if (!string.IsNullOrEmpty(genre))
        query = query.WhereArrayContains("metadata.genre", genre);

    query = query.OrderByDescending("stats.purchaseCount").Limit(limit);

    // Projection: só carregar campos da UI do card
    // (reduz de ~30 KB para ~1 KB por documento = 20x menos dados)
    var snapshot = await query.Select(
        "metadata.title",
        "metadata.authorName",
        "metadata.genre",
        "metadata.estimatedDurationMinutes",
        "pricing.type",
        "pricing.priceTier",
        "stats.averageRating",
        "stats.ratingCount",
        "storage.thumbnailSmallPath"  // URL para thumbnail 128px
    ).GetSnapshotAsync();

    return snapshot.Documents.Select(doc => new StoryCardData
    {
        StoryId = doc.Id,
        Title = doc.GetValue<string>("metadata.title"),
        AuthorName = doc.GetValue<string>("metadata.authorName"),
        Genres = doc.GetValue<List<string>>("metadata.genre"),
        DurationMinutes = doc.GetValue<int>("metadata.estimatedDurationMinutes"),
        PricingType = doc.GetValue<string>("pricing.type"),
        PriceTier = doc.GetValue<string>("pricing.priceTier"),
        AverageRating = doc.GetValue<double>("stats.averageRating"),
        RatingCount = doc.GetValue<int>("stats.ratingCount"),
        ThumbnailUrl = doc.GetValue<string>("storage.thumbnailSmallPath"),
    }).ToList();
}
```

**IMPORTANTE:** Projection queries (`Select`) no Firestore exigem índices nos campos projetados, mesmo que a query não tenha `OrderBy` explícito nesses campos. Os campos em `Select` precisam estar no índice.

### 5.3 Cold Start — Tempo até a Store Visível

**Problema:** Quando o usuário abre o app pela primeira vez após instalar, a sequência de operações é:

```
1. Firebase Init (CheckAndFixDependenciesAsync)    ~500ms-2s
2. Firebase Auth (SignInAnonymouslyAsync)           ~200ms-1s
3. Firestore Query (catálogo)                       ~200ms-1s
4. Cloud Storage (thumbnails - N requisições)       ~100ms-500ms cada
   → 20 thumbnails × 200ms = 4s
5. Renderizar UI                                    ~100ms

TOTAL (pior caso): ~7 segundos
TOTAL (com cache offline vazio): ~4 segundos
```

🟢 **Otimizações de Cold Start:**

```
┌─────────────────────────────────────────────────────────────┐
│         ESTRATÉGIA DE COLD START                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. SPLASH SCREEN ENQUANTO INICIALIZA:                      │
│     - Logo + "Preparando suas aventuras..."                 │
│     - Barra de progresso indeterminada                      │
│     - Timeout máximo: 10 segundos                           │
│     - Se > 5s: mostrar dica ou lore quote                  │
│                                                             │
│  2. BUNDLE DE THUMBNAILS (RECOMENDADO):                     │
│     - Em vez de 20 HTTP requests por thumbnail,             │
│       criar um spritesheet ou texture atlas com              │
│       todos os thumbnails pequenos                          │
│     - Ou: gerar thumbnails em base64 e incluir              │
│       na resposta do catálogo (Firestore)                   │
│     - 20 thumbnails × 5 KB base64 = 100 KB no doc           │
│     - Viável se o catálogo é pequeno (< 200 stories)        │
│                                                             │
│  3. PLACEHOLDER SKELETON (OBRIGATÓRIO):                     │
│     - Mostrar cards skeleton imediatamente                  │
│     - Preencher com dados reais conforme chegam             │
│     - Thumbnails carregam com fade-in                       │
│     - Usuário percebe app como "rápido"                     │
│                                                             │
│  4. CACHE DE CATÁLOGO (Firestore offline):                  │
│     - Após primeiro load, catálogo persiste offline         │
│     - Próximo cold start: catálogo instantâneo              │
│     - Background refresh para verificar mudanças            │
│                                                             │
│  5. PRE-FETCH NO BACKGROUND (ANDROID):                      │
│     - Usar WorkManager para pre-fetch do catálogo           │
│       quando dispositivo está em Wi-Fi e carregando         │
│     - App abre e catálogo já está em cache                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 5.4 Tamanho do Cache Local vs Storage do Dispositivo

**Problema:** O relatório propõe:
- Limite de cache: **500 MB** (soft) / **10 histórias**
- Cada história: ~31 MB

Em dispositivos com 32 GB (comuns no mercado brasileiro), o usuário médio tem 5-10 GB livres. 500 MB é 5-10% do espaço livre. O WhatsApp ocupa 2-5 GB. Uma história de 31 MB é significativa.

🟢 **Solução: Cache Adaptativo**

```csharp
public class AdaptiveCacheManager
{
    public long CalculateMaxCacheBytes()
    {
        // Detectar espaço livre
        var driveInfo = new DriveInfo(Application.persistentDataPath);
        var freeSpaceBytes = driveInfo.AvailableFreeSpace;

        // Cache máximo = min(10% do espaço livre, 500 MB, máximo configurado)
        var maxByFreeSpace = (long)(freeSpaceBytes * 0.10);
        var maxByConfig = 500_000_000L; // 500 MB default

        return Math.Min(maxByFreeSpace, maxByConfig);
    }

    public int CalculateMaxStories()
    {
        var maxBytes = CalculateMaxCacheBytes();
        var avgStorySize = CalculateAverageStorySize();
        return (int)(maxBytes / avgStorySize);
    }
}
```

---

## 6. EDGE CASES & FAILURE MODES

### 6.1 Matriz de Edge Cases de Compra

| Cenário | Descrição | Comportamento Esperado |
|---------|-----------|----------------------|
| **Android → iOS** | Comprou no Android, trocou para iPhone | Restore via Firestore (`ownedStoryIds`). Se IAP: Google Play não restaura na App Store. Solução: usuário faz login com mesma conta Google/Firebase → `ownedStoryIds` sincroniza. Se comprou via IAP (não account-based), precisa recomprar. Comunicar claramente. |
| **Comprou deslogado → logou** | Usuário anônimo compra história, depois faz login Google | **Crítico!** Entitlements do usuário anônimo precisam ser transferidos para a conta Google. Firebase Auth permite `LinkWithCredentialAsync`. Mas se o usuário já tem uma conta Google com compras próprias, precisa de MERGE de entitlements. |
| **Gift Purchase** | Usuário A compra história para usuário B | Não suportado pelo Google Play (IAP é pessoal). Alternativa: "gift codes" gerados pelo backend, resgatáveis no app. |
| **Promo Code Expirado** | Usuário tenta resgatar código promocional vencido | Backend verifica `expiresAt`. Se expirado: "Este código expirou em DD/MM/AAAA". |
| **Subscription Cancelada** | Usuário cancela assinatura no meio do mês | Google Play/App Store mantém acesso até o fim do período pago. Após: bloquear histórias exclusive (exceto as "keep forever"). Avisar 7 dias antes. |
| **Reembolso** | Usuário pede reembolso via Google Play | Google notifica o app via `OnPurchaseFailed` ou RTDN (Real-Time Developer Notification). Cloud Run processa: revogar entitlement → remover de `ownedStoryIds` → `arrayRemove`. |
| **Multiple Accounts** | Mesmo dispositivo, 2 contas Google diferentes | Normal. Firestore particiona por `userId`. Cada conta vê suas próprias compras. |
| **Family Library (Google Play)** | Compra compartilhada com família | Google Play Family Library: `purchaseToken` é o mesmo para todos os membros. Backend precisa permitir múltiplos `userId` para o mesmo token. OU: desabilitar Family Library para IAP não-consumíveis (recomendado para conteúdo digital individual). |

### 6.2 Uninstall/Reinstall — Perda Total de Dados Locais

**Problema:** Se o usuário desinstala o app antes do cloud sync completar, todos os saves locais são perdidos permanentemente (Android `persistentDataPath` é deletado na desinstalação).

🟡 **Mitigação:**

```
┌─────────────────────────────────────────────────────────────┐
│         PROTEÇÃO CONTRA PERDA DE DADOS LOCAL                 │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. AUTO-SAVE AGGRESSIVO:                                   │
│     - Salvar na nuvem a cada transição de seção              │
│     - Salvar na nuvem ao pausar/minimizar o app             │
│     - Salvar na nuvem a cada 5 minutos de jogo              │
│                                                             │
│  2. BACKUP ANDROID (AUTO BACKUP):                           │
│     - Android 6+ tem Auto Backup (25 MB gratuito)           │
│     - Incluir save files no backup automático:              │
│       <application                                           │
│         android:allowBackup="true"                          │
│         android:fullBackupContent="@xml/backup_rules">       │
│     - backup_rules.xml:                                     │
│       <full-backup-content>                                 │
│         <include domain="file" path="stories" />            │
│         <include domain="sharedpref" path="." />            │
│       </full-backup-content>                                │
│     - Limitação: 25 MB. Histórias baixadas (30 MB cada)     │
│       NÃO cabem. Apenas saves e metadados.                  │
│                                                             │
│  3. INDICADOR DE SYNC STATUS:                               │
│     - Badge no save slot: "☁️ Salvo" (verde)                │
│       "🔄 Sincronizando..." (amarelo)                       │
│       "⚠️ Pendente" (vermelho — não sincronizado)           │
│     - Alerta ao sair do app: "Há saves não sincronizados"  │
│                                                             │
│  4. WELCOME BACK FLOW (após reinstalação):                  │
│     - Login → detecta que é dispositivo novo               │
│     - "Bem-vindo de volta! Seus saves foram restaurados."  │
│     - Mostrar último save sincronizado para cada história   │
│     - Se nada foi sincronizado: mostrar estado vazio         │
│       com CTA "Começar uma nova aventura"                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 6.3 Account Deletion Mid-Game

**Problema:** Usuário está jogando ativamente (seção 87) e deleta a conta pelo site/web (não pelo app). O app não sabe que a conta foi deletada e tenta salvar progresso. O Firestore retorna `PERMISSION_DENIED`.

🟢 **Comportamento:**

```
1. App tenta salvar → retorna erro de permissão
2. App verifica se o token ainda é válido (Auth.CurrentUser)
3. Se token inválido/conta deletada:
   → Mostrar diálogo: "Sua conta foi encerrada. Seus dados
      foram removidos. Deseja continuar com uma nova conta?"
   → Salvar progresso LOCALMENTE (não perde o jogo atual)
   → Oferecer criar nova conta OU continuar anônimo
4. Próximo save: associado à nova conta
```

### 6.4 Network Flapping (Wi-Fi instável)

**Problema:** Wi-Fi conecta/desconecta rapidamente durante download de 30 MB. O `UnityWebRequest` padrão não tem resume bom. O download reinicia do zero.

🟡 **Solução: Download Resumível com Range Requests**

```csharp
public class ResumableDownloader
{
    private const int CHUNK_SIZE = 256 * 1024; // 256 KB por chunk

    public async Task<DownloadResult> DownloadWithResume(
        string url, string destinationPath, IProgress<float> progress,
        CancellationToken cancellationToken)
    {
        var tempPath = destinationPath + ".partial";
        long downloadedBytes = 0;

        // Verificar se existe download parcial
        if (File.Exists(tempPath))
        {
            downloadedBytes = new FileInfo(tempPath).Length;
        }

        // Tentar até 5 vezes com exponential backoff
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using (var request = new UnityWebRequest(url, "GET"))
                {
                    // Range request: continuar de onde parou
                    if (downloadedBytes > 0)
                    {
                        request.SetRequestHeader("Range",
                            $"bytes={downloadedBytes}-");
                    }

                    request.downloadHandler = new DownloadHandlerFile(tempPath,
                        append: downloadedBytes > 0);

                    var operation = request.SendWebRequest();

                    // Timeout por chunk: 30 segundos sem progresso
                    var lastProgress = downloadedBytes;
                    float timeout = 30f;

                    while (!operation.isDone && !cancellationToken.IsCancellationRequested)
                    {
                        await UniTask.Yield();

                        if (request.downloadedBytes > lastProgress)
                        {
                            lastProgress = request.downloadedBytes;
                            timeout = 30f; // resetar timeout
                            progress?.Report((float)(downloadedBytes + request.downloadedBytes)
                                / request.GetResponseHeader("Content-Length") != null
                                    ? long.Parse(request.GetResponseHeader("Content-Length"))
                                    : downloadedBytes + 1000000);
                        }
                        else
                        {
                            timeout -= Time.deltaTime;
                            if (timeout <= 0)
                            {
                                request.Abort(); // timeout, vai para retry
                                break;
                            }
                        }
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Download completo → renomear temp para final
                        if (File.Exists(destinationPath))
                            File.Delete(destinationPath);
                        File.Move(tempPath, destinationPath);
                        return DownloadResult.Success;
                    }

                    // Falhou → preparar para retry
                    downloadedBytes = new FileInfo(tempPath).Length;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Download attempt {attempt + 1} failed: {ex.Message}");
            }

            // Exponential backoff: 1s, 2s, 4s, 8s, 16s
            await UniTask.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }

        // Todas as tentativas falharam
        return DownloadResult.Failed(downloadedBytes);
    }
}
```

**Importante:** Cloud Storage suporta `Range` headers por padrão (HTTP 206 Partial Content). Não precisa de configuração especial.

### 6.5 Storage Full (Zero Bytes Livres)

**Problema:** Dispositivo com 0 bytes livres tenta baixar assets. O download falha silenciosamente com `IOException: No space left on device`.

🟡 **Solução:**

```csharp
public class StorageAwareDownloadManager : DownloadManager
{
    private const long MIN_FREE_SPACE_BYTES = 100_000_000; // 100 MB

    public async Task<DownloadResult> DownloadStory(string storyId,
        IProgress<float> progress)
    {
        // Verificar espaço ANTES de iniciar download
        var freeSpace = GetFreeSpace();
        var requiredSpace = await GetRemoteStorySize(storyId);

        if (freeSpace < requiredSpace + MIN_FREE_SPACE_BYTES)
        {
            // Não tem espaço suficiente
            var missingSpace = requiredSpace + MIN_FREE_SPACE_BYTES - freeSpace;

            return DownloadResult.InsufficientSpace(
                requiredSpace,
                freeSpace,
                missingSpace,
                OnInsufficientSpaceResult.Continue,
                OnInsufficientSpaceResult.Cancel,
                OnInsufficientSpaceResult.ManageCache
            );
        }

        // Prosseguir com download (com verificações periódicas de espaço)
        return await DownloadWithSpaceCheck(storyId, requiredSpace, progress);
    }

    private async Task<DownloadResult> DownloadWithSpaceCheck(
        string storyId, long requiredSpace, IProgress<float> progress)
    {
        // ... download normal com resume ...

        // Verificar espaço a cada 1 MB baixado
        // Se espaço caiu abaixo do mínimo durante o download:
        //   → Pausar, avisar usuário, oferecer "liberar espaço"
    }

    private long GetFreeSpace()
    {
        var driveInfo = new DriveInfo(Application.persistentDataPath);
        return driveInfo.AvailableFreeSpace;
    }
}
```

**UX para storage full:**

```
┌─────────────────────────────────────────────────────────────┐
│         UX — ARMAZENAMENTO CHEIO                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Cenário 1: Download não inicia (espaço insuficiente):      │
│  ┌────────────────────────────────────────┐                │
│  │  💾 Espaço Insuficiente                │                │
│  │                                        │                │
│  │  Esta história precisa de 30 MB.       │                │
│  │  Seu dispositivo tem apenas 15 MB      │                │
│  │  livres.                               │                │
│  │                                        │                │
│  │  [Liberar Espaço (gerenciar cache)]    │                │
│  │  [Cancelar]                            │                │
│  └────────────────────────────────────────┘                │
│                                                             │
│  Cenário 2: Jogo em andamento, espaço acaba:                │
│  - GameEngine detecta falha ao salvar localmente            │
│  - Alerta: "Sem espaço para salvar. Libere espaço ou        │
│    seu progresso será perdido ao fechar o app."            │
│  - Oferecer: salvar na nuvem (se online)                    │
│  - Oferecer: gerenciar cache (remover histórias antigas)    │
│                                                             │
│  Cenário 3: Cache eviction automática (sem perguntar):      │
│  - NUNCA remover história em progresso                      │
│  - Remover histórias completadas + 30 dias sem jogar        │
│  - Remover histórias nunca jogadas (só na biblioteca)       │
│  - Notificação: "Removemos 3 histórias antigas do cache     │
│    para liberar espaço. Você pode baixá-las novamente."     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 7. FUNCIONALIDADES AUSENTES

### 7.1 Content Moderation (Revisão de Histórias)

🔴 **Problema:** O fluxo de publicação (Editor → Publish → disponível na Store) **não tem etapa de moderação**. Qualquer autor pode publicar qualquer conteúdo — incluindo discurso de ódio, conteúdo sexual, violência extrema, ou material protegido por copyright.

**Solução:**

```
┌─────────────────────────────────────────────────────────────┐
│         PIPELINE DE MODERAÇÃO DE CONTEÚDO                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ETAPA 1 — Auditoria Automática (pré-publish):              │
│  - Flow Analyzer (já existe no editor)                      │
│  - Content Scanner:                                         │
│    - Palavras proibidas em todos os idiomas                 │
│    - Detecção de conteúdo adulto via Perspective API        │
│      (Google Cloud Natural Language)                       │
│    - Verificação de copyright (hash de imagens)             │
│  - Score de moderação automatizado (0-100)                  │
│  - Se score < 80: bloqueado, requer revisão manual          │
│                                                             │
│  ETAPA 2 — Revisão Manual (pós-publish, pre-listing):       │
│  - Fila de moderação para equipe                            │
│  - Interface de revisão: preview completo + flagged          │
│    sections + decisão (approve/reject/request changes)      │
│  - SLA: 48h para primeira revisão                           │
│  - Autores com bom histórico: moderação acelerada           │
│                                                             │
│  ETAPA 3 — Denúncia de Usuário (pós-publish):               │
│  - Botão "Reportar" na tela de detalhes da história         │
│  - Categorias: conteúdo impróprio, copyright, bug, outro    │
│  - Report → cria ticket no Firestore                        │
│  - Múltiplos reports (threshold: 5) → flag automática       │
│    para re-revisão                                          │
│                                                             │
│  ETAPA 4 — Age Rating (por história):                       │
│  - Cada história tem metadata.ageRating:                    │
│    - "everyone" (livre)                                     │
│    - "teen" (12+)                                           │
│    - "mature" (16+)                                         │
│    - "adult" (18+)                                          │
│  - Determinado por: conteúdo de combate, temas, linguagem   │
│  - Filtro na Store baseado na idade do usuário              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Parental Controls

🟡 **Problema:** Conectado ao age-gate (seção 4.2). Se uma criança < 13 usa o app, os pais precisam de controles.

**Funcionalidades necessárias:**

```
┌─────────────────────────────────────────────────────────────┐
│         PARENTAL CONTROLS                                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. PURCHASE PIN:                                           │
│     - PIN de 4 dígitos configurável nos Settings            │
│     - Necessário para: comprar histórias, assinar,          │
│       resgatar códigos                                      │
│     - Tentativas: 3 erradas → bloqueio de 15 min            │
│                                                             │
│  2. CONTENT FILTER:                                         │
│     - Por age rating (everyone / teen / mature / adult)     │
│     - Configurável pelos pais (PIN necessário)               │
│     - Default: baseado na idade do perfil                   │
│                                                             │
│  3. PLAYTIME LIMITS:                                        │
│     - Tempo máximo de jogo por dia                          │
│     - Configurável: 30min, 1h, 2h, ilimitado               │
│     - Alerta 5 min antes do limite                          │
│     - Bloqueio após atingir limite                          │
│                                                             │
│  4. SCREEN TIME REPORT (para pais):                         │
│     - Dashboard simples: tempo jogado por história,         │
│       por dia, histórias jogadas                            │
│     - Acessível via PIN                                     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 7.3 Acessibilidade da Store UI

🟡 **Problema:** O GDD dedica uma fase inteira (Fase 3: Semanas 15-20) à acessibilidade do **jogo** (texto narrativo, escolhas, combate). Mas a **Store UI** (catálogo, detalhes, biblioteca, perfil) também precisa ser acessível.

**Checklist de acessibilidade para Store UI:**

| Elemento | Requisito WCAG 2.1 AA |
|----------|----------------------|
| Story cards | Focusable, label descritivo ("História: A Montanha do Mago de Fogo, 4.2 estrelas, R$ 9,99") |
| Rating stars | Texto alternativo: "4.2 de 5 estrelas, 328 avaliações" |
| Botão Comprar | Contraste mínimo 4.5:1, label claro, focus indicator |
| Filtros/Search | Navegáveis por teclado, anunciados por screen reader |
| Download progress | Anunciar "% baixado" para screen reader |
| Toast/notificações | `aria-live` region para anúncios dinâmicos |
| Navegação por abas | Roles corretos (`tablist`, `tab`, `tabpanel`) |
| Estados vazios | Screen reader anuncia: "Nenhuma história na biblioteca. Visite a Loja para descobrir aventuras." |
| Estados de erro | Screen reader anuncia o erro e as ações disponíveis |

**Como implementar no Unity (UI Toolkit / uGUI):**

```csharp
// Unity não tem acessibilidade nativa para UI (apenas para texto).
// Solução: usar o Accessibility Plugin do Unity ou implementar manualmente.

public class AccessibleStoryCard : MonoBehaviour
{
    public void ApplyAccessibility()
    {
        // Para Android TalkBack / iOS VoiceOver
        var card = GetComponent<Selectable>();
        var titleText = GetComponentInChildren<TextMeshProUGUI>();
        var ratingText = GetComponentInChildren<RatingStars>();
        var priceText = GetComponentInChildren<PriceTag>();

        // Construir descrição acessível
        var description = $"História: {titleText.text}. " +
                         $"Avaliação: {ratingText.Value} de 5 estrelas. " +
                         $"Preço: {priceText.Text}.";

        // Plataformas mobile: setar contentDescription
        #if UNITY_ANDROID
        // Usar AccessibilityNodeInfo compatível
        #elif UNITY_IOS
        // Usar UIAccessibility
        #endif

        // Atributos ARIA-equivalentes no Unity UI
        gameObject.name = description; // fallback para screen reader
    }
}
```

🔵 **Nota:** Para a Web (Editor), acessibilidade WCAG é muito mais fácil com React + ARIA. Mas para o Player (Unity), é significativamente mais complexo. Considerar WebGL para a Store UI (embutida em WebView) em vez de Unity UI nativa, para aproveitar a acessibilidade do browser.

### 7.4 Analytics Dashboard para Autores

🟢 **Problema:** O GDD menciona "escritores" como público-alvo do Editor, mas não há menção a um **dashboard de analytics** para autores verem como suas histórias estão performando.

**Funcionalidades desejadas:**

```
┌─────────────────────────────────────────────────────────────┐
│         AUTHOR DASHBOARD (editor.interactive...com/dashboard)│
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Métricas por história:                                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  📊 Visão Geral                                      │   │
│  │  ┌──────────┬──────────┬──────────┬──────────┐      │   │
│  │  │ 1,247    │ 843      │ 67.5%    │ ★★★★☆    │      │   │
│  │  │ Compras  │ Jogadores│ Complet. │ Média    │      │   │
│  │  └──────────┴──────────┴──────────┴──────────┘      │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│  Gráficos:                                                  │
│  - Compras por dia/semana/mês                               │
│  - Receita total e por plataforma                          │
│  - Heatmap de escolhas: qual caminho os jogadores tomam?   │
│  - Mapa de calor: seções mais visitadas                    │
│  - Endings alcançados (% cada final)                       │
│  - Taxa de abandono por seção (onde jogadores desistem?)   │
│  - Tempo médio de jogo                                     │
│  - Distribuição de ratings                                 │
│                                                             │
│  Dados exportáveis: CSV, PDF                                │
│  Atualização: diária (não tempo real)                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Implementação:** Cloud Function que agrega eventos do Firestore + Analytics e materializa em uma coleção `author_stats/{authorId}/{storyId}/daily/{date}`.

### 7.5 AB Testing & Feature Flags

🟢 **Problema:** O plano de rollout é faseado (Fases A-E), mas não há mecanismo para:
- Habilitar/desabilitar features remotamente (feature flags)
- Fazer AB test de novas funcionalidades (ex: "qual layout de card vende mais?")
- Rollout gradual (10% dos usuários → 50% → 100%)

**Solução: Firebase Remote Config**

```csharp
public class RemoteConfigService
{
    private FirebaseRemoteConfig remoteConfig;

    public async Task InitializeAsync()
    {
        remoteConfig = FirebaseRemoteConfig.DefaultInstance;

        // Defaults (hardcoded no app, usados offline)
        var defaults = new Dictionary<string, object>
        {
            ["store_layout_version"] = "v1",        // "v1" | "v2_grid" | "v2_carousel"
            ["enable_search"] = "true",
            ["enable_subscription"] = "false",       // ainda não lançado
            ["max_cache_stories"] = "10",
            ["purchase_pin_required"] = "false",
            ["new_user_tutorial_enabled"] = "true",
            ["store_card_style"] = "default",        // "default" | "test_a" | "test_b"
        };
        await remoteConfig.SetDefaultsAsync(defaults);

        // Fetch com intervalo mínimo de 1 hora
        await remoteConfig.FetchAsync(TimeSpan.FromHours(1));
        await remoteConfig.ActivateAsync();
    }

    // Feature flag com AB test
    public string GetStoreCardStyle()
    {
        var style = remoteConfig.GetValue("store_card_style").StringValue;

        // Se "test_a" ou "test_b", o usuário foi randomizado
        // pelo Firebase AB Testing
        return style;
    }

    // Rollout gradual
    public bool IsSubscriptionEnabled()
    {
        return remoteConfig.GetValue("enable_subscription").BooleanValue;
    }
}
```

---

## 8. MATRIZ DE RECOMENDAÇÕES

### 🔴 CRÍTICO (Deve ser corrigido antes do lançamento)

| ID | Problema | Solução | Seção |
|----|----------|---------|-------|
| C1 | Firestore Security Rules inexistentes | Implementar regras completas (proposta na seção 1.1) + Firebase App Check | 1.1 |
| C2 | Compra sem idempotência — risco de cobrança sem entrega | Backend com transação + cache de idempotência + reconciliation job | 1.2, 2.4 |
| C3 | Auth APIs Cloud Run ausente — endpoints expostos | Firebase Auth token em todas as APIs; userId extraído do JWT | 1.3 |
| C4 | Assets de histórias pagas potencialmente públicos | Signed URLs com verificação de entitlement; bucket fechado | 1.4 |
| C5 | Sem proteção contra client-side tampering | App Check + validação server-side de limites + economia server-authoritative | 1.5 |
| C6 | Sem moderação de conteúdo — qualquer autor publica qualquer coisa | Pipeline de moderação: automática + manual + user reports + age rating | 7.1 |
| C7 | Sem age-gate — risco COPPA/GDPR-K | Age gate obrigatório no primeiro launch; modo infantil sem tracking/IAP | 4.2, 7.2 |
| C8 | Cenário de custo viral ($1.2k/dia) não mitigado | CDN obrigatório + budget alerts + download sob demanda | 5.1 |

### 🟡 ALTA PRIORIDADE (Deve ser corrigido no MVP)

| ID | Problema | Solução | Seção |
|----|----------|---------|-------|
| A1 | Conflito de save: last-write-wins é inaceitável | Three-way merge com auto-merge para campos independentes + UI para conflitos | 3.1 |
| A2 | Índices compostos ausentes — queries falham | firestore.indexes.json com índices para todas as queries do catálogo | 2.1 |
| A3 | Save offline vs cloud: sem atomicidade no batch | Batch Write no Firestore para todos os slots (atômico) | 3.3 |
| A4 | Schema migration — formato v1 → v2 sem estratégia | Migration pipeline com version gate + fallback + testes de regressão | 2.2 |
| A5 | Story version vs player save — seção removida = crash | Navigation validator que detecta seções órfãs e redireciona | 2.3 |
| A6 | Relógio do dispositivo vs servidor — inconsistência | ServerTimestamp em todo lugar; cliente usa UTC para UI apenas | 3.2 |
| A7 | Cache poisoning — story.json corrompido não detectado | SHA-256 verification + validação semântica ao carregar | 3.5, 2.1 |
| A8 | Uninstall/reinstall — perda de saves não sincronizados | Auto-backup Android + indicador de sync status + welcome-back flow | 6.2 |
| A9 | Download sem resume — redes instáveis | Range requests + resumable downloader com retry exponencial | 6.4 |
| A10 | Acessibilidade Store UI não planejada | Mesmo rigor de acessibilidade do jogo aplicado à Store | 7.3 |
| A11 | Consentimento GDPR ausente — analytics sem opt-in | Fluxo de consentimento granular no primeiro launch | 4.1 |
| A12 | Purchase PIN parental não planejado | PIN de 4 dígitos para compras + limites de tempo | 7.2 |

### 🟢 MÉDIA PRIORIDADE (Pode aguardar pós-MVP)

| ID | Problema | Solução | Seção |
|----|----------|---------|-------|
| M1 | Catálogo carrega documentos inteiros (30 KB cada) | Projection queries (Select) para carregar só campos do card | 5.2 |
| M2 | Cold start lento (4-7s até Store visível) | Skeleton loading + thumbnail bundle + cache agressivo | 5.3 |
| M3 | Sem feature flags para rollout gradual | Firebase Remote Config com AB testing | 7.5 |
| M4 | Sem analytics dashboard para autores | Cloud Function de agregação + dashboard web simples | 7.4 |
| M5 | `previewSections` no documento principal infla catálogo | Mover para subcoleção ou limitar a 1 seção teaser de 300 chars | 2.1 |
| M6 | `ownedStoryIds` cresce ilimitado (risco baixo) | Migrar para subcoleção se usuário > 200 histórias | 2.1 |
| M7 | Sem detecção de PII nos eventos analytics | Sanitização automática; hash em vez de plaintext em queries | 4.3 |
| M8 | Cache de 500 MB pode ser agressivo para 32 GB | Cache adaptativo baseado em espaço livre | 5.4 |
| M9 | Storage full durante jogo ativo sem UX clara | Alerta pró-ativo + opção de cloud save + eviction inteligente | 6.5 |
| M10 | Edge case: usuário anônimo compra → login Google | Merge de entitlements via Firebase Auth LinkWithCredential | 6.1 |

### 🔵 OBSERVAÇÕES (Nice to have / Watch items)

| ID | Observação | Seção |
|----|-----------|-------|
| O1 | Considerar PlayFab como alternativa a longo prazo (economia virtual pronta) | 8.3 do relatório |
| O2 | Monitorar quota do Firestore: 1 write/seg por documento (soft limit para subcoleções `saves`) | 2.1 |
| O3 | Cloud CDN signed URL verification para evitar hotlinking de assets mesmo com URL assinada | 1.4 |
| O4 | Implementar "wishlist" offline para compras — melhora UX em países com internet instável | 3.4 |
| O5 | Considerar WebGL + WebView para Store UI (acessibilidade nativa do browser) | 7.3 |
| O6 | GDPR: nomear DPO se escala > 100k usuários europeus ou qualquer dado de crianças | 4.1 |
| O7 | Testar cenário "100k downloads em 24h" em ambiente de staging com teste de carga | 5.1 |
| O8 | Log de auditoria de acesso a dados (quem leu/escreveu o quê, quando) para compliance | 4.1 |
| O9 | Criptografia client-side para saves locais (dificulta tampering casual, não é à prova de root) | 1.5 |
| O10 | Suporte Family Library (Google Play) — decidir se habilita ou bloqueia para conteúdo digital | 6.1 |

---

## 9. RESUMO EXECUTIVO

O sistema de story delivery proposto é **arquiteturalmente bem fundamentado** — o modelo híbrido Firestore + Cloud Storage, o phaseamento de implementação (A→E), e a estratégia de cache offline são decisões acertadas para o contexto do produto.

**Porém, existem 8 vulnerabilidades críticas que impedem o lançamento em produção:**

1. **Zero regras de segurança no Firestore** — qualquer pessoa pode acessar dados de todos os usuários
2. **Fluxo de compra sem idempotência** — risco real de cobrar sem entregar entitlements
3. **APIs Cloud Run sem autenticação** — endpoints críticos expostos
4. **Assets pagos potencialmente públicos** — download sem autorização
5. **Zero proteção contra client-side tampering** — economia do jogo manipulável
6. **Sem moderação de conteúdo** — risco legal e reputacional
7. **Sem age-gate** — risco COPPA/GDPR-K para um produto com apelo infantojuvenil
8. **Sem mitigação de custo de egress** — um dia viral custa R$ 6.800 em Cloud Storage

**Recomendação:** As 8 questões críticas DEVEM ser resolvidas antes do lançamento em produção. As 12 questões de alta prioridade devem ser endereçadas durante o MVP. O custo estimado de corrigir todas as questões críticas antes do lançamento é de 4-6 semanas de desenvolvimento, mas o custo de NÃO corrigi-las é uma combinação de: violação de dados, chargebacks em massa, banimento das lojas (Google Play/App Store), e multas regulatórias (LGPD/GDPR/COPPA).

O relatório original afirma: *"A maior fragilidade é o conflito de save offline-vs-cloud"*. Esta auditoria demonstra que existem fragilidades muito mais graves que devem ser priorizadas.

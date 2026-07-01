# IFT — Setup de Producao (Mock → Real)

## 1. Firebase Config (google-services.json)

1. Acesse [Firebase Console](https://console.firebase.google.com) → projeto `interactive-tales-2026`
2. Project Settings → Your apps → Add app → Unity/Android
3. Preencha o package name: `com.ift.player` (ou outro se alterado)
4. Baixe `google-services.json`
5. Coloque em `player-app/Assets/google-services.json`
6. Repita para iOS → baixe `GoogleService-Info.plist` → coloque em `player-app/Assets/`

## 2. Ativar Firebase Auth Providers

No Firebase Console → Authentication → Sign-in method:
- Ativar **Anônimo** (habilitado por padrao no app)
- Ativar **Google** (se quiser login social)
- Ativar **Email/Senha** (opcional)

## 3. Ativar Firestore

No Firebase Console → Firestore Database → Create database
- Modo de producao (com regras)
- Regiao: `us-central1` (ou `southamerica-east1` para menor latencia no Brasil)

## 4. Deploy Backend

```bash
# Certifique-se de ter:
# - Node.js 18+ instalado
# - Firebase CLI: npm install -g firebase-tools
# - gcloud CLI: https://cloud.google.com/sdk/docs/install

# Login
firebase login
gcloud auth login

# Deploy completo (Firestore + Hosting + Cloud Run)
deploy-all.bat
```

Apos deploy, copie a URL do Cloud Run e atualize nos arquivos:
- `player-app/Assets/_Game/Services/PurchaseService.cs` — linha 52
- `player-app/Assets/_Game/Services/SignedUrlService.cs` — linha 56

## 5. Seed Data

```bash
cd cloud-run
npm install
npx ts-node seed-firestore.ts
```

Isso popula 6 historias no Firestore para o catalogo inicial.

## 6. Configurar IAP

### Google Play Console
1. Criar produtos in-app (Non-consumable):
   - `com.ift.story.forest_of_doom` — R$ 4,99
   - `com.ift.story.mountain_of_fire` — R$ 9,99
   - `com.ift.story.vampire_crypts` — R$ 9,99
   - `com.ift.story.city_of_thieves` — R$ 14,99
2. Criar assinaturas:
   - `com.ift.premium.monthly` — R$ 14,99/mes
   - `com.ift.premium.yearly` — R$ 99,99/ano

### Apple App Store Connect
Mesmo procedimento com os mesmos IDs de produto.

## 7. Cloud Storage

1. Criar bucket no GCP Console (se nao existir): `interactive-tales-2026.appspot.com`
2. Criar estrutura de pastas:
   ```
   stories/
     demo/v1.0.0/
       data/story_1.0.0.json
       assets/images/
       assets/audio/
     mountain-of-fire/v2.1.0/
       ...
   ```
3. Configurar permissao: **NAO tornar publico** — o Cloud Run gera signed URLs

## 8. Verificar Funcionamento

1. Abra o Unity → Play Mode
2. Se `google-services.json` foi colocado, Firebase inicializa e:
   - Catalogo carrega do Firestore (se nao, fallback para mock)
   - Save/load sincroniza com Firestore
   - Compras verificam via Cloud Run
3. Verifique o console Unity para erros

### Troubleshooting

| Erro | Solucao |
|------|---------|
| Firebase dependencies not resolved | Abra Package Manager → aguarde resolver |
| `Failed to initialize Firebase` | Verifique google-services.json em Assets/ |
| `Firestore unavailable` | Verifique Firestore Database criado no Console |
| Cloud Run 401 | Verifique Firebase Auth providers ativados |
| Cloud Run 403 | Verifique entitlement do usuario no Firestore |

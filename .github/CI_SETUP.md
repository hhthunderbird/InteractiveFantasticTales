# CI/CD Setup Guide

## 1. Unity License (obrigatorio)

### Opcao A: Unity Personal (gratuito)
```bash
# Localmente, gere o arquivo de licenca:
# 1. Abra Unity Hub
# 2. Manage Licenses > Manual Activation > Save license request
# 3. Va para https://license.unity3d.com/manual
# 4. Upload o arquivo .alf, baixe o .ulf

# Converta o .ulf para base64 e adicione como secret:
cat Unity_v6000.x.ulf | base64 -w0
```

### Opcao B: Unity Pro/Plus
Configure os secrets:
- `UNITY_EMAIL` — email da conta Unity
- `UNITY_PASSWORD` — senha
- `UNITY_SERIAL` — numero de serie

## 2. GitHub Secrets Necessarios

Va em Settings > Secrets and variables > Actions > New repository secret:

| Secret | Descricao | Workflow |
|--------|-----------|----------|
| `UNITY_EMAIL` | Email Unity (Pro/Plus) | Tests |
| `UNITY_PASSWORD` | Senha Unity (Pro/Plus) | Tests |
| `UNITY_SERIAL` | Serial Unity (Pro/Plus) | Tests |
| `ANDROID_KEYSTORE_NAME` | Nome do keystore (ex: `ift.keystore`) | Android |
| `ANDROID_KEYSTORE_BASE64` | Keystore em base64 | Android |
| `ANDROID_KEYSTORE_PASS` | Senha do keystore | Android |
| `ANDROID_KEYALIAS_NAME` | Alias da chave | Android |
| `ANDROID_KEYALIAS_PASS` | Senha da chave | Android |
| `APPLE_TEAM_ID` | Apple Developer Team ID | iOS |
| `APPLE_DISTRIBUTION_CERT_BASE64` | Certificado .p12 em base64 | iOS |
| `APPLE_CERTIFICATE_PASSWORD` | Senha do certificado | iOS |
| `APPLE_PROVISIONING_PROFILE_BASE64` | Provisioning profile em base64 | iOS |
| `GCP_SA_KEY` | Service account key JSON | Backend |
| `FIREBASE_TOKEN` | `firebase login:ci` token | Backend |

## 3. Gerar Keystore Android

```bash
keytool -genkey -v -keystore ift.keystore -alias ift-release -keyalg RSA -keysize 2048 -validity 10000 -storepass SUA_SENHA -keypass SUA_SENHA -dname "CN=Interactive Fantastic Tales, OU=Dev, O=IFT, L=SP, ST=SP, C=BR"

# Converter para base64 para o secret:
base64 -w0 ift.keystore
```

## 4. Gerar Firebase Token

```bash
firebase login:ci
# Copie o token e adicione como FIREBASE_TOKEN secret
```

## 5. Workflows

| Workflow | Gatilho | O que faz |
|----------|---------|-----------|
| `build-android.yml` | Push main/develop, PR, manual | Build APK (debug) + AAB (release) |
| `build-ios.yml` | Push main, manual | Build Xcode project + opcional IPA |
| `deploy-backend.yml` | Push main (cloud-run/ + firestore.*) | Deploy Firestore rules/indexes + Cloud Run |
| `unity-tests.yml` | PR, manual | Roda EditMode + PlayMode tests |

## 6. Estrutura de Branches

```
main        ← Build AAB release + iOS IPA + deploy backend
develop     ← Build APK debug + rodar testes
feature/*   ← Rodar testes no PR
```

# Guia de Deploy no Google Cloud Platform (GCP)

Este guia documenta o processo de deploy do **Interactive Fantastic Tales** no Google Cloud Platform (GCP). Toda a infraestrutura foi preparada para permitir o deploy automatizado.

---

## 🛠️ Pré-requisitos para o Deploy

Antes de executar os scripts de deploy, a LLM ou desenvolvedor deve garantir que as seguintes ferramentas estejam instaladas e configuradas na máquina local:

1. **Google Cloud SDK (gcloud CLI)**:
   - Instalado e atualizado.
   - Autenticado com a conta correta rodando:
     ```bash
     gcloud auth login
     ```
   - Login de credenciais de aplicação (opcional, para emuladores/APIs):
     ```bash
     gcloud auth application-default login
     ```
2. **Node.js (v20+) & npm (v10+)**:
   - Necessário para compilar a SPA localmente se optar pelo deploy via Firebase Hosting.

---

## ⚙️ Configurando o Firebase e Variáveis de Ambiente

Como o projeto utiliza Firebase (Firestore, Auth, Storage), é essencial associar o projeto GCP `interactive-tales-2026` ao Firebase Console e obter as chaves de API:

1. Acesse o [Firebase Console](https://console.firebase.google.com/).
2. Clique em **Adicionar projeto** e selecione o projeto GCP existente: `interactive-tales-2026`.
3. Siga os passos e ative o Firebase no projeto.
4. No console do Firebase:
   - Ative o **Authentication** (habilite o provedor Google).
   - Ative o **Cloud Firestore** em modo de produção ou teste.
   - Ative o **Cloud Storage**.
5. Crie um aplicativo Web no console do Firebase para obter o objeto de configuração (`firebaseConfig`).
6. Copie o arquivo `story-editor/.env.example` para `story-editor/.env` e preencha as variáveis correspondentes:
   ```env
   VITE_FIREBASE_API_KEY=sua-api-key
   VITE_FIREBASE_AUTH_DOMAIN=interactive-tales-2026.firebaseapp.com
   VITE_FIREBASE_PROJECT_ID=interactive-tales-2026
   VITE_FIREBASE_STORAGE_BUCKET=interactive-tales-2026.appspot.com
   VITE_FIREBASE_MESSAGING_SENDER_ID=seu-sender-id
   VITE_FIREBASE_APP_ID=seu-app-id
   VITE_USE_FIREBASE_EMULATOR=false
   ```

> [!IMPORTANT]
> O build do React (Vite) exige que essas variáveis estejam presentes em `story-editor/.env` no momento do build/deploy, caso contrário o editor web não conseguirá se conectar ao banco de dados Firestore.

---

## 🚀 Executando o Deploy

Fornecemos scripts automatizados que habilitam as APIs necessárias no GCP, criam o banco de dados Firestore e realizam o deploy utilizando o método de sua escolha.

### Opção 1: Cloud Run (Container Nginx) — Recomendado 🐳

Este método encapsula a SPA dentro de um container Nginx otimizado e a serve no Cloud Run. É totalmente serverless e autogerenciado pelo GCP.

**Como rodar**:
- No Windows (PowerShell/CMD):
  ```cmd
  .\deploy-to-gcp.bat
  ```
  Ou de forma **não-interativa/automatizada** (ideal para agentes LLM):
  ```cmd
  .\deploy-to-gcp.bat --non-interactive
  ```

- No Linux/macOS:
  ```bash
  chmod +x deploy-to-gcp.sh
  ./deploy-to-gcp.sh
  ```
  Ou de forma **não-interativa/automatizada** (ideal para agentes LLM):
  ```bash
  ./deploy-to-gcp.sh --non-interactive
  ```

> [!NOTE]
> O modo não-interativo escolhe automaticamente o deploy para o **Cloud Run**, não solicita input do usuário e falhará com erro explicativo se o arquivo `.env` necessário não estiver presente, evitando travamentos em pipelines.

### Opção 2: Firebase Hosting (Nativo) ⚡

Este método faz o deploy dos arquivos estáticos diretamente no CDN global do Firebase Hosting. É a forma recomendada e gratuita.

> [!TIP]
> **Autenticação Automática**: Geramos e deixamos configurada uma chave de conta de serviço no arquivo `firebase-key.json` na raiz do projeto. O script de deploy detectará esta chave automaticamente e realizará a autenticação sem que a outra LLM precise digitar credenciais ou fazer login!

**Como rodar de forma 100% automatizada (sem login interativo)**:
- No Windows:
  ```cmd
  .\deploy-to-gcp.bat --non-interactive
  ```
  *(Selecione a Opção 2 ou deixe rodar de forma automática).*
- No Linux/macOS:
  ```bash
  ./deploy-to-gcp.sh --non-interactive
  ```

---

## 📁 Estrutura de Arquivos Criada para o Deploy

- [Dockerfile](file:///c:/Projetos/InteractiveFantasticTales/story-editor/Dockerfile): Configura a imagem Docker multi-stage para construir a aplicação e servi-la pelo Nginx.
- [nginx.conf](file:///c:/Projetos/InteractiveFantasticTales/story-editor/nginx.conf): Configuração Nginx para tratar o roteamento SPA (fallback para index.html).
- [firebase.json](file:///c:/Projetos/InteractiveFantasticTales/firebase.json) & [.firebaserc](file:///c:/Projetos/InteractiveFantasticTales/.firebaserc): Arquivos de configuração que associam o projeto ao Firebase Hosting e definem o diretório estático.
- [firebase-key.json](file:///c:/Projetos/InteractiveFantasticTales/firebase-key.json): Chave de conta de serviço para autenticação automática sem login interativo.
- [deploy-to-gcp.bat](file:///c:/Projetos/InteractiveFantasticTales/deploy-to-gcp.bat) & [deploy-to-gcp.sh](file:///c:/Projetos/InteractiveFantasticTales/deploy-to-gcp.sh): Scripts automatizados em lote e shell.


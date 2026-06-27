#!/bin/bash

# Aborta o script em caso de erros
set -e

echo "==================================================="
echo "  Interactive Fantastic Tales - Deploy para o GCP"
echo "==================================================="

# Configura??es
PROJECT_ID="interactive-tales-2026"
REGION="us-central1"
REPO_NAME="story-editor-repo"
SERVICE_NAME="story-editor"

# Parse de argumentos
NON_INTERACTIVE=0
for arg in "$@"; do
    if [ "$arg" = "--non-interactive" ] || [ "$arg" = "-y" ]; then
        NON_INTERACTIVE=1
    fi
done

echo "[*] Configurando o projeto ativo do gcloud para: $PROJECT_ID"
gcloud config set project "$PROJECT_ID"

echo "[*] Habilitando as APIs do Google Cloud necessarias..."
gcloud services enable run.googleapis.com \
                       artifactregistry.googleapis.com \
                       cloudbuild.googleapis.com \
                       firestore.googleapis.com \
                       firebase.googleapis.com --quiet
echo "[OK] APIs habilitadas com sucesso."

echo "[*] Verificando configuracao do Firestore..."
if gcloud firestore databases create --location=us-east1 --quiet >/dev/null 2>&1; then
    echo "[OK] Banco de dados Firestore provisionado com sucesso na regiao us-east1."
else
    echo "[*] Firestore ja ativo ou configurado."
fi

echo "[*] Verificando variaveis de ambiente (.env) em story-editor..."
if [ ! -f "story-editor/.env" ]; then
    if [ "$NON_INTERACTIVE" = "1" ]; then
        echo "[ERROR] Arquivo story-editor/.env nao encontrado!"
        echo "[INFO] Como o script esta rodando em modo nao-interativo, nao podemos continuar."
        echo "[INFO] Crie o arquivo 'story-editor/.env' com as credenciais do Firebase antes de rodar o deploy."
        exit 1
    else
        echo "[WARNING] Arquivo story-editor/.env nao encontrado!"
        echo "[INFO] Copiando story-editor/.env.example para story-editor/.env..."
        cp "story-editor/.env.example" "story-editor/.env"
        echo "[IMPORTANT] Por favor, edite o arquivo 'story-editor/.env' com as credenciais reais do Firebase antes de continuar."
        read -p "Pressione [Enter] para continuar ap?s configurar o arquivo .env..."
    fi
fi

DEPLOY_CHOICE=1
if [ "$NON_INTERACTIVE" = "1" ]; then
    echo "[*] Modo nao-interativo ativo: selecionando automaticamente Cloud Run."
else
    echo "==================================================="
    echo "Escolha o m?todo de deploy:"
    echo "[1] Cloud Run (Docker Container - Recomendado, totalmente automatizado)"
    echo "[2] Firebase Hosting (Requer configuracao manual previa do Firebase no console)"
    echo "==================================================="
    read -p "Escolha uma opcao (1 ou 2, padrao 1): " DEPLOY_CHOICE
fi

# Fallback se a escolha for vazia
if [ -z "$DEPLOY_CHOICE" ]; then
    DEPLOY_CHOICE=1
fi

if [ "$DEPLOY_CHOICE" = "1" ]; then
    echo "[*] Iniciando deploy no Google Cloud Run..."
    
    echo "[*] Verificando se o repositorio Artifact Registry existe..."
    if ! gcloud artifacts repositories describe "$REPO_NAME" --location="$REGION" >/dev/null 2>&1; then
        echo "[*] Criando repositorio no Artifact Registry para o container..."
        gcloud artifacts repositories create "$REPO_NAME" \
            --repository-format=docker \
            --location="$REGION" \
            --description="Repositorio do Story Editor" --quiet
    fi
    
    IMAGE_TAG="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${SERVICE_NAME}:latest"
    
    echo "[*] Enviando build para o Google Cloud Build e registrando imagem..."
    gcloud builds submit --tag "$IMAGE_TAG" ./story-editor
    
    echo "[*] Realizando o deploy no Cloud Run..."
    gcloud run deploy "$SERVICE_NAME" \
        --image "$IMAGE_TAG" \
        --platform managed \
        --region "$REGION" \
        --allow-unauthenticated
        
    echo "==================================================="
    echo "[SUCCESS] Deploy concluido com sucesso no Cloud Run!"
    echo "==================================================="
    
elif [ "$DEPLOY_CHOICE" = "2" ]; then
    echo "[*] Iniciando deploy no Firebase Hosting..."
    
    echo "[*] Instalando dependencias locais..."
    cd story-editor
    npm install
    
    echo "[*] Construindo o build de producao (Vite)..."
    npx vite build
    
    cd ..
    echo "[*] Executando deploy via Firebase CLI local..."
    if [ -f "firebase-key.json" ]; then
        echo "[OK] Chave firebase-key.json encontrada! Usando para autenticacao automatica."
        export GOOGLE_APPLICATION_CREDENTIALS="$(pwd)/firebase-key.json"
    fi
    npx firebase deploy --only hosting
    
    echo "==================================================="
    echo "[SUCCESS] Deploy concluido com sucesso no Firebase Hosting!"
    echo "==================================================="
else
    echo "[ERROR] Opcao invalida. Saindo..."
    exit 1
fi



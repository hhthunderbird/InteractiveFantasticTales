@echo off
SETLOCAL EnableDelayedExpansion

echo ===================================================
echo   Interactive Fantastic Tales - Deploy para o GCP
echo ===================================================

:: Configura??es
set PROJECT_ID=interactive-tales-2026
set REGION=us-central1
set REPO_NAME=story-editor-repo
set SERVICE_NAME=story-editor

:: Parse de argumentos
set NON_INTERACTIVE=0
for %%x in (%*) do (
    if "%%x"=="--non-interactive" set NON_INTERACTIVE=1
    if "%%x"=="-y" set NON_INTERACTIVE=1
)

echo [*] Configurando o projeto ativo do gcloud para: %PROJECT_ID%
call gcloud config set project %PROJECT_ID%
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Falha ao definir o projeto gcloud. Certifique-se de que o gcloud CLI est? instalado e autenticado.
    exit /b 1
)

echo [*] Habilitando as APIs do Google Cloud necessarias...
call gcloud services enable run.googleapis.com artifactregistry.googleapis.com cloudbuild.googleapis.com firestore.googleapis.com firebase.googleapis.com --quiet
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Falha ao habilitar APIs do Google Cloud.
    exit /b 1
)
echo [OK] APIs habilitadas com sucesso.

echo [*] Verificando configuracao do Firestore...
call gcloud firestore databases create --location=us-east1 --quiet >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo [OK] Banco de dados Firestore provisionado com sucesso na regiao us-east1.
) else (
    echo [*] Firestore ja ativo ou configurado.
)

echo [*] Verificando variaveis de ambiente (.env) em story-editor...
if not exist "story-editor\.env" (
    if "%NON_INTERACTIVE%"=="1" (
        echo [ERROR] Arquivo story-editor\.env nao encontrado!
        echo [INFO] Como o script esta rodando em modo nao-interativo, nao podemos continuar.
        echo [INFO] Crie o arquivo 'story-editor\.env' com as credenciais do Firebase antes de rodar o deploy.
        exit /b 1
    ) else (
        echo [WARNING] Arquivo story-editor\.env nao encontrado!
        echo [INFO] Copiando story-editor\.env.example para story-editor\.env...
        copy "story-editor\.env.example" "story-editor\.env"
        echo [IMPORTANT] Por favor, edite o arquivo 'story-editor\.env' com as credenciais reais do Firebase antes de continuar o deploy.
        pause
    )
)

:: Escolha do m?todo de deploy
set DEPLOY_CHOICE=1
if "%NON_INTERACTIVE%"=="1" (
    echo [*] Modo nao-interativo ativo: selecionando automaticamente Cloud Run.
) else (
    echo ===================================================
    echo Escolha o m?todo de deploy:
    echo [1] Cloud Run (Docker Container - Recomendado, totalmente automatizado)
    echo [2] Firebase Hosting (Requer configuracao manual previa do Firebase no console)
    echo ===================================================
    set /p DEPLOY_CHOICE="Escolha uma opcao (1 ou 2, padrao 1): "
)

if "%DEPLOY_CHOICE%"=="" set DEPLOY_CHOICE=1

if "%DEPLOY_CHOICE%"=="1" (
    echo [*] Iniciando deploy no Google Cloud Run...
    
    echo [*] Verificando se o repositorio Artifact Registry existe...
    call gcloud artifacts repositories describe %REPO_NAME% --location=%REGION% >nul 2>&1
    if %ERRORLEVEL% neq 0 (
        echo [*] Criando repositorio no Artifact Registry para o container...
        call gcloud artifacts repositories create %REPO_NAME% --repository-format=docker --location=%REGION% --description="Repositorio do Story Editor" --quiet
    )
    
    echo [*] Enviando build para o Google Cloud Build e registrando imagem...
    set IMAGE_TAG=%REGION%-docker.pkg.dev/%PROJECT_ID%/%REPO_NAME%/%SERVICE_NAME%:latest
    call gcloud builds submit --tag !IMAGE_TAG! ./story-editor
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Falha no build da imagem docker via Cloud Build.
        exit /b 1
    )
    
    echo [*] Realizando o deploy no Cloud Run...
    call gcloud run deploy %SERVICE_NAME% --image !IMAGE_TAG! --platform managed --region %REGION% --allow-unauthenticated
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Falha no deploy para o Cloud Run.
        exit /b 1
    )
    echo ===================================================
    echo [SUCCESS] Deploy concluido com sucesso no Cloud Run!
    echo ===================================================
) else if "%DEPLOY_CHOICE%"=="2" (
    echo [*] Iniciando deploy no Firebase Hosting...
    
    echo [*] Instalando dependencias locais (incluindo firebase-tools)...
    cd story-editor
    call npm install
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Falha ao rodar npm install.
        cd ..
        exit /b 1
    )
    
    echo [*] Construindo o build de producao (Vite)...
    call npx vite build
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Falha no build da aplicacao React.
        cd ..
        exit /b 1
    )
    
    cd ..
    echo [*] Executando deploy via Firebase CLI local...
    if exist "firebase-key.json" (
        echo [OK] Chave firebase-key.json encontrada! Usando para autenticacao automatica.
        set GOOGLE_APPLICATION_CREDENTIALS=%CD%\firebase-key.json
    )
    call npx firebase deploy --only hosting
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Falha no deploy para o Firebase Hosting.
        echo [INFO] Certifique-se de ter a chave firebase-key.json no diretorio ou fazer login rodando: npx firebase login
        exit /b 1
    )
    echo ===================================================
    echo [SUCCESS] Deploy concluido com sucesso no Firebase Hosting!
    echo ===================================================
) else (
    echo [ERROR] Opcao invalida. Saindo...
    exit /b 1
)

ENDLOCAL



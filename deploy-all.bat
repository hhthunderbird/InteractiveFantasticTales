@echo off
echo ============================================
echo  IFT Deploy - All Services
echo ============================================
echo.

echo [1/4] Deploying Firestore Rules + Indexes...
call npx firebase deploy --only firestore:rules,firestore:indexes
if %errorlevel% neq 0 (
    echo ERROR: Firestore deploy failed
    exit /b %errorlevel%
)
echo.

echo [2/4] Deploying Firebase Hosting (Story Editor)...
call npx firebase deploy --only hosting
if %errorlevel% neq 0 (
    echo WARNING: Hosting deploy failed (check firebase login)
)
echo.

echo [3/4] Building Cloud Run backend...
cd cloud-run
call npm install
call npm run build
if %errorlevel% neq 0 (
    echo ERROR: Cloud Run build failed
    cd ..
    exit /b %errorlevel%
)
echo.

echo [4/4] Deploying Cloud Run service...
call gcloud run deploy ift-api --source . --region us-central1 --allow-unauthenticated
if %errorlevel% neq 0 (
    echo ERROR: Cloud Run deploy failed
    cd ..
    exit /b %errorlevel%
)
cd ..
echo.

echo ============================================
echo  DEPLOY COMPLETO!
echo ============================================
echo.
echo   Proximos passos:
echo   1. Copie a URL do Cloud Run acima
echo   2. Atualize CloudRunBaseUrl em:
echo      - Assets\_Game\Services\PurchaseService.cs
echo      - Assets\_Game\Services\SignedUrlService.cs
echo   3. Coloque google-services.json em Assets/
echo   4. Rode seed: npx ts-node cloud-run/seed-firestore.ts
echo.

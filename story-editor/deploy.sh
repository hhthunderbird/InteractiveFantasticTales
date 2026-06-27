#!/bin/bash
set -e

echo "============================================"
echo "  Interactive Fantastic Tales — Deploy"
echo "============================================"

cd "$(dirname "$0")/story-editor"

if [ ! -f ".env" ]; then
  echo "[WARNING] .env not found. Copying .env.example..."
  cp .env.example .env
  echo "[ACTION] Fill in .env with real Firebase credentials, then run again."
  exit 1
fi

echo "Select deploy target:"
echo "[1] Firebase Hosting (CDN)"
echo "[2] Cloud Run (Docker)"
read -p "Choice (1 or 2): " choice

if [ "$choice" = "1" ]; then
  npm run deploy:firebase
elif [ "$choice" = "2" ]; then
  npm run build
  cd ..
  ./deploy-to-gcp.sh
else
  echo "Invalid choice."
  exit 1
fi

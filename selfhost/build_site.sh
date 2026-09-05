#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
set -a
. ./.env
set +a

ROOT="$(cd .. && pwd)"

if ! command -v node >/dev/null 2>&1; then
	echo "==> Устанавливаю Node.js"
	curl -fsSL https://deb.nodesource.com/setup_22.x | bash -
	apt-get install -y nodejs
fi

echo "==> Node $(node -v)"

cd "$ROOT"

echo "==> Прописываю адрес функций"
echo "VITE_API_BASE=https://${DOMAIN}/api" >.env.production
cat .env.production

echo "==> Устанавливаю зависимости"
npm install --no-audit --no-fund

echo "==> Собираю сайт"
npm run build

echo "==> Копирую файлы сайта"
mkdir -p selfhost/site
rm -rf selfhost/site/*
cp -r dist/* selfhost/site/

echo
echo "==> Готово. Файлы сайта:"
ls selfhost/site | head -10
echo
echo "Проверка: curl -s -H 'Host: ${DOMAIN}' http://localhost | head -5"

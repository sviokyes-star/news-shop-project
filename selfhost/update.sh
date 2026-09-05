#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
ROOT="$(cd .. && pwd)"

echo "==> Забираю обновления из репозитория"
cd "$ROOT"
git pull

cd "$ROOT/selfhost"

CHANGED_BACKEND=$(git -C "$ROOT" diff --name-only HEAD@{1} HEAD 2>/dev/null | grep -c '^backend/' || true)
CHANGED_FRONT=$(git -C "$ROOT" diff --name-only HEAD@{1} HEAD 2>/dev/null | grep -cE '^(src/|package.json|index.html|tailwind|vite)' || true)

if [ "${1:-}" = "--all" ]; then
	CHANGED_BACKEND=1
	CHANGED_FRONT=1
fi

if [ "$CHANGED_BACKEND" -gt 0 ]; then
	echo "==> Обновляю функции"
	docker compose up -d --build api
else
	echo "==> Функции не менялись"
fi

if [ "$CHANGED_FRONT" -gt 0 ]; then
	echo "==> Пересобираю сайт"
	bash build_site.sh
else
	echo "==> Сайт не менялся"
fi

echo
echo "==> Готово. Проверка функций:"
docker compose exec -T api curl -s http://localhost:8000/health | head -c 300
echo

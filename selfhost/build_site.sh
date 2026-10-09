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

echo "==> Убираю служебные скрипты платформы"
sed -i -E '/cdn\.poehali\.dev\/intertnal\//d; /name="pp-name"/d' selfhost/site/index.html

echo "==> Перевожу ссылки на картинки на свой домен"
find selfhost/site -type f \( -name '*.js' -o -name '*.css' -o -name '*.html' \) -print0 |
	xargs -0 sed -i -E "s#https://cdn\.poehali\.dev/projects/[^/\"']+/(bucket|files)/#https://${DOMAIN}/files/#g"

echo "==> Скачиваю оставшиеся картинки к себе"
mkdir -p selfhost/img-cache selfhost/site/static-img
grep -rhoE "https://cdn\.poehali\.dev/files/[A-Za-z0-9._-]+" selfhost/site --include='*.js' --include='*.css' --include='*.html' |
	sort -u | while read -r url; do
	name="$(basename "$url")"
	if [ ! -f "selfhost/img-cache/$name" ]; then
		curl -fsSL --max-time 60 "$url" -o "selfhost/img-cache/$name" || echo "    не удалось скачать: $url"
	fi
done
cp -r selfhost/img-cache/. selfhost/site/static-img/ 2>/dev/null || true
find selfhost/site -type f \( -name '*.js' -o -name '*.css' -o -name '*.html' \) -print0 |
	xargs -0 sed -i -E "s#https://cdn\.poehali\.dev/files/#/static-img/#g"

echo "==> Проверяю, что от платформы ничего не осталось"
if grep -rlq "poehali\.dev" selfhost/site --include='*.js' --include='*.html' --include='*.css'; then
	echo "    Внимание, остались ссылки:"
	grep -rhoE "https://[a-z.]*poehali\.dev[^\"' )]*" selfhost/site --include='*.js' --include='*.html' --include='*.css' | sort -u | head -20
else
	echo "    Чисто: сайт не зависит от платформы"
fi

echo
echo "==> Готово. Файлы сайта:"
ls selfhost/site | head -10
echo
echo "Проверка: curl -s -H 'Host: ${DOMAIN}' http://localhost | head -5"
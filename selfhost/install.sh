#!/usr/bin/env bash
set -euo pipefail

echo "==> Проверка прав"
if [ "$(id -u)" -ne 0 ]; then
	echo "Запусти под root: sudo bash install.sh"
	exit 1
fi

echo "==> Установка Docker"
if ! command -v docker >/dev/null 2>&1; then
	curl -fsSL https://get.docker.com | sh
else
	echo "Docker уже установлен"
fi

echo "==> Настройка файрвола"
if command -v ufw >/dev/null 2>&1; then
	ufw allow 22/tcp || true
	ufw allow 80/tcp || true
	ufw allow 443/tcp || true
	ufw --force enable || true
fi

cd "$(dirname "$0")"

if [ ! -f .env ]; then
	cp .env.example .env
	echo
	echo "!! Создан файл .env — заполни его перед запуском:"
	echo "   nano $(pwd)/.env"
	echo
	echo "Обязательно: DOMAIN, POSTGRES_PASSWORD, AWS_SECRET_ACCESS_KEY,"
	echo "а также ключи STEAM_API_KEY и GAME_SYNC_KEY."
	echo
	echo "После заполнения запусти снова: sudo bash install.sh"
	exit 0
fi

mkdir -p backups site

echo "==> Запуск сервисов"
docker compose up -d --build

echo
echo "==> Готово. Статус:"
docker compose ps
echo
echo "Проверка работоспособности: curl -s http://localhost/api/health"

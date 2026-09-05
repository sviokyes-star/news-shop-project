#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
set -a
. ./.env
set +a

MIGRATIONS="../db_migrations"

if [ ! -d "$MIGRATIONS" ]; then
	echo "Папка с миграциями не найдена: $MIGRATIONS"
	exit 1
fi

echo "==> Создаю схему ${MAIN_DB_SCHEMA}"
docker compose exec -T db psql -v ON_ERROR_STOP=0 -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
	-c "CREATE SCHEMA IF NOT EXISTS ${MAIN_DB_SCHEMA};" >/dev/null

echo "==> Применяю миграции"
COUNT=0
for file in $(ls "$MIGRATIONS"/*.sql | sort); do
	NAME=$(basename "$file")
	printf '  %-60s' "$NAME"
	if docker compose exec -T db psql -q -v ON_ERROR_STOP=1 \
		-U "$POSTGRES_USER" -d "$POSTGRES_DB" <"$file" >/dev/null 2>&1; then
		echo "ok"
	else
		echo "пропущена"
	fi
	COUNT=$((COUNT + 1))
done

echo
echo "==> Обработано файлов: $COUNT"
echo "==> Созданные таблицы:"
docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c \
	"SELECT table_name FROM information_schema.tables WHERE table_schema = '${MAIN_DB_SCHEMA}' ORDER BY 1;"

#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
	echo "Использование: bash restore.sh <файл-дампа.sql | файл.sql.gz>"
	exit 1
fi

DUMP="$1"
if [ ! -f "$DUMP" ]; then
	echo "Файл не найден: $DUMP"
	exit 1
fi

cd "$(dirname "$0")"
set -a
. ./.env
set +a

echo "==> Очищаю таблицы перед загрузкой"
docker compose exec -T db psql -q -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<SQL
DO \$\$
DECLARE t text;
BEGIN
  FOR t IN SELECT tablename FROM pg_tables WHERE schemaname = '${MAIN_DB_SCHEMA}'
  LOOP
    EXECUTE format('TRUNCATE TABLE %I.%I CASCADE', '${MAIN_DB_SCHEMA}', t);
  END LOOP;
END \$\$;
SQL

echo "==> Загружаю данные в базу из $DUMP"
if [[ "$DUMP" == *.gz ]]; then
	gunzip -c "$DUMP" | docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"
else
	docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" <"$DUMP"
fi

echo "==> Готово. Проверяю таблицы:"
docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c \
	"SELECT table_name FROM information_schema.tables WHERE table_schema = '${MAIN_DB_SCHEMA}' ORDER BY 1;"
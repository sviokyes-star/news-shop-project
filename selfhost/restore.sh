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

echo "==> Отключаю проверку связей на время загрузки"
docker compose exec -T db psql -q -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<SQL
DO \$\$
DECLARE r record;
BEGIN
  FOR r IN SELECT conrelid::regclass AS tbl, conname FROM pg_constraint c
           JOIN pg_class t ON t.oid = c.conrelid
           JOIN pg_namespace n ON n.oid = t.relnamespace
           WHERE c.contype = 'f' AND n.nspname = '${MAIN_DB_SCHEMA}'
  LOOP
    EXECUTE format('ALTER TABLE %s ALTER CONSTRAINT %I DEFERRABLE INITIALLY DEFERRED', r.tbl, r.conname);
  END LOOP;
END \$\$;
SQL

echo "==> Загружаю данные в базу из $DUMP"
if [[ "$DUMP" == *.gz ]]; then
	gunzip -c "$DUMP" | docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"
else
	docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" <"$DUMP"
fi

echo
echo "==> Готово. Количество записей:"
docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<SQL
SELECT 'users' AS "таблица", count(*) FROM ${MAIN_DB_SCHEMA}.users
UNION ALL SELECT 'purchases', count(*) FROM ${MAIN_DB_SCHEMA}.purchases
UNION ALL SELECT 'balance_transactions', count(*) FROM ${MAIN_DB_SCHEMA}.balance_transactions
UNION ALL SELECT 'chat_messages', count(*) FROM ${MAIN_DB_SCHEMA}.chat_messages
UNION ALL SELECT 'tournaments', count(*) FROM ${MAIN_DB_SCHEMA}.tournaments
UNION ALL SELECT 'shop_items', count(*) FROM ${MAIN_DB_SCHEMA}.shop_items
UNION ALL SELECT 'servers', count(*) FROM ${MAIN_DB_SCHEMA}.servers
UNION ALL SELECT 'news', count(*) FROM ${MAIN_DB_SCHEMA}.news;
SQL
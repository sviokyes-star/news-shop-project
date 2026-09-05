#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
set -a
. ./.env
set +a

SOURCE_PROJECT_ID="${SOURCE_PROJECT_ID:-0cd5ea72-8c09-43b2-b92c-a0fdee84371e}"

echo "==> Переношу файлы с платформы в локальное хранилище"
docker compose exec -T \
	-e SOURCE_PROJECT_ID="$SOURCE_PROJECT_ID" \
	api python /app/selfhost/migrate_files.py

echo
echo "==> Переписываю ссылки в базе на локальные"
docker compose exec -T db psql -q -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<SQL
DO \$\$
DECLARE r record; q text;
BEGIN
  FOR r IN
    SELECT table_name, column_name FROM information_schema.columns
    WHERE table_schema = '${MAIN_DB_SCHEMA}'
      AND data_type IN ('text','character varying')
  LOOP
    q := format(
      'UPDATE %I.%I SET %I = regexp_replace(%I, %L, %L, %L) WHERE %I LIKE %L',
      '${MAIN_DB_SCHEMA}', r.table_name, r.column_name, r.column_name,
      'https://cdn\.poehali\.dev/projects/[^/]+/(bucket|files)/',
      'https://${DOMAIN}/files/',
      'g',
      r.column_name, '%cdn.poehali.dev%'
    );
    BEGIN
      EXECUTE q;
    EXCEPTION WHEN others THEN NULL;
    END;
  END LOOP;
END \$\$;
SQL

echo
echo "==> Проверяю, что ссылок на платформу не осталось"
docker compose exec -T db psql -t -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
	-c "SELECT count(*) FROM ${MAIN_DB_SCHEMA}.site_settings WHERE value LIKE '%cdn.poehali.dev%';"

echo "==> Готово"

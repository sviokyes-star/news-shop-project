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

run_sql_file() {
	{
		echo "SET search_path TO ${MAIN_DB_SCHEMA}, public;"
		cat "$1"
	} | docker compose exec -T db psql -q -v ON_ERROR_STOP=1 \
		-U "$POSTGRES_USER" -d "$POSTGRES_DB" 2>&1
}

if [ "${1:-}" = "--reset" ]; then
	echo "==> Удаляю старые таблицы и создаю схему заново"
	docker compose exec -T db psql -q -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
		-c "DROP SCHEMA IF EXISTS ${MAIN_DB_SCHEMA} CASCADE;" \
		-c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;" >/dev/null
fi

echo "==> Создаю схему ${MAIN_DB_SCHEMA}"
docker compose exec -T db psql -q -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
	-c "CREATE SCHEMA IF NOT EXISTS ${MAIN_DB_SCHEMA};" >/dev/null

FAILED=$(ls "$MIGRATIONS"/*.sql | sort)

for PASS in 1 2 3; do
	echo
	echo "==> Проход $PASS"
	STILL_FAILED=""
	for file in $FAILED; do
		if run_sql_file "$file" >/dev/null 2>&1; then
			echo "  ok        $(basename "$file")"
		else
			STILL_FAILED="$STILL_FAILED $file"
		fi
	done
	FAILED="$STILL_FAILED"
	[ -z "$FAILED" ] && break
done

if [ -n "$FAILED" ]; then
	echo
	echo "==> Не применились (причина ниже):"
	for file in $FAILED; do
		echo "--- $(basename "$file")"
		run_sql_file "$file" | grep -i 'ошибка\|error' | head -2 || true
	done
fi

echo
echo "==> Созданные таблицы:"
docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c \
	"SELECT table_name FROM information_schema.tables WHERE table_schema = '${MAIN_DB_SCHEMA}' ORDER BY 1;"
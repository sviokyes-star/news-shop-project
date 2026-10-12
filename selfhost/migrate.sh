#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
set -a
. ./.env
set +a

MIGRATIONS="../db_migrations"

psql_q() {
	docker compose exec -T db psql -q -t -A -v ON_ERROR_STOP=1 \
		-U "$POSTGRES_USER" -d "$POSTGRES_DB" "$@"
}

psql_q -c "CREATE TABLE IF NOT EXISTS public.selfhost_migrations (name TEXT PRIMARY KEY, applied_at TIMESTAMP DEFAULT now());" >/dev/null

COUNT=$(psql_q -c "SELECT count(*) FROM public.selfhost_migrations;" | tr -d '[:space:]')

if [ "$COUNT" = "0" ]; then
	echo "==> Первый запуск: отмечаю уже существующие изменения базы как применённые"
	for file in $(ls "$MIGRATIONS"/*.sql | sort); do
		name=$(basename "$file")
		psql_q -c "INSERT INTO public.selfhost_migrations (name) VALUES ('$name') ON CONFLICT DO NOTHING;" >/dev/null
	done
	echo "  отмечено: $(ls "$MIGRATIONS"/*.sql | wc -l)"
	[ "${1:-}" = "--baseline" ] && exit 0
fi

[ "${1:-}" = "--baseline" ] && exit 0

APPLIED=$(psql_q -c "SELECT name FROM public.selfhost_migrations;")

NEW=0
for file in $(ls "$MIGRATIONS"/*.sql | sort); do
	name=$(basename "$file")
	if ! grep -qxF "$name" <<<"$APPLIED"; then
		echo "  применяю  $name"
		if ! {
			echo "SET search_path TO ${MAIN_DB_SCHEMA}, public;"
			cat "$file"
			echo ";"
			echo "INSERT INTO public.selfhost_migrations (name) VALUES ('$name');"
		} | psql_q -1 >/dev/null; then
			echo
			echo "!!! Ошибка в $name — база не изменена, остальные изменения не применялись."
			echo "!!! Пришли этот текст разработчику."
			exit 1
		fi
		NEW=$((NEW + 1))
	fi
done

if [ "$NEW" -eq 0 ]; then
	echo "==> База не менялась"
else
	echo "==> Применено изменений базы: $NEW"
fi

#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
set -a
. ./.env
set +a

KEEP_DAYS="${BACKUP_KEEP_DAYS:-14}"
mkdir -p backups
STAMP=$(date +%Y%m%d-%H%M%S)
DB_FILE="backups/db-${STAMP}.sql.gz"
FILES_FILE="backups/files-${STAMP}.tar.gz"

echo "==> $(date '+%Y-%m-%d %H:%M:%S') Создаю резервную копию базы"
if ! docker compose exec -T db pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --data-only --schema="$MAIN_DB_SCHEMA" | gzip >"$DB_FILE"; then
	rm -f "$DB_FILE"
	echo "ОШИБКА: копия базы не создана"
	exit 1
fi
if [ "$(gzip -dc "$DB_FILE" | head -c 1000 | wc -c)" -lt 100 ]; then
	rm -f "$DB_FILE"
	echo "ОШИБКА: копия базы получилась пустой"
	exit 1
fi
echo "    база: $DB_FILE ($(du -h "$DB_FILE" | cut -f1))"

echo "==> Копирую картинки и файлы"
STORAGE_ID="$(docker compose ps -q storage)"
if [ -n "$STORAGE_ID" ]; then
	if docker run --rm --volumes-from "$STORAGE_ID" alpine tar czf - -C /data . >"$FILES_FILE"; then
		echo "    файлы: $FILES_FILE ($(du -h "$FILES_FILE" | cut -f1))"
	else
		rm -f "$FILES_FILE"
		echo "    не удалось скопировать файлы"
	fi
else
	echo "    хранилище не запущено, пропускаю"
fi

echo "==> Удаляю копии старше ${KEEP_DAYS} дней"
find backups -name 'db-*.sql.gz' -mtime +"$KEEP_DAYS" -delete
find backups -name 'files-*.tar.gz' -mtime +"$KEEP_DAYS" -delete

echo "==> Готово. Последние копии:"
ls -lh backups | tail -n 6
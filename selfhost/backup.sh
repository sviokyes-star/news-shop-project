#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
set -a
. ./.env
set +a

mkdir -p backups
STAMP=$(date +%Y%m%d-%H%M%S)
FILE="backups/db-${STAMP}.sql.gz"

echo "==> Создаю резервную копию базы"
docker compose exec -T db pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" | gzip >"$FILE"

echo "==> Готово: $FILE ($(du -h "$FILE" | cut -f1))"

echo "==> Удаляю копии старше 14 дней"
find backups -name 'db-*.sql.gz' -mtime +14 -delete

ls -lh backups | tail -n 5

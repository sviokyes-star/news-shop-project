#!/usr/bin/env python3
"""Переносит картинки и файлы с платформы в локальное хранилище сервера."""
import os
import re
import sys
import urllib.request

import boto3
import psycopg2

CDN_RE = re.compile(r"https://cdn\.poehali\.dev/projects/[^/\s\"']+/(?:bucket|files)/([^\s\"'\\)<>]+)")

DSN = os.environ["DATABASE_URL"]
SCHEMA = os.environ.get("MAIN_DB_SCHEMA", "public")
ENDPOINT = os.environ.get("S3_ENDPOINT", "http://storage:9000")

CONTENT_TYPES = {
    ".png": "image/png",
    ".jpg": "image/jpeg",
    ".jpeg": "image/jpeg",
    ".gif": "image/gif",
    ".webp": "image/webp",
    ".svg": "image/svg+xml",
    ".ico": "image/x-icon",
    ".mp4": "video/mp4",
    ".pdf": "application/pdf",
}


def collect_from_db() -> set:
    urls = set()
    conn = psycopg2.connect(DSN)
    cur = conn.cursor()
    cur.execute(
        "SELECT table_name, column_name FROM information_schema.columns "
        "WHERE table_schema = '%s' AND data_type IN "
        "('text','character varying','json','jsonb')" % SCHEMA
    )
    columns = cur.fetchall()
    for table, column in columns:
        try:
            cur.execute(
                'SELECT "%s"::text FROM %s."%s" WHERE "%s"::text LIKE \'%%cdn.poehali.dev%%\''
                % (column, SCHEMA, table, column)
            )
            for (value,) in cur.fetchall():
                urls.update(CDN_RE.findall(value or ""))
        except Exception:
            conn.rollback()
    cur.close()
    conn.close()
    return urls


def collect_from_sources(root: str) -> set:
    urls = set()
    skip = {"node_modules", ".git", "dist", "site", "backups"}
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in skip]
        for name in filenames:
            if not name.endswith((".tsx", ".ts", ".js", ".jsx", ".json", ".css", ".html", ".py", ".md")):
                continue
            path = os.path.join(dirpath, name)
            try:
                with open(path, "r", encoding="utf-8", errors="ignore") as fh:
                    urls.update(CDN_RE.findall(fh.read()))
            except OSError:
                continue
    return urls


def main() -> int:
    root = os.environ.get("SCAN_ROOT") or os.path.dirname(
        os.path.dirname(os.path.abspath(__file__))
    )

    print("==> Ищу файлы в базе")
    keys = collect_from_db()
    print("    найдено ссылок: %d" % len(keys))

    print("==> Ищу файлы в коде сайта")
    from_src = collect_from_sources(root)
    print("    найдено ссылок: %d" % len(from_src))
    keys |= from_src

    if not keys:
        print("Файлов для переноса нет.")
        return 0

    s3 = boto3.client(
        "s3",
        endpoint_url=ENDPOINT,
        aws_access_key_id=os.environ["AWS_ACCESS_KEY_ID"],
        aws_secret_access_key=os.environ["AWS_SECRET_ACCESS_KEY"],
    )

    existing = set()
    paginator = s3.get_paginator("list_objects_v2")
    for page in paginator.paginate(Bucket="files"):
        for obj in page.get("Contents", []):
            existing.add(obj["Key"])

    ok = skipped = failed = 0
    total = len(keys)
    for i, key in enumerate(sorted(keys), 1):
        if key in existing:
            skipped += 1
            continue
        ext = os.path.splitext(key)[1].lower()
        ctype = CONTENT_TYPES.get(ext, "application/octet-stream")
        for prefix in ("bucket", "files"):
            url = "https://cdn.poehali.dev/projects/%s/%s/%s" % (
                os.environ["SOURCE_PROJECT_ID"],
                prefix,
                key,
            )
            try:
                with urllib.request.urlopen(url, timeout=60) as resp:
                    data = resp.read()
            except Exception:
                continue
            s3.put_object(Bucket="files", Key=key, Body=data, ContentType=ctype)
            ok += 1
            print("    [%d/%d] %s (%d КБ)" % (i, total, key, len(data) // 1024))
            break
        else:
            failed += 1
            print("    [%d/%d] НЕ НАЙДЕН: %s" % (i, total, key))

    print()
    print("==> Готово. Перенесено: %d, уже были: %d, не найдено: %d" % (ok, skipped, failed))
    return 0


if __name__ == "__main__":
    sys.exit(main())
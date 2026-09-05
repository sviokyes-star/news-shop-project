import json
import os
import sys
from datetime import date, datetime
from decimal import Decimal

import psycopg2

SCHEMA = os.environ.get("MAIN_DB_SCHEMA", "t_p15345778_news_shop_project")
DSN = os.environ.get("SOURCE_DATABASE_URL") or os.environ.get("DATABASE_URL")
OUT = sys.argv[1] if len(sys.argv) > 1 else "backups/data.sql"

if not DSN:
    print("Укажи строку подключения в SOURCE_DATABASE_URL")
    sys.exit(1)


def q(value):
    if value is None:
        return "NULL"
    if isinstance(value, bool):
        return "TRUE" if value else "FALSE"
    if isinstance(value, (int, float, Decimal)):
        return str(value)
    if isinstance(value, (datetime, date)):
        return "'" + value.isoformat() + "'"
    if isinstance(value, (dict, list)):
        return "'" + json.dumps(value, ensure_ascii=False).replace("'", "''") + "'"
    return "'" + str(value).replace("'", "''") + "'"


conn = psycopg2.connect(DSN)
cur = conn.cursor()

cur.execute(
    "SELECT table_name FROM information_schema.tables "
    "WHERE table_schema = %s AND table_type = 'BASE TABLE' ORDER BY table_name",
    (SCHEMA,),
)
tables = [r[0] for r in cur.fetchall()]

lines = [
    "-- Данные проекта. Структуру создают миграции из db_migrations.",
    f"SET search_path TO {SCHEMA};",
    "BEGIN;",
    "",
]

total = 0
for table in tables:
    cur.execute(
        "SELECT column_name FROM information_schema.columns "
        "WHERE table_schema = %s AND table_name = %s ORDER BY ordinal_position",
        (SCHEMA, table),
    )
    cols = [r[0] for r in cur.fetchall()]
    if not cols:
        continue

    cur.execute(f'SELECT {", ".join(chr(34) + c + chr(34) for c in cols)} FROM {SCHEMA}.{table}')
    rows = cur.fetchall()
    if not rows:
        continue

    col_list = ", ".join(f'"{c}"' for c in cols)
    lines.append(f"-- {table}: {len(rows)} строк")
    for row in rows:
        values = ", ".join(q(v) for v in row)
        lines.append(f'INSERT INTO "{table}" ({col_list}) VALUES ({values});')
    lines.append("")
    total += len(rows)

lines.append("-- Сброс счётчиков id")
for table in tables:
    cur.execute(
        "SELECT column_name FROM information_schema.columns "
        "WHERE table_schema = %s AND table_name = %s AND column_default LIKE 'nextval%%'",
        (SCHEMA, table),
    )
    for (col,) in cur.fetchall():
        lines.append(
            f"SELECT setval(pg_get_serial_sequence('{SCHEMA}.{table}', '{col}'), "
            f'COALESCE((SELECT MAX("{col}") FROM "{table}"), 1), true);'
        )

lines.append("")
lines.append("COMMIT;")

os.makedirs(os.path.dirname(OUT) or ".", exist_ok=True)
with open(OUT, "w", encoding="utf-8") as fh:
    fh.write("\n".join(lines))

print(f"Готово: {OUT} — таблиц {len(tables)}, строк {total}")

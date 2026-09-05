import json
import os
from datetime import date, datetime
from decimal import Decimal

import psycopg2

SCHEMA = os.environ.get('MAIN_DB_SCHEMA', 't_p15345778_news_shop_project')


def quote(value):
    if value is None:
        return 'NULL'
    if isinstance(value, bool):
        return 'TRUE' if value else 'FALSE'
    if isinstance(value, (int, float, Decimal)):
        return str(value)
    if isinstance(value, (datetime, date)):
        return "'" + value.isoformat() + "'"
    if isinstance(value, (dict, list)):
        return "'" + json.dumps(value, ensure_ascii=False).replace("'", "''") + "'"
    if isinstance(value, (bytes, memoryview)):
        return "'\\x" + bytes(value).hex() + "'"
    return "'" + str(value).replace("'", "''") + "'"


def handler(event, context):
    '''Выгружает данные проекта одним SQL-файлом для переноса на свой сервер.'''
    method = event.get('httpMethod', 'GET')

    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-Dump-Key',
                'Access-Control-Max-Age': '86400',
            },
            'body': '',
        }

    expected = os.environ.get('GAME_SYNC_KEY', '')
    params = event.get('queryStringParameters') or {}
    headers = event.get('headers') or {}
    provided = params.get('key') or headers.get('X-Dump-Key') or headers.get('x-dump-key')

    if not expected or provided != expected:
        return {
            'statusCode': 401,
            'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
            'body': json.dumps({'error': 'Unauthorized'}),
        }

    conn = psycopg2.connect(os.environ['DATABASE_URL'])
    cur = conn.cursor()

    cur.execute(
        "SELECT table_name FROM information_schema.tables "
        "WHERE table_schema = %s AND table_type = 'BASE TABLE' ORDER BY table_name",
        (SCHEMA,),
    )
    tables = [r[0] for r in cur.fetchall()]

    if params.get('mode') == 'schema':
        ddl = [
            '-- Структура базы, снятая с рабочего проекта.',
            'CREATE SCHEMA IF NOT EXISTS ' + SCHEMA + ';',
            'SET search_path TO ' + SCHEMA + ';',
            '',
        ]
        for table in tables:
            cur.execute(
                'SELECT column_name, data_type, character_maximum_length, '
                'numeric_precision, numeric_scale, is_nullable, column_default '
                'FROM information_schema.columns '
                'WHERE table_schema = %s AND table_name = %s ORDER BY ordinal_position',
                (SCHEMA, table),
            )
            parts = []
            for name, dtype, maxlen, prec, scale, nullable, default in cur.fetchall():
                if default and 'nextval' in str(default):
                    col = '"' + name + '" ' + ('BIGSERIAL' if dtype == 'bigint' else 'SERIAL')
                else:
                    typ = dtype.upper()
                    if dtype == 'character varying' and maxlen:
                        typ = 'VARCHAR(' + str(maxlen) + ')'
                    elif dtype == 'character varying':
                        typ = 'VARCHAR'
                    elif dtype == 'numeric' and prec:
                        typ = 'NUMERIC(' + str(prec) + ',' + str(scale or 0) + ')'
                    elif dtype == 'timestamp without time zone':
                        typ = 'TIMESTAMP'
                    elif dtype == 'timestamp with time zone':
                        typ = 'TIMESTAMPTZ'
                    elif dtype == 'USER-DEFINED':
                        typ = 'TEXT'
                    col = '"' + name + '" ' + typ
                    if default is not None:
                        col += ' DEFAULT ' + str(default)
                if nullable == 'NO' and (not default or 'nextval' not in str(default)):
                    col += ' NOT NULL'
                parts.append('  ' + col)

            cur.execute(
                'SELECT a.attname FROM pg_index i '
                'JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey) '
                'WHERE i.indrelid = %s::regclass AND i.indisprimary',
                (SCHEMA + '.' + table,),
            )
            pk = [r[0] for r in cur.fetchall()]
            if pk:
                parts.append('  PRIMARY KEY (' + ', '.join('"' + c + '"' for c in pk) + ')')

            ddl.append('CREATE TABLE IF NOT EXISTS "' + table + '" (')
            ddl.append(',\n'.join(parts))
            ddl.append(');')
            ddl.append('')

        cur.close()
        conn.close()
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'text/plain; charset=utf-8',
                'Access-Control-Allow-Origin': '*',
            },
            'body': '\n'.join(ddl),
        }

    lines = [
        '-- Данные проекта. Структуру создают миграции из db_migrations.',
        'SET search_path TO ' + SCHEMA + ';',
        'BEGIN;',
        '',
    ]
    total = 0

    for table in tables:
        cur.execute(
            'SELECT column_name FROM information_schema.columns '
            'WHERE table_schema = %s AND table_name = %s ORDER BY ordinal_position',
            (SCHEMA, table),
        )
        cols = [r[0] for r in cur.fetchall()]
        if not cols:
            continue

        col_sql = ', '.join('"' + c + '"' for c in cols)
        cur.execute('SELECT ' + col_sql + ' FROM ' + SCHEMA + '.' + table)
        rows = cur.fetchall()
        if not rows:
            continue

        lines.append('-- ' + table + ': ' + str(len(rows)) + ' строк')
        for row in rows:
            values = ', '.join(quote(v) for v in row)
            lines.append('INSERT INTO "' + table + '" (' + col_sql + ') VALUES (' + values + ');')
        lines.append('')
        total += len(rows)

    lines.append('-- Сброс счётчиков id')
    for table in tables:
        cur.execute(
            'SELECT column_name FROM information_schema.columns '
            "WHERE table_schema = %s AND table_name = %s AND column_default LIKE 'nextval%%'",
            (SCHEMA, table),
        )
        for (col,) in cur.fetchall():
            lines.append(
                "SELECT setval(pg_get_serial_sequence('" + SCHEMA + '.' + table + "', '" + col + "'), "
                'COALESCE((SELECT MAX("' + col + '") FROM "' + table + '"), 1), true);'
            )

    lines.append('')
    lines.append('COMMIT;')
    lines.append('-- Всего строк: ' + str(total))

    cur.close()
    conn.close()

    return {
        'statusCode': 200,
        'headers': {
            'Content-Type': 'text/plain; charset=utf-8',
            'Access-Control-Allow-Origin': '*',
        },
        'body': '\n'.join(lines),
    }
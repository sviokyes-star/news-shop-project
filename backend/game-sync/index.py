'''
Business: Выдаёт игровому CS2 серверу (плагину) невыданные начисления золота/серебра и помечает их доставленными
Args: event с httpMethod, queryStringParameters (key), body для подтверждения доставки
      context - объект с request_id
Returns: JSON со списком начислений для игроков либо подтверждение доставки
'''

import json
import os
from typing import Dict, Any
import psycopg2

SCHEMA = 't_p15345778_news_shop_project'


def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    method: str = event.get('httpMethod', 'GET')

    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-Sync-Key',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }

    sync_key = os.environ.get('GAME_SYNC_KEY', '')
    params = event.get('queryStringParameters') or {}
    headers = event.get('headers') or {}
    provided_key = params.get('key') or headers.get('X-Sync-Key') or headers.get('x-sync-key') or ''

    if not sync_key or provided_key != sync_key:
        return _resp(403, {'error': 'Forbidden'})

    db_url = os.environ.get('DATABASE_URL')
    conn = psycopg2.connect(db_url)
    cur = conn.cursor()

    try:
        if method == 'GET':
            # Отдать все невыданные начисления
            cur.execute(f"""
                SELECT id, steam_id, currency, amount
                FROM {SCHEMA}.game_deliveries
                WHERE status = 'pending'
                ORDER BY id ASC
                LIMIT 500
            """)
            rows = cur.fetchall()
            deliveries = [
                {'id': r[0], 'steam_id': r[1], 'currency': r[2], 'amount': r[3]}
                for r in rows
            ]
            return _resp(200, {'deliveries': deliveries})

        if method == 'POST':
            # Плагин подтверждает, что выдал начисления игрокам
            body_data = json.loads(event.get('body', '{}'))
            ids = body_data.get('delivered_ids', [])
            if not isinstance(ids, list) or not ids:
                return _resp(400, {'error': 'delivered_ids required'})

            safe_ids = [str(int(i)) for i in ids]
            ids_str = ','.join(safe_ids)
            cur.execute(f"""
                UPDATE {SCHEMA}.game_deliveries
                SET status = 'delivered', delivered_at = CURRENT_TIMESTAMP
                WHERE id IN ({ids_str}) AND status = 'pending'
            """)
            updated = cur.rowcount
            conn.commit()
            return _resp(200, {'status': 'ok', 'delivered': updated})

        return _resp(405, {'error': 'Method not allowed'})

    finally:
        cur.close()
        conn.close()


def _resp(status: int, body: Dict[str, Any]) -> Dict[str, Any]:
    return {
        'statusCode': status,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'body': json.dumps(body),
        'isBase64Encoded': False
    }

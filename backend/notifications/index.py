import json
import os
import psycopg2

SCHEMA = 't_p15345778_news_shop_project'
CORS = {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, POST, PUT, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
}

def get_conn():
    return psycopg2.connect(os.environ['DATABASE_URL'])

def ok(data, code=200):
    return {'statusCode': code, 'headers': CORS, 'body': json.dumps(data)}

def err(code, msg):
    return {'statusCode': code, 'headers': CORS, 'body': json.dumps({'error': msg})}

def handler(event: dict, context) -> dict:
    """Уведомления: получить список, отметить прочитанными, создать."""
    if event.get('httpMethod') == 'OPTIONS':
        return {'statusCode': 200, 'headers': CORS, 'body': ''}

    method = event.get('httpMethod', 'GET')
    params = event.get('queryStringParameters') or {}

    # GET — получить уведомления пользователя
    if method == 'GET':
        steam_id = params.get('steam_id')
        if not steam_id:
            return err(400, 'steam_id required')

        conn = get_conn()
        cur = conn.cursor()
        cur.execute(
            f"SELECT id, type, title, body, link, is_read, created_at FROM {SCHEMA}.notifications "
            f"WHERE steam_id = %s ORDER BY created_at DESC LIMIT 50",
            (steam_id,)
        )
        rows = cur.fetchall()
        cur.execute(
            f"SELECT COUNT(*) FROM {SCHEMA}.notifications WHERE steam_id = %s AND is_read = FALSE",
            (steam_id,)
        )
        unread = cur.fetchone()[0]
        cur.close()
        conn.close()

        notifications = [
            {
                'id': r[0], 'type': r[1], 'title': r[2], 'body': r[3],
                'link': r[4], 'isRead': r[5], 'createdAt': r[6].isoformat()
            }
            for r in rows
        ]
        return ok({'notifications': notifications, 'unread': unread})

    # PUT — отметить прочитанными
    if method == 'PUT':
        body = json.loads(event.get('body') or '{}')
        steam_id = body.get('steam_id')
        notification_id = body.get('id')

        if not steam_id:
            return err(400, 'steam_id required')

        conn = get_conn()
        cur = conn.cursor()
        if notification_id:
            cur.execute(
                f"UPDATE {SCHEMA}.notifications SET is_read = TRUE WHERE id = %s AND steam_id = %s",
                (notification_id, steam_id)
            )
        else:
            cur.execute(
                f"UPDATE {SCHEMA}.notifications SET is_read = TRUE WHERE steam_id = %s",
                (steam_id,)
            )
        conn.commit()
        cur.close()
        conn.close()
        return ok({'success': True})

    # POST — создать уведомление (внутренний вызов из других функций)
    if method == 'POST':
        body = json.loads(event.get('body') or '{}')
        steam_id = body.get('steam_id')
        ntype = body.get('type')
        title = body.get('title')
        nbody = body.get('body', '')
        link = body.get('link', '')

        if not steam_id or not ntype or not title:
            return err(400, 'steam_id, type, title required')

        conn = get_conn()
        cur = conn.cursor()
        cur.execute(
            f"INSERT INTO {SCHEMA}.notifications (steam_id, type, title, body, link) VALUES (%s, %s, %s, %s, %s) RETURNING id",
            (steam_id, ntype, title, nbody, link)
        )
        nid = cur.fetchone()[0]
        conn.commit()
        cur.close()
        conn.close()
        return ok({'success': True, 'id': nid}, 201)

    return err(405, 'Method not allowed')

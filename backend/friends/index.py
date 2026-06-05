import json
import os
import psycopg2

SCHEMA = 't_p15345778_news_shop_project'

def get_conn():
    return psycopg2.connect(os.environ['DATABASE_URL'])

def handler(event: dict, context) -> dict:
    """Управление дружбой: отправка заявок, принятие/отклонение, список друзей, онлайн-статус."""
    cors = {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type, X-User-Id',
    }

    if event.get('httpMethod') == 'OPTIONS':
        return {'statusCode': 200, 'headers': cors, 'body': ''}

    method = event.get('httpMethod', 'GET')
    params = event.get('queryStringParameters') or {}

    # GET — получить список друзей, заявок, статус дружбы
    if method == 'GET':
        steam_id = params.get('steam_id')
        target_id = params.get('target_id')
        action = params.get('action', 'friends')

        if not steam_id:
            return {'statusCode': 400, 'headers': cors, 'body': json.dumps({'error': 'steam_id required'})}

        conn = get_conn()
        cur = conn.cursor()

        # Обновляем онлайн-статус текущего пользователя
        cur.execute(
            f"UPDATE {SCHEMA}.users SET is_online = TRUE, last_online = NOW() WHERE steam_id = %s",
            (steam_id,)
        )
        conn.commit()

        if action == 'status' and target_id:
            # Статус дружбы между двумя пользователями
            cur.execute(f"""
                SELECT status, requester_steam_id FROM {SCHEMA}.friendships
                WHERE (requester_steam_id = %s AND addressee_steam_id = %s)
                   OR (requester_steam_id = %s AND addressee_steam_id = %s)
            """, (steam_id, target_id, target_id, steam_id))
            row = cur.fetchone()
            if not row:
                result = {'status': 'none'}
            else:
                result = {'status': row[0], 'requester': row[1]}
            cur.close()
            conn.close()
            return {'statusCode': 200, 'headers': cors, 'body': json.dumps(result)}

        if action == 'pending':
            # Входящие заявки в друзья
            cur.execute(f"""
                SELECT f.requester_steam_id, u.persona_name, u.avatar_url, f.created_at
                FROM {SCHEMA}.friendships f
                JOIN {SCHEMA}.users u ON u.steam_id = f.requester_steam_id
                WHERE f.addressee_steam_id = %s AND f.status = 'pending'
                ORDER BY f.created_at DESC
            """, (steam_id,))
            rows = cur.fetchall()
            requests = [{'steamId': r[0], 'personaName': r[1], 'avatarUrl': r[2], 'createdAt': str(r[3])} for r in rows]
            cur.close()
            conn.close()
            return {'statusCode': 200, 'headers': cors, 'body': json.dumps({'requests': requests, 'count': len(requests)})}

        # Список друзей с онлайн-статусом
        cur.execute(f"""
            SELECT
                CASE WHEN f.requester_steam_id = %s THEN f.addressee_steam_id ELSE f.requester_steam_id END AS friend_id,
                u.persona_name, u.avatar_url,
                (u.last_online IS NOT NULL AND u.last_online > NOW() - INTERVAL '5 minutes') AS is_online,
                u.last_online
            FROM {SCHEMA}.friendships f
            JOIN {SCHEMA}.users u ON u.steam_id = CASE WHEN f.requester_steam_id = %s THEN f.addressee_steam_id ELSE f.requester_steam_id END
            WHERE (f.requester_steam_id = %s OR f.addressee_steam_id = %s) AND f.status = 'accepted'
            ORDER BY is_online DESC, u.last_online DESC
        """, (steam_id, steam_id, steam_id, steam_id))
        rows = cur.fetchall()
        friends = [{'steamId': r[0], 'personaName': r[1], 'avatarUrl': r[2], 'isOnline': r[3], 'lastOnline': str(r[4])} for r in rows]
        cur.close()
        conn.close()
        return {'statusCode': 200, 'headers': cors, 'body': json.dumps({'friends': friends})}

    # POST — отправить заявку в друзья
    if method == 'POST':
        body = json.loads(event.get('body') or '{}')
        requester = body.get('requester_steam_id')
        addressee = body.get('addressee_steam_id')

        if not requester or not addressee or requester == addressee:
            return {'statusCode': 400, 'headers': cors, 'body': json.dumps({'error': 'invalid steam ids'})}

        conn = get_conn()
        cur = conn.cursor()
        cur.execute(f"""
            INSERT INTO {SCHEMA}.friendships (requester_steam_id, addressee_steam_id, status)
            VALUES (%s, %s, 'pending')
            ON CONFLICT (requester_steam_id, addressee_steam_id) DO NOTHING
        """, (requester, addressee))
        conn.commit()
        cur.close()
        conn.close()
        return {'statusCode': 200, 'headers': cors, 'body': json.dumps({'success': True})}

    # PUT — принять или отклонить заявку
    if method == 'PUT':
        body = json.loads(event.get('body') or '{}')
        requester = body.get('requester_steam_id')
        addressee = body.get('addressee_steam_id')
        action = body.get('action')  # 'accept' или 'decline'

        if not requester or not addressee or action not in ('accept', 'decline'):
            return {'statusCode': 400, 'headers': cors, 'body': json.dumps({'error': 'invalid params'})}

        conn = get_conn()
        cur = conn.cursor()
        if action == 'accept':
            cur.execute(f"""
                UPDATE {SCHEMA}.friendships SET status = 'accepted', updated_at = NOW()
                WHERE requester_steam_id = %s AND addressee_steam_id = %s AND status = 'pending'
            """, (requester, addressee))
        else:
            cur.execute(f"""
                DELETE FROM {SCHEMA}.friendships
                WHERE requester_steam_id = %s AND addressee_steam_id = %s
            """, (requester, addressee))
        conn.commit()
        cur.close()
        conn.close()
        return {'statusCode': 200, 'headers': cors, 'body': json.dumps({'success': True})}

    # DELETE — удалить из друзей
    if method == 'DELETE':
        body = json.loads(event.get('body') or '{}')
        steam_id = body.get('steam_id')
        friend_id = body.get('friend_id')

        if not steam_id or not friend_id:
            return {'statusCode': 400, 'headers': cors, 'body': json.dumps({'error': 'invalid params'})}

        conn = get_conn()
        cur = conn.cursor()
        cur.execute(f"""
            DELETE FROM {SCHEMA}.friendships
            WHERE (requester_steam_id = %s AND addressee_steam_id = %s)
               OR (requester_steam_id = %s AND addressee_steam_id = %s)
        """, (steam_id, friend_id, friend_id, steam_id))
        conn.commit()
        cur.close()
        conn.close()
        return {'statusCode': 200, 'headers': cors, 'body': json.dumps({'success': True})}

    return {'statusCode': 405, 'headers': cors, 'body': json.dumps({'error': 'method not allowed'})}
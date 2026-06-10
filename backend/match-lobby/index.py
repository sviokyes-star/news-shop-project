import json
import os
import psycopg2
from psycopg2.extras import RealDictCursor
from typing import Dict, Any

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    Лобби матча турнира: получить/создать лобби, чат, сообщить результат
    """
    method = event.get('httpMethod', 'GET')

    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-User-Steam-Id',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }

    HEADERS = {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'}
    SCHEMA = 't_p15345778_news_shop_project'

    conn = psycopg2.connect(os.environ['DATABASE_URL'])
    cur = conn.cursor(cursor_factory=RealDictCursor)

    params = event.get('queryStringParameters') or {}

    # GET — получить лобби по tournament_id + round_index + match_index
    if method == 'GET':
        tournament_id = params.get('tournament_id')
        round_index = params.get('round_index')
        match_index = params.get('match_index')
        steam_id = params.get('steam_id')

        if not tournament_id or round_index is None or match_index is None:
            return {'statusCode': 400, 'headers': HEADERS, 'body': json.dumps({'error': 'tournament_id, round_index, match_index required'})}

        # Проверить что пользователь — участник турнира
        if steam_id:
            escaped = steam_id.replace("'", "''")
            cur.execute(f"SELECT id FROM {SCHEMA}.tournament_registrations WHERE tournament_id = {int(tournament_id)} AND steam_id = '{escaped}'")
            if not cur.fetchone():
                return {'statusCode': 403, 'headers': HEADERS, 'body': json.dumps({'error': 'Доступ только для участников турнира'})}

        # Получить или создать лобби
        cur.execute(f"""
            SELECT id, player1_steam_id, player2_steam_id, winner_steam_id, status
            FROM {SCHEMA}.match_lobbies
            WHERE tournament_id = {int(tournament_id)}
              AND round_index = {int(round_index)}
              AND match_index = {int(match_index)}
        """)
        lobby = cur.fetchone()

        if not lobby:
            p1 = (params.get('player1_steam_id') or '').replace("'", "''")
            p2 = (params.get('player2_steam_id') or '').replace("'", "''")
            p1_val = f"'{p1}'" if p1 else 'NULL'
            p2_val = f"'{p2}'" if p2 else 'NULL'
            cur.execute(f"""
                INSERT INTO {SCHEMA}.match_lobbies (tournament_id, round_index, match_index, status, player1_steam_id, player2_steam_id)
                VALUES ({int(tournament_id)}, {int(round_index)}, {int(match_index)}, 'waiting', {p1_val}, {p2_val})
                RETURNING id, player1_steam_id, player2_steam_id, winner_steam_id, status
            """)
            lobby = cur.fetchone()
            conn.commit()

        lobby_id = lobby['id']

        # Получить сообщения чата
        cur.execute(f"""
            SELECT id, steam_id, persona_name, avatar_url, message,
                   to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS"+00:00"') as created_at
            FROM {SCHEMA}.lobby_messages
            WHERE lobby_id = {lobby_id}
            ORDER BY created_at ASC
        """)
        messages = [dict(r) for r in cur.fetchall()]

        # Получить профили игроков
        def get_player(sid):
            if not sid:
                return None
            s = sid.replace("'", "''")
            cur.execute(f"""
                SELECT COALESCE(nickname, steam_id) as persona_name, avatar_url,
                       COALESCE(is_admin, false) as is_admin
                FROM {SCHEMA}.users WHERE steam_id = '{s}'
            """)
            row = cur.fetchone()
            # Fallback — взять из tournament_registrations
            if not row:
                cur.execute(f"""
                    SELECT persona_name, avatar_url FROM {SCHEMA}.tournament_registrations
                    WHERE steam_id = '{s}' AND tournament_id = {int(tournament_id)}
                """)
                row = cur.fetchone()
            if not row:
                return {'steam_id': sid, 'persona_name': sid, 'avatar_url': None}
            return {'steam_id': sid, **dict(row)}

        result = {
            'lobby': dict(lobby),
            'messages': messages,
            'player1': get_player(lobby['player1_steam_id']),
            'player2': get_player(lobby['player2_steam_id']),
        }

        conn.close()
        return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps(result)}

    # POST — отправить сообщение в чат
    if method == 'POST':
        body = json.loads(event.get('body') or '{}')
        action = body.get('action', 'message')
        steam_id = (event.get('headers') or {}).get('X-User-Steam-Id') or body.get('steam_id', '')

        if not steam_id:
            return {'statusCode': 401, 'headers': HEADERS, 'body': json.dumps({'error': 'Требуется авторизация'})}

        tournament_id = body.get('tournament_id')
        round_index = body.get('round_index')
        match_index = body.get('match_index')

        if not tournament_id or round_index is None or match_index is None:
            return {'statusCode': 400, 'headers': HEADERS, 'body': json.dumps({'error': 'tournament_id, round_index, match_index required'})}

        # Проверить участие в турнире
        escaped = steam_id.replace("'", "''")
        cur.execute(f"SELECT id FROM {SCHEMA}.tournament_registrations WHERE tournament_id = {int(tournament_id)} AND steam_id = '{escaped}'")
        if not cur.fetchone():
            return {'statusCode': 403, 'headers': HEADERS, 'body': json.dumps({'error': 'Доступ только для участников турнира'})}

        # Получить лобби
        cur.execute(f"""
            SELECT id, player1_steam_id, player2_steam_id, status
            FROM {SCHEMA}.match_lobbies
            WHERE tournament_id = {int(tournament_id)} AND round_index = {int(round_index)} AND match_index = {int(match_index)}
        """)
        lobby = cur.fetchone()
        if not lobby:
            return {'statusCode': 404, 'headers': HEADERS, 'body': json.dumps({'error': 'Лобби не найдено'})}

        lobby_id = lobby['id']

        # Отправить сообщение
        if action == 'message':
            message = body.get('message', '').strip()
            if not message:
                return {'statusCode': 400, 'headers': HEADERS, 'body': json.dumps({'error': 'Пустое сообщение'})}

            persona_name = body.get('persona_name', steam_id).replace("'", "''")
            avatar_url = (body.get('avatar_url') or '').replace("'", "''")
            escaped_message = message.replace("'", "''")

            cur.execute(f"""
                INSERT INTO {SCHEMA}.lobby_messages (lobby_id, steam_id, persona_name, avatar_url, message)
                VALUES ({lobby_id}, '{escaped}', '{persona_name}', '{avatar_url}', '{escaped_message}')
                RETURNING id, steam_id, persona_name, avatar_url, message,
                          to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS"+00:00"') as created_at
            """)
            msg = dict(cur.fetchone())
            conn.commit()
            conn.close()
            return {'statusCode': 201, 'headers': HEADERS, 'body': json.dumps({'message': msg})}

        # Сообщить результат (только участники матча)
        if action == 'report_result':
            if lobby['player1_steam_id'] not in [steam_id] and lobby['player2_steam_id'] not in [steam_id]:
                return {'statusCode': 403, 'headers': HEADERS, 'body': json.dumps({'error': 'Только участники матча могут сообщить результат'})}

            winner_steam_id = body.get('winner_steam_id', '').replace("'", "''")
            if winner_steam_id not in [lobby['player1_steam_id'], lobby['player2_steam_id']]:
                return {'statusCode': 400, 'headers': HEADERS, 'body': json.dumps({'error': 'Победитель должен быть участником матча'})}

            cur.execute(f"""
                UPDATE {SCHEMA}.match_lobbies
                SET winner_steam_id = '{winner_steam_id}', status = 'completed', updated_at = NOW()
                WHERE id = {lobby_id}
            """)
            conn.commit()
            conn.close()
            return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps({'ok': True})}

    conn.close()
    return {'statusCode': 405, 'headers': HEADERS, 'body': json.dumps({'error': 'Method not allowed'})}
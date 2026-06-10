import json
import os
import psycopg2
from psycopg2.extras import RealDictCursor
from typing import Dict, Any

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    """
    Лобби матча: чат, сообщить результат, разрешить спор
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

    # ─── GET: получить / создать лобби ───────────────────────────────────────
    if method == 'GET':
        tournament_id = params.get('tournament_id')
        round_index   = params.get('round_index')
        match_index   = params.get('match_index')
        steam_id      = params.get('steam_id')

        if not tournament_id or round_index is None or match_index is None:
            conn.close()
            return {'statusCode': 400, 'headers': HEADERS,
                    'body': json.dumps({'error': 'tournament_id, round_index, match_index required'})}

        if steam_id:
            esc = steam_id.replace("'", "''")
            cur.execute(f"SELECT id FROM {SCHEMA}.tournament_registrations "
                        f"WHERE tournament_id = {int(tournament_id)} AND steam_id = '{esc}'")
            if not cur.fetchone():
                conn.close()
                return {'statusCode': 403, 'headers': HEADERS,
                        'body': json.dumps({'error': 'Доступ только для участников турнира'})}

        cur.execute(f"""
            SELECT id, player1_steam_id, player2_steam_id, winner_steam_id, status,
                   player1_reported_winner, player2_reported_winner, is_dispute, admin_steam_id
            FROM {SCHEMA}.match_lobbies
            WHERE tournament_id = {int(tournament_id)}
              AND round_index   = {int(round_index)}
              AND match_index   = {int(match_index)}
        """)
        lobby = cur.fetchone()

        if not lobby:
            p1 = (params.get('player1_steam_id') or '').replace("'", "''")
            p2 = (params.get('player2_steam_id') or '').replace("'", "''")
            p1v = f"'{p1}'" if p1 else 'NULL'
            p2v = f"'{p2}'" if p2 else 'NULL'
            cur.execute(f"""
                INSERT INTO {SCHEMA}.match_lobbies
                    (tournament_id, round_index, match_index, status, player1_steam_id, player2_steam_id)
                VALUES ({int(tournament_id)}, {int(round_index)}, {int(match_index)}, 'waiting', {p1v}, {p2v})
                RETURNING id, player1_steam_id, player2_steam_id, winner_steam_id, status,
                          player1_reported_winner, player2_reported_winner, is_dispute, admin_steam_id
            """)
            lobby = cur.fetchone()
            conn.commit()

        lobby_id = lobby['id']

        cur.execute(f"""
            SELECT id, steam_id, persona_name, avatar_url, message,
                   to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS"+00:00"') as created_at
            FROM {SCHEMA}.lobby_messages
            WHERE lobby_id = {lobby_id}
            ORDER BY created_at ASC
        """)
        messages = [dict(r) for r in cur.fetchall()]

        def get_player(sid):
            if not sid:
                return None
            s = sid.replace("'", "''")
            cur.execute(f"SELECT COALESCE(nickname, steam_id) as persona_name, avatar_url "
                        f"FROM {SCHEMA}.users WHERE steam_id = '{s}'")
            row = cur.fetchone()
            if not row:
                cur.execute(f"SELECT persona_name, avatar_url FROM {SCHEMA}.tournament_registrations "
                            f"WHERE steam_id = '{s}' AND tournament_id = {int(tournament_id)}")
                row = cur.fetchone()
            if not row:
                return {'steam_id': sid, 'persona_name': sid, 'avatar_url': None}
            return {'steam_id': sid, **dict(row)}

        conn.close()
        return {
            'statusCode': 200, 'headers': HEADERS,
            'body': json.dumps({
                'lobby': dict(lobby),
                'messages': messages,
                'player1': get_player(lobby['player1_steam_id']),
                'player2': get_player(lobby['player2_steam_id']),
            })
        }

    # ─── POST: сообщение / результат / решение спора ─────────────────────────
    if method == 'POST':
        body      = json.loads(event.get('body') or '{}')
        action    = body.get('action', 'message')
        steam_id  = (event.get('headers') or {}).get('X-User-Steam-Id') or body.get('steam_id', '')
        escaped   = steam_id.replace("'", "''") if steam_id else ''

        if not steam_id:
            conn.close()
            return {'statusCode': 401, 'headers': HEADERS, 'body': json.dumps({'error': 'Требуется авторизация'})}

        tournament_id = body.get('tournament_id')
        round_index   = body.get('round_index')
        match_index   = body.get('match_index')

        if not tournament_id or round_index is None or match_index is None:
            conn.close()
            return {'statusCode': 400, 'headers': HEADERS,
                    'body': json.dumps({'error': 'tournament_id, round_index, match_index required'})}

        # Проверить участие в турнире
        cur.execute(f"SELECT id FROM {SCHEMA}.tournament_registrations "
                    f"WHERE tournament_id = {int(tournament_id)} AND steam_id = '{escaped}'")
        if not cur.fetchone():
            conn.close()
            return {'statusCode': 403, 'headers': HEADERS,
                    'body': json.dumps({'error': 'Доступ только для участников турнира'})}

        cur.execute(f"""
            SELECT id, player1_steam_id, player2_steam_id, status,
                   player1_reported_winner, player2_reported_winner, is_dispute
            FROM {SCHEMA}.match_lobbies
            WHERE tournament_id = {int(tournament_id)}
              AND round_index   = {int(round_index)}
              AND match_index   = {int(match_index)}
        """)
        lobby = cur.fetchone()
        if not lobby:
            conn.close()
            return {'statusCode': 404, 'headers': HEADERS, 'body': json.dumps({'error': 'Лобби не найдено'})}

        lobby_id = lobby['id']

        # ── Отправить сообщение ──────────────────────────────────────────────
        if action == 'message':
            message = body.get('message', '').strip()
            if not message:
                conn.close()
                return {'statusCode': 400, 'headers': HEADERS, 'body': json.dumps({'error': 'Пустое сообщение'})}
            persona_name   = body.get('persona_name', steam_id).replace("'", "''")
            avatar_url     = (body.get('avatar_url') or '').replace("'", "''")
            escaped_message = message.replace("'", "''")
            cur.execute(f"""
                INSERT INTO {SCHEMA}.lobby_messages
                    (lobby_id, steam_id, persona_name, avatar_url, message)
                VALUES ({lobby_id}, '{escaped}', '{persona_name}', '{avatar_url}', '{escaped_message}')
                RETURNING id, steam_id, persona_name, avatar_url, message,
                          to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS"+00:00"') as created_at
            """)
            msg = dict(cur.fetchone())
            conn.commit()
            conn.close()
            return {'statusCode': 201, 'headers': HEADERS, 'body': json.dumps({'message': msg})}

        # ── Сообщить результат ───────────────────────────────────────────────
        if action == 'report_result':
            p1_id = lobby['player1_steam_id']
            p2_id = lobby['player2_steam_id']

            if steam_id not in [p1_id, p2_id]:
                conn.close()
                return {'statusCode': 403, 'headers': HEADERS,
                        'body': json.dumps({'error': 'Только участники матча могут сообщить результат'})}

            winner_id = body.get('winner_steam_id', '')
            if winner_id not in [p1_id, p2_id]:
                conn.close()
                return {'statusCode': 400, 'headers': HEADERS,
                        'body': json.dumps({'error': 'Победитель должен быть участником матча'})}

            winner_esc = winner_id.replace("'", "''")

            # Записываем голос игрока
            if steam_id == p1_id:
                cur.execute(f"UPDATE {SCHEMA}.match_lobbies SET player1_reported_winner = '{winner_esc}', updated_at = NOW() WHERE id = {lobby_id}")
            else:
                cur.execute(f"UPDATE {SCHEMA}.match_lobbies SET player2_reported_winner = '{winner_esc}', updated_at = NOW() WHERE id = {lobby_id}")
            conn.commit()

            # Перечитать актуальные голоса
            cur.execute(f"SELECT player1_reported_winner, player2_reported_winner FROM {SCHEMA}.match_lobbies WHERE id = {lobby_id}")
            votes = cur.fetchone()
            v1 = votes['player1_reported_winner']
            v2 = votes['player2_reported_winner']

            if v1 and v2:
                if v1 == v2:
                    # Оба согласны — победитель определён
                    cur.execute(f"""
                        UPDATE {SCHEMA}.match_lobbies
                        SET winner_steam_id = '{v1.replace("'","''")}', status = 'completed',
                            is_dispute = FALSE, updated_at = NOW()
                        WHERE id = {lobby_id}
                    """)
                    conn.commit()

                    # Системное сообщение о победителе
                    cur.execute(f"SELECT COALESCE(nickname, persona_name) as name FROM {SCHEMA}.users "
                                f"LEFT JOIN {SCHEMA}.tournament_registrations tr ON tr.steam_id = users.steam_id "
                                f"WHERE users.steam_id = '{v1.replace(chr(39), chr(39)*2)}'")
                    name_row = cur.fetchone()
                    winner_name = name_row['name'] if name_row else v1
                    sys_msg = f'🏆 Победитель определён: {winner_name}'
                    sys_msg_esc = sys_msg.replace("'", "''")
                    cur.execute(f"""
                        INSERT INTO {SCHEMA}.lobby_messages (lobby_id, steam_id, persona_name, message)
                        VALUES ({lobby_id}, 'system', 'Система', '{sys_msg_esc}')
                    """)
                    conn.commit()
                    conn.close()
                    return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps({'result': 'winner_set', 'winner': v1})}
                else:
                    # Голоса расходятся — спор, приглашаем администратора
                    cur.execute(f"""
                        SELECT steam_id FROM {SCHEMA}.users
                        WHERE is_admin = TRUE LIMIT 1
                    """)
                    admin_row = cur.fetchone()
                    admin_id = admin_row['steam_id'] if admin_row else None
                    admin_val = f"'{admin_id.replace(chr(39), chr(39)*2)}'" if admin_id else 'NULL'

                    cur.execute(f"""
                        UPDATE {SCHEMA}.match_lobbies
                        SET is_dispute = TRUE, status = 'dispute', admin_steam_id = {admin_val}, updated_at = NOW()
                        WHERE id = {lobby_id}
                    """)
                    # Системное сообщение о споре
                    sys_msg = '⚠️ Игроки указали разных победителей. Открыт спор — ожидается решение администратора.'
                    cur.execute(f"""
                        INSERT INTO {SCHEMA}.lobby_messages (lobby_id, steam_id, persona_name, message)
                        VALUES ({lobby_id}, 'system', 'Система', '{sys_msg}')
                    """)
                    conn.commit()
                    conn.close()
                    return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps({'result': 'dispute'})}
            else:
                # Только один проголосовал
                conn.close()
                return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps({'result': 'waiting_second_vote'})}

        # ── Администратор решает спор ────────────────────────────────────────
        if action == 'resolve_dispute':
            # Проверить права администратора
            cur.execute(f"SELECT is_admin FROM {SCHEMA}.users WHERE steam_id = '{escaped}'")
            admin_row = cur.fetchone()
            if not admin_row or not admin_row['is_admin']:
                conn.close()
                return {'statusCode': 403, 'headers': HEADERS, 'body': json.dumps({'error': 'Только администратор может разрешить спор'})}

            winner_id = body.get('winner_steam_id', '')
            p1_id = lobby['player1_steam_id']
            p2_id = lobby['player2_steam_id']
            if winner_id not in [p1_id, p2_id]:
                conn.close()
                return {'statusCode': 400, 'headers': HEADERS, 'body': json.dumps({'error': 'Некорректный победитель'})}

            winner_esc = winner_id.replace("'", "''")
            cur.execute(f"""
                UPDATE {SCHEMA}.match_lobbies
                SET winner_steam_id = '{winner_esc}', status = 'completed',
                    is_dispute = FALSE, updated_at = NOW()
                WHERE id = {lobby_id}
            """)
            sys_msg = f'✅ Администратор разрешил спор. Победитель определён.'
            cur.execute(f"""
                INSERT INTO {SCHEMA}.lobby_messages (lobby_id, steam_id, persona_name, message)
                VALUES ({lobby_id}, 'system', 'Система', '{sys_msg}')
            """)
            conn.commit()
            conn.close()
            return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps({'result': 'resolved', 'winner': winner_id})}

    conn.close()
    return {'statusCode': 405, 'headers': HEADERS, 'body': json.dumps({'error': 'Method not allowed'})}

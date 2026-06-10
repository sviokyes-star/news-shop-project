import json
import os
import base64
import uuid
import psycopg2
import boto3
from psycopg2.extras import RealDictCursor
from typing import Dict, Any

def apply_elo_ratings(cursor, conn, tournament_id: int, game: str):
    """Начисляет рейтинг Эло участникам рейтингового турнира по итогам сетки матчей."""
    K = 32
    SCHEMA = 't_p15345778_news_shop_project'

    cursor.execute(f"SELECT steam_id FROM {SCHEMA}.tournament_registrations WHERE tournament_id = {tournament_id}")
    participants = [r['steam_id'] for r in cursor.fetchall()]
    if not participants:
        return

    esc_game = game.replace("'", "''")
    ratings = {}
    for sid in participants:
        esc = sid.replace("'", "''")
        cursor.execute(f"SELECT points FROM {SCHEMA}.player_rankings WHERE steam_id = '{esc}' AND game = '{esc_game}'")
        row = cursor.fetchone()
        ratings[sid] = row['points'] if row else 1000

    cursor.execute(f"""
        SELECT player1_steam_id, player2_steam_id, winner_steam_id
        FROM {SCHEMA}.match_lobbies
        WHERE tournament_id = {tournament_id}
          AND status = 'completed'
          AND winner_steam_id IS NOT NULL
          AND player1_steam_id IS NOT NULL
          AND player2_steam_id IS NOT NULL
    """)
    matches = cursor.fetchall()

    for match in matches:
        p1, p2, winner = match['player1_steam_id'], match['player2_steam_id'], match['winner_steam_id']
        if p1 not in ratings or p2 not in ratings:
            continue
        r1, r2 = ratings[p1], ratings[p2]
        e1 = 1 / (1 + 10 ** ((r2 - r1) / 400))
        e2 = 1 - e1
        s1 = 1.0 if winner == p1 else 0.0
        s2 = 1.0 - s1
        ratings[p1] = round(r1 + K * (s1 - e1))
        ratings[p2] = round(r2 + K * (s2 - e2))

    for sid, new_rating in ratings.items():
        esc_sid = sid.replace("'", "''")
        cursor.execute(f"""
            INSERT INTO {SCHEMA}.player_rankings (steam_id, game, points)
            VALUES ('{esc_sid}', '{esc_game}', {new_rating})
            ON CONFLICT (steam_id, game) DO UPDATE SET points = {new_rating}
        """)
        cursor.execute(f"""
            INSERT INTO {SCHEMA}.notifications (steam_id, type, title, body, link)
            VALUES ('{esc_sid}', 'rating_update', 'Рейтинг обновлён',
                    'Ваш новый рейтинг: {new_rating} pts', '/profile')
        """)
    conn.commit()


def assign_final_places(cursor, conn, tournament_id: int):
    """Расставляет итоговые места участникам по результатам сетки."""
    SCHEMA = 't_p15345778_news_shop_project'

    # Получаем все завершённые матчи, сортируем по раунду (финал — максимальный round_index)
    cursor.execute(f"""
        SELECT round_index, match_index, player1_steam_id, player2_steam_id, winner_steam_id
        FROM {SCHEMA}.match_lobbies
        WHERE tournament_id = {tournament_id} AND status = 'completed'
          AND winner_steam_id IS NOT NULL
        ORDER BY round_index DESC
    """)
    matches = cursor.fetchall()
    if not matches:
        return

    max_round = matches[0]['round_index']

    # Собираем проигравших по раундам
    losers_by_round = {}
    winner_of_final = None
    loser_of_final = None

    for m in matches:
        p1, p2, w = m['player1_steam_id'], m['player2_steam_id'], m['winner_steam_id']
        loser = p2 if w == p1 else p1
        r = m['round_index']
        if r not in losers_by_round:
            losers_by_round[r] = []
        losers_by_round[r].append(loser)
        if r == max_round:
            winner_of_final = w
            loser_of_final = loser

    if not winner_of_final:
        return

    # Присваиваем места: 1 — победитель финала, 2 — проигравший финала,
    # затем проигравшие предыдущих раундов получают места 3+
    place_assignments = {winner_of_final: 1, loser_of_final: 2}
    current_place = 3
    for rnd in range(max_round - 1, -1, -1):
        losers = losers_by_round.get(rnd, [])
        for sid in losers:
            if sid not in place_assignments:
                place_assignments[sid] = current_place
        current_place += len(losers)

    for sid, place in place_assignments.items():
        esc = sid.replace("'", "''")
        cursor.execute(f"""
            UPDATE {SCHEMA}.tournament_registrations
            SET final_place = {place}
            WHERE tournament_id = {tournament_id} AND steam_id = '{esc}'
        """)
    conn.commit()


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

    def advance_winner(cur, conn, tournament_id, round_index, match_index, winner_steam_id):
        """Продвигает победителя в следующий раунд сетки."""
        next_round = round_index + 1
        next_match = match_index // 2
        slot = match_index % 2  # 0 → player1, 1 → player2
        winner_esc = winner_steam_id.replace("'", "''")

        # Узнаём сколько матчей в текущем раунде — если следующий раунд выходит за финал, ничего не делаем
        cur.execute(f"""
            SELECT COUNT(*) as cnt FROM {SCHEMA}.match_lobbies
            WHERE tournament_id = {int(tournament_id)} AND round_index = {round_index}
        """)
        row = cur.fetchone()
        total_in_round = row['cnt'] if row else 0
        if total_in_round <= 1:
            # Финал сыгран — завершаем турнир
            cur.execute(f"""
                UPDATE {SCHEMA}.tournaments
                SET status = 'completed'
                WHERE id = {int(tournament_id)} AND status != 'completed'
            """)
            conn.commit()
            # Начисляем Эло если турнир рейтинговый
            cur.execute(f"SELECT is_rated, game FROM {SCHEMA}.tournaments WHERE id = {int(tournament_id)}")
            t = cur.fetchone()
            if t and t['is_rated']:
                try:
                    apply_elo_ratings(cur, conn, int(tournament_id), t['game'].lower())
                except Exception as e:
                    print(f"Elo error: {e}")
            try:
                assign_final_places(cur, conn, int(tournament_id))
            except Exception as e:
                print(f"Final places error: {e}")
            return

        field = 'player1_steam_id' if slot == 0 else 'player2_steam_id'

        # Найти или создать лобби следующего раунда
        cur.execute(f"""
            SELECT id FROM {SCHEMA}.match_lobbies
            WHERE tournament_id = {int(tournament_id)}
              AND round_index = {next_round}
              AND match_index = {next_match}
        """)
        next_lobby = cur.fetchone()
        if next_lobby:
            cur.execute(f"""
                UPDATE {SCHEMA}.match_lobbies
                SET {field} = '{winner_esc}', updated_at = NOW()
                WHERE id = {next_lobby['id']}
            """)
        else:
            if slot == 0:
                cur.execute(f"""
                    INSERT INTO {SCHEMA}.match_lobbies
                        (tournament_id, round_index, match_index, status, player1_steam_id)
                    VALUES ({int(tournament_id)}, {next_round}, {next_match}, 'waiting', '{winner_esc}')
                    ON CONFLICT (tournament_id, round_index, match_index) DO UPDATE
                    SET player1_steam_id = '{winner_esc}', updated_at = NOW()
                """)
            else:
                cur.execute(f"""
                    INSERT INTO {SCHEMA}.match_lobbies
                        (tournament_id, round_index, match_index, status, player2_steam_id)
                    VALUES ({int(tournament_id)}, {next_round}, {next_match}, 'waiting', '{winner_esc}')
                    ON CONFLICT (tournament_id, round_index, match_index) DO UPDATE
                    SET player2_steam_id = '{winner_esc}', updated_at = NOW()
                """)
        conn.commit()

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
            SELECT id, steam_id, persona_name, avatar_url, message, image_url,
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

        result_body = {
            'lobby': dict(lobby),
            'messages': messages,
            'player1': get_player(lobby['player1_steam_id']),
            'player2': get_player(lobby['player2_steam_id']),
        }
        conn.close()
        return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps(result_body)}

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
                   player1_reported_winner, player2_reported_winner, is_dispute,
                   round_index, match_index
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

        # ── Загрузить скриншот в S3 и отправить в чат ───────────────────────
        if action == 'upload_screenshot':
            image_b64 = body.get('image_b64', '')
            content_type = body.get('content_type', 'image/png')
            if not image_b64:
                conn.close()
                return {'statusCode': 400, 'headers': HEADERS, 'body': json.dumps({'error': 'Нет изображения'})}

            ext = 'jpg' if 'jpeg' in content_type else 'png'
            key = f"match-screenshots/{tournament_id}/{round_index}_{match_index}_{uuid.uuid4().hex[:8]}.{ext}"
            img_data = base64.b64decode(image_b64)

            s3 = boto3.client(
                's3',
                endpoint_url='https://bucket.poehali.dev',
                aws_access_key_id=os.environ['AWS_ACCESS_KEY_ID'],
                aws_secret_access_key=os.environ['AWS_SECRET_ACCESS_KEY'],
            )
            s3.put_object(Bucket='files', Key=key, Body=img_data, ContentType=content_type)
            image_url = f"https://cdn.poehali.dev/projects/{os.environ['AWS_ACCESS_KEY_ID']}/bucket/{key}"
            image_url_esc = image_url.replace("'", "''")

            persona_name = body.get('persona_name', steam_id).replace("'", "''")
            avatar_url_val = (body.get('avatar_url') or '').replace("'", "''")
            msg_text = 'Скриншот победного экрана'.replace("'", "''")

            cur.execute(f"""
                INSERT INTO {SCHEMA}.lobby_messages
                    (lobby_id, steam_id, persona_name, avatar_url, message, image_url)
                VALUES ({lobby_id}, '{escaped}', '{persona_name}', '{avatar_url_val}', '{msg_text}', '{image_url_esc}')
                RETURNING id, steam_id, persona_name, avatar_url, message, image_url,
                          to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS"+00:00"') as created_at
            """)
            msg = dict(cur.fetchone())
            conn.commit()
            conn.close()
            return {'statusCode': 201, 'headers': HEADERS, 'body': json.dumps({'message': msg, 'image_url': image_url})}

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
                RETURNING id, steam_id, persona_name, avatar_url, message, image_url,
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
                    cur.execute(f"SELECT COALESCE(users.nickname, users.persona_name) as name FROM {SCHEMA}.users "
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
                    # Продвигаем победителя в следующий раунд
                    advance_winner(cur, conn, tournament_id, lobby['round_index'], lobby['match_index'], v1)
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
            # Проверить: глобальный админ ИЛИ администратор турнира
            cur.execute(f"SELECT is_admin FROM {SCHEMA}.users WHERE steam_id = '{escaped}'")
            admin_row = cur.fetchone()
            is_global_admin = admin_row and admin_row['is_admin']

            cur.execute(f"""
                SELECT id FROM {SCHEMA}.tournament_admins
                WHERE tournament_id = {int(tournament_id)} AND steam_id = '{escaped}'
            """)
            is_tournament_admin = cur.fetchone() is not None

            if not is_global_admin and not is_tournament_admin:
                conn.close()
                return {'statusCode': 403, 'headers': HEADERS, 'body': json.dumps({'error': 'Только администратор турнира может разрешить спор'})}

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
            # Продвигаем победителя в следующий раунд
            advance_winner(cur, conn, tournament_id, lobby['round_index'], lobby['match_index'], winner_id)
            conn.close()
            return {'statusCode': 200, 'headers': HEADERS, 'body': json.dumps({'result': 'resolved', 'winner': winner_id})}

    conn.close()
    return {'statusCode': 405, 'headers': HEADERS, 'body': json.dumps({'error': 'Method not allowed'})}
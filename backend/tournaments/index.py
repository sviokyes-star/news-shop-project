'''
Business: Управление регистрациями на турниры
Args: event - dict с httpMethod, body, queryStringParameters
      context - объект с атрибутами request_id, function_name
Returns: HTTP response dict
'''

import json
import os
from typing import Dict, Any, List, Optional
from pydantic import BaseModel, Field, ValidationError
import psycopg2
from psycopg2.extras import RealDictCursor

SCHEMA = 't_p15345778_news_shop_project'


class TournamentRegistration(BaseModel):
    tournament_id: int = Field(..., gt=0)
    steam_id: str = Field(..., min_length=1)
    persona_name: str = Field(..., min_length=1)
    avatar_url: Optional[str] = None


def get_db_connection():
    database_url = os.environ.get('DATABASE_URL')
    if not database_url:
        raise ValueError('DATABASE_URL not configured')
    return psycopg2.connect(database_url)


def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    method: str = event.get('httpMethod', 'GET')
    
    # Handle CORS OPTIONS request
    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, PATCH, DELETE, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-User-Id, X-Auth-Token, X-Admin-Steam-Id',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }
    
    conn = None
    
    try:
        conn = get_db_connection()
        cursor = conn.cursor(cursor_factory=RealDictCursor)
        
        # GET: Получить список турниров с количеством участников или детали одного турнира
        if method == 'GET':
            params = event.get('queryStringParameters', {}) or {}
            steam_id = params.get('steam_id')
            tournament_id = params.get('tournament_id')

            # Генерируем напоминания о скором старте (тихо, без ошибок)
            try:
                generate_tournament_reminders(cursor, conn)
            except Exception:
                pass
            
            # Получить детали турнира с участниками
            if tournament_id:
                cursor.execute('''
                    SELECT 
                        t.id,
                        t.name,
                        t.description,
                        t.prize_pool,
                        t.max_participants,
                        t.status,
                        t.tournament_type,
                        t.game,
                        t.bracket_type,
                        to_char(t.start_date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as start_date,
                        COUNT(tr.id) as participants_count
                    FROM tournaments t
                    LEFT JOIN tournament_registrations tr ON t.id = tr.tournament_id
                    WHERE t.id = %s
                    GROUP BY t.id
                ''', (tournament_id,))
                
                tournament = cursor.fetchone()
                if not tournament:
                    return {
                        'statusCode': 404,
                        'headers': {
                            'Content-Type': 'application/json',
                            'Access-Control-Allow-Origin': '*'
                        },
                        'isBase64Encoded': False,
                        'body': json.dumps({'error': 'Турнир не найден'})
                    }
                
                # Получить участников турнира
                cursor.execute('''
                    SELECT 
                        tr.steam_id,
                        COALESCE(u.nickname, tr.persona_name) as persona_name,
                        COALESCE(u.avatar_url, tr.avatar_url) as avatar_url,
                        to_char(tr.registered_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as registered_at,
                        to_char(tr.confirmed_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as confirmed_at,
                        COALESCE(u.is_admin, false) as is_admin,
                        COALESCE(u.is_moderator, false) as is_moderator,
                        (u.last_online IS NOT NULL AND u.last_online > NOW() - INTERVAL '5 minutes') AS is_online
                    FROM tournament_registrations tr
                    LEFT JOIN t_p15345778_news_shop_project.users u ON tr.steam_id = u.steam_id
                    WHERE tr.tournament_id = %s
                    ORDER BY tr.registered_at ASC
                ''', (tournament_id,))
                
                participants = cursor.fetchall()
                
                result = dict(tournament)
                result['participants'] = [dict(p) for p in participants]
                
                return {
                    'statusCode': 200,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps(result)
                }
            
            # Получить список турниров
            if steam_id:
                # Получить турниры с информацией о регистрации пользователя
                cursor.execute('''
                    SELECT 
                        t.id,
                        t.name,
                        t.description,
                        t.prize_pool,
                        t.max_participants,
                        t.status,
                        t.tournament_type,
                        t.game,
                        t.bracket_type,
                        to_char(t.start_date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as start_date,
                        COUNT(tr.id) as participants_count,
                        EXISTS(
                            SELECT 1 FROM tournament_registrations 
                            WHERE tournament_id = t.id AND steam_id = %s
                        ) as is_registered,
                        (
                            SELECT to_char(confirmed_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"')
                            FROM tournament_registrations 
                            WHERE tournament_id = t.id AND steam_id = %s
                        ) as confirmed_at
                    FROM tournaments t
                    LEFT JOIN tournament_registrations tr ON t.id = tr.tournament_id
                    GROUP BY t.id
                    ORDER BY t.start_date
                ''', (steam_id, steam_id))
            else:
                # Получить все турниры с количеством участников
                cursor.execute('''
                    SELECT 
                        t.id,
                        t.name,
                        t.description,
                        t.prize_pool,
                        t.max_participants,
                        t.status,
                        t.tournament_type,
                        t.game,
                        t.bracket_type,
                        to_char(t.start_date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as start_date,
                        COUNT(tr.id) as participants_count
                    FROM tournaments t
                    LEFT JOIN tournament_registrations tr ON t.id = tr.tournament_id
                    GROUP BY t.id
                    ORDER BY t.start_date
                ''')
            
            tournaments = cursor.fetchall()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'isBase64Encoded': False,
                'body': json.dumps({'tournaments': [dict(row) for row in tournaments]})
            }
        
        # POST: Создать турнир (админ) или зарегистрироваться на турнир (пользователь)
        if method == 'POST':
            body_data = json.loads(event.get('body', '{}'))
            print(f"POST body_data: {body_data}")
            admin_steam_id = event.get('headers', {}).get('X-Admin-Steam-Id')
            print(f"Admin Steam ID: {admin_steam_id}")
            
            # Админ создает турнир
            if admin_steam_id and 'name' in body_data:
                escaped_steam_id = admin_steam_id.replace("'", "''")
                cursor.execute(f"SELECT is_admin FROM users WHERE steam_id = '{escaped_steam_id}'")
                result = cursor.fetchone()
                is_admin = result['is_admin'] if result else False
                
                if not is_admin:
                    return {
                        'statusCode': 403,
                        'headers': {
                            'Content-Type': 'application/json',
                            'Access-Control-Allow-Origin': '*'
                        },
                        'isBase64Encoded': False,
                        'body': json.dumps({'error': 'Admin rights required'})
                    }
                
                name = body_data.get('name', '').strip()
                description = body_data.get('description', '').strip()
                prize_pool = body_data.get('prize_pool')
                max_participants = body_data.get('max_participants')
                tournament_type = body_data.get('tournament_type', 'solo')
                start_date = body_data.get('start_date')
                status = body_data.get('status', 'upcoming')
                game = body_data.get('game', 'CS2')
                bracket_type = body_data.get('bracket_type', 'random')
                
                if not name or prize_pool is None or max_participants is None or not start_date:
                    return {
                        'statusCode': 400,
                        'headers': {
                            'Content-Type': 'application/json',
                            'Access-Control-Allow-Origin': '*'
                        },
                        'isBase64Encoded': False,
                        'body': json.dumps({'error': 'name, prize_pool, max_participants, start_date are required'})
                    }
                
                escaped_name = name.replace("'", "''")
                escaped_description = description.replace("'", "''")
                escaped_start_date = start_date.replace("'", "''")
                escaped_game = game.replace("'", "''")
                
                cursor.execute(f"""
                    INSERT INTO tournaments (name, description, prize_pool, max_participants, tournament_type, start_date, status, game, bracket_type)
                    VALUES ('{escaped_name}', '{escaped_description}', {int(prize_pool)}, {int(max_participants)}, '{tournament_type}', '{escaped_start_date}', '{status}', '{escaped_game}', '{bracket_type}')
                    RETURNING id, name, description, prize_pool, max_participants, tournament_type, start_date, status, game, bracket_type
                """)
                
                tournament = cursor.fetchone()
                conn.commit()
                
                return {
                    'statusCode': 201,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps(dict(tournament), default=str)
                }
            
            # Пользователь регистрируется на турнир
            registration = TournamentRegistration(**body_data)
            
            # Проверить, не зарегистрирован ли уже пользователь
            cursor.execute('''
                SELECT id FROM tournament_registrations 
                WHERE tournament_id = %s AND steam_id = %s
            ''', (registration.tournament_id, registration.steam_id))
            
            if cursor.fetchone():
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Вы уже зарегистрированы на этот турнир'})
                }
            
            # Проверить, есть ли свободные места и время до начала
            cursor.execute('''
                SELECT 
                    t.max_participants,
                    t.start_date,
                    COUNT(tr.id) as participants_count
                FROM tournaments t
                LEFT JOIN tournament_registrations tr ON t.id = tr.tournament_id
                WHERE t.id = %s
                GROUP BY t.id, t.max_participants, t.start_date
            ''', (registration.tournament_id,))
            
            tournament_info = cursor.fetchone()
            if not tournament_info:
                return {
                    'statusCode': 404,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Турнир не найден'})
                }
            
            # Проверить, не начался ли уже турнир
            from datetime import datetime, timezone
            start_date = tournament_info['start_date']
            
            if start_date.tzinfo is None:
                start_date = start_date.replace(tzinfo=timezone.utc)
            
            now = datetime.now(timezone.utc)
            
            if now >= start_date:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Регистрация закрыта. Турнир уже начался.'})
                }
            
            if tournament_info['participants_count'] >= tournament_info['max_participants']:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Нет свободных мест на турнир'})
                }
            
            # Зарегистрировать пользователя
            cursor.execute('''
                INSERT INTO tournament_registrations 
                (tournament_id, steam_id, persona_name, avatar_url)
                VALUES (%s, %s, %s, %s)
                RETURNING id, registered_at
            ''', (
                registration.tournament_id,
                registration.steam_id,
                registration.persona_name,
                registration.avatar_url
            ))
            
            result = cursor.fetchone()

            # Создать уведомление о регистрации
            tournament_name = tournament_info.get('name', 'турнир') if hasattr(tournament_info, 'get') else 'турнир'
            cursor.execute(
                f"SELECT name FROM tournaments WHERE id = %s",
                (registration.tournament_id,)
            )
            t_row = cursor.fetchone()
            t_name = t_row['name'] if t_row else 'турнир'
            cursor.execute(
                f"INSERT INTO {SCHEMA}.notifications (steam_id, type, title, body, link) VALUES (%s, %s, %s, %s, %s)",
                (registration.steam_id, 'tournament_registered',
                 f'Вы зарегистрированы на турнир',
                 f'Вы успешно зарегистрировались на «{t_name}»',
                 f'/tournament/{registration.tournament_id}')
            )

            conn.commit()
            
            return {
                'statusCode': 201,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'isBase64Encoded': False,
                'body': json.dumps({
                    'id': result['id'],
                    'registered_at': str(result['registered_at']),
                    'message': 'Регистрация успешна'
                })
            }
        
        # PUT: Обновить турнир (только админ)
        if method == 'PUT':
            admin_steam_id = event.get('headers', {}).get('X-Admin-Steam-Id')
            
            if not admin_steam_id:
                return {
                    'statusCode': 403,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Admin authentication required'})
                }
            
            escaped_steam_id = admin_steam_id.replace("'", "''")
            cursor.execute(f"SELECT is_admin FROM users WHERE steam_id = '{escaped_steam_id}'")
            result = cursor.fetchone()
            is_admin = result['is_admin'] if result else False
            
            if not is_admin:
                return {
                    'statusCode': 403,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Admin rights required'})
                }
            
            body_data = json.loads(event.get('body', '{}'))
            tournament_id = body_data.get('id')
            
            if not tournament_id:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'id is required'})
                }
            
            update_fields = []
            
            if 'name' in body_data:
                name = body_data['name'].strip()
                escaped_name = name.replace("'", "''")
                update_fields.append(f"name = '{escaped_name}'")
            
            if 'description' in body_data:
                description = body_data['description'].strip()
                escaped_description = description.replace("'", "''")
                update_fields.append(f"description = '{escaped_description}'")
            
            if 'prize_pool' in body_data:
                update_fields.append(f"prize_pool = {int(body_data['prize_pool'])}")
            
            if 'max_participants' in body_data:
                update_fields.append(f"max_participants = {int(body_data['max_participants'])}")
            
            if 'tournament_type' in body_data:
                update_fields.append(f"tournament_type = '{body_data['tournament_type']}'")
            
            if 'start_date' in body_data:
                escaped_date = body_data['start_date'].replace("'", "''")
                update_fields.append(f"start_date = '{escaped_date}'")
            
            if 'status' in body_data:
                update_fields.append(f"status = '{body_data['status']}'")
            
            if 'game' in body_data:
                game = body_data['game']
                escaped_game = game.replace("'", "''")
                update_fields.append(f"game = '{escaped_game}'")
            
            if 'bracket_type' in body_data:
                update_fields.append(f"bracket_type = '{body_data['bracket_type']}'")
            
            if not update_fields:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'No fields to update'})
                }
            
            cursor.execute(f"""
                UPDATE tournaments 
                SET {', '.join(update_fields)}
                WHERE id = {int(tournament_id)}
                RETURNING id, name, description, prize_pool, max_participants, tournament_type, start_date, status, game
            """)
            
            tournament = cursor.fetchone()
            
            if not tournament:
                return {
                    'statusCode': 404,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Tournament not found'})
                }
            
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'isBase64Encoded': False,
                'body': json.dumps(dict(tournament), default=str)
            }
        
        # DELETE: Отменить регистрацию или удалить турнир (админ)
        if method == 'DELETE':
            body_data = json.loads(event.get('body', '{}'))
            tournament_id = body_data.get('tournament_id') or body_data.get('id')
            steam_id = body_data.get('steam_id')
            
            # Отмена регистрации пользователя
            if tournament_id and steam_id:
                escaped_steam_id = steam_id.replace("'", "''")
                
                cursor.execute(
                    f"DELETE FROM tournament_registrations WHERE tournament_id = {int(tournament_id)} AND steam_id = '{escaped_steam_id}' RETURNING id"
                )
                
                deleted = cursor.fetchone()
                
                if not deleted:
                    return {
                        'statusCode': 404,
                        'headers': {
                            'Content-Type': 'application/json',
                            'Access-Control-Allow-Origin': '*'
                        },
                        'isBase64Encoded': False,
                        'body': json.dumps({'error': 'Регистрация не найдена'})
                    }
                
                conn.commit()
                
                return {
                    'statusCode': 200,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'message': 'Регистрация отменена'})
                }
            
            # Удаление турнира (только админ)
            admin_steam_id = event.get('headers', {}).get('X-Admin-Steam-Id')
            
            if not admin_steam_id:
                return {
                    'statusCode': 403,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Admin authentication required'})
                }
            
            escaped_steam_id = admin_steam_id.replace("'", "''")
            cursor.execute(f"SELECT is_admin FROM users WHERE steam_id = '{escaped_steam_id}'")
            result = cursor.fetchone()
            is_admin = result['is_admin'] if result else False
            
            if not is_admin:
                return {
                    'statusCode': 403,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Admin rights required'})
                }
            
            if not tournament_id:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'id is required'})
                }
            
            cursor.execute(f"DELETE FROM tournament_registrations WHERE tournament_id = {int(tournament_id)}")
            cursor.execute(f"DELETE FROM tournaments WHERE id = {int(tournament_id)}")
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'isBase64Encoded': False,
                'body': json.dumps({'message': 'Tournament deleted successfully'})
            }
        
        # PATCH: Подтвердить участие в турнире
        if method == 'PATCH':
            body_data = json.loads(event.get('body', '{}'))
            tournament_id = body_data.get('tournament_id')
            steam_id = body_data.get('steam_id')
            
            if not tournament_id or not steam_id:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'tournament_id and steam_id required'})
                }
            
            escaped_steam_id = steam_id.replace("'", "''")
            
            # Проверяем, что регистрация существует
            cursor.execute(
                f"SELECT * FROM tournament_registrations WHERE tournament_id = {int(tournament_id)} AND steam_id = '{escaped_steam_id}'"
            )
            registration = cursor.fetchone()
            
            if not registration:
                return {
                    'statusCode': 404,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'isBase64Encoded': False,
                    'body': json.dumps({'error': 'Регистрация не найдена'})
                }
            
            # Обновляем время подтверждения
            cursor.execute(
                f"UPDATE tournament_registrations SET confirmed_at = NOW() WHERE tournament_id = {int(tournament_id)} AND steam_id = '{escaped_steam_id}'"
            )
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'isBase64Encoded': False,
                'body': json.dumps({'message': 'Участие подтверждено'})
            }
        
        return {
            'statusCode': 405,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'isBase64Encoded': False,
            'body': json.dumps({'error': 'Method not allowed'})
        }
    
    finally:
        if conn:
            conn.close()


def generate_tournament_reminders(cursor, conn):
    """Генерирует уведомления за 24ч и за 1ч до начала турнира для всех участников."""
    from datetime import datetime, timezone, timedelta
    now = datetime.now(timezone.utc)

    for hours, ntype, label in [(24, 'tournament_24h', 'через 24 часа'), (1, 'tournament_1h', 'через 1 час')]:
        window_start = now + timedelta(hours=hours) - timedelta(minutes=5)
        window_end   = now + timedelta(hours=hours) + timedelta(minutes=5)

        cursor.execute("""
            SELECT t.id, t.name, tr.steam_id
            FROM tournaments t
            JOIN tournament_registrations tr ON tr.tournament_id = t.id
            WHERE t.start_date >= %s AND t.start_date <= %s
        """, (window_start, window_end))
        rows = cursor.fetchall()

        for row in rows:
            tid, tname, steam_id = row['id'], row['name'], row['steam_id']
            # Не создаём дубли
            cursor.execute(
                f"SELECT id FROM {SCHEMA}.notifications WHERE steam_id = %s AND type = %s AND link = %s",
                (steam_id, ntype, f'/tournament/{tid}')
            )
            if cursor.fetchone():
                continue
            cursor.execute(
                f"INSERT INTO {SCHEMA}.notifications (steam_id, type, title, body, link) VALUES (%s, %s, %s, %s, %s)",
                (steam_id, ntype,
                 f'Турнир начнётся {label}',
                 f'«{tname}» стартует {label}. Не пропусти!',
                 f'/tournament/{tid}')
            )
    conn.commit()
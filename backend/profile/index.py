'''
Business: Получение данных профиля пользователя
Args: event - dict с httpMethod, queryStringParameters
      context - объект с атрибутами request_id, function_name
Returns: HTTP response dict с данными профиля
'''

import json
import os
from typing import Dict, Any
import psycopg2
from psycopg2.extras import RealDictCursor


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
                'Access-Control-Allow-Methods': 'GET, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-User-Id, X-Auth-Token',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }
    
    if method != 'GET':
        return {
            'statusCode': 405,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'isBase64Encoded': False,
            'body': json.dumps({'error': 'Method not allowed'})
        }
    
    conn = None
    
    try:
        params = event.get('queryStringParameters', {}) or {}
        steam_id = params.get('steam_id')
        
        if not steam_id:
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'isBase64Encoded': False,
                'body': json.dumps({'error': 'steam_id is required'})
            }
        
        conn = get_db_connection()
        cursor = conn.cursor(cursor_factory=RealDictCursor)
        
        escaped_steam_id = steam_id.replace("'", "''")
        
        cursor.execute(f"""
            SELECT id, steam_id, persona_name, nickname, avatar_url, profile_url, 
                   balance, is_blocked, block_reason,
                   to_char(last_login, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as last_login,
                   to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as created_at,
                   is_online,
                   to_char(last_online, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as last_online
            FROM t_p15345778_news_shop_project.users
            WHERE steam_id = '{escaped_steam_id}'
        """)
        
        user_profile = cursor.fetchone()
        
        if not user_profile:
            user_data = {
                'balance': 0,
                'isBlocked': False
            }
        else:
            user_data = {
                'id': user_profile['id'],
                'steamId': user_profile['steam_id'],
                'personaName': user_profile['persona_name'],
                'nickname': user_profile['nickname'],
                'avatarUrl': user_profile['avatar_url'],
                'profileUrl': user_profile['profile_url'],
                'balance': user_profile['balance'],
                'isBlocked': user_profile['is_blocked'],
                'blockReason': user_profile['block_reason'],
                'lastLogin': user_profile['last_login'],
                'createdAt': user_profile['created_at'],
                'isOnline': user_profile['is_online'],
                'lastOnline': user_profile['last_online']
            }
        
        # Получить турниры пользователя
        cursor.execute('''
            SELECT 
                t.id,
                t.name,
                t.description,
                t.prize_pool,
                t.max_participants,
                t.status,
                t.tournament_type,
                to_char(t.start_date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as start_date,
                to_char(tr.registered_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as registered_at,
                (
                    SELECT COUNT(*) 
                    FROM tournament_registrations 
                    WHERE tournament_id = t.id AND registered_at < tr.registered_at
                ) + 1 as registration_position
            FROM tournament_registrations tr
            JOIN tournaments t ON tr.tournament_id = t.id
            WHERE tr.steam_id = %s
            ORDER BY tr.registered_at DESC
        ''', (steam_id,))
        
        tournaments = cursor.fetchall()
        
        # Проверить существование таблицы purchases
        cursor.execute('''
            SELECT EXISTS (
                SELECT FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_name = 'purchases'
            )
        ''')
        
        purchases_table_exists = cursor.fetchone()['exists']
        
        purchases_count = 0
        total_spent = 0
        
        if purchases_table_exists:
            cursor.execute('''
                SELECT 
                    COUNT(*) as total_purchases,
                    COALESCE(SUM(amount), 0) as total_spent
                FROM purchases
                WHERE steam_id = %s
            ''', (steam_id,))
            
            purchases_stats = cursor.fetchone()
            if purchases_stats:
                purchases_count = purchases_stats['total_purchases']
                total_spent = float(purchases_stats['total_spent'])
        
        result = {
            'user': user_data,
            'tournaments': [dict(row) for row in tournaments],
            'statistics': {
                'tournaments_count': len(tournaments),
                'purchases_count': purchases_count,
                'total_spent': total_spent
            }
        }
        
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'isBase64Encoded': False,
            'body': json.dumps(result)
        }
    
    finally:
        if conn:
            conn.close()
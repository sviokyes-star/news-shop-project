'''
Business: Handle global chat messages - get, post, and moderate (hide) messages
Args: event with httpMethod, body, queryStringParameters
Returns: HTTP response with chat messages or operation confirmation
'''

import json
import os
import psycopg2
from typing import Dict, Any

def get_db_connection():
    dsn = os.environ.get('DATABASE_URL')
    if not dsn:
        raise ValueError('DATABASE_URL environment variable is not set')
    return psycopg2.connect(dsn)

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    method: str = event.get('httpMethod', 'GET')
    
    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, DELETE, PUT, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-Admin-Steam-Id',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }
    
    if method == 'GET':
        return get_messages(event)
    elif method == 'POST':
        return post_message(event)
    elif method == 'DELETE':
        return hide_message(event)
    elif method == 'PUT':
        return toggle_chat_freeze(event)
    
    return {
        'statusCode': 405,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'body': json.dumps({'error': 'Method not allowed'})
    }

def get_messages(event: Dict[str, Any]) -> Dict[str, Any]:
    params = event.get('queryStringParameters') or {}
    limit = int(params.get('limit', '50'))
    
    conn = get_db_connection()
    cur = conn.cursor()
    
    cur.execute('SELECT is_frozen FROM t_p15345778_news_shop_project.chat_settings LIMIT 1')
    is_frozen_row = cur.fetchone()
    is_frozen = is_frozen_row[0] if is_frozen_row else False
    
    cur.execute('''
        SELECT cm.id, cm.steam_id, COALESCE(u.nickname, cm.persona_name) as persona_name, cm.avatar_url, cm.message, 
               to_char(cm.created_at, 'YYYY-MM-DD"T"HH24:MI:SS"+00:00"') as created_at,
               cm.reply_to_message_id,
               COALESCE(u.is_admin, false) as is_admin,
               COALESCE(u.is_moderator, false) as is_moderator,
               (u.last_online IS NOT NULL AND u.last_online > NOW() - INTERVAL '5 minutes') AS is_online
        FROM t_p15345778_news_shop_project.chat_messages cm
        LEFT JOIN t_p15345778_news_shop_project.users u ON cm.steam_id = u.steam_id
        WHERE cm.is_hidden = FALSE
        ORDER BY cm.created_at DESC
        LIMIT %s
    ''', (limit,))
    
    rows = cur.fetchall()
    
    messages = []
    for row in rows:
        reply_to = None
        if row[6]:
            cur.execute('''
                SELECT cm.id, COALESCE(u.nickname, cm.persona_name) as persona_name, cm.message
                FROM t_p15345778_news_shop_project.chat_messages cm
                LEFT JOIN t_p15345778_news_shop_project.users u ON cm.steam_id = u.steam_id
                WHERE cm.id = %s
            ''', (row[6],))
            reply_row = cur.fetchone()
            if reply_row:
                reply_to = {
                    'id': reply_row[0],
                    'personaName': reply_row[1],
                    'message': reply_row[2]
                }
        
        messages.append({
            'id': row[0],
            'steamId': row[1],
            'personaName': row[2],
            'avatarUrl': row[3],
            'message': row[4],
            'createdAt': row[5],
            'replyTo': reply_to,
            'isAdmin': row[7],
            'isModerator': row[8],
            'isOnline': row[9]
        })
    
    messages.reverse()
    
    cur.close()
    conn.close()
    
    return {
        'statusCode': 200,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'isBase64Encoded': False,
        'body': json.dumps({'messages': messages, 'isFrozen': is_frozen})
    }

def post_message(event: Dict[str, Any]) -> Dict[str, Any]:
    body_data = json.loads(event.get('body', '{}'))
    
    steam_id = body_data.get('steam_id')
    persona_name = body_data.get('persona_name')
    avatar_url = body_data.get('avatar_url')
    message = body_data.get('message', '').strip()
    reply_to_message_id = body_data.get('reply_to_message_id')
    
    conn = get_db_connection()
    cur = conn.cursor()
    
    cur.execute('SELECT is_frozen FROM t_p15345778_news_shop_project.chat_settings LIMIT 1')
    is_frozen_row = cur.fetchone()
    is_frozen = is_frozen_row[0] if is_frozen_row else False
    
    cur.execute('SELECT is_admin FROM t_p15345778_news_shop_project.users WHERE steam_id = %s', (steam_id,))
    result = cur.fetchone()
    is_admin = result[0] if result else False
    
    if is_frozen and not is_admin:
        cur.close()
        conn.close()
        return {
            'statusCode': 403,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Chat is frozen by administrator'})
        }
    
    cur.close()
    conn.close()
    
    if not steam_id or not persona_name:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'steam_id and persona_name required'})
        }
    
    if not message or len(message) > 500:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Message must be 1-500 characters'})
        }
    
    conn = get_db_connection()
    cur = conn.cursor()
    
    cur.execute('''
        INSERT INTO t_p15345778_news_shop_project.chat_messages 
        (steam_id, persona_name, avatar_url, message, reply_to_message_id)
        VALUES (%s, %s, %s, %s, %s)
        RETURNING id, to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS"+00:00"') as created_at
    ''', (steam_id, persona_name, avatar_url, message, reply_to_message_id))
    
    row = cur.fetchone()
    message_id = row[0]
    created_at = row[1]
    
    conn.commit()
    cur.close()
    conn.close()
    
    return {
        'statusCode': 201,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'isBase64Encoded': False,
        'body': json.dumps({
            'message': 'Message posted successfully',
            'id': message_id,
            'createdAt': created_at
        })
    }

def hide_message(event: Dict[str, Any]) -> Dict[str, Any]:
    headers = event.get('headers', {})
    admin_steam_id = headers.get('x-admin-steam-id') or headers.get('X-Admin-Steam-Id')
    
    if not admin_steam_id:
        return {
            'statusCode': 401,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Admin authentication required'})
        }
    
    conn = get_db_connection()
    cur = conn.cursor()
    
    cur.execute('SELECT is_admin FROM t_p15345778_news_shop_project.users WHERE steam_id = %s', (admin_steam_id,))
    result = cur.fetchone()
    is_admin = result[0] if result else False
    
    if not is_admin:
        cur.close()
        conn.close()
        return {
            'statusCode': 403,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Admin access required'})
        }
    
    params = event.get('queryStringParameters') or {}
    message_id = params.get('message_id')
    
    if not message_id:
        cur.close()
        conn.close()
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'message_id required'})
        }
    
    cur.execute('UPDATE t_p15345778_news_shop_project.chat_messages SET is_hidden = TRUE WHERE id = %s', (message_id,))
    conn.commit()
    
    cur.close()
    conn.close()
    
    return {
        'statusCode': 200,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'isBase64Encoded': False,
        'body': json.dumps({'message': 'Message hidden successfully'})
    }

def toggle_chat_freeze(event: Dict[str, Any]) -> Dict[str, Any]:
    headers = event.get('headers', {})
    admin_steam_id = headers.get('x-admin-steam-id') or headers.get('X-Admin-Steam-Id')
    
    if not admin_steam_id:
        return {
            'statusCode': 401,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Admin authentication required'})
        }
    
    conn = get_db_connection()
    cur = conn.cursor()
    
    cur.execute('SELECT is_admin FROM t_p15345778_news_shop_project.users WHERE steam_id = %s', (admin_steam_id,))
    result = cur.fetchone()
    is_admin = result[0] if result else False
    
    if not is_admin:
        cur.close()
        conn.close()
        return {
            'statusCode': 403,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Admin access required'})
        }
    
    body_data = json.loads(event.get('body', '{}'))
    is_frozen = body_data.get('is_frozen', False)
    
    cur.execute('UPDATE t_p15345778_news_shop_project.chat_settings SET is_frozen = %s, updated_at = NOW()', (is_frozen,))
    conn.commit()
    
    cur.close()
    conn.close()
    
    return {
        'statusCode': 200,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'isBase64Encoded': False,
        'body': json.dumps({'message': 'Chat freeze status updated', 'isFrozen': is_frozen})
    }
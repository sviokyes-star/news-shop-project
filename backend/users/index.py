'''
Business: Manage users - list, block/unblock, update balance for admin panel
Args: event - dict with httpMethod, body, queryStringParameters
      context - object with request_id
Returns: HTTP response with users data or operation confirmation
'''

import json
import os
from typing import Dict, Any
import psycopg2
from psycopg2.extras import RealDictCursor

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    method: str = event.get('httpMethod', 'GET')
    
    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-Admin-Steam-Id',
                'Access-Control-Max-Age': '86400'
            },
            'body': '',
            'isBase64Encoded': False
        }
    
    db_url = os.environ.get('DATABASE_URL')
    if not db_url:
        return {
            'statusCode': 500,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Database not configured'}),
            'isBase64Encoded': False
        }
    
    conn = psycopg2.connect(db_url)
    cursor = conn.cursor(cursor_factory=RealDictCursor)
    
    try:
        if method == 'GET':
            return get_users(cursor)
        elif method == 'PUT':
            body_data = json.loads(event.get('body', '{}'))
            return update_user(body_data, cursor, conn)
        
        return {
            'statusCode': 405,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Method not allowed'}),
            'isBase64Encoded': False
        }
    
    finally:
        cursor.close()
        conn.close()

def get_users(cursor) -> Dict[str, Any]:
    cursor.execute("""
        SELECT u.id, u.steam_id, u.persona_name, u.avatar_url, u.profile_url, 
               u.balance, u.is_blocked, u.block_reason, u.is_moderator, u.last_login, u.created_at, u.updated_at,
               COALESCE(u.is_admin, false) as is_admin
        FROM t_p15345778_news_shop_project.users u
        ORDER BY u.last_login DESC NULLS LAST, u.created_at DESC
    """)
    
    users = cursor.fetchall()
    
    users_list = []
    for user in users:
        users_list.append({
            'id': user['id'],
            'steamId': user['steam_id'],
            'personaName': user['persona_name'],
            'avatarUrl': user['avatar_url'],
            'profileUrl': user['profile_url'],
            'balance': user['balance'],
            'isBlocked': user['is_blocked'],
            'blockReason': user['block_reason'],
            'isAdmin': user['is_admin'],
            'isModerator': user['is_moderator'] if user['is_moderator'] else False,
            'lastLogin': user['last_login'].isoformat() if user['last_login'] else None,
            'createdAt': user['created_at'].isoformat() if user['created_at'] else None,
            'updatedAt': user['updated_at'].isoformat() if user['updated_at'] else None
        })
    
    return {
        'statusCode': 200,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'body': json.dumps({'users': users_list}),
        'isBase64Encoded': False
    }

def update_user(body_data: Dict[str, Any], cursor, conn) -> Dict[str, Any]:
    steam_id = body_data.get('steamId', '').strip()
    
    if not steam_id:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Steam ID required'}),
            'isBase64Encoded': False
        }
    
    escaped_steam_id = steam_id.replace("'", "''")
    
    # Handle moderator status
    if 'isModerator' in body_data:
        is_moderator = 'true' if body_data['isModerator'] else 'false'
        cursor.execute(f"""
            UPDATE t_p15345778_news_shop_project.users
            SET is_moderator = {is_moderator}, updated_at = NOW()
            WHERE steam_id = '{escaped_steam_id}'
        """)
        conn.commit()
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'success': True}),
            'isBase64Encoded': False
        }
    
    # Handle admin status
    if 'isAdmin' in body_data:
        is_admin = 'true' if body_data['isAdmin'] else 'false'
        cursor.execute(f"""
            UPDATE t_p15345778_news_shop_project.users
            SET is_admin = {is_admin}, updated_at = NOW()
            WHERE steam_id = '{escaped_steam_id}'
        """)
        conn.commit()
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'success': True}),
            'isBase64Encoded': False
        }
    
    # Handle user table updates
    updates = []
    
    if 'balance' in body_data:
        updates.append(f"balance = {int(body_data['balance'])}")
    
    if 'isBlocked' in body_data:
        is_blocked = 'true' if body_data['isBlocked'] else 'false'
        updates.append(f"is_blocked = {is_blocked}")
        
        if body_data['isBlocked'] and 'blockReason' in body_data:
            escaped_reason = body_data['blockReason'].replace("'", "''") if body_data['blockReason'] else ''
            if escaped_reason:
                updates.append(f"block_reason = '{escaped_reason}'")
            else:
                updates.append("block_reason = NULL")
        elif not body_data['isBlocked']:
            updates.append("block_reason = NULL")
    
    if not updates:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'No fields to update'}),
            'isBase64Encoded': False
        }
    
    updates.append("updated_at = NOW()")
    
    cursor.execute(f"""
        UPDATE t_p15345778_news_shop_project.users
        SET {', '.join(updates)}
        WHERE steam_id = '{escaped_steam_id}'
        RETURNING id
    """)
    
    result = cursor.fetchone()
    
    if not result:
        return {
            'statusCode': 404,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'User not found'}),
            'isBase64Encoded': False
        }
    
    conn.commit()

    if 'isBlocked' in body_data:
        if body_data['isBlocked']:
            cursor.execute(
                "UPDATE t_p15345778_news_shop_project.chat_messages SET is_hidden = TRUE WHERE steam_id = %s",
                (steam_id,)
            )
            cursor.execute(
                """
                DELETE FROM t_p15345778_news_shop_project.comment_likes
                WHERE comment_id IN (
                    SELECT id FROM t_p15345778_news_shop_project.comments WHERE steam_id = %s
                )
                """,
                (steam_id,)
            )
            cursor.execute(
                "DELETE FROM t_p15345778_news_shop_project.comments WHERE steam_id = %s",
                (steam_id,)
            )
        else:
            pass
        conn.commit()

    return {
        'statusCode': 200,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'body': json.dumps({'success': True}),
        'isBase64Encoded': False
    }
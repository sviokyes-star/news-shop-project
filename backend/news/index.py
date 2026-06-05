import json
import os
import psycopg2
from psycopg2.extras import RealDictCursor
from datetime import datetime
from typing import Dict, Any

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    '''
    Business: CRUD operations for news management
    Args: event with httpMethod, body, queryStringParameters
    Returns: HTTP response with news data
    '''
    method: str = event.get('httpMethod', 'GET')
    
    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, x-admin-steam-id',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }
    
    dsn = os.environ.get('DATABASE_URL')
    conn = psycopg2.connect(dsn)
    
    try:
        if method == 'GET':
            params = event.get('queryStringParameters') or {}
            news_id = params.get('id')
            
            with conn.cursor(cursor_factory=RealDictCursor) as cur:
                if news_id:
                    cur.execute("""
                        SELECT id, title, category, image_url, content, badge,
                               to_char(date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as date,
                               to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as created_at,
                               to_char(updated_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as updated_at
                        FROM news WHERE id = %s
                    """, (int(news_id),))
                    news_item = cur.fetchone()
                    result = dict(news_item) if news_item else None
                    return {
                        'statusCode': 200,
                        'headers': {
                            'Content-Type': 'application/json',
                            'Access-Control-Allow-Origin': '*'
                        },
                        'body': json.dumps({'news': result})
                    }
                else:
                    cur.execute("""
                        SELECT id, title, category, image_url, content, badge,
                               to_char(date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as date,
                               to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as created_at,
                               to_char(updated_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as updated_at
                        FROM news ORDER BY date DESC
                    """)
                    news_list = cur.fetchall()
                    results = [dict(item) for item in news_list]
                    return {
                        'statusCode': 200,
                        'headers': {
                            'Content-Type': 'application/json',
                            'Access-Control-Allow-Origin': '*'
                        },
                        'body': json.dumps({'news': results})
                    }
        
        elif method == 'POST':
            headers = event.get('headers', {})
            admin_steam_id = headers.get('x-admin-steam-id') or headers.get('X-Admin-Steam-Id')
            
            if not admin_steam_id:
                return {
                    'statusCode': 401,
                    'headers': {'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'Admin authentication required'})
                }
            
            with conn.cursor() as cur:
                escaped_steam_id = admin_steam_id.replace("'", "''")
                cur.execute(f"SELECT COUNT(*) FROM users WHERE steam_id = '{escaped_steam_id}' AND is_admin = true")
                is_admin = cur.fetchone()[0] > 0
                
                if not is_admin:
                    return {
                        'statusCode': 403,
                        'headers': {'Access-Control-Allow-Origin': '*'},
                        'body': json.dumps({'error': 'Admin access required'})
                    }
            
            body_data = json.loads(event.get('body', '{}'))
            
            with conn.cursor(cursor_factory=RealDictCursor) as cur:
                pub_date = body_data.get('date') or 'NOW()'
                cur.execute("""
                    INSERT INTO news (title, category, image_url, content, badge, date)
                    VALUES (%s, %s, %s, %s, %s, %s)
                    RETURNING id, title, category, image_url, content, badge,
                              to_char(date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as date,
                              to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as created_at,
                              to_char(updated_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as updated_at
                """, (
                    body_data['title'],
                    body_data['category'],
                    body_data.get('image_url'),
                    body_data['content'],
                    body_data.get('badge'),
                    pub_date
                ))
                new_news = cur.fetchone()
                conn.commit()
                
                result = dict(new_news)
                
                return {
                    'statusCode': 201,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'news': result})
                }
        
        elif method == 'PUT':
            headers = event.get('headers', {})
            admin_steam_id = headers.get('x-admin-steam-id') or headers.get('X-Admin-Steam-Id')
            
            if not admin_steam_id:
                return {
                    'statusCode': 401,
                    'headers': {'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'Admin authentication required'})
                }
            
            with conn.cursor() as cur:
                escaped_steam_id = admin_steam_id.replace("'", "''")
                cur.execute(f"SELECT COUNT(*) FROM users WHERE steam_id = '{escaped_steam_id}' AND is_admin = true")
                is_admin = cur.fetchone()[0] > 0
                
                if not is_admin:
                    return {
                        'statusCode': 403,
                        'headers': {'Access-Control-Allow-Origin': '*'},
                        'body': json.dumps({'error': 'Admin access required'})
                    }
            
            body_data = json.loads(event.get('body', '{}'))
            news_id = body_data.get('id')
            
            with conn.cursor(cursor_factory=RealDictCursor) as cur:
                date_set = ", date = %s" if body_data.get('date') else ""
                params = [
                    body_data['title'],
                    body_data['category'],
                    body_data.get('image_url'),
                    body_data['content'],
                    body_data.get('badge'),
                ]
                if body_data.get('date'):
                    params.append(body_data['date'])
                params.append(news_id)
                cur.execute(f"""
                    UPDATE news 
                    SET title = %s, category = %s, image_url = %s, 
                        content = %s, badge = %s{date_set}, updated_at = CURRENT_TIMESTAMP
                    WHERE id = %s
                    RETURNING id, title, category, image_url, content, badge,
                              to_char(date, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as date,
                              to_char(created_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as created_at,
                              to_char(updated_at, 'YYYY-MM-DD"T"HH24:MI:SS.MS"+00:00"') as updated_at
                """, params)
                updated_news = cur.fetchone()
                conn.commit()
                
                result = dict(updated_news) if updated_news else None
                
                return {
                    'statusCode': 200,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'news': result})
                }
        
        elif method == 'DELETE':
            headers = event.get('headers', {})
            admin_steam_id = headers.get('x-admin-steam-id') or headers.get('X-Admin-Steam-Id')
            
            if not admin_steam_id:
                return {
                    'statusCode': 401,
                    'headers': {'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'Admin authentication required'})
                }
            
            with conn.cursor() as cur:
                escaped_steam_id = admin_steam_id.replace("'", "''")
                cur.execute(f"SELECT COUNT(*) FROM users WHERE steam_id = '{escaped_steam_id}' AND is_admin = true")
                is_admin = cur.fetchone()[0] > 0
                
                if not is_admin:
                    return {
                        'statusCode': 403,
                        'headers': {'Access-Control-Allow-Origin': '*'},
                        'body': json.dumps({'error': 'Admin access required'})
                    }
            
            params = event.get('queryStringParameters') or {}
            news_id = params.get('id')
            
            with conn.cursor() as cur:
                cur.execute("DELETE FROM news WHERE id = %s", (int(news_id),))
                conn.commit()
                
                return {
                    'statusCode': 200,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'success': True})
                }
        
        return {
            'statusCode': 405,
            'headers': {'Access-Control-Allow-Origin': '*'},
            'body': json.dumps({'error': 'Method not allowed'})
        }
    
    finally:
        conn.close()
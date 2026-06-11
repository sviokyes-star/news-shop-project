import json
import os
import psycopg2
from typing import Dict, Any

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    '''
    Business: Get user balance from users table and add rubles via purchase
    Args: event with httpMethod, queryStringParameters for steam_id, body for transactions
    Returns: User balance or transaction result
    '''
    method: str = event.get('httpMethod', 'GET')
    
    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-User-Steam-Id',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }
    
    dsn = os.environ.get('DATABASE_URL')
    conn = psycopg2.connect(dsn)
    cur = conn.cursor()
    
    try:
        if method == 'GET':
            params = event.get('queryStringParameters') or {}
            steam_id = params.get('steam_id', '').strip()
            action = params.get('action', '')

            if not steam_id:
                return {
                    'statusCode': 400,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'steam_id required'})
                }

            escaped_steam_id = steam_id.replace("'", "''")

            if action == 'history':
                cur.execute(f"""
                    SELECT id, amount, transaction_type, description, created_at
                    FROM t_p15345778_news_shop_project.balance_transactions
                    WHERE steam_id = '{escaped_steam_id}'
                    ORDER BY created_at DESC
                    LIMIT 50
                """)
                rows = cur.fetchall()
                history = [{
                    'id': r[0],
                    'amount': r[1],
                    'type': r[2],
                    'description': r[3] or '',
                    'created_at': r[4].isoformat() if r[4] else None
                } for r in rows]
                return {
                    'statusCode': 200,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'history': history})
                }

            cur.execute(f"""
                SELECT balance
                FROM t_p15345778_news_shop_project.users
                WHERE steam_id = '{escaped_steam_id}'
            """)

            row = cur.fetchone()

            return {
                'statusCode': 200,
                'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                'body': json.dumps({'balance': row[0] if row else 0})
            }
        
        if method == 'POST':
            body_data = json.loads(event.get('body', '{}'))
            steam_id = body_data.get('steam_id', '').strip()
            persona_name = body_data.get('persona_name', '').strip()
            amount = body_data.get('amount')
            transaction_type = body_data.get('transaction_type', 'purchase')
            description = body_data.get('description', '')
            
            if not steam_id or not amount:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'steam_id and amount required'})
                }
            
            escaped_steam_id = steam_id.replace("'", "''")
            escaped_persona_name = persona_name.replace("'", "''")
            escaped_description = description.replace("'", "''")
            
            cur.execute(f"""
                UPDATE t_p15345778_news_shop_project.users
                SET balance = balance + {int(amount)},
                    updated_at = NOW()
                WHERE steam_id = '{escaped_steam_id}'
                RETURNING balance
            """)
            
            result = cur.fetchone()
            if not result:
                conn.close()
                return {
                    'statusCode': 404,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'User not found'})
                }
            
            new_balance = result[0]
            
            cur.execute(f"""
                INSERT INTO t_p15345778_news_shop_project.balance_transactions 
                (steam_id, amount, transaction_type, description)
                VALUES ('{escaped_steam_id}', {int(amount)}, '{transaction_type}', '{escaped_description}')
            """)
            
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({
                    'balance': new_balance,
                    'added': amount
                })
            }
        
        return {
            'statusCode': 405,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Method not allowed'})
        }
    
    finally:
        cur.close()
        conn.close()
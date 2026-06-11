'''
Business: Handle shop purchases with balance deduction
Args: event - dict with httpMethod, body for purchase
      context - object with request_id
Returns: HTTP response with purchase confirmation and updated balance
'''

import json
import os
from typing import Dict, Any
import psycopg2

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    method: str = event.get('httpMethod', 'GET')
    
    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, X-Steam-Id',
                'Access-Control-Max-Age': '86400'
            },
            'body': '',
            'isBase64Encoded': False
        }
    
    if method != 'POST':
        return {
            'statusCode': 405,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Method not allowed'}),
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
    
    body_data = json.loads(event.get('body', '{}'))
    
    steam_id = body_data.get('steam_id', '').strip()
    persona_name = body_data.get('persona_name', '').strip()
    shop_item_id = body_data.get('shop_item_id')
    quantity = body_data.get('quantity')

    if not steam_id or not shop_item_id:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'steam_id and shop_item_id required'}),
            'isBase64Encoded': False
        }
    
    conn = psycopg2.connect(db_url)
    cursor = conn.cursor()
    
    try:
        conn.autocommit = False
        
        # Get shop item details
        escaped_steam_id = steam_id.replace("'", "''")
        escaped_persona_name = persona_name.replace("'", "''")
        
        cursor.execute(f"""
            SELECT name, amount, price, is_slider, unit_price, unit_name, slider_min, slider_max, slider_step
            FROM t_p15345778_news_shop_project.shop_items 
            WHERE id = {int(shop_item_id)} AND is_active = true
        """)
        
        item = cursor.fetchone()
        
        if not item:
            cursor.close()
            conn.close()
            return {
                'statusCode': 404,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({'error': 'Shop item not found'}),
                'isBase64Encoded': False
            }
        
        item_name, item_amount, item_price, is_slider, unit_price, unit_name, slider_min, slider_max, slider_step = item

        # Для товаров с ползунком — считаем цену из quantity
        if is_slider:
            if quantity is None:
                cursor.close()
                conn.close()
                return {
                    'statusCode': 400,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'quantity required for slider items'}),
                    'isBase64Encoded': False
                }
            qty = int(quantity)
            if qty < slider_min or qty > slider_max:
                cursor.close()
                conn.close()
                return {
                    'statusCode': 400,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': f'quantity must be between {slider_min} and {slider_max}'}),
                    'isBase64Encoded': False
                }
            item_price = qty * unit_price
            item_amount = f'{qty} {unit_name}'
        
        # Get user balance
        cursor.execute(f"""
            SELECT balance 
            FROM t_p15345778_news_shop_project.users 
            WHERE steam_id = '{escaped_steam_id}'
        """)
        
        balance_row = cursor.fetchone()
        current_balance = balance_row[0] if balance_row else 0
        
        # Check if user has enough balance
        if current_balance < item_price:
            cursor.close()
            conn.close()
            return {
                'statusCode': 400,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({
                    'error': 'Insufficient balance',
                    'required': item_price,
                    'current': current_balance
                }),
                'isBase64Encoded': False
            }
        
        # Deduct balance
        new_balance = current_balance - item_price
        
        cursor.execute(f"""
            UPDATE t_p15345778_news_shop_project.users 
            SET balance = {int(new_balance)}, updated_at = NOW()
            WHERE steam_id = '{escaped_steam_id}'
        """)
        
        # Record transaction
        escaped_item_name = item_name.replace("'", "''")
        cursor.execute(f"""
            INSERT INTO t_p15345778_news_shop_project.balance_transactions 
            (steam_id, persona_name, amount, transaction_type, description)
            VALUES ('{escaped_steam_id}', '{escaped_persona_name}', {int(-item_price)}, 'purchase', 'Purchased: {escaped_item_name}')
        """)
        
        # Record purchase
        escaped_item_amount = item_amount.replace("'", "''")
        cursor.execute(f"""
            INSERT INTO t_p15345778_news_shop_project.purchases 
            (steam_id, persona_name, product_id, product_name, amount, price)
            VALUES ('{escaped_steam_id}', '{escaped_persona_name}', {int(shop_item_id)}, '{escaped_item_name}', '{escaped_item_amount}', {int(item_price)})
            RETURNING id
        """)
        
        purchase_id = cursor.fetchone()[0]
        
        conn.commit()
        cursor.close()
        conn.close()
        
        return {
            'statusCode': 200,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({
                'success': True,
                'purchase_id': purchase_id,
                'new_balance': new_balance,
                'item_name': item_name,
                'item_amount': item_amount
            }),
            'isBase64Encoded': False
        }
        
    except Exception as e:
        conn.rollback()
        cursor.close()
        conn.close()
        return {
            'statusCode': 500,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': f'Purchase failed: {str(e)}'}),
            'isBase64Encoded': False
        }
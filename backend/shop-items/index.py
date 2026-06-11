import json
import os
import psycopg2
from typing import Dict, Any

def handler(event: Dict[str, Any], context: Any) -> Dict[str, Any]:
    '''
    Business: Manage shop items (CRUD operations)
    Args: event with httpMethod, body, queryStringParameters
    Returns: HTTP response with shop items or operation status
    '''
    method: str = event.get('httpMethod', 'GET')
    
    if method == 'OPTIONS':
        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, PATCH, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, x-admin-steam-id',
                'Access-Control-Max-Age': '86400'
            },
            'body': ''
        }
    
    dsn = os.environ.get('DATABASE_URL')
    conn = psycopg2.connect(dsn)
    cur = conn.cursor()
    
    def row_to_item(row):
        return {
            'id': row[0],
            'name': row[1],
            'amount': row[2],
            'price': row[3],
            'is_active': row[4],
            'order_position': row[5],
            'category': row[6] or '',
            'is_slider': row[7],
            'slider_min': row[8],
            'slider_max': row[9],
            'slider_step': row[10],
            'unit_price': row[11],
            'unit_name': row[12] or '',
        }

    SELECT_FIELDS = "id, name, amount, price, is_active, order_position, category, is_slider, slider_min, slider_max, slider_step, unit_price, unit_name"
    
    try:
        if method == 'GET':
            params = event.get('queryStringParameters') or {}
            include_inactive = params.get('include_inactive') == 'true'
            
            if include_inactive:
                query = f"SELECT {SELECT_FIELDS} FROM t_p15345778_news_shop_project.shop_items ORDER BY order_position, id"
            else:
                query = f"SELECT {SELECT_FIELDS} FROM t_p15345778_news_shop_project.shop_items WHERE is_active = true ORDER BY order_position, id"
            
            cur.execute(query)
            rows = cur.fetchall()
            items = [row_to_item(r) for r in rows]
            
            return {
                'statusCode': 200,
                'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                'body': json.dumps({'items': items})
            }
        
        if method in ['POST', 'PUT', 'DELETE']:
            admin_steam_id = event.get('headers', {}).get('X-Admin-Steam-Id')
            
            if not admin_steam_id:
                return {
                    'statusCode': 403,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'Admin authentication required'})
                }
            
            escaped_steam_id = admin_steam_id.replace("'", "''")
            cur.execute(f"SELECT COUNT(*) FROM users WHERE steam_id = '{escaped_steam_id}' AND is_admin = true")
            is_admin = cur.fetchone()[0] > 0
            
            if not is_admin:
                return {
                    'statusCode': 403,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'Access denied. Admin rights required'})
                }
        
        if method == 'POST':
            body_data = json.loads(event.get('body', '{}'))
            name = body_data.get('name', '').strip()
            amount = body_data.get('amount', '').strip()
            price = body_data.get('price')
            category = body_data.get('category', '').strip()
            is_slider = bool(body_data.get('is_slider', False))
            slider_min = int(body_data.get('slider_min', 1))
            slider_max = int(body_data.get('slider_max', 100))
            slider_step = int(body_data.get('slider_step', 1))
            unit_price = int(body_data.get('unit_price', 0))
            unit_name = body_data.get('unit_name', '').strip()
            
            if not name or not amount or price is None:
                return {
                    'statusCode': 400,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'name, amount, and price are required'})
                }
            
            escaped_name = name.replace("'", "''")
            escaped_amount = amount.replace("'", "''")
            escaped_category = category.replace("'", "''")
            escaped_unit_name = unit_name.replace("'", "''")
            
            cur.execute("SELECT COALESCE(MAX(order_position), 0) FROM t_p15345778_news_shop_project.shop_items")
            max_position = cur.fetchone()[0]
            new_position = max_position + 10
            
            cur.execute(f"""
                INSERT INTO t_p15345778_news_shop_project.shop_items
                (name, amount, price, order_position, category, is_slider, slider_min, slider_max, slider_step, unit_price, unit_name)
                VALUES ('{escaped_name}', '{escaped_amount}', {int(price)}, {new_position}, '{escaped_category}',
                        {is_slider}, {slider_min}, {slider_max}, {slider_step}, {unit_price}, '{escaped_unit_name}')
                RETURNING {SELECT_FIELDS}
            """)
            
            row = cur.fetchone()
            conn.commit()
            
            return {
                'statusCode': 201,
                'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                'body': json.dumps({'item': row_to_item(row)})
            }
        
        if method == 'PUT':
            body_data = json.loads(event.get('body', '{}'))
            item_id = body_data.get('id')
            
            if not item_id:
                return {
                    'statusCode': 400,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'id is required'})
                }
            
            update_fields = []
            
            if 'name' in body_data:
                name = body_data['name'].strip()
                if name:
                    update_fields.append(f"name = '{name.replace(chr(39), chr(39)*2)}'")
            
            if 'amount' in body_data:
                amount = body_data['amount'].strip()
                if amount:
                    update_fields.append(f"amount = '{amount.replace(chr(39), chr(39)*2)}'")
            
            if 'price' in body_data:
                update_fields.append(f"price = {int(body_data['price'])}")
            
            if 'is_active' in body_data:
                update_fields.append(f"is_active = {body_data['is_active']}")
            
            if 'order_position' in body_data:
                update_fields.append(f"order_position = {int(body_data['order_position'])}")
            
            if 'category' in body_data:
                cat = body_data['category'].strip().replace("'", "''")
                update_fields.append(f"category = '{cat}'")
            
            if 'is_slider' in body_data:
                update_fields.append(f"is_slider = {bool(body_data['is_slider'])}")
            
            if 'slider_min' in body_data:
                update_fields.append(f"slider_min = {int(body_data['slider_min'])}")
            
            if 'slider_max' in body_data:
                update_fields.append(f"slider_max = {int(body_data['slider_max'])}")
            
            if 'slider_step' in body_data:
                update_fields.append(f"slider_step = {int(body_data['slider_step'])}")
            
            if 'unit_price' in body_data:
                update_fields.append(f"unit_price = {int(body_data['unit_price'])}")
            
            if 'unit_name' in body_data:
                un = body_data['unit_name'].strip().replace("'", "''")
                update_fields.append(f"unit_name = '{un}'")
            
            if not update_fields:
                return {
                    'statusCode': 400,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'No fields to update'})
                }
            
            update_fields.append("updated_at = CURRENT_TIMESTAMP")
            update_query = f"""
                UPDATE t_p15345778_news_shop_project.shop_items 
                SET {', '.join(update_fields)}
                WHERE id = {int(item_id)}
                RETURNING {SELECT_FIELDS}
            """
            
            cur.execute(update_query)
            row = cur.fetchone()
            
            if not row:
                return {
                    'statusCode': 404,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'Item not found'})
                }
            
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                'body': json.dumps({'item': row_to_item(row)})
            }
        
        if method == 'DELETE':
            params = event.get('queryStringParameters') or {}
            item_id = params.get('id')
            
            if not item_id:
                return {
                    'statusCode': 400,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'id is required'})
                }
            
            cur.execute(f"DELETE FROM t_p15345778_news_shop_project.shop_items WHERE id = {int(item_id)} RETURNING id")
            deleted = cur.fetchone()
            
            if not deleted:
                return {
                    'statusCode': 404,
                    'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                    'body': json.dumps({'error': 'Item not found'})
                }
            
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
                'body': json.dumps({'success': True, 'deleted_id': int(item_id)})
            }
        
        return {
            'statusCode': 405,
            'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'},
            'body': json.dumps({'error': 'Method not allowed'})
        }
    
    finally:
        cur.close()
        conn.close()

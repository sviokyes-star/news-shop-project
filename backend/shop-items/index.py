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
    
    try:
        if method == 'GET':
            params = event.get('queryStringParameters') or {}
            include_inactive = params.get('include_inactive') == 'true'
            
            if include_inactive:
                query = "SELECT id, name, amount, price, is_active, order_position, category FROM t_p15345778_news_shop_project.shop_items ORDER BY order_position, id"
            else:
                query = "SELECT id, name, amount, price, is_active, order_position, category FROM t_p15345778_news_shop_project.shop_items WHERE is_active = true ORDER BY order_position, id"
            
            cur.execute(query)
            rows = cur.fetchall()
            
            items = []
            for row in rows:
                items.append({
                    'id': row[0],
                    'name': row[1],
                    'amount': row[2],
                    'price': row[3],
                    'is_active': row[4],
                    'order_position': row[5],
                    'category': row[6] if len(row) > 6 else ''
                })
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({'items': items})
            }
        
        if method in ['POST', 'PUT', 'DELETE']:
            admin_steam_id = event.get('headers', {}).get('X-Admin-Steam-Id')
            
            if not admin_steam_id:
                return {
                    'statusCode': 403,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'Admin authentication required'})
                }
            
            escaped_steam_id = admin_steam_id.replace("'", "''")
            cur.execute(f"SELECT COUNT(*) FROM users WHERE steam_id = '{escaped_steam_id}' AND is_admin = true")
            is_admin = cur.fetchone()[0] > 0
            
            if not is_admin:
                return {
                    'statusCode': 403,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'Access denied. Admin rights required'})
                }
        
        if method == 'POST':
            body_data = json.loads(event.get('body', '{}'))
            name = body_data.get('name', '').strip()
            amount = body_data.get('amount', '').strip()
            price = body_data.get('price')
            category = body_data.get('category', '').strip()
            
            if not name or not amount or price is None:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'name, amount, and price are required'})
                }
            
            escaped_name = name.replace("'", "''")
            escaped_amount = amount.replace("'", "''")
            escaped_category = category.replace("'", "''")
            
            # Get max order_position and add 10
            cur.execute("SELECT COALESCE(MAX(order_position), 0) FROM t_p15345778_news_shop_project.shop_items")
            max_position = cur.fetchone()[0]
            new_position = max_position + 10
            
            cur.execute(f"""
                INSERT INTO t_p15345778_news_shop_project.shop_items (name, amount, price, order_position, category)
                VALUES ('{escaped_name}', '{escaped_amount}', {int(price)}, {new_position}, '{escaped_category}')
                RETURNING id, name, amount, price, is_active, order_position, category
            """)
            
            row = cur.fetchone()
            conn.commit()
            
            return {
                'statusCode': 201,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({
                    'item': {
                        'id': row[0],
                        'name': row[1],
                        'amount': row[2],
                        'price': row[3],
                        'is_active': row[4],
                        'order_position': row[5],
                        'category': row[6]
                    }
                })
            }
        
        if method == 'PUT':
            body_data = json.loads(event.get('body', '{}'))
            item_id = body_data.get('id')
            
            if not item_id:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'id is required'})
                }
            
            # Build dynamic UPDATE query based on provided fields
            update_fields = []
            
            if 'name' in body_data:
                name = body_data['name'].strip()
                if name:
                    escaped_name = name.replace("'", "''")
                    update_fields.append(f"name = '{escaped_name}'")
            
            if 'amount' in body_data:
                amount = body_data['amount'].strip()
                if amount:
                    escaped_amount = amount.replace("'", "''")
                    update_fields.append(f"amount = '{escaped_amount}'")
            
            if 'price' in body_data:
                price = body_data['price']
                update_fields.append(f"price = {int(price)}")
            
            if 'is_active' in body_data:
                is_active = body_data['is_active']
                update_fields.append(f"is_active = {is_active}")
            
            if 'order_position' in body_data:
                order_position = body_data['order_position']
                update_fields.append(f"order_position = {int(order_position)}")
            
            if 'category' in body_data:
                category = body_data['category'].strip()
                escaped_category = category.replace("'", "''")
                update_fields.append(f"category = '{escaped_category}'")
            
            if not update_fields:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'No fields to update'})
                }
            
            update_fields.append("updated_at = CURRENT_TIMESTAMP")
            update_query = f"""
                UPDATE t_p15345778_news_shop_project.shop_items 
                SET {', '.join(update_fields)}
                WHERE id = {int(item_id)}
                RETURNING id, name, amount, price, is_active, order_position, category
            """
            
            cur.execute(update_query)
            row = cur.fetchone()
            
            if not row:
                return {
                    'statusCode': 404,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'Item not found'})
                }
            
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({
                    'item': {
                        'id': row[0],
                        'name': row[1],
                        'amount': row[2],
                        'price': row[3],
                        'is_active': row[4],
                        'order_position': row[5]
                    }
                })
            }
        
        if method == 'DELETE':
            params = event.get('queryStringParameters') or {}
            item_id = params.get('id')
            
            if not item_id:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'item id required'})
                }
            
            cur.execute(f"DELETE FROM t_p15345778_news_shop_project.shop_items WHERE id = {int(item_id)}")
            conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({'success': True, 'message': 'Item deleted'})
            }
        
        if method == 'PATCH':
            body_data = json.loads(event.get('body', '{}'))
            item_id = body_data.get('id')
            direction = body_data.get('direction')
            
            if not item_id or direction not in ['up', 'down']:
                return {
                    'statusCode': 400,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'id and direction (up/down) required'})
                }
            
            cur.execute(f"SELECT order_position FROM t_p15345778_news_shop_project.shop_items WHERE id = {int(item_id)}")
            result = cur.fetchone()
            
            if not result:
                return {
                    'statusCode': 404,
                    'headers': {
                        'Content-Type': 'application/json',
                        'Access-Control-Allow-Origin': '*'
                    },
                    'body': json.dumps({'error': 'Item not found'})
                }
            
            current_position = result[0]
            
            if direction == 'up':
                cur.execute(f"""
                    SELECT id, order_position 
                    FROM t_p15345778_news_shop_project.shop_items 
                    WHERE order_position < {current_position} 
                    ORDER BY order_position DESC 
                    LIMIT 1
                """)
            else:
                cur.execute(f"""
                    SELECT id, order_position 
                    FROM t_p15345778_news_shop_project.shop_items 
                    WHERE order_position > {current_position} 
                    ORDER BY order_position ASC 
                    LIMIT 1
                """)
            
            swap_result = cur.fetchone()
            
            if swap_result:
                swap_id, swap_position = swap_result
                
                cur.execute(f"UPDATE t_p15345778_news_shop_project.shop_items SET order_position = {swap_position} WHERE id = {int(item_id)}")
                cur.execute(f"UPDATE t_p15345778_news_shop_project.shop_items SET order_position = {current_position} WHERE id = {swap_id}")
                conn.commit()
            
            return {
                'statusCode': 200,
                'headers': {
                    'Content-Type': 'application/json',
                    'Access-Control-Allow-Origin': '*'
                },
                'body': json.dumps({'success': True, 'message': 'Order updated'})
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
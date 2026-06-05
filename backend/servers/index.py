'''
Business: Manage game servers - CRUD operations for admin panel
Args: event - dict with httpMethod, body, queryStringParameters
      context - object with request_id
Returns: HTTP response with servers data or operation confirmation
'''

import json
import os
import socket
import struct
from typing import Dict, Any, Tuple, Optional
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
            params = event.get('queryStringParameters') or {}
            if params.get('action') == 'status':
                server_id = params.get('server_id')
                if server_id:
                    return query_single_server(int(server_id), db_url)
                return query_all_servers(db_url)
            return get_servers(cursor)
        elif method == 'POST':
            params = event.get('queryStringParameters') or {}
            if params.get('action') == 'status':
                return query_all_servers(db_url)
            body_data = json.loads(event.get('body', '{}'))
            return create_server(body_data, cursor, conn)
        elif method == 'PUT':
            body_data = json.loads(event.get('body', '{}'))
            return update_server(body_data, cursor, conn)
        elif method == 'DELETE':
            params = event.get('queryStringParameters', {})
            return delete_server(params, cursor, conn)
        
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

def get_servers(cursor) -> Dict[str, Any]:
    cursor.execute("""
        SELECT id, name, ip_address, port, game_type, map, max_players, 
               current_players, status, description, is_active, order_position,
               created_at, updated_at
        FROM t_p15345778_news_shop_project.servers
        WHERE is_active = true
        ORDER BY order_position, id
    """)
    
    servers = cursor.fetchall()
    
    servers_list = []
    for server in servers:
        servers_list.append({
            'id': server['id'],
            'name': server['name'],
            'ipAddress': server['ip_address'],
            'port': server['port'],
            'gameType': server['game_type'],
            'map': server['map'],
            'maxPlayers': server['max_players'],
            'currentPlayers': server['current_players'],
            'status': server['status'],
            'description': server['description'],
            'isActive': server['is_active'],
            'orderPosition': server['order_position'],
            'createdAt': server['created_at'].isoformat() if server['created_at'] else None,
            'updatedAt': server['updated_at'].isoformat() if server['updated_at'] else None
        })
    
    return {
        'statusCode': 200,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'body': json.dumps({'servers': servers_list}),
        'isBase64Encoded': False
    }

def create_server(body_data: Dict[str, Any], cursor, conn) -> Dict[str, Any]:
    name = body_data.get('name', '').strip()
    ip_address = body_data.get('ipAddress', '').strip()
    port = body_data.get('port')
    game_type = body_data.get('gameType', '').strip()
    map_name = body_data.get('map', '').strip()
    max_players = body_data.get('maxPlayers', 32)
    current_players = body_data.get('currentPlayers', 0)
    status = body_data.get('status', 'online')
    description = body_data.get('description', '').strip()
    order_position = body_data.get('orderPosition', 0)
    
    if not name or not ip_address or not port or not game_type:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Missing required fields: name, ipAddress, port, gameType'}),
            'isBase64Encoded': False
        }
    
    escaped_name = name.replace("'", "''")
    escaped_ip = ip_address.replace("'", "''")
    escaped_game_type = game_type.replace("'", "''")
    escaped_map = map_name.replace("'", "''") if map_name else ''
    escaped_description = description.replace("'", "''") if description else ''
    escaped_status = status.replace("'", "''")
    
    cursor.execute(f"""
        INSERT INTO t_p15345778_news_shop_project.servers 
        (name, ip_address, port, game_type, map, max_players, current_players, status, description, order_position)
        VALUES ('{escaped_name}', '{escaped_ip}', {int(port)}, '{escaped_game_type}', 
                {f"'{escaped_map}'" if escaped_map else "NULL"}, {int(max_players)}, {int(current_players)}, 
                '{escaped_status}', {f"'{escaped_description}'" if escaped_description else "NULL"}, {int(order_position)})
        RETURNING id
    """)
    
    server_id = cursor.fetchone()['id']
    conn.commit()
    
    return {
        'statusCode': 201,
        'headers': {
            'Content-Type': 'application/json',
            'Access-Control-Allow-Origin': '*'
        },
        'body': json.dumps({'success': True, 'serverId': server_id}),
        'isBase64Encoded': False
    }

def update_server(body_data: Dict[str, Any], cursor, conn) -> Dict[str, Any]:
    server_id = body_data.get('id')
    
    if not server_id:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Server ID required'}),
            'isBase64Encoded': False
        }
    
    updates = []
    
    if 'name' in body_data:
        escaped_name = body_data['name'].replace("'", "''")
        updates.append(f"name = '{escaped_name}'")
    
    if 'ipAddress' in body_data:
        escaped_ip = body_data['ipAddress'].replace("'", "''")
        updates.append(f"ip_address = '{escaped_ip}'")
    
    if 'port' in body_data:
        updates.append(f"port = {int(body_data['port'])}")
    
    if 'gameType' in body_data:
        escaped_game_type = body_data['gameType'].replace("'", "''")
        updates.append(f"game_type = '{escaped_game_type}'")
    
    if 'map' in body_data:
        escaped_map = body_data['map'].replace("'", "''") if body_data['map'] else ''
        if escaped_map:
            updates.append(f"map = '{escaped_map}'")
        else:
            updates.append("map = NULL")
    
    if 'maxPlayers' in body_data:
        updates.append(f"max_players = {int(body_data['maxPlayers'])}")
    
    if 'currentPlayers' in body_data:
        updates.append(f"current_players = {int(body_data['currentPlayers'])}")
    
    if 'status' in body_data:
        escaped_status = body_data['status'].replace("'", "''")
        updates.append(f"status = '{escaped_status}'")
    
    if 'description' in body_data:
        escaped_description = body_data['description'].replace("'", "''") if body_data['description'] else ''
        if escaped_description:
            updates.append(f"description = '{escaped_description}'")
        else:
            updates.append("description = NULL")
    
    if 'orderPosition' in body_data:
        updates.append(f"order_position = {int(body_data['orderPosition'])}")
    
    if 'isActive' in body_data:
        updates.append(f"is_active = {body_data['isActive']}")
    
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
    
    updates.append("updated_at = CURRENT_TIMESTAMP")
    
    cursor.execute(f"""
        UPDATE t_p15345778_news_shop_project.servers
        SET {', '.join(updates)}
        WHERE id = {int(server_id)}
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

def delete_server(params: Dict[str, Any], cursor, conn) -> Dict[str, Any]:
    server_id = params.get('id')
    
    if not server_id:
        return {
            'statusCode': 400,
            'headers': {
                'Content-Type': 'application/json',
                'Access-Control-Allow-Origin': '*'
            },
            'body': json.dumps({'error': 'Server ID required'}),
            'isBase64Encoded': False
        }
    
    cursor.execute(f"""
        UPDATE t_p15345778_news_shop_project.servers
        SET is_active = false, updated_at = CURRENT_TIMESTAMP
        WHERE id = {int(server_id)}
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


def query_source_server(ip: str, port: int, timeout: int = 3) -> Optional[Dict[str, Any]]:
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.settimeout(timeout)
        request = b'\xFF\xFF\xFF\xFFTSource Engine Query\x00'
        sock.sendto(request, (ip, port))
        data, _ = sock.recvfrom(4096)
        sock.close()
        if len(data) < 6:
            return None
        if data[4:5] == b'A':
            challenge = data[5:9]
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            sock.settimeout(timeout)
            sock.sendto(b'\xFF\xFF\xFF\xFFTSource Engine Query\x00' + challenge, (ip, port))
            data, _ = sock.recvfrom(4096)
            sock.close()
        offset = 5
        offset += 1  # protocol
        name_end = data.find(b'\x00', offset); offset = name_end + 1
        map_end = data.find(b'\x00', offset)
        map_name = data[offset:map_end].decode('utf-8', errors='ignore'); offset = map_end + 1
        folder_end = data.find(b'\x00', offset); offset = folder_end + 1
        game_end = data.find(b'\x00', offset)
        game_name = data[offset:game_end].decode('utf-8', errors='ignore'); offset = game_end + 1
        if offset + 2 > len(data):
            return None
        offset += 2  # app_id
        if offset + 2 <= len(data):
            players = data[offset]
            max_players = data[offset + 1]
            return {'status': 'online', 'players': players, 'max_players': max_players, 'map': map_name, 'game': game_name}
        return None
    except socket.timeout:
        return {'status': 'offline', 'error': 'timeout'}
    except Exception as e:
        return {'status': 'offline', 'error': str(e)}


def query_single_server(server_id: int, db_url: str) -> Dict[str, Any]:
    conn = psycopg2.connect(db_url)
    cursor = conn.cursor()
    try:
        cursor.execute(f"""
            SELECT id, name, ip_address, port, game_type, map, max_players, current_players
            FROM t_p15345778_news_shop_project.servers
            WHERE id = {int(server_id)} AND is_active = true
        """)
        server = cursor.fetchone()
        if not server:
            return {'statusCode': 404, 'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'}, 'body': json.dumps({'error': 'Server not found'}), 'isBase64Encoded': False}
        sid, name, ip_address, port, game_type, map_name, max_players, current_players = server
        query_result = query_source_server(ip_address, port)
        if query_result and query_result['status'] == 'online':
            cursor.execute(f"""
                UPDATE t_p15345778_news_shop_project.servers
                SET status = 'online', current_players = {int(query_result['players'])},
                    max_players = {int(query_result['max_players'])},
                    map = '{query_result['map'].replace("'", "''")}', updated_at = CURRENT_TIMESTAMP
                WHERE id = {int(sid)}
            """)
            conn.commit()
            result = {'id': sid, 'name': name, 'ipAddress': ip_address, 'port': port, 'status': 'online', 'currentPlayers': query_result['players'], 'maxPlayers': query_result['max_players'], 'map': query_result['map']}
        else:
            cursor.execute(f"UPDATE t_p15345778_news_shop_project.servers SET status = 'offline', updated_at = CURRENT_TIMESTAMP WHERE id = {int(sid)}")
            conn.commit()
            result = {'id': sid, 'name': name, 'ipAddress': ip_address, 'port': port, 'status': 'offline', 'currentPlayers': 0, 'maxPlayers': max_players}
        return {'statusCode': 200, 'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'}, 'body': json.dumps({'server': result}), 'isBase64Encoded': False}
    finally:
        cursor.close()
        conn.close()


def query_all_servers(db_url: str) -> Dict[str, Any]:
    conn = psycopg2.connect(db_url)
    cursor = conn.cursor()
    try:
        cursor.execute("SELECT id, ip_address, port FROM t_p15345778_news_shop_project.servers WHERE is_active = true")
        servers = cursor.fetchall()
        results = []
        for server_id, ip_address, port in servers:
            query_result = query_source_server(ip_address, port)
            if query_result and query_result['status'] == 'online':
                cursor.execute(f"""
                    UPDATE t_p15345778_news_shop_project.servers
                    SET status = 'online', current_players = {int(query_result['players'])},
                        max_players = {int(query_result['max_players'])},
                        map = '{query_result['map'].replace("'", "''")}', updated_at = CURRENT_TIMESTAMP
                    WHERE id = {int(server_id)}
                """)
                results.append({'id': server_id, 'status': 'online', 'currentPlayers': query_result['players'], 'maxPlayers': query_result['max_players'], 'map': query_result['map']})
            else:
                cursor.execute(f"UPDATE t_p15345778_news_shop_project.servers SET status = 'offline', updated_at = CURRENT_TIMESTAMP WHERE id = {int(server_id)}")
                results.append({'id': server_id, 'status': 'offline'})
        conn.commit()
        return {'statusCode': 200, 'headers': {'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*'}, 'body': json.dumps({'servers': results}), 'isBase64Encoded': False}
    finally:
        cursor.close()
        conn.close()
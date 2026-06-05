import json
import os
import base64
import psycopg2

SCHEMA = 't_p15345778_news_shop_project'
CORS = {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
    'Access-Control-Max-Age': '86400',
}

def ok(data): return {'statusCode': 200, 'headers': CORS, 'body': json.dumps(data)}
def err(code, msg): return {'statusCode': code, 'headers': CORS, 'body': json.dumps({'error': msg})}

def handler(event: dict, context) -> dict:
    """Обновление профиля пользователя: никнейм (action=nickname) или аватар (action=avatar)."""
    if event.get('httpMethod') == 'OPTIONS':
        return {'statusCode': 200, 'headers': CORS, 'body': ''}
    if event.get('httpMethod') != 'POST':
        return err(405, 'Method not allowed')

    body = json.loads(event.get('body') or '{}')
    action = body.get('action')
    steam_id = body.get('steam_id')

    if not steam_id:
        return err(400, 'steam_id required')

    conn = psycopg2.connect(os.environ['DATABASE_URL'])
    cur = conn.cursor()

    try:
        if action == 'nickname':
            nickname = body.get('nickname', '').strip()
            if not nickname:
                return err(400, 'nickname required')
            if len(nickname) < 3 or len(nickname) > 30:
                return err(400, 'Nickname must be between 3 and 30 characters')
            cur.execute(
                f"UPDATE {SCHEMA}.users SET nickname = %s, updated_at = NOW() WHERE steam_id = %s RETURNING id",
                (nickname, steam_id)
            )
            if not cur.fetchone():
                return err(404, 'User not found')
            conn.commit()
            return ok({'success': True, 'nickname': nickname})

        elif action == 'avatar':
            image_data = body.get('image')
            if not image_data:
                return err(400, 'image required')
            if ',' in image_data:
                image_data = image_data.split(',')[1]
            image_bytes = base64.b64decode(image_data)
            if len(image_bytes) > 5 * 1024 * 1024:
                return err(400, 'Image size must be less than 5MB')
            avatar_url = f"data:image/png;base64,{image_data}"
            cur.execute(
                f"UPDATE {SCHEMA}.users SET avatar_url = %s, updated_at = NOW() WHERE steam_id = %s",
                (avatar_url, steam_id)
            )
            conn.commit()
            return ok({'success': True, 'avatar_url': avatar_url})

        else:
            return err(400, 'action must be "nickname" or "avatar"')

    finally:
        cur.close()
        conn.close()

import json
import os
import base64
import uuid
import psycopg2
import boto3

SCHEMA = 't_p15345778_news_shop_project'

CORS = {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
}

def get_conn():
    return psycopg2.connect(os.environ['DATABASE_URL'])

def handler(event: dict, context) -> dict:
    """Управление настройками сайта: логотип и другие параметры."""
    if event.get('httpMethod') == 'OPTIONS':
        return {'statusCode': 200, 'headers': CORS, 'body': ''}

    method = event.get('httpMethod', 'GET')

    if method == 'GET':
        conn = get_conn()
        cur = conn.cursor()
        cur.execute(f"SELECT key, value FROM {SCHEMA}.site_settings")
        rows = cur.fetchall()
        cur.close()
        conn.close()
        settings = {r[0]: r[1] for r in rows}
        return {'statusCode': 200, 'headers': CORS, 'body': json.dumps(settings)}

    if method == 'POST':
        body = json.loads(event.get('body') or '{}')
        image_data = body.get('image')

        if not image_data:
            return {'statusCode': 400, 'headers': CORS, 'body': json.dumps({'error': 'image required'})}

        if ',' in image_data:
            image_data = image_data.split(',')[1]

        image_bytes = base64.b64decode(image_data)

        if len(image_bytes) > 5 * 1024 * 1024:
            return {'statusCode': 400, 'headers': CORS, 'body': json.dumps({'error': 'Max 5MB'})}

        s3 = boto3.client(
            's3',
            endpoint_url='https://bucket.poehali.dev',
            aws_access_key_id=os.environ['AWS_ACCESS_KEY_ID'],
            aws_secret_access_key=os.environ['AWS_SECRET_ACCESS_KEY'],
        )

        filename = f"logo/logo-{uuid.uuid4().hex}.png"
        s3.put_object(Bucket='files', Key=filename, Body=image_bytes, ContentType='image/png')
        logo_url = f"https://cdn.poehali.dev/projects/{os.environ['AWS_ACCESS_KEY_ID']}/files/{filename}"

        conn = get_conn()
        cur = conn.cursor()
        cur.execute(
            f"INSERT INTO {SCHEMA}.site_settings (key, value, updated_at) VALUES ('logo_url', %s, NOW()) ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value, updated_at = NOW()",
            (logo_url,)
        )
        conn.commit()
        cur.close()
        conn.close()

        return {'statusCode': 200, 'headers': CORS, 'body': json.dumps({'success': True, 'logo_url': logo_url})}

    return {'statusCode': 405, 'headers': CORS, 'body': json.dumps({'error': 'method not allowed'})}

import json
import os
import base64
import uuid
import boto3

CORS = {
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
}

def handler(event: dict, context) -> dict:
    """Загрузка логотипа партнёра в S3 и возврат публичного URL."""
    if event.get('httpMethod') == 'OPTIONS':
        return {'statusCode': 200, 'headers': CORS, 'body': ''}

    if event.get('httpMethod') != 'POST':
        return {'statusCode': 405, 'headers': CORS, 'body': json.dumps({'error': 'method not allowed'})}

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

    filename = f"partners/logo-{uuid.uuid4().hex}.png"
    s3.put_object(Bucket='files', Key=filename, Body=image_bytes, ContentType='image/png')
    logo_url = f"https://cdn.poehali.dev/projects/{os.environ['AWS_ACCESS_KEY_ID']}/files/{filename}"

    return {'statusCode': 200, 'headers': CORS, 'body': json.dumps({'success': True, 'logo_url': logo_url})}
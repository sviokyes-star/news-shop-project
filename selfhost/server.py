import base64
import importlib.util
import json
import os
import re
import sys
import traceback
from pathlib import Path
from typing import Any, Callable, Dict

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse, PlainTextResponse, Response

BACKEND_DIR = Path(os.environ.get("BACKEND_DIR", "/app/backend"))
PUBLIC_FILES_BASE = os.environ.get("PUBLIC_FILES_BASE", "").rstrip("/")
S3_ENDPOINT = os.environ.get("S3_ENDPOINT", "")

CDN_PATTERN = re.compile(r"https://cdn\.poehali\.dev/projects/[^/\"]+/(?:bucket|files)/")


def _patch_boto3() -> None:
    if not S3_ENDPOINT:
        return
    try:
        import boto3
    except ImportError:
        return

    original_client = boto3.client

    def client(*args: Any, **kwargs: Any) -> Any:
        if args and args[0] == "s3":
            kwargs["endpoint_url"] = S3_ENDPOINT
        return original_client(*args, **kwargs)

    boto3.client = client


_patch_boto3()


def _load_handlers() -> Dict[str, Callable]:
    handlers: Dict[str, Callable] = {}
    if not BACKEND_DIR.exists():
        print(f"[server] backend dir not found: {BACKEND_DIR}", flush=True)
        return handlers

    for entry in sorted(BACKEND_DIR.iterdir()):
        index_file = entry / "index.py"
        if not entry.is_dir() or not index_file.exists():
            continue

        module_name = f"fn_{entry.name.replace('-', '_')}"
        spec = importlib.util.spec_from_file_location(module_name, index_file)
        if spec is None or spec.loader is None:
            continue

        module = importlib.util.module_from_spec(spec)
        sys.modules[module_name] = module
        sys.path.insert(0, str(entry))
        try:
            spec.loader.exec_module(module)
        except Exception:
            print(f"[server] failed to load {entry.name}:\n{traceback.format_exc()}", flush=True)
            continue
        finally:
            sys.path.remove(str(entry))

        handler = getattr(module, "handler", None)
        if callable(handler):
            handlers[entry.name] = handler
            print(f"[server] loaded function: {entry.name}", flush=True)

    return handlers


HANDLERS = _load_handlers()


class LambdaContext:
    def __init__(self, request_id: str, function_name: str) -> None:
        self.request_id = request_id
        self.function_name = function_name
        self.function_version = "1"
        self.memory_limit_in_mb = 512


async def _build_event(request: Request, path: str) -> Dict[str, Any]:
    raw_body = await request.body()
    is_base64 = False
    if raw_body:
        try:
            body = raw_body.decode("utf-8")
        except UnicodeDecodeError:
            body = base64.b64encode(raw_body).decode("ascii")
            is_base64 = True
    else:
        body = ""

    headers = dict(request.headers)
    if "authorization" in headers:
        headers.setdefault("x-authorization", headers["authorization"])
    if "cookie" in headers:
        headers.setdefault("x-cookie", headers["cookie"])

    client_ip = headers.get("x-forwarded-for", "").split(",")[0].strip()
    if not client_ip and request.client:
        client_ip = request.client.host

    return {
        "httpMethod": request.method,
        "path": "/" + path if path else "/",
        "headers": headers,
        "queryStringParameters": dict(request.query_params) or {},
        "body": body,
        "isBase64Encoded": is_base64,
        "requestContext": {
            "identity": {"sourceIp": client_ip},
            "httpMethod": request.method,
        },
    }


def _rewrite_cdn(text: str) -> str:
    if not PUBLIC_FILES_BASE:
        return text
    return CDN_PATTERN.sub(PUBLIC_FILES_BASE + "/", text)


app = FastAPI(title="Self-hosted functions", docs_url=None, redoc_url=None)


@app.get("/health")
def health() -> JSONResponse:
    return JSONResponse({"status": "ok", "functions": sorted(HANDLERS.keys())})


@app.api_route("/{function_name}", methods=["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"])
@app.api_route(
    "/{function_name}/{path:path}",
    methods=["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
)
async def invoke(function_name: str, request: Request, path: str = "") -> Response:
    handler = HANDLERS.get(function_name)
    if handler is None:
        return JSONResponse({"error": "Function not found"}, status_code=404)

    event = await _build_event(request, path)
    context = LambdaContext(request.headers.get("x-request-id", "local"), function_name)

    try:
        result = handler(event, context)
    except Exception:
        print(f"[{function_name}] error:\n{traceback.format_exc()}", flush=True)
        return JSONResponse(
            {"error": "Internal server error"},
            status_code=500,
            headers={"Access-Control-Allow-Origin": "*"},
        )

    if not isinstance(result, dict):
        return JSONResponse({"error": "Invalid handler response"}, status_code=500)

    status_code = int(result.get("statusCode", 200))
    headers = {str(k): str(v) for k, v in (result.get("headers") or {}).items()}
    headers.setdefault("Access-Control-Allow-Origin", "*")
    headers.pop("Content-Length", None)
    headers.pop("content-length", None)

    body = result.get("body", "")
    if result.get("isBase64Encoded"):
        content = base64.b64decode(body or "")
        return Response(content=content, status_code=status_code, headers=headers)

    if not isinstance(body, str):
        body = json.dumps(body, ensure_ascii=False)

    return PlainTextResponse(
        content=_rewrite_cdn(body),
        status_code=status_code,
        headers=headers,
        media_type=headers.get("Content-Type", "application/json"),
    )

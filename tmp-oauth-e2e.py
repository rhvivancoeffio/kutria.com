#!/usr/bin/env python3
import json, hashlib, base64, secrets, urllib.parse, urllib.request

API = "http://localhost:5050"
MCP = "http://localhost:5055"


def req(method, url, data=None, headers=None, form=False):
    headers = dict(headers or {})
    body = None
    if data is not None:
        if form:
            body = urllib.parse.urlencode(data).encode()
            headers.setdefault("Content-Type", "application/x-www-form-urlencoded")
        else:
            body = json.dumps(data).encode()
            headers.setdefault("Content-Type", "application/json")
    request = urllib.request.Request(url, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=30) as res:
            raw = res.read().decode()
            if raw.startswith("{") or raw.startswith("["):
                return res.status, json.loads(raw)
            return res.status, {"raw": raw[:500]}
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            payload = json.loads(raw) if raw else {}
        except Exception:
            payload = {"raw": raw}
        print(f"FAIL HTTP {e.code} {method} {url}")
        print(json.dumps(payload, indent=2))
        raise SystemExit(1)


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).decode().rstrip("=")


def main() -> None:
    verifier = b64url(secrets.token_bytes(32))
    challenge = b64url(hashlib.sha256(verifier.encode()).digest())
    suffix = secrets.token_hex(3)
    email = f"mcp-test-{suffix}@kutria.test"
    password = "Test1234!"
    identifier = f"mcp{suffix}"

    print(f"1) SignUp {email}")
    _, signup = req("POST", f"{API}/auth/signup", {
        "identifier": identifier, "email": email, "password": password,
        "displayName": "MCP Tester", "name": "MCP Tester",
    })
    token = signup.get("token") or signup.get("accessToken")
    if not token:
        _, signin = req("POST", f"{API}/auth/signin", {"email": email, "password": password})
        token = signin.get("token") or signin.get("accessToken")

    print("2) DCR")
    _, client = req("POST", f"{MCP}/oauth/register", {
        "redirect_uris": ["https://oauth.pstmn.io/v1/callback"],
        "grant_types": ["authorization_code", "refresh_token"],
        "response_types": ["code"],
        "client_name": "AgentCurl",
        "token_endpoint_auth_method": "none",
    })
    client_id = client["client_id"]

    print("3) authorize/complete")
    _, complete = req("POST", f"{MCP}/oauth/authorize/complete", {
        "clientId": client_id,
        "redirectUri": "https://oauth.pstmn.io/v1/callback",
        "state": "agent-state-1",
        "codeChallenge": challenge,
        "codeChallengeMethod": "S256",
        "scope": "mcp:read mcp:write",
        "resource": MCP,
        "clientName": "AgentCurl",
    }, headers={"Authorization": f"Bearer {token}"})
    code = urllib.parse.parse_qs(urllib.parse.urlparse(complete["redirectUrl"]).query)["code"][0]

    print("4) token")
    _, tok = req("POST", f"{MCP}/oauth/token", {
        "grant_type": "authorization_code",
        "code": code,
        "redirect_uri": "https://oauth.pstmn.io/v1/callback",
        "client_id": client_id,
        "code_verifier": verifier,
        "resource": MCP,
    }, form=True)
    print("OK", tok.get("token_type"), "expires_in=", tok.get("expires_in"),
          "access_token=", (tok.get("access_token") or "")[:40] + "...")


if __name__ == "__main__":
    main()

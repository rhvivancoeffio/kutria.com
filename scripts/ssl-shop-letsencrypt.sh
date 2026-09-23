#!/usr/bin/env bash
# Interactive Let's Encrypt wildcard renew + upload for Commerce.Shop.
# Asks only for inputs that are required; everything else has defaults.
#
# Usage:
#   ./scripts/ssl-shop-letsencrypt.sh
#   make ssl-shop-letsencrypt
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

WEBAPP_SHOP_NAME="${WEBAPP_SHOP_NAME:-web-app-kutria-commerce-shop}"
WEBAPP_RESOURCE_GROUP="${WEBAPP_RESOURCE_GROUP:-Gravity-Agentic}"
SHOP_SSL_DOMAIN="${SHOP_SSL_DOMAIN:-kutria.com}"
AZURE_DNS_RG="${AZURE_DNS_RG:-gravity-agentic}"
AZURE_DNS_ZONE="${AZURE_DNS_ZONE:-kutria.com}"
LE_CONFIG_DIR="$ROOT/certs/letsencrypt"
LE_WORK_DIR="$ROOT/certs/letsencrypt-work"
LE_LOGS_DIR="$ROOT/certs/letsencrypt-logs"
LE_CERT_NAME="kutria-wildcard"
SHOP_SSL_PFX="$ROOT/certs/kutria-wildcard.pfx"
AUTH_HOOK="$ROOT/scripts/certbot-azure-auth.sh"
CLEANUP_HOOK="$ROOT/scripts/certbot-azure-cleanup.sh"

prompt() {
  local label="$1" default="${2:-}" value
  if [ -n "$default" ]; then
    read -r -p "${label} [${default}]: " value
    echo "${value:-$default}"
  else
    read -r -p "${label}: " value
    echo "$value"
  fi
}

prompt_secret() {
  local label="$1" default_hint="${2:-}" value
  if [ -n "$default_hint" ]; then
    read -r -s -p "${label} [${default_hint}]: " value
    echo >&2
    echo "${value:-}"
  else
    read -r -s -p "${label}: " value
    echo >&2
    echo "$value"
  fi
}

command -v certbot >/dev/null 2>&1 || {
  echo "Error: certbot no está instalado. En macOS: brew install certbot"
  exit 1
}
command -v az >/dev/null 2>&1 || {
  echo "Error: az CLI no está instalado o no está en PATH."
  exit 1
}
command -v openssl >/dev/null 2>&1 || {
  echo "Error: openssl no está instalado."
  exit 1
}
[ -x "$AUTH_HOOK" ] && [ -x "$CLEANUP_HOOK" ] || {
  chmod +x "$AUTH_HOOK" "$CLEANUP_HOOK"
}

echo "=== Let's Encrypt → Azure Shop SSL ==="
echo "  Web App:  ${WEBAPP_SHOP_NAME}"
echo "  Hostname: *.${SHOP_SSL_DOMAIN}"
echo "  DNS zone: ${AZURE_DNS_ZONE} (RG ${AZURE_DNS_RG})"
echo ""

EMAIL="$(prompt "Email Let's Encrypt" "${LETSENCRYPT_EMAIL:-ricardo@kutria.com}")"
[ -n "$EMAIL" ] || { echo "Error: email requerido."; exit 1; }

echo "Password del PFX (Enter = generar una aleatoria):"
PFX_PASS="$(prompt_secret "SHOP_SSL_PFX_PASSWORD")"
if [ -z "$PFX_PASS" ]; then
  PFX_PASS="$(openssl rand -base64 18 | tr -d '/+=' | head -c 24)"
  echo "  Generada automáticamente (se guardará en .env)."
fi

echo ""
read -r -p "¿Continuar con emisión + upload? [Y/n]: " confirm
confirm="${confirm:-Y}"
case "$confirm" in
  Y|y|yes|YES) ;;
  *) echo "Cancelado."; exit 0 ;;
esac

mkdir -p "$LE_CONFIG_DIR" "$LE_WORK_DIR" "$LE_LOGS_DIR" "$(dirname "$SHOP_SSL_PFX")"

export AZURE_DNS_RG AZURE_DNS_ZONE

echo ""
echo "→ Emitiendo certificado Let's Encrypt para *.${SHOP_SSL_DOMAIN}..."
# Prefer renew if lineage exists; otherwise certonly
if [ -d "$LE_CONFIG_DIR/live/$LE_CERT_NAME" ]; then
  certbot renew \
    --cert-name "$LE_CERT_NAME" \
    --force-renewal \
    --non-interactive \
    --preferred-challenges dns \
    --manual \
    --manual-auth-hook "$AUTH_HOOK" \
    --manual-cleanup-hook "$CLEANUP_HOOK" \
    --preferred-chain "ISRG Root X1" \
    --config-dir "$LE_CONFIG_DIR" \
    --work-dir "$LE_WORK_DIR" \
    --logs-dir "$LE_LOGS_DIR" \
    --no-random-sleep-on-renew
else
  certbot certonly \
    --non-interactive \
    --agree-tos \
    --email "$EMAIL" \
    --preferred-challenges dns \
    --manual \
    --manual-auth-hook "$AUTH_HOOK" \
    --manual-cleanup-hook "$CLEANUP_HOOK" \
    --preferred-chain "ISRG Root X1" \
    --config-dir "$LE_CONFIG_DIR" \
    --work-dir "$LE_WORK_DIR" \
    --logs-dir "$LE_LOGS_DIR" \
    --cert-name "$LE_CERT_NAME" \
    -d "*.${SHOP_SSL_DOMAIN}"
fi

LIVE="$LE_CONFIG_DIR/live/$LE_CERT_NAME"
[ -f "$LIVE/fullchain.pem" ] && [ -f "$LIVE/privkey.pem" ] || {
  echo "Error: no se encontró el certificado en ${LIVE}"
  exit 1
}

echo "→ Empaquetando PFX en ${SHOP_SSL_PFX}..."
openssl pkcs12 -export \
  -out "$SHOP_SSL_PFX" \
  -inkey "$LIVE/privkey.pem" \
  -in "$LIVE/fullchain.pem" \
  -passout "pass:${PFX_PASS}"

# Persist password for later make ssl-shop-renew (gitignored)
if [ -f "$ROOT/.env" ]; then
  if grep -q '^SHOP_SSL_PFX_PASSWORD=' "$ROOT/.env"; then
    sed -i.bak "s|^SHOP_SSL_PFX_PASSWORD=.*|SHOP_SSL_PFX_PASSWORD=${PFX_PASS}|" "$ROOT/.env"
    rm -f "$ROOT/.env.bak"
  else
    echo "SHOP_SSL_PFX_PASSWORD=${PFX_PASS}" >> "$ROOT/.env"
  fi
else
  echo "SHOP_SSL_PFX_PASSWORD=${PFX_PASS}" > "$ROOT/.env"
fi
if grep -q '^LETSENCRYPT_EMAIL=' "$ROOT/.env" 2>/dev/null; then
  sed -i.bak "s|^LETSENCRYPT_EMAIL=.*|LETSENCRYPT_EMAIL=${EMAIL}|" "$ROOT/.env"
  rm -f "$ROOT/.env.bak"
else
  echo "LETSENCRYPT_EMAIL=${EMAIL}" >> "$ROOT/.env"
fi

echo "→ Subiendo PFX y haciendo bind SNI en Azure..."
WEBAPP_SHOP_NAME="$WEBAPP_SHOP_NAME" \
WEBAPP_RESOURCE_GROUP="$WEBAPP_RESOURCE_GROUP" \
SHOP_SSL_DOMAIN="$SHOP_SSL_DOMAIN" \
SHOP_SSL_PFX="$SHOP_SSL_PFX" \
SHOP_SSL_PFX_PASSWORD="$PFX_PASS" \
SKIP_GENERATE=1 \
"$ROOT/scripts/ssl-shop.sh" renew

echo ""
echo "=== Verificar ==="
echo | openssl s_client -connect "demo.${SHOP_SSL_DOMAIN}:443" -servername "demo.${SHOP_SSL_DOMAIN}" 2>/dev/null \
  | openssl x509 -noout -issuer -dates || true
curl -sS --max-time 30 "https://demo.${SHOP_SSL_DOMAIN}/health" || true
echo ""
echo "Listo. Cuando expire (~90 días), vuelve a ejecutar: ./scripts/ssl-shop-letsencrypt.sh"

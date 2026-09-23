#!/usr/bin/env bash
# Shop custom domain + SSL helpers for web-app-kutria-commerce-shop.
# No cron: run renew manually when the cert is about to expire.
#
# Usage:
#   scripts/ssl-shop.sh dns
#   scripts/ssl-shop.sh generate
#   scripts/ssl-shop.sh upload
#   scripts/ssl-shop.sh renew   # generate (unless SKIP_GENERATE=1) + upload + bind
#
# Env:
#   WEBAPP_SHOP_NAME          (default: web-app-kutria-commerce-shop)
#   WEBAPP_RESOURCE_GROUP     (default: Gravity-Agentic)
#   SHOP_SSL_DOMAIN           (default: kutria.com)
#   SHOP_SSL_PFX              (default: ./certs/kutria-wildcard.pfx)
#   SHOP_SSL_PFX_PASSWORD     (required for upload; used when generating)
#   SHOP_SSL_DAYS             (default: 365)
#   SKIP_GENERATE=1           (renew: only upload existing PFX)

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WEBAPP_SHOP_NAME="${WEBAPP_SHOP_NAME:-web-app-kutria-commerce-shop}"
WEBAPP_RESOURCE_GROUP="${WEBAPP_RESOURCE_GROUP:-Gravity-Agentic}"
SHOP_SSL_DOMAIN="${SHOP_SSL_DOMAIN:-kutria.com}"
SHOP_SSL_PFX="${SHOP_SSL_PFX:-$ROOT/certs/kutria-wildcard.pfx}"
SHOP_SSL_DAYS="${SHOP_SSL_DAYS:-365}"
WILDCARD_HOST="*.${SHOP_SSL_DOMAIN}"

cmd="${1:-}"

require_az() {
  command -v az >/dev/null 2>&1 || { echo "Error: az CLI is required."; exit 1; }
}

print_dns() {
  require_az
  local default_host verification_id
  default_host="$(az webapp show \
    --name "$WEBAPP_SHOP_NAME" \
    --resource-group "$WEBAPP_RESOURCE_GROUP" \
    --query defaultHostName -o tsv)"
  verification_id="$(az webapp show \
    --name "$WEBAPP_SHOP_NAME" \
    --resource-group "$WEBAPP_RESOURCE_GROUP" \
    --query customDomainVerificationId -o tsv)"

  echo "=== DNS records for ${SHOP_SSL_DOMAIN} (create at your DNS host) ==="
  echo ""
  echo "  CNAME  *                 →  ${default_host}"
  echo "  TXT    asuid             →  ${verification_id}"
  echo "  TXT    asuid.${SHOP_SSL_DOMAIN}  →  ${verification_id}   (if registrar requires fqdn)"
  echo ""
  echo "Keep explicit records for api / mcp / www so they are not covered by the wildcard."
  echo ""
}

bind_hostname() {
  require_az
  echo "→ Binding hostname ${WILDCARD_HOST} on ${WEBAPP_SHOP_NAME}..."
  if az webapp config hostname list \
      --webapp-name "$WEBAPP_SHOP_NAME" \
      --resource-group "$WEBAPP_RESOURCE_GROUP" \
      --query "[?name=='${WILDCARD_HOST}'] | [0].name" -o tsv | grep -q .; then
    echo "  Hostname already bound."
  else
    az webapp config hostname add \
      --webapp-name "$WEBAPP_SHOP_NAME" \
      --resource-group "$WEBAPP_RESOURCE_GROUP" \
      --hostname "$WILDCARD_HOST"
  fi
}

generate_pfx() {
  command -v openssl >/dev/null 2>&1 || { echo "Error: openssl is required."; exit 1; }
  if [ -z "${SHOP_SSL_PFX_PASSWORD:-}" ]; then
    echo "Error: SHOP_SSL_PFX_PASSWORD is required to generate a PFX."
    echo "  Example: make ssl-shop-renew SHOP_SSL_PFX_PASSWORD='...'"
    exit 1
  fi

  mkdir -p "$(dirname "$SHOP_SSL_PFX")"
  local work key crt conf
  work="$(mktemp -d)"
  key="$work/key.pem"
  crt="$work/cert.pem"
  conf="$work/openssl.cnf"

  cat >"$conf" <<EOF
[req]
default_bits = 2048
prompt = no
default_md = sha256
distinguished_name = dn
x509_extensions = v3_req

[dn]
CN = ${WILDCARD_HOST}
O = Kutria Commerce Shop

[v3_req]
basicConstraints = CA:FALSE
keyUsage = digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = ${WILDCARD_HOST}
DNS.2 = ${SHOP_SSL_DOMAIN}
EOF

  echo "→ Generating ${SHOP_SSL_DAYS}-day wildcard PFX at ${SHOP_SSL_PFX}"
  echo "  Note: openssl self-signed is for pipeline validation; replace with a CA-signed 1y wildcard for production browsers."
  openssl req -x509 -newkey rsa:2048 -nodes \
    -keyout "$key" -out "$crt" \
    -days "$SHOP_SSL_DAYS" \
    -config "$conf" \
    >/dev/null 2>&1
  openssl pkcs12 -export \
    -out "$SHOP_SSL_PFX" \
    -inkey "$key" \
    -in "$crt" \
    -passout "pass:${SHOP_SSL_PFX_PASSWORD}"
  rm -rf "$work"
  echo "  Created ${SHOP_SSL_PFX}"
}

upload_and_bind() {
  require_az
  if [ -z "${SHOP_SSL_PFX_PASSWORD:-}" ]; then
    echo "Error: SHOP_SSL_PFX_PASSWORD is required to upload."
    exit 1
  fi
  if [ ! -f "$SHOP_SSL_PFX" ]; then
    echo "Error: PFX not found: ${SHOP_SSL_PFX}"
    echo "  Run generate first, or set SHOP_SSL_PFX to a CA-signed wildcard PFX."
    exit 1
  fi

  bind_hostname

  echo "→ Uploading certificate to ${WEBAPP_SHOP_NAME}..."
  local thumbprint
  thumbprint="$(az webapp config ssl upload \
    --name "$WEBAPP_SHOP_NAME" \
    --resource-group "$WEBAPP_RESOURCE_GROUP" \
    --certificate-file "$SHOP_SSL_PFX" \
    --certificate-password "$SHOP_SSL_PFX_PASSWORD" \
    --query thumbprint -o tsv)"

  if [ -z "$thumbprint" ]; then
    echo "Error: upload did not return a thumbprint."
    exit 1
  fi
  echo "  Thumbprint: ${thumbprint}"

  echo "→ Binding SNI SSL for ${WILDCARD_HOST}..."
  az webapp config ssl bind \
    --name "$WEBAPP_SHOP_NAME" \
    --resource-group "$WEBAPP_RESOURCE_GROUP" \
    --certificate-thumbprint "$thumbprint" \
    --ssl-type SNI

  echo "SSL ready for https://<slug>.${SHOP_SSL_DOMAIN}"
}

case "$cmd" in
  dns)
    print_dns
    bind_hostname
    ;;
  generate)
    generate_pfx
    ;;
  upload)
    upload_and_bind
    ;;
  renew)
    if [ "${SKIP_GENERATE:-}" != "1" ]; then
      generate_pfx
    else
      echo "→ SKIP_GENERATE=1 — using existing ${SHOP_SSL_PFX}"
    fi
    upload_and_bind
    ;;
  *)
    echo "Usage: $0 {dns|generate|upload|renew}"
    exit 1
    ;;
esac

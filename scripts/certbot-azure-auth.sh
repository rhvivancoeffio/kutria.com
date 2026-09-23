#!/usr/bin/env bash
# Certbot DNS-01 auth hook for Azure DNS (kutria.com).
set -euo pipefail

RG="${AZURE_DNS_RG:-gravity-agentic}"
ZONE="${AZURE_DNS_ZONE:-kutria.com}"
# CERTBOT_DOMAIN is e.g. kutria.com or *.kutria.com → always _acme-challenge at zone root
RECORD="_acme-challenge"

az network dns record-set txt create -g "$RG" -z "$ZONE" -n "$RECORD" --ttl 60 >/dev/null 2>&1 || true
# Avoid duplicate values on renew/retry
az network dns record-set txt remove-record -g "$RG" -z "$ZONE" -n "$RECORD" -v "$CERTBOT_VALIDATION" --keep-empty-record-set >/dev/null 2>&1 || true
az network dns record-set txt add-record -g "$RG" -z "$ZONE" -n "$RECORD" -v "$CERTBOT_VALIDATION" >/dev/null

echo "Added TXT ${RECORD}.${ZONE}=${CERTBOT_VALIDATION}"
# Wait for propagation
sleep 45

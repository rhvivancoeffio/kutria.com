#!/usr/bin/env bash
# Certbot DNS-01 cleanup hook for Azure DNS (kutria.com).
set -euo pipefail

RG="${AZURE_DNS_RG:-gravity-agentic}"
ZONE="${AZURE_DNS_ZONE:-kutria.com}"
RECORD="_acme-challenge"

az network dns record-set txt remove-record \
  -g "$RG" -z "$ZONE" -n "$RECORD" \
  -v "$CERTBOT_VALIDATION" \
  --keep-empty-record-set >/dev/null 2>&1 || true

echo "Removed TXT ${RECORD}.${ZONE}=${CERTBOT_VALIDATION}"

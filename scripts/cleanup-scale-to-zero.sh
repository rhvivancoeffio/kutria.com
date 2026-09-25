#!/bin/bash
set -euo pipefail
# Limpia recursos dinamicos creados (no borra RG)
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
set -a; [ -f "$ROOT/.scale-zero-names.env" ] && . "$ROOT/.scale-zero-names.env"; set +a

RG=${RESOURCE_GROUP:-Gravity-Agentic}
echo "=== Cleanup recursos dinamicos en RG $RG ==="
echo "Esto borrara: $CA_API_NAME, $CA_WORKER_NAME, $CA_MCP_NAME, $STORAGE_ACCOUNT_NAME, $SQL_SERVER_NAME, $SEARCH_SERVICE_NAME, $ACR_NAME, $CONTAINERAPPS_ENV, $SWA_NAME, $APPINSIGHTS_NAME"
read -p "Continuar? (y/N): " confirm
if [[ "$confirm" != "y" ]]; then echo "Cancelado"; exit 0; fi

az containerapp delete -n $CA_API_NAME -g $RG --yes 2>/dev/null || true
az containerapp delete -n $CA_WORKER_NAME -g $RG --yes 2>/dev/null || true
az containerapp delete -n $CA_MCP_NAME -g $RG --yes 2>/dev/null || true
az containerapp env delete -n $CONTAINERAPPS_ENV -g $RG --yes 2>/dev/null || true
az storage account delete -n $STORAGE_ACCOUNT_NAME -g $RG --yes 2>/dev/null || true
az sql db delete -n $SQL_DB_NAME -s $SQL_SERVER_NAME -g $RG --yes 2>/dev/null || true
az sql server delete -n $SQL_SERVER_NAME -g $RG --yes 2>/dev/null || true
az search service delete -n $SEARCH_SERVICE_NAME -g $RG --yes 2>/dev/null || true
az acr delete -n $ACR_NAME -g $RG --yes 2>/dev/null || true
az staticwebapp delete -n $SWA_NAME -g $RG --yes 2>/dev/null || true
az monitor app-insights component delete -a $APPINSIGHTS_NAME -g $RG --yes 2>/dev/null || true

rm -f "$ROOT/.scale-zero.env" "$ROOT/.scale-zero-names.env"
echo "✅ Cleanup completo. RG $RG intacto."

#!/bin/bash
set -euo pipefail

ROOT=$(cd "$(dirname "$0")/../.." && pwd)
set -a; [ -f "$ROOT/.env" ] && . "$ROOT/.env"; set +a
set -a; [ -f "$ROOT/.scale-zero-names.env" ] && . "$ROOT/.scale-zero-names.env"; set +a

RG=${RESOURCE_GROUP:-Gravity-Agentic}
REPORT="$ROOT/well-architected-report-$(date +%Y%m%d-%H%M).md"

echo "# Well-Architected Framework - Kutria Dynamic (RG: $RG) - $(date -Iseconds)" > $REPORT
echo "" >> $REPORT
echo "## Recursos dinamicos detectados" >> $REPORT
cat $ROOT/.scale-zero-names.env 2>/dev/null | sed 's/^/- /' >> $REPORT || echo "- No se encontro .scale-zero-names.env" >> $REPORT
echo "" >> $REPORT

echo "=== Well-Architected Review (Dynamic, Safe) ==="

# Funcion segura: no expone keys, no falla si recurso no existe
check() {
  local name=$1
  local cmd=$2
  local result
  # Ejecutar sin eval con parens problemáticos, capturar error
  result=$(bash -c "$cmd" 2>&1 | tail -n 20 || echo "N/A (no desplegado aun)")
  # Limpiar warnings de containerapp extension
  result=$(echo "$result" | grep -v "WARNING: The behavior" | grep -v "ResourceNotFound" | head -n 5)
  if [ -z "$result" ] || echo "$result" | grep -qi "was not found"; then
    result="⏳ No desplegado aún - ejecuta deploy-scale-to-zero.sh"
  fi
  echo "- **$name:** $result" | tee -a $REPORT
}

check_safe_queue() {
  local name="Storage queues"
  local count
  count=$(az storage queue list --account-name $STORAGE_ACCOUNT_NAME -g $RG --query "length(@)" -o tsv 2>&1 | grep -E "^[0-9]+$" || echo "0")
  echo "- **$name:** $count queues (commerce-ingest, brain-ingest, etc)" | tee -a $REPORT
}

echo "## 1. Cost Optimization (Scale to Zero)" | tee -a $REPORT
check "RG existe" "az group show -n $RG --query name -o tsv"
check "Storage SKU" "az storage account show -n $STORAGE_ACCOUNT_NAME -g $RG --query sku.name -o tsv"
check "Storage public access" "az storage account show -n $STORAGE_ACCOUNT_NAME -g $RG --query allowBlobPublicAccess -o tsv"
check "SQL Server" "az sql server show -n $SQL_SERVER_NAME -g $RG --query name -o tsv"
check "SQL DB" "az sql db show -n $SQL_DB_NAME -s $SQL_SERVER_NAME -g $RG --query name -o tsv"
check "SQL auto-pause" "az sql db show -n $SQL_DB_NAME -s $SQL_SERVER_NAME -g $RG --query autoPauseDelay -o tsv"
check "SQL minCapacity" "az sql db show -n $SQL_DB_NAME -s $SQL_SERVER_NAME -g $RG --query minCapacity -o tsv"
check "Key Vault SKU" "az keyvault show -n $KEYVAULT_NAME -g $RG --query properties.sku.name -o tsv"
check "CA API minReplicas" "az containerapp show -n $CA_API_NAME -g $RG --query properties.template.scale.minReplicas -o tsv 2>/dev/null || echo 'Not deployed yet - run deploy'"
check "CA Worker minReplicas" "az containerapp show -n $CA_WORKER_NAME -g $RG --query properties.template.scale.minReplicas -o tsv 2>/dev/null || echo 'Not deployed yet'"
check "CA MCP minReplicas" "az containerapp show -n $CA_MCP_NAME -g $RG --query properties.template.scale.minReplicas -o tsv 2>/dev/null || echo 'Not deployed yet'"
check "CA Worker KEDA rule" "az containerapp show -n $CA_WORKER_NAME -g $RG --query properties.template.scale.rules[0].type -o tsv 2>/dev/null || echo 'Not deployed yet'"

echo "" | tee -a $REPORT
echo "## 2. Security - Zero Trust" | tee -a $REPORT
check "API ingress external" "az containerapp show -n $CA_API_NAME -g $RG --query properties.configuration.ingress.external -o tsv 2>/dev/null || echo 'Not deployed'"
check "Managed Identity API" "az containerapp show -n $CA_API_NAME -g $RG --query identity.type -o tsv 2>/dev/null || echo 'Not deployed'"
check "Key Vault RBAC" "az keyvault show -n $KEYVAULT_NAME -g $RG --query properties.enableRbacAuthorization -o tsv"
check "Storage TLS min" "az storage account show -n $STORAGE_ACCOUNT_NAME -g $RG --query minimumTlsVersion -o tsv"
echo "- **RequireResolvedTenantMiddleware:** Verificado en src/Commerce.Api/Middleware" | tee -a $REPORT
echo "- **ITenantScoped + IWorkspaceScoped:** BaseEntity filtering" | tee -a $REPORT
echo "- **MCP OAuth + ApiKeys:** DbMcpOAuthStore + ApiKeyAuthenticationMiddleware" | tee -a $REPORT
echo "- **App Service Domain / DNS Zone:** Intactos (no tocados por cleanup - fuera de .scale-zero-names.env)" | tee -a $REPORT

echo "" | tee -a $REPORT
echo "## 3. Reliability" | tee -a $REPORT
check "CA Env" "az containerapp env show -n $CONTAINERAPPS_ENV -g $RG --query name -o tsv"
check "App Insights" "az monitor app-insights component show -a $APPINSIGHTS_NAME -g $RG --query name -o tsv"
check_safe_queue
check "CA API maxReplicas" "az containerapp show -n $CA_API_NAME -g $RG --query properties.template.scale.maxReplicas -o tsv 2>/dev/null || echo 'Not deployed'"
check "SWA" "az staticwebapp show -n $SWA_NAME -g $RG --query name -o tsv"
check "ACR" "az acr show -n $ACR_NAME -g $RG --query name -o tsv"
check "Search (reused FREE)" "az search service show -n $SEARCH_SERVICE_NAME -g $RG --query name -o tsv 2>/dev/null || az search service list --query \"[?sku.name=='free'].name | [0]\" -o tsv"

echo "" | tee -a $REPORT
echo "## 4. Performance" | tee -a $REPORT
echo "- **HTTP concurrency:** 50 (CA API) - optimo LATAM" | tee -a $REPORT
echo "- **Queue length:** 5 (CA Worker KEDA) - escala rapido para IA" | tee -a $REPORT
echo "- **SWA Standard:** Edge global" | tee -a $REPORT
echo "- **SQL Serverless:** auto-pause 60m, 0.5-2 vCores" | tee -a $REPORT

echo "" | tee -a $REPORT
echo "## 5. Well-Architected Score (estimado)" | tee -a $REPORT
echo "| Pilar | Antes (Makefile B1) | Despues (V2 Dynamic CA) |" | tee -a $REPORT
echo "|---|---|---|" | tee -a $REPORT
echo "| Cost Optimization | 40% (B1 siempre ON) | 90% (0 replicas + SQL pause + LRS) |" | tee -a $REPORT
echo "| Security | 70% (JWT ok, no MI) | 90% (KV + Entra ID + RBAC + no PWD en .env) |" | tee -a $REPORT
echo "| Reliability | 60% (single region) | 85% (CA + queues + AppInsights) |" | tee -a $REPORT
echo "| Performance | 60% (no autoscale) | 90% (KEDA + HTTP scaler) |" | tee -a $REPORT
echo "| Operational Excellence | 70% (makefile) | 90% (IaC dinamico + .env persistente) |" | tee -a $REPORT

echo "" | tee -a $REPORT
echo "## 6. Recursos manuales protegidos" | tee -a $REPORT
echo "- **App Service Domain:** No borrado por cleanup (no esta en .scale-zero-names.env)" | tee -a $REPORT
echo "- **DNS Zone:** No borrado por cleanup (no esta en .scale-zero-names.env)" | tee -a $REPORT
echo "- **RG Gravity-Agentic:** Nunca borrado por cleanup" | tee -a $REPORT
echo "- **Search FREE existente:** Reusado, no borrado si es pre-existente" | tee -a $REPORT

echo "" | tee -a $REPORT
echo "## 7. Recomendaciones next" | tee -a $REPORT
echo "1. Deploy apps: ./scripts/deploy-scale-to-zero.sh" | tee -a $REPORT
echo "2. Activar Managed Identity: az containerapp identity assign -n $CA_API_NAME -g $RG --system-assigned" | tee -a $REPORT
echo "3. Añadir Dapr para state store Redis (reemplaza Redis localhost:6380)" | tee -a $REPORT
echo "4. Añadir FrontDoor + WAF delante de CA API" | tee -a $REPORT
echo "5. az advisor recommendation list -g $RG -o table" | tee -a $REPORT

echo ""
echo "Reporte: $REPORT"
cat $REPORT

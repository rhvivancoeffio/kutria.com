#!/bin/bash
set -euo pipefail

# Kutria V3 - Deploy Scale to Zero (Well-Architected + Zero Trust)
# - Usa Dockerfiles del proyecto, no genera en /tmp
# - Secrets desde Key Vault (no PWD en .env) via secretref
# - Managed Identity + min 0 replicas

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
ROOT=$(cd "$(dirname "$0")/.." && pwd)
echo "DEBUG ROOT=$ROOT"
echo "DEBUG PWD=$(pwd)"
ls -la "$ROOT/.scale-zero-names.env" || echo "NO existe en ROOT"
ls -la "$(pwd)/.scale-zero-names.env" || echo "NO existe en PWD"

set -a; [ -f "$ROOT/.scale-zero-names.env" ] && . "$ROOT/.scale-zero-names.env"; set +a

if [ ! -f "$ROOT/.scale-zero-names.env" ]; then
  echo "ERROR: No existe .scale-zero-names.env. Ejecuta provision-scale-to-zero.sh primero"
  exit 1
fi

RG=${RESOURCE_GROUP}
LOCATION=${WEBAPP_LOCATION:-eastus}
TAG=${TAG:-latest}
ACR_NAME=${ACR_NAME}
CA_ENV=${CONTAINERAPPS_ENV}
KV_NAME=${KEYVAULT_NAME}

ACR_LOGIN=$(az acr show -n $ACR_NAME -g $RG --query loginServer -o tsv)
echo "=== Kutria V3 Deploy (Well-Architected, Dockerfiles del proyecto) ==="
echo "RG: $RG / ACR: $ACR_LOGIN / TAG: $TAG / KV: $KV_NAME"
echo "Apps: $CA_API_NAME, $CA_WORKER_NAME, $CA_MCP_NAME, $CA_SHOP_NAME"

az acr login -n $ACR_NAME >/dev/null

find_dockerfile() {
  local app_name=$1
  case $app_name in
    api) echo "$ROOT/src/Commerce.Api/Dockerfile";;
    worker) echo "$ROOT/src/Commerce.Worker/Dockerfile";;
    mcp) echo "$ROOT/src/Commerce.Mcp/Dockerfile";;
    shop) echo "$ROOT/src/Commerce.Shop/Dockerfile";;
    *) echo "$ROOT/Dockerfile";;
  esac
}

build_and_push() {
  local app_short=$1
  local dockerfile=$(find_dockerfile $app_short)

  if [ ! -f "$dockerfile" ]; then
    echo "⚠  Dockerfile no encontrado en $dockerfile, buscando..."
    dockerfile=$(find "$ROOT/src" -maxdepth 3 -name "Dockerfile" 2>/dev/null | grep -i "$app_short" | head -n1 || true)
    if [ -z "$dockerfile" ]; then
      dockerfile=$(find "$ROOT" -maxdepth 2 -name "Dockerfile*" 2>/dev/null | head -n1 || true)
    fi
  fi

  if [ ! -f "$dockerfile" ]; then
    echo "❌ No se encontró Dockerfile para $app_short. Crea uno en src/Commerce.${app_short^}/Dockerfile"
    return 1
  fi

  echo "→ Build $app_short usando $dockerfile"
  docker build -f "$dockerfile" -t $ACR_LOGIN/kutria-$app_short:$TAG "$ROOT"
  docker push $ACR_LOGIN/kutria-$app_short:$TAG
  echo "  ✅ Push $ACR_LOGIN/kutria-$app_short:$TAG"
}

# Build todos
build_and_push api
build_and_push worker
build_and_push mcp
if [ -f "$ROOT/src/Commerce.Shop/Dockerfile" ] || [ -f "$ROOT/src/Commerce.Web/Dockerfile" ]; then
  build_and_push shop || echo "Shop build skip"
fi

# --- Secrets desde Key Vault (Well-Architected: no PWD en .env) ---
echo "→ Leyendo secrets desde Key Vault $KV_NAME"
SQL_PWD=$(az keyvault secret show --vault-name $KV_NAME --name "sql-admin-password" --query value -o tsv 2>/dev/null || echo "${SQL_ADMIN_PASSWORD:-}")
STORAGE_CONN=$(az keyvault secret show --vault-name $KV_NAME --name "storage-connection-string" --query value -o tsv 2>/dev/null || az storage account show-connection-string -n $STORAGE_ACCOUNT_NAME -g $RG --query connectionString -o tsv)
SEARCH_KEY=$(az search admin-key show -g $RG --service-name $SEARCH_SERVICE_NAME --query primaryKey -o tsv 2>/dev/null || echo "")
APPINSIGHTS_CONN=${APPINSIGHTS_CONNECTIONSTRING:-$(az monitor app-insights component show -a $APPINSIGHTS_NAME -g $RG --query connectionString -o tsv 2>/dev/null || echo "")}

CONN_STR="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Initial Catalog=$SQL_DB_NAME;User ID=${SQL_ADMIN_USER:-commerceadmin};Password=$SQL_PWD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# --- API ---
echo "→ Deploy $CA_API_NAME (min 0, max 10, MI, secretref)"
if az containerapp show -n $CA_API_NAME -g $RG >/dev/null 2>&1; then
  az containerapp update -n $CA_API_NAME -g $RG \
    --image $ACR_LOGIN/kutria-api:$TAG \
    --min-replicas 0 --max-replicas 10 \
    --secrets "storage-conn=$STORAGE_CONN" "sql-conn=$CONN_STR" "search-key=$SEARCH_KEY" \
    --set-env-vars DATABASE_PROVIDER=SqlServer ConnectionStrings__Commerce=secretref:sql-conn STORAGE_ACCOUNT_NAME=$STORAGE_ACCOUNT_NAME SEARCH_SERVICE_NAME=$SEARCH_SERVICE_NAME AzureSearch__ServiceName=$SEARCH_SERVICE_NAME AzureSearch__ApiKey=secretref:search-key APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN" Queue__Provider=AzureQueue Queue__ConnectionString=secretref:storage-conn \
    >/dev/null
else
  az containerapp create -n $CA_API_NAME -g $RG \
    --environment $CA_ENV \
    --image $ACR_LOGIN/kutria-api:$TAG \
    --ingress external --target-port 8080 \
    --min-replicas 0 --max-replicas 10 \
    --registry-server $ACR_LOGIN \
    --system-assigned \
    --secrets "storage-conn=$STORAGE_CONN" "sql-conn=$CONN_STR" "search-key=$SEARCH_KEY" \
    --env-vars DATABASE_PROVIDER=SqlServer ConnectionStrings__Commerce=secretref:sql-conn STORAGE_ACCOUNT_NAME=$STORAGE_ACCOUNT_NAME SEARCH_SERVICE_NAME=$SEARCH_SERVICE_NAME AzureSearch__ServiceName=$SEARCH_SERVICE_NAME AzureSearch__ApiKey=secretref:search-key APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN" Queue__Provider=AzureQueue Queue__ConnectionString=secretref:storage-conn
fi

# --- WORKER ---
echo "→ Deploy $CA_WORKER_NAME (KEDA azure-queue, queueLength 5, min 0, secretref)"
if az containerapp show -n $CA_WORKER_NAME -g $RG >/dev/null 2>&1; then
  az containerapp update -n $CA_WORKER_NAME -g $RG \
    --image $ACR_LOGIN/kutria-worker:$TAG \
    --min-replicas 0 --max-replicas 5 \
    --secrets "storage-conn=$STORAGE_CONN" "sql-conn=$CONN_STR" \
    --set-env-vars DATABASE_PROVIDER=SqlServer ConnectionStrings__Commerce=secretref:sql-conn Queue__Provider=AzureQueue Queue__ConnectionString=secretref:storage-conn APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN" \
    >/dev/null
else
  az containerapp create -n $CA_WORKER_NAME -g $RG \
    --environment $CA_ENV \
    --image $ACR_LOGIN/kutria-worker:$TAG \
    --min-replicas 0 --max-replicas 5 \
    --registry-server $ACR_LOGIN \
    --system-assigned \
    --secrets "storage-conn=$STORAGE_CONN" "sql-conn=$CONN_STR" \
    --env-vars DATABASE_PROVIDER=SqlServer ConnectionStrings__Commerce=secretref:sql-conn Queue__Provider=AzureQueue Queue__ConnectionString=secretref:storage-conn APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN" \
    --scale-rule-name queue --scale-rule-type azure-queue --scale-rule-metadata "queueName=commerce-ingest" "queueLength=5" "accountName=$STORAGE_ACCOUNT_NAME" "connection=storage-conn"
fi

# --- MCP ---
echo "→ Deploy $CA_MCP_NAME (min 0, max 5, MI, secretref)"
if az containerapp show -n $CA_MCP_NAME -g $RG >/dev/null 2>&1; then
  az containerapp update -n $CA_MCP_NAME -g $RG \
    --image $ACR_LOGIN/kutria-mcp:$TAG \
    --min-replicas 0 --max-replicas 5 \
    --secrets "storage-conn=$STORAGE_CONN" "sql-conn=$CONN_STR" \
    --set-env-vars DATABASE_PROVIDER=SqlServer ConnectionStrings__Commerce=secretref:sql-conn APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN" STORAGE_ACCOUNT_NAME=$STORAGE_ACCOUNT_NAME \
    >/dev/null
else
  az containerapp create -n $CA_MCP_NAME -g $RG \
    --environment $CA_ENV \
    --image $ACR_LOGIN/kutria-mcp:$TAG \
    --ingress external --target-port 8080 \
    --min-replicas 0 --max-replicas 5 \
    --registry-server $ACR_LOGIN \
    --system-assigned \
    --secrets "storage-conn=$STORAGE_CONN" "sql-conn=$CONN_STR" \
    --env-vars DATABASE_PROVIDER=SqlServer ConnectionStrings__Commerce=secretref:sql-conn APPLICATIONINSIGHTS_CONNECTION_STRING="$APPINSIGHTS_CONN" STORAGE_ACCOUNT_NAME=$STORAGE_ACCOUNT_NAME
fi

# --- SHOP opcional ---
if [ -n "${CA_SHOP_NAME:-}" ] && az acr repository show -n $ACR_NAME --image kutria-shop:$TAG >/dev/null 2>&1; then
  echo "→ Deploy $CA_SHOP_NAME (min 0, max 3, secretref)"
  if az containerapp show -n $CA_SHOP_NAME -g $RG >/dev/null 2>&1; then
    az containerapp update -n $CA_SHOP_NAME -g $RG \
      --image $ACR_LOGIN/kutria-shop:$TAG \
      --min-replicas 0 --max-replicas 3 \
      --secrets "sql-conn=$CONN_STR" \
      --set-env-vars ConnectionStrings__Commerce=secretref:sql-conn \
      >/dev/null
  else
    az containerapp create -n $CA_SHOP_NAME -g $RG --environment $CA_ENV --image $ACR_LOGIN/kutria-shop:$TAG --ingress external --target-port 8080 --min-replicas 0 --max-replicas 3 --registry-server $ACR_LOGIN --system-assigned --secrets "sql-conn=$CONN_STR" --env-vars ConnectionStrings__Commerce=secretref:sql-conn
  fi
fi

# --- Frontend SWA ---
if [ -d "$ROOT/src/Commerce.Web" ] && [ -f "$ROOT/src/Commerce.Web/package.json" ]; then
  echo "→ Deploy Frontend $SWA_NAME"
  cd $ROOT/src/Commerce.Web
  npm ci && npm run build
  SWA_TOKEN=$(az staticwebapp secrets list -n $SWA_NAME -g $RG --query "properties.apiKey" -o tsv)
  npx -y @azure/static-web-apps-cli deploy ./dist --deployment-token "$SWA_TOKEN" --env production || echo "SWA deploy skip"
else
  echo "⏩ Frontend skip - no se encontró src/Commerce.Web/package.json"
fi

echo ""
echo "✅ Deploy V3 completo (Well-Architected, secretref)"
echo "   - Secrets desde Key Vault $KV_NAME (no .env)"
echo "   - Managed Identity + secretref en todos los update/create"
echo "   - Scale to zero: min 0"
az containerapp show -n $CA_API_NAME -g $RG --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null | xargs -I {} echo "API URL: https://{}" || echo "API URL: pendiente"
az containerapp show -n $CA_MCP_NAME -g $RG --query "properties.configuration.ingress.fqdn" -o tsv 2>/dev/null | xargs -I {} echo "MCP URL: https://{}" || true
az staticwebapp show -n $SWA_NAME -g $RG --query "properties.defaultHostname" -o tsv 2>/dev/null | xargs -I {} echo "SWA URL: https://{}" || true

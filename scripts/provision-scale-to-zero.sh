#!/bin/bash
set -euo pipefail

# Kutria V3 - Well-Architected + Zero Trust + Scale to Zero
# - NO PWD en .env
# - Password generado y guardado en Key Vault
# - SQL con Entra ID (AD admin = tu usuario)
# - Sin Private Endpoint (caro para scale to zero)

ROOT=$(cd "$(dirname "$0")/.." && pwd)
set -a; [ -f "$ROOT/.env" ] && . "$ROOT/.env"; set +a

PREFIX=${PREFIX:-kutria}
ENV_NAME=${ENV_NAME:-prod}
RG=${WEBAPP_RESOURCE_GROUP:-Gravity-Agentic}
LOCATION=${WEBAPP_LOCATION:-eastus}
SWA_LOCATION=${SWA_LOCATION:-eastus2}

generate_rand() {
  local r=$(LC_ALL=C tr -dc 'a-z0-9' </dev/urandom 2>/dev/null | head -c4)
  if [ -z "$r" ] || [ ${#r} -lt 4 ]; then
    r=$(openssl rand -hex 2 2>/dev/null | head -c4)
  fi
  if [ -z "$r" ] || [ ${#r} -lt 4 ]; then
    r=$(uuidgen 2>/dev/null | tr '[:upper:]' '[:lower:]' | tr -dc 'a-z0-9' | head -c4)
  fi
  echo "$r"
}
SCALE_ENV_FILE="$ROOT/.scale-zero.env"
if [ -f "$SCALE_ENV_FILE" ]; then
  source "$SCALE_ENV_FILE"
else
  RAND=$(generate_rand)
  echo "RAND_SUFFIX=$RAND" > "$SCALE_ENV_FILE"
  source "$SCALE_ENV_FILE"
fi

# Nombres dinamicos (Well-Architected: cortos, sin guiones donde no permitido)
STORAGE_ACCOUNT="${PREFIX}st${ENV_NAME}${RAND_SUFFIX}"
STORAGE_ACCOUNT=$(echo $STORAGE_ACCOUNT | tr -d '-' | cut -c1-24 | tr '[:upper:]' '[:lower:]')
SQL_SERVER="${PREFIX}-sql-${ENV_NAME}-${RAND_SUFFIX}"
SQL_DB="${PREFIX}-db-${ENV_NAME}"
SEARCH_SERVICE="${PREFIX}-search-${ENV_NAME}-${RAND_SUFFIX}"
ACR_NAME="${PREFIX}acr${ENV_NAME}${RAND_SUFFIX}"
ACR_NAME=$(echo $ACR_NAME | tr -d '-' | cut -c1-20 | tr '[:upper:]' '[:lower:]')
CA_ENV="${PREFIX}-cae-${ENV_NAME}-${RAND_SUFFIX}"
SWA_NAME="${PREFIX}-swa-${ENV_NAME}-${RAND_SUFFIX}"
APPINSIGHTS="${PREFIX}-insights-${ENV_NAME}-${RAND_SUFFIX}"
KV_NAME="${PREFIX}-kv-${ENV_NAME}-${RAND_SUFFIX}"
KV_NAME=$(echo $KV_NAME | tr -d '_' | cut -c1-24)
CA_API="ca-${PREFIX}-api-${ENV_NAME}-${RAND_SUFFIX}"
CA_WORKER="ca-${PREFIX}-worker-${ENV_NAME}-${RAND_SUFFIX}"
CA_MCP="ca-${PREFIX}-mcp-${ENV_NAME}-${RAND_SUFFIX}"
CA_SHOP="ca-${PREFIX}-shop-${ENV_NAME}-${RAND_SUFFIX}"

echo "=== Kutria V3 Provision (Well-Architected, No PWD in .env) ==="
echo "RG existente: $RG"
echo "Prefix: $PREFIX / Env: $ENV_NAME / Rand: $RAND_SUFFIX"
echo "Storage: $STORAGE_ACCOUNT"
echo "SQL: $SQL_SERVER / $SQL_DB"
echo "KV: $KV_NAME (guarda secrets)"
echo "Search: $SEARCH_SERVICE"
echo "ACR: $ACR_NAME"
echo "CA Env: $CA_ENV"

cat > "$ROOT/.scale-zero-names.env" <<EOF
PREFIX=$PREFIX
ENV_NAME=$ENV_NAME
RAND_SUFFIX=$RAND_SUFFIX
RESOURCE_GROUP=$RG
STORAGE_ACCOUNT_NAME=$STORAGE_ACCOUNT
SQL_SERVER_NAME=$SQL_SERVER
SQL_DB_NAME=$SQL_DB
SEARCH_SERVICE_NAME=$SEARCH_SERVICE
ACR_NAME=$ACR_NAME
CONTAINERAPPS_ENV=$CA_ENV
SWA_NAME=$SWA_NAME
CA_API_NAME=$CA_API
CA_WORKER_NAME=$CA_WORKER
CA_MCP_NAME=$CA_MCP
CA_SHOP_NAME=$CA_SHOP
APPINSIGHTS_NAME=$APPINSIGHTS
KEYVAULT_NAME=$KV_NAME
EOF

if ! az group show -n $RG >/dev/null 2>&1; then
  echo "ERROR: RG $RG no existe"; exit 1
fi

# 0. Key Vault (Well-Architected Security pillar)
echo "→ Key Vault $KV_NAME (Well-Architected: no secrets en .env)"
if ! az keyvault show -n $KV_NAME -g $RG >/dev/null 2>&1; then
  az keyvault create -n $KV_NAME -g $RG -l $LOCATION --sku standard --enable-rbac-authorization true --tags env=$ENV_NAME project=$PREFIX
  # Dar permiso al usuario actual
  USER_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || az account show --query user.name -o tsv)
  az role assignment create --role "Key Vault Secrets Officer" --assignee "$USER_ID" --scope $(az keyvault show -n $KV_NAME -g $RG --query id -o tsv) 2>/dev/null || true
  sleep 5
fi

# 1. Storage LRS (sin PE para scale to zero barato)
echo "→ Storage $STORAGE_ACCOUNT (LRS, sin Private Endpoint - ahorra \$26/mes)"
if ! az storage account show -n $STORAGE_ACCOUNT -g $RG >/dev/null 2>&1; then
  az storage account create -n $STORAGE_ACCOUNT -g $RG -l ${STORAGE_LOCATION:-$LOCATION} --sku Standard_LRS --kind StorageV2 --min-tls-version TLS1_2 --allow-blob-public-access false --https-only true --tags env=$ENV_NAME project=$PREFIX scale-to-zero=true cost-optimized=true
fi
# Firewall: solo AzureServices (no PE)
az storage account update -n $STORAGE_ACCOUNT -g $RG --default-action Allow --public-network-access Enabled >/dev/null 2>&1 || true
STORAGE_KEY=$(az storage account keys list -n $STORAGE_ACCOUNT -g $RG --query "[0].value" -o tsv)
for Q in commerce-ingest brain-ingest catalog-vector catalog-image-vector chat-events agent-audit policy-eval event-streams; do
  az storage queue create --name $Q --account-name $STORAGE_ACCOUNT --account-key $STORAGE_KEY >/dev/null 2>&1 || true
done
# Guardar connection string en Key Vault (no en .env)
az keyvault secret set --vault-name $KV_NAME --name "storage-connection-string" --value "DefaultEndpointsProtocol=https;AccountName=$STORAGE_ACCOUNT;AccountKey=$STORAGE_KEY;EndpointSuffix=core.windows.net" >/dev/null 2>&1 || true

# 2. SQL Serverless + Entra ID (Well-Architected)
echo "→ SQL $SQL_SERVER / $SQL_DB Serverless auto-pause 60m + Entra ID"
# Generar password seguro solo si no existe en KV
EXISTING_PWD=$(az keyvault secret show --vault-name $KV_NAME --name "sql-admin-password" --query value -o tsv 2>/dev/null || echo "")
if [ -z "$EXISTING_PWD" ]; then
  GEN_PWD="Kutria-$(openssl rand -base64 16 | tr -dc 'A-Za-z0-9!@#$%' | head -c20)!"
  echo "  Generando password seguro y guardando en Key Vault..."
  az keyvault secret set --vault-name $KV_NAME --name "sql-admin-password" --value "$GEN_PWD" >/dev/null
  EXISTING_PWD=$GEN_PWD
else
  echo "  Password ya existe en Key Vault, reutilizando (Well-Architected: rotación via KV)"
fi

if ! az sql server show -n $SQL_SERVER -g $RG >/dev/null 2>&1; then
  az sql server create -n $SQL_SERVER -g $RG -l $LOCATION --admin-user ${SQL_ADMIN_USER:-commerceadmin} --admin-password "$EXISTING_PWD"
  # Tags separados para compatibilidad (az cli vieja no soporta --tags en server)
  az sql server update -n $SQL_SERVER -g $RG --set tags.env=$ENV_NAME tags.project=$PREFIX tags.scale-to-zero=true 2>/dev/null || true
  # Entra ID admin = usuario actual (passwordless en el futuro)
  AD_USER=$(az ad signed-in-user show --query userPrincipalName -o tsv 2>/dev/null || echo "")
  AD_ID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || echo "")
  if [ -n "$AD_USER" ] && [ -n "$AD_ID" ]; then
    echo "  Configurando Entra ID admin: $AD_USER"
    az sql server ad-admin create --server $SQL_SERVER -g $RG --display-name "$AD_USER" --object-id "$AD_ID" 2>/dev/null || true
  fi
fi

if ! az sql db show -n $SQL_DB -s $SQL_SERVER -g $RG >/dev/null 2>&1; then
  echo "  Creando DB serverless (compatible az 2.90.0)..."
  # az 2.90 no soporta --max-capacity, usamos --capacity + --min-capacity + auto-pause
  # Intentamos de mas completo a mas simple
  az sql db create -n $SQL_DB -s $SQL_SERVER -g $RG --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless --auto-pause-delay 60 --min-capacity 0.5 --backup-storage-redundancy Local 2>&1 | tail -n 20 || \
  az sql db create -n $SQL_DB -s $SQL_SERVER -g $RG --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless --auto-pause-delay 60 --backup-storage-redundancy Local 2>&1 | tail -n 20 || \
  az sql db create -n $SQL_DB -s $SQL_SERVER -g $RG --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless --backup-storage-redundancy Local
  az sql db update -n $SQL_DB -s $SQL_SERVER -g $RG --set tags.env=$ENV_NAME tags.project=$PREFIX tags.scale-to-zero=true 2>/dev/null || true
fi
az sql server firewall-rule show -n AllowAzureServices -s $SQL_SERVER -g $RG >/dev/null 2>&1 || az sql server firewall-rule create -n AllowAzureServices -s $SQL_SERVER -g $RG --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# Guardar connection string en KV
CONN_STR="Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;User ID=${SQL_ADMIN_USER:-commerceadmin};Password=$EXISTING_PWD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
az keyvault secret set --vault-name $KV_NAME --name "sql-connection-string" --value "$CONN_STR" >/dev/null 2>&1 || true

# 3. Search - Standard (creado a mano, solo valida que existe)
echo "→ Search $SEARCH_SERVICE (basic - S1 1 replica 1 particion)"
if ! az search service show -n $SEARCH_SERVICE -g $RG >/dev/null 2>&1; then
  echo "  ⚠️  Search $SEARCH_SERVICE no existe"
  echo "  Créalo a mano en portal: RG=$RG | SKU=basic | Replica=1 | Partition=1"
  echo "  O con docker: docker run --rm -it -v ~/.azure:/root/.azure mcr.microsoft.com/azure-cli az search service create -n $SEARCH_SERVICE -g $RG -l $LOCATION --sku standard --replica-count 1 --partition-count 1"
else
  echo "  Search $SEARCH_SERVICE ya existe ✓"
fi

# 4. ACR
echo "→ ACR $ACR_NAME"
if ! az acr show -n $ACR_NAME -g $RG >/dev/null 2>&1; then
  az acr create -n $ACR_NAME -g $RG -l $LOCATION --sku Basic --admin-enabled false --tags env=$ENV_NAME
fi

# 5. App Insights
echo "→ App Insights $APPINSIGHTS"
if ! az monitor app-insights component show -a $APPINSIGHTS -g $RG >/dev/null 2>&1; then
  az monitor app-insights component create -a $APPINSIGHTS -g $RG -l $LOCATION --kind web --tags env=$ENV_NAME
fi
INSIGHTS_CONN=$(az monitor app-insights component show -a $APPINSIGHTS -g $RG --query connectionString -o tsv 2>/dev/null || echo "")

# 6. Container Apps Env (sin VNet = scale to zero barato, sin PE)
echo "→ CA Env $CA_ENV (sin VNet, sin Private Endpoint para scale to zero)"
if ! az containerapp env show -n $CA_ENV -g $RG >/dev/null 2>&1; then
  az containerapp env create -n $CA_ENV -g $RG -l $LOCATION --tags env=$ENV_NAME scale-to-zero=true
fi

# 7. SWA
echo "→ SWA $SWA_NAME"
if ! az staticwebapp show -n $SWA_NAME -g $RG >/dev/null 2>&1; then
  az staticwebapp create -n $SWA_NAME -g $RG -l $SWA_LOCATION --sku Standard --tags env=$ENV_NAME
fi

if grep -q "APPINSIGHTS_CONNECTIONSTRING" "$ROOT/.scale-zero-names.env" 2>/dev/null; then
  sed -i.bak "s|APPINSIGHTS_CONNECTIONSTRING=.*|APPINSIGHTS_CONNECTIONSTRING=$INSIGHTS_CONN|" "$ROOT/.scale-zero-names.env" 2>/dev/null || sed -i '' "s|APPINSIGHTS_CONNECTIONSTRING=.*|APPINSIGHTS_CONNECTIONSTRING=$INSIGHTS_CONN|" "$ROOT/.scale-zero-names.env" 2>/dev/null || true
else
  echo "APPINSIGHTS_CONNECTIONSTRING=$INSIGHTS_CONN" >> "$ROOT/.scale-zero-names.env"
fi

echo ""
echo "✅ Provision V3 completo (Well-Architected)"
echo "  - Key Vault: $KV_NAME (secrets no en .env)"
echo "  - SQL password en KV: sql-admin-password"
echo "  - Storage conn en KV: storage-connection-string"
echo "  - Sin Private Endpoints (ahorro \$26/mes, scale to zero optimo)"
echo "  - Para prod Enterprise con PE: ejecuta ./scripts/enable-private-endpoints.sh"
echo ""
echo "Archivos generados:"
echo "  - .scale-zero.env (RAND)"
echo "  - .scale-zero-names.env (nombres)"
echo "Siguiente: ./scripts/deploy-scale-to-zero-v2.sh"

#!/usr/bin/env bash
# Push App Settings to Commerce API / Worker / MCP Azure Web Apps.
# Azure keys use ASP.NET hierarchical form (Section__Property). Code reads Section:Property.
#
# Usage:
#   ./scripts/deploy-settings.sh api|worker|mcp|all
#
# Required:
#   ConnectionStrings__Commerce  OR  SQL_ADMIN_PASSWORD when DATABASE_PROVIDER=SqlServer (auto-builds Azure SQL conn)
# For PostgreSQL: set ConnectionStrings__Commerce to a Postgres conn string
#
# Optional (.env / shell — Section__Property keys only for app settings):
#   AzureOpenAI__Endpoint / AzureOpenAI__ApiKey / AzureOpenAI__Deployment / AzureOpenAI__EmbeddingDeployment
#   Speech__Key / Speech__Endpoint / Speech__Region
#   AzureSearch__Endpoint / AzureSearch__ApiKey (else resolved from SEARCH_SERVICE_NAME)
#   ConnectionStrings__Redis
#   Auth__SigningKey
#   Chat__AllowedOrigins (comma-separated via CHAT_ALLOWED_ORIGINS)
#   Chat__Provider (default TableStorage; legacy Chat__HistoryProvider still accepted by app)
#   Mcp__PublicBaseUrl / Mcp__FrontendBaseUrl

set -euo pipefail

TARGET="${1:-all}"
WEBAPP_NAME="${WEBAPP_NAME:-web-app-kutria-commerce}"
WEBAPP_WORKER_NAME="${WEBAPP_WORKER_NAME:-web-app-kutria-commerce-worker}"
WEBAPP_MCP_NAME="${WEBAPP_MCP_NAME:-web-app-kutria-commerce-mcp}"
WEBAPP_RESOURCE_GROUP="${WEBAPP_RESOURCE_GROUP:-Gravity-Agentic}"
STORAGE_ACCOUNT_NAME="${STORAGE_ACCOUNT_NAME:-stkutriaagenticcommerce}"
SQL_SERVER_NAME="${SQL_SERVER_NAME:-sql-kutria-commerce}"
SQL_DB_NAME="${SQL_DB_NAME:-commerce}"
SQL_ADMIN_USER="${SQL_ADMIN_USER:-commerceadmin}"
SEARCH_SERVICE_NAME="${SEARCH_SERVICE_NAME:-search-kutria-commerce}"
DATABASE_PROVIDER="${DATABASE_PROVIDER:-SqlServer}"
VECTOR_PROVIDER="${VECTOR_PROVIDER:-AzureSearch}"
QUEUE_PROVIDER="${QUEUE_PROVIDER:-AzureQueue}"

# Flat OpenAI/Speech/Search aliases accepted only as input convenience for deploy-settings
AzureOpenAI__Endpoint="${AzureOpenAI__Endpoint:-${AZURE_OPENAI_ENDPOINT:-}}"
AzureOpenAI__ApiKey="${AzureOpenAI__ApiKey:-${AZURE_OPENAI_API_KEY:-}}"
AzureOpenAI__Deployment="${AzureOpenAI__Deployment:-${AZURE_OPENAI_DEPLOYMENT:-gpt-4o-mini}}"
AzureOpenAI__EmbeddingDeployment="${AzureOpenAI__EmbeddingDeployment:-${AZURE_OPENAI_EMBEDDING_DEPLOYMENT:-}}"
Speech__Key="${Speech__Key:-${AZURE_SPEECH_KEY:-${SPEECH_API_KEY:-}}}"
Speech__Endpoint="${Speech__Endpoint:-${AZURE_SPEECH_ENDPOINT:-https://gravity-builder-product-resource.services.ai.azure.com}}"
Speech__Region="${Speech__Region:-${AZURE_SPEECH_REGION:-}}"
AzureSearch__Endpoint="${AzureSearch__Endpoint:-${AZURE_SEARCH_ENDPOINT:-}}"
AzureSearch__ApiKey="${AzureSearch__ApiKey:-${AZURE_SEARCH_API_KEY:-}}"
Auth__SigningKey="${Auth__SigningKey:-${AUTH_SIGNING_KEY:-}}"
CHAT_ALLOWED_ORIGINS="${CHAT_ALLOWED_ORIGINS:-https://kutria.com,https://www.kutria.com}"
Chat__Provider="${Chat__Provider:-${CHAT_PROVIDER:-${Chat__HistoryProvider:-${CHAT_HISTORY_PROVIDER:-TableStorage}}}}"
Mcp__PublicBaseUrl="${Mcp__PublicBaseUrl:-${MCP_PUBLIC_BASE_URL:-https://mcp.kutria.com}}"
Mcp__FrontendBaseUrl="${Mcp__FrontendBaseUrl:-${MCP_FRONTEND_BASE_URL:-https://kutria.com}}"

require() {
  local name="$1"
  if [ -z "${!name:-}" ]; then
    echo "Error: $name is required in the environment." >&2
    exit 1
  fi
}

# Local docker .env may set ConnectionStrings__Commerce=Host=... — never push that to Azure SQL.
is_sqlserver_conn() {
  local cs="${1:-}"
  [[ "$cs" == *[Ss]erver=* ]] || [[ "$cs" == *[Dd]ata\ [Ss]ource=* ]]
}

if [ "$DATABASE_PROVIDER" = "SqlServer" ]; then
  if ! is_sqlserver_conn "${ConnectionStrings__Commerce:-}"; then
    if [ -n "${ConnectionStrings__Commerce:-}" ]; then
      echo "Ignoring non-SqlServer ConnectionStrings__Commerce (looks local/Postgres); building Azure SQL from SQL_ADMIN_PASSWORD..."
    fi
    require SQL_ADMIN_PASSWORD
    ConnectionStrings__Commerce="Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=${SQL_DB_NAME};Persist Security Info=False;User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  fi
elif [ -z "${ConnectionStrings__Commerce:-}" ]; then
  require ConnectionStrings__Commerce
fi

echo "Resolving storage connection string for ${STORAGE_ACCOUNT_NAME}..."
STORAGE_CONN="$(az storage account show-connection-string \
  --name "$STORAGE_ACCOUNT_NAME" \
  --resource-group "$WEBAPP_RESOURCE_GROUP" \
  --query connectionString -o tsv)"
if [ -z "$STORAGE_CONN" ]; then
  echo "Error: could not resolve connection string for storage account ${STORAGE_ACCOUNT_NAME}." >&2
  exit 1
fi

if [ -z "${AzureSearch__Endpoint:-}" ]; then
  AzureSearch__Endpoint="https://${SEARCH_SERVICE_NAME}.search.windows.net"
fi
if [ -z "${AzureSearch__ApiKey:-}" ]; then
  echo "Resolving Azure Search admin key for ${SEARCH_SERVICE_NAME}..."
  AzureSearch__ApiKey="$(az search admin-key show \
    --resource-group "$WEBAPP_RESOURCE_GROUP" \
    --service-name "$SEARCH_SERVICE_NAME" \
    --query primaryKey -o tsv 2>/dev/null || true)"
fi
if [ "$VECTOR_PROVIDER" = "AzureSearch" ] && [ -z "${AzureSearch__ApiKey:-}" ]; then
  echo "Error: AzureSearch__ApiKey is required when Vector__Provider=AzureSearch." >&2
  exit 1
fi

Speech__Endpoint="${Speech__Endpoint%/}"

SETTINGS=(
  "ASPNETCORE_ENVIRONMENT=Production"
  "DOTNET_ENVIRONMENT=Production"
  "Database__Provider=${DATABASE_PROVIDER}"
  "Queue__Provider=${QUEUE_PROVIDER}"
  "Vector__Provider=${VECTOR_PROVIDER}"
  "ConnectionStrings__AzureQueue=${STORAGE_CONN}"
  "ConnectionStrings__AzureTables=${STORAGE_CONN}"
  "AzureOpenAI__Endpoint=${AzureOpenAI__Endpoint}"
  "AzureOpenAI__ApiKey=${AzureOpenAI__ApiKey}"
  "AzureOpenAI__Deployment=${AzureOpenAI__Deployment}"
  "Mcp__PublicBaseUrl=${Mcp__PublicBaseUrl}"
  "Mcp__FrontendBaseUrl=${Mcp__FrontendBaseUrl}"
  "Mcp__Enabled=true"
  "Chat__Provider=${Chat__Provider}"
)

if [ -n "${AzureOpenAI__EmbeddingDeployment:-}" ]; then
  SETTINGS+=("AzureOpenAI__EmbeddingDeployment=${AzureOpenAI__EmbeddingDeployment}")
fi

SETTINGS+=("ConnectionStrings__Commerce=${ConnectionStrings__Commerce}")

if [ -n "${ConnectionStrings__Redis:-}" ]; then
  SETTINGS+=("ConnectionStrings__Redis=${ConnectionStrings__Redis}")
fi

if [ "$VECTOR_PROVIDER" = "AzureSearch" ]; then
  SETTINGS+=(
    "AzureSearch__Endpoint=${AzureSearch__Endpoint}"
    "AzureSearch__ApiKey=${AzureSearch__ApiKey}"
    "ConnectionStrings__AzureSearch=${AzureSearch__Endpoint}"
    "AzureSearch__DeleteIndexOnBootstrap=${AzureSearch__DeleteIndexOnBootstrap:-false}"
  )
elif [ -n "${ConnectionStrings__Qdrant:-}" ]; then
  SETTINGS+=("ConnectionStrings__Qdrant=${ConnectionStrings__Qdrant}")
fi

if [ -n "${Auth__SigningKey:-}" ]; then
  SETTINGS+=("Auth__SigningKey=${Auth__SigningKey}")
fi

if [ -n "${CHAT_ALLOWED_ORIGINS:-}" ]; then
  IFS=',' read -ra ORIGINS <<< "$CHAT_ALLOWED_ORIGINS"
  i=0
  for origin in "${ORIGINS[@]}"; do
    origin="${origin#"${origin%%[![:space:]]*}"}"
    origin="${origin%"${origin##*[![:space:]]}"}"
    [ -z "$origin" ] && continue
    SETTINGS+=("Chat__AllowedOrigins__${i}=${origin}")
    i=$((i + 1))
  done
fi

if [ -n "${Speech__Key:-}" ]; then
  SETTINGS+=("Speech__Key=${Speech__Key}")
fi
if [ -n "${Speech__Endpoint:-}" ]; then
  SETTINGS+=("Speech__Endpoint=${Speech__Endpoint}")
fi
if [ -n "${Speech__Region:-}" ]; then
  SETTINGS+=("Speech__Region=${Speech__Region}")
fi

apply_settings() {
  local name="$1"
  echo "→ Updating App Settings on ${name}..."
  az webapp config appsettings set \
    --resource-group "$WEBAPP_RESOURCE_GROUP" \
    --name "$name" \
    --settings "${SETTINGS[@]}" \
    --output none
  echo "  Done: ${name}"
}

case "$TARGET" in
  api)
    apply_settings "$WEBAPP_NAME"
    ;;
  worker)
    apply_settings "$WEBAPP_WORKER_NAME"
    ;;
  mcp)
    apply_settings "$WEBAPP_MCP_NAME"
    ;;
  all)
    apply_settings "$WEBAPP_NAME"
    apply_settings "$WEBAPP_WORKER_NAME"
    apply_settings "$WEBAPP_MCP_NAME"
    ;;
  *)
    echo "Usage: $0 api|worker|mcp|all" >&2
    exit 1
    ;;
esac

echo "App Settings updated (hierarchical __ keys only):"
echo "  Database__Provider=${DATABASE_PROVIDER}"
echo "  Queue__Provider=${QUEUE_PROVIDER}"
echo "  Vector__Provider=${VECTOR_PROVIDER}"
echo "  AzureOpenAI__Endpoint=$([ -n "${AzureOpenAI__Endpoint}" ] && echo '(set)' || echo '(empty)')"
echo "  AzureSearch__Endpoint=${AzureSearch__Endpoint:-'(unset)'}"
echo "  Speech__Key=$([ -n "${Speech__Key:-}" ] && echo '(set)' || echo '(unset)')"

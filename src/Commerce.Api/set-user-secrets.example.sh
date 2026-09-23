#!/usr/bin/env bash
# Copy to set-user-secrets.sh (gitignored) and fill the values.
# Do not commit the key. Dev source: team-ecommerce user-secrets, not appsettings.json.
set -euo pipefail

API_PROJECT="$(cd "$(dirname "$0")" && pwd)/Commerce.Api.csproj"

dotnet user-secrets set "AzureOpenAI:Endpoint" "https://tu-recurso.openai.azure.com/" --project "$API_PROJECT"
dotnet user-secrets set "AzureOpenAI:ApiKey" "tu-api-key" --project "$API_PROJECT"
dotnet user-secrets set "AzureOpenAI:Deployment" "gpt-4o-mini" --project "$API_PROJECT"
dotnet user-secrets set "AzureOpenAI:EmbeddingDeployment" "text-embedding-3-small" --project "$API_PROJECT"
dotnet user-secrets set "Speech:Key" "tu-speech-key" --project "$API_PROJECT"
dotnet user-secrets set "Speech:Region" "eastus" --project "$API_PROJECT"
dotnet user-secrets set "Speech:Endpoint" "" --project "$API_PROJECT"

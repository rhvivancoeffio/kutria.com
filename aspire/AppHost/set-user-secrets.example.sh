#!/usr/bin/env bash
# Template for Aspire AppHost user secrets (Commerce).
# Copy to set-user-secrets.sh, fill values, then: make setup-secrets-apphost
# set-user-secrets.sh is gitignored — never commit real keys.

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$SCRIPT_DIR/Commerce.AppHost.csproj"
cd "$REPO_ROOT"

echo "Configuring User Secrets for Aspire AppHost (Commerce)..."

dotnet user-secrets set "Parameters:postgres-password" "commerce" --project "$PROJECT"

echo "Done. List with: dotnet user-secrets list --project aspire/AppHost/Commerce.AppHost.csproj"

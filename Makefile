.PHONY: help build clean restore \
	run-apphost run-apphost-postgres run-apphost-sqlserver run-backend run-api run-worker \
	run-frontend frontend-install frontend-preview frontend-dist \
	run-compose compose-up compose-down compose-logs \
	backend-publish worker-publish mcp-publish shop-publish \
	deploy-zip deploy-worker-zip deploy-mcp-zip deploy-shop-zip \
	deploy-backend deploy-worker deploy-mcp deploy-shop deploy-frontend deploy-swa deploy-all \
	deploy-settings deploy-settings-api deploy-settings-worker deploy-settings-mcp \
	provision-infra \
	ssl-shop-dns ssl-shop-renew ssl-shop-letsencrypt \
	migrate-add migrate-add-tenant migrate-update migrate-reset migrate-reset-postgres \
	postgres-clean postgres-volume-rm \
	setup-secrets-apphost setup-env

# Makefile directory (allows: make -C ocp-project deploy-backend)
ROOT := $(dir $(abspath $(firstword $(MAKEFILE_LIST))))

SOLUTION = Commerce.sln
APPHOST = aspire/AppHost/Commerce.AppHost.csproj
API_PROJECT = src/Commerce.Api/Commerce.Api.csproj
WORKER_PROJECT = src/Commerce.Worker/Commerce.Worker.csproj
MCP_PROJECT = src/Commerce.Mcp/Commerce.Mcp.csproj
SHOP_PROJECT = src/Commerce.Shop/Commerce.Shop.csproj
PERSISTENCE_PROJECT = src/Commerce.Infrastructure.Persistence/Commerce.Infrastructure.Persistence.csproj
PERSISTENCE_DIR = src/Commerce.Infrastructure.Persistence
FRONTEND_DIR = src/Commerce.Web
PUBLISH_DIR = publish
PUBLISH_WORKER_DIR = publish-worker
PUBLISH_MCP_DIR = publish-mcp
PUBLISH_SHOP_DIR = publish-shop
# Shop SSL (manual renew). Override SHOP_SSL_PFX / SHOP_SSL_PFX_PASSWORD / SHOP_SSL_DOMAIN.
SHOP_SSL_DOMAIN ?= kutria.com
SHOP_SSL_PFX ?= $(ROOT)certs/kutria-wildcard.pfx
SHOP_SSL_PFX_PASSWORD ?=
SHOP_SSL_DAYS ?= 365

POSTGRES_CONN ?= Host=localhost;Port=5433;Database=commerce;Username=commerce;Password=commerce
SQL_SERVER_CONN ?= Server=localhost,1433;Database=commerce;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
REDIS_CONN ?= localhost:6380
DB_PROVIDER ?= Postgres

# Deploy frontend (Azure Static Web App)
SWA_NAME ?= swa-kutria-commerce
SWA_RESOURCE_GROUP ?= Gravity-Agentic
SWA_LOCATION ?= eastus2
SWA_SKU ?= Standard

# Deploy backend API / worker / MCP / shop (Azure Web App)
WEBAPP_NAME ?= web-app-kutria-commerce
WEBAPP_WORKER_NAME ?= web-app-kutria-commerce-worker
WEBAPP_MCP_NAME ?= web-app-kutria-commerce-mcp
WEBAPP_SHOP_NAME ?= web-app-kutria-commerce-shop
WEBAPP_RESOURCE_GROUP ?= Gravity-Agentic
WEBAPP_LOCATION ?= eastus
WEBAPP_RUNTIME ?= "DOTNETCORE:10.0"
APP_SERVICE_PLAN_NAME ?= service-plan-kutria-agentic
APP_SERVICE_PLAN_SKU ?= B1
# Azure Storage Account (queues/blobs) — same RG as Web App
STORAGE_ACCOUNT_NAME ?= stkutriaagenticcommerce
STORAGE_LOCATION ?= eastus
STORAGE_SKU ?= Standard_RAGRS
# Azure SQL (Basic 5 DTU) — provision-infra; password via SQL_ADMIN_PASSWORD env
SQL_SERVER_NAME ?= sql-kutria-commerce
SQL_DB_NAME ?= commerce
SQL_ADMIN_USER ?= commerceadmin
# Azure AI Search (Free) — one Free service per subscription
SEARCH_SERVICE_NAME ?= search-kutria-commerce
SEARCH_SKU ?= free
SEARCH_LOCATION ?= $(WEBAPP_LOCATION)
# App Settings (make deploy-settings) — override via env or make vars
DATABASE_PROVIDER ?= SqlServer

help: ## Show available commands
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-24s %s\n", $$1, $$2}'

# ─── Build ───────────────────────────────────────────────────────────────────

restore: ## Restore NuGet packages
	dotnet restore $(SOLUTION)

build: ## Build solution
	dotnet build $(SOLUTION)

clean: ## Clean binaries (dotnet clean + EF 10 bin\\Debug quirk)
	dotnet clean $(SOLUTION)
	@-find . -type d -name 'bin\\Debug' -exec rm -rf {} + 2>/dev/null || true

# ─── Run (Aspire / local) ────────────────────────────────────────────────────

run-apphost-postgres: ## Aspire AppHost with PostgreSQL (default). Frontend: make run-frontend
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	DB_PROVIDER=Postgres Database__Provider=PostgreSQL \
		dotnet watch run --project $(APPHOST) /p:DatabaseProvider=Postgres

run-apphost-sqlserver: ## Aspire AppHost with SQL Server. Frontend: make run-frontend
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	DB_PROVIDER=SqlServer Database__Provider=SqlServer \
		dotnet watch run --project $(APPHOST) /p:DatabaseProvider=SqlServer

run-apphost: run-apphost-postgres ## Alias for run-apphost-postgres

run-backend: run-apphost ## Alias: start backend (Postgres; use run-apphost-sqlserver for SQL Server)

run-api: ## Run API only (expects compose Postgres :5433 / Redis :6380)
	ConnectionStrings__Commerce="$(POSTGRES_CONN)" ConnectionStrings__Redis=$(REDIS_CONN) \
		dotnet run --project $(API_PROJECT) --urls http://localhost:5050

run-worker: ## Run Worker only (expects compose Postgres :5433 / Redis :6380)
	ConnectionStrings__Commerce="$(POSTGRES_CONN)" ConnectionStrings__Redis=$(REDIS_CONN) \
		dotnet run --project $(WORKER_PROJECT)

frontend-install: ## Install frontend dependencies (npm ci)
	@cd "$(ROOT)$(FRONTEND_DIR)" && npm ci

run-frontend: ## Vite dev server (http://localhost:3000/t/{tenant} — Finbuckle path)
	@cd "$(ROOT)$(FRONTEND_DIR)" && npm run dev

frontend-preview: ## Preview production build (run frontend-dist first)
	@cd "$(ROOT)$(FRONTEND_DIR)" && npm run preview

# ─── Docker Compose ──────────────────────────────────────────────────────────

compose-up: ## docker compose up --build (detached)
	@cd "$(ROOT)" && (test -f .env || cp .env.example .env) && docker compose up --build -d

compose-down: ## docker compose down
	@cd "$(ROOT)" && docker compose down

compose-logs: ## Follow docker compose logs
	@cd "$(ROOT)" && docker compose logs -f

run-compose: compose-up ## Alias for compose-up

# ─── Migrations (App + TenantStore; both SqlServer and Postgres — same pattern as channel-ecommerce) ─

migrate-add: ## Add App migration for SqlServer + Postgres (MIGRATION_NAME=Name)
	@if [ -z "$(MIGRATION_NAME)" ]; then \
		echo "Error: Specify MIGRATION_NAME. Example: make migrate-add MIGRATION_NAME=AddFoo"; \
		exit 1; \
	fi
	@echo "Creating App migration $(MIGRATION_NAME) for SqlServer..."
	@Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context SqlServerCommerceDbContext \
		--project $(PERSISTENCE_PROJECT) \
		--startup-project $(API_PROJECT) \
		--output-dir Migrations/App/SqlServer \
		-- /p:DatabaseProvider=SqlServer
	@echo "Creating App migration $(MIGRATION_NAME) for Postgres..."
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context PostgresCommerceDbContext \
		--project $(PERSISTENCE_PROJECT) \
		--startup-project $(API_PROJECT) \
		--output-dir Migrations/App/Postgres \
		-- /p:DatabaseProvider=Postgres

migrate-add-tenant: ## Add TenantStore migration for SqlServer + Postgres (MIGRATION_NAME=Name)
	@if [ -z "$(MIGRATION_NAME)" ]; then \
		echo "Error: Specify MIGRATION_NAME. Example: make migrate-add-tenant MIGRATION_NAME=AddFoo"; \
		exit 1; \
	fi
	@echo "Creating TenantStore migration $(MIGRATION_NAME) for SqlServer..."
	@Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context SqlServerCommerceTenantStoreDbContext \
		--project $(PERSISTENCE_PROJECT) \
		--startup-project $(API_PROJECT) \
		--output-dir Migrations/TenantStore/SqlServer \
		-- /p:DatabaseProvider=SqlServer
	@echo "Creating TenantStore migration $(MIGRATION_NAME) for Postgres..."
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context PostgresCommerceTenantStoreDbContext \
		--project $(PERSISTENCE_PROJECT) \
		--startup-project $(API_PROJECT) \
		--output-dir Migrations/TenantStore/Postgres \
		-- /p:DatabaseProvider=Postgres

migrate-update: ## Apply migrations (DB_PROVIDER=Postgres|SqlServer|Both, default Postgres). TenantStore then App.
	@if [ "$(DB_PROVIDER)" = "Both" ]; then \
		echo "Applying SqlServer TenantStore + App migrations..."; \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceTenantStoreDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
		echo "Applying Postgres TenantStore + App migrations..."; \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceTenantStoreDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
	elif [ "$(DB_PROVIDER)" = "SqlServer" ]; then \
		echo "Applying SqlServer TenantStore + App migrations..."; \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceTenantStoreDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
	else \
		echo "Applying Postgres TenantStore + App migrations..."; \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceTenantStoreDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceDbContext \
			--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
	fi

migrate-reset: ## Wipe migrations, recreate Initial* for SqlServer + Postgres, remove postgres volumes
	@echo "Cleaning Postgres volumes/containers..."
	@$(MAKE) postgres-clean 2>/dev/null || true
	@echo "Removing migrations..."
	@rm -rf $(PERSISTENCE_DIR)/Migrations
	@dotnet restore $(SOLUTION)
	@$(MAKE) migrate-add-tenant MIGRATION_NAME=InitialTenantStore
	@$(MAKE) migrate-add MIGRATION_NAME=InitialApp
	@dotnet build $(SOLUTION)
	@echo "Migrations reset complete. Run: make run-apphost  (or make compose-up)"

migrate-reset-postgres: ## Remove Postgres migrations only and add Initial* (SqlServer untouched). Override: POSTGRES_CONN=...
	@echo "Removing Postgres migrations only..."
	@rm -rf $(PERSISTENCE_DIR)/Migrations/App/Postgres $(PERSISTENCE_DIR)/Migrations/TenantStore/Postgres
	@dotnet restore $(SOLUTION)
	@echo "Creating InitialTenantStore for Postgres..."
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add InitialTenantStore \
		--context PostgresCommerceTenantStoreDbContext \
		--project $(PERSISTENCE_PROJECT) \
		--startup-project $(API_PROJECT) \
		--output-dir Migrations/TenantStore/Postgres \
		-- /p:DatabaseProvider=Postgres
	@echo "Creating InitialApp for Postgres..."
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add InitialApp \
		--context PostgresCommerceDbContext \
		--project $(PERSISTENCE_PROJECT) \
		--startup-project $(API_PROJECT) \
		--output-dir Migrations/App/Postgres \
		-- /p:DatabaseProvider=Postgres
	@echo "Postgres Initial migrations created."

postgres-clean: ## Stop Commerce Postgres containers + remove related volumes
	@chmod +x "$(ROOT)scripts/postgres-clean.sh"
	@"$(ROOT)scripts/postgres-clean.sh"

postgres-volume-rm: postgres-clean ## Alias for postgres-clean

# ─── Secrets / env ────────────────────────────────────────────────────────────

setup-env: ## Copy .env.example → .env if missing
	@cd "$(ROOT)" && if [ ! -f .env ]; then cp .env.example .env && echo "Created .env from .env.example"; else echo ".env already exists"; fi

setup-secrets-apphost: ## Configure Aspire AppHost user secrets
	@cd "$(ROOT)" && if [ -f aspire/AppHost/set-user-secrets.sh ]; then \
		bash aspire/AppHost/set-user-secrets.sh; \
	else \
		echo "aspire/AppHost/set-user-secrets.sh not found."; \
		echo "Copy aspire/AppHost/set-user-secrets.example.sh → set-user-secrets.sh and fill values."; \
		exit 1; \
	fi

# ─── Provision (Azure infrastructure) ─────────────────────────────────────────
# Idempotent: creates resources only if missing. Names match deploy-* targets.

provision-infra: ## Provision Storage, SQL, Search, App Service Plan, Web Apps (API+Worker+MCP+Shop), SWA
	@echo "Provisioning infrastructure in $(WEBAPP_RESOURCE_GROUP)..."
	@echo "→ Storage Account $(STORAGE_ACCOUNT_NAME)"
	@az storage account show --name $(STORAGE_ACCOUNT_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az storage account create \
			--name $(STORAGE_ACCOUNT_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--location $(STORAGE_LOCATION) \
			--sku $(STORAGE_SKU) \
			--kind StorageV2 \
			--min-tls-version TLS1_2 \
			--allow-blob-public-access false \
			--https-only true
	@echo "→ SQL Server $(SQL_SERVER_NAME) + DB $(SQL_DB_NAME) (Basic 5 DTU)"
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	if [ -n "$(SQL_ADMIN_PASSWORD)" ]; then SQL_ADMIN_PASSWORD="$(SQL_ADMIN_PASSWORD)"; fi; \
	if ! az sql server show --name $(SQL_SERVER_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1; then \
		if [ -z "$$SQL_ADMIN_PASSWORD" ]; then \
			echo "Error: SQL_ADMIN_PASSWORD is required to create SQL Server $(SQL_SERVER_NAME)."; \
			echo "Add it to .env, or: make provision-infra SQL_ADMIN_PASSWORD='...'"; \
			exit 1; \
		fi; \
		az sql server create \
			--name $(SQL_SERVER_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--location $(WEBAPP_LOCATION) \
			--admin-user $(SQL_ADMIN_USER) \
			--admin-password "$$SQL_ADMIN_PASSWORD"; \
	fi
	@az sql db show --name $(SQL_DB_NAME) --server $(SQL_SERVER_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az sql db create \
			--name $(SQL_DB_NAME) \
			--server $(SQL_SERVER_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--edition Basic \
			--capacity 5 \
			--backup-storage-redundancy Local \
			--zone-redundant false
	@az sql server firewall-rule show --name AllowAzureServices --server $(SQL_SERVER_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az sql server firewall-rule create \
			--name AllowAzureServices \
			--server $(SQL_SERVER_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--start-ip-address 0.0.0.0 \
			--end-ip-address 0.0.0.0
	@echo "→ Azure AI Search $(SEARCH_SERVICE_NAME) (SKU $(SEARCH_SKU))"
	@az search service show --name $(SEARCH_SERVICE_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az search service create \
			--name $(SEARCH_SERVICE_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--location $(SEARCH_LOCATION) \
			--sku $(SEARCH_SKU)
	@echo "→ App Service Plan $(APP_SERVICE_PLAN_NAME)"
	@az appservice plan show --name $(APP_SERVICE_PLAN_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az appservice plan create \
			--name $(APP_SERVICE_PLAN_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--location $(WEBAPP_LOCATION) \
			--sku $(APP_SERVICE_PLAN_SKU) \
			--is-linux
	@echo "→ Web App $(WEBAPP_NAME)"
	@az webapp show --name $(WEBAPP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az webapp create \
			--name $(WEBAPP_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--plan $(APP_SERVICE_PLAN_NAME) \
			--runtime $(WEBAPP_RUNTIME)
	@az webapp update --name $(WEBAPP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) --https-only true >/dev/null
	@az webapp config set --name $(WEBAPP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) --ftps-state FtpsOnly >/dev/null
	@echo "→ Web App worker $(WEBAPP_WORKER_NAME)"
	@az webapp show --name $(WEBAPP_WORKER_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az webapp create \
			--name $(WEBAPP_WORKER_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--plan $(APP_SERVICE_PLAN_NAME) \
			--runtime $(WEBAPP_RUNTIME)
	@az webapp update --name $(WEBAPP_WORKER_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) --https-only true >/dev/null
	@echo "→ Web App MCP $(WEBAPP_MCP_NAME) (Commerce.Mcp)"
	@az webapp show --name $(WEBAPP_MCP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az webapp create \
			--name $(WEBAPP_MCP_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--plan $(APP_SERVICE_PLAN_NAME) \
			--runtime $(WEBAPP_RUNTIME)
	@az webapp update --name $(WEBAPP_MCP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) --https-only true >/dev/null
	@az webapp config set --name $(WEBAPP_MCP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) --ftps-state FtpsOnly >/dev/null
	@echo "→ Web App shop $(WEBAPP_SHOP_NAME)"
	@az webapp show --name $(WEBAPP_SHOP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az webapp create \
			--name $(WEBAPP_SHOP_NAME) \
			--resource-group $(WEBAPP_RESOURCE_GROUP) \
			--plan $(APP_SERVICE_PLAN_NAME) \
			--runtime $(WEBAPP_RUNTIME)
	@az webapp update --name $(WEBAPP_SHOP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) --https-only true >/dev/null
	@az webapp config set --name $(WEBAPP_SHOP_NAME) --resource-group $(WEBAPP_RESOURCE_GROUP) --ftps-state FtpsOnly >/dev/null
	@echo "→ Static Web App $(SWA_NAME)"
	@az staticwebapp show --name $(SWA_NAME) --resource-group $(SWA_RESOURCE_GROUP) >/dev/null 2>&1 || \
		az staticwebapp create \
			--name $(SWA_NAME) \
			--resource-group $(SWA_RESOURCE_GROUP) \
			--location $(SWA_LOCATION) \
			--sku $(SWA_SKU)
	@echo "Infrastructure ready:"
	@echo "  Storage:         $(STORAGE_ACCOUNT_NAME)"
	@echo "  SQL Server:      $(SQL_SERVER_NAME).database.windows.net"
	@echo "  SQL Database:    $(SQL_DB_NAME) (Basic 5 DTU)"
	@echo "  Azure Search:    https://$(SEARCH_SERVICE_NAME).search.windows.net (SKU $(SEARCH_SKU))"
	@echo "  Web App:         $(WEBAPP_NAME)"
	@echo "  Web App worker:  $(WEBAPP_WORKER_NAME)"
	@echo "  Web App MCP:     $(WEBAPP_MCP_NAME)"
	@echo "  Web App shop:    $(WEBAPP_SHOP_NAME)"
	@echo "  SWA:             $(SWA_NAME)"
	@echo "  Search admin key: az search admin-key show -g $(WEBAPP_RESOURCE_GROUP) --service-name $(SEARCH_SERVICE_NAME) --query primaryKey -o tsv"

# ─── Deploy (production) ──────────────────────────────────────────────────────

frontend-dist: ## Build frontend for production (dist/) — VITE_API_URL=https://api.kutria.com
	@echo "Building frontend (from $(ROOT))..."
	@echo "  Frontend: https://kutria.com"
	@echo "  API:      $$(grep -E '^VITE_API_URL=' $(FRONTEND_DIR)/.env.production | cut -d= -f2-)"
	@cd "$(ROOT)" && rm -rf $(FRONTEND_DIR)/dist && cd "$(FRONTEND_DIR)" && npm ci && npm run build
	@cd "$(ROOT)$(FRONTEND_DIR)" && node scripts/verify-build-env.js

backend-publish: ## Publish API for production (publish/)
	@echo "Publishing API (from $(ROOT))..."
	@cd $(ROOT) && rm -rf $(PUBLISH_DIR) && dotnet publish $(API_PROJECT) -c Release -o $(PUBLISH_DIR)
	@echo "Publish output: $(ROOT)$(PUBLISH_DIR)"
	@test -f $(ROOT)$(PUBLISH_DIR)/Commerce.Api.dll || (echo "ERROR: Commerce.Api.dll not found in publish/." && exit 1)

worker-publish: ## Publish Worker for production (publish-worker/)
	@echo "Publishing Worker (from $(ROOT))..."
	@cd $(ROOT) && rm -rf $(PUBLISH_WORKER_DIR) && dotnet publish $(WORKER_PROJECT) -c Release -o $(PUBLISH_WORKER_DIR)
	@echo "Publish output: $(ROOT)$(PUBLISH_WORKER_DIR)"
	@test -f $(ROOT)$(PUBLISH_WORKER_DIR)/Commerce.Worker.dll || (echo "ERROR: Commerce.Worker.dll not found in publish-worker/." && exit 1)

mcp-publish: ## Publish MCP (Commerce.Mcp) for production (publish-mcp/)
	@echo "Publishing MCP (from $(ROOT))..."
	@test -f "$(ROOT)$(MCP_PROJECT)" || (echo "ERROR: $(MCP_PROJECT) not found. Create Commerce.Mcp first." && exit 1)
	@cd $(ROOT) && rm -rf $(PUBLISH_MCP_DIR) && dotnet publish $(MCP_PROJECT) -c Release -o $(PUBLISH_MCP_DIR)
	@echo "Publish output: $(ROOT)$(PUBLISH_MCP_DIR)"
	@test -f $(ROOT)$(PUBLISH_MCP_DIR)/Commerce.Mcp.dll || (echo "ERROR: Commerce.Mcp.dll not found in publish-mcp/." && exit 1)

shop-publish: ## Publish Shop (Commerce.Shop) for production (publish-shop/)
	@echo "Publishing Shop (from $(ROOT))..."
	@test -f "$(ROOT)$(SHOP_PROJECT)" || (echo "ERROR: $(SHOP_PROJECT) not found. Create Commerce.Shop first." && exit 1)
	@cd $(ROOT) && rm -rf $(PUBLISH_SHOP_DIR) && dotnet publish $(SHOP_PROJECT) -c Release -o $(PUBLISH_SHOP_DIR)
	@echo "Publish output: $(ROOT)$(PUBLISH_SHOP_DIR)"
	@test -f $(ROOT)$(PUBLISH_SHOP_DIR)/Commerce.Shop.dll || (echo "ERROR: Commerce.Shop.dll not found in publish-shop/." && exit 1)

deploy-zip: backend-publish ## Create deploy.zip (no Azure deploy)
	@cd $(ROOT)$(PUBLISH_DIR) && zip -r $(ROOT)deploy.zip . -x "bin/*" -x "BuildHost-net472/*" -x "BuildHost-netcore/*"
	@echo "Created $(ROOT)deploy.zip"

deploy-worker-zip: worker-publish ## Create deploy-worker.zip (no Azure deploy)
	@cd $(ROOT)$(PUBLISH_WORKER_DIR) && zip -r $(ROOT)deploy-worker.zip . -x "bin/*" -x "BuildHost-net472/*" -x "BuildHost-netcore/*"
	@echo "Created $(ROOT)deploy-worker.zip"

deploy-mcp-zip: mcp-publish ## Create deploy-mcp.zip (no Azure deploy)
	@cd $(ROOT)$(PUBLISH_MCP_DIR) && zip -r $(ROOT)deploy-mcp.zip . -x "bin/*" -x "BuildHost-net472/*" -x "BuildHost-netcore/*"
	@echo "Created $(ROOT)deploy-mcp.zip"

deploy-shop-zip: shop-publish ## Create deploy-shop.zip (no Azure deploy)
	@cd $(ROOT)$(PUBLISH_SHOP_DIR) && zip -r $(ROOT)deploy-shop.zip . -x "bin/*" -x "BuildHost-net472/*" -x "BuildHost-netcore/*"
	@echo "Created $(ROOT)deploy-shop.zip"

deploy-backend: deploy-zip ## Deploy API to Azure Web App. Override WEBAPP_NAME, WEBAPP_RESOURCE_GROUP.
	@echo "Deploying to $(WEBAPP_NAME) (--clean true)..."
	az webapp deploy --resource-group $(WEBAPP_RESOURCE_GROUP) --name $(WEBAPP_NAME) --src-path $(ROOT)deploy.zip --type zip --clean true --verbose
	@rm -f $(ROOT)deploy.zip

deploy-worker: deploy-worker-zip ## Deploy Worker to Azure Web App. Override WEBAPP_WORKER_NAME, WEBAPP_RESOURCE_GROUP.
	@echo "Deploying worker to $(WEBAPP_WORKER_NAME) (--clean true)..."
	az webapp deploy --resource-group $(WEBAPP_RESOURCE_GROUP) --name $(WEBAPP_WORKER_NAME) --src-path $(ROOT)deploy-worker.zip --type zip --clean true --verbose
	@rm -f $(ROOT)deploy-worker.zip

deploy-mcp: deploy-mcp-zip ## Deploy Commerce.Mcp to Azure Web App. Override WEBAPP_MCP_NAME, WEBAPP_RESOURCE_GROUP.
	@echo "Deploying MCP to $(WEBAPP_MCP_NAME) (--clean true)..."
	az webapp deploy --resource-group $(WEBAPP_RESOURCE_GROUP) --name $(WEBAPP_MCP_NAME) --src-path $(ROOT)deploy-mcp.zip --type zip --clean true --verbose
	@rm -f $(ROOT)deploy-mcp.zip

deploy-shop: deploy-shop-zip ## Deploy Commerce.Shop to Azure Web App. Override WEBAPP_SHOP_NAME, WEBAPP_RESOURCE_GROUP.
	@echo "Deploying Shop to $(WEBAPP_SHOP_NAME) (--clean true)..."
	az webapp deploy --resource-group $(WEBAPP_RESOURCE_GROUP) --name $(WEBAPP_SHOP_NAME) --src-path $(ROOT)deploy-shop.zip --type zip --clean true --verbose
	@rm -f $(ROOT)deploy-shop.zip

# Shop custom domain + SSL (manual). DNS records must be created at your DNS host.
ssl-shop-dns: ## Print DNS records + bind *.kutria.com hostname on shop Web App
	@chmod +x "$(ROOT)scripts/ssl-shop.sh"
	@WEBAPP_SHOP_NAME="$(WEBAPP_SHOP_NAME)" \
		WEBAPP_RESOURCE_GROUP="$(WEBAPP_RESOURCE_GROUP)" \
		SHOP_SSL_DOMAIN="$(SHOP_SSL_DOMAIN)" \
		"$(ROOT)scripts/ssl-shop.sh" dns

# Regenerate (default 365 days) + upload + SNI bind when the cert expires. No cron.
# Override: SHOP_SSL_PFX, SHOP_SSL_PFX_PASSWORD, SHOP_SSL_DAYS. Pass SKIP_GENERATE=1 to only upload an existing PFX.
ssl-shop-renew: ## Generate/upload shop wildcard PFX (~1y) and bind SNI (run manually when needed)
	@chmod +x "$(ROOT)scripts/ssl-shop.sh"
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	WEBAPP_SHOP_NAME="$(WEBAPP_SHOP_NAME)" \
		WEBAPP_RESOURCE_GROUP="$(WEBAPP_RESOURCE_GROUP)" \
		SHOP_SSL_DOMAIN="$(SHOP_SSL_DOMAIN)" \
		SHOP_SSL_PFX="$(SHOP_SSL_PFX)" \
		SHOP_SSL_PFX_PASSWORD="$${SHOP_SSL_PFX_PASSWORD:-$(SHOP_SSL_PFX_PASSWORD)}" \
		SHOP_SSL_DAYS="$(SHOP_SSL_DAYS)" \
		SKIP_GENERATE="$(SKIP_GENERATE)" \
		"$(ROOT)scripts/ssl-shop.sh" renew

# Interactive: Let's Encrypt DNS-01 (Azure) → PFX → upload. Prompts for email + PFX password.
ssl-shop-letsencrypt: ## Renew free Let's Encrypt wildcard + upload to shop (asks for inputs)
	@chmod +x "$(ROOT)scripts/ssl-shop-letsencrypt.sh" "$(ROOT)scripts/ssl-shop.sh" \
		"$(ROOT)scripts/certbot-azure-auth.sh" "$(ROOT)scripts/certbot-azure-cleanup.sh"
	@"$(ROOT)scripts/ssl-shop-letsencrypt.sh"

# App Settings: DB + storage + Azure Search + Speech from env. Does not deploy code.
# Defaults: Database__Provider=SqlServer, Queue__Provider=AzureQueue, Vector__Provider=AzureSearch
# Required: ConnectionStrings__Commerce (SqlServer Server=...) or SQL_ADMIN_PASSWORD to auto-build
# Optional: AzureOpenAI__* / Speech__* / AzureSearch__* / ConnectionStrings__Redis,
#   Auth__SigningKey, CHAT_ALLOWED_ORIGINS (default https://kutria.com)
# Override: DATABASE_PROVIDER=PostgreSQL + ConnectionStrings__Commerce for Postgres
deploy-settings: ## Update Azure App Settings on API + Worker + MCP (secrets from env)
	@chmod +x "$(ROOT)scripts/deploy-settings.sh"
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	DATABASE_PROVIDER="$(DATABASE_PROVIDER)" \
		SEARCH_SERVICE_NAME="$(SEARCH_SERVICE_NAME)" \
		WEBAPP_NAME="$(WEBAPP_NAME)" \
		WEBAPP_WORKER_NAME="$(WEBAPP_WORKER_NAME)" \
		WEBAPP_MCP_NAME="$(WEBAPP_MCP_NAME)" \
		WEBAPP_RESOURCE_GROUP="$(WEBAPP_RESOURCE_GROUP)" \
		STORAGE_ACCOUNT_NAME="$(STORAGE_ACCOUNT_NAME)" \
		SQL_SERVER_NAME="$(SQL_SERVER_NAME)" \
		SQL_DB_NAME="$(SQL_DB_NAME)" \
		SQL_ADMIN_USER="$(SQL_ADMIN_USER)" \
		"$(ROOT)scripts/deploy-settings.sh" all

deploy-settings-api: ## Update Azure App Settings on API Web App only
	@chmod +x "$(ROOT)scripts/deploy-settings.sh"
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	DATABASE_PROVIDER="$(DATABASE_PROVIDER)" \
		SEARCH_SERVICE_NAME="$(SEARCH_SERVICE_NAME)" \
		WEBAPP_NAME="$(WEBAPP_NAME)" \
		WEBAPP_RESOURCE_GROUP="$(WEBAPP_RESOURCE_GROUP)" \
		STORAGE_ACCOUNT_NAME="$(STORAGE_ACCOUNT_NAME)" \
		SQL_SERVER_NAME="$(SQL_SERVER_NAME)" \
		SQL_DB_NAME="$(SQL_DB_NAME)" \
		SQL_ADMIN_USER="$(SQL_ADMIN_USER)" \
		"$(ROOT)scripts/deploy-settings.sh" api

deploy-settings-worker: ## Update Azure App Settings on Worker Web App only
	@chmod +x "$(ROOT)scripts/deploy-settings.sh"
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	DATABASE_PROVIDER="$(DATABASE_PROVIDER)" \
		SEARCH_SERVICE_NAME="$(SEARCH_SERVICE_NAME)" \
		WEBAPP_WORKER_NAME="$(WEBAPP_WORKER_NAME)" \
		WEBAPP_RESOURCE_GROUP="$(WEBAPP_RESOURCE_GROUP)" \
		STORAGE_ACCOUNT_NAME="$(STORAGE_ACCOUNT_NAME)" \
		SQL_SERVER_NAME="$(SQL_SERVER_NAME)" \
		SQL_DB_NAME="$(SQL_DB_NAME)" \
		SQL_ADMIN_USER="$(SQL_ADMIN_USER)" \
		"$(ROOT)scripts/deploy-settings.sh" worker

deploy-settings-mcp: ## Update Azure App Settings on MCP Web App only
	@chmod +x "$(ROOT)scripts/deploy-settings.sh"
	@set -a; [ -f "$(ROOT).env" ] && . "$(ROOT).env"; set +a; \
	DATABASE_PROVIDER="$(DATABASE_PROVIDER)" \
		SEARCH_SERVICE_NAME="$(SEARCH_SERVICE_NAME)" \
		WEBAPP_MCP_NAME="$(WEBAPP_MCP_NAME)" \
		WEBAPP_RESOURCE_GROUP="$(WEBAPP_RESOURCE_GROUP)" \
		STORAGE_ACCOUNT_NAME="$(STORAGE_ACCOUNT_NAME)" \
		SQL_SERVER_NAME="$(SQL_SERVER_NAME)" \
		SQL_DB_NAME="$(SQL_DB_NAME)" \
		SQL_ADMIN_USER="$(SQL_ADMIN_USER)" \
		"$(ROOT)scripts/deploy-settings.sh" mcp

deploy-frontend: frontend-dist ## Deploy frontend to Azure Static Web App. Override SWA_NAME, SWA_RESOURCE_GROUP.
	@echo "Verifying build env (URLs from .env.production)..."
	@cd $(ROOT) && cd $(FRONTEND_DIR) && node scripts/verify-build-env.js
	@echo "Deploying to Static Web App $(SWA_NAME)..."
	@cd $(ROOT) && SWA_TOKEN=$$(az staticwebapp secrets list --name $(SWA_NAME) --resource-group $(SWA_RESOURCE_GROUP) --query "properties.apiKey" -o tsv) && \
		npx -y @azure/static-web-apps-cli deploy $(FRONTEND_DIR)/dist --deployment-token "$$SWA_TOKEN" --env production

deploy-swa: deploy-frontend ## Alias for deploy-frontend

deploy-all: ## Deploy API, worker, MCP, and frontend
	@$(MAKE) -C "$(ROOT)" deploy-backend
	@$(MAKE) -C "$(ROOT)" deploy-worker
	@$(MAKE) -C "$(ROOT)" deploy-mcp
	@$(MAKE) -C "$(ROOT)" deploy-frontend

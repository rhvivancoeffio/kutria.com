.PHONY: help build clean restore \
	run-apphost run-apphost-postgres run-apphost-sqlserver run-backend run-api run-worker \
	run-frontend frontend-install frontend-preview frontend-dist \
	run-compose compose-up compose-down compose-logs \
	migrate-add migrate-add-tenant migrate-update migrate-reset migrate-reset-postgres \
	postgres-clean postgres-volume-rm \
	setup-secrets-apphost setup-env

ROOT := $(dir $(abspath $(firstword $(MAKEFILE_LIST))))

SOLUTION = Commerce.sln
APPHOST = aspire/AppHost/Commerce.AppHost.csproj
API_PROJECT = src/Commerce.Api/Commerce.Api.csproj
WORKER_PROJECT = src/Commerce.Worker/Commerce.Worker.csproj
PERSISTENCE_PROJECT = src/Commerce.Infrastructure.Persistence/Commerce.Infrastructure.Persistence.csproj
PERSISTENCE_DIR = src/Commerce.Infrastructure.Persistence
FRONTEND_DIR = src/Commerce.Web

POSTGRES_CONN ?= Host=localhost;Port=5433;Database=commerce;Username=commerce;Password=commerce
SQL_SERVER_CONN ?= Server=localhost,1433;Database=commerce;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
REDIS_CONN ?= localhost:6380
DB_PROVIDER ?= Postgres

help: ## Show available commands
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-24s %s\n", $$1, $$2}'

# ─── Build ───────────────────────────────────────────────────────────────────

restore: ## Restore NuGet packages
	dotnet restore $(SOLUTION)

build: ## Build solution
	dotnet build $(SOLUTION)

clean: ## Clean binaries
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

frontend-dist: ## Build frontend for production (dist/)
	@cd "$(ROOT)" && rm -rf $(FRONTEND_DIR)/dist && cd "$(FRONTEND_DIR)" && npm ci && npm run build
	@cd "$(ROOT)$(FRONTEND_DIR)" && node scripts/verify-build-env.js

# ─── Docker Compose ──────────────────────────────────────────────────────────

compose-up: ## docker compose up --build (detached)
	@cd "$(ROOT)" && (test -f .env || cp .env.example .env) && docker compose up --build -d

compose-down: ## docker compose down
	@cd "$(ROOT)" && docker compose down

compose-logs: ## Follow docker compose logs
	@cd "$(ROOT)" && docker compose logs -f

run-compose: compose-up ## Alias for compose-up

# ─── Migrations ──────────────────────────────────────────────────────────────

migrate-add: ## Add App migration for SqlServer + Postgres (MIGRATION_NAME=Name)
	@if [ -z "$(MIGRATION_NAME)" ]; then echo "Error: Specify MIGRATION_NAME"; exit 1; fi
	@echo "Creating App migration $(MIGRATION_NAME) for SqlServer..."
	@Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context SqlServerCommerceDbContext \
		--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) \
		--output-dir Migrations/App/SqlServer -- /p:DatabaseProvider=SqlServer
	@echo "Creating App migration $(MIGRATION_NAME) for Postgres..."
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context PostgresCommerceDbContext \
		--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) \
		--output-dir Migrations/App/Postgres -- /p:DatabaseProvider=Postgres

migrate-add-tenant: ## Add TenantStore migration (MIGRATION_NAME=Name)
	@if [ -z "$(MIGRATION_NAME)" ]; then echo "Error: Specify MIGRATION_NAME"; exit 1; fi
	@echo "Creating TenantStore migration $(MIGRATION_NAME) for SqlServer..."
	@Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context SqlServerCommerceTenantStoreDbContext \
		--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) \
		--output-dir Migrations/TenantStore/SqlServer -- /p:DatabaseProvider=SqlServer
	@echo "Creating TenantStore migration $(MIGRATION_NAME) for Postgres..."
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add $(MIGRATION_NAME) \
		--context PostgresCommerceTenantStoreDbContext \
		--project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) \
		--output-dir Migrations/TenantStore/Postgres -- /p:DatabaseProvider=Postgres

migrate-update: ## Apply migrations (DB_PROVIDER=Postgres|SqlServer|Both)
	@if [ "$(DB_PROVIDER)" = "Both" ]; then \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceTenantStoreDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceTenantStoreDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
	elif [ "$(DB_PROVIDER)" = "SqlServer" ]; then \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceTenantStoreDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
		Database__Provider=SqlServer ConnectionStrings__Commerce="$(SQL_SERVER_CONN)" \
			dotnet ef database update --context SqlServerCommerceDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=SqlServer; \
	else \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceTenantStoreDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
		DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
			dotnet ef database update --context PostgresCommerceDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --verbose -- /p:DatabaseProvider=Postgres; \
	fi

migrate-reset: ## Wipe migrations, recreate Initial*
	@echo "Cleaning Postgres volumes/containers..."
	@$(MAKE) postgres-clean 2>/dev/null || true
	@rm -rf $(PERSISTENCE_DIR)/Migrations
	@dotnet restore $(SOLUTION)
	@$(MAKE) migrate-add-tenant MIGRATION_NAME=InitialTenantStore
	@$(MAKE) migrate-add MIGRATION_NAME=InitialApp
	@dotnet build $(SOLUTION)

migrate-reset-postgres: ## Remove Postgres migrations only
	@rm -rf $(PERSISTENCE_DIR)/Migrations/App/Postgres $(PERSISTENCE_DIR)/Migrations/TenantStore/Postgres
	@dotnet restore $(SOLUTION)
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add InitialTenantStore --context PostgresCommerceTenantStoreDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --output-dir Migrations/TenantStore/Postgres -- /p:DatabaseProvider=Postgres
	@DATABASE_PROVIDER=Postgres Database__Provider=PostgreSQL ConnectionStrings__Commerce="$(POSTGRES_CONN)" \
		dotnet ef migrations add InitialApp --context PostgresCommerceDbContext --project $(PERSISTENCE_PROJECT) --startup-project $(API_PROJECT) --output-dir Migrations/App/Postgres -- /p:DatabaseProvider=Postgres

postgres-clean: ## Stop Postgres containers + remove volumes
	@chmod +x "$(ROOT)scripts/postgres-clean.sh"
	@"$(ROOT)scripts/postgres-clean.sh"

postgres-volume-rm: postgres-clean ## Alias

# ─── Secrets / env ────────────────────────────────────────────────────────────

setup-env: ## Copy .env.example → .env if missing
	@cd "$(ROOT)" && if [ ! -f .env ]; then cp .env.example .env && echo "Created .env"; else echo ".env exists"; fi

setup-secrets-apphost: ## Configure Aspire AppHost user secrets
	@cd "$(ROOT)" && if [ -f aspire/AppHost/set-user-secrets.sh ]; then bash aspire/AppHost/set-user-secrets.sh; else echo "set-user-secrets.sh not found."; exit 1; fi
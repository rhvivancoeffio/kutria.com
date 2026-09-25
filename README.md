# Commerce.*

Open-source multi-tenant system for solo business owners. Well-Architected, Zero Trust, Scale to Zero.

## Architecture — Well-Architected Framework

**5 pillars aplicados:**

- **Cost Optimization (Scale to Zero):** SQL Serverless GP_Gen5 0.5-2 vCores auto-pause 60m, Storage LRS, ACR Basic, Search FREE (1 por sub, reusado), Container Apps min 0, sin Private Endpoints ($26/mes ahorro). Todo taggeado `scale-to-zero=true`.
- **Security (Zero Trust):** Key Vault RBAC, secrets nunca en `.env` (se generan en KV: `sql-admin-password`, `storage-connection-string`, `sql-connection-string`), MI + secretref en Container Apps con **URL completa** `https://kv-*.vault.azure.net/secrets/*`, `USER 1000` non-root en Dockerfiles, Entra ID admin en SQL.
- **Reliability:** Healthchecks en compose, `WaitFor` en Aspire, RabbitMQ + Redis con retries, Azurite local = Azure Storage en prod, **EF `EnableRetryOnFailure(5, 10s)`** para SQL Serverless auto-pause 40613.
- **Performance:** Qdrant local / Azure Search en prod switch por `VECTOR_PROVIDER`, Redis cache, Queue provider switch `RabbitMQ` local / `AzureQueue` prod.
- **Operational Excellence:** `DB_PROVIDER` env único para Postgres/SQL (Aspire logic: `if(useSqlServer) else postgres`), `VECTOR_PROVIDER` env, `.env.example.*` separados, `make help` para todo.

## Frontend (Commerce.Web)

Cloned from **template-project** `TemplateProject.Frontend`, with tenancy replaced by **Finbuckle.MultiTenant**:

- Path: `http://localhost:3000/t/tenant1` (also tenant2 / tenant3)
- Production: frontend `https://kutria.com`, API `https://api.kutria.com/t/{identifier}/...` y `https://kutria.com/api/...` vía Front Door
- `X-Tenant` remains a fallback for the embed.
- Store: `src/stores/tenantStore.js` — not Account/Workspace

See `.cursor/skills/finbuckle-tenancy/SKILL.md`.

## Quick start — Aspire (recomendado dev)

Aspire es la verdad: levanta solo 1 DB a la vez via `DB_PROVIDER` env (igual que `AppHost.cs`).

```bash
make help
make setup-env
cp .env.example.local .env   # o .env.example

# Default: PostgreSQL + Qdrant + RabbitMQ + Redis + Azurite
make run-apphost          # Aspire: Postgres + Redis + Api + Worker + Azurite
make run-frontend         # Vite (not in AppHost — same as template/builder)

# SQL Server local en Aspire:
DB_PROVIDER=SqlServer make run-apphost
```

- Web: http://localhost:3000/t/tenant1
- API: http://localhost:5050
- MCP: http://localhost:5055
- Postgres (host): `localhost:5433` (container 5432)
- Redis (host): `localhost:6380` (container 6379)
- RabbitMQ: `localhost:15683` / `5693`
- Qdrant: `localhost:6333`
- Azurite: `10000-10002`

Ports offset from sibling GravityAI stacks (e.g. team-ecommerce keeps `:5432` / `:6379`).

## Quick start — Docker Compose (V4 Aspire-aligned)

Fix V4: nunca levanta Postgres y SQL al mismo tiempo. Usa profiles mutuamente exclusivos como Aspire.

**Estructura:**
- `commerce.db` -> `profiles: ["postgres"]` (default)
- `commerce.sql` -> `profiles: ["sqlserver"]`
- `commerce.azurite` siempre (como AppHost.cs)
- `x-common-env` anchor para settings uniformes Api/Worker/Mcp/Shop (clona appsettings.json global)

**Matar todo:**
```bash
docker compose -f docker-compose.local.yaml --profile postgres --profile sqlserver --profile azure --profile brain down -v --remove-orphans
```

**PostgreSQL (default):**
```bash
cp .env.example.local .env
mkdir -p data/brains-raw
docker compose -f docker-compose.local.yaml --profile postgres up -d --build
# o solo infra + apps
docker compose -f docker-compose.local.yaml --profile postgres up commerce.api commerce.worker commerce.web
```

**SQL Server:**
```bash
# .env: DB_PROVIDER=SqlServer, COMPOSE_PROFILES=sqlserver
# ConnectionStrings__Commerce=Server=commerce.sql,1433;Database=commerce;User Id=sa;Password=Your_password123;TrustServerCertificate=True;

# Mac M1/M2/M3: cambiar image en compose a mcr.microsoft.com/azure-sql-edge
docker compose -f docker-compose.local.yaml --profile sqlserver up -d --build
```

**Opción B brains-raw (fix Permission denied):**
Compose usa bind mount `./data/brains-raw:/var/commerce/brains-raw` en vez de volumen nombrado `brains-raw`. Así Aspire y Docker comparten `./data/brains-raw` con tu usuario y no falla con `USER 1000` non-root.

```bash
mkdir -p data/brains-raw
# Si tenías volumen viejo con root:
docker compose -f docker-compose.local.yaml down -v
```

## Azure — Zero Trust + Scale to Zero V3

### 1. Provision — No guarda passwords en `.env`

```bash
make provision-infra
# Crea:
# - Key Vault kutria-kv-prod-xxxx (RBAC)
#   - sql-admin-password (generado seguro si no existe)
#   - storage-connection-string
#   - sql-connection-string (Server=tcp:sql-kutria-prod.database.windows.net...)
# - Storage LRS (sin PE, $26/mes ahorro)
# - SQL Serverless GP_Gen5 0.5-2 vCores auto-pause 60m + Entra ID admin = tu usuario
# - Search FREE (1 por sub, reusado si existe)
# - ACR Basic, App Insights, CA Env (sin VNet), Front Door Standard
# Genera: .scale-zero.env (RAND) + .scale-zero-names.env (nombres)
```

**Variables clave `deploy/Makefile`:**
```makefile
RG=Gravity-Agentic
ACR=acrkutriaprod
CA_ENV=cae-kutria-prod
KV_NAME=kv-kutria-prod
SQL_SERVER=sql-kutria-prod
SQL_DB=commerce
ACR_IDENTITY=mi-kutria-acr-pull
SUB_ID=98c1a4e2-a4a1-4f0c-8807-60d8336a086f
KV_URI=https://kv-kutria-prod.vault.azure.net
IDENTITY_FULL_ID=/subscriptions/.../userAssignedIdentities/mi-kutria-acr-pull
```

### 2. Fix obligatorio para KeyVault en Container Apps (error que tuvimos 2 días)

Azure CLI antiguo requiere URL completa, NO `kv-name/secret`:

```bash
# MAL - da ContainerAppSecretKeyVaultUrlInvalid
sql-connection-string=keyvaultref:kv-kutria-prod/sql-connection-string

# BIEN - formato que pide Azure
sql-connection-string=keyvaultref:https://kv-kutria-prod.vault.azure.net/secrets/sql-connection-string,identityref:/subscriptions/98c1a4e2-a4a1-4f0c-8807-60d8336a086f/resourcegroups/Gravity-Agentic/providers/Microsoft.ManagedIdentity/userAssignedIdentities/mi-kutria-acr-pull
```

El `Makefile` ya está corregido en `api:`, `mcp:`, `workers:`:

```makefile
KV_URI=https://$(KV_NAME).vault.azure.net
IDENTITY_FULL_ID=/subscriptions/$(SUB_ID)/resourcegroups/$(RG)/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$(ACR_IDENTITY)
SECRETS="$$SECRETS $$sec=keyvaultref:$(KV_URI)/secrets/$$sec,identityref:$(IDENTITY_FULL_ID)"
```

### 3. Fix obligatorio para SQL Serverless 40613 (Database not currently available)

Serverless se pausa a los 60m. Sin retry, EF crashea. Agregar en `Commerce.Infrastructure.Persistence`:

```csharp
// src/Commerce.Infrastructure.Persistence/DependencyInjection.cs
var options = new DbContextOptionsBuilder<SqlServerCommerceDbContext>()
    .UseSqlServer(connectionString, sql =>
    {
        sql.MigrationsAssembly(typeof(SqlServerCommerceDbContext).Assembly.FullName);
        sql.MigrationsHistoryTable("__EFMigrationsHistory_App");
        // FIX 40613 - Serverless auto-pause
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        sql.CommandTimeout(60);
    })
    .Options;
```

### 4. Deploy

**Primera vez — todo:**
```bash
make -C deploy deploy-core TAG=final-$(date +%s)
# Hace: core-infra + api + web + mcp + workers + front-door + dns + advisor
```

**Solo aplicación (día a día):**
```bash
# Backend API
make -C deploy api TAG=api-$(date +%s)

# Frontend WEB
make -C deploy web TAG=web-$(date +%s)

# MCP
make -C deploy mcp TAG=mcp-$(date +%s)

# Workers (image + data)
make -C deploy workers TAG=worker-$(date +%s)

# Todo el código sin tocar infra
make -C deploy api web mcp workers TAG=app-$(date +%s)
```

**Solo infra sin código:**
```bash
make -C deploy core-infra
make -C deploy infra-front-origins infra-front-routes infra-front-routes-associate infra-dns-wildcard
```

**Verificar:**
```bash
# Directo al Container App (debe dar 200, usa GET no HEAD)
curl https://ca-api-core.lemonsea-fee6cb0b.eastus.azurecontainerapps.io/health
curl -i https://ca-api-core.lemonsea-fee6cb0b.eastus.azurecontainerapps.io/health

# Via Front Door default domain
curl https://fd-kutria-std-endpoint-crc7ccawa9dghzag.a03.azurefd.net/api/health

# Via custom domain (cuando DNS propague)
curl https://kutria.com/api/health
dig kutria.com @8.8.8.8 +short
az network dns zone show -g Gravity-Agentic -n kutria.com --query nameServers -o tsv
```

## Common Make targets — Detallado

| Target | Purpose | Cuando usarlo |
|--------|---------|---------------|
| `help` | Lista todos los targets | Siempre |
| `setup-env` | Crea `.env` desde `.env.example.local` | Primera vez local |
| `run-apphost` | Aspire: Postgres/Qdrant + Redis + Rabbit + Api + Worker + Azurite (1 DB a la vez) | Dev local recomendado |
| `run-frontend` | Vite UI `localhost:3000/t/{tenant}` | Frontend separado |
| `compose-up` / `compose-down` | Docker Compose V4 profiles postgres/sqlserver | Alternativa a Aspire |
| `clean-docker-local` | Mata todo + volúmenes root (fix brains-raw Permission denied) | Cuando `USER 1000` da Permission denied |
| `migrate-add NAME=MiMigracion` | EF Add Migration App + TenantStore para **SqlServer + Postgres** | Nuevo modelo |
| `migrate-update` | EF Database Update ambos providers | Aplicar migraciones local |
| `migrate-reset` | Drop + Update + Seed | Reset DB local |
| `setup-secrets-apphost` | Aspire user secrets (postgres pwd = commerce) | Primera vez Aspire |
| `provision-infra` / `core-infra` | V3 Zero Trust: KV + Storage LRS + SQL Serverless + Search FREE + ACR + CA Env + Front Door | Primera vez Azure |
| `infra-qdrant` | Qdrant Container App `ca-qdrant-core:6333` | Si no existe Qdrant |
| `api` | Build ACR `kutria/api:TAG` + `secret set` con URL completa KV + update CA `ca-api-core` min 0 max 10 | Cambio en Commerce.Api |
| `web` | Build ACR `kutria/web:TAG` + update CA `ca-web-core` | Cambio en Commerce.Web |
| `mcp` | Build ACR `kutria/mcp:TAG` + secret set + update CA `ca-mcp-core` | Cambio en Commerce.Mcp |
| `workers` | Build ACR `kutria/worker:TAG` + update CA `ca-worker-image` (0-20) + `ca-worker-data` (0-10) | Cambio en Commerce.Worker |
| `deploy-core` | `core-infra + api + web + mcp + workers + infra-front-* + infra-dns-*` | Deploy completo prod |
| `deploy-backend` / `deploy-worker` / `deploy-mcp` / `deploy-shop` / `deploy-frontend` / `deploy-all` | Aliases legacy -> api/web/mcp/workers | Compatibilidad |
| `deploy-settings` | Push App Settings desde KV (secretref) | Cambias env vars sin rebuild |
| `infra-front` | Crea Front Door Standard `fd-kutria-std` + endpoint | Primera vez Front Door |
| `infra-front-domains` | Custom domains `kutria.com` + `*.kutria.com` | Primera vez dominios |
| `infra-front-origins` | Origin groups `og-api-core`, `og-web-core`, `og-mcp-core` + origins a CA FQDNs | Conectar Front Door a CAs |
| `infra-front-routes` | Routes `/api/*` -> og-api-core, `/mcp/*` -> og-mcp-core, `/*` -> og-web-core | Ruteo Front Door |
| `infra-front-routes-associate` | Asocia custom domains a routes vía `az rest` | Fix para que `kutria.com` funcione |
| `infra-dns-validation` | Crea TXT `_dnsauth` con token de validación Front Door | Validación dominio |
| `infra-dns-wildcard` | CNAME `*` y `www` -> `fd-*.azurefd.net` | DNS final |
| `ssl-shop-letsencrypt` | Let's Encrypt wildcard *.kutria.com (90 días) | Si no usas Front Door managed cert |

## Env samples

**Local Aspire / Compose** `.env.example.local`:
```env
DB_PROVIDER=PostgreSQL
VECTOR_PROVIDER=Qdrant
COMPOSE_PROFILES=postgres
ConnectionStrings__Commerce=Host=commerce.db;Port=5432;Database=commerce;Username=postgres;Password=commerce
REDIS_CONNECTION=commerce.redis:6379
RABBITMQ_HOST=commerce.rabbitmq
```

**Local SQL Server** `.env`:
```env
DB_PROVIDER=SqlServer
COMPOSE_PROFILES=sqlserver
ConnectionStrings__Commerce=Server=commerce.sql,1433;Database=commerce;User Id=sa;Password=Your_password123;TrustServerCertificate=True;
```

**Azure prod** `.env.example.azure`:
```env
WEBAPP_RESOURCE_GROUP=Gravity-Agentic
PREFIX=kutria
ENV_NAME=prod
SQL_ADMIN_PASSWORD= # vacío - se genera en KV
# Todo lo demás viene de KV via secretref:
# ConnectionStrings__DefaultConnection=secretref:sql-connection-string
# Queue__ConnectionString=secretref:storage-connection-string
# ASPNETCORE_ENVIRONMENT=Production
# Database__Provider=SqlServer
# Queue__Provider=AzureQueue
# Vector__Provider=Qdrant (o AzureSearch)
# Qdrant__Endpoint=http://ca-qdrant-core:6333
```

Ver `.env.example`, `.env.example.local`, `.env.example.azure`.

## Aspire secrets

```bash
cp aspire/AppHost/set-user-secrets.example.sh aspire/AppHost/set-user-secrets.sh
make setup-secrets-apphost
# Setea: postgres pwd = commerce, redis, rabbitmq, etc en dotnet user-secrets
```

## Troubleshooting — Errores que tuvimos

### 1. `ContainerAppSecretKeyVaultUrlInvalid`
**Causa:** `keyvaultref:kv-name/secret` formato viejo.
**Fix:** Usar URL completa `https://kv-name.vault.azure.net/secrets/secret` + `identityref` con resource ID completo. Ya está en `deploy/Makefile`.

### 2. `Database 'commerce' is not currently available. Error 40613`
**Causa:** SQL Serverless GP_Gen5 auto-pausa a los 60m. Primera query falla.
**Fix:** `EnableRetryOnFailure(5, 10s)` + `CommandTimeout(60)` en `UseSqlServer`. Ver sección 3 arriba.
**Wake manual:** `az sql db show -n commerce --server sql-kutria-prod -g Gravity-Agentic --query status` + `az sql db update ... --auto-pause-delay 120` o simplemente reintentar deploy cuando esté `Online`.

### 3. `curl -I /health` da `405 Allow: GET`
**Causa:** `-I` hace HEAD, tu API solo acepta GET.
**Fix:** `curl https://.../health` sin `-I` o `curl -i`.

### 4. `Could not resolve host: kutria.com`
**Causa:** DNS zone no delegada o CNAME `*` no creado.
**Fix:** `make -C deploy infra-dns-wildcard` + verificar `dig kutria.com @8.8.8.8`.

### 5. `brains-raw Permission denied` con `USER 1000`
**Causa:** Volumen nombrado creado como root, contenedor corre como 1000.
**Fix:** Usar bind mount `./data/brains-raw:/var/commerce/brains-raw` + `mkdir -p data/brains-raw` + `docker compose down -v`.

## License

MIT

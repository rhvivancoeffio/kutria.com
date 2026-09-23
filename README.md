# Commerce.*

Open-source multi-tenant system for solo business owners.

## Frontend (Commerce.Web)

Cloned from **template-project** `TemplateProject.Frontend`, with tenancy replaced by **Finbuckle.MultiTenant**:

- Path: `http://localhost:3000/t/tenant1` (also tenant2 / tenant3)
- Production: frontend `https://kutria.com`, API `https://api.kutria.com/t/{identifier}/...`
- `X-Tenant` remains a fallback for the embed.
- Store: `src/stores/tenantStore.js` — not Account/Workspace

See `.cursor/skills/finbuckle-tenancy/SKILL.md`.

## Quick start

```bash
make help
make setup-env
make run-apphost          # Aspire: Postgres + Redis + Api + Worker
make run-frontend         # Vite (not in AppHost — same as template/builder)
# or
make compose-up           # docker compose
```

- Web: http://localhost:3000/t/tenant1
- API: http://localhost:5050
- Postgres (host): `localhost:5433` (container still uses 5432)
- Redis (host): `localhost:6380` (container still uses 6379)

Ports are offset from sibling GravityAI stacks (e.g. team-ecommerce keeps `:5432` / `:6379`).

## Common Make targets

| Target | Purpose |
|--------|---------|
| `run-apphost` | Aspire: Postgres + Redis + Api + Worker |
| `run-frontend` | Vite UI (`localhost:3000/t/{tenant}`) |
| `compose-up` / `compose-down` | Docker Compose (incluye web nginx) |
| `run-api` / `run-frontend` | API or Vite alone |
| `migrate-add` / `migrate-add-tenant` / `migrate-update` / `migrate-reset` | EF App + TenantStore for **SqlServer + Postgres** (same as channel) |
| `setup-secrets-apphost` | Aspire user secrets |
| `provision-infra` | Create Azure Storage + SQL + Search + Web Apps (API/Worker/MCP/Shop) + SWA |
| `deploy-backend` / `deploy-worker` / `deploy-mcp` / `deploy-shop` / `deploy-frontend` / `deploy-all` | Azure deploy |
| `deploy-settings` / `deploy-settings-api` / `deploy-settings-worker` / `deploy-settings-mcp` | Push App Settings (OpenAI/DB from env) |
| `ssl-shop-dns` | Print DNS records for `*.kutria.com` + bind hostname on shop Web App |
| `ssl-shop-letsencrypt` | Interactive: renew Let's Encrypt wildcard + upload (asks email/password) |
| `ssl-shop-renew` | Upload existing PFX / self-signed generate (manual) |

Shop SSL (gratis, ~90 días). Cuando expire:

```bash
make ssl-shop-letsencrypt
# o: ./scripts/ssl-shop-letsencrypt.sh
```

App Settings (`make deploy-settings`) read secrets from the environment — see `.env.example`. Storage queue/tables connection is resolved from `STORAGE_ACCOUNT_NAME`. Default `DATABASE_PROVIDER=SqlServer` (set `SQL_ADMIN_PASSWORD` or `ConnectionStrings__Commerce` with a `Server=` string). Use `DATABASE_PROVIDER=PostgreSQL` + `ConnectionStrings__Commerce` for Postgres.

`make provision-infra` creates SQL Server `sql-gravity-commerce` + DB `commerce` (Basic 5 DTU). Set `SQL_ADMIN_PASSWORD` in the env when the server does not exist yet.

## Aspire secrets

Postgres password lives in Aspire user secrets (default `commerce`). Copy the example and apply:

```bash
cp aspire/AppHost/set-user-secrets.example.sh aspire/AppHost/set-user-secrets.sh
make setup-secrets-apphost
```

`set-user-secrets.sh` is gitignored — never commit real keys.

## License

MIT

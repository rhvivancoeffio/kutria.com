RG ?= Gravity-Agentic
ACR ?= acrkutriaprod
CA_ENV ?= cae-kutria-prod
LOCATION ?= eastus
FRONT ?= fd-kutria-std
FRONT_SKU ?= Standard_AzureFrontDoor
FRONT_ENDPOINT ?= fd-kutria-std-endpoint
STORAGE ?= stkutriaagentic
KV_NAME ?= kv-kutria-prod
TAG ?= $(shell git rev-parse --short HEAD)
SQL_SERVER ?= sql-kutria-prod
SQL_DB ?= commerce
SQL_ADMIN_USER ?= commerceadmin
ACR_IDENTITY ?= mi-kutria-acr-pull
DNS_ZONE ?= kutria.com
SUB_ID ?= 98c1a4e2-a4a1-4f0c-8807-60d8336a086f

API_IMG = $(ACR).azurecr.io/kutria/api:$(TAG)
WEB_IMG = $(ACR).azurecr.io/kutria/web:$(TAG)
WORKER_IMG = $(ACR).azurecr.io/kutria/worker:$(TAG)
MCP_IMG = $(ACR).azurecr.io/kutria/mcp:$(TAG)

KV_URI = https://$(KV_NAME).vault.azure.net
IDENTITY_FULL_ID = /subscriptions/$(SUB_ID)/resourcegroups/$(RG)/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$(ACR_IDENTITY)

infra-rg:
	@az group show -n $(RG) --only-show-errors 2>/dev/null || az group create -n $(RG) -l $(LOCATION) -o none
infra-acr: infra-rg
	@az acr show -n $(ACR) -g $(RG) --only-show-errors 2>/dev/null || az acr create -n $(ACR) -g $(RG) -l $(LOCATION) --sku Basic -o none
infra-env: infra-rg
	@az containerapp env show -n $(CA_ENV) -g $(RG) --only-show-errors 2>/dev/null || az containerapp env create -n $(CA_ENV) -g $(RG) -l $(LOCATION) -o none
infra-storage: infra-rg
	@az storage account show -n $(STORAGE) -g $(RG) --only-show-errors 2>/dev/null || az storage account create -n $(STORAGE) -g $(RG) -l $(LOCATION) --sku Standard_LRS -o none
	@az storage queue create -n data-ingest --account-name $(STORAGE) -o none 2>/dev/null || true
	@az storage queue create -n catalog-image-vector --account-name $(STORAGE) -o none 2>/dev/null || true
	@az storage queue create -n catalog-vector --account-name $(STORAGE) -o none 2>/dev/null || true
	@CONN=$$(az storage account show-connection-string -n $(STORAGE) -g $(RG) --query connectionString -o tsv 2>/dev/null); \
	if [ -n "$$CONN" ]; then az keyvault secret set --vault-name $(KV_NAME) -n storage-connection-string --value "$$CONN" -o none 2>/dev/null || true; fi
infra-keyvault: infra-rg
	@az keyvault show -n $(KV_NAME) -g $(RG) --only-show-errors 2>/dev/null || az keyvault create -n $(KV_NAME) -g $(RG) -l $(LOCATION) --enable-rbac-authorization true -o none
infra-keyvault-rbac: infra-keyvault
	@KV_ID=$$(az keyvault show -n $(KV_NAME) -g $(RG) --query id -o tsv); \
	CURRENT_OID=$$(az ad signed-in-user show --query id -o tsv 2>/dev/null); \
	az role assignment show --assignee $$CURRENT_OID --scope $$KV_ID --role "Key Vault Secrets Officer" --only-show-errors 2>/dev/null || az role assignment create --assignee $$CURRENT_OID --role "Key Vault Secrets Officer" --scope $$KV_ID -o none 2>/dev/null || true; sleep 10
infra-identity: infra-rg infra-acr infra-keyvault
	@az identity show -n $(ACR_IDENTITY) -g $(RG) --only-show-errors 2>/dev/null || az identity create -n $(ACR_IDENTITY) -g $(RG) -l $(LOCATION) -o none
	@ACR_ID=$$(az acr show -n $(ACR) -g $(RG) --query id -o tsv); \
	PRINCIPAL=$$(az identity show -n $(ACR_IDENTITY) -g $(RG) --query principalId -o tsv); \
	KV_ID=$$(az keyvault show -n $(KV_NAME) -g $(RG) --query id -o tsv); \
	az role assignment show --assignee $$PRINCIPAL --scope $$ACR_ID --role AcrPull --only-show-errors 2>/dev/null || az role assignment create --assignee $$PRINCIPAL --role AcrPull --scope $$ACR_ID -o none 2>/dev/null || true; \
	az role assignment show --assignee $$PRINCIPAL --scope $$KV_ID --role "Key Vault Secrets User" --only-show-errors 2>/dev/null || az role assignment create --assignee $$PRINCIPAL --role "Key Vault Secrets User" --scope $$KV_ID -o none 2>/dev/null || true; sleep 5
infra-sql: infra-rg infra-keyvault infra-keyvault-rbac
	@SQL_PWD=$$(az keyvault secret show --vault-name $(KV_NAME) -n sql-admin-password --query value -o tsv 2>/dev/null); \
	if [ -z "$$SQL_PWD" ]; then SQL_PWD=$$(openssl rand -base64 24 | tr -d '\n/+' | cut -c1-20)Aa1!; az keyvault secret set --vault-name $(KV_NAME) -n sql-admin-password --value "$$SQL_PWD" -o none; sleep 5; fi; \
	SQL_PWD=$$(az keyvault secret show --vault-name $(KV_NAME) -n sql-admin-password --query value -o tsv); \
	az sql server show -n $(SQL_SERVER) -g $(RG) --only-show-errors 2>/dev/null || az sql server create -n $(SQL_SERVER) -g $(RG) -l $(LOCATION) --admin-user $(SQL_ADMIN_USER) --admin-password "$$SQL_PWD" -o none; \
	az sql server firewall-rule show -n AllowAzureServices --server $(SQL_SERVER) -g $(RG) --only-show-errors 2>/dev/null || az sql server firewall-rule create -n AllowAzureServices --server $(SQL_SERVER) -g $(RG) --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none; \
	az sql db show -n $(SQL_DB) --server $(SQL_SERVER) -g $(RG) --only-show-errors 2>/dev/null || az sql db create -n $(SQL_DB) --server $(SQL_SERVER) -g $(RG) --edition GeneralPurpose --family Gen5 --capacity 1 --compute-model Serverless --auto-pause-delay 60 --min-capacity 0.5 -o none; \
	CONN="Server=tcp:$(SQL_SERVER).database.windows.net,1433;Initial Catalog=$(SQL_DB);Persist Security Info=False;User ID=$(SQL_ADMIN_USER);Password=$$SQL_PWD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; \
	az keyvault secret set --vault-name $(KV_NAME) -n sql-connection-string --value "$$CONN" -o none; \
	az keyvault secret set --vault-name $(KV_NAME) -n core-db-connection --value "$$CONN" -o none
infra-kv-secrets: infra-keyvault infra-identity
	@echo ">> KV $(KV_NAME) - secrets disponibles:"
	@for sec in sql-connection-string storage-connection-string; do \
		if az keyvault secret show --vault-name $(KV_NAME) -n $$sec >/dev/null 2>&1; then echo "   ✅ $$sec"; else echo "   ⚠️  $$sec falta"; fi; done
infra-qdrant: infra-rg infra-env
	@az containerapp show -n ca-qdrant-core -g $(RG) --only-show-errors 2>/dev/null || az containerapp create -n ca-qdrant-core -g $(RG) --environment $(CA_ENV) --image qdrant/qdrant:latest --ingress external --target-port 6333 --min-replicas 1 --max-replicas 1 -o none
infra-front:
	@az afd profile show --profile-name $(FRONT) -g $(RG) --only-show-errors 2>/dev/null || az afd profile create --profile-name $(FRONT) -g $(RG) --sku $(FRONT_SKU) -o none
	@az afd endpoint show --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --only-show-errors 2>/dev/null || az afd endpoint create --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) -o none
infra-front-domains: infra-front
	@az afd custom-domain show --profile-name $(FRONT) -g $(RG) --custom-domain-name kutria-com --only-show-errors 2>/dev/null || az afd custom-domain create --profile-name $(FRONT) -g $(RG) --custom-domain-name kutria-com --host-name kutria.com -o none
	@az afd custom-domain show --profile-name $(FRONT) -g $(RG) --custom-domain-name wildcard-kutria-com --only-show-errors 2>/dev/null || az afd custom-domain create --profile-name $(FRONT) -g $(RG) --custom-domain-name wildcard-kutria-com --host-name "*.kutria.com" -o none
infra-front-origins: infra-front infra-env
	@az afd origin-group show --profile-name $(FRONT) -g $(RG) --origin-group-name og-api-core --only-show-errors 2>/dev/null || az afd origin-group create --profile-name $(FRONT) -g $(RG) --origin-group-name og-api-core --probe-request-type GET --probe-protocol Https --probe-path /health --sample-size 4 --successful-samples-required 3 --additional-latency-in-milliseconds 50 -o none
	@az afd origin-group show --profile-name $(FRONT) -g $(RG) --origin-group-name og-web-core --only-show-errors 2>/dev/null || az afd origin-group create --profile-name $(FRONT) -g $(RG) --origin-group-name og-web-core --probe-request-type GET --probe-protocol Https --probe-path / --sample-size 4 --successful-samples-required 3 --additional-latency-in-milliseconds 50 -o none
	@az afd origin-group show --profile-name $(FRONT) -g $(RG) --origin-group-name og-mcp-core --only-show-errors 2>/dev/null || az afd origin-group create --profile-name $(FRONT) -g $(RG) --origin-group-name og-mcp-core --probe-request-type GET --probe-protocol Https --probe-path /health --sample-size 4 --successful-samples-required 3 --additional-latency-in-milliseconds 50 -o none
	@API_FQDN=$$(az containerapp show -n ca-api-core -g $(RG) --query properties.configuration.ingress.fqdn -o tsv 2>/dev/null); if [ -n "$$API_FQDN" ]; then az afd origin show --profile-name $(FRONT) -g $(RG) --origin-group-name og-api-core --origin-name ca-api-core --only-show-errors 2>/dev/null || az afd origin create --profile-name $(FRONT) -g $(RG) --origin-group-name og-api-core --origin-name ca-api-core --host-name $$API_FQDN --origin-host-header $$API_FQDN --priority 1 --weight 1000 --http-port 80 --https-port 443 -o none; fi
	@WEB_FQDN=$$(az containerapp show -n ca-web-core -g $(RG) --query properties.configuration.ingress.fqdn -o tsv 2>/dev/null); if [ -n "$$WEB_FQDN" ]; then az afd origin show --profile-name $(FRONT) -g $(RG) --origin-group-name og-web-core --origin-name ca-web-core --only-show-errors 2>/dev/null || az afd origin create --profile-name $(FRONT) -g $(RG) --origin-group-name og-web-core --origin-name ca-web-core --host-name $$WEB_FQDN --origin-host-header $$WEB_FQDN --priority 1 --weight 1000 --http-port 80 --https-port 443 -o none; fi
	@MCP_FQDN=$$(az containerapp show -n ca-mcp-core -g $(RG) --query properties.configuration.ingress.fqdn -o tsv 2>/dev/null); if [ -n "$$MCP_FQDN" ]; then az afd origin show --profile-name $(FRONT) -g $(RG) --origin-group-name og-mcp-core --origin-name ca-mcp-core --only-show-errors 2>/dev/null || az afd origin create --profile-name $(FRONT) -g $(RG) --origin-group-name og-mcp-core --origin-name ca-mcp-core --host-name $$MCP_FQDN --origin-host-header $$MCP_FQDN --priority 1 --weight 1000 --http-port 80 --https-port 443 -o none; fi
infra-front-routes: infra-front-origins infra-front-domains
	@az afd route show --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --route-name route-api --only-show-errors 2>/dev/null || az afd route create --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --route-name route-api --origin-group og-api-core --patterns-to-match "/api/*" --https-redirect Enabled --link-to-default-domain Enabled -o none
	@az afd route show --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --route-name route-mcp --only-show-errors 2>/dev/null || az afd route create --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --route-name route-mcp --origin-group og-mcp-core --patterns-to-match "/mcp/*" --https-redirect Enabled --link-to-default-domain Enabled -o none
	@az afd route show --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --route-name route-web --only-show-errors 2>/dev/null || az afd route create --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --route-name route-web --origin-group og-web-core --patterns-to-match "/*" --https-redirect Enabled --link-to-default-domain Enabled -o none
infra-front-routes-associate: infra-front-routes
	@SUB_ID=$$(az account show --query id -o tsv); WEB_CD_ID="/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/customDomains/kutria-com"; WILD_CD_ID="/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/customDomains/wildcard-kutria-com"; OG_API_ID="/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/originGroups/og-api-core"; OG_MCP_ID="/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/originGroups/og-mcp-core"; OG_WEB_ID="/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/originGroups/og-web-core"; az rest --method put --uri "https://management.azure.com/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/afdEndpoints/$(FRONT_ENDPOINT)/routes/route-api?api-version=2023-05-01" --body "{\"properties\":{\"originGroup\":{\"id\":\"$$OG_API_ID\"},\"customDomains\":[{\"id\":\"$$WEB_CD_ID\"},{\"id\":\"$$WILD_CD_ID\"}],\"patternsToMatch\":[\"/api/*\"],\"linkToDefaultDomain\":\"Enabled\",\"httpsRedirect\":\"Enabled\"}}" -o none; az rest --method put --uri "https://management.azure.com/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/afdEndpoints/$(FRONT_ENDPOINT)/routes/route-mcp?api-version=2023-05-01" --body "{\"properties\":{\"originGroup\":{\"id\":\"$$OG_MCP_ID\"},\"customDomains\":[{\"id\":\"$$WEB_CD_ID\"},{\"id\":\"$$WILD_CD_ID\"}],\"patternsToMatch\":[\"/mcp/*\"],\"linkToDefaultDomain\":\"Enabled\",\"httpsRedirect\":\"Enabled\"}}" -o none; az rest --method put --uri "https://management.azure.com/subscriptions/$$SUB_ID/resourceGroups/$(RG)/providers/Microsoft.Cdn/profiles/$(FRONT)/afdEndpoints/$(FRONT_ENDPOINT)/routes/route-web?api-version=2023-05-01" --body "{\"properties\":{\"originGroup\":{\"id\":\"$$OG_WEB_ID\"},\"customDomains\":[{\"id\":\"$$WEB_CD_ID\"},{\"id\":\"$$WILD_CD_ID\"}],\"patternsToMatch\":[\"/*\"],\"linkToDefaultDomain\":\"Enabled\",\"httpsRedirect\":\"Enabled\"}}" -o none
infra-dns-validation: infra-front-domains
	@RG_DNS=$$(az network dns zone list --query "[?name=='$(DNS_ZONE)'].resourceGroup" -o tsv | head -n1); if [ -z "$$RG_DNS" ]; then echo ">> No zona DNS $(DNS_ZONE), saltando"; exit 0; fi; TOKEN_APEX=$$(az afd custom-domain show --profile-name $(FRONT) -g $(RG) --custom-domain-name kutria-com --query validationProperties.validationToken -o tsv); TOKEN_WILD=$$(az afd custom-domain show --profile-name $(FRONT) -g $(RG) --custom-domain-name wildcard-kutria-com --query validationProperties.validationToken -o tsv); az network dns record-set txt create -g $$RG_DNS -z $(DNS_ZONE) -n _dnsauth -o none 2>/dev/null || true; if [ -n "$$TOKEN_APEX" ]; then az network dns record-set txt add-record -g $$RG_DNS -z $(DNS_ZONE) -n _dnsauth -v $$TOKEN_APEX -o none 2>/dev/null || true; fi; if [ -n "$$TOKEN_WILD" ]; then az network dns record-set txt add-record -g $$RG_DNS -z $(DNS_ZONE) -n _dnsauth -v $$TOKEN_WILD -o none 2>/dev/null || true; fi
infra-dns-wildcard: infra-front
	@FD_HOST=$$(az afd endpoint show --profile-name $(FRONT) -g $(RG) --endpoint-name $(FRONT_ENDPOINT) --query hostName -o tsv); RG_DNS=$$(az network dns zone list --query "[?name=='$(DNS_ZONE)'].resourceGroup" -o tsv | head -n1); if [ -z "$$RG_DNS" ]; then exit 0; fi; az network dns record-set cname create -g $$RG_DNS -z $(DNS_ZONE) -n "*" -o none 2>/dev/null || true; az network dns record-set cname set-record -g $$RG_DNS -z $(DNS_ZONE) -n "*" --cname $$FD_HOST -o none; az network dns record-set cname create -g $$RG_DNS -z $(DNS_ZONE) -n www -o none 2>/dev/null || true; az network dns record-set cname set-record -g $$RG_DNS -z $(DNS_ZONE) -n www --cname $$FD_HOST -o none 2>/dev/null || true

# ─── API con SQL Serverless override desde KV - URL COMPLETA FIX ───
api: infra-acr infra-env infra-sql infra-keyvault infra-storage infra-identity infra-kv-secrets infra-qdrant
	@az acr build -r $(ACR) -t kutria/api:$(TAG) -f ../src/Commerce.Api/Dockerfile .. -o none
	@IDENTITY_RESOURCE_ID=$(IDENTITY_FULL_ID); \
	az containerapp show -n ca-api-core -g $(RG) --only-show-errors 2>/dev/null || \
	az containerapp create -n ca-api-core -g $(RG) --environment $(CA_ENV) --image $(API_IMG) --ingress external --target-port 5000 --min-replicas 0 --max-replicas 10 --user-assigned $$IDENTITY_RESOURCE_ID --registry-server $(ACR).azurecr.io --registry-identity $$IDENTITY_RESOURCE_ID -o none; \
	echo ">> Seteando secrets con URL completa de KV..."; \
	SECRETS=""; \
	for sec in sql-connection-string storage-connection-string; do \
		if az keyvault secret show --vault-name $(KV_NAME) -n $$sec >/dev/null 2>&1; then \
			SECRETS="$$SECRETS $$sec=keyvaultref:$(KV_URI)/secrets/$$sec,identityref:$$IDENTITY_RESOURCE_ID"; \
		fi; \
	done; \
	echo "   $$SECRETS"; \
	az containerapp secret set -n ca-api-core -g $(RG) --secrets $$SECRETS -o none; \
	az containerapp update -n ca-api-core -g $(RG) --image $(API_IMG) --set-env-vars Worker__Type=api ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://+:5000 Database__Provider=SqlServer Queue__Provider=AzureQueue Vector__Provider=Qdrant Chat__Provider=TableStorage Chat__AllowedOrigins=https://kutria.com,https://www.kutria.com Mcp__PublicBaseUrl=https://mcp.kutria.com Mcp__FrontendBaseUrl=https://kutria.com Qdrant__Endpoint=http://ca-qdrant-core:6333 ConnectionStrings__DefaultConnection=secretref:sql-connection-string ConnectionStrings__Commerce=secretref:sql-connection-string Queue__ConnectionString=secretref:storage-connection-string AzureStorage__ConnectionString=secretref:storage-connection-string -o none

web: infra-acr infra-env infra-identity
	@az acr build -r $(ACR) -t kutria/web:$(TAG) -f ../src/Commerce.Web/Dockerfile .. -o none
	@az containerapp show -n ca-web-core -g $(RG) --only-show-errors 2>/dev/null || \
	az containerapp create -n ca-web-core -g $(RG) --environment $(CA_ENV) --image $(WEB_IMG) --ingress external --target-port 80 --min-replicas 0 --max-replicas 10 --user-assigned $(IDENTITY_FULL_ID) --registry-server $(ACR).azurecr.io --registry-identity $(IDENTITY_FULL_ID) -o none
	@az containerapp update -n ca-web-core -g $(RG) --image $(WEB_IMG) -o none

mcp: infra-acr infra-env infra-sql infra-keyvault infra-storage infra-identity infra-kv-secrets
	@az acr build -r $(ACR) -t kutria/mcp:$(TAG) -f ../src/Commerce.Mcp/Dockerfile .. -o none
	@az containerapp show -n ca-mcp-core -g $(RG) --only-show-errors 2>/dev/null || \
	az containerapp create -n ca-mcp-core -g $(RG) --environment $(CA_ENV) --image $(MCP_IMG) --ingress external --target-port 5000 --min-replicas 0 --max-replicas 10 --user-assigned $(IDENTITY_FULL_ID) --registry-server $(ACR).azurecr.io --registry-identity $(IDENTITY_FULL_ID) -o none; \
	SECRETS=""; \
	for sec in sql-connection-string storage-connection-string; do \
		if az keyvault secret show --vault-name $(KV_NAME) -n $$sec >/dev/null 2>&1; then \
			SECRETS="$$SECRETS $$sec=keyvaultref:$(KV_URI)/secrets/$$sec,identityref:$(IDENTITY_FULL_ID)"; \
		fi; \
	done; \
	if [ -n "$$SECRETS" ]; then az containerapp secret set -n ca-mcp-core -g $(RG) --secrets $$SECRETS -o none; fi; \
	az containerapp update -n ca-mcp-core -g $(RG) --image $(MCP_IMG) --set-env-vars Worker__Type=mcp ASPNETCORE_ENVIRONMENT=Production Database__Provider=SqlServer Queue__Provider=AzureQueue Vector__Provider=Qdrant ConnectionStrings__DefaultConnection=secretref:sql-connection-string -o none

workers: infra-acr infra-env infra-storage infra-sql infra-keyvault infra-identity infra-kv-secrets
	@az acr build -r $(ACR) -t kutria/worker:$(TAG) -f ../src/Commerce.Worker/Dockerfile .. -o none
	@az containerapp show -n ca-worker-image -g $(RG) --only-show-errors 2>/dev/null || \
	az containerapp create -n ca-worker-image -g $(RG) --environment $(CA_ENV) --image $(WORKER_IMG) --min-replicas 0 --max-replicas 20 --user-assigned $(IDENTITY_FULL_ID) --registry-server $(ACR).azurecr.io --registry-identity $(IDENTITY_FULL_ID) -o none; \
	SECRETS=""; \
	for sec in sql-connection-string storage-connection-string; do \
		if az keyvault secret show --vault-name $(KV_NAME) -n $$sec >/dev/null 2>&1; then SECRETS="$$SECRETS $$sec=keyvaultref:$(KV_URI)/secrets/$$sec,identityref:$(IDENTITY_FULL_ID)"; fi; \
	done; \
	if [ -n "$$SECRETS" ]; then az containerapp secret set -n ca-worker-image -g $(RG) --secrets $$SECRETS -o none; fi; \
	az containerapp update -n ca-worker-image -g $(RG) --image $(WORKER_IMG) --set-env-vars Worker__Type=image Database__Provider=SqlServer Queue__Provider=AzureQueue Vector__Provider=Qdrant Qdrant__Endpoint=http://ca-qdrant-core:6333 ConnectionStrings__DefaultConnection=secretref:sql-connection-string Queue__ConnectionString=secretref:storage-connection-string -o none; \
	az containerapp show -n ca-worker-data -g $(RG) --only-show-errors 2>/dev/null || \
	az containerapp create -n ca-worker-data -g $(RG) --environment $(CA_ENV) --image $(WORKER_IMG) --min-replicas 0 --max-replicas 10 --user-assigned $(IDENTITY_FULL_ID) --registry-server $(ACR).azurecr.io --registry-identity $(IDENTITY_FULL_ID) -o none; \
	if [ -n "$$SECRETS" ]; then az containerapp secret set -n ca-worker-data -g $(RG) --secrets $$SECRETS -o none; fi; \
	az containerapp update -n ca-worker-data -g $(RG) --image $(WORKER_IMG) --set-env-vars Worker__Type=data Database__Provider=SqlServer Queue__Provider=AzureQueue Vector__Provider=Qdrant ConnectionStrings__DefaultConnection=secretref:sql-connection-string Queue__ConnectionString=secretref:storage-connection-string -o none

core-infra: infra-rg infra-acr infra-env infra-storage infra-keyvault infra-keyvault-rbac infra-sql infra-identity infra-qdrant infra-front

deploy-core: core-infra api web mcp workers infra-front-domains infra-front-origins infra-front-routes infra-front-routes-associate infra-dns-validation infra-dns-wildcard
	@echo "✅ Deploy completo con SQL Serverless desde KV"

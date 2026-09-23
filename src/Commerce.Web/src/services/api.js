import axios from 'axios'
import { fetchEventSource } from '@microsoft/fetch-event-source'
import { buildExecuteRequestBody } from './toolPayload.js'
import { FINBUCKLE_TENANT_HEADER, resolveTenantSlug, withTenantApiPath } from '@/utils/tenant'

const AUTH_TOKEN_KEY = 'auth_token'
// Legacy TemplateProject keys kept only so old localStorage does not break; Commerce does not use Workspace tenancy.
const ACTIVE_WORKSPACE_KEY = 'active_workspace_id'
const ACTIVE_WORKSPACE_NAME_KEY = 'active_workspace_name'

const apiBaseUrl = import.meta.env.VITE_API_URL || '/api'
const mcpBaseUrl = (import.meta.env.VITE_MCP_URL || 'http://localhost:5055').replace(/\/$/, '')
const api = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json'
  }
})

/** Maps FE/API workspace env to camelCase enum names System.Text.Json expects. */
function normalizeWorkspaceEnvironment(value) {
  const raw = value == null ? '' : String(value).trim()
  if (raw === '1' || raw.toLowerCase() === 'production') return 'production'
  return 'sandbox'
}

function normalizePublicationListingSummary(pub) {
  if (!pub || typeof pub !== 'object') return null
  return {
    externalListingId: pub.externalListingId ?? pub.ExternalListingId ?? null,
    marketplaceStatus: pub.marketplaceStatus ?? pub.MarketplaceStatus ?? null,
    syncState: pub.syncState ?? pub.SyncState ?? null,
    listingLastError: pub.listingLastError ?? pub.ListingLastError ?? null,
    latestPublishItemId: pub.latestPublishItemId ?? pub.LatestPublishItemId ?? null,
    latestPublishStatus: pub.latestPublishStatus ?? pub.LatestPublishStatus ?? null,
    latestPublishServiceKind: pub.latestPublishServiceKind ?? pub.LatestPublishServiceKind ?? null,
    latestPublishErrorMessage: pub.latestPublishErrorMessage ?? pub.LatestPublishErrorMessage ?? null,
    latestPublishCreatedAt: pub.latestPublishCreatedAt ?? pub.LatestPublishCreatedAt ?? null
  }
}

/** Fila de GET /channel-destinations/{id}/publication-catalog: product + published. */
function normalizeChannelDestinationPublicationCatalog(data) {
  const rawItems = data?.items ?? data?.Items ?? []
  const items = rawItems
    .map((row) => {
      const prod = row?.product ?? row?.Product ?? row
      if (!prod || typeof prod !== 'object') return null
      const r = prod
      const published = normalizePublicationListingSummary(row?.published ?? row?.Published)
      return {
        id: String(r.id ?? r.Id ?? ''),
        title: r.title ?? r.Title ?? null,
        sku: r.sku ?? r.Sku ?? null,
        primaryImageUrl: r.primaryImageUrl ?? r.PrimaryImageUrl ?? null,
        price: r.price ?? r.Price ?? null,
        specialPrice: r.specialPrice ?? r.SpecialPrice ?? null,
        currencyId: r.currencyId ?? r.CurrencyId ?? null,
        availableQuantity: r.availableQuantity ?? r.AvailableQuantity ?? null,
        loadStatus: r.loadStatus ?? r.LoadStatus ?? '',
        categoryName: r.categoryName ?? r.CategoryName ?? null,
        published
      }
    })
    .filter((x) => x && x.id)
  return {
    items,
    page: Number(data?.page ?? data?.Page ?? 1),
    pageSize: Number(data?.pageSize ?? data?.PageSize ?? 20),
    totalCount: Number(data?.totalCount ?? data?.TotalCount ?? 0),
    aggregates: data?.aggregates ?? data?.Aggregates ?? null
  }
}

/** Respuesta de GET /channel-destinations/{id}/price-list. */
function normalizeChannelDestinationPriceList(data) {
  const rawItems = data?.items ?? data?.Items ?? []
  const items = rawItems
    .map((r) => {
      if (!r || typeof r !== 'object') return null
      return {
        id: String(r.id ?? r.Id ?? ''),
        productId: String(r.productId ?? r.ProductId ?? ''),
        skuId: String(r.skuId ?? r.SkuId ?? ''),
        price: r.price ?? r.Price ?? null,
        specialPrice: r.specialPrice ?? r.SpecialPrice ?? null,
        createdAt: r.createdAt ?? r.CreatedAt ?? null,
        productTitle: r.productTitle ?? r.ProductTitle ?? null,
        sku: r.sku ?? r.Sku ?? null,
        primaryImageUrl: r.primaryImageUrl ?? r.PrimaryImageUrl ?? null
      }
    })
    .filter((x) => x && x.id)
  return {
    items,
    page: Number(data?.page ?? data?.Page ?? 1),
    pageSize: Number(data?.pageSize ?? data?.PageSize ?? 20),
    totalCount: Number(data?.totalCount ?? data?.TotalCount ?? 0)
  }
}

function normalizePipelineCatalogProductChannelPublication(row) {
  if (!row || typeof row !== 'object') return null
  return {
    publicationId: String(row.publicationId ?? row.PublicationId ?? ''),
    channelDestinationId: String(row.channelDestinationId ?? row.ChannelDestinationId ?? ''),
    channelDestinationName: row.channelDestinationName ?? row.ChannelDestinationName ?? '',
    marketplaceKey: row.marketplaceKey ?? row.MarketplaceKey ?? '',
    externalListingId: row.externalListingId ?? row.ExternalListingId ?? null,
    marketplaceStatus: row.marketplaceStatus ?? row.MarketplaceStatus ?? null,
    syncState: row.syncState ?? row.SyncState ?? '',
    lastError: row.lastError ?? row.LastError ?? null,
    lastWarning: row.lastWarning ?? row.LastWarning ?? null,
    createdAt: row.createdAt ?? row.CreatedAt ?? null,
    updatedAt: row.updatedAt ?? row.UpdatedAt ?? null
  }
}

function normalizePipelineCatalogProductPublicationEvent(row) {
  if (!row || typeof row !== 'object') return null
  return {
    id: String(row.id ?? row.Id ?? ''),
    serviceKind: row.serviceKind ?? row.ServiceKind ?? '',
    status: row.status ?? row.Status ?? '',
    errorMessage: row.errorMessage ?? row.ErrorMessage ?? null,
    warningMessage: row.warningMessage ?? row.WarningMessage ?? null,
    batchJobId: row.batchJobId ?? row.BatchJobId ?? null,
    createdAt: row.createdAt ?? row.CreatedAt ?? null
  }
}

function normalizeChannelCatalogProductDetail(data) {
  const d = data ?? {}
  const skuIdsRaw = d.skuIds ?? d.SkuIds ?? []
  return {
    id: d.id ?? d.Id,
    channelDataPipelineId: d.channelDataPipelineId ?? d.ChannelDataPipelineId,
    integrationId: d.integrationId ?? d.IntegrationId ?? null,
    entityId: d.entityId ?? d.EntityId ?? '',
    naturalKey: d.naturalKey ?? d.NaturalKey ?? '',
    title: d.title ?? d.Title ?? null,
    sku: d.sku ?? d.Sku ?? null,
    skuIds: Array.isArray(skuIdsRaw) ? skuIdsRaw.map(String) : [],
    categoryId: d.categoryId ?? d.CategoryId ?? null,
    categoryName: d.categoryName ?? d.CategoryName ?? null,
    brandId: d.brandId ?? d.BrandId ?? null,
    brandName: d.brandName ?? d.BrandName ?? null,
    price: d.price ?? d.Price ?? null,
    specialPrice: d.specialPrice ?? d.SpecialPrice ?? null,
    currencyId: d.currencyId ?? d.CurrencyId ?? null,
    availableQuantity: d.availableQuantity ?? d.AvailableQuantity ?? null,
    primaryImageUrl: d.primaryImageUrl ?? d.PrimaryImageUrl ?? null,
    loadStatus: d.loadStatus ?? d.LoadStatus ?? '',
    loadError: d.loadError ?? d.LoadError ?? null,
    sourceUpdatedAt: d.sourceUpdatedAt ?? d.SourceUpdatedAt ?? null,
    canonicalJson: d.canonicalJson ?? d.CanonicalJson ?? '{}',
    createdAt: d.createdAt ?? d.CreatedAt ?? null,
    updatedAt: d.updatedAt ?? d.UpdatedAt ?? null
  }
}

function normalizeChannelSaleOrderListItem(raw) {
  const r = raw ?? {}
  return {
    id: String(r.id ?? r.Id ?? ''),
    integrationId: String(r.integrationId ?? r.IntegrationId ?? ''),
    provider: r.provider ?? r.Provider ?? '',
    externalOrderId: r.externalOrderId ?? r.ExternalOrderId ?? '',
    orderStatus: r.orderStatus ?? r.OrderStatus ?? null,
    total: r.total ?? r.Total ?? null,
    currency: r.currency ?? r.Currency ?? null,
    receivedAtUtc: r.receivedAtUtc ?? r.ReceivedAtUtc ?? null,
    createdAt: r.createdAt ?? r.CreatedAt ?? null,
    lineCount: Number(r.lineCount ?? r.LineCount ?? 0)
  }
}

function normalizeChannelSaleOrderLine(raw) {
  const r = raw ?? {}
  return {
    id: String(r.id ?? r.Id ?? ''),
    externalLineId: r.externalLineId ?? r.ExternalLineId ?? null,
    sku: r.sku ?? r.Sku ?? null,
    title: r.title ?? r.Title ?? null,
    quantity: Number(r.quantity ?? r.Quantity ?? 0),
    unitPrice: r.unitPrice ?? r.UnitPrice ?? null,
    lineTotal: r.lineTotal ?? r.LineTotal ?? null,
    listingId: r.listingId ?? r.ListingId ?? null
  }
}

function normalizeOriginMappingTreeNode(n) {
  if (!n || typeof n !== 'object') return null
  const ch = n.children ?? n.Children
  let children = null
  if (Array.isArray(ch) && ch.length) {
    children = ch.map(normalizeOriginMappingTreeNode).filter(Boolean)
  }
  return {
    key: String(n.key ?? n.Key ?? ''),
    label: String(n.label ?? n.Label ?? ''),
    dataType: n.dataType ?? n.DataType ?? null,
    children
  }
}

function normalizeDestinationPayloadJsonSchema(raw) {
  if (raw == null) return null
  if (typeof raw === 'object' && !Array.isArray(raw)) return raw
  if (typeof raw === 'string') {
    try {
      return JSON.parse(raw)
    } catch {
      return null
    }
  }
  return null
}

function normalizeChannelSaleOrderOriginSchema(raw) {
  const d = raw ?? {}
  const orderFieldsRaw = d.stagingOrderFields ?? d.StagingOrderFields ?? []
  const lineFieldsRaw = d.stagingLineFields ?? d.StagingLineFields ?? []
  const mapRaw = d.defaultStagingToTargetPath ?? d.DefaultStagingToTargetPath ?? {}
  const orderFields = Array.isArray(orderFieldsRaw)
    ? orderFieldsRaw.map((f) => ({
        key: f.key ?? f.Key ?? '',
        label: f.label ?? f.Label ?? '',
        group: f.group ?? f.Group ?? 'order'
      }))
    : []
  const lineFields = Array.isArray(lineFieldsRaw)
    ? lineFieldsRaw.map((f) => ({
        key: f.key ?? f.Key ?? '',
        label: f.label ?? f.Label ?? '',
        group: f.group ?? f.Group ?? 'line'
      }))
    : []
  const defaultStagingToTargetPath = {}
  if (mapRaw && typeof mapRaw === 'object' && !Array.isArray(mapRaw)) {
    for (const [k, v] of Object.entries(mapRaw)) {
      if (v != null && String(v).trim() !== '') defaultStagingToTargetPath[k] = String(v)
    }
  }
  const treeRaw = d.stagingFieldsTree ?? d.StagingFieldsTree
  const stagingFieldsTree = Array.isArray(treeRaw)
    ? treeRaw.map(normalizeOriginMappingTreeNode).filter(Boolean)
    : []
  return {
    providerKey: d.providerKey ?? d.ProviderKey ?? '',
    integrationId: String(d.integrationId ?? d.IntegrationId ?? ''),
    lineItemsPath: d.lineItemsPath ?? d.LineItemsPath ?? null,
    stagingOrderFields: orderFields,
    stagingLineFields: lineFields,
    defaultStagingToTargetPath,
    destinationPayloadJsonSchema: normalizeDestinationPayloadJsonSchema(
      d.destinationPayloadJsonSchema ?? d.DestinationPayloadJsonSchema
    ),
    stagingFieldsTree
  }
}

function normalizeChannelSaleOrderDetail(raw) {
  const d = raw ?? {}
  const linesRaw = d.lines ?? d.Lines ?? []
  const lines = Array.isArray(linesRaw) ? linesRaw.map(normalizeChannelSaleOrderLine) : []
  return {
    id: String(d.id ?? d.Id ?? ''),
    accountId: String(d.accountId ?? d.AccountId ?? ''),
    workspaceId: String(d.workspaceId ?? d.WorkspaceId ?? ''),
    integrationId: String(d.integrationId ?? d.IntegrationId ?? ''),
    provider: d.provider ?? d.Provider ?? '',
    externalOrderId: d.externalOrderId ?? d.ExternalOrderId ?? '',
    orderStatus: d.orderStatus ?? d.OrderStatus ?? null,
    paymentStatus: d.paymentStatus ?? d.PaymentStatus ?? null,
    fulfillmentStatus: d.fulfillmentStatus ?? d.FulfillmentStatus ?? null,
    currency: d.currency ?? d.Currency ?? null,
    subtotal: d.subtotal ?? d.Subtotal ?? null,
    total: d.total ?? d.Total ?? null,
    shippingTotal: d.shippingTotal ?? d.ShippingTotal ?? null,
    discountTotal: d.discountTotal ?? d.DiscountTotal ?? null,
    taxTotal: d.taxTotal ?? d.TaxTotal ?? null,
    channelCreatedAt: d.channelCreatedAt ?? d.ChannelCreatedAt ?? null,
    channelUpdatedAt: d.channelUpdatedAt ?? d.ChannelUpdatedAt ?? null,
    receivedAtUtc: d.receivedAtUtc ?? d.ReceivedAtUtc ?? null,
    buyerEmail: d.buyerEmail ?? d.BuyerEmail ?? null,
    shippingCountry: d.shippingCountry ?? d.ShippingCountry ?? null,
    canonicalJson: d.canonicalJson ?? d.CanonicalJson ?? '{}',
    createdAt: d.createdAt ?? d.CreatedAt ?? null,
    updatedAt: d.updatedAt ?? d.UpdatedAt ?? null,
    lines
  }
}

// Auth API: public /auth/* and tenant /t/{id}/auth/* live on the backend root.
// In Vite dev, both must go through `/api` so the proxy rewrites to the API
// (`/api/t/{id}/auth/me` → `/t/{id}/auth/me`). An empty baseURL would hit the SPA
// fallback and return index.html for `/t/.../auth/me`.
const authBaseUrl = apiBaseUrl && apiBaseUrl !== '/api'
  ? apiBaseUrl.replace(/\/api\/?$/, '')
  : (apiBaseUrl === '/api' ? '/api' : '')
const authApi = axios.create({
  baseURL: authBaseUrl,
  headers: { 'Content-Type': 'application/json' }
})

const mcpApi = axios.create({
  baseURL: mcpBaseUrl,
  headers: { 'Content-Type': 'application/json' }
})

mcpApi.interceptors.request.use((config) => {
  const token = localStorage.getItem(AUTH_TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Add Bearer token to auth API for authenticated endpoints
authApi.interceptors.request.use((config) => {
  const token = localStorage.getItem(AUTH_TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  if (typeof config.url === 'string') {
    config.url = withTenantApiPath(config.url)
  }
  return config
})

// Redirect to login on 401 for auth API (except sign-in, sign-up, and social callback pages)
authApi.interceptors.response.use(
  (response) => response,
  (error) => {
    normalizeErrorResponse(error)
    if (isProvisioningFailedResponse(error)) {
      const p = window.location.pathname
      if (!shouldSkipProvisioningFailureRedirect(p)) {
        redirectToSignInAfterProvisioningFailure()
      }
    }
    if (error.response?.status === 401) {
      const currentPath = window.location.pathname
      const isAuthPage = currentPath.startsWith('/sign-in') || currentPath.startsWith('/sign-up')
      const isSocialCallback = /^\/auth\/[^/]+\/callback$/.test(currentPath)
      if (!isAuthPage && !isSocialCallback) {
        const returnUrl = encodeURIComponent(currentPath + window.location.search)
        window.location.href = `/sign-in?returnUrl=${returnUrl}`
      }
    }
    return Promise.reject(error)
  }
)

// Bearer + Finbuckle path `/t/{identifier}`. X-Workspace-Id is optional secondary partition only.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(AUTH_TOKEN_KEY)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  if (typeof config.url === 'string') {
    config.url = withTenantApiPath(config.url)
  }

  const tenant = resolveTenantSlug()
  if (tenant) {
    config.headers[FINBUCKLE_TENANT_HEADER] = tenant
  }

  const workspaceId = localStorage.getItem(ACTIVE_WORKSPACE_KEY)
  if (workspaceId) {
    config.headers['X-Workspace-Id'] = workspaceId
  }

  // FormData needs multipart/form-data with boundary - let browser set it (not application/json)
  if (config.data instanceof FormData) {
    const h = config.headers
    if (h && typeof h.delete === 'function') {
      h.delete('Content-Type')
      h.delete('content-type')
    } else if (h) {
      delete h['Content-Type']
      delete h['content-type']
    }
  }
  return config
})

/**
 * Normaliza el cuerpo de error de la API para que el mensaje esté en .error.
 * El backend devuelve ProblemDetails (RFC 7807) con .detail y .title; muchas respuestas 422
 * usan ese formato y el frontend espera .error. Así todas las llamadas que usan
 * e.response?.data?.error siguen mostrando el mensaje sin cambiar cada una.
 */
function normalizeErrorResponse(error) {
  const data = error.response?.data
  if (!data || typeof data !== 'object') return
  if (data.error != null && data.error !== '') return // ya tiene mensaje
  const msg = data.detail ?? data.title
  if (msg != null && msg !== '') {
    error.response.data.error = typeof msg === 'string' ? msg : String(msg)
    return
  }
  // Validación: array de { errorMessage } o ValidationProblemDetails: { "Campo": ["msg1", "msg2"] }
  const errors = data.errors
  if (Array.isArray(errors) && errors.length > 0) {
    const parts = errors.map((e) => (e && typeof e === 'object' ? (e.errorMessage ?? e.message ?? e.ErrorMessage ?? e.Message) : String(e)))
    if (parts.some(Boolean)) error.response.data.error = parts.filter(Boolean).join('. ')
    return
  }
  if (errors && typeof errors === 'object' && !Array.isArray(errors)) {
    const parts = []
    for (const arr of Object.values(errors)) {
      if (Array.isArray(arr)) parts.push(...arr.filter((m) => m != null && m !== ''))
      else if (arr != null && arr !== '') parts.push(String(arr))
    }
    if (parts.length > 0) error.response.data.error = parts.join('. ')
  }
}

function isProvisioningFailedResponse(error) {
  return error.response?.data?.error === 'ProvisioningFailed'
}

/** IdP JIT provisioning falló (middleware o /auth/identity/provision). Limpia sesión y vuelve a iniciar sesión. */
function redirectToSignInAfterProvisioningFailure() {
  try {
    localStorage.removeItem(AUTH_TOKEN_KEY)
    localStorage.removeItem(ACTIVE_WORKSPACE_KEY)
    localStorage.removeItem(ACTIVE_WORKSPACE_NAME_KEY)
  } catch {
    /* ignore */
  }
  const path = window.location.pathname + window.location.search
  const returnUrl = encodeURIComponent(path)
  window.location.replace(`/sign-in?returnUrl=${returnUrl}`)
}

function shouldSkipProvisioningFailureRedirect(pathname) {
  return pathname.startsWith('/sign-in') || pathname.startsWith('/sign-up')
}

// Redirect to login on 401 (admin/protected routes)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    normalizeErrorResponse(error)
    if (isProvisioningFailedResponse(error)) {
      const p = window.location.pathname
      if (!shouldSkipProvisioningFailureRedirect(p)) {
        redirectToSignInAfterProvisioningFailure()
      }
    }
    if (error.response?.status === 401) {
      const currentPath = window.location.pathname
      const isAuthPage = currentPath.startsWith('/sign-in') || currentPath.startsWith('/sign-up')
      const isInvitePage = currentPath.startsWith('/invite/')
      if (!isAuthPage && !isInvitePage) {
        const returnUrl = encodeURIComponent(currentPath + window.location.search)
        window.location.href = `/sign-in?returnUrl=${returnUrl}`
      }
    }
    return Promise.reject(error)
  }
)

/** ChannelPipelineEntityType numérico (ASP.NET serializa enums como número por defecto). */
const CHANNEL_PIPELINE_ENTITY_TYPE = {
  products: 0,
  stock: 1,
  price: 2,
  orders: 3
}

/** Guid vacío que la API usa como «sin integración de marketplace» (p. ej. outbound solo staging). */
const EMPTY_INTEGRATION_GUID = '00000000-0000-0000-0000-000000000000'

function normalizeMarketplaceIntegrationIdForPipeline(raw) {
  if (raw == null || raw === '') return null
  const s = String(raw).trim().toLowerCase()
  if (s === EMPTY_INTEGRATION_GUID) return null
  return raw
}

/** Cuerpo JSON para POST/PUT `/channel-pipelines` (CreateChannelPipelineRequest en API). */
async function buildChannelPipelineUpsertBody(payload) {
  const ws = String(payload?.workspaceId ?? localStorage.getItem(ACTIVE_WORKSPACE_KEY) ?? '').trim()
  if (!ws) {
    const err = new Error('Selecciona un workspace activo.')
    err.response = { data: { error: err.message } }
    throw err
  }
  if (payload?.sourceType !== 0 || !payload?.integrationId) {
    const err = new Error('Solo está soportada la creación desde integración de tienda por ahora.')
    err.response = { data: { error: err.message } }
    throw err
  }
  const { data: integrations } = await api.get('/integrations')
  const list = Array.isArray(integrations) ? integrations : []
  const dirKey = String(payload.direction ?? payload.pipelineDirection ?? 'outbound').toLowerCase()
  const direction = dirKey === 'inbound' ? 1 : 0
  const isInbound = direction === 1
  const mpId = normalizeMarketplaceIntegrationIdForPipeline(payload.marketplaceIntegrationId)
  if (isInbound) {
    if (!mpId) {
      const err = new Error('Selecciona el marketplace de origen de pedidos.')
      err.response = { data: { error: err.message } }
      throw err
    }
    const mpIn = list.find((i) => String(i.id) === String(mpId))
    if (!mpIn?.id) {
      const err = new Error('La integración de marketplace seleccionada no existe o no tienes acceso.')
      err.response = { data: { error: err.message } }
      throw err
    }
  } else if (mpId) {
    const mpCheck = list.find((i) => String(i.id) === String(mpId))
    if (!mpCheck?.id) {
      const err = new Error('La integración de marketplace seleccionada no existe o no tienes acceso.')
      err.response = { data: { error: err.message } }
      throw err
    }
  }
  const entityType = direction === 1 ? CHANNEL_PIPELINE_ENTITY_TYPE.orders : CHANNEL_PIPELINE_ENTITY_TYPE.products
  let cfg = {}
  if (payload.sourceConfig) {
    try {
      cfg = JSON.parse(payload.sourceConfig)
    } catch {
      cfg = {}
    }
  }
  if (payload.retentionDays != null) cfg.retentionDays = payload.retentionDays
  if (payload.daysBack != null) cfg.daysBack = payload.daysBack
  const storeInt = list.find((i) => String(i.id) === String(payload.integrationId))
  const mk = String(payload.marketplaceKey || 'mercadolibre').toLowerCase()
  const mp = mpId ? list.find((i) => String(i.id) === String(mpId)) : null
  const marketplaceIntegrationId = mp?.id ?? EMPTY_INTEGRATION_GUID
  const autoName = mp
    ? storeInt?.name
      ? `${storeInt.name} → ${mp.name || mp.provider}`
      : `Pipeline → ${mp.name || mp.provider}`
    : storeInt?.name
      ? `${storeInt.name} · staging`
      : 'Pipeline catálogo'
  const name = String(payload.name || '').trim() || autoName
  const body = {
    name,
    workspaceId: ws,
    direction,
    entityType,
    storeIntegrationId: payload.integrationId,
    marketplaceIntegrationId,
    marketplaceKey: mk,
    sourceConfigJson: JSON.stringify(cfg),
    cronExpression: payload.cronExpression || '0 */6 * * *',
    batchSize: payload.batchSize ?? 50,
    maxRecordsPerRun: payload.maxRecordsPerRun ?? 500,
    isActive: payload.isActive !== false
  }
  if (payload.channelDestinationId != null && payload.channelDestinationId !== '') {
    body.channelDestinationId = payload.channelDestinationId
  }
  return body
}

function parseEntityTypeStringFromApi(raw) {
  const t = String(raw ?? 'Products').toLowerCase()
  if (t === 'stock') return 'stock'
  if (t === 'price') return 'price'
  if (t === 'orders') return 'orders'
  return 'products'
}

/** Outbound = tienda→canal (productos); Inbound = canal→tienda (pedidos). */
function parsePipelineDirectionFromApi(raw) {
  const s = String(raw ?? 'Outbound').toLowerCase()
  return s === 'inbound' ? 'inbound' : 'outbound'
}

function mapChannelPipelineListToUi(p) {
  if (!p) return null
  const entityType = parseEntityTypeStringFromApi(p.entityType)
  const workspaceId = p.workspaceId ?? p.WorkspaceId ?? null
  return {
    ...p,
    id: p.id,
    workspaceId,
    name: p.name ?? p.Name,
    entityType,
    pipelineDirection: parsePipelineDirectionFromApi(p.direction),
    sourceType: 0,
    cronExpression: p.cronExpression,
    isActive: p.isActive ?? p.IsActive ?? true,
    integrationId: p.storeIntegrationId
  }
}

function mapChannelPipelineDetailToUi(p) {
  if (!p) return null
  const entityType = parseEntityTypeStringFromApi(p.entityType)
  const cfg =
    p.sourceConfigJson != null
      ? typeof p.sourceConfigJson === 'string'
        ? p.sourceConfigJson
        : JSON.stringify(p.sourceConfigJson)
      : '{}'
  let retentionDays = 30
  let daysBack = 7
  try {
    const o = JSON.parse(cfg)
    if (o.retentionDays != null) retentionDays = Number(o.retentionDays)
    if (o.daysBack != null) daysBack = Number(o.daysBack)
  } catch {
    /* ignore */
  }
  return {
    id: p.id,
    workspaceId: p.workspaceId ?? p.WorkspaceId ?? null,
    sourceType: 0,
    integrationId: p.storeIntegrationId,
    entityType,
    pipelineDirection: parsePipelineDirectionFromApi(p.direction),
    cronExpression: p.cronExpression || '0 */6 * * *',
    batchSize: p.batchSize ?? 50,
    maxRecordsPerRun: p.maxRecordsPerRun ?? 500,
    retentionDays,
    daysBack,
    isActive: p.isActive ?? true,
    sourceConfig: cfg,
    name: p.name,
    direction: p.direction,
    marketplaceIntegrationId: p.marketplaceIntegrationId,
    marketplaceKey: p.marketplaceKey
  }
}

/** Categoría de taxonomía marketplace; Gravity con fullTree incluye children anidados. */
function mapMarketplaceTaxonomyCategoryDto(x) {
  if (!x || typeof x !== 'object') return null
  const rawKids = x.children ?? x.Children
  const childList = Array.isArray(rawKids)
    ? rawKids.map(mapMarketplaceTaxonomyCategoryDto).filter(Boolean)
    : []
  return {
    id: x.id ?? x.Id ?? '',
    name: x.name ?? x.Name ?? '',
    parentId: x.parentId ?? x.ParentId ?? null,
    domainId: x.domainId ?? x.DomainId ?? null,
    domainName: x.domainName ?? x.DomainName ?? null,
    children: childList
  }
}

const apiService = {
  // Auth API
  async getAuthConfig() {
    const { data } = await authApi.get('/auth/config')
    return data
  },
  async signIn(email, password) {
    const { data } = await authApi.post('/auth/signin', { email, password })
    return data
  },
  async checkTenantAvailability(identifier) {
    const { data } = await authApi.get('/auth/tenants/available', { params: { identifier } })
    return data
  },
  async signUp(email, password, displayName = null, planKey = null, partnerStartToken = null, identifier = null, company = null) {
    const body = { email, password, displayName, identifier }
    if (company) body.name = company
    if (planKey) body.planKey = planKey
    if (partnerStartToken) body.partnerStartToken = partnerStartToken
    const { data } = await authApi.post('/auth/signup', body)
    return data
  },
  async getSocialLoginAuthorizeUrl(provider, returnUrl = null) {
    const params = returnUrl ? { returnUrl } : {}
    const { data } = await authApi.get(`/auth/social/${provider}/authorize`, { params })
    return data
  },
  async socialLoginCallback(provider, code, redirectUri) {
    const { data } = await authApi.post(`/auth/social/${provider}/callback`, { code, redirectUri })
    return data
  },
  async unlinkSocialLogin(provider) {
    await authApi.delete(`/auth/social/${provider}`)
  },
  async getProfile() {
    const { data } = await authApi.get('/auth/me', {
      headers: {
        'Cache-Control': 'no-cache',
        Pragma: 'no-cache'
      },
      // Bust browser/proxy HTTP cache (avoids sticky 304 after logout/re-login).
      params: { _: Date.now() }
    })
    return data
  },
  async getPartnerBranding(space) {
    const { data } = await api.get(`/partners/branding/${encodeURIComponent(space)}`)
    return data
  },
  /** Call after storing IdP token in callback; ensures local user/account exist. */
  async provisionIdentity() {
    await authApi.post('/auth/identity/provision')
  },
  async completeMcpOAuthAuthorize(params) {
    const { data } = await mcpApi.post('/oauth/authorize/complete', {
      clientId: params.client_id,
      redirectUri: params.redirect_uri,
      state: params.state,
      codeChallenge: params.code_challenge,
      codeChallengeMethod: params.code_challenge_method,
      scope: params.scope,
      resource: params.resource,
      clientName: params.client_name
    })
    return data
  },
  async updateProfile(displayName) {
    const { data } = await authApi.put('/auth/profile', { displayName })
    return data
  },
  async changePassword(currentPassword, newPassword) {
    await authApi.post('/auth/change-password', { currentPassword, newPassword })
  },
  async setPassword(newPassword) {
    await authApi.post('/auth/set-password', { newPassword })
  },
  async forgotPassword(email) {
    await authApi.post('/auth/forgot-password', { email })
  },
  async resetPassword(email, token, newPassword) {
    await authApi.post('/auth/reset-password', { email, token, newPassword })
  },
  async confirmEmail(email, token) {
    await authApi.post('/auth/confirm-email', { email, token })
  },
  async resendConfirmEmail(email) {
    await authApi.post('/auth/resend-confirm-email', { email })
  },
  setToken(token) {
    if (token) localStorage.setItem(AUTH_TOKEN_KEY, token)
    else localStorage.removeItem(AUTH_TOKEN_KEY)
  },
  signOut() {
    localStorage.removeItem(AUTH_TOKEN_KEY)
    localStorage.removeItem(ACTIVE_WORKSPACE_KEY)
    localStorage.removeItem(ACTIVE_WORKSPACE_NAME_KEY)
    window.location.replace('/sign-in')
  },
  getToken() {
    return localStorage.getItem(AUTH_TOKEN_KEY)
  },
  isAuthenticated() {
    return !!localStorage.getItem(AUTH_TOKEN_KEY)
  },

  // Admin API — partners (SuperAdmin)
  async adminListPartners() {
    const { data } = await api.get('/admin/partners')
    return data
  },
  /** SuperAdmin: formato + unicidad del slug (blur en alta de partner). */
  async adminValidatePartnerSpace(space) {
    const { data } = await api.get('/admin/partners/validate-space', {
      params: { space: space ?? '' }
    })
    return data
  },
  async adminGetPartner(id) {
    const { data } = await api.get(`/admin/partners/${id}`)
    return data
  },
  async adminCreatePartner(body) {
    const { data } = await api.post('/admin/partners', body)
    return data
  },
  async adminUpdatePartner(id, body) {
    const { data } = await api.put(`/admin/partners/${id}`, body)
    return data
  },
  async adminDeletePartner(id) {
    await api.delete(`/admin/partners/${id}`)
  },
  async adminCreatePartnerReservation(partnerId, body = {}) {
    const { data } = await api.post(`/admin/partners/${partnerId}/reservations`, body)
    return data
  },
  async adminListPartnerReservations(partnerId) {
    const { data } = await api.get(`/admin/partners/${partnerId}/reservations`)
    return data
  },
  async adminCreatePartnerOperatorInvite(partnerId, email) {
    const { data } = await api.post(`/admin/partners/${partnerId}/operator-invite`, { email })
    return data
  },
  /** Partner staff — crear link de cuenta cliente */
  async partnerStaffCreateReservation(body = {}) {
    const { data } = await api.post('/partner/reservations', body)
    return data
  },

  // Admin API (SuperAdmin only)
  async listAccounts() {
    const { data } = await api.get('/admin/accounts')
    return data
  },
  async impersonate(accountId) {
    const { data } = await api.post('/admin/impersonate', { accountId })
    return data
  },
  async impersonateExit() {
    const { data } = await api.post('/admin/impersonate/exit')
    return data
  },

  createAccountStreamConnection(callbacks) {
    // Ruta /api/stream/account comentada temporalmente
    // let baseURL = api.defaults.baseURL || '/api'
    // if (baseURL.endsWith('/')) baseURL = baseURL.slice(0, -1)
    // const url = `${baseURL}/stream/account`
    // const token = localStorage.getItem(AUTH_TOKEN_KEY)
    //
    // const ctrl = new AbortController()
    // const processEvent = (eventData, onEvent) => {
    //   try {
    //     const data = JSON.parse(eventData)
    //     if (data.type && onEvent) onEvent(data.type, data.data)
    //   } catch (e) {
    //     console.warn('Account stream: parse error', e)
    //   }
    // }
    //
    // fetchEventSource(url, {
    //   signal: ctrl.signal,
    //   headers: token ? { Authorization: `Bearer ${token}` } : {},
    //   onmessage(event) {
    //     processEvent(event.data, callbacks.onEvent)
    //   },
    //   onclose() {
    //     if (callbacks.onClose) callbacks.onClose()
    //   },
    //   onerror(err) {
    //     if (callbacks.onError) callbacks.onError(err?.message || 'Stream error')
    //     throw err
    //   }
    // }).catch(() => {})
    //
    // return {
    //   close: () => ctrl.abort()
    // }

    const ctrl = { abort: () => {} }
    return {
      close: () => ctrl.abort()
    }
  },

  async getAccountMembers() {
    const response = await api.get('/accounts/members')
    return response.data
  },
  async getPendingInvitations() {
    const response = await api.get('/accounts/invitations')
    return response.data
  },
  async inviteMember(email = null, permissions = null) {
    const response = await api.post('/accounts/invite', { email: email || null, permissions })
    return response.data
  },
  async getMemberPermissionsCatalog() {
    const { data } = await api.get('/accounts/member-permissions/catalog')
    return data
  },
  async getMemberPermissions(memberUserId) {
    const { data } = await api.get(`/accounts/members/${memberUserId}/permissions`)
    return data
  },
  async updateMemberPermissions(memberUserId, permissions) {
    const { data } = await api.put(`/accounts/members/${memberUserId}/permissions`, { permissions })
    return data
  },
  async resendInvitation(invitationId) {
    const { data } = await api.post(`/accounts/invitations/${invitationId}/resend`)
    return data
  },
  async revokeInvitation(invitationId) {
    await api.delete(`/accounts/invitations/${invitationId}`)
  },
  async removeMember(userId) {
    await api.delete(`/accounts/members/${userId}`)
  },
  async deleteAccount() {
    const response = await api.delete('/accounts')
    return response.data
  },
  async getApiKeys() {
    const { data } = await api.get('/api-keys')
    return data
  },
  async getWorkspaces() {
    const { data } = await api.get('/workspaces')
    const items = Array.isArray(data?.items) ? data.items : Array.isArray(data?.Items) ? data.Items : Array.isArray(data) ? data : []
    return items.map((x) => ({
      id: x.id ?? x.Id,
      name: String(x.name ?? x.Name ?? '').trim() || 'Workspace',
      isDefault: !!(x.isDefault ?? x.IsDefault),
      isSystem: !!(x.isSystem ?? x.IsSystem),
      environmentKind: normalizeWorkspaceEnvironment(x.environmentKind ?? x.EnvironmentKind)
    })).filter((w) => w.id)
  },

  /** List workspaces for the modal (id + name). Backed by GET /workspaces. */
  async getWorkspaceLookups() {
    const list = await this.getWorkspaces()
    return list.map((w) => ({ id: w.id, name: w.name }))
  },
  async createWorkspace(name, environmentKind = 'sandbox', isDefault = false) {
    const { data } = await api.post('/workspaces', {
      name,
      environmentKind: normalizeWorkspaceEnvironment(environmentKind),
      isDefault
    })
    return data
  },
  /** Client-side selection only — Finbuckle tenant remains the isolation root. */
  async selectWorkspace(workspaceId, displayName = null) {
    const id = String(workspaceId)
    localStorage.setItem(ACTIVE_WORKSPACE_KEY, id)
    const name = displayName != null ? String(displayName).trim() : ''
    if (name) {
      try {
        localStorage.setItem(ACTIVE_WORKSPACE_NAME_KEY, name)
      } catch (_) {
        /* ignore */
      }
    }
    return { workspaceId: id, name }
  },
  async updateWorkspace(id, name, environmentKind = 'sandbox', isDefault = false) {
    const { data } = await api.put(`/workspaces/${id}`, {
      name,
      environmentKind: normalizeWorkspaceEnvironment(environmentKind),
      isDefault
    })
    return data
  },
  async deleteWorkspace(id) {
    const { data } = await api.delete(`/workspaces/${id}`)
    return data
  },
  async getBilling() {
    const { data } = await api.get('/billing')
    return data
  },
  async updateBillingPlan(planCode) {
    const { data } = await api.put('/billing/plan', { planCode })
    return data
  },
  async listBillingInvoices() {
    const { data } = await api.get('/billing/invoices')
    const items = Array.isArray(data?.items) ? data.items : Array.isArray(data?.Items) ? data.Items : []
    return items
  },
  async listIntegrations(workspaceId = null) {
    const params = workspaceId ? { workspaceId } : undefined
    const { data } = await api.get('/integrations', { params })
    return Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : Array.isArray(data?.Items) ? data.Items : []
  },
  async enqueueDataIngest(integrationId, kind = 'all') {
    const { data } = await api.post(`/integrations/${integrationId}/data-ingestion/enqueue`, { kind })
    return data
  },
  async listIngestedCatalog(integrationId, { page = 1, pageSize = 25 } = {}) {
    const { data } = await api.get(`/integrations/${integrationId}/catalog`, { params: { page, pageSize } })
    return data
  },
  async getIngestedCatalogItem(integrationId, id) {
    const { data } = await api.get(`/integrations/${integrationId}/catalog/${encodeURIComponent(id)}`)
    return data
  },

  async startProductOnboardingStream({ title = null, imageFile = null, integrationId = null } = {}) {
    const form = new FormData()
    if (title) form.append('title', title)
    if (integrationId) form.append('integrationId', integrationId)
    if (imageFile) form.append('image', imageFile)

    const tenant = resolveTenantSlug()
    const prefix = tenant ? `/t/${tenant}` : ''
    let baseURL = api.defaults.baseURL || '/api'
    if (baseURL.endsWith('/')) baseURL = baseURL.slice(0, -1)
    const url = `${baseURL}${prefix}/products/onboarding/stream`
    const token = localStorage.getItem(AUTH_TOKEN_KEY)

    return await new Promise((resolve, reject) => {
      let settled = false
      let lastEvent = null
      const ctrl = new AbortController()
      const finish = (value, isError = false) => {
        if (settled) return
        settled = true
        ctrl.abort()
        if (isError) reject(value instanceof Error ? value : new Error(String(value)))
        else resolve(value)
      }

      fetchEventSource(url, {
        method: 'POST',
        signal: ctrl.signal,
        headers: {
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
          [FINBUCKLE_TENANT_HEADER]: tenant || ''
        },
        body: form,
        openWhenHidden: true,
        async onmessage(event) {
          if (!event.data || event.data.startsWith(':')) return
          try {
            const payload = JSON.parse(event.data)
            lastEvent = payload
            if (payload.type === 'approval_needed' || payload.type === 'error') {
              finish(payload)
            }
          } catch (err) {
            finish(err, true)
          }
        },
        onerror(err) {
          if (settled) return
          finish(err instanceof Error ? err : new Error(err?.message || 'Stream error'), true)
          throw err
        },
        onclose() {
          if (!settled) {
            if (lastEvent) finish(lastEvent)
            else finish(new Error('Onboarding stream closed without result'), true)
          }
        }
      }).catch((err) => {
        if (!settled) {
          if (lastEvent) finish(lastEvent)
          else finish(err, true)
        }
      })
    })
  },

  async approveProductOnboarding(workflowId, body) {
    const { data } = await api.post(`/products/onboarding/${encodeURIComponent(workflowId)}/approve`, body)
    return data
  },

  async listIngestedOrders(integrationId, { page = 1, pageSize = 25 } = {}) {
    const { data } = await api.get(`/integrations/${integrationId}/orders`, { params: { page, pageSize } })
    return data
  },
  async getIngestedOrder(integrationId, id) {
    const { data } = await api.get(`/integrations/${integrationId}/orders/${encodeURIComponent(id)}`)
    return data
  },
  async listIngestionsCatalog(integrationId, { pageSize = 25, nextToken = null } = {}) {
    const params = { pageSize }
    if (nextToken) params.nextToken = nextToken
    const { data } = await api.get(`/integrations/${integrationId}/ingestions/catalog`, { params })
    return data
  },
  async getIngestionCatalogItem(integrationId, id) {
    const { data } = await api.get(
      `/integrations/${integrationId}/ingestions/catalog/${encodeURIComponent(id)}`
    )
    return data
  },
  async forceSyncIngestionCatalogItem(integrationId, id) {
    const { data } = await api.post(
      `/integrations/${integrationId}/ingestions/catalog/${encodeURIComponent(id)}/force-sync`
    )
    return data
  },
  async listIngestionsOrders(integrationId, { pageSize = 25, nextToken = null } = {}) {
    const params = { pageSize }
    if (nextToken) params.nextToken = nextToken
    const { data } = await api.get(`/integrations/${integrationId}/ingestions/orders`, { params })
    return data
  },
  async getIngestionOrder(integrationId, id) {
    const { data } = await api.get(
      `/integrations/${integrationId}/ingestions/orders/${encodeURIComponent(id)}`
    )
    return data
  },
  async createIntegration({ provider, name, settings, settingsJson, workspaceId = null }) {
    const body = { provider, name, workspaceId }
    if (settings != null) body.settings = settings
    else if (settingsJson != null) {
      try {
        body.settings = typeof settingsJson === 'string' ? JSON.parse(settingsJson || '{}') : settingsJson
      } catch {
        body.settings = {}
      }
    }
    const { data } = await api.post('/integrations', body)
    return data
  },
  async updateIntegration(id, { name, settings, settingsJson, isActive, workspaceId } = {}) {
    const body = {}
    if (name !== undefined) body.name = name
    if (isActive !== undefined) body.isActive = isActive
    if (workspaceId !== undefined) body.workspaceId = workspaceId
    if (settings != null) body.settings = settings
    else if (settingsJson != null) {
      try {
        body.settings = typeof settingsJson === 'string' ? JSON.parse(settingsJson || '{}') : settingsJson
      } catch {
        body.settings = {}
      }
    }
    const { data } = await api.put(`/integrations/${id}`, body)
    return data
  },
  async deleteIntegration(id) {
    const { data } = await api.delete(`/integrations/${id}`)
    return data
  },

  /** Gravity native store brands — GET /catalog/brands */
  async listCatalogBrands({ name = null, page = 1, pageSize = 20, integrationId = null } = {}) {
    const params = { page, pageSize }
    if (name && String(name).trim()) params.name = String(name).trim()
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get('/catalog/brands', { params })
    const raw = data?.items ?? data?.Items ?? (Array.isArray(data) ? data : [])
    return (Array.isArray(raw) ? raw : []).map((b) => ({
      brandId: String(b.brandId ?? b.BrandId ?? ''),
      name: b.name ?? b.Name ?? null,
      isActive: b.isActive ?? b.IsActive ?? true
    }))
  },
  async autocompleteCatalogBrands(name, integrationId = null) {
    const q = String(name || '').trim()
    if (!q) return []
    const params = { name: q }
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get('/catalog/brands/autocomplete', { params })
    const raw = data?.items ?? data?.Items ?? (Array.isArray(data) ? data : [])
    return (Array.isArray(raw) ? raw : []).map((b) => ({
      brandId: String(b.brandId ?? b.BrandId ?? ''),
      name: b.name ?? b.Name ?? null
    }))
  },
  async getCatalogBrand(brandId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get(`/catalog/brands/${encodeURIComponent(brandId)}`, { params })
    if (!data) return null
    return {
      brandId: String(data.brandId ?? data.BrandId ?? ''),
      name: data.name ?? data.Name ?? null,
      isActive: data.isActive ?? data.IsActive ?? true
    }
  },
  async createCatalogBrand({ name, description = null, isActive = true, imageUrl = null, integrationId = null }) {
    const body = { name, isActive }
    if (description != null) body.description = description
    if (imageUrl != null) body.imageUrl = imageUrl
    if (integrationId) body.integrationId = integrationId
    const { data } = await api.post('/catalog/brands', body)
    return {
      brandId: String(data.brandId ?? data.BrandId ?? ''),
      name: data.name ?? data.Name ?? null,
      isActive: data.isActive ?? data.IsActive ?? true
    }
  },
  async updateCatalogBrand(brandId, { name, description = null, isActive = true, imageUrl = null, integrationId = null }) {
    const body = { name, isActive }
    if (description != null) body.description = description
    if (imageUrl != null) body.imageUrl = imageUrl
    if (integrationId) body.integrationId = integrationId
    const { data } = await api.put(`/catalog/brands/${encodeURIComponent(brandId)}`, body)
    return {
      brandId: String(data.brandId ?? data.BrandId ?? brandId),
      name: data.name ?? data.Name ?? null,
      isActive: data.isActive ?? data.IsActive ?? true
    }
  },
  async deleteCatalogBrand(brandId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.delete(`/catalog/brands/${encodeURIComponent(brandId)}`, { params })
    return data
  },

  /** Gravity EntityAttributes — GET /catalog/attributes */
  async listCatalogAttributes({ name = null, page = 1, pageSize = 20, integrationId = null } = {}) {
    const params = { page, pageSize }
    if (name && String(name).trim()) params.name = String(name).trim()
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get('/catalog/attributes', { params })
    const raw = data?.items ?? data?.Items ?? (Array.isArray(data) ? data : [])
    return (Array.isArray(raw) ? raw : []).map(mapCatalogAttribute)
  },
  async getCatalogAttribute(entityAttributeId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get(
      `/catalog/attributes/${encodeURIComponent(entityAttributeId)}`,
      { params }
    )
    if (!data) return null
    return mapCatalogAttribute(data)
  },
  async createCatalogAttribute(payload) {
    const body = buildCatalogAttributeBody(payload)
    const { data } = await api.post('/catalog/attributes', body)
    return mapCatalogAttribute(data)
  },
  async updateCatalogAttribute(entityAttributeId, payload) {
    const body = buildCatalogAttributeBody(payload)
    const { data } = await api.put(
      `/catalog/attributes/${encodeURIComponent(entityAttributeId)}`,
      body
    )
    return mapCatalogAttribute(data)
  },
  async deleteCatalogAttribute(entityAttributeId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.delete(
      `/catalog/attributes/${encodeURIComponent(entityAttributeId)}`,
      { params }
    )
    return data
  },

  /** Gravity native store categories (tree) — GET /catalog/categories */
  async listCatalogCategories({ name = null, integrationId = null } = {}) {
    const params = {}
    if (name && String(name).trim()) params.name = String(name).trim()
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get('/catalog/categories', { params })
    const raw = data?.items ?? data?.Items ?? (Array.isArray(data) ? data : [])
    return mapCategoryTreeNodes(raw)
  },
  async getCatalogCategory(categoryId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get(`/catalog/categories/${encodeURIComponent(categoryId)}`, { params })
    if (!data) return null
    return {
      categoryId: String(data.categoryId ?? data.CategoryId ?? ''),
      name: data.name ?? data.Name ?? null,
      parentId: data.parentId ?? data.ParentId ?? null,
      url: data.url ?? data.Url ?? null
    }
  },
  async createCatalogCategory({
    name,
    slug = null,
    description = null,
    parentCategoryId = null,
    isActive = true,
    integrationId = null
  }) {
    const body = { name, isActive }
    if (slug != null) body.slug = slug
    if (description != null) body.description = description
    if (parentCategoryId != null) body.parentCategoryId = parentCategoryId
    if (integrationId) body.integrationId = integrationId
    const { data } = await api.post('/catalog/categories', body)
    return {
      categoryId: String(data.categoryId ?? data.CategoryId ?? ''),
      name: data.name ?? data.Name ?? null,
      parentId: data.parentId ?? data.ParentId ?? null,
      url: data.url ?? data.Url ?? null
    }
  },
  async updateCatalogCategory(categoryId, {
    name,
    slug = null,
    description = null,
    parentCategoryId = null,
    isActive = true,
    integrationId = null
  }) {
    const body = { name, isActive }
    if (slug != null) body.slug = slug
    if (description != null) body.description = description
    if (parentCategoryId != null) body.parentCategoryId = parentCategoryId
    if (integrationId) body.integrationId = integrationId
    const { data } = await api.put(`/catalog/categories/${encodeURIComponent(categoryId)}`, body)
    return {
      categoryId: String(data.categoryId ?? data.CategoryId ?? categoryId),
      name: data.name ?? data.Name ?? null,
      parentId: data.parentId ?? data.ParentId ?? null,
      url: data.url ?? data.Url ?? null
    }
  },
  async deleteCatalogCategory(categoryId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.delete(`/catalog/categories/${encodeURIComponent(categoryId)}`, { params })
    return data
  },

  /** Gravity native store products — GET /catalog/products */
  async listCatalogProducts({ page = 1, pageSize = 20, integrationId = null } = {}) {
    const params = { page, pageSize }
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get('/catalog/products', { params })
    const raw = data?.items ?? data?.Items ?? []
    return {
      currentPage: Number(data?.currentPage ?? data?.CurrentPage ?? page),
      pageCount: Number(data?.pageCount ?? data?.PageCount ?? 1),
      pageSize: Number(data?.pageSize ?? data?.PageSize ?? pageSize),
      rowCount: Number(data?.rowCount ?? data?.RowCount ?? (Array.isArray(raw) ? raw.length : 0)),
      items: (Array.isArray(raw) ? raw : []).map((p) => ({
        productId: String(p.productId ?? p.ProductId ?? ''),
        name: p.name ?? p.Name ?? null,
        productStatusName: p.productStatusName ?? p.ProductStatusName ?? null,
        updatedOn: p.updatedOn ?? p.UpdatedOn ?? null,
        imageUrl: p.imageUrl ?? p.ImageUrl ?? null,
        brandName: p.brandName ?? p.BrandName ?? null,
        categoryPath: p.categoryPath ?? p.CategoryPath ?? null,
        stock: p.stock ?? p.Stock ?? null,
        basePrice: p.basePrice ?? p.BasePrice ?? null,
        specialPrice: p.specialPrice ?? p.SpecialPrice ?? null,
        currencySymbol: p.currencySymbol ?? p.CurrencySymbol ?? null
      }))
    }
  },
  async getCatalogProduct(productId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get(`/catalog/products/${encodeURIComponent(productId)}`, { params })
    if (!data) return null
    return {
      productId: String(data.productId ?? data.ProductId ?? ''),
      name: data.name ?? data.Name ?? null,
      productStatusName: data.productStatusName ?? data.ProductStatusName ?? null,
      updatedOn: data.updatedOn ?? data.UpdatedOn ?? null,
      payloadJson: data.payloadJson ?? data.PayloadJson ?? null
    }
  },
  async createCatalogProduct(payload) {
    const body = {
      name: payload.name,
      description: payload.description,
      isActive: payload.isActive !== false,
      showInCatalog: payload.showInCatalog !== false
    }
    if (payload.brandId != null) body.brandId = payload.brandId
    if (payload.brandName != null) body.brandName = payload.brandName
    if (payload.categoryId != null) body.categoryId = payload.categoryId
    if (payload.categoryPath != null) body.categoryPath = payload.categoryPath
    if (payload.imageUrl != null) body.imageUrl = payload.imageUrl
    if (payload.integrationId) body.integrationId = payload.integrationId
    if (Array.isArray(payload.variations) && payload.variations.length) {
      body.variations = payload.variations
    }
    const { data } = await api.post('/catalog/products', body)
    return {
      productId: String(data.productId ?? data.ProductId ?? ''),
      name: data.name ?? data.Name ?? null
    }
  },
  /** Starts generative product content-from-image; returns { processId } (no wait/poll). Accepts file upload or URL. */
  async startGenerativeContentByImageRun({
    imageFile = null,
    imageUrl = null,
    imageAttachmentId = null,
    hint = null,
    brandId = null,
    brandName = null,
    categoryId = null,
    categoryPath = null
  } = {}) {
    if (imageFile) {
      const fd = new FormData()
      fd.append('image', imageFile)
      if (hint) fd.append('hint', hint)
      if (imageUrl) fd.append('imageUrl', imageUrl)
      if (imageAttachmentId) fd.append('imageAttachmentId', imageAttachmentId)
      if (brandId) fd.append('brandId', brandId)
      if (brandName) fd.append('brandName', brandName)
      if (categoryId) fd.append('categoryId', categoryId)
      if (categoryPath) fd.append('categoryPath', categoryPath)
      const { data } = await api.post('/generative/content-by-image/runs', fd)
      return {
        processId: String(data?.processId ?? data?.ProcessId ?? ''),
        agentKey: data?.agentKey ?? data?.AgentKey ?? null,
        imageUrl: data?.imageUrl ?? data?.ImageUrl ?? null,
        imageAttachmentId: data?.imageAttachmentId ?? data?.ImageAttachmentId ?? null
      }
    }
    const body = {}
    if (imageUrl) body.imageUrl = imageUrl
    if (imageAttachmentId) body.imageAttachmentId = imageAttachmentId
    if (hint) body.hint = hint
    if (brandId) body.brandId = brandId
    if (brandName) body.brandName = brandName
    if (categoryId) body.categoryId = categoryId
    if (categoryPath) body.categoryPath = categoryPath
    const { data } = await api.post('/generative/content-by-image/runs', body)
    return {
      processId: String(data?.processId ?? data?.ProcessId ?? ''),
      agentKey: data?.agentKey ?? data?.AgentKey ?? null,
      imageUrl: data?.imageUrl ?? data?.ImageUrl ?? imageUrl ?? null,
      imageAttachmentId: data?.imageAttachmentId ?? data?.ImageAttachmentId ?? imageAttachmentId ?? null
    }
  },
  /** Starts generative product content-from-name/modes; returns { processId } (no wait/poll). */
  async startGenerativeContentRun({
    name,
    modes = null,
    brandId = null,
    brandName = null,
    categoryId = null,
    categoryPath = null
  } = {}) {
    const body = { name }
    if (Array.isArray(modes) && modes.length) body.modes = modes
    if (brandId) body.brandId = brandId
    if (brandName) body.brandName = brandName
    if (categoryId) body.categoryId = categoryId
    if (categoryPath) body.categoryPath = categoryPath
    const { data } = await api.post('/generative/content/runs', body)
    return {
      processId: String(data?.processId ?? data?.ProcessId ?? ''),
      agentKey: data?.agentKey ?? data?.AgentKey ?? null
    }
  },
  /** Starts AI product image generation; returns { processId }. */
  async startGenerativeImagesRun({ name, description = null, count = 1 } = {}) {
    const body = { name, count }
    if (description != null) body.description = description
    const { data } = await api.post('/generative/images/runs', body)
    return {
      processId: String(data?.processId ?? data?.ProcessId ?? ''),
      agentKey: data?.agentKey ?? data?.AgentKey ?? null
    }
  },
  /** Starts stub UGC video generation; returns { processId } (no wait/poll). */
  async startGenerativeVideoRun({ name, description = null, imageUrl = null } = {}) {
    const body = { name }
    if (description != null) body.description = description
    if (imageUrl) body.imageUrl = imageUrl
    const { data } = await api.post('/generative/video/runs', body)
    return {
      processId: String(data?.processId ?? data?.ProcessId ?? ''),
      agentKey: data?.agentKey ?? data?.AgentKey ?? null
    }
  },
  async updateCatalogProduct(productId, payload) {
    const body = {
      name: payload.name,
      description: payload.description,
      isActive: payload.isActive !== false,
      showInCatalog: payload.showInCatalog !== false
    }
    if (payload.brandId != null) body.brandId = payload.brandId
    if (payload.brandName != null) body.brandName = payload.brandName
    if (payload.categoryId != null) body.categoryId = payload.categoryId
    if (payload.categoryPath != null) body.categoryPath = payload.categoryPath
    if (payload.imageUrl != null) body.imageUrl = payload.imageUrl
    if (payload.integrationId) body.integrationId = payload.integrationId
    const { data } = await api.put(`/catalog/products/${encodeURIComponent(productId)}`, body)
    return {
      productId: String(data.productId ?? data.ProductId ?? productId),
      name: data.name ?? data.Name ?? null
    }
  },
  async deleteCatalogProduct(productId, integrationId = null) {
    const params = {}
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.delete(`/catalog/products/${encodeURIComponent(productId)}`, { params })
    return data
  },

  async createApiKey(name, channelDataPipelineId = null) {
    const body = { name }
    if (channelDataPipelineId) body.channelDataPipelineId = channelDataPipelineId
    const { data } = await api.post('/api-keys', body)
    return data
  },
  async revokeApiKey(id) {
    await api.patch(`/api-keys/${id}/revoke`)
  },
  async deleteApiKey(id) {
    await api.delete(`/api-keys/${id}`)
  },

  /** GET /outbound-event-hooks/workspace/{workspaceId} */
  async listOutboundEventHooks(workspaceId) {
    const { data } = await api.get(`/outbound-event-hooks/workspace/${workspaceId}`)
    const list = Array.isArray(data) ? data : []
    return list.map((h) => ({
      id: h.id ?? h.Id,
      workspaceId: h.workspaceId ?? h.WorkspaceId,
      domainEventType: h.domainEventType ?? h.DomainEventType ?? '',
      name: h.name ?? h.Name ?? '',
      targetUrl: h.targetUrl ?? h.TargetUrl ?? '',
      headersJson: h.headersJson ?? h.HeadersJson ?? '{}',
      isEnabled: h.isEnabled ?? h.IsEnabled ?? false,
      createdAt: h.createdAt ?? h.CreatedAt ?? null
    }))
  },

  /** POST /outbound-event-hooks */
  async createOutboundEventHook(body) {
    const { data } = await api.post('/outbound-event-hooks', body)
    return {
      id: data?.id ?? data?.Id,
      name: data?.name ?? data?.Name ?? '',
      pingWarning: data?.pingWarning ?? data?.PingWarning ?? null
    }
  },

  /** PUT /outbound-event-hooks/{id} */
  async updateOutboundEventHook(body) {
    await api.put(`/outbound-event-hooks/${body.id}`, body)
  },

  /** DELETE /outbound-event-hooks/{id}?workspaceId= */
  async deleteOutboundEventHook(id, workspaceId) {
    await api.delete(`/outbound-event-hooks/${id}`, { params: { workspaceId } })
  },

  /**
   * GET /outbound-event-hooks/dispatches — histórico en Table Storage.
   * Partición = account|workspace|domainEventType; paginación con continuationToken.
   */
  async listOutboundHookDispatches({ workspaceId, domainEventType, pageSize = 50, continuationToken }) {
    const params = { workspaceId, domainEventType, pageSize }
    if (continuationToken) params.continuationToken = continuationToken
    const { data } = await api.get('/outbound-event-hooks/dispatches', { params })
    const raw = data?.items ?? data?.Items ?? []
    const items = Array.isArray(raw) ? raw : []
    return {
      items: items.map((row) => ({
        rowKey: row.rowKey ?? row.RowKey ?? '',
        subscriptionId: row.subscriptionId ?? row.SubscriptionId,
        outboxMessageId: row.outboxMessageId ?? row.OutboxMessageId ?? null,
        httpStatus: row.httpStatus ?? row.HttpStatus ?? 0,
        succeeded: row.succeeded ?? row.Succeeded ?? false,
        durationMs: row.durationMs ?? row.DurationMs ?? 0,
        errorMessage: row.errorMessage ?? row.ErrorMessage ?? null,
        requestLogSnippet: row.requestLogSnippet ?? row.RequestLogSnippet ?? null,
        responseLogSnippet:
          row.responseLogSnippet
          ?? row.ResponseLogSnippet
          ?? row.responseBodySnippet
          ?? row.ResponseBodySnippet
          ?? null,
        dispatchedAtUtc: row.dispatchedAtUtc ?? row.DispatchedAtUtc ?? null
      })),
      continuationToken: data?.continuationToken ?? data?.ContinuationToken ?? null,
      hasMore: data?.hasMore ?? data?.HasMore ?? false
    }
  },

  /** POST /outbound-event-hooks/dispatches/resend — re-envía un intento HTTP usando el outbox guardado. */
  async resendOutboundHookDispatch(body) {
    const { data } = await api.post('/outbound-event-hooks/dispatches/resend', body)
    return {
      succeeded: data?.succeeded ?? data?.Succeeded ?? false,
      httpStatus: data?.httpStatus ?? data?.HttpStatus ?? 0,
      durationMs: data?.durationMs ?? data?.DurationMs ?? 0,
      errorMessage: data?.errorMessage ?? data?.ErrorMessage ?? null,
      requestLogSnippet: data?.requestLogSnippet ?? data?.RequestLogSnippet ?? null,
      responseLogSnippet:
        data?.responseLogSnippet
        ?? data?.ResponseLogSnippet
        ?? data?.responseBodySnippet
        ?? data?.ResponseBodySnippet
        ?? null
    }
  },

  async getEnvironmentVariables() {
    const { data } = await api.get('/account/environment-variables')
    return data
  },
  async createEnvironmentVariable({ name, group, value }) {
    const { data } = await api.post('/account/environment-variables', { name, group: group || null, value })
    return data
  },
  async updateEnvironmentVariable(id, { name, group, value }) {
    await api.put(`/account/environment-variables/${id}`, {
      name,
      group: group ?? null,
      value: value !== undefined && value !== '' ? value : null
    })
  },
  async deleteEnvironmentVariable(id) {
    await api.delete(`/account/environment-variables/${id}`)
  },
  async revealEnvironmentVariableValue(id) {
    const { data } = await api.get(`/account/environment-variables/${id}/value`)
    return data
  },


  // Invitations API (public - no auth)
  async getInvitationByToken(token) {
    const response = await api.get(`/invitations/${encodeURIComponent(token)}`)
    return response.data
  },
  async acceptInvitation(token, { displayName, email, password, provider, code, redirectUri }) {
    const response = await api.post(`/invitations/${encodeURIComponent(token)}/accept`, {
      displayName: displayName || null,
      email: email || null,
      password: password || null,
      provider: provider || null,
      code: code || null,
      redirectUri: redirectUri || null
    })
    return response.data
  },

  // Billing API
  async getPlans() {
    const response = await api.get('/billing/plans')
    return response.data
  },
  async getCurrentBilling() {
    const response = await api.get('/billing/current')
    return response.data
  },
  async confirmPayment(sessionId) {
    const response = await api.get('/billing/confirm-payment', { params: { session_id: sessionId } })
    return response.data
  },
  async validateCoupon(code) {
    const response = await api.get('/billing/validate-coupon', { params: { code: code || '' } })
    return response.data
  },
  async applyCouponToAccount(couponCode) {
    const response = await api.post('/billing/apply-coupon', { couponCode })
    return response.data
  },
  async createCheckout(planKey, couponCode = null) {
    const body = { planKey }
    if (couponCode) body.couponCode = couponCode
    const response = await api.post('/billing/checkout', body)
    return response.data
  },
  async createPortal() {
    const response = await api.post('/billing/portal', {})
    return response.data
  },
  async cancelSubscription() {
    const response = await api.post('/billing/cancel')
    return response.data
  },
  async getBillingInvoices(limit = 50) {
    const response = await api.get('/billing/invoices', { params: { limit } })
    return response.data
  },

  // Account usage (dashboard). Pass viewAccountId for partner staff viewing a program account.
  async getAccountUsage(viewAccountId = null) {
    const params = viewAccountId ? { accountId: viewAccountId } : {}
    const response = await api.get('/accounts/usage', { params })
    return response.data
  },

  /** Partner staff: accounts provisioned under the partner (PartnerId). Paginated (pageSize max 20). */
  async getPartnerProgramAccounts({ page = 1, pageSize = 20 } = {}) {
    const { data } = await api.get('/partner/accounts', { params: { page, pageSize } })
    return data
  },

  /** Partner staff: program-account invitations (history + pending). Paginated (pageSize max 20). */
  async getPartnerProgramInvitations({ page = 1, pageSize = 20 } = {}) {
    const { data } = await api.get('/partner/reservations', { params: { page, pageSize } })
    return data
  },

  async partnerStaffResendInvitationEmail(reservationId) {
    const { data } = await api.post(`/partner/reservations/${reservationId}/resend-email`)
    return data
  },

  async partnerStaffGetInvitationLink(reservationId) {
    const { data } = await api.get(`/partner/reservations/${reservationId}/invite-link`)
    return data
  },

  async partnerStaffRevokeInvitation(reservationId) {
    await api.post(`/partner/reservations/${reservationId}/revoke`)
  },

  // Consumption history (credit usage events, paginated)
  async getConsumptionHistory({ page = 1, pageSize = 20, accountId = null } = {}) {
    const params = { page, pageSize }
    if (accountId) params.accountId = accountId
    const response = await api.get('/accounts/consumption-history', { params })
    return response.data
  },
  async getConsumptionEventDetail(id, accountId = null) {
    const params = accountId ? { accountId } : {}
    const response = await api.get(`/accounts/consumption-history/${id}`, { params })
    return response.data
  },

  // Tours API (metadata from YAML, progress in localStorage per account)
  async getTour(tourId) {
    const response = await api.get(`/tours/${tourId}`)
    return response.data
  },

  // Setup API
  async getAvailableIntegrations() {
    const response = await api.get('/integrations/available')
    const data = response.data
    return Array.isArray(data) ? data : []
  },
  async getMyIntegrations() {
    const response = await api.get('/integrations')
    const data = response.data
    return Array.isArray(data) ? data : Array.isArray(data?.items) ? data.items : Array.isArray(data?.Items) ? data.Items : []
  },
  async getIntegrationById(id, { revealSensitiveSettings = false } = {}) {
    const params = revealSensitiveSettings ? { revealSensitiveSettings: 'true' } : {}
    const response = await api.get(`/integrations/${id}`, { params })
    return response.data
  },
  async activateWebhook(integrationId) {
    const { data } = await api.post(`/integrations/${integrationId}/activate-webhook`)
    return data
  },
  async getIntegrationOperations(integrationId, { domain } = {}) {
    const params = domain ? { domain } : {}
    const { data } = await api.get(`/integrations/${integrationId}/operations`, { params })
    return data
  },
  async getUseCaseTemplates({ provider, team } = {}) {
    const params = {}
    if (provider) params.provider = provider
    if (team) params.team = team
    const { data } = await api.get('/integrations/use-case-templates', { params })
    return data
  },
  async fetchMcpTools(settings, integrationId = null) {
    const { data } = await api.post('/integrations/mcp/tools', {
      serverUrl: settings?.mcpServerUrl,
      authType: settings?.authType,
      bearerToken: settings?.bearerToken,
      apiKey: settings?.apiKey,
      apiKeyHeader: settings?.apiKeyHeader,
      integrationId: integrationId ?? undefined
    })
    return data
  },

  async getIntegrationOAuthAuthorizeUrl(integrationId) {
    const { data } = await api.get('/integrations/oauth/authorize', { params: { integrationId } })
    return data
  },

  async integrationOAuthCallback({ integrationId, code, state, redirectUri, callbackQueryParams }) {
    const { data } = await api.post('/integrations/oauth/callback', {
      integrationId,
      code,
      state,
      redirectUri,
      callbackQueryParams
    })
    return data
  },

  async getMcpOAuthAuthorizeUrl(integrationId) {
    return apiService.getIntegrationOAuthAuthorizeUrl(integrationId)
  },

  async mcpOAuthCallback({ integrationId, code, state, redirectUri }) {
    return apiService.integrationOAuthCallback({ integrationId, code, state, redirectUri })
  },

  async fetchOpenApiSchema(schemaUrl) {
    const { data } = await api.post('/integrations/openapi/schema', { schemaUrl })
    return data
  },

  async getEmbeddedSchema(provider, { format = 'openapi', variant, domain } = {}) {
    const params = { format }
    if (variant) params.variant = variant
    if (domain) params.domain = domain
    const { data } = await api.get(`/integrations/schemas/${encodeURIComponent(provider)}`, { params })
    return data
  },

  async fetchPostmanCollection(collectionUrl) {
    const { data } = await api.post('/integrations/postman/collection', { collectionUrl })
    return data
  },

  // MCP Definitions API (TemplateProject)
  async getMyMcps() {
    const { data } = await api.get('/mcps')
    return data
  },
  async getMcpById(id) {
    const { data } = await api.get(`/mcps/${id}`)
    return data
  },
  async getMcpPrompt(mcpId, promptId) {
    const { data } = await api.get(`/mcps/${mcpId}/prompts/${promptId}`)
    return data
  },
  async getMcpTools(mcpId) {
    const { data } = await api.get(`/mcps/${mcpId}/tools`)
    return data
  },
  async getMcpPrompts(mcpId) {
    const { data } = await api.get(`/mcps/${mcpId}/prompts`)
    return data
  },
  async getMcpResources(mcpId) {
    const { data } = await api.get(`/mcps/${mcpId}/resources`)
    return data
  },
  async getMcpTool(mcpId, toolId) {
    const { data } = await api.get(`/mcps/${mcpId}/tools/${toolId}`)
    return data
  },
  async createMcp({ name, description, version, securityType }) {
    const { data } = await api.post('/mcps', { name, description, version, securityType })
    return data
  },
  async importMcp({ integrationId, name, description }) {
    const { data } = await api.post('/mcps/import', { integrationId, name, description })
    return data
  },
  async updateMcp(id, { name, description, version, isEnabled, securityType }) {
    const { data } = await api.put(`/mcps/${id}`, { name, description, version, isEnabled, securityType })
    return data
  },
  async deleteMcp(id) {
    await api.delete(`/mcps/${id}`)
  },
  async associateIntegrationToMcp(mcpId, { integrationId, displayOrder }) {
    const { data } = await api.post(`/mcps/${mcpId}/integrations`, { integrationId, displayOrder })
    return data
  },
  async removeIntegrationFromMcp(mcpId, integrationId) {
    await api.delete(`/mcps/${mcpId}/integrations/${integrationId}`)
  },
  async getMcpIntegrationOperations(mcpId, integrationId) {
    const { data } = await api.get(`/mcps/${mcpId}/integrations/${integrationId}/operations`)
    return data
  },
  async importMcpTools(mcpId, { integrationId, selectedOperationIds }) {
    const { data } = await api.post(`/mcps/${mcpId}/tools/import`, { integrationId, selectedOperationIds })
    return data
  },
  async addMcpTool(mcpId, { integrationId, integrationIds, name, title, description, inputSchema, outputSchema, executionConfig, workflowDefinitionJson, toolType }) {
    const body = { name, title, description, inputSchema: inputSchema ?? '{}', outputSchema, executionConfig }
    if (workflowDefinitionJson) body.workflowDefinitionJson = workflowDefinitionJson
    if (toolType) body.toolType = toolType
    if (integrationIds?.length > 0) body.integrationIds = integrationIds
    else if (integrationId) body.integrationId = integrationId
    const { data } = await api.post(`/mcps/${mcpId}/tools`, body)
    return data
  },
  async addMcpToolsBatch(mcpId, tools) {
    const body = { tools: tools.map(t => ({
      name: t.name,
      title: t.title,
      description: t.description,
      inputSchema: t.inputSchema ?? '{}',
      outputSchema: t.outputSchema,
      executionConfig: t.executionConfig,
      workflowDefinitionJson: t.workflowDefinitionJson,
      integrationIds: t.integrationIds
    })) }
    const { data } = await api.post(`/mcps/${mcpId}/tools/batch`, body)
    return data
  },
  async updateMcpTool(mcpId, toolId, { name, title, description, inputSchema, outputSchema, executionConfig, workflowDefinitionJson, toolType, isEnabled }) {
    const body = { name, title, description, inputSchema: inputSchema ?? '{}', outputSchema, executionConfig }
    if (workflowDefinitionJson !== undefined) body.workflowDefinitionJson = workflowDefinitionJson
    if (toolType !== undefined) body.toolType = toolType
    if (isEnabled !== undefined) body.isEnabled = isEnabled
    const { data } = await api.put(`/mcps/${mcpId}/tools/${toolId}`, body)
    return data
  },
  async setMcpToolEnabled(mcpId, toolId, isEnabled) {
    const { data } = await api.put(`/mcps/${mcpId}/tools/${toolId}/enabled`, { isEnabled })
    return data
  },
  async duplicateMcpTool(mcpId, toolId) {
    const { data } = await api.post(`/mcps/${mcpId}/tools/${toolId}/duplicate`)
    return data
  },
  async deleteMcpTool(mcpId, toolId) {
    await api.delete(`/mcps/${mcpId}/tools/${toolId}`)
  },
  async getMcpToolVersionHistory(mcpId, toolId) {
    const { data } = await api.get(`/mcps/${mcpId}/tools/${toolId}/versions`)
    return data
  },
  async getMcpToolVersionSnapshot(mcpId, toolId, version) {
    const { data } = await api.get(`/mcps/${mcpId}/tools/${toolId}/versions/${version}`)
    return data
  },
  async restoreMcpToolFromVersion(mcpId, toolId, version) {
    const { data } = await api.post(`/mcps/${mcpId}/tools/${toolId}/versions/${version}/restore`)
    return data
  },
  async addMcpPrompt(mcpId, { name, title, description, messageBlocks, argumentsSchema, allowedToolIds, temperature, topP, topK, maxTokens, outputFormat, outputSchema, templateId }) {
    const body = {
      name,
      title,
      description,
      messageBlocks: typeof messageBlocks === 'string' ? messageBlocks : JSON.stringify(messageBlocks ?? []),
      argumentsSchema
    }
    if (allowedToolIds != null) body.allowedToolIds = allowedToolIds
    if (temperature != null) body.temperature = temperature
    if (topP != null) body.topP = topP
    if (topK != null) body.topK = topK
    if (maxTokens != null) body.maxTokens = maxTokens
    if (outputFormat != null) body.outputFormat = outputFormat
    if (outputSchema != null && outputSchema !== '') body.outputSchema = outputSchema
    if (templateId != null) body.templateId = templateId
    const { data } = await api.post(`/mcps/${mcpId}/prompts`, body)
    return data
  },
  async updateMcpPrompt(mcpId, promptId, { name, title, description, messageBlocks, argumentsSchema, allowedToolIds, temperature, topP, topK, maxTokens, outputFormat, outputSchema, isEnabled }) {
    const body = {
      name,
      title,
      description,
      messageBlocks: typeof messageBlocks === 'string' ? messageBlocks : JSON.stringify(messageBlocks ?? []),
      argumentsSchema
    }
    if (allowedToolIds != null) body.allowedToolIds = allowedToolIds
    if (temperature != null) body.temperature = temperature
    if (topP != null) body.topP = topP
    if (topK != null) body.topK = topK
    if (maxTokens != null) body.maxTokens = maxTokens
    if (outputFormat != null) body.outputFormat = outputFormat
    if (outputSchema != null) body.outputSchema = outputSchema
    if (isEnabled !== undefined) body.isEnabled = isEnabled
    const { data } = await api.put(`/mcps/${mcpId}/prompts/${promptId}`, body)
    return data
  },
  async setMcpPromptEnabled(mcpId, promptId, isEnabled) {
    const { data } = await api.put(`/mcps/${mcpId}/prompts/${promptId}/enabled`, { isEnabled })
    return data
  },
  async duplicateMcpPrompt(mcpId, promptId) {
    const { data } = await api.post(`/mcps/${mcpId}/prompts/${promptId}/duplicate`)
    return data
  },
  async deleteMcpPrompt(mcpId, promptId) {
    await api.delete(`/mcps/${mcpId}/prompts/${promptId}`)
  },
  async getMcpPromptVersionHistory(mcpId, promptId) {
    const { data } = await api.get(`/mcps/${mcpId}/prompts/${promptId}/versions`)
    return data
  },
  async getMcpPromptVersionSnapshot(mcpId, promptId, version) {
    const { data } = await api.get(`/mcps/${mcpId}/prompts/${promptId}/versions/${version}`)
    return data
  },
  async restoreMcpPromptFromVersion(mcpId, promptId, version) {
    const { data } = await api.post(`/mcps/${mcpId}/prompts/${promptId}/versions/${version}/restore`)
    return data
  },
  async saveMcpPromptAsTemplate(mcpId, promptId, { templateName }) {
    const { data } = await api.post(`/mcps/${mcpId}/prompts/${promptId}/save-as-template`, { templateName })
    return data
  },
  async getMcpSnippets() {
    const { data } = await api.get('/mcps/snippets')
    return data
  },
  async addMcpSnippet({ name, description, content, displayOrder }) {
    const { data } = await api.post('/mcps/snippets', { name, description, content: content ?? '', displayOrder: displayOrder ?? 0 })
    return data
  },
  async updateMcpSnippet(id, { name, description, content, displayOrder }) {
    const { data } = await api.put(`/mcps/snippets/${id}`, { name, description, content: content ?? '', displayOrder: displayOrder ?? 0 })
    return data
  },
  async deleteMcpSnippet(id) {
    await api.delete(`/mcps/snippets/${id}`)
  },
  async getMcpPromptTemplates() {
    const { data } = await api.get('/mcps/templates')
    return data
  },
  async addMcpPromptTemplate({ name, title, description, template, argumentsSchema }) {
    const { data } = await api.post('/mcps/templates', { name, title, description, template: template ?? '', argumentsSchema: argumentsSchema ?? null })
    return data
  },
  async updateMcpPromptTemplate(id, { name, title, description, template, argumentsSchema }) {
    const { data } = await api.put(`/mcps/templates/${id}`, { name, title, description, template: template ?? '', argumentsSchema: argumentsSchema ?? null })
    return data
  },
  async deleteMcpPromptTemplate(id) {
    await api.delete(`/mcps/templates/${id}`)
  },
  async addMcpResource(mcpId, { uri, name, title, description, mimeType, content }) {
    const { data } = await api.post(`/mcps/${mcpId}/resources`, {
      uri,
      name,
      title,
      description,
      mimeType,
      content
    })
    return data
  },
  async updateMcpResource(mcpId, resourceId, { uri, name, title, description, mimeType, content, isEnabled }) {
    const body = { uri, name, title, description, mimeType, content }
    if (isEnabled !== undefined) body.isEnabled = isEnabled
    const { data } = await api.put(`/mcps/${mcpId}/resources/${resourceId}`, body)
    return data
  },
  async setMcpResourceEnabled(mcpId, resourceId, isEnabled) {
    const { data } = await api.put(`/mcps/${mcpId}/resources/${resourceId}/enabled`, { isEnabled })
    return data
  },
  async duplicateMcpResource(mcpId, resourceId) {
    const { data } = await api.post(`/mcps/${mcpId}/resources/${resourceId}/duplicate`)
    return data
  },
  async deleteMcpResource(mcpId, resourceId) {
    await api.delete(`/mcps/${mcpId}/resources/${resourceId}`)
  },
  async getMcpResourceVersionHistory(mcpId, resourceId) {
    const { data } = await api.get(`/mcps/${mcpId}/resources/${resourceId}/versions`)
    return data
  },
  async getMcpResourceVersionSnapshot(mcpId, resourceId, version) {
    const { data } = await api.get(`/mcps/${mcpId}/resources/${resourceId}/versions/${version}`)
    return data
  },
  async restoreMcpResourceFromVersion(mcpId, resourceId, version) {
    const { data } = await api.post(`/mcps/${mcpId}/resources/${resourceId}/versions/${version}/restore`)
    return data
  },

  async executeToolWorkflow(mcpId, toolId, input, test = false) {
    const { data } = await api.post(`/mcps/${mcpId}/tools/${toolId}/execute`, buildExecuteRequestBody(input, test))
    return data
  },

  async getWebhookLogs(params = {}) {
    const { data } = await api.get('/webhook-logs', { params })
    return data
  },

  /** Chat turn / agent_invoked telemetry (Azure Tables, tenant partition). */
  async listChatEvents(params = {}) {
    const { pageSize = 25, nextToken, threadId, eventType, emptyOnly } = params
    const query = { pageSize }
    if (nextToken) query.nextToken = nextToken
    if (threadId) query.threadId = threadId
    if (eventType) query.eventType = eventType
    if (emptyOnly === true) query.emptyOnly = true
    const { data } = await api.get('/observability/chat-events', { params: query })
    const rawItems = data?.items ?? data?.Items ?? []
    return {
      items: (Array.isArray(rawItems) ? rawItems : []).map((r) => ({
        id: r.id ?? r.Id,
        eventType: r.eventType ?? r.EventType ?? '',
        threadId: r.threadId ?? r.ThreadId ?? '',
        audience: r.audience ?? r.Audience ?? '',
        agentKey: r.agentKey ?? r.AgentKey ?? null,
        tools: r.tools ?? r.Tools ?? [],
        latencyMs: r.latencyMs ?? r.LatencyMs ?? 0,
        empty: r.empty ?? r.Empty ?? false,
        error: r.error ?? r.Error ?? null,
        occurredAt: r.occurredAt ?? r.OccurredAt ?? null
      })),
      pageSize: data?.pageSize ?? data?.PageSize ?? pageSize,
      nextToken: data?.nextToken ?? data?.NextToken ?? null,
      hasMore: data?.hasMore ?? data?.HasMore ?? false
    }
  },

  async getToolExecutions(params = {}) {
    const { data } = await api.get('/observability/tool-executions', { params })
    return data
  },

  async getObservabilityPublishBatchJobs(params = {}) {
    const { take = 50, channelDestinationId, status, serviceKind, period } = params
    const query = { take }
    if (channelDestinationId) query.channelDestinationId = channelDestinationId
    if (status) query.status = status
    if (serviceKind) query.serviceKind = serviceKind
    if (period) query.period = period
    const { data } = await api.get('/observability/publish-batch-jobs', { params: query })
    return Array.isArray(data) ? data : []
  },

  /** Outbox transaccional de catálogo (productos) del workspace (paginación en servidor). */
  async getObservabilityCatalogProductOutbox(params = {}) {
    const { page = 1, pageSize = 25, status, eventKind, period } = params
    const query = { page, pageSize }
    if (status) query.status = status
    if (eventKind) query.eventKind = eventKind
    if (period) query.period = period
    const { data } = await api.get('/observability/catalog-product-outbox', { params: query })
    const rawItems = data?.items ?? data?.Items ?? []
    const list = (Array.isArray(rawItems) ? rawItems : []).map((r) => ({
      id: r.id ?? r.Id,
      messageType: r.messageType ?? r.MessageType ?? '',
      payloadJson: r.payloadJson ?? r.PayloadJson ?? '',
      channelCatalogProductId: r.channelCatalogProductId ?? r.ChannelCatalogProductId ?? null,
      processedAt: r.processedAt ?? r.ProcessedAt ?? null,
      dispatchAfterUtc: r.dispatchAfterUtc ?? r.DispatchAfterUtc ?? null,
      attemptCount: r.attemptCount ?? r.AttemptCount ?? 0,
      lastError: r.lastError ?? r.LastError ?? null,
      lockedUntilUtc: r.lockedUntilUtc ?? r.LockedUntilUtc ?? null,
      createdAt: r.createdAt ?? r.CreatedAt ?? null,
      updatedAt: r.updatedAt ?? r.UpdatedAt ?? null
    }))
    return {
      items: list,
      totalCount: Number(data?.totalCount ?? data?.TotalCount ?? 0) || 0,
      page: Number(data?.page ?? data?.Page ?? page) || 1,
      pageSize: Number(data?.pageSize ?? data?.PageSize ?? pageSize) || pageSize
    }
  },

  /** Outbox transaccional de pedidos del workspace (paginación en servidor). */
  async getObservabilitySaleOrderOutbox(params = {}) {
    const { page = 1, pageSize = 25, status, eventKind, period } = params
    const query = { page, pageSize }
    if (status) query.status = status
    if (eventKind) query.eventKind = eventKind
    if (period) query.period = period
    const { data } = await api.get('/observability/sale-order-outbox', { params: query })
    const rawItems = data?.items ?? data?.Items ?? []
    const list = (Array.isArray(rawItems) ? rawItems : []).map((r) => ({
      id: r.id ?? r.Id,
      messageType: r.messageType ?? r.MessageType ?? '',
      payloadJson: r.payloadJson ?? r.PayloadJson ?? '',
      channelSaleOrderId: r.channelSaleOrderId ?? r.ChannelSaleOrderId ?? null,
      processedAt: r.processedAt ?? r.ProcessedAt ?? null,
      dispatchAfterUtc: r.dispatchAfterUtc ?? r.DispatchAfterUtc ?? null,
      attemptCount: r.attemptCount ?? r.AttemptCount ?? 0,
      lastError: r.lastError ?? r.LastError ?? null,
      lockedUntilUtc: r.lockedUntilUtc ?? r.LockedUntilUtc ?? null,
      createdAt: r.createdAt ?? r.CreatedAt ?? null,
      updatedAt: r.updatedAt ?? r.UpdatedAt ?? null
    }))
    return {
      items: list,
      totalCount: Number(data?.totalCount ?? data?.TotalCount ?? 0) || 0,
      page: Number(data?.page ?? data?.Page ?? page) || 1,
      pageSize: Number(data?.pageSize ?? data?.PageSize ?? pageSize) || pageSize
    }
  },

  /** Ejecuciones de pipelines del workspace (observabilidad). */
  async getPipelineExecutions(params = {}) {
    const { limit = 50, pipelineId, status } = params
    const query = { take: limit }
    if (pipelineId) query.pipelineId = pipelineId
    if (status !== null && status !== undefined && status !== '') query.status = status
    const { data } = await api.get('/observability/pipeline-runs', { params: query })
    const list = Array.isArray(data) ? data : []
    return list.map((r) => {
      const tk = r.triggerKind ?? r.TriggerKind ?? ''
      const manual = String(tk).toLowerCase() === 'manual'
      return {
        id: r.id ?? r.Id,
        pipelineId: r.channelDataPipelineId ?? r.ChannelDataPipelineId,
        pipelineName: r.pipelineName ?? r.PipelineName ?? '',
        startedAt: r.startedAt ?? r.StartedAt,
        finishedAt: r.finishedAt ?? r.FinishedAt ?? r.completedAt ?? r.CompletedAt ?? null,
        status: r.status ?? r.Status,
        recordsProcessed: r.recordsProcessed ?? r.RecordsProcessed ?? 0,
        recordsFailed: r.recordsFailed ?? r.RecordsFailed ?? 0,
        errorMessage: r.errorMessage ?? r.ErrorMessage ?? null,
        executedQuery: r.executedQuery ?? r.ExecutedQuery ?? null,
        triggerType: manual ? 0 : 1
      }
    })
  },

  // --- Data pipeline: extracción y staging (GET/POST /channel-pipelines). Catálogo materializado e import CSV: /data-pipelines. ---
  async getDataPipelines(viewAccountId = null) {
    const params = {}
    if (viewAccountId) params.accountId = viewAccountId
    const { data } = await api.get('/channel-pipelines', { params })
    const list = Array.isArray(data) ? data : []
    return list.map(mapChannelPipelineListToUi)
  },

  /**
   * Listado mínimo (id, name) para selectores. GET /channel-pipelines/lookups — query ligera en servidor.
   * @param {string|null} viewAccountId
   * @param {{ entityType?: string, direction?: string, isActive?: boolean }} [filters]
   */
  async getDataPipelineLookups(viewAccountId = null, filters = {}) {
    const params = {}
    if (viewAccountId) params.accountId = viewAccountId
    if (filters.entityType) params.entityType = String(filters.entityType)
    if (filters.direction) params.direction = String(filters.direction)
    if (filters.isActive === true || filters.isActive === false) params.isActive = filters.isActive
    const { data } = await api.get('/channel-pipelines/lookups', { params })
    const list = Array.isArray(data) ? data : []
    return list
      .map((x) => ({
        id: x.id ?? x.Id,
        name: String(x.name ?? x.Name ?? '').trim() || 'Pipeline'
      }))
      .filter((p) => p.id)
  },

  async getDataPipelineById(id) {
    const { data: p } = await api.get(`/channel-pipelines/${id}`)
    return mapChannelPipelineDetailToUi(p)
  },

  /** Métricas de catálogo (pipeline Outbound productos). Respuesta: { notFound, wrongPipelineKind, data }. */
  async getPipelineCatalogStats(pipelineId, { stockLowThreshold, topCategoryCount, accountId } = {}) {
    const params = {}
    if (stockLowThreshold != null && stockLowThreshold !== '') params.stockLowThreshold = stockLowThreshold
    if (topCategoryCount != null && topCategoryCount !== '') params.topCategoryCount = topCategoryCount
    if (accountId) params.accountId = accountId
    const { data } = await api.get(`/channel-pipelines/${encodeURIComponent(pipelineId)}/stats/catalog`, { params })
    return data
  },

  /** Métricas de pedidos (pipeline Inbound pedidos). Respuesta: { notFound, wrongPipelineKind, missingMarketplaceIntegration, data }. */
  async getPipelineOrderStats(pipelineId, { receivedFromUtc, receivedToUtc, topSkuCount, accountId } = {}) {
    const params = {}
    if (receivedFromUtc) params.receivedFromUtc = receivedFromUtc
    if (receivedToUtc) params.receivedToUtc = receivedToUtc
    if (topSkuCount != null && topSkuCount !== '') params.topSkuCount = topSkuCount
    if (accountId) params.accountId = accountId
    const { data } = await api.get(`/channel-pipelines/${encodeURIComponent(pipelineId)}/stats/orders`, { params })
    return data
  },

  async createDataPipeline(payload) {
    const body = await buildChannelPipelineUpsertBody(payload)
    const { data } = await api.post('/channel-pipelines', body)
    return { id: data?.id, ...mapChannelPipelineDetailToUi(data) }
  },

  async updateDataPipeline(id, payload) {
    if (!id) {
      const err = new Error('Pipeline sin id.')
      err.response = { data: { error: err.message } }
      throw err
    }
    const body = await buildChannelPipelineUpsertBody(payload)
    const { data } = await api.put(`/channel-pipelines/${encodeURIComponent(id)}`, body)
    return mapChannelPipelineDetailToUi(data)
  },

  async deleteDataPipeline(id) {
    await api.delete(`/channel-pipelines/${encodeURIComponent(id)}`)
  },

  /** Encola borrado asíncrono de productos materializados + metadata de specs (solo Outbound + Productos). 202. */
  async enqueueChannelPipelineCatalogPurge(id) {
    const { data, status } = await api.post(
      `/data-pipelines/${encodeURIComponent(id)}/catalog-products/purge-queue`
    )
    return { data, status }
  },

  async patchChannelPipelineActive(id, isActive) {
    const { data } = await api.patch(`/channel-pipelines/${id}/active`, { isActive })
    return mapChannelPipelineListToUi(data)
  },

  async getPipelineFiltersSchema(integrationId, entityType = 'products') {
    if (!integrationId) return { filters: [] }
    const { data } = await api.get('/channel-pipelines/extract-filters-schema', {
      params: { integrationId, entityType: entityType || 'products' }
    })
    return data ?? { filters: [] }
  },

  async getFieldMappingSchema(source, entityType, integrationId = null, marketplaceKey = 'mercadolibre') {
    const params = {
      source: source || '',
      entityType: entityType || 'products',
      marketplaceKey: marketplaceKey || 'mercadolibre'
    }
    if (integrationId) params.integrationId = integrationId
    const { data } = await api.get('/channel-pipelines/field-mapping-schema', { params })
    return data
  },

  async getPipelineCronOptions() {
    const { data } = await api.get('/channel-pipelines/schedule-options')
    const cron = data?.cronOptions ?? data?.CronOptions ?? []
    return {
      options: cron,
      retentionOptions: data?.retentionOptions ?? data?.RetentionOptions ?? [],
      maxRecordsOptions: data?.maxRecordsOptions ?? data?.MaxRecordsOptions ?? [],
      ordersDaysOptions: data?.ordersDaysOptions ?? data?.OrdersDaysOptions ?? []
    }
  },

  async getExtractionPreview(integrationId, entityType, filters = {}) {
    const params = { integrationId, entityType: entityType || 'products' }
    Object.entries(filters || {}).forEach(([k, v]) => {
      if (v != null && String(v).trim()) params[k] = v
    })
    const { data } = await api.get('/channel-pipelines/extraction-preview', { params })
    return data
  },

  async getPipelineRuns(pipelineId, limit = 20) {
    const { data } = await api.get(`/channel-pipelines/${pipelineId}/runs`, { params: { limit } })
    return data ?? []
  },

  async getPipelineStagingRecords(pipelineId, params = {}) {
    const { pageSize = 20, continuationToken } = params
    const query = { pageSize }
    if (continuationToken) query.continuationToken = continuationToken
    const { data } = await api.get(`/channel-pipelines/${pipelineId}/staging-records`, { params: query })
    return data
  },

  /**
   * Productos materializados en catálogo canónico (post staging), paginados por cuenta.
   * @param {object} [listOptions]
   * @param {boolean} [listOptions.unmappedCategoryOnly] Si true, usa POST con `categoryMappings`.
   * @param {'products'|'stocks'|'prices'} [listOptions.catalogListKind] Ruta de listado: catalog-products | catalog-stocks | catalog-prices (mismo payload).
   */
  async getChannelCatalogProducts(page = 1, pageSize = 20, pipelineId = null, filters = {}, listOptions = {}) {
    const f = filters || {}
    const {
      unmappedCategoryOnly = false,
      categoryMappings = null,
      catalogListKind = 'products'
    } = listOptions
    const kind = String(catalogListKind || 'products').toLowerCase()
    const catalogListBase =
      kind === 'stocks'
        ? '/data-pipelines/catalog-stocks'
        : kind === 'prices'
          ? '/data-pipelines/catalog-prices'
          : '/data-pipelines/catalog-products'

    const mapRow = (r) => ({
      ...r,
      categoryId: r.categoryId ?? r.CategoryId ?? null,
      categoryName: r.categoryName ?? r.CategoryName ?? null,
      brandId: r.brandId ?? r.BrandId ?? null,
      brandName: r.brandName ?? r.BrandName ?? null,
      primaryImageUrl: r.primaryImageUrl ?? r.PrimaryImageUrl ?? null
    })

    const normalizeAggregates = (raw) => {
      if (!raw || typeof raw !== 'object') return null
      const sc = raw.sourceCategories ?? raw.SourceCategories ?? []
      const sb = raw.sourceBrands ?? raw.SourceBrands ?? []
      return {
        sourceCategories: (Array.isArray(sc) ? sc : []).map((x) => ({
          categoryId: x.categoryId ?? x.CategoryId ?? '',
          categoryName: x.categoryName ?? x.CategoryName ?? '',
          productCount: Number(x.productCount ?? x.ProductCount ?? 0)
        })),
        sourceBrands: (Array.isArray(sb) ? sb : []).map((x) => ({
          brandId: x.brandId ?? x.BrandId ?? '',
          brandName: x.brandName ?? x.BrandName ?? '',
          productCount: Number(x.productCount ?? x.ProductCount ?? 0)
        }))
      }
    }

    const packResponse = (data) => {
      const items = data?.items ?? data?.Items ?? []
      const total = Number(data?.totalCount ?? data?.TotalCount ?? 0)
      const p = Number(data?.page ?? data?.Page ?? page)
      const ps = Number(data?.pageSize ?? data?.PageSize ?? pageSize)
      const aggregates = normalizeAggregates(data?.aggregates ?? data?.Aggregates ?? null)
      return {
        items: items.map(mapRow),
        totalCount: total,
        page: p,
        pageSize: ps,
        aggregates
      }
    }

    if (unmappedCategoryOnly) {
      const pid = pipelineId != null && String(pipelineId).trim() ? String(pipelineId).trim() : ''
      if (!pid) {
        const err = new Error('pipelineId es obligatorio.')
        err.code = 'PIPELINE_ID_REQUIRED'
        throw err
      }
      const body = {
        page,
        pageSize,
        pipelineId: pid,
        unmappedCategoryOnly: true,
        categoryMappings: (categoryMappings || [])
          .filter((m) => String(m.targetCategoryId ?? m.TargetCategoryId ?? '').trim())
          .map((m) => ({
            sourceCategoryId: m.sourceCategoryId ?? m.SourceCategoryId ?? '',
            sourceCategoryName: m.sourceCategoryName ?? m.SourceCategoryName ?? ''
          }))
      }
      if (f.category != null && f.category !== '') body.category = f.category
      if (f.brand != null && f.brand !== '') body.brand = f.brand
      if (f.priceMin != null && f.priceMin !== '') body.priceMin = f.priceMin
      if (f.priceMax != null && f.priceMax !== '') body.priceMax = f.priceMax
      if (f.stockMin != null && f.stockMin !== '') body.stockMin = f.stockMin
      if (f.stockMax != null && f.stockMax !== '') body.stockMax = f.stockMax
      if (f.nameContains != null && f.nameContains !== '') body.nameContains = f.nameContains
      if (f.skuContains != null && f.skuContains !== '') body.skuContains = f.skuContains
      if (f.primaryImagePresentOnly === true) body.primaryImagePresentOnly = true
      const { data } = await api.post(`${catalogListBase}/list`, body)
      return packResponse(data)
    }

    const pid = pipelineId != null && String(pipelineId).trim() ? String(pipelineId).trim() : ''
    if (!pid) {
      const err = new Error('pipelineId es obligatorio.')
      err.code = 'PIPELINE_ID_REQUIRED'
      throw err
    }
    const params = { page, pageSize, pipelineId: pid, ...f }
    const { data } = await api.get(catalogListBase, { params })
    return packResponse(data)
  },

  async getChannelCatalogProductDetail(catalogProductId) {
    const id = String(catalogProductId || '').trim()
    if (!id) return null
    const { data } = await api.get(`/data-pipelines/catalog-products/${id}`)
    return normalizeChannelCatalogProductDetail(data)
  },

  /**
   * Matriz producto × canales de venta (workspace) para un data pipeline de catálogo.
   * @param {string} pipelineId
   * @param {{ page?: number, pageSize?: number, channelDestinationId?: string|null }} [params]
   */
  async getCatalogPublicationsMatrix(pipelineId, params = {}) {
    const pid = String(pipelineId || '').trim()
    if (!pid) {
      const err = new Error('pipelineId es obligatorio.')
      err.code = 'PIPELINE_ID_REQUIRED'
      throw err
    }
    const page = Number(params.page) > 0 ? Number(params.page) : 1
    const pageSize = Number(params.pageSize) > 0 ? Math.min(100, Number(params.pageSize)) : 25
    const cid = String(params.channelDestinationId || '').trim()
    const { data } = await api.get('/data-pipelines/catalog-publications-matrix', {
      params: {
        pipelineId: pid,
        page,
        pageSize,
        ...(cid ? { channelDestinationId: cid } : {})
      }
    })
    const raw = data && typeof data === 'object' ? data : {}
    const channelsRaw = raw.channels ?? raw.Channels ?? []
    const rowsRaw = raw.rows ?? raw.Rows ?? []
    const channels = Array.isArray(channelsRaw)
      ? channelsRaw.map((c) => ({
          channelDestinationId: c.channelDestinationId ?? c.ChannelDestinationId,
          name: String(c.name ?? c.Name ?? '').trim(),
          marketplaceKey: String(c.marketplaceKey ?? c.MarketplaceKey ?? '').trim(),
          isActive: Boolean(c.isActive ?? c.IsActive ?? false),
          marketplaceIntegrationId: c.marketplaceIntegrationId ?? c.MarketplaceIntegrationId ?? null,
          integrationProvider:
            c.integrationProvider != null && c.integrationProvider !== ''
              ? String(c.integrationProvider).trim()
              : c.IntegrationProvider != null && c.IntegrationProvider !== ''
                ? String(c.IntegrationProvider).trim()
                : null,
          isCatalogPipelineAligned: Boolean(c.isCatalogPipelineAligned ?? c.IsCatalogPipelineAligned ?? false)
        }))
      : []
    const rows = Array.isArray(rowsRaw)
      ? rowsRaw.map((r) => {
          const cellsRaw = r.cellsByChannelDestinationId ?? r.CellsByChannelDestinationId
          const cells = cellsRaw && typeof cellsRaw === 'object' ? { ...cellsRaw } : {}
          const cellsMap = {}
          for (const [k, v] of Object.entries(cells)) {
            if (v == null) {
              cellsMap[k] = null
              continue
            }
            cellsMap[k] = {
              publicationId: v.publicationId ?? v.PublicationId ?? null,
              externalListingId: v.externalListingId ?? v.ExternalListingId ?? null,
              syncState: String(v.syncState ?? v.SyncState ?? ''),
              marketplaceStatus: v.marketplaceStatus ?? v.MarketplaceStatus ?? null
            }
          }
          const img = r.primaryImageUrl ?? r.PrimaryImageUrl
          return {
            catalogProductId: r.catalogProductId ?? r.CatalogProductId,
            title: String(r.title ?? r.Title ?? '').trim(),
            naturalKey: String(r.naturalKey ?? r.NaturalKey ?? '').trim(),
            primaryImageUrl:
              img != null && String(img).trim() !== '' ? String(img).trim() : null,
            cellsByChannelDestinationId: cellsMap
          }
        })
      : []
    return {
      channelDataPipelineId: raw.channelDataPipelineId ?? raw.ChannelDataPipelineId ?? pid,
      pipelineName: String(raw.pipelineName ?? raw.PipelineName ?? '').trim(),
      channels,
      rows,
      page: Number(raw.page ?? raw.Page ?? page),
      pageSize: Number(raw.pageSize ?? raw.PageSize ?? pageSize),
      totalCount: Number(raw.totalCount ?? raw.TotalCount ?? 0)
    }
  },

  /**
   * CSV completo (sin paginación): productId, skuId, productname, sku + columnas por canal {nombre}_{id}_{provider}.
   * @param {string} pipelineId
   * @param {string} [suggestedBaseName]
   * @param {{ channelDestinationId?: string|null }} [opts]
   */
  async downloadPublicationsMatrixCsv(pipelineId, suggestedBaseName = 'publicaciones-matriz', opts = {}) {
    const pid = String(pipelineId || '').trim()
    if (!pid) {
      const err = new Error('pipelineId es obligatorio.')
      err.code = 'PIPELINE_ID_REQUIRED'
      throw err
    }
    const cid = String(opts.channelDestinationId || '').trim()
    const res = await api.get('/data-pipelines/catalog-publications-matrix/export/csv', {
      params: { pipelineId: pid, ...(cid ? { channelDestinationId: cid } : {}) },
      responseType: 'blob'
    })
    const blob = res.data
    let filename = `${String(suggestedBaseName || 'publicaciones-matriz').replace(/[/\\?%*:|"<>]/g, '_')}.csv`
    const cd = res.headers['content-disposition']
    if (cd && typeof cd === 'string') {
      const m = /filename\*=UTF-8''([^;\s]+)|filename="([^"]+)"|filename=([^;\s]+)/i.exec(cd)
      const raw = m?.[1] || m?.[2] || m?.[3]
      if (raw) {
        try {
          filename = decodeURIComponent(raw.replace(/"/g, '').trim())
        } catch {
          filename = raw.replace(/"/g, '').trim()
        }
      }
    }
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    a.rel = 'noopener'
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  },

  async getPipelineCatalogProductChannels(catalogProductId) {
    const id = String(catalogProductId || '').trim()
    if (!id) return { publications: [] }
    const { data } = await api.get(`/data-pipelines/catalog-products/${encodeURIComponent(id)}/channels`)
    const raw = data?.publications ?? data?.Publications ?? []
    const publications = Array.isArray(raw)
      ? raw.map(normalizePipelineCatalogProductChannelPublication).filter((x) => x && x.publicationId)
      : []
    return { publications }
  },

  async getPipelineCatalogProductPublicationEvents(catalogProductId, publicationId, take = 100) {
    const pid = String(catalogProductId || '').trim()
    const pubId = String(publicationId || '').trim()
    if (!pid || !pubId) return { events: [] }
    const { data } = await api.get(
      `/data-pipelines/catalog-products/${encodeURIComponent(pid)}/channels/${encodeURIComponent(pubId)}/events`,
      { params: { take } }
    )
    const raw = data?.events ?? data?.Events ?? []
    const events = Array.isArray(raw)
      ? raw.map(normalizePipelineCatalogProductPublicationEvent).filter((x) => x && x.id)
      : []
    return { events }
  },

  async createChannelCatalogProduct(payload) {
    const pipelineId = String(payload?.pipelineId || '').trim()
    if (!pipelineId) {
      const err = new Error('Selecciona un pipeline de catálogo.')
      err.response = { data: { error: err.message } }
      throw err
    }
    const title = payload?.title != null ? String(payload.title).trim() : ''
    const sku = payload?.sku != null ? String(payload.sku).trim() : ''
    let price = null
    if (payload?.price != null && payload.price !== '') {
      const n = Number(payload.price)
      if (!Number.isNaN(n)) price = n
    }
    let specialPriceRoot = null
    if (payload?.specialPrice != null && payload.specialPrice !== '') {
      const n = Number(payload.specialPrice)
      if (!Number.isNaN(n)) specialPriceRoot = n
    }
    let availableQuantity = null
    if (payload?.availableQuantity != null && payload.availableQuantity !== '') {
      const q = parseInt(String(payload.availableQuantity), 10)
      if (!Number.isNaN(q)) availableQuantity = q
    }
    const mapVariation = (v) => {
      if (!v || typeof v !== 'object') return null
      const skuId = v.skuId != null && String(v.skuId).trim() ? String(v.skuId).trim() : null
      const skuV = v.sku != null && String(v.sku).trim() ? String(v.sku).trim() : null
      let pv = null
      if (v.price != null && v.price !== '') {
        const n = Number(v.price)
        if (!Number.isNaN(n)) pv = n
      }
      let sv = null
      if (v.specialPrice != null && v.specialPrice !== '') {
        const n = Number(v.specialPrice)
        if (!Number.isNaN(n)) sv = n
      }
      let qv = null
      if (v.availableQuantity != null && v.availableQuantity !== '') {
        const qq = parseInt(String(v.availableQuantity), 10)
        if (!Number.isNaN(qq)) qv = qq
      }
      const curV = v.currencyId != null && String(v.currencyId).trim() ? String(v.currencyId).trim() : null
      let specifications = null
      if (v.specifications && typeof v.specifications === 'object' && !Array.isArray(v.specifications)) {
        const o = {}
        for (const [k, val] of Object.entries(v.specifications)) {
          const kk = String(k).trim()
          if (!kk) continue
          o[kk] = val == null ? '' : String(val)
        }
        if (Object.keys(o).length) specifications = o
      }
      let images = null
      if (Array.isArray(v.images) && v.images.length) {
        const il = v.images.map((x) => String(x).trim()).filter(Boolean)
        if (il.length) images = il
      }
      if (!skuId && !skuV && pv == null && sv == null && qv == null && !curV && !specifications && !images) return null
      return {
        skuId,
        sku: skuV,
        price: pv,
        specialPrice: sv,
        availableQuantity: qv,
        currencyId: curV,
        specifications,
        images
      }
    }

    let variations = null
    if (Array.isArray(payload.variations) && payload.variations.length) {
      const list = payload.variations.map(mapVariation).filter(Boolean)
      if (list.length) variations = list
    }

    const trimOrNull = (v) => (v != null && String(v).trim() ? String(v).trim() : null)

    const body = {
      pipelineId,
      title: title || null,
      sku: sku || null,
      price,
      specialPrice: specialPriceRoot,
      currencyId:
        payload?.currencyId != null && String(payload.currencyId).trim()
          ? String(payload.currencyId).trim()
          : null,
      availableQuantity,
      categoryId: trimOrNull(payload?.categoryId),
      categoryName: trimOrNull(payload?.categoryName),
      brandId: trimOrNull(payload?.brandId),
      brandName: trimOrNull(payload?.brandName),
      variations
    }
    if (!body.title && !body.sku) {
      const err = new Error('Indica al menos título o SKU.')
      err.response = { data: { error: err.message } }
      throw err
    }
    const { data } = await api.post('/data-pipelines/catalog-products', body)
    return normalizeChannelCatalogProductDetail(data)
  },

  async uploadChannelCatalogProductMedia(pipelineId, file) {
    const pid = String(pipelineId || '').trim()
    if (!pid || !file) {
      const err = new Error('pipelineId y archivo requeridos')
      err.response = { data: { error: err.message } }
      throw err
    }
    const fd = new FormData()
    fd.append('pipelineId', pid)
    fd.append('file', file)
    const { data } = await api.post('/data-pipelines/catalog-products/media', fd)
    const url = data?.url ?? data?.Url
    if (!url) {
      const err = new Error('La API no devolvió URL de imagen')
      throw err
    }
    return String(url)
  },

  async updateChannelCatalogProductCanonical(catalogProductId, canonicalJson) {
    const id = String(catalogProductId || '').trim()
    if (!id) {
      const err = new Error('catalogProductId requerido')
      throw err
    }
    const body =
      typeof canonicalJson === 'string' ? canonicalJson : JSON.stringify(canonicalJson ?? {}, null, 2)
    const { data } = await api.put(`/data-pipelines/catalog-products/${encodeURIComponent(id)}/canonical-json`, {
      canonicalJson: body
    })
    return normalizeChannelCatalogProductDetail(data)
  },

  async deleteChannelCatalogProduct(catalogProductId) {
    const id = String(catalogProductId || '').trim()
    if (!id) {
      const err = new Error('catalogProductId requerido')
      throw err
    }
    await api.delete(`/data-pipelines/catalog-products/${encodeURIComponent(id)}`)
  },

  /**
   * CSV UTF-8 con BOM. Columnas según exportKind:
   * - products: snapshot completo (Id, ChannelDataPipelineId, EntityId, NaturalKey, Sku, Title, Price, …)
   * - stocks: ChannelDataPipelineId, ProductId, SkuId, Sku, AvailableQuantity
   * - prices: ChannelDataPipelineId, ProductId, SkuId, Sku, Price, SpecialPrice, CurrencyId
   */
  async downloadChannelCatalogCsv(pipelineId, suggestedBaseName = 'catalogo', exportKind = 'products') {
    const pid = String(pipelineId || '').trim()
    if (!pid) {
      const err = new Error('pipelineId requerido')
      throw err
    }
    const ek = String(exportKind || 'products').toLowerCase().trim()
    const res = await api.get('/data-pipelines/catalog-products/export/csv', {
      params: { pipelineId: pid, exportKind: ek === 'stocks' || ek === 'prices' ? ek : 'products' },
      responseType: 'blob'
    })
    const blob = res.data
    let filename = `${String(suggestedBaseName || 'catalogo').replace(/[/\\?%*:|"<>]/g, '_')}.csv`
    const cd = res.headers['content-disposition']
    if (cd && typeof cd === 'string') {
      const m = /filename\*=UTF-8''([^;\s]+)|filename="([^"]+)"|filename=([^;\s]+)/i.exec(cd)
      const raw = m?.[1] || m?.[2] || m?.[3]
      if (raw) {
        try {
          filename = decodeURIComponent(raw.replace(/"/g, '').trim())
        } catch {
          filename = raw.replace(/"/g, '').trim()
        }
      }
    }
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    a.rel = 'noopener'
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  },

  /**
   * Encola import CSV (blob + cola). Respuesta 202 con jobId. importKind: products | stocks | prices.
   */
  async enqueueCatalogCsvImport(pipelineId, importKind, file) {
    const pid = String(pipelineId || '').trim()
    if (!pid) throw new Error('pipelineId requerido')
    if (!file) throw new Error('Archivo requerido')
    const fd = new FormData()
    fd.append('pipelineId', pid)
    fd.append('importKind', String(importKind || 'products').toLowerCase())
    fd.append('file', file)
    const res = await api.post('/data-pipelines/catalog-products/import/csv', fd, {
      validateStatus: (s) => [202, 400, 404, 422].includes(s)
    })
    if (res.status === 404) return null
    if (res.status !== 202) {
      const err = new Error(res.data?.error || res.data?.detail || 'No se pudo encolar el import')
      err.response = res
      throw err
    }
    const data = res.data ?? {}
    return {
      jobId: data.jobId ?? data.JobId,
      importKind: data.importKind ?? data.ImportKind
    }
  },

  /** URL absoluta o same-origin para SSE de progreso del job. */
  getCatalogImportEventsUrl(jobId, afterId = null) {
    const id = String(jobId || '').trim()
    if (!id) return ''
    let base = api.defaults.baseURL || ''
    if (!base || base === '/') base = '/api'
    if (base.endsWith('/')) base = base.slice(0, -1)
    const q = afterId != null && Number(afterId) > 0 ? `?afterId=${Number(afterId)}` : ''
    return `${base}/data-pipelines/catalog-import-jobs/${encodeURIComponent(id)}/events${q}`
  },

  async getPipelineFilePreview(_pipelineId) {
    return { data: new Blob() }
  },

  async runPipelineNow(pipelineId) {
    const { data, status } = await api.post(`/channel-pipelines/${pipelineId}/run-now`)
    return { ...(data || {}), statusCode: status }
  },

  async validatePipelineConfig(_payload) {
    return { valid: true, errors: [] }
  },

  async uploadPipelineFile(_file) {
    const err = new Error('Carga de archivo: no disponible en TemplateProject.')
    err.response = { data: { error: err.message } }
    throw err
  },

  // --- Channel destinations (embudo destino / publicación) ---
  async getChannelDestinationPrerequisites(workspaceId) {
    const ws = String(workspaceId || localStorage.getItem(ACTIVE_WORKSPACE_KEY) || '').trim()
    if (!ws) {
      const err = new Error('Selecciona un workspace en la cabecera.')
      err.response = { data: { error: err.message } }
      throw err
    }
    const { data } = await api.get('/channel-destinations/prerequisites', { params: { workspaceId: ws } })
    const hasOutboundProductPipeline =
      data?.hasOutboundProductPipeline ?? data?.HasOutboundProductPipeline ?? false
    const catalogProductCount = Number(data?.catalogProductCount ?? data?.CatalogProductCount ?? 0)
    const canCreateChannel =
      data?.canCreateChannel ??
      data?.CanCreateChannel ??
      (hasOutboundProductPipeline && catalogProductCount > 0)
    const canCreateBasicChannel =
      data?.canCreateBasicChannel ?? data?.CanCreateBasicChannel ?? true
    return { hasOutboundProductPipeline, catalogProductCount, canCreateChannel, canCreateBasicChannel }
  },

  /**
   * Catálogo de definiciones de reglas declarativas (JSON embebido en el servidor).
   * @param {string} [typePrefix] ej. "channel.marketplace.publish" o "pipeline.catalog.extract_publish"
   */
  async getChannelRuleDefinitions(typePrefix = null) {
    const params = {}
    if (typePrefix != null && String(typePrefix).trim() !== '') {
      params.typePrefix = String(typePrefix).trim()
    }
    const { data } = await api.get('/channel-destinations/rule-definitions', { params })
    return Array.isArray(data) ? data : []
  },

  async getChannelDestinations(params = {}) {
    const ws = localStorage.getItem(ACTIVE_WORKSPACE_KEY)?.trim()
    const query = { ...params }
    if (ws && query.workspaceId == null) query.workspaceId = ws
    const { data } = await api.get('/channel-destinations', { params: query })
    return Array.isArray(data) ? data : []
  },

  async getChannelDestinationById(id) {
    const { data } = await api.get(`/channel-destinations/${id}`)
    return data
  },

  /**
   * Catálogo acotado por filtros guardados en el destino + resumen de publicación Mercado Libre por producto (servidor aplica filtros).
   */
  async getChannelDestinationPublicationCatalog(destinationId, page = 1, pageSize = 20, publicationSync = null) {
    const id = String(destinationId || '').trim()
    if (!id) {
      const err = new Error('destinationId requerido')
      err.code = 'DESTINATION_ID_REQUIRED'
      throw err
    }
    const params = { page, pageSize }
    let sync = null
    if (Array.isArray(publicationSync)) {
      const parts = publicationSync.map((x) => String(x).trim()).filter(Boolean)
      sync = parts.length ? parts.join(',') : null
    } else if (publicationSync != null && String(publicationSync).trim()) {
      sync = String(publicationSync).trim()
    }
    if (sync) params.publicationSync = sync
    const { data } = await api.get(`/channel-destinations/${encodeURIComponent(id)}/publication-catalog`, {
      params
    })
    return normalizeChannelDestinationPublicationCatalog(data)
  },

  /**
   * Lista paginada de precios persistidos por canal (GET /channel-destinations/{id}/price-list).
   */
  async getChannelDestinationPriceList(destinationId, page = 1, pageSize = 20) {
    const id = String(destinationId || '').trim()
    if (!id) {
      const err = new Error('destinationId requerido')
      err.code = 'DESTINATION_ID_REQUIRED'
      throw err
    }
    const { data } = await api.get(`/channel-destinations/${encodeURIComponent(id)}/price-list`, {
      params: { page, pageSize }
    })
    return normalizeChannelDestinationPriceList(data)
  },

  /**
   * CSV lista de precios del canal: si no hay filas guardadas, plantilla desde catálogo filtrado (mismas columnas que CSV precios del canal).
   */
  async downloadChannelDestinationPriceListCsv(destinationId, suggestedBaseName = 'lista-precios-canal') {
    const id = String(destinationId || '').trim()
    if (!id) {
      const err = new Error('destinationId requerido')
      throw err
    }
    const res = await api.get(`/channel-destinations/${encodeURIComponent(id)}/price-list/export.csv`, {
      responseType: 'blob',
      validateStatus: () => true
    })
    const blob = res.data
    if (res.status >= 400) {
      let msg = res.status === 404 ? 'Canal no encontrado.' : `Error ${res.status}`
      if (blob && typeof blob.text === 'function') {
        try {
          const text = await blob.text()
          if (text && (text.trim().startsWith('{') || text.trim().startsWith('['))) {
            const j = JSON.parse(text)
            msg = j.error || j.detail || j.message || msg
          } else if (text && text.length < 2000) {
            msg = text.trim() || msg
          }
        } catch {
          /* keep msg */
        }
      }
      const err = new Error(typeof msg === 'string' ? msg : 'No se pudo descargar el CSV.')
      err.response = res
      throw err
    }
    let filename = `${String(suggestedBaseName || 'lista-precios-canal').replace(/[/\\?%*:|"<>]/g, '_')}.csv`
    const cd = res.headers['content-disposition']
    if (cd && typeof cd === 'string') {
      const m = /filename\*=UTF-8''([^;\s]+)|filename="([^"]+)"|filename=([^;\s]+)/i.exec(cd)
      const raw = m?.[1] || m?.[2] || m?.[3]
      if (raw) {
        try {
          filename = decodeURIComponent(raw.replace(/"/g, '').trim())
        } catch {
          filename = raw.replace(/"/g, '').trim()
        }
      }
    }
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    a.rel = 'noopener'
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  },

  /**
   * Encola import CSV de lista de precios del canal (202 + jobId). Seguimiento: getChannelDestinationPriceListImportEventsUrl.
   */
  async enqueueChannelDestinationPriceListCsvImport(destinationId, file) {
    const id = String(destinationId || '').trim()
    if (!id) throw new Error('destinationId requerido')
    if (!file) throw new Error('Archivo requerido')
    const fd = new FormData()
    fd.append('file', file)
    const res = await api.post(`/channel-destinations/${encodeURIComponent(id)}/price-list/import`, fd, {
      validateStatus: (s) => [202, 400, 404, 422].includes(s)
    })
    if (res.status === 404) return null
    if (res.status !== 202) {
      const msg =
        res.data?.error ||
        res.data?.Error ||
        res.data?.detail ||
        res.data?.title ||
        `Error ${res.status}`
      const err = new Error(typeof msg === 'string' ? msg : 'No se pudo encolar el import.')
      err.response = res
      throw err
    }
    const data = res.data ?? {}
    return { jobId: data.jobId ?? data.JobId }
  },

  /** URL para SSE del import de lista de precios por canal. */
  getChannelDestinationPriceListImportEventsUrl(jobId, afterId = null) {
    const jid = String(jobId || '').trim()
    if (!jid) return ''
    let base = api.defaults.baseURL || ''
    if (!base || base === '/') base = '/api'
    if (base.endsWith('/')) base = base.slice(0, -1)
    const q = afterId != null && Number(afterId) > 0 ? `?afterId=${Number(afterId)}` : ''
    return `${base}/channel-destinations/price-list-import-jobs/${encodeURIComponent(jid)}/events${q}`
  },

  /**
   * CSV UTF-8 con BOM: mismas columnas que export de pipeline, filas acotadas por filtros guardados en el canal (channel id).
   * exportKind: products | stocks | prices
   */
  async downloadChannelDestinationPublicationCatalogCsv(
    destinationId,
    suggestedBaseName = 'catalogo-canal',
    exportKind = 'products'
  ) {
    const id = String(destinationId || '').trim()
    if (!id) {
      const err = new Error('destinationId requerido')
      throw err
    }
    const ek = String(exportKind || 'products').toLowerCase().trim()
    const kind = ek === 'stocks' || ek === 'prices' ? ek : 'products'
    const res = await api.get(
      `/channel-destinations/${encodeURIComponent(id)}/publication-catalog/export/csv`,
      {
        params: { exportKind: kind },
        responseType: 'blob',
        validateStatus: () => true
      }
    )
    const blob = res.data
    if (res.status >= 400) {
      let msg = res.status === 404 ? 'Canal no encontrado.' : `Error ${res.status}`
      if (blob && typeof blob.text === 'function') {
        try {
          const text = await blob.text()
          if (text && (text.trim().startsWith('{') || text.trim().startsWith('['))) {
            const j = JSON.parse(text)
            msg = j.error || j.detail || j.message || msg
          } else if (text && text.length < 2000) {
            msg = text.trim() || msg
          }
        } catch {
          /* keep msg */
        }
      }
      const err = new Error(typeof msg === 'string' ? msg : 'No se pudo descargar el CSV.')
      err.response = res
      throw err
    }
    let filename = `${String(suggestedBaseName || 'catalogo-canal').replace(/[/\\?%*:|"<>]/g, '_')}.csv`
    const cd = res.headers['content-disposition']
    if (cd && typeof cd === 'string') {
      const m = /filename\*=UTF-8''([^;\s]+)|filename="([^"]+)"|filename=([^;\s]+)/i.exec(cd)
      const raw = m?.[1] || m?.[2] || m?.[3]
      if (raw) {
        try {
          filename = decodeURIComponent(raw.replace(/"/g, '').trim())
        } catch {
          filename = raw.replace(/"/g, '').trim()
        }
      }
    }
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    a.rel = 'noopener'
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  },

  async createChannelDestination(body) {
    const { data, status } = await api.post('/channel-destinations', body)
    return { ...(data || {}), statusCode: status }
  },

  async updateChannelDestination(id, body) {
    await api.put(`/channel-destinations/${id}`, body)
  },

  /** En marcha / detenido (IsActive) sin reenviar el resto del wizard. */
  async patchChannelDestinationActiveStatus(id, isActive) {
    const sid = String(id || '').trim()
    if (!sid) {
      const err = new Error('destinationId requerido')
      err.code = 'DESTINATION_ID_REQUIRED'
      throw err
    }
    await api.patch(`/channel-destinations/${encodeURIComponent(sid)}/active-status`, { isActive })
  },

  async deleteChannelDestination(id) {
    await api.delete(`/channel-destinations/${id}`)
  },

  /**
   * Valida publicación Mercado Libre: esquema YAML + opcionalmente catálogo filtrado por pipeline/integración y mapeos.
   * Body: marketplaceKey, buyingMode, listingTypeId, condition, categoryMappings;
   * opcional channelDataPipelineId, marketplaceIntegrationId, catalogFilters (category, brand, priceMin/Max, …).
   */
  async validateChannelListingPublication(body) {
    const { data } = await api.post('/channel-destinations/validate-listing-publication', body)
    return data
  },

  /** Lotes de publicación a marketplace (Mercado Libre, etc.): historial por destino. */
  async listMarketplaceProductPublishBatchJobs(destinationId, take = 30) {
    const { data } = await api.get(
      `/channel-destinations/${encodeURIComponent(destinationId)}/marketplace-products/batch-jobs`,
      { params: { take } }
    )
    return Array.isArray(data) ? data : []
  },

  async getMarketplaceProductPublishBatchJobDetail(destinationId, jobId) {
    const { data } = await api.get(
      `/channel-destinations/${encodeURIComponent(destinationId)}/marketplace-products/batch-jobs/${encodeURIComponent(jobId)}`
    )
    return data
  },

  /** URL para SSE de eventos del job de publicación (mismo patrón que import CSV). */
  getMarketplaceProductPublishJobEventsUrl(destinationId, jobId, afterId = null) {
    const dest = String(destinationId || '').trim()
    const jid = String(jobId || '').trim()
    if (!dest || !jid) return ''
    let base = api.defaults.baseURL || ''
    if (!base || base === '/') base = '/api'
    if (base.endsWith('/')) base = base.slice(0, -1)
    const q =
      afterId != null && Number(afterId) > 0 ? `?afterId=${encodeURIComponent(String(Number(afterId)))}` : ''
    return `${base}/channel-destinations/${encodeURIComponent(dest)}/marketplace-products/batch-jobs/${encodeURIComponent(jid)}/events${q}`
  },

  /**
   * Vista previa del cuerpo JSON para publicar (Meli): mismo orden de ids en items; matchear por catalogProductId.
   * @param {string} destinationId
   * @param {string[]} catalogProductIds
   */
  async previewMarketplaceProductPublishBodies(destinationId, catalogProductIds) {
    const ids = Array.isArray(catalogProductIds) ? catalogProductIds.map((x) => String(x).trim()).filter(Boolean) : []
    const { data } = await api.post(
      `/channel-destinations/${encodeURIComponent(destinationId)}/marketplace-products/preview-bodies`,
      { catalogProductIds: ids }
    )
    return data
  },

  /** Encola un batch de publicación (202 + jobId). */
  async enqueueMarketplaceProductPublishBatch(destinationId, body) {
    const res = await api.post(
      `/channel-destinations/${encodeURIComponent(destinationId)}/marketplace-products/batches`,
      body,
      { validateStatus: (s) => [202, 400, 404].includes(s) }
    )
    return {
      data: res.data || {},
      statusCode: res.status,
      headers: res.headers,
      ok: res.status === 202
    }
  },

  /**
   * Ejecuta una publicación de un solo producto en el proceso API (sin cola ni job).
   * Misma lógica de proveedor que cada ítem del batch.
   * @param {string} destinationId
   * @param {{ catalogProductId: string, serviceKind: string, metadataJson?: string|null, dataJson?: string|null }} body
   */
  async executeMarketplaceProductPublishItem(destinationId, body) {
    const { data } = await api.post(
      `/channel-destinations/${encodeURIComponent(destinationId)}/marketplace-products/publish-item`,
      body
    )
    return data
  },

  /**
   * Estado Mercado Libre, último payload guardado e historial de request/response por producto.
   * @param {string} destinationId
   * @param {string} catalogProductId
   * @param {number} [logTake]
   */
  async getMarketplaceProductListingDetail(destinationId, catalogProductId, logTake) {
    const dest = String(destinationId || '').trim()
    const pid = String(catalogProductId || '').trim()
    const params = {}
    if (logTake != null && Number(logTake) > 0) params.logTake = Number(logTake)
    const { data } = await api.get(
      `/channel-destinations/${encodeURIComponent(dest)}/marketplace-products/${encodeURIComponent(pid)}/listing-detail`,
      { params }
    )
    return data
  },

  /** Mercado Libre: dominios activos para guías de tallas (proxy OAuth). Si omitís siteId, el servidor usa el del JSON de la integración. */
  async getMercadoLibreSizeChartActiveDomains(integrationId, siteId) {
    const params = { integrationId }
    const s = siteId != null && String(siteId).trim() ? String(siteId).trim() : null
    if (s) params.siteId = s
    const { data } = await api.get('/mercadolibre-size-charts/active-domains', { params })
    return data
  },

  /** Mercado Libre: ficha técnica del dominio (proxy OAuth). section opcional (ej. grids). */
  async getMercadoLibreDomainTechnicalSpecs(integrationId, domainId, section) {
    const params = { integrationId }
    const s = section != null && String(section).trim() ? String(section).trim() : null
    if (s) params.section = s
    const { data } = await api.get(
      `/mercadolibre/domains/${encodeURIComponent(domainId)}/technical-specs`,
      { params }
    )
    return data
  },

  /** Mercado Libre: plantilla grids (POST section=grids, proxy OAuth). */
  async postMercadoLibreSizeChartGridsTemplate(integrationId, domainId, body) {
    const { data } = await api.post(
      `/mercadolibre-size-charts/domain/${encodeURIComponent(domainId)}/grids-template`,
      body,
      { params: { integrationId } }
    )
    return data
  },

  /**
   * Borrador JSON para crear guía Mercado Libre: recorre catálogo (variations[].specifications) y mapeos SKU/Producto.
   * @param {object} payload pipelineId, filtros, domainId, siteId, chartTitle, mainAttributeId, attributeMappings[], …
   */
  async buildMercadoLibreSizeChartDraftFromCatalog(payload) {
    const { data } = await api.post('/mercadolibre-size-charts/draft-from-catalog', payload)
    return {
      chartCreateBodyJson: data?.chartCreateBodyJson ?? data?.ChartCreateBodyJson ?? '',
      productsScanned: Number(data?.productsScanned ?? data?.ProductsScanned ?? 0),
      variationsConsidered: Number(data?.variationsConsidered ?? data?.VariationsConsidered ?? 0),
      distinctRows: Number(data?.distinctRows ?? data?.DistinctRows ?? 0),
      warnings: data?.warnings ?? data?.Warnings ?? []
    }
  },

  /**
   * Plan efímero: tasks + borrador. Requiere catalogDomain + mlCategoryId (trazas) y mismos campos que draft-from-catalog.
   * @param {object} payload
   */
  async getMercadoLibreSizeChartCompliancePlan(payload) {
    const { data } = await api.post('/mercadolibre-size-charts/compliance-plan', payload)
    return data
  },

  /**
   * Valida plan + JSON de guía; devuelve missing[]. chartCreateBodyJson opcional.
   * @param {object} payload mismo cuerpo que compliance-plan + chartCreateBodyJson opcional
   */
  async executeMercadoLibreSizeChartCompliancePlan(payload) {
    const { data } = await api.post('/mercadolibre-size-charts/compliance-plan/execute', payload)
    return data
  },

  /** Search/create chart en Mercado Libre; persiste en integración + destinationConfigJson.sizeCharts. */
  async ensureMercadoLibreSizeChart(payload) {
    const { data } = await api.post('/mercadolibre-size-charts/ensure', payload)
    return data
  },

  /**
   * Solo búsqueda (registro + POST /catalog/charts/search); no crea guía. Mismo cuerpo que ensure.
   * @returns {Promise<{ found?: boolean, Found?: boolean, chartId?: string, ChartId?: string, fingerprint?: string, Fingerprint?: string, reused?: boolean, Reused?: boolean, destinationConfigJson?: string, DestinationConfigJson?: string }>}
   */
  async searchMercadoLibreSizeChart(payload) {
    const { data } = await api.post('/mercadolibre-size-charts/search-existing', payload)
    return data
  },

  /**
   * Lista charts[] desde POST /catalog/charts/search (sin persistir).
   * Solo envía workspaceId, marketplaceIntegrationId, domainId, brand, genderId, genderName (sin channelDestinationId).
   * @returns {Promise<{ charts?: { id: string, displayName?: string }[] }>}
   */
  async listMercadoLibreChartsFromSearch(payload) {
    const p = payload && typeof payload === 'object' ? payload : {}
    const body = {
      workspaceId: String(p.workspaceId ?? p.WorkspaceId ?? '').trim(),
      marketplaceIntegrationId: String(p.marketplaceIntegrationId ?? p.MarketplaceIntegrationId ?? '').trim(),
      domainId: String(p.domainId ?? p.DomainId ?? '').trim(),
      brand: String(p.brand ?? p.Brand ?? '').trim(),
      genderId: String(p.genderId ?? p.GenderId ?? '').trim()
    }
    const gn = p.genderName ?? p.GenderName
    if (gn != null && String(gn).trim() !== '') body.genderName = String(gn).trim()
    const { data } = await api.post('/mercadolibre-size-charts/list-from-search', body)
    const charts = data?.charts ?? data?.Charts ?? []
    return {
      charts: charts.map((x) => ({
        id: String(x?.id ?? x?.Id ?? '').trim(),
        displayName: (x?.displayName ?? x?.DisplayName ?? '').trim() || null
      })).filter((x) => x.id)
    }
  },

  /**
   * Escribe gravityTemplateProject.mercadolibre.sizeGridChartId en todos los ChannelCatalogProduct del pipeline (offline).
   */
  async applyMercadoLibreChartToCatalogProducts({ channelDataPipelineId, chartId }) {
    const { data } = await api.post('/mercadolibre-size-charts/apply-chart-to-catalog', {
      channelDataPipelineId,
      chartId
    })
    return {
      productsUpdated: Number(data?.productsUpdated ?? data?.ProductsUpdated ?? 0)
    }
  },

  /** Registro seller-scoped + sección sizeCharts del destino. */
  async getMercadoLibreSizeChartRegistry(channelDestinationId) {
    const { data } = await api.get('/mercadolibre-size-charts/registry', {
      params: { channelDestinationId }
    })
    return data
  },

  /** Facetas desde catálogo canónico (JSON en BD). */
  async getMarketplaceTaxonomyCanonicalCategories(pipelineId = null) {
    const params = {}
    if (pipelineId) params.pipelineId = pipelineId
    const { data } = await api.get('/marketplace-taxonomy/canonical/categories', { params })
    const items = data?.items ?? data?.Items ?? []
    return items.map((x) => ({
      categoryId: x.categoryId ?? x.CategoryId ?? '',
      categoryName: x.categoryName ?? x.CategoryName ?? '',
      productCount: Number(x.productCount ?? x.ProductCount ?? 0)
    }))
  },

  async getMarketplaceTaxonomyCanonicalBrands(pipelineId = null) {
    const params = {}
    if (pipelineId) params.pipelineId = pipelineId
    const { data } = await api.get('/marketplace-taxonomy/canonical/brands', { params })
    const items = data?.items ?? data?.Items ?? []
    return items.map((x) => ({
      brandId: x.brandId ?? x.BrandId ?? '',
      brandName: x.brandName ?? x.BrandName ?? '',
      productCount: Number(x.productCount ?? x.ProductCount ?? 0)
    }))
  },

  /**
   * Sugerencias por parecido de texto: valores del catálogo (categoría id+nombre exacta) vs lista cerrada del atributo del marketplace.
   * @param {object} body integrationId, catalogPipelineId, sourceCategoryId, sourceCategoryName, sourceSpecificationKey,
   *   sourceSpecificationScope (Product|Sku), marketplaceCategoryId, targetAttributeId, fuzzyCutoff?, maxSuggestionsPerSource?, maxCatalogProducts?,
   *   includeUnmappedCategoryProducts?, categoryMappings? (sourceCategoryId, sourceCategoryName, targetCategoryId for mapped rows)
   */
  async suggestCatalogSpecificationValueMappings(body) {
    const { data } = await api.post('/marketplace-taxonomy/canonical/suggest-spec-value-mappings', body)
    const rows = data?.rows ?? data?.Rows ?? []
    return {
      catalogProductsScanned: Number(data?.catalogProductsScanned ?? data?.CatalogProductsScanned ?? 0),
      distinctSourceValues: Number(data?.distinctSourceValues ?? data?.DistinctSourceValues ?? 0),
      rows: rows.map((r) => ({
        sourceValue: r.sourceValue ?? r.SourceValue ?? '',
        targetValueId: r.targetValueId ?? r.TargetValueId ?? '',
        targetValueName: r.targetValueName ?? r.TargetValueName ?? '',
        score: Number(r.score ?? r.Score ?? 0)
      })),
      warnings: data?.warnings ?? data?.Warnings ?? []
    }
  },

  /**
   * Valores distintos de una specification en productos del catálogo (categoría origen exacta).
   * @param {{ pipelineId: string, categoryId?: string|null, categoryName?: string|null, specificationName: string, specificationScope?: string, maxProducts?: number }} p
   */
  async getMarketplaceTaxonomyCanonicalCategorySpecificationDistinctValues(p) {
    const params = {
      pipelineId: String(p.pipelineId || '').trim(),
      categoryId: p.categoryId != null && String(p.categoryId).trim() ? String(p.categoryId).trim() : undefined,
      categoryName: p.categoryName != null && String(p.categoryName).trim() ? String(p.categoryName).trim() : undefined,
      specificationName: String(p.specificationName || '').trim(),
      specificationScope: p.specificationScope != null && String(p.specificationScope).trim()
        ? String(p.specificationScope).trim()
        : 'Product',
      maxProducts: p.maxProducts != null ? Math.min(10000, Math.max(1, Number(p.maxProducts))) : undefined
    }
    const { data } = await api.get('/marketplace-taxonomy/canonical/category-specification-distinct-values', {
      params
    })
    const values = data?.values ?? data?.Values ?? []
    return {
      values: Array.isArray(values) ? values.map((x) => String(x ?? '').trim()).filter(Boolean) : [],
      productsScanned: Number(data?.productsScanned ?? data?.ProductsScanned ?? 0),
      distinctCount: Number(data?.distinctCount ?? data?.DistinctCount ?? 0)
    }
  },

  async getMarketplaceTaxonomyCanonicalCategoryAttributes(pipelineId = null, categoryId = null, categoryName = null) {
    const params = {}
    if (pipelineId) params.pipelineId = pipelineId
    if (categoryId) params.categoryId = categoryId
    if (categoryName) params.categoryName = categoryName
    const { data } = await api.get('/marketplace-taxonomy/canonical/category-attributes', { params })
    const items = data?.items ?? data?.Items ?? []
    const mapItem = (x) => {
      const rawType = String(x.type ?? x.Type ?? 'specification').toLowerCase()
      const type = rawType === 'canonical' ? 'canonical' : 'specification'
      return {
        type,
        specificationScope: x.specificationScope ?? x.SpecificationScope ?? 'Product',
        categoryId: x.categoryId ?? x.CategoryId ?? '',
        categoryName: x.categoryName ?? x.CategoryName ?? '',
        attributeName: x.attributeName ?? x.AttributeName ?? '',
        occurrenceCount:
          x.occurrenceCount != null && x.occurrenceCount !== ''
            ? Number(x.occurrenceCount ?? x.OccurrenceCount)
            : null,
        canonicalSpecificationsPath:
          x.canonicalSpecificationsPath ?? x.CanonicalSpecificationsPath ?? '',
        jsonKey: x.jsonKey ?? x.JsonKey ?? '',
        alternateJsonKey: x.alternateJsonKey ?? x.AlternateJsonKey ?? null,
        jsonPath: x.jsonPath ?? x.JsonPath ?? '',
        displayName: x.displayName ?? x.DisplayName ?? '',
        valueKind: x.valueKind ?? x.ValueKind ?? '',
        fieldGroup: x.fieldGroup ?? x.FieldGroup ?? '',
        summary: x.summary ?? x.Summary ?? '',
        catalogColumn: x.catalogColumn ?? x.CatalogColumn ?? null
      }
    }
    return {
      items: Array.isArray(items) ? items.map(mapItem) : []
    }
  },

  /** Taxonomía del marketplace (p. ej. API pública Mercado Libre). */
  async getMarketplaceTaxonomyMarketplaceCategories(integrationId, opts = {}) {
    const params = {
      integrationId,
      ...opts
    }
    const { data } = await api.get('/marketplace-taxonomy/marketplace/categories', { params })
    const items = data?.items ?? data?.Items ?? []
    return {
      items: items.map(mapMarketplaceTaxonomyCategoryDto).filter(Boolean),
      warning: data?.warning ?? data?.Warning ?? null,
      fullTree: data?.fullTree === true || data?.FullTree === true
    }
  },

  /** Detalle por ID (Mercado Libre: nombre + path_from_root como rootPath). */
  async getMarketplaceTaxonomyMarketplaceCategoryDetail(integrationId, marketplaceCategoryId) {
    const mid = String(marketplaceCategoryId || '').trim()
    if (!mid) return null
    const { data } = await api.get(`/marketplace-taxonomy/marketplace/categories/${encodeURIComponent(mid)}/detail`, {
      params: { integrationId }
    })
    return {
      id: data?.id ?? data?.Id ?? '',
      name: data?.name ?? data?.Name ?? '',
      rootPath: data?.rootPath ?? data?.RootPath ?? null,
      catalogDomain: data?.catalogDomain ?? data?.CatalogDomain ?? null
    }
  },

  async getMarketplaceTaxonomyMarketplaceCategoryAttributes(integrationId, marketplaceCategoryId) {
    const { data } = await api.get(
      `/marketplace-taxonomy/marketplace/categories/${encodeURIComponent(String(marketplaceCategoryId))}/attributes`,
      { params: { integrationId } }
    )
    const list = Array.isArray(data) ? data : []
    const mapped = list.map((x) => ({
      id: x.id ?? x.Id ?? '',
      name: x.name ?? x.Name ?? '',
      isRequired: Boolean(x.isRequired ?? x.IsRequired),
      valueType: x.valueType ?? x.ValueType ?? null,
      /** Mercado Libre: tags.variation_attribute o allow_variations */
      isVariationLevel: Boolean(x.isVariationLevel ?? x.IsVariationLevel),
      allowedValues: Array.isArray(x.allowedValues ?? x.AllowedValues)
        ? (x.allowedValues ?? x.AllowedValues).map((v) => ({
            id: v.id ?? v.Id ?? '',
            name: v.name ?? v.Name ?? ''
          }))
        : []
    }))
    mapped.sort((a, b) => {
      const sa = a.isRequired ? 1 : 0
      const sb = b.isRequired ? 1 : 0
      if (sb !== sa) return sb - sa
      return String(a.name || a.id || '').localeCompare(String(b.name || b.id || ''), undefined, { sensitivity: 'base' })
    })
    return mapped
  },

  async getMarketplaceTaxonomyMarketplaceBrands(integrationId, opts = {}) {
    const params = { integrationId, ...opts }
    const { data } = await api.get('/marketplace-taxonomy/marketplace/brands', { params })
    const items = data?.items ?? data?.Items ?? []
    return {
      items: items.map((x) => ({
        id: x.id ?? x.Id ?? '',
        name: x.name ?? x.Name ?? ''
      })),
      warning: data?.warning ?? data?.Warning ?? null
    }
  },

  /** Lista reglas de negocio de canal por cuenta. Sin filtro workspace: el wizard puede elegir cualquier regla de la cuenta. */
  async getChannelBusinessRules(params = {}) {
    const { data } = await api.get('/channel-business-rules', { params })
    return Array.isArray(data) ? data : []
  },

  /** Pedidos de canal del workspace activo (X-Workspace-Id). */
  async getChannelSaleOrderOriginMappingSchema(integrationId) {
    const { data } = await api.get('/channel-sale-orders/origin-mapping-schema', {
      params: { integrationId }
    })
    return normalizeChannelSaleOrderOriginSchema(data)
  },

  /** Estados de pedido distintos en staging para la integración de canal indicada (workspace actual). */
  async getChannelSaleOrderDistinctOrderStatuses(integrationId) {
    const { data } = await api.get('/channel-sale-orders/distinct-order-statuses', {
      params: { integrationId }
    })
    const raw = data?.items ?? data?.Items ?? data
    return Array.isArray(raw) ? raw.map((s) => String(s)) : []
  },

  async listChannelSaleOrders(params = {}) {
    const { data } = await api.get('/channel-sale-orders', { params })
    const d = data ?? {}
    const items = Array.isArray(d.items ?? d.Items) ? d.items ?? d.Items : []
    return {
      items: items.map(normalizeChannelSaleOrderListItem),
      page: Number(d.page ?? d.Page ?? 1),
      pageSize: Number(d.pageSize ?? d.PageSize ?? 20),
      totalCount: Number(d.totalCount ?? d.TotalCount ?? 0)
    }
  },

  async getChannelSaleOrder(orderId) {
    const { data } = await api.get(`/channel-sale-orders/${encodeURIComponent(orderId)}`)
    return normalizeChannelSaleOrderDetail(data)
  },

  async syncChannelSaleOrder(orderId) {
    const { data } = await api.post(`/channel-sale-orders/${encodeURIComponent(orderId)}/sync`)
    return normalizeChannelSaleOrderDetail(data)
  },

  async listPublishedPolicies() {
    const { data } = await api.get('/policies')
    const items = data?.items ?? data?.Items ?? []
    return Array.isArray(items) ? items : []
  },

  async startPolicyEval(id) {
    const { data } = await api.post(`/policies/jobs/${encodeURIComponent(id)}/eval`)
    return data
  },

  async getPolicyEvalItems() {
    const { data } = await api.get('/policies/eval-items')
    const items = data?.items ?? data?.Items ?? []
    return {
      version: data?.version ?? data?.Version ?? '',
      items: Array.isArray(items) ? items : []
    }
  },

  async savePolicyEvalItems(items) {
    const { data } = await api.put('/policies/eval-items', { items })
    return data
  },

  async listPolicyJobEvals(id) {
    const { data } = await api.get(`/policies/jobs/${encodeURIComponent(id)}/evals`)
    const items = data?.items ?? data?.Items ?? []
    return Array.isArray(items) ? items : []
  },

  async downloadPolicyFile(id, fileName) {
    const response = await api.get(`/policies/jobs/${encodeURIComponent(id)}/file`, { responseType: 'blob' })
    const url = URL.createObjectURL(response.data)
    const link = document.createElement('a')
    link.href = url
    link.download = fileName || 'documento'
    document.body.appendChild(link)
    link.click()
    link.remove()
    URL.revokeObjectURL(url)
  },

  async uploadPolicy(file, type, effectiveFrom) {
    const form = new FormData()
    form.append('file', file)
    form.append('type', type)
    form.append('effectiveFrom', effectiveFrom)
    const { data } = await api.post('/policies', form, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    return data
  },

  async getPolicyJob(id) {
    const { data } = await api.get(`/policies/jobs/${encodeURIComponent(id)}`)
    return data
  },

  async publishPolicy(id, text) {
    const { data } = await api.post(`/policies/jobs/${encodeURIComponent(id)}/publish`, { text })
    return data
  }
}

/**
 * User-facing message from an API error. Supports RFC 7807 ProblemDetails (detail) and legacy error field.
 * @param {import('axios').AxiosError} error
 * @returns {string}
 */
export function getApiErrorMessage(error) {
  if (!error) return ''
  const d = error.response?.data
  if (d && typeof d === 'object') {
    if (typeof d.detail === 'string' && d.detail.trim()) return d.detail.trim()
    if (typeof d.title === 'string' && d.title.trim() && Array.isArray(d.errors)) {
      const parts = [d.title.trim()]
      for (const e of d.errors.slice(0, 8)) {
        if (typeof e === 'string' && e.trim()) parts.push(e.trim())
        else if (e && typeof e === 'object') {
          const code = typeof e.code === 'string' ? e.code : ''
          const msg = typeof e.message === 'string' ? e.message : ''
          const one = [code && `[${code}]`, msg].filter(Boolean).join(' ').trim()
          if (one) parts.push(one)
        }
      }
      if (parts.length > 1) return parts.join('\n')
    }
    if (typeof d.error === 'string') return d.error
  }
  return error.message || ''
}

/** Aplana árbol anidado (children) a filas para lógica que aún usa parentId (fullTree). */
export function flattenMarketplaceTaxonomyCategoryTree(items) {
  const acc = []
  function walk(nodes) {
    for (const x of nodes || []) {
      if (!x?.id) continue
      acc.push({
        id: x.id,
        name: x.name,
        parentId: x.parentId ?? null,
        domainId: x.domainId ?? null,
        domainName: x.domainName ?? null
      })
      if (Array.isArray(x.children) && x.children.length) walk(x.children)
    }
  }
  walk(items)
  return acc
}

function mapCatalogAttribute(a) {
  if (!a) return null
  return {
    entityAttributeId: String(a.entityAttributeId ?? a.EntityAttributeId ?? ''),
    entityName: a.entityName ?? a.EntityName ?? null,
    key: a.key ?? a.Key ?? null,
    name: a.name ?? a.Name ?? null,
    description: a.description ?? a.Description ?? null,
    label: a.label ?? a.Label ?? null,
    values: a.values ?? a.Values ?? null,
    isMultiOption: a.isMultiOption ?? a.IsMultiOption ?? false,
    specificationType: Number(a.specificationType ?? a.SpecificationType ?? 0),
    specificationTypeName: a.specificationTypeName ?? a.SpecificationTypeName ?? null,
    order: Number(a.order ?? a.Order ?? 0),
    required: a.required ?? a.Required ?? false,
    isPublic: a.isPublic ?? a.IsPublic ?? false,
    sectionGroup: a.sectionGroup ?? a.SectionGroup ?? null
  }
}

function buildCatalogAttributeBody({
  entityName,
  name,
  isMultiOption = false,
  specificationType,
  required = false,
  description = null,
  label = null,
  values = null,
  order = 0,
  isPublic = false,
  sectionGroup = null,
  integrationId = null
}) {
  const body = {
    entityName,
    name,
    isMultiOption: !!isMultiOption,
    specificationType: Number(specificationType),
    required: !!required,
    order: Number(order) || 0,
    isPublic: !!isPublic
  }
  if (description != null) body.description = description
  if (label != null) body.label = label
  if (values != null) body.values = values
  if (sectionGroup != null) body.sectionGroup = sectionGroup
  if (integrationId) body.integrationId = integrationId
  return body
}

function mapCategoryTreeNodes(nodes) {
  if (!Array.isArray(nodes)) return []
  return nodes.map((n) => {
    const childrenRaw = n.children ?? n.Children ?? []
    return {
      categoryId: String(n.categoryId ?? n.CategoryId ?? ''),
      name: n.name ?? n.Name ?? null,
      parentId: n.parentId ?? n.ParentId ?? null,
      url: n.url ?? n.Url ?? null,
      children: mapCategoryTreeNodes(Array.isArray(childrenRaw) ? childrenRaw : [])
    }
  })
}

export { api }
export default apiService

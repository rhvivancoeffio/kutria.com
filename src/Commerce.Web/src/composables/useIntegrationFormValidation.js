/**
 * Validación y visibilidad de formularios de integración (Conectar / Editar).
 * Una sola fuente de verdad para OpenAPI, Postman y CustomAPI.
 */

const OPENAPI_PROVIDER_KEYS = ['Dynamic', 'OpenAPI']
const POSTMAN_PROVIDER_KEY = 'Postman'
const CUSTOM_API_PROVIDER_KEY = 'CustomAPI'

const OPENAPI_SCHEMA_PHASE1_KEYS = ['schemaSource', 'openApiSchemaUrl']
const OPENAPI_SCHEMA_PHASE2_KEYS = [
  'baseUrl', 'authType', 'bearerToken', 'apiKey', 'apiKeyHeader',
  'oauthTokenUrl', 'oauthClientId', 'oauthClientSecret', 'oauthRedirectUri',
  'requestTimeoutSeconds', 'acceptHeader'
]

const POSTMAN_PHASE1_KEYS = ['collectionSource', 'postmanCollectionUrl']
const POSTMAN_PHASE2_KEYS = [
  'baseUrl', 'collectionVariables', 'authType', 'bearerToken', 'apiKey', 'apiKeyHeader',
  'oauthTokenUrl', 'oauthClientId', 'oauthClientSecret', 'oauthRedirectUri',
  'requestTimeoutSeconds', 'acceptHeader'
]

function isOpenApiSchemaConfigured(settings) {
  const src = settings?.schemaSource ?? ''
  if (src === 'url') return !!(settings?.openApiSchemaUrl?.trim())
  if (src === 'inline') return !!(settings?.openApiSchema?.trim())
  return false
}

function isPostmanCollectionConfigured(settings) {
  const src = settings?.collectionSource ?? ''
  if (src === 'url') return !!(settings?.postmanCollectionUrl?.trim())
  if (src === 'inline') return !!(settings?.postmanCollection?.trim())
  return false
}

/**
 * True if visibleWhen is empty or every (k,v) in visibleWhen matches settings[k] === v.
 * @param {Record<string, string> | null | undefined} visibleWhen - from setting.visibleWhen (YAML metadata)
 * @param {Record<string, unknown>} settings - current form settings
 */
function matchesVisibleWhen(visibleWhen, settings) {
  if (!visibleWhen || typeof visibleWhen !== 'object' || Object.keys(visibleWhen).length === 0) return true
  for (const [k, v] of Object.entries(visibleWhen)) {
    const current = settings?.[k]
    if (String(current ?? '') !== String(v ?? '')) return false
  }
  return true
}

function isMcpSettingVisible(setting, settings) {
  if (setting.visibleWhen && Object.keys(setting.visibleWhen).length > 0) return matchesVisibleWhen(setting.visibleWhen, settings)
  const key = setting.key
  const auth = settings?.authType ?? ''
  if (['mcpServerUrl', 'description', 'authType'].includes(key)) return true
  if (key === 'bearerToken') return auth === 'bearer'
  if (['apiKey', 'apiKeyHeader'].includes(key)) return auth === 'apiKey'
  if (['oauthClientId', 'oauthClientSecret'].includes(key)) return auth === 'oauth'
  return true
}

/**
 * Indica si un setting debe mostrarse según provider (OpenAPI, Postman, CustomAPI, MCP) y estado del formulario.
 */
function isApiSettingVisible(setting, settings, meta, postmanProviderKey = 'Postman') {
  const isOpenApi = OPENAPI_PROVIDER_KEYS.includes(meta?.key) || meta?.name === 'OpenAPI'
  const isCustomApi = meta?.key === CUSTOM_API_PROVIDER_KEY
  const isPostman = meta?.key === postmanProviderKey
  const isMcp = meta?.key === 'MCP'

  if (meta?.credentialsSource === 'platform' && ['clientId', 'clientSecret'].includes(setting.key)) return false
  if (isMcp) return isMcpSettingVisible(setting, settings)

  if (isCustomApi) {
    const key = setting.key
    if (key === 'customActions') return false
    if (setting.visibleWhen && Object.keys(setting.visibleWhen).length > 0) return matchesVisibleWhen(setting.visibleWhen, settings)
    const auth = settings?.authType ?? ''
    if (['baseUrl', 'authType'].includes(key)) return true
    if (key === 'bearerToken') return auth === 'bearer'
    if (['apiKey', 'apiKeyHeader'].includes(key)) return auth === 'apiKey'
    if (['oauthTokenUrl', 'oauthClientId', 'oauthClientSecret', 'oauthRedirectUri'].includes(key)) return auth === 'oauth2'
    return true
  }

  if (isPostman) {
    const key = setting.key
    const collectionSource = settings?.collectionSource ?? ''
    const collectionConfigured = isPostmanCollectionConfigured(settings)
    const auth = settings?.authType ?? ''
    if (key === 'collectionSource') return true
    if (key === 'postmanCollectionUrl') return collectionSource === 'url'
    if (key === 'postmanCollection') return false
    if (!POSTMAN_PHASE2_KEYS.includes(key)) return true
    if (!collectionConfigured) return false
    if (setting.visibleWhen && Object.keys(setting.visibleWhen).length > 0) return matchesVisibleWhen(setting.visibleWhen, settings)
    if (key === 'bearerToken') return auth === 'bearer'
    if (['apiKey', 'apiKeyHeader'].includes(key)) return auth === 'apiKey'
    if (['oauthTokenUrl', 'oauthClientId', 'oauthClientSecret', 'oauthRedirectUri'].includes(key)) return auth === 'oauth2'
    return true
  }

  if (!isOpenApi) return true

  const key = setting.key
  const auth = settings?.authType ?? ''
  const schemaSource = settings?.schemaSource ?? ''
  const schemaConfigured = isOpenApiSchemaConfigured(settings)

  if (key === 'schemaSource') return true
  if (key === 'openApiSchemaUrl') return schemaSource === 'url'
  if (key === 'openApiSchema') return false
  if (!OPENAPI_SCHEMA_PHASE2_KEYS.includes(key)) return true
  if (!schemaConfigured) return false
  if (setting.visibleWhen && Object.keys(setting.visibleWhen).length > 0) return matchesVisibleWhen(setting.visibleWhen, settings)
  if (key === 'bearerToken') return auth === 'bearer'
  if (['apiKey', 'apiKeyHeader'].includes(key)) return auth === 'apiKey'
  if (['oauthTokenUrl', 'oauthClientId', 'oauthClientSecret', 'oauthRedirectUri'].includes(key)) return auth === 'oauth2'
  return true
}

function isSettingValueEmpty(setting, value) {
  const v = value ?? ''
  if (setting.type === 'radio') return !v
  if (setting.type === 'select' || setting.type === 'list') return v === '' || !v
  return !String(v).trim()
}

/**
 * Valida si el formulario Conectar puede guardarse (settings + nombre + customActions si aplica).
 */
function isConnectFormValid(settings, meta, customActionsForm, postmanProviderKey = 'Postman') {
  const isOpenApi = OPENAPI_PROVIDER_KEYS.includes(meta?.key) || meta?.name === 'OpenAPI'

  if (isOpenApi) {
    const src = settings.schemaSource ?? ''
    if (src === 'url' && !(settings.openApiSchemaUrl ?? '').trim()) return false
    if (src === 'inline' && !(settings.openApiSchema ?? '').trim()) return false
    if (isOpenApiSchemaConfigured(settings) && !(settings.baseUrl ?? '').trim()) return false
  }
  if (meta?.key === CUSTOM_API_PROVIDER_KEY) {
    const actions = customActionsForm ?? []
    const valid = actions.some(a => (a.key ?? '').trim() && (a.path ?? '').trim())
    if (!valid) return false
  }
  if (meta?.key === postmanProviderKey) {
    const src = settings.collectionSource ?? ''
    if (src === 'url' && !(settings.postmanCollectionUrl ?? '').trim()) return false
    if (src === 'inline' && !(settings.postmanCollection ?? '').trim()) return false
    if (isPostmanCollectionConfigured(settings) && !(settings.baseUrl ?? '').trim()) return false
  }
  return true
}

/**
 * Valida si el formulario Editar puede guardarse.
 */
function isEditFormValid(settings, meta, editCustomActionsForm, postmanProviderKey = 'Postman') {
  return isConnectFormValid(settings, meta, editCustomActionsForm, postmanProviderKey)
}

function getConnectSettingsPhase1OrAll(meta) {
  const settings = meta?.settings ?? []
  const isOpenApi = OPENAPI_PROVIDER_KEYS.includes(meta?.key) || meta?.name === 'OpenAPI'
  const isPostman = meta?.key === POSTMAN_PROVIDER_KEY
  const isCustomApi = meta?.key === CUSTOM_API_PROVIDER_KEY
  if (isOpenApi) return settings.filter(s => OPENAPI_SCHEMA_PHASE1_KEYS.includes(s.key))
  if (isPostman) return settings.filter(s => POSTMAN_PHASE1_KEYS.includes(s.key))
  if (isCustomApi) return settings.filter(s => s.key !== 'customActions')
  return settings
}

function getConnectSettingsForPhase2(meta) {
  const settings = meta?.settings ?? []
  const isOpenApi = OPENAPI_PROVIDER_KEYS.includes(meta?.key) || meta?.name === 'OpenAPI'
  const isPostman = meta?.key === POSTMAN_PROVIDER_KEY
  const isCustomApi = meta?.key === CUSTOM_API_PROVIDER_KEY
  if (isOpenApi) return settings.filter(s => OPENAPI_SCHEMA_PHASE2_KEYS.includes(s.key))
  if (isPostman) return settings.filter(s => POSTMAN_PHASE2_KEYS.includes(s.key))
  if (isCustomApi) return []
  // Tiendas (VTEX, Shopify, etc.) y otros: solo phase 1 tiene los campos; phase 2 vacío para no duplicar
  return []
}

function getEditSettingsPhase1OrAll(meta) {
  return getConnectSettingsPhase1OrAll(meta)
}

function getEditSettingsPhase2(meta) {
  return getConnectSettingsForPhase2(meta)
}

export {
  OPENAPI_PROVIDER_KEYS,
  POSTMAN_PROVIDER_KEY,
  CUSTOM_API_PROVIDER_KEY,
  OPENAPI_SCHEMA_PHASE1_KEYS,
  OPENAPI_SCHEMA_PHASE2_KEYS,
  POSTMAN_PHASE1_KEYS,
  POSTMAN_PHASE2_KEYS,
  isOpenApiSchemaConfigured,
  isPostmanCollectionConfigured,
  isApiSettingVisible,
  isSettingValueEmpty,
  isConnectFormValid,
  isEditFormValid,
  getConnectSettingsPhase1OrAll,
  getConnectSettingsForPhase2,
  getEditSettingsPhase1OrAll,
  getEditSettingsPhase2
}

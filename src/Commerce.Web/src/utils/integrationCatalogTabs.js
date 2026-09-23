/** Shared tab grouping for admin/integraciones and mandatory connect modal. */

export const API_CATALOG_PROVIDER_KEYS = ['Dynamic', 'CustomAPI', 'Postman', 'GravityAPI']

export const INTEGRATION_VALID_TABS = ['stores', 'apis', 'channels', 'support', 'mcp']

export function isApiCatalogProvider(key) {
  return API_CATALOG_PROVIDER_KEYS.includes(key)
}

export function storeProviderKeysFrom(available = []) {
  return available.filter((m) => m.integrationType === 'Shops').map((m) => m.key)
}

export function mcpProviderKeysFrom(available = []) {
  return available.filter((m) => m.type === 'mcp' && m.integrationType !== 'Support').map((m) => m.key)
}

export function channelProviderKeysFrom(available = []) {
  return available
    .filter((m) => m.integrationType === 'Channel' || m.channelMarketplace)
    .map((m) => m.key)
}

export function supportProviderKeysFrom(available = []) {
  return available.filter((m) => m.integrationType === 'Support').map((m) => m.key)
}

/**
 * @param {string} provider
 * @param {string} tab
 * @param {object[]} available
 */
export function belongsToTab(provider, tab, available = []) {
  if (tab === 'stores') return storeProviderKeysFrom(available).includes(provider)
  if (tab === 'apis') return isApiCatalogProvider(provider)
  if (tab === 'mcp') return mcpProviderKeysFrom(available).includes(provider)
  if (tab === 'channels') return channelProviderKeysFrom(available).includes(provider)
  if (tab === 'support') return supportProviderKeysFrom(available).includes(provider)
  return false
}

/**
 * @param {object[]} available
 * @param {string} tab
 */
export function filterAvailableByTab(available = [], tab) {
  if (tab === 'stores') return available.filter((m) => m.integrationType === 'Shops')
  if (tab === 'apis') return available.filter((m) => isApiCatalogProvider(m.key))
  if (tab === 'mcp') return available.filter((m) => m.type === 'mcp' && m.integrationType !== 'Support')
  if (tab === 'channels') {
    return available.filter((m) => m.integrationType === 'Channel' || m.channelMarketplace)
  }
  if (tab === 'support') return available.filter((m) => m.integrationType === 'Support')
  return []
}

/** Resolve which integraciones tab a catalog item belongs to. */
export function resolveTabForMeta(meta) {
  if (!meta) return 'stores'
  if (meta.integrationType === 'Shops') return 'stores'
  if (isApiCatalogProvider(meta.key)) return 'apis'
  if (meta.integrationType === 'Channel' || meta.channelMarketplace) return 'channels'
  if (meta.integrationType === 'Support') return 'support'
  if (meta.type === 'mcp') return 'mcp'
  return 'stores'
}

/**
 * @param {object[]} available
 * @param {object[]} [myIntegrations]
 * @param {{ hideStoresAndApis?: boolean }} [options] — Native Gravity store tenants hide Tiendas/APIs tabs
 */
export function buildIntegrationTabs(available = [], myIntegrations = [], options = {}) {
  const hideStoresAndApis = options.hideStoresAndApis === true
  const storesCount =
    myIntegrations.filter((i) => belongsToTab(i.provider, 'stores', available)).length +
    available.filter((m) => m.integrationType === 'Shops').length
  const apisCount =
    myIntegrations.filter((i) => belongsToTab(i.provider, 'apis', available)).length +
    available.filter((m) => isApiCatalogProvider(m.key)).length
  const channelsCount =
    myIntegrations.filter((i) => channelProviderKeysFrom(available).includes(i.provider)).length +
    available.filter((m) => m.integrationType === 'Channel' || m.channelMarketplace).length
  const supportCount =
    myIntegrations.filter((i) => supportProviderKeysFrom(available).includes(i.provider)).length +
    available.filter((m) => m.integrationType === 'Support').length
  const mcpCount =
    myIntegrations.filter((i) => mcpProviderKeysFrom(available).includes(i.provider)).length +
    available.filter((m) => m.type === 'mcp' && m.integrationType !== 'Support').length

  const tabs = [
    { id: 'stores', label: 'Tiendas', icon: '🛒', count: storesCount },
    { id: 'apis', label: 'APIs', icon: '🔌', count: apisCount },
    { id: 'channels', label: 'Canales', icon: '💬', count: channelsCount },
    { id: 'support', label: 'Soporte', icon: '📋', count: supportCount },
    { id: 'mcp', label: 'MCP', icon: '🔗', count: mcpCount }
  ]
  if (hideStoresAndApis) {
    return tabs.filter((t) => t.id !== 'stores' && t.id !== 'apis')
  }
  return tabs
}

export const NATIVE_STORE_PROVIDER_KEY = 'Gravity'

/** Hide Gravity catalog/instance when tenant already is a native store. */
export function isGravityNativeProvider(key) {
  return String(key || '').trim() === NATIVE_STORE_PROVIDER_KEY
}

/**
 * @param {object[]} list catalog or my-integrations rows
 * @param {boolean} isNativeStore
 * @param {(item: object) => string} [getKey] key accessor (default: .key || .provider)
 */
export function excludeGravityWhenNativeStore(list = [], isNativeStore, getKey = (item) => item?.key ?? item?.provider) {
  if (!isNativeStore) return list
  return list.filter((item) => !isGravityNativeProvider(getKey(item)))
}

/** Default tab when landing on Integraciones (Native store → channels). */
export function defaultIntegrationTab(isNativeStore) {
  return isNativeStore ? 'channels' : 'stores'
}

export function integrationTypeBadge(meta) {
  if (!meta) return 'API'
  if (meta.type === 'mcp') return 'MCP'
  if (meta.integrationType === 'Channel' || meta.channelMarketplace) return 'Canal'
  if (meta.integrationType === 'Support') return 'Soporte'
  if (meta.integrationType === 'Shops') return 'Tienda'
  return 'API'
}

/** True when the account has no store (Shops) and no API catalog integration. */
export function lacksStoreOrApiIntegration(usagePayload) {
  const u = usagePayload?.usage ?? usagePayload
  if (!u) return true
  return (u.storesUsed ?? 0) === 0 && (u.openApiUsed ?? 0) === 0
}

/**
 * Deriva el tab de integraciones a partir del provider y la metadata.
 * Genérico: funciona para MCP, APIs (OpenAPI/Custom), tiendas (incl. OAuth como Shopify).
 * @param {string} provider - Clave del proveedor (ej. MCP, Dynamic, Shopify)
 * @param {Array<{key: string, type: string}>} availableIntegrations - Metadata de integraciones
 * @returns {'mcp'|'apis'|'stores'|'channels'|'support'|null} - Tab id o null si no se puede determinar
 */
export function getTabForProvider(provider, availableIntegrations = []) {
  if (!provider || !Array.isArray(availableIntegrations)) return null
  const meta = availableIntegrations.find((m) => m.key === provider)
  if (!meta) return null
  if (meta.integrationType === 'Support') return 'support'
  if (meta.type === 'mcp') return 'mcp'
  if (meta.integrationType === 'Channel' || meta.channelMarketplace) return 'channels'
  if (meta.key === 'Dynamic' || meta.key === 'CustomAPI') return 'apis'
  if (meta.integrationType === 'Shops') return 'stores'
  return 'stores'
}

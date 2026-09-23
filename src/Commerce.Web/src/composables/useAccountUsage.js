import { computed } from 'vue'
import { useAppStore } from '../stores/appStore'

/**
 * Composable para gestionar el uso y límites de la cuenta.
 * Usa el store compartido para evitar llamadas duplicadas a /accounts/usage.
 */
export function useAccountUsage() {
  const store = useAppStore()

  const usage = computed(() => store.usage)

  const membersAtLimit = computed(() => {
    const u = store.usage?.usage
    if (!u || u.membersLimit == null) return false
    return (u.membersUsed ?? 0) >= u.membersLimit
  })

  function getLimitForTab(tab) {
    if (!store.usage?.usage) return null
    const u = store.usage.usage
    if (tab === 'stores') return { used: u.storesUsed ?? 0, limit: u.storesLimit, label: 'tiendas' }
    if (tab === 'apis') return { used: u.openApiUsed ?? 0, limit: u.openApiLimit, label: 'APIs' }
    if (tab === 'channels') return { used: u.channelsUsed ?? 0, limit: u.channelsLimit, label: 'canales' }
    if (tab === 'support' || tab === 'mcp') return { used: u.mcpUsed ?? 0, limit: u.mcpLimit, label: 'MCP' }
    return null
  }

  function isAtLimitForTab(tab) {
    const lim = getLimitForTab(tab)
    return lim != null && lim.limit != null && lim.used >= lim.limit
  }

  function getLimitMessageForTab(tab) {
    const lim = getLimitForTab(tab)
    if (!lim || lim.limit == null || lim.used < lim.limit) return null
    return `Llegaste al límite de ${lim.label} (${lim.used}/${lim.limit}).`
  }

  function getLimitForProvider(meta) {
    if (!meta || !store.usage?.usage) return null
    const u = store.usage.usage
    if (meta.channelMarketplace && !meta.planLimitCountsAsStore) {
      return { used: u.channelsUsed ?? 0, limit: u.channelsLimit, label: 'canales' }
    }
    if (meta.integrationType === 'Shops') return { used: u.storesUsed ?? 0, limit: u.storesLimit, label: 'tiendas' }
    if (meta.integrationType === 'Support' || meta.type === 'mcp') return { used: u.mcpUsed ?? 0, limit: u.mcpLimit, label: 'MCP' }
    if (meta.key === 'Dynamic' || meta.key === 'CustomAPI' || meta.name === 'OpenAPI' || meta.name === 'Custom API') return { used: u.openApiUsed ?? 0, limit: u.openApiLimit, label: 'OpenAPI' }
    if (meta.integrationType === 'Channel') return { used: u.channelsUsed ?? 0, limit: u.channelsLimit, label: 'canales' }
    return null
  }

  function isAtLimitForProvider(meta) {
    const lim = getLimitForProvider(meta)
    return lim != null && lim.limit != null && lim.used >= lim.limit
  }

  function getLimitMessageForProvider(meta) {
    const lim = getLimitForProvider(meta)
    if (!lim || lim.limit == null || lim.used < lim.limit) return null
    return `Llegaste al límite de ${lim.label} (${lim.used}/${lim.limit}).`
  }

  /** MCP Servers: at limit for creating more MCP servers */
  const isAtLimitForMcpServers = computed(() => isAtLimitForTab('mcp'))
  const getLimitMessageForMcpServers = () => {
    const lim = getLimitForTab('mcp')
    if (!lim || lim.limit == null || lim.used < lim.limit) return null
    return `Llegaste al límite de servidores MCP (${lim.used}/${lim.limit}). Actualiza tu plan para más.`
  }

  /** Per-MCP limits: tools, prompts, resources (used = count in current MCP) */
  function isAtLimitForMcpTools(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpToolsLimit == null) return false
    return (usedCount ?? 0) >= u.mcpToolsLimit
  }
  function getLimitMessageForMcpTools(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpToolsLimit == null || (usedCount ?? 0) < u.mcpToolsLimit) return null
    return `Llegaste al límite de tools (${usedCount}/${u.mcpToolsLimit}). Actualiza tu plan para más.`
  }
  function isAtLimitForMcpPrompts(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpPromptsLimit == null) return false
    return (usedCount ?? 0) >= u.mcpPromptsLimit
  }
  function getLimitMessageForMcpPrompts(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpPromptsLimit == null || (usedCount ?? 0) < u.mcpPromptsLimit) return null
    return `Llegaste al límite de prompts (${usedCount}/${u.mcpPromptsLimit}). Actualiza tu plan para más.`
  }
  function isAtLimitForMcpResources(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpResourcesLimit == null) return false
    return (usedCount ?? 0) >= u.mcpResourcesLimit
  }
  function getLimitMessageForMcpResources(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpResourcesLimit == null || (usedCount ?? 0) < u.mcpResourcesLimit) return null
    return `Llegaste al límite de resources (${usedCount}/${u.mcpResourcesLimit}). Actualiza tu plan para más.`
  }

  /** Workflow tools per MCP (Basic plan: 1). usedCount = number of tools with toolType === 'Workflow' in current MCP. */
  function isAtLimitForMcpWorkflowTools(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpWorkflowToolsLimit == null) return false
    return (usedCount ?? 0) >= u.mcpWorkflowToolsLimit
  }
  function getLimitMessageForMcpWorkflowTools(usedCount) {
    const u = store.usage?.usage
    if (!u || u.mcpWorkflowToolsLimit == null || (usedCount ?? 0) < u.mcpWorkflowToolsLimit) return null
    return `En tu plan solo puedes tener ${u.mcpWorkflowToolsLimit} tool de tipo Workflow por servidor MCP. Actualiza tu plan para más.`
  }

  const membersLimitMessage = computed(() => {
    if (!membersAtLimit.value) return null
    return 'Llegaste al límite del plan. No puedes invitar más miembros.'
  })

  return {
    usage,
    fetchUsage: (force, viewAccountId) => store.fetchUsage(force, viewAccountId),
    membersAtLimit,
    membersLimitMessage,
    getLimitForTab,
    isAtLimitForTab,
    getLimitMessageForTab,
    getLimitForProvider,
    isAtLimitForProvider,
    getLimitMessageForProvider,
    isAtLimitForMcpServers,
    getLimitMessageForMcpServers,
    isAtLimitForMcpTools,
    getLimitMessageForMcpTools,
    isAtLimitForMcpPrompts,
    getLimitMessageForMcpPrompts,
    isAtLimitForMcpResources,
    getLimitMessageForMcpResources,
    isAtLimitForMcpWorkflowTools,
    getLimitMessageForMcpWorkflowTools
  }
}

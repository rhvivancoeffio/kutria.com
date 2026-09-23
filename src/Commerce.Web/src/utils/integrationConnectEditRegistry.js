/** API global para abrir los slide-overs de conectar / editar / OAuth (montados en AdminLayout). */
let api = null

export function registerIntegrationConnectEditApi(handlers) {
  api = handlers
}

export function unregisterIntegrationConnectEditApi(handlers) {
  if (api === handlers) api = null
}

export function openIntegrationConnectModal(meta) {
  if (!api?.openConnectModal) {
    console.warn('[integrations] Modal conectar no disponible')
    return false
  }
  api.openConnectModal(meta)
  return true
}

export function openIntegrationEditCredentialsModal(integration) {
  if (!api?.openEditCredentialsModal) {
    console.warn('[integrations] Modal editar no disponible')
    return false
  }
  api.openEditCredentialsModal(integration)
  return true
}

export function openIntegrationOAuthConnectModal(integration) {
  if (!api?.openOAuthConnectModal) {
    console.warn('[integrations] Modal OAuth no disponible')
    return false
  }
  api.openOAuthConnectModal(integration)
  return true
}

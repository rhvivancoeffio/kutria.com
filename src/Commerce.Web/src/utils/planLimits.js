/**
 * Formats plan limits for display in the UI.
 * Excludes fields not surfaced in this UI (e.g. history retention).
 * @param {Object} limits - plan.limits from API (maxStores, maxChannels, maxMembers, etc.)
 * @returns {string[]} Array of human-readable limit strings
 */
export function formatPlanLimits(limits) {
  if (!limits) return []

  const items = []

  if (limits.maxStores != null) {
    items.push(`Hasta ${limits.maxStores} tienda${limits.maxStores !== 1 ? 's' : ''}`)
  } else {
    items.push('Tiendas ilimitadas')
  }

  if (limits.maxChannels != null) {
    items.push(`Hasta ${limits.maxChannels} canal${limits.maxChannels !== 1 ? 'es' : ''}`)
  } else {
    items.push('Canales ilimitados')
  }

  if (limits.maxMembers != null) {
    items.push(`Hasta ${limits.maxMembers} miembro${limits.maxMembers !== 1 ? 's' : ''}`)
  } else {
    items.push('Miembros ilimitados')
  }

  const maxCat = limits.maxCatalogDataPipelines
  const maxOrd = limits.maxOrdersDataPipelines
  const legacyMax = limits.maxDataPipelines

  if (maxCat != null || maxOrd != null) {
    if (maxCat != null) {
      items.push(`Hasta ${maxCat} data pipeline${maxCat !== 1 ? 's' : ''} (catálogo)`)
    } else {
      items.push('Data pipelines de catálogo ilimitados')
    }
    if (maxOrd != null) {
      items.push(`Hasta ${maxOrd} data pipeline${maxOrd !== 1 ? 's' : ''} (pedidos)`)
    } else {
      items.push('Data pipelines de pedidos ilimitados')
    }
  } else if (legacyMax != null) {
    items.push(`Hasta ${legacyMax} data pipeline${legacyMax !== 1 ? 's' : ''}`)
  } else {
    items.push('Data pipelines (catálogo y pedidos) ilimitados')
  }

  if (limits.maxSyncedProducts != null) {
    items.push(`Hasta ${limits.maxSyncedProducts.toLocaleString('es')} productos por sincronización`)
  }

  return items
}

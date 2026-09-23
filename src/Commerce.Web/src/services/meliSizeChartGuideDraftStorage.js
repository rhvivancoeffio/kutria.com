import Dexie from 'dexie'

class MeliSizeChartGuideDexie extends Dexie {
  constructor() {
    super('gravity_channel_meli_size_chart_guides_v1')
    this.version(1).stores({
      guides: 'id, scopeKey'
    })
  }
}

const db = new MeliSizeChartGuideDexie()

function storableDeep(value) {
  if (value == null) return value
  if (typeof value !== 'object') return value
  try {
    return JSON.parse(JSON.stringify(value))
  } catch {
    return Array.isArray(value) ? [] : {}
  }
}

function normBrand(brandText) {
  return String(brandText ?? '')
    .trim()
    .toLowerCase()
    .replace(/\s+/g, ' ')
    .replace(/[^a-z0-9\u00C0-\u024F\s-]/gi, '')
    .slice(0, 120)
}

/**
 * Segmento estable por proveedor de marketplace (p. ej. mercadolibre, gravity).
 * @param {string|null|undefined} providerId — típicamente `marketplaceKey` del destino
 */
export function meliProviderSegment(providerId) {
  if (providerId == null || providerId === '') return 'no-provider'
  const s = String(providerId).trim().toLowerCase()
  return s || 'no-provider'
}

/**
 * Ámbito IndexedDB para borradores de guía de tallas: workspace + proveedor + dominio de grilla + categoría ML.
 * @param {string} workspaceId
 * @param {string} providerId — marketplaceKey / proveedor del canal
 * @param {string} searchDomain — id usado en GET technical_specs (p. ej. MPE-SNEAKERS)
 * @param {string} targetCategoryId — categoría ML mapeada
 */
export function buildMeliSizeChartGuideScopeKey(workspaceId, providerId, searchDomain, targetCategoryId) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  const p = meliProviderSegment(providerId)
  const dom = String(searchDomain || '').trim() || 'no-domain'
  const c = String(targetCategoryId || '').trim() || 'no-cat'
  return `${w}::${p}::${dom}::${c}`
}

/**
 * Clave primaria: ámbito + marca normalizada + id género ML (+ brandId si existe, para desambiguar).
 */
export function buildMeliSizeChartGuideDraftId(scopeKey, brandText, genderId, brandId) {
  const b = normBrand(brandText) || 'no-brand'
  const g = String(genderId ?? '')
    .trim()
    .replace(/[^a-zA-Z0-9_-]/g, '') || 'no-gender'
  const bid = String(brandId ?? '')
    .trim()
    .replace(/[^a-zA-Z0-9_-]/g, '')
  const suffix = bid ? `::${bid}` : ''
  const raw = `${scopeKey}::${b}::${g}${suffix}`
  return raw.length > 450 ? raw.slice(0, 450) : raw
}

/**
 * @param {object} row
 * @returns {Record<string, unknown>}
 */
function plainMatrixRow(row) {
  const o = storableDeep(row)
  if (!o || typeof o !== 'object') return {}
  return o
}

/**
 * @param {object} tab — pestaña reactiva del modal
 */
export function meliSizeChartTabToDraftRecord({
  scopeKey,
  tab,
  mappingKey,
  catalogDomainRaw,
  workspaceId,
  providerId,
  integrationId,
  channelDestinationId,
  searchDomain,
  targetCategoryId
}) {
  const brandText = String(tab.brandText || '').trim()
  const brandIdStored = String(tab.brandId ?? '').trim()
  const genderId = String(tab.genderId || '').trim()
  const id = buildMeliSizeChartGuideDraftId(scopeKey, brandText, genderId, brandIdStored)
  const matrixRows = Array.isArray(tab.matrixRows)
    ? tab.matrixRows.map((r) => plainMatrixRow(r))
    : []
  const otherValues = tab.otherValues && typeof tab.otherValues === 'object' ? storableDeep(tab.otherValues) : {}
  const mk = mappingKey != null ? String(mappingKey).trim() : String(tab.mappingKey || '').trim()
  const sizeChartName = String(tab.sizeChartName || tab.title || '').trim()
  const chartIdStr = String(tab.chartId || '').trim()
  return {
    id,
    scopeKey,
    workspaceId: String(workspaceId || '').trim(),
    providerId: String(providerId ?? '').trim().toLowerCase(),
    integrationId: String(integrationId || '').trim(),
    channelDestinationId: String(channelDestinationId || '').trim(),
    searchDomain: String(searchDomain || '').trim(),
    catalogDomainRaw: String(catalogDomainRaw || '').trim(),
    targetCategoryId: String(targetCategoryId || '').trim(),
    brandId: brandIdStored,
    brandNorm: normBrand(brandText),
    brandText,
    brandName: brandText,
    genderId,
    genderLabel: String(tab.genderLabel || '').trim(),
    title: String(tab.title || '').trim(),
    sizeChartName,
    sizeChartId: chartIdStr,
    tabKey: String(tab.key || '').trim(),
    matrixRows,
    euSizesText: String(tab.euSizesText ?? ''),
    otherValues,
    chartId: String(tab.chartId || '').trim(),
    fingerprint: String(tab.fingerprint || '').trim(),
    mappingKey: mk,
    updatedAt: Date.now()
  }
}

/**
 * @returns {Promise<object[]>}
 */
export async function listMeliSizeChartGuideDraftsForScope(scopeKey) {
  const sk = String(scopeKey || '').trim()
  if (!sk) return []
  return db.guides.where('scopeKey').equals(sk).toArray()
}

export async function getMeliSizeChartGuideDraft(id) {
  return db.guides.get(String(id || '').trim())
}

export async function upsertMeliSizeChartGuideDraft(record) {
  const row = storableDeep(record)
  if (!row?.id || !row?.scopeKey) return
  row.updatedAt = Date.now()
  await db.guides.put(row)
}

export async function deleteMeliSizeChartGuideDraft(id) {
  await db.guides.delete(String(id || '').trim())
}

/**
 * Para el wizard / variaciones: borradores del mismo dominio de grilla y proveedor.
 */
export async function listMeliSizeChartGuideDraftsForWizard(
  workspaceId,
  providerId,
  searchDomain,
  targetCategoryId
) {
  const scopeKey = buildMeliSizeChartGuideScopeKey(workspaceId, providerId, searchDomain, targetCategoryId)
  return listMeliSizeChartGuideDraftsForScope(scopeKey)
}


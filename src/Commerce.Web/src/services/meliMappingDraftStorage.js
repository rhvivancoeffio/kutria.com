import Dexie from 'dexie'

class MeliMappingDexie extends Dexie {
  constructor() {
    super('gravity_channel_meli_mapping_v1')
    this.version(1).stores({
      drafts: 'draftKey, fingerprint'
    })
  }
}

const db = new MeliMappingDexie()

/**
 * IndexedDB usa structuredClone; los proxies/array reactivos de Vue fallan con DataCloneError.
 * @returns {unknown}
 */
function storableDeep(value) {
  if (value == null) return value
  if (typeof value !== 'object') return value
  try {
    return JSON.parse(JSON.stringify(value))
  } catch {
    return Array.isArray(value) ? [] : {}
  }
}

function storableMappingArray(arr) {
  if (!Array.isArray(arr) || !arr.length) return []
  const plain = storableDeep(arr)
  return Array.isArray(plain) ? plain : []
}

function meliDexieLog(phase, detail) {
  if (!import.meta.env.DEV) return
  console.log('[meli-dexie]', phase, detail)
}

/** Segmento estable para integración de marketplace en claves Dexie (evita mezclar borradores entre conexiones). */
export function meliIntegrationSegment(integrationId) {
  if (integrationId == null || integrationId === '') return 'no-int'
  const s = String(integrationId).trim()
  return s || 'no-int'
}

/**
 * Borrador por colección: workspace + destino + integración + huella de filtros/pipeline.
 * @param {string|null|undefined} integrationId marketplaceIntegrationId
 */
export function buildMeliDraftKey(workspaceId, destinationKey, integrationId, fingerprint) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  const d = String(destinationKey || '').trim() || 'new'
  const i = meliIntegrationSegment(integrationId)
  const f = String(fingerprint || '').trim() || 'no-fp'
  return `${w}::${d}::${i}::${f}`
}

/** Compat: claves anteriores sin integración en el path. */
export function buildMeliDraftKeyLegacy(workspaceId, destinationKey, fingerprint) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  const d = String(destinationKey || '').trim() || 'new'
  const f = String(fingerprint || '').trim() || 'no-fp'
  return `${w}::${d}::${f}`
}

/** Clave de sesión: workspace + destino + integración + marcador fijo. */
export function buildMeliSessionKey(workspaceId, destinationKey, integrationId) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  const d = String(destinationKey || '').trim() || 'new'
  const i = meliIntegrationSegment(integrationId)
  return `${w}::${d}::${i}::__session__`
}

/** Compat: sesión sin segmento de integración. */
export function buildMeliSessionKeyLegacy(workspaceId, destinationKey) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  const d = String(destinationKey || '').trim() || 'new'
  return `${w}::${d}::__session__`
}

/**
 * @param {string} draftKey
 * @returns {Promise<{ draftKey: string, fingerprint: string, categoryMappings: object[], attributeMappings?: object[], updatedAt: number } | undefined>}
 */
export async function loadMeliMappingDraft(draftKey) {
  return db.drafts.get(draftKey)
}

/**
 * @param {{ draftKey: string, fingerprint: string, categoryMappings: object[], attributeMappings?: object[] }} record
 */
export async function saveMeliMappingDraft(record) {
  const {
    draftKey,
    fingerprint,
    categoryMappings,
    attributeMappings,
    specificationValueMappings,
    brandMappings
  } = record
  const cats = storableMappingArray(Array.isArray(categoryMappings) ? categoryMappings : [])
  const attrs = storableMappingArray(Array.isArray(attributeMappings) ? attributeMappings : [])
  const vmaps = storableMappingArray(Array.isArray(specificationValueMappings) ? specificationValueMappings : [])
  const brands = storableMappingArray(Array.isArray(brandMappings) ? brandMappings : [])
  const updatedAt = Date.now()
  meliDexieLog('saveMeliMappingDraft → put', {
    draftKey,
    fingerprint,
    cats: cats.length,
    attrs: attrs.length,
    vmaps: vmaps.length,
    brands: brands.length,
    updatedAt
  })
  await db.drafts.put({
    draftKey,
    fingerprint,
    categoryMappings: cats,
    attributeMappings: attrs,
    specificationValueMappings: vmaps,
    brandMappings: brands,
    updatedAt
  })
}

export async function deleteMeliMappingDraft(draftKey) {
  await db.drafts.delete(draftKey)
}

/**
 * @param {{ sessionKey: string, fingerprint: string, filters: object, pipelineId?: string | null, marketplaceKey?: string | null, categoryMappings: object[], attributeMappings: object[] }} record
 */
export async function saveMeliSessionDraft(record) {
  const {
    sessionKey,
    fingerprint,
    filters,
    pipelineId,
    marketplaceKey,
    categoryMappings,
    attributeMappings,
    specificationValueMappings,
    brandMappings
  } = record
  const cats = storableMappingArray(Array.isArray(categoryMappings) ? categoryMappings : [])
  const attrs = storableMappingArray(Array.isArray(attributeMappings) ? attributeMappings : [])
  const vmaps = storableMappingArray(Array.isArray(specificationValueMappings) ? specificationValueMappings : [])
  const brands = storableMappingArray(Array.isArray(brandMappings) ? brandMappings : [])
  const filtersPlain =
    filters && typeof filters === 'object' ? (storableDeep(filters) ?? {}) : {}
  const fp = String(fingerprint || '').trim()
  const updatedAt = Date.now()
  meliDexieLog('saveMeliSessionDraft → put', {
    sessionKey,
    fingerprint: fp,
    cats: cats.length,
    attrs: attrs.length,
    vmaps: vmaps.length,
    brands: brands.length,
    pipelineId: pipelineId == null || pipelineId === '' ? null : String(pipelineId),
    updatedAt
  })
  await db.drafts.put({
    draftKey: sessionKey,
    fingerprint: fp,
    filters: filtersPlain && typeof filtersPlain === 'object' ? filtersPlain : {},
    pipelineId: pipelineId == null || pipelineId === '' ? null : String(pipelineId),
    marketplaceKey: marketplaceKey != null && String(marketplaceKey).trim() ? String(marketplaceKey).trim() : null,
    categoryMappings: cats,
    attributeMappings: attrs,
    specificationValueMappings: vmaps,
    brandMappings: brands,
    updatedAt
  })
}

/**
 * @returns {Promise<{ draftKey: string, fingerprint: string, filters?: object, pipelineId?: string | null, marketplaceKey?: string | null, categoryMappings?: object[], attributeMappings?: object[], updatedAt: number } | undefined>}
 */
export async function loadMeliSessionDraft(sessionKey) {
  const row = await db.drafts.get(sessionKey)
  meliDexieLog('loadMeliSessionDraft', {
    sessionKey,
    hit: Boolean(row),
    fingerprint: row?.fingerprint,
    cats: row?.categoryMappings?.length ?? 0,
    attrs: row?.attributeMappings?.length ?? 0,
    vmaps: row?.specificationValueMappings?.length ?? 0,
    updatedAt: row?.updatedAt
  })
  return row
}

export async function deleteMeliSessionDraft(sessionKey) {
  await db.drafts.delete(sessionKey)
}

/**
 * Huella estable de la “collection filtrada” (filtros + pipeline opcional).
 * Si cambia, se invalidan borradores locales (IndexedDB).
 */
export function computeCollectionFingerprint({ pipelineId, filters }) {
  const normalized = {
    pipelineId: pipelineId == null || pipelineId === '' ? null : String(pipelineId),
    filters: sortKeysDeep(filters && typeof filters === 'object' ? filters : {})
  }
  const payload = JSON.stringify(normalized)
  let h = 5381
  for (let i = 0; i < payload.length; i++) {
    h = (h * 33) ^ payload.charCodeAt(i)
  }
  return `v1_${(h >>> 0).toString(16)}`
}

function sortKeysDeep(obj) {
  if (obj === null || typeof obj !== 'object' || Array.isArray(obj)) return obj
  const out = {}
  for (const k of Object.keys(obj).sort()) {
    const v = obj[k]
    out[k] = v && typeof v === 'object' && !Array.isArray(v) ? sortKeysDeep(v) : v
  }
  return out
}

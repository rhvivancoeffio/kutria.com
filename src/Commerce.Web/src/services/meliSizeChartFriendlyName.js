/**
 * Título visible de guía ML y clave de mapeo estable (alineado con modal / wizard).
 */

/**
 * @param {{ brandName?: string, genderLabel?: string, domainId?: string }} p
 * @returns {string} máx. 60 caracteres (límite ML en chart title / names)
 */
export function buildFriendlySizeChartTitle(p) {
  const b = String(p?.brandName ?? '')
    .trim() || 'Marca'
  const g = String(p?.genderLabel ?? '')
    .trim() || 'Género'
  const d = String(p?.domainId ?? '')
    .trim() || 'dominio'
  const raw = `Guía ${b} ${g} - ${d}`
  return raw.slice(0, 60)
}

/**
 * Etiqueta corta para la pestaña del modal: «marca-género» (sin dominio).
 * @param {string} brandName
 * @param {string} genderLabel
 */
export function buildSizeChartTabStripLabel(brandName, genderLabel) {
  const b = String(brandName ?? '').trim() || '—'
  const g = String(genderLabel ?? '').trim() || '—'
  return `${b}-${g}`
}

/**
 * Misma regla que `mappingKeyForTab` en el modal (identifica entrada en sizeCharts.entries).
 * @param {string} domainSearchId — p. ej. MPE-SNEAKERS
 * @param {string} brandName — texto marca
 * @param {string} genderId — id ML género
 */
export function buildMeliSizeChartMappingKey(domainSearchId, brandName, genderId) {
  const dom = String(domainSearchId || 'dom').replace(/[^a-zA-Z0-9_-]/g, '')
  const g = String(genderId || '').replace(/[^a-zA-Z0-9_-]/g, '')
  const b = String(brandName || '')
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '')
    .slice(0, 48)
  const raw = `ml-sc:${dom}:${g}:${b}`
  return raw.length > 200 ? raw.slice(0, 200) : raw
}

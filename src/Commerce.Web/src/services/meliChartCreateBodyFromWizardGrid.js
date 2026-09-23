import { pickEuMatrixColumnId } from '@/services/meliSuggestedEuSizesByGender'

const PUBLISH_SITES = ['CBT', 'MLB', 'MLM', 'MCO', 'MLC']

/**
 * @typedef {'female'|'male'|'unknown'} GenderKind
 * - Calzado ML: `male` también cubre niños/niñas (misma convención que `meliSizeChartRowAutofill` / US hombre
 *   para FOOT y exclusión de `F_US_SIZE`). `unknown` deja ambas columnas US (p. ej. unisex).
 */

/**
 * @param {{ id: string, values?: { id?: string, name?: string }[] }[]} chartLevelAttributeBlocks
 * @returns {GenderKind}
 */
export function inferGenderKindFromChartBlocks(chartLevelAttributeBlocks) {
  const blocks = Array.isArray(chartLevelAttributeBlocks) ? chartLevelAttributeBlocks : []
  const g = blocks.find((b) => {
    const id = String(b?.id || '')
      .trim()
      .toUpperCase()
    return id === 'GENDER' || id.endsWith('-GENDER') || id.endsWith('_GENDER') || /\bGENDER\b/.test(id)
  })
  const v0 = g?.values && g.values[0] ? g.values[0] : null
  const name = String(v0?.name ?? '')
    .trim()
    .toLowerCase()
  const vid = String(v0?.id ?? '')
    .trim()
    .toLowerCase()
  const blob = `${name} ${vid}`

  if (/mujer|woman|female|femenin|ladies|dama/.test(blob) && !/hombre|man|male|masculin|caballero/.test(blob)) {
    return 'female'
  }
  if (/hombre|man|male|masculin|caballero|men\b/.test(blob)) {
    return 'male'
  }
  // Infantil: misma pista que autofill (isMaleGenderLabel) — una sola columna US (M_US); evita vacíos en F_US y errores de unicidad ML.
  if (/niñ|niño|niña|kid|infant|junior|youth|joven/.test(blob)) {
    return 'male'
  }
  if (/unisex|uni\b/.test(blob)) {
    return 'unknown'
  }
  return 'unknown'
}

/**
 * Excluye columnas matrix que ML rechaza según género (p. ej. M_US_SIZE en guía Mujer).
 * @param {object[]} matrixFields
 * @param {GenderKind} genderKind
 */
export function filterMatrixFieldsForMercadoLibreGender(matrixFields, genderKind) {
  const fs = Array.isArray(matrixFields) ? matrixFields : []
  if (genderKind === 'female') {
    return fs.filter((f) => String(f?.id || '').trim().toUpperCase() !== 'M_US_SIZE')
  }
  if (genderKind === 'male') {
    return fs.filter((f) => String(f?.id || '').trim().toUpperCase() !== 'F_US_SIZE')
  }
  return fs.slice()
}

/** @param {{ id: string, tags?: string[] }[]} matrixFields */
export function pickMainAttributeIdForChart(matrixFields) {
  const fs = Array.isArray(matrixFields) ? matrixFields : []
  if (fs.some((f) => String(f?.id || '').toUpperCase() === 'MANUFACTURER_SIZE')) {
    return 'MANUFACTURER_SIZE'
  }
  const cand = fs.find((f) =>
    (f.tags || []).some((t) => String(t).toLowerCase() === 'main_attribute_candidate')
  )
  if (cand?.id) return cand.id
  const eu = pickEuMatrixColumnId(fs)
  if (eu) return eu
  return String(fs[0]?.id || '').trim()
}

export function buildNamesBySite(chartTitle, maxLen = 60) {
  const t = String(chartTitle || '')
    .trim()
    .slice(0, maxLen)
  const o = {}
  for (const s of PUBLISH_SITES) o[s] = t
  return o
}

export function buildMainAttributeBlock(mainAttributeId) {
  const id = String(mainAttributeId || '').trim()
  return {
    attributes: PUBLISH_SITES.map((site_id) => ({ site_id, id }))
  }
}

/**
 * @param {{ id: string, allowedValues?: {id:string,name:string}[], valueType?: string, valueMin?: number, valueMax?: number }} col
 */
function buildCellValue(col, rawValue, unitRaw) {
  const s = String(rawValue ?? '').trim()
  if (!s) return null
  if (Array.isArray(col.allowedValues) && col.allowedValues.length) {
    const opt = col.allowedValues.find((x) => String(x.id) === s)
    if (opt) return { id: String(opt.id), name: String(opt.name || opt.id) }
    return { id: s, name: s }
  }
  const vtl = String(col.valueType || '').toLowerCase()
  if (vtl === 'number_unit' || vtl === 'number_unit_input') {
    const u = String(unitRaw ?? '').trim()
    let numStr = s
    if (/^-?\d+([.,]\d+)?$/.test(s)) {
      let n = Number(s.replace(',', '.'))
      const lo = col.valueMin
      const hi = col.valueMax
      if (Number.isFinite(lo) && n < lo) n = lo
      if (Number.isFinite(hi) && n > hi) n = hi
      numStr = String(n)
    }
    const name = u ? `${numStr} ${u}` : numStr
    if (/^-?\d+([.,]\d+)?$/.test(numStr)) return { id: numStr.replace(',', '.'), name }
    return { name }
  }
  if (/^-?\d+([.,]\d+)?$/.test(s)) {
    let n = Number(s.replace(',', '.'))
    const lo = col.valueMin
    const hi = col.valueMax
    if (Number.isFinite(lo) && n < lo) n = lo
    if (Number.isFinite(hi) && n > hi) n = hi
    const id = String(n)
    return { id, name: id }
  }
  return { name: s }
}

/**
 * @param {Record<string, unknown>} row
 * @param {object[]} matrixFields
 */
export function matrixRowToChartApiRow(row, matrixFields) {
  const attributes = []
  for (const col of matrixFields || []) {
    const id = String(col.id || '').trim()
    if (!id) continue
    const cell = buildCellValue(col, row[id], row[`${id}__unit`])
    if (!cell) continue
    attributes.push({ id, values: [cell] })
  }
  return { sites: [...PUBLISH_SITES], attributes }
}

/**
 * @param {object[]} chartBlocks — { id, values: [{ id?, name? }] }[]
 * @param {object[]} matrixFields
 * @param {Record<string, unknown>[]} matrixRows — sin __key
 */
export function buildChartCreateBodyObject({
  domainSearchId,
  chartTitle,
  mainAttributeId,
  chartLevelAttributeBlocks,
  matrixFields,
  matrixRows
}) {
  const genderKind = inferGenderKindFromChartBlocks(chartLevelAttributeBlocks || [])
  const matrixForApi = filterMatrixFieldsForMercadoLibreGender(matrixFields || [], genderKind)
  const rows = (matrixRows || []).map((r) => {
    const { __key, ...rest } = r
    return matrixRowToChartApiRow(rest, matrixForApi)
  })
  return {
    names: buildNamesBySite(chartTitle),
    domain_id: String(domainSearchId || '').trim(),
    site_id: 'CBT',
    type: 'SPECIFIC',
    main_attribute: buildMainAttributeBlock(mainAttributeId),
    attributes: chartLevelAttributeBlocks || [],
    rows
  }
}

export function buildChartCreateBodyJson(params) {
  return JSON.stringify(buildChartCreateBodyObject(params), null, 2)
}

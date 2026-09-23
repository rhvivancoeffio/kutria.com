import { pickEuMatrixColumnId } from './meliSuggestedEuSizesByGender.js'

/**
 * Mercado Libre GET /domains/{id}/technical_specs?section=grids → columnas UI.
 * El path usa el catalog_domain completo (p. ej. MPE-SNEAKERS, MLA-SNEAKERS), no solo el sufijo.
 */

function pick(obj, ...keys) {
  if (!obj || typeof obj !== 'object') return undefined
  for (const k of keys) {
    if (obj[k] != null) return obj[k]
    const p = k.charAt(0).toUpperCase() + k.slice(1)
    if (obj[p] != null) return obj[p]
  }
  return undefined
}

function normTags(raw) {
  if (!Array.isArray(raw)) return []
  return raw.map((t) => String(t ?? '').trim().toLowerCase()).filter(Boolean)
}

/** Atributos ML que no queremos en la grilla UI (p. ej. STYLE en calzado). */
function isExcludedFromGridUi(attributeId) {
  const u = String(attributeId ?? '')
    .trim()
    .toUpperCase()
  return u === 'STYLE' || u.endsWith(':STYLE')
}

/**
 * Ids de atributos que ML pone en el GRID como **columnas de fila** (scope interno `matrix`).
 * No deben clasificarse como `chart_filter` por heurísticas de nombre: p. ej. el label
 * "Talle de marca" (`MANUFACTURER_SIZE`) contiene la palabra "marca" y antes disparaba
 * la rama de inferencia de BRAND.
 */
const MATRIX_ROW_ATTRIBUTE_IDS = new Set(['MANUFACTURER_SIZE'])

function isMatrixRowAttributeId(id) {
  return MATRIX_ROW_ATTRIBUTE_IDS.has(String(id ?? '').trim().toUpperCase())
}

/**
 * Cuando ML no trae tags chart_filter, inferimos filtros por id/nombre (p. ej. id GENDER sin "/" o nombre "Género").
 */
function inferChartFilterFromShape(id, name) {
  const idU = String(id ?? '').trim().toUpperCase()
  const n = String(name ?? '')
  if (isMatrixRowAttributeId(id)) return false
  if (idU.startsWith('FILTER_')) return true
  if (/^LINE(\/|$)/.test(idU) || /^AGE_GROUP(\/|$)/.test(idU) || /^DEPARTMENT(\/|$)/.test(idU)) return true
  if (/^BRAND(\/|$)/.test(idU) || /^GENDER(\/|$)/.test(idU)) return true
  if (/\bGENDER\b/.test(idU) || idU.endsWith('-GENDER') || idU.endsWith('_GENDER')) return true
  if (/\bBRAND\b/.test(idU) || idU.endsWith('-BRAND') || idU.endsWith('_BRAND')) return true
  if (/gender|género|genero/i.test(n)) return true
  if (/\bmarca\b|(^|\s)brand(\s|$)|fabricante/i.test(n)) return true
  return false
}

function mapAllowedValues(values) {
  if (!Array.isArray(values)) return []
  const out = []
  for (const v of values) {
    if (v == null) continue
    const t = typeof v
    if (t === 'string' || t === 'number' || t === 'boolean') {
      const id = String(v).trim()
      if (id) out.push({ id, name: id })
      continue
    }
    if (t !== 'object') continue
    const id = String(pick(v, 'id', 'value_id', 'ValueId') ?? '').trim()
    const name = String(pick(v, 'name', 'label', 'value_name', 'ValueName') ?? id).trim()
    if (id || name) out.push({ id: id || name, name: name || id })
  }
  return out
}

/**
 * @param {string|null|undefined} catalogDomainFromApi ej. MLA-SNEAKERS, MPE-SNEAKERS
 * @returns {string} mismo valor recortado; id en GET /domains/{id}/technical_specs
 */
export function toMeliGridsSearchDomainId(catalogDomainFromApi) {
  return String(catalogDomainFromApi ?? '').trim()
}

/**
 * @param {unknown} raw — JSON ML (objeto o string)
 * @returns {{ id: string, name: string, scope: 'chart_filter'|'matrix', valueType: string, required: boolean, allowedValues: {id:string,name:string}[], units: {id:string,name:string}[], defaultUnitId: string }[]}
 */
export function extractGridsUiFieldsFromTechnicalSpecs(raw) {
  let root = raw
  if (typeof root === 'string') {
    try {
      root = JSON.parse(root)
    } catch {
      return []
    }
  }
  /** ML GET …/technical_specs?section=grids → cuerpo { input, output }; los atributos están bajo input. */
  if (root && typeof root === 'object' && !Array.isArray(root)) {
    const inner = root.input ?? root.Input
    if (inner != null && typeof inner === 'object' && !Array.isArray(inner)) {
      root = inner
    }
  }
  /** @type {Map<string, any>} */
  const byId = new Map()

  function consider(obj) {
    if (!obj || typeof obj !== 'object' || Array.isArray(obj)) return
    const id = String(pick(obj, 'id', 'attribute_id') ?? '').trim()
    if (!id) return
    if (isExcludedFromGridUi(id)) return

    const hierarchy = pick(obj, 'hierarchy', 'Hierarchy')
    const tagsRaw = pick(obj, 'tags', 'Tags')
    const valuesRaw = pick(
      obj,
      'values',
      'Values',
      'allowed_values',
      'AllowedValues',
      'possible_values',
      'PossibleValues'
    )
    const hasDeclaredValueType = pick(obj, 'value_type', 'valueType', 'ValueType') != null
    const looksLikeGridAttribute =
      hierarchy != null ||
      hasDeclaredValueType ||
      (Array.isArray(tagsRaw) && tagsRaw.length > 0) ||
      (Array.isArray(valuesRaw) && valuesRaw.length > 0)
    if (!looksLikeGridAttribute) return

    const tags = normTags(tagsRaw)
    const name = String(pick(obj, 'name', 'label', 'title') ?? id).trim()
    const valueType = String(pick(obj, 'value_type', 'valueType', 'ValueType') ?? 'string').toLowerCase()
    const required =
      Boolean(pick(obj, 'required', 'Required')) ||
      tags.includes('required') ||
      tags.includes('grid_template_required')
    const allowedValues = mapAllowedValues(valuesRaw)
    const unitsRaw = pick(obj, 'units', 'Units')
    const units = Array.isArray(unitsRaw)
      ? unitsRaw
          .map((u) => ({
            id: String(pick(u, 'id') ?? '').trim(),
            name: String(pick(u, 'name') ?? '').trim()
          }))
          .filter((u) => u.id)
      : []
    const defaultUnitId = String(pick(obj, 'default_unit_id', 'defaultUnitId', 'DefaultUnitId') ?? '').trim()

    const constraints = pick(obj, 'constraints', 'Constraints')
    let valueMin = undefined
    let valueMax = undefined
    if (constraints && typeof constraints === 'object') {
      const cmin = pick(constraints, 'minimum', 'min', 'value_min', 'ValueMin')
      const cmax = pick(constraints, 'maximum', 'max', 'value_max', 'ValueMax')
      if (cmin != null && Number.isFinite(Number(cmin))) valueMin = Number(cmin)
      if (cmax != null && Number.isFinite(Number(cmax))) valueMax = Number(cmax)
    }
    const vminLoose = pick(obj, 'value_min', 'valueMin', 'minimum', 'Minimum')
    const vmaxLoose = pick(obj, 'value_max', 'valueMax', 'maximum', 'Maximum')
    if (valueMin === undefined && vminLoose != null && Number.isFinite(Number(vminLoose))) valueMin = Number(vminLoose)
    if (valueMax === undefined && vmaxLoose != null && Number.isFinite(Number(vmaxLoose))) valueMax = Number(vmaxLoose)

    const prev = byId.get(id)
    const mergedTags = new Set([...(prev?.tags || []), ...tags])
    const hasExplicitChartFilterTag =
      mergedTags.has('chart_filter') ||
      mergedTags.has('grid_filter') ||
      mergedTags.has('chartfilter') ||
      mergedTags.has('gridfilter')
    const mergedScope =
      hasExplicitChartFilterTag || inferChartFilterFromShape(id, name) ? 'chart_filter' : 'matrix'

    const mergedMin =
      prev?.valueMin !== undefined && Number.isFinite(prev.valueMin) ? prev.valueMin : valueMin
    const mergedMax =
      prev?.valueMax !== undefined && Number.isFinite(prev.valueMax) ? prev.valueMax : valueMax

    const row = {
      id,
      name: (prev?.name && prev.name !== prev.id ? prev.name : null) || name || id,
      scope: mergedScope,
      valueType: prev?.valueType || valueType,
      required: Boolean(prev?.required) || required,
      allowedValues: (prev?.allowedValues?.length ? prev.allowedValues : null) || allowedValues,
      units: (prev?.units?.length ? prev.units : null) || units,
      defaultUnitId: prev?.defaultUnitId || defaultUnitId || (units[0]?.id ?? ''),
      tags: [...mergedTags],
      ...(mergedMin !== undefined ? { valueMin: mergedMin } : {}),
      ...(mergedMax !== undefined ? { valueMax: mergedMax } : {})
    }
    byId.set(id, row)
  }

  function walk(node) {
    if (node == null) return
    if (Array.isArray(node)) {
      for (const x of node) walk(x)
      return
    }
    if (typeof node !== 'object') return
    consider(node)
    for (const v of Object.values(node)) walk(v)
  }

  walk(root)
  return [...byId.values()].sort((a, b) => {
    if (a.scope !== b.scope) return a.scope === 'chart_filter' ? -1 : 1
    return a.id.localeCompare(b.id)
  })
}

export function chartFilterFieldsFromExtracted(fields) {
  return (fields || []).filter((f) => f.scope === 'chart_filter' && !isExcludedFromGridUi(f.id))
}

export function matrixFieldsFromExtracted(fields) {
  return (fields || []).filter((f) => f.scope === 'matrix' && !isExcludedFromGridUi(f.id))
}

/** Primera columna de grilla: talla principal (EU / fabricante). */
const PRIMARY_SIZE_FALLBACK_IDS = ['EU_SIZE', 'MANUFACTURER_SIZE']

/** Segunda “zona”: largo del pie (rango from–to en ML). */
const FOOT_LENGTH_COLUMN_IDS = ['FOOT_LENGTH', 'FOOT_LENGTH_TO']

/**
 * Después de foot: códigos de talla regional / comercial (orden UX; lo que no esté no se usa).
 * Cualquier id matrix nuevo cae al final ordenado alfabéticamente.
 */
const REGIONAL_MATRIX_COLUMN_ORDER = [
  'AR_SIZE',
  'BR_SIZE',
  'MX_SIZE',
  'CL_SIZE',
  'CO_SIZE',
  'UY_SIZE',
  'M_US_SIZE',
  'F_US_SIZE',
  'US_SIZE',
  'UK_SIZE',
  'JP_SIZE',
  'CN_SIZE',
  'KR_SIZE',
  'IN_SIZE',
  'AU_SIZE',
  'NZ_SIZE'
]

function hasMainAttributeCandidateTag(f) {
  const tags = Array.isArray(f?.tags) ? f.tags : []
  return tags.some((t) => String(t).toLowerCase() === 'main_attribute_candidate')
}

function scoreMainCandidateId(id) {
  const u = String(id || '').toUpperCase()
  if (u === 'EU_SIZE') return 100
  if (u === 'MANUFACTURER_SIZE') return 95
  if (u.includes('EU') && u.includes('SIZE')) return 85
  if (u.includes('EU')) return 70
  if (u.includes('US') && u.includes('SIZE')) return 60
  if (u.includes('UK') && u.includes('SIZE')) return 55
  return 0
}

/**
 * Resuelve qué columna matrix va primero (tallas tipo 39, 40, 41).
 * @param {{ id: string, tags?: string[] }[]} fs
 */
export function resolvePrimarySizeMatrixColumnId(fs) {
  const list = Array.isArray(fs) ? fs.filter((f) => f && String(f.id || '').trim()) : []
  if (!list.length) return ''

  const eu = pickEuMatrixColumnId(list)
  if (eu && list.some((f) => String(f.id) === eu)) return eu

  for (const id of PRIMARY_SIZE_FALLBACK_IDS) {
    if (list.some((f) => String(f.id) === id)) return id
  }

  const mainCands = list.filter(hasMainAttributeCandidateTag)
  if (mainCands.length === 1) return String(mainCands[0].id)

  if (mainCands.length > 1) {
    const sorted = [...mainCands].sort(
      (a, b) =>
        scoreMainCandidateId(b.id) - scoreMainCandidateId(a.id) ||
        String(a.id).localeCompare(String(b.id), undefined, { sensitivity: 'base' })
    )
    return String(sorted[0].id)
  }

  return ''
}

function isNumberUnitLikeValueType(valueType) {
  const vt = String(valueType || '').toLowerCase()
  return vt === 'number_unit' || vt === 'number_unit_input'
}

/**
 * Orden de columnas matrix para la grilla de guía (dinámico según ids que venga ML).
 * 1) MANUFACTURER_SIZE (talla fabricante; se rellena desde catálogo / EU en el modal)
 * 2) FOOT_LENGTH, FOOT_LENGTH_TO (UI las agrupa como «Largo del pie»)
 * 3) Atributos number_unit / number_unit_input restantes
 * 4) Tallas regionales (lista fija)
 * 5) Resto alfabético
 * @param {{ id: string, tags?: string[], name?: string, valueType?: string }[]} matrixFields
 * @returns {{ id: string, tags?: string[], name?: string, valueType?: string }[]}
 */
export function sortMatrixFieldsForSizeChartGrid(matrixFields) {
  const fs = Array.isArray(matrixFields) ? matrixFields.filter((f) => f && String(f.id || '').trim()) : []
  if (fs.length <= 1) return fs.slice()

  const byId = new Map(fs.map((f) => [String(f.id).trim(), f]))
  const used = new Set()
  const out = []

  const take = (id) => {
    const k = String(id || '').trim()
    if (!k || !byId.has(k) || used.has(k)) return
    out.push(byId.get(k))
    used.add(k)
  }

  take('MANUFACTURER_SIZE')

  for (const id of FOOT_LENGTH_COLUMN_IDS) take(id)

  const numberish = fs
    .filter((f) => !used.has(String(f.id).trim()))
    .filter((f) => isNumberUnitLikeValueType(f.valueType) && Array.isArray(f.units) && f.units.length)
    .sort((a, b) => String(a.id).localeCompare(String(b.id), undefined, { sensitivity: 'base' }))
  for (const f of numberish) take(f.id)

  for (const id of REGIONAL_MATRIX_COLUMN_ORDER) take(id)

  const rest = fs
    .filter((f) => !used.has(String(f.id).trim()))
    .sort((a, b) => String(a.id).localeCompare(String(b.id), undefined, { sensitivity: 'base' }))
  out.push(...rest)

  return out
}

export function isBrandChartFilterField(f) {
  if (!f || f.scope !== 'chart_filter') return false
  const idU = String(f.id || '').toUpperCase()
  if (idU === 'BRAND' || idU.includes('BRAND')) return true
  return /marca|brand|fabricante/i.test(String(f.name || ''))
}

export function isGenderChartFilterField(f) {
  if (!f || f.scope !== 'chart_filter') return false
  const id = String(f.id || '')
  const name = String(f.name || '')
  return (
    /gender|género|genero/i.test(id) ||
    /gender|género|genero/i.test(name) ||
    id.toUpperCase().includes('GENDER')
  )
}

/** Filtros de guía distintos de marca/género (COLOR, etc.). */
export function otherChartFilterFieldsFromExtracted(fields) {
  return chartFilterFieldsFromExtracted(fields).filter((f) => !isBrandChartFilterField(f) && !isGenderChartFilterField(f))
}

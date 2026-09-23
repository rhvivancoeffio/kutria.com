/**
 * Helpers for editing canonical catalog JSON (specifications / variations).
 * Mirrors backend CatalogSpecificationJsonHelper + common staging shapes.
 */

export function deepCloneJson(value) {
  if (value === undefined) return undefined
  try {
    return JSON.parse(JSON.stringify(value))
  } catch {
    return value
  }
}

export function parseCanonicalRoot(canonicalJson) {
  if (canonicalJson == null || canonicalJson === '') return {}
  try {
    const o = typeof canonicalJson === 'string' ? JSON.parse(canonicalJson) : canonicalJson
    return o && typeof o === 'object' && !Array.isArray(o) ? deepCloneJson(o) : {}
  } catch {
    return {}
  }
}

export function displaySpecValue(v) {
  if (v == null) return ''
  const k = typeof v
  if (k === 'string' || k === 'number' || k === 'boolean') return String(v)
  if (k === 'object') {
    if (v.name != null && (typeof v.name === 'string' || typeof v.name === 'number')) return String(v.name).trim()
    if (v.value !== undefined) return displaySpecValue(v.value)
    try {
      return JSON.stringify(v)
    } catch {
      return ''
    }
  }
  return String(v)
}

export function parseSpecInput(text) {
  const raw = text == null ? '' : String(text)
  const t = raw.trim()
  if (!t) return ''
  if (/^-?\d+(\.\d+)?$/.test(t)) return Number(t)
  if (t === 'true') return true
  if (t === 'false') return false
  if (t.startsWith('{') || t.startsWith('[')) {
    try {
      return JSON.parse(t)
    } catch {
      return raw
    }
  }
  return raw
}

export function readSpecifications(obj) {
  const specs = obj?.specifications
  if (!specs || typeof specs !== 'object' || Array.isArray(specs)) return {}
  return specs
}

export function readVariations(root) {
  const v = root?.variations
  if (!Array.isArray(v)) return []
  return v.map((row) => (row && typeof row === 'object' ? deepCloneJson(row) : {}))
}

export function ensureVariationsArray(root) {
  if (!root.variations || !Array.isArray(root.variations)) root.variations = []
  return root.variations
}

export function unionKeys(...keyLists) {
  const s = new Set()
  for (const list of keyLists) {
    if (!Array.isArray(list)) continue
    for (const k of list) {
      if (k != null && String(k).trim() !== '') s.add(String(k).trim())
    }
  }
  return [...s].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' }))
}

export function collectVariationSpecKeys(variations) {
  const keys = []
  for (const row of variations) {
    const sp = readSpecifications(row)
    keys.push(...Object.keys(sp))
  }
  return keys
}

export function attributeNamesFromMetadata(items, scope) {
  if (!Array.isArray(items)) return []
  const want = String(scope || '').toLowerCase()
  return items
    .filter((x) => x && String(x.type || '').toLowerCase() === 'specification')
    .filter((x) => String(x.specificationScope || '').toLowerCase() === want)
    .map((x) => x.attributeName)
    .filter(Boolean)
}

/** Lee URLs a nivel producto en el root canónico (`images` + `primaryImageUrl`). Misma forma que en variaciones. */
export function readProductImageUrls(root) {
  if (!root || typeof root !== 'object') return []
  const raw = root.images ?? root.Images
  const out = []
  if (Array.isArray(raw)) {
    for (const x of raw) {
      if (typeof x === 'string') {
        const u = x.trim()
        if (u) out.push(u)
        continue
      }
      if (x && typeof x === 'object') {
        const u = x.url ?? x.URL ?? x.href ?? x.Href
        if (u != null && String(u).trim()) out.push(String(u).trim())
      }
    }
  }
  if (!out.length) {
    const one = root.primaryImageUrl ?? root.PrimaryImageUrl
    if (one != null && String(one).trim()) out.push(String(one).trim())
  }
  return out
}

/** Lee URLs de imágenes en una variación (strings u objetos con url/URL). Incluye primaryImageUrl si no hay lista. */
export function readVariationImageUrls(row) {
  if (!row || typeof row !== 'object') return []
  const raw = row.images ?? row.Images
  const out = []
  if (Array.isArray(raw)) {
    for (const x of raw) {
      if (typeof x === 'string') {
        const u = x.trim()
        if (u) out.push(u)
        continue
      }
      if (x && typeof x === 'object') {
        const u = x.url ?? x.URL ?? x.href ?? x.Href
        if (u != null && String(u).trim()) out.push(String(u).trim())
      }
    }
  }
  if (!out.length) {
    const one = row.primaryImageUrl ?? row.PrimaryImageUrl
    if (one != null && String(one).trim()) out.push(String(one).trim())
  }
  return out
}

export function readVariationDimensions(row) {
  if (!row || typeof row !== 'object') return null
  const d = row.dimensions ?? row.Dimensions
  if (!d || typeof d !== 'object' || Array.isArray(d)) return null
  return d
}

/** Claves canónicas en JSON (el builder usa camelCase). */
export const VARIATION_DIMENSION_FIELDS = [
  { key: 'length', shortLabel: 'L', label: 'Largo' },
  { key: 'width', shortLabel: 'W', label: 'Ancho' },
  { key: 'height', shortLabel: 'H', label: 'Alto' },
  { key: 'weight', shortLabel: 'Peso', label: 'Peso' },
  { key: 'cubicWeight', shortLabel: 'Cúb.', label: 'Peso cúbico' }
]

export function dimensionKeyAsPascal(key) {
  if (key === 'cubicWeight') return 'CubicWeight'
  if (!key) return key
  return key[0].toUpperCase() + key.slice(1)
}

export function readVariationDimensionValue(row, key) {
  const dim = readVariationDimensions(row)
  if (!dim || !key) return null
  const pascal = dimensionKeyAsPascal(key)
  const v = dim[key] ?? dim[pascal]
  if (v == null || v === '') return null
  if (typeof v === 'number') return Number.isFinite(v) ? v : null
  const n = Number(String(v).trim().replace(',', '.'))
  return Number.isFinite(n) ? n : null
}

/** True si no queda ningún valor numérico en los campos estándar de dimensiones. */
export function variationDimensionsRowIsEmpty(dim) {
  if (!dim || typeof dim !== 'object' || Array.isArray(dim)) return true
  return !VARIATION_DIMENSION_FIELDS.some(({ key }) => readVariationDimensionValue({ dimensions: dim }, key) != null)
}

/** Texto compacto para L×W×H, peso, peso cúbico (claves camel o Pascal). */
export function formatDimensionsDisplay(dim) {
  if (!dim) return ''
  const L = dim.length ?? dim.Length
  const W = dim.width ?? dim.Width
  const H = dim.height ?? dim.Height
  const parts = []
  const lwh = [L, W, H].filter((x) => x != null && x !== '')
  if (lwh.length) parts.push(lwh.join(' × '))
  const w = dim.weight ?? dim.Weight
  if (w != null && w !== '') parts.push(`peso ${w}`)
  const cw = dim.cubicWeight ?? dim.CubicWeight
  if (cw != null && cw !== '') parts.push(`cúb. ${cw}`)
  return parts.join(' · ')
}

export function readVariationPrice(row) {
  if (!row || typeof row !== 'object') return null
  const p = row.price ?? row.Price
  if (p == null || p === '') return null
  const n = typeof p === 'number' ? p : Number(p)
  return Number.isFinite(n) ? n : null
}

/** Precio promocional canónico (`specialPrice`); ausente o no numérico = null. */
export function readVariationSpecialPrice(row) {
  if (!row || typeof row !== 'object') return null
  const p = row.specialPrice ?? row.SpecialPrice
  if (p == null || p === '') return null
  const n = typeof p === 'number' ? p : Number(p)
  return Number.isFinite(n) ? n : null
}

export function readVariationStock(row) {
  if (!row || typeof row !== 'object') return null
  const q = row.availableQuantity ?? row.AvailableQuantity
  if (q == null || q === '') return null
  const n = typeof q === 'number' ? q : Number(q)
  return Number.isFinite(n) ? n : null
}

export function readVariationCurrencyId(row) {
  if (!row || typeof row !== 'object') return null
  const c = row.currencyId ?? row.CurrencyId
  if (c == null) return null
  const s = String(c).trim()
  return s || null
}

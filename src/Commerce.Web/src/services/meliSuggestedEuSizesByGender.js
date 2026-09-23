/**
 * Tallas EU sugeridas para calzado cuando el usuario no ingresa lista propia.
 * @param {string} genderLabel — texto o id del filtro género (ML suele traer nombre legible)
 * @returns {number[]}
 */
export function suggestEuSizesFromGenderLabel(genderLabel) {
  const g = String(genderLabel ?? '')
    .trim()
    .toLowerCase()
  if (!g) return [...DEFAULT_UNISEX_EU]

  if (
    /mujer|woman|female|femenin|ladies|dama|w\b/.test(g) &&
    !/hombre|man|male|masculin|niño|niña|kid|infant|junior/.test(g)
  ) {
    return [35, 36, 37, 38, 39, 40, 41, 42]
  }
  if (/niñ|niño|niña|kid|infant|junior|youth|joven/.test(g)) {
    return [28, 29, 30, 31, 32, 33, 34, 35, 36]
  }
  if (/hombre|man|male|masculin|caballero|men\b/.test(g)) {
    return [38, 39, 40, 41, 42, 43, 44, 45, 46]
  }
  if (/unisex|uni\b/.test(g)) {
    return [...DEFAULT_UNISEX_EU]
  }
  return [...DEFAULT_UNISEX_EU]
}

const DEFAULT_UNISEX_EU = [38, 39, 40, 41, 42, 43, 44]

/**
 * @param {string} text — "39, 40, 41" o "39;40"
 * @returns {number[]}
 */
export function parseEuSizesInput(text) {
  const s = String(text ?? '').trim()
  if (!s) return []
  return s
    .split(/[,;\s]+/)
    .map((x) => x.trim())
    .filter(Boolean)
    .map((x) => Number(x.replace(',', '.')))
    .filter((n) => Number.isFinite(n) && n > 0)
}

/**
 * Columna donde rellenar tallas desde el catálogo (specification) o tallas EU sugeridas:
 * prioriza MANUFACTURER_SIZE si existe en la matriz ML.
 */
export function pickCatalogSizeMatrixColumnId(matrixFields) {
  const fs = Array.isArray(matrixFields) ? matrixFields : []
  if (fs.some((f) => String(f?.id || '').toUpperCase() === 'MANUFACTURER_SIZE')) {
    return 'MANUFACTURER_SIZE'
  }
  const eu = pickEuMatrixColumnId(fs)
  if (eu) return eu
  return String(fs[0]?.id || '').trim()
}

/** Heurística: columna principal de talla EU en la matriz ML. */
export function pickEuMatrixColumnId(matrixFields) {
  const fs = Array.isArray(matrixFields) ? matrixFields : []
  const byScore = (id) => {
    const u = String(id || '').toUpperCase()
    if (u === 'EU_SIZE' || u === 'EU' || u.endsWith('EU_SIZE')) return 100
    if (u.includes('EU') && u.includes('SIZE')) return 80
    if (u.includes('EU')) return 50
    return 0
  }
  let best = ''
  let score = 0
  for (const f of fs) {
    const id = String(f?.id || '').trim()
    const sc = byScore(id)
    if (sc > score) {
      score = sc
      best = id
    }
  }
  return best
}

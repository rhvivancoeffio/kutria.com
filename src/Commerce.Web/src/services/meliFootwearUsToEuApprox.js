/**
 * Conversión aproximada US → EU (calzado) para autocompletar guías ML cuando la columna
 * principal es MANUFACTURER_SIZE en escala US. No sustituye tablas de marca; solo evita
 * valores EU absurdos fuera del rango típico que exige ML (p. ej. 33–51).
 */

function interpPairs(pairs, x) {
  if (!pairs.length) return NaN
  if (x <= pairs[0][0]) return pairs[0][1] + (x - pairs[0][0]) * 0.5
  const last = pairs[pairs.length - 1]
  if (x >= last[0]) return last[1] + (x - last[0]) * 0.5
  for (let i = 0; i < pairs.length - 1; i++) {
    const [u0, e0] = pairs[i]
    const [u1, e1] = pairs[i + 1]
    if (x >= u0 && x <= u1) {
      const t = (x - u0) / (u1 - u0 || 1)
      return e0 + t * (e1 - e0)
    }
  }
  return pairs[0][1]
}

/** [US, EU] anclas mujer (US 4–15). */
const WOMENS_US_EU = [
  [4, 34],
  [5, 35],
  [6, 36],
  [7, 37.5],
  [8, 39],
  [9, 40],
  [10, 41],
  [11, 42.5],
  [12, 44],
  [13, 45],
  [14, 46],
  [15, 47]
]

/** [US, EU] anclas hombre (US 6–15). */
const MENS_US_EU = [
  [6, 38],
  [7, 39],
  [8, 40],
  [9, 41],
  [10, 42],
  [11, 43],
  [12, 44.5],
  [13, 46],
  [14, 47],
  [15, 48]
]

function isMaleGenderLabel(genderLabel) {
  const g = String(genderLabel ?? '')
    .trim()
    .toLowerCase()
  return (
    (/hombre|man|male|masculin|caballero|men\b/.test(g) && !/mujer|woman|female|femenin|dama/.test(g)) ||
    /niñ|niño|niña|kid|infant|junior|youth|joven/.test(g)
  )
}

function isFemaleGenderLabel(genderLabel) {
  const g = String(genderLabel ?? '')
    .trim()
    .toLowerCase()
  return (
    /mujer|woman|female|femenin|ladies|dama/.test(g) &&
    !/hombre|man|male|masculin|caballero|men\b/.test(g) &&
    !/niñ|niño|niña|kid|infant|junior|youth|joven/.test(g)
  )
}

/**
 * @param {number} usNum — talla US (fabricante)
 * @param {string} genderLabel — etiqueta de género de la pestaña
 * @param {{ minEu?: number, maxEu?: number }} bounds
 * @returns {number} EU aproximada, acotada
 */
export function approxEuFromUsFootwear(usNum, genderLabel, bounds = {}) {
  const minEu = Number.isFinite(bounds.minEu) ? bounds.minEu : 33
  const maxEu = Number.isFinite(bounds.maxEu) ? bounds.maxEu : 51
  if (!Number.isFinite(usNum)) return NaN

  let eu
  if (isMaleGenderLabel(genderLabel)) {
    eu = usNum < 6 ? 33 + (usNum - 1) * 0.6 : interpPairs(MENS_US_EU, usNum)
  } else if (isFemaleGenderLabel(genderLabel)) {
    eu = usNum < 4 ? 32 + usNum * 0.35 : interpPairs(WOMENS_US_EU, usNum)
  } else {
    const w = interpPairs(WOMENS_US_EU, Math.min(Math.max(usNum, 4), 15))
    const m = interpPairs(MENS_US_EU, Math.min(Math.max(usNum, 6), 15))
    eu = (w + m) / 2
  }

  if (!Number.isFinite(eu)) return NaN
  const rounded = Math.round(eu * 2) / 2
  return Math.min(maxEu, Math.max(minEu, rounded))
}

/** [EU, US] hombre (inverso de MENS_US_EU + extrapolación baja). */
const MENS_EU_US_ANCHORS = [
  [33, 3.5],
  [34, 4],
  [35, 4.5],
  [36, 5],
  [37, 5.5],
  [38, 6],
  [39, 7],
  [40, 8],
  [41, 9],
  [42, 10],
  [43, 11],
  [44.5, 12],
  [46, 13],
  [47, 14],
  [48, 15]
]

/** [EU, US] mujer (inverso de WOMENS_US_EU + extrapolación baja). */
const WOMENS_EU_US_ANCHORS = [
  [32, 3],
  [33, 3.5],
  [34, 4],
  [35, 5],
  [36, 6],
  [37.5, 7],
  [39, 8],
  [40, 9],
  [41, 10],
  [42.5, 11],
  [44, 12],
  [45, 13],
  [46, 14],
  [47, 15]
]

/**
 * EU aproximada → US aproximado (inverso de approxEuFromUsFootwear).
 * @param {number} euNum
 * @param {string} genderLabel
 * @returns {number} US en ~1–15
 */
export function approxUsFromEuFootwear(euNum, genderLabel) {
  if (!Number.isFinite(euNum)) return NaN
  if (isMaleGenderLabel(genderLabel)) return interpPairs(MENS_EU_US_ANCHORS, euNum)
  if (isFemaleGenderLabel(genderLabel)) return interpPairs(WOMENS_EU_US_ANCHORS, euNum)
  const m = interpPairs(MENS_EU_US_ANCHORS, euNum)
  const w = interpPairs(WOMENS_EU_US_ANCHORS, euNum)
  const mid = (m + w) / 2
  return Math.min(15, Math.max(1, Math.round(mid * 2) / 2))
}

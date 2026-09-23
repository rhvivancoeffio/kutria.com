/**
 * Autocompletado de filas de guía de tallas ML (grids) a partir de género y «Talle de marca» (MANUFACTURER_SIZE u otra columna catálogo).
 * Heurísticas aproximadas para largo del pie (cm); EU se deriva de conversión US→EU (no copia del fabricante).
 */

import { approxEuFromUsFootwear, approxUsFromEuFootwear } from '@/services/meliFootwearUsToEuApprox'

/**
 * Puntos [talla EU aprox., cm planta] monotónicos para interpolación.
 * Aproximación orientativa (no sustituye tabla de marca); sirve para autocompletar guía en UI.
 */
const EU_TO_FOOT_CM_ANCHORS = [
  [18, 17.0],
  [20, 18.0],
  [22, 19.0],
  [24, 20.0],
  [26, 21.0],
  [28, 22.0],
  [30, 23.0],
  [32, 24.0],
  [34, 24.8],
  [35, 25.0],
  [36, 25.4],
  [37, 25.8],
  [38, 26.1],
  [39, 26.5],
  [40, 26.9],
  [41, 27.3],
  [42, 27.7],
  [43, 28.1],
  [44, 28.5],
  [45, 28.9],
  [46, 29.3],
  [47, 29.7],
  [48, 30.1]
]

function parseNumericSize(raw) {
  const s = String(raw ?? '')
    .trim()
    .replace(',', '.')
  const n = Number(s)
  return Number.isFinite(n) ? n : NaN
}

/** Interpola cm medio de planta a partir de talla tratada como EU (aprox.). */
function footCmMidFromEuApprox(eu) {
  const a = EU_TO_FOOT_CM_ANCHORS
  if (eu <= a[0][0]) return a[0][1] + (eu - a[0][0]) * 0.5
  if (eu >= a[a.length - 1][0]) return a[a.length - 1][1] + (eu - a[a.length - 1][0]) * 0.5
  for (let i = 0; i < a.length - 1; i++) {
    const [e0, c0] = a[i]
    const [e1, c1] = a[i + 1]
    if (eu >= e0 && eu <= e1) {
      const t = (eu - e0) / (e1 - e0 || 1)
      return c0 + t * (c1 - c0)
    }
  }
  return a[0][1]
}

function genderFootCmDelta(genderLabel) {
  const g = String(genderLabel ?? '')
    .trim()
    .toLowerCase()
  if (/niñ|niño|niña|kid|infant|junior|youth|joven/.test(g)) return -0.4
  if (/mujer|woman|female|femenin|ladies|dama/.test(g) && !/hombre|man|male|masculin|caballero/.test(g)) {
    return -0.2
  }
  if (/hombre|man|male|masculin|caballero|men\b/.test(g)) return 0.2
  return 0
}

function round1(n) {
  return Math.round(n * 10) / 10
}

/**
 * Rango sugerido para FOOT_LENGTH / FOOT_LENGTH_TO (cm).
 * @param {number} euLike — valor numérico del talle (se interpreta como escala EU aprox. para largo planta).
 * @param {string} genderLabel
 * @returns {{ from: string, to: string } | null}
 */
export function suggestFootLengthCmRangeFromEuLike(euLike, genderLabel) {
  if (!Number.isFinite(euLike)) return null
  const mid = footCmMidFromEuApprox(euLike) + genderFootCmDelta(genderLabel)
  const half = 0.35
  return {
    from: String(round1(mid - half)),
    to: String(round1(mid + half))
  }
}

/** Pares [US, cm planta aprox.] monótonos (mujer / default). Evita duplicar FOOT_* cuando varias US chicas mapean a la misma EU. */
const WOMEN_US_FOOT_MID_ANCHORS = [
  [1, 11.8],
  [1.5, 12.3],
  [2, 12.8],
  [2.5, 13.4],
  [3, 14.1],
  [3.5, 14.9],
  [4, 18.6],
  [4.5, 19.5],
  [5, 21.5],
  [5.5, 22.0],
  [6, 22.5],
  [6.5, 23.0],
  [7, 23.5],
  [7.5, 24.0],
  [8, 24.5],
  [8.5, 25.0],
  [9, 25.6],
  [9.5, 26.1],
  [10, 26.7],
  [10.5, 27.2],
  [11, 27.8],
  [11.5, 28.3],
  [12, 28.9],
  [12.5, 29.4],
  [13, 29.9],
  [13.5, 30.3],
  [14, 30.7],
  [14.5, 31.1],
  [15, 31.5]
]

/** Hombre: US 1–15 (antes solo 6–15; US menor que 6 se trataban como 6 y duplicaban FOOT_* en ML). */
const MEN_US_FOOT_MID_ANCHORS = [
  [1, 12.1],
  [1.5, 12.7],
  [2, 13.3],
  [2.5, 14.1],
  [3, 15.1],
  [3.5, 16.4],
  [4, 19.4],
  [4.5, 20.5],
  [5, 22.4],
  [5.5, 23.5],
  [6, 24.0],
  [6.5, 24.5],
  [7, 25.0],
  [7.5, 25.5],
  [8, 26.0],
  [8.5, 26.5],
  [9, 27.0],
  [9.5, 27.5],
  [10, 28.0],
  [10.5, 28.5],
  [11, 29.0],
  [11.5, 29.5],
  [12, 30.0],
  [12.5, 30.5],
  [13, 31.0],
  [13.5, 31.4],
  [14, 31.8],
  [14.5, 32.2],
  [15, 32.6]
]

function interpFootMidFromAnchors(anchors, usNum) {
  const a = anchors
  if (!a.length || !Number.isFinite(usNum)) return NaN
  if (usNum <= a[0][0]) return a[0][1] + (usNum - a[0][0]) * 0.35
  if (usNum >= a[a.length - 1][0]) return a[a.length - 1][1] + (usNum - a[a.length - 1][0]) * 0.35
  for (let i = 0; i < a.length - 1; i++) {
    const [u0, c0] = a[i]
    const [u1, c1] = a[i + 1]
    if (usNum >= u0 && usNum <= u1) {
      const t = (usNum - u0) / (u1 - u0 || 1)
      return c0 + t * (c1 - c0)
    }
  }
  return a[0][1]
}

/**
 * Largo de pie (cm) coherente con talla US del fabricante, estrictamente creciente en US.
 * ML rechaza `duplicated_measure_value` si dos filas comparten el mismo par FOOT_LENGTH / FOOT_LENGTH_TO.
 */
function footCmMidFromManufacturerUs(usNum, genderLabel) {
  if (!Number.isFinite(usNum)) return NaN
  const g = String(genderLabel ?? '')
    .trim()
    .toLowerCase()
  const male =
    (/hombre|man|male|masculin|caballero|men\b/.test(g) && !/mujer|woman|female|femenin|dama/.test(g)) ||
    /niñ|niño|niña|kid|infant|junior|youth|joven/.test(g)

  if (male) {
    const u = Math.max(1, Math.min(15, usNum))
    return interpFootMidFromAnchors(MEN_US_FOOT_MID_ANCHORS, u) + genderFootCmDelta(genderLabel)
  }
  const u = Math.max(1, Math.min(15, usNum))
  return interpFootMidFromAnchors(WOMEN_US_FOOT_MID_ANCHORS, u) + genderFootCmDelta(genderLabel)
}

/**
 * Rango FOOT_LENGTH / FOOT_LENGTH_TO desde talla US (MANUFACTURER_SIZE), sin colapsar por EU.
 * @param {number} usNum
 * @param {string} genderLabel
 */
export function suggestFootLengthCmRangeFromManufacturerUs(usNum, genderLabel) {
  const mid = footCmMidFromManufacturerUs(usNum, genderLabel)
  if (!Number.isFinite(mid)) return null
  const half = 0.35
  return {
    from: String(round1(mid - half)),
    to: String(round1(mid + half))
  }
}

/** Interpolación 1D (x creciente). */
function interpPairsAxis(pairs, x) {
  if (!pairs.length || !Number.isFinite(x)) return NaN
  if (x <= pairs[0][0]) return pairs[0][1] + (x - pairs[0][0]) * 0.08
  const last = pairs[pairs.length - 1]
  if (x >= last[0]) return last[1] + (x - last[0]) * 0.08
  for (let i = 0; i < pairs.length - 1; i++) {
    const [x0, y0] = pairs[i]
    const [x1, y1] = pairs[i + 1]
    if (x >= x0 && x <= x1) {
      const t = (x - x0) / (x1 - x0 || 1)
      return y0 + t * (y1 - y0)
    }
  }
  return pairs[0][1]
}

/** Talla BR (17–45 típico) → US hombre aprox. (1–15) para columnas ML (rango numérico ~1–18). */
const BR_TO_US_MEN = [
  [17, 1],
  [18, 1.5],
  [19, 2],
  [20, 2.5],
  [21, 3],
  [22, 3.5],
  [23, 4],
  [24, 4.5],
  [25, 5],
  [26, 5.5],
  [27, 6],
  [28, 6.5],
  [29, 7],
  [30, 7.5],
  [31, 8],
  [32, 8.5],
  [33, 9],
  [34, 9.5],
  [35, 10],
  [36, 10.5],
  [37, 11],
  [38, 11.5],
  [39, 12],
  [40, 12.5],
  [41, 13],
  [42, 13.5],
  [43, 14],
  [44, 14.5],
  [45, 15]
]

function approxUsFromBrMen(br) {
  return interpPairsAxis(BR_TO_US_MEN, br)
}

const UK_FROM_US_MEN = [
  [1, 0.5],
  [1.5, 1],
  [2, 1.5],
  [2.5, 2],
  [3, 2.5],
  [4, 3.5],
  [5, 4.5],
  [6, 5.5],
  [7, 6.5],
  [8, 7.5],
  [9, 8.5],
  [10, 9.5],
  [11, 10.5],
  [12, 11.5],
  [13, 12.5],
  [14, 13.5],
  [15, 14.5]
]

function approxUkFromUsMen(us) {
  if (!Number.isFinite(us)) return NaN
  const u = Math.min(15, Math.max(1, us))
  return interpPairsAxis(UK_FROM_US_MEN, u)
}

function classifyManufacturerScale(mfgNum, mfgIdUpper, bounds) {
  if (!Number.isFinite(mfgNum) || mfgIdUpper !== 'MANUFACTURER_SIZE') return 'unknown'
  const minEu = bounds.minEu
  const maxEu = bounds.maxEu
  if (mfgNum >= minEu && mfgNum <= maxEu) return 'eu'
  if (mfgNum >= 1 && mfgNum <= 15.5) return 'us'
  if (mfgNum >= 16 && mfgNum <= 60) return 'br'
  return 'unknown'
}

function formatUsHalf(us) {
  if (!Number.isFinite(us)) return ''
  const r = Math.round(us * 2) / 2
  return Number.isInteger(r) ? String(r) : String(r)
}

function isMatrixRegionalSizeColumnId(colId) {
  const idU = String(colId || '').trim().toUpperCase()
  if (idU === 'EU_SIZE' || idU.endsWith('EU_SIZE')) return true
  return (
    idU === 'M_US_SIZE' ||
    idU === 'W_US_SIZE' ||
    idU === 'F_US_SIZE' ||
    idU === 'UK_SIZE' ||
    idU === 'AR_SIZE' ||
    idU === 'BR_SIZE' ||
    idU === 'JP_SIZE' ||
    idU === 'MX_SIZE' ||
    idU === 'CHILE_SIZE'
  )
}

function normVt(c) {
  return String(c?.valueType || '').toLowerCase()
}

function isNumberUnitInputDeclaredType(c) {
  return normVt(c) === 'number_unit_input'
}

/** Misma regla que el modal: una sola unidad o tipo input-only → sin selector en UI. */
export function isNumberUnitInputOnlyColumn(c) {
  if (isNumberUnitInputDeclaredType(c)) return true
  if (normVt(c) !== 'number_unit') return false
  const n = Array.isArray(c.units) ? c.units.length : 0
  return n <= 1
}

function isEuMatrixColumnId(colId) {
  const idU = String(colId || '').trim().toUpperCase()
  return idU === 'EU_SIZE' || idU.endsWith('EU_SIZE')
}

function isKidsGenderLabel(genderLabel) {
  const g = String(genderLabel ?? '')
    .trim()
    .toLowerCase()
  return /niñ|niño|niña|kid|infant|junior|youth|joven|bebé|bebe|toddler/.test(g)
}

/**
 * Límites por defecto si la ficha grids no trae valueMin/valueMax (ML suele validar en remoto).
 * UK: ML rechaza valores bajo 1.0 (p. ej. 0.5 UK desde US 1).
 */
function defaultNumericBoundsForColumnId(colId) {
  const idU = String(colId || '')
    .trim()
    .toUpperCase()
  if (idU === 'UK_SIZE') return { min: 1.0, max: 18.0 }
  if (idU === 'EU_SIZE' || idU.endsWith('EU_SIZE')) return { min: 16.0, max: 51.0 }
  if (idU === 'M_US_SIZE' || idU === 'W_US_SIZE' || idU === 'F_US_SIZE') return { min: 1.0, max: 15.0 }
  return { min: NaN, max: NaN }
}

/**
 * @param {{ id?: string, valueMin?: number, valueMax?: number } | null | undefined} col
 * @param {number} num
 */
function clampNumericForMatrixColumn(col, num) {
  if (!Number.isFinite(num)) return NaN
  const def = defaultNumericBoundsForColumnId(col?.id)
  const lo = Number.isFinite(col?.valueMin) ? col.valueMin : def.min
  const hi = Number.isFinite(col?.valueMax) ? col.valueMax : def.max
  let v = num
  if (Number.isFinite(lo)) v = Math.max(lo, v)
  if (Number.isFinite(hi)) v = Math.min(hi, v)
  return v
}

function shouldExcludeUsColumnForGender(colId, genderLabel) {
  const idU = String(colId || '').trim().toUpperCase()
  const g = String(genderLabel ?? '')
    .trim()
    .toLowerCase()
  const female =
    /mujer|woman|female|femenin|ladies|dama/.test(g) && !/hombre|man|male|masculin|caballero/.test(g)
  const male = /hombre|man|male|masculin|caballero|men\b/.test(g)
  /** En dominios niño/a ML suele rechazar M_US_SIZE en filas; no autocompletar. */
  if (idU === 'M_US_SIZE' && isKidsGenderLabel(genderLabel)) return true
  if (idU === 'M_US_SIZE' && female) return true
  if (idU === 'F_US_SIZE' && male) return true
  return false
}

/**
 * @param {{ id: string, valueType?: string, units?: { id: string }[], valueMin?: number, valueMax?: number }[]} matrixFields
 * @param {string} manufacturerColumnId
 * @param {string} colId
 * @param {string} genderLabel
 */
function shouldCopyManufacturerToColumn(matrixFields, manufacturerColumnId, colId, genderLabel) {
  const idU = String(colId || '').trim().toUpperCase()
  if (!idU) return false
  if (isMatrixRegionalSizeColumnId(colId)) return false
  if (shouldExcludeUsColumnForGender(colId, genderLabel)) return false
  if (idU === String(manufacturerColumnId || '').trim().toUpperCase()) return false
  if (idU === 'FOOT_LENGTH' || idU === 'FOOT_LENGTH_TO') return false
  const c = matrixFields.find((f) => String(f.id).trim().toUpperCase() === idU)
  if (!c) return false
  return isNumberUnitInputOnlyColumn(c)
}

/**
 * Rellena largo del pie (cm) y copia el texto de «Talle de marca» a columnas number (una unidad / NUMBER_UNIT_INPUT).
 * @param {{ matrixFields: object[], matrixRows: object[], genderLabel: string, manufacturerColumnId: string }} p
 * @returns {number} filas tocadas (con talle de marca no vacío)
 */
export function autofillSizeChartGuideRows(p) {
  const matrixFields = Array.isArray(p.matrixFields) ? p.matrixFields : []
  const matrixRows = Array.isArray(p.matrixRows) ? p.matrixRows : []
  const genderLabel = String(p.genderLabel ?? '')
  const mfgId = String(p.manufacturerColumnId ?? '').trim()
  if (!mfgId || !matrixRows.length) return 0

  const footL = matrixFields.find((f) => String(f.id).toUpperCase() === 'FOOT_LENGTH')
  const footR = matrixFields.find((f) => String(f.id).toUpperCase() === 'FOOT_LENGTH_TO')
  const euCol = matrixFields.find((f) => isEuMatrixColumnId(f.id))
  const mUsCol = matrixFields.find((f) => String(f.id).toUpperCase() === 'M_US_SIZE')
  const wUsCol = matrixFields.find((f) => String(f.id).toUpperCase() === 'W_US_SIZE')
  const ukCol = matrixFields.find((f) => String(f.id).toUpperCase() === 'UK_SIZE')
  const preferCm =
    footL &&
    Array.isArray(footL.units) &&
    footL.units.some((u) => String(u.id).replace(/"/g, '') === 'cm' || String(u.id).toLowerCase() === 'cm')

  let touched = 0
  for (const row of matrixRows) {
    const mfg = String(row[mfgId] ?? '').trim()
    if (!mfg) continue
    touched += 1

    const mfgNum = parseNumericSize(mfg)
    const bounds = {
      minEu: Number.isFinite(euCol?.valueMin) ? euCol.valueMin : 33,
      maxEu: Number.isFinite(euCol?.valueMax) ? euCol.valueMax : 51
    }
    const mfgIdU = String(mfgId || '').toUpperCase()
    const scale = classifyManufacturerScale(mfgNum, mfgIdU, bounds)

    let derivedUs = NaN
    let euForFoot = NaN
    let euDisplay = NaN

    if (scale === 'eu' && Number.isFinite(mfgNum)) {
      euForFoot = mfgNum
      euDisplay = mfgNum
      derivedUs = approxUsFromEuFootwear(mfgNum, genderLabel)
    } else if (scale === 'us' && Number.isFinite(mfgNum)) {
      derivedUs = Math.min(15, Math.max(1, mfgNum))
      euForFoot = approxEuFromUsFootwear(derivedUs, genderLabel, bounds)
      euDisplay = euForFoot
    } else if (scale === 'br' && Number.isFinite(mfgNum)) {
      derivedUs = Math.min(15, Math.max(1, approxUsFromBrMen(mfgNum)))
      euForFoot = approxEuFromUsFootwear(derivedUs, genderLabel, bounds)
      euDisplay = euForFoot
    } else if (Number.isFinite(mfgNum)) {
      euForFoot = approxEuFromUsFootwear(mfgNum, genderLabel, bounds)
      euDisplay = euForFoot
      derivedUs = approxUsFromEuFootwear(euForFoot, genderLabel)
    }

    if (footL && footR && Number.isFinite(mfgNum)) {
      let range = null
      if (scale === 'eu' && Number.isFinite(euForFoot)) {
        range = suggestFootLengthCmRangeFromEuLike(euForFoot, genderLabel)
      } else if (Number.isFinite(derivedUs)) {
        range = suggestFootLengthCmRangeFromManufacturerUs(derivedUs, genderLabel)
      } else if (Number.isFinite(euForFoot)) {
        range = suggestFootLengthCmRangeFromEuLike(euForFoot, genderLabel)
      }
      if (range) {
        row[footL.id] = range.from
        row[footR.id] = range.to
        if (preferCm) {
          const cmId =
            footL.units.find((u) => String(u.id).replace(/"/g, '').toLowerCase() === 'cm')?.id || 'cm'
          row[`${footL.id}__unit`] = cmId
          row[`${footR.id}__unit`] = cmId
        }
      }
    }

    if (euCol && Number.isFinite(euDisplay)) {
      const clampedEu = clampNumericForMatrixColumn(euCol, euDisplay)
      const half = Math.round(clampedEu * 2) / 2
      row[euCol.id] = Number.isInteger(half) ? String(half) : String(half)
    }

    if (Number.isFinite(derivedUs)) {
      if (mUsCol && !shouldExcludeUsColumnForGender(mUsCol.id, genderLabel)) {
        const usClamped = clampNumericForMatrixColumn(mUsCol, derivedUs)
        row[mUsCol.id] = formatUsHalf(usClamped)
      }
      if (wUsCol && !shouldExcludeUsColumnForGender(wUsCol.id, genderLabel)) {
        const usClamped = clampNumericForMatrixColumn(wUsCol, derivedUs)
        row[wUsCol.id] = formatUsHalf(usClamped)
      }
      if (ukCol) {
        const uk = approxUkFromUsMen(derivedUs)
        if (Number.isFinite(uk)) {
          const ukClamped = clampNumericForMatrixColumn(ukCol, uk)
          row[ukCol.id] = String(round1(ukClamped))
        }
      }
    }

    for (const c of matrixFields) {
      if (!shouldCopyManufacturerToColumn(matrixFields, mfgId, c.id, genderLabel)) continue
      row[c.id] = mfg
    }
  }

  // ML exige medidas de pie únicas por fila; si el redondeo iguala dos pares (from|to), separar ligeramente el "to".
  if (footL && footR) {
    const seen = new Set()
    for (const row of matrixRows) {
      const a = String(row[footL.id] ?? '').trim()
      let b = String(row[footR.id] ?? '').trim()
      if (!a || !b) continue
      let k = `${a}|${b}`
      if (!seen.has(k)) {
        seen.add(k)
        continue
      }
      let toNum = Number(b.replace(',', '.'))
      if (!Number.isFinite(toNum)) continue
      while (seen.has(k)) {
        toNum = round1(toNum + 0.1)
        b = String(toNum)
        k = `${a}|${b}`
      }
      row[footR.id] = b
      seen.add(k)
    }
  }

  return touched
}

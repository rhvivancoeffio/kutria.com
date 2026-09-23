/** @typedef {'products' | 'stocks' | 'prices'} CatalogImportKind */

export const MAX_CATALOG_CSV_DATA_ROWS = 200_000

export const CATALOG_IMPORT_KINDS = /** @type {const} */ ([
  {
    id: 'products',
    label: 'Productos',
    description: 'Título y SKU por fila. Id opcional: vacío o ausente = alta; UUID = actualización.',
    requiredHint: 'Title o Sku (o columnas equivalentes). Id opcional.',
    forbidHint: 'Podés incluir opcionalmente Price, CurrencyId, stock, color, talla y dimensiones por fila.',
    backPath: '/admin/data/products',
    backLabel: '← Productos (Datos)'
  },
  {
    id: 'stocks',
    label: 'Stocks',
    description: 'Stock por variación: ProductId del producto + SkuId de la variación (como en el CSV de descarga).',
    requiredHint: 'ProductId, SkuId y AvailableQuantity.',
    forbidHint: 'No debe incluir Title, Price ni CurrencyId.',
    backPath: '/admin/data/stocks',
    backLabel: '← Stocks'
  },
  {
    id: 'prices',
    label: 'Precios',
    description: 'Precio/moneda por variación: ProductId + SkuId (como en el CSV de descarga).',
    requiredHint: 'ProductId, SkuId y al menos Price o CurrencyId.',
    forbidHint: 'No debe incluir Title ni AvailableQuantity.',
    backPath: '/admin/data/prices',
    backLabel: '← Precios'
  }
])

/** @param {string[]} headers */
export function headerKeySet(headers) {
  return new Set(headers.map((h) => String(h ?? '').trim().toLowerCase()).filter(Boolean))
}

/** @param {Set<string>} set @param {string} name */
function hasCol(set, name) {
  return set.has(name.toLowerCase())
}

/** Cabecera usada como título de producto (export o plantillas en español). */
export function findCatalogProductTitleHeader(headers) {
  if (!headers?.length) return null
  const candidates = ['title', 'titulo', 'nombre', 'name', 'product_name', 'modelo_nombre']
  const byLower = new Map(headers.map((h) => [String(h ?? '').trim().toLowerCase(), h]))
  for (const c of candidates) {
    if (byLower.has(c)) return /** @type {string} */ (byLower.get(c))
  }
  return null
}

/** Cabecera SKU (texto de variación en import de productos). No usa SkuId para no confundir con el id de variación. */
export function findCatalogProductSkuHeader(headers) {
  if (!headers?.length) return null
  const candidates = ['sku', 'sku_id']
  const byLower = new Map(headers.map((h) => [String(h ?? '').trim().toLowerCase(), h]))
  for (const c of candidates) {
    if (byLower.has(c)) return /** @type {string} */ (byLower.get(c))
  }
  return null
}

/** Cabecera Id (GUID). */
export function findCatalogIdHeader(headers) {
  if (!headers?.length) return null
  const byLower = new Map(headers.map((h) => [String(h ?? '').trim().toLowerCase(), h]))
  return byLower.get('id') ?? null
}

function hasProductTitleCol(s) {
  return ['title', 'titulo', 'nombre', 'name', 'product_name', 'modelo_nombre'].some((n) => hasCol(s, n))
}

function hasProductSkuCol(s) {
  return ['sku', 'sku_id'].some((n) => hasCol(s, n))
}

function hasMetricsProductIdCol(s) {
  return (
    hasCol(s, 'productid') ||
    hasCol(s, 'entityid') ||
    hasCol(s, 'sourceproductid')
  )
}

function hasMetricsSkuIdCol(s) {
  return (
    hasCol(s, 'skuid') ||
    hasCol(s, 'sourcevariationskuid') ||
    hasCol(s, 'variation_sku_id') ||
    hasCol(s, 'ml_sku_id') ||
    hasCol(s, 'numeric_sku_id')
  )
}

/**
 * Infiere el tipo solo por columnas (archivo “puro” por dominio).
 * Productos puede no traer Id (altas).
 * @param {string[]} headers
 * @returns {CatalogImportKind | null}
 */
export function inferCatalogImportKindFromHeaders(headers) {
  const s = headerKeySet(headers)
  const hasStock = hasCol(s, 'availablequantity')
  const hasPriceCols = hasCol(s, 'price') || hasCol(s, 'currencyid')
  const hasMetricsPid = hasMetricsProductIdCol(s)
  const hasMetricsSid = hasMetricsSkuIdCol(s)

  if (
    hasMetricsPid &&
    hasMetricsSid &&
    hasStock &&
    !hasPriceCols &&
    !hasProductTitleCol(s)
  ) {
    return 'stocks'
  }
  if (
    hasMetricsPid &&
    hasMetricsSid &&
    hasPriceCols &&
    !hasStock &&
    !hasProductTitleCol(s)
  ) {
    return 'prices'
  }

  const hasProduct = hasProductTitleCol(s) || hasProductSkuCol(s)
  if (hasProduct) return 'products'

  if (hasStock && hasPriceCols) return null
  return null
}

const Guidish = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

/**
 * Vista previa: agrupa filas por título repetido (mismo producto, varias SKUs).
 * @param {Record<string, string>[]} rows
 * @param {string | null} titleHeader
 * @param {string | null} skuHeader
 * @param {string | null} idHeader
 */
export function groupCatalogProductRowsByTitle(rows, titleHeader, skuHeader, idHeader) {
  if (!titleHeader || !rows?.length) return []

  /** @type {Map<string, { displayTitle: string, skus: string[], rowCount: number, inserts: number, updates: number }>} */
  const map = new Map()

  for (const row of rows) {
    const rawTitle = String(row[titleHeader] ?? '').trim()
    const key = rawTitle.toLowerCase() || '\u0000empty'
    const sku = skuHeader ? String(row[skuHeader] ?? '').trim() : ''
    const idRaw = idHeader ? String(row[idHeader] ?? '').trim() : ''
    const hasGuid = Guidish.test(idRaw)

    let g = map.get(key)
    if (!g) {
      g = { displayTitle: rawTitle || '—', skus: [], rowCount: 0, inserts: 0, updates: 0 }
      map.set(key, g)
    }
    if (rawTitle && g.displayTitle === '—') g.displayTitle = rawTitle
    g.rowCount++
    if (hasGuid) g.updates++
    else g.inserts++
    if (sku && !g.skus.includes(sku)) g.skus.push(sku)
  }

  return [...map.values()].sort((a, b) => a.displayTitle.localeCompare(b.displayTitle, 'es'))
}

/**
 * Valida que el CSV encaje con el tipo elegido (columnas obligatorias y sin mezcla de dominios).
 * @param {CatalogImportKind} kind
 * @param {string[]} headers
 * @returns {{ ok: boolean, message?: string, inferred: CatalogImportKind | null }}
 */
export function validateCatalogImportHeadersForKind(kind, headers) {
  const s = headerKeySet(headers)
  const inferred = inferCatalogImportKindFromHeaders(headers)

  if (kind === 'stocks' || kind === 'prices') {
    if (hasCol(s, 'id')) {
      return {
        ok: false,
        inferred,
        message:
          'Las importaciones de stocks y precios no deben incluir la columna Id. Usá ProductId y SkuId (como en el CSV de descarga).'
      }
    }
    if (!hasMetricsProductIdCol(s)) {
      return {
        ok: false,
        inferred,
        message:
          'Para stocks o precios hace falta la columna ProductId (también acepta entityId u otros alias de la plantilla).'
      }
    }
    if (!hasMetricsSkuIdCol(s)) {
      return {
        ok: false,
        inferred,
        message:
          'Para stocks o precios hace falta la columna SkuId (identificador de la variación en el canónico).'
      }
    }
  }

  if (kind === 'stocks') {
    if (!hasCol(s, 'availablequantity')) {
      return { ok: false, inferred, message: 'Para stocks hace falta la columna AvailableQuantity.' }
    }
    for (const c of ['title', 'titulo', 'nombre', 'name', 'product_name', 'modelo_nombre', 'price', 'currencyid']) {
      if (hasCol(s, c)) {
        return {
          ok: false,
          inferred,
          message:
            'Este archivo mezcla columnas de otros tipos. Un CSV de stock solo debe tener ProductId, SkuId, AvailableQuantity, opcionalmente Sku y columnas auxiliares (sin título, precio ni moneda).'
        }
      }
    }
    return { ok: true, inferred }
  }

  if (kind === 'prices') {
    if (!hasCol(s, 'price') && !hasCol(s, 'currencyid')) {
      return { ok: false, inferred, message: 'Para precios hace falta al menos una columna: Price o CurrencyId.' }
    }
    for (const c of ['title', 'titulo', 'nombre', 'name', 'product_name', 'modelo_nombre', 'availablequantity']) {
      if (hasCol(s, c)) {
        return {
          ok: false,
          inferred,
          message:
            'Este archivo mezcla columnas de otros tipos. Un CSV de precios solo debe tener ProductId, SkuId, Price/CurrencyId, opcionalmente Sku y columnas auxiliares.'
        }
      }
    }
    return { ok: true, inferred }
  }

  // products — Id opcional (vacío = alta, UUID = actualización)
  if (!hasProductTitleCol(s) && !hasProductSkuCol(s)) {
    return {
      ok: false,
      inferred,
      message:
        'Para productos hace falta al menos una columna de título (Title, titulo, nombre, …) o de SKU. La columna Id es opcional: si falta o va vacía, la fila se interpreta como producto nuevo.'
    }
  }
  return { ok: true, inferred }
}

/** @param {string} raw */
export function normalizeImportKindQuery(raw) {
  const v = String(raw || '').toLowerCase().trim()
  if (v === 'products' || v === 'stocks' || v === 'prices') return v
  return 'products'
}

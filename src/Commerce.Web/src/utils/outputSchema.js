/**
 * Construye un JSON Schema mínimo desde items (path, type, itemType).
 * - Objeto anidado: paths con punto (address.street) → objeto anidado.
 * - Array simple: path "tags" type array + itemType string → { type: 'array', items: { type: 'string' } }.
 * - Array de objetos: path "items" type array itemType object + paths "items.id", "items.name" → array con items.object.properties.
 * @param {{ path: string, type: string, itemType?: string }[]} items
 * @returns {string|null}
 */
export function buildMinimalOutputSchemaFromPaths(items) {
  const list = (items || []).filter(x => (x.path || '').trim()).map(x => ({
    path: x.path.trim(),
    type: (x.type || 'string').toLowerCase(),
    itemType: x.type === 'array' ? (x.itemType || 'string').toLowerCase() : undefined
  }))
  if (list.length === 0) return null

  function buildNode(allItems, prefix) {
    const atThisLevel = allItems.filter(x => x.path === prefix)
    const hasPrefix = prefix !== ''
    const withPrefix = hasPrefix
      ? allItems.filter(x => x.path !== prefix && x.path.startsWith(prefix + '.'))
      : allItems.filter(x => (x.path || '').trim() !== '')
    const direct = atThisLevel[0]

    if (direct?.type === 'array') {
      const childPaths = withPrefix.map(x => ({ ...x, path: x.path.slice((prefix + '.').length) }))
      if (childPaths.length > 0) {
        return { type: 'array', items: buildNode(childPaths, '') }
      }
      return { type: 'array', items: { type: direct.itemType || 'string' } }
    }

    const props = {}
    const prefixLen = hasPrefix ? (prefix + '.').length : 0
    const byFirst = {}
    for (const x of withPrefix) {
      const rel = hasPrefix ? x.path.slice(prefixLen) : x.path
      const first = rel.split('.')[0]
      if (!byFirst[first]) byFirst[first] = []
      byFirst[first].push({ ...x, path: rel })
    }
    for (const [first] of Object.entries(byFirst)) {
      const fullPath = hasPrefix ? prefix + '.' + first : first
      props[first] = buildNode(allItems, fullPath)
    }

    if (direct && direct.type !== 'array') return { type: direct.type }
    if (Object.keys(props).length > 0) return { type: 'object', properties: props }
    if (direct) return { type: direct.type }
    return { type: 'object', properties: {} }
  }

  const rootProps = {}
  const byFirst = {}
  for (const x of list) {
    const first = x.path.split('.')[0]
    if (!byFirst[first]) byFirst[first] = []
    byFirst[first].push(x)
  }
  for (const [first] of Object.entries(byFirst)) {
    rootProps[first] = buildNode(list, first)
  }
  return Object.keys(rootProps).length > 0 ? JSON.stringify({ type: 'object', properties: rootProps }) : null
}

/**
 * @param {object} schema - parsed JSON Schema root
 * @param {string} path - dotted path e.g. "data.id"
 */
export function getSchemaAtPath(schema, path) {
  if (!schema || !path) return { type: 'string' }
  const parts = path.split('.').filter(Boolean)
  let cur = schema
  for (const p of parts) {
    const next = cur?.properties?.[p]
    if (next != null) {
      cur = next
    } else if (cur?.type === 'array' && cur?.items?.properties?.[p] != null) {
      cur = cur.items.properties[p]
    } else {
      return { type: 'string' }
    }
  }
  return cur && typeof cur === 'object' ? { ...cur, type: cur.type || 'string' } : { type: 'string' }
}

/**
 * JSON Schema reducido a las hojas seleccionadas (estructura anidada).
 * @param {string|object|null} fullSchema
 * @param {string[]} selectedPaths
 * @returns {string|null}
 */
export function buildOutputSchemaFromSelection(fullSchema, selectedPaths) {
  const pathList = (selectedPaths || []).filter(Boolean)
  if (pathList.length === 0) return null
  const schema = typeof fullSchema === 'string' ? (() => { try { return JSON.parse(fullSchema) } catch { return {} } })() : (fullSchema || {})
  const props = schema.properties || {}
  if (Object.keys(props).length === 0) return null

  function buildNode(prefix, pathList) {
    const byFirst = {}
    for (const path of pathList) {
      const relative = prefix ? path.slice(prefix.length + 1) : path
      const first = relative.split('.')[0]
      const rest = relative.split('.').slice(1).join('.')
      if (!byFirst[first]) byFirst[first] = []
      byFirst[first].push(rest)
    }
    const properties = {}
    for (const [first, restList] of Object.entries(byFirst)) {
      const fullPath = prefix ? `${prefix}.${first}` : first
      const allLeaves = restList.every(r => !r || r.trim() === '')
      const childPaths = pathList.filter(p => p === fullPath || p.startsWith(fullPath + '.'))
      if (allLeaves) {
        properties[first] = getSchemaAtPath(schema, fullPath)
      } else {
        const sourceNode = getSchemaAtPath(schema, fullPath)
        if (sourceNode?.type === 'array' && sourceNode?.items != null) {
          properties[first] = { type: 'array', items: buildNode(fullPath, childPaths) }
        } else {
          properties[first] = buildNode(fullPath, childPaths)
        }
      }
    }
    return { type: 'object', properties }
  }

  const root = buildNode('', pathList)
  return root?.properties && Object.keys(root.properties).length > 0 ? JSON.stringify(root) : null
}

/** Vista resumida de un fragmento JSON Schema (solo forma / tipos). */
export function schemaFragmentToLitePreview(node) {
  if (node == null || typeof node !== 'object') return 'any'
  const t = node.type
  if (t === 'object') {
    const props = node.properties
    if (props && typeof props === 'object' && Object.keys(props).length > 0) {
      const out = {}
      for (const [k, v] of Object.entries(props)) {
        out[k] = schemaFragmentToLitePreview(v)
      }
      return out
    }
    return {}
  }
  if (t === 'array' && node.items && typeof node.items === 'object') {
    const it = node.items
    if (it.type === 'object' && it.properties && Object.keys(it.properties).length > 0) {
      const inner = {}
      for (const [k, v] of Object.entries(it.properties)) {
        inner[k] = schemaFragmentToLitePreview(v)
      }
      return [inner]
    }
    if (it.type) return `array<${it.type}>`
    return 'array<any>'
  }
  if (t) return t
  return 'any'
}

export function parseOutputSchemaRoot(fullSchema) {
  if (fullSchema == null || fullSchema === '') return null
  try {
    return typeof fullSchema === 'string' ? JSON.parse(fullSchema) : fullSchema
  } catch {
    return null
  }
}

/**
 * Texto JSON para Preview / Expandir (misma semántica que AddActionModal).
 * @param {{
 *   fullSchema: string|object|null,
 *   selectedPaths?: string[],
 *   manualItems?: { path: string, type?: string, itemType?: string }[],
 *   previewFullSchema?: boolean,
 *   hasSelectablePaths?: boolean,
 *   emptyMessage?: string
 * }} opts
 */
export function computeOutputSelectionPreviewText(opts) {
  const {
    fullSchema,
    selectedPaths = [],
    manualItems = [],
    previewFullSchema = true,
    hasSelectablePaths = false,
    emptyMessage = 'Sin schema de respuesta o propiedades. Añade rutas manualmente o define un output schema.'
  } = opts || {}

  const parsedRoot = parseOutputSchemaRoot(fullSchema)
  let parsed = null

  if (hasSelectablePaths && parsedRoot?.properties && Object.keys(parsedRoot.properties).length > 0) {
    const paths = selectedPaths.filter(Boolean)
    if (!paths.length) return 'Selecciona al menos una propiedad.'
    const built = buildOutputSchemaFromSelection(fullSchema, paths)
    if (!built) return 'Selecciona al menos una propiedad.'
    try {
      parsed = JSON.parse(built)
    } catch {
      return built
    }
  } else {
    const manual = (manualItems || []).filter(m => (m.path || '').trim()).map(m => ({
      path: m.path.trim(),
      type: m.type || 'string',
      itemType: m.type === 'array' ? (m.itemType || 'string') : undefined
    }))
    if (manual.length > 0) {
      const built = buildMinimalOutputSchemaFromPaths(manual)
      if (!built) return 'Completa las rutas de propiedad.'
      try {
        parsed = JSON.parse(built)
      } catch {
        return built
      }
    } else if (parsedRoot && typeof parsedRoot === 'object' && Object.keys(parsedRoot).length > 0) {
      if (previewFullSchema) {
        return JSON.stringify(parsedRoot, null, 2)
      }
      return JSON.stringify(schemaFragmentToLitePreview(parsedRoot), null, 2)
    } else {
      return emptyMessage
    }
  }

  if (!parsed || typeof parsed !== 'object') return String(parsed ?? '')
  if (previewFullSchema) {
    return JSON.stringify(parsed, null, 2)
  }
  return JSON.stringify(schemaFragmentToLitePreview(parsed), null, 2)
}

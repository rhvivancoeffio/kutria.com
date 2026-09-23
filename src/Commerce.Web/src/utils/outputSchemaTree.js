/**
 * Árbol anidado para OutputSchemaReturnTree (misma semántica que el aplanado de campos OpenAPI).
 * Compartido entre AddActionModal y AdminMcpToolForm.
 */

export function finalizeTreeLeafPaths(nodes) {
  for (const n of nodes || []) {
    if (n.children?.length) {
      finalizeTreeLeafPaths(n.children)
      n.leafPaths = n.children.flatMap((c) => c.leafPaths || [])
    } else {
      n.leafPaths = n.selectable ? [n.path] : []
    }
  }
}

export function buildOutputTreeNodes(prefix, propsObj, requiredSet = new Set()) {
  const nodes = []
  for (const [name, prop] of Object.entries(propsObj || {})) {
    if (name == null) continue
    const path = prefix ? `${prefix}.${name}` : name
    const type = prop?.type || 'string'
    const nested = prop?.properties
    const isReq = requiredSet.has(name)

    if (type === 'object' && nested && typeof nested === 'object' && Object.keys(nested).length > 0) {
      const childReq = new Set(prop.required || [])
      const children = buildOutputTreeNodes(path, nested, childReq)
      nodes.push({
        key: name,
        path,
        jsonType: 'object',
        typeLabel: 'object',
        description: prop.description || '',
        required: isReq,
        children,
        selectable: false
      })
    } else if (type === 'array' && prop?.items) {
      const itemProps = prop.items?.properties
      if (itemProps && typeof itemProps === 'object' && Object.keys(itemProps).length > 0) {
        const childReq = new Set(prop.items.required || [])
        const children = buildOutputTreeNodes(path, itemProps, childReq)
        nodes.push({
          key: name,
          path,
          jsonType: 'array',
          typeLabel: 'array',
          description: prop.description || '',
          required: isReq,
          children,
          selectable: false
        })
      } else {
        const itemType = prop.items?.type || 'any'
        nodes.push({
          key: name,
          path,
          jsonType: 'array',
          typeLabel: `array<${itemType}>`,
          description: prop.description || '',
          required: isReq,
          children: [],
          selectable: true
        })
      }
    } else {
      nodes.push({
        key: name,
        path,
        jsonType: type,
        typeLabel: type,
        description: prop.description || '',
        required: isReq,
        children: [],
        selectable: true
      })
    }
  }
  return nodes
}

/**
 * @param {string|object|null|undefined} raw - JSON Schema de respuesta (objeto con properties)
 * @returns {Array} nodos raíz para OutputSchemaReturnTree
 */
export function buildOutputSchemaTreeFromResponseSchema(raw) {
  if (raw == null || raw === '') return []
  try {
    const schema = typeof raw === 'string' ? JSON.parse(raw) : raw
    const props = schema?.properties
    if (!props || typeof props !== 'object' || Object.keys(props).length === 0) return []
    const req = new Set(schema.required || [])
    const nodes = buildOutputTreeNodes('', props, req)
    finalizeTreeLeafPaths(nodes)
    return nodes
  } catch {
    return []
  }
}

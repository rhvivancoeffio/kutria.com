const PLACEHOLDER_RE = /\{\{([a-zA-Z0-9_]+)\}\}/g

function isManualArg(a) {
  return a && a.fromTemplate === false
}

/** Orden: bloques en secuencia; dentro de cada bloque, orden de aparición. */
export function extractMcpArgumentNamesOrdered(messageBlocks) {
  const ordered = []
  const seen = new Set()
  const blocks = Array.isArray(messageBlocks) ? messageBlocks : []
  for (const block of blocks) {
    const content = block?.content ?? ''
    PLACEHOLDER_RE.lastIndex = 0
    let m
    while ((m = PLACEHOLDER_RE.exec(content)) !== null) {
      const name = m[1]
      if (!seen.has(name)) {
        seen.add(name)
        ordered.push(name)
      }
    }
  }
  return ordered
}

/**
 * Reconcilia argumentos con `{{nombre}}` cerrados en los bloques.
 * No vacía la lista si hay `{{` sin cierre (edición a medias).
 * Filas con `fromTemplate: false` (botón +) se conservan si no están en el texto.
 */
export function syncMcpArgumentsFromMessageBlocks(existing, messageBlocks) {
  const blocks = Array.isArray(messageBlocks) ? messageBlocks : []
  const templateNames = extractMcpArgumentNamesOrdered(blocks)
  const combined = blocks.map((b) => b?.content ?? '').join('\n')
  if (templateNames.length === 0 && /\{\{/.test(combined)) {
    return Array.isArray(existing) ? [...existing] : []
  }

  const list = Array.isArray(existing) ? [...existing] : []
  const byName = new Map()
  for (const a of list) {
    const k = String(a?.name ?? '').trim()
    if (!k) continue
    const prev = byName.get(k)
    if (!prev || isManualArg(a)) {
      byName.set(k, { ...a })
    }
  }

  const auto = []
  for (const name of templateNames) {
    const prev = byName.get(name)
    if (prev) {
      auto.push({
        ...prev,
        name,
        fromTemplate: true
      })
    } else {
      auto.push({
        name,
        description: '',
        required: false,
        fromTemplate: true
      })
    }
  }

  const manualOrphans = list.filter((a) => {
    if (!isManualArg(a)) return false
    const k = String(a?.name ?? '').trim()
    if (!k) return true
    return !templateNames.includes(k)
  })

  return [...auto, ...manualOrphans]
}

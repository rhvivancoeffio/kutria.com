/**
 * Reglas declarativas (rules[]) alineadas con ChannelDestinationMergedPublishRules / catálogo rule-definitions.v1.
 */

function paramDefault(p) {
  if (p == null) return null
  const d = p.default !== undefined ? p.default : p.Default
  if (d !== undefined && d !== null) return d
  const vt = String(p.valueType ?? p.ValueType ?? 'string').toLowerCase()
  if (vt === 'int' || vt === 'number') return 0
  if (vt === 'bool' || vt === 'boolean') return true
  return ''
}

/** Texto visible: `label` del catálogo o la clave (`key`) si falta. */
export function ruleParameterDisplayLabel(p) {
  if (p == null) return ''
  const raw = p.label ?? p.Label
  if (raw != null && String(raw).trim() !== '') return String(raw).trim()
  const k = p.key ?? p.Key
  return k != null ? String(k) : ''
}

function defaultGroupsFromDefinition(def) {
  const groups = {}
  for (const g of def.groups || def.Groups || []) {
    const gid = String(g.id ?? g.Id ?? '').trim()
    if (!gid) continue
    const params = g.parameters ?? g.Parameters ?? []
    groups[gid] = {}
    for (const p of params) {
      const key = p.key ?? p.Key
      if (!key) continue
      groups[gid][key] = paramDefault(p)
    }
  }
  return groups
}

/** Instancia por defecto para una fila del catálogo (definition). */
export function defaultRuleInstanceFromDefinition(def) {
  const type = String(def.type ?? def.Type ?? '').trim()
  const id = String(def.id ?? def.Id ?? '').trim()
  return {
    definitionId: id || null,
    type,
    enabled: true,
    groups: defaultGroupsFromDefinition(def)
  }
}

function normalizeGroupShape(def, groupsIn) {
  const base = defaultGroupsFromDefinition(def)
  const gIn = groupsIn && typeof groupsIn === 'object' ? groupsIn : {}
  for (const gid of Object.keys(base)) {
    const src = gIn[gid] ?? gIn[gid.charAt(0).toUpperCase() + gid.slice(1)] ?? {}
    if (src && typeof src === 'object') {
      for (const k of Object.keys(base[gid])) {
        if (Object.prototype.hasOwnProperty.call(src, k)) {
          base[gid][k] = src[k]
        } else if (Object.prototype.hasOwnProperty.call(src, k.charAt(0).toUpperCase() + k.slice(1))) {
          base[gid][k] = src[k.charAt(0).toUpperCase() + k.slice(1)]
        }
      }
    }
  }
  return base
}

/** Fusiona reglas guardadas con el catálogo actual (una instancia por definición del catálogo, en orden). */
export function ensureRulesForDefinitions(definitions, existingRules) {
  const defs = Array.isArray(definitions) ? definitions : []
  const rules = Array.isArray(existingRules) ? existingRules : []
  const byType = new Map()
  for (const r of rules) {
    const t = String(r.type ?? r.Type ?? '').trim().toLowerCase()
    if (t) byType.set(t, r)
  }
  return defs.map((def) => {
    const t = String(def.type ?? def.Type ?? '').trim().toLowerCase()
    const prev = t ? byType.get(t) : null
    if (!prev || typeof prev !== 'object') {
      return defaultRuleInstanceFromDefinition(def)
    }
    const en = prev.enabled ?? prev.Enabled
    return {
      definitionId: String(prev.definitionId ?? prev.DefinitionId ?? def.id ?? def.Id ?? '').trim() || null,
      type: String(prev.type ?? prev.Type ?? def.type ?? def.Type ?? '').trim(),
      enabled: en !== false,
      groups: normalizeGroupShape(def, prev.groups ?? prev.Groups)
    }
  })
}

/** Migra dataRules legado + rules existentes hacia el array esperado por el catálogo de canal marketplace. */
export function migrateChannelRulesFromConfig(parsedRoot, marketplaceDefinitions) {
  const defs = Array.isArray(marketplaceDefinitions) ? marketplaceDefinitions : []
  const o = parsedRoot && typeof parsedRoot === 'object' ? parsedRoot : {}
  const raw = o.rules ?? o.Rules
  let fromRules = []
  if (Array.isArray(raw)) {
    fromRules = raw
  }
  const merged = ensureRulesForDefinitions(defs, fromRules)
  if (fromRules.length || !defs.length) return merged

  const dr = o.dataRules ?? o.DataRules
  if (!dr || typeof dr !== 'object') return merged

  const st = dr.stocks ?? dr.Stocks ?? {}
  const pr = dr.prices ?? dr.Prices ?? {}
  const minParsed = parseInt(String(st.minimumAvailableQuantityToPublish ?? st.MinimumAvailableQuantityToPublish ?? ''), 10)
  const minStock = Number.isFinite(minParsed) && minParsed > 0 ? minParsed : 0
  const sendSpecial = !(pr.sendSpecialPriceToMarketplace === false || pr.SendSpecialPriceToMarketplace === false)

  return defs.map((def) => {
    const inst = defaultRuleInstanceFromDefinition(def)
    if (inst.groups?.stocks) {
      inst.groups.stocks.minimumAvailableQuantityToPublish = minStock
    }
    if (inst.groups?.prices) {
      inst.groups.prices.sendSpecialPriceToMarketplace = sendSpecial
    }
    return inst
  })
}

/** Migra sourceConfig del pipeline (rules[] o dataRules en raíz) para definiciones pipeline.catalog.extract_publish. */
export function migratePipelineRulesFromConfig(parsedRoot, pipelineDefinitions) {
  const defs = Array.isArray(pipelineDefinitions) ? pipelineDefinitions : []
  const o = parsedRoot && typeof parsedRoot === 'object' ? parsedRoot : {}
  const raw = o.rules ?? o.Rules
  if (Array.isArray(raw) && raw.length) {
    return ensureRulesForDefinitions(defs, raw)
  }
  const dr = o.dataRules ?? o.DataRules
  if (dr && typeof dr === 'object') {
    const fakeRoot = { dataRules: dr }
    return migrateChannelRulesFromConfig(fakeRoot, defs)
  }
  return ensureRulesForDefinitions(defs, [])
}

/**
 * Resolves YAML template tool blueprints against GET /integrations/{id}/operations
 * (same rules as UseCaseTemplateOperationResolver on the server).
 */
export function resolveBlueprintToOperation(blueprint, operations) {
  const ops = Array.isArray(operations) ? operations : []
  const oid = blueprint?.operationId?.trim()
  if (oid) {
    const exact = ops.find(o => String(o.id ?? o.operationId ?? '').toLowerCase() === oid.toLowerCase())
    if (exact) return exact
    const prefix = `${oid}_`
    const prefixed = ops.filter(o => String(o.id ?? '').toLowerCase().startsWith(prefix.toLowerCase()))
    if (prefixed.length === 1) return prefixed[0]
  }
  const method = blueprint?.method?.trim()
  const path = blueprint?.path?.trim()
  if (method && path) {
    const m = method.toUpperCase()
    const p = normalizePath(path)
    return ops.find(o => String(o.method ?? '').toUpperCase() === m && pathsMatch(o.path, p)) ?? null
  }
  return null
}

function normalizePath(path) {
  let p = (path || '').trim()
  if (!p.startsWith('/')) p = `/${p}`
  return p
}

function pathsMatch(a, b) {
  return normalizePath(a) === normalizePath(b)
}

/**
 * Mirrors WorkflowDefinitionBuilder.BuildSingleActionWorkflow (single manual step).
 */
export function buildSingleActionWorkflow(integrationId, method, path, operationId) {
  const stepId = 'step1'
  let pathNorm = (path || '').trim()
  if (!pathNorm.startsWith('/')) pathNorm = `/${pathNorm}`
  const methodUpper = (method || 'GET').toUpperCase()
  const inputMapping = {}
  if (pathNorm.toLowerCase().includes('{id}')) inputMapping['pathParams.id'] = '{input.id}'
  if (['POST', 'PUT', 'PATCH'].includes(methodUpper)) inputMapping.body = '{input}'
  if (Object.keys(inputMapping).length === 0) inputMapping.body = '{}'

  const step = {
    id: stepId,
    type: 'action',
    integrationId,
    method: methodUpper,
    path: pathNorm,
    inputMapping,
    inputKeyMapping: null,
    nextStepId: null
  }
  const opId = operationId != null && String(operationId).length > 0 ? String(operationId) : null
  if (opId) step.operationId = opId

  return {
    steps: [step],
    entryStepId: stepId,
    outputStepIds: [stepId]
  }
}

/**
 * @param {object} template - item from use-case-templates API (tools[] with name, title, description, operationId, method, path)
 * @param {string} integrationId
 * @param {Array} operations - from getIntegrationOperations
 * @returns {{ batchItems: Array, unresolved: string[] }}
 */
export function buildBatchItemsFromTemplate(template, integrationId, operations) {
  const unresolved = []
  const batchItems = []
  for (const b of template?.tools || []) {
    const op = resolveBlueprintToOperation(b, operations)
    if (!op) {
      const label = b.operationId
        ? `${b.name} (operationId: ${b.operationId})`
        : `${b.name} (${b.method} ${b.path})`
      unresolved.push(label)
      continue
    }
    const resolvedId = op.id ?? op.operationId
    const def = buildSingleActionWorkflow(integrationId, op.method, op.path, resolvedId)
    batchItems.push({
      name: (b.name || '').trim(),
      title: (b.title || op.summary || '').trim(),
      description: (b.description || op.description || '').trim() || undefined,
      inputSchema: '{}',
      workflowDefinitionJson: def,
      integrationIds: [integrationId]
    })
  }
  return { batchItems, unresolved }
}

/**
 * True si el workflow JSON representa un flujo (varios pasos o condicionales).
 * Usado para distinguir tool tipo "Workflow" (multi-paso/canvas) de "Manual" (una acción).
 * @param {string|object} workflowDefinitionJson - JSON del workflow (string o objeto)
 * @returns {boolean}
 */
export function isFlowTool(workflowDefinitionJson) {
  try {
    const w = typeof workflowDefinitionJson === 'string'
      ? JSON.parse(workflowDefinitionJson || '{}')
      : workflowDefinitionJson
    const steps = Array.isArray(w?.steps) ? w.steps : []
    if (steps.length > 1) return true
    return steps.some(s => s?.type === 'conditional')
  } catch {
    return false
  }
}

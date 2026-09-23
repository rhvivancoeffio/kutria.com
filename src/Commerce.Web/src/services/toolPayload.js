/**
 * Single place for tool/workflow payload shape. Use for manual creation, batch, and workflow
 * so the backend always receives consistent data (no corruption from different casing or structure).
 */

/**
 * Builds the request body for the tool execute endpoint.
 * Backend expects { input: <object>, test?: boolean }. When test is true, execution does not consume credits.
 * @param {Record<string, unknown>} input - Tool arguments (keys from input schema; backend resolves case-insensitively)
 * @param {boolean} [test=false] - If true, run in test mode (no credit consumption)
 * @returns {{ input: Record<string, unknown>, test?: boolean }}
 */
export function buildExecuteRequestBody(input, test = false) {
  const body = { input: input ?? {} }
  if (test) body.test = true
  return body
}

/**
 * Normalizes workflow definition JSON before create/update so manual, batch, and flow creation
 * all send the same minimal structure. Ensures steps array, entryStepId, outputStepIds exist.
 * @param {string|object} workflow - Workflow definition (string JSON or parsed object)
 * @returns {string} JSON string suitable for workflowDefinitionJson
 */
export function normalizeWorkflowDefinitionForSubmit(workflow) {
  let obj
  try {
    obj = typeof workflow === 'string' ? JSON.parse(workflow || '{}') : (workflow ?? {})
  } catch {
    return '{}'
  }
  if (!Array.isArray(obj.steps)) obj.steps = []
  if (typeof obj.entryStepId !== 'string') obj.entryStepId = obj.steps[0]?.id ?? ''
  if (!Array.isArray(obj.outputStepIds)) obj.outputStepIds = obj.steps.length ? [obj.steps[0].id] : []
  return JSON.stringify(obj)
}

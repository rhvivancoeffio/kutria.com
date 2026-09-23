<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="fixed inset-0 z-50 flex">
        <div class="absolute inset-0 bg-black/50" />
        <div
          class="relative ml-auto w-full max-w-lg md:max-w-2xl h-full bg-white dark:bg-gray-800 shadow-xl flex flex-col animate-slide-in"
          @click.stop
        >
          <!-- Header -->
          <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Reglas del condicional</h2>
            <button
              type="button"
              @click="$emit('update:modelValue', false)"
              class="p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>

          <!-- Content -->
          <div class="flex-1 min-h-0 overflow-y-auto p-4 space-y-6">
            <!-- Ramas conectadas (true/false) -->
            <div v-if="connectedBranchesInfo.success || connectedBranchesInfo.failure" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/30">
              <div class="px-3 py-2.5 text-xs font-semibold text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/70">Ramas conectadas</div>
              <div class="p-3 flex flex-wrap gap-3">
                <div v-if="connectedBranchesInfo.success" class="flex items-center gap-2 px-3 py-2 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800">
                  <span class="text-xs font-medium text-green-800 dark:text-green-300">True</span>
                  <span class="text-gray-400">→</span>
                  <span class="text-sm text-gray-900 dark:text-white truncate max-w-[200px]" :title="connectedBranchesInfo.success.summary">{{ connectedBranchesInfo.success.summary || connectedBranchesInfo.success.id }}</span>
                </div>
                <div v-if="connectedBranchesInfo.failure" class="flex items-center gap-2 px-3 py-2 rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800">
                  <span class="text-xs font-medium text-amber-800 dark:text-amber-300">False</span>
                  <span class="text-gray-400">→</span>
                  <span class="text-sm text-gray-900 dark:text-white truncate max-w-[200px]" :title="connectedBranchesInfo.failure.summary">{{ connectedBranchesInfo.failure.summary || connectedBranchesInfo.failure.id }}</span>
                </div>
              </div>
            </div>

            <!-- Parent step info -->
            <div class="space-y-3">
              <h3 class="text-sm font-medium text-gray-900 dark:text-white">Datos del nodo anterior</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400">Las reglas evalúan el output del nodo anterior en el flujo.</p>
              <div v-if="parentStepLoading" class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden">
                <div class="px-3 py-2 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50">
                  <div class="h-4 w-48 rounded bg-gray-200 dark:bg-gray-600 animate-pulse" />
                </div>
                <div class="p-3 space-y-2 max-h-40">
                  <div
                    v-for="i in 5"
                    :key="i"
                    class="flex items-center justify-between gap-2 p-2 rounded border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30"
                  >
                    <div class="h-4 flex-1 max-w-[60%] rounded bg-gray-200 dark:bg-gray-600 animate-pulse" />
                    <div class="h-5 w-16 rounded bg-gray-200 dark:bg-gray-600 animate-pulse shrink-0" />
                  </div>
                </div>
              </div>
              <div v-else-if="!parentStep" class="py-6 text-center text-amber-600 dark:text-amber-400">
                <p class="text-sm">No hay nodo anterior (acción) conectado. Conecta una acción antes del condicional.</p>
              </div>
              <div v-else-if="outputSchemaFields.length === 0" class="py-6 text-center text-amber-600 dark:text-amber-400">
                <p class="text-sm">El nodo anterior no tiene schema de respuesta definido en el OpenAPI.</p>
              </div>
              <div v-else class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden">
                <div class="px-3 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50">
                  Output de {{ parentStep.summary || parentStep.id }}
                </div>
                <div class="p-3 space-y-2 max-h-40 overflow-y-auto">
                  <div
                    v-for="(field, idx) in outputSchemaFields"
                    :key="field.name || idx"
                    class="flex items-center justify-between gap-2 p-2 rounded border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30"
                  >
                    <span class="text-sm font-mono text-gray-900 dark:text-white">{{ field.name }}</span>
                    <span class="px-1.5 py-0.5 text-xs rounded bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300">{{ field.type }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Rules builder -->
            <div v-if="fieldOptionsForSelect.length > 0" class="space-y-3">
              <h3 class="text-sm font-medium text-gray-900 dark:text-white">Condiciones</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400">Si alguna regla se cumple, el flujo sigue por "true". Si ninguna, por "false". Puedes comparar output del nodo anterior, parámetros iniciales (input) y outputs de otros nodos.</p>
              <div class="space-y-3">
                <div
                  v-for="(rule, idx) in rules"
                  :key="idx"
                  class="flex flex-wrap items-end gap-2 p-3 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30"
                >
                  <div class="flex-1 min-w-[140px]">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Campo</label>
                    <SearchableSelect
                      v-model="rule.field"
                      :options="fieldOptionsForSelector"
                      placeholder="Seleccionar..."
                      searchPlaceholder="Buscar campo..."
                      emptyMessage="No hay campos"
                      always-show-empty-option
                      teleport
                    />
                  </div>
                  <div class="w-36 shrink-0">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Operador</label>
                    <select
                      v-model="rule.operator"
                      class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
                    >
                      <option value="equals">igual a</option>
                      <option value="notEquals">distinto de</option>
                      <option value="greaterThan">mayor que</option>
                      <option value="lessThan">menor que</option>
                      <option value="greaterOrEqual">≥</option>
                      <option value="lessOrEqual">≤</option>
                      <option value="contains">contiene</option>
                      <option value="isEmpty">está vacío</option>
                      <option value="isNotEmpty">no está vacío</option>
                    </select>
                  </div>
                  <template v-if="!['isEmpty', 'isNotEmpty'].includes(rule.operator)">
                    <div class="flex-1 min-w-[120px] flex gap-2 items-center">
                      <label class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap shrink-0">Valor:</label>
                      <template v-if="rule.value === '__literal__'">
                        <div class="flex-1 min-w-0 flex items-center gap-2">
                          <input
                            v-model="rule.valueLiteral"
                            type="text"
                            :placeholder="valuePlaceholder(rule)"
                            class="flex-1 min-w-0 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
                          />
                          <button
                            type="button"
                            @click="rule.value = ''"
                            class="text-xs text-primary-600 dark:text-primary-400 hover:underline whitespace-nowrap shrink-0"
                          >
                            Usar referencia
                          </button>
                        </div>
                      </template>
                      <SearchableSelect
                        v-else
                        v-model="rule.value"
                        :options="valueOptionsForSelector"
                        placeholder="Valor o referencia..."
                        searchPlaceholder="Buscar..."
                        emptyMessage="No hay opciones"
                        always-show-empty-option
                        teleport
                        class="flex-1 min-w-0"
                      />
                    </div>
                  </template>
                  <button
                    type="button"
                    @click="removeRule(idx)"
                    class="p-2 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg shrink-0"
                    title="Eliminar regla"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                  </button>
                </div>
                <button
                  type="button"
                  @click="addRule"
                  class="w-full py-2 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-500 hover:text-primary-600 dark:hover:border-primary-400 dark:hover:text-primary-400 text-sm font-medium transition-colors"
                >
                  + Añadir condición
                </button>
              </div>
            </div>
          </div>

          <!-- Footer -->
          <div class="shrink-0 px-4 py-3 border-t border-gray-200 dark:border-gray-600 flex justify-end gap-3">
            <button
              type="button"
              @click="$emit('update:modelValue', false)"
              class="px-4 py-2 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              Cancelar
            </button>
            <button
              type="button"
              @click="handleSave"
              :disabled="!parentStep"
              :class="['px-4 py-2 rounded-lg font-medium transition-colors', parentStep ? 'bg-green-600 hover:bg-green-700 text-white' : 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed']"
            >
              Guardar
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import apiService from '../../services/api'
import SearchableSelect from '../SearchableSelect.vue'

const PREFIX_PARENT = 'parentOutput:'
const PREFIX_INPUT = 'input:'
const PREFIX_OUTPUT = 'output:'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  conditionalStep: { type: Object, default: null },
  workflowJson: { type: String, default: '{}' },
  inputSchemaJson: { type: String, default: '{}' },
  integrations: { type: Array, default: () => [] }
})

const emit = defineEmits(['update:modelValue', 'save'])

const parentStep = ref(null)
const parentStepLoading = ref(false)
const parentOperation = ref(null)
const upstreamOperations = ref({}) // stepId -> operation (response schema)
const rules = ref([])

/** Info de los nodos de acción conectados a las ramas true/false del condicional */
const connectedBranchesInfo = computed(() => {
  const step = props.conditionalStep?.step
  const branches = step?.branches
  if (!branches || !props.workflowJson) return { success: null, failure: null }
  let def
  try {
    def = JSON.parse(props.workflowJson || '{}')
  } catch {
    return { success: null, failure: null }
  }
  const stepMap = new Map((def.steps || []).map(s => [s.id, s]))
  const successId = branches.success || branches.true
  const failureId = branches.failure || branches.false
  const successStep = successId ? stepMap.get(successId) : null
  const failureStep = failureId ? stepMap.get(failureId) : null
  return {
    success: successStep ? { id: successStep.id, summary: successStep.summary || successStep.displayName } : null,
    failure: failureStep ? { id: failureStep.id, summary: failureStep.summary || failureStep.displayName } : null
  }
})

function flattenSchemaFromProps(propsObj) {
  const fields = []
  if (!propsObj || typeof propsObj !== 'object') return fields
  function flatten(prefix, obj) {
      if (!obj || typeof obj !== 'object') return
      for (const [name, prop] of Object.entries(obj)) {
        const path = prefix ? `${prefix}.${name}` : name
        const type = prop?.type || 'string'
        const nested = prop?.properties
        const hasNestedObject = nested && typeof nested === 'object' && Object.keys(nested).length > 0
        const itemsSchema = prop?.items
        const itemsProps = itemsSchema?.properties
        const hasArrayOfObjects = type === 'array' && itemsProps && Object.keys(itemsProps).length > 0

        if (hasArrayOfObjects) {
          for (const [subName, subProp] of Object.entries(itemsProps)) {
            const subPath = `${path}[].${subName}`
            const subType = subProp?.type || 'string'
            const subNested = subProp?.properties
            if (subNested && typeof subNested === 'object' && Object.keys(subNested).length > 0) {
              flatten(`${subPath}.`, subNested)
            } else {
              let displayType = subType
              if (subProp?.items) displayType = `array of ${subProp.items?.type || 'any'}`
              fields.push({ name: subPath, type: displayType })
            }
          }
        } else if (hasNestedObject) {
          flatten(path, nested)
        } else {
          let displayType = type
          if (type === 'array' && prop?.items) {
            const itemType = prop.items?.type || 'any'
            displayType = `array of ${itemType}`
          }
          fields.push({ name: path, type: displayType })
        }
      }
    }
  flatten('', propsObj)
  return fields
}

const outputSchemaFields = computed(() => {
  if (!parentOperation.value?.responseBody?.schema) return []
  try {
    const schema = typeof parentOperation.value.responseBody.schema === 'string'
      ? JSON.parse(parentOperation.value.responseBody.schema)
      : parentOperation.value.responseBody.schema
    return flattenSchemaFromProps(schema.properties || {})
  } catch {
    return []
  }
})

const inputParamsFields = computed(() => {
  try {
    const schema = typeof props.inputSchemaJson === 'string' ? JSON.parse(props.inputSchemaJson || '{}') : props.inputSchemaJson
    return flattenSchemaFromProps(schema.properties || {}).map(f => ({ ...f, name: f.name }))
  } catch {
    return []
  }
})

function getUpstreamStepIds() {
  const conditionalStepId = props.conditionalStep?.stepId
  const parentNodeId = props.conditionalStep?.parentNodeId
  if (!conditionalStepId || !props.workflowJson) return []
  let def
  try {
    def = JSON.parse(props.workflowJson || '{}')
  } catch {
    return []
  }
  const steps = def.steps || []
  const stepMap = new Map(steps.map(s => [s.id, s]))
  const entryStepIds = def.entryStepIds || (def.entryStepId ? [def.entryStepId] : [])
  function getChildren(stepId) {
    if (!stepId) return []
    const s = stepMap.get(stepId)
    if (!s) return []
    if (s.type === 'conditional') return [s.branches?.success, s.branches?.failure].filter(Boolean)
    return Object.values(s.outputs || {}).filter(Boolean)
  }
  const visited = new Set()
  const queue = [...entryStepIds]
  while (queue.length) {
    const id = queue.shift()
    if (!id || visited.has(id)) continue
    if (id === conditionalStepId) continue
    visited.add(id)
    queue.push(...getChildren(id))
  }
  return steps.filter(s => s.type === 'action' && visited.has(s.id) && s.integrationId && s.operationId).map(s => s.id)
}

const upstreamStepList = computed(() => {
  const ids = getUpstreamStepIds()
  if (!props.workflowJson) return []
  let def
  try {
    def = JSON.parse(props.workflowJson || '{}')
  } catch {
    return []
  }
  const steps = (def.steps || []).filter(s => ids.includes(s.id))
  return steps.map(s => ({ id: s.id, label: s.displayName || s.summary || s.id }))
})

function upstreamFieldsForStep(stepId) {
  const op = upstreamOperations.value[stepId]
  if (!op?.responseBody?.schema) return []
  try {
    const schema = typeof op.responseBody.schema === 'string' ? JSON.parse(op.responseBody.schema) : op.responseBody.schema
    return flattenSchemaFromProps(schema.properties || {})
  } catch {
    return []
  }
}

const fieldOptionsForSelect = computed(() => {
  const options = []
  if (outputSchemaFields.value.length > 0 && parentStep.value) {
    options.push({ group: `Output: ${parentStep.value.displayName || parentStep.value.summary || parentStep.value.id}`, items: outputSchemaFields.value.map(f => ({ value: PREFIX_PARENT + f.name, label: f.name })) })
  }
  if (inputParamsFields.value.length > 0) {
    options.push({ group: 'Parámetros iniciales (input)', items: inputParamsFields.value.map(f => ({ value: PREFIX_INPUT + f.name, label: `input.${f.name}` })) })
  }
  for (const s of upstreamStepList.value) {
    if (s.id === parentStep.value?.id) continue
    const fields = upstreamFieldsForStep(s.id)
    if (fields.length > 0) {
      options.push({ group: `Output: ${s.label}`, items: fields.map(f => ({ value: PREFIX_OUTPUT + s.id + ':' + f.name, label: f.name })) })
    }
  }
  return options
})

const allFieldOptionsFlat = computed(() => {
  const flat = []
  for (const g of fieldOptionsForSelect.value) {
    for (const o of g.items) flat.push(o)
  }
  return flat
})

/** Commerceiones planas con grupo en la etiqueta para SearchableSelect (Campo). */
const fieldOptionsForSelector = computed(() => {
  const list = [{ value: '', label: 'Seleccionar...' }]
  for (const g of fieldOptionsForSelect.value) {
    for (const o of g.items) {
      list.push({ value: o.value, label: `${g.group} › ${o.label}` })
    }
  }
  return list
})

/** Commerceiones para Valor unificado: literal + referencias. */
const valueOptionsForSelector = computed(() => {
  const list = [{ value: '', label: 'Seleccionar...' }, { value: '__literal__', label: '✎ Valor literal' }]
  for (const g of fieldOptionsForSelect.value) {
    for (const o of g.items) {
      list.push({ value: o.value, label: `${g.group} › ${o.label}` })
    }
  }
  return list
})

function valuePlaceholder(rule) {
  const fieldKey = rule.field || ''
  const opt = allFieldOptionsFlat.value.find(o => o.value === fieldKey)
  const field = opt ? { type: '' } : outputSchemaFields.value.find(f => (PREFIX_PARENT + f.name) === fieldKey)
  const t = (field?.type || '').toLowerCase()
  if (t.includes('number') || t.includes('integer')) return '0'
  if (t.includes('boolean')) return 'true'
  return 'Escribir valor...'
}

function addRule() {
  rules.value.push({ field: '', operator: 'equals', value: '', valueLiteral: '' })
}

function removeRule(idx) {
  rules.value.splice(idx, 1)
}

function fieldKeyToExpressionPath(key) {
  if (!key || typeof key !== 'string') return null
  const k = key.trim()
  if (k.startsWith(PREFIX_INPUT)) return 'input.' + k.slice(PREFIX_INPUT.length)
  if (k.startsWith(PREFIX_OUTPUT)) {
    const rest = k.slice(PREFIX_OUTPUT.length)
    const colon = rest.indexOf(':')
    if (colon === -1) return null
    const stepId = rest.slice(0, colon).replace(/-/g, '_')
    const path = rest.slice(colon + 1)
    return `output_${stepId}.${path}`
  }
  if (k.startsWith(PREFIX_PARENT) || !k.includes(':')) {
    const path = k.startsWith(PREFIX_PARENT) ? k.slice(PREFIX_PARENT.length) : k
    return `input1.${path}`
  }
  return null
}

function buildCSharpExpression(rule) {
  const fieldKey = rule.field?.trim()
  if (!fieldKey) return null
  const lhs = fieldKeyToExpressionPath(fieldKey)
  if (!lhs) return null
  const op = rule.operator || 'equals'

  if (op === 'isEmpty') return `string.IsNullOrEmpty(${lhs}?.ToString())`
  if (op === 'isNotEmpty') return `!string.IsNullOrEmpty(${lhs}?.ToString())`

  let rhs
  const isLiteral = rule.value === '__literal__'
  const literalVal = (rule.valueLiteral ?? '').trim()
  const refKey = !isLiteral && (rule.value ?? '').trim()
  if (refKey) {
    const refPath = fieldKeyToExpressionPath(refKey)
    rhs = refPath || 'null'
  } else {
    const val = isLiteral ? literalVal : ''
    const isNumeric = /^-?\d+(\.\d+)?$/.test(val)
    const isBool = val.toLowerCase() === 'true' || val.toLowerCase() === 'false'
    const needsQuote = ['equals', 'notEquals', 'contains'].includes(op) && !isNumeric && !isBool
    rhs = needsQuote ? `"${String(val).replace(/"/g, '\\"')}"` : val
  }

  switch (op) {
    case 'equals': return `${lhs} == ${rhs}`
    case 'notEquals': return `${lhs} != ${rhs}`
    case 'greaterThan': return `${lhs} > ${rhs}`
    case 'lessThan': return `${lhs} < ${rhs}`
    case 'greaterOrEqual': return `${lhs} >= ${rhs}`
    case 'lessOrEqual': return `${lhs} <= ${rhs}`
    case 'contains': return `${lhs} != null && ${lhs}.ToString().Contains(${rhs})`
    default: return `${lhs} == ${rhs}`
  }
}

function buildRulesJson() {
  const validRules = rules.value.filter(r => {
    if (!r.field?.trim()) return false
    if (['isEmpty', 'isNotEmpty'].includes(r.operator)) return true
    if (r.value === '__literal__') return (r.valueLiteral ?? '').trim() !== ''
    return (r.value ?? '').trim() !== ''
  })
  if (validRules.length === 0) return '[]'

  const expressions = validRules.map(r => buildCSharpExpression(r)).filter(Boolean)
  if (expressions.length === 0) return '[]'

  const workflow = {
    WorkflowName: 'Conditional',
    Rules: expressions.map((expr, i) => ({
      RuleName: `Rule${i + 1}`,
      Expression: expr
    }))
  }
  return JSON.stringify([workflow], null, 2)
}

async function loadParentSchema() {
  const parentNodeId = props.conditionalStep?.parentNodeId
  if (!parentNodeId || !props.workflowJson) {
    parentStep.value = null
    parentOperation.value = null
    upstreamOperations.value = {}
    return
  }
  let def
  try {
    def = JSON.parse(props.workflowJson || '{}')
  } catch {
    parentStep.value = null
    parentOperation.value = null
    upstreamOperations.value = {}
    return
  }
  const steps = def.steps || []
  const step = steps.find(s => s.id === parentNodeId)
  if (!step || step.type !== 'action') {
    parentStep.value = step || null
    parentOperation.value = null
    upstreamOperations.value = {}
    return
  }
  parentStep.value = step
  parentStepLoading.value = true
  const upstreamIds = getUpstreamStepIds().filter(id => id !== parentNodeId)
  const toLoad = [parentNodeId, ...upstreamIds]
  const stepsToLoad = toLoad.map(id => steps.find(s => s.id === id)).filter(s => s && s.integrationId && s.operationId)
  const loaded = {}
  try {
    await Promise.all(stepsToLoad.map(async (s) => {
      try {
        const result = await apiService.getIntegrationOperations(s.integrationId)
        const ops = result.operations || []
        const op = ops.find(o => o.id === s.operationId) || null
        if (op) loaded[s.id] = op
      } catch {
        // ignore per-step
      }
    }))
    parentOperation.value = loaded[parentNodeId] || null
    const upstream = { ...loaded }
    delete upstream[parentNodeId]
    upstreamOperations.value = upstream
  } catch {
    parentOperation.value = null
    upstreamOperations.value = {}
  } finally {
    parentStepLoading.value = false
  }
}

function parseExpressionToFieldKey(exprPath) {
  if (!exprPath) return ''
  const p = exprPath.trim()
  if (p.startsWith('input1.')) return PREFIX_PARENT + p.slice(7)
  if (p.startsWith('input.')) return PREFIX_INPUT + p.slice(6)
  const outMatch = p.match(/^output_([^.]+)\.(.+)$/)
  if (outMatch) return PREFIX_OUTPUT + outMatch[1].replace(/_/g, '-') + ':' + outMatch[2]
  return p ? PREFIX_PARENT + p : ''
}

function loadRulesFromStep() {
  const step = props.conditionalStep?.step
  if (!step?.rulesJson) {
    rules.value = [{ field: '', operator: 'equals', value: '', valueLiteral: '' }]
    return
  }
  try {
    const arr = JSON.parse(step.rulesJson || '[]')
    const wf = Array.isArray(arr) ? arr[0] : null
    const ruleList = wf?.Rules || []
    if (ruleList.length === 0) {
      rules.value = [{ field: '', operator: 'equals', value: '', valueLiteral: '' }]
      return
    }
    rules.value = ruleList.map(r => {
      const expr = r.Expression || ''
      const isEmpty = expr.includes('IsNullOrEmpty')
      const isNotEmpty = expr.includes('!string.IsNullOrEmpty')
      if (isEmpty || isNotEmpty) {
        const pathMatch = expr.match(/((?:input1|input|output_[a-zA-Z0-9_-]+)(?:\.[^\s?]+)?)/)
        const fullPath = pathMatch ? pathMatch[1].replace(/\?\.ToString\(\)|\.ToString\(\)/g, '').trim() : ''
        const fieldKey = parseExpressionToFieldKey(fullPath)
        return { field: fieldKey || '', operator: isEmpty ? 'isEmpty' : 'isNotEmpty', value: '', valueLiteral: '' }
      }
      const binaryMatch = expr.match(/(.+?)\s*(==|!=|>|<|>=|<=)\s*(.+)/)
      if (binaryMatch) {
        const [, left, op, right] = binaryMatch
        const leftPath = left?.trim()
        const rightVal = right?.trim()
        const opMap = { '==': 'equals', '!=': 'notEquals', '>': 'greaterThan', '<': 'lessThan', '>=': 'greaterOrEqual', '<=': 'lessOrEqual' }
        const fieldKey = parseExpressionToFieldKey(leftPath)
        const isRef = /^(input1|input|output_[^.]+)\.[^.]+/.test(rightVal) && !/^".*"$/.test(rightVal)
        if (isRef) {
          const refKey = parseExpressionToFieldKey(rightVal)
          return { field: fieldKey, operator: opMap[op] || 'equals', value: refKey, valueLiteral: '' }
        }
        const literalVal = rightVal.replace(/^"|"$/g, '').replace(/\\"/g, '"').trim()
        return { field: fieldKey, operator: opMap[op] || 'equals', value: '__literal__', valueLiteral: literalVal }
      }
      return { field: '', operator: 'equals', value: '', valueLiteral: '' }
    }).map(r => ({ field: r.field ?? '', operator: r.operator ?? 'equals', value: r.value ?? '', valueLiteral: r.valueLiteral ?? '' })).filter(r => r.field || r.operator)
    if (rules.value.length === 0) rules.value = [{ field: '', operator: 'equals', value: '', valueLiteral: '' }]
  } catch {
    rules.value = [{ field: '', operator: 'equals', value: '', valueLiteral: '' }]
  }
}

function closeModal() {
  emit('update:modelValue', false)
}

function handleSave() {
  if (!parentStep.value) return
  const rulesJson = buildRulesJson()
  const inputKey = parentStep.value.id
  emit('save', { stepId: props.conditionalStep?.stepId, rulesJson, inputKey })
  closeModal()
}

watch(() => [props.modelValue, props.conditionalStep?.parentNodeId, props.workflowJson], () => {
  if (props.modelValue && props.conditionalStep) {
    loadParentSchema()
    loadRulesFromStep()
  }
}, { immediate: true })
</script>

<style scoped>
.animate-slide-in {
  animation: slideIn 0.25s ease-out;
}
@keyframes slideIn {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}
.modal-enter-active, .modal-leave-active {
  transition: opacity 0.2s;
}
.modal-enter-from, .modal-leave-to {
  opacity: 0;
}
</style>

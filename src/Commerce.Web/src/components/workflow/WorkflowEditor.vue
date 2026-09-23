<template>
  <div
    class="workflow-editor flex flex-col rounded-lg border border-gray-300 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800"
    :style="editorHeightStyle"
  >
    <div class="flex-1 min-h-0 relative workflow-canvas">
      <VueFlow
        ref="vueFlowRef"
        v-model:nodes="nodes"
        v-model:edges="edges"
        :node-types="nodeTypes"
        :default-edge-options="{ markerEnd: { type: 'arrowclosed', color: '#9ca3af' } }"
        :default-viewport="{ x: 0, y: 0, zoom: 1 }"
        :min-zoom="0.2"
        :max-zoom="4"
        :zoom-on-double-click="false"
        :connect-on-click="false"
        fit-view-on-init
        class="workflow-vueflow"
        @connect="onConnect"
        @connect-start="onConnectStart"
        @connect-end="onConnectEnd"
        @node-click="onNodeClick"
        @node-double-click="onNodeDoubleClick"
        @pane-click="onPaneClick"
        @edge-click="onEdgeClick"
        @init="onFlowInit"
        @nodes-change="onNodesChange"
        @edges-change="onEdgesChange"
        :is-valid-connection="isValidConnection"
      >
        <WorkflowControls />
        <Panel position="top-right" class="workflow-add-nodes-panel">
          <button
            type="button"
            class="workflow-add-nodes__btn workflow-add-nodes__btn--icon"
            title="Exportar workflow como JSON (solo en memoria)"
            @click="exportWorkflow"
          >
            <svg class="workflow-add-nodes__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="7 10 12 15 17 10" />
              <line x1="12" y1="15" x2="12" y2="3" />
            </svg>
          </button>
          <button
            type="button"
            class="workflow-add-nodes__btn workflow-add-nodes__btn--icon"
            title="Importar workflow desde JSON (solo en memoria)"
            @click="showImportModal = true"
          >
            <svg class="workflow-add-nodes__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
              <polyline points="17 8 12 3 7 8" />
              <line x1="12" y1="3" x2="12" y2="15" />
            </svg>
          </button>
          <button
            type="button"
            class="workflow-add-nodes__btn workflow-add-nodes__btn--icon"
            title="Añadir nodo de acción al canvas (conectar manualmente)"
            @click="addLooseAction"
          >
            <svg class="workflow-add-nodes__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M9 11l3 3L22 4" />
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
            </svg>
          </button>
          <button
            type="button"
            class="workflow-add-nodes__btn workflow-add-nodes__btn--icon"
            title="Añadir nodo condicional al canvas (conectar manualmente)"
            @click="addLooseConditional"
          >
            <svg class="workflow-add-nodes__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 2v8" />
              <path d="M8 14l4 4 4-4" />
              <path d="M6 22h12" />
              <path d="M12 10a2 2 0 1 0 0-4 2 2 0 0 0 0 4z" />
            </svg>
          </button>
          <button
            v-if="showRemoveDuplicatesButton"
            type="button"
            class="workflow-add-nodes__btn workflow-add-nodes__btn--text text-amber-600 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-900/20"
            :title="hasDuplicateActions ? 'Eliminar acciones duplicadas (mantiene una por integración+operación)' : 'Quitar acciones duplicadas si las hay'"
            @click="removeDuplicateActions"
          >
            Eliminar duplicados
          </button>
          <button
            v-if="showExecuteInCanvas"
            type="button"
            class="workflow-add-nodes__btn workflow-add-nodes__btn--icon text-green-600 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20"
            title="Ejecutar tool"
            @click="emit('execute-request')"
          >
            <svg class="workflow-add-nodes__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
              <path d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </button>
        </Panel>
      </VueFlow>
      <Teleport to="body">
        <div
          v-if="dropMenuVisible"
          data-drop-menu
          class="fixed z-[9999] py-1 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 shadow-xl min-w-[160px]"
          :style="{ left: `${dropMenuX}px`, top: `${dropMenuY}px` }"
        >
          <template v-if="dropMenuSourceIsConditional">
            <button
              type="button"
              class="w-full px-4 py-2 text-left text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="edges.some(e => e.source === menuSourceId && e.sourceHandle === 'success')"
              title="Añadir paso en la rama verdadera"
              @click="onMenuChoice('success')"
            >
              <span class="w-2 h-2 rounded-full bg-green-500" />
              True
            </button>
            <button
              type="button"
              class="w-full px-4 py-2 text-left text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="edges.some(e => e.source === menuSourceId && e.sourceHandle === 'failure')"
              title="Añadir paso en la rama falsa"
              @click="onMenuChoice('failure')"
            >
              <span class="w-2 h-2 rounded-full bg-red-500" />
              False
            </button>
          </template>
          <template v-else>
            <button
              type="button"
              class="w-full px-4 py-2 text-left text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
              @click="onMenuChoice('newAction')"
            >
              <span class="w-2 h-2 rounded-full bg-primary-500" />
              New Action
            </button>
            <button
              type="button"
              class="w-full px-4 py-2 text-left text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2"
              @click="onMenuChoice('newConditional')"
            >
              <span class="w-2 h-2 rounded-full bg-amber-500" />
              Conditional
            </button>
          </template>
        </div>
      </Teleport>
      <ImportWorkflowModal
        v-model="showImportModal"
        @import="applyImportedWorkflow"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, shallowRef, nextTick, provide, markRaw } from 'vue'
import { VueFlow, Panel } from '@vue-flow/core'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import StartNode from './StartNode.vue'
import ActionNode from './ActionNode.vue'
import ConditionalNode from './ConditionalNode.vue'
import WorkflowControls from './WorkflowControls.vue'
import ImportWorkflowModal from './ImportWorkflowModal.vue'

const props = defineProps({
  modelValue: { type: String, default: '{}' },
  addActionMode: { type: String, default: 'inline' },
  workflowKey: { type: String, default: '' },
  minHeight: { type: String, default: '480px' },
  integrations: { type: Array, default: () => [] },
  /** Si true y el workflow está vacío, al montar se carga la definición de prueba (Start + 3 actions + 1 conditional por action). */
  loadTestWorkflow: { type: Boolean, default: false },
  /** Si true, muestra el botón Ejecutar en el panel del canvas (vista edición workflow). */
  showExecuteInCanvas: { type: Boolean, default: false }
})

const editorHeightStyle = computed(() => ({ minHeight: props.minHeight, height: props.minHeight }))
const emit = defineEmits(['update:modelValue', 'add-action-request', 'edit-action-request', 'edit-conditional-request', 'edit-start-input-request', 'set-output-step', 'execute-request'])

// --- Constants ---
const NODE_SPACING_X = 240
const NODE_SPACING_Y = 140
const START_NODE_ID = 'start'
const START_NODE_X = 80
const START_NODE_Y = 250
const LAYOUT_BASE_Y = 40
const OUTPUT_HANDLES = ['out-1', 'out-2', 'out-3']

// --- Single source of truth ---
const nodes = ref([])
const edges = ref([])
const vueFlowRef = ref(null)
const vueFlowStore = shallowRef(null)
const lastEmittedJson = ref('')
const isProgrammaticUpdate = ref(false)

const showImportModal = ref(false)

// --- Menu state ---
const dropMenuVisible = ref(false)
const dropMenuX = ref(0)
const dropMenuY = ref(0)
const dropMenuOpenAt = ref(0)
const menuSourceId = ref(null)
const menuSourceHandle = ref(null)
const dropMenuSourceIsConditional = computed(() => {
  const id = menuSourceId.value
  return id ? (nodes.value.find(n => n.id === id)?.data?.step?.type === 'conditional') : false
})

// --- Connect-drag state ---
const connectingFromNodeId = ref(null)
const connectingFromHandle = ref(null)
const didConnect = ref(false)

// --- Selected node (para borde de selección) ---
const selectedNodeId = ref(null)

// --- Helpers ---
function defaultDef() {
  return {
    steps: [],
    entryStepId: '',
    entryStepIds: [],
    outputStepIds: [],
    outputMapping: undefined,
    nodePositions: {}
  }
}

function parseWorkflow(json) {
  try {
    const d = JSON.parse(json || '{}')
    const entryStepIds = Array.isArray(d.entryStepIds) ? d.entryStepIds : (d.entryStepId ? [d.entryStepId] : [])
    return {
      steps: Array.isArray(d.steps) ? d.steps : [],
      entryStepId: d.entryStepId || entryStepIds[0] || '',
      entryStepIds,
      outputStepIds: Array.isArray(d.outputStepIds) ? d.outputStepIds : [],
      outputMapping: d.outputMapping && typeof d.outputMapping === 'object' ? d.outputMapping : undefined,
      nodePositions: (d.nodePositions && typeof d.nodePositions === 'object') ? d.nodePositions : {}
    }
  } catch {
    return defaultDef()
  }
}

function generateNodeId() {
  let d = Date.now()
  if (typeof performance !== 'undefined' && typeof performance.now === 'function') d += performance.now()
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (d + Math.random() * 16) % 16 | 0
    d = Math.floor(d / 16)
    return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16)
  })
}

function defaultActionStep(id) {
  return {
    id,
    type: 'action',
    integrationId: null,
    method: 'GET',
    path: '/',
    inputMapping: {},
    inputKeyMapping: {},
    outputHandles: [...OUTPUT_HANDLES],
    outputs: {}
  }
}

// --- Sync to Vue Flow store (single place) ---
function syncToStore() {
  const store = vueFlowStore.value
  if (!store) return
  nextTick(() => {
    // Apply nodes and edges in one go so createGraphEdges sees all nodes when validating edges.
    const nodeList = [...nodes.value]
    const edgeList = [...edges.value]
    console.log('[syncToStore]', { nodes: nodeList.length, edges: edgeList.length, edgeIds: edgeList.map(e => e.id) })
    store.setElements([...nodeList, ...edgeList])
  })
}

// --- Layout by levels (DAG: level = distance from Start) ---
function computeLayoutByLevels(def) {
  const steps = def.steps || []
  const entryStepIds = def.entryStepIds?.length ? def.entryStepIds : (def.entryStepId ? [def.entryStepId] : [])
  const stepMap = new Map(steps.map(s => [s.id, s]))

  function getChildren(nodeId) {
    if (nodeId === START_NODE_ID) return entryStepIds.filter(Boolean)
    const s = stepMap.get(nodeId)
    if (!s) return []
    if (s.type === 'conditional') return [s.branches?.success, s.branches?.failure].filter(Boolean)
    return Object.values(s.outputs || {}).filter(Boolean)
  }

  const levelOf = { [START_NODE_ID]: 0 }
  const queue = [START_NODE_ID]
  let head = 0
  while (head < queue.length) {
    const nodeId = queue[head++]
    for (const childId of getChildren(nodeId)) {
      if (levelOf[childId] == null) {
        levelOf[childId] = levelOf[nodeId] + 1
        queue.push(childId)
      }
    }
  }

  const levels = []
  for (const nodeId of queue) {
    const l = levelOf[nodeId]
    if (!levels[l]) levels[l] = []
    levels[l].push(nodeId)
  }
  const reached = new Set(queue)
  for (const s of steps) {
    if (!reached.has(s.id)) {
      const last = Math.max(0, levels.length - 1)
      if (!levels[last]) levels[last] = []
      levels[last].push(s.id)
    }
  }

  const nodePositions = {}
  for (let L = 0; L < levels.length; L++) {
    levels[L].forEach((id, i) => {
      nodePositions[id] = {
        x: START_NODE_X + L * NODE_SPACING_X,
        y: LAYOUT_BASE_Y + i * NODE_SPACING_Y
      }
    })
  }
  return nodePositions
}

// --- Load graph from definition ---
function loadFromDef(def) {
  const steps = def.steps || []
  const savedPositions = def.nodePositions || {}
  const layoutPositions = computeLayoutByLevels(def)
  const n = []
  const e = []
  const startPos = savedPositions[START_NODE_ID]?.x != null && savedPositions[START_NODE_ID]?.y != null
    ? savedPositions[START_NODE_ID]
    : (layoutPositions[START_NODE_ID] ?? { x: START_NODE_X, y: START_NODE_Y })
  n.push({
    id: START_NODE_ID,
    type: 'start',
    position: startPos,
    data: {},
    class: 'start-node-wrapper',
    draggable: true
  })
  const stepPositions = {}
  for (const s of steps) {
    const id = s.id || generateNodeId()
    const pos = savedPositions[id]?.x != null && savedPositions[id]?.y != null
      ? savedPositions[id]
      : (layoutPositions[id] ?? { x: START_NODE_X + NODE_SPACING_X, y: START_NODE_Y })
    stepPositions[id] = pos
  }
  const outIds = new Set((def.outputStepIds || []).map(String))
  const integrations = props.integrations || []
  const entryStepIds = def.entryStepIds?.length ? def.entryStepIds : (def.entryStepId ? [def.entryStepId] : [])

  for (const entryId of entryStepIds) {
    if (steps.some(s => (s.id || '') === entryId)) {
      e.push({ id: `e-${START_NODE_ID}-${entryId}`, source: START_NODE_ID, target: entryId, sourceHandle: 'start', targetHandle: 'target' })
    }
  }
  for (const s of steps) {
    const id = s.id || generateNodeId()
    const pos = stepPositions[id] ?? layoutPositions[id] ?? { x: START_NODE_X + NODE_SPACING_X, y: START_NODE_Y }
    const nodeType = s.type === 'conditional' ? 'conditional' : 'action'
    let stepData = { ...s }
    if (nodeType === 'action' && s.integrationId && integrations.length > 0) {
      const int = integrations.find(i => i.id === s.integrationId)
      if (int) stepData = { ...stepData, integrationLogoUrl: s.integrationLogoUrl || int.logoUrl || int.LogoUrl, integrationName: s.integrationName || int.name }
    }
    n.push({
      id,
      type: nodeType,
      position: pos,
      data: {
        label: s.displayName || (s.type === 'conditional' ? 'Condicional' : (s.summary || `${s.method || 'GET'} ${(s.path || '').replace(/^\/+/, '')}` || `Acción: ${id}`)),
        step: stepData,
        isOutputStep: nodeType === 'action' && outIds.has(String(id))
      }
    })
    if (s.type === 'conditional') {
      const branches = s.branches || { success: '', failure: '' }
      if (branches.success) e.push({ id: `e-${id}-success-${branches.success}`, source: id, target: branches.success, sourceHandle: 'success', targetHandle: 'target', label: 'true' })
      if (branches.failure) e.push({ id: `e-${id}-failure-${branches.failure}`, source: id, target: branches.failure, sourceHandle: 'failure', targetHandle: 'target', label: 'false' })
    } else {
      const outputHandles = Array.isArray(s.outputHandles) && s.outputHandles.length > 0 ? s.outputHandles : OUTPUT_HANDLES
      if (s.outputs && typeof s.outputs === 'object') {
        for (const handleId of outputHandles) {
          const tid = s.outputs[handleId]
          if (tid) e.push({ id: `e-${id}-${handleId}-${tid}`, source: id, target: tid, sourceHandle: handleId, targetHandle: 'target' })
        }
      } else {
        const nextIds = Array.isArray(s.nextStepIds) && s.nextStepIds.length > 0 ? s.nextStepIds : (s.nextStepId ? [s.nextStepId] : [])
        nextIds.filter(Boolean).forEach((tid, i) => {
          const handleId = outputHandles[i] || `out-${i + 1}`
          e.push({ id: `e-${id}-${handleId}-${tid}`, source: id, target: tid, sourceHandle: handleId, targetHandle: 'target' })
        })
      }
    }
  }
  nodes.value = n
  edges.value = e
  syncToStore()
}

// --- Serialize graph to definition (single format: outputs for actions) ---
function getNextIds(s, stepMap) {
  if (!s) return []
  if (s.type === 'conditional') return [s.branches?.success, s.branches?.failure].filter(Boolean)
  const outputHandles = Array.isArray(s.outputHandles) && s.outputHandles.length > 0 ? s.outputHandles : OUTPUT_HANDLES
  return outputHandles.map(h => s.outputs?.[h]).filter(Boolean)
}

function flowToDef() {
  const stepNodes = nodes.value.filter(n => n.id !== START_NODE_ID)
  const steps = stepNodes.map(n => {
    const step = { ...(n.data?.step || {}), id: n.id, type: n.data?.step?.type || 'action' }
    if (step.type === 'conditional') {
      step.branches = { success: '', failure: '' }
      const successEdge = edges.value.find(e => e.source === n.id && e.sourceHandle === 'success')
      const failureEdge = edges.value.find(e => e.source === n.id && e.sourceHandle === 'failure')
      step.branches.success = successEdge?.target || ''
      step.branches.failure = failureEdge?.target || ''
    } else {
      step.outputHandles = [...OUTPUT_HANDLES]
      step.outputs = {}
      for (const edge of edges.value.filter(e => e.source === n.id)) {
        const handleId = edge.sourceHandle || OUTPUT_HANDLES[0]
        if (edge.target) step.outputs[handleId] = edge.target
      }
    }
    return step
  })
  const stepMap = new Map(steps.map(s => [s.id, s]))
  const startTargets = edges.value.filter(e => e.source === START_NODE_ID).map(e => e.target).filter(Boolean)
  const entryStepIds = [...new Set(startTargets)]
  const entryId = entryStepIds[0] ?? steps[0]?.id ?? ''
  const stepsInOrder = []
  const seen = new Set()
  const queue = [...entryStepIds]
  while (queue.length) {
    const cur = queue.shift()
    if (!cur || seen.has(cur)) continue
    seen.add(cur)
    const step = stepMap.get(cur)
    if (step) stepsInOrder.push(step)
    queue.push(...getNextIds(step, stepMap))
  }
  const orphaned = steps.filter(s => !seen.has(s.id))
  const orderedSteps = [...stepsInOrder, ...orphaned]
  const outputStepIds = steps.filter(s => getNextIds(s, stepMap).length === 0).map(s => s.id)
  const def = parseWorkflow(props.modelValue)
  def.steps = orderedSteps
  def.entryStepIds = entryStepIds.length > 0 ? entryStepIds : (def.entryStepIds?.length ? def.entryStepIds : [])
  def.entryStepId = def.entryStepId || entryId
  def.outputStepIds = def.outputStepIds?.length ? def.outputStepIds : (outputStepIds.length ? outputStepIds : [])
  def.nodePositions = {}
  for (const node of nodes.value) {
    if (node.position && typeof node.position.x === 'number' && typeof node.position.y === 'number') {
      def.nodePositions[node.id] = { x: node.position.x, y: node.position.y }
    }
  }
  return def
}

function emitUpdate() {
  const def = flowToDef()
  const json = JSON.stringify(def, null, 2)
  lastEmittedJson.value = json
  emit('update:modelValue', json)
}

/** Normaliza operationId quitando sufijo _N (ej. CreateCoupon_2 → CreateCoupon) para detectar duplicados. */
function normalizeOperationIdForDedup(opId) {
  if (!opId || typeof opId !== 'string') return opId || ''
  return opId.replace(/_\d+$/, '')
}

/** Clave para detectar acción duplicada: misma integración + misma operación + mismo nombre (displayName). Así acciones sueltas con nombre dinámico (ej. "Get product (2)") no cuentan como duplicado. */
function actionDuplicateKey(step) {
  if (!step || step.type !== 'action') return null
  const intId = step.integrationId ?? ''
  const opId = step.operationId ?? ''
  const base = opId ? `${intId}|${normalizeOperationIdForDedup(opId)}` : `${intId}|${(step.method || 'GET').toString().toUpperCase()}|${(step.path || '').toString().replace(/^\/+/, '')}`
  const name = (step.displayName || step.summary || '').toString().trim()
  return name ? `${base}|${name}` : base
}

/** True si el workflow actual tiene al menos dos acciones con la misma integración+operación. */
const hasDuplicateActions = computed(() => {
  const def = flowToDef()
  const steps = def.steps || []
  const keyCount = new Map()
  for (const s of steps) {
    const k = actionDuplicateKey(s)
    if (k) keyCount.set(k, (keyCount.get(k) || 0) + 1)
  }
  return [...keyCount.values()].some(c => c > 1)
})

/** Mostrar botón "Eliminar duplicados" solo cuando hay acciones con la misma integración+operación+nombre (duplicados reales). */
const showRemoveDuplicatesButton = computed(() => hasDuplicateActions.value)

/** Elimina acciones duplicadas: deja solo la primera de cada grupo (misma integración+operación) y reconecta referencias. */
function removeDuplicateActions() {
  const def = flowToDef()
  const steps = def.steps || []
  console.log('[removeDuplicateActions] def.steps count:', steps.length, 'steps:', steps.map(s => ({ id: s.id, type: s.type, key: actionDuplicateKey(s) })))
  const seenKeys = new Map()
  const idToSurvivor = new Map()
  const stepsToKeep = []
  for (const s of steps) {
    if (s.type === 'conditional') {
      stepsToKeep.push(s)
      continue
    }
    const k = actionDuplicateKey(s)
    if (!k) {
      stepsToKeep.push(s)
      continue
    }
    const survivor = seenKeys.get(k)
    if (survivor == null) {
      seenKeys.set(k, s.id)
      stepsToKeep.push(s)
    } else {
      idToSurvivor.set(s.id, survivor)
    }
  }
  console.log('[removeDuplicateActions] idToSurvivor (duplicates to remove):', Object.fromEntries(idToSurvivor))
  if (idToSurvivor.size === 0) {
    console.log('[removeDuplicateActions] No duplicates found, nothing to remove')
    return
  }
  function remap(id) {
    return idToSurvivor.get(id) ?? id
  }
  const newSteps = stepsToKeep.map(s => {
    const step = { ...s }
    if (step.type === 'conditional' && step.branches) {
      step.branches = {
        success: remap(step.branches.success) || '',
        failure: remap(step.branches.failure) || ''
      }
    } else if (step.outputs && typeof step.outputs === 'object') {
      const out = {}
      for (const [h, tid] of Object.entries(step.outputs)) if (tid) out[h] = remap(tid)
      step.outputs = out
    } else if (Array.isArray(step.nextStepIds)) {
      step.nextStepIds = step.nextStepIds.map(remap).filter(Boolean)
    } else if (step.nextStepId) {
      step.nextStepId = remap(step.nextStepId) || ''
    }
    return step
  })
  const newDef = {
    ...def,
    steps: newSteps,
    entryStepIds: (def.entryStepIds || []).map(remap).filter(Boolean),
    outputStepIds: (def.outputStepIds || []).map(remap).filter(Boolean)
  }
  if (newDef.entryStepId) newDef.entryStepId = remap(newDef.entryStepId) || newDef.entryStepIds?.[0] || ''
  const json = JSON.stringify(newDef, null, 2)
  console.log('[removeDuplicateActions] Applying newDef with', newDef.steps.length, 'steps')
  lastEmittedJson.value = json
  emit('update:modelValue', json)
  loadFromDef(newDef)
}

function exportWorkflow() {
  const def = flowToDef()
  const json = JSON.stringify(def, null, 2)
  const blob = new Blob([json], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `workflow-export-${new Date().toISOString().slice(0, 10)}.json`
  a.click()
  URL.revokeObjectURL(url)
}

function applyImportedWorkflow(json) {
  if (!json || typeof json !== 'string') return
  const def = parseWorkflow(json)
  lastEmittedJson.value = json
  emit('update:modelValue', json)
  loadFromDef(def)
  showImportModal.value = false
}

// --- Vue Flow handlers ---
function onFlowInit(store) {
  vueFlowStore.value = store
}

function isValidConnection({ source, sourceHandle, target }) {
  // One connection per (source, sourceHandle). Exclude the current edge when checking so programmatic load is not rejected.
  if (sourceHandle === 'success' || sourceHandle === 'failure' || (typeof sourceHandle === 'string' && sourceHandle.startsWith('out-'))) {
    if (edges.value.some(e => e.source === source && e.sourceHandle === sourceHandle && e.target !== target)) return false
  }
  return true
}

function onConnect(params) {
  didConnect.value = true
  const edge = {
    id: `e-${params.source}-${params.target}${params.sourceHandle ? `-${params.sourceHandle}` : ''}`,
    source: params.source,
    target: params.target,
    targetHandle: params.targetHandle ?? 'target'
  }
  if (params.sourceHandle) {
    edge.sourceHandle = params.sourceHandle
    if (params.sourceHandle === 'success' || params.sourceHandle === 'failure') edge.label = params.sourceHandle === 'success' ? 'true' : 'false'
  }
  const sameSourceHandle = (e) => e.source === params.source && (e.sourceHandle || null) === (params.sourceHandle || null)
  let newEdges
  if (params.source === START_NODE_ID) {
    newEdges = edges.value.filter(e => !(e.source === START_NODE_ID && e.target === params.target))
    newEdges = [...newEdges, edge]
  } else if (params.sourceHandle) {
    newEdges = edges.value.filter(e => !sameSourceHandle(e)).concat([edge])
  } else {
    newEdges = edges.value.filter(e => !(e.source === params.source && !e.sourceHandle && e.target === params.target)).concat([edge])
  }
  edges.value = newEdges
  syncToStore()
  emitUpdate()
}

function onConnectStart(params) {
  connectingFromNodeId.value = params.nodeId ?? null
  connectingFromHandle.value = params.handleId ?? params.handle?.id ?? null
  didConnect.value = false
}

function showDropMenu(sourceId, event, handleId = null) {
  if (!sourceId) return
  menuSourceId.value = sourceId
  menuSourceHandle.value = handleId ?? null
  if (event) {
    dropMenuX.value = event.clientX ?? 0
    dropMenuY.value = (event.clientY ?? 0) + 4
  } else {
    dropMenuX.value = window.innerWidth / 2 - 80
    dropMenuY.value = window.innerHeight / 2 - 60
  }
  dropMenuVisible.value = true
  dropMenuOpenAt.value = Date.now()
}

function onConnectEnd(event) {
  const sourceId = connectingFromNodeId.value
  const handleId = connectingFromHandle.value
  if (handleId === 'target') {
    connectingFromNodeId.value = null
    connectingFromHandle.value = null
    didConnect.value = false
    return
  }
  if (!didConnect.value && sourceId) showDropMenu(sourceId, event, handleId)
  connectingFromNodeId.value = null
  connectingFromHandle.value = null
  didConnect.value = false
}

function closeDropMenu() {
  dropMenuVisible.value = false
  menuSourceId.value = null
  menuSourceHandle.value = null
}

function closeDropMenuIfClickOutside() {
  if (!dropMenuVisible.value) return
  if (Date.now() - dropMenuOpenAt.value < 100) return
  closeDropMenu()
}

function onMenuChoice(choice) {
  const sourceId = menuSourceId.value
  const sourceHandle = menuSourceHandle.value
  closeDropMenu()
  if (choice === 'newAction' && props.addActionMode === 'modal') {
    emit('add-action-request', { sourceNodeId: sourceId, sourceHandle })
    return
  }
  addFromMenu(sourceId, sourceHandle, choice)
}

// --- Single entry point: add node(s) + edge(s) from menu ---
// Diferencia Start vs Action: Vue Flow necesita sourceHandle en la edge para enganchar al port.
// - Start: Handle id="start" → edge debe llevar sourceHandle: 'start'.
// - Action: Handle :id="handleId" (out-1, out-2, out-3) → edge debe llevar sourceHandle: 'out-1' etc.
// Antes solo setear sourceHandle cuando handle !== null (Action); para Start no se seteaba y
// funcionaba por suerte (un solo handle). Para que Action también dibuje la conexión, siempre
// setear sourceHandle (incl. 'start' para Start).
function addFromMenu(sourceNodeId, sourceHandle, choice) {
  if (!sourceNodeId && choice !== 'newAction' && choice !== 'newConditional') return
  isProgrammaticUpdate.value = true
  const stepNodes = nodes.value.filter(n => n.id !== START_NODE_ID)
  const connectFrom = sourceNodeId ?? (stepNodes[stepNodes.length - 1]?.id ?? START_NODE_ID)
  const sourceNode = nodes.value.find(n => n.id === connectFrom)
  const maxX = stepNodes.length > 0 ? Math.max(...stepNodes.map(n => n.position.x)) : START_NODE_X
  const x = sourceNode ? sourceNode.position.x + NODE_SPACING_X : maxX + NODE_SPACING_X

  function resolveHandle() {
    if (connectFrom === START_NODE_ID) return 'start'
    const used = new Set(edges.value.filter(e => e.source === connectFrom).map(e => e.sourceHandle))
    return sourceHandle && !used.has(sourceHandle) ? sourceHandle : (OUTPUT_HANDLES.find(h => !used.has(h)) ?? OUTPUT_HANDLES[0])
  }

  let newNodes = []
  let newEdges = [...edges.value]

  if (choice === 'newAction') {
    const id = generateNodeId()
    newNodes = [{
      id,
      type: 'action',
      position: { x, y: START_NODE_Y },
      data: { label: `Acción: ${id}`, step: defaultActionStep(id) }
    }]
    const handle = resolveHandle()
    const edge = { id: `e-${connectFrom}-${handle}-${id}`, source: connectFrom, target: id, targetHandle: 'target', sourceHandle: handle }
    newEdges.push(edge)
    if (stepNodes.length === 0) {
      const def = parseWorkflow(props.modelValue)
      def.entryStepId = id
      def.outputStepIds = [id]
      lastEmittedJson.value = JSON.stringify(def, null, 2)
      emit('update:modelValue', lastEmittedJson.value)
    }
  } else if (choice === 'newConditional') {
    const condId = generateNodeId()
    const actionTrueId = generateNodeId()
    const actionFalseId = generateNodeId()
    const handle = resolveHandle()
    const edgeToCond = { id: `e-${connectFrom}-${handle}-${condId}`, source: connectFrom, target: condId, targetHandle: 'target', sourceHandle: handle }
    newEdges.push(edgeToCond)
    newEdges.push({ id: `e-${condId}-success-${actionTrueId}`, source: condId, target: actionTrueId, sourceHandle: 'success', targetHandle: 'target' })
    newEdges.push({ id: `e-${condId}-failure-${actionFalseId}`, source: condId, target: actionFalseId, sourceHandle: 'failure', targetHandle: 'target' })
    newNodes = [
      { id: condId, type: 'conditional', position: { x, y: START_NODE_Y }, data: { label: 'Condicional', step: { id: condId, type: 'conditional', rulesJson: '[]', inputKey: '', branches: { success: actionTrueId, failure: actionFalseId }, nextStepId: null } } },
      { id: actionTrueId, type: 'action', position: { x: x + NODE_SPACING_X, y: START_NODE_Y - 60 }, data: { label: 'Acción (true)', step: { ...defaultActionStep(actionTrueId) } } },
      { id: actionFalseId, type: 'action', position: { x: x + NODE_SPACING_X, y: START_NODE_Y + 60 }, data: { label: 'Acción (false)', step: { ...defaultActionStep(actionFalseId) } } }
    ]
  } else if (choice === 'success' || choice === 'failure') {
    if (edges.value.some(e => e.source === sourceNodeId && e.sourceHandle === choice)) {
      isProgrammaticUpdate.value = false
      return
    }
    const id = generateNodeId()
    const condNode = nodes.value.find(n => n.id === sourceNodeId)
    const y = condNode ? condNode.position.y + (choice === 'success' ? -60 : 60) : START_NODE_Y
    newNodes = [{ id, type: 'action', position: { x, y }, data: { label: `Acción: ${id}`, step: defaultActionStep(id) } }]
    newEdges.push({ id: `e-${sourceNodeId}-${choice}-${id}`, source: sourceNodeId, target: id, sourceHandle: choice, targetHandle: 'target' })
  }

  nodes.value = [...nodes.value, ...newNodes]
  edges.value = newEdges
  // Debug: al añadir desde un output port, el nodo puede quedar suelto si la edge no se aplica
  const edgesFromSource = newEdges.filter(e => e.source === connectFrom)
  console.log('[addFromMenu]', { choice, connectFrom, sourceHandle, newNodes: newNodes.length, newEdges: newEdges.length, edgesFromSource, edgeToNewNode: newEdges.find(e => newNodes.some(n => n.id === e.target)) })
  syncToStore()
  emitUpdate()
  nextTick(() => { isProgrammaticUpdate.value = false })
  if (newNodes.length > 0 && (choice === 'success' || choice === 'failure')) {
    nextTick(() => vueFlowStore.value?.fitView?.({ padding: 0.2 }))
  }
}

function nextLooseNodePosition() {
  const stepNodes = nodes.value.filter(n => n.id !== START_NODE_ID)
  if (stepNodes.length === 0) return { x: START_NODE_X + NODE_SPACING_X, y: START_NODE_Y }
  const maxX = Math.max(...stepNodes.map(n => n.position.x))
  const maxY = Math.max(...stepNodes.map(n => n.position.y))
  return { x: maxX + NODE_SPACING_X, y: maxY }
}

function addLooseAction() {
  const id = generateNodeId()
  const pos = nextLooseNodePosition()
  const newNode = {
    id,
    type: 'action',
    position: pos,
    data: { label: `Acción: ${id}`, step: defaultActionStep(id) }
  }
  nodes.value = [...nodes.value, newNode]
  syncToStore()
  emitUpdate()
}

function addLooseConditional() {
  const condId = generateNodeId()
  const pos = nextLooseNodePosition()
  const newNode = {
    id: condId,
    type: 'conditional',
    position: pos,
    data: {
      label: 'Condicional',
      step: { id: condId, type: 'conditional', rulesJson: '[]', inputKey: '', branches: { success: '', failure: '' }, nextStepId: null }
    }
  }
  nodes.value = [...nodes.value, newNode]
  syncToStore()
  emitUpdate()
}

function onPaneClick() {
  selectedNodeId.value = null
  closeDropMenuIfClickOutside()
}
function onEdgeClick() { closeDropMenuIfClickOutside() }
function onNodeClick({ node }) {
  selectedNodeId.value = node?.id && node.id !== START_NODE_ID ? node.id : null
  closeDropMenuIfClickOutside()
}

function onNodeDoubleClick({ event, node }) {
  if (node.id === START_NODE_ID) {
    event?.stopPropagation?.()
    event?.preventDefault?.()
    if (props.addActionMode === 'modal') emit('edit-start-input-request')
    return
  }
  const step = node.data?.step
  if (step?.type === 'action' && props.addActionMode === 'modal') emit('edit-action-request', { stepId: node.id, step })
  else if (step?.type === 'conditional' && props.addActionMode === 'modal') {
    const incomingEdge = edges.value.find(e => e.target === node.id && !e.sourceHandle) ?? edges.value.find(e => e.target === node.id)
    const parentNodeId = incomingEdge?.source && incomingEdge.source !== START_NODE_ID ? incomingEdge.source : null
    emit('edit-conditional-request', { stepId: node.id, step, parentNodeId })
  }
}

function onNodesChange() {
  if (isProgrammaticUpdate.value) return
  emitUpdate()
}

function onEdgesChange() {
  if (isProgrammaticUpdate.value) return
  if (connectingFromNodeId.value != null) return
  emitUpdate()
}

// --- Provide for child components ---
provide('setOutputStep', (stepId) => emit('set-output-step', stepId))
provide('selectedNodeId', selectedNodeId)

function updateNodeLabel(nodeId, newLabel, defaultLabel) {
  const idx = nodes.value.findIndex(n => n.id === nodeId)
  if (idx === -1) return
  const n = nodes.value[idx]
  const label = (newLabel || defaultLabel || n.data?.label || nodeId).trim() || defaultLabel || n.data?.label || nodeId
  const updated = [...nodes.value]
  updated[idx] = { ...n, data: { ...n.data, label, step: { ...(n.data?.step || {}), displayName: (newLabel || '').trim() || undefined } } }
  nodes.value = updated
  syncToStore()
  emitUpdate()
}
provide('updateNodeLabel', updateNodeLabel)

const duplicateNodeIds = computed(() => {
  const stepNodes = nodes.value.filter(n => n.id !== START_NODE_ID)
  const byLabel = new Map()
  for (const n of stepNodes) {
    const label = (n.data?.label ?? n.id ?? '').toString().trim() || n.id
    if (!byLabel.has(label)) byLabel.set(label, [])
    byLabel.get(label).push(n.id)
  }
  const set = new Set()
  for (const ids of byLabel.values()) if (ids.length > 1) ids.forEach(id => set.add(id))
  return set
})
provide('duplicateNodeIds', duplicateNodeIds)

// --- Node types ---
const nodeTypes = {
  start: markRaw(StartNode),
  action: markRaw(ActionNode),
  conditional: markRaw(ConditionalNode)
}

// --- Test definition (simple): Start → Action 1 → Action 2, Action 3 ---
function getTestDefinition() {
  const a1 = generateNodeId()
  const a2 = generateNodeId()
  const a3 = generateNodeId()
  const actionStep = (id, outputs = {}, displayName = '') => ({
    id,
    type: 'action',
    method: 'GET',
    path: '/',
    outputHandles: OUTPUT_HANDLES,
    outputs,
    displayName: displayName || `Action ${id === a1 ? 1 : id === a2 ? 2 : 3}`
  })
  const steps = [
    actionStep(a1, { 'out-1': a2, 'out-2': a3 }, 'Action 1'),
    actionStep(a2, {}, 'Action 2'),
    actionStep(a3, {}, 'Action 3')
  ]
  const def = { steps, entryStepIds: [a1] }
  const nodePositions = computeLayoutByLevels(def)
  return {
    steps,
    entryStepId: a1,
    entryStepIds: [a1],
    outputStepIds: [a2, a3],
    outputMapping: undefined,
    nodePositions
  }
}

// --- Init & watch ---
watch(() => props.modelValue, (val) => {
  if (!val) return
  if (val === lastEmittedJson.value) return
  loadFromDef(parseWorkflow(val))
}, { flush: 'post' })

onMounted(() => {
  const def = parseWorkflow(props.modelValue)
  const isEmpty = !def.steps || def.steps.length === 0
  if (props.loadTestWorkflow && isEmpty) {
    const testDef = getTestDefinition()
    console.log('Test workflow def (canvas load):', JSON.stringify(testDef, null, 2))
    loadFromDef(testDef)
  } else if (isEmpty) {
    loadFromDef(defaultDef())
    lastEmittedJson.value = JSON.stringify(defaultDef(), null, 2)
    emit('update:modelValue', lastEmittedJson.value)
  } else {
    loadFromDef(def)
  }
})
</script>

<style>
.workflow-canvas {
  background-color: #ffffff;
  background-image:
    radial-gradient(circle at 1px 1px, rgba(0, 0, 0, 0.08) 1px, transparent 0);
  background-size: 20px 20px;
}
:global(.dark) .workflow-canvas {
  background-color: #1f2937;
  background-image:
    radial-gradient(circle at 1px 1px, rgba(156, 163, 175, 0.25) 1px, transparent 0);
}
.workflow-vueflow {
  background: transparent !important;
}
.workflow-editor .vue-flow__node-start {
  padding: 0;
  border: none;
  background: transparent;
}
.workflow-editor .vue-flow__node-action,
.workflow-editor .vue-flow__node-conditional {
  padding: 0;
  border: none;
  background: transparent;
}
.workflow-editor .vue-flow__handle {
  width: 12px;
  height: 12px;
  border-width: 2px;
  border-radius: 0;
  background: #e5e7eb !important;
  border-color: #6b7280 !important;
}
.workflow-editor .vue-flow__edge-path,
.workflow-editor .vue-flow__connection-path {
  stroke: #9ca3af;
  stroke-width: 2.5;
}

.workflow-add-nodes-panel {
  margin: 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.workflow-add-nodes__btn {
  display: block;
  width: 100%;
  margin-bottom: 0;
  padding: 8px 14px;
  font-size: 0.8125rem;
  color: #e5e7eb;
  background: rgba(31, 41, 55, 0.92);
  border: 1px solid #4b5563;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}
.workflow-add-nodes__btn--icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  min-width: 32px;
}
.workflow-add-nodes__btn--text {
  white-space: nowrap;
  min-width: 32px;
  width: max-content;
  padding-left: 10px;
  padding-right: 10px;
}
.workflow-add-nodes-panel:has(.workflow-add-nodes__btn--text) {
  min-width: 160px;
}
.workflow-add-nodes__icon {
  width: 18px;
  height: 18px;
}
.workflow-add-nodes__btn:last-child {
  margin-bottom: 0;
}
.workflow-add-nodes__btn:hover {
  color: #f9fafb;
  background: rgba(55, 65, 81, 0.95);
}
:global(.dark) .workflow-add-nodes__btn {
  background: rgba(31, 41, 55, 0.95);
  border-color: #6b7280;
}
:global(.dark) .workflow-add-nodes__btn:hover {
  background: rgba(55, 65, 81, 0.98);
}
</style>

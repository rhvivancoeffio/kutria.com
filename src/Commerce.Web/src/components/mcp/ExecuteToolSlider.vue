<template>
  <Teleport to="body">
    <div
      v-if="modelValue"
      class="fixed inset-0 z-50 flex justify-end bg-black/50"
    >
      <div
        class="w-full h-full max-h-full sm:max-w-md bg-white dark:bg-gray-800 shadow-xl overflow-hidden flex flex-col animate-slide-in-right"
        @click.stop
      >
        <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between gap-2 shrink-0 min-h-[52px]">
          <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white truncate min-w-0">
            Ejecutar: {{ tool?.name ?? tool?.title ?? 'Tool' }}
          </h3>
          <button
            type="button"
            @click="close"
            class="p-2 shrink-0 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded touch-manipulation"
            aria-label="Cerrar"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
          </button>
        </div>
        <div class="flex-1 min-h-0 overflow-y-auto p-4 space-y-4">
          <p class="text-sm text-gray-600 dark:text-gray-400">Completa los parámetros del tool y ejecuta.</p>
          <template v-if="executeInputFields.length">
            <div v-for="field in executeInputFields" :key="field.name" class="space-y-1">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                {{ field.name }}
                <span v-if="field.required" class="text-red-500">*</span>
              </label>
              <!-- Boolean: toggle -->
              <div v-if="field.type === 'boolean'" class="flex items-center gap-3">
                <button
                  type="button"
                  role="switch"
                  :aria-checked="executeInputValues[field.name] === true || executeInputValues[field.name] === 'true'"
                  :class="[
                    'relative inline-flex h-6 w-11 shrink-0 rounded-full border-2 border-transparent transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:focus:ring-offset-gray-800',
                    (executeInputValues[field.name] === true || executeInputValues[field.name] === 'true')
                      ? 'bg-primary-600'
                      : 'bg-gray-200 dark:bg-gray-600'
                  ]"
                  @click="toggleBoolean(field.name)"
                >
                  <span
                    :class="[
                      'pointer-events-none inline-block h-5 w-5 rounded-full bg-white shadow ring-0 transition',
                      (executeInputValues[field.name] === true || executeInputValues[field.name] === 'true')
                        ? 'translate-x-5'
                        : 'translate-x-1'
                    ]"
                  />
                </button>
                <span class="text-sm text-gray-600 dark:text-gray-400">
                  {{ (executeInputValues[field.name] === true || executeInputValues[field.name] === 'true') ? 'Sí' : 'No' }}
                </span>
              </div>
              <!-- Date / date-time: date input -->
              <input
                v-else-if="isDateOrDateTime(field)"
                v-model="executeInputValues[field.name]"
                :type="field.format === 'date-time' ? 'datetime-local' : 'date'"
                :class="[
                  'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm transition-colors',
                  isExecuteFieldInvalid(field)
                    ? 'border-red-500 focus:border-red-500 focus:ring-red-500'
                    : 'border-gray-300 dark:border-gray-600 focus:border-primary-500 focus:ring-primary-500'
                ]"
                @blur="markExecuteFieldTouched(field.name)"
              />
              <!-- Text, number, etc. -->
              <input
                v-else
                v-model="executeInputValues[field.name]"
                :type="field.type === 'number' || field.type === 'integer' ? 'number' : 'text'"
                :class="[
                  'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm transition-colors',
                  isExecuteFieldInvalid(field)
                    ? 'border-red-500 focus:border-red-500 focus:ring-red-500'
                    : 'border-gray-300 dark:border-gray-600 focus:border-primary-500 focus:ring-primary-500'
                ]"
                :placeholder="field.default !== undefined ? String(field.default) : ''"
                @blur="markExecuteFieldTouched(field.name)"
              />
              <p v-if="isExecuteFieldInvalid(field)" class="text-xs text-red-500">Requerido</p>
            </div>
          </template>
          <p v-else class="text-sm text-gray-500 dark:text-gray-400">Este tool no tiene parámetros de entrada.</p>
          <p v-if="executeError" class="text-sm text-red-600 dark:text-red-400">{{ executeError }}</p>
          <div v-if="executeResult !== null" class="rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 overflow-hidden">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 px-3 py-2 border-b border-gray-200 dark:border-gray-600">Resultado</p>
            <div class="relative p-3 overflow-auto min-h-[280px] max-h-[min(70vh,520px)] json-viewer-wrapper">
              <button
                type="button"
                title="Copiar JSON"
                class="absolute top-2 right-2 z-10 p-2 rounded-lg bg-white/90 dark:bg-gray-800/90 border border-gray-200 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-gray-100 shadow-sm transition-colors"
                @click="copyResultToClipboard"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
              <JsonViewer
                v-if="executeResultObject != null"
                :value="executeResultObject"
                :theme="jsonViewerTheme"
                boxed
                sort
                :expand-depth="2"
                expanded
              />
              <pre v-else class="text-xs font-mono text-gray-800 dark:text-gray-200 whitespace-pre-wrap">{{ typeof executeResult === 'object' ? JSON.stringify(executeResult, null, 2) : executeResult }}</pre>
            </div>
          </div>
        </div>
        <div class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex flex-col gap-3 shrink-0 min-h-0 pb-safe">
          <div class="flex items-center justify-between gap-2 flex-wrap">
            <span class="text-xs text-gray-500 dark:text-gray-400">Disponible en</span>
            <div class="flex items-center gap-2">
              <ClientIcon v-for="cid in sliderClientIds" :key="cid" :client-id="cid" size="sm" />
            </div>
          </div>
          <div class="flex items-center justify-between gap-3">
            <span class="text-sm text-gray-600 dark:text-gray-400">Probar sin consumir créditos</span>
            <button
              type="button"
              role="switch"
              :aria-checked="runAsTest"
              :class="[
                'relative inline-flex h-6 w-11 shrink-0 rounded-full border-2 border-transparent transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:focus:ring-offset-gray-800',
                runAsTest ? 'bg-primary-600' : 'bg-gray-200 dark:bg-gray-600'
              ]"
              @click="runAsTest = !runAsTest"
            >
              <span
                :class="[
                  'pointer-events-none inline-block h-5 w-5 rounded-full bg-white shadow ring-0 transition',
                  runAsTest ? 'translate-x-5' : 'translate-x-1'
                ]"
              />
            </button>
          </div>
          <div class="flex flex-col gap-2 sm:flex-row sm:justify-end sm:gap-2">
            <button
              type="button"
              :disabled="executing || !executeFormValid"
              :class="['w-full sm:w-auto px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg', (executing || !executeFormValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-green-600 hover:bg-green-700 text-white']"
              @click="doExecute"
            >
              {{ executing ? 'Ejecutando…' : 'Ejecutar' }}
            </button>
            <button
              type="button"
              class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
              @click="close"
            >
              Cerrar
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useToast } from 'vue-toastification'
import { JsonViewer } from 'vue3-json-viewer'
import 'vue3-json-viewer/dist/vue3-json-viewer.css'
import { useTheme } from '../../composables/useTheme'
import apiService from '../../services/api'
import ClientIcon from './ClientIcon.vue'
import { MCP_CLIENTS } from '../../data/mcpClients'

const sliderClientIds = MCP_CLIENTS.filter(c => c.id !== 'other').map(c => c.id)

const { theme } = useTheme()
const jsonViewerTheme = computed(() => (theme.value === 'dark' ? 'dark' : 'light'))

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  /** Tool a ejecutar: { id, name?, title?, inputSchema? } */
  tool: { type: Object, default: null },
  mcpId: { type: String, default: '' }
})

const emit = defineEmits(['update:modelValue'])

const toast = useToast()
const executeInputValues = ref({})
const executeTouched = ref({})
const executeResult = ref(null)
const executeError = ref(null)
const executing = ref(false)
/** Si true, la ejecución no consume créditos de la cuenta. */
const runAsTest = ref(true)

/** Objeto para JsonViewer (objeto o array); si es string no parseable, null y se usa pre. */
const executeResultObject = computed(() => {
  const r = executeResult.value
  if (r == null) return null
  if (typeof r === 'object') return r
  if (typeof r === 'string') {
    try {
      return JSON.parse(r)
    } catch {
      return null
    }
  }
  return null
})

const executeInputFields = computed(() => {
  const t = props.tool
  if (!t?.inputSchema) return []
  try {
    const schema = typeof t.inputSchema === 'string' ? JSON.parse(t.inputSchema) : t.inputSchema
    const schemaProps = schema?.properties ?? {}
    const required = new Set(schema?.required ?? [])
    return Object.entries(schemaProps).map(([name, p]) => ({
      name,
      type: p?.type ?? 'string',
      format: p?.format,
      required: required.has(name),
      default: p?.default
    }))
  } catch {
    return []
  }
})

function isDateOrDateTime(field) {
  const t = (field.type || '').toLowerCase()
  const f = (field.format || '').toLowerCase()
  return t === 'string' && (f === 'date' || f === 'date-time') || t === 'date' || f === 'date' || f === 'date-time'
}

function toggleBoolean(fieldName) {
  const current = executeInputValues.value[fieldName]
  const next = current === true || current === 'true'
  executeInputValues.value = { ...executeInputValues.value, [fieldName]: !next }
}

const executeFormValid = computed(() => {
  const fields = executeInputFields.value
  if (!fields.length) return true
  for (const field of fields) {
    if (!field.required) continue
    const val = executeInputValues.value[field.name]
    if (field.type === 'boolean') {
      if (val === undefined || val === null) return false
    } else if (val === undefined || val === null) return false
    if (field.type === 'number' || field.type === 'integer') {
      if (val === '') return false
      if (Number.isNaN(Number(val))) return false
    } else if (field.type !== 'boolean' && String(val).trim() === '') {
      return false
    }
  }
  return true
})

function isExecuteFieldInvalid(field) {
  if (!field.required) return false
  const touched = executeTouched.value[field.name]
  if (!touched) return false
  const val = executeInputValues.value[field.name]
  if (field.type === 'boolean') return val === undefined || val === null
  if (val === undefined || val === null) return true
  if (field.type === 'number' || field.type === 'integer') {
    if (val === '') return true
    return Number.isNaN(Number(val))
  }
  return String(val).trim() === ''
}

function markExecuteFieldTouched(fieldName) {
  executeTouched.value = { ...executeTouched.value, [fieldName]: true }
}

function close() {
  emit('update:modelValue', false)
}

async function copyResultToClipboard() {
  if (executeResult.value == null) return
  const text = typeof executeResult.value === 'object'
    ? JSON.stringify(executeResult.value, null, 2)
    : String(executeResult.value)
  try {
    await navigator.clipboard.writeText(text)
    toast.success('JSON copiado al portapapeles')
  } catch {
    toast.error('No se pudo copiar')
  }
}

watch(() => props.tool, (t) => {
  executeResult.value = null
  executeError.value = null
  executeTouched.value = {}
  if (!t) {
    executeInputValues.value = {}
    return
  }
  const vals = {}
  try {
    const schema = typeof t.inputSchema === 'string' ? JSON.parse(t.inputSchema || '{}') : (t.inputSchema || {})
    const schemaProps = schema?.properties ?? {}
    for (const [name, p] of Object.entries(schemaProps)) {
      const type = (p?.type ?? 'string').toLowerCase()
      const format = (p?.format ?? '').toLowerCase()
      if (type === 'boolean') {
        vals[name] = p?.default === true || p?.default === 'true'
      } else if ((type === 'string' && (format === 'date' || format === 'date-time')) || format === 'date' || format === 'date-time') {
        vals[name] = p?.default ?? ''
      } else {
        vals[name] = p?.default !== undefined ? String(p.default) : ''
      }
    }
  } catch {
    /* ignore */
  }
  executeInputValues.value = vals
}, { immediate: true })

async function doExecute() {
  if (!props.tool?.id || !props.mcpId) return
  executing.value = true
  executeError.value = null
  executeResult.value = null
  try {
    const input = {}
    for (const [key, val] of Object.entries(executeInputValues.value)) {
      if (val === undefined || val === null) continue
      const field = executeInputFields.value.find(f => f.name === key)
      const type = (field?.type ?? 'string').toLowerCase()
      if (type === 'number' || type === 'integer') {
        if (val === '') {
          input[key] = null
        } else {
          const n = Number(val)
          input[key] = Number.isNaN(n) ? null : (type === 'integer' ? Math.floor(n) : n)
        }
      } else if (type === 'boolean') {
        input[key] = val === true || val === 'true' || val === '1'
      } else if (isDateOrDateTime(field)) {
        input[key] = String(val).trim() || null
      } else {
        input[key] = val
      }
    }
    const result = await apiService.executeToolWorkflow(props.mcpId, props.tool.id, input, runAsTest.value)
    executeResult.value = result
    toast.success(runAsTest.value ? 'Ejecución completada (modo prueba, no se consumieron créditos)' : 'Ejecución completada')
  } catch (e) {
    executeError.value = e.response?.data?.error || e.message || 'Error al ejecutar'
    toast.error(executeError.value)
  } finally {
    executing.value = false
  }
}
</script>

<style scoped>
.animate-slide-in-right {
  animation: slideInRight 0.2s ease-out;
}
@keyframes slideInRight {
  from {
    transform: translateX(100%);
  }
  to {
    transform: translateX(0);
  }
}
</style>

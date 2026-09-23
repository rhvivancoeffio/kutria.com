<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="fixed inset-0 z-50 flex">
        <div class="absolute inset-0 bg-black/50" />
        <div
          class="relative ml-auto w-full max-w-2xl h-full bg-white dark:bg-gray-800 shadow-xl flex flex-col animate-slide-in"
          @click.stop
        >
          <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Resultado agrupado</h2>
            <button
              type="button"
              class="p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
              @click="$emit('update:modelValue', false)"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>

          <div class="flex-1 min-h-0 overflow-y-auto p-4 space-y-4">
            <p class="text-sm text-gray-600 dark:text-gray-400">
              El tool devolverá un objeto con las claves que definas. Cada clave puede tomar el valor del output de un paso (completo o una subruta).
            </p>

            <!-- Preview JSON -->
            <div class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50 dark:bg-gray-900/50">
              <div class="px-3 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-100 dark:bg-gray-800/50">
                Vista previa del resultado
              </div>
              <pre class="p-3 text-xs font-mono text-gray-800 dark:text-gray-200 overflow-x-auto overflow-y-auto max-h-40">{{ previewJson }}</pre>
            </div>

            <div class="flex items-center justify-between">
              <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Campos del resultado</span>
              <button
                type="button"
                class="text-xs px-3 py-1.5 rounded-lg border border-slate-400 dark:border-slate-500 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700"
                @click="generateFromSteps"
              >
                Generar desde pasos
              </button>
            </div>

            <div class="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-600">
              <table class="w-full text-sm border-collapse">
                <thead>
                  <tr class="text-left text-xs text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50">
                    <th class="pb-2 pt-2 pl-3 pr-2">Clave</th>
                    <th class="pb-2 pt-2 pr-2">Paso</th>
                    <th class="pb-2 pt-2 pr-2">Subruta (opcional)</th>
                    <th class="pb-2 pt-2 w-9 pr-2" />
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(row, idx) in rows" :key="idx" class="border-b border-gray-200 dark:border-gray-600/50">
                    <td class="py-1.5 pl-3 pr-2">
                      <input
                        v-model="row.key"
                        type="text"
                        placeholder="ej. producto"
                        class="w-full max-w-[140px] px-2 py-1.5 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
                        @input="normalizeKey(row)"
                      />
                    </td>
                    <td class="py-1.5 pr-2">
                      <select
                        v-model="row.stepId"
                        class="px-2 py-1.5 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 min-w-[140px]"
                      >
                        <option value="">—</option>
                        <option v-for="s in actionSteps" :key="s.id" :value="s.id">{{ s.summary || s.operationId || s.id }}</option>
                      </select>
                    </td>
                    <td class="py-1.5 pr-2">
                      <input
                        v-model="row.path"
                        type="text"
                        placeholder="ej. items.0.name"
                        class="w-full max-w-[160px] px-2 py-1.5 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 font-mono"
                      />
                    </td>
                    <td class="py-1.5 pr-2">
                      <button type="button" class="p-1 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 rounded" title="Quitar" @click="removeRow(idx)">×</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <button
              type="button"
              class="w-full text-sm px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800 bg-white dark:bg-gray-800/50"
              @click="addRow"
            >
              + Añadir campo
            </button>
          </div>

          <div class="shrink-0 px-4 py-3 border-t border-gray-200 dark:border-gray-600 flex justify-end gap-2">
            <button type="button" class="px-4 py-2 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700" @click="$emit('update:modelValue', false)">
              Cancelar
            </button>
            <button type="button" class="px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white" @click="save">
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

const PLACEHOLDER_REGEX = /^\{([^.]+)\.output\.?(.*)\}$/

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  workflowJson: { type: String, default: '{}' },
  /** outputMapping actual del workflow: { key -> "{stepId.output}" } */
  outputMapping: { type: Object, default: () => ({}) }
})

const emit = defineEmits(['update:modelValue', 'save'])

const rows = ref([])

const actionSteps = computed(() => {
  try {
    const w = JSON.parse(props.workflowJson || '{}')
    const steps = Array.isArray(w.steps) ? w.steps : []
    return steps.filter(s => s.type === 'action' && s.id)
  } catch {
    return []
  }
})

function slugFromStep(step) {
  const s = (step.summary || step.operationId || step.id || 'step').toString()
  return s
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^a-z0-9_]/g, '')
    .slice(0, 40) || step.id
}

function parseToRows(mapping) {
  if (!mapping || typeof mapping !== 'object') return []
  return Object.entries(mapping).map(([key, value]) => {
    const v = typeof value === 'string' ? value.trim() : ''
    const m = v.match(PLACEHOLDER_REGEX)
    return {
      key: key || '',
      stepId: m ? m[1] : '',
      path: m ? (m[2] || '') : ''
    }
  })
}

function buildMapping() {
  const out = {}
  for (const row of rows.value) {
    const k = (row.key || '').trim().replace(/[^a-zA-Z0-9_]/g, '')
    if (!k || !row.stepId) continue
    const placeholder = row.path?.trim()
      ? `{${row.stepId}.output.${row.path.trim()}}`
      : `{${row.stepId}.output}`
    out[k] = placeholder
  }
  return out
}

const previewJson = computed(() => {
  const mapping = buildMapping()
  const obj = {}
  const steps = actionSteps.value
  for (const [key, placeholder] of Object.entries(mapping)) {
    const m = placeholder.match(PLACEHOLDER_REGEX)
    if (m) {
      const step = steps.find(s => s.id === m[1])
      const label = step ? (step.summary || step.operationId || m[1]) : m[1]
      obj[key] = m[2] ? `<${label}: ${m[2]}>` : `<output: ${label}>`
    } else {
      obj[key] = '?'
    }
  }
  return Object.keys(obj).length ? JSON.stringify(obj, null, 2) : '{}'
})

function normalizeKey(row) {
  row.key = (row.key || '').replace(/[^a-zA-Z0-9_]/g, '')
}

function generateFromSteps() {
  const steps = actionSteps.value
  const used = new Set()
  rows.value = steps.map(s => {
    let key = slugFromStep(s)
    if (used.has(key)) {
      let n = 1
      while (used.has(`${key}_${n}`)) n++
      key = `${key}_${n}`
    }
    used.add(key)
    return { key, stepId: s.id, path: '' }
  })
}

function addRow() {
  rows.value = [...rows.value, { key: '', stepId: '', path: '' }]
}

function removeRow(idx) {
  rows.value = rows.value.filter((_, i) => i !== idx)
}

function save() {
  const mapping = buildMapping()
  emit('save', Object.keys(mapping).length > 0 ? mapping : undefined)
  emit('update:modelValue', false)
}

watch(() => props.modelValue, (open) => {
  if (open) {
    const current = props.outputMapping && typeof props.outputMapping === 'object' ? props.outputMapping : {}
    const parsed = parseToRows(current)
    if (parsed.length > 0) {
      rows.value = parsed
    } else {
      generateFromSteps()
    }
  }
})
</script>

<style scoped>
.animate-slide-in {
  animation: slideIn 0.25s ease-out;
}
@keyframes slideIn {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>

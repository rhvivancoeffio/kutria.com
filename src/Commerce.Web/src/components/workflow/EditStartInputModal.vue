<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="fixed inset-0 z-50 flex">
        <div class="absolute inset-0 bg-black/50" />
        <div
          class="relative ml-auto w-full max-w-lg h-full bg-white dark:bg-gray-800 shadow-xl flex flex-col animate-slide-in"
          @click.stop
        >
          <!-- Header -->
          <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Input del tool</h2>
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
            <div class="rounded-lg border border-blue-200 dark:border-blue-800 bg-blue-50/50 dark:bg-blue-900/20 p-4 space-y-3">
              <h3 class="text-sm font-medium text-blue-900 dark:text-blue-100">Cómo mapear a los nodos</h3>
              <p class="text-xs text-blue-800 dark:text-blue-200">
                Cada parámetro que definas aquí estará disponible en todos los nodos del workflow. Al configurar una acción (doble clic o al añadirla), en el campo «Valor por defecto» podrás usar la sintaxis:
              </p>
              <div v-if="mappingPlaceholders.length > 0" class="space-y-2">
                <p class="text-xs font-medium text-blue-900 dark:text-blue-100">Sintaxis disponible:</p>
                <div class="flex flex-wrap gap-2">
                  <code
                    v-for="ph in mappingPlaceholders"
                    :key="ph"
                    class="px-2 py-1 rounded bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-200 font-mono text-xs"
                  >{{ ph }}</code>
                </div>
                <p class="text-xs text-blue-700 dark:text-blue-300 mt-2">
                  Haz clic en «← input» al configurar cada acción para aplicar el valor automáticamente.
                </p>
              </div>
              <p v-else class="text-xs text-blue-600 dark:text-blue-400 italic">
                Añade parámetros abajo para ver la sintaxis.
              </p>
            </div>
            <div class="space-y-3">
              <div
                v-for="(field, idx) in fields"
                :key="idx"
                class="p-4 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700/50"
              >
                <div class="flex items-center justify-between gap-2 mb-3">
                  <span class="text-xs font-medium text-gray-500 dark:text-gray-400">Parámetro {{ idx + 1 }}</span>
                  <button type="button" @click="removeField(idx)" class="p-1.5 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded hover:bg-gray-200 dark:hover:bg-gray-600" title="Quitar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                  </button>
                </div>
                <div class="grid grid-cols-2 gap-3">
                  <div class="space-y-1">
                    <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Nombre</label>
                    <input v-model="field.name" type="text" placeholder="param_name" class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                  </div>
                  <div class="space-y-1">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400">Tipo</label>
                    <select v-model="field.type" class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white">
                      <option value="string">string</option>
                      <option value="number">number</option>
                      <option value="integer">integer</option>
                      <option value="boolean">boolean</option>
                      <option value="array">array</option>
                      <option value="object">object</option>
                    </select>
                  </div>
                  <div class="space-y-1 col-span-2">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400">Valor por defecto</label>
                    <input v-model="field.defaultValue" type="text" placeholder="(opcional)" class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                  </div>
                  <div class="space-y-1">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400">Requerido</label>
                    <div class="flex items-center h-[42px]">
                      <label class="relative inline-flex items-center cursor-pointer select-none">
                        <input v-model="field.required" type="checkbox" class="sr-only peer" />
                        <div class="w-12 h-7 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[3px] after:left-[3px] after:bg-white after:rounded-full after:h-6 after:w-6 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5" />
                        <span class="ml-3 text-sm font-medium text-gray-700 dark:text-gray-300">{{ field.required ? 'Sí' : 'No' }}</span>
                      </label>
                    </div>
                  </div>
                </div>
              </div>
              <button type="button" @click="addField" class="w-full py-2.5 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-primary-50/50 dark:hover:bg-primary-900/10 text-sm font-medium transition-colors">
                + Añadir parámetro
              </button>
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
              class="px-4 py-2 rounded-lg font-medium bg-green-600 hover:bg-green-700 text-white"
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

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  inputSchemaJson: { type: String, default: '{}' }
})

const emit = defineEmits(['update:modelValue', 'save'])

const fields = ref([])

const mappingPlaceholders = computed(() =>
  fields.value
    .filter(f => (f.name || '').trim())
    .map(f => `{input.${f.name.trim()}}`)
)

function parseSchemaToFields(json) {
  try {
    const schema = typeof json === 'string' ? JSON.parse(json || '{}') : json
    const propsObj = schema.properties || {}
    const requiredSet = new Set(schema.required || [])
    const result = []
    for (const [name, prop] of Object.entries(propsObj)) {
      result.push({
        name,
        type: prop?.type || 'string',
        required: requiredSet.has(name),
        defaultValue: prop?.default !== undefined ? (typeof prop.default === 'object' ? JSON.stringify(prop.default) : String(prop.default)) : ''
      })
    }
    return result.length > 0 ? result : [{ name: '', type: 'string', required: false, defaultValue: '' }]
  } catch {
    return [{ name: '', type: 'string', required: false, defaultValue: '' }]
  }
}

function buildSchemaJson() {
  const valid = fields.value.filter(f => (f.name || '').trim())
  if (valid.length === 0) return '{}'
  const properties = {}
  const required = []
  for (const f of valid) {
    const name = f.name.trim()
    if (!name) continue
    properties[name] = { type: f.type || 'string' }
    const defVal = f.defaultValue?.trim()
    if (defVal !== '') {
      try {
        if (f.type === 'number' || f.type === 'integer') {
          properties[name].default = Number(defVal)
        } else if (f.type === 'boolean') {
          properties[name].default = defVal === 'true' || defVal === '1'
        } else if (f.type === 'array' || f.type === 'object') {
          properties[name].default = JSON.parse(defVal)
        } else {
          properties[name].default = defVal
        }
      } catch {
        properties[name].default = defVal
      }
    }
    if (f.required) required.push(name)
  }
  return JSON.stringify({ type: 'object', properties, required }, null, 2)
}

function addField() {
  fields.value.push({ name: '', type: 'string', required: false, defaultValue: '' })
}

function removeField(idx) {
  fields.value.splice(idx, 1)
  if (fields.value.length === 0) {
    fields.value.push({ name: '', type: 'string', required: false, defaultValue: '' })
  }
}

function handleSave() {
  emit('save', buildSchemaJson())
  emit('update:modelValue', false)
}

watch(() => props.modelValue, (open) => {
  if (open) {
    fields.value = parseSchemaToFields(props.inputSchemaJson)
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
.modal-enter-active, .modal-leave-active {
  transition: opacity 0.2s;
}
.modal-enter-from, .modal-leave-to {
  opacity: 0;
}
</style>

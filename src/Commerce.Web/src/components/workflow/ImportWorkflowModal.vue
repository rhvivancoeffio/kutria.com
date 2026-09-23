<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="fixed inset-0 z-50 flex">
        <div class="absolute inset-0 bg-black/50" />
        <div
          class="relative ml-auto w-full max-w-lg md:max-w-2xl h-full bg-white dark:bg-gray-800 shadow-xl flex flex-col animate-slide-in"
          @click.stop
        >
          <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Importar workflow</h2>
            <button
              type="button"
              @click="close"
              class="p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>

          <div class="flex-1 min-h-0 overflow-y-auto p-4 space-y-4">
            <p class="text-sm text-gray-600 dark:text-gray-400">
              Carga un workflow desde un archivo JSON o pega el contenido debajo. El flujo se aplicará al canvas (solo en memoria).
            </p>

            <!-- Paso 1: elegir modo -->
            <template v-if="inputMode === 'choice'">
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div
                  class="relative flex flex-col items-center justify-center p-6 rounded-xl border-2 border-gray-300 dark:border-gray-600 card-add-hover cursor-pointer"
                  @click="inputMode = 'file'"
                >
                  <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 16m4-4V4" /></svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Cargar archivo</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">Arrastra o selecciona un .json</span>
                </div>
                <div
                  class="flex flex-col items-center justify-center p-6 rounded-xl border-2 border-gray-300 dark:border-gray-600 card-add-hover cursor-pointer"
                  @click="inputMode = 'paste'"
                >
                  <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Pegar JSON</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">Escribe o pega el workflow en el área de texto</span>
                </div>
              </div>
            </template>

            <!-- Modo archivo -->
            <template v-else-if="inputMode === 'file'">
              <div class="space-y-3">
                <button
                  type="button"
                  class="inline-flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                  @click="inputMode = 'choice'"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" /></svg>
                  Cambiar a pegar JSON
                </button>
                <div
                  class="flex flex-col items-center justify-center p-8 rounded-xl border-2 border-dashed border-gray-300 dark:border-gray-600 card-add-hover cursor-pointer"
                  :class="{ 'border-primary-500 bg-primary-50/30 dark:bg-primary-900/10': isDragging }"
                  @dragover.prevent="isDragging = true"
                  @dragleave.prevent="isDragging = false"
                  @drop.prevent="onFileDrop"
                  @click="fileInputRef?.click()"
                >
                  <input
                    ref="fileInputRef"
                    type="file"
                    accept=".json,application/json"
                    class="hidden"
                    @change="onFileSelect"
                  />
                  <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l7-7m-7 7h18" /></svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Arrastra un archivo aquí o haz clic para seleccionar</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">.json</span>
                </div>
                <p v-if="fileError" class="text-sm text-red-600 dark:text-red-400">{{ fileError }}</p>
                <!-- JSON cargado: mostrar en el editor para revisar o editar antes de importar -->
                <div v-if="fileContent" class="space-y-1">
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400">JSON cargado (puedes editarlo antes de importar)</label>
                  <JsonEditor
                    :model-value="fileContent"
                    placeholder='{"steps":[],"entryStepIds":[],"entryStepId":"","outputStepIds":[]}'
                    height="min(360px, 50vh)"
                    :invalid="!!validationError"
                    @update:model-value="fileContent = $event; validationError = ''"
                  />
                  <p v-if="validationError" class="text-sm text-red-600 dark:text-red-400">{{ validationError }}</p>
                </div>
              </div>
            </template>

            <!-- Modo pegar -->
            <template v-else>
              <div class="space-y-3">
                <button
                  type="button"
                  class="inline-flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                  @click="inputMode = 'choice'"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" /></svg>
                  Cambiar a cargar archivo
                </button>
                <div class="space-y-1">
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400">JSON del workflow</label>
                  <JsonEditor
                    :model-value="pastedJson"
                    placeholder='{"steps":[],"entryStepIds":[],"entryStepId":"","outputStepIds":[]}'
                    height="min(360px, 50vh)"
                    :invalid="!!validationError"
                    @update:model-value="pastedJson = $event; validationError = ''"
                  />
                  <p v-if="validationError" class="text-sm text-red-600 dark:text-red-400">{{ validationError }}</p>
                </div>
              </div>
            </template>

            <p v-if="validationError" class="text-sm text-red-600 dark:text-red-400">{{ validationError }}</p>
          </div>

          <div class="shrink-0 px-4 py-3 border-t border-gray-200 dark:border-gray-600 flex justify-end gap-3">
            <button
              type="button"
              @click="close"
              class="px-4 py-2 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              Cancelar
            </button>
            <button
              type="button"
              @click="applyImport"
              :disabled="!canImport"
              :class="['px-4 py-2 rounded-lg font-medium transition-colors', canImport ? 'bg-primary-600 hover:bg-primary-700 text-white' : 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed']"
            >
              Importar
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import JsonEditor from '../JsonEditor.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'import'])

const inputMode = ref('choice')
const pastedJson = ref('')
const fileContent = ref('')
const isDragging = ref(false)
const fileInputRef = ref(null)
const fileError = ref('')
const pasteError = ref('')
const validationError = ref('')

const canImport = computed(() => {
  if (inputMode.value === 'file') return !!fileContent.value
  if (inputMode.value === 'paste') return (pastedJson.value || '').trim().length > 0
  return false
})

function parseAndValidate(str) {
  validationError.value = ''
  if (!str || typeof str !== 'string') return null
  const trimmed = str.trim()
  if (!trimmed) return null
  try {
    const data = JSON.parse(trimmed)
    if (data !== null && typeof data === 'object') {
      if (!Array.isArray(data.steps)) data.steps = []
      if (!Array.isArray(data.entryStepIds)) data.entryStepIds = data.entryStepId != null ? [data.entryStepId] : []
      return data
    }
    validationError.value = 'El JSON debe ser un objeto (workflow con steps, entryStepIds, etc.).'
    return null
  } catch (e) {
    validationError.value = 'JSON inválido: ' + (e.message || 'error de sintaxis')
    return null
  }
}

function onFileSelect(ev) {
  fileError.value = ''
  fileContent.value = ''
  const file = ev.target?.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = () => {
    const text = reader.result
    const parsed = parseAndValidate(text)
    if (parsed) fileContent.value = JSON.stringify(parsed, null, 2)
  }
  reader.onerror = () => {
    fileError.value = 'No se pudo leer el archivo.'
  }
  reader.readAsText(file, 'UTF-8')
}

function onFileDrop(ev) {
  isDragging.value = false
  fileError.value = ''
  fileContent.value = ''
  const file = ev.dataTransfer?.files?.[0]
  if (!file) return
  if (!file.name.toLowerCase().endsWith('.json') && !file.type.includes('json')) {
    fileError.value = 'Solo se aceptan archivos .json'
    return
  }
  const reader = new FileReader()
  reader.onload = () => {
    const text = reader.result
    const parsed = parseAndValidate(text)
    if (parsed) fileContent.value = JSON.stringify(parsed, null, 2)
  }
  reader.onerror = () => { fileError.value = 'No se pudo leer el archivo.' }
  reader.readAsText(file, 'UTF-8')
}

function applyImport() {
  validationError.value = ''
  let str = ''
  if (inputMode.value === 'file') str = fileContent.value
  else str = pastedJson.value?.trim() || ''
  const parsed = parseAndValidate(str)
  if (parsed) {
    emit('import', JSON.stringify(parsed, null, 2))
    close()
  }
}

function close() {
  inputMode.value = 'choice'
  pastedJson.value = ''
  fileContent.value = ''
  fileError.value = ''
  pasteError.value = ''
  validationError.value = ''
  emit('update:modelValue', false)
}

watch(() => props.modelValue, (open) => {
  if (!open) return
  inputMode.value = 'choice'
  pastedJson.value = ''
  fileContent.value = ''
  fileError.value = ''
  pasteError.value = ''
  validationError.value = ''
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

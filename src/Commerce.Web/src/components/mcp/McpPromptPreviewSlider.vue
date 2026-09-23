<template>
  <Teleport to="body">
    <div
      v-if="modelValue"
      class="fixed inset-0 z-50 flex justify-end bg-black/50"
    >
      <div
        class="w-full h-full max-h-full sm:max-w-2xl bg-white dark:bg-gray-800 shadow-xl overflow-hidden flex flex-col animate-slide-in-right"
        @click.stop
      >
        <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between gap-2 shrink-0 min-h-[52px]">
          <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white truncate min-w-0">
            Preview: {{ prompt?.name ?? 'Prompt' }}
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
          <template v-if="!prompt">
            <p class="text-sm text-gray-500 dark:text-gray-400">No hay prompt para previsualizar.</p>
          </template>
          <template v-else>
            <div v-if="prompt.title" class="space-y-0.5">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Título</p>
              <p class="text-sm text-gray-900 dark:text-white">{{ prompt.title }}</p>
            </div>
            <div v-if="prompt.description" class="space-y-0.5">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Descripción</p>
              <p class="text-sm text-gray-600 dark:text-gray-300">{{ prompt.description }}</p>
            </div>

            <!-- Argumentos (input schema): ingresar valores; la sustitución en mensajes se hará vía backend -->
            <div v-if="argumentFields.length > 0" class="space-y-2">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Argumentos del prompt</p>
              <p class="text-xs text-gray-400 dark:text-gray-500">Valores para los placeholders del prompt. La sustitución en el contenido se realizará mediante el backend.</p>
              <div class="space-y-2">
                <div
                  v-for="arg in argumentFields"
                  :key="arg.name"
                  class="space-y-1"
                >
                  <label :for="'preview-arg-' + arg.name" class="block text-xs font-medium text-gray-600 dark:text-gray-400">
                    {{ arg.name }}
                    <span v-if="arg.required" class="text-red-500">*</span>
                    <span v-if="arg.description" class="font-normal text-gray-500 dark:text-gray-500"> — {{ arg.description }}</span>
                  </label>
                  <input
                    :id="'preview-arg-' + arg.name"
                    v-model="argumentValues[arg.name]"
                    type="text"
                    class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                    :placeholder="'ej: valor para {{' + arg.name + '}}'"
                  />
                </div>
              </div>
            </div>

            <div class="space-y-1.5">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Mensajes por rol</p>
              <div v-if="parsedBlocks.length === 0" class="text-sm text-gray-500 dark:text-gray-400 italic">Sin mensajes</div>
              <div v-else class="space-y-3">
                <div
                  v-for="(block, idx) in parsedBlocks"
                  :key="idx"
                  class="rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700/50 overflow-hidden"
                >
                  <div class="px-3 py-1.5 border-b border-gray-200 dark:border-gray-600 bg-gray-100 dark:bg-gray-700">
                    <span class="text-xs font-medium text-gray-700 dark:text-gray-300 capitalize">{{ block.role }}</span>
                  </div>
                  <div class="p-3">
                    <pre class="text-xs font-mono text-gray-800 dark:text-gray-200 whitespace-pre-wrap break-words">{{ block.content || '(vacío)' }}</pre>
                  </div>
                </div>
              </div>
            </div>

            <div v-if="(tools?.length ?? 0) > 0" class="space-y-1.5">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Tools permitidas</p>
              <p class="text-sm text-gray-600 dark:text-gray-300">
                <span v-if="allowedToolNames.length === 0">Todas las tools del MCP</span>
                <span v-else>{{ allowedToolNames.join(', ') }}</span>
              </p>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div v-if="hasInferenceParams" class="col-span-2 space-y-1.5">
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Parámetros de inferencia</p>
                <div class="flex flex-wrap gap-x-4 gap-y-1 text-sm text-gray-700 dark:text-gray-300">
                  <span v-if="prompt.temperature != null">Temperature: {{ prompt.temperature }}</span>
                  <span v-if="prompt.topP != null">Top P: {{ prompt.topP }}</span>
                  <span v-if="prompt.topK != null">Top K: {{ prompt.topK }}</span>
                  <span v-if="prompt.maxTokens != null">Max tokens: {{ prompt.maxTokens }}</span>
                </div>
              </div>
              <div class="space-y-0.5">
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Formato de salida</p>
                <p class="text-sm text-gray-900 dark:text-white">{{ outputFormatLabel }}</p>
              </div>
            </div>

            <div v-if="prompt.outputFormat === 1 && prompt.outputSchema" class="space-y-1.5">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Output schema</p>
              <div class="rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 p-3 max-h-40 overflow-auto">
                <pre class="text-xs font-mono text-gray-800 dark:text-gray-200 whitespace-pre-wrap break-words">{{ prompt.outputSchema }}</pre>
              </div>
            </div>

            <div class="pt-2 border-t border-gray-200 dark:border-gray-700 flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
              <span>v{{ prompt.version ?? 1 }}</span>
              <span v-if="prompt.isEnabled !== false" class="px-1.5 py-0.5 rounded bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300">Habilitado</span>
              <span v-else class="px-1.5 py-0.5 rounded bg-gray-200 dark:bg-gray-600 text-gray-600 dark:text-gray-400">Deshabilitado</span>
            </div>
          </template>
        </div>
        <div class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 shrink-0">
          <button
            type="button"
            class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600"
            @click="close"
          >
            Cerrar
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { computed, ref, watch } from 'vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  /** Prompt a previsualizar: { name, title, description, argumentsSchema?, messageBlocks, allowedToolIds, ... } */
  prompt: { type: Object, default: null },
  /** Lista de tools del MCP para resolver nombres de allowedToolIds */
  tools: { type: Array, default: () => [] }
})

const emit = defineEmits(['update:modelValue'])

/** Valores ingresados por el usuario para cada argumento (sustitución en contenido pendiente de endpoint backend) */
const argumentValues = ref({})

const argumentFields = computed(() => {
  const raw = props.prompt?.argumentsSchema
  if (!raw) return []
  try {
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
    const arr = Array.isArray(parsed) ? parsed : []
    return arr
      .filter(a => a && (a.name || a.Name))
      .map(a => ({
        name: a.name ?? a.Name ?? '',
        description: a.description ?? a.Description ?? '',
        required: !!a.required
      }))
  } catch {
    return []
  }
})

watch(
  () => [props.prompt?.argumentsSchema, props.modelValue],
  () => {
    const fields = argumentFields.value
    const next = {}
    fields.forEach(f => {
      next[f.name] = argumentValues.value[f.name] ?? ''
    })
    argumentValues.value = next
  },
  { immediate: true }
)

function close() {
  emit('update:modelValue', false)
}

const parsedBlocks = computed(() => {
  const raw = props.prompt?.messageBlocks
  if (!raw) return []
  if (typeof raw === 'string') {
    try {
      const arr = JSON.parse(raw)
      return Array.isArray(arr) ? arr.map(b => ({ role: b.role || 'user', content: b.content ?? '' })) : []
    } catch {
      return []
    }
  }
  return Array.isArray(raw) ? raw.map(b => ({ role: b.role || 'user', content: b.content ?? '' })) : []
})

const allowedToolNames = computed(() => {
  const ids = props.prompt?.allowedToolIds
  const list = props.tools ?? []
  if (!Array.isArray(ids) || ids.length === 0) return []
  const idSet = new Set(ids.map(id => String(id)))
  return list.filter(t => idSet.has(String(t.id))).map(t => t.name ?? t.title ?? t.id)
})

const hasInferenceParams = computed(() => {
  const p = props.prompt
  return p && (p.temperature != null || p.topP != null || p.topK != null || p.maxTokens != null)
})

const outputFormatLabel = computed(() => {
  const f = props.prompt?.outputFormat
  if (f === 1 || f === 'Structured') return 'Estructurado'
  return 'Texto'
})
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

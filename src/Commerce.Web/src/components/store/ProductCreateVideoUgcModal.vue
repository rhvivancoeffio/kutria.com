<template>
  <Teleport to="body">
    <Transition name="gen-modal">
      <div
        v-if="open"
        class="fixed inset-0 z-[120] flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50 sm:backdrop-blur-[2px]"
        role="presentation"
        @click.self="onBackdrop"
      >
        <div
          class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-2xl shadow-xl w-full max-w-lg max-h-[92vh] overflow-y-auto"
          role="dialog"
          aria-modal="true"
          :aria-labelledby="titleId"
          :aria-busy="phase === 'running' ? 'true' : 'false'"
          @click.stop
        >
          <!-- RUNNING -->
          <div v-if="phase === 'running'" class="p-5 sm:p-6">
            <div class="flex items-center gap-2.5 mb-2">
              <svg
                class="h-5 w-5 shrink-0 animate-spin text-gray-800 dark:text-gray-200"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
              <h2 :id="titleId" class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white">
                Generando guion UGC…
              </h2>
            </div>
            <p v-if="productName" class="text-sm text-slate-500 dark:text-slate-400 mb-2 truncate">
              Producto: <span class="font-medium text-slate-700 dark:text-slate-300">{{ productName }}</span>
            </p>
            <p class="text-sm text-gray-600 dark:text-gray-400">
              {{ statusText || 'La IA prepara hooks y guion para video corto.' }}
            </p>
          </div>

          <!-- SUCCESS -->
          <div v-else-if="phase === 'success'" class="p-5 sm:p-6 space-y-4">
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <h2 :id="titleId" class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white">
                  Guion UGC listo
                </h2>
                <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                  Stub v1 — script y hooks (sin render de video real).
                </p>
              </div>
              <button
                type="button"
                class="p-2 -m-2 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300"
                aria-label="Cerrar"
                @click="emit('close')"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div v-if="hooks.length" class="space-y-1.5">
              <p class="text-[11px] font-bold uppercase tracking-wide text-gray-400">Hooks</p>
              <ul class="space-y-1.5">
                <li
                  v-for="(h, i) in hooks"
                  :key="i"
                  class="text-sm text-gray-800 dark:text-gray-200 rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-100 dark:border-gray-700 px-3 py-2"
                >
                  {{ h }}
                </li>
              </ul>
            </div>

            <div v-if="script" class="space-y-1.5">
              <p class="text-[11px] font-bold uppercase tracking-wide text-gray-400">Script</p>
              <p
                class="text-sm text-gray-800 dark:text-gray-200 whitespace-pre-wrap rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-100 dark:border-gray-700 px-3 py-2.5 leading-relaxed"
              >
                {{ script }}
              </p>
            </div>

            <p v-if="jobId" class="text-[11px] text-gray-400 font-mono truncate">jobId: {{ jobId }}</p>

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-2 pt-1">
              <button
                type="button"
                class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
                @click="emit('regenerate')"
              >
                Regenerar
              </button>
              <button
                type="button"
                class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white"
                @click="emit('copy')"
              >
                Copiar guion
              </button>
            </div>
          </div>

          <!-- ERROR -->
          <div v-else class="p-5 sm:p-6 space-y-4">
            <h2 :id="titleId" class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white">
              No pudimos generar el UGC
            </h2>
            <p class="text-sm text-red-600 dark:text-red-400">{{ errorMessage || 'Error desconocido' }}</p>
            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-2">
              <button
                type="button"
                class="w-full sm:w-auto min-h-[44px] px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200"
                @click="emit('close')"
              >
                Cerrar
              </button>
              <button
                type="button"
                class="w-full sm:w-auto min-h-[44px] px-4 py-2.5 rounded-lg bg-primary-600 text-white text-sm font-medium"
                @click="emit('regenerate')"
              >
                Reintentar
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  open: { type: Boolean, default: false },
  phase: { type: String, default: 'running' },
  productName: { type: String, default: '' },
  statusText: { type: String, default: '' },
  errorMessage: { type: String, default: '' },
  video: { type: Object, default: null }
})

const emit = defineEmits(['close', 'regenerate', 'copy'])

const titleId = 'product-create-video-ugc-title'

const script = computed(() => {
  const s = props.video?.script
  return typeof s === 'string' ? s.trim() : ''
})

const jobId = computed(() => {
  const j = props.video?.jobId
  return typeof j === 'string' ? j.trim() : ''
})

const hooks = computed(() => {
  const h = props.video?.hooks
  if (!Array.isArray(h)) return []
  return h.map((x) => (typeof x === 'string' ? x.trim() : String(x ?? '').trim())).filter(Boolean)
})

function onBackdrop() {
  if (props.phase === 'running') return
  emit('close')
}
</script>

<style scoped>
.gen-modal-enter-active,
.gen-modal-leave-active {
  transition: opacity 0.18s ease;
}
.gen-modal-enter-from,
.gen-modal-leave-to {
  opacity: 0;
}
</style>

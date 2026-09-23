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
                IA mejorando tu contenido…
              </h2>
            </div>
            <p v-if="productName" class="text-sm text-slate-500 dark:text-slate-400 mb-5 truncate">
              Producto: <span class="font-medium text-slate-700 dark:text-slate-300">{{ productName }}</span>
            </p>

            <ul class="space-y-4">
              <li v-for="(s, idx) in steps" :key="s.id" class="flex gap-3">
                <span
                  class="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-[11px] font-bold transition-colors"
                  :class="stepIconClass(s.status)"
                >
                  <svg
                    v-if="s.status === 'done'"
                    class="w-3.5 h-3.5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                  </svg>
                  <template v-else>{{ idx + 1 }}</template>
                </span>
                <div class="min-w-0">
                  <p class="text-sm font-semibold transition-colors" :class="stepTitleClass(s.status)">
                    {{ s.title }}
                  </p>
                  <p class="mt-0.5 text-xs leading-snug transition-colors" :class="stepDetailClass(s.status)">
                    {{ s.detail }}
                  </p>
                </div>
              </li>
            </ul>

            <div class="mt-5 h-1.5 rounded-full bg-slate-200 dark:bg-slate-700 overflow-hidden">
              <div
                class="h-full rounded-full bg-emerald-500 transition-all duration-500 ease-out"
                :style="{ width: `${Math.min(100, Math.max(0, progress))}%` }"
              />
            </div>
          </div>

          <!-- SUCCESS -->
          <div v-else-if="phase === 'success'" class="p-5 sm:p-8">
            <div
              class="inline-flex items-center gap-2 max-w-full rounded-full border border-emerald-200 dark:border-emerald-800 bg-emerald-50 dark:bg-emerald-900/30 px-3 py-1.5"
            >
              <span class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-emerald-600 text-white">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                </svg>
              </span>
              <p class="text-sm font-medium text-emerald-800 dark:text-emerald-200 truncate">
                ¡Listo! Contenido mejorado para {{ detectedName }}
              </p>
            </div>

            <h2 :id="titleId" class="mt-5 text-xl sm:text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
              ¿Usar este contenido?
            </h2>
            <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">
              Revisamos nombre, descripción y bullets. Puedes aplicarlos o generar de nuevo.
            </p>

            <div
              v-if="previewDescription"
              class="mt-4 rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900/50 px-3.5 py-3"
            >
              <p class="text-[11px] font-semibold uppercase tracking-wide text-gray-400 mb-1">Vista previa</p>
              <p class="text-sm text-gray-700 dark:text-gray-300 line-clamp-3">{{ previewDescription }}</p>
            </div>

            <p
              v-if="previewBrandHint || previewCategoryHint"
              class="mt-3 text-xs text-gray-500 dark:text-gray-400"
            >
              <span v-if="previewBrandHint">Marca sugerida: {{ previewBrandHint }}</span>
              <span v-if="previewBrandHint && previewCategoryHint"> · </span>
              <span v-if="previewCategoryHint">Categoría sugerida: {{ previewCategoryHint }}</span>
            </p>

            <button
              type="button"
              class="mt-6 w-full inline-flex items-center justify-center gap-2 min-h-[48px] px-4 py-3 rounded-xl bg-gray-900 hover:bg-black dark:bg-white dark:hover:bg-gray-100 text-white dark:text-gray-900 text-sm font-semibold transition-colors"
              @click="emit('use-content')"
            >
              Usar contenido
            </button>

            <button
              type="button"
              class="mt-3 w-full inline-flex items-center justify-center gap-2 min-h-[44px] px-4 py-2.5 rounded-xl border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-800 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700/60 transition-colors"
              @click="emit('regenerate')"
            >
              Generar nuevo contenido con IA
            </button>

            <button
              type="button"
              class="mt-4 w-full text-center text-sm text-gray-500 dark:text-gray-400 underline underline-offset-2 hover:text-gray-800 dark:hover:text-gray-200"
              @click="emit('close')"
            >
              Cerrar
            </button>
          </div>

          <!-- ERROR -->
          <div v-else class="p-5 sm:p-8">
            <div
              class="inline-flex items-center gap-2 max-w-full rounded-full border border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-900/30 px-3 py-1.5"
            >
              <span class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-red-600 text-white">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </span>
              <p class="text-sm font-medium text-red-800 dark:text-red-200">No pudimos mejorar el contenido</p>
            </div>

            <h2 :id="titleId" class="mt-5 text-xl sm:text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
              Algo falló al generar
            </h2>
            <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">
              {{ errorMessage || 'Intenta de nuevo en unos segundos.' }}
            </p>

            <button
              type="button"
              class="mt-6 w-full inline-flex items-center justify-center gap-2 min-h-[48px] px-4 py-3 rounded-xl bg-gray-900 hover:bg-black dark:bg-white dark:hover:bg-gray-100 text-white dark:text-gray-900 text-sm font-semibold transition-colors"
              @click="emit('regenerate')"
            >
              Generar nuevo contenido con IA
            </button>

            <button
              type="button"
              class="mt-4 w-full text-center text-sm text-gray-500 dark:text-gray-400 underline underline-offset-2 hover:text-gray-800 dark:hover:text-gray-200"
              @click="emit('close')"
            >
              Cerrar
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
const props = defineProps({
  open: { type: Boolean, default: false },
  phase: { type: String, default: 'running' }, // running | success | error
  productName: { type: String, default: '' },
  steps: { type: Array, default: () => [] },
  progress: { type: Number, default: 0 },
  detectedName: { type: String, default: 'tu producto' },
  previewDescription: { type: String, default: '' },
  previewBrandHint: { type: String, default: '' },
  previewCategoryHint: { type: String, default: '' },
  errorMessage: { type: String, default: '' }
})

const emit = defineEmits(['use-content', 'regenerate', 'close'])

const titleId = 'product-create-content-improve-title'

function stepIconClass(status) {
  if (status === 'done') return 'bg-emerald-500 text-white border border-emerald-500'
  if (status === 'active') {
    return 'border-2 border-slate-400 dark:border-slate-500 text-slate-500 dark:text-slate-400 bg-transparent'
  }
  return 'border border-slate-300 dark:border-slate-600 text-slate-400 dark:text-slate-500 bg-transparent'
}

function stepTitleClass(status) {
  if (status === 'done') return 'text-emerald-700 dark:text-emerald-400'
  if (status === 'active') return 'text-slate-600 dark:text-slate-300'
  return 'text-slate-400 dark:text-slate-500'
}

function stepDetailClass(status) {
  if (status === 'done') return 'text-emerald-600/80 dark:text-emerald-500/80'
  if (status === 'active') return 'text-slate-500 dark:text-slate-400'
  return 'text-slate-400/80 dark:text-slate-600'
}

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

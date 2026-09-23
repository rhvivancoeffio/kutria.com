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
          class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-2xl shadow-xl w-full max-w-3xl max-h-[92vh] overflow-y-auto"
          role="dialog"
          aria-modal="true"
          :aria-labelledby="titleId"
          :aria-busy="phase === 'running' ? 'true' : 'false'"
          @click.stop
        >
          <!-- READY: user photo + start CTA -->
          <div v-if="phase === 'ready'" class="p-5 sm:p-8">
            <div class="flex flex-col sm:flex-row gap-5 sm:gap-6 items-start">
              <div
                class="w-full sm:w-[11.5rem] shrink-0 rounded-2xl border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 overflow-hidden aspect-square"
              >
                <img
                  v-if="imageSrc"
                  :src="imageSrc"
                  alt="Producto cargado"
                  class="w-full h-full object-contain p-3"
                />
                <div
                  v-else
                  class="w-full h-full flex items-center justify-center text-xs text-gray-400"
                >
                  Sin imagen
                </div>
              </div>
              <div class="flex-1 min-w-0">
                <h2 :id="titleId" class="text-xl sm:text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                  ¿Listo para analizar tu producto?
                </h2>
                <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">
                  La IA detectará el producto y generará nombre, descripción y detalles.
                </p>

                <ul class="mt-5 space-y-3">
                  <li v-for="(s, idx) in steps" :key="'ready-' + s.id" class="flex gap-3">
                    <span
                      class="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-[11px] font-bold border border-slate-300 dark:border-slate-600 text-slate-400 dark:text-slate-500 bg-transparent"
                    >
                      {{ idx + 1 }}
                    </span>
                    <div class="min-w-0">
                      <p class="text-sm font-semibold text-slate-400 dark:text-slate-500">{{ s.title }}</p>
                      <p class="mt-0.5 text-xs text-slate-400/80 dark:text-slate-600 leading-snug">{{ s.detail }}</p>
                    </div>
                  </li>
                </ul>

                <button
                  type="button"
                  class="mt-6 w-full inline-flex items-center justify-center gap-2 min-h-[48px] px-4 py-3 rounded-xl bg-gray-900 hover:bg-black dark:bg-white dark:hover:bg-gray-100 text-white dark:text-gray-900 text-sm font-semibold transition-colors"
                  @click="emit('start')"
                >
                  <svg class="w-4 h-4 shrink-0" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      d="M12 2l1.6 5.2L19 9l-5.2 1.6L12 16l-1.6-5.4L5 9l5.4-1.8L12 2zm6.5 11.5l.8 2.5 2.5.8-2.5.8-.8 2.5-.8-2.5-2.5-.8 2.5-.8.8-2.5z"
                    />
                  </svg>
                  Iniciar análisis
                </button>
                <button
                  type="button"
                  class="mt-4 w-full text-center text-sm text-gray-500 dark:text-gray-400 underline underline-offset-2 hover:text-gray-800 dark:hover:text-gray-200"
                  @click="emit('change-photo')"
                >
                  Cambiar foto
                </button>
              </div>
            </div>
          </div>

          <!-- RUNNING -->
          <div v-else-if="phase === 'running'" class="p-4 sm:p-6">
            <div class="flex flex-col sm:flex-row gap-5 sm:gap-6">
              <div
                class="sm:w-[11.5rem] shrink-0 rounded-2xl border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 overflow-hidden aspect-square"
              >
                <img
                  v-if="imageSrc"
                  :src="imageSrc"
                  alt="Producto cargado"
                  class="w-full h-full object-contain p-3"
                />
              </div>

              <div class="flex-1 min-w-0 flex flex-col">
                <div class="flex items-center gap-2.5 mb-4">
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
                    IA analizando tu producto…
                  </h2>
                </div>

                <ul class="space-y-4 flex-1">
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
                ¡Perfecto! Ya detectamos que es {{ detectedName }}
              </p>
            </div>

            <h2 :id="titleId" class="mt-5 text-xl sm:text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
              ¿Listo para usar este contenido?
            </h2>
            <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">
              Creamos nombre, descripción y detalles automáticamente.
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
              <svg class="w-4 h-4 shrink-0" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path
                  d="M12 2l1.6 5.2L19 9l-5.2 1.6L12 16l-1.6-5.4L5 9l5.4-1.8L12 2zm6.5 11.5l.8 2.5 2.5.8-2.5.8-.8 2.5-.8-2.5-2.5-.8 2.5-.8.8-2.5z"
                />
              </svg>
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
              @click="emit('change-photo')"
            >
              Cambiar foto
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
              <p class="text-sm font-medium text-red-800 dark:text-red-200">No pudimos generar el contenido</p>
            </div>

            <h2 :id="titleId" class="mt-5 text-xl sm:text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
              Algo falló al analizar la imagen
            </h2>
            <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">
              {{ errorMessage || 'Intenta de nuevo o cambia la foto.' }}
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
              @click="emit('change-photo')"
            >
              Cambiar foto
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
  phase: { type: String, default: 'ready' }, // ready | running | success | error
  imageSrc: { type: String, default: '' },
  steps: { type: Array, default: () => [] },
  progress: { type: Number, default: 8 },
  detectedName: { type: String, default: 'tu producto' },
  previewDescription: { type: String, default: '' },
  previewBrandHint: { type: String, default: '' },
  previewCategoryHint: { type: String, default: '' },
  errorMessage: { type: String, default: '' }
})

const emit = defineEmits(['start', 'use-content', 'regenerate', 'change-photo', 'close'])

const titleId = 'product-create-image-generate-title'

function stepIconClass(status) {
  if (status === 'done') return 'bg-emerald-500 text-white border border-emerald-500'
  if (status === 'active') {
    return 'border-2 border-slate-400 dark:border-slate-500 text-slate-500 dark:text-slate-400 bg-transparent'
  }
  // plomo / pending
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

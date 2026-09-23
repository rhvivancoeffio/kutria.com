<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-50 bg-black/50"
      aria-hidden="true"
      @click="onClose"
    />
    <div
      v-if="open"
      class="fixed top-0 right-0 z-[51] h-full w-full max-w-2xl bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
      role="dialog"
      aria-modal="true"
      :aria-labelledby="titleId"
    >
      <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 flex items-start justify-between gap-4 shrink-0">
        <div class="min-w-0 flex-1">
          <template v-if="loading">
            <div class="h-5 w-48 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
            <div class="mt-2 h-3 w-32 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          </template>
          <template v-else>
            <h2 :id="titleId" class="text-lg font-semibold text-gray-900 dark:text-white truncate">
              {{ title || 'Detalle' }}
            </h2>
            <p class="mt-1 text-sm text-gray-500 dark:text-gray-400 truncate">{{ subtitle }}</p>
          </template>
        </div>
        <button
          type="button"
          class="p-2 -m-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"
          aria-label="Cerrar"
          @click="onClose"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="flex-1 overflow-y-auto p-4 sm:p-6 min-h-0">
        <div v-if="loading" class="space-y-3" aria-busy="true">
          <div class="h-4 w-full rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-4 w-5/6 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-4 w-4/6 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-40 w-full rounded-lg bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-4 w-3/4 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-4 w-full rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
        <div
          v-else-if="error"
          class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800"
        >
          <p class="text-sm text-red-800 dark:text-red-300">{{ error }}</p>
        </div>
        <pre
          v-else
          class="text-xs whitespace-pre-wrap break-words text-gray-800 dark:text-gray-200 bg-gray-50 dark:bg-gray-900 rounded-lg p-3 border border-gray-200 dark:border-gray-700"
        >{{ prettyPayload }}</pre>
      </div>

      <div
        class="shrink-0 px-4 sm:px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex justify-end"
      >
        <button
          type="button"
          class="min-h-[44px] px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
          @click="onClose"
        >
          Cerrar
        </button>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  open: { type: Boolean, default: false },
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  title: { type: String, default: '' },
  subtitle: { type: String, default: '' },
  payloadJson: { type: String, default: '' }
})

const emit = defineEmits(['close'])

const titleId = 'store-detail-slider-title'

const prettyPayload = computed(() => {
  if (!props.payloadJson) return '{}'
  try {
    return JSON.stringify(JSON.parse(props.payloadJson), null, 2)
  } catch {
    return props.payloadJson
  }
})

function onClose() {
  emit('close')
}
</script>

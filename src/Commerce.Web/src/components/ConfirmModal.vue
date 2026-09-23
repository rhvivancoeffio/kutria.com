<template>
  <Teleport to="body">
    <div
      v-if="open"
      :class="[
        'fixed inset-0 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50',
        overlayZIndexClass,
        busy ? 'cursor-wait' : ''
      ]"
      role="presentation"
      @click.self="onCancel"
    >
      <div
        class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6 max-h-[85vh] overflow-y-auto"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        :aria-busy="busy ? 'true' : 'false'"
        @click.stop
      >
        <h3 :id="titleId" class="text-lg font-semibold text-gray-900 dark:text-white">
          {{ title }}
        </h3>
        <p class="mt-2 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          {{ message }}
        </p>
        <div class="mt-6 flex flex-col-reverse sm:flex-row justify-end gap-3">
          <button
            type="button"
            class="w-full sm:w-auto min-h-[44px] px-4 py-3 sm:py-2 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg font-medium transition-colors disabled:opacity-50"
            :disabled="busy"
            @click="onCancel"
          >
            Cancelar
          </button>
          <button
            type="button"
            :class="[
              'inline-flex items-center justify-center gap-2 w-full sm:w-auto min-h-[44px] px-4 py-3 sm:py-2 text-white rounded-lg font-medium transition-colors disabled:opacity-60 disabled:pointer-events-none disabled:cursor-not-allowed',
              variant === 'danger'
                ? 'bg-red-600 hover:bg-red-700'
                : 'bg-primary-600 hover:bg-primary-700'
            ]"
            :disabled="busy"
            :aria-busy="busy ? 'true' : 'false'"
            @click="emit('confirm')"
          >
            <svg
              v-if="busy"
              class="h-4 w-4 shrink-0 animate-spin text-white/90"
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
            {{ busy ? busyLabel : confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
const props = defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, default: 'Confirmar' },
  message: { type: String, default: '' },
  busy: { type: Boolean, default: false },
  confirmLabel: { type: String, default: 'Confirmar' },
  busyLabel: { type: String, default: 'Procesando…' },
  variant: { type: String, default: 'primary' },
  overlayZIndexClass: { type: String, default: 'z-[60]' }
})

const emit = defineEmits(['confirm', 'cancel'])

const titleId = 'confirm-modal-title'

function onCancel() {
  if (props.busy) return
  emit('cancel')
}
</script>

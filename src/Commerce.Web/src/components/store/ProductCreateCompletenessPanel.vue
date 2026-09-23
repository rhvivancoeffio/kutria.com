<template>
  <aside class="space-y-4">
    <div
      class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm p-4 sm:p-5"
    >
      <div class="flex items-center justify-between gap-2 mb-4">
        <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Validación</h2>
        <span
          class="text-[10px] font-bold tracking-wider uppercase px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-100 dark:bg-emerald-900/30 dark:text-emerald-300 dark:border-emerald-800"
        >
          Tiempo real
        </span>
      </div>

      <div class="flex items-center gap-4 mb-5">
        <div class="relative h-[4.5rem] w-[4.5rem] shrink-0">
          <svg class="h-[4.5rem] w-[4.5rem] -rotate-90" viewBox="0 0 36 36" aria-hidden="true">
            <circle
              cx="18"
              cy="18"
              r="15.5"
              fill="none"
              class="stroke-gray-100 dark:stroke-gray-700"
              stroke-width="3"
            />
            <circle
              cx="18"
              cy="18"
              r="15.5"
              fill="none"
              class="stroke-primary-600 transition-all duration-300"
              stroke-width="3"
              stroke-linecap="round"
              :stroke-dasharray="scoreDash"
            />
          </svg>
          <span
            class="absolute inset-0 flex items-center justify-center text-sm font-bold text-gray-900 dark:text-white"
          >
            {{ scorePercent }}
          </span>
        </div>
        <div class="min-w-0">
          <p class="text-sm font-semibold text-gray-900 dark:text-white">{{ scorePercent }}/100</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 leading-snug">{{ scoreHint }}</p>
        </div>
      </div>

      <ul class="space-y-2.5 mb-4">
        <li v-for="item in checklist" :key="item.id" class="flex items-start gap-2.5 text-[13px]">
          <span
            class="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-[10px] font-bold"
            :class="item.done ? 'bg-primary-600 text-white' : 'bg-gray-100 dark:bg-gray-700 text-gray-400'"
          >
            {{ item.done ? '✓' : '×' }}
          </span>
          <span :class="item.done ? 'text-gray-800 dark:text-gray-200' : 'text-gray-500 dark:text-gray-400'">
            {{ item.label }}
          </span>
        </li>
      </ul>

      <div
        class="rounded-xl border border-amber-200 dark:border-amber-800/60 bg-amber-50 dark:bg-amber-900/20 p-3 mb-4"
      >
        <p class="text-xs text-amber-900 dark:text-amber-100 leading-relaxed">
          <span class="font-semibold">Tip:</span> {{ tipText }}
        </p>
      </div>

      <button
        type="button"
        class="w-full min-h-[48px] rounded-xl text-sm font-semibold transition-colors inline-flex items-center justify-center gap-2 touch-manipulation"
        :class="
          canSubmit && !submitDisabled
            ? 'bg-primary-600 hover:bg-primary-700 text-white'
            : 'bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400 cursor-not-allowed'
        "
        :disabled="saving || !canSubmit || submitDisabled"
        @click="$emit('submit')"
      >
        <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
          />
        </svg>
        {{ saving ? savingLabel : submitLabel }}
      </button>
      <p class="mt-2 text-[11px] text-center text-gray-400 dark:text-gray-500">
        Se guarda en Gravity (tienda nativa del tenant).
      </p>
    </div>

    <div
      class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm p-4 sm:p-5"
    >
      <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">Checklist de ficha</h3>
      <dl class="space-y-2.5 text-xs">
        <div class="flex items-center justify-between gap-2">
          <dt class="text-gray-500 dark:text-gray-400">Título</dt>
          <dd class="font-medium text-gray-800 dark:text-gray-200">{{ titleChars }} caracteres</dd>
        </div>
        <div class="flex items-center justify-between gap-2">
          <dt class="text-gray-500 dark:text-gray-400">Descripción</dt>
          <dd class="font-medium text-gray-800 dark:text-gray-200">{{ descriptionChars }} caracteres</dd>
        </div>
        <div class="flex items-center justify-between gap-2">
          <dt class="text-gray-500 dark:text-gray-400">Imagen</dt>
          <dd class="font-medium text-gray-800 dark:text-gray-200">{{ hasImage ? '1 lista' : 'Pendiente' }}</dd>
        </div>
      </dl>
      <div
        class="mt-3 rounded-xl border border-emerald-200 dark:border-emerald-800/50 bg-emerald-50 dark:bg-emerald-900/20 p-3"
      >
        <p class="text-xs text-emerald-900 dark:text-emerald-100 leading-relaxed">
          Completa título, descripción e imagen para maximizar visibilidad en catálogo.
        </p>
      </div>
    </div>
  </aside>
</template>

<script setup>
defineProps({
  scorePercent: { type: Number, required: true },
  scoreDash: { type: String, required: true },
  scoreHint: { type: String, required: true },
  checklist: { type: Array, required: true },
  tipText: { type: String, required: true },
  titleChars: { type: Number, default: 0 },
  descriptionChars: { type: Number, default: 0 },
  hasImage: { type: Boolean, default: false },
  canSubmit: { type: Boolean, default: false },
  saving: { type: Boolean, default: false },
  submitDisabled: { type: Boolean, default: false },
  submitLabel: { type: String, default: 'Validar y publicar' },
  savingLabel: { type: String, default: 'Creando…' }
})

defineEmits(['submit'])
</script>

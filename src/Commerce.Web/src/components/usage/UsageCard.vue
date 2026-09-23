<template>
  <component
    :is="to ? 'router-link' : 'div'"
    :to="to"
    class="block p-4 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 transition-colors"
    :class="to ? 'card-hover cursor-pointer' : ''"
  >
    <div class="flex items-start justify-between gap-2">
      <div class="min-w-0 flex-1">
        <p class="text-sm font-medium text-gray-600 dark:text-gray-400 mb-2">{{ label }}</p>
        <p class="text-2xl font-bold text-gray-900 dark:text-white">
          {{ used }}
          <span v-if="limit != null" class="text-lg font-normal text-gray-500 dark:text-gray-400">/ {{ limit }}</span>
          <span v-else class="text-lg font-normal text-gray-500 dark:text-gray-400">/ ∞</span>
        </p>
        <div v-if="limit != null && limit > 0" class="mt-2 h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
          <div
            class="h-full rounded-full transition-all duration-300"
            :class="progressClass"
            :style="{ width: `${Math.min(100, (used / limit) * 100)}%` }"
          />
        </div>
      </div>
      <span v-if="to" class="shrink-0 text-primary-500 dark:text-primary-400 mt-1">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
        </svg>
      </span>
    </div>
  </component>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  label: { type: String, required: true },
  used: { type: Number, required: true },
  limit: { type: Number, default: null },
  /** Ruta a la que navegar al hacer clic (ej. /admin/usage). Si no se pasa, la card no es clickeable. */
  to: { type: [String, Object], default: null }
})

const progressClass = computed(() => {
  if (props.limit == null || props.limit <= 0) return 'bg-gray-400'
  const pct = (props.used / props.limit) * 100
  if (pct >= 100) return 'bg-red-500'
  if (pct >= 80) return 'bg-amber-500'
  return 'bg-primary-500'
})
</script>

<template>
  <div
    class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4"
    role="group"
    :aria-label="label"
  >
    <p class="text-xs font-medium text-gray-500 dark:text-gray-400">{{ label }}</p>
    <p class="mt-1 text-2xl font-semibold text-gray-900 dark:text-white tabular-nums">{{ displayValue }}</p>
    <p v-if="hint" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ hint }}</p>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  label: { type: String, required: true },
  value: { type: [String, Number], default: '—' },
  hint: { type: String, default: '' }
})

const displayValue = computed(() =>
  props.value === null || props.value === undefined || props.value === '' ? '—' : props.value
)
</script>

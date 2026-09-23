<template>
  <div>
    <h2 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">
      {{ title }}
    </h2>
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4">
      <button
        v-for="item in integrations"
        :key="item.id"
        type="button"
        class="text-left rounded-xl border p-4 sm:p-5 transition-colors min-h-[88px]"
        :class="
          item.id === modelValue
            ? 'border-primary-500 bg-primary-50/60 dark:bg-primary-900/20 ring-1 ring-primary-500'
            : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-primary-300 dark:hover:border-primary-600'
        "
        @click="$emit('update:modelValue', item.id)"
      >
        <div class="flex items-start gap-3 min-w-0">
          <div
            class="w-11 h-11 rounded-lg bg-gray-50 dark:bg-gray-700 flex items-center justify-center shrink-0 overflow-hidden p-1.5"
          >
            <img
              v-if="item.logoUrl"
              :src="item.logoUrl"
              :alt="item.provider || item.name"
              class="max-h-full max-w-full object-contain"
            />
            <span v-else class="text-xs font-semibold text-gray-500">{{ initials(item) }}</span>
          </div>
          <div class="min-w-0 flex-1">
            <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">
              {{ item.name || item.provider }}
            </p>
            <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400 truncate">
              {{ item.provider }}
            </p>
            <span
              v-if="item.id === modelValue"
              class="mt-2 inline-flex text-xs font-medium text-primary-700 dark:text-primary-300"
            >
              Seleccionada
            </span>
          </div>
        </div>
      </button>
    </div>
  </div>
</template>

<script setup>
defineProps({
  modelValue: { type: String, default: '' },
  integrations: { type: Array, default: () => [] },
  title: { type: String, default: 'Selecciona una conexión' }
})

defineEmits(['update:modelValue'])

function initials(item) {
  const label = item?.name || item?.provider || '?'
  return String(label).slice(0, 2).toUpperCase()
}
</script>

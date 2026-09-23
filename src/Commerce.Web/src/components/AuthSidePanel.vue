<template>
  <aside
    class="flex-1 hidden lg:flex flex-col justify-center px-12 xl:px-20 py-16 bg-gradient-to-br from-primary-50 to-primary-100/50 dark:from-primary-900/30 dark:to-primary-800/20 border-l border-primary-100 dark:border-primary-800/50"
  >
    <div class="max-w-md">
      <h3 class="text-2xl xl:text-3xl font-bold text-gray-900 dark:text-white mb-4">
        {{ title }}
      </h3>
      <p class="text-gray-600 dark:text-gray-300 mb-8 text-lg leading-relaxed">
        {{ description }}
      </p>
      <ul class="space-y-4">
        <li
          v-for="(item, i) in features"
          :key="i"
          class="flex items-center gap-3 text-gray-700 dark:text-gray-300"
        >
          <span class="flex-shrink-0 w-8 h-8 rounded-full bg-primary-500/20 dark:bg-primary-500/30 flex items-center justify-center text-primary-600 dark:text-primary-400">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </span>
          {{ item }}
        </li>
      </ul>
    </div>
  </aside>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  description: { type: String, required: true },
  features: { type: Array, default: () => [] }
})
</script>

<template>
  <div class="space-y-3" aria-busy="true" aria-label="Cargando">
    <!-- Mobile cards -->
    <div class="md:hidden space-y-3">
      <div
        v-for="i in rows"
        :key="`m-${i}`"
        class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 space-y-2"
      >
        <div class="h-4 w-2/3 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-1/2 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
      </div>
    </div>
    <!-- Desktop table -->
    <div class="hidden md:block rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 overflow-hidden">
      <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex gap-4">
        <div class="h-3 w-16 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-24 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-20 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
      </div>
      <div
        v-for="i in rows"
        :key="`d-${i}`"
        class="px-4 py-3 border-b border-gray-100 dark:border-gray-700/60 last:border-0 flex gap-4"
      >
        <div class="h-4 w-28 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-4 w-40 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-4 w-20 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-4 w-24 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  rows: { type: Number, default: 6 }
})
</script>

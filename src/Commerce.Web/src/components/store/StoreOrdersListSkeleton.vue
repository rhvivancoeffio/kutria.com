<template>
  <div class="space-y-3" aria-busy="true" aria-label="Cargando ventas">
    <div class="md:hidden space-y-3">
      <div
        v-for="i in rows"
        :key="`m-${i}`"
        class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 space-y-2"
      >
        <div class="h-4 w-40 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-28 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-48 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="flex gap-2">
          <div class="h-5 w-10 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-5 w-10 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
      </div>
    </div>

    <div class="hidden md:block rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 overflow-hidden">
      <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 grid grid-cols-[1.2fr_0.9fr_0.9fr_1fr_0.9fr_0.5fr_0.7fr_0.7fr] gap-3">
        <div v-for="i in 8" :key="i" class="h-3 w-16 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
      </div>
      <div
        v-for="i in rows"
        :key="`d-${i}`"
        class="px-4 py-4 border-b border-gray-100 dark:border-gray-700/60 last:border-0 grid grid-cols-[1.2fr_0.9fr_0.9fr_1fr_0.9fr_0.5fr_0.7fr_0.7fr] gap-3 items-start"
      >
        <div class="space-y-2">
          <div class="h-4 w-36 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="flex gap-1.5">
            <div class="h-5 w-10 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
            <div class="h-5 w-10 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          </div>
        </div>
        <div class="space-y-2">
          <div class="h-3 w-20 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-24 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
        <div class="space-y-2">
          <div class="h-3 w-20 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-24 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
        <div class="space-y-2">
          <div class="h-3 w-24 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-16 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
        <div class="h-3 w-28 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-6 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-16 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-16 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  rows: { type: Number, default: 6 }
})
</script>

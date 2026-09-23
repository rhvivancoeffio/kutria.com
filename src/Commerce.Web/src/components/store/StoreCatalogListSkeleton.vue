<template>
  <div class="space-y-3" aria-busy="true" aria-label="Cargando catálogo">
    <div class="md:hidden space-y-3">
      <div
        v-for="i in rows"
        :key="`m-${i}`"
        class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 flex gap-3"
      >
        <div class="h-16 w-16 rounded-lg bg-gray-200 dark:bg-gray-700 animate-pulse shrink-0" />
        <div class="flex-1 space-y-2 min-w-0">
          <div class="h-4 w-2/3 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-1/2 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-3/4 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
      </div>
    </div>

    <div class="hidden md:block rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 overflow-hidden">
      <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 grid grid-cols-[72px_1fr_80px_120px] gap-4">
        <div class="h-3 w-12 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-20 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="h-3 w-12 rounded bg-gray-200 dark:bg-gray-700 animate-pulse mx-auto" />
        <div class="h-3 w-14 rounded bg-gray-200 dark:bg-gray-700 animate-pulse ml-auto" />
      </div>
      <div
        v-for="i in rows"
        :key="`d-${i}`"
        class="px-4 py-4 border-b border-gray-100 dark:border-gray-700/60 last:border-0 grid grid-cols-[72px_1fr_80px_120px] gap-4 items-center"
      >
        <div class="h-14 w-14 rounded-lg bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div class="space-y-2 min-w-0">
          <div class="h-4 w-48 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-40 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-56 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-3 w-36 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
        <div class="h-4 w-8 rounded bg-gray-200 dark:bg-gray-700 animate-pulse mx-auto" />
        <div class="space-y-2 ml-auto w-20">
          <div class="h-3 w-full rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
          <div class="h-4 w-full rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  rows: { type: Number, default: 6 }
})
</script>

<template>
  <div class="space-y-4" role="status" aria-live="polite" aria-busy="true">
    <div
      v-for="g in groupCount"
      :key="`perm-skel-group-${g}`"
      class="border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden"
    >
      <div class="flex items-center gap-3 px-3 py-3 border-b border-gray-200 dark:border-gray-600 bg-gray-50/90 dark:bg-gray-900/50">
        <div class="h-4 w-4 rounded bg-gray-200 dark:bg-gray-700 animate-pulse shrink-0" />
        <div class="h-4 w-28 sm:w-36 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
      </div>
      <div class="p-3 space-y-3">
        <div
          v-for="r in rowsForGroup(g)"
          :key="`perm-skel-row-${g}-${r}`"
          class="flex items-start gap-3"
        >
          <div
            class="shrink-0 mt-0.5 bg-gray-200 dark:bg-gray-700 animate-pulse"
            :class="variant === 'viewer' ? 'h-4 w-4 rounded-full' : 'h-4 w-4 rounded'"
          />
          <div class="min-w-0 flex-1 space-y-2">
            <div
              class="h-4 bg-gray-200 dark:bg-gray-700 rounded animate-pulse"
              :class="r === 1 ? 'w-20' : 'w-28'"
            />
            <div class="h-3 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
            <div
              v-if="r !== 2 || g % 2 === 0"
              class="h-3 w-4/5 max-w-md bg-gray-200 dark:bg-gray-700 rounded animate-pulse"
            />
          </div>
        </div>
      </div>
    </div>
    <span class="sr-only">Cargando permisos…</span>
  </div>
</template>

<script setup>
defineProps({
  /** Número de bloques de recurso (solo Integraciones en template). */
  groupCount: {
    type: Number,
    default: 1
  },
  /** checklist = casilla; viewer = indicador circular. */
  variant: {
    type: String,
    default: 'checklist',
    validator: (v) => ['checklist', 'viewer'].includes(v)
  }
})

/** Variación ligera de filas por grupo para que no se vea repetitivo. */
function rowsForGroup(g) {
  return 2
}
</script>

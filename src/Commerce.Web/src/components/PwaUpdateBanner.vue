<template>
  <Transition
    enter-active-class="transition duration-300 ease-out"
    enter-from-class="opacity-0 translate-y-4 sm:translate-y-0 sm:translate-x-4"
    enter-to-class="opacity-100 translate-y-0 sm:translate-x-0"
    leave-active-class="transition duration-200 ease-in"
    leave-from-class="opacity-100 translate-y-0"
    leave-to-class="opacity-0 translate-y-2"
  >
    <div
      v-if="pwaStore.needRefresh || pwaStore.offlineReady"
      class="fixed bottom-4 left-4 right-4 z-[9999] sm:left-auto sm:right-4 sm:max-w-sm overflow-hidden rounded-xl border shadow-xl"
      :class="pwaStore.needRefresh
        ? 'border-primary-500/50 bg-gradient-to-br from-primary-50 to-primary-100/80 dark:from-primary-900/50 dark:to-primary-800/30 dark:border-primary-500/30'
        : 'border-emerald-500/30 bg-gradient-to-br from-emerald-50 to-white dark:from-emerald-900/20 dark:to-gray-800 dark:border-emerald-500/20'"
    >
      <div class="flex items-start gap-4 p-4">
        <div
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full"
          :class="pwaStore.needRefresh
            ? 'bg-primary-500/20 text-primary-600 dark:text-primary-400'
            : 'bg-emerald-500/20 text-emerald-600 dark:text-emerald-400'"
        >
          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <template v-if="pwaStore.needRefresh">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </template>
            <template v-else>
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </template>
          </svg>
        </div>
        <div class="min-w-0 flex-1">
          <p class="text-sm font-semibold" :class="pwaStore.needRefresh ? 'text-primary-900 dark:text-primary-100' : 'text-gray-900 dark:text-gray-100'">
            {{ pwaStore.needRefresh ? 'Nueva versión disponible' : 'Listo para usar sin conexión' }}
          </p>
          <p class="mt-0.5 text-sm" :class="pwaStore.needRefresh ? 'text-primary-700/80 dark:text-primary-200/80' : 'text-gray-600 dark:text-gray-400'">
            {{ pwaStore.needRefresh ? 'Recarga para obtener las últimas mejoras.' : 'La app ya está disponible offline.' }}
          </p>
          <div class="mt-3 flex gap-2">
            <template v-if="pwaStore.needRefresh">
              <button
                type="button"
                @click="pwaStore.applyUpdate"
                class="rounded-lg bg-primary-600 px-3 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-700"
              >
                Recargar ahora
              </button>
              <button
                type="button"
                @click="pwaStore.dismissUpdate"
                class="rounded-lg px-3 py-2 text-sm font-medium text-primary-700 transition-colors hover:bg-primary-100 dark:text-primary-300 dark:hover:bg-primary-900/40"
              >
                Más tarde
              </button>
            </template>
            <template v-else>
              <button
                type="button"
                @click="pwaStore.dismissOfflineReady"
                class="rounded-lg bg-emerald-600 px-3 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-emerald-700 dark:bg-emerald-500 dark:hover:bg-emerald-600"
              >
                Entendido
              </button>
            </template>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { usePwaStore } from '../stores/pwaStore'

const pwaStore = usePwaStore()
</script>

<template>
  <div
    class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
  >
    <div
      class="mx-auto flex h-14 w-14 items-center justify-center rounded-xl bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-300"
    >
      <svg class="h-7 w-7" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
        />
      </svg>
    </div>
    <h2 class="mt-4 text-lg font-semibold text-gray-900 dark:text-white">
      {{ title }}
    </h2>
    <p class="mt-2 text-sm sm:text-base text-gray-600 dark:text-gray-400 max-w-md mx-auto">
      {{ message }}
    </p>
    <router-link
      :to="integrationsPath"
      class="mt-6 inline-flex items-center justify-center min-h-[44px] px-4 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium"
    >
      {{ ctaLabel }}
    </router-link>
  </div>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    default: 'Sin conexión de integración'
  },
  message: {
    type: String,
    default: 'Conecta una integración de tienda o API Gravity para consultar catálogo y ventas.'
  },
  ctaLabel: {
    type: String,
    default: 'Ir a Integraciones'
  },
  integrationsPath: {
    type: String,
    default: '/admin/integraciones'
  }
})
</script>

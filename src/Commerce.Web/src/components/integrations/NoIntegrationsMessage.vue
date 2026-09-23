<template>
  <p :class="classNames">
    No hay integraciones. Conecta una en <router-link :to="integrationsPath" class="text-primary-600 dark:text-primary-400 underline hover:no-underline">Integraciones</router-link>.
  </p>
</template>

<script setup>
defineProps({
  /** Ruta a la página de integraciones */
  integrationsPath: {
    type: String,
    default: '/admin/integraciones'
  },
  /** Clases CSS para el párrafo (ej: text-sm, text-amber-600) */
  classNames: {
    type: String,
    default: 'text-sm text-gray-600 dark:text-gray-400'
  }
})
</script>

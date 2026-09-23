<template>
  <p
    v-if="message"
    :class="classNames"
  >
    {{ message }}
    <router-link
      v-if="showBillingLink"
      to="/admin/billing"
      class="underline hover:no-underline"
    >
      {{ billingLinkText }}
    </router-link>
  </p>
</template>

<script setup>
defineProps({
  /** Mensaje principal a mostrar */
  message: {
    type: String,
    default: null
  },
  /** Mostrar enlace a billing */
  showBillingLink: {
    type: Boolean,
    default: true
  },
  /** Texto del enlace a billing */
  billingLinkText: {
    type: String,
    default: 'Actualiza tu plan'
  },
  /** Clases CSS adicionales (ej: text-sm, mt-1) */
  classNames: {
    type: String,
    default: 'text-sm text-amber-600 dark:text-amber-400'
  }
})
</script>

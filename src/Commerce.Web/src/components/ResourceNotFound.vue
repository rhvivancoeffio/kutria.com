<template>
  <div class="resource-not-found">
    <div class="resource-not-found__icon-wrap">
      <div class="resource-not-found__icon-bg">
        <svg class="resource-not-found__icon" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </div>
    </div>
    <h2 class="resource-not-found__title">
      No existe
    </h2>
    <p class="resource-not-found__message">
      {{ message }}
    </p>
    <div class="resource-not-found__actions">
      <router-link
        :to="backRoute"
        class="resource-not-found__btn inline-flex items-center justify-center gap-2 px-5 py-2.5 bg-primary-600 hover:bg-primary-700 text-white font-semibold text-sm rounded-xl shadow-sm hover:shadow-md hover:scale-[1.02] active:scale-[0.98] transition-all duration-200"
      >
        <svg class="resource-not-found__btn-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
        {{ backLabel }}
      </router-link>
    </div>
  </div>
</template>

<script setup>
defineProps({
  /** Mensaje descriptivo (ej: "Este agente no existe o ha sido eliminado.") */
  message: {
    type: String,
    required: true
  },
  /** Ruta para volver al listado */
  backRoute: {
    type: String,
    required: true
  },
  /** Texto del botón de volver (ej: "Volver a agentes") */
  backLabel: {
    type: String,
    required: true
  }
})
</script>

<style scoped>
.resource-not-found {
  @apply rounded-2xl overflow-hidden;
  background: linear-gradient(135deg, rgb(255 251 235 / 0.9) 0%, rgb(254 243 199 / 0.5) 50%, rgb(253 230 138 / 0.3) 100%);
  border: 1px solid rgb(253 224 71 / 0.4);
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.05), 0 2px 4px -2px rgb(0 0 0 / 0.05);
}

:global(.dark) .resource-not-found {
  background: linear-gradient(135deg, rgb(120 53 15 / 0.25) 0%, rgb(146 64 14 / 0.15) 50%, rgb(161 98 7 / 0.1) 100%);
  border-color: rgb(180 83 9 / 0.4);
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.2), 0 2px 4px -2px rgb(0 0 0 / 0.15);
}

.resource-not-found__icon-wrap {
  @apply flex justify-center pt-10 pb-4;
}

.resource-not-found__icon-bg {
  @apply flex items-center justify-center rounded-full;
  width: 5.5rem;
  height: 5.5rem;
  background: linear-gradient(145deg, rgb(253 224 71 / 0.6), rgb(250 204 21 / 0.4));
  box-shadow: 0 8px 16px -4px rgb(253 224 71 / 0.3), inset 0 1px 0 rgb(255 255 255 / 0.5);
}

:global(.dark) .resource-not-found__icon-bg {
  background: linear-gradient(145deg, rgb(180 83 9 / 0.5), rgb(146 64 14 / 0.4));
  box-shadow: 0 8px 16px -4px rgb(0 0 0 / 0.3), inset 0 1px 0 rgb(255 255 255 / 0.05);
}

.resource-not-found__icon {
  @apply text-amber-700;
  width: 2.5rem;
  height: 2.5rem;
}

:global(.dark) .resource-not-found__icon {
  @apply text-amber-400;
}

.resource-not-found__title {
  @apply text-xl font-bold text-gray-900 text-center mb-2 px-6;
}

:global(.dark) .resource-not-found__title {
  @apply text-white;
}

.resource-not-found__message {
  @apply text-gray-600 text-center max-w-md mx-auto px-6 text-[15px] leading-relaxed;
}

.resource-not-found__actions {
  @apply flex justify-center pb-10 pt-2;
}

:global(.dark) .resource-not-found__message {
  @apply text-gray-400;
}

.resource-not-found__btn-icon {
  width: 1rem;
  height: 1rem;
}
</style>

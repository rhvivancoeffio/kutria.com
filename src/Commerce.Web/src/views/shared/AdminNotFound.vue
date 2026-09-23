<template>
  <div class="py-8 px-4">
    <div class="admin-not-found">
      <div class="admin-not-found__icon-wrap">
        <span class="admin-not-found__code">404</span>
        <div class="admin-not-found__icon-bg">
          <svg class="admin-not-found__icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>
      </div>
      <h2 class="admin-not-found__title">Página no encontrada</h2>
      <p class="admin-not-found__message">
        Esta sección del panel no existe. Revisa la URL o usa los enlaces para navegar.
      </p>
      <div class="admin-not-found__actions">
        <router-link
          to="/admin/usage"
          class="admin-not-found__btn admin-not-found__btn--primary"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
          Ir al panel
        </router-link>
        <button type="button" @click="goBack" class="admin-not-found__btn admin-not-found__btn--secondary">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Volver atrás
        </button>
      </div>
      <div class="admin-not-found__links">
        <span class="admin-not-found__links-label">Secciones disponibles:</span>
        <div class="admin-not-found__links-list">
          <router-link to="/admin/usage" class="admin-not-found__link">Uso</router-link>
          <router-link to="/admin/integraciones" class="admin-not-found__link">Integraciones</router-link>
          <router-link to="/admin/webhook-monitor" class="admin-not-found__link">Webhooks</router-link>
          <router-link to="/admin/profile" class="admin-not-found__link">Perfil</router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()

const goBack = () => {
  if (window.history.length > 1) {
    router.go(-1)
  } else {
    router.push('/admin/usage')
  }
}
</script>

<style scoped>
.admin-not-found {
  @apply max-w-md mx-auto rounded-2xl overflow-hidden;
  background: linear-gradient(135deg, rgb(239 246 255 / 0.95) 0%, rgb(219 234 254 / 0.6) 100%);
  border: 1px solid rgb(147 197 253 / 0.5);
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.06);
}

:global(.dark) .admin-not-found {
  background: linear-gradient(135deg, rgb(30 58 138 / 0.25) 0%, rgb(30 64 175 / 0.15) 100%);
  border-color: rgb(59 130 246 / 0.4);
}

.admin-not-found__icon-wrap {
  @apply flex items-center justify-center gap-3 pt-6 pb-2;
}

.admin-not-found__code {
  @apply text-2xl font-bold text-primary-600 dark:text-primary-400;
}

.admin-not-found__icon-bg {
  @apply flex items-center justify-center rounded-full;
  width: 2.5rem;
  height: 2.5rem;
  background: rgb(147 197 253 / 0.5);
}

:global(.dark) .admin-not-found__icon-bg {
  background: rgb(59 130 246 / 0.4);
}

.admin-not-found__icon {
  @apply text-primary-600 dark:text-primary-400;
  width: 1.25rem;
  height: 1.25rem;
}

.admin-not-found__title {
  @apply text-lg font-semibold text-gray-900 dark:text-white text-center mb-1 px-5;
}

.admin-not-found__message {
  @apply text-sm text-gray-600 dark:text-gray-400 text-center mb-5 px-5 leading-relaxed;
}

.admin-not-found__actions {
  @apply flex flex-wrap justify-center gap-2 px-5 pb-4;
}

.admin-not-found__btn {
  @apply inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg transition-all duration-200;
}

.admin-not-found__btn--primary {
  @apply bg-primary-600 hover:bg-primary-700 text-white shadow-sm hover:shadow;
}

.admin-not-found__btn--secondary {
  @apply bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:border-primary-400 dark:hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400;
}

.admin-not-found__links {
  @apply border-t border-blue-200/60 dark:border-blue-800/50 px-5 py-3 flex flex-wrap items-center justify-center gap-2;
}

.admin-not-found__links-label {
  @apply text-xs text-gray-500 dark:text-gray-400;
}

.admin-not-found__links-list {
  @apply flex flex-wrap gap-2;
}

.admin-not-found__link {
  @apply px-3 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-400 bg-white/60 dark:bg-gray-800/60 rounded-md border border-blue-200/50 dark:border-blue-800/50 hover:border-primary-400 dark:hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400 transition-colors;
}
</style>

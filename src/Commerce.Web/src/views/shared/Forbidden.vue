<template>
  <div class="py-8 px-4">
  <div class="forbidden-card">
    <div class="forbidden-card__icon-wrap">
      <span class="forbidden-card__code">403</span>
      <div class="forbidden-card__icon-bg">
        <svg class="forbidden-card__icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
        </svg>
      </div>
    </div>
    <h2 class="forbidden-card__title">Acceso denegado</h2>
    <p class="forbidden-card__message">
      No tienes permisos para acceder a esta página. Si crees que es un error, contacta al administrador.
    </p>
    <div class="forbidden-card__actions">
      <router-link
        to="/admin/usage"
        class="forbidden-card__btn forbidden-card__btn--primary"
      >
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
        </svg>
        Ir al panel
      </router-link>
      <button type="button" @click="goBack" class="forbidden-card__btn forbidden-card__btn--secondary">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
        Volver atrás
      </button>
    </div>
    <div class="forbidden-card__links">
      <span class="forbidden-card__links-label">Acceso permitido:</span>
      <div class="forbidden-card__links-list">
        <router-link to="/admin/usage" class="forbidden-card__link">Uso</router-link>
        <router-link to="/admin/integraciones" class="forbidden-card__link">Integraciones</router-link>
        <router-link to="/admin/profile" class="forbidden-card__link">Perfil</router-link>
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
.forbidden-card {
  @apply max-w-md mx-auto rounded-2xl overflow-hidden;
  background: linear-gradient(135deg, rgb(255 251 235 / 0.95) 0%, rgb(254 243 199 / 0.6) 100%);
  border: 1px solid rgb(253 224 71 / 0.5);
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.06);
}

:global(.dark) .forbidden-card {
  background: linear-gradient(135deg, rgb(120 53 15 / 0.3) 0%, rgb(146 64 14 / 0.2) 100%);
  border-color: rgb(180 83 9 / 0.5);
}

.forbidden-card__icon-wrap {
  @apply flex items-center justify-center gap-3 pt-6 pb-2;
}

.forbidden-card__code {
  @apply text-2xl font-bold text-amber-600 dark:text-amber-400;
}

.forbidden-card__icon-bg {
  @apply flex items-center justify-center rounded-full;
  width: 2.5rem;
  height: 2.5rem;
  background: rgb(253 224 71 / 0.5);
}

:global(.dark) .forbidden-card__icon-bg {
  background: rgb(180 83 9 / 0.5);
}

.forbidden-card__icon {
  @apply text-amber-700 dark:text-amber-400;
  width: 1.25rem;
  height: 1.25rem;
}

.forbidden-card__title {
  @apply text-lg font-semibold text-gray-900 dark:text-white text-center mb-1 px-5;
}

.forbidden-card__message {
  @apply text-sm text-gray-600 dark:text-gray-400 text-center mb-5 px-5 leading-relaxed;
}

.forbidden-card__actions {
  @apply flex flex-wrap justify-center gap-2 px-5 pb-4;
}

.forbidden-card__btn {
  @apply inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg transition-all duration-200;
}

.forbidden-card__btn--primary {
  @apply bg-primary-600 hover:bg-primary-700 text-white shadow-sm hover:shadow;
}

.forbidden-card__btn--secondary {
  @apply bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:border-primary-400 dark:hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400;
}

.forbidden-card__links {
  @apply border-t border-amber-200/60 dark:border-amber-800/50 px-5 py-3 flex flex-wrap items-center justify-center gap-2;
}

.forbidden-card__links-label {
  @apply text-xs text-gray-500 dark:text-gray-400;
}

.forbidden-card__links-list {
  @apply flex flex-wrap gap-2;
}

.forbidden-card__link {
  @apply px-3 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-400 bg-white/60 dark:bg-gray-800/60 rounded-md border border-amber-200/50 dark:border-amber-800/50 hover:border-primary-400 dark:hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400 transition-colors;
}
</style>

<template>
  <div class="p-6 max-w-xl">
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">
        {{ hasPassword ? 'Cambiar Contraseña' : 'Configurar Contraseña' }}
      </h1>
      <p class="text-gray-600 dark:text-gray-400 mt-1">
        {{ hasPassword
          ? 'Actualiza tu contraseña de acceso.'
          : 'Configura una contraseña para poder iniciar sesión con email y para desvincular proveedores sociales.' }}
      </p>
    </div>

    <div v-if="loading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
    </div>

    <section v-else class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-b border-gray-200 dark:border-gray-700">
        <h2 class="text-lg font-semibold text-gray-900 dark:text-white">
          {{ hasPassword ? 'Nueva contraseña' : 'Contraseña' }}
        </h2>
        <p class="text-sm text-gray-600 dark:text-gray-400">
          {{ hasPassword
            ? 'Ingresa tu contraseña actual y la nueva contraseña.'
            : 'Ingresa la contraseña que usarás para iniciar sesión con tu email.' }}
        </p>
      </div>
      <form id="password-form" @submit.prevent="handleSubmit">
        <div class="p-6 space-y-4">
          <div v-if="hasPassword">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Contraseña actual <span class="text-red-500">*</span></label>
            <input
              v-model="form.currentPassword"
              type="password"
              :required="hasPassword"
              class="input-field w-full"
              placeholder="••••••••"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              {{ hasPassword ? 'Nueva contraseña' : 'Contraseña' }} <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.newPassword"
              type="password"
              required
              :minlength="isDev ? 1 : 8"
              class="input-field w-full"
              placeholder="••••••••"
            />
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">{{ isDev ? 'Mínimo 1 carácter (dev)' : 'Mínimo 8 caracteres' }}</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Confirmar contraseña <span class="text-red-500">*</span></label>
            <input
              v-model="form.confirmPassword"
              type="password"
              required
              class="input-field w-full"
              placeholder="••••••••"
            />
          </div>
          <p v-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
          <p v-if="success" class="text-sm text-green-600 dark:text-green-400">
            {{ hasPassword ? 'Contraseña actualizada correctamente.' : 'Contraseña configurada correctamente. Ya puedes desvincular proveedores sociales.' }}
          </p>
        </div>
        <div class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex justify-end gap-3">
          <router-link to="/admin/profile" class="btn-secondary">
            Cancelar
          </router-link>
          <button
            type="submit"
            :disabled="submitting"
            class="btn-primary"
          >
            {{ submitting ? 'Guardando...' : (hasPassword ? 'Cambiar contraseña' : 'Configurar contraseña') }}
          </button>
        </div>
      </form>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import apiService from '../../services/api'

const isDev = import.meta.env.DEV
const loading = ref(true)
const hasPassword = ref(true)

const form = ref({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const submitting = ref(false)
const error = ref(null)
const success = ref(false)

async function loadProfile() {
  loading.value = true
  try {
    const profile = await apiService.getProfile()
    hasPassword.value = profile?.hasPassword ?? true
  } catch {
    hasPassword.value = true
  } finally {
    loading.value = false
  }
}

async function handleSubmit() {
  if (form.value.newPassword !== form.value.confirmPassword) {
    error.value = 'Las contraseñas no coinciden'
    return
  }
  const minLen = isDev ? 1 : 8
  if (form.value.newPassword.length < minLen) {
    error.value = isDev ? 'La contraseña debe tener al menos 1 carácter' : 'La contraseña debe tener al menos 8 caracteres'
    return
  }

  submitting.value = true
  error.value = null
  success.value = false
  try {
    if (hasPassword.value) {
      await apiService.changePassword(form.value.currentPassword, form.value.newPassword)
    } else {
      await apiService.setPassword(form.value.newPassword)
      hasPassword.value = true
    }
    success.value = true
    form.value = { currentPassword: '', newPassword: '', confirmPassword: '' }
    setTimeout(() => { success.value = false }, 3000)
  } catch (err) {
    error.value = err.response?.data?.error || err.message || (hasPassword.value ? 'Error al cambiar la contraseña' : 'Error al configurar la contraseña')
  } finally {
    submitting.value = false
  }
}

onMounted(loadProfile)
</script>

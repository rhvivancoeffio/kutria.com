<template>
  <div class="p-6 max-w-4xl">
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Mi Perfil</h1>
      <p class="text-gray-600 dark:text-gray-400 mt-1">Gestiona y actualiza tu información personal.</p>
    </div>

    <div v-if="loading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
    </div>

    <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 mb-6">
      <p class="text-red-800 dark:text-red-300">{{ error }}</p>
    </div>

    <template v-else>
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
        <!-- Card header -->
        <div class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-b border-gray-200 dark:border-gray-700">
          <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Información de Usuario</h2>
          <p class="text-sm text-gray-600 dark:text-gray-400">ID: {{ profile?.userId }}</p>
        </div>

        <!-- Edit mode: form -->
        <div v-if="editing">
          <div class="p-6 space-y-6">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre para mostrar</label>
                <input
                  v-model="updateForm.displayName"
                  type="text"
                  class="input-field w-full"
                  placeholder="Tu nombre"
                />
              </div>
            </div>
            <p v-if="updateError" class="text-sm text-red-600 dark:text-red-400">{{ updateError }}</p>
            <p v-if="updateSuccess" class="text-sm text-green-600 dark:text-green-400">Perfil actualizado correctamente.</p>
          </div>
          <div class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex justify-end gap-3">
            <button type="button" @click="cancelEdit" class="btn-secondary">Cancelar</button>
            <button
              type="button"
              @click="handleUpdateProfile"
              :disabled="updating"
              class="btn-primary"
            >
              {{ updating ? 'Guardando...' : 'Guardar' }}
            </button>
          </div>
        </div>

        <!-- View mode: key-value grid -->
        <div v-else class="p-6">
          <dl class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <dt class="text-sm text-gray-500 dark:text-gray-400">Correo electrónico</dt>
              <dd class="text-gray-900 dark:text-white font-medium mt-0.5">{{ profile?.email || '—' }}</dd>
            </div>
            <div>
              <dt class="text-sm text-gray-500 dark:text-gray-400">Nombre para mostrar</dt>
              <dd class="text-gray-900 dark:text-white font-medium mt-0.5">{{ profile?.displayName || '—' }}</dd>
            </div>
            <div>
              <dt class="text-sm text-gray-500 dark:text-gray-400">Miembro desde</dt>
              <dd class="text-gray-900 dark:text-white font-medium mt-0.5">{{ formatDate(profile?.createdAt) }}</dd>
            </div>
            <template v-if="profile?.partnerId || profile?.partnerSpace || profile?.isPartnerUser || profile?.isPartnerStaff">
              <div class="md:col-span-2 pt-2 border-t border-gray-100 dark:border-gray-700/80">
                <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Partner</h3>
              </div>
              <div v-if="profile?.partnerSpace">
                <dt class="text-sm text-gray-500 dark:text-gray-400">Space</dt>
                <dd class="text-gray-900 dark:text-white font-medium mt-0.5 font-mono text-sm">{{ profile.partnerSpace }}</dd>
              </div>
              <div v-if="profile?.partnerId">
                <dt class="text-sm text-gray-500 dark:text-gray-400">Partner ID</dt>
                <dd class="text-gray-900 dark:text-white font-medium mt-0.5 font-mono text-xs break-all">{{ profile.partnerId }}</dd>
              </div>
              <div>
                <dt class="text-sm text-gray-500 dark:text-gray-400">Rol programa partner</dt>
                <dd class="text-gray-900 dark:text-white font-medium mt-0.5">{{ profile.isPartnerUser ? 'Sí' : 'No' }}</dd>
              </div>
              <div>
                <dt class="text-sm text-gray-500 dark:text-gray-400">Staff de partner</dt>
                <dd class="text-gray-900 dark:text-white font-medium mt-0.5">{{ profile.isPartnerStaff ? 'Sí' : 'No' }}</dd>
              </div>
            </template>
          </dl>

          <div v-if="socialLogins?.length" class="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
            <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Inicios de sesión vinculados</h3>
            <div class="flex flex-wrap gap-3">
              <div
                v-for="provider in socialLogins"
                :key="provider"
                class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-gray-100 dark:bg-gray-700 border border-gray-200 dark:border-gray-600"
              >
                <span class="font-medium text-gray-900 dark:text-white">{{ providerLabel(provider) }}</span>
                <button
                  type="button"
                  @click="handleUnlink(provider)"
                  :disabled="unlinking === provider || (socialLogins.length === 1 && !hasPassword)"
                  class="text-sm text-red-600 dark:text-red-400 hover:underline disabled:opacity-50 disabled:cursor-not-allowed"
                  :title="socialLogins.length === 1 && !hasPassword ? 'Configura una contraseña antes de desvincular' : 'Desvincular'"
                >
                  {{ unlinking === provider ? 'Desvinculando...' : 'Desvincular' }}
                </button>
              </div>
            </div>
            <p v-if="socialLogins.length === 1 && !hasPassword" class="mt-2 text-xs text-amber-600 dark:text-amber-400">
              Para desvincular, primero configura una contraseña en Configurar contraseña.
            </p>
          </div>

          <div class="mt-6 flex flex-wrap gap-4">
            <button
              type="button"
              @click="editing = true"
              class="inline-flex items-center gap-1.5 text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
              </svg>
              Editar
            </button>
            <router-link
              to="/admin/change-password"
              class="inline-flex items-center gap-1.5 text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
              </svg>
              {{ hasPassword ? 'Cambiar contraseña' : 'Configurar contraseña' }}
            </router-link>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'

const toast = useToast()
const profile = ref(null)
const loading = ref(true)
const error = ref(null)
const editing = ref(false)

const updateForm = ref({ displayName: '' })
const updating = ref(false)
const updateError = ref(null)
const updateSuccess = ref(false)
const unlinking = ref(null)

const socialLogins = computed(() => profile.value?.socialLogins ?? [])
const hasPassword = computed(() => profile.value?.hasPassword ?? false)

function formatDate(dateStr) {
  if (!dateStr) return '—'
  try {
    const d = new Date(dateStr)
    return d.toLocaleDateString('es', { year: 'numeric', month: 'long', day: 'numeric' })
  } catch {
    return dateStr
  }
}

function cancelEdit() {
  editing.value = false
  updateForm.value.displayName = profile.value?.displayName || ''
  updateError.value = null
}

async function loadProfile() {
  loading.value = true
  error.value = null
  try {
    profile.value = await apiService.getProfile()
    updateForm.value.displayName = profile.value.displayName || ''
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al cargar el perfil'
  } finally {
    loading.value = false
  }
}

async function handleUpdateProfile() {
  updating.value = true
  updateError.value = null
  updateSuccess.value = false
  try {
    await apiService.updateProfile(updateForm.value.displayName || null)
    profile.value = { ...profile.value, displayName: updateForm.value.displayName }
    updateSuccess.value = true
    editing.value = false
    setTimeout(() => { updateSuccess.value = false }, 3000)
  } catch (err) {
    updateError.value = err.response?.data?.error || err.message || 'Error al actualizar el perfil'
  } finally {
    updating.value = false
  }
}

function providerLabel(provider) {
  const labels = { Google: 'Google', GitHub: 'GitHub' }
  return labels[provider] ?? provider
}

async function handleUnlink(provider) {
  if (socialLogins.value.length === 1 && !hasPassword.value) {
    toast.warning('Configura una contraseña antes de desvincular el único método de inicio de sesión.')
    return
  }
  unlinking.value = provider
  try {
    await apiService.unlinkSocialLogin(provider)
    profile.value = {
      ...profile.value,
      socialLogins: profile.value.socialLogins.filter(p => p !== provider)
    }
    toast.success(`${providerLabel(provider)} desvinculado correctamente.`)
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'Error al desvincular')
  } finally {
    unlinking.value = null
  }
}

onMounted(loadProfile)
</script>

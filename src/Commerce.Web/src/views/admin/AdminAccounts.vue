<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Accounts</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Lista de cuentas (solo SuperAdmin). Haz clic en Impersonar para actuar como otra cuenta.
          </p>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="space-y-3 sm:space-y-4">
        <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <!-- Forbidden / 401 -->
      <div
        v-else-if="forbidden"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-amber-800 dark:text-amber-300">
          Solo los SuperAdmin pueden acceder a esta página.
        </p>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
      </div>

      <template v-else>
        <!-- Vista móvil: tarjetas apiladas -->
        <div class="md:hidden space-y-3">
          <div
            v-for="acc in accounts"
            :key="acc.id"
            class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm card-hover"
          >
            <div class="flex flex-col gap-2 min-w-0">
              <p class="text-sm font-medium text-gray-900 dark:text-white truncate" :title="acc.ownerEmail || '-'">
                {{ acc.ownerEmail || '-' }}
              </p>
              <dl class="grid grid-cols-2 gap-x-3 gap-y-1 text-xs sm:text-sm">
                <div class="min-w-0">
                  <dt class="text-gray-500 dark:text-gray-400">Plan</dt>
                  <dd class="text-gray-900 dark:text-gray-100 truncate">{{ acc.planKey }}</dd>
                </div>
                <div class="min-w-0">
                  <dt class="text-gray-500 dark:text-gray-400">Creado</dt>
                  <dd class="text-gray-900 dark:text-gray-100">{{ formatDate(acc.createdAt) }}</dd>
                </div>
                <div class="min-w-0">
                  <dt class="text-gray-500 dark:text-gray-400">Trial</dt>
                  <dd class="text-gray-900 dark:text-gray-100">{{ acc.trialExpiresAt ? formatDate(acc.trialExpiresAt) : '-' }}</dd>
                </div>
                <div class="min-w-0">
                  <dt class="text-gray-500 dark:text-gray-400">Suscripción</dt>
                  <dd>
                    <span
                      v-if="acc.hasActiveSubscription"
                      class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300"
                    >
                      Activa
                    </span>
                    <span v-else class="text-gray-500 dark:text-gray-400">-</span>
                  </dd>
                </div>
              </dl>
              <p class="text-xs font-mono text-gray-500 dark:text-gray-400 truncate" :title="acc.id">{{ acc.id }}</p>
              <div class="pt-2 border-t border-gray-200 dark:border-gray-600">
                <button
                  type="button"
                  :disabled="impersonatingId === acc.id"
                  @click="doImpersonate(acc.id)"
                  class="w-full min-h-[44px] inline-flex items-center justify-center gap-1 px-3 py-2.5 text-sm font-medium text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed touch-manipulation"
                >
                  {{ impersonatingId === acc.id ? 'Impersonando...' : 'Impersonar' }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Vista tablet/desktop: tabla -->
        <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-x-auto">
          <table v-resizable class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-900/50">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">ID</th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Correo</th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Plan</th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Creado</th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Trial</th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Suscripción</th>
                <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Acciones</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="acc in accounts"
                :key="acc.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/50"
              >
                <td class="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100 truncate max-w-[120px]" :title="acc.id">
                  {{ acc.id }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-700 dark:text-gray-300 truncate max-w-[180px]" :title="acc.ownerEmail || '-'">
                  {{ acc.ownerEmail || '-' }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{{ acc.planKey }}</td>
                <td class="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{{ formatDate(acc.createdAt) }}</td>
                <td class="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{{ acc.trialExpiresAt ? formatDate(acc.trialExpiresAt) : '-' }}</td>
                <td class="px-4 py-3">
                  <span
                    v-if="acc.hasActiveSubscription"
                    class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300"
                  >
                    Activa
                  </span>
                  <span v-else class="text-gray-500 dark:text-gray-400 text-sm">-</span>
                </td>
                <td class="px-4 py-3 text-right">
                  <button
                    type="button"
                    :disabled="impersonatingId === acc.id"
                    @click="doImpersonate(acc.id)"
                    class="inline-flex items-center gap-1 px-3 py-1.5 text-sm font-medium text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {{ impersonatingId === acc.id ? 'Impersonando...' : 'Impersonar' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="!accounts.length" class="text-center py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
          <p class="text-gray-500 dark:text-gray-400">No hay cuentas.</p>
        </div>
      </template>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'

const toast = useToast()

const loading = ref(true)
const forbidden = ref(false)
const error = ref(null)
const accounts = ref([])
const impersonatingId = ref(null)

function formatDate(d) {
  if (!d) return '-'
  const date = typeof d === 'string' ? new Date(d) : d
  return date.toLocaleDateString('es-ES', { dateStyle: 'short' })
}

async function load() {
  loading.value = true
  error.value = null
  forbidden.value = false
  try {
    const result = await apiService.listAccounts()
    accounts.value = result?.accounts ?? []
  } catch (err) {
    if (err.response?.status === 401 || err.response?.status === 403) {
      forbidden.value = true
    } else {
      error.value = err.response?.data?.error || err.message || 'Error al cargar cuentas'
    }
  } finally {
    loading.value = false
  }
}

async function doImpersonate(accountId) {
  impersonatingId.value = accountId
  try {
    const data = await apiService.impersonate(accountId)
    if (data?.token) {
      apiService.setToken(data.token)
      toast.success('Impersonación iniciada. Recargando...')
      window.location.href = '/admin/usage'
    } else {
      error.value = 'No se recibió el token'
    }
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al impersonar'
    toast.error(error.value)
  } finally {
    impersonatingId.value = null
  }
}

onMounted(load)
</script>

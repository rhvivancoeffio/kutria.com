<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col items-center justify-center px-4 py-8">
    <div class="max-w-md w-full">
      <div class="flex flex-col items-center mb-8">
        <div class="flex items-center justify-center gap-4 mb-6">
          <KutriaMark size="md" class="text-gray-900 dark:text-white" />
        </div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white text-center">
          Autorizar <span class="text-primary-600 dark:text-primary-400">{{ appName }}</span>
        </h1>
        <p class="mt-2 text-sm text-gray-600 dark:text-gray-400 text-center">
          Esta app podrá usar las herramientas MCP de tu tenant Kutria.
        </p>
      </div>

      <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div v-if="error" class="p-4 bg-red-50 dark:bg-red-900/20 text-red-700 dark:text-red-300 text-sm">
          {{ error }}
        </div>

        <div v-else-if="loading" class="p-12 text-center">
          <div class="w-10 h-10 border-4 border-primary-500 border-t-transparent rounded-full animate-spin mx-auto"></div>
          <p class="mt-4 text-gray-600 dark:text-gray-400">Verificando sesión...</p>
        </div>

        <template v-else>
          <div class="p-5 border-b border-gray-100 dark:border-gray-700">
            <div class="flex items-center gap-4">
              <div class="w-12 h-12 rounded-full bg-primary-100 dark:bg-primary-900/40 flex items-center justify-center shrink-0">
                <span class="text-lg font-semibold text-primary-600 dark:text-primary-400">{{ userInitials }}</span>
              </div>
              <div class="min-w-0">
                <p class="font-semibold text-gray-900 dark:text-white truncate">{{ displayName }}</p>
                <p class="text-sm text-gray-500 dark:text-gray-400 truncate">{{ email }}</p>
              </div>
            </div>
          </div>

          <div class="p-5 space-y-4">
            <p class="text-sm text-gray-600 dark:text-gray-400">
              {{ appName }} solicita acceso a las herramientas de catálogo, pedidos y operaciones expuestas por MCP.
            </p>
            <div class="flex gap-3">
              <button
                type="button"
                @click="deny"
                :disabled="submitting"
                class="flex-1 px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 font-medium hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50"
              >
                Cancelar
              </button>
              <button
                type="button"
                @click="approve"
                :disabled="submitting"
                class="flex-1 px-4 py-3 rounded-xl bg-primary-600 hover:bg-primary-700 disabled:opacity-50 text-white font-medium transition-colors shadow-sm"
              >
                {{ submitting ? 'Autorizando...' : 'Autorizar' }}
              </button>
            </div>
            <p class="text-xs text-gray-500 dark:text-gray-400 text-center">
              Serás redirigido a <span class="font-mono break-all">{{ redirectHost }}</span>
            </p>
          </div>
        </template>
      </div>

      <p class="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
        ¿No eres tú?
        <a
          :href="`/sign-in?returnUrl=${encodeURIComponent(route.fullPath)}`"
          class="text-primary-600 dark:text-primary-400 hover:underline font-medium"
        >
          Usar otra cuenta
        </a>
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import apiService from '../../services/api'
import KutriaMark from '../../components/kutria/KutriaMark.vue'
import { parseJwtPayload } from '../../utils/jwtUtils'

const route = useRoute()
const loading = ref(true)
const submitting = ref(false)
const error = ref(null)

const params = ref({
  client_id: '',
  client_name: '',
  redirect_uri: '',
  state: '',
  code_challenge: '',
  code_challenge_method: 'S256',
  scope: '',
  resource: ''
})

const payload = computed(() => parseJwtPayload())
const email = computed(() => payload.value?.email || '')
const displayName = computed(() => email.value || 'Usuario')
const userInitials = computed(() => {
  const name = displayName.value
  return (name[0] || 'U').toUpperCase()
})

const appName = computed(() => {
  const fromBackend = params.value.client_name?.trim()
  if (fromBackend) return fromBackend
  const uri = params.value.redirect_uri || ''
  if (uri.includes('chatgpt.com')) return 'ChatGPT'
  if (uri.includes('claude.ai')) return 'Claude'
  if (uri.includes('anthropic.com')) return 'Anthropic'
  if (uri.includes('cursor')) return 'Cursor'
  return 'la aplicación'
})

const redirectHost = computed(() => {
  try {
    return new URL(params.value.redirect_uri || '').origin
  } catch {
    return params.value.redirect_uri || ''
  }
})

onMounted(() => {
  params.value = {
    client_id: route.query.client_id || '',
    client_name: route.query.client_name || '',
    redirect_uri: route.query.redirect_uri || '',
    state: route.query.state || '',
    code_challenge: route.query.code_challenge || '',
    code_challenge_method: route.query.code_challenge_method || 'S256',
    scope: route.query.scope || '',
    resource: route.query.resource || ''
  }
  if (!params.value.client_id || !params.value.redirect_uri) {
    error.value = 'Parámetros OAuth incompletos. Vuelve a intentar desde la aplicación que solicitó la conexión.'
    loading.value = false
    return
  }
  if (!localStorage.getItem('auth_token')) {
    window.location.href = `/sign-in?returnUrl=${encodeURIComponent(route.fullPath)}`
    return
  }
  loading.value = false
})

async function approve() {
  submitting.value = true
  error.value = null
  try {
    const { redirectUrl } = await apiService.completeMcpOAuthAuthorize(params.value)
    window.location.href = redirectUrl
  } catch (e) {
    error.value = e.response?.data?.error_description || e.message || 'Error al autorizar'
    submitting.value = false
  }
}

function deny() {
  const url = new URL(params.value.redirect_uri)
  url.searchParams.set('error', 'access_denied')
  url.searchParams.set('error_description', 'User denied access')
  if (params.value.state) url.searchParams.set('state', params.value.state)
  window.location.href = url.toString()
}
</script>

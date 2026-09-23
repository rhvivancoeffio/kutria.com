<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center px-4">
    <div class="text-center">
      <div v-if="loading" class="animate-pulse">
        <div class="w-12 h-12 border-4 border-primary-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
        <p class="text-gray-600 dark:text-gray-400">Completando autorización...</p>
      </div>
      <div v-else-if="error" class="space-y-4">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <router-link
          to="/admin/integraciones"
          class="inline-block px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700"
        >
          Volver a integraciones
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { useAppStore } from '../../stores/appStore'
import { getTabForProvider } from '../../composables/useIntegrationTab'
import { eventBus, MY_INTEGRATIONS_UPDATED } from '../../utils/eventBus'

const router = useRouter()
const appStore = useAppStore()
const route = useRoute()
const toast = useToast()

const loading = ref(true)
const error = ref(null)

onMounted(async () => {
  const code = route.query.code
  const state = route.query.state

  if (!code || !state) {
    error.value = 'Código o state no recibidos. Intenta el flujo de autorización de nuevo.'
    loading.value = false
    return
  }

  const stateParts = state.split('|')
  const integrationId = stateParts.length >= 1 ? stateParts[0] : null
  if (!integrationId) {
    error.value = 'State inválido. Intenta el flujo de autorización de nuevo.'
    loading.value = false
    return
  }

  const baseUrl = window.location.origin
  const redirectUri = `${baseUrl}${route.path}`

  const callbackQueryParams = Object.fromEntries(
    Object.entries(route.query).filter(([, v]) => v != null && v !== '')
  )

  try {
    await apiService.integrationOAuthCallback({
      integrationId,
      code,
      state,
      redirectUri,
      callbackQueryParams
    })
    toast.success('Integración autorizada correctamente.')
    const [integration, available, my] = await Promise.all([
      apiService.getIntegrationById(integrationId),
      appStore.fetchAvailableIntegrations(),
      apiService.getMyIntegrations()
    ])
    eventBus.emit(MY_INTEGRATIONS_UPDATED, { my })
    const tab = getTabForProvider(integration?.provider, available ?? [])
    const target = tab ? { path: '/admin/integraciones', query: { tab } } : '/admin/integraciones'
    router.push(target)
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al completar la autorización.'
    loading.value = false
  }
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center px-4">
    <div class="text-center">
      <div v-if="loading" class="animate-pulse">
        <div class="w-12 h-12 border-4 border-primary-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
        <p class="text-gray-600 dark:text-gray-400">Completando inicio de sesión...</p>
      </div>
      <div v-else-if="error" class="space-y-4">
        <p class="text-red-600 dark:text-red-400 font-medium">{{ error }}</p>
        <router-link
          to="/sign-in"
          class="inline-block px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700"
        >
          Volver a iniciar sesión
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
import { clearPartnerSignupCookieClient } from '../../utils/partnerCookie'

const router = useRouter()
const route = useRoute()
const toast = useToast()

const loading = ref(true)
const error = ref(null)

const TOOLS_RETURN_PATH_KEY = 'tools_return_path'

onMounted(async () => {
  const provider = route.params.provider
  const code = route.query.code
  const state = route.query.state

  // Central IdP callback (password or social): Identity redirects here with token in URL, no code exchange
  if (provider === 'identity') {
    const hash = window.location.hash
    const tokenFromQuery = route.query.token
    const hashQuery = hash ? hash.slice(1) : ''
    const tokenFromHash = hashQuery ? new URLSearchParams(hashQuery).get('token') : null
    const token = tokenFromQuery || tokenFromHash
    if (token) {
      apiService.setToken(token)
      try {
        await apiService.provisionIdentity()
      } catch (err) {
        error.value = err.response?.data?.error || err.message || 'Error al completar el registro.'
        loading.value = false
        return
      }
      clearPartnerSignupCookieClient()
      toast.success('Sesión iniciada.')
      let target = '/admin'
      try {
        const saved = sessionStorage.getItem(TOOLS_RETURN_PATH_KEY)
        if (saved && saved.startsWith('/')) {
          sessionStorage.removeItem(TOOLS_RETURN_PATH_KEY)
          target = saved
        }
      } catch (_) {}
      loading.value = false
      router.replace(target)
    } else {
      error.value = 'No se recibió el token. Intenta iniciar sesión de nuevo.'
      loading.value = false
    }
    return
  }

  // Social login (google, github): code exchange with API
  const returnUrl = state ? decodeURIComponent(state) : '/admin'
  if (!provider || !code) {
    error.value = 'Código de autorización no recibido. Intenta de nuevo.'
    loading.value = false
    return
  }

  // Accept invitation flow: redirect to invite page with code so it can call accept API
  if (returnUrl.startsWith('/invite/')) {
    const sep = returnUrl.includes('?') ? '&' : '?'
    window.location.href = `${returnUrl}${sep}code=${encodeURIComponent(code)}&provider=${encodeURIComponent(provider)}`
    return
  }

  const baseUrl = window.location.origin
  const redirectUri = `${baseUrl}/auth/${provider}/callback`

  try {
    const data = await apiService.socialLoginCallback(provider, code, redirectUri)
    apiService.setToken(data.token)
    clearPartnerSignupCookieClient()
    toast.success('Sesión iniciada.')
    router.push(returnUrl)
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al completar el inicio de sesión.'
    loading.value = false
  }
})
</script>

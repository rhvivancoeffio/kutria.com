<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col lg:flex-row">
    <section
      class="flex-1 flex flex-col justify-center px-6 sm:px-12 lg:px-16 py-12 lg:py-16 bg-white dark:bg-gray-800"
    >
      <div class="max-w-sm mx-auto w-full">
        <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Aceptar invitación
        </h2>
        <p class="text-gray-600 dark:text-gray-400 mb-6">
          Te han invitado a unirte a un equipo. Completa tus datos para continuar.
        </p>

        <div v-if="loading" class="flex justify-center py-8">
          <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600"></div>
        </div>

        <div v-else-if="invitationError" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
          <p class="text-red-800 dark:text-red-300">{{ invitationError }}</p>
          <router-link to="/sign-in" class="mt-3 inline-block text-sm text-primary-600 dark:text-primary-400 hover:underline">
            Ir a iniciar sesión
          </router-link>
        </div>

        <template v-else>
          <div v-if="invitation?.email" class="mb-6 p-3 rounded-lg bg-primary-50 dark:bg-primary-900/20 border border-primary-200 dark:border-primary-800">
            <p class="text-sm text-primary-700 dark:text-primary-300">
              Invitación para: <strong>{{ invitation.email }}</strong>
            </p>
          </div>

          <!-- Social login -->
          <div class="flex flex-col gap-3 mb-6">
            <button
              type="button"
              @click="handleSocialLogin('google')"
              class="w-full flex items-center justify-center gap-3 py-3 px-4 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-200 font-medium hover:bg-gray-50 dark:hover:bg-gray-600 transition-colors"
            >
              <svg class="w-5 h-5" viewBox="0 0 24 24">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
              </svg>
              Continuar con Google
            </button>
            <button
              type="button"
              @click="handleSocialLogin('github')"
              class="w-full flex items-center justify-center gap-3 py-3 px-4 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-200 font-medium hover:bg-gray-50 dark:hover:bg-gray-600 transition-colors"
            >
              <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/>
              </svg>
              Continuar con GitHub
            </button>
          </div>

          <div class="flex items-center gap-4 my-6">
            <span class="flex-1 h-px bg-gray-200 dark:bg-gray-600"></span>
            <span class="text-sm text-gray-500 dark:text-gray-400">o</span>
            <span class="flex-1 h-px bg-gray-200 dark:bg-gray-600"></span>
          </div>

          <!-- Email/password form -->
          <form @submit.prevent="handleSubmit" class="space-y-5">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre</label>
              <input
                v-model="form.displayName"
                type="text"
                class="input-field w-full"
                placeholder="Tu nombre"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Correo electrónico <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.email"
                type="email"
                required
                :class="['input-field w-full', touched.email && !form.email && '!border-red-500 dark:!border-red-500']"
                placeholder="tu@empresa.com"
                @blur="touched.email = true"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Contraseña <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.password"
                type="password"
                required
                :minlength="isDev ? 1 : 8"
                :class="['input-field w-full', touched.password && !form.password && '!border-red-500 dark:!border-red-500']"
                :placeholder="isDev ? 'Mínimo 1 carácter (dev)' : 'Mínimo 8 caracteres'"
                @blur="touched.password = true"
              />
            </div>
            <p v-if="submitError" class="text-sm text-red-600 dark:text-red-400">{{ submitError }}</p>
            <button
              type="submit"
              :disabled="submitting"
              :class="[
                'w-full py-3 rounded-lg font-semibold transition-colors',
                submitting
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-primary-600 hover:bg-primary-700 text-white'
              ]"
            >
              {{ submitting ? 'Aceptando...' : 'Aceptar invitación' }}
            </button>
          </form>
        </template>

        <p class="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
          ¿Ya tienes cuenta?
          <router-link to="/sign-in" class="text-primary-600 dark:text-primary-400 hover:underline font-medium">
            Iniciar sesión
          </router-link>
        </p>
      </div>
    </section>

    <AuthSidePanel
      title="Únete al equipo"
      description="Colabora con tu equipo en la gestión de tu ecommerce con AI."
      :features="[
        'Acceso compartido a agentes y configuraciones',
        'Reportes y análisis en tiempo real',
        'Monitor de órdenes colaborativo'
      ]"
    />
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import AuthSidePanel from '../../components/AuthSidePanel.vue'
import apiService from '../../services/api'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const token = ref('')
const invitation = ref(null)
const loading = ref(true)
const invitationError = ref(null)
const isDev = import.meta.env.DEV
const form = reactive({ displayName: '', email: '', password: '' })
const touched = reactive({ email: false, password: false })
const submitting = ref(false)
const submitError = ref(null)

async function loadInvitation() {
  const t = route.params.token
  if (!t) {
    invitationError.value = 'Enlace de invitación no válido.'
    loading.value = false
    return
  }
  token.value = t
  loading.value = true
  invitationError.value = null
  try {
    const data = await apiService.getInvitationByToken(t)
    if (!data) {
      invitationError.value = 'Invitación no encontrada o expirada.'
      return
    }
    if (data.status !== 'Pending') {
      invitationError.value = 'Esta invitación ya no es válida.'
      return
    }
    if (new Date(data.expiresAt) < new Date()) {
      invitationError.value = 'Esta invitación ha expirado.'
      return
    }
    invitation.value = data
    if (data.email) form.email = data.email
  } catch (err) {
    if (err.response?.status === 404) {
      invitationError.value = 'Invitación no encontrada.'
    } else {
      invitationError.value = err.response?.data?.error || 'Error al cargar la invitación.'
    }
  } finally {
    loading.value = false
  }
}

async function handleSubmit() {
  if (!form.email?.trim()) {
    submitError.value = 'El correo es obligatorio.'
    return
  }
  const minLen = isDev ? 1 : 8
  if (!form.password || form.password.length < minLen) {
    submitError.value = isDev ? 'La contraseña debe tener al menos 1 carácter.' : 'La contraseña debe tener al menos 8 caracteres.'
    return
  }

  submitting.value = true
  submitError.value = null
  try {
    const result = await apiService.acceptInvitation(token.value, {
      displayName: form.displayName?.trim() || null,
      email: form.email.trim(),
      password: form.password
    })
    if (result?.token) {
      apiService.setToken(result.token)
      toast.success('Invitación aceptada. Bienvenido al equipo.')
      router.push('/admin')
    } else {
      submitError.value = result?.error || 'Error al aceptar.'
    }
  } catch (err) {
    submitError.value = err.response?.data?.error || err.message || 'Error al aceptar la invitación.'
  } finally {
    submitting.value = false
  }
}

async function handleSocialLogin(provider) {
  try {
    const returnUrl = `/invite/${token.value}`
    const data = await apiService.getSocialLoginAuthorizeUrl(provider, returnUrl)
    if (data?.redirectUrl) {
      window.location.href = data.redirectUrl
    } else {
      toast.error(`Inicio de sesión con ${provider} no está configurado.`)
    }
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || `Error al conectar con ${provider}.`)
  }
}

// Handle OAuth callback: we land here with ?code=xxx&provider=google when coming from SocialLoginCallback redirect
async function handleOAuthCallback() {
  const code = route.query.code
  const provider = route.query.provider
  if (!code || !provider || !token.value) return

  submitting.value = true
  submitError.value = null
  try {
    const baseUrl = window.location.origin
    const redirectUri = `${baseUrl}/auth/${provider}/callback`
    const result = await apiService.acceptInvitation(token.value, {
      provider,
      code,
      redirectUri
    })
    if (result?.token) {
      apiService.setToken(result.token)
      toast.success('Invitación aceptada. Bienvenido al equipo.')
      router.push('/admin')
    } else {
      submitError.value = result?.error || 'Error al aceptar con red social.'
    }
  } catch (err) {
    submitError.value = err.response?.data?.error || err.message || 'Error al aceptar la invitación.'
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  await loadInvitation()
  if (route.query.code && route.query.provider && token.value) {
    await handleOAuthCallback()
  }
})

watch(() => route.params.token, (newToken) => {
  if (newToken && newToken !== token.value) {
    loadInvitation()
  }
})
</script>

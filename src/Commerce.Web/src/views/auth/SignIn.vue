<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col lg:flex-row">
    <!-- Formulario - Izquierda -->
    <section
        class="flex-1 flex flex-col justify-center px-6 sm:px-12 lg:px-16 py-12 lg:py-16 bg-white dark:bg-gray-800"
      >
        <div class="max-w-sm mx-auto w-full">
          <router-link to="/" class="mb-8 flex justify-center text-gray-900 dark:text-white">
            <KutriaMark size="lg" />
          </router-link>
          <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">
            Iniciar sesión
          </h2>
          <p class="text-gray-600 dark:text-gray-400 mb-6">
            {{ signInLeadText }}
          </p>
          <div
            v-if="justCreated"
            class="mb-6 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900 dark:border-emerald-800 dark:bg-emerald-900/25 dark:text-emerald-100"
          >
            Tu tienda ya está lista. Entra con el correo y la contraseña que acabas de crear.
          </div>

          <div
            v-if="inviteKind === 'operator'"
            class="mb-6 rounded-lg border border-primary-200 bg-primary-50 px-4 py-3 text-sm text-primary-800 dark:border-primary-800 dark:bg-primary-900/25 dark:text-primary-200"
          >
            Acceso para <strong>operador del partner</strong>. Usa el correo con el que te invitaron.
          </div>

          <div class="flex flex-col gap-3">
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

          <form @submit.prevent="handleSubmit" class="space-y-5">
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
              <div class="flex items-center justify-between mb-1">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Contraseña <span class="text-red-500">*</span>
                </label>
                <router-link
                  to="/forgot-password"
                  class="text-sm text-primary-600 dark:text-primary-400 hover:underline"
                >
                  ¿Olvidaste tu contraseña?
                </router-link>
              </div>
              <input
                v-model="form.password"
                type="password"
                required
                :class="['input-field w-full', touched.password && !form.password && '!border-red-500 dark:!border-red-500']"
                placeholder="••••••••"
                @blur="touched.password = true"
              />
            </div>
            <div v-if="error" class="space-y-2">
              <p class="text-sm text-red-600 dark:text-red-400">
                {{ error }}
              </p>
              <p v-if="emailNotConfirmed" class="text-sm text-gray-600 dark:text-gray-400">
                Revisa tu bandeja de entrada y haz clic en el enlace que te enviamos. Si no lo encuentras, revisa la carpeta de spam.
              </p>
              <router-link
                v-if="emailNotConfirmed"
                :to="{ path: '/resend-confirmation', query: { email: form.email } }"
                class="text-sm text-primary-600 dark:text-primary-400 hover:underline font-medium"
              >
                ¿No recibiste el correo? Reenviar
              </router-link>
            </div>
            <button
              type="submit"
              :disabled="submitting || !form.email || !form.password"
              :class="[
                'w-full py-3 rounded-lg font-semibold transition-colors',
                submitting || !form.email || !form.password
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                  : 'bg-primary-600 hover:bg-primary-700 text-white'
              ]"
            >
              {{ submitting ? 'Entrando...' : 'Entrar' }}
            </button>
          </form>

          <p class="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
            ¿No tienes cuenta?
            <router-link
              :to="Object.keys(partnerFlowQuery).length ? { path: '/sign-up', query: partnerFlowQuery } : '/sign-up'"
              class="text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              Crear cuenta
            </router-link>
          </p>
        </div>
    </section>

    <!-- Panel derecho - Contenido -->
    <AuthSidePanel
        title="Bienvenido de vuelta"
        description="Accede a Kutria: agentes de venta y operación conectados a tu tienda, sin migrar tu catálogo."
        :features="[
          'Descubrimiento, compra y post-venta con stock real de tu tienda',
          'Asesor de operaciones: detecta el patrón y tú apruebas en 1 click',
          'Se conecta a VTEX, Shopify, WooCommerce y marketplaces, sin migrar',
          'Tus clientes escriben o hablan. El mismo agente atiende ambos',
          'Desde $70/mes. Infra incluida. Pagas por crecimiento, no por servidores'
        ]"
    />
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import AuthSidePanel from '../../components/AuthSidePanel.vue'
import KutriaMark from '../../components/kutria/KutriaMark.vue'
import apiService from '../../services/api'
import { rememberTenant } from '../../utils/tenant'

const router = useRouter()
const route = useRoute()
const toast = useToast()

const form = ref({ email: '', password: '' })
const touched = ref({ email: false, password: false })
const submitting = ref(false)
const error = ref(null)
const emailNotConfirmed = ref(false)
const justCreated = computed(() => route.query.created === '1')

const returnUrl = computed(() => {
  const url = route.query.returnUrl
  return url && typeof url === 'string' ? decodeURIComponent(url) : '/admin'
})

const inviteKind = computed(() => {
  const raw = route.query.inviteKind
  if (typeof raw !== 'string') return null
  const v = raw.trim().toLowerCase()
  if (v === 'operator' || v === 'program') return v
  return null
})

const partnerFlowQuery = computed(() => {
  const out = {}
  const pr = route.query.partnerReservationToken
  if (typeof pr === 'string' && pr.trim()) out.partnerReservationToken = pr.trim()
  const cid = route.query.clientId
  if (typeof cid === 'string' && cid.trim()) out.clientId = cid.trim()
  if (inviteKind.value) out.inviteKind = inviteKind.value
  return out
})

const signInLeadText = computed(() => {
  if (inviteKind.value === 'operator') return 'Entra con tu usuario de operador del partner.'
  return '¿Ya tienes cuenta? Accede a tu panel.'
})

async function handleSubmit() {
  submitting.value = true
  error.value = null
  emailNotConfirmed.value = false
  try {
    const data = await apiService.signIn(form.value.email, form.value.password)
    const slug = data.identifier
    if (!slug) {
      error.value = 'No encontramos la tienda de esta cuenta.'
      return
    }
    rememberTenant(slug)
    apiService.setToken(data.token)
    toast.success('Sesión iniciada.')
    const next = returnUrl.value.startsWith('/admin')
      ? `/t/${slug}${returnUrl.value}`
      : `/t/${slug}/admin`
    router.push(next)
  } catch (err) {
    const data = err.response?.data
    const code = data?.code
    const msg = data?.error || err.message || 'Error al iniciar sesión'
    error.value = msg
    emailNotConfirmed.value = code === 'EmailNotConfirmed'
  } finally {
    submitting.value = false
  }
}

async function handleSocialLogin(provider) {
  try {
    const data = await apiService.getSocialLoginAuthorizeUrl(provider, returnUrl.value)
    if (data?.redirectUrl) {
      window.location.href = data.redirectUrl
    } else {
      toast.error(`Inicio con ${provider} no está configurado.`)
    }
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || `Error al iniciar con ${provider}.`)
  }
}
</script>

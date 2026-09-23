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
            Crear cuenta
          </h2>
          <p class="text-gray-600 dark:text-gray-400 mb-6">
            {{ signupLeadText }}
          </p>

          <div
            v-if="partnerStartToken && inviteKind === 'operator'"
            class="mb-6 rounded-lg border border-primary-200 bg-primary-50 px-4 py-3 text-sm text-primary-800 dark:border-primary-800 dark:bg-primary-900/25 dark:text-primary-200"
          >
            Estás completando el acceso como <strong>operador</strong> de un partner en Kutria. Usa el mismo
            correo que recibió la invitación.
          </div>
          <div
            v-else-if="partnerStartToken && inviteKind === 'program'"
            class="mb-6 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900 dark:border-emerald-800 dark:bg-emerald-900/25 dark:text-emerald-100"
          >
            Tu organización te invitó a crear una <strong>cuenta cliente</strong> en Kutria. Es un registro normal;
            la cuenta quedará asociada a ese partner.
          </div>
          <div
            v-else-if="partnerStartToken"
            class="mb-6 rounded-lg border border-primary-200 bg-primary-50 px-4 py-3 text-sm text-primary-800 dark:border-primary-800 dark:bg-primary-900/25 dark:text-primary-200"
          >
            Completarás el registro con el enlace de invitación que recibiste.
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

          <div
            v-if="selectedPlan"
            class="mb-6 inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-50 dark:bg-primary-900/20 border border-primary-200 dark:border-primary-800"
          >
            <span class="text-sm font-medium text-primary-700 dark:text-primary-300">
              Plan: {{ selectedPlan.name }}
            </span>
            <span class="text-sm text-primary-600 dark:text-primary-400">
              ${{ selectedPlan.priceUsd }}/mes
            </span>
          </div>

          <form @submit.prevent="handleSubmit" class="space-y-5">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Nombre completo <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.name"
                type="text"
                required
                :class="['input-field w-full', touched.name && !form.name && '!border-red-500 dark:!border-red-500']"
                placeholder="Juan Pérez"
                @blur="touched.name = true"
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
                Empresa (opcional)
              </label>
              <input
                v-model="form.company"
                type="text"
                class="input-field w-full"
                placeholder="Mi Tienda S.A."
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Identificador de la tienda <span class="text-red-500">*</span>
              </label>
              <div class="flex items-center">
                <span class="shrink-0 rounded-l-lg border border-r-0 border-gray-300 bg-gray-50 px-3 py-2.5 text-sm text-gray-500 dark:border-gray-600 dark:bg-gray-900 dark:text-gray-400">
                  /t/
                </span>
                <input
                  v-model="form.subdomain"
                  type="text"
                  required
                  maxlength="80"
                  autocomplete="organization"
                  :class="['input-field w-full rounded-l-none', subdomainInputClass]"
                  placeholder="Mi tienda favorita"
                  @blur="touched.subdomain = true"
                />
              </div>
              <p v-if="subdomainMessage" :class="['mt-1 text-sm', subdomainMessageClass]">
                {{ subdomainMessage }}
              </p>
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
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Confirmar contraseña <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.passwordConfirm"
                type="password"
                required
                :class="['input-field w-full', touched.passwordConfirm && !form.passwordConfirm && '!border-red-500 dark:!border-red-500']"
                placeholder="Repite tu contraseña"
                @blur="touched.passwordConfirm = true"
              />
              <p v-if="form.password && form.passwordConfirm && form.password !== form.passwordConfirm" class="mt-1 text-sm text-red-600 dark:text-red-400">
                Las contraseñas no coinciden
              </p>
            </div>
            <p v-if="error" class="text-sm text-red-600 dark:text-red-400">
              {{ error }}
            </p>
            <button
              type="submit"
              :disabled="!canSubmit"
              :class="[
                'w-full py-3 rounded-lg font-semibold transition-colors',
                !canSubmit
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                  : 'bg-primary-600 hover:bg-primary-700 text-white'
              ]"
            >
              {{ submitting ? 'Creando cuenta...' : 'Crear cuenta' }}
            </button>
          </form>

          <p class="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
            ¿Ya tienes cuenta?
            <router-link
              :to="Object.keys(partnerFlowQuery).length ? { path: '/sign-in', query: partnerFlowQuery } : '/sign-in'"
              class="text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              Iniciar sesión
            </router-link>
          </p>
        </div>
    </section>

    <!-- Panel derecho - Contenido -->
    <AuthSidePanel
        title="Empieza con Kutria"
        description="La plataforma que escala tus ventas y mejora tus operaciones, sin cambiar tu tienda."
        :features="[
          'Cuatro agentes de retail: descubren, compran, resuelven y operan',
          'Entiende lenguaje natural, confirma stock y manda el link de pago',
          'Tu operación mejora sola: propone el cambio y tú lo apruebas',
          'Sin migraciones. Se conecta a lo que ya usas: tienda, marketplaces y ERP',
          'Desde $70/mes para empezar. Crece contigo, sin contratos anuales'
        ]"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import AuthSidePanel from '../../components/AuthSidePanel.vue'
import KutriaMark from '../../components/kutria/KutriaMark.vue'
import apiService from '../../services/api'
const route = useRoute()
const router = useRouter()
const toast = useToast()

const form = ref({
  name: '',
  email: '',
  company: '',
  subdomain: '',
  password: '',
  passwordConfirm: ''
})
const touched = ref({ name: false, email: false, subdomain: false, password: false, passwordConfirm: false })
const subdomainStatus = ref('idle')
const subdomainMessage = ref('')
let subdomainTimer = null
const submitting = ref(false)
const error = ref(null)
const plans = ref([])
const planKey = computed(() => route.query.plan || '')
const partnerStartToken = computed(() => {
  const q = route.query
  const raw = q.pt ?? q.partnerStartToken ?? q.partnerReservationToken
  if (typeof raw !== 'string') return null
  const t = raw.trim()
  return t || null
})
/** Normalized invite line from /api/partners/start: operator | program */
const inviteKind = computed(() => {
  const raw = route.query.inviteKind
  if (typeof raw !== 'string') return null
  const v = raw.trim().toLowerCase()
  if (v === 'operator' || v === 'program') return v
  return null
})
/** Query to preserve partner invite context between sign-up ↔ sign-in */
const partnerFlowQuery = computed(() => {
  const out = {}
  const t = partnerStartToken.value
  if (t) out.partnerReservationToken = t
  const cid = route.query.clientId
  if (typeof cid === 'string' && cid.trim()) out.clientId = cid.trim()
  if (inviteKind.value) out.inviteKind = inviteKind.value
  return out
})
const signupLeadText = computed(() => {
  if (partnerStartToken.value && inviteKind.value === 'operator')
    return 'Completa tus datos para activar tu acceso como operador del partner.'
  if (partnerStartToken.value && inviteKind.value === 'program')
    return 'Completa tus datos para crear tu cuenta cliente.'
  return 'Completa tus datos para comenzar.'
})
const isDev = import.meta.env.DEV
const subdomainInputClass = computed(() => {
  if (subdomainStatus.value === 'available') return '!border-emerald-500 dark:!border-emerald-500'
  if (subdomainStatus.value === 'taken' || subdomainStatus.value === 'reserved' || subdomainStatus.value === 'invalid') {
    return '!border-red-500 dark:!border-red-500'
  }
  return ''
})
const subdomainMessageClass = computed(() =>
  subdomainStatus.value === 'available'
    ? 'text-emerald-600 dark:text-emerald-400'
    : 'text-red-600 dark:text-red-400'
)
const canSubmit = computed(() =>
  !submitting.value
  && form.value.name
  && form.value.email
  && form.value.subdomain
  && subdomainStatus.value === 'available'
  && form.value.password
  && form.value.passwordConfirm
  && form.value.password === form.value.passwordConfirm
)

const selectedPlan = computed(() => {
  if (!planKey.value || !plans.value.length) return null
  return plans.value.find(p => p.key === planKey.value) || null
})

async function loadPlans() {
  try {
    const data = await apiService.getPlans()
    plans.value = data.plans ?? []
  } catch {
    plans.value = []
  }
}

const reasonText = {
  invalid: 'Ese nombre no genera un identificador válido. Usa al menos 3 letras o números.',
  reserved: 'Ese identificador está reservado.',
  taken: 'Ese identificador ya está en uso.'
}

watch(() => form.value.subdomain, (value) => {
  const name = String(value || '')
  subdomainStatus.value = 'idle'
  subdomainMessage.value = ''
  clearTimeout(subdomainTimer)
  if (!name.trim()) return
  subdomainStatus.value = 'checking'
  subdomainMessage.value = 'Comprobando disponibilidad...'
  subdomainTimer = setTimeout(() => checkSubdomain(name), 350)
})

async function checkSubdomain(name) {
  try {
    const data = await apiService.checkTenantAvailability(name)
    if (form.value.subdomain !== name) return
    if (data.available) {
      subdomainStatus.value = 'available'
      subdomainMessage.value = `Tu tienda quedará en /t/${data.identifier}`
      return
    }
    subdomainStatus.value = data.reason || 'taken'
    subdomainMessage.value = data.identifier
      ? `${reasonText[data.reason] || 'Ese identificador no está disponible.'} (${data.identifier})`
      : (reasonText[data.reason] || 'Ese identificador no está disponible.')
  } catch (err) {
    if (form.value.subdomain !== name) return
    subdomainStatus.value = 'invalid'
    subdomainMessage.value = err.response?.data?.error || 'No se pudo comprobar el identificador.'
  }
}

async function handleSubmit() {
  if (form.value.password !== form.value.passwordConfirm) {
    error.value = 'Las contraseñas no coinciden'
    return
  }
  const minLen = isDev ? 1 : 8
  if (form.value.password.length < minLen) {
    error.value = isDev ? 'La contraseña debe tener al menos 1 carácter' : 'La contraseña debe tener al menos 8 caracteres'
    return
  }

  submitting.value = true
  error.value = null

  try {
    const data = await apiService.signUp(
      form.value.email,
      form.value.password,
      form.value.name || null,
      planKey.value || null,
      partnerStartToken.value,
      form.value.subdomain,
      form.value.company || null
    )
    toast.success('Tienda creada. Inicia sesión para continuar.')
    router.push(`/t/${data.identifier}/sign-in?created=1`)
  } catch (err) {
    const msg = err.response?.data?.error || err.message || 'Error al crear la cuenta'
    error.value = msg
  } finally {
    submitting.value = false
  }
}

async function handleSocialLogin(provider) {
  try {
    const returnUrl = route.query.returnUrl ? decodeURIComponent(route.query.returnUrl) : '/admin'
    const data = await apiService.getSocialLoginAuthorizeUrl(provider, returnUrl)
    if (data?.redirectUrl) {
      window.location.href = data.redirectUrl
    } else {
      toast.error(`Registro con ${provider} no está configurado.`)
    }
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || `Error al registrar con ${provider}.`)
  }
}

onMounted(loadPlans)
</script>

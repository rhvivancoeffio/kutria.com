<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col lg:flex-row">
    <section
      class="flex-1 flex flex-col justify-center px-6 sm:px-12 lg:px-16 py-12 lg:py-16 bg-white dark:bg-gray-800"
    >
      <div class="max-w-sm mx-auto w-full">
        <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Confirmar correo electrónico
        </h2>

        <div v-if="!token || !email" class="space-y-4">
          <div class="p-4 rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800">
            <p class="text-sm text-amber-800 dark:text-amber-200">
              Enlace inválido o expirado. Si acabas de registrarte, revisa tu correo y usa el enlace que te enviamos.
            </p>
          </div>
          <div class="flex flex-col gap-2">
            <router-link
              to="/resend-confirmation"
              class="inline-block text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              ¿No recibiste el correo? Reenviar
            </router-link>
            <router-link
              to="/sign-in"
              class="inline-block text-gray-600 dark:text-gray-400 hover:underline text-sm"
            >
              Volver a iniciar sesión
            </router-link>
          </div>
        </div>

        <div v-else-if="processing" class="space-y-4">
          <p class="text-gray-600 dark:text-gray-400">
            Confirmando tu cuenta...
          </p>
        </div>

        <div v-else-if="success" class="space-y-6">
          <div class="p-4 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800">
            <p class="text-sm text-green-800 dark:text-green-200">
              ¡Cuenta confirmada! Ya puedes usar la plataforma.
            </p>
          </div>
          <router-link
            to="/sign-in"
            class="inline-block w-full text-center py-3 rounded-lg font-semibold bg-primary-600 hover:bg-primary-700 text-white transition-colors"
          >
            Iniciar sesión
          </router-link>
        </div>

        <div v-else-if="error" class="space-y-4">
          <div class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
            <p class="text-sm text-red-800 dark:text-red-200">
              {{ error }}
            </p>
          </div>
          <div class="flex flex-col gap-2">
            <router-link
              :to="{ path: '/resend-confirmation', query: { email } }"
              class="inline-block text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              Reenviar correo de confirmación
            </router-link>
            <router-link
              to="/sign-in"
              class="inline-block text-gray-600 dark:text-gray-400 hover:underline text-sm"
            >
              Volver a iniciar sesión
            </router-link>
          </div>
        </div>

        <p class="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
          <router-link to="/sign-in" class="text-primary-600 dark:text-primary-400 hover:underline font-medium">
            Volver a iniciar sesión
          </router-link>
        </p>
      </div>
    </section>

    <AuthSidePanel
      title="Verificación de cuenta"
      description="La confirmación de correo ayuda a asegurar que tu cuenta sea válida y que puedas recuperar el acceso si lo necesitas."
      :features="[
        'Protege tu cuenta',
        'Recuperación de acceso',
        'Notificaciones importantes',
        'Un solo clic para confirmar'
      ]"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import AuthSidePanel from '../../components/AuthSidePanel.vue'
import apiService from '../../services/api'

const route = useRoute()
const toast = useToast()

const token = ref('')
const email = ref('')
const processing = ref(true)
const success = ref(false)
const error = ref(null)

onMounted(async () => {
  token.value = route.query.token || ''
  email.value = route.query.email || ''

  if (!token.value || !email.value) {
    processing.value = false
    return
  }

  try {
    await apiService.confirmEmail(email.value, token.value)
    success.value = true
    toast.success('Cuenta confirmada correctamente.')
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al confirmar el correo.'
  } finally {
    processing.value = false
  }
})
</script>

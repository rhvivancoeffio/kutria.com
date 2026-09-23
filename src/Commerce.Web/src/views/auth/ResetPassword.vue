<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col lg:flex-row">
    <section
      class="flex-1 flex flex-col justify-center px-6 sm:px-12 lg:px-16 py-12 lg:py-16 bg-white dark:bg-gray-800"
    >
      <div class="max-w-sm mx-auto w-full">
        <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Nueva contraseña
        </h2>
        <p class="text-gray-600 dark:text-gray-400 mb-8">
          Ingresa tu nueva contraseña. El enlace expira en 1 hora.
        </p>

        <div v-if="!token || !email" class="space-y-4">
          <div class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
            <p class="text-sm text-red-800 dark:text-red-200">
              Enlace inválido o expirado. Solicita un nuevo enlace desde la página de recuperar contraseña.
            </p>
          </div>
          <router-link
            to="/forgot-password"
            class="inline-block text-primary-600 dark:text-primary-400 hover:underline font-medium"
          >
            Solicitar nuevo enlace
          </router-link>
        </div>

        <form v-else @submit.prevent="handleSubmit" class="space-y-5">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Nueva contraseña <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.password"
              type="password"
              required
              :minlength="isDev ? 1 : 6"
              :class="['input-field w-full', touched.password && !form.password && '!border-red-500 dark:!border-red-500']"
              placeholder="••••••••"
              @blur="touched.password = true"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Confirmar contraseña <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.confirmPassword"
              type="password"
              required
              :class="['input-field w-full', touched.confirmPassword && form.password !== form.confirmPassword && '!border-red-500 dark:!border-red-500']"
              placeholder="••••••••"
              @blur="touched.confirmPassword = true"
            />
            <p v-if="touched.confirmPassword && form.password && form.confirmPassword && form.password !== form.confirmPassword" class="mt-1 text-sm text-red-600 dark:text-red-400">
              Las contraseñas no coinciden
            </p>
          </div>
          <p v-if="error" class="text-sm text-red-600 dark:text-red-400">
            {{ error }}
          </p>
          <button
            type="submit"
            :disabled="submitting || !form.password || !form.confirmPassword || form.password !== form.confirmPassword || form.password.length < minPasswordLen"
            :class="[
              'w-full py-3 rounded-lg font-semibold transition-colors',
              submitting || !form.password || !form.confirmPassword || form.password !== form.confirmPassword || form.password.length < minPasswordLen
                ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                : 'bg-primary-600 hover:bg-primary-700 text-white'
            ]"
          >
            {{ submitting ? 'Guardando...' : 'Restablecer contraseña' }}
          </button>
        </form>

        <div v-if="success" class="mt-6 space-y-4">
          <div class="p-4 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800">
            <p class="text-sm text-green-800 dark:text-green-200">
              Contraseña actualizada. Ya puedes iniciar sesión.
            </p>
          </div>
          <router-link
            to="/sign-in"
            class="inline-block w-full text-center py-3 rounded-lg font-semibold bg-primary-600 hover:bg-primary-700 text-white transition-colors"
          >
            Ir a iniciar sesión
          </router-link>
        </div>

        <p class="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
          <router-link to="/sign-in" class="text-primary-600 dark:text-primary-400 hover:underline font-medium">
            Volver a iniciar sesión
          </router-link>
        </p>
      </div>
    </section>

    <AuthSidePanel
      title="Seguridad"
      description="Usa una contraseña fuerte con al menos 6 caracteres. Evita contraseñas que uses en otros servicios."
      :features="[
        'Mínimo 6 caracteres',
        'El enlace solo funciona una vez',
        'Caduca en 1 hora',
        'Si expiró, solicita uno nuevo'
      ]"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import AuthSidePanel from '../../components/AuthSidePanel.vue'
import apiService from '../../services/api'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const token = ref('')
const email = ref('')

const isDev = import.meta.env.DEV
const minPasswordLen = isDev ? 1 : 6
const form = ref({ password: '', confirmPassword: '' })
const touched = ref({ password: false, confirmPassword: false })
const submitting = ref(false)
const error = ref(null)
const success = ref(false)

onMounted(() => {
  token.value = route.query.token || ''
  email.value = route.query.email || ''
})

async function handleSubmit() {
  if (form.value.password !== form.value.confirmPassword) return
  if (form.value.password.length < minPasswordLen) return

  submitting.value = true
  error.value = null
  try {
    await apiService.resetPassword(email.value, token.value, form.value.password)
    success.value = true
    toast.success('Contraseña restablecida correctamente.')
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al restablecer la contraseña.'
  } finally {
    submitting.value = false
  }
}
</script>

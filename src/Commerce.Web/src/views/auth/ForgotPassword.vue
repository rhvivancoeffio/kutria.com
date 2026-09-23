<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col lg:flex-row">
    <!-- Formulario - Izquierda -->
    <section
      class="flex-1 flex flex-col justify-center px-6 sm:px-12 lg:px-16 py-12 lg:py-16 bg-white dark:bg-gray-800"
    >
      <div class="max-w-sm mx-auto w-full">
        <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Recuperar contraseña
        </h2>
        <p class="text-gray-600 dark:text-gray-400 mb-8">
          Ingresa tu correo y te enviaremos un enlace para restablecer tu contraseña.
        </p>

        <form v-if="!sent" @submit.prevent="handleSubmit" class="space-y-5">
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
          <p v-if="error" class="text-sm text-red-600 dark:text-red-400">
            {{ error }}
          </p>
          <button
            type="submit"
            :disabled="submitting || !form.email"
            :class="[
              'w-full py-3 rounded-lg font-semibold transition-colors',
              submitting || !form.email
                ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                : 'bg-primary-600 hover:bg-primary-700 text-white'
            ]"
          >
            {{ submitting ? 'Enviando...' : 'Enviar enlace' }}
          </button>
        </form>

        <div v-else class="space-y-6">
          <div class="p-4 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800">
            <p class="text-sm text-green-800 dark:text-green-200">
              Revisa tu correo. Te enviamos un enlace para restablecer tu contraseña.
            </p>
          </div>
          <button
            type="button"
            @click="sent = false"
            class="text-sm text-primary-600 dark:text-primary-400 hover:underline"
          >
            ¿No lo recibiste? Reenviar
          </button>
        </div>

        <p class="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
          <router-link to="/sign-in" class="text-primary-600 dark:text-primary-400 hover:underline font-medium">
            Volver a iniciar sesión
          </router-link>
        </p>
      </div>
    </section>

    <!-- Panel derecho - Contenido -->
    <AuthSidePanel
      title="¿Problemas para acceder?"
      description="Tu seguridad es importante. Revisa tu bandeja de entrada y la carpeta de spam si no encuentras el correo."
      :features="[
        'El enlace expira en 24 horas',
        'Solo podrás usarlo una vez',
        'Si no lo recibes, revisa tu carpeta de spam',
        'Contacta soporte si el problema persiste'
      ]"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useToast } from 'vue-toastification'
import AuthSidePanel from '../../components/AuthSidePanel.vue'
import apiService from '../../services/api'

const toast = useToast()

const form = ref({ email: '' })
const touched = ref({ email: false })
const submitting = ref(false)
const error = ref(null)
const sent = ref(false)

async function handleSubmit() {
  submitting.value = true
  error.value = null
  try {
    await apiService.forgotPassword(form.value.email)
    sent.value = true
    toast.success('Revisa tu correo para restablecer tu contraseña.')
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al enviar el enlace.'
  } finally {
    submitting.value = false
  }
}
</script>

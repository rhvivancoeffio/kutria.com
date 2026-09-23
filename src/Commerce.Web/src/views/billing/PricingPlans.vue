<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <AppHeader />

    <main class="max-w-[1400px] mx-auto px-4 sm:px-6 lg:px-8 py-12">
      <div class="text-center mb-12">
        <h1 class="text-4xl font-bold text-gray-900 dark:text-white mb-4">
          Planes y Precios
        </h1>
        <p class="text-xl text-gray-600 dark:text-gray-400 max-w-2xl mx-auto mb-4">
          Conecta tus canales, sincroniza catálogo y pedidos, y opera con IA sobre datos unificados. Elige el plan según tu volumen y límites.
        </p>
        <p class="text-base text-primary-600 dark:text-primary-400 font-medium max-w-xl mx-auto">
          Prueba la versión trial sin tarjeta de crédito. Regístrate y empieza a usar la plataforma de inmediato.
        </p>
      </div>

      <div v-if="loading" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        <div
          v-for="i in 4"
          :key="i"
          class="rounded-2xl border-2 border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-6 flex flex-col animate-pulse"
        >
          <div class="mb-4">
            <div class="h-6 w-32 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
            <div class="h-4 w-48 bg-gray-200 dark:bg-gray-700 rounded" />
          </div>
          <div class="mb-6">
            <div class="h-10 w-24 bg-gray-200 dark:bg-gray-700 rounded" />
          </div>
          <div class="mb-6 flex-1 space-y-3">
            <div class="h-4 w-36 bg-gray-200 dark:bg-gray-700 rounded" />
            <div class="space-y-2">
              <div v-for="j in 3" :key="j" class="h-3 w-full bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
            <div class="space-y-2 pt-2">
              <div v-for="j in 4" :key="`f-${j}`" class="h-3 w-full bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
          </div>
          <div class="h-3 w-40 bg-gray-200 dark:bg-gray-700 rounded mb-4" />
          <div class="h-12 w-full bg-gray-200 dark:bg-gray-700 rounded-lg" />
        </div>
      </div>

      <div v-else-if="error" class="text-center py-16">
        <p class="text-red-600 dark:text-red-400">{{ error }}</p>
      </div>

      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        <div
          v-for="plan in plans"
          :key="plan.key"
          :class="[
            'rounded-2xl border-2 p-6 flex flex-col',
            plan.key === 'smartops'
              ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20 dark:border-primary-400 shadow-lg'
              : plan.key === 'enterprise-ops'
                ? 'border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/80'
                : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800'
          ]"
        >
          <div class="mb-4">
            <h2 class="text-xl font-bold text-gray-900 dark:text-white">
              {{ plan.name }}
            </h2>
            <p class="text-sm text-gray-600 dark:text-gray-400 mt-1">
              {{ plan.tagline }}
            </p>
          </div>

          <div class="mb-6">
            <template v-if="plan.contactSales">
              <span class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Precio a medida</span>
            </template>
            <template v-else>
              <span v-if="plan.strikethroughPriceUsd" class="text-xl text-gray-500 dark:text-gray-400 line-through mr-2">${{ plan.strikethroughPriceUsd }}</span>
              <span class="text-4xl font-bold text-gray-900 dark:text-white">${{ plan.priceUsd }}</span>
              <span class="text-gray-500 dark:text-gray-400">/mes</span>
            </template>
          </div>

          <div class="mb-6 flex-1">
            <p v-if="plan.contactSales" class="text-sm text-gray-600 dark:text-gray-400 mb-4">
              Condiciones y volumen a medida
            </p>
            <p v-else-if="plan.creditsPerMonth != null" class="text-sm text-gray-600 dark:text-gray-400 mb-4">
              {{ plan.creditsPerMonth.toLocaleString() }} / mes
            </p>
            <ul v-if="formatPlanLimits(plan.limits).length" class="space-y-2 mb-4">
              <li
                v-for="limit in formatPlanLimits(plan.limits)"
                :key="limit"
                class="flex items-start gap-2 text-sm text-gray-600 dark:text-gray-400"
              >
                <span class="text-primary-500 shrink-0">•</span>
                {{ limit }}
              </li>
            </ul>
            <ul class="space-y-2">
              <li
                v-for="feature in plan.features"
                :key="feature"
                class="flex items-start gap-2 text-sm text-gray-700 dark:text-gray-300"
              >
                <span class="text-green-500 shrink-0">✓</span>
                {{ feature }}
              </li>
            </ul>
          </div>

          <p class="text-xs text-gray-500 dark:text-gray-400 mb-4 italic">
            {{ plan.idealFor }}
          </p>

          <button
            v-if="plan.contactSales"
            class="w-full py-3 rounded-lg font-medium transition-colors bg-gray-800 dark:bg-gray-700 hover:bg-gray-700 dark:hover:bg-gray-600 text-white"
            @click="showContactSalesModal = true"
          >
            Contactar
          </button>
          <button
            v-else
            :class="[
              'w-full py-3 rounded-lg font-medium transition-colors',
              plan.key === 'smartops'
                ? 'bg-primary-600 hover:bg-primary-700 text-white'
                : 'bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-900 dark:text-white'
            ]"
            @click="goToSignUp(plan)"
          >
            Elegir plan
          </button>
        </div>
      </div>

      <div v-if="creditAddOns?.length && !loading && !error" class="mt-16 text-center">
        <h3 class="text-xl font-semibold text-gray-900 dark:text-white mb-4">
          Créditos adicionales
        </h3>
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
          Packs de créditos extra para tu plan (1 crédito = 1 interacción con la IA).
        </p>
        <div class="flex flex-wrap justify-center gap-4">
          <div
            v-for="addOn in creditAddOns"
            :key="addOn.credits"
            class="px-6 py-3 rounded-lg bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700"
          >
            <span class="font-medium text-gray-900 dark:text-white">{{ addOn.credits.toLocaleString() }} créditos</span>
            <span class="text-gray-600 dark:text-gray-400"> → ${{ addOn.priceUsd }}</span>
            <span v-if="addOn.discountLabel" class="text-xs text-primary-600 dark:text-primary-400 ml-2">
              {{ addOn.discountLabel }}
            </span>
          </div>
        </div>
      </div>

      <ContactSalesModal v-model="showContactSalesModal" />
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '../../components/AppHeader.vue'
import ContactSalesModal from '../../components/billing/ContactSalesModal.vue'
import apiService from '../../services/api'
import { formatPlanLimits } from '../../utils/planLimits'

const router = useRouter()
const plans = ref([])
const creditAddOns = ref([])
const loading = ref(true)
const error = ref(null)
const showContactSalesModal = ref(false)

async function load() {
  loading.value = true
  error.value = null
  try {
    const data = await apiService.getPlans()
    plans.value = data.plans ?? []
    creditAddOns.value = data.creditAddOns ?? []
  } catch (err) {
    console.error('Error loading plans:', err)
    error.value = err.response?.data?.message || 'Error al cargar los planes'
  } finally {
    loading.value = false
  }
}

function goToSignUp(plan) {
  router.push({ path: '/sign-up', query: { plan: plan.key } })
}

onMounted(load)
</script>

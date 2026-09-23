<template>
  <div :class="isEmbeddedUsage ? 'px-1 py-0' : 'max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8'">
    <div v-if="!isEmbeddedUsage" class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900 dark:text-white">
        {{ showPartnerPortal ? 'Panel de uso — programa partner' : 'Panel de uso' }}
      </h1>
      <p class="mt-1 text-gray-600 dark:text-gray-400">
        <template v-if="showPartnerPortal">
          Uso de la cuenta del programa seleccionada (mismos datos que el cliente). Elige la cuenta con el query
          <code class="text-xs bg-gray-100 dark:bg-gray-800 px-1 rounded">accountId</code> o desde
          <router-link :to="toPath('partner/accounts')" class="text-primary-600 dark:text-primary-400 font-medium hover:underline">
            Cuentas programa
          </router-link>
          .
        </template>
        <template v-else> TemplateProject: recursos configurados (integraciones, pipelines, canales de venta) según tu plan. </template>
      </p>
      <p v-if="showPartnerPortal && effectiveAccountId" class="mt-2 text-xs font-mono text-gray-500 dark:text-gray-400 break-all">
        Cuenta: {{ effectiveAccountId }}
      </p>
    </div>

    <div
      v-if="showPartnerPortal && !effectiveAccountId && !isEmbeddedUsage"
      class="mb-6 rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-900/20 px-4 py-3 text-sm text-amber-900 dark:text-amber-100"
    >
      Selecciona una cuenta del programa en
      <router-link :to="toPath('partner/accounts')" class="font-semibold text-primary-700 dark:text-primary-300 hover:underline">
        Cuentas programa
      </router-link>
      para ver el uso detallado de esa cuenta.
    </div>

    <!-- Sin integraciones: solo cuenta operador -->
    <StoreConnectionRequired
      v-if="!showPartnerPortal && !loadingIntegrations && myIntegrations.length === 0"
      class="mb-6"
      title="Sin conexión de integración"
      message="Para ver el uso de tu cuenta debes conectar una integración (tienda o Gravity API) en Integraciones."
    />

    <!-- Loading skeleton -->
    <div v-if="loading" class="space-y-8">
      <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="i in 6" :key="i" class="p-4 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 animate-pulse">
          <div class="h-4 w-20 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
          <div class="h-8 w-16 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
          <div class="h-2 bg-gray-200 dark:bg-gray-700 rounded-full" />
        </div>
      </div>
      <div class="rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 overflow-hidden animate-pulse">
        <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 space-y-1">
          <div class="h-5 w-48 bg-gray-200 dark:bg-gray-700 rounded" />
          <div class="h-4 w-64 bg-gray-200 dark:bg-gray-700 rounded" />
        </div>
        <div class="divide-y divide-gray-200 dark:divide-gray-700">
          <div v-for="i in 5" :key="i" class="px-6 py-4 flex items-center justify-between">
            <div class="space-y-2 flex-1">
              <div class="h-4 w-40 bg-gray-200 dark:bg-gray-700 rounded" />
              <div class="h-3 w-24 bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
            <div class="h-5 w-5 bg-gray-200 dark:bg-gray-700 rounded" />
          </div>
        </div>
      </div>
    </div>

    <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 mb-6">
      <p class="text-red-800 dark:text-red-300">{{ error }}</p>
    </div>

    <template v-else-if="usage && (!showPartnerPortal || effectiveAccountId)">
      <!-- Usage cards -->
      <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <UsageCard
          label="Tiendas"
          :used="usage.usage.storesUsed"
          :limit="usage.usage.storesLimit"
          :to="usageCardTo({ path: '/admin/integraciones', query: { tab: 'stores' } })"
        />
        <UsageCard
          label="OpenAPI"
          :used="usage.usage.openApiUsed"
          :limit="usage.usage.openApiLimit"
          :to="usageCardTo({ path: '/admin/integraciones', query: { tab: 'apis' } })"
        />
        <UsageCard
          label="Canales"
          :used="usage.usage.channelsUsed"
          :limit="usage.usage.channelsLimit"
          :to="usageCardTo({ path: '/admin/integraciones', query: { tab: 'channels' } })"
        />
        <UsageCard
          label="Canales de venta"
          :used="usage.usage.channelDestinationsUsed ?? 0"
          :limit="usage.usage.channelDestinationsLimit"
          :to="usageCardTo('/admin/channels/destinations')"
        />
        <UsageCard
          label="Data pipeline (catálogo)"
          :used="usage.usage.catalogDataPipelinesUsed ?? 0"
          :limit="usage.usage.catalogDataPipelinesLimit"
          :to="usageCardTo('/admin/data-pipeline')"
        />
        <UsageCard
          label="Data pipeline (pedidos)"
          :used="usage.usage.ordersDataPipelinesUsed ?? 0"
          :limit="usage.usage.ordersDataPipelinesLimit"
          :to="usageCardTo('/admin/data-pipeline')"
        />
        <UsageCard
          label="Miembros"
          :used="usage.usage.membersUsed"
          :limit="usage.usage.membersLimit"
          :to="usageCardTo('/admin/members')"
        />
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue'
import { useAppStore } from '../../stores/appStore'
import apiService from '../../services/api'
import UsageCard from '../../components/usage/UsageCard.vue'
import StoreConnectionRequired from '../../components/store/StoreConnectionRequired.vue'
import { usePartnerProgramView } from '../../composables/usePartnerProgramView'
import { useAdminPaths } from '../../composables/useAdminPaths'

const props = defineProps({
  /** Cuando viene del slide-over de Cuentas programa: cuenta a consultar sin depender del query de la ruta. */
  embeddedViewAccountId: { type: String, default: '' }
})

const appStore = useAppStore()
const { toPath } = useAdminPaths()
const { showPartnerPortal, partnerViewAccountId } = usePartnerProgramView()

const isEmbeddedUsage = computed(() => Boolean(String(props.embeddedViewAccountId || '').trim()))
const effectiveAccountId = computed(
  () => String(props.embeddedViewAccountId || '').trim() || partnerViewAccountId.value
)

const usage = ref(null)
const loading = ref(true)
const error = ref(null)
const myIntegrations = ref([])
const loadingIntegrations = ref(false)

function usageCardTo(dest) {
  if (showPartnerPortal.value) return null
  return dest
}

async function load() {
  if (showPartnerPortal.value && !effectiveAccountId.value) {
    usage.value = null
    loading.value = false
    error.value = null
    return
  }
  loading.value = true
  error.value = null
  try {
    const viewId = effectiveAccountId.value || null
    usage.value = await appStore.fetchUsage(true, viewId)
  } catch (err) {
    error.value = err.response?.data?.error || err.response?.data?.message || 'Error al cargar el uso'
    usage.value = null
  } finally {
    loading.value = false
  }
}

async function loadMyIntegrations() {
  if (showPartnerPortal.value) {
    myIntegrations.value = [{ _p: 1 }]
    loadingIntegrations.value = false
    return
  }
  loadingIntegrations.value = true
  try {
    myIntegrations.value = await apiService.getMyIntegrations()
  } catch {
    myIntegrations.value = []
  } finally {
    loadingIntegrations.value = false
  }
}

watch(
  [showPartnerPortal, partnerViewAccountId, () => props.embeddedViewAccountId],
  async () => {
    await loadMyIntegrations()
    await load()
  },
  { immediate: true }
)
</script>

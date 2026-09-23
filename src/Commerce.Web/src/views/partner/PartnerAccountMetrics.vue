<template>
  <div :class="embedded ? 'px-1 py-0' : 'max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8'">
    <div v-if="!embedded" class="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white">Métricas de cuenta</h1>
        <p class="mt-1 text-gray-600 dark:text-gray-400 text-sm font-mono break-all">{{ accountId }}</p>
      </div>
      <div class="flex flex-wrap gap-2">
        <router-link
          :to="toPath('partner/accounts')"
          class="text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
        >
          ← Cuentas
        </router-link>
        <router-link
          :to="toPath(`partner/accounts/${accountId}/consumption`)"
          class="text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
        >
          Historial de consumo
        </router-link>
      </div>
    </div>

    <div v-if="loading" class="space-y-8">
      <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="i in 6" :key="i" class="p-4 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 animate-pulse">
          <div class="h-4 w-20 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
          <div class="h-8 w-16 bg-gray-200 dark:bg-gray-700 rounded" />
        </div>
      </div>
    </div>
    <div v-else-if="err" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
      <p class="text-red-800 dark:text-red-300">{{ err }}</p>
    </div>
    <div v-else-if="usage" class="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <UsageCard label="Tiendas" :used="usage.usage.storesUsed" :limit="usage.usage.storesLimit" />
      <UsageCard label="OpenAPI" :used="usage.usage.openApiUsed" :limit="usage.usage.openApiLimit" />
      <UsageCard label="Canales" :used="usage.usage.channelsUsed" :limit="usage.usage.channelsLimit" />
      <UsageCard
        label="Canales de venta"
        :used="usage.usage.channelDestinationsUsed ?? 0"
        :limit="usage.usage.channelDestinationsLimit"
      />
      <UsageCard
        label="Data pipeline (catálogo)"
        :used="usage.usage.catalogDataPipelinesUsed ?? 0"
        :limit="usage.usage.catalogDataPipelinesLimit"
      />
      <UsageCard
        label="Data pipeline (pedidos)"
        :used="usage.usage.ordersDataPipelinesUsed ?? 0"
        :limit="usage.usage.ordersDataPipelinesLimit"
      />
      <UsageCard label="Miembros" :used="usage.usage.membersUsed" :limit="usage.usage.membersLimit" />
      <div class="p-4 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
        <p class="text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">Créditos (periodo)</p>
        <p class="text-2xl font-bold text-gray-900 dark:text-white">
          {{ usage.credits?.creditsUsed ?? 0 }}
          <span v-if="usage.credits?.creditsLimit != null" class="text-lg font-normal text-gray-500">
            / {{ usage.credits.creditsLimit }}
          </span>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import apiService from '../../services/api'
import { useAdminPaths } from '../../composables/useAdminPaths'
import UsageCard from '../../components/usage/UsageCard.vue'

const props = defineProps({
  /** Montado dentro del slide-over de Cuentas programa (sin cabecera de página ni enlaces). */
  embedded: { type: Boolean, default: false },
  /** Si se informa, sustituye a <code>route.params.accountId</code>. */
  accountIdProp: { type: String, default: '' }
})

const route = useRoute()
const { toPath } = useAdminPaths()

const accountId = computed(() => String(props.accountIdProp || route.params.accountId || ''))
const usage = ref(null)
const loading = ref(true)
const err = ref(null)

async function load() {
  const id = accountId.value
  if (!id) return
  loading.value = true
  err.value = null
  try {
    usage.value = await apiService.getAccountUsage(id)
  } catch (e) {
    err.value = e.response?.data?.error || e.message || 'Error al cargar métricas'
    usage.value = null
  } finally {
    loading.value = false
  }
}

watch(accountId, load, { immediate: true })
</script>

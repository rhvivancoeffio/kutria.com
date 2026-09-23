<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8">
        <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Monitor de Webhooks</h1>
        <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          Log de webhooks recibidos. Mercado Libre usa partición global: elegí el filtro <strong>Mercado Libre</strong> para verlos (no aparecen en «Todos»).
        </p>
      </div>

      <!-- Filters -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-4 sm:mb-6">
        <div class="flex flex-col sm:flex-row sm:flex-wrap gap-4 items-stretch sm:items-end">
          <div class="flex-1 min-w-0 sm:min-w-[180px]">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Provider
            </label>
            <select
              v-model="filterProvider"
              @change="loadLogs"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="mercadolibre">Mercado Libre</option>
              <option value="vtex">VTEX</option>
              <option value="shopify">Shopify</option>
              <option value="stripe">Stripe</option>
              <option value="slack">Slack</option>
              <option value="whatsapp">WhatsApp</option>
            </select>
          </div>
          <div class="flex-1 min-w-0 sm:min-w-[160px]">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Desde
            </label>
            <input
              v-model="filterFrom"
              type="date"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500"
              @change="loadLogs"
            />
          </div>
          <div class="flex-1 min-w-0 sm:min-w-[160px]">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Hasta
            </label>
            <input
              v-model="filterTo"
              type="date"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500"
              @change="loadLogs"
            />
          </div>
          <div class="flex flex-col sm:flex-row gap-2">
            <button
              type="button"
              :disabled="loading"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation bg-primary-600 hover:bg-primary-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              @click="loadLogs"
            >
              <span v-if="loading" class="inline-flex items-center justify-center gap-2">
                <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                Cargando...
              </span>
              <span v-else>Actualizar</span>
            </button>
          </div>
        </div>
      </div>

    <!-- Loading -->
    <div v-if="loading" class="space-y-3 sm:space-y-4">
      <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
    </div>

    <!-- Empty state -->
    <div
      v-else-if="!items.length"
      class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
    >
      <svg
        class="mx-auto h-12 w-12 text-gray-400"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.172-1.171a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.172 1.171z"
        />
      </svg>
      <h3 class="mt-4 text-base sm:text-lg font-medium text-gray-900 dark:text-white">No hay webhooks registrados</h3>
      <p class="mt-2 text-sm sm:text-base text-gray-500 dark:text-gray-400">
        Los webhooks aparecerán aquí cuando las integraciones reciban eventos. Activa WebhookLogging en la configuración.
      </p>
    </div>

    <!-- Webhook logs list -->
    <div v-else class="space-y-4 md:space-y-0">
      <!-- Mobile: cards -->
      <div class="md:hidden space-y-3">
        <div
          v-for="log in items"
          :key="log.id"
          class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0 flex-1">
              <span class="inline-block px-2.5 py-0.5 rounded-full text-xs font-medium bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300 capitalize">
                {{ log.provider }}
              </span>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(log.receivedAt) }}</p>
              <p class="mt-1 text-xs text-gray-600 dark:text-gray-300 font-mono truncate" :title="log.rawBody">
                {{ truncate(log.rawBody, 80) }}
              </p>
            </div>
            <button
              type="button"
              class="p-2 shrink-0 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-colors touch-manipulation"
              :title="expandedId === log.id ? 'Ocultar' : 'Ver detalle'"
              @click="toggleExpand(log.id)"
            >
              <svg
                class="w-4 h-4 transition-transform"
                :class="{ 'rotate-180': expandedId === log.id }"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </button>
          </div>
          <div v-if="expandedId === log.id" class="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700 space-y-3">
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Raw Body</label>
              <pre class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200 max-h-48 overflow-y-auto">{{ formatJson(log.rawBody) }}</pre>
            </div>
            <div v-if="log.headersJson">
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Headers</label>
              <pre class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200">{{ formatJson(log.headersJson) }}</pre>
            </div>
          </div>
        </div>
      </div>

      <!-- Desktop: table with expandable rows -->
      <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div class="overflow-x-auto">
          <table v-resizable class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-700/50">
              <tr>
                <th class="px-4 lg:px-6 py-3 w-10" />
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Provider
                </th>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Fecha/Hora
                </th>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Raw Body
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <template v-for="log in items" :key="log.id">
                <tr
                  class="hover:bg-gray-50 dark:hover:bg-gray-700/30 cursor-pointer"
                  @click="toggleExpand(log.id)"
                >
                  <td class="px-4 lg:px-6 py-4">
                    <svg
                      class="w-4 h-4 text-gray-500 transition-transform"
                      :class="{ 'rotate-180': expandedId === log.id }"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                    </svg>
                  </td>
                  <td class="px-4 lg:px-6 py-4">
                    <span class="inline-block px-2.5 py-1 rounded-full text-xs font-medium bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300 capitalize">
                      {{ log.provider }}
                    </span>
                  </td>
                  <td class="px-4 lg:px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                    {{ formatDate(log.receivedAt) }}
                  </td>
                  <td class="px-4 lg:px-6 py-4 text-sm text-gray-600 dark:text-gray-300 max-w-[320px]">
                    <span class="block truncate font-mono" :title="log.rawBody">
                      {{ truncate(log.rawBody, 100) }}
                    </span>
                  </td>
                </tr>
                <tr v-if="expandedId === log.id" class="bg-gray-50 dark:bg-gray-800/50">
                  <td colspan="4" class="px-4 lg:px-6 py-4">
                    <div class="space-y-4">
                      <div>
                        <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Raw Body</label>
                        <pre class="text-xs bg-gray-100 dark:bg-gray-900 p-4 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200 max-h-64 overflow-y-auto">{{ formatJson(log.rawBody) }}</pre>
                      </div>
                      <div v-if="log.headersJson">
                        <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Headers</label>
                        <pre class="text-xs bg-gray-100 dark:bg-gray-900 p-4 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200">{{ formatJson(log.headersJson) }}</pre>
                      </div>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
        <!-- Pagination (Next/Previous page) -->
        <div
          v-if="canShowPagination"
          class="px-4 lg:px-6 py-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-end"
        >
          <div class="flex gap-2">
            <button
              type="button"
              :disabled="!canGoPrevious"
              class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
              @click="goToPreviousPage"
            >
              Anterior
            </button>
            <button
              type="button"
              :disabled="!hasMore"
              class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
              @click="goToNextPage"
            >
              Siguiente
            </button>
          </div>
        </div>
      </div>

      <!-- Mobile pagination -->
      <div
        v-if="canShowPagination && items.length"
        class="md:hidden px-4 py-3 flex items-center justify-end bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <div class="flex gap-2">
          <button
            type="button"
            :disabled="!canGoPrevious"
            class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 disabled:opacity-50"
            @click="goToPreviousPage"
          >
            Anterior
          </button>
          <button
            type="button"
            :disabled="!hasMore"
            class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 disabled:opacity-50"
            @click="goToNextPage"
          >
            Siguiente
          </button>
        </div>
      </div>
    </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import apiService from '../../services/api'

const loading = ref(false)
const items = ref([])
const pageSize = ref(20)
const continuationToken = ref(null)
const tokenUsedForCurrentPage = ref(null)
const tokenStack = ref([])
const hasMore = ref(false)
const filterProvider = ref('')
const filterFrom = ref('')
const filterTo = ref('')
const expandedId = ref(null)

const canGoPrevious = computed(() => tokenStack.value.length > 0)
const canShowPagination = computed(() => canGoPrevious.value || hasMore.value)

const loadLogs = async (token = undefined) => {
  if (token === undefined) {
    tokenStack.value = []
    continuationToken.value = null
    tokenUsedForCurrentPage.value = null
  }
  loading.value = true
  try {
    const params = { pageSize: pageSize.value }
    const tokenToUse = token !== undefined ? token : continuationToken.value
    if (tokenToUse != null) params.continuationToken = tokenToUse
    tokenUsedForCurrentPage.value = tokenToUse
    if (filterProvider.value) params.provider = filterProvider.value
    if (filterFrom.value) params.from = filterFrom.value
    if (filterTo.value) params.to = `${filterTo.value}T23:59:59.999Z`
    const result = await apiService.getWebhookLogs(params)
    items.value = result?.items ?? []
    continuationToken.value = result?.continuationToken ?? null
    hasMore.value = result?.hasMore ?? false
  } catch {
    items.value = []
    hasMore.value = false
  } finally {
    loading.value = false
  }
}

const goToNextPage = async () => {
  tokenStack.value.push(tokenUsedForCurrentPage.value)
  await loadLogs(continuationToken.value)
}

const goToPreviousPage = async () => {
  const prevToken = tokenStack.value.pop()
  await loadLogs(prevToken ?? null)
}

const toggleExpand = (id) => {
  expandedId.value = expandedId.value === id ? null : id
}

const formatDate = (d) => {
  if (!d) return '-'
  return new Date(d).toLocaleString()
}

const truncate = (str, len) => {
  if (!str) return '-'
  if (str.length <= len) return str
  return str.slice(0, len) + '...'
}

const formatJson = (str) => {
  if (!str) return ''
  try {
    return JSON.stringify(JSON.parse(str), null, 2)
  } catch {
    return str
  }
}

onMounted(() => {
  loadLogs()
})
</script>

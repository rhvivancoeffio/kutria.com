<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8">
        <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Ejecuciones de tools</h1>
        <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          Historial de ejecuciones de workflows y tools (paginated)
        </p>
      </div>

      <!-- Filters -->
      <div class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-4 sm:mb-6">
        <div class="flex flex-col sm:flex-row sm:flex-wrap gap-4 items-stretch sm:items-end">
          <div class="flex-1 min-w-0 sm:min-w-[200px]">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Server (MCP)</label>
            <SearchableSelect
              v-model="filterMcpId"
              :options="mcpsOptions"
              placeholder="Todos"
              search-placeholder="Buscar servidor..."
              empty-message="No hay servidores"
              always-show-empty-option
              teleport
              class="[&_.input-field]:w-full [&_.input-field]:px-3 [&_.input-field]:py-2 [&_.input-field]:border [&_.input-field]:border-gray-300 [&_.input-field]:dark:border-gray-600 [&_.input-field]:rounded-lg [&_.input-field]:bg-white [&_.input-field]:dark:bg-gray-700 [&_.input-field]:text-gray-900 [&_.input-field]:dark:text-gray-100 [&_.input-field]:focus:outline-none [&_.input-field]:focus:ring-2 [&_.input-field]:focus:ring-primary-500"
            />
          </div>
          <div class="flex-1 min-w-0 sm:min-w-[200px]" :class="{ 'opacity-50 pointer-events-none': !filterMcpId }">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Tool</label>
            <SearchableSelect
              v-model="filterToolId"
              :options="mcpToolsOptions"
              placeholder="Todos"
              search-placeholder="Buscar tool..."
              empty-message="No hay tools"
              always-show-empty-option
              teleport
              class="[&_.input-field]:w-full [&_.input-field]:px-3 [&_.input-field]:py-2 [&_.input-field]:border [&_.input-field]:border-gray-300 [&_.input-field]:dark:border-gray-600 [&_.input-field]:rounded-lg [&_.input-field]:bg-white [&_.input-field]:dark:bg-gray-700 [&_.input-field]:text-gray-900 [&_.input-field]:dark:text-gray-100 [&_.input-field]:focus:outline-none [&_.input-field]:focus:ring-2 [&_.input-field]:focus:ring-primary-500"
            />
          </div>
          <div class="flex-1 min-w-0 sm:min-w-[160px]">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Desde</label>
            <input
              v-model="filterFrom"
              type="datetime-local"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>
          <div class="flex-1 min-w-0 sm:min-w-[160px]">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Hasta</label>
            <input
              v-model="filterTo"
              type="datetime-local"
              class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>
          <div class="flex flex-col sm:flex-row gap-2">
            <button
              type="button"
              :disabled="loading"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation bg-primary-600 hover:bg-primary-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              @click="loadFirstPage"
            >
              <span v-if="loading" class="inline-flex items-center justify-center gap-2">
                <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                Cargando...
              </span>
              <span v-else>Buscar</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading && !items.length" class="space-y-3 sm:space-y-4">
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
            d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"
          />
        </svg>
        <h3 class="mt-4 text-base sm:text-lg font-medium text-gray-900 dark:text-white">No hay ejecuciones</h3>
        <p class="mt-2 text-sm sm:text-base text-gray-500 dark:text-gray-400">
          Las ejecuciones de tools y workflows aparecerán aquí.
        </p>
      </div>

      <!-- List -->
      <div v-else class="space-y-4 md:space-y-0">
        <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-700/50">
                <tr>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Inicio
                  </th>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Fin
                  </th>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Tool / Workflow
                  </th>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Estado
                  </th>
                  <th class="px-4 lg:px-6 py-3 w-20" />
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                <tr
                  v-for="row in items"
                  :key="row.id"
                  class="hover:bg-gray-50 dark:hover:bg-gray-700/30 cursor-pointer"
                  @click="goToToolDetail(row)"
                >
                  <td class="px-4 lg:px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                    {{ formatDate(row.startUtc) }}
                  </td>
                  <td class="px-4 lg:px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                    {{ formatDate(row.endUtc) }}
                  </td>
                  <td class="px-4 lg:px-6 py-4 text-sm text-gray-600 dark:text-gray-300">
                    <span class="font-mono text-xs">{{ shortId(row.toolId) }}</span>
                    <span v-if="row.isWorkflow" class="ml-1 text-xs text-primary-600 dark:text-primary-400">(workflow)</span>
                  </td>
                  <td class="px-4 lg:px-6 py-4">
                    <span
                      :class="row.success
                        ? 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-300'
                        : 'bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300'"
                      class="inline-block px-2.5 py-0.5 rounded-full text-xs font-medium"
                    >
                      {{ row.success ? 'OK' : 'Error' }}
                    </span>
                  </td>
                  <td class="px-4 lg:px-6 py-4">
                    <button
                      type="button"
                      class="p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20"
                      title="Ver detalle de la ejecución"
                      @click.stop="openDetail(row)"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div
            v-if="hasMore || tokenStack.length"
            class="px-4 lg:px-6 py-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between"
          >
            <button
              type="button"
              :disabled="!tokenStack.length"
              class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
              @click="goToPreviousPage"
            >
              Anterior
            </button>
            <button
              type="button"
              :disabled="!hasMore || loadingMore"
              class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-700"
              @click="loadMore"
            >
              <span v-if="loadingMore">Cargando...</span>
              <span v-else>Siguiente</span>
            </button>
          </div>
        </div>

        <!-- Mobile cards -->
        <div class="md:hidden space-y-3">
          <div
            v-for="row in items"
            :key="row.id"
            class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm card-hover"
            @click="goToToolDetail(row)"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0 flex-1">
                <span
                  :class="row.success
                    ? 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-300'
                    : 'bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300'"
                  class="inline-block px-2.5 py-0.5 rounded-full text-xs font-medium"
                >
                  {{ row.success ? 'OK' : 'Error' }}
                </span>
                <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(row.startUtc) }}</p>
                <p class="mt-1 text-xs font-mono text-gray-600 dark:text-gray-300">{{ shortId(row.toolId) }}</p>
              </div>
              <span class="text-primary-600 dark:text-primary-400 text-sm">Ver</span>
            </div>
          </div>
        </div>
        <div
          v-if="(hasMore || tokenStack.length) && items.length"
          class="md:hidden flex gap-2 justify-center py-4"
        >
          <button
            type="button"
            :disabled="!tokenStack.length"
            class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 disabled:opacity-50"
            @click="goToPreviousPage"
          >
            Anterior
          </button>
          <button
            type="button"
            :disabled="!hasMore || loadingMore"
            class="px-3 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 disabled:opacity-50"
            @click="loadMore"
          >
            {{ loadingMore ? 'Cargando...' : 'Siguiente' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Detail drawer -->
    <Teleport to="body">
      <div
        v-if="detailRow"
        class="fixed inset-0 z-50 overflow-hidden"
        aria-labelledby="drawer-title"
        role="dialog"
        aria-modal="true"
      >
        <div class="absolute inset-0 bg-gray-500/75 dark:bg-gray-900/75 transition-opacity" aria-hidden="true" />
        <div class="fixed inset-y-0 right-0 flex max-w-full pl-10">
          <div class="w-screen max-w-md">
            <div class="flex h-full flex-col bg-white dark:bg-gray-800 shadow-xl">
              <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700">
                <h2 id="drawer-title" class="text-lg font-semibold text-gray-900 dark:text-white">
                  Ejecución {{ shortId(detailRow.id) }}
                </h2>
                <button
                  type="button"
                  class="p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"
                  @click="closeDetail"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              <div class="flex-1 overflow-y-auto px-4 py-4 space-y-4">
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Inicio / Fin</label>
                  <p class="text-sm text-gray-900 dark:text-gray-100">
                    {{ formatDate(detailRow.startUtc) }} → {{ formatDate(detailRow.endUtc) }}
                  </p>
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Estado</label>
                  <span
                    :class="detailRow.success
                      ? 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-300'
                      : 'bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300'"
                    class="inline-block px-2.5 py-0.5 rounded-full text-xs font-medium"
                  >
                    {{ detailRow.success ? 'OK' : 'Error' }}
                  </span>
                  <p v-if="detailRow.errorMessage" class="mt-1 text-sm text-red-600 dark:text-red-400">
                    {{ detailRow.errorMessage }}
                  </p>
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Input</label>
                  <pre class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200 max-h-48 overflow-y-auto">{{ formatJson(detailRow.inputJson) }}</pre>
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Output</label>
                  <pre class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200 max-h-48 overflow-y-auto">{{ formatJson(detailRow.outputJson) }}</pre>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import apiService from '../../services/api'
import SearchableSelect from '../../components/SearchableSelect.vue'

const router = useRouter()

const loading = ref(false)
const loadingMore = ref(false)
const items = ref([])
const pageSize = ref(20)
const continuationToken = ref(null)
const tokenStack = ref([])
const hasMore = ref(false)
const mcps = ref([])
const mcpTools = ref([])
const filterMcpId = ref('')
const filterToolId = ref('')
const filterFrom = ref('')
const filterTo = ref('')
const detailRow = ref(null)

const tokenUsedForCurrentPage = ref(null)

const mcpsOptions = computed(() => [
  { value: '', label: 'Todos' },
  ...(mcps.value || []).map((m) => ({ value: m.id, label: m.name }))
])

const mcpToolsOptions = computed(() => [
  { value: '', label: 'Todos' },
  ...(mcpTools.value || []).map((t) => ({ value: t.id, label: t.title || t.name }))
])

const loadMcpTools = async () => {
  filterToolId.value = ''
  mcpTools.value = []
  if (filterMcpId.value) {
    try {
      const tools = await apiService.getMcpTools(filterMcpId.value)
      mcpTools.value = (tools ?? []).map((t) => ({ id: t.id, name: t.name, title: t.title }))
    } catch {
      mcpTools.value = []
    }
  }
}

watch(filterMcpId, loadMcpTools)

const buildParams = (token) => {
  const params = { pageSize: pageSize.value }
  if (token != null) params.continuationToken = token
  if (filterMcpId.value) params.mcpDefinitionId = filterMcpId.value
  if (filterToolId.value) params.toolId = filterToolId.value
  if (filterFrom.value) params.fromUtc = new Date(filterFrom.value).toISOString()
  if (filterTo.value) params.toUtc = new Date(filterTo.value).toISOString()
  return params
}

const loadFirstPage = async () => {
  tokenStack.value = []
  continuationToken.value = null
  tokenUsedForCurrentPage.value = null
  loading.value = true
  try {
    const result = await apiService.getToolExecutions(buildParams(null))
    items.value = result?.items ?? []
    continuationToken.value = result?.nextContinuationToken ?? null
    hasMore.value = result?.hasMore ?? false
    tokenUsedForCurrentPage.value = null
  } catch {
    items.value = []
    hasMore.value = false
  } finally {
    loading.value = false
  }
}

const loadPage = async (token) => {
  loadingMore.value = true
  try {
    const result = await apiService.getToolExecutions(buildParams(token))
    items.value = result?.items ?? []
    continuationToken.value = result?.nextContinuationToken ?? null
    hasMore.value = result?.hasMore ?? false
    tokenUsedForCurrentPage.value = token
  } catch {
    items.value = []
    hasMore.value = false
  } finally {
    loadingMore.value = false
  }
}

const loadMore = () => {
  if (!hasMore.value || !continuationToken.value) return
  tokenStack.value.push(tokenUsedForCurrentPage.value)
  loadPage(continuationToken.value)
}

const goToPreviousPage = async () => {
  const prevToken = tokenStack.value.pop()
  if (prevToken === undefined) return
  loadingMore.value = true
  try {
    const result = await apiService.getToolExecutions(buildParams(prevToken ?? null))
    items.value = result?.items ?? []
    continuationToken.value = result?.nextContinuationToken ?? null
    hasMore.value = result?.hasMore ?? false
    tokenUsedForCurrentPage.value = prevToken ?? null
  } catch {
    items.value = []
  } finally {
    loadingMore.value = false
  }
}

function goToToolDetail(row) {
  const mcpId = row.mcpDefinitionId
  const toolId = row.toolId
  if (mcpId && toolId) {
    router.push({ name: 'AdminMcpToolEdit', params: { mcpId, toolId } })
  } else {
    openDetail(row)
  }
}

const openDetail = (row) => {
  detailRow.value = row
}

const closeDetail = () => {
  detailRow.value = null
}

const formatDate = (d) => {
  if (!d) return '-'
  return new Date(d).toLocaleString()
}

const shortId = (id) => {
  if (!id) return '-'
  const s = typeof id === 'string' ? id : String(id)
  return s.length > 12 ? s.slice(0, 8) + '…' : s
}

const formatJson = (str) => {
  if (str == null || str === '') return ''
  try {
    return JSON.stringify(JSON.parse(str), null, 2)
  } catch {
    return str
  }
}

onMounted(async () => {
  try {
    const list = await apiService.getMcps()
    mcps.value = list ?? []
  } catch {
    mcps.value = []
  }
  loadFirstPage()
})
</script>

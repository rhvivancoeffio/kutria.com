<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8 flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Outbox transaccional</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Mensajes pendientes o recientes en el outbox de catálogo y de pedidos (workspace activo).
          </p>
        </div>
        <button
          type="button"
          :disabled="loading"
          class="shrink-0 px-4 py-2.5 min-h-[44px] bg-primary-600 hover:bg-primary-700 text-white rounded-lg font-medium disabled:opacity-50 transition-colors touch-manipulation"
          @click="load"
        >
          {{ loading ? 'Cargando…' : 'Actualizar' }}
        </button>
      </div>

      <div
        class="mb-6 admin-surface p-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3 text-sm"
        role="search"
        aria-label="Filtros de outbox"
      >
        <label class="block min-w-0">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Estado</span>
          <select
            v-model="filterOutboxStatus"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="onFilterChange"
          >
            <option value="">Todos</option>
            <option value="pending">Pendiente</option>
            <option value="processed">Procesado</option>
          </select>
        </label>
        <label class="block min-w-0">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Tipo de evento</span>
          <select
            v-model="filterEventKind"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="onFilterChange"
          >
            <option value="">Todos</option>
            <option value="created">Create</option>
            <option value="updated">Update</option>
            <option value="deleted">Delete</option>
          </select>
        </label>
        <label class="block min-w-0 sm:col-span-2 lg:col-span-2">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Fecha (creado ≥)</span>
          <select
            v-model="filterPeriod"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="onFilterChange"
          >
            <option value="">Sin límite</option>
            <option value="today">Hoy (UTC)</option>
            <option value="last7days">Última semana</option>
            <option value="last15days">Últimos 15 días</option>
          </select>
        </label>
        <label class="block min-w-0 sm:col-span-2 lg:col-span-1">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Filas por página</span>
          <select
            v-model.number="pageSize"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="onPageSizeChange"
          >
            <option :value="25">25</option>
            <option :value="50">50</option>
            <option :value="100">100</option>
          </select>
        </label>
      </div>

      <div
        v-if="workspaceError"
        class="mb-4 rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-900/20 px-4 py-3 text-sm text-amber-900 dark:text-amber-100"
        role="alert"
      >
        {{ workspaceError }}
        <router-link
          :to="paths.toPath('workspaces')"
          class="ml-2 font-medium text-primary-700 dark:text-primary-300 underline"
        >
          Elegir workspace
        </router-link>
      </div>

      <div class="border-b border-gray-200 dark:border-gray-700 mb-6">
        <nav class="flex flex-wrap gap-1 -mb-px" aria-label="Secciones" role="tablist">
          <button
            type="button"
            class="px-4 py-3 text-sm font-medium border-b-2 transition-colors"
            :class="
              activeTab === 'catalog'
                ? 'border-primary-600 text-primary-700 dark:text-primary-300'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
            "
            role="tab"
            :aria-selected="activeTab === 'catalog'"
            @click="activeTab = 'catalog'"
          >
            Productos (catálogo)
          </button>
          <button
            type="button"
            class="px-4 py-3 text-sm font-medium border-b-2 transition-colors"
            :class="
              activeTab === 'orders'
                ? 'border-primary-600 text-primary-700 dark:text-primary-300'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
            "
            role="tab"
            :aria-selected="activeTab === 'orders'"
            @click="activeTab = 'orders'"
          >
            Pedidos
          </button>
        </nav>
      </div>

      <div role="tabpanel">
        <div v-if="loading && !currentItems.length" class="space-y-3">
          <div v-for="i in 6" :key="i" class="h-14 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
        </div>

        <div v-else-if="!workspaceError && !currentItems.length" class="admin-empty-state admin-card p-8 text-center">
          <p class="text-gray-600 dark:text-gray-400">
            {{
              hasActiveFilters
                ? 'No hay filas con estos filtros en esta pestaña.'
                : 'No hay filas recientes en este outbox para el workspace activo.'
            }}
          </p>
        </div>

        <div v-else-if="currentItems.length" class="admin-surface overflow-hidden hidden md:block">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-700/50">
                <tr>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Tipo
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Entidad
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Estado
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Intentos
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider max-w-[180px]">
                    Último error
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Creado
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider max-w-[200px]">
                    Payload
                  </th>
                  <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Acción
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="row in currentItems" :key="row.id" class="hover:bg-gray-50 dark:hover:bg-gray-800/40">
                  <td class="px-4 py-3 text-sm text-gray-900 dark:text-gray-100 font-mono text-xs max-w-[220px] truncate" :title="row.messageType">
                    {{ row.messageType || '—' }}
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 font-mono text-xs">
                    {{ entityId(row) || '—' }}
                  </td>
                  <td class="px-4 py-3">
                    <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', row.processedAt ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-800 dark:text-emerald-200' : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300']">
                      {{ row.processedAt ? 'Procesado' : 'Pendiente' }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300">
                    {{ row.attemptCount }}
                  </td>
                  <td class="px-4 py-3 text-sm text-red-700 dark:text-red-300 max-w-[180px] truncate" :title="row.lastError || ''">
                    {{ row.lastError || '—' }}
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 whitespace-nowrap">
                    {{ formatDate(row.createdAt) }}
                  </td>
                  <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400 font-mono max-w-[200px] truncate" :title="row.payloadJson">
                    {{ truncatePayload(row.payloadJson) }}
                  </td>
                  <td class="px-4 py-3 text-right">
                    <button
                      type="button"
                      class="text-primary-600 dark:text-primary-400 text-sm font-medium hover:underline"
                      @click="openDetail(row)"
                    >
                      Ver detalle
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-if="currentItems.length" class="md:hidden space-y-3">
          <div v-for="row in currentItems" :key="row.id" class="admin-card p-4 space-y-2">
            <div class="text-xs font-mono text-gray-500 dark:text-gray-400 break-all">{{ row.messageType }}</div>
            <div class="text-sm text-gray-900 dark:text-white">
              {{ entityId(row) || 'Sin id de entidad' }}
            </div>
            <div class="flex flex-wrap gap-2 text-sm text-gray-600 dark:text-gray-400">
              <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', row.processedAt ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-800 dark:text-emerald-200' : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300']">
                {{ row.processedAt ? 'Procesado' : 'Pendiente' }}
              </span>
              <span>Intentos: {{ row.attemptCount }}</span>
            </div>
            <div v-if="row.lastError" class="text-xs text-red-700 dark:text-red-300 break-words">
              {{ row.lastError }}
            </div>
            <div class="text-xs text-gray-500">{{ formatDate(row.createdAt) }}</div>
            <button
              type="button"
              class="w-full mt-2 px-3 py-2 text-sm font-medium text-primary-600 dark:text-primary-400 border border-primary-200 dark:border-primary-800 rounded-lg hover:bg-primary-50 dark:hover:bg-primary-900/20"
              @click="openDetail(row)"
            >
              Ver detalle
            </button>
          </div>
        </div>

        <div
          v-if="!workspaceError && tabTotalCount > 0"
          class="mt-4 admin-surface px-4 py-3 flex flex-col sm:flex-row sm:flex-wrap sm:items-center sm:justify-between gap-3 border border-gray-200 dark:border-gray-700 rounded-lg"
        >
          <p class="text-sm text-gray-600 dark:text-gray-400">
            Mostrando {{ rangeStart }}–{{ rangeEnd }} de {{ tabTotalCount }} filas
          </p>
          <div v-if="totalPages > 1" class="flex flex-wrap items-center gap-2">
            <button
              type="button"
              :disabled="currentPageIndex <= 1 || loading"
              class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600 touch-manipulation"
              @click="goToPage(currentPageIndex - 1)"
            >
              Anterior
            </button>
            <span class="text-sm text-gray-600 dark:text-gray-400">
              Página {{ currentPageIndex }} de {{ totalPages }}
            </span>
            <button
              type="button"
              :disabled="currentPageIndex >= totalPages || loading"
              class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600 touch-manipulation"
              @click="goToPage(currentPageIndex + 1)"
            >
              Siguiente
            </button>
          </div>
        </div>
      </div>
    </div>

    <ObservabilityOutboxDetailSlider
      v-model="detailOpen"
      :row="detailRow"
      :outbox-kind="activeTab"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import apiService from '@/services/api'
import { useAdminPaths } from '@/composables/useAdminPaths'
import ObservabilityOutboxDetailSlider from '@/components/observability/ObservabilityOutboxDetailSlider.vue'

const paths = useAdminPaths()
const loading = ref(false)
const workspaceError = ref('')
const activeTab = ref('catalog')
const catalogItems = ref([])
const orderItems = ref([])
const catalogPage = ref(1)
const orderPage = ref(1)
const catalogTotalCount = ref(0)
const orderTotalCount = ref(0)
const pageSize = ref(25)
const detailOpen = ref(false)
const detailRow = ref(null)

const filterOutboxStatus = ref('')
const filterEventKind = ref('')
const filterPeriod = ref('')

const hasActiveFilters = computed(
  () =>
    Boolean(filterOutboxStatus.value || filterEventKind.value || filterPeriod.value)
)

const currentItems = computed(() =>
  activeTab.value === 'catalog' ? catalogItems.value : orderItems.value
)

const tabTotalCount = computed(() =>
  activeTab.value === 'catalog' ? catalogTotalCount.value : orderTotalCount.value
)

const currentPageIndex = computed(() =>
  activeTab.value === 'catalog' ? catalogPage.value : orderPage.value
)

const totalPages = computed(() =>
  Math.max(1, Math.ceil(tabTotalCount.value / pageSize.value))
)

const rangeStart = computed(() => {
  const n = currentItems.value.length
  if (!n || !tabTotalCount.value) return 0
  return (currentPageIndex.value - 1) * pageSize.value + 1
})

const rangeEnd = computed(() => {
  const n = currentItems.value.length
  if (!n) return 0
  return (currentPageIndex.value - 1) * pageSize.value + n
})

function buildOutboxQueryParams() {
  const q = {}
  if (filterOutboxStatus.value?.trim()) q.status = filterOutboxStatus.value.trim()
  if (filterEventKind.value?.trim()) q.eventKind = filterEventKind.value.trim()
  if (filterPeriod.value?.trim()) q.period = filterPeriod.value.trim()
  return q
}

function onFilterChange() {
  catalogPage.value = 1
  orderPage.value = 1
  load()
}

function onPageSizeChange() {
  catalogPage.value = 1
  orderPage.value = 1
  load()
}

function goToPage(p) {
  if (p < 1 || p > totalPages.value) return
  if (activeTab.value === 'catalog') catalogPage.value = p
  else orderPage.value = p
  load()
}

function openDetail(row) {
  detailRow.value = row
  detailOpen.value = true
}

watch(activeTab, () => {
  detailOpen.value = false
  detailRow.value = null
  load()
})

function formatDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString()
}

function entityId(row) {
  if (activeTab.value === 'catalog') return row.channelCatalogProductId
  return row.channelSaleOrderId
}

function truncatePayload(json) {
  if (!json) return '—'
  const s = String(json)
  return s.length > 80 ? `${s.slice(0, 80)}…` : s
}

async function load() {
  loading.value = true
  workspaceError.value = ''
  try {
    const base = buildOutboxQueryParams()
    const common = { ...base, pageSize: pageSize.value }
    if (activeTab.value === 'catalog') {
      const res = await apiService.getObservabilityCatalogProductOutbox({
        ...common,
        page: catalogPage.value
      })
      catalogItems.value = res.items
      catalogPage.value = res.page
      catalogTotalCount.value = res.totalCount
      if (res.pageSize) pageSize.value = res.pageSize
    } else {
      const res = await apiService.getObservabilitySaleOrderOutbox({
        ...common,
        page: orderPage.value
      })
      orderItems.value = res.items
      orderPage.value = res.page
      orderTotalCount.value = res.totalCount
      if (res.pageSize) pageSize.value = res.pageSize
    }
  } catch (e) {
    const status = e?.response?.status
    const msg = e?.response?.data?.error
    if (status === 400 && msg) {
      workspaceError.value = typeof msg === 'string' ? msg : 'Se requiere un workspace activo.'
    } else {
      workspaceError.value = ''
    }
    catalogItems.value = []
    orderItems.value = []
    catalogTotalCount.value = 0
    orderTotalCount.value = 0
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

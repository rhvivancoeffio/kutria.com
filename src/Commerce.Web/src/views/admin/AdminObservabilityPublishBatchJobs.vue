<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8 flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Jobs de publicación</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Lotes recientes hacia destinos de publicación (workspace activo). Desde aquí puedes abrir el detalle por destino.
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
        aria-label="Filtros de jobs"
      >
        <label class="block min-w-0">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Estado</span>
          <select
            v-model="filterStatus"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="load"
          >
            <option value="">Todos</option>
            <option value="Pending">Pending</option>
            <option value="Processing">Processing</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="CompletedWithErrors">CompletedWithErrors</option>
          </select>
        </label>
        <label class="block min-w-0">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Destino</span>
          <select
            v-model="filterDestinationId"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="load"
          >
            <option value="">Todos</option>
            <option v-for="d in destinations" :key="d.id" :value="String(d.id)">{{ d.name }}</option>
          </select>
        </label>
        <label class="block min-w-0">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Servicio</span>
          <select
            v-model="filterServiceKind"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="load"
          >
            <option value="">Todos</option>
            <option value="Create">Create</option>
            <option value="Update">Update</option>
            <option value="Delete">Delete</option>
            <option value="ChangeStatus">ChangeStatus</option>
            <option value="UpdateStock">UpdateStock</option>
            <option value="UpdatePrice">UpdatePrice</option>
            <option value="UploadImages">UploadImages</option>
          </select>
        </label>
        <label class="block min-w-0 sm:col-span-2 lg:col-span-2">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Fecha (creado ≥)</span>
          <select
            v-model="filterPeriod"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2"
            @change="load"
          >
            <option value="">Sin límite</option>
            <option value="today">Hoy (UTC)</option>
            <option value="last7days">Última semana</option>
            <option value="last15days">Últimos 15 días</option>
          </select>
        </label>
      </div>

      <div v-if="loading && !items.length" class="space-y-3">
        <div v-for="i in 6" :key="i" class="h-14 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <div v-else-if="!items.length" class="admin-empty-state admin-card p-8 text-center">
        <p class="text-gray-600 dark:text-gray-400">
          {{ hasActiveFilters ? 'No hay jobs con estos filtros.' : 'No hay jobs recientes en este workspace.' }}
        </p>
        <router-link
          :to="paths.channels"
          class="mt-4 inline-block text-primary-600 dark:text-primary-400 font-medium hover:underline"
        >
          Ir a canales
        </router-link>
      </div>

      <div v-else class="admin-surface overflow-hidden hidden md:block">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-700/50">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Destino
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Estado
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Servicio
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Ítems
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Definición
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Inicio
                </th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Acción
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="row in items" :key="row.id" class="hover:bg-gray-50 dark:hover:bg-gray-800/40">
                <td class="px-4 py-3 text-sm text-gray-900 dark:text-gray-100 font-medium">
                  {{ row.channelDestinationName || '—' }}
                </td>
                <td class="px-4 py-3">
                  <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(row.status)]">
                    {{ row.status }}
                  </span>
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300">
                  {{ row.serviceKind }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300">
                  {{ row.itemCount }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-500 dark:text-gray-400 max-w-[200px] truncate" :title="row.jobDefinitionKey">
                  {{ row.jobDefinitionKey || '—' }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 whitespace-nowrap">
                  {{ formatDate(row.createdAt) }}
                </td>
                <td class="px-4 py-3 text-right">
                  <router-link
                    :to="jobLink(row)"
                    class="text-primary-600 dark:text-primary-400 text-sm font-medium hover:underline"
                  >
                    Ver job
                  </router-link>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- mobile cards -->
      <div v-if="items.length" class="md:hidden space-y-3">
        <div v-for="row in items" :key="row.id" class="admin-card p-4 space-y-2">
          <div class="font-medium text-gray-900 dark:text-white">{{ row.channelDestinationName }}</div>
          <div class="flex flex-wrap gap-2 text-sm text-gray-600 dark:text-gray-400">
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', statusClass(row.status)]">{{ row.status }}</span>
            <span>{{ row.serviceKind }} · {{ row.itemCount }} ítems</span>
          </div>
          <div class="text-xs text-gray-500">{{ formatDate(row.createdAt) }}</div>
          <router-link
            :to="jobLink(row)"
            class="inline-block text-primary-600 dark:text-primary-400 text-sm font-medium"
          >
            Ver job
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import apiService from '@/services/api'
import { useAdminPaths } from '@/composables/useAdminPaths'

const paths = useAdminPaths()
const loading = ref(false)
const items = ref([])
const destinations = ref([])

const filterStatus = ref('')
const filterDestinationId = ref('')
const filterServiceKind = ref('')
const filterPeriod = ref('')

const hasActiveFilters = computed(
  () =>
    Boolean(
      filterStatus.value ||
        filterDestinationId.value ||
        filterServiceKind.value ||
        filterPeriod.value
    )
)

function normalizeJobRow(r) {
  return {
    id: r.id ?? r.Id,
    channelDestinationId: r.channelDestinationId ?? r.ChannelDestinationId,
    channelDestinationName: r.channelDestinationName ?? r.ChannelDestinationName ?? '',
    serviceKind: r.serviceKind ?? r.ServiceKind ?? '',
    status: r.status ?? r.Status ?? '',
    itemCount: r.itemCount ?? r.ItemCount ?? 0,
    errorMessage: r.errorMessage ?? r.ErrorMessage ?? null,
    createdAt: r.createdAt ?? r.CreatedAt ?? null,
    jobDefinitionKey: r.jobDefinitionKey ?? r.JobDefinitionKey ?? '',
    triggerKind: r.triggerKind ?? r.TriggerKind ?? ''
  }
}

function buildQueryParams() {
  const q = { take: 80 }
  if (filterStatus.value?.trim()) q.status = filterStatus.value.trim()
  if (filterDestinationId.value?.trim()) q.channelDestinationId = filterDestinationId.value.trim()
  if (filterServiceKind.value?.trim()) q.serviceKind = filterServiceKind.value.trim()
  if (filterPeriod.value?.trim()) q.period = filterPeriod.value.trim()
  return q
}

async function loadDestinations() {
  try {
    const list = await apiService.getChannelDestinations()
    destinations.value = (Array.isArray(list) ? list : [])
      .map((d) => ({
        id: d.id ?? d.Id,
        name: d.name ?? d.Name ?? 'Destino'
      }))
      .filter((d) => d.id)
  } catch {
    destinations.value = []
  }
}

function formatDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString()
}

function statusClass(status) {
  const s = String(status || '')
  if (s === 'Completed') return 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-800 dark:text-emerald-200'
  if (s === 'Failed') return 'bg-red-100 dark:bg-red-900/40 text-red-800 dark:text-red-200'
  if (s === 'CompletedWithErrors') return 'bg-amber-100 dark:bg-amber-900/40 text-amber-800 dark:text-amber-200'
  if (s === 'Processing') return 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-200'
  if (s === 'Pending') return 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300'
  return 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
}

function jobLink(row) {
  const dest = row.channelDestinationId
  const id = row.id
  return paths.toPath(`channels/destinations/${dest}/publication-jobs/${id}`)
}

async function load() {
  loading.value = true
  try {
    const raw = await apiService.getObservabilityPublishBatchJobs(buildQueryParams())
    items.value = (Array.isArray(raw) ? raw : []).map(normalizeJobRow)
  } catch {
    items.value = []
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await loadDestinations()
  await load()
})
</script>

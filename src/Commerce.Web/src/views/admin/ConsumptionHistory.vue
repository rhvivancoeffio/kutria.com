<template>
  <div
    :class="
      embedded
        ? 'flex h-full min-h-0 flex-col bg-gray-50 dark:bg-gray-900'
        : 'min-h-screen bg-gray-50 dark:bg-gray-900'
    "
  >
    <div
      :class="
        embedded
          ? 'flex min-h-0 flex-1 flex-col px-2 py-0 sm:px-3'
          : 'max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8'
      "
    >
      <div v-if="!embedded" class="mb-6 sm:mb-8">
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">
              Historial de consumo
            </h1>
            <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
              Eventos de créditos consumidos con detalle de tokens.
            </p>
            <p v-if="partnerViewAccountId" class="mt-1 text-xs font-mono text-gray-500 dark:text-gray-400 break-all">
              Cuenta: {{ partnerViewAccountId }}
            </p>
          </div>
          <router-link
            v-if="partnerViewAccountId"
            :to="toPath(`partner/accounts/${partnerViewAccountId}/metrics`)"
            class="inline-flex items-center text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
          >
            ← Métricas de cuenta
          </router-link>
          <router-link
            v-else
            to="/admin/billing"
            class="inline-flex items-center text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
          >
            ← Volver a Facturación
          </router-link>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" :class="embedded ? 'min-h-0 flex-1 space-y-3' : 'space-y-3'">
        <div v-for="i in 8" :key="i" class="h-14 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <!-- Empty -->
      <div
        v-else-if="!items.length"
        class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-8 sm:p-12 text-center"
      >
        <p class="text-gray-600 dark:text-gray-400">
          <template v-if="isProgramAccountConsumption">
            Aún no hay consumo registrado para esta cuenta cliente. Los eventos aparecen cuando el espacio TemplateProject usa
            créditos del plan (por ejemplo catálogo o pedidos, sincronización con canales externos o pipelines con
            coste según contratación).
          </template>
          <template v-else>
            No hay consumo registrado aún. En TemplateProject los movimientos reflejan créditos usados por tu cuenta al operar
            catálogo, pedidos, canales de venta o automatizaciones con IA, según lo que tengas activo y tu plan.
          </template>
        </p>
        <router-link
          v-if="!isProgramAccountConsumption"
          to="/admin/billing"
          class="mt-4 inline-block text-primary-600 dark:text-primary-400 font-medium hover:underline"
        >
          Ver facturación
        </router-link>
      </div>

      <!-- Table + Pagination -->
      <template v-else>
        <div
          :class="[
            'bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden',
            embedded ? 'flex min-h-0 flex-1 flex-col' : ''
          ]"
        >
          <div :class="embedded ? 'min-h-0 flex-1 overflow-x-auto overflow-y-auto' : 'overflow-x-auto'">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-700/50">
                <tr>
                  <th scope="col" class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Fecha
                  </th>
                  <th scope="col" class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Tipo
                  </th>
                  <th scope="col" class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Créditos
                  </th>
                  <th scope="col" class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Tokens
                  </th>
                  <th scope="col" class="px-4 py-3 w-10"></th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                <tr
                  v-for="evt in items"
                  :key="evt.id"
                  @click="openDetail(evt.id)"
                  class="hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer transition-colors"
                >
                  <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-900 dark:text-white">
                    {{ formatDate(evt.consumedAt) }}
                  </td>
                  <td class="px-4 py-3 whitespace-nowrap">
                    <span
                      :class="[
                        'inline-flex px-2 py-1 text-xs font-medium rounded-full',
                        evt.eventType === 'chat'
                          ? 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300'
                          : 'bg-violet-100 text-violet-800 dark:bg-violet-900/30 dark:text-violet-300'
                      ]"
                    >
                      {{ eventTypeLabel(evt.eventType) }}
                    </span>
                  </td>
                  <td class="px-4 py-3 whitespace-nowrap text-sm text-right font-medium text-gray-900 dark:text-white">
                    {{ evt.credits }}
                  </td>
                  <td class="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-600 dark:text-gray-400">
                    {{ tokensSummary(evt) }}
                  </td>
                  <td class="px-4 py-3 whitespace-nowrap text-gray-400">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                    </svg>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          <div
            v-if="totalPages > 1"
            class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex flex-wrap items-center justify-between gap-2"
          >
            <p class="text-sm text-gray-600 dark:text-gray-400">
              Mostrando {{ rangeStart }}–{{ rangeEnd }} de {{ totalCount }} eventos
            </p>
            <div class="flex items-center gap-2">
              <button
                :disabled="page <= 1"
                @click="goToPage(page - 1)"
                class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600"
              >
                Anterior
              </button>
              <span class="text-sm text-gray-600 dark:text-gray-400">
                Página {{ page }} de {{ totalPages }}
              </span>
              <button
                :disabled="page >= totalPages"
                @click="goToPage(page + 1)"
                class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600"
              >
                Siguiente
              </button>
            </div>
          </div>
        </div>
      </template>

      <!-- Slider modal: detail -->
      <Transition name="slide-overlay">
        <div
          v-if="selectedId"
          :class="['fixed inset-0 bg-black/50', embedded ? 'z-[70]' : 'z-50']"
          aria-hidden="true"
        />
      </Transition>
      <Transition name="slide-panel">
        <div
          v-if="selectedId"
          :class="[
            'fixed inset-y-0 right-0 w-full max-w-lg bg-white dark:bg-gray-800 shadow-2xl overflow-y-auto',
            embedded ? 'z-[71]' : 'z-50'
          ]"
        >
          <div class="sticky top-0 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-4 sm:px-6 py-4 flex items-center justify-between z-10">
            <h2 class="text-xl font-bold text-gray-900 dark:text-white">
              Detalle de consumo
            </h2>
            <button
              @click="closeDetail"
              class="p-2 -m-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              aria-label="Cerrar"
            >
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div v-if="loadingDetail" class="p-6 space-y-4 animate-pulse">
            <div class="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4" />
            <div class="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/2" />
            <div class="h-4 bg-gray-200 dark:bg-gray-700 rounded w-5/6" />
          </div>
          <div v-else-if="detail" class="p-6 space-y-6 pb-8">
            <div class="grid grid-cols-1 gap-4">
              <div>
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Fecha y hora</label>
                <p class="text-gray-900 dark:text-white">{{ formatDateFull(detail.consumedAt) }}</p>
              </div>
              <div>
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Tipo de evento</label>
                <p class="text-gray-900 dark:text-white">{{ eventTypeLabel(detail.eventType) }}</p>
              </div>
              <div>
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Créditos</label>
                <p class="text-gray-900 dark:text-white font-medium">{{ detail.credits }}</p>
              </div>
              <div v-if="detail.inputTokenCount != null || detail.outputTokenCount != null || detail.totalTokenCount != null">
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Tokens</label>
                <ul class="text-sm text-gray-900 dark:text-white space-y-1">
                  <li v-if="detail.inputTokenCount != null">Entrada: {{ detail.inputTokenCount }}</li>
                  <li v-if="detail.outputTokenCount != null">Salida: {{ detail.outputTokenCount }}</li>
                  <li v-if="detail.totalTokenCount != null">Total: {{ detail.totalTokenCount }}</li>
                </ul>
              </div>
              <div v-if="detail.correlationId">
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Correlación</label>
                <p class="text-gray-900 dark:text-white font-mono text-xs break-all">{{ detail.correlationId }}</p>
              </div>
              <div v-if="detail.sessionId">
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Session ID</label>
                <p class="text-gray-900 dark:text-white font-mono text-xs break-all">{{ detail.sessionId }}</p>
              </div>
              <div v-if="detail.agentId">
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Agent ID</label>
                <p class="text-gray-900 dark:text-white font-mono text-xs break-all">{{ detail.agentId }}</p>
              </div>
              <div v-if="detail.instanceId">
                <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Instance ID</label>
                <p class="text-gray-900 dark:text-white font-mono text-xs break-all">{{ detail.instanceId }}</p>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import apiService from '../../services/api'
import { useAdminPaths } from '../../composables/useAdminPaths'

const props = defineProps({
  /** Dentro del slide-over de Cuentas programa: sin cabecera de página; detalle usa z-index mayor. */
  embedded: { type: Boolean, default: false },
  /** Cuenta a filtrar (sustituye a <code>route.params.accountId</code> en rutas partner). */
  accountIdProp: { type: String, default: '' }
})

const route = useRoute()
const { toPath } = useAdminPaths()

const partnerViewAccountId = computed(() => {
  const fromProp = String(props.accountIdProp || '').trim()
  if (fromProp) return fromProp
  const p = route.params.accountId
  return p ? String(p) : ''
})

/** Historial filtrado por una cuenta del programa partner (ruta o slide); sin enlace a facturación del titular. */
const isProgramAccountConsumption = computed(() => Boolean(String(partnerViewAccountId.value || '').trim()))

const PAGE_SIZE = 20

const items = ref([])
const totalCount = ref(0)
const page = ref(1)
const pageSize = ref(PAGE_SIZE)
const loading = ref(true)
const selectedId = ref(null)
const detail = ref(null)
const loadingDetail = ref(false)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))
const rangeStart = computed(() => (page.value - 1) * pageSize.value + (items.value.length ? 1 : 0))
const rangeEnd = computed(() => (page.value - 1) * pageSize.value + items.value.length)

const loadPage = async () => {
  loading.value = true
  try {
    const result = await apiService.getConsumptionHistory({
      page: page.value,
      pageSize: pageSize.value,
      accountId: partnerViewAccountId.value || null
    })
    items.value = result?.items ?? []
    totalCount.value = result?.totalCount ?? 0
    pageSize.value = result?.pageSize ?? PAGE_SIZE
  } catch (err) {
    console.error('Error loading consumption history:', err)
    items.value = []
    totalCount.value = 0
  } finally {
    loading.value = false
  }
}

const goToPage = (p) => {
  if (p < 1 || p > totalPages.value) return
  page.value = p
  loadPage()
}

const openDetail = async (id) => {
  selectedId.value = id
  loadingDetail.value = true
  detail.value = null
  try {
    detail.value = await apiService.getConsumptionEventDetail(id, partnerViewAccountId.value || null)
  } catch (err) {
    console.error('Error loading consumption detail:', err)
  } finally {
    loadingDetail.value = false
  }
}

const closeDetail = () => {
  selectedId.value = null
  detail.value = null
}

const formatDate = (dateString) => {
  if (!dateString) return '—'
  return new Date(dateString).toLocaleString('es-ES', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const formatDateFull = (dateString) => {
  if (!dateString) return '—'
  return new Date(dateString).toLocaleString('es-ES', {
    dateStyle: 'full',
    timeStyle: 'medium'
  })
}

const eventTypeLabel = (type) => {
  if (type === 'chat') return 'Chat'
  if (type === 'trigger') return 'Trigger'
  return type || '—'
}

const tokensSummary = (evt) => {
  if (evt.totalTokenCount != null) return evt.totalTokenCount.toLocaleString()
  if (evt.inputTokenCount != null && evt.outputTokenCount != null) {
    return `${(evt.inputTokenCount + evt.outputTokenCount).toLocaleString()} (in/out)`
  }
  if (evt.inputTokenCount != null) return `in: ${evt.inputTokenCount.toLocaleString()}`
  if (evt.outputTokenCount != null) return `out: ${evt.outputTokenCount.toLocaleString()}`
  return '—'
}

onMounted(() => loadPage())

watch(partnerViewAccountId, () => {
  page.value = 1
  loadPage()
})
</script>

<style scoped>
.slide-overlay-enter-active,
.slide-overlay-leave-active {
  transition: opacity 0.3s ease;
}
.slide-overlay-enter-from,
.slide-overlay-leave-to {
  opacity: 0;
}
.slide-panel-enter-active {
  transition: transform 0.3s ease-out;
}
.slide-panel-leave-active {
  transition: transform 0.3s ease-in;
}
.slide-panel-enter-from {
  transform: translateX(100%);
}
.slide-panel-leave-to {
  transform: translateX(100%);
}
</style>

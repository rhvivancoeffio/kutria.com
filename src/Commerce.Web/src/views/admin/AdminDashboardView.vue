<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <div class="mb-8 flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white">
          {{ showPartnerPortal ? 'Inicio — programa partner' : 'Inicio' }}
        </h1>
        <p class="mt-1 text-gray-600 dark:text-gray-400">
          <template v-if="showPartnerPortal">
            Métricas de las cuentas provisionadas; elige una cuenta para ver pipelines y detalle.
          </template>
          <template v-else> Resumen de catálogo y pedidos del workspace. </template>
        </p>
        <p v-if="showPartnerPortal && partnerViewAccountId" class="mt-1 text-xs font-mono text-gray-500 dark:text-gray-400 break-all">
          Cuenta: {{ partnerViewAccountId }}
        </p>
      </div>
      <router-link
        v-if="!showPartnerPortal"
        to="/admin/data/stats"
        class="text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline shrink-0"
      >
        Ver detalle estadísticas →
      </router-link>
    </div>

    <div
      v-if="!showPartnerPortal && !loadingIntegrations && myIntegrations.length === 0"
      class="mb-6 rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-900/20 px-4 py-3"
    >
      <NoIntegrationsMessage class-names="text-sm text-amber-800 dark:text-amber-200 m-0" />
    </div>

    <!-- Pipelines: siempre mismas secciones (métricas vacías si no hay selección / datos). Partner: solo con cuenta seleccionada. -->
    <section v-if="!showPartnerPortal || partnerViewAccountId" aria-labelledby="dash-pipelines-heading">
      <h2 id="dash-pipelines-heading" class="text-lg font-semibold text-gray-900 dark:text-white mb-4">Datos del pipeline</h2>
      <p
        v-if="!pipelinesLoading && !pipelines.length"
        class="mb-4 text-sm text-amber-800 dark:text-amber-200 rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-900/20 px-4 py-3"
      >
        No hay pipelines en este workspace.
        <router-link to="/admin/data-pipeline" class="font-medium text-primary-600 dark:text-primary-400 hover:underline">
          Crear datapipeline
        </router-link>
      </p>
      <div class="space-y-10">
        <!-- Catálogo -->
        <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-900/20 p-4 sm:p-6">
          <div class="flex flex-col sm:flex-row sm:items-end gap-3 sm:justify-between mb-4">
            <div>
              <h3 class="text-base font-semibold text-gray-900 dark:text-white">Catálogo</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Pipeline outbound de productos</p>
            </div>
            <div v-if="!pipelinesLoading && productPipelines.length > 1" class="w-full sm:max-w-xs">
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Pipeline</label>
              <select
                v-model="catalogPipelineId"
                class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm px-3 py-2"
                @change="onCatalogPipelineChange"
              >
                <option value="">— Seleccionar —</option>
                <option v-for="pl in productPipelines" :key="pl.id" :value="String(pl.id)">{{ pl.name }}</option>
              </select>
            </div>
            <div
              v-else-if="!pipelinesLoading && productPipelines.length === 1"
              class="text-xs text-gray-500 dark:text-gray-400 sm:text-right"
            >
              {{ productPipelines[0].name }}
            </div>
          </div>
          <p
            v-if="!pipelinesLoading && !productPipelines.length"
            class="text-sm text-amber-700 dark:text-amber-300 mb-4"
          >
            No hay pipelines outbound de productos.
            <router-link to="/admin/data-pipeline" class="font-medium underline">Configurar en Datapipelines</router-link>
          </p>
          <div v-if="catalogError" class="mb-4 p-3 rounded-lg bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 text-sm">
            {{ catalogError }}
          </div>
          <div v-if="showCatalogMetricsSkeleton" class="grid sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <div v-for="i in 4" :key="i" class="h-24 rounded-lg bg-gray-200 dark:bg-gray-700 animate-pulse" />
          </div>
          <template v-else>
            <div class="grid sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
              <DashboardKpiCard label="Productos" :value="catalogDisplay.totalProducts" />
              <DashboardKpiCard label="Sin stock" :value="catalogDisplay.outOfStockCount" />
              <DashboardKpiCard
                label="Carga OK"
                :value="catalogDisplay.loadOkPct"
                hint="Productos sin error de carga"
              />
              <DashboardKpiCard label="Stock bajo" :value="catalogDisplay.lowStockCount" />
            </div>
            <div class="grid lg:grid-cols-2 gap-4">
              <DashboardChartBar
                title="Top categorías (por productos)"
                :labels="topCategoryLabels"
                :values="topCategoryValues"
                aria-label="Gráfico de barras: productos por categoría"
              />
              <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 text-sm text-gray-600 dark:text-gray-300">
                <h4 class="text-sm font-semibold text-gray-900 dark:text-white mb-2">Cobertura rápida</h4>
                <ul class="space-y-1">
                  <li>Imagen: {{ catalogDisplay.coverageImage }}</li>
                  <li>Título: {{ catalogDisplay.coverageTitle }}</li>
                  <li>SKU: {{ catalogDisplay.coverageSku }}</li>
                </ul>
              </div>
            </div>
          </template>
        </div>

        <!-- Pedidos -->
        <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-900/20 p-4 sm:p-6">
          <div class="flex flex-col sm:flex-row sm:items-end gap-3 sm:justify-between mb-4">
            <div>
              <h3 class="text-base font-semibold text-gray-900 dark:text-white">Pedidos</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Últimos 30 días (UTC) · pipeline inbound</p>
            </div>
            <div v-if="!pipelinesLoading && orderPipelines.length > 1" class="w-full sm:max-w-xs">
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Pipeline</label>
              <select
                v-model="orderPipelineId"
                class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm px-3 py-2"
                @change="onOrderPipelineChange"
              >
                <option value="">— Seleccionar —</option>
                <option v-for="pl in orderPipelines" :key="pl.id" :value="String(pl.id)">{{ pl.name }}</option>
              </select>
            </div>
            <div
              v-else-if="!pipelinesLoading && orderPipelines.length === 1"
              class="text-xs text-gray-500 dark:text-gray-400 sm:text-right"
            >
              {{ orderPipelines[0].name }}
            </div>
          </div>
          <p
            v-if="!pipelinesLoading && !orderPipelines.length"
            class="text-sm text-amber-700 dark:text-amber-300 mb-4"
          >
            No hay pipelines inbound de pedidos.
          </p>
          <div v-if="orderError" class="mb-4 p-3 rounded-lg bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200 text-sm">
            {{ orderError }}
          </div>
          <div v-if="showOrderMetricsSkeleton" class="grid sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-4">
            <div v-for="i in 8" :key="i" class="h-24 rounded-lg bg-gray-200 dark:bg-gray-700 animate-pulse" />
          </div>
          <template v-else>
            <div
              v-if="orderStats?.data && orderStats.missingMarketplaceIntegration"
              class="mb-4 p-3 rounded-lg bg-amber-50 dark:bg-amber-900/20 text-amber-900 dark:text-amber-100 text-sm"
            >
              Este pipeline no tiene integración de canal configurada; los totales pueden quedar vacíos.
            </div>
            <div class="grid sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3 mb-4">
              <DashboardKpiCard
                v-for="card in orderDashboardKpis"
                :key="card.label"
                :label="card.label"
                :value="card.value"
                :hint="card.hint || ''"
              />
            </div>
            <div class="grid lg:grid-cols-2 gap-4">
              <DashboardChartDoughnut
                title="Estado del pedido"
                :labels="orderStatusLabels"
                :values="orderStatusValues"
                aria-label="Gráfico de dona: distribución de estados de pedido"
              />
              <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 overflow-x-auto min-h-[12rem]">
                <h4 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">Top SKU (unidades)</h4>
                <table class="min-w-full text-sm">
                  <thead>
                    <tr class="text-left text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600">
                      <th class="py-2 pr-4">SKU</th>
                      <th class="py-2 pr-4">Unidades</th>
                      <th class="py-2">Ingresos</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-if="!topSkusRows.length">
                      <td colspan="3" class="py-8 text-center text-gray-500 dark:text-gray-400">Sin datos</td>
                    </tr>
                    <tr
                      v-for="(row, idx) in topSkusRows"
                      :key="idx"
                      class="border-b border-gray-100 dark:border-gray-700"
                    >
                      <td class="py-2 pr-4 font-mono text-xs">{{ row.sku }}</td>
                      <td class="py-2 pr-4">{{ row.units }}</td>
                      <td class="py-2">{{ fmtMoney(row.revenue, null) }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </template>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { useAppStore } from '../../stores/appStore'
import { usePartnerProgramView } from '../../composables/usePartnerProgramView'
import NoIntegrationsMessage from '../../components/integrations/NoIntegrationsMessage.vue'
import DashboardKpiCard from '../../components/dashboard/DashboardKpiCard.vue'
import DashboardChartDoughnut from '../../components/dashboard/DashboardChartDoughnut.vue'
import DashboardChartBar from '../../components/dashboard/DashboardChartBar.vue'
import {
  persistAdminCatalogPipelineId,
  readAdminCatalogPipelineIdFromStorage
} from '../../composables/useCatalogPipelineContext'

const toast = useToast()
const route = useRoute()
const router = useRouter()
const appStore = useAppStore()
const { showPartnerPortal, partnerViewAccountId } = usePartnerProgramView()

const ADMIN_ORDERS_PIPELINE_KEY = 'admin_orders_pipeline_id'

const myIntegrations = ref([])
const loadingIntegrations = ref(false)

const pipelines = ref([])
const pipelinesLoading = ref(true)

const catalogPipelineId = ref('')
const catalogStats = ref(null)
const catalogLoading = ref(false)
const catalogError = ref(null)

const orderPipelineId = ref('')
const orderStats = ref(null)
const orderLoading = ref(false)
const orderError = ref(null)

const productPipelines = computed(() =>
  pipelines.value.filter((p) => p.entityType === 'products' && p.pipelineDirection === 'outbound')
)
const orderPipelines = computed(() =>
  pipelines.value.filter((p) => p.entityType === 'orders' && p.pipelineDirection === 'inbound')
)

const ch = computed(() => catalogStats.value?.data?.catalogHealth ?? {})
const st = computed(() => catalogStats.value?.data?.stock ?? {})
const od = computed(() => orderStats.value?.data ?? {})

const showCatalogMetricsSkeleton = computed(
  () => pipelinesLoading.value || (Boolean(String(catalogPipelineId.value || '').trim()) && catalogLoading.value)
)

const showOrderMetricsSkeleton = computed(
  () => pipelinesLoading.value || (Boolean(String(orderPipelineId.value || '').trim()) && orderLoading.value)
)

/** Valores de KPI / cobertura: siempre string; vacíos si no hay datos cargados. */
const catalogDisplay = computed(() => {
  if (!catalogStats.value?.data) {
    return {
      totalProducts: '—',
      outOfStockCount: '—',
      loadOkPct: '—',
      lowStockCount: '—',
      coverageImage: '—',
      coverageTitle: '—',
      coverageSku: '—'
    }
  }
  const h = ch.value
  const s = st.value
  const r = h.failedLoadRatio
  const loadOk =
    r == null || Number.isNaN(r) ? '—' : `${((1 - r) * 100).toFixed(1)}%`
  return {
    totalProducts: h.totalProducts ?? '—',
    outOfStockCount: s.outOfStockCount ?? '—',
    loadOkPct: loadOk,
    lowStockCount: s.lowStockCount ?? '—',
    coverageImage: pct(h.withPrimaryImageRatio),
    coverageTitle: pct(h.withTitleRatio),
    coverageSku: pct(h.withSkuRatio)
  }
})

const topCategoryLabels = computed(() => {
  const rows = ch.value.topCategoriesByProductCount
  if (!Array.isArray(rows)) return []
  return rows.slice(0, 5).map((row) => {
    const name = row.categoryName || row.categoryId || '—'
    return name.length > 36 ? `${name.slice(0, 34)}…` : name
  })
})

const topCategoryValues = computed(() => {
  const rows = ch.value.topCategoriesByProductCount
  if (!Array.isArray(rows)) return []
  return rows.slice(0, 5).map((row) => Number(row.productCount) || 0)
})

const orderStatusLabels = computed(() =>
  (od.value.orderStatusDistribution ?? []).map((row) => String(row.status ?? '—'))
)
const orderStatusValues = computed(() =>
  (od.value.orderStatusDistribution ?? []).map((row) => Number(row.count) || 0)
)

const topSkusRows = computed(() => {
  const rows = od.value.topSkusByUnits
  if (!Array.isArray(rows)) return []
  return rows.slice(0, 5)
})

/** Contadores de pedidos: misma lista siempre visible (— sin datos). */
const orderDashboardKpis = computed(() => {
  const has = !!orderStats.value?.data
  if (!has) {
    return [
      { label: 'Pedidos', value: '—' },
      { label: 'Líneas', value: '—' },
      { label: 'Unidades vendidas', value: '—' },
      { label: 'GMV', value: '—' },
      { label: 'Errores de enriquecimiento', value: '—' },
      { label: 'Líneas sin SKU', value: '—' },
      { label: 'Líneas sin total', value: '—' },
      { label: 'Ticket medio', value: '—' },
      { label: 'Multi-moneda', value: '—' }
    ]
  }

  const o = od.value
  const rows = o.totalsByCurrency
  let gmvVal = '—'
  let gmvHint = ''
  if (Array.isArray(rows) && rows.length) {
    if (rows.length === 1) {
      gmvVal = fmtMoney(rows[0].sumTotal, rows[0].currency)
    } else {
      gmvVal = 'Varias monedas'
      gmvHint = 'Ver desglose en estadísticas detalladas'
    }
  }
  const cur0 = rows?.[0]?.currency
  const ticket =
    !o.multiCurrency && o.overallAverageOrderTotalSingleCurrency != null
      ? fmtMoney(o.overallAverageOrderTotalSingleCurrency, cur0)
      : '—'
  let multiVal = '—'
  let multiHint = ''
  if (o.multiCurrency === true) {
    multiVal = 'Sí'
    multiHint = 'Ver tabla por moneda'
  } else if (o.multiCurrency === false) {
    multiVal = 'No'
  }

  return [
    { label: 'Pedidos', value: o.orderCount ?? '—' },
    { label: 'Líneas', value: o.lineCount ?? '—' },
    { label: 'Unidades vendidas', value: o.unitsSold ?? '—' },
    { label: 'GMV', value: gmvVal, hint: gmvHint },
    { label: 'Errores de enriquecimiento', value: o.ordersWithEnrichmentErrorCount ?? '—' },
    { label: 'Líneas sin SKU', value: o.linesMissingSkuCount ?? '—' },
    { label: 'Líneas sin total', value: o.linesMissingLineTotalCount ?? '—' },
    { label: 'Ticket medio', value: ticket },
    { label: 'Multi-moneda', value: multiVal, hint: multiHint }
  ]
})

function pct(r) {
  if (r == null || Number.isNaN(r)) return '—'
  return `${(r * 100).toFixed(1)}%`
}

function fmtMoney(n, currency) {
  if (n == null || Number.isNaN(Number(n))) return '—'
  const c = currency && String(currency).trim() ? String(currency).trim() : ''
  try {
    if (c) {
      return new Intl.NumberFormat(undefined, {
        style: 'currency',
        currency: c,
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
      }).format(Number(n))
    }
    return new Intl.NumberFormat(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Number(n))
  } catch {
    return String(n)
  }
}

function orderRangeIso() {
  const to = new Date()
  const from = new Date(to.getTime())
  from.setUTCDate(from.getUTCDate() - 30)
  return {
    receivedFromUtc: from.toISOString(),
    receivedToUtc: to.toISOString()
  }
}

async function loadMyIntegrations() {
  if (showPartnerPortal.value) {
    myIntegrations.value = [{ stub: true }]
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

function pipelineViewAccountId() {
  return partnerViewAccountId.value || undefined
}

/** Si el partner tiene una sola cuenta provisionada, fija accountId en la URL (sin listado en UI). */
async function loadPartnerProgramOverview() {
  if (!showPartnerPortal.value) return
  try {
    const data = await apiService.getPartnerProgramAccounts({ page: 1, pageSize: 20 })
    const totalCount = Number(data?.totalCount ?? data?.TotalCount ?? 0)
    const items = data?.items ?? data?.Items ?? []
    const raw = Array.isArray(items) ? items : []
    const accounts = raw.map((row) => String(row.id ?? row.Id ?? '')).filter(Boolean)
    if (totalCount === 1 && accounts.length === 1 && !String(route.query.accountId || '').trim()) {
      await router.replace({ path: route.path, query: { ...route.query, accountId: accounts[0] } })
    }
  } catch (e) {
    toast.error(e?.response?.data?.error || e?.message || 'No se pudieron cargar las cuentas del programa')
  }
}

async function loadCatalogStats() {
  catalogError.value = null
  const id = String(catalogPipelineId.value || '').trim()
  if (!id) {
    catalogStats.value = null
    catalogLoading.value = false
    return
  }
  persistAdminCatalogPipelineId(id)
  catalogLoading.value = true
  try {
    const res = await apiService.getPipelineCatalogStats(id, {
      topCategoryCount: 5,
      accountId: pipelineViewAccountId()
    })
    if (res?.notFound) {
      catalogError.value = 'Pipeline no encontrado.'
      catalogStats.value = null
    } else if (res?.wrongPipelineKind) {
      catalogError.value = 'Este pipeline no es de catálogo (outbound productos).'
      catalogStats.value = null
    } else {
      catalogStats.value = res
    }
  } catch (e) {
    catalogError.value = e?.response?.data?.error || e?.message || 'Error al cargar estadísticas de catálogo'
    catalogStats.value = null
    toast.error(catalogError.value)
  } finally {
    catalogLoading.value = false
  }
}

async function loadOrderStats() {
  orderError.value = null
  const id = String(orderPipelineId.value || '').trim()
  if (!id) {
    orderStats.value = null
    orderLoading.value = false
    return
  }
  try {
    sessionStorage.setItem(ADMIN_ORDERS_PIPELINE_KEY, id)
  } catch {
    /* ignore */
  }
  orderLoading.value = true
  const range = orderRangeIso()
  try {
    const res = await apiService.getPipelineOrderStats(id, {
      receivedFromUtc: range.receivedFromUtc,
      receivedToUtc: range.receivedToUtc,
      topSkuCount: 5,
      accountId: pipelineViewAccountId()
    })
    if (res?.notFound) {
      orderError.value = 'Pipeline no encontrado.'
      orderStats.value = null
    } else if (res?.wrongPipelineKind) {
      orderError.value = 'Este pipeline no es de pedidos (inbound).'
      orderStats.value = null
    } else {
      orderStats.value = res
    }
  } catch (e) {
    orderError.value = e?.response?.data?.error || e?.message || 'Error al cargar estadísticas de pedidos'
    orderStats.value = null
    toast.error(orderError.value)
  } finally {
    orderLoading.value = false
  }
}

function onCatalogPipelineChange() {
  const id = String(catalogPipelineId.value || '').trim()
  if (!id) {
    catalogStats.value = null
    catalogError.value = null
    persistAdminCatalogPipelineId('')
    return
  }
  loadCatalogStats()
}

function onOrderPipelineChange() {
  const id = String(orderPipelineId.value || '').trim()
  if (!id) {
    orderStats.value = null
    orderError.value = null
    try {
      sessionStorage.removeItem(ADMIN_ORDERS_PIPELINE_KEY)
    } catch {
      /* ignore */
    }
    return
  }
  loadOrderStats()
}

async function loadPipelines() {
  if (showPartnerPortal.value && !partnerViewAccountId.value) {
    pipelines.value = []
    pipelinesLoading.value = false
    return
  }
  pipelinesLoading.value = true
  try {
    const viewAid = partnerViewAccountId.value || null
    const [productPl, orderPl] = await Promise.all([
      apiService.getDataPipelineLookups(viewAid, { entityType: 'products', direction: 'outbound' }),
      apiService.getDataPipelineLookups(viewAid, { entityType: 'orders', direction: 'inbound' })
    ])
    pipelines.value = [
      ...(productPl || []).map((p) => ({
        ...p,
        entityType: 'products',
        pipelineDirection: 'outbound'
      })),
      ...(orderPl || []).map((p) => ({
        ...p,
        entityType: 'orders',
        pipelineDirection: 'inbound'
      }))
    ]

    const catCur = String(catalogPipelineId.value || '').trim().toLowerCase()
    if (catCur && !productPipelines.value.some((p) => String(p.id).toLowerCase() === catCur)) {
      catalogPipelineId.value = ''
      persistAdminCatalogPipelineId('')
    }
    const ordCur = String(orderPipelineId.value || '').trim().toLowerCase()
    if (ordCur && !orderPipelines.value.some((p) => String(p.id).toLowerCase() === ordCur)) {
      orderPipelineId.value = ''
      try {
        sessionStorage.removeItem(ADMIN_ORDERS_PIPELINE_KEY)
      } catch {
        /* ignore */
      }
    }

    const stored = readAdminCatalogPipelineIdFromStorage().trim()
    if (stored && productPipelines.value.some((p) => String(p.id).toLowerCase() === stored.toLowerCase())) {
      catalogPipelineId.value = stored
    } else if (productPipelines.value.length === 1) {
      catalogPipelineId.value = String(productPipelines.value[0].id)
      persistAdminCatalogPipelineId(catalogPipelineId.value)
    }

    try {
      const o = sessionStorage.getItem(ADMIN_ORDERS_PIPELINE_KEY)?.trim()
      if (o && orderPipelines.value.some((p) => String(p.id).toLowerCase() === o.toLowerCase())) {
        orderPipelineId.value = o
      } else if (orderPipelines.value.length === 1) {
        orderPipelineId.value = String(orderPipelines.value[0].id)
        sessionStorage.setItem(ADMIN_ORDERS_PIPELINE_KEY, orderPipelineId.value)
      }
    } catch {
      /* ignore */
    }

    if (catalogPipelineId.value) await loadCatalogStats()
    if (orderPipelineId.value) await loadOrderStats()
  } catch (e) {
    toast.error(e?.response?.data?.error || e?.message || 'No se pudieron cargar los pipelines')
    pipelines.value = []
  } finally {
    pipelinesLoading.value = false
  }
}

watch(
  [showPartnerPortal, partnerViewAccountId],
  async ([partner, accountId]) => {
    if (partner) {
      await appStore.fetchUsage(true, accountId || null)
      if (accountId) await loadPipelines()
      else {
        pipelines.value = []
        pipelinesLoading.value = false
      }
      return
    }
    await loadPipelines()
  },
  { immediate: true }
)

onMounted(async () => {
  await loadMyIntegrations()
  if (showPartnerPortal.value) {
    await loadPartnerProgramOverview()
  }
})
</script>

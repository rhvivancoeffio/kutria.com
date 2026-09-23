<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between mb-6">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Ventas</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Consulta pedidos en vivo desde la integración seleccionada.
          </p>
        </div>
        <div v-if="integrations.length" class="flex flex-wrap gap-2 shrink-0">
          <button
            type="button"
            class="inline-flex items-center justify-center gap-2 min-h-[44px] px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
            :disabled="!selectedIntegrationId || listLoading"
            @click="refreshList"
          >
            <svg class="w-4 h-4" :class="listLoading ? 'animate-spin' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            Actualizar
          </button>
          <button
            type="button"
            class="inline-flex items-center justify-center min-h-[44px] px-4 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium disabled:opacity-50"
            :disabled="!selectedIntegrationId || syncing"
            @click="openSyncConfirm"
          >
            {{ syncing ? 'Encolando…' : 'Sincronizar' }}
          </button>
        </div>
      </div>

      <div v-if="bootLoading" class="space-y-4">
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          <div v-for="i in 3" :key="i" class="h-24 rounded-xl bg-gray-200 dark:bg-gray-700 animate-pulse" />
        </div>
        <StoreOrdersListSkeleton />
      </div>

      <StoreConnectionRequired
        v-else-if="!integrations.length"
        title="Sin conexión de integración"
        message="Para ver ventas debes conectar una integración (tienda o Gravity API) en Integraciones."
      />

      <template v-else>
        <StoreIntegrationCards
          v-model="selectedIntegrationId"
          :integrations="integrations"
          class="mb-6"
          @update:model-value="onIntegrationSelected"
        />

        <div
          v-if="error"
          class="mb-4 p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800"
        >
          <p class="text-red-800 dark:text-red-300 text-sm">{{ error }}</p>
        </div>

        <StoreOrdersListSkeleton v-if="listLoading" />

        <template v-else-if="selectedIntegrationId">
          <div
            v-if="!items.length"
            class="text-center py-12 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
          >
            <p class="text-gray-500 dark:text-gray-400">No hay pedidos en esta conexión.</p>
          </div>

          <template v-else>
            <!-- Mobile -->
            <div class="md:hidden space-y-3">
              <button
                v-for="row in items"
                :key="row.id"
                type="button"
                class="w-full text-left bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3"
                @click="openDetail(row)"
              >
                <div>
                  <p class="text-sm font-semibold text-primary-600 dark:text-primary-400">
                    {{ row.orderNumber || row.name || row.id }}
                  </p>
                  <div v-if="row.providerNames?.length" class="mt-1.5 flex flex-wrap gap-1.5">
                    <img
                      v-for="provider in row.providerNames"
                      :key="provider"
                      :src="providerLogo(provider)"
                      :alt="provider"
                      class="h-5 w-auto max-w-[72px] object-contain"
                      @error="onLogoError"
                    />
                  </div>
                </div>
                <div class="grid grid-cols-2 gap-3 text-xs text-gray-500 dark:text-gray-400">
                  <div>
                    <p class="uppercase tracking-wide text-[10px] text-gray-400">Compra</p>
                    <p class="mt-0.5 text-gray-700 dark:text-gray-300 whitespace-pre-line">{{ formatDateParts(row.orderDate) }}</p>
                  </div>
                  <div>
                    <p class="uppercase tracking-wide text-[10px] text-gray-400">Entrega</p>
                    <p class="mt-0.5 text-gray-700 dark:text-gray-300 whitespace-pre-line">{{ formatDateParts(row.deliveryDate) }}</p>
                  </div>
                </div>
                <div class="text-sm">
                  <p class="font-medium text-gray-900 dark:text-white">{{ row.clientName || '—' }}</p>
                  <p v-if="row.clientSecondary" class="text-xs text-gray-500 dark:text-gray-400">{{ row.clientSecondary }}</p>
                </div>
                <div class="flex items-center justify-between gap-3 text-sm">
                  <span class="text-primary-600 dark:text-primary-400">{{ row.sellerName || '—' }}</span>
                  <span class="tabular-nums text-gray-700 dark:text-gray-300">{{ formatMoney(row.total, row.currencySymbol) }}</span>
                </div>
                <div class="flex items-center justify-between gap-3 text-sm">
                  <span class="text-gray-500 dark:text-gray-400">Artículos: {{ formatCount(row.itemCount) }}</span>
                  <span :class="statusClass(row.status)">{{ row.status || '—' }}</span>
                </div>
              </button>
            </div>

            <!-- Desktop -->
            <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-900/50">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400"># de orden</th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Fecha de compra</th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Fecha entrega</th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Cliente</th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Vendedor</th>
                    <th class="px-4 py-3 text-center text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Artículos</th>
                    <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Total</th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Estado</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                  <tr
                    v-for="row in items"
                    :key="row.id"
                    class="hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer align-top"
                    @click="openDetail(row)"
                  >
                    <td class="px-4 py-4">
                      <button
                        type="button"
                        class="text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline text-left"
                        @click.stop="openDetail(row)"
                      >
                        {{ row.orderNumber || row.name || row.id }}
                      </button>
                      <div v-if="row.providerNames?.length" class="mt-2 flex flex-wrap items-center gap-1.5">
                        <img
                          v-for="provider in row.providerNames"
                          :key="`${row.id}-${provider}`"
                          :src="providerLogo(provider)"
                          :alt="provider"
                          :title="provider"
                          class="h-5 w-auto max-w-[72px] object-contain"
                          @error="onLogoError"
                        />
                      </div>
                    </td>
                    <td class="px-4 py-4 text-sm text-gray-700 dark:text-gray-300 whitespace-pre-line leading-5">
                      {{ formatDateParts(row.orderDate) }}
                    </td>
                    <td class="px-4 py-4 text-sm text-gray-700 dark:text-gray-300 whitespace-pre-line leading-5">
                      {{ formatDateParts(row.deliveryDate) }}
                    </td>
                    <td class="px-4 py-4">
                      <p class="text-sm text-gray-900 dark:text-white">{{ row.clientName || '—' }}</p>
                      <p v-if="row.clientSecondary" class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">
                        {{ row.clientSecondary }}
                      </p>
                    </td>
                    <td class="px-4 py-4">
                      <span class="text-sm text-primary-600 dark:text-primary-400">
                        {{ row.sellerName || '—' }}
                      </span>
                    </td>
                    <td class="px-4 py-4 text-center text-sm text-gray-800 dark:text-gray-200 tabular-nums">
                      {{ formatCount(row.itemCount) }}
                    </td>
                    <td class="px-4 py-4 text-right text-sm font-medium text-gray-900 dark:text-white tabular-nums">
                      {{ formatMoney(row.total, row.currencySymbol) }}
                    </td>
                    <td class="px-4 py-4">
                      <span class="text-sm font-medium" :class="statusClass(row.status)">
                        {{ row.status || '—' }}
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>

          <div v-if="hasMore || page > 1" class="mt-4 flex justify-end gap-2">
            <button
              type="button"
              class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
              :disabled="page <= 1 || listLoading"
              @click="prevPage"
            >
              Anterior
            </button>
            <button
              type="button"
              class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
              :disabled="!hasMore || listLoading"
              @click="nextPage"
            >
              Siguiente
            </button>
          </div>
        </template>
      </template>
    </main>

    <StoreDetailSlider
      :open="detailOpen"
      :loading="detailLoading"
      :error="detailError"
      :title="detail?.name || detailPreviewName"
      :subtitle="detail?.id || detailPreviewId"
      :payload-json="detail?.payloadJson || ''"
      @close="closeDetail"
    />

    <ConfirmModal
      :open="syncConfirmOpen"
      title="Sincronizar ventas"
      :message="syncConfirmMessage"
      confirm-label="Sincronizar"
      busy-label="Encolando…"
      :busy="syncing"
      @confirm="confirmSync"
      @cancel="syncConfirmOpen = false"
    />
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import ConfirmModal from '../../components/ConfirmModal.vue'
import StoreConnectionRequired from '../../components/store/StoreConnectionRequired.vue'
import StoreIntegrationCards from '../../components/store/StoreIntegrationCards.vue'
import StoreOrdersListSkeleton from '../../components/store/StoreOrdersListSkeleton.vue'
import StoreDetailSlider from '../../components/store/StoreDetailSlider.vue'

const PROVIDER_LOGOS = {
  vtex: '/logos/vtex.png',
  woocommerce: '/logos/woocommerce.png',
  shopify: '/logos/shopify.png',
  gravity: '/logos/gravity.jpeg',
  'gravity api': '/logos/gravity.jpeg'
}

const toast = useToast()
const bootLoading = ref(true)
const listLoading = ref(false)
const syncing = ref(false)
const syncConfirmOpen = ref(false)
const error = ref(null)
const integrations = ref([])
const selectedIntegrationId = ref('')
const items = ref([])
const page = ref(1)
const pageSize = 25
const hasMore = ref(false)
const detailOpen = ref(false)
const detailLoading = ref(false)
const detailError = ref('')
const detail = ref(null)
const detailPreviewName = ref('')
const detailPreviewId = ref('')

const selectedIntegration = computed(() =>
  integrations.value.find((x) => x.id === selectedIntegrationId.value)
)

const syncConfirmMessage = computed(() => {
  const name = selectedIntegration.value?.name || selectedIntegration.value?.provider || 'esta integración'
  return `¿Encolar la sincronización de ventas para ${name}?`
})

function providerLogo(provider) {
  const key = String(provider || '').trim().toLowerCase()
  return PROVIDER_LOGOS[key] || '/logos/gravity.jpeg'
}

function onLogoError(event) {
  event.target.style.display = 'none'
}

function formatDateParts(value) {
  if (!value) return '—'
  const date = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(date.getTime())) return '—'
  const day = date.toLocaleDateString('es-CO', { day: '2-digit', month: '2-digit', year: 'numeric' })
  const time = date.toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
  return `${day}\n${time}`
}

function formatMoney(value, symbol = 'Col$') {
  if (value == null || value === '') return '—'
  const num = Number(value)
  if (Number.isNaN(num)) return '—'
  const formatted = num.toLocaleString('es-CO', {
    minimumFractionDigits: Number.isInteger(num) ? 0 : 2,
    maximumFractionDigits: 2
  })
  return `${symbol || 'Col$'} ${formatted}`
}

function formatCount(value) {
  if (value == null || value === '') return '—'
  const num = Number(value)
  if (Number.isNaN(num)) return '—'
  return String(num)
}

function statusClass(status) {
  const value = String(status || '').toLowerCase()
  if (value.includes('cancel')) return 'text-red-600 dark:text-red-400'
  if (value.includes('complet') || value.includes('entreg') || value.includes('closed')) {
    return 'text-emerald-600 dark:text-emerald-400'
  }
  return 'text-gray-600 dark:text-gray-300'
}

function normalizeList(data) {
  const list = data?.items ?? data?.Items ?? []
  return {
    items: list.map((x) => ({
      id: x.id ?? x.Id,
      name: x.name ?? x.Name,
      status: x.status ?? x.Status,
      syncedAt: x.syncedAt ?? x.SyncedAt,
      orderNumber: x.orderNumber ?? x.OrderNumber ?? x.name ?? x.Name ?? null,
      orderDate: x.orderDate ?? x.OrderDate ?? null,
      deliveryDate: x.deliveryDate ?? x.DeliveryDate ?? null,
      clientName: x.clientName ?? x.ClientName ?? null,
      clientSecondary: x.clientSecondary ?? x.ClientSecondary ?? null,
      sellerName: x.sellerName ?? x.SellerName ?? null,
      itemCount: x.itemCount ?? x.ItemCount ?? null,
      total: x.total ?? x.Total ?? null,
      currencySymbol: x.currencySymbol ?? x.CurrencySymbol ?? null,
      providerNames: x.providerNames ?? x.ProviderNames ?? []
    })),
    hasMore: Boolean(data?.hasMore ?? data?.HasMore)
  }
}

async function loadIntegrations() {
  const list = await apiService.listIntegrations()
  integrations.value = (list || []).filter((x) => x.isActive !== false)
  if (!selectedIntegrationId.value && integrations.value.length) {
    selectedIntegrationId.value = integrations.value[0].id
  }
}

async function reload() {
  if (!selectedIntegrationId.value) {
    items.value = []
    return
  }
  listLoading.value = true
  error.value = null
  try {
    const data = await apiService.listIngestedOrders(selectedIntegrationId.value, {
      page: page.value,
      pageSize
    })
    const normalized = normalizeList(data)
    items.value = normalized.items
    hasMore.value = normalized.hasMore
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al cargar ventas'
    items.value = []
  } finally {
    listLoading.value = false
  }
}

function onIntegrationSelected() {
  page.value = 1
  reload()
}

function refreshList() {
  reload()
}

function openSyncConfirm() {
  if (!selectedIntegrationId.value || syncing.value) return
  syncConfirmOpen.value = true
}

async function confirmSync() {
  if (!selectedIntegrationId.value) return
  syncing.value = true
  try {
    await apiService.enqueueDataIngest(selectedIntegrationId.value, 'orders')
    toast.success('Sincronización de ventas encolada')
    syncConfirmOpen.value = false
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'No se pudo encolar')
  } finally {
    syncing.value = false
  }
}

function normalizeDetail(data) {
  if (!data) return null
  return {
    id: data.id ?? data.Id ?? '',
    name: data.name ?? data.Name ?? '',
    status: data.status ?? data.Status ?? '',
    syncedAt: data.syncedAt ?? data.SyncedAt,
    payloadJson: data.payloadJson ?? data.PayloadJson ?? ''
  }
}

async function openDetail(row) {
  detailPreviewName.value = row?.orderNumber || row?.name || ''
  detailPreviewId.value = row?.id || ''
  detail.value = null
  detailError.value = ''
  detailOpen.value = true
  detailLoading.value = true
  try {
    const data = await apiService.getIngestedOrder(selectedIntegrationId.value, row.id)
    detail.value = normalizeDetail(data)
  } catch (err) {
    detailError.value = err.response?.data?.error || err.message || 'No se pudo cargar el detalle'
  } finally {
    detailLoading.value = false
  }
}

function closeDetail() {
  detailOpen.value = false
  detail.value = null
  detailError.value = ''
  detailLoading.value = false
  detailPreviewName.value = ''
  detailPreviewId.value = ''
}

function prevPage() {
  if (page.value <= 1) return
  page.value -= 1
  reload()
}

function nextPage() {
  if (!hasMore.value) return
  page.value += 1
  reload()
}

watch(selectedIntegrationId, (id, prev) => {
  if (id && id !== prev && !bootLoading.value) {
    page.value = 1
  }
})

onMounted(async () => {
  bootLoading.value = true
  try {
    await loadIntegrations()
    if (selectedIntegrationId.value) {
      await reload()
    }
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al cargar'
  } finally {
    bootLoading.value = false
  }
})
</script>

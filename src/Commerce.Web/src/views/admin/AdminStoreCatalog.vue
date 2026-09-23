<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between mb-6">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Catalog</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Consulta productos en vivo desde la integración seleccionada.
          </p>
        </div>
        <div v-if="integrations.length" class="flex flex-wrap gap-2 shrink-0">
          <button
            type="button"
            class="inline-flex items-center justify-center min-h-[44px] px-4 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium disabled:opacity-50"
            :disabled="!selectedIntegrationId"
            @click="onboardingOpen = true"
          >
            Nuevo producto
          </button>
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
        <StoreCatalogListSkeleton />
      </div>

      <StoreConnectionRequired
        v-else-if="!integrations.length"
        title="Sin conexión de integración"
        message="Para ver el catálogo debes conectar una integración (tienda o Gravity API) en Integraciones."
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

        <StoreCatalogListSkeleton v-if="listLoading" />

        <template v-else-if="selectedIntegrationId">
          <div
            v-if="!items.length"
            class="text-center py-12 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
          >
            <p class="text-gray-500 dark:text-gray-400">No hay productos en esta conexión.</p>
          </div>

          <template v-else>
            <!-- Mobile -->
            <div class="md:hidden space-y-3">
              <div
                v-for="row in items"
                :key="row.id"
                class="w-full bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4"
              >
                <button
                  type="button"
                  class="w-full text-left flex gap-3"
                  @click="openDetail(row)"
                >
                  <div
                    class="h-16 w-16 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 shrink-0 overflow-hidden flex items-center justify-center"
                  >
                    <img
                      v-if="row.imageUrl"
                      :src="row.imageUrl"
                      :alt="row.name || row.id"
                      class="h-full w-full object-contain"
                    />
                    <svg
                      v-else
                      class="h-8 w-8 text-gray-400"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="1.5"
                        d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                      />
                    </svg>
                  </div>
                  <div class="min-w-0 flex-1">
                    <p
                      v-if="row.name"
                      class="text-sm font-semibold text-gray-900 dark:text-white truncate"
                    >
                      {{ row.name }}
                    </p>
                    <div class="mt-1 space-y-0.5 text-xs text-gray-500 dark:text-gray-400">
                      <p v-if="row.id">ID: {{ row.id }}</p>
                      <p v-if="row.sellerName">Vendedor: {{ row.sellerName }}</p>
                      <p v-if="row.brandName">Marca: {{ row.brandName }}</p>
                      <p v-if="row.categoryPath">Categoría: {{ row.categoryPath }}</p>
                      <p v-if="row.marketplaceId" class="font-semibold text-gray-800 dark:text-gray-200">
                        Marketplace ID: {{ row.marketplaceId }}
                      </p>
                    </div>
                    <div class="mt-2 flex items-center justify-between gap-3">
                      <span class="text-sm text-gray-700 dark:text-gray-300">Stock: {{ formatStock(row.stock) }}</span>
                      <div v-if="hasSpecialPrice(row)" class="text-right space-y-0.5">
                        <p class="text-xs text-gray-400 line-through tabular-nums">
                          {{ formatMoney(row.basePrice, row.currencySymbol) }}
                        </p>
                        <p class="text-sm font-medium text-gray-900 dark:text-white tabular-nums">
                          {{ formatMoney(row.specialPrice, row.currencySymbol) }}
                        </p>
                      </div>
                      <p
                        v-else
                        class="text-sm font-medium text-gray-900 dark:text-white tabular-nums"
                      >
                        {{ formatMoney(row.specialPrice ?? row.basePrice, row.currencySymbol) }}
                      </p>
                    </div>
                  </div>
                </button>
                <div class="mt-3 flex justify-end">
                  <button
                    type="button"
                    class="min-h-[40px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="forceSyncingId === row.id || listLoading"
                    @click="forceSyncItem(row)"
                  >
                    {{ forceSyncingId === row.id ? 'Encolando…' : 'Force sync' }}
                  </button>
                </div>
              </div>
            </div>

            <!-- Desktop -->
            <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-900/50">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400 w-[88px]">
                      Imagen
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                      Nombre
                    </th>
                    <th class="px-4 py-3 text-center text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400 w-[96px]">
                      Stock
                    </th>
                    <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400 w-[140px]">
                      Precio
                    </th>
                    <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400 w-[140px]">
                      Acciones
                    </th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                  <tr
                    v-for="row in items"
                    :key="row.id"
                    class="hover:bg-gray-50 dark:hover:bg-gray-700/50 align-top"
                  >
                    <td class="px-4 py-4 cursor-pointer" @click="openDetail(row)">
                      <div
                        class="h-14 w-14 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700 overflow-hidden flex items-center justify-center"
                      >
                        <img
                          v-if="row.imageUrl"
                          :src="row.imageUrl"
                          :alt="row.name || row.id"
                          class="h-full w-full object-contain"
                        />
                        <svg
                          v-else
                          class="h-8 w-8 text-gray-400"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                          aria-hidden="true"
                        >
                          <path
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="1.5"
                            d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                          />
                        </svg>
                      </div>
                    </td>
                    <td class="px-4 py-4 cursor-pointer" @click="openDetail(row)">
                      <p
                        v-if="row.name"
                        class="text-sm font-semibold text-gray-900 dark:text-white"
                      >
                        {{ row.name }}
                      </p>
                      <div class="mt-1 space-y-0.5 text-xs leading-relaxed text-gray-500 dark:text-gray-400">
                        <p v-if="row.id">ID: {{ row.id }}</p>
                        <p v-if="row.sellerName">Vendedor: {{ row.sellerName }}</p>
                        <p v-if="row.brandName">Marca: {{ row.brandName }}</p>
                        <p v-if="row.categoryPath">Categoría: {{ row.categoryPath }}</p>
                        <p
                          v-if="row.marketplaceId"
                          class="font-semibold text-gray-800 dark:text-gray-200"
                        >
                          Marketplace ID: {{ row.marketplaceId }}
                        </p>
                      </div>
                    </td>
                    <td
                      class="px-4 py-4 text-center text-sm text-gray-800 dark:text-gray-200 tabular-nums cursor-pointer"
                      @click="openDetail(row)"
                    >
                      {{ formatStock(row.stock) }}
                    </td>
                    <td class="px-4 py-4 text-right cursor-pointer" @click="openDetail(row)">
                      <div v-if="hasSpecialPrice(row)" class="space-y-0.5 text-right">
                        <p class="text-xs text-gray-400 line-through tabular-nums">
                          {{ formatMoney(row.basePrice, row.currencySymbol) }}
                        </p>
                        <p class="text-sm font-medium text-gray-900 dark:text-white tabular-nums">
                          {{ formatMoney(row.specialPrice, row.currencySymbol) }}
                        </p>
                      </div>
                      <p
                        v-else
                        class="text-sm font-medium text-gray-900 dark:text-white tabular-nums text-right"
                      >
                        {{ formatMoney(row.specialPrice ?? row.basePrice, row.currencySymbol) }}
                      </p>
                    </td>
                    <td class="px-4 py-4 text-right">
                      <button
                        type="button"
                        class="inline-flex items-center min-h-[40px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                        :disabled="forceSyncingId === row.id || listLoading"
                        @click.stop="forceSyncItem(row)"
                      >
                        {{ forceSyncingId === row.id ? 'Encolando…' : 'Force sync' }}
                      </button>
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
      title="Sincronizar catálogo"
      :message="syncConfirmMessage"
      confirm-label="Sincronizar"
      busy-label="Encolando…"
      :busy="syncing"
      @confirm="confirmSync"
      @cancel="syncConfirmOpen = false"
    />

    <ProductOnboardingSlider
      v-model="onboardingOpen"
      :integration-id="selectedIntegrationId"
      @created="onProductCreated"
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
import StoreCatalogListSkeleton from '../../components/store/StoreCatalogListSkeleton.vue'
import StoreDetailSlider from '../../components/store/StoreDetailSlider.vue'
import ProductOnboardingSlider from '../../components/catalog/ProductOnboardingSlider.vue'

const toast = useToast()
const bootLoading = ref(true)
const listLoading = ref(false)
const syncing = ref(false)
const syncConfirmOpen = ref(false)
const forceSyncingId = ref(null)
const onboardingOpen = ref(false)
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
  return `¿Encolar la sincronización del catálogo para ${name}?`
})

function hasSpecialPrice(row) {
  const base = row?.basePrice
  const special = row?.specialPrice
  return special != null && Number(special) > 0 && (base == null || Number(special) < Number(base))
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

function formatStock(value) {
  if (value == null || value === '') return '—'
  const num = Number(value)
  if (Number.isNaN(num)) return '—'
  return Number.isInteger(num) ? String(num) : num.toLocaleString('es-CO')
}

function normalizeList(data) {
  const list = data?.items ?? data?.Items ?? []
  return {
    items: list.map((x) => ({
      id: x.id ?? x.Id,
      name: x.name ?? x.Name,
      status: x.status ?? x.Status,
      syncedAt: x.syncedAt ?? x.SyncedAt,
      imageUrl: x.imageUrl ?? x.ImageUrl ?? null,
      sellerName: x.sellerName ?? x.SellerName ?? null,
      brandName: x.brandName ?? x.BrandName ?? null,
      categoryPath: x.categoryPath ?? x.CategoryPath ?? null,
      marketplaceId: x.marketplaceId ?? x.MarketplaceId ?? null,
      stock: x.stock ?? x.Stock ?? null,
      basePrice: x.basePrice ?? x.BasePrice ?? null,
      specialPrice: x.specialPrice ?? x.SpecialPrice ?? null,
      currencySymbol: x.currencySymbol ?? x.CurrencySymbol ?? null
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
    const data = await apiService.listIngestedCatalog(selectedIntegrationId.value, {
      page: page.value,
      pageSize
    })
    const normalized = normalizeList(data)
    items.value = normalized.items
    hasMore.value = normalized.hasMore
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al cargar catálogo'
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
    await apiService.enqueueDataIngest(selectedIntegrationId.value, 'catalog')
    toast.success('Sincronización de catálogo encolada')
    syncConfirmOpen.value = false
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'No se pudo encolar')
  } finally {
    syncing.value = false
  }
}

async function forceSyncItem(row) {
  if (!selectedIntegrationId.value || !row?.id || forceSyncingId.value) return
  forceSyncingId.value = row.id
  try {
    await apiService.forceSyncIngestionCatalogItem(selectedIntegrationId.value, row.id)
    toast.success(`Force sync encolado: ${row.name || row.id}`)
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'No se pudo encolar force sync')
  } finally {
    forceSyncingId.value = null
  }
}

function onProductCreated() {
  onboardingOpen.value = false
  refreshList()
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
  detailPreviewName.value = row?.name || ''
  detailPreviewId.value = row?.id || ''
  detail.value = null
  detailError.value = ''
  detailOpen.value = true
  detailLoading.value = true
  try {
    const data = await apiService.getIngestedCatalogItem(selectedIntegrationId.value, row.id)
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

<template>
  <div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
    <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between mb-6 sm:mb-8">
      <div class="min-w-0">
        <router-link
          :to="paths.toPath('integraciones')"
          class="text-sm text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400"
        >
          ← Integraciones
        </router-link>
        <h1 class="mt-2 text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Publicaciones</h1>
        <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          Productos del catálogo del data pipeline y su estado por canal de venta del workspace (id externo cuando ya está
          publicado).
        </p>
      </div>
    </div>

    <div class="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-end flex-1 min-w-0 w-full">
        <div class="min-w-0 w-full sm:max-w-md">
          <label for="pipeline-select" class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
            Data pipeline (catálogo)
          </label>
          <select
            id="pipeline-select"
            v-model="selectedPipelineId"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm px-3 py-2.5"
          >
            <option value="">Seleccionar…</option>
            <option v-for="p in catalogPipelines" :key="p.id" :value="p.id">
              {{ pipelineLabel(p) }}
            </option>
          </select>
        </div>
        <div v-if="selectedPipelineId" class="min-w-0 w-full sm:max-w-md">
          <label
            for="channel-slice-select"
            class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1"
            title="Usa los filtros de catálogo guardados en el canal (misma lógica que la vista de publicación del destino)."
          >
            Canal (opcional)
          </label>
          <select
            id="channel-slice-select"
            v-model="selectedChannelDestinationId"
            :disabled="sliceFilterChannelOptions.length === 0"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm px-3 py-2.5 disabled:opacity-60"
          >
            <option value="">Todos los canales</option>
            <option v-for="c in sliceFilterChannelOptions" :key="c.channelDestinationId" :value="String(c.channelDestinationId)">
              {{ c.name }}
            </option>
          </select>
        </div>
      </div>
      <div
        v-if="selectedPipelineId"
        class="flex flex-wrap items-center justify-end gap-3 shrink-0 w-full sm:w-auto"
      >
        <span class="text-sm text-gray-600 dark:text-gray-400">{{ totalCount }} producto(s)</span>
        <button
          type="button"
          :disabled="csvExporting || matrixLoading"
          class="inline-flex items-center justify-center gap-2 px-3 py-2 rounded-lg text-sm font-medium border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed"
          @click="downloadMatrixCsv"
        >
          <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
            />
          </svg>
          {{ csvExporting ? 'Generando CSV…' : 'Descargar CSV' }}
        </button>
      </div>
    </div>

    <div v-if="pipelinesLoading" class="space-y-3">
      <div v-for="i in 4" :key="i" class="h-12 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
    </div>

    <div v-else-if="pipelinesError" class="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
      <p class="text-sm text-red-800 dark:text-red-200">{{ pipelinesError }}</p>
    </div>

    <template v-else-if="selectedPipelineId">
      <div v-if="matrixLoading" class="space-y-3">
        <div v-for="i in 6" :key="i" class="h-10 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
      </div>
      <div v-else-if="matrixError" class="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
        <p class="text-sm text-red-800 dark:text-red-200">{{ matrixError }}</p>
      </div>
      <div v-else class="space-y-4">
        <div
          v-if="tableTemplateProject.length === 0"
          class="rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-950/30 px-4 py-3 text-sm text-amber-900 dark:text-amber-100"
        >
          <p class="m-0">
            No hay canales de venta en este workspace. Para ver columnas aquí,
            <router-link
              :to="paths.toPath('channels/destinations/new')"
              class="font-medium text-primary-700 dark:text-primary-300 underline hover:no-underline"
            >
              creá un canal de venta
            </router-link>
            (también podés ir a Canales → Mis canales venta en el menú).
          </p>
        </div>

        <div
          class="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm"
        >
          <table class="min-w-full text-sm">
            <thead>
              <tr class="border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
                <th
                  class="sticky left-0 z-10 bg-gray-50 dark:bg-gray-900/50 px-3 py-3 text-left font-semibold text-gray-900 dark:text-white whitespace-nowrap border-r border-gray-200 dark:border-gray-700 min-w-[14rem]"
                >
                  Producto
                </th>
                <th
                  v-for="ch in tableChannels"
                  :key="ch.channelDestinationId"
                  class="px-2 py-3 align-top min-w-[7.5rem] max-w-[12rem] w-[9rem]"
                  :title="ch.marketplaceKey ? `${ch.name} · ${ch.marketplaceKey}` : ch.name"
                >
                  <div class="flex flex-col items-center gap-2 min-w-0">
                    <div
                      class="h-11 w-11 shrink-0 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 flex items-center justify-center overflow-hidden shadow-sm"
                    >
                      <img
                        :src="channelLogoSrc(ch)"
                        :alt="ch.name"
                        class="max-h-full max-w-full object-contain p-1"
                        @error="onChannelLogoError"
                      />
                    </div>
                    <div class="min-w-0 w-full text-center">
                      <span
                        class="block font-semibold text-gray-900 dark:text-white text-xs leading-snug break-words hyphens-auto"
                        >{{ ch.name }}</span
                      >
                      <span
                        v-if="!ch.isActive"
                        class="mt-1 block text-[10px] font-medium text-amber-800 dark:text-amber-200"
                        >Detenido</span
                      >
                    </div>
                  </div>
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in rows" :key="row.catalogProductId" class="border-b border-gray-100 dark:border-gray-700/80">
                <td
                  class="sticky left-0 z-[1] bg-white dark:bg-gray-800 px-3 py-2.5 text-gray-900 dark:text-gray-100 border-r border-gray-100 dark:border-gray-700 max-w-[20rem]"
                >
                  <div class="flex items-center gap-3 min-w-0">
                    <div
                      class="h-11 w-11 shrink-0 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700/80 overflow-hidden flex items-center justify-center"
                    >
                      <img
                        v-if="row.primaryImageUrl && !productImageFailed(row.catalogProductId)"
                        :src="row.primaryImageUrl"
                        alt=""
                        class="h-full w-full object-cover"
                        @error="() => markProductImageFailed(row.catalogProductId)"
                      />
                      <svg
                        v-else
                        class="h-6 w-6 text-gray-400 dark:text-gray-500"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="1.5"
                          d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
                        />
                      </svg>
                    </div>
                    <span class="line-clamp-2 text-sm leading-snug min-w-0" :title="row.title">{{ row.title }}</span>
                  </div>
                </td>
                <td
                  v-for="ch in tableChannels"
                  :key="`${row.catalogProductId}-${ch.channelDestinationId}`"
                  class="px-2 py-2.5 align-top text-center"
                >
                  <template v-if="cellFor(row, ch)">
                    <div class="font-mono text-xs text-gray-900 dark:text-gray-100 break-all max-w-[10rem] mx-auto">
                      {{ cellFor(row, ch).externalListingId || '—' }}
                    </div>
                    <div
                      v-if="cellFor(row, ch).syncState"
                      class="mt-0.5 text-[10px] text-gray-500 dark:text-gray-400 uppercase tracking-wide"
                    >
                      {{ cellFor(row, ch).syncState }}
                    </div>
                  </template>
                  <span v-else class="text-gray-400 dark:text-gray-600">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="rows.length === 0" class="text-center py-10 text-gray-500 dark:text-gray-400 text-sm">
          No hay productos en este catálogo.
        </div>

        <div v-if="totalPages > 1" class="flex flex-wrap items-center justify-between gap-3 pt-2">
          <p class="text-sm text-gray-600 dark:text-gray-400">
            Página {{ page }} de {{ totalPages }}
          </p>
          <div class="flex gap-2">
            <button
              type="button"
              :disabled="page <= 1 || matrixLoading"
              class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium disabled:opacity-40"
              @click="goPage(page - 1)"
            >
              Anterior
            </button>
            <button
              type="button"
              :disabled="page >= totalPages || matrixLoading"
              class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium disabled:opacity-40"
              @click="goPage(page + 1)"
            >
              Siguiente
            </button>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { useAdminPaths } from '../../composables/useAdminPaths'

const paths = useAdminPaths()
const route = useRoute()
const router = useRouter()
const toast = useToast()

const pipelinesLoading = ref(true)
const pipelinesError = ref(null)
const allPipelines = ref([])

const selectedPipelineId = ref('')
/** Filtro opcional: mismo slice de catálogo que el destino (filters en DestinationConfigJson + pipeline alineado). */
const selectedChannelDestinationId = ref('')
const matrixLoading = ref(false)
const matrixError = ref(null)
const channels = ref([])
const rows = ref([])
const page = ref(1)
const pageSize = ref(25)
const totalCount = ref(0)
const csvExporting = ref(false)

const availableIntegrations = ref([])
const myIntegrations = ref([])
const failedChannelLogoUrls = ref(new Set())
const failedProductImageIds = ref(new Set())

const catalogPipelines = computed(() =>
  (allPipelines.value || []).filter(
    (p) =>
      String(p.entityType || p.EntityType || '').toLowerCase() === 'products' &&
      String(p.pipelineDirection || p.PipelineDirection || '').toLowerCase() === 'outbound'
  )
)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value) || 1))

const sliceFilterChannelOptions = computed(() =>
  (channels.value || []).filter((c) => c.isCatalogPipelineAligned)
)

/** Columnas visibles: todas o solo el canal elegido (backend ya devuelve celdas acotadas). */
const tableChannels = computed(() => {
  const id = String(selectedChannelDestinationId.value || '').trim()
  if (!id) return channels.value || []
  return (channels.value || []).filter((c) => String(c.channelDestinationId) === id)
})

function pipelineLabel(p) {
  return p.name || p.Name || p.id
}

function cellKey(ch) {
  return String(ch.channelDestinationId ?? ch.ChannelDestinationId ?? '')
}

function cellFor(row, ch) {
  const map = row.cellsByChannelDestinationId || {}
  const k = cellKey(ch)
  return map[k] ?? map[ch.channelDestinationId] ?? null
}

function metaLogoForKey(key) {
  if (key == null || key === '') return null
  const k = String(key).trim().toLowerCase()
  const meta = availableIntegrations.value.find((m) => String(m.key || '').toLowerCase() === k)
  return meta?.logoUrl ?? meta?.LogoUrl ?? null
}

function channelLogoSrc(ch) {
  const mid = ch.marketplaceIntegrationId ?? ch.MarketplaceIntegrationId
  if (mid) {
    const int = myIntegrations.value.find((i) => String(i.id ?? i.Id) === String(mid))
    if (int) {
      const fromInt = int.logoUrl ?? int.LogoUrl ?? null
      if (fromInt && String(fromInt).trim() && !failedChannelLogoUrls.value.has(String(fromInt)))
        return String(fromInt).trim()
      const prov = int.provider ?? int.Provider
      const u = metaLogoForKey(prov)
      if (u && !failedChannelLogoUrls.value.has(u)) return u
    }
  }
  const prov2 = ch.integrationProvider ?? ch.IntegrationProvider
  const u2 = metaLogoForKey(prov2) || metaLogoForKey(ch.marketplaceKey)
  if (u2 && !failedChannelLogoUrls.value.has(u2)) return u2
  return '/images/avatar-default.svg'
}

function onChannelLogoError(e) {
  const src = e?.target?.src
  if (src && src !== '/images/avatar-default.svg') {
    failedChannelLogoUrls.value = new Set([...failedChannelLogoUrls.value, src])
  }
}

function productImageFailed(id) {
  return failedProductImageIds.value.has(String(id))
}

function markProductImageFailed(id) {
  failedProductImageIds.value = new Set([...failedProductImageIds.value, String(id)])
}

async function loadIntegrationLogos() {
  const [avail, mine] = await Promise.all([
    apiService.getAvailableIntegrations().catch(() => []),
    apiService.getMyIntegrations().catch(() => [])
  ])
  availableIntegrations.value = Array.isArray(avail) ? avail : []
  myIntegrations.value = Array.isArray(mine) ? mine : []
}

async function loadPipelines() {
  pipelinesLoading.value = true
  pipelinesError.value = null
  try {
    const list = await apiService.getDataPipelineLookups(null, {
      entityType: 'products',
      direction: 'outbound'
    })
    allPipelines.value = (Array.isArray(list) ? list : []).map((p) => ({
      ...p,
      entityType: 'products',
      pipelineDirection: 'outbound'
    }))
    const qPid = String(route.query.pipelineId || '').trim()
    if (qPid && catalogPipelines.value.some((p) => String(p.id) === qPid)) {
      selectedPipelineId.value = qPid
    } else if (catalogPipelines.value.length > 0) {
      selectedPipelineId.value = String(catalogPipelines.value[0].id)
    } else {
      selectedPipelineId.value = ''
    }
    selectedChannelDestinationId.value = String(route.query.channelDestinationId || '').trim()
  } catch (e) {
    pipelinesError.value = e.response?.data?.error || e.message || 'No se pudieron cargar los pipelines.'
  } finally {
    pipelinesLoading.value = false
  }
}

function goPage(p) {
  page.value = Math.max(1, p)
}

async function downloadMatrixCsv() {
  const pid = String(selectedPipelineId.value || '').trim()
  if (!pid) return
  csvExporting.value = true
  try {
    await apiService.downloadPublicationsMatrixCsv(pid, 'publicaciones-matriz', {
      channelDestinationId: String(selectedChannelDestinationId.value || '').trim() || null
    })
    toast.success('CSV descargado.')
  } catch (e) {
    const msg = e.response?.data?.error || e.message || 'No se pudo generar el CSV.'
    toast.error(typeof msg === 'string' ? msg : 'No se pudo generar el CSV.')
  } finally {
    csvExporting.value = false
  }
}

watch(selectedPipelineId, (id, oldId) => {
  page.value = 1
  const q = { ...route.query }
  if (id) q.pipelineId = String(id)
  else delete q.pipelineId

  if (!String(id || '').trim()) {
    selectedChannelDestinationId.value = ''
    delete q.channelDestinationId
  } else if (
    oldId !== undefined &&
    String(oldId || '').trim() !== '' &&
    String(oldId || '') !== String(id || '')
  ) {
    selectedChannelDestinationId.value = ''
    delete q.channelDestinationId
  }

  void router.replace({ query: q })
})

watch(selectedChannelDestinationId, (cid) => {
  page.value = 1
  const q = { ...route.query }
  if (selectedPipelineId.value) q.pipelineId = String(selectedPipelineId.value)
  const c = String(cid || '').trim()
  if (c) q.channelDestinationId = c
  else delete q.channelDestinationId
  void router.replace({ query: q })
})

let matrixFetchSeq = 0

watch([selectedPipelineId, page, selectedChannelDestinationId], async () => {
  const pid = String(selectedPipelineId.value || '').trim()
  if (!pid) {
    channels.value = []
    rows.value = []
    totalCount.value = 0
    matrixError.value = null
    matrixLoading.value = false
    return
  }
  const seq = ++matrixFetchSeq
  matrixLoading.value = true
  matrixError.value = null
  const cid = String(selectedChannelDestinationId.value || '').trim()
  try {
    const data = await apiService.getCatalogPublicationsMatrix(pid, {
      page: page.value,
      pageSize: pageSize.value,
      channelDestinationId: cid || null
    })
    if (seq !== matrixFetchSeq) return
    channels.value = data.channels || []
    rows.value = data.rows || []
    totalCount.value = Number(data.totalCount) || 0
  } catch (e) {
    if (seq !== matrixFetchSeq) return
    if (e.response?.status === 404) {
      if (cid) {
        selectedChannelDestinationId.value = ''
        const q = { ...route.query }
        delete q.channelDestinationId
        void router.replace({ query: q })
        return
      }
      matrixError.value = 'Pipeline no encontrado o sin acceso.'
    } else {
      matrixError.value = e.response?.data?.error || e.message || 'Error al cargar la matriz.'
    }
    channels.value = []
    rows.value = []
    totalCount.value = 0
  } finally {
    if (seq === matrixFetchSeq) matrixLoading.value = false
  }
})

onMounted(async () => {
  await Promise.all([loadPipelines(), loadIntegrationLogos()])
})
</script>

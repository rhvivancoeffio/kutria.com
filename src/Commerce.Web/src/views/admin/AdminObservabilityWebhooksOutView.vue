<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8">
        <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Webhooks salientes (histórico)</h1>
        <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          Envíos HTTP registrados por tipo de evento de dominio. La consulta usa la partición
          <span class="font-mono text-xs">account|workspace|domainEventType</span> y paginación por
          <span class="font-mono text-xs">continuationToken</span>.
        </p>
      </div>

      <div
        v-if="!workspaceId"
        class="p-6 rounded-xl border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-900/20 text-amber-900 dark:text-amber-200"
      >
        <p class="font-medium">Selecciona un workspace</p>
        <p class="mt-1 text-sm opacity-90">El histórico se filtra por workspace y tipo de evento.</p>
      </div>

      <template v-else>
        <div class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 mb-4 sm:mb-6">
          <div class="flex flex-col lg:flex-row lg:flex-wrap gap-4 items-stretch lg:items-end">
            <div class="flex-1 min-w-0 sm:min-w-[240px]">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Tipo de evento (dominio)</label>
              <select
                v-model="domainEventType"
                class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500 text-sm"
                @change="onDomainEventChange"
              >
                <option v-for="opt in domainEventOptions" :key="opt" :value="opt">{{ opt }}</option>
              </select>
            </div>
            <div class="flex flex-col sm:flex-row gap-2">
              <button
                type="button"
                :disabled="loading"
                class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] touch-manipulation bg-primary-600 hover:bg-primary-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50"
                @click="loadDispatches()"
              >
                Actualizar
              </button>
            </div>
          </div>
        </div>

        <div v-if="loading" class="space-y-3">
          <div v-for="i in 6" :key="i" class="h-14 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
        </div>

        <div
          v-else-if="error"
          class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
        >
          <p class="text-red-800 dark:text-red-300 text-sm">{{ error }}</p>
        </div>

        <div
          v-else-if="!items.length"
          class="text-center py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
        >
          <p class="text-sm text-gray-500 dark:text-gray-400">No hay registros para este tipo de evento.</p>
        </div>

        <div v-else class="space-y-4 md:space-y-0">
          <div class="md:hidden space-y-3">
            <div
              v-for="row in items"
              :key="row.rowKey"
              class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
            >
              <div class="flex items-center justify-between gap-2">
                <span
                  :class="[
                    'inline-flex px-2 py-0.5 text-xs font-medium rounded-full',
                    row.succeeded
                      ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                      : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
                  ]"
                >
                  {{ row.succeeded ? 'OK' : 'Error' }} · HTTP {{ row.httpStatus || '—' }}
                </span>
                <button
                  type="button"
                  class="inline-flex p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700/60 touch-manipulation shrink-0"
                  aria-label="Ver detalle"
                  @click="openDetail(row)"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                    />
                  </svg>
                </button>
              </div>
              <p class="mt-2 text-xs text-gray-500">{{ formatWhen(row) }}</p>
              <p class="mt-1 text-xs font-mono text-gray-600 dark:text-gray-300 truncate">{{ hookLabel(row.subscriptionId) }}</p>
            </div>
          </div>

          <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-700/50">
                  <tr>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Fecha
                    </th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Resultado
                    </th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      ms
                    </th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Webhook
                    </th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Error / respuesta
                    </th>
                    <th class="px-4 lg:px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                      Acción
                    </th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                  <tr
                    v-for="row in items"
                    :key="row.rowKey"
                    class="hover:bg-gray-50 dark:hover:bg-gray-700/30"
                  >
                    <td class="px-4 lg:px-6 py-3 text-sm text-gray-600 dark:text-gray-300 whitespace-nowrap">
                      {{ formatWhen(row) }}
                    </td>
                    <td class="px-4 lg:px-6 py-3">
                      <span
                        :class="[
                          'inline-flex px-2 py-0.5 text-xs font-medium rounded-full',
                          row.succeeded
                            ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                            : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300'
                        ]"
                      >
                        {{ row.succeeded ? 'OK' : 'Falló' }} · {{ row.httpStatus || '—' }}
                      </span>
                    </td>
                    <td class="px-4 lg:px-6 py-3 text-sm text-gray-600 dark:text-gray-300">
                      {{ Math.round(row.durationMs ?? 0) }}
                    </td>
                    <td class="px-4 lg:px-6 py-3 text-xs font-mono text-primary-600 dark:text-primary-400 max-w-[200px] truncate" :title="hookLabel(row.subscriptionId)">
                      {{ hookLabel(row.subscriptionId) }}
                    </td>
                    <td class="px-4 lg:px-6 py-3 text-xs text-gray-600 dark:text-gray-300 max-w-md truncate" :title="snippet(row)">
                      {{ snippet(row) }}
                    </td>
                    <td class="px-4 lg:px-6 py-3 text-right">
                      <button
                        type="button"
                        class="inline-flex p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700/60 touch-manipulation"
                        aria-label="Ver detalle"
                        @click="openDetail(row)"
                      >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                          <path
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="2"
                            d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                          />
                        </svg>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <div
            v-if="canShowPagination"
            class="flex justify-end gap-2 px-2 md:px-0 py-3"
          >
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
      </template>
    </main>

    <Teleport to="body">
      <Transition name="wh-slide-overlay">
        <div
          v-if="detailOpen"
          class="fixed inset-0 bg-black/50 z-50"
          aria-hidden="true"
          @click="closeDetail"
        />
      </Transition>
      <Transition name="wh-slide-panel">
        <aside
          v-if="detailOpen && detailRow"
          class="fixed inset-y-0 right-0 z-[60] h-full w-full max-w-2xl bg-white dark:bg-gray-800 shadow-2xl flex flex-col border-l border-gray-200 dark:border-gray-700"
          role="dialog"
          aria-modal="true"
          aria-labelledby="wh-out-detail-title"
          @click.stop
        >
          <div class="shrink-0 px-4 sm:px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between gap-3">
            <h3 id="wh-out-detail-title" class="text-lg font-semibold text-gray-900 dark:text-white truncate pr-2">
              Detalle del envío
            </h3>
            <button
              type="button"
              class="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
              aria-label="Cerrar"
              @click="closeDetail"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="flex-1 overflow-y-auto px-4 sm:px-6 py-5 space-y-4 text-sm">
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Tipo de evento</p>
              <p class="mt-0.5 font-mono text-gray-900 dark:text-white break-all">{{ domainEventType }}</p>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Fecha (registro)</p>
              <p class="mt-0.5 text-gray-900 dark:text-white">{{ formatWhen(detailRow) }}</p>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">HTTP</p>
                <p class="mt-0.5 text-gray-900 dark:text-white">{{ detailRow.httpStatus || '—' }}</p>
              </div>
              <div>
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Duración (ms)</p>
                <p class="mt-0.5 text-gray-900 dark:text-white">{{ Math.round(detailRow.durationMs ?? 0) }}</p>
              </div>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Suscripción (hook)</p>
              <p class="mt-0.5 font-mono text-xs text-gray-900 dark:text-white break-all">{{ detailRow.subscriptionId }}</p>
              <p v-if="hookLabel(detailRow.subscriptionId) !== String(detailRow.subscriptionId)" class="mt-1 text-gray-600 dark:text-gray-300">
                {{ hookLabel(detailRow.subscriptionId) }}
              </p>
            </div>
            <div v-if="detailRow.outboxMessageId">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Outbox message</p>
              <p class="mt-0.5 font-mono text-xs break-all text-gray-900 dark:text-white">{{ detailRow.outboxMessageId }}</p>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">RowKey</p>
              <p class="mt-0.5 font-mono text-xs break-all text-gray-900 dark:text-white">{{ detailRow.rowKey }}</p>
            </div>
            <div v-if="detailRow.errorMessage">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Error</p>
              <pre class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-red-800 dark:text-red-300 whitespace-pre-wrap">{{ detailRow.errorMessage }}</pre>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Request</p>
              <pre
                v-if="detailRow.requestLogSnippet"
                class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200 max-h-72 overflow-y-auto whitespace-pre-wrap font-mono"
              >{{ detailRow.requestLogSnippet }}</pre>
              <p v-else class="text-xs text-gray-500 dark:text-gray-400 italic">Sin registro de request (envíos anteriores al log verboso).</p>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Response</p>
              <pre
                v-if="detailRow.responseLogSnippet"
                class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-gray-800 dark:text-gray-200 max-h-72 overflow-y-auto whitespace-pre-wrap font-mono"
              >{{ detailRow.responseLogSnippet }}</pre>
              <p v-else class="text-xs text-gray-500 dark:text-gray-400 italic">Sin registro de response.</p>
            </div>

            <div class="pt-2 border-t border-gray-200 dark:border-gray-700 space-y-3">
              <p class="text-xs text-gray-600 dark:text-gray-400">
                Re-enviar vuelve a publicar el evento al webhook de esta suscripción usando el mensaje outbox original.
                Se registra un nuevo intento en el histórico.
              </p>
              <p
                v-if="!detailRow.outboxMessageId"
                class="text-xs text-amber-800 dark:text-amber-200"
              >
                Este registro no tiene outbox message asociado; no se puede re-enviar.
              </p>
              <p
                v-else-if="resendFeedback"
                :class="[
                  'text-xs rounded-lg px-3 py-2',
                  resendFeedback.ok
                    ? 'bg-green-50 text-green-800 dark:bg-green-900/30 dark:text-green-300'
                    : 'bg-red-50 text-red-800 dark:bg-red-900/30 dark:text-red-300'
                ]"
              >
                {{ resendFeedback.message }}
              </p>
              <button
                type="button"
                :disabled="resendLoading || !detailRow.outboxMessageId"
                class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg font-medium text-white bg-amber-600 hover:bg-amber-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                @click="resendDispatch"
              >
                {{ resendLoading ? 'Re-enviando…' : 'Re-enviar evento' }}
              </button>
            </div>
          </div>
        </aside>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import apiService from '../../services/api'

const ACTIVE_WORKSPACE_KEY = 'active_workspace_id'

const DEFAULT_EVENT = 'ChannelSaleOrder.Created.v1'

const workspaceId = computed(() => {
  if (typeof localStorage === 'undefined') return ''
  return localStorage.getItem(ACTIVE_WORKSPACE_KEY)?.trim() || ''
})

const domainEventType = ref(DEFAULT_EVENT)
const domainEventOptions = ref([DEFAULT_EVENT])
const hooksBySubscriptionId = ref(new Map())

const loading = ref(false)
const error = ref(null)
const items = ref([])
const pageSize = ref(50)
const continuationToken = ref(null)
const tokenUsedForCurrentPage = ref(null)
const tokenStack = ref([])
const hasMore = ref(false)

const detailOpen = ref(false)
const detailRow = ref(null)
const resendLoading = ref(false)
const resendFeedback = ref(null)

const canGoPrevious = computed(() => tokenStack.value.length > 0)
const canShowPagination = computed(() => canGoPrevious.value || hasMore.value)

function hookLabel(subscriptionId) {
  const id = subscriptionId ? String(subscriptionId) : ''
  const name = hooksBySubscriptionId.value.get(id)
  return name || id || '—'
}

function snippet(row) {
  if (row.errorMessage) return row.errorMessage
  if (row.responseLogSnippet) return row.responseLogSnippet
  return '—'
}

function formatWhen(row) {
  const raw = row.dispatchedAtUtc
  if (!raw) return '—'
  const d = new Date(raw)
  if (Number.isNaN(d.getTime())) return String(raw)
  return d.toLocaleString()
}

async function loadHookNames() {
  const wid = workspaceId.value
  if (!wid) return
  try {
    const hooks = await apiService.listOutboundEventHooks(wid)
    const map = new Map()
    const types = new Set([DEFAULT_EVENT])
    for (const h of hooks) {
      if (h.id) map.set(String(h.id), h.name || h.targetUrl || h.id)
      if (h.domainEventType) {
        for (const t of String(h.domainEventType).split(/[,|]/)) {
          const s = t.trim()
          if (s) types.add(s)
        }
      }
    }
    hooksBySubscriptionId.value = map
    domainEventOptions.value = [...types].sort()
    if (!types.has(domainEventType.value) && domainEventOptions.value.length) {
      domainEventType.value = domainEventOptions.value[0]
    }
  } catch {
    hooksBySubscriptionId.value = new Map()
    domainEventOptions.value = [DEFAULT_EVENT]
  }
}

async function loadDispatches(token = undefined) {
  const wid = workspaceId.value
  if (!wid) return
  if (token === undefined) {
    tokenStack.value = []
    continuationToken.value = null
    tokenUsedForCurrentPage.value = null
  }
  loading.value = true
  error.value = null
  try {
    const tokenToUse = token !== undefined ? token : continuationToken.value
    const result = await apiService.listOutboundHookDispatches({
      workspaceId: wid,
      domainEventType: domainEventType.value,
      pageSize: pageSize.value,
      continuationToken: tokenToUse != null ? tokenToUse : undefined
    })
    tokenUsedForCurrentPage.value = tokenToUse
    items.value = result.items ?? []
    continuationToken.value = result.continuationToken ?? null
    hasMore.value = result.hasMore ?? false
  } catch (e) {
    error.value = e.response?.data?.detail || e.response?.data?.title || e.message || 'Error al cargar histórico'
    items.value = []
    hasMore.value = false
  } finally {
    loading.value = false
  }
}

function onDomainEventChange() {
  loadDispatches()
}

async function goToNextPage() {
  tokenStack.value.push(tokenUsedForCurrentPage.value)
  await loadDispatches(continuationToken.value)
}

async function goToPreviousPage() {
  const prevToken = tokenStack.value.pop()
  await loadDispatches(prevToken ?? null)
}

function openDetail(row) {
  detailRow.value = row
  resendFeedback.value = null
  detailOpen.value = true
}

function closeDetail() {
  detailOpen.value = false
  detailRow.value = null
  resendFeedback.value = null
  resendLoading.value = false
}

async function resendDispatch() {
  const row = detailRow.value
  const wid = workspaceId.value
  if (!row?.outboxMessageId || !wid || !row.subscriptionId) return

  resendLoading.value = true
  resendFeedback.value = null
  try {
    const result = await apiService.resendOutboundHookDispatch({
      workspaceId: wid,
      domainEventType: domainEventType.value,
      subscriptionId: row.subscriptionId,
      outboxMessageId: row.outboxMessageId
    })
    if (result.succeeded) {
      resendFeedback.value = {
        ok: true,
        message: `Re-envío exitoso (HTTP ${result.httpStatus || '—'}, ${Math.round(result.durationMs ?? 0)} ms).`
      }
    } else {
      const detail = result.errorMessage || result.responseLogSnippet
      resendFeedback.value = {
        ok: false,
        message: detail
          ? `Re-envío falló (HTTP ${result.httpStatus || '—'}): ${detail}`
          : `Re-envío falló (HTTP ${result.httpStatus || '—'}).`
      }
    }
    await loadDispatches(tokenUsedForCurrentPage.value)
  } catch (e) {
    resendFeedback.value = {
      ok: false,
      message: e.response?.data?.detail || e.response?.data?.title || e.message || 'Error al re-enviar'
    }
  } finally {
    resendLoading.value = false
  }
}

watch(workspaceId, async (wid) => {
  if (!wid) {
    items.value = []
    return
  }
  await loadHookNames()
  await loadDispatches()
})

onMounted(async () => {
  await loadHookNames()
  if (workspaceId.value) await loadDispatches()
})
</script>

<style scoped>
.wh-slide-overlay-enter-active,
.wh-slide-overlay-leave-active {
  transition: opacity 0.25s ease;
}
.wh-slide-overlay-enter-from,
.wh-slide-overlay-leave-to {
  opacity: 0;
}
.wh-slide-panel-enter-active {
  transition: transform 0.28s ease-out;
}
.wh-slide-panel-leave-active {
  transition: transform 0.22s ease-in;
}
.wh-slide-panel-enter-from,
.wh-slide-panel-leave-to {
  transform: translateX(100%);
}
</style>

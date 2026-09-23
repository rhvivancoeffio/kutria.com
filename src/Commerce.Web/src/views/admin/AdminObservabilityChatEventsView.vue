<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8 flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Chat events</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Telemetría de turnos y agentes (<span class="font-mono text-xs">chat_turn</span> /
            <span class="font-mono text-xs">agent_invoked</span>). Partición por tenant, más recientes primero.
          </p>
        </div>
        <button
          type="button"
          :disabled="loading"
          class="shrink-0 px-4 py-2.5 min-h-[44px] touch-manipulation bg-primary-600 hover:bg-primary-700 text-white rounded-lg font-medium disabled:opacity-50 transition-colors"
          @click="reload"
        >
          {{ loading ? 'Cargando…' : 'Actualizar' }}
        </button>
      </div>

      <div
        class="mb-6 bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 text-sm"
        role="search"
        aria-label="Filtros de chat events"
      >
        <label class="block min-w-0">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Tipo</span>
          <select
            v-model="eventType"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white px-3 py-2"
            @change="reload"
          >
            <option value="">Todos</option>
            <option value="chat_turn">chat_turn</option>
            <option value="agent_invoked">agent_invoked</option>
          </select>
        </label>
        <label class="block min-w-0">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Solo vacíos / error</span>
          <select
            v-model="emptyOnly"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white px-3 py-2"
            @change="reload"
          >
            <option value="false">No</option>
            <option value="true">Sí</option>
          </select>
        </label>
        <label class="block min-w-0 sm:col-span-2">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Thread ID</span>
          <input
            v-model="threadId"
            type="text"
            placeholder="Filtrar por thread…"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white px-3 py-2 font-mono text-xs"
            @keydown.enter="reload"
          />
        </label>
      </div>

      <div
        v-if="error"
        class="mb-4 p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-sm text-red-800 dark:text-red-300"
      >
        {{ error }}
      </div>

      <div v-if="loading && items.length === 0" class="space-y-3">
        <div
          v-for="n in 5"
          :key="n"
          class="h-16 rounded-xl bg-gray-200/70 dark:bg-gray-700/50 animate-pulse"
        />
      </div>

      <div
        v-else-if="items.length === 0"
        class="rounded-xl border border-dashed border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-6 py-12 text-center text-sm text-gray-500 dark:text-gray-400"
      >
        No hay eventos de chat todavía. Habla con el widget y vuelve a actualizar.
      </div>

      <template v-else>
        <div class="hidden md:block overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <table class="min-w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-900/40 text-left text-xs uppercase tracking-wide text-gray-500 dark:text-gray-400">
              <tr>
                <th class="px-4 py-3 font-medium">Cuándo</th>
                <th class="px-4 py-3 font-medium">Tipo</th>
                <th class="px-4 py-3 font-medium">Agente</th>
                <th class="px-4 py-3 font-medium">Thread</th>
                <th class="px-4 py-3 font-medium">Tools</th>
                <th class="px-4 py-3 font-medium">ms</th>
                <th class="px-4 py-3 font-medium">Estado</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="row in items"
                :key="row.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/40 cursor-pointer"
                @click="openDetail(row)"
              >
                <td class="px-4 py-3 whitespace-nowrap text-gray-900 dark:text-gray-100">{{ formatWhen(row.occurredAt) }}</td>
                <td class="px-4 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">{{ row.eventType }}</td>
                <td class="px-4 py-3 text-gray-700 dark:text-gray-300">{{ row.agentKey || '—' }}</td>
                <td class="px-4 py-3 font-mono text-xs text-gray-600 dark:text-gray-400 max-w-[140px] truncate" :title="row.threadId">
                  {{ row.threadId }}
                </td>
                <td class="px-4 py-3 text-xs text-gray-600 dark:text-gray-400 max-w-[180px] truncate">
                  {{ toolsLabel(row.tools) }}
                </td>
                <td class="px-4 py-3 tabular-nums text-gray-700 dark:text-gray-300">{{ row.latencyMs }}</td>
                <td class="px-4 py-3">
                  <span
                    :class="[
                      'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
                      row.empty || row.error
                        ? 'bg-amber-100 text-amber-900 dark:bg-amber-900/40 dark:text-amber-200'
                        : 'bg-emerald-100 text-emerald-900 dark:bg-emerald-900/40 dark:text-emerald-200'
                    ]"
                  >
                    {{ row.error ? 'error' : row.empty ? 'empty' : 'ok' }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="md:hidden space-y-3">
          <button
            v-for="row in items"
            :key="row.id"
            type="button"
            class="w-full text-left rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4 touch-manipulation"
            @click="openDetail(row)"
          >
            <div class="flex items-start justify-between gap-2">
              <p class="font-mono text-xs text-gray-500 dark:text-gray-400">{{ row.eventType }}</p>
              <span
                :class="[
                  'shrink-0 inline-flex rounded-full px-2 py-0.5 text-xs font-medium',
                  row.empty || row.error
                    ? 'bg-amber-100 text-amber-900 dark:bg-amber-900/40 dark:text-amber-200'
                    : 'bg-emerald-100 text-emerald-900 dark:bg-emerald-900/40 dark:text-emerald-200'
                ]"
              >
                {{ row.error ? 'error' : row.empty ? 'empty' : 'ok' }}
              </span>
            </div>
            <p class="mt-1 text-sm text-gray-900 dark:text-white">{{ row.agentKey || '—' }} · {{ row.latencyMs }} ms</p>
            <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ formatWhen(row.occurredAt) }}</p>
            <p class="mt-1 font-mono text-xs text-gray-500 dark:text-gray-400 truncate">{{ row.threadId }}</p>
          </button>
        </div>

        <div
          v-if="canShowPagination"
          class="mt-4 flex flex-col-reverse sm:flex-row sm:justify-end gap-2"
        >
          <button
            type="button"
            :disabled="!canGoPrevious || loading"
            class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
            @click="goPrevious"
          >
            Anterior
          </button>
          <button
            type="button"
            :disabled="!hasMore || loading"
            class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50"
            @click="goNext"
          >
            Siguiente
          </button>
        </div>
      </template>
    </main>

    <Teleport to="body">
      <Transition name="chat-ev-overlay">
        <div
          v-if="detailOpen"
          class="fixed inset-0 bg-black/50 z-50"
          aria-hidden="true"
          @click="closeDetail"
        />
      </Transition>
      <Transition name="chat-ev-panel">
        <aside
          v-if="detailOpen && detailRow"
          class="fixed inset-y-0 right-0 z-[60] h-full w-full max-w-lg bg-white dark:bg-gray-800 shadow-2xl flex flex-col border-l border-gray-200 dark:border-gray-700"
          role="dialog"
          aria-modal="true"
          aria-labelledby="chat-ev-detail-title"
          @click.stop
        >
          <div class="shrink-0 px-4 sm:px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between gap-3">
            <h3 id="chat-ev-detail-title" class="text-lg font-semibold text-gray-900 dark:text-white truncate pr-2">
              Detalle del evento
            </h3>
            <button
              type="button"
              class="p-2 -m-2 shrink-0 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300"
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
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Cuándo</p>
              <p class="mt-0.5 text-gray-900 dark:text-white">{{ formatWhen(detailRow.occurredAt) }}</p>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Tipo</p>
                <p class="mt-0.5 font-mono text-xs text-gray-900 dark:text-white">{{ detailRow.eventType }}</p>
              </div>
              <div>
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Audience</p>
                <p class="mt-0.5 text-gray-900 dark:text-white">{{ detailRow.audience || '—' }}</p>
              </div>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Agente</p>
              <p class="mt-0.5 text-gray-900 dark:text-white">{{ detailRow.agentKey || '—' }}</p>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Thread</p>
              <p class="mt-0.5 font-mono text-xs break-all text-gray-900 dark:text-white">{{ detailRow.threadId }}</p>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Tools</p>
              <p class="mt-0.5 text-gray-900 dark:text-white">{{ toolsLabel(detailRow.tools) }}</p>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Latencia</p>
                <p class="mt-0.5 text-gray-900 dark:text-white">{{ detailRow.latencyMs }} ms</p>
              </div>
              <div>
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400">Empty</p>
                <p class="mt-0.5 text-gray-900 dark:text-white">{{ detailRow.empty ? 'true' : 'false' }}</p>
              </div>
            </div>
            <div v-if="detailRow.error">
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Error</p>
              <pre class="text-xs bg-gray-50 dark:bg-gray-900 p-3 rounded-lg overflow-x-auto text-red-800 dark:text-red-300 whitespace-pre-wrap">{{ detailRow.error }}</pre>
            </div>
            <div>
              <p class="text-xs font-medium text-gray-500 dark:text-gray-400">RowKey</p>
              <p class="mt-0.5 font-mono text-xs break-all text-gray-600 dark:text-gray-400">{{ detailRow.id }}</p>
            </div>
            <div class="pt-2">
              <button
                type="button"
                class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
                @click="filterByThread(detailRow.threadId)"
              >
                Filtrar por este thread
              </button>
            </div>
          </div>
        </aside>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import apiService from '../../services/api'

const loading = ref(false)
const error = ref(null)
const items = ref([])
const pageSize = ref(25)
const continuationToken = ref(null)
const tokenUsedForCurrentPage = ref(null)
const tokenStack = ref([])
const hasMore = ref(false)

const eventType = ref('')
const emptyOnly = ref('false')
const threadId = ref('')

const detailOpen = ref(false)
const detailRow = ref(null)

const canGoPrevious = computed(() => tokenStack.value.length > 0)
const canShowPagination = computed(() => canGoPrevious.value || hasMore.value)

function toolsLabel(tools) {
  if (!Array.isArray(tools) || tools.length === 0) return '—'
  return tools.join(', ')
}

function formatWhen(raw) {
  if (!raw) return '—'
  const d = new Date(raw)
  if (Number.isNaN(d.getTime())) return String(raw)
  return d.toLocaleString()
}

async function load(tokenToUse = null) {
  loading.value = true
  error.value = null
  try {
    const data = await apiService.listChatEvents({
      pageSize: pageSize.value,
      nextToken: tokenToUse != null ? tokenToUse : undefined,
      threadId: threadId.value.trim() || undefined,
      eventType: eventType.value || undefined,
      emptyOnly: emptyOnly.value === 'true'
    })
    tokenUsedForCurrentPage.value = tokenToUse
    items.value = data.items
    continuationToken.value = data.nextToken
    hasMore.value = !!data.hasMore
  } catch (e) {
    error.value = e?.response?.data?.error || e?.message || 'No se pudo cargar chat events.'
    items.value = []
    continuationToken.value = null
    hasMore.value = false
  } finally {
    loading.value = false
  }
}

function reload() {
  tokenStack.value = []
  load(null)
}

async function goNext() {
  if (!hasMore.value || !continuationToken.value) return
  tokenStack.value.push(tokenUsedForCurrentPage.value)
  await load(continuationToken.value)
}

async function goPrevious() {
  const prevToken = tokenStack.value.pop()
  await load(prevToken ?? null)
}

function openDetail(row) {
  detailRow.value = row
  detailOpen.value = true
}

function closeDetail() {
  detailOpen.value = false
  detailRow.value = null
}

function filterByThread(id) {
  threadId.value = id || ''
  closeDetail()
  reload()
}

onMounted(() => reload())
</script>

<style scoped>
.chat-ev-overlay-enter-active,
.chat-ev-overlay-leave-active {
  transition: opacity 0.2s ease;
}
.chat-ev-overlay-enter-from,
.chat-ev-overlay-leave-to {
  opacity: 0;
}
.chat-ev-panel-enter-active,
.chat-ev-panel-leave-active {
  transition: transform 0.25s ease;
}
.chat-ev-panel-enter-from,
.chat-ev-panel-leave-to {
  transform: translateX(100%);
}
</style>

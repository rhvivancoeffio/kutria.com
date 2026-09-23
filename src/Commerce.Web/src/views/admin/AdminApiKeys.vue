<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">API Keys</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Claves de acceso programático al tenant (MCP y API)
          </p>
          <p class="mt-1 text-xs text-gray-500 dark:text-gray-500">
            Usa el header <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">x-api-key</code> contra
            <router-link to="/admin/mcp" class="text-primary-600 dark:text-primary-400 hover:underline">el host MCP</router-link>.
          </p>
        </div>
        <button
          type="button"
          @click="openCreateModal"
          class="inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white transition-colors min-h-[44px] touch-manipulation shrink-0"
        >
          <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Crear API Key
        </button>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="space-y-3 sm:space-y-4">
        <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
      </div>

      <!-- Empty -->
      <div
        v-else-if="!apiKeys.length"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
        </svg>
        <p class="mt-4 text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay API keys configuradas</p>
        <p class="mt-1 text-sm text-gray-400 dark:text-gray-500">Crea una para acceder a la API de forma programática</p>
        <button
          type="button"
          @click="openCreateModal"
          class="mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white"
        >
          Crear API Key
        </button>
      </div>

      <!-- List -->
      <div v-else class="space-y-4 md:space-y-0">
        <!-- Mobile: cards -->
        <div class="md:hidden space-y-3">
          <div
            v-for="key in apiKeys"
            :key="key.id"
            class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0 flex-1">
                <p class="font-semibold text-gray-900 dark:text-white truncate">{{ key.name }}</p>
                <p class="mt-0.5 font-mono text-sm text-gray-500 dark:text-gray-400 truncate">{{ key.keyPrefix }}</p>
                <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Creada: {{ formatDate(key.createdAt) }}</p>
                <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">Último uso: {{ formatDate(key.lastUsedAt) || '-' }}</p>
                <p v-if="key.channelDataPipelineId" class="mt-0.5 text-xs text-primary-600 dark:text-primary-400 font-mono truncate">
                  Pipeline: {{ key.channelDataPipelineId }}
                </p>
                <span
                  :class="[
                    'inline-block mt-2 px-2 py-0.5 text-xs font-medium rounded-full',
                    key.isActive ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                  ]"
                >
                  {{ key.isActive ? 'Activa' : 'Revocada' }}
                </span>
              </div>
              <div class="flex gap-1 shrink-0">
                <button
                  v-if="key.isActive"
                  type="button"
                  @click="confirmRevoke(key)"
                  class="p-2 rounded-lg text-amber-600 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-900/20 active:bg-amber-100 dark:active:bg-amber-900/30 transition-colors touch-manipulation"
                  title="Revocar"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                  </svg>
                </button>
                <button
                  type="button"
                  @click="confirmDelete(key)"
                  class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 active:bg-red-100 dark:active:bg-red-900/30 transition-colors touch-manipulation"
                  title="Eliminar"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>
        <!-- Desktop: table -->
        <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-x-auto">
          <table v-resizable class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-700/50">
              <tr>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Nombre</th>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Catálogo</th>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Prefijo</th>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Creada</th>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Último uso</th>
                <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Estado</th>
                <th class="px-4 lg:px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Acciones</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="key in apiKeys"
                :key="key.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/30"
              >
                <td class="px-4 lg:px-6 py-4 font-medium text-gray-900 dark:text-white">{{ key.name }}</td>
                <td class="px-4 lg:px-6 py-4 text-xs font-mono text-gray-500 dark:text-gray-400 max-w-[140px] truncate" :title="key.channelDataPipelineId || ''">
                  {{ key.channelDataPipelineId ? 'Sí' : '—' }}
                </td>
                <td class="px-4 lg:px-6 py-4 font-mono text-sm text-gray-600 dark:text-gray-300">{{ key.keyPrefix }}</td>
                <td class="px-4 lg:px-6 py-4 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(key.createdAt) }}</td>
                <td class="px-4 lg:px-6 py-4 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(key.lastUsedAt) || '-' }}</td>
                <td class="px-4 lg:px-6 py-4">
                  <span
                    :class="[
                      'px-2 py-1 text-xs font-medium rounded-full',
                      key.isActive ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                    ]"
                  >
                    {{ key.isActive ? 'Activa' : 'Revocada' }}
                  </span>
                </td>
                <td class="px-4 lg:px-6 py-4 text-right">
                  <div class="inline-flex gap-1">
                    <button
                      v-if="key.isActive"
                      type="button"
                      @click="confirmRevoke(key)"
                      class="inline-flex p-2 rounded-lg text-amber-600 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-900/20 transition-colors"
                      title="Revocar"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      @click="confirmDelete(key)"
                      class="inline-flex p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                      title="Eliminar"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </main>

    <!-- Create API Key - Slide modal (mismo patrón que Integrations) -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="createModalOpen"
          class="fixed inset-0 z-50 bg-black/50"
        />
      </Transition>
      <Transition
        enter-active-class="transition duration-300 ease-out transform"
        enter-from-class="translate-x-full"
        enter-to-class="translate-x-0"
        leave-active-class="transition duration-200 ease-in transform"
        leave-from-class="translate-x-0"
        leave-to-class="translate-x-full"
      >
        <div
          v-if="createModalOpen"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-md sm:max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 flex items-start justify-between shrink-0">
            <div>
              <h3 class="text-lg font-semibold text-gray-900 dark:text-white">
                {{ createdKey ? 'API Key creada' : 'Crear API Key' }}
              </h3>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                {{ createdKey ? 'Copia la clave ahora. No podrás verla de nuevo.' : 'Asigna un nombre para identificar esta clave.' }}
              </p>
            </div>
            <button
              type="button"
              @click="closeCreateModal"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              aria-label="Cerrar"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
            <div v-if="!createdKey" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
                <input
                  v-model="createName"
                  type="text"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500"
                  placeholder="ej. Producción, CI/CD"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Pipeline de catálogo (opcional)</label>
                <select
                  v-model="createPipelineId"
                  class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500"
                >
                  <option value="">Cualquiera (según reglas de la API)</option>
                  <option v-for="p in catalogPipelinesForKey" :key="p.id" :value="p.id">
                    {{ p.name }} ({{ p.id }})
                  </option>
                </select>
                <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                  Si eliges uno, la clave solo podrá usar ese data pipeline en la API de catálogo externo.
                </p>
              </div>
            </div>

            <div v-else class="space-y-4">
              <div class="p-4 rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800">
                <label class="block text-sm font-medium text-amber-800 dark:text-amber-300 mb-2">Tu API Key (cópiala ahora)</label>
                <div class="flex gap-2">
                  <input :value="createdKey.key" readonly class="w-full min-w-0 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 font-mono text-sm" />
                  <button
                    type="button"
                    @click="copyKey"
                    class="px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg bg-amber-600 hover:bg-amber-700 text-white text-sm font-medium shrink-0"
                  >
                    Copiar
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div class="shrink-0 px-4 sm:px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 bg-gray-50 dark:bg-gray-700/50">
            <template v-if="!createdKey">
              <button
                type="button"
                @click="closeCreateModal"
                class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700"
              >
                Cancelar
              </button>
              <button
                type="button"
                @click="createKey"
                :disabled="createLoading || !createName?.trim()"
                :class="[
                  'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
                  createLoading || !createName?.trim()
                    ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                    : 'bg-primary-600 hover:bg-primary-700 text-white'
                ]"
              >
                {{ createLoading ? 'Creando...' : 'Crear' }}
              </button>
            </template>
            <template v-else>
              <button
                type="button"
                @click="closeCreateModal"
                class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white"
              >
                Cerrar
              </button>
            </template>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Revoke confirmation -->
    <Teleport to="body">
      <div
        v-if="revokeTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Revocar API Key</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Revocar la API Key "{{ revokeTarget?.name }}"? Dejará de funcionar pero permanecerá en el listado.
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              @click="revokeTarget = null"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              Cancelar
            </button>
            <button
              type="button"
              :disabled="revokeKeyBusy"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
                revokeKeyBusy
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-amber-600 hover:bg-amber-700 text-white'
              ]"
              @click="doRevoke"
            >
              {{ revokeKeyBusy ? 'Revocando…' : 'Revocar' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delete confirmation -->
    <Teleport to="body">
      <div
        v-if="deleteTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar API Key</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Eliminar la API Key "{{ deleteTarget?.name }}"? Esta acción no se puede deshacer.
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              @click="deleteTarget = null"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              Cancelar
            </button>
            <button
              type="button"
              :disabled="deleteKeyBusy"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
                deleteKeyBusy
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-red-600 hover:bg-red-700 text-white'
              ]"
              @click="doDelete"
            >
              {{ deleteKeyBusy ? 'Eliminando…' : 'Eliminar' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'

const ACTIVE_WORKSPACE_KEY = 'active_workspace_id'
const ACTIVE_WORKSPACE_NAME_KEY = 'active_workspace_name'

const toast = useToast()
const apiKeys = ref([])
const currentWorkspaceLabel = ref('')
const loading = ref(true)
const error = ref(null)
const createModalOpen = ref(false)
const createName = ref('')
const createPipelineId = ref('')
const catalogPipelinesForKey = ref([])
const createLoading = ref(false)
const createdKey = ref(null)
const revokeTarget = ref(null)
const deleteTarget = ref(null)
const revokeKeyBusy = ref(false)
const deleteKeyBusy = ref(false)

function refreshWorkspaceLabel() {
  if (typeof localStorage === 'undefined') {
    currentWorkspaceLabel.value = ''
    return
  }
  const name = localStorage.getItem(ACTIVE_WORKSPACE_NAME_KEY)?.trim()
  const id = localStorage.getItem(ACTIVE_WORKSPACE_KEY)?.trim()
  currentWorkspaceLabel.value = name || id || ''
}

function formatDate(d) {
  if (!d) return null
  const date = new Date(d)
  return date.toLocaleDateString('es', { dateStyle: 'short' }) + ' ' + date.toLocaleTimeString('es', { timeStyle: 'short' })
}

async function loadData() {
  loading.value = true
  error.value = null
  refreshWorkspaceLabel()
  try {
    apiKeys.value = await apiService.getApiKeys()
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar API keys'
  } finally {
    loading.value = false
  }
}

async function openCreateModal() {
  createName.value = ''
  createPipelineId.value = ''
  createdKey.value = null
  catalogPipelinesForKey.value = []
  createModalOpen.value = true
  try {
    catalogPipelinesForKey.value = await apiService.getDataPipelineLookups(null, {
      entityType: 'products',
      direction: 'outbound',
      isActive: true
    })
  } catch {
    catalogPipelinesForKey.value = []
  }
}

function closeCreateModal() {
  createModalOpen.value = false
  createName.value = ''
  createPipelineId.value = ''
  catalogPipelinesForKey.value = []
  createdKey.value = null
}

async function createKey() {
  const name = createName.value?.trim()
  if (!name) return
  createLoading.value = true
  try {
    const pid = createPipelineId.value?.trim() || null
    createdKey.value = await apiService.createApiKey(name, pid)
    await loadData()
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al crear API key')
  } finally {
    createLoading.value = false
  }
}

function copyKey() {
  if (!createdKey.value?.key) return
  navigator.clipboard.writeText(createdKey.value.key).then(() => {
    toast.success('API Key copiada al portapapeles')
  })
}

function confirmRevoke(key) {
  revokeTarget.value = key
}

function confirmDelete(key) {
  deleteTarget.value = key
}

async function doRevoke() {
  if (!revokeTarget.value || revokeKeyBusy.value) return
  const id = revokeTarget.value.id
  revokeKeyBusy.value = true
  try {
    await apiService.revokeApiKey(id)
    revokeTarget.value = null
    await loadData()
    toast.success('API Key revocada')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al revocar')
  } finally {
    revokeKeyBusy.value = false
  }
}

async function doDelete() {
  if (!deleteTarget.value || deleteKeyBusy.value) return
  const id = deleteTarget.value.id
  deleteKeyBusy.value = true
  try {
    await apiService.deleteApiKey(id)
    deleteTarget.value = null
    await loadData()
    toast.success('API Key eliminada')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al eliminar')
  } finally {
    deleteKeyBusy.value = false
  }
}

onMounted(loadData)
</script>

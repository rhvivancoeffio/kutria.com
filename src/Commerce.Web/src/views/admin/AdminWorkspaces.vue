<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <main>
      <div class="mb-6 sm:mb-8">
        <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Workspaces</h1>
        <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          Gestiona los workspaces de la cuenta y elige el workspace activo (MCP servers, integraciones y variables se acotan por workspace). La creación manual solo añade workspaces
          <span class="font-medium text-gray-800 dark:text-gray-200">sandbox</span>; el workspace de producción es fijo y lo gestiona el sistema.
        </p>
      </div>

      <div v-if="loading" class="space-y-3 sm:space-y-4">
        <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6">
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
      </div>

      <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        <article
          v-for="ws in workspaces"
          :key="ws.id"
          class="rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <h2 class="text-base font-semibold text-gray-900 dark:text-white truncate">{{ ws.name }}</h2>
              <div class="mt-2 flex items-center gap-2 flex-wrap">
                <span
                  class="inline-flex px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="
                    environmentLabel(ws) === 'Producción'
                      ? 'bg-violet-100 text-violet-800 dark:bg-violet-900/40 dark:text-violet-200'
                      : 'bg-sky-100 text-sky-800 dark:bg-sky-900/40 dark:text-sky-200'
                  "
                >
                  {{ environmentLabel(ws) }}
                </span>
                <span
                  v-if="ws.isSystem"
                  class="inline-flex px-2 py-0.5 rounded-full text-xs font-medium bg-gray-200 text-gray-800 dark:bg-gray-600 dark:text-gray-100"
                >
                  Sistema
                </span>
                <span
                  class="inline-flex px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="ws.isDefault ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300'"
                >
                  {{ ws.isDefault ? 'Por defecto' : 'Extra' }}
                </span>
                <span
                  class="inline-flex px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="isActive(ws.id) ? 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300'"
                >
                  {{ isActive(ws.id) ? 'Activo' : 'Inactivo' }}
                </span>
              </div>
            </div>

            <div class="inline-flex items-center gap-1 shrink-0">
              <ActionIconButton action="edit" title="Editar workspace" @click="openEditModal(ws)" />
              <ActionIconButton
                action="delete"
                :title="deleteWorkspaceTitle(ws)"
                :disabled="!canDeleteWorkspace(ws)"
                @click="openDeleteModal(ws)"
              />
            </div>
          </div>

          <div class="mt-4">
            <button
              type="button"
              @click="selectWorkspace(ws.id)"
              class="w-full inline-flex justify-center px-3 py-2 rounded-lg text-sm font-medium"
              :class="isActive(ws.id) ? 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300' : 'bg-primary-600 hover:bg-primary-700 text-white'"
            >
              {{ isActive(ws.id) ? 'Workspace activo' : 'Seleccionar workspace' }}
            </button>
          </div>
        </article>

        <article class="rounded-xl bg-primary-50/40 dark:bg-primary-950/20 border border-primary-200/60 dark:border-primary-900/40 p-4">
          <div class="flex items-start gap-3">
            <div
              class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary-100 dark:bg-primary-900/50 text-primary-600 dark:text-primary-400"
              aria-hidden="true"
            >
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
            </div>
            <div class="min-w-0 flex-1">
              <h2 class="text-base font-semibold text-gray-900 dark:text-white">Nuevo workspace</h2>
              <p class="mt-1 text-sm text-gray-600 dark:text-gray-400">
                Añade un workspace sandbox para separar servidores MCP, integraciones y claves API.
              </p>
            </div>
          </div>
          <div class="mt-4">
            <button
              type="button"
              @click="openCreateModal"
              class="w-full inline-flex justify-center items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium bg-primary-600 hover:bg-primary-700 text-white transition-colors"
            >
              Crear
            </button>
          </div>
        </article>
      </div>
    </main>

    <Teleport to="body">
      <div v-if="createModalOpen" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50">
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Crear workspace</h3>
          <input
            v-model="newWorkspaceName"
            type="text"
            class="mt-4 w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
            placeholder="Nombre del workspace"
          />
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700" @click="createModalOpen = false">Cancelar</button>
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white" @click="createWorkspace">Crear</button>
          </div>
        </div>
      </div>
    </Teleport>

    <Teleport to="body">
      <div v-if="editModalOpen" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50">
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Editar workspace</h3>
          <input v-model="editWorkspaceName" type="text" class="mt-4 w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700" @click="editModalOpen = false">Cancelar</button>
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white" @click="updateWorkspace">Guardar</button>
          </div>
        </div>
      </div>
    </Teleport>

    <Teleport to="body">
      <div v-if="deleteModalOpen" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50">
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar workspace</h3>
          <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">Esta acción no se puede deshacer. No podrás eliminar si hay datos en el workspace.</p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700" @click="deleteModalOpen = false">Cancelar</button>
            <button
              type="button"
              :disabled="deleteWorkspaceBusy"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 rounded-lg font-medium transition-colors',
                deleteWorkspaceBusy
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-red-600 hover:bg-red-700 text-white'
              ]"
              @click="deleteWorkspace"
            >
              {{ deleteWorkspaceBusy ? 'Eliminando…' : 'Eliminar' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { eventBus, WORKSPACE_CONTEXT_CHANGED } from '../../utils/eventBus'
import ActionIconButton from '../../components/common/ActionIconButton.vue'

const ACTIVE_WORKSPACE_KEY = 'active_workspace_id'
const ACTIVE_WORKSPACE_NAME_KEY = 'active_workspace_name'
const router = useRouter()
const toast = useToast()

const loading = ref(true)
const error = ref('')
const workspaces = ref([])
const createModalOpen = ref(false)
const newWorkspaceName = ref('')
const editModalOpen = ref(false)
const editWorkspaceId = ref('')
const editWorkspaceName = ref('')
const deleteModalOpen = ref(false)
const deleteWorkspaceId = ref('')
const deleteWorkspaceBusy = ref(false)

const activeWorkspaceId = computed(() => String(localStorage.getItem(ACTIVE_WORKSPACE_KEY) || ''))

function isActive(id) {
  return String(id) === activeWorkspaceId.value
}

/** API serializa el enum como string en camelCase (p. ej. production). */
function environmentLabel(ws) {
  const k = ws?.environmentKind
  if (k === 'production' || k === 'Production' || k === 1) return 'Producción'
  return 'Sandbox'
}

function canDeleteWorkspace(ws) {
  return !ws?.isDefault && !ws?.isSystem
}

function deleteWorkspaceTitle(ws) {
  if (ws?.isSystem) return 'Los workspaces del sistema no se pueden eliminar'
  if (ws?.isDefault) return 'No se puede eliminar el workspace por defecto'
  return 'Eliminar workspace'
}

function openCreateModal() {
  newWorkspaceName.value = ''
  createModalOpen.value = true
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const data = await apiService.getWorkspaces()
    workspaces.value = Array.isArray(data) ? data : []
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar workspaces'
  } finally {
    loading.value = false
  }
}

async function createWorkspace() {
  const name = newWorkspaceName.value.trim()
  if (!name) return
  try {
    const result = await apiService.createWorkspace(name)
    createModalOpen.value = false
    newWorkspaceName.value = ''
    await load()
    if (result?.id) {
      await selectWorkspace(result.id)
    }
    toast.success('Workspace creado')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'No se pudo crear el workspace')
  }
}

async function selectWorkspace(id) {
  const workspaceId = String(id)
  const ws = workspaces.value.find((w) => String(w.id) === workspaceId)
  const displayName = (ws?.name && String(ws.name).trim()) || ''
  try {
    await apiService.selectWorkspace(workspaceId, displayName || null)
    try {
      localStorage.setItem(ACTIVE_WORKSPACE_KEY, workspaceId)
      if (displayName) localStorage.setItem(ACTIVE_WORKSPACE_NAME_KEY, displayName)
    } catch {
      /* ignore */
    }
    eventBus.emit(WORKSPACE_CONTEXT_CHANGED)
    await router.replace('/admin/usage')
    toast.success('Workspace activo actualizado')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'No se pudo seleccionar el workspace')
  }
}

function openEditModal(ws) {
  editWorkspaceId.value = String(ws.id)
  editWorkspaceName.value = ws.name || ''
  editModalOpen.value = true
}

async function updateWorkspace() {
  const name = editWorkspaceName.value.trim()
  if (!name || !editWorkspaceId.value) return
  try {
    await apiService.updateWorkspace(editWorkspaceId.value, name)
    editModalOpen.value = false
    await load()
    if (String(editWorkspaceId.value) === activeWorkspaceId.value) {
      try {
        localStorage.setItem(ACTIVE_WORKSPACE_NAME_KEY, name)
      } catch {
        /* ignore */
      }
      eventBus.emit(WORKSPACE_CONTEXT_CHANGED)
    }
    toast.success('Workspace actualizado')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'No se pudo actualizar el workspace')
  }
}

function openDeleteModal(ws) {
  deleteWorkspaceId.value = String(ws.id)
  deleteModalOpen.value = true
}

async function deleteWorkspace() {
  if (!deleteWorkspaceId.value || deleteWorkspaceBusy.value) return
  deleteWorkspaceBusy.value = true
  try {
    await apiService.deleteWorkspace(deleteWorkspaceId.value)
    deleteModalOpen.value = false
    if (activeWorkspaceId.value === deleteWorkspaceId.value) {
      localStorage.removeItem(ACTIVE_WORKSPACE_KEY)
      try {
        localStorage.removeItem(ACTIVE_WORKSPACE_NAME_KEY)
      } catch {
        /* ignore */
      }
      eventBus.emit(WORKSPACE_CONTEXT_CHANGED)
      await router.replace('/admin/workspaces')
    }
    await load()
    toast.success('Workspace eliminado')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'No se pudo eliminar el workspace')
  } finally {
    deleteWorkspaceBusy.value = false
  }
}

onMounted(load)
</script>

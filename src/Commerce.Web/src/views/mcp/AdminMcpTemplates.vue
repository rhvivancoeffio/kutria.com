<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Plantillas de prompt</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Plantillas reutilizables para crear prompts. Al crear un nuevo prompt puedes elegir una plantilla como base.
          </p>
        </div>
        <router-link
          to="/admin/templates/new"
          class="inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white text-sm min-h-[44px] touch-manipulation shrink-0"
        >
          <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
          Nueva plantilla
        </router-link>
      </div>

      <div v-if="loading" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="i in 6" :key="i" class="h-32 bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse" />
      </div>
      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <p class="text-red-800 dark:text-red-300 text-sm">{{ error }}</p>
      </div>
      <div v-else-if="templates.length === 0" class="text-center py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
        <p class="text-gray-500 dark:text-gray-400">No hay plantillas. Crea una o guarda un prompt como plantilla desde el editor del prompt.</p>
        <router-link to="/admin/templates/new" class="mt-4 inline-block px-4 py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white text-sm">Nueva plantilla</router-link>
      </div>
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div
          v-for="t in templates"
          :key="t.id"
          class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm card-hover flex flex-col min-h-0"
        >
          <div class="min-w-0 flex-1">
            <p class="font-medium text-gray-900 dark:text-white truncate" :title="t.name">{{ t.name }}</p>
            <p v-if="t.title" class="mt-0.5 text-sm text-gray-500 dark:text-gray-400 truncate" :title="t.title">{{ t.title }}</p>
            <p v-if="t.description" class="mt-1.5 text-sm text-gray-600 dark:text-gray-300 line-clamp-3">{{ t.description }}</p>
          </div>
          <div class="flex gap-1 shrink-0 mt-3 pt-3 border-t border-gray-100 dark:border-gray-700">
            <router-link :to="`/admin/templates/${t.id}`" class="p-2 rounded-lg text-gray-500 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700" title="Editar">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
            </router-link>
            <button type="button" @click="confirmDelete(t)" class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20" title="Eliminar">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
            </button>
          </div>
        </div>
      </div>
    </main>

    <!-- Delete confirmation modal (same idea as MCP Server) -->
    <Teleport to="body">
      <div
        v-if="deleteTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar plantilla</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Eliminar la plantilla <strong class="text-gray-900 dark:text-white">{{ deleteTarget?.name }}</strong>? Esta acción no se puede deshacer.
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button type="button" @click="deleteTarget = null" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
              Cancelar
            </button>
            <button type="button" @click="doDelete" :disabled="deleting" :class="['w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors', deleting ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-red-600 hover:bg-red-700 text-white']">
              {{ deleting ? 'Eliminando…' : 'Eliminar' }}
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

const toast = useToast()
const loading = ref(true)
const error = ref(null)
const templates = ref([])
const deleteTarget = ref(null)
const deleting = ref(false)

async function load() {
  loading.value = true
  error.value = null
  try {
    const res = await apiService.getMcpPromptTemplates()
    templates.value = res?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar plantillas'
  } finally {
    loading.value = false
  }
}

function confirmDelete(t) {
  deleteTarget.value = t
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await apiService.deleteMcpPromptTemplate(deleteTarget.value.id)
    toast.success('Plantilla eliminada')
    deleteTarget.value = null
    await load()
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al eliminar')
  } finally {
    deleting.value = false
  }
}

onMounted(load)
</script>

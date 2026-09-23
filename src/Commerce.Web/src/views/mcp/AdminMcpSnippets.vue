<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Snippets</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Fragmentos reutilizables. Usa <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600 text-xs">&#123;&#123;snippet:Nombre&#125;&#125;</code> en los mensajes de un prompt para insertar el contenido.
          </p>
        </div>
        <button
          type="button"
          @click="openForm(null)"
          class="inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white text-sm min-h-[44px] touch-manipulation shrink-0"
        >
          <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
          Nuevo snippet
        </button>
      </div>

      <div v-if="loading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="h-20 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>
      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <p class="text-red-800 dark:text-red-300 text-sm">{{ error }}</p>
      </div>
      <div v-else-if="snippets.length === 0" class="text-center py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
        <p class="text-gray-500 dark:text-gray-400">No hay snippets. Crea uno para usarlo en prompts con <code class="text-xs">&#123;&#123;snippet:Nombre&#125;&#125;</code>.</p>
        <button type="button" @click="openForm(null)" class="mt-4 px-4 py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white text-sm">Nuevo snippet</button>
      </div>
      <div v-else class="space-y-3">
        <div
          v-for="s in snippets"
          :key="s.id"
          class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm card-hover"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0 flex-1">
              <p class="font-medium text-gray-900 dark:text-white">
                {{ s.name }}
                <span class="text-xs text-gray-500 dark:text-gray-400 font-mono ml-1">&#123;&#123;snippet:{{ s.name }}&#125;&#125;</span>
              </p>
              <p v-if="s.description" class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">{{ s.description }}</p>
              <p class="mt-1 text-sm text-gray-600 dark:text-gray-300 line-clamp-2 break-words">{{ s.content || '(vacío)' }}</p>
            </div>
            <div class="flex gap-1 shrink-0">
              <button type="button" @click="openForm(s)" class="p-2 rounded-lg text-gray-500 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700" title="Editar">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
              </button>
              <button type="button" @click="confirmDelete(s)" class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20" title="Eliminar">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- Slider Add/Edit Snippet -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="showModal"
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
          v-if="showModal"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-xl sm:max-w-2xl bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ editingId ? 'Editar snippet' : 'Nuevo snippet' }}</h2>
                <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Usa <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600 text-xs">&#123;&#123;snippet:Nombre&#125;&#125;</code> en los mensajes de un prompt para insertar el contenido.</p>
              </div>
              <button
                type="button"
                @click="showModal = false"
                class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
                aria-label="Cerrar"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          </div>
          <form @submit.prevent="submitForm" class="flex flex-col flex-1 min-h-0">
            <div class="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
                <input v-model.trim="form.name" type="text" required placeholder="ej: IntroEmpresa" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
                <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">Usa este nombre en el prompt: &#123;&#123;snippet:Nombre&#125;&#125;</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción (opcional)</label>
                <input v-model.trim="form.description" type="text" placeholder="Breve descripción" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Contenido <span class="text-red-500">*</span></label>
                <textarea v-model="form.content" rows="6" required placeholder="Texto que se insertará cuando uses {{snippet:Nombre}}" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm resize-y" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Orden (opcional)</label>
                <input v-model.number="form.displayOrder" type="number" min="0" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
              </div>
            </div>
            <div class="shrink-0 p-4 sm:p-6 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 bg-gray-50 dark:bg-gray-800/50">
              <button type="button" @click="showModal = false" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700">Cancelar</button>
              <button type="button" @click="submitForm" :disabled="saving" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white disabled:opacity-50">{{ saving ? 'Guardando…' : 'Guardar' }}</button>
            </div>
          </form>
        </div>
      </Transition>
    </Teleport>

    <!-- Delete confirmation modal (same idea as MCP Server / Templates) -->
    <Teleport to="body">
      <div
        v-if="deleteTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar snippet</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Eliminar el snippet <strong class="text-gray-900 dark:text-white">{{ deleteTarget?.name }}</strong>? Los prompts que usen <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600 text-sm">&#123;&#123;snippet:{{ deleteTarget?.name }}&#125;&#125;</code> dejarán de resolver este contenido. Esta acción no se puede deshacer.
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
const snippets = ref([])
const showModal = ref(false)
const editingId = ref(null)
const form = ref({ name: '', description: '', content: '', displayOrder: 0 })
const saving = ref(false)
const deleteTarget = ref(null)
const deleting = ref(false)

async function load() {
  loading.value = true
  error.value = null
  try {
    const res = await apiService.getMcpSnippets()
    snippets.value = res?.items ?? []
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar snippets'
  } finally {
    loading.value = false
  }
}

function openForm(snippet) {
  if (snippet) {
    editingId.value = snippet.id
    form.value = { name: snippet.name, description: snippet.description ?? '', content: snippet.content ?? '', displayOrder: snippet.displayOrder ?? 0 }
  } else {
    editingId.value = null
    form.value = { name: '', description: '', content: '', displayOrder: 0 }
  }
  showModal.value = true
}

async function submitForm() {
  if (!form.value.name?.trim()) return
  saving.value = true
  try {
    if (editingId.value) {
      await apiService.updateMcpSnippet(editingId.value, { name: form.value.name, description: form.value.description || null, content: form.value.content || '', displayOrder: form.value.displayOrder ?? 0 })
      toast.success('Snippet actualizado')
    } else {
      await apiService.addMcpSnippet({ name: form.value.name, description: form.value.description || null, content: form.value.content || '', displayOrder: form.value.displayOrder ?? 0 })
      toast.success('Snippet creado')
    }
    showModal.value = false
    await load()
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al guardar')
  } finally {
    saving.value = false
  }
}

function confirmDelete(s) {
  deleteTarget.value = s
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await apiService.deleteMcpSnippet(deleteTarget.value.id)
    toast.success('Snippet eliminado')
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

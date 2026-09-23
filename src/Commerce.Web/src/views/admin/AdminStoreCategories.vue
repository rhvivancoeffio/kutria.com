<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Categories</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Árbol de categorías Gravity de la tienda.
          </p>
        </div>
        <div class="flex flex-wrap gap-2 shrink-0">
          <button
            type="button"
            class="inline-flex items-center justify-center gap-2 px-3 py-2.5 min-h-[44px] rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
            :disabled="loading || !tree.length"
            @click="expandAll"
          >
            Expandir
          </button>
          <button
            type="button"
            class="inline-flex items-center justify-center gap-2 px-3 py-2.5 min-h-[44px] rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
            :disabled="loading || !tree.length"
            @click="collapseAll"
          >
            Colapsar
          </button>
          <button
            type="button"
            class="inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white transition-colors min-h-[44px] touch-manipulation"
            @click="openCreateRoot"
          >
            <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Nueva categoría
          </button>
        </div>
      </div>

      <div class="mb-4 flex flex-col sm:flex-row gap-3">
        <div class="relative flex-1 min-w-0">
          <input
            v-model="searchName"
            type="search"
            class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm px-3 py-2.5 pl-10"
            placeholder="Filtrar por nombre (Gravity)…"
            autocomplete="off"
            @keydown.enter.prevent="reload"
          />
          <svg
            class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        <button
          type="button"
          class="inline-flex items-center justify-center px-4 py-2.5 min-h-[44px] rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
          :disabled="loading"
          @click="reload"
        >
          Buscar
        </button>
      </div>

      <div v-if="loading" class="space-y-3">
        <div v-for="i in 6" :key="i" class="h-12 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <div
        v-else-if="error"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
        <button
          type="button"
          class="mt-3 text-sm font-medium text-red-700 dark:text-red-300 hover:underline"
          @click="reload"
        >
          Reintentar
        </button>
      </div>

      <div
        v-else-if="!tree.length"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay categorías</p>
        <p class="mt-1 text-sm text-gray-400 dark:text-gray-500">Crea la primera categoría en Gravity.</p>
        <button
          type="button"
          class="mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white"
          @click="openCreateRoot"
        >
          Nueva categoría
        </button>
      </div>

      <div
        v-else
        class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden"
      >
        <ul class="divide-y divide-gray-100 dark:divide-gray-700/80" role="tree">
          <AdminStoreCategoryTreeRows
            :nodes="tree"
            :depth="0"
            :expanded="expanded"
            :deleting-id="deletingId"
            @toggle="toggle"
            @add-child="openCreateChild"
            @edit="openEdit"
            @delete="confirmDelete"
          />
        </ul>
      </div>
    </main>

    <AdminStoreCategoryFormSlider
      v-model="sliderOpen"
      :category="editingCategory"
      :parent-options="parentOptions"
      @saved="onSaved"
    />

    <ConfirmModal
      :open="deleteConfirmOpen"
      title="Eliminar categoría"
      :message="deleteConfirmMessage"
      confirm-label="Eliminar"
      busy-label="Eliminando…"
      variant="danger"
      :busy="!!deletingId"
      @confirm="executeDelete"
      @cancel="cancelDelete"
    />
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import ConfirmModal from '../../components/ConfirmModal.vue'
import AdminStoreCategoryFormSlider from '../../components/store/AdminStoreCategoryFormSlider.vue'
import AdminStoreCategoryTreeRows from '../../components/store/AdminStoreCategoryTreeRows.vue'

const toast = useToast()
const tree = ref([])
const loading = ref(true)
const error = ref(null)
const searchName = ref('')
const sliderOpen = ref(false)
const editingCategory = ref(null)
const deletingId = ref(null)
const pendingDelete = ref(null)
const deleteConfirmOpen = ref(false)
const expanded = ref({})

const deleteConfirmMessage = computed(() => {
  const node = pendingDelete.value
  if (!node) return ''
  const label = node.name || node.categoryId
  const hasKids = node.children?.length > 0
  return hasKids
    ? `«${label}» tiene subcategorías. ¿Eliminar solo esta categoría en Gravity?`
    : `¿Eliminar la categoría «${label}»? Esta acción no se puede deshacer.`
})

function collectIds(nodes, acc = []) {
  for (const n of nodes || []) {
    if (n.categoryId) acc.push(n.categoryId)
    if (n.children?.length) collectIds(n.children, acc)
  }
  return acc
}

function flattenForParentSelect(nodes, depth = 0, acc = []) {
  for (const n of nodes || []) {
    const pad = depth ? `${'—'.repeat(depth)} ` : ''
    acc.push({
      categoryId: n.categoryId,
      label: `${pad}${n.name || n.categoryId}`
    })
    if (n.children?.length) flattenForParentSelect(n.children, depth + 1, acc)
  }
  return acc
}

const parentOptions = computed(() => flattenForParentSelect(tree.value))

function expandAll() {
  const next = {}
  for (const id of collectIds(tree.value)) next[id] = true
  expanded.value = next
}

function collapseAll() {
  expanded.value = {}
}

function toggle(id) {
  expanded.value = { ...expanded.value, [id]: !expanded.value[id] }
}

async function reload() {
  loading.value = true
  error.value = null
  try {
    tree.value = await apiService.listCatalogCategories({
      name: searchName.value.trim() || null
    })
    const next = { ...expanded.value }
    for (const n of tree.value) {
      if (n.children?.length && next[n.categoryId] === undefined) next[n.categoryId] = true
    }
    expanded.value = next
  } catch (err) {
    tree.value = []
    error.value = err.response?.data?.error || err.message || 'Error al cargar categorías'
  } finally {
    loading.value = false
  }
}

function openCreateRoot() {
  editingCategory.value = { parentCategoryId: '' }
  sliderOpen.value = true
}

function openCreateChild(node) {
  editingCategory.value = { parentCategoryId: node.categoryId, parentId: node.categoryId }
  sliderOpen.value = true
}

function openEdit(node) {
  editingCategory.value = {
    categoryId: node.categoryId,
    name: node.name,
    parentId: node.parentId,
    url: node.url
  }
  sliderOpen.value = true
}

function confirmDelete(node) {
  pendingDelete.value = node
  deleteConfirmOpen.value = true
}

function cancelDelete() {
  if (deletingId.value) return
  deleteConfirmOpen.value = false
  pendingDelete.value = null
}

async function executeDelete() {
  const node = pendingDelete.value
  if (!node) return
  deletingId.value = node.categoryId
  try {
    await apiService.deleteCatalogCategory(node.categoryId)
    toast.success('Categoría eliminada')
    deleteConfirmOpen.value = false
    pendingDelete.value = null
    await reload()
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'No se pudo eliminar')
  } finally {
    deletingId.value = null
  }
}

async function onSaved() {
  await reload()
}

onMounted(reload)
</script>

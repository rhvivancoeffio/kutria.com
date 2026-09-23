<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Atributos</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Atributos de entidad del catálogo Gravity de la tienda.
          </p>
        </div>
        <button
          type="button"
          class="inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white transition-colors min-h-[44px] touch-manipulation shrink-0"
          @click="openCreate"
        >
          <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Nuevo atributo
        </button>
      </div>

      <div class="mb-4 flex flex-col sm:flex-row gap-3">
        <div class="relative flex-1 min-w-0">
          <input
            v-model="searchName"
            type="search"
            class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm px-3 py-2.5 pl-10"
            placeholder="Buscar por nombre…"
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
        <div v-for="i in 5" :key="i" class="h-14 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
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
        v-else-if="!attributes.length"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay atributos</p>
        <p class="mt-1 text-sm text-gray-400 dark:text-gray-500">Crea el primer atributo en Gravity.</p>
        <button
          type="button"
          class="mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white"
          @click="openCreate"
        >
          Nuevo atributo
        </button>
      </div>

      <div v-else class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-700/50">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Nombre
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden md:table-cell">
                  Entidad
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">
                  Tipo
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Flags
                </th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Acciones
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="a in attributes"
                :key="a.entityAttributeId"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/40"
              >
                <td class="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                  <div>{{ a.label || a.name || '—' }}</div>
                  <div v-if="a.label && a.name && a.label !== a.name" class="text-xs text-gray-500 dark:text-gray-400 font-normal">
                    {{ a.name }}
                  </div>
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 hidden md:table-cell">
                  {{ a.entityName || '—' }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 hidden sm:table-cell">
                  {{ a.specificationTypeName || typeLabel(a.specificationType) }}
                </td>
                <td class="px-4 py-3">
                  <div class="flex flex-wrap gap-1">
                    <span
                      v-if="a.required"
                      class="inline-block px-2 py-0.5 text-xs font-medium rounded-full bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400"
                    >
                      Requerido
                    </span>
                    <span
                      v-if="a.isPublic"
                      class="inline-block px-2 py-0.5 text-xs font-medium rounded-full bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400"
                    >
                      Público
                    </span>
                    <span
                      v-if="a.isMultiOption"
                      class="inline-block px-2 py-0.5 text-xs font-medium rounded-full bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400"
                    >
                      Multi
                    </span>
                  </div>
                </td>
                <td class="px-4 py-3 text-right whitespace-nowrap">
                  <button
                    type="button"
                    class="p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 touch-manipulation"
                    title="Editar"
                    @click="openEdit(a)"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 touch-manipulation"
                    title="Eliminar"
                    :disabled="deletingId === a.entityAttributeId"
                    @click="confirmDelete(a)"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </main>

    <AdminStoreAttributeFormSlider
      v-model="sliderOpen"
      :attribute="editingAttribute"
      @saved="onSaved"
    />

    <ConfirmModal
      :open="deleteConfirmOpen"
      title="Eliminar atributo"
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
import AdminStoreAttributeFormSlider from '../../components/store/AdminStoreAttributeFormSlider.vue'
import { specificationTypeLabel } from '../../components/store/attributeSpecTypes'

const toast = useToast()
const attributes = ref([])
const loading = ref(true)
const error = ref(null)
const searchName = ref('')
const sliderOpen = ref(false)
const editingAttribute = ref(null)
const deletingId = ref(null)
const pendingDelete = ref(null)
const deleteConfirmOpen = ref(false)

const deleteConfirmMessage = computed(() => {
  const a = pendingDelete.value
  if (!a) return ''
  const label = a.label || a.name || a.entityAttributeId
  return `¿Eliminar el atributo «${label}»? Esta acción no se puede deshacer.`
})

function typeLabel(type) {
  return specificationTypeLabel(type) || (type ? `Tipo ${type}` : '—')
}

async function reload() {
  loading.value = true
  error.value = null
  try {
    attributes.value = await apiService.listCatalogAttributes({
      name: searchName.value.trim() || null,
      page: 1,
      pageSize: 50
    })
  } catch (err) {
    attributes.value = []
    error.value = err.response?.data?.error || err.message || 'Error al cargar atributos'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingAttribute.value = null
  sliderOpen.value = true
}

async function openEdit(row) {
  try {
    const detail = await apiService.getCatalogAttribute(row.entityAttributeId)
    editingAttribute.value = detail || { ...row }
  } catch {
    editingAttribute.value = { ...row }
  }
  sliderOpen.value = true
}

function confirmDelete(attr) {
  pendingDelete.value = attr
  deleteConfirmOpen.value = true
}

function cancelDelete() {
  if (deletingId.value) return
  deleteConfirmOpen.value = false
  pendingDelete.value = null
}

async function executeDelete() {
  const attr = pendingDelete.value
  if (!attr) return
  deletingId.value = attr.entityAttributeId
  try {
    await apiService.deleteCatalogAttribute(attr.entityAttributeId)
    toast.success('Atributo eliminado')
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

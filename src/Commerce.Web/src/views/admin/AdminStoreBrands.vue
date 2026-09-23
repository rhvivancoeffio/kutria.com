<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Marcas</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Marcas del catálogo Gravity de la tienda.
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
          Nueva marca
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
        v-else-if="!brands.length"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay marcas</p>
        <p class="mt-1 text-sm text-gray-400 dark:text-gray-500">Crea la primera marca en Gravity.</p>
        <button
          type="button"
          class="mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white"
          @click="openCreate"
        >
          Nueva marca
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
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">
                  ID
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Estado
                </th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                  Acciones
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="b in brands"
                :key="b.brandId"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/40"
              >
                <td class="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                  {{ b.name || '—' }}
                </td>
                <td class="px-4 py-3 text-xs font-mono text-gray-500 dark:text-gray-400 hidden sm:table-cell truncate max-w-[12rem]">
                  {{ b.brandId }}
                </td>
                <td class="px-4 py-3">
                  <span
                    :class="[
                      'inline-block px-2 py-0.5 text-xs font-medium rounded-full',
                      b.isActive
                        ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                        : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                    ]"
                  >
                    {{ b.isActive ? 'Activa' : 'Inactiva' }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right whitespace-nowrap">
                  <button
                    type="button"
                    class="p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 touch-manipulation"
                    title="Editar"
                    @click="openEdit(b)"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                    </svg>
                  </button>
                  <button
                    type="button"
                    class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 touch-manipulation"
                    title="Eliminar"
                    :disabled="deletingId === b.brandId"
                    @click="confirmDelete(b)"
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

    <AdminStoreBrandFormSlider
      v-model="sliderOpen"
      :brand="editingBrand"
      @saved="onSaved"
    />

    <ConfirmModal
      :open="deleteConfirmOpen"
      title="Eliminar marca"
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
import AdminStoreBrandFormSlider from '../../components/store/AdminStoreBrandFormSlider.vue'

const toast = useToast()
const brands = ref([])
const loading = ref(true)
const error = ref(null)
const searchName = ref('')
const sliderOpen = ref(false)
const editingBrand = ref(null)
const deletingId = ref(null)
const pendingDelete = ref(null)
const deleteConfirmOpen = ref(false)

const deleteConfirmMessage = computed(() => {
  const b = pendingDelete.value
  if (!b) return ''
  const label = b.name || b.brandId
  return `¿Eliminar la marca «${label}»? Esta acción no se puede deshacer.`
})

async function reload() {
  loading.value = true
  error.value = null
  try {
    brands.value = await apiService.listCatalogBrands({
      name: searchName.value.trim() || null,
      page: 1,
      pageSize: 50
    })
  } catch (err) {
    brands.value = []
    error.value = err.response?.data?.error || err.message || 'Error al cargar marcas'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingBrand.value = null
  sliderOpen.value = true
}

function openEdit(brand) {
  editingBrand.value = { ...brand }
  sliderOpen.value = true
}

function confirmDelete(brand) {
  pendingDelete.value = brand
  deleteConfirmOpen.value = true
}

function cancelDelete() {
  if (deletingId.value) return
  deleteConfirmOpen.value = false
  pendingDelete.value = null
}

async function executeDelete() {
  const brand = pendingDelete.value
  if (!brand) return
  deletingId.value = brand.brandId
  try {
    await apiService.deleteCatalogBrand(brand.brandId)
    toast.success('Marca eliminada')
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

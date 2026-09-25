<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Productos</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Catálogo de productos Gravity de la tienda.
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
          Nuevo producto
        </button>
      </div>

      <div v-if="loading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
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
        v-else-if="!products.length"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay productos</p>
        <p class="mt-1 text-sm text-gray-400 dark:text-gray-500">Crea el primero en Gravity.</p>
        <button
          type="button"
          class="mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white"
          @click="openCreate"
        >
          Nuevo producto
        </button>
      </div>

      <template v-else>
        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-700/50">
                <tr>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Producto
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden md:table-cell">
                    Marca
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">
                    Precio
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
                  v-for="p in products"
                  :key="p.productId"
                  class="hover:bg-gray-50 dark:hover:bg-gray-700/40"
                >
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-3 min-w-0">
                      <img
                        v-if="p.imageUrl"
                        :src="p.imageUrl"
                        alt=""
                        class="w-10 h-10 rounded-lg object-cover shrink-0 bg-gray-100 dark:bg-gray-700"
                      />
                      <div
                        v-else
                        class="w-10 h-10 rounded-lg shrink-0 bg-gray-100 dark:bg-gray-700"
                      />
                      <div class="min-w-0">
                        <p class="text-sm font-medium text-gray-900 dark:text-white truncate">
                          {{ p.name || '—' }}
                        </p>
                        <p class="text-xs font-mono text-gray-400 truncate">{{ p.productId }}</p>
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 hidden md:table-cell">
                    {{ p.brandName || '—' }}
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 hidden lg:table-cell">
                    {{ formatPrice(p) }}
                  </td>
                  <td class="px-4 py-3">
                    <span
                      class="inline-block px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300"
                    >
                      {{ p.productStatusName || '—' }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-right whitespace-nowrap">
                    <button
                      type="button"
                      class="p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 touch-manipulation"
                      title="Editar"
                      @click="openEdit(p)"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 touch-manipulation"
                      title="Eliminar"
                      :disabled="deletingId === p.productId"
                      @click="confirmDelete(p)"
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

        <div
          v-if="pageCount > 1"
          class="mt-4 flex items-center justify-between gap-3"
        >
          <p class="text-sm text-gray-500 dark:text-gray-400">
            Página {{ page }} de {{ pageCount }} · {{ rowCount }} productos
          </p>
          <div class="flex gap-2">
            <button
              type="button"
              class="px-3 py-2 min-h-[40px] rounded-lg border border-gray-300 dark:border-gray-600 text-sm disabled:opacity-40"
              :disabled="page <= 1 || loading"
              @click="goPage(page - 1)"
            >
              Anterior
            </button>
            <button
              type="button"
              class="px-3 py-2 min-h-[40px] rounded-lg border border-gray-300 dark:border-gray-600 text-sm disabled:opacity-40"
              :disabled="page >= pageCount || loading"
              @click="goPage(page + 1)"
            >
              Siguiente
            </button>
          </div>
        </div>
      </template>
    </main>

    <ConfirmModal
      :open="deleteConfirmOpen"
      title="Eliminar producto"
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
import { useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import ConfirmModal from '../../components/ConfirmModal.vue'

const router = useRouter()
const toast = useToast()
const products = ref([])
const loading = ref(true)
const error = ref(null)
const page = ref(1)
const pageSize = 20
const pageCount = ref(1)
const rowCount = ref(0)
const deletingId = ref(null)
const pendingDelete = ref(null)
const deleteConfirmOpen = ref(false)


const confirmImproveWithAI = async () => {
  if (!form.name.trim()) {
    alert('Pon un nombre primero')
    return
  }
  const ok = window.confirm(`¿Mejorar "${form.name.trim()}" con IA? Esto consume créditos.`)
  if (!ok) return
  // Llama a tu función original que sí existe
  await startContentByName()
}

const confirmGenerateWithAI = async () => {
  if (!primaryImageUrl.value && !imageFile.value) {
    alert('Sube una foto primero')
    return
  }
  const ok = window.confirm('¿Generar ficha desde foto con IA? Esto consume créditos.')
  if (!ok) return
  await startContentByImage()
}

const deleteConfirmMessage = computed(() => {
  const p = pendingDelete.value
  if (!p) return ''
  const label = p.name || p.productId
  return `¿Eliminar el producto «${label}»? Esta acción no se puede deshacer.`
})

function formatPrice(p) {
  const price = p.specialPrice ?? p.basePrice
  if (price == null) return '—'
  const sym = p.currencySymbol || ''
  return `${sym}${Number(price).toLocaleString()}`
}

async function reload() {
  loading.value = true
  error.value = null
  try {
    const data = await apiService.listCatalogProducts({
      page: page.value,
      pageSize
    })
    products.value = data.items
    pageCount.value = Math.max(1, data.pageCount)
    rowCount.value = data.rowCount
    page.value = data.currentPage || page.value
  } catch (err) {
    products.value = []
    error.value = err.response?.data?.error || err.message || 'Error al cargar productos'
  } finally {
    loading.value = false
  }
}

function goPage(n) {
  page.value = n
  reload()
}

function openCreate() {
  router.push({ name: 'AdminStoreProductCreate' })
}

function openEdit(product) {
  if (!product?.productId) return
  router.push({
    name: 'AdminStoreProductEdit',
    params: { productId: product.productId }
  })
}

function confirmDelete(product) {
  pendingDelete.value = product
  deleteConfirmOpen.value = true
}

function cancelDelete() {
  if (deletingId.value) return
  deleteConfirmOpen.value = false
  pendingDelete.value = null
}

async function executeDelete() {
  const product = pendingDelete.value
  if (!product) return
  deletingId.value = product.productId
  try {
    await apiService.deleteCatalogProduct(product.productId)
    toast.success('Producto eliminado')
    deleteConfirmOpen.value = false
    pendingDelete.value = null
    await reload()
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'No se pudo eliminar')
  } finally {
    deletingId.value = null
  }
}

onMounted(reload)
</script>

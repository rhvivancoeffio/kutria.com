<template>
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
        v-if="modelValue"
        class="fixed inset-0 z-50 bg-black/50"
        aria-hidden="true"
        @click="!saving && close()"
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
        v-if="modelValue"
        class="fixed top-0 right-0 z-[51] h-full min-h-0 w-full max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="headingId"
        @click.stop
      >
        <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <h2
                :id="headingId"
                class="text-lg font-semibold text-gray-900 dark:text-white truncate"
              >
                Editar producto
              </h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                Se guarda en Gravity (tienda nativa del tenant).
              </p>
            </div>
            <button
              type="button"
              class="p-2 -m-2 shrink-0 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300"
              aria-label="Cerrar"
              :disabled="saving"
              @click="close"
            >
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <form class="flex flex-col flex-1 min-h-0" @submit.prevent="submit">
          <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre</label>
              <input
                v-model="form.name"
                type="text"
                required
                maxlength="500"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                placeholder="Nombre del producto"
                autocomplete="off"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción</label>
              <textarea
                v-model="form.description"
                rows="4"
                required
                maxlength="8000"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                placeholder="Descripción"
              />
            </div>

            <!-- Brand autocomplete -->
            <div class="relative" @keydown.escape="brandOpen = false">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Marca <span class="text-gray-400 font-normal">(opcional)</span>
              </label>
              <div
                v-if="form.brandId"
                class="flex items-center gap-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900/60 px-3 py-2"
              >
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium text-gray-900 dark:text-white truncate">
                    {{ form.brandName || '—' }}
                  </p>
                  <p class="text-xs font-mono text-gray-400 truncate">{{ form.brandId }}</p>
                </div>
                <button
                  type="button"
                  class="p-1.5 rounded-lg text-gray-500 hover:bg-gray-200 dark:hover:bg-gray-700"
                  aria-label="Quitar marca"
                  @click="clearBrand"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              <template v-else>
                <input
                  v-model="brandQuery"
                  type="search"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  placeholder="Buscar marca…"
                  autocomplete="off"
                  @focus="brandOpen = true"
                  @input="onBrandInput"
                />
                <div
                  v-if="brandOpen && (brandLoading || brandResults.length || brandQuery.trim())"
                  class="absolute z-20 mt-1 w-full max-h-48 overflow-y-auto rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 shadow-lg"
                >
                  <p v-if="brandLoading" class="px-3 py-2 text-xs text-gray-500">Buscando…</p>
                  <p
                    v-else-if="brandQuery.trim().length >= 1 && !brandResults.length"
                    class="px-3 py-2 text-xs text-gray-500"
                  >
                    Sin resultados
                  </p>
                  <button
                    v-for="b in brandResults"
                    :key="b.brandId"
                    type="button"
                    class="w-full text-left px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-700/60 border-b border-gray-100 dark:border-gray-700 last:border-0"
                    @click="selectBrand(b)"
                  >
                    <span class="block text-sm text-gray-900 dark:text-white">{{ b.name || '—' }}</span>
                    <span class="block text-xs font-mono text-gray-400">{{ b.brandId }}</span>
                  </button>
                </div>
              </template>
            </div>

            <!-- Category tree -->
            <div>
              <div class="flex items-center justify-between gap-2 mb-1">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Categoría <span class="text-gray-400 font-normal">(opcional)</span>
                </label>
                <button
                  v-if="!categoryPickerOpen"
                  type="button"
                  class="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline"
                  @click="openCategoryPicker"
                >
                  {{ form.categoryId ? 'Cambiar' : 'Elegir del árbol' }}
                </button>
              </div>
              <div
                v-if="form.categoryId"
                class="flex items-center gap-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900/60 px-3 py-2 mb-2"
              >
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-medium text-gray-900 dark:text-white truncate">
                    {{ form.categoryPath || '—' }}
                  </p>
                  <p class="text-xs font-mono text-gray-400 truncate">{{ form.categoryId }}</p>
                </div>
                <button
                  type="button"
                  class="p-1.5 rounded-lg text-gray-500 hover:bg-gray-200 dark:hover:bg-gray-700"
                  aria-label="Quitar categoría"
                  @click="clearCategory"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
              <div
                v-if="categoryPickerOpen"
                class="rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 overflow-hidden"
              >
                <div class="flex items-center justify-between px-3 py-2 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/80">
                  <span class="text-xs font-medium text-gray-600 dark:text-gray-300">Árbol de categorías</span>
                  <button
                    type="button"
                    class="text-xs text-gray-500 hover:text-gray-800 dark:hover:text-gray-200"
                    @click="categoryPickerOpen = false"
                  >
                    Cerrar
                  </button>
                </div>
                <p v-if="categoryLoading" class="px-3 py-4 text-xs text-gray-500">Cargando…</p>
                <p v-else-if="categoryError" class="px-3 py-4 text-xs text-red-600 dark:text-red-400">
                  {{ categoryError }}
                </p>
                <p
                  v-else-if="!categoryTree.length"
                  class="px-3 py-4 text-xs text-gray-500"
                >
                  No hay categorías
                </p>
                <ul
                  v-else
                  class="max-h-56 overflow-y-auto divide-y divide-gray-100 dark:divide-gray-700/80"
                  role="tree"
                >
                  <ProductCategoryTreePick
                    :nodes="categoryTree"
                    :depth="0"
                    :expanded="categoryExpanded"
                    :selected-id="form.categoryId"
                    @toggle="toggleCategory"
                    @select="selectCategory"
                  />
                </ul>
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                URL de imagen <span class="text-gray-400 font-normal">(opcional)</span>
              </label>
              <input
                v-model="form.imageUrl"
                type="url"
                maxlength="2000"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                placeholder="https://…"
                autocomplete="off"
              />
            </div>
            <div class="space-y-3">
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-gray-700 dark:text-gray-300">Activo</span>
                <FormToggle v-model="form.isActive" aria-label="Activo" />
              </div>
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm text-gray-700 dark:text-gray-300">Mostrar en catálogo</span>
                <FormToggle v-model="form.showInCatalog" aria-label="Mostrar en catálogo" />
              </div>
            </div>
            <p v-if="formError" class="text-sm text-red-600 dark:text-red-400">{{ formError }}</p>
          </div>

          <div
            class="shrink-0 px-4 sm:px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-2"
          >
            <button
              type="button"
              class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
              :disabled="saving"
              @click="close"
            >
              Cancelar
            </button>
            <button
              type="submit"
              class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="saving || !form.name.trim() || !form.description.trim() || !isEdit"
            >
              {{ saving ? 'Guardando…' : 'Guardar' }}
            </button>
          </div>
        </form>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import FormToggle from '../FormToggle.vue'
import ProductCategoryTreePick from './ProductCategoryTreePick.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  product: { type: Object, default: null }
})

const emit = defineEmits(['update:modelValue', 'saved'])

const toast = useToast()
const saving = ref(false)
const formError = ref(null)
const form = reactive({
  name: '',
  description: '',
  brandId: '',
  brandName: '',
  categoryId: '',
  categoryPath: '',
  imageUrl: '',
  isActive: true,
  showInCatalog: true
})

const brandQuery = ref('')
const brandOpen = ref(false)
const brandLoading = ref(false)
const brandResults = ref([])
let brandTimer = null

const categoryPickerOpen = ref(false)
const categoryTree = ref([])
const categoryLoading = ref(false)
const categoryError = ref(null)
const categoryExpanded = ref({})

const isEdit = computed(() => !!(props.product && props.product.productId))
const headingId = 'admin-store-product-form-heading'

function resetForm() {
  form.name = props.product?.name ?? ''
  form.description = props.product?.description ?? ''
  form.brandId = props.product?.brandId ?? ''
  form.brandName = props.product?.brandName ?? ''
  form.categoryId = props.product?.categoryId ?? ''
  form.categoryPath = props.product?.categoryPath ?? ''
  form.imageUrl = props.product?.imageUrl ?? ''
  form.isActive = props.product?.isActive !== false
  form.showInCatalog = props.product?.showInCatalog !== false
  formError.value = null
  brandQuery.value = ''
  brandResults.value = []
  brandOpen.value = false
  categoryPickerOpen.value = false
}

watch(
  () => [props.modelValue, props.product],
  ([open]) => {
    if (open) resetForm()
  }
)

function onBrandInput() {
  brandOpen.value = true
  if (brandTimer) clearTimeout(brandTimer)
  const q = brandQuery.value.trim()
  if (q.length < 1) {
    brandResults.value = []
    return
  }
  brandTimer = setTimeout(async () => {
    brandLoading.value = true
    try {
      brandResults.value = await apiService.autocompleteCatalogBrands(q)
    } catch {
      brandResults.value = []
    } finally {
      brandLoading.value = false
    }
  }, 280)
}

function selectBrand(b) {
  form.brandId = b.brandId
  form.brandName = b.name || ''
  brandQuery.value = ''
  brandResults.value = []
  brandOpen.value = false
}

function clearBrand() {
  form.brandId = ''
  form.brandName = ''
}

function collectExpandIds(nodes, acc = {}) {
  for (const n of nodes || []) {
    if (n.children?.length) {
      acc[n.categoryId] = true
      collectExpandIds(n.children, acc)
    }
  }
  return acc
}

async function openCategoryPicker() {
  categoryPickerOpen.value = true
  if (categoryTree.value.length) return
  categoryLoading.value = true
  categoryError.value = null
  try {
    categoryTree.value = await apiService.listCatalogCategories()
    categoryExpanded.value = collectExpandIds(categoryTree.value, {})
  } catch (err) {
    categoryError.value = err.response?.data?.error || err.message || 'Error al cargar categorías'
    categoryTree.value = []
  } finally {
    categoryLoading.value = false
  }
}

function toggleCategory(id) {
  categoryExpanded.value = { ...categoryExpanded.value, [id]: !categoryExpanded.value[id] }
}

function findPath(nodes, targetId, trail = []) {
  for (const n of nodes || []) {
    const next = [...trail, n.name || n.categoryId]
    if (n.categoryId === targetId) return { node: n, path: next.join(' > ') }
    const hit = findPath(n.children, targetId, next)
    if (hit) return hit
  }
  return null
}

function selectCategory(node) {
  const hit = findPath(categoryTree.value, node.categoryId)
  form.categoryId = node.categoryId
  form.categoryPath = hit?.path || node.name || node.categoryId
  categoryPickerOpen.value = false
}

function clearCategory() {
  form.categoryId = ''
  form.categoryPath = ''
}

function close() {
  if (saving.value) return
  emit('update:modelValue', false)
}

async function submit() {
  const name = form.name.trim()
  const description = form.description.trim()
  if (!name || !description) return
  saving.value = true
  formError.value = null
  const payload = {
    name,
    description,
    brandId: form.brandId.trim() || null,
    brandName: form.brandName.trim() || null,
    categoryId: form.categoryId.trim() || null,
    categoryPath: form.categoryPath.trim() || null,
    imageUrl: form.imageUrl.trim() || null,
    isActive: form.isActive,
    showInCatalog: form.showInCatalog
  }
  try {
    if (!isEdit.value) {
      throw new Error('La creación de productos usa la vista dedicada.')
    }
    const result = await apiService.updateCatalogProduct(props.product.productId, payload)
    toast.success('Producto actualizado')
    emit('saved', result)
    emit('update:modelValue', false)
  } catch (err) {
    const msg = err.response?.data?.error || err.message || 'No se pudo guardar el producto'
    formError.value = msg
    toast.error(msg)
  } finally {
    saving.value = false
  }
}
</script>

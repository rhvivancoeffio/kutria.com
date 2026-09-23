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
                {{ isEdit ? 'Editar categoría' : 'Nueva categoría' }}
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
                maxlength="200"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                placeholder="Nombre de la categoría"
                autocomplete="off"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Slug <span class="text-gray-400 font-normal">(opcional)</span>
              </label>
              <input
                v-model="form.slug"
                type="text"
                maxlength="200"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2 font-mono"
                placeholder="auto desde el nombre"
                autocomplete="off"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Descripción <span class="text-gray-400 font-normal">(opcional)</span>
              </label>
              <textarea
                v-model="form.description"
                rows="3"
                maxlength="2000"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                placeholder="Descripción breve"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Categoría padre
              </label>
              <select
                v-model="form.parentCategoryId"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
              >
                <option value="">(raíz)</option>
                <option
                  v-for="opt in parentOptions"
                  :key="opt.categoryId"
                  :value="opt.categoryId"
                  :disabled="opt.categoryId === excludeCategoryId"
                >
                  {{ opt.label }}
                </option>
              </select>
            </div>
            <label class="inline-flex items-center gap-2 cursor-pointer select-none">
              <input
                v-model="form.isActive"
                type="checkbox"
                class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
              />
              <span class="text-sm text-gray-700 dark:text-gray-300">Activa</span>
            </label>
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
              :disabled="saving || !form.name.trim()"
            >
              {{ saving ? 'Guardando…' : isEdit ? 'Guardar' : 'Crear' }}
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

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  /** null = create root; object with categoryId = edit; { parentCategoryId } = create child */
  category: { type: Object, default: null },
  /** Flat options for parent select: { categoryId, label } */
  parentOptions: { type: Array, default: () => [] }
})

const emit = defineEmits(['update:modelValue', 'saved'])

const toast = useToast()
const saving = ref(false)
const formError = ref(null)
const form = reactive({
  name: '',
  slug: '',
  description: '',
  parentCategoryId: '',
  isActive: true
})

const isEdit = computed(() => !!(props.category && props.category.categoryId))
const excludeCategoryId = computed(() => (isEdit.value ? props.category.categoryId : ''))
const headingId = 'admin-store-category-form-heading'

function resetForm() {
  form.name = props.category?.name ?? ''
  form.slug = props.category?.slug ?? ''
  form.description = props.category?.description ?? ''
  form.parentCategoryId = props.category?.parentId ?? props.category?.parentCategoryId ?? ''
  form.isActive = props.category?.isActive !== false
  formError.value = null
}

watch(
  () => [props.modelValue, props.category],
  ([open]) => {
    if (open) resetForm()
  }
)

function close() {
  if (saving.value) return
  emit('update:modelValue', false)
}

async function submit() {
  const name = form.name.trim()
  if (!name) return
  saving.value = true
  formError.value = null
  const payload = {
    name,
    slug: form.slug.trim() || null,
    description: form.description.trim() || null,
    parentCategoryId: form.parentCategoryId.trim() || null,
    isActive: form.isActive
  }
  try {
    let result
    if (isEdit.value) {
      result = await apiService.updateCatalogCategory(props.category.categoryId, payload)
      toast.success('Categoría actualizada')
    } else {
      result = await apiService.createCatalogCategory(payload)
      toast.success('Categoría creada')
    }
    emit('saved', result)
    emit('update:modelValue', false)
  } catch (err) {
    const msg = err.response?.data?.error || err.message || 'No se pudo guardar la categoría'
    formError.value = msg
    toast.error(msg)
  } finally {
    saving.value = false
  }
}
</script>

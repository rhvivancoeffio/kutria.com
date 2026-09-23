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
        @click="!busy && close()"
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
        aria-labelledby="product-onboarding-heading"
        @click.stop
      >
        <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <h2
                id="product-onboarding-heading"
                class="text-lg font-semibold text-gray-900 dark:text-white"
              >
                Nuevo producto
              </h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                Sube o toma una foto, o escribe el nombre. Al menos uno es obligatorio.
              </p>
            </div>
            <button
              type="button"
              class="p-2 -m-2 shrink-0 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300"
              aria-label="Cerrar"
              :disabled="busy"
              @click="close"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-6 space-y-5">
          <div v-if="step === 'input'" class="space-y-4">
            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
                Nombre del producto
              </label>
              <input
                v-model="title"
                type="text"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                placeholder="Ej. iPhone 15 Pro Max"
                autocomplete="off"
                :disabled="busy"
              />
            </div>

            <div>
              <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
                Foto
              </label>

              <div
                v-if="previewUrl"
                class="relative rounded-xl overflow-hidden border border-gray-200 dark:border-gray-600 bg-gray-100 dark:bg-gray-900 shadow-sm"
              >
                <div class="relative aspect-[4/3] bg-gray-100 dark:bg-gray-900">
                  <img
                    :src="previewUrl"
                    alt="Vista previa del producto"
                    class="absolute inset-0 w-full h-full object-contain p-4"
                  />
                  <div class="pointer-events-none absolute inset-x-0 bottom-0 h-20 bg-gradient-to-t from-black/55 to-transparent" />
                  <div class="absolute inset-x-0 bottom-0 p-3 flex items-end justify-between gap-2">
                    <p class="min-w-0 text-xs sm:text-sm font-medium text-white truncate drop-shadow">
                      {{ imageFile?.name || 'Imagen seleccionada' }}
                    </p>
                    <div class="shrink-0 flex items-center gap-2">
                      <button
                        type="button"
                        class="inline-flex items-center min-h-[36px] px-2.5 py-1.5 rounded-lg bg-white/95 dark:bg-gray-900/95 text-xs font-medium text-gray-700 dark:text-gray-200 border border-gray-200/80 dark:border-gray-600 shadow-sm hover:bg-white dark:hover:bg-gray-800 disabled:opacity-50"
                        :disabled="busy"
                        @click="!busy && fileInput?.click()"
                      >
                        Cambiar
                      </button>
                      <button
                        type="button"
                        class="inline-flex items-center min-h-[36px] px-2.5 py-1.5 rounded-lg bg-white/95 dark:bg-gray-900/95 text-xs font-medium text-red-600 dark:text-red-400 border border-gray-200/80 dark:border-gray-600 shadow-sm hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-50"
                        :disabled="busy"
                        @click="clearImage"
                      >
                        Quitar imagen
                      </button>
                    </div>
                  </div>
                </div>
                <input
                  ref="fileInput"
                  type="file"
                  accept="image/*"
                  class="hidden"
                  :disabled="busy"
                  @change="onFileChange"
                />
              </div>

              <div
                v-else
                :class="[
                  'rounded-xl border-2 transition-colors flex flex-col items-center justify-center min-h-[160px] p-6',
                  busy ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer',
                  dragging
                    ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20'
                    : 'border-gray-300 dark:border-gray-600 hover:border-primary-400 hover:bg-gray-50 dark:hover:bg-gray-700/50'
                ]"
                @dragover.prevent="!busy && (dragging = true)"
                @dragleave.prevent="dragging = false"
                @drop.prevent="onDrop"
                @click="!busy && fileInput?.click()"
              >
                <input
                  ref="fileInput"
                  type="file"
                  accept="image/*"
                  class="hidden"
                  :disabled="busy"
                  @change="onFileChange"
                />
                <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
                <p class="text-sm text-gray-600 dark:text-gray-400 text-center">
                  Arrastra una imagen aquí o <span class="text-primary-600 dark:text-primary-400">selecciona</span>
                </p>
                <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">JPG, PNG o WEBP</p>
              </div>

              <label
                class="mt-2 inline-flex items-center gap-1.5 text-sm text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 cursor-pointer"
              >
                <input
                  type="file"
                  accept="image/*"
                  capture="environment"
                  class="hidden"
                  :disabled="busy"
                  @change="onFileChange"
                />
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 01-2 2v9a2 2 0 01-2-2H5a2 2 0 01-2-2V9z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                Tomar foto
              </label>
            </div>

            <p v-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
          </div>

          <div v-else-if="step === 'approval'" class="space-y-4">
            <p class="text-sm text-gray-600 dark:text-gray-300">
              Revisa el borrador y aprueba qué crear en Gravity.
            </p>

            <div v-if="draft?.product?.image_url" class="rounded-lg overflow-hidden border border-gray-200 dark:border-gray-600">
              <img :src="draft.product.image_url" alt="" class="max-h-40 w-full object-contain bg-gray-50 dark:bg-gray-900" />
            </div>

            <label class="flex items-start gap-3 p-3 rounded-lg border border-gray-200 dark:border-gray-600">
              <input v-model="approvals.brand" type="checkbox" class="mt-1" :disabled="!draft?.brand?.is_new || busy" />
              <span class="min-w-0">
                <span class="block text-sm font-medium text-gray-900 dark:text-white">Marca</span>
                <span class="block text-sm text-gray-500 dark:text-gray-400">
                  {{ draft?.brand?.name || '—' }}
                  <span v-if="draft?.brand?.is_new" class="text-amber-600 dark:text-amber-400">(nueva)</span>
                  <span v-else class="text-emerald-600 dark:text-emerald-400">(existente)</span>
                </span>
              </span>
            </label>

            <label class="flex items-start gap-3 p-3 rounded-lg border border-gray-200 dark:border-gray-600">
              <input v-model="approvals.category" type="checkbox" class="mt-1" :disabled="!draft?.category?.is_new || busy" />
              <span class="min-w-0">
                <span class="block text-sm font-medium text-gray-900 dark:text-white">Categoría</span>
                <span class="block text-sm text-gray-500 dark:text-gray-400">
                  {{ draft?.category?.name || '—' }}
                  <span v-if="draft?.category?.is_new" class="text-amber-600 dark:text-amber-400">(nueva)</span>
                  <span v-else class="text-emerald-600 dark:text-emerald-400">(existente)</span>
                </span>
              </span>
            </label>

            <label class="flex items-start gap-3 p-3 rounded-lg border border-gray-200 dark:border-gray-600">
              <input v-model="approvals.product" type="checkbox" class="mt-1" :disabled="busy" />
              <span class="min-w-0">
                <span class="block text-sm font-medium text-gray-900 dark:text-white">Producto</span>
                <span class="block text-sm text-gray-500 dark:text-gray-400">{{ draft?.product?.title || '—' }}</span>
              </span>
            </label>

            <p v-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
          </div>

          <div v-else-if="step === 'done'" class="space-y-3">
            <p class="text-sm text-emerald-700 dark:text-emerald-300">
              Producto creado en Gravity.
            </p>
            <dl class="text-sm space-y-1 text-gray-700 dark:text-gray-300">
              <div><dt class="inline font-medium">Product ID:</dt> <dd class="inline font-mono">{{ created?.product_id }}</dd></div>
              <div v-if="created?.brand_id"><dt class="inline font-medium">Brand ID:</dt> <dd class="inline font-mono">{{ created.brand_id }}</dd></div>
              <div v-if="created?.category_id"><dt class="inline font-medium">Category ID:</dt> <dd class="inline font-mono">{{ created.category_id }}</dd></div>
            </dl>
          </div>
        </div>

        <div class="shrink-0 px-4 sm:px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-2">
          <button
            type="button"
            class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
            :disabled="busy"
            @click="close"
          >
            {{ step === 'done' ? 'Cerrar' : 'Cancelar' }}
          </button>
          <button
            v-if="step === 'input'"
            type="button"
            class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="busy || !canAnalyze"
            @click="startAnalyze"
          >
            {{ busy ? 'Analizando…' : 'Analizar' }}
          </button>
          <button
            v-else-if="step === 'approval'"
            type="button"
            class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="busy || !approvals.product || (draft?.brand?.is_new && !approvals.brand) || (draft?.category?.is_new && !approvals.category)"
            @click="approve"
          >
            {{ busy ? 'Creando…' : 'Crear aprobado' }}
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  integrationId: { type: String, default: '' }
})

const emit = defineEmits(['update:modelValue', 'created'])

const toast = useToast()
const step = ref('input')
const title = ref('')
const imageFile = ref(null)
const previewUrl = ref('')
const fileInput = ref(null)
const dragging = ref(false)
const busy = ref(false)
const error = ref('')
const workflowId = ref('')
const tenantId = ref('')
const draft = ref(null)
const created = ref(null)
const approvals = ref({ brand: true, category: true, product: true })

const canAnalyze = computed(() => !!title.value.trim() || !!imageFile.value)

watch(
  () => props.modelValue,
  (open) => {
    if (open) reset()
  }
)

function reset() {
  step.value = 'input'
  title.value = ''
  clearImage()
  dragging.value = false
  busy.value = false
  error.value = ''
  workflowId.value = ''
  tenantId.value = ''
  draft.value = null
  created.value = null
  approvals.value = { brand: true, category: true, product: true }
}

function close() {
  if (busy.value) return
  emit('update:modelValue', false)
}

function setImageFile(file) {
  if (!file || !file.type?.startsWith('image/')) {
    error.value = 'Selecciona un archivo de imagen válido'
    return
  }
  imageFile.value = file
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = URL.createObjectURL(file)
  error.value = ''
  dragging.value = false
}

function onFileChange(event) {
  const file = event.target?.files?.[0]
  if (!file) return
  setImageFile(file)
  event.target.value = ''
}

function onDrop(event) {
  dragging.value = false
  if (busy.value) return
  const file = event.dataTransfer?.files?.[0]
  if (file) setImageFile(file)
}

function clearImage() {
  imageFile.value = null
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = ''
  dragging.value = false
}

async function startAnalyze() {
  if (!canAnalyze.value || busy.value) return
  error.value = ''
  busy.value = true
  try {
    const event = await apiService.startProductOnboardingStream({
      title: title.value.trim() || null,
      imageFile: imageFile.value,
      integrationId: props.integrationId || null
    })
    if (event?.type === 'error') {
      error.value = event.text || 'No se pudo analizar'
      return
    }
    if (event?.type !== 'approval_needed') {
      error.value = 'Respuesta inesperada del onboarding'
      return
    }
    workflowId.value = event.workflow_id
    tenantId.value = event.tenant_id
    draft.value = event.draft
    approvals.value = {
      brand: !!event.draft?.brand?.is_new,
      category: !!event.draft?.category?.is_new,
      product: true
    }
    step.value = 'approval'
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al analizar'
  } finally {
    busy.value = false
  }
}

async function approve() {
  if (!workflowId.value || busy.value) return
  error.value = ''
  busy.value = true
  try {
    const result = await apiService.approveProductOnboarding(workflowId.value, {
      tenant_id: tenantId.value,
      approvals: {
        brand: !!approvals.value.brand,
        category: !!approvals.value.category,
        product: !!approvals.value.product
      }
    })
    created.value = result
    step.value = 'done'
    toast.success('Producto creado en Gravity')
    emit('created', result)
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al crear'
  } finally {
    busy.value = false
  }
}
</script>

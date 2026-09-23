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
                {{ isEdit ? 'Editar atributo' : 'Nuevo atributo' }}
              </h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                <template v-if="isEdit">
                  Se guarda en Gravity (tienda nativa del tenant).
                </template>
                <template v-else-if="step === 1">
                  Paso 1 de 2 — elige la entidad
                </template>
                <template v-else>
                  Paso 2 de 2 — define el atributo
                </template>
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
          <div v-if="!isEdit" class="mt-4 flex gap-2" aria-hidden="true">
            <div
              class="h-1 flex-1 rounded-full transition-colors"
              :class="step >= 1 ? 'bg-primary-600' : 'bg-gray-200 dark:bg-gray-600'"
            />
            <div
              class="h-1 flex-1 rounded-full transition-colors"
              :class="step >= 2 ? 'bg-primary-600' : 'bg-gray-200 dark:bg-gray-600'"
            />
          </div>
        </div>

        <form class="flex flex-col flex-1 min-h-0" @submit.prevent="onFormSubmit">
          <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-6 space-y-4">
            <!-- Step 1: entity (create only) -->
            <div v-if="!isEdit && step === 1" class="space-y-3">
              <p class="text-sm text-gray-600 dark:text-gray-400">
                ¿Sobre qué entidad aplica este atributo?
              </p>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <button
                  v-for="opt in ENTITY_OPTIONS"
                  :key="opt.value"
                  type="button"
                  class="text-left rounded-lg border px-4 py-3.5 min-h-[44px] touch-manipulation transition-colors"
                  :class="
                    form.entityName === opt.value
                      ? 'border-primary-600 bg-primary-50 dark:bg-primary-900/20 ring-1 ring-primary-600'
                      : 'border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500 bg-white dark:bg-gray-900'
                  "
                  @click="selectEntity(opt.value)"
                >
                  <div class="flex items-start gap-3">
                    <span
                      class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg"
                      :class="
                        form.entityName === opt.value
                          ? 'bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300'
                          : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300'
                      "
                    >
                      <AdminNavIcon :name="opt.icon" />
                    </span>
                    <span class="min-w-0">
                      <span class="block text-sm font-semibold text-gray-900 dark:text-white">{{ opt.label }}</span>
                      <span class="block mt-0.5 text-xs leading-snug text-gray-500 dark:text-gray-400">
                        {{ opt.description }}
                      </span>
                    </span>
                  </div>
                </button>
              </div>
            </div>

            <!-- Step 2: fields -->
            <template v-else>
              <div
                class="flex items-center justify-between gap-3 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900/50 px-3 py-2.5"
              >
                <div class="min-w-0 flex items-center gap-3">
                  <span
                    v-if="selectedEntity"
                    class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300"
                  >
                    <AdminNavIcon :name="selectedEntity.icon" />
                  </span>
                  <div class="min-w-0">
                    <p class="text-xs text-gray-500 dark:text-gray-400">Entidad</p>
                    <p class="text-sm font-medium text-gray-900 dark:text-white truncate">
                      {{ entityLabel }}
                    </p>
                  </div>
                </div>
                <button
                  v-if="!isEdit"
                  type="button"
                  class="shrink-0 text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline min-h-[44px] px-2"
                  :disabled="saving"
                  @click="step = 1"
                >
                  Cambiar
                </button>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre</label>
                <input
                  v-model="form.name"
                  type="text"
                  required
                  maxlength="200"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  placeholder="Nombre interno"
                  autocomplete="off"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Etiqueta <span class="text-gray-400 font-normal">(opcional)</span>
                </label>
                <input
                  v-model="form.label"
                  type="text"
                  maxlength="200"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  placeholder="Texto visible"
                  autocomplete="off"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Descripción <span class="text-gray-400 font-normal">(opcional)</span>
                </label>
                <textarea
                  v-model="form.description"
                  rows="2"
                  maxlength="2000"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  placeholder="Descripción breve"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tipo</label>
                <select
                  v-model.number="form.specificationType"
                  required
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                >
                  <option v-for="opt in SPECIFICATION_TYPE_OPTIONS" :key="opt.value" :value="opt.value">
                    {{ opt.label }}
                  </option>
                </select>
              </div>

              <div v-if="showValuesField">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Valores <span class="text-gray-400 font-normal">(opcional)</span>
                </label>
                <div
                  class="flex flex-wrap items-center gap-2 min-h-[42px] rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-2 py-1.5 focus-within:ring-2 focus-within:ring-primary-500/40"
                  @click="focusValuesInput"
                >
                  <span
                    v-for="(badge, idx) in valueBadges"
                    :key="`${badge}-${idx}`"
                    class="inline-flex items-center gap-1 max-w-full rounded-md bg-primary-50 dark:bg-primary-900/30 text-primary-800 dark:text-primary-200 text-xs font-medium pl-2 pr-1 py-1"
                  >
                    <span class="truncate">{{ badge }}</span>
                    <button
                      type="button"
                      class="shrink-0 p-0.5 rounded hover:bg-primary-100 dark:hover:bg-primary-800/50"
                      :aria-label="`Quitar ${badge}`"
                      :disabled="saving"
                      @click.stop="removeBadge(idx)"
                    >
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </span>
                  <input
                    ref="valuesInputRef"
                    v-model="valuesDraft"
                    type="text"
                    maxlength="400"
                    class="flex-1 min-w-[8rem] border-0 bg-transparent text-sm text-gray-900 dark:text-white px-1 py-1 focus:outline-none focus:ring-0"
                    placeholder="Escribe y pulsa Tab"
                    autocomplete="off"
                    @keydown="onValuesKeydown"
                    @blur="commitValuesDraft"
                  />
                </div>
                <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                  Tab o coma para crear un badge. Se envían unidos por coma.
                </p>
              </div>

              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Orden</label>
                  <input
                    v-model.number="form.order"
                    type="number"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    Sección <span class="text-gray-400 font-normal">(opcional)</span>
                  </label>
                  <input
                    v-model="form.sectionGroup"
                    type="text"
                    maxlength="200"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                    placeholder="Grupo"
                    autocomplete="off"
                  />
                </div>
              </div>

              <div class="space-y-3 pt-1">
                <div class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-700 dark:text-gray-300">Multi-opción</span>
                  <FormToggle v-model="form.isMultiOption" aria-label="Multi-opción" />
                </div>
                <div class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-700 dark:text-gray-300">Requerido</span>
                  <FormToggle v-model="form.required" aria-label="Requerido" />
                </div>
                <div class="flex items-center justify-between gap-3">
                  <span class="text-sm text-gray-700 dark:text-gray-300">Público</span>
                  <FormToggle v-model="form.isPublic" aria-label="Público" />
                </div>
              </div>
            </template>

            <p v-if="formError" class="text-sm text-red-600 dark:text-red-400">{{ formError }}</p>
          </div>

          <div
            class="shrink-0 px-4 sm:px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-2"
          >
            <button
              type="button"
              class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
              :disabled="saving"
              @click="onSecondaryAction"
            >
              {{ showBackToEntity ? 'Atrás' : 'Cancelar' }}
            </button>
            <button
              v-if="!isEdit && step === 1"
              type="button"
              class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="!form.entityName"
              @click="goToStep2"
            >
              Continuar
            </button>
            <button
              v-else
              type="submit"
              class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="saving || !form.name.trim() || !form.entityName"
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
import { computed, nextTick, reactive, ref, watch } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import FormToggle from '../FormToggle.vue'
import AdminNavIcon from '../../layouts/adminNav/AdminNavIcon.vue'
import { SPECIFICATION_TYPE_OPTIONS, specificationTypeSupportsValues } from './attributeSpecTypes'

const ENTITY_OPTIONS = [
  {
    value: 'Product',
    label: 'Producto',
    icon: 'cube',
    description: 'Especificaciones del catálogo: color, talla, material y más.'
  },
  {
    value: 'SaleOrder',
    label: 'Orden',
    icon: 'shoppingBag',
    description: 'Datos extra del pedido: prioridad, canal o notas internas.'
  },
  {
    value: 'Category',
    label: 'Categoría',
    icon: 'tag',
    description: 'Metadatos de la categoría para filtrar u organizar el árbol.'
  },
  {
    value: 'Brand',
    label: 'Marca',
    icon: 'brand',
    description: 'Propiedades de la marca: origen, licencia o estilo comercial.'
  }
]

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  /** null = create; object with entityAttributeId = edit */
  attribute: { type: Object, default: null }
})

const emit = defineEmits(['update:modelValue', 'saved'])

const toast = useToast()
const saving = ref(false)
const formError = ref(null)
const step = ref(1)
const valuesDraft = ref('')
const valueBadges = ref([])
const valuesInputRef = ref(null)

const form = reactive({
  entityName: '',
  name: '',
  label: '',
  description: '',
  specificationType: 1,
  order: 0,
  sectionGroup: '',
  isMultiOption: false,
  required: false,
  isPublic: false
})

const isEdit = computed(() => !!(props.attribute && props.attribute.entityAttributeId))
const showBackToEntity = computed(() => !isEdit.value && step.value === 2)
const showValuesField = computed(() => specificationTypeSupportsValues(form.specificationType))
const headingId = 'admin-store-attribute-form-heading'
const selectedEntity = computed(() => ENTITY_OPTIONS.find((o) => o.value === form.entityName) ?? null)
const entityLabel = computed(() => {
  const opt = selectedEntity.value
  return opt ? opt.label : form.entityName || '—'
})

watch(
  () => form.specificationType,
  (type) => {
    if (!specificationTypeSupportsValues(type)) {
      valueBadges.value = []
      valuesDraft.value = ''
    }
  }
)
function parseValuesToBadges(raw) {
  if (!raw || !String(raw).trim()) return []
  const seen = new Set()
  const out = []
  for (const part of String(raw).split(',')) {
    const v = part.trim()
    if (!v || seen.has(v)) continue
    seen.add(v)
    out.push(v)
  }
  return out
}

function resetForm() {
  form.entityName = props.attribute?.entityName || ''
  form.name = props.attribute?.name ?? ''
  form.label = props.attribute?.label ?? ''
  form.description = props.attribute?.description ?? ''
  form.specificationType = Number(props.attribute?.specificationType) || 1
  form.order = Number(props.attribute?.order) || 0
  form.sectionGroup = props.attribute?.sectionGroup ?? ''
  form.isMultiOption = !!props.attribute?.isMultiOption
  form.required = !!props.attribute?.required
  form.isPublic = !!props.attribute?.isPublic
  valueBadges.value = parseValuesToBadges(props.attribute?.values)
  valuesDraft.value = ''
  formError.value = null
  step.value = isEdit.value ? 2 : 1
}

watch(
  () => [props.modelValue, props.attribute],
  ([open]) => {
    if (open) resetForm()
  }
)

function close() {
  if (saving.value) return
  emit('update:modelValue', false)
}

function selectEntity(value) {
  form.entityName = value
}

function goToStep2() {
  if (!form.entityName) return
  step.value = 2
}

function onSecondaryAction() {
  if (showBackToEntity.value) {
    step.value = 1
    return
  }
  close()
}

function focusValuesInput() {
  valuesInputRef.value?.focus()
}

function addBadge(raw) {
  const v = String(raw || '').trim().replace(/^,+|,+$/g, '').trim()
  if (!v) return
  if (!valueBadges.value.includes(v)) valueBadges.value = [...valueBadges.value, v]
  valuesDraft.value = ''
}

function removeBadge(idx) {
  valueBadges.value = valueBadges.value.filter((_, i) => i !== idx)
}

function commitValuesDraft() {
  if (valuesDraft.value.trim()) addBadge(valuesDraft.value)
}

function onValuesKeydown(e) {
  if (e.key === 'Tab' || e.key === ',') {
    if (!valuesDraft.value.trim()) return
    e.preventDefault()
    addBadge(valuesDraft.value.replace(/,/g, ''))
    return
  }
  if (e.key === 'Enter') {
    e.preventDefault()
    if (valuesDraft.value.trim()) addBadge(valuesDraft.value)
    return
  }
  if (e.key === 'Backspace' && !valuesDraft.value && valueBadges.value.length) {
    e.preventDefault()
    valueBadges.value = valueBadges.value.slice(0, -1)
  }
}

function onFormSubmit() {
  if (!isEdit.value && step.value === 1) {
    goToStep2()
    return
  }
  submit()
}

async function submit() {
  const name = form.name.trim()
  const entityName = form.entityName.trim()
  if (!name || !entityName) return
  commitValuesDraft()
  await nextTick()
  saving.value = true
  formError.value = null
  const valuesJoined =
    specificationTypeSupportsValues(form.specificationType) && valueBadges.value.length
      ? valueBadges.value.join(',')
      : null
  const payload = {
    entityName,
    name,
    label: form.label.trim() || null,
    description: form.description.trim() || null,
    values: valuesJoined,
    specificationType: form.specificationType,
    order: form.order || 0,
    sectionGroup: form.sectionGroup.trim() || null,
    isMultiOption: form.isMultiOption,
    required: form.required,
    isPublic: form.isPublic
  }
  try {
    let result
    if (isEdit.value) {
      result = await apiService.updateCatalogAttribute(props.attribute.entityAttributeId, payload)
      toast.success('Atributo actualizado')
    } else {
      result = await apiService.createCatalogAttribute(payload)
      toast.success('Atributo creado')
    }
    emit('saved', result)
    emit('update:modelValue', false)
  } catch (err) {
    const msg = err.response?.data?.error || err.message || 'No se pudo guardar el atributo'
    formError.value = msg
    toast.error(msg)
  } finally {
    saving.value = false
  }
}
</script>

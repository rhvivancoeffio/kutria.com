<template>
  <div class="relative" ref="containerRef">
    <button
      ref="triggerRef"
      type="button"
      @click="open = !open"
      :class="[
        'input-field w-full text-left flex items-center justify-between gap-2',
        invalid && '!border-red-500 dark:!border-red-500'
      ]"
    >
      <slot name="trigger" :displayValue="displayValue" :placeholder="placeholder" :open="open">
        <span class="truncate">{{ triggerLabel ?? (displayValue || placeholder) }}</span>
        <svg class="w-4 h-4 shrink-0" :class="{ 'rotate-180': open }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </slot>
    </button>
    <Transition
      :enter-active-class="teleport ? 'duration-0' : 'transition duration-150 ease-out'"
      :enter-from-class="teleport ? '' : 'opacity-0 scale-95'"
      :enter-to-class="teleport ? '' : 'opacity-100 scale-100'"
      :leave-active-class="teleport ? 'duration-0' : 'transition duration-100 ease-in'"
      :leave-from-class="teleport ? '' : 'opacity-100 scale-100'"
      :leave-to-class="teleport ? '' : 'opacity-0 scale-95'"
    >
      <component
        :is="teleport ? 'Teleport' : 'div'"
        :to="teleport ? '#portal-root' : undefined"
        v-show="open"
      >
      <div
        ref="dropdownRef"
        :class="[
          'max-h-72 overflow-hidden flex flex-col bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 z-[9999]',
          teleport ? 'pointer-events-auto max-w-[calc(100vw-2rem)]' : 'absolute mt-1 left-0 right-0 w-full max-w-[calc(100vw-2rem)]'
        ]"
        :style="teleport ? dropdownStyle : undefined"
      >
        <div v-if="$slots['before-options']" class="p-2 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <slot name="before-options" :close="close" />
        </div>
        <div class="p-2 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <input
            ref="searchInputRef"
            v-model="searchQuery"
            type="text"
            :placeholder="searchPlaceholder"
            class="input-field w-full text-sm py-1.5"
            @keydown.esc="close()"
            @keydown.down.prevent="focusNext"
            @keydown.up.prevent="focusPrev"
            @keydown.enter.prevent="selectHighlighted"
          />
        </div>
        <div class="overflow-y-auto py-1 min-h-0 flex-1">
          <div v-if="effectiveLoading" class="px-3 py-4 text-center text-xs text-gray-500 dark:text-gray-400">
            {{ loadingText }}
          </div>
          <template v-else>
            <div
              v-for="(item, idx) in selectableItems"
              :key="item.key"
              role="button"
              tabindex="0"
              @click="(e) => { if (!e.target.closest('button, input, textarea')) select(item.value) }"
              @keydown.enter.prevent="select(item.value)"
              @keydown.space.prevent="select(item.value)"
              :class="[
                'flex items-center gap-2 w-full px-3 py-2 text-left text-sm transition-colors cursor-pointer',
                (modelValue === item.value || highlightedIndex === idx)
                  ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                  : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
              ]"
            >
              <slot name="option" :item="item" :selected="modelValue === item.value" :index="idx">
                {{ item.label }}
              </slot>
            </div>
            <div
              v-if="selectableItems.length === 0"
              class="px-3 py-4 text-xs text-gray-500 dark:text-gray-400 text-center"
            >
              {{ emptyMessage }}
            </div>
          </template>
        </div>
        </div>
      </component>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import { computePosition, offset, flip, shift, autoUpdate } from '@floating-ui/dom'

function useDebounce(fn, ms) {
  let timeout
  return (...args) => {
    clearTimeout(timeout)
    timeout = setTimeout(() => fn(...args), ms)
  }
}

const props = defineProps({
  modelValue: {
    type: [String, Number],
    default: ''
  },
  options: {
    type: Array,
    default: () => []
  },
  placeholder: {
    type: String,
    default: '— Seleccionar —'
  },
  searchPlaceholder: {
    type: String,
    default: 'Buscar...'
  },
  emptyMessage: {
    type: String,
    default: 'No hay opciones'
  },
  loadingText: {
    type: String,
    default: 'Cargando...'
  },
  loading: {
    type: Boolean,
    default: false
  },
  invalid: {
    type: Boolean,
    default: false
  },
  allowCustom: {
    type: Boolean,
    default: false
  },
  customOptionLabel: {
    type: String,
    default: 'Usar'
  },
  /** Cuando se define, reemplaza el texto del trigger (ej: "Conversaciones (5)") */
  triggerLabel: {
    type: String,
    default: null
  },
  /** Alineación del dropdown: 'left' | 'right' */
  dropdownAlign: {
    type: String,
    default: 'left'
  },
  /** Si true, las opciones con value vacío ('') siempre se muestran al filtrar (ej: "— No mapear") */
  alwaysShowEmptyOption: {
    type: Boolean,
    default: false
  },
  /** Si true, el dropdown se renderiza en body (evita que quede detrás de sidebar/header) */
  teleport: {
    type: Boolean,
    default: false
  },
  /** Función async para cargar opciones dinámicamente. Recibe query y retorna [{value, label}]. Si se define, ignora options estáticos. */
  fetchOptions: {
    type: Function,
    default: null
  }
})

const emit = defineEmits(['update:modelValue', 'select', 'open', 'close'])

const open = ref(false)
const searchQuery = ref('')
const containerRef = ref(null)
const asyncOptions = ref([])
const fetchLoading = ref(false)
const triggerRef = ref(null)
const dropdownRef = ref(null)
const searchInputRef = ref(null)
const highlightedIndex = ref(0)
const dropdownPosition = ref({ top: 0, left: 0 })
/** Ancho del trigger (px) para igualar el popover en modo teleport. */
const dropdownWidthPx = ref(0)
const positionReady = ref(false)
let autoUpdateCleanup = null

const dropdownStyle = computed(() => {
  const { top, left } = dropdownPosition.value
  const style = {
    position: 'fixed',
    top: `${top}px`,
    left: `${left}px`,
    right: 'auto',
    boxSizing: 'border-box'
  }
  const w = dropdownWidthPx.value
  if (w > 0) {
    style.width = `${w}px`
  }
  if (!positionReady.value) {
    style.visibility = 'hidden'
    style.pointerEvents = 'none'
  } else {
    style.visibility = 'visible'
  }
  return style
})

const effectiveOptions = computed(() => {
  if (props.fetchOptions && typeof props.fetchOptions === 'function') {
    return asyncOptions.value
  }
  return props.options || []
})

const normalizedOptions = computed(() => {
  return (effectiveOptions.value || []).map((o) => {
    if (typeof o === 'string') return { value: o, label: o }
    const v = o.value ?? o
    const l = o.label ?? o.value ?? o
    return { value: String(v), label: String(l), ...o }
  })
})

const effectiveLoading = computed(() => props.loading || fetchLoading.value)

const filteredOptions = computed(() => {
  const q = (searchQuery.value ?? '').toLowerCase().trim()
  const opts = normalizedOptions.value
  if (!q) return opts
  const filtered = opts.filter((o) => o.label.toLowerCase().includes(q) || String(o.value || '').toLowerCase().includes(q))
  if (props.alwaysShowEmptyOption) {
    const emptyOpt = opts.find((o) => o.value === '' || o.value == null)
    if (emptyOpt && !filtered.some((f) => String(f.value || '') === String(emptyOpt.value || ''))) {
      return [emptyOpt, ...filtered]
    }
  }
  return filtered
})

const hasCustomOption = computed(() =>
  props.allowCustom && searchQuery.value.trim() && !normalizedOptions.value.some((o) => o.value === searchQuery.value.trim())
)

const selectableItems = computed(() => {
  const items = []
  if (hasCustomOption.value) {
    items.push({ key: 'custom', value: searchQuery.value.trim(), label: `${props.customOptionLabel} "${searchQuery.value.trim()}"` })
  }
  filteredOptions.value.forEach((o) => items.push({ key: o.value, value: o.value, label: o.label, ...o }))
  return items
})

const selectedLabelCache = ref(null)

const displayValue = computed(() => {
  const v = props.modelValue
  if (v == null || v === '') {
    selectedLabelCache.value = null
    return ''
  }
  const opt = normalizedOptions.value.find((o) => o.value === String(v))
  if (opt) {
    selectedLabelCache.value = opt.label
    return opt.label
  }
  return selectedLabelCache.value ?? String(v)
})

function select(value) {
  const item = selectableItems.value.find((i) => i.value === value)
  if (item) selectedLabelCache.value = item.label
  emit('update:modelValue', value)
  emit('select', value)
  close()
}

function close() {
  autoUpdateCleanup?.()
  autoUpdateCleanup = null
  positionReady.value = false
  dropdownWidthPx.value = 0
  open.value = false
  searchQuery.value = ''
  emit('close')
}

function focusNext() {
  const max = selectableItems.value.length - 1
  highlightedIndex.value = Math.min(highlightedIndex.value + 1, max)
}

function focusPrev() {
  highlightedIndex.value = Math.max(highlightedIndex.value - 1, 0)
}

function selectHighlighted() {
  const items = selectableItems.value
  if (items.length === 0) return
  const idx = highlightedIndex.value
  if (idx >= 0 && idx < items.length) select(items[idx].value)
}

async function updateDropdownPosition() {
  const referenceEl = triggerRef.value ?? containerRef.value
  const floatingEl = dropdownRef.value
  if (!referenceEl || !floatingEl || !props.teleport) return
  const refRect = referenceEl.getBoundingClientRect()
  const vw = typeof window !== 'undefined' ? window.innerWidth : refRect.width
  const maxW = Math.max(0, vw - 32)
  dropdownWidthPx.value = Math.min(refRect.width, maxW)
  const placement = props.dropdownAlign === 'right' ? 'bottom-end' : 'bottom-start'
  const { x, y } = await computePosition(referenceEl, floatingEl, {
    placement,
    strategy: 'fixed',
    middleware: [
      offset(4),
      flip({ padding: 16 }),
      shift({ padding: 16, crossAxis: true })
    ]
  })
  dropdownPosition.value = { top: y, left: x }
  positionReady.value = true
}

async function doFetch(query) {
  if (typeof props.fetchOptions !== 'function') return
  fetchLoading.value = true
  try {
    const opts = await props.fetchOptions(query)
    asyncOptions.value = Array.isArray(opts) ? opts : []
  } catch {
    asyncOptions.value = []
  } finally {
    fetchLoading.value = false
  }
}

const debouncedFetch = useDebounce(doFetch, 300)

watch(open, async (isOpen) => {
  if (isOpen) {
    searchQuery.value = ''
    highlightedIndex.value = 0
    positionReady.value = false
    dropdownWidthPx.value = 0
    emit('open')
    if (props.fetchOptions) {
      await doFetch('')
    }
    if (props.teleport) {
      await nextTick()
      await updateDropdownPosition()
      const refEl = triggerRef.value ?? containerRef.value
      const floatEl = dropdownRef.value
      if (refEl && floatEl) {
        autoUpdateCleanup?.()
        autoUpdateCleanup = autoUpdate(refEl, floatEl, updateDropdownPosition)
      }
    }
    nextTick(() => searchInputRef.value?.focus())
  } else {
    autoUpdateCleanup?.()
    autoUpdateCleanup = null
  }
})

watch(searchQuery, (q) => {
  highlightedIndex.value = 0
  if (props.fetchOptions && open.value) {
    debouncedFetch(q)
  }
})

function handleClickOutside(e) {
  const inContainer = containerRef.value?.contains(e.target)
  const inDropdown = props.teleport && dropdownRef.value?.contains(e.target)
  if (!inContainer && !inDropdown) close()
}

onMounted(() => document.addEventListener('click', handleClickOutside))
onUnmounted(() => {
  autoUpdateCleanup?.()
  document.removeEventListener('click', handleClickOutside)
})
</script>

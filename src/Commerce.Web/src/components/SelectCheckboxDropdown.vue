<template>
  <div ref="rootEl" class="relative min-w-0" :class="wrapperClass">
    <label v-if="label" :for="buttonId" class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
      {{ label }}
    </label>
    <p v-if="hint" class="text-[11px] text-gray-500 dark:text-gray-400 -mt-0.5 mb-1.5">
      {{ hint }}
    </p>

    <button
      :id="buttonId"
      ref="buttonEl"
      type="button"
      class="flex w-full min-w-0 items-center justify-between gap-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2.5 text-left text-sm text-gray-900 dark:text-gray-100 shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 disabled:cursor-not-allowed disabled:opacity-50"
      :disabled="disabled"
      :aria-expanded="open"
      :aria-controls="listboxId"
      aria-haspopup="listbox"
      @keydown="onButtonKeydown"
      @click="toggleOpen"
    >
      <span class="min-w-0 truncate">{{ summaryText }}</span>
      <svg
        class="h-4 w-4 shrink-0 text-gray-500 dark:text-gray-400 transition-transform duration-200"
        :class="open ? 'rotate-180' : ''"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <Transition
      enter-active-class="transition ease-out duration-100"
      enter-from-class="opacity-0 scale-95"
      enter-to-class="opacity-100 scale-100"
      leave-active-class="transition ease-in duration-75"
      leave-from-class="opacity-100 scale-100"
      leave-to-class="opacity-0 scale-95"
    >
      <div
        v-show="open"
        :id="listboxId"
        class="absolute left-0 right-0 z-[100] mt-1 max-h-60 overflow-y-auto rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 py-1 shadow-lg ring-1 ring-black/5 dark:ring-white/10"
        role="listbox"
        :aria-multiselectable="true"
        tabindex="-1"
        @keydown.stop="onListboxKeydown"
      >
        <label
          v-for="opt in normalizedOptions"
          :key="String(opt.value)"
          class="flex cursor-pointer items-center gap-2.5 px-3 py-2 text-sm text-gray-800 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700/80"
          :class="isChecked(opt.value) ? 'bg-primary-50/80 dark:bg-primary-950/30' : ''"
        >
          <input
            type="checkbox"
            class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
            :checked="isChecked(opt.value)"
            @change.stop="toggleValue(opt.value)"
            @click.stop
          />
          <span class="min-w-0 flex-1">{{ opt.label }}</span>
        </label>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'

const props = defineProps({
  /** Valores seleccionados (p. ej. claves de enum). */
  modelValue: { type: Array, default: () => [] },
  /** Commerceiones `{ value, label }`. */
  options: { type: Array, required: true },
  label: { type: String, default: '' },
  hint: { type: String, default: '' },
  /** Texto del botón cuando no hay ninguna opción marcada. */
  placeholder: { type: String, default: 'Seleccionar…' },
  disabled: { type: Boolean, default: false },
  /** Clases extra en el contenedor externo. */
  wrapperClass: { type: String, default: '' }
})

const emit = defineEmits(['update:modelValue'])

const rootEl = ref(null)
const buttonEl = ref(null)
const open = ref(false)

const instanceSuffix = Math.random().toString(36).slice(2, 11)
const buttonId = `scb-btn-${instanceSuffix}`
const listboxId = `scb-list-${instanceSuffix}`

const normalizedOptions = computed(() =>
  (props.options || []).map((o) => ({
    value: o.value,
    label: String(o.label ?? o.value ?? '')
  }))
)

function isChecked(value) {
  const v = String(value)
  return (props.modelValue || []).some((x) => String(x) === v)
}

const summaryText = computed(() => {
  const sel = props.modelValue || []
  if (sel.length === 0) return props.placeholder
  if (sel.length === 1) {
    const v = String(sel[0])
    const o = normalizedOptions.value.find((x) => String(x.value) === v)
    return o?.label ?? v
  }
  return `${sel.length} seleccionados`
})

function toggleValue(value) {
  const v = String(value)
  const cur = [...(props.modelValue || [])]
  const i = cur.findIndex((x) => String(x) === v)
  if (i >= 0) cur.splice(i, 1)
  else cur.push(v)
  emit('update:modelValue', cur)
}

function toggleOpen() {
  if (props.disabled) return
  open.value = !open.value
}

function close() {
  open.value = false
}

function onDocPointerDown(ev) {
  const root = rootEl.value
  if (!root || !open.value) return
  if (root.contains(ev.target)) return
  close()
}

function onDocKeydown(ev) {
  if (ev.key === 'Escape' && open.value) {
    ev.preventDefault()
    close()
    buttonEl.value?.focus()
  }
}

watch(open, (isOpen) => {
  if (isOpen) {
    nextTick(() => {
      document.addEventListener('pointerdown', onDocPointerDown, true)
      document.addEventListener('keydown', onDocKeydown, true)
    })
  } else {
    document.removeEventListener('pointerdown', onDocPointerDown, true)
    document.removeEventListener('keydown', onDocKeydown, true)
  }
})

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocPointerDown, true)
  document.removeEventListener('keydown', onDocKeydown, true)
})

function onButtonKeydown(ev) {
  if (props.disabled) return
  if (ev.key === 'Enter' || ev.key === ' ') {
    ev.preventDefault()
    toggleOpen()
  }
  if (ev.key === 'ArrowDown' && !open.value) {
    ev.preventDefault()
    open.value = true
  }
}

function onListboxKeydown(ev) {
  if (ev.key === 'Escape') {
    ev.preventDefault()
    close()
    buttonEl.value?.focus()
  }
}
</script>

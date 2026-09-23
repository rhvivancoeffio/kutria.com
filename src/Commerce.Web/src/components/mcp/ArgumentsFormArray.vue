<template>
  <div class="flex flex-col min-h-0 rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/30 dark:bg-gray-800/30 overflow-hidden">
    <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">{{ title }}</label>
      <p v-if="hint" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5" v-html="hint"></p>
    </div>
    <div class="p-3 flex-1 min-h-[280px] lg:min-h-[360px] overflow-y-auto space-y-3">
      <div
        v-for="(arg, idx) in modelValue"
        :key="idx"
        class="rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700/50 overflow-hidden"
      >
        <button
          type="button"
          class="w-full flex items-center justify-between gap-2 px-3 py-2.5 text-left hover:bg-gray-50 dark:hover:bg-gray-700/70 transition-colors"
          @click="toggleExpanded(idx)"
        >
          <span class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ itemLabel }} {{ idx + 1 }}{{ arg.name ? ` — ${arg.name}` : '' }}</span>
          <span class="flex items-center gap-1">
            <svg class="w-4 h-4 text-gray-400 transition-transform" :class="{ 'rotate-180': isExpanded(idx) }" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" /></svg>
            <button type="button" @click.stop="remove(idx)" class="p-1.5 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded hover:bg-gray-200 dark:hover:bg-gray-600" title="Quitar">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
            </button>
          </span>
        </button>
        <div v-show="isExpanded(idx)" class="px-3 pb-3 pt-0 space-y-2 border-t border-gray-100 dark:border-gray-600">
          <div class="space-y-0.5 pt-2">
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Nombre</label>
            <input
              :value="arg.name"
              type="text"
              placeholder="name"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              @input="e => updateField(idx, 'name', e.target.value)"
            />
          </div>
          <div class="space-y-0.5">
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Descripción</label>
            <input
              :value="arg.description"
              type="text"
              placeholder="(opcional)"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              @input="e => updateField(idx, 'description', e.target.value)"
            />
          </div>
          <div class="flex items-center gap-2">
            <label class="relative inline-flex items-center cursor-pointer select-none">
              <input
                :checked="arg.required"
                type="checkbox"
                class="sr-only peer"
                @change="e => updateField(idx, 'required', e.target.checked)"
              />
              <div class="w-11 h-6 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5" />
              <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Requerido</span>
            </label>
          </div>
        </div>
      </div>
      <button
        type="button"
        class="w-full py-2 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors"
        @click="add"
      >
        {{ addButtonLabel }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'

const props = defineProps({
  modelValue: {
    type: Array,
    default: () => []
  },
  /** Título de la sección */
  title: { type: String, default: 'Argumentos' },
  /** Texto de ayuda (puede incluir HTML, ej. código) */
  hint: { type: String, default: '' },
  /** Etiqueta por ítem en el acordeón (ej. "Argumento") */
  itemLabel: { type: String, default: 'Argumento' },
  /** Texto del botón añadir */
  addButtonLabel: { type: String, default: '+ Añadir argumento' }
})

const emit = defineEmits(['update:modelValue'])

/** fromTemplate: false = fila añadida manualmente; no se elimina al reconciliar con el texto. */
const defaultItem = () => ({ name: '', description: '', required: false, fromTemplate: false })

/** Por índice: true = expandido. Se sincroniza con la longitud de modelValue. */
const expanded = ref([])

function syncExpanded() {
  const len = (props.modelValue || []).length
  const prev = expanded.value.length
  if (len > prev) {
    for (let i = prev; i < len; i++) expanded.value.push(false)
  } else if (len < prev) {
    expanded.value = expanded.value.slice(0, len)
  }
}

watch(() => props.modelValue?.length, syncExpanded, { immediate: true })

function isExpanded(idx) {
  return expanded.value[idx] === true
}

function toggleExpanded(idx) {
  const arr = [...expanded.value]
  arr[idx] = !arr[idx]
  expanded.value = arr
}

function updateField(idx, key, value) {
  const list = (props.modelValue || []).map((item, i) =>
    i === idx ? { ...item, [key]: value } : item
  )
  emit('update:modelValue', list)
}

function add() {
  const list = [...(props.modelValue || []), defaultItem()]
  emit('update:modelValue', list)
  expanded.value.push(false)
}

function remove(idx) {
  const list = (props.modelValue || []).filter((_, i) => i !== idx)
  if (list.length === 0) {
    emit('update:modelValue', [defaultItem()])
    expanded.value = [false]
  } else {
    emit('update:modelValue', list)
    expanded.value = expanded.value.filter((_, i) => i !== idx)
  }
}
</script>

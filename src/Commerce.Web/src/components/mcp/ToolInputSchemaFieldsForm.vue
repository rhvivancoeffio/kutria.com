<template>
  <div class="space-y-3">
    <div
      v-for="(field, idx) in fields"
      :key="idx"
      class="rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-700/50 overflow-hidden"
    >
      <button
        type="button"
        class="w-full flex items-center justify-between gap-2 px-3 py-2.5 text-left hover:bg-gray-100 dark:hover:bg-gray-700/70 transition-colors"
        @click="toggleExpanded(idx)"
      >
        <span class="text-xs font-medium text-gray-600 dark:text-gray-300">
          Parámetro {{ idx + 1 }}{{ field.name ? ` — ${field.name}` : '' }}{{ field.required && field.fromAction ? ' (requerido por la acción)' : '' }}{{ field.fromAction && !(field.required && field.fromAction) ? ' (acción)' : '' }}
        </span>
        <span class="flex items-center gap-1">
          <svg
            class="w-4 h-4 text-gray-400 transition-transform"
            :class="{ 'rotate-180': isExpanded(idx) }"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
          </svg>
          <button
            v-if="!(field.required && field.fromAction)"
            type="button"
            class="p-1.5 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded hover:bg-gray-200 dark:hover:bg-gray-600"
            title="Quitar"
            @click.stop="removeField(idx)"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </span>
      </button>
      <div v-show="isExpanded(idx)" class="px-3 pb-3 pt-0 border-t border-gray-200 dark:border-gray-600">
        <div class="grid grid-cols-2 gap-3 pt-3">
          <div class="space-y-1">
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Nombre</label>
            <input
              v-model="field.name"
              type="text"
              placeholder="param_name"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
            />
          </div>
          <div class="space-y-1">
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Tipo</label>
            <select
              v-model="field.type"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
            >
              <option value="string">string</option>
              <option value="number">number</option>
              <option value="integer">integer</option>
              <option value="boolean">boolean</option>
              <option value="array">array</option>
              <option value="object">object</option>
            </select>
          </div>
          <div class="space-y-1">
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Valor por defecto</label>
            <input
              v-model="field.defaultValue"
              type="text"
              placeholder="(opcional)"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
            />
          </div>
          <div class="space-y-1">
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Requerido</label>
            <div class="flex items-center h-[42px]">
              <label class="relative inline-flex items-center cursor-pointer select-none">
                <input v-model="field.required" type="checkbox" class="sr-only peer" />
                <div
                  class="w-12 h-7 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[3px] after:left-[3px] after:bg-white after:rounded-full after:h-6 after:w-6 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5"
                />
                <span class="ml-3 text-sm font-medium text-gray-700 dark:text-gray-300">{{ field.required ? 'Sí' : 'No' }}</span>
              </label>
            </div>
          </div>
        </div>
      </div>
    </div>

    <button
      type="button"
      class="w-full py-2.5 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-primary-50/50 dark:hover:bg-primary-900/10 text-sm font-medium transition-colors"
      @click="addField"
    >
      + Añadir parámetro
    </button>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'

const fields = defineModel({ type: Array, required: true })

const expanded = ref([])

function syncExpandedToFieldsLength() {
  const n = fields.value.length
  const e = expanded.value
  if (e.length < n) {
    expanded.value = [...e, ...Array(n - e.length).fill(true)]
  } else if (e.length > n) {
    expanded.value = e.slice(0, n)
  }
}

watch(() => fields.value.length, syncExpandedToFieldsLength, { immediate: true })

function isExpanded(idx) {
  return expanded.value[idx] !== false
}

function toggleExpanded(idx) {
  const arr = [...expanded.value]
  arr[idx] = !arr[idx]
  expanded.value = arr
}

const defaultNewField = () => ({
  name: '',
  type: 'string',
  required: false,
  defaultValue: '',
  fromAction: false
})

function addField() {
  fields.value = [...fields.value, defaultNewField()]
}

function removeField(idx) {
  const field = fields.value[idx]
  if (field?.required && field?.fromAction) return
  fields.value = fields.value.filter((_, i) => i !== idx)
  expanded.value = expanded.value.filter((_, i) => i !== idx)
}
</script>

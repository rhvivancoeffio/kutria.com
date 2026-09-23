<template>
  <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/30">
    <div
      v-if="showHeader"
      class="px-3 py-2.5 text-xs font-semibold text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/70"
    >
      {{ title }}
    </div>
    <div class="p-3 space-y-2">
      <p class="text-xs text-gray-500 dark:text-gray-400">
        {{ hint }}
      </p>
      <div class="space-y-2">
        <div
          v-for="(item, idx) in modelValue"
          :key="'manual-' + idx"
          class="space-y-1"
        >
          <div class="flex items-center gap-2 flex-wrap">
            <input
              v-model="item.path"
              type="text"
              placeholder="ej. id, data.name, items"
              class="flex-1 min-w-[120px] px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white font-mono placeholder-gray-400"
            />
            <select
              v-model="item.type"
              class="shrink-0 px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
            >
              <option v-for="t in typeOptions" :key="t.value" :value="t.value">{{ t.label }}</option>
            </select>
            <template v-if="item.type === 'array'">
              <span class="text-xs text-gray-500 dark:text-gray-400 shrink-0">elemento:</span>
              <select
                v-model="item.itemType"
                class="shrink-0 px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
              >
                <option v-for="t in itemTypeOptions" :key="t.value" :value="t.value">{{ t.label }}</option>
              </select>
            </template>
            <button
              type="button"
              @click="removeAt(idx)"
              class="p-2 rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 border border-transparent hover:border-gray-300 dark:hover:border-gray-600 shrink-0"
              title="Quitar"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
            </button>
          </div>
          <p v-if="item.type === 'array'" class="text-xs text-gray-500 dark:text-gray-400 pl-1">
            Ej. array simple: path <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">tags</code>. Array de objetos: path <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">items</code> + añade filas <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">items.id</code>, <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">items.name</code>.
          </p>
          <p v-else-if="item.type === 'object'" class="text-xs text-gray-500 dark:text-gray-400 pl-1">
            Ej. path <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">address</code> + añade filas <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">address.street</code>, <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">address.city</code> para objeto anidado.
          </p>
        </div>
        <button
          type="button"
          @click="add"
          class="flex items-center gap-2 px-3 py-2 text-sm text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 rounded-lg border border-primary-300 dark:border-primary-600"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
          Añadir propiedad
        </button>
      </div>
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
  title: { type: String, default: 'Propiedades a devolver' },
  hint: { type: String, default: 'Indica a mano las propiedades que debe devolver el tool (ej. id, data.items).' },
  showHeader: { type: Boolean, default: true }
})

const emit = defineEmits(['update:modelValue'])

const typeOptions = [
  { value: 'string', label: 'string' },
  { value: 'number', label: 'number' },
  { value: 'integer', label: 'integer' },
  { value: 'boolean', label: 'boolean' },
  { value: 'array', label: 'array' },
  { value: 'object', label: 'object' }
]
const itemTypeOptions = [
  { value: 'string', label: 'string' },
  { value: 'number', label: 'number' },
  { value: 'integer', label: 'integer' },
  { value: 'boolean', label: 'boolean' },
  { value: 'object', label: 'object' }
]

const defaultItem = () => ({ path: '', type: 'string', itemType: 'string' })

function add() {
  emit('update:modelValue', [...(props.modelValue || []), defaultItem()])
}

function removeAt(idx) {
  const list = (props.modelValue || []).filter((_, i) => i !== idx)
  emit('update:modelValue', list.length > 0 ? list : [defaultItem()])
}
</script>

<template>
  <template v-for="node in nodes" :key="node.categoryId">
    <li role="treeitem" :aria-expanded="hasChildren(node) ? !!expanded[node.categoryId] : undefined">
      <div
        class="flex items-center gap-1 sm:gap-2 py-2.5 pr-3 hover:bg-gray-50 dark:hover:bg-gray-700/40 min-h-[44px]"
        :style="{ paddingLeft: `${12 + depth * 20}px` }"
      >
        <button
          type="button"
          class="p-1.5 rounded-lg shrink-0 touch-manipulation"
          :class="
            hasChildren(node)
              ? 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700'
              : 'text-transparent cursor-default'
          "
          :disabled="!hasChildren(node)"
          :aria-label="expanded[node.categoryId] ? 'Colapsar' : 'Expandir'"
          @click="hasChildren(node) && $emit('toggle', node.categoryId)"
        >
          <svg
            class="w-4 h-4 transition-transform"
            :class="expanded[node.categoryId] ? 'rotate-90' : ''"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
        <div class="min-w-0 flex-1">
          <p class="text-sm font-medium text-gray-900 dark:text-white truncate">{{ node.name || '—' }}</p>
          <p class="text-xs font-mono text-gray-400 dark:text-gray-500 truncate">{{ node.categoryId }}</p>
        </div>
        <div class="flex items-center shrink-0">
          <button
            type="button"
            class="p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 touch-manipulation"
            title="Agregar hija"
            @click="$emit('add-child', node)"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
          </button>
          <button
            type="button"
            class="p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 touch-manipulation"
            title="Editar"
            @click="$emit('edit', node)"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
            </svg>
          </button>
          <button
            type="button"
            class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 touch-manipulation disabled:opacity-50"
            title="Eliminar"
            :disabled="deletingId === node.categoryId"
            @click="$emit('delete', node)"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
      </div>
      <ul
        v-if="hasChildren(node) && expanded[node.categoryId]"
        role="group"
        class="divide-y divide-gray-100 dark:divide-gray-700/80"
      >
        <AdminStoreCategoryTreeRows
          :nodes="node.children"
          :depth="depth + 1"
          :expanded="expanded"
          :deleting-id="deletingId"
          @toggle="$emit('toggle', $event)"
          @add-child="$emit('add-child', $event)"
          @edit="$emit('edit', $event)"
          @delete="$emit('delete', $event)"
        />
      </ul>
    </li>
  </template>
</template>

<script setup>
defineOptions({ name: 'AdminStoreCategoryTreeRows' })

defineProps({
  nodes: { type: Array, required: true },
  depth: { type: Number, default: 0 },
  expanded: { type: Object, required: true },
  deletingId: { type: String, default: null }
})

defineEmits(['toggle', 'add-child', 'edit', 'delete'])

function hasChildren(node) {
  return Array.isArray(node.children) && node.children.length > 0
}
</script>

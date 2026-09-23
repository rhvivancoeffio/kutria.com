<template>
  <template v-for="node in nodes" :key="node.categoryId">
    <li role="treeitem">
      <div
        class="flex items-center gap-1 py-2 pr-2 hover:bg-gray-50 dark:hover:bg-gray-700/40 min-h-[40px]"
        :style="{ paddingLeft: `${8 + depth * 16}px` }"
      >
        <button
          type="button"
          class="p-1 rounded shrink-0"
          :class="
            hasChildren(node)
              ? 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700'
              : 'text-transparent cursor-default'
          "
          :disabled="!hasChildren(node)"
          @click.stop="hasChildren(node) && $emit('toggle', node.categoryId)"
        >
          <svg
            class="w-3.5 h-3.5 transition-transform"
            :class="expanded[node.categoryId] ? 'rotate-90' : ''"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </button>
        <button
          type="button"
          class="min-w-0 flex-1 text-left px-1.5 py-1 rounded-lg"
          :class="
            selectedId === node.categoryId
              ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
              : 'text-gray-900 dark:text-white'
          "
          @click="$emit('select', node)"
        >
          <span class="block text-sm truncate">{{ node.name || '—' }}</span>
          <span class="block text-[10px] font-mono text-gray-400 truncate">{{ node.categoryId }}</span>
        </button>
      </div>
      <ul
        v-if="hasChildren(node) && expanded[node.categoryId]"
        role="group"
        class="divide-y divide-gray-100 dark:divide-gray-700/80"
      >
        <ProductCategoryTreePick
          :nodes="node.children"
          :depth="depth + 1"
          :expanded="expanded"
          :selected-id="selectedId"
          @toggle="$emit('toggle', $event)"
          @select="$emit('select', $event)"
        />
      </ul>
    </li>
  </template>
</template>

<script setup>
defineOptions({ name: 'ProductCategoryTreePick' })

defineProps({
  nodes: { type: Array, required: true },
  depth: { type: Number, default: 0 },
  expanded: { type: Object, required: true },
  selectedId: { type: String, default: '' }
})

defineEmits(['toggle', 'select'])

function hasChildren(node) {
  return Array.isArray(node.children) && node.children.length > 0
}
</script>

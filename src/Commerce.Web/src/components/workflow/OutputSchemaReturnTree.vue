<template>
  <div
    class="overflow-hidden rounded-md border border-slate-200/90 dark:border-gray-600/90 bg-slate-50/40 dark:bg-gray-900/25 flex flex-col"
    :class="unconstrained ? 'flex-1 min-h-0' : 'max-h-[min(20rem,42vh)]'"
  >
    <div
      v-if="nodes.length > 0"
      class="shrink-0 flex items-center justify-end gap-2 px-2 py-1.5 border-b border-slate-200/80 dark:border-gray-700/60 bg-white/70 dark:bg-gray-800/50 font-sans text-[11px]"
    >
      <button type="button" class="text-primary-600 dark:text-primary-400 hover:underline font-medium" @click="emit('select-all')">
        Todo
      </button>
      <span class="text-slate-300 dark:text-gray-600 select-none">|</span>
      <button type="button" class="text-slate-500 dark:text-gray-400 hover:underline" @click="emit('select-none')">
        Ninguno
      </button>
    </div>
    <div class="overflow-y-auto text-[13px] leading-snug font-mono flex-1 min-h-0">
      <template v-for="(row, idx) in visibleRows" :key="row.node.path + '-' + idx">
        <div
          class="group flex items-start gap-2 py-2 pl-2 pr-2 border-b border-slate-200/50 dark:border-gray-700/40 last:border-b-0 hover:bg-white/80 dark:hover:bg-gray-800/40 transition-colors"
          :style="{ paddingLeft: `${8 + row.depth * 16}px` }"
        >
          <span class="flex items-start justify-end shrink-0 w-5 mt-0.5">
            <button
              v-if="row.hasChildren"
              type="button"
              class="w-5 h-5 flex items-center justify-center rounded text-slate-500 dark:text-gray-400 hover:bg-slate-200/80 dark:hover:bg-gray-700"
              :title="row.expanded ? 'Contraer' : 'Expandir'"
              @click="toggleExpand(row.node.path)"
            >
              <svg v-if="row.expanded" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
              <svg v-else class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </button>
            <span v-else class="w-5 shrink-0" aria-hidden="true" />
          </span>

          <template v-if="row.node.selectable">
            <input
              type="checkbox"
              :checked="selectedSet.has(row.node.path)"
              class="mt-1 rounded border-slate-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 shrink-0"
              @change="emit('toggle-leaf', row.node.path)"
            />
          </template>
          <span v-else class="mt-1 w-4 h-4 shrink-0" aria-hidden="true" />

          <div class="min-w-0 flex-1 pt-0.5 space-y-0.5">
            <div class="flex flex-wrap items-baseline gap-x-2 gap-y-0.5">
              <span class="text-violet-700 dark:text-violet-300">"{{ row.node.key }}"</span>
              <span v-if="row.node.required" class="text-[10px] font-sans font-semibold text-rose-600 dark:text-rose-400 uppercase">req</span>
              <span :class="['text-[11px] px-1.5 py-0.5 rounded font-medium tracking-tight', typePillClass(row.node.jsonType)]">
                {{ row.node.typeLabel }}
              </span>
            </div>
            <p v-if="row.node.description" class="text-[11px] text-slate-600 dark:text-gray-400 font-sans leading-relaxed line-clamp-2">
              {{ row.node.description }}
            </p>
            <div v-if="row.hasChildren && (row.node.leafPaths?.length || 0) > 0" class="pt-1">
              <button
                type="button"
                class="text-[11px] font-sans text-primary-600 dark:text-primary-400 hover:underline"
                @click="onBranchClick(row.node)"
              >
                {{ branchActionLabel(row.node) }}
              </button>
            </div>
          </div>
        </div>
      </template>
      <p v-if="visibleRows.length === 0" class="px-3 py-6 text-center text-xs text-slate-500 dark:text-gray-400 font-sans">
        Sin propiedades en el schema de respuesta.
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  nodes: { type: Array, default: () => [] },
  selectedPaths: { type: Array, default: () => [] },
  /** Sin tope de altura (vista maximizada / split) */
  unconstrained: { type: Boolean, default: false }
})

const emit = defineEmits(['toggle-leaf', 'branch-toggle', 'select-all', 'select-none'])

const expandedKeys = ref(new Set())

const selectedSet = computed(() => new Set(props.selectedPaths || []))

function allExpandablePaths(nodes, acc = []) {
  for (const n of nodes || []) {
    if (n.children?.length) {
      acc.push(n.path)
      allExpandablePaths(n.children, acc)
    }
  }
  return acc
}

watch(
  () => props.nodes,
  (nodes) => {
    expandedKeys.value = new Set(allExpandablePaths(nodes))
  },
  { immediate: true, deep: true }
)

function flattenVisible(nodes, depth = 0, out = []) {
  for (const n of nodes || []) {
    const hasChildren = (n.children?.length || 0) > 0
    const expanded = !hasChildren || expandedKeys.value.has(n.path)
    out.push({ node: n, depth, hasChildren, expanded })
    if (hasChildren && expanded) {
      flattenVisible(n.children, depth + 1, out)
    }
  }
  return out
}

const visibleRows = computed(() => flattenVisible(props.nodes || [], 0, []))

function toggleExpand(path) {
  const next = new Set(expandedKeys.value)
  if (next.has(path)) next.delete(path)
  else next.add(path)
  expandedKeys.value = next
}

function typePillClass(jsonType) {
  const t = (jsonType || '').toLowerCase()
  const map = {
    string: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/35 dark:text-emerald-300',
    number: 'bg-blue-100 text-blue-800 dark:bg-blue-900/35 dark:text-blue-300',
    integer: 'bg-sky-100 text-sky-800 dark:bg-sky-900/35 dark:text-sky-300',
    boolean: 'bg-amber-100 text-amber-900 dark:bg-amber-900/35 dark:text-amber-300',
    array: 'bg-violet-100 text-violet-800 dark:bg-violet-900/35 dark:text-violet-300',
    object: 'bg-orange-100 text-orange-900 dark:bg-orange-900/35 dark:text-orange-300'
  }
  return map[t] || 'bg-slate-200 text-slate-700 dark:bg-gray-700 dark:text-gray-300'
}

function branchAllSelected(node) {
  const leaves = node.leafPaths || []
  return leaves.length > 0 && leaves.every((p) => selectedSet.value.has(p))
}

function branchActionLabel(node) {
  return branchAllSelected(node) ? 'Quitar toda la rama' : 'Seleccionar toda la rama'
}

function onBranchClick(node) {
  const paths = node.leafPaths || []
  if (!paths.length) return
  const select = !branchAllSelected(node)
  emit('branch-toggle', { paths, select })
}
</script>

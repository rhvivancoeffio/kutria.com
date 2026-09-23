<template>
  <div
    class="doc-json-schema mt-4 rounded-xl overflow-hidden border border-slate-200 dark:border-slate-700/90 bg-white dark:bg-slate-900/40 shadow-md ring-1 ring-slate-900/[0.04] dark:ring-white/[0.06]"
  >
    <div
      class="doc-json-schema__head flex flex-wrap items-center justify-between gap-2 gap-y-2 px-3 sm:px-4 py-2.5 bg-gradient-to-r from-slate-800 via-slate-800 to-slate-900 dark:from-slate-950 dark:via-slate-900 dark:to-slate-950 text-white"
    >
      <div class="flex items-center gap-2 min-w-0 flex-1">
        <span class="shrink-0 flex h-8 w-8 items-center justify-center rounded-lg bg-white/10 text-slate-100" aria-hidden="true">
          <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h7" />
          </svg>
        </span>
        <div class="min-w-0">
          <p class="text-sm font-semibold tracking-tight leading-tight">
            JSON Schema
          </p>
          <p v-if="subtitle" class="text-[11px] text-slate-300/95 truncate leading-snug mt-0.5 font-medium">
            {{ subtitle }}
          </p>
        </div>
      </div>
      <div class="flex items-center gap-2 shrink-0">
        <span
          v-if="draftBadge"
          class="hidden sm:inline rounded-md bg-white/10 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-slate-200"
        >
          {{ draftBadge }}
        </span>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-lg bg-white/15 px-2.5 py-1.5 text-xs font-medium text-white hover:bg-white/25 focus:outline-none focus-visible:ring-2 focus-visible:ring-white/70 transition-colors"
          @click="copyJson"
        >
          <svg v-if="!copied" class="h-3.5 w-3.5 shrink-0 opacity-90" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
          </svg>
          <svg v-else class="h-3.5 w-3.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          {{ copied ? 'Copiado' : 'Copiar JSON' }}
        </button>
      </div>
    </div>
    <div
      class="doc-json-schema__body json-viewer-doc-schema p-3 sm:p-4 bg-slate-50/90 dark:bg-slate-950/50 max-h-[min(70vh,36rem)] overflow-auto overscroll-contain"
    >
      <JsonViewer
        :value="schema"
        :theme="jsonViewerTheme"
        boxed
        :expand-depth="expandDepth"
        expanded
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onUnmounted } from 'vue'
import { JsonViewer } from 'vue3-json-viewer'
import 'vue3-json-viewer/dist/vue3-json-viewer.css'
import { useTheme } from '../../../composables/useTheme'

const props = defineProps({
  /** Objeto JSON Schema (referencia en docs.json) */
  schema: { type: Object, required: true },
  /** Profundidad inicial expandida (objetos anidados, $defs) */
  expandDepth: { type: Number, default: 8 }
})

const { theme } = useTheme()
const jsonViewerTheme = computed(() => (theme.value === 'dark' ? 'dark' : 'light'))

const subtitle = computed(() => {
  const t = props.schema?.title
  return typeof t === 'string' && t.trim() ? t.trim() : ''
})

const draftBadge = computed(() => {
  const s = props.schema?.$schema
  if (typeof s !== 'string') return ''
  if (s.includes('2020-12')) return 'Draft 2020-12'
  if (s.includes('draft-07')) return 'Draft-07'
  return 'Schema'
})

const copied = ref(false)
let copyTimer

async function copyJson() {
  try {
    const text = JSON.stringify(props.schema, null, 2)
    await navigator.clipboard.writeText(text)
    copied.value = true
    clearTimeout(copyTimer)
    copyTimer = setTimeout(() => { copied.value = false }, 2000)
  } catch {
    copied.value = false
  }
}

onUnmounted(() => {
  clearTimeout(copyTimer)
})
</script>

<style scoped>
.json-viewer-doc-schema {
  min-height: 0;
}
.json-viewer-doc-schema :deep(.jv-container) {
  font-size: 12px;
  line-height: 1.45;
  background: transparent !important;
}
.json-viewer-doc-schema :deep(.jv-code) {
  padding: 0;
}
.json-viewer-doc-schema :deep(.jv-key) {
  font-size: 12px;
}
.json-viewer-doc-schema :deep(.jv-item) {
  letter-spacing: 0.01em;
}
</style>

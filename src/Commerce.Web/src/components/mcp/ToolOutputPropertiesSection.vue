<template>
  <!-- Solo manual, sin tarjeta exterior (p. ej. edición de tool sin paths) -->
  <template v-if="unwrapManualOnly && !hasSchemaPicker">
    <p v-if="manualTopHint" class="text-xs text-gray-500 dark:text-gray-400">{{ manualTopHint }}</p>
    <ManualOutputPathsFormArray
      v-model="manualPathsProxy"
      :title="title"
      :hint="manualHint"
    />
  </template>

  <!-- Batch / fallback: sin paths ni manual (misma UX que tarjeta principal: Preview / Expandir) -->
  <template v-else-if="isFallbackOnly">
    <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/30">
      <div class="px-3 py-2 text-xs font-semibold text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/70 flex items-center justify-between gap-2 flex-wrap">
        <span>{{ title }}</span>
        <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
          <template v-if="enablePreviewToolbar && !expandedOverlay">
            <button
              type="button"
              class="text-xs font-medium transition-colors"
              :class="inlinePreviewOpen ? 'text-primary-800 dark:text-primary-200 underline' : 'text-primary-600 dark:text-primary-400 hover:underline'"
              @click="inlinePreviewOpen = !inlinePreviewOpen"
            >
              Preview
            </button>
            <button
              type="button"
              class="text-xs font-medium text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 inline-flex items-center gap-1"
              @click="expandedOverlay = true"
            >
              <svg class="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
              </svg>
              Expandir
            </button>
          </template>
          <span v-if="enablePreviewToolbar && expandedOverlay" class="text-[11px] text-amber-700 dark:text-amber-400">Vista expandida</span>
        </div>
      </div>
      <template v-if="!expandedOverlay">
        <div class="p-3 space-y-3">
          <div
            v-if="readonlyFallbackPreview"
            class="rounded-lg border border-gray-200 dark:border-gray-600 bg-slate-50 dark:bg-gray-900/40 overflow-hidden"
          >
            <pre class="p-3 m-0 text-xs font-mono text-gray-800 dark:text-gray-200 overflow-x-auto max-h-40 overflow-y-auto whitespace-pre-wrap">{{ readonlyFallbackPreview }}</pre>
          </div>
          <p v-else class="text-sm text-gray-500 dark:text-gray-400">{{ emptyFallbackMessage }}</p>
          <div
            v-if="enablePreviewToolbar && inlinePreviewOpen"
            class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden bg-slate-50 dark:bg-gray-900/40"
          >
            <div class="px-2.5 py-1.5 text-[11px] font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-white/80 dark:bg-gray-800/60 flex flex-wrap items-center justify-between gap-2">
              <span>Preview — propiedades seleccionadas</span>
              <label class="inline-flex items-center gap-1.5 font-sans font-normal cursor-pointer text-[10px] text-gray-600 dark:text-gray-400 shrink-0">
                <input
                  v-model="previewFullSchemaProxy"
                  type="checkbox"
                  class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
                />
                JSON Schema completo
              </label>
            </div>
            <pre class="p-2.5 m-0 text-[11px] font-mono leading-relaxed text-gray-800 dark:text-gray-200 overflow-x-auto max-h-52 overflow-y-auto whitespace-pre-wrap break-words">{{ previewText }}</pre>
          </div>
        </div>
      </template>
      <div v-else class="px-3 py-3 text-xs text-gray-500 dark:text-gray-400 leading-relaxed">
        Estás editando en la vista expandida. Cierra ese panel para volver a esta vista.
      </div>
    </div>
  </template>

  <!-- Tarjeta principal: árbol, campos, paths o manual envuelto -->
  <template v-else>
    <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/30">
      <div class="px-3 py-2 text-xs font-semibold text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/70 flex items-center justify-between gap-2 flex-wrap">
        <span>{{ title }}</span>
        <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
          <div
            v-if="showFlatTodoNinguno"
            class="flex items-center gap-2"
          >
            <button type="button" class="text-primary-600 dark:text-primary-400 hover:underline font-medium text-xs" @click="selectAllOutputFields">Todo</button>
            <span class="text-gray-400">|</span>
            <button type="button" class="text-gray-500 dark:text-gray-400 hover:underline text-xs" @click="deselectAllOutputFields">Ninguno</button>
          </div>
          <template v-if="enablePreviewToolbar && !expandedOverlay">
            <button
              type="button"
              class="text-xs font-medium transition-colors"
              :class="inlinePreviewOpen ? 'text-primary-800 dark:text-primary-200 underline' : 'text-primary-600 dark:text-primary-400 hover:underline'"
              @click="inlinePreviewOpen = !inlinePreviewOpen"
            >
              Preview
            </button>
            <button
              type="button"
              class="text-xs font-medium text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 inline-flex items-center gap-1"
              @click="expandedOverlay = true"
            >
              <svg class="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
              </svg>
              Expandir
            </button>
          </template>
          <span v-if="enablePreviewToolbar && expandedOverlay" class="text-[11px] text-amber-700 dark:text-amber-400">Vista expandida</span>
        </div>
      </div>
      <template v-if="!expandedOverlay">
        <div class="p-3 space-y-3">
          <template v-if="schemaTree.length > 0">
            <OutputSchemaReturnTree
              :nodes="schemaTree"
              :selected-paths="selectedPathsProxy"
              @toggle-leaf="toggleOutputPath"
              @branch-toggle="handleOutputBranchToggle"
              @select-all="selectAllOutputFields"
              @select-none="deselectAllOutputFields"
            />
          </template>
          <template v-else-if="schemaFields.length > 0">
            <label
              v-for="(field, idx) in schemaFields"
              :key="field.name || idx"
              class="flex items-center gap-3 py-2.5 px-3 rounded-lg border cursor-pointer transition-colors"
              :class="selectedPathsProxy.includes(field.name)
                ? 'border-primary-500 dark:border-primary-400 bg-primary-50/60 dark:bg-primary-900/25 ring-1 ring-primary-500/30'
                : 'border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 card-hover'"
            >
              <input
                type="checkbox"
                :checked="selectedPathsProxy.includes(field.name)"
                class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 shrink-0"
                @change="toggleOutputPath(field.name)"
              />
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="text-sm font-medium text-gray-900 dark:text-white font-mono">{{ field.name }}</span>
                  <span class="px-1.5 py-0.5 text-xs rounded bg-gray-200 dark:bg-gray-600 text-gray-600 dark:text-gray-300">{{ field.type }}</span>
                </div>
                <p v-if="field.description" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 line-clamp-2">{{ field.description }}</p>
              </div>
            </label>
          </template>
          <template v-else-if="availablePaths.length > 0">
            <p v-if="pathListVariant === 'inline' && pathDescription" class="text-xs text-gray-500 dark:text-gray-400">{{ pathDescription }}</p>
            <div
              :class="pathListVariant === 'inline'
                ? 'flex flex-wrap gap-2 max-h-48 overflow-y-auto'
                : 'space-y-2 max-h-40 overflow-y-auto'"
            >
              <label
                v-for="path in availablePaths"
                :key="path"
                class="cursor-pointer transition-colors"
                :class="pathLabelClasses(path)"
              >
                <input
                  type="checkbox"
                  :checked="selectedPathsProxy.includes(path)"
                  class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
                  @change="toggleOutputPath(path)"
                />
                <span
                  class="text-sm font-mono"
                  :class="pathListVariant === 'inline' ? 'text-gray-800 dark:text-gray-200' : 'text-gray-900 dark:text-white'"
                >{{ path }}</span>
              </label>
            </div>
          </template>
          <template v-else-if="allowManualWhenEmpty">
            <p class="text-xs text-gray-500 dark:text-gray-400 py-2">
              <template v-if="manualIntro">{{ manualIntro }}</template>
              <template v-else>
                No hay propiedades derivadas del OpenAPI. Indica a mano las propiedades que debe devolver el tool (ej. <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">id</code>, <code class="px-1 rounded bg-gray-200 dark:bg-gray-600">data.items</code>).
              </template>
            </p>
            <ManualOutputPathsFormArray
              v-model="manualPathsProxy"
              :title="''"
              :hint="manualHint"
              :show-header="false"
            />
          </template>
          <div
            v-if="enablePreviewToolbar && inlinePreviewOpen"
            class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden bg-slate-50 dark:bg-gray-900/40"
          >
            <div class="px-2.5 py-1.5 text-[11px] font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-white/80 dark:bg-gray-800/60 flex flex-wrap items-center justify-between gap-2">
              <span>Preview — propiedades seleccionadas</span>
              <label class="inline-flex items-center gap-1.5 font-sans font-normal cursor-pointer text-[10px] text-gray-600 dark:text-gray-400 shrink-0">
                <input
                  v-model="previewFullSchemaProxy"
                  type="checkbox"
                  class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
                />
                JSON Schema completo
              </label>
            </div>
            <pre class="p-2.5 m-0 text-[11px] font-mono leading-relaxed text-gray-800 dark:text-gray-200 overflow-x-auto max-h-52 overflow-y-auto whitespace-pre-wrap break-words">{{ previewText }}</pre>
          </div>
        </div>
      </template>
      <div v-else class="px-3 py-3 text-xs text-gray-500 dark:text-gray-400 leading-relaxed">
        Estás editando en la vista expandida. Cierra ese panel para volver a esta vista.
      </div>
    </div>
  </template>

  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="enablePreviewToolbar && expandedOverlay && expandedTeleportVisible"
        class="fixed inset-0 z-[110] flex items-stretch justify-center sm:p-3"
      >
        <div class="absolute inset-0 bg-black/60" aria-hidden="true" />
        <div
          class="relative flex flex-col w-full max-w-5xl h-full sm:max-h-[min(92vh,880px)] sm:my-auto bg-white dark:bg-gray-800 sm:rounded-xl shadow-2xl border border-gray-200 dark:border-gray-600 overflow-hidden"
          role="dialog"
          :aria-labelledby="expandedTitleId"
          @click.stop
        >
          <div class="shrink-0 flex items-center justify-between gap-2 px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900/50">
            <h3 :id="expandedTitleId" class="text-sm font-semibold text-gray-900 dark:text-white">
              {{ title }}
            </h3>
            <div class="flex items-center gap-3">
              <div
                v-if="schemaTree.length === 0 && (schemaFields.length > 0 || availablePaths.length > 0)"
                class="flex items-center gap-2 text-xs"
              >
                <button type="button" class="text-primary-600 dark:text-primary-400 hover:underline font-medium" @click="selectAllOutputFields">Todo</button>
                <span class="text-gray-400">|</span>
                <button type="button" class="text-gray-500 dark:text-gray-400 hover:underline" @click="deselectAllOutputFields">Ninguno</button>
              </div>
              <button
                type="button"
                class="p-2 rounded-lg text-gray-500 hover:text-gray-800 dark:hover:text-gray-200 hover:bg-gray-200 dark:hover:bg-gray-700"
                title="Cerrar vista expandida"
                @click="expandedOverlay = false"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          </div>
          <div class="flex-1 flex flex-col md:flex-row min-h-0">
            <div class="flex flex-col min-h-0 md:w-1/2 md:max-w-[50%] border-b md:border-b-0 md:border-r border-gray-200 dark:border-gray-600 min-h-[42vh] md:min-h-0">
              <div class="shrink-0 px-3 py-1.5 text-[11px] font-medium text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/80">
                Schema (JSON)
              </div>
              <div class="flex-1 min-h-0 overflow-hidden p-3 flex flex-col">
                <template v-if="schemaTree.length > 0">
                  <OutputSchemaReturnTree
                    class="min-h-0 flex-1"
                    :nodes="schemaTree"
                    :selected-paths="selectedPathsProxy"
                    unconstrained
                    @toggle-leaf="toggleOutputPath"
                    @branch-toggle="handleOutputBranchToggle"
                    @select-all="selectAllOutputFields"
                    @select-none="deselectAllOutputFields"
                  />
                </template>
                <template v-else-if="schemaFields.length > 0">
                  <div class="space-y-1.5 flex-1 min-h-0 overflow-y-auto">
                    <label
                      v-for="(field, idx) in schemaFields"
                      :key="'exp-' + (field.name || idx)"
                      class="flex items-center gap-3 py-2.5 px-3 rounded-lg border cursor-pointer transition-colors"
                      :class="selectedPathsProxy.includes(field.name)
                        ? 'border-primary-500 dark:border-primary-400 bg-primary-50/60 dark:bg-primary-900/25 ring-1 ring-primary-500/30'
                        : 'border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30'"
                    >
                      <input
                        type="checkbox"
                        :checked="selectedPathsProxy.includes(field.name)"
                        class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 shrink-0"
                        @change="toggleOutputPath(field.name)"
                      />
                      <div class="min-w-0 flex-1">
                        <div class="flex items-center gap-2 flex-wrap">
                          <span class="text-sm font-medium text-gray-900 dark:text-white font-mono">{{ field.name }}</span>
                          <span class="px-1.5 py-0.5 text-xs rounded bg-gray-200 dark:bg-gray-600 text-gray-600 dark:text-gray-300">{{ field.type }}</span>
                        </div>
                        <p v-if="field.description" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 line-clamp-2">{{ field.description }}</p>
                      </div>
                    </label>
                  </div>
                </template>
                <template v-else-if="availablePaths.length > 0">
                  <div class="space-y-1.5 flex-1 min-h-0 overflow-y-auto">
                    <label
                      v-for="path in availablePaths"
                      :key="'exp-path-' + path"
                      class="flex items-center gap-2 p-2 rounded-lg border border-gray-200 dark:border-gray-600 cursor-pointer transition-colors"
                      :class="selectedPathsProxy.includes(path)
                        ? 'ring-1 ring-primary-500 border-primary-500 dark:border-primary-400 bg-primary-50/30 dark:bg-primary-900/20'
                        : 'hover:border-primary-400 dark:hover:border-primary-500'"
                    >
                      <input
                        type="checkbox"
                        :checked="selectedPathsProxy.includes(path)"
                        class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
                        @change="toggleOutputPath(path)"
                      />
                      <span class="text-sm font-mono text-gray-900 dark:text-white">{{ path }}</span>
                    </label>
                  </div>
                </template>
                <template v-else-if="showExpandedReadonlySchema">
                  <pre class="flex-1 min-h-0 overflow-auto p-2 m-0 text-[11px] sm:text-xs font-mono text-gray-800 dark:text-gray-200 whitespace-pre-wrap break-words">{{ readonlyFallbackPreview }}</pre>
                </template>
                <template v-else-if="allowManualWhenEmpty">
                  <p class="text-xs text-gray-500 dark:text-gray-400 pb-2 shrink-0">
                    <template v-if="manualIntro">{{ manualIntro }}</template>
                    <template v-else>Propiedades manuales (sin schema OpenAPI).</template>
                  </p>
                  <div class="space-y-2 flex-1 min-h-0 overflow-y-auto">
                    <ManualOutputPathsFormArray
                      v-model="manualPathsProxy"
                      :title="''"
                      :hint="manualHint"
                      :show-header="false"
                    />
                  </div>
                </template>
                <template v-else>
                  <p class="text-xs text-gray-500 dark:text-gray-400 p-2">{{ emptyFallbackMessage }}</p>
                </template>
              </div>
            </div>
            <div class="flex flex-col min-h-0 flex-1 md:w-1/2 md:max-w-[50%] min-h-[38vh] md:min-h-0 bg-slate-50 dark:bg-gray-900/30">
              <div class="shrink-0 px-3 py-1.5 text-[11px] font-medium text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/80 flex flex-wrap items-center justify-between gap-2">
                <span>Preview — propiedades seleccionadas</span>
                <label class="inline-flex items-center gap-1.5 font-sans font-normal cursor-pointer text-[10px] text-gray-600 dark:text-gray-400 shrink-0">
                  <input
                    v-model="previewFullSchemaProxy"
                    type="checkbox"
                    class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500"
                  />
                  JSON Schema completo
                </label>
              </div>
              <pre class="flex-1 min-h-0 overflow-auto p-3 m-0 text-[11px] sm:text-xs font-mono leading-relaxed text-gray-800 dark:text-gray-200 whitespace-pre-wrap break-words">{{ previewText }}</pre>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed } from 'vue'
import OutputSchemaReturnTree from '../workflow/OutputSchemaReturnTree.vue'
import ManualOutputPathsFormArray from './ManualOutputPathsFormArray.vue'

const props = defineProps({
  title: { type: String, default: 'Propiedades a devolver' },
  /** Árbol anidado (prioridad sobre schemaFields y availablePaths) */
  schemaTree: { type: Array, default: () => [] },
  /** Lista aplanada { name, type, description } */
  schemaFields: { type: Array, default: () => [] },
  /** Paths planos (batch / edición tool) */
  availablePaths: { type: Array, default: () => [] },
  pathListVariant: { type: String, default: 'stacked' },
  /** Texto bajo la lista en variante inline */
  pathDescription: { type: String, default: '' },
  selectedPaths: { type: Array, default: () => [] },
  manualPaths: { type: Array, default: () => [] },
  manualHint: {
    type: String,
    default: 'Indica path y tipo (ej. id, data.name). Para array: path + tipo de elemento; para objeto anidado: address.street, address.city.'
  },
  manualTopHint: { type: String, default: '' },
  /** Texto introductorio sobre el formulario manual (sustituye el párrafo por defecto de OpenAPI). */
  manualIntro: { type: String, default: '' },
  allowManualWhenEmpty: { type: Boolean, default: true },
  /** Sin paths OpenAPI: mostrar solo ManualOutputPathsFormArray (sin tarjeta “Propiedades a devolver”) */
  unwrapManualOnly: { type: Boolean, default: false },
  enablePreviewToolbar: { type: Boolean, default: false },
  previewText: { type: String, default: '' },
  inlinePreviewOpen: { type: Boolean, default: false },
  previewFullSchema: { type: Boolean, default: true },
  expandedOverlay: { type: Boolean, default: false },
  expandedTeleportVisible: { type: Boolean, default: true },
  readonlyFallbackPreview: { type: String, default: '' },
  emptyFallbackMessage: { type: String, default: 'Sin output schema definido' }
})

function pathLabelClasses(path) {
  const on = props.selectedPaths?.includes(path)
  if (props.pathListVariant === 'inline') {
    return on
      ? 'inline-flex items-center gap-2 px-3 py-2 rounded-lg border ring-1 ring-primary-500 border-primary-500 dark:border-primary-400 bg-primary-50/30 dark:bg-primary-900/20'
      : 'inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 hover:border-gray-300 dark:hover:border-gray-500'
  }
  return on
    ? 'flex items-center gap-2 p-2 rounded-lg border ring-1 ring-primary-500 border-primary-500 dark:border-primary-400 bg-primary-50/30 dark:bg-primary-900/20'
    : 'flex items-center gap-2 p-2 rounded-lg border border-gray-200 dark:border-gray-600 hover:border-primary-400 dark:hover:border-primary-500'
}

const emit = defineEmits([
  'update:selectedPaths',
  'update:manualPaths',
  'update:inlinePreviewOpen',
  'update:previewFullSchema',
  'update:expandedOverlay'
])

const expandedTitleId = `tool-output-expanded-${Math.random().toString(36).slice(2, 9)}`

const selectedPathsProxy = computed({
  get: () => props.selectedPaths || [],
  set: (v) => emit('update:selectedPaths', v)
})

const manualPathsProxy = computed({
  get: () => props.manualPaths || [],
  set: (v) => emit('update:manualPaths', v)
})

const inlinePreviewOpen = computed({
  get: () => props.inlinePreviewOpen,
  set: (v) => emit('update:inlinePreviewOpen', v)
})

const previewFullSchemaProxy = computed({
  get: () => props.previewFullSchema,
  set: (v) => emit('update:previewFullSchema', v)
})

const expandedOverlay = computed({
  get: () => props.expandedOverlay,
  set: (v) => emit('update:expandedOverlay', v)
})

const hasSchemaPicker = computed(() =>
  (props.schemaTree && props.schemaTree.length > 0) ||
  (props.schemaFields && props.schemaFields.length > 0) ||
  (props.availablePaths && props.availablePaths.length > 0)
)

const isFallbackOnly = computed(() =>
  !hasSchemaPicker.value &&
  !props.allowManualWhenEmpty &&
  !props.unwrapManualOnly
)

/** Panel izquierdo del modal expandido: solo schema de solo lectura (batch sin paths). */
const showExpandedReadonlySchema = computed(() =>
  !props.schemaTree?.length &&
  !props.schemaFields?.length &&
  !props.availablePaths?.length &&
  !props.allowManualWhenEmpty &&
  !!(props.readonlyFallbackPreview && String(props.readonlyFallbackPreview).trim())
)

const showFlatTodoNinguno = computed(() => {
  if (props.expandedOverlay) return false
  if (props.schemaTree?.length > 0) return false
  if (props.schemaFields?.length > 0) return true
  if (props.availablePaths?.length > 0) return true
  return false
})

function toggleOutputPath(path) {
  const list = [...(props.selectedPaths || [])]
  const idx = list.indexOf(path)
  if (idx >= 0) {
    emit('update:selectedPaths', list.filter(p => p !== path))
  } else {
    emit('update:selectedPaths', [...list, path])
  }
}

function selectAllOutputFields() {
  if (props.schemaFields?.length > 0) {
    emit('update:selectedPaths', props.schemaFields.map(f => f.name))
    return
  }
  if (props.availablePaths?.length > 0) {
    emit('update:selectedPaths', [...props.availablePaths])
  }
}

function deselectAllOutputFields() {
  emit('update:selectedPaths', [])
}

function handleOutputBranchToggle({ paths, select }) {
  const list = paths || []
  if (!list.length) return
  const cur = [...(props.selectedPaths || [])]
  if (select) {
    emit('update:selectedPaths', [...new Set([...cur, ...list])])
  } else {
    const remove = new Set(list)
    emit('update:selectedPaths', cur.filter(p => !remove.has(p)))
  }
}
</script>

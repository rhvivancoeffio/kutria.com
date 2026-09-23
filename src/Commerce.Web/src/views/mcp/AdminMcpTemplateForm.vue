<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6">
      <div v-if="loading" class="space-y-4">
        <div class="h-8 w-48 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        <div class="h-64 bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse" />
      </div>

      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <p class="text-red-800 dark:text-red-300">{{ error }}</p>
        <router-link :to="backUrl" class="mt-2 inline-block text-sm text-primary-600 dark:text-primary-400">← Volver a plantillas</router-link>
      </div>

      <div v-else class="space-y-6">
        <div class="space-y-1">
          <router-link :to="backUrl" class="text-sm text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400">← Volver a plantillas</router-link>
          <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ isEdit ? 'Editar plantilla' : 'Nueva plantilla' }}</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400">Mensajes por rol y argumentos, igual que en el editor de prompt. Al crear un nuevo prompt puedes elegir esta plantilla como base.</p>
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <form @submit.prevent="submitForm" class="p-4 sm:p-6 space-y-6">
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
                <input v-model.trim="form.name" type="text" required placeholder="ej: Soporte básico" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Título (opcional)</label>
                <input v-model.trim="form.title" type="text" placeholder="Título del prompt" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción (opcional)</label>
                <input v-model.trim="form.description" type="text" placeholder="Breve descripción" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
              </div>
            </div>

            <!-- MCP para cargar tools (opcional): permite asociar tools por bloque tipo Tool -->
            <div class="rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/30 dark:bg-gray-800/30 p-4">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">MCP para tools (opcional)</label>
              <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">Elige un server MCP para poder asignar tools a los bloques con rol Tool. Al usar la plantilla en un prompt, se respetarán las tools seleccionadas si coinciden con ese MCP.</p>
              <select
                v-model="selectedMcpId"
                class="w-full max-w-md px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
              >
                <option value="">— Sin MCP —</option>
                <option v-for="m in mcpsList" :key="m.id" :value="m.id">{{ m.name }}</option>
              </select>
            </div>

            <!-- Split view: Mensajes por rol | Argumentos — igual que MCP Prompt -->
            <div class="grid grid-cols-1 lg:grid-cols-[3fr_2fr] gap-4 lg:gap-6 lg:items-start">
              <div class="flex flex-col min-h-0 rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/30 dark:bg-gray-800/30 overflow-hidden">
                <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Mensajes por rol <span class="text-red-500">*</span></label>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Orden: system, user, assistant, tool. Arrastra para reordenar. Usa <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600">&#123;&#123;nombre&#125;&#125;</code> para argumentos; al escribir <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600">&#123;&#123;...&#125;&#125;</code> se crea el argumento automáticamente.</p>
                </div>
                <div class="p-3 flex-1 min-h-[280px] lg:min-h-[360px] overflow-y-auto space-y-3">
                  <div
                    v-for="(block, idx) in messageBlocks"
                    :key="idx"
                    class="p-3 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700/50 transition-colors"
                    :class="{ 'opacity-50': dragFromIndex === idx, 'ring-2 ring-primary-400 dark:ring-primary-500 border-primary-400 dark:border-primary-500': dragOverIndex === idx }"
                    @dragover.prevent="onBlockDragOver($event, idx)"
                    @dragleave="onBlockDragLeave(idx)"
                    @drop.prevent="onBlockDrop($event, idx)"
                  >
                    <div class="flex items-center justify-between gap-2 mb-2">
                      <div class="flex items-center gap-2 shrink-0">
                        <span
                          draggable="true"
                          class="p-1.5 rounded cursor-grab active:cursor-grabbing text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 touch-none"
                          title="Arrastrar para reordenar"
                          @dragstart="onBlockDragStart($event, idx)"
                          @dragend="onBlockDragEnd"
                        >
                          <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path d="M8 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm8-12a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4z" /></svg>
                        </span>
                        <select
                          v-model="block.role"
                          class="px-2.5 py-1 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                        >
                          <option value="system">System</option>
                          <option value="user">User</option>
                          <option value="assistant">Assistant</option>
                          <option value="tool">Tool</option>
                        </select>
                      </div>
                      <button type="button" @click="removeMessageBlock(idx)" class="p-1.5 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded hover:bg-gray-200 dark:hover:bg-gray-600 shrink-0" title="Quitar bloque">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                      </button>
                    </div>
                    <textarea
                      v-model="block.content"
                      rows="3"
                      :placeholder="placeholderForRole(block.role)"
                      class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                      @input="syncArgumentsFromMessageBlocks"
                    />
                    <!-- Tools permitidas (mismo flujo que MCP Prompt): siempre visible cuando rol es Tool -->
                    <div v-if="block.role === 'tool'" class="mt-2 space-y-2">
                      <template v-if="templateToolsList.length === 0">
                        <p class="text-xs text-gray-500 dark:text-gray-400">Selecciona un <strong>MCP para tools</strong> arriba para poder agregar o quitar tools en este bloque.</p>
                      </template>
                      <template v-else>
                        <div class="flex flex-wrap items-center gap-1.5">
                          <span class="text-xs text-gray-500 dark:text-gray-400 shrink-0">Seleccionadas (este bloque):</span>
                          <template v-if="(getBlockAllowedToolIds(block) || []).length === 0">
                            <span class="text-xs text-gray-500 dark:text-gray-400 italic">Todas las tools del MCP</span>
                          </template>
                          <template v-else>
                            <span
                              v-for="tid in (getBlockAllowedToolIds(block) || [])"
                              :key="tid"
                              class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 text-xs text-gray-700 dark:text-gray-300"
                            >
                              {{ getToolNameById(tid) }}
                              <button type="button" @click="removeBlockAllowedTool(idx, tid)" class="p-0.5 -mr-0.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-500 hover:text-red-600 dark:hover:text-red-400" title="Quitar tool">×</button>
                            </span>
                          </template>
                        </div>
                        <div>
                          <button type="button" @click="openAllowedToolsModalForBlock(idx)" class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg border-2 border-primary-500 dark:border-primary-400 text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/30 font-medium text-xs transition-colors">
                            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                            Agregar tools permitidos
                          </button>
                        </div>
                      </template>
                    </div>
                  </div>
                  <button type="button" @click="addMessageBlock" class="w-full py-2 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors">
                    + Añadir mensaje
                  </button>
                </div>
                <div class="px-4 py-2 border-t border-gray-200 dark:border-gray-600 shrink-0 bg-white/50 dark:bg-gray-800/50">
                  <p v-if="messageBlocksTouched && !hasValidMessageBlocks" class="text-xs text-red-500">Al menos un bloque debe tener contenido no vacío.</p>
                </div>
              </div>

              <ArgumentsFormArray
                v-model="argumentsFields"
                title="Argumentos"
                hint="En los mensajes usa <code class='px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600'>&#123;&#123;nombre&#125;&#125;</code>. Al escribir &#123;&#123;...&#125;&#125; se añade aquí."
                item-label="Argumento"
                add-button-label="+ Añadir argumento"
              />
            </div>

            <!-- Tools permitidas por bloque (resumen) -->
            <div v-if="templateToolsList.length > 0 && hasToolRoleBlock" class="p-4 rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tools permitidas por bloque (resumen)</label>
              <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">Cada bloque con rol Tool tiene su propia lista. Para cambiar, usa el botón «Agregar tools permitidos» en ese bloque.</p>
              <div class="space-y-2">
                <template v-for="(block, idx) in messageBlocks" :key="idx">
                  <div v-if="block.role === 'tool'" class="flex flex-wrap items-center gap-2 py-1.5 px-2 rounded-lg bg-white dark:bg-gray-800/50 border border-gray-200 dark:border-gray-600">
                    <span class="text-xs font-medium text-gray-600 dark:text-gray-400 shrink-0">Bloque {{ idx + 1 }} (Tool):</span>
                    <template v-if="(getBlockAllowedToolIds(block) || []).length === 0">
                      <span class="text-xs text-gray-500 dark:text-gray-400 italic">Todas las tools</span>
                    </template>
                    <template v-else>
                      <span
                        v-for="tid in (getBlockAllowedToolIds(block) || [])"
                        :key="tid"
                        class="inline-flex items-center px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs text-gray-700 dark:text-gray-300"
                      >
                        {{ getToolNameById(tid) }}
                      </span>
                    </template>
                  </div>
                </template>
              </div>
            </div>

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-3 pt-2 border-t border-gray-200 dark:border-gray-700">
              <router-link :to="backUrl" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 text-center">Cancelar</router-link>
              <button type="submit" :disabled="saving || !formValid" :class="['w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors', (saving || !formValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']">
                {{ saving ? 'Guardando…' : 'Guardar' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </main>

    <!-- Slider: seleccionar tools permitidas por bloque -->
    <Teleport to="body">
      <Transition name="allowed-tools-slider">
        <div v-if="showAllowedToolsModal" class="fixed inset-0 z-50 flex">
          <div class="absolute inset-0 bg-black/50" />
          <div class="relative ml-auto w-full max-w-lg h-full bg-white dark:bg-gray-800 shadow-xl flex flex-col animate-slide-in-panel" @click.stop>
            <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
              <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Tools permitidas{{ allowedToolsModalBlockIndex != null ? ` (Bloque ${allowedToolsModalBlockIndex + 1})` : '' }}</h3>
              <button type="button" @click="showAllowedToolsModal = false" class="p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700" aria-label="Cerrar">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
              <p class="text-xs text-gray-500 dark:text-gray-400">Marca las tools que este bloque podrá usar. Sin selección = todas permitidas.</p>
              <div class="mt-3 flex items-center gap-2">
                <input
                  v-model="allowedToolsModalSearch"
                  type="text"
                  placeholder="Buscar por nombre..."
                  class="flex-1 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
                />
                <div class="flex gap-2 shrink-0">
                  <button type="button" @click="selectAllFilteredToolsInModal" class="text-xs px-2 py-1.5 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/30 font-medium">Todo</button>
                  <button type="button" @click="selectNoFilteredToolsInModal" class="text-xs px-2 py-1.5 rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700">Ninguno</button>
                </div>
              </div>
            </div>
            <div class="flex-1 min-h-0 overflow-y-auto p-4">
              <div class="space-y-1">
                <label
                  v-for="tool in filteredToolsForModal"
                  :key="tool.id"
                  class="flex items-center gap-3 px-3 py-2.5 rounded-lg border border-gray-200 dark:border-gray-600 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                  :class="{ 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20': selectedToolIdsSet.has(String(tool.id)) }"
                >
                  <input v-model="allowedToolsModalSelection" type="checkbox" :value="tool.id" class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500" />
                  <div class="min-w-0 flex-1">
                    <span class="text-sm font-medium text-gray-900 dark:text-white">{{ tool.name }}</span>
                    <p v-if="tool.description" class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ tool.description }}</p>
                  </div>
                </label>
              </div>
              <p v-if="filteredToolsForModal.length === 0" class="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">No hay tools que coincidan con la búsqueda.</p>
            </div>
            <div class="px-4 py-3 border-t border-gray-200 dark:border-gray-600 flex justify-end gap-3 shrink-0">
              <button type="button" @click="showAllowedToolsModal = false" class="px-4 py-2 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Cancelar</button>
              <button type="button" @click="applyAllowedToolsSelection(); showAllowedToolsModal = false" class="px-4 py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white">Aplicar</button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import ArgumentsFormArray from '../../components/mcp/ArgumentsFormArray.vue'
import { syncMcpArgumentsFromMessageBlocks } from '../../utils/mcpPromptArgumentSync'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const templateId = computed(() => route.params.templateId)
const isEdit = computed(() => !!templateId.value && templateId.value !== 'new')
const backUrl = '/admin/templates'

const loading = ref(true)
const error = ref(null)
const form = ref({ name: '', title: '', description: '' })
const messageBlocks = ref([{ role: 'user', content: '' }])
const argumentsFields = ref([{ name: '', description: '', required: false, fromTemplate: false }])
const messageBlocksTouched = ref(false)
const saving = ref(false)

const mcpsList = ref([])
const selectedMcpId = ref('')
const templateToolsList = ref([])
const dragFromIndex = ref(null)
const dragOverIndex = ref(null)
const showAllowedToolsModal = ref(false)
const allowedToolsModalBlockIndex = ref(null)
const allowedToolsModalSearch = ref('')
const allowedToolsModalSelection = ref([])

const hasValidMessageBlocks = computed(() => messageBlocks.value.some(b => (b.content ?? '').trim().length > 0))
const formValid = computed(() => !!form.value.name?.trim() && hasValidMessageBlocks.value)

const hasToolRoleBlock = computed(() => messageBlocks.value.some(b => b.role === 'tool'))
const filteredToolsForModal = computed(() => {
  const list = templateToolsList.value
  const q = (allowedToolsModalSearch.value || '').trim().toLowerCase()
  if (!q) return list
  return list.filter(t => (t.name || '').toLowerCase().includes(q) || (t.description || '').toLowerCase().includes(q))
})
const selectedToolIdsSet = computed(() => new Set((allowedToolsModalSelection.value || []).map(id => String(id))))

function placeholderForRole(role) {
  const hints = {
    system: 'Instrucciones globales para el modelo. Usa {{nombre}} para argumentos.',
    user: 'Mensaje del usuario. Ej: "Hola {{name}}, ¿cuál es el estado del pedido {{order_id}}?"',
    assistant: 'Ejemplo de respuesta del asistente. Ej: "El pedido {{order_id}} está en camino."',
    tool: 'Instrucciones sobre tools. Usa {{arg}} si depende de argumentos.'
  }
  return hints[role] ?? 'Contenido del mensaje. Usa {{nombre}} para argumentos.'
}

let mcpArgSyncTimer = null
function syncArgumentsFromMessageBlocks() {
  if (mcpArgSyncTimer != null) clearTimeout(mcpArgSyncTimer)
  mcpArgSyncTimer = setTimeout(() => {
    mcpArgSyncTimer = null
    argumentsFields.value = syncMcpArgumentsFromMessageBlocks(
      argumentsFields.value,
      messageBlocks.value
    )
  }, 400)
}

onUnmounted(() => {
  if (mcpArgSyncTimer != null) clearTimeout(mcpArgSyncTimer)
})

function addMessageBlock() {
  messageBlocks.value.push({ role: 'user', content: '' })
}
function removeMessageBlock(idx) {
  messageBlocks.value.splice(idx, 1)
  if (messageBlocks.value.length === 0) messageBlocks.value.push({ role: 'user', content: '' })
}

function onBlockDragStart(e, idx) {
  dragFromIndex.value = idx
  e.dataTransfer.effectAllowed = 'move'
  e.dataTransfer.setData('text/plain', String(idx))
}
function onBlockDragEnd() {
  dragFromIndex.value = null
  dragOverIndex.value = null
}
function onBlockDragOver(e, idx) {
  if (dragFromIndex.value === null) return
  if (dragFromIndex.value !== idx) dragOverIndex.value = idx
}
function onBlockDragLeave(idx) {
  if (dragOverIndex.value === idx) dragOverIndex.value = null
}
function onBlockDrop(e, toIndex) {
  const fromIndex = dragFromIndex.value
  dragFromIndex.value = null
  dragOverIndex.value = null
  if (fromIndex == null || fromIndex === toIndex) return
  const blocks = [...messageBlocks.value]
  const [moved] = blocks.splice(fromIndex, 1)
  blocks.splice(toIndex, 0, moved)
  messageBlocks.value = blocks
}

function getBlockAllowedToolIds(block) {
  if (!block || block.role !== 'tool') return []
  if (!Array.isArray(block.allowedToolIds)) return []
  return block.allowedToolIds
}
function ensureBlockAllowedToolIds(block) {
  if (block.role === 'tool' && !Array.isArray(block.allowedToolIds)) {
    block.allowedToolIds = []
  }
}
function getToolNameById(toolId) {
  const t = templateToolsList.value.find(tool => String(tool.id) === String(toolId))
  return t?.name ?? toolId
}
function removeBlockAllowedTool(blockIdx, toolId) {
  const block = messageBlocks.value[blockIdx]
  if (!block || block.role !== 'tool') return
  ensureBlockAllowedToolIds(block)
  block.allowedToolIds = block.allowedToolIds.filter(id => String(id) !== String(toolId))
}
function openAllowedToolsModalForBlock(blockIdx) {
  const block = messageBlocks.value[blockIdx]
  if (!block || block.role !== 'tool') return
  ensureBlockAllowedToolIds(block)
  allowedToolsModalBlockIndex.value = blockIdx
  allowedToolsModalSearch.value = ''
  allowedToolsModalSelection.value = [...block.allowedToolIds]
  showAllowedToolsModal.value = true
}
function applyAllowedToolsSelection() {
  const idx = allowedToolsModalBlockIndex.value
  if (idx == null || idx < 0) return
  const block = messageBlocks.value[idx]
  if (block && block.role === 'tool') {
    ensureBlockAllowedToolIds(block)
    block.allowedToolIds = [...allowedToolsModalSelection.value]
  }
}
function selectAllFilteredToolsInModal() {
  const ids = filteredToolsForModal.value.map(t => t.id)
  const current = new Set(allowedToolsModalSelection.value)
  ids.forEach(id => current.add(id))
  allowedToolsModalSelection.value = Array.from(current)
}
function selectNoFilteredToolsInModal() {
  const toRemove = new Set(filteredToolsForModal.value.map(t => t.id))
  allowedToolsModalSelection.value = allowedToolsModalSelection.value.filter(id => !toRemove.has(id))
}

function buildMessageBlocksJson() {
  return JSON.stringify(messageBlocks.value.map(b => {
    const role = (b.role || 'user').toLowerCase()
    const out = { role, content: (b.content ?? '').trim() }
    if (role === 'tool' && Array.isArray(b.allowedToolIds) && b.allowedToolIds.length > 0) {
      out.allowedToolIds = b.allowedToolIds
    }
    return out
  }))
}
function buildArgumentsSchemaJson() {
  const args = argumentsFields.value.filter(a => a.name?.trim())
  if (args.length === 0) return null
  return JSON.stringify(args.map(a => ({
    name: a.name.trim(),
    description: a.description?.trim() || undefined,
    required: a.required
  })))
}

function applyTemplate(tpl) {
  form.value = { name: tpl.name, title: tpl.title ?? '', description: tpl.description ?? '' }
  try {
    const raw = tpl.template
    const parsed = typeof raw === 'string' ? (raw?.trim() ? JSON.parse(raw) : []) : (raw ?? [])
    messageBlocks.value = Array.isArray(parsed) && parsed.length > 0
      ? parsed.map(b => {
          const role = (b.role ?? 'user').toLowerCase()
          const block = { role, content: b.content ?? '' }
          if (role === 'tool' && Array.isArray(b.allowedToolIds)) block.allowedToolIds = [...b.allowedToolIds]
          return block
        })
      : [{ role: 'user', content: '' }]
  } catch {
    messageBlocks.value = [{ role: 'user', content: '' }]
  }
  try {
    const schema = (tpl.argumentsSchema ?? '').trim()
    if (schema) {
      const parsed = JSON.parse(schema)
      const arr = Array.isArray(parsed) ? parsed : []
      argumentsFields.value = arr.length > 0
        ? arr.map(a => ({
            name: a.name ?? '',
            description: a.description ?? '',
            required: !!a.required,
            fromTemplate: true
          }))
        : [{ name: '', description: '', required: false, fromTemplate: false }]
    } else {
      argumentsFields.value = [{ name: '', description: '', required: false, fromTemplate: false }]
    }
  } catch {
    argumentsFields.value = [{ name: '', description: '', required: false, fromTemplate: false }]
  }
}

function resetForm() {
  form.value = { name: '', title: '', description: '' }
  messageBlocks.value = [{ role: 'user', content: '' }]
  argumentsFields.value = [{ name: '', description: '', required: false, fromTemplate: false }]
  messageBlocksTouched.value = false
}

async function loadTemplate() {
  loading.value = true
  error.value = null
  try {
    try {
      const list = await apiService.getMyMcps()
      mcpsList.value = list ?? []
    } catch {
      mcpsList.value = []
    }
    if (!isEdit.value) {
      resetForm()
      loading.value = false
      return
    }
    const res = await apiService.getMcpPromptTemplates()
    const items = res?.items ?? []
    const tpl = items.find(t => String(t.id) === String(templateId.value))
    if (!tpl) {
      error.value = 'Plantilla no encontrada'
      return
    }
    applyTemplate(tpl)
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar plantilla'
  } finally {
    loading.value = false
  }
}

async function submitForm() {
  messageBlocksTouched.value = true
  if (!formValid.value) return
  saving.value = true
  try {
    const payload = {
      name: form.value.name.trim(),
      title: form.value.title?.trim() || null,
      description: form.value.description?.trim() || null,
      template: buildMessageBlocksJson(),
      argumentsSchema: buildArgumentsSchemaJson()
    }
    if (isEdit.value) {
      await apiService.updateMcpPromptTemplate(templateId.value, payload)
      toast.success('Plantilla actualizada')
    } else {
      await apiService.addMcpPromptTemplate(payload)
      toast.success('Plantilla creada')
    }
    router.push(backUrl)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al guardar')
  } finally {
    saving.value = false
  }
}

onMounted(loadTemplate)
watch(templateId, loadTemplate)

watch(selectedMcpId, async (id) => {
  if (!id) {
    templateToolsList.value = []
    return
  }
  try {
    const tools = await apiService.getMcpTools(id)
    templateToolsList.value = tools ?? []
  } catch {
    templateToolsList.value = []
  }
})

watch(showAllowedToolsModal, (open) => {
  if (!open) allowedToolsModalBlockIndex.value = null
})
</script>

<style scoped>
.allowed-tools-slider-enter-active,
.allowed-tools-slider-leave-active {
  transition: opacity 0.2s ease;
}
.allowed-tools-slider-enter-from,
.allowed-tools-slider-leave-to {
  opacity: 0;
}
.animate-slide-in-panel {
  animation: slideInPanel 0.25s ease-out;
}
@keyframes slideInPanel {
  from {
    transform: translateX(100%);
  }
  to {
    transform: translateX(0);
  }
}
</style>

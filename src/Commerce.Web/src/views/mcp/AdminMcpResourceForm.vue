<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6">
      <div v-if="loading" class="space-y-4">
        <div class="h-8 w-48 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        <div class="h-64 bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse" />
      </div>

      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <p class="text-red-800 dark:text-red-300">{{ error }}</p>
        <router-link :to="backUrl" class="mt-2 inline-block text-sm text-primary-600 dark:text-primary-400">← Volver a server</router-link>
      </div>

      <div v-else class="space-y-6">
        <div class="space-y-1">
          <router-link :to="backUrl" class="text-sm text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400">← Volver a server</router-link>
          <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ isEdit ? 'Editar Resource' : 'Nuevo resource' }}</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400">{{ isEdit ? 'Modifica el resource definido.' : 'Datos estáticos o dinámicos accesibles por URI.' }}</p>
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <form @submit.prevent="handleSave" class="p-4 sm:p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">URI <span class="text-red-500">*</span></label>
              <input
                v-model="form.uri"
                type="text"
                required
                placeholder="gravity://mcp/xxx/resource/my-resource"
                :class="[
                  'w-full px-3 py-2 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white font-mono text-sm transition-colors',
                  uriTouched && !isUriValid
                    ? 'border-2 border-red-500 focus:border-red-500 focus:ring-red-500'
                    : 'border border-gray-300 dark:border-gray-600 focus:border-primary-500 focus:ring-primary-500'
                ]"
                @blur="uriTouched = true"
              />
              <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Formato: scheme:path (ej: gravity://mcp/xxx/resource/name, file:///path)</p>
              <p v-if="uriTouched && form.uri && !isUriValid" class="mt-1 text-xs text-red-500">El URI debe tener formato válido (scheme seguido de : y path).</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
              <input
                v-model="form.name"
                type="text"
                required
                placeholder="my_resource"
                :class="[
                  'w-full px-3 py-2 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white transition-colors',
                  nameTouched && !isNameValid
                    ? 'border-2 border-red-500 focus:border-red-500 focus:ring-red-500'
                    : 'border border-gray-300 dark:border-gray-600 focus:border-primary-500 focus:ring-primary-500'
                ]"
                @blur="nameTouched = true"
                @input="form.name = (form.name || '').replace(/[^a-zA-Z0-9_]/g, '')"
              />
              <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Alfanumérico y sin espacios (ej: my_resource)</p>
              <p v-if="nameTouched && form.name && !isNameValid" class="mt-1 text-xs text-red-500">Solo letras, números y guión bajo. Sin espacios.</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Título (opcional)</label>
              <input v-model="form.title" type="text" placeholder="Mi resource" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción (opcional)</label>
              <textarea v-model="form.description" rows="2" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">MIME Type (opcional)</label>
              <SearchableSelect
                v-model="form.mimeType"
                :options="mimeTypeOptions"
                placeholder="(ninguno)"
                search-placeholder="Buscar MIME type..."
                empty-message="No hay coincidencias"
                :allow-custom="true"
                custom-option-label="Usar"
                :always-show-empty-option="true"
                teleport
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Contenido (opcional)</label>
              <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">
                {{ form.mimeType ? `Solo se aceptan archivos ${form.mimeType}.` : 'Arrastra un archivo o escribe el contenido. Selecciona el MIME type para validar la carga.' }}
              </p>
              <div
                :class="[
                  'rounded-lg border-2 transition-colors flex flex-col items-center justify-center min-h-[140px] p-4 cursor-pointer',
                  isDragging
                    ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20'
                    : 'border-gray-300 dark:border-gray-600 hover:border-primary-400 hover:bg-gray-50 dark:hover:bg-gray-700/50'
                ]"
                @dragover.prevent="isDragging = true"
                @dragleave.prevent="isDragging = false"
                @drop.prevent="handleFileDrop"
                @click="fileInputRef?.click()"
              >
                <input ref="fileInputRef" type="file" class="hidden" :accept="fileAcceptAttribute" @change="handleFileSelect" />
                <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
                <p class="text-sm text-gray-600 dark:text-gray-400 text-center">Arrastra un archivo aquí o <span class="text-primary-600 dark:text-primary-400">selecciona</span></p>
                <p v-if="fileLoadError" class="mt-2 text-xs text-red-500">{{ fileLoadError }}</p>
              </div>
              <textarea v-model="form.content" rows="4" placeholder="O escribe el contenido manualmente..." class="mt-2 w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white font-mono text-sm" />
              <div v-if="form.content?.trim()" class="mt-3 rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50 dark:bg-gray-800/50">
                <div class="px-3 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600">Preview</div>
                <div class="p-3 max-h-64 overflow-auto">
                  <img v-if="isImagePreview" :src="previewDataUrl" alt="Preview" class="max-w-full max-h-48 object-contain rounded" />
                  <pre v-else-if="isTextPreview" class="text-xs font-mono text-gray-800 dark:text-gray-200 whitespace-pre-wrap break-words">{{ previewText }}</pre>
                  <div v-else class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2">
                    <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                    <span>Archivo cargado ({{ form.mimeType || 'binario' }})</span>
                  </div>
                </div>
              </div>
            </div>
            <div v-if="isEdit" class="flex items-center gap-3">
              <label class="relative inline-flex items-center cursor-pointer select-none">
                <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                <div class="w-12 h-7 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[3px] after:left-[3px] after:bg-white after:rounded-full after:h-6 after:w-6 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5" />
                <span class="ml-3 text-sm font-medium text-gray-700 dark:text-gray-300">Habilitado</span>
              </label>
              <p class="text-xs text-gray-500 dark:text-gray-400">Si está deshabilitado, el resource no se expondrá a clientes MCP.</p>
            </div>
            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-3 pt-2">
              <router-link :to="backUrl" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 text-center">Cancelar</router-link>
              <button type="submit" :disabled="saving || !formValid" :class="['w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors', (saving || !formValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']">Guardar</button>
            </div>
          </form>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import SearchableSelect from '../../components/SearchableSelect.vue'
import apiService from '../../services/api'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const mcpId = computed(() => route.params.mcpId)
const resourceId = computed(() => route.params.resourceId)
const isEdit = computed(() => !!resourceId.value)
const mcp = ref(null)
const loading = ref(true)
const error = ref(null)
const saving = ref(false)
const form = ref({ uri: '', name: '', title: '', description: '', mimeType: '', content: '', isEnabled: true })
const uriTouched = ref(false)
const nameTouched = ref(false)
const fileInputRef = ref(null)
const isDragging = ref(false)
const fileLoadError = ref(null)

/** MIME types equivalentes (file.type del navegador puede variar) */
const MIME_ALIASES = { 'image/jpg': 'image/jpeg' }

function normalizeMime(m) {
  if (!m) return ''
  const lower = m.toLowerCase().trim()
  return MIME_ALIASES[lower] ?? lower
}

function mimeMatches(selectedMime, fileMime) {
  if (!selectedMime?.trim()) return true
  const s = normalizeMime(selectedMime)
  const f = normalizeMime(fileMime)
  if (s === f) return true
  if (s === 'application/octet-stream') return true
  return false
}

/** accept para el input file según MIME seleccionado */
const fileAcceptAttribute = computed(() => {
  const m = form.value.mimeType?.trim()
  if (!m) return '*'
  if (m.startsWith('image/')) return m
  if (m === 'application/json') return '.json,application/json'
  if (m === 'application/xml') return '.xml,application/xml'
  if (m === 'text/plain') return '.txt,text/plain'
  if (m === 'text/html') return '.html,.htm,text/html'
  if (m === 'text/markdown') return '.md,.markdown,text/markdown'
  if (m === 'text/csv') return '.csv,text/csv'
  if (m === 'application/pdf') return '.pdf,application/pdf'
  return m
})

const TEXT_MIME_PREFIXES = ['text/', 'application/json', 'application/xml', 'application/javascript']
const isTextMime = (mime) => !mime || TEXT_MIME_PREFIXES.some(p => mime.startsWith(p) || mime === p)

function processFile(file) {
  fileLoadError.value = null
  const selectedMime = form.value.mimeType?.trim()
  const fileMime = file.type || 'application/octet-stream'
  if (selectedMime && !mimeMatches(selectedMime, fileMime)) {
    fileLoadError.value = `El archivo no coincide con el MIME type seleccionado (${selectedMime}). El archivo es: ${fileMime || 'desconocido'}.`
    toast.error(fileLoadError.value)
    return
  }
  if (!selectedMime) form.value.mimeType = fileMime
  if (isTextMime(fileMime)) {
    const reader = new FileReader()
    reader.onload = () => {
      form.value.content = reader.result
      toast.success(`Archivo "${file.name}" cargado`)
    }
    reader.readAsText(file)
  } else {
    const reader = new FileReader()
    reader.onload = () => {
      form.value.content = reader.result
      toast.success(`Archivo "${file.name}" cargado`)
    }
    reader.readAsDataURL(file)
  }
}

function handleFileDrop(e) {
  isDragging.value = false
  const file = e.dataTransfer?.files?.[0]
  if (file) processFile(file)
}

function handleFileSelect(e) {
  const file = e.target?.files?.[0]
  if (file) processFile(file)
  e.target.value = ''
}


const isImagePreview = computed(() => {
  const mime = form.value.mimeType?.toLowerCase() ?? ''
  const content = form.value.content ?? ''
  return (mime.startsWith('image/') || content.startsWith('data:image/')) && content.length > 0
})

const previewDataUrl = computed(() => {
  const c = form.value.content ?? ''
  if (c.startsWith('data:')) return c
  const mime = form.value.mimeType?.toLowerCase() ?? ''
  if (mime.startsWith('image/') && c.length > 0) return `data:${mime};base64,${c}`
  return ''
})

const isTextPreview = computed(() => {
  const mime = form.value.mimeType?.toLowerCase() ?? ''
  const content = form.value.content ?? ''
  if (!content) return false
  if (content.startsWith('data:')) return false
  return isTextMime(mime)
})

const previewText = computed(() => {
  const c = form.value.content ?? ''
  const max = 2000
  return c.length > max ? c.slice(0, max) + '\n…' : c
})

const backUrl = computed(() => `/admin/mcps/${mcpId.value}`)

/** URI válido: scheme:path (RFC 3986) - scheme = letra + alfanum/+/-/. */
const uriRegex = /^[a-zA-Z][a-zA-Z0-9+.-]*:.+/
const isUriValid = computed(() => {
  const uri = form.value.uri?.trim() ?? ''
  return uri.length > 0 && uriRegex.test(uri)
})

const nameRegex = /^[a-zA-Z0-9_]+$/
const isNameValid = computed(() => {
  const name = form.value.name?.trim() ?? ''
  return name.length > 0 && nameRegex.test(name)
})

const formValid = computed(() => isUriValid.value && isNameValid.value)

const MIME_TYPES = ['text/plain', 'text/html', 'text/markdown', 'text/csv', 'application/json', 'application/xml', 'application/octet-stream', 'image/png', 'image/jpeg', 'image/gif', 'image/svg+xml', 'application/pdf']
const mimeTypeOptions = computed(() => {
  const opts = [{ value: '', label: '(ninguno)' }, ...MIME_TYPES.map(m => ({ value: m, label: m }))]
  const current = form.value.mimeType?.trim()
  if (current && !MIME_TYPES.includes(current)) opts.push({ value: current, label: current })
  return opts
})

async function loadMcp() {
  loading.value = true
  error.value = null
  try {
    mcp.value = await apiService.getMcpById(mcpId.value)
    if (isEdit.value && mcp.value?.resources) {
      const resource = mcp.value.resources.find(r => String(r.id) === String(resourceId.value))
      if (resource) {
        form.value = {
          uri: resource.uri,
          name: resource.name,
          title: resource.title ?? '',
          description: resource.description ?? '',
          mimeType: resource.mimeType ?? '',
          content: resource.content ?? '',
          isEnabled: resource.isEnabled !== false
        }
      }
    }
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar'
  } finally {
    loading.value = false
  }
}

async function handleSave() {
  saving.value = true
  try {
    if (isEdit.value) {
      const payload = { ...form.value }
      payload.isEnabled = form.value.isEnabled
      await apiService.updateMcpResource(mcpId.value, resourceId.value, payload)
      toast.success('Resource actualizado')
    } else {
      await apiService.addMcpResource(mcpId.value, form.value)
      toast.success('Resource añadido')
    }
    router.push(backUrl.value)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al guardar')
  } finally {
    saving.value = false
  }
}

watch(() => form.value.mimeType, () => { fileLoadError.value = null })

onMounted(() => loadMcp())
</script>

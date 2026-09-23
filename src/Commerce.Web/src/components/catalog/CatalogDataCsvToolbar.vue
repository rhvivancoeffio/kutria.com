<template>
  <div v-if="pipelineIdTrimmed" class="flex flex-wrap gap-2 items-center">
    <button
      type="button"
      class="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-800 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700/60 min-h-[40px] touch-manipulation disabled:opacity-50 disabled:cursor-not-allowed"
      :disabled="busy"
      @click="onDownload"
    >
      <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
      </svg>
      Descargar CSV
    </button>
    <RouterLink
      :to="importWizardTo"
      class="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-primary-500/40 dark:border-primary-400/40 bg-primary-50/80 dark:bg-primary-900/25 text-sm font-medium text-primary-800 dark:text-primary-200 hover:bg-primary-100/90 dark:hover:bg-primary-900/40 min-h-[40px] touch-manipulation"
      @click="onImportWizardNavigate"
    >
      <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
      </svg>
      Importar (asistente)
    </RouterLink>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { RouterLink } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '@/services/api'
import { persistAdminCatalogPipelineId } from '@/composables/useCatalogPipelineContext'

const props = defineProps({
  catalogPipelineId: { type: String, default: '' },
  downloadNameBase: { type: String, default: 'catalogo' },
  /** Preselecciona el tipo de importación en el asistente: products | stocks | prices */
  importKind: { type: String, default: '' }
})

defineEmits(['imported'])

const toast = useToast()
const busy = ref(false)

const pipelineIdTrimmed = computed(() => String(props.catalogPipelineId || '').trim())

const importWizardTo = computed(() => {
  const q = {}
  const k = String(props.importKind || '').toLowerCase().trim()
  if (k === 'stocks' || k === 'prices' || k === 'products') q.importKind = k
  return { path: '/admin/data/import', query: q }
})

function onImportWizardNavigate() {
  persistAdminCatalogPipelineId(pipelineIdTrimmed.value)
}

async function onDownload() {
  if (!pipelineIdTrimmed.value) return
  busy.value = true
  try {
    const k = String(props.importKind || '').toLowerCase().trim()
    const exportKind =
      k === 'stocks' || k === 'prices' || k === 'products' ? k : 'products'
    await apiService.downloadChannelCatalogCsv(
      pipelineIdTrimmed.value,
      props.downloadNameBase,
      exportKind
    )
    toast.success('Descarga iniciada')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'No se pudo descargar el CSV')
  } finally {
    busy.value = false
  }
}
</script>

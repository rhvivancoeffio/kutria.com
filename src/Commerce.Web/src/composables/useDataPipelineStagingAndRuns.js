import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '@/services/api'
import { eventBus, PIPELINE_RUN_NOTIFICATION } from '@/utils/eventBus'

/**
 * Runs, staging pagination, «Ejecutar ahora», polling y notificaciones SSE (account stream → eventBus),
 * alineado con PipelineWizard.vue.
 */
export function useDataPipelineStagingAndRuns(pipelineIdRef) {
  const toast = useToast()

  const runNowLoading = ref(false)
  const pendingRunId = ref(null)
  const runsLoading = ref(false)
  const runs = ref([])

  const stagingRecordsLoading = ref(false)
  const stagingRecords = ref([])
  const stagingRecordsPageSize = ref(20)
  const stagingRecordsContinuationToken = ref(null)
  const stagingRecordsHasMore = ref(false)
  const stagingRecordsTokenStack = ref([])
  const stagingRecordsTokenUsedForCurrentPage = ref(null)
  const expandedStagingId = ref(null)

  const stagingRecordsCanGoPrevious = computed(() => stagingRecordsTokenStack.value.length > 0)
  const stagingRecordsCanShowPagination = computed(
    () => stagingRecordsCanGoPrevious.value || stagingRecordsHasMore.value
  )

  const runStatusLabel = (s) => {
    if (s === 0) return 'Pendiente'
    if (s === 1) return 'En ejecución'
    if (s === 2) return 'Completado'
    if (s === 3) return 'Fallido'
    return String(s)
  }

  const runDuration = (r) => {
    if (!r?.startedAt || !r?.finishedAt) return ''
    const a = new Date(r.startedAt).getTime()
    const b = new Date(r.finishedAt).getTime()
    if (Number.isNaN(a) || Number.isNaN(b) || b < a) return ''
    const sec = Math.round((b - a) / 1000)
    if (sec < 60) return `${sec}s`
    const m = Math.floor(sec / 60)
    const s = sec % 60
    return `${m}m ${s}s`
  }

  const formatDate = (d) => {
    if (!d) return '-'
    return new Date(d).toLocaleString()
  }

  const formatJson = (str) => {
    if (!str) return ''
    try {
      return JSON.stringify(JSON.parse(str), null, 2)
    } catch {
      return str
    }
  }

  async function loadRuns() {
    const id = pipelineIdRef.value
    if (!id) return
    runsLoading.value = true
    try {
      runs.value = (await apiService.getPipelineRuns(id)) ?? []
    } catch {
      runs.value = []
    } finally {
      runsLoading.value = false
    }
  }

  async function loadStagingRecords(token = undefined) {
    const id = pipelineIdRef.value
    if (!id) return
    if (token === undefined) {
      stagingRecordsTokenStack.value = []
      stagingRecordsContinuationToken.value = null
      stagingRecordsTokenUsedForCurrentPage.value = null
    }
    stagingRecordsLoading.value = true
    try {
      const params = { pageSize: stagingRecordsPageSize.value }
      const tokenToUse = token !== undefined ? token : stagingRecordsContinuationToken.value
      if (tokenToUse != null) params.continuationToken = tokenToUse
      stagingRecordsTokenUsedForCurrentPage.value = tokenToUse
      const res = await apiService.getPipelineStagingRecords(id, params)
      stagingRecords.value = res?.records ?? []
      stagingRecordsContinuationToken.value = res?.continuationToken ?? null
      stagingRecordsHasMore.value = res?.hasMore ?? false
    } catch {
      stagingRecords.value = []
      stagingRecordsHasMore.value = false
    } finally {
      stagingRecordsLoading.value = false
    }
  }

  async function stagingRecordsGoNext() {
    stagingRecordsTokenStack.value.push(stagingRecordsTokenUsedForCurrentPage.value)
    await loadStagingRecords(stagingRecordsContinuationToken.value)
  }

  async function stagingRecordsGoPrevious() {
    const prevToken = stagingRecordsTokenStack.value.pop()
    await loadStagingRecords(prevToken ?? null)
  }

  async function pollRunsUntilIdle() {
    const id = pipelineIdRef.value
    if (!id) return
    for (let i = 0; i < 45; i++) {
      await new Promise((resolve) => setTimeout(resolve, 2000))
      runs.value = (await apiService.getPipelineRuns(id)) ?? []
      const active = runs.value.some((r) => r.status === 0 || r.status === 1)
      if (!active) break
    }
    await loadStagingRecords()
  }

  async function runNow() {
    const id = pipelineIdRef.value
    if (!id) return
    runNowLoading.value = true
    pendingRunId.value = null
    try {
      const res = await apiService.runPipelineNow(id)
      if (res?.statusCode === 202 && res?.runId) {
        pendingRunId.value = res.runId
        toast.success(
          'Ejecución encolada. El worker actualizará staging al terminar; estado en «Últimas ejecuciones».'
        )
      } else {
        toast.success('Solicitud de ejecución registrada.')
      }
      await loadRuns()
      void pollRunsUntilIdle()
    } catch (err) {
      console.error('Run failed:', err)
      toast.error(err?.response?.data?.error || 'Error al ejecutar')
    } finally {
      runNowLoading.value = false
    }
  }

  function handlePipelineRunNotification(data) {
    const id = pipelineIdRef.value
    if (!data?.runId || !id) return
    if (data.pipelineId && String(data.pipelineId) !== String(id)) return
    if (pendingRunId.value && String(data.runId) !== String(pendingRunId.value)) return
    runNowLoading.value = false
    pendingRunId.value = null
    runs.value = []
    runsLoading.value = true
    apiService
      .getPipelineRuns(id)
      .then((r) => {
        runs.value = r ?? []
      })
      .finally(() => {
        runsLoading.value = false
      })
    if (data.success) {
      toast.success(`Pipeline completado. ${data.recordsProcessed ?? 0} registros procesados.`)
      void loadStagingRecords()
    } else {
      toast.error(data.errorMessage || 'El pipeline falló.')
    }
  }

  watch(
    pipelineIdRef,
    (id) => {
      if (!id) {
        runs.value = []
        stagingRecords.value = []
        expandedStagingId.value = null
        return
      }
      void loadRuns()
      void loadStagingRecords()
    },
    { immediate: true }
  )

  onMounted(() => {
    eventBus.on(PIPELINE_RUN_NOTIFICATION, handlePipelineRunNotification)
  })

  onUnmounted(() => {
    eventBus.off(PIPELINE_RUN_NOTIFICATION, handlePipelineRunNotification)
  })

  return {
    runNowLoading,
    runsLoading,
    runs,
    runNow,
    loadRuns,
    runStatusLabel,
    runDuration,
    formatDate,
    formatJson,
    stagingRecordsLoading,
    stagingRecords,
    expandedStagingId,
    loadStagingRecords,
    stagingRecordsCanGoPrevious,
    stagingRecordsCanShowPagination,
    stagingRecordsHasMore,
    stagingRecordsGoNext,
    stagingRecordsGoPrevious
  }
}

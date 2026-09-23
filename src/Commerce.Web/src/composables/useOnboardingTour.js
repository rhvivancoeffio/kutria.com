import { ref, computed } from 'vue'
import apiService from '../services/api'
import { eventBus, TOUR_PROGRESS_UPDATED } from '../utils/eventBus'

const TOUR_STORAGE_PREFIX = 'tour:onboarding:'

/**
 * Obtiene la clave de localStorage para el progreso del tour.
 * Usa accountId para soportar múltiples cuentas en el mismo navegador.
 * @param {string} accountId - ID de la cuenta (Guid)
 * @returns {string}
 */
export function getTourStorageKey(accountId) {
  if (!accountId) return ''
  return `${TOUR_STORAGE_PREFIX}${accountId}`
}

/**
 * Lee el progreso del tour desde localStorage.
 * @param {string} accountId
 * @returns {{ completedSteps: string[], currentStepIndex: number, completedAt: string | null } | null}
 */
export function getTourProgress(accountId) {
  const key = getTourStorageKey(accountId)
  if (!key) return null
  try {
    const raw = localStorage.getItem(key)
    if (!raw) return null
    return JSON.parse(raw)
  } catch {
    return null
  }
}

/**
 * Guarda el progreso del tour en localStorage.
 * @param {string} accountId
 * @param {{ completedSteps?: string[], currentStepIndex?: number, completedAt?: string | null }} progress
 */
export function setTourProgress(accountId, progress) {
  const key = getTourStorageKey(accountId)
  if (!key) return
  try {
    const existing = getTourProgress(accountId) || {}
    const merged = {
      completedSteps: progress.completedSteps ?? existing.completedSteps ?? [],
      currentStepIndex: progress.currentStepIndex ?? existing.currentStepIndex ?? 0,
      completedAt: progress.completedAt !== undefined ? progress.completedAt : existing.completedAt
    }
    localStorage.setItem(key, JSON.stringify(merged))
  } catch (e) {
    console.warn('Failed to save tour progress:', e)
  }
}

/**
 * Marca el tour como completado para la cuenta.
 * @param {string} accountId
 */
export function completeTour(accountId) {
  setTourProgress(accountId, {
    completedAt: new Date().toISOString()
  })
}

/**
 * Resetea el progreso del tour (para "Repetir tour").
 * @param {string} accountId
 */
export function resetTourProgress(accountId) {
  const key = getTourStorageKey(accountId)
  if (key) localStorage.removeItem(key)
}

/**
 * Composable para el tour de onboarding.
 * Metadata desde API, progreso en localStorage por accountId.
 * Sincroniza pasos completados con el estado real de la cuenta (usage).
 * @param {import('vue').Ref<string>|import('vue').ComputedRef<string>} [accountIdRef] - Ref/computed con el accountId (ej. desde usage.accountId)
 * @param {import('vue').Ref<{ usage?: { storesUsed?: number; openApiUsed?: number; mcpUsed?: number } }>} [usageRef] - Ref with usage to sync integration step with real state
 */
export function useOnboardingTour(accountIdRef, usageRef) {
  const tour = ref(null)
  const loading = ref(false)
  const error = ref(null)

  const accountId = computed(() => {
    const v = accountIdRef?.value
    return (typeof v === 'string' ? v : v?.toString?.() ?? '') || ''
  })

  const storageKey = computed(() => getTourStorageKey(accountId.value))

  const progressVersion = ref(0)
  const progress = computed(() => {
    progressVersion.value // dependency: re-run when invalidated
    return getTourProgress(accountId.value)
  })

  function refreshProgress() {
    progressVersion.value++
  }

  const isCompleted = computed(() => !!progress.value?.completedAt)

  const completedSteps = computed(() => progress.value?.completedSteps ?? [])

  /** Steps completed from real account state (workspace + integrations) without going through the tour UI */
  const autoCompletedSteps = computed(() => {
    const steps = []
    try {
      if (localStorage.getItem('active_workspace_id')) steps.push('workspace')
    } catch {
      /* ignore */
    }
    const u = usageRef?.value?.usage
    if (u) {
      const integrationsTotal = (u.storesUsed ?? 0) + (u.openApiUsed ?? 0) + (u.mcpUsed ?? 0)
      if (integrationsTotal > 0) steps.push('integration')
    }
    return steps
  })

  /** Union of manually marked steps and those completed from real state */
  const effectiveCompletedSteps = computed(() => {
    const manual = completedSteps.value
    const auto = autoCompletedSteps.value
    const merged = new Set([...manual, ...auto])
    // Migration from older channel-style tour ids
    if (manual.includes('channels') || manual.includes('data_pipeline') || manual.includes('team_agent') || manual.includes('mcp')) {
      merged.add('policies')
    }
    if (manual.includes('pipeline')) merged.add('billing')
    return [...merged]
  })

  const currentStepIndex = computed(() => progress.value?.currentStepIndex ?? 0)

  const currentStep = computed(() => {
    const steps = tour.value?.steps ?? []
    const idx = Math.min(currentStepIndex.value, steps.length - 1)
    return idx >= 0 ? steps[idx] : null
  })

  const shouldShowTour = computed(() => {
    if (!accountId.value || !tour.value) return false
    if (isCompleted.value) return false
    return true
  })

  const totalSteps = computed(() => (tour.value?.steps ?? []).length)

  const pendingCount = computed(() => {
    const steps = tour.value?.steps ?? []
    const completed = effectiveCompletedSteps.value
    return steps.filter(s => !completed.includes(s.id)).length
  })

  async function fetchTour() {
    loading.value = true
    error.value = null
    try {
      tour.value = await apiService.getTour('onboarding')
      return tour.value
    } catch (e) {
      error.value = e.response?.data?.error || e.message || 'Error al cargar el tour'
      return null
    } finally {
      loading.value = false
    }
  }

  function markStepCompleted(stepId) {
    if (!accountId.value) return
    const steps = tour.value?.steps ?? []
    const completed = [...new Set([...(progress.value?.completedSteps ?? []), stepId])]
    const nextIndex = steps.findIndex(s => s.id === stepId) + 1
    setTourProgress(accountId.value, {
      completedSteps: completed,
      currentStepIndex: Math.min(nextIndex, steps.length)
    })
    eventBus.emit(TOUR_PROGRESS_UPDATED)
  }

  function goToStep(index) {
    if (!accountId.value) return
    const steps = tour.value?.steps ?? []
    const clamped = Math.max(0, Math.min(index, steps.length - 1))
    setTourProgress(accountId.value, { currentStepIndex: clamped })
  }

  function complete() {
    if (!accountId.value) return
    const steps = tour.value?.steps ?? []
    const allStepIds = steps.map(s => s.id)
    setTourProgress(accountId.value, {
      completedAt: new Date().toISOString(),
      completedSteps: allStepIds,
      currentStepIndex: steps.length
    })
    eventBus.emit(TOUR_PROGRESS_UPDATED)
  }

  function reset() {
    if (accountId.value) {
      resetTourProgress(accountId.value)
      eventBus.emit(TOUR_PROGRESS_UPDATED)
    }
  }

  return {
    tour,
    loading,
    error,
    storageKey,
    progress,
    isCompleted,
    completedSteps,
    effectiveCompletedSteps,
    currentStepIndex,
    currentStep,
    shouldShowTour,
    totalSteps,
    pendingCount,
    fetchTour,
    refreshProgress,
    markStepCompleted,
    goToStep,
    complete,
    reset
  }
}

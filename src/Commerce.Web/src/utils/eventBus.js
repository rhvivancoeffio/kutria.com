/**
 * Lightweight event bus for cross-component communication (tours, workspace, integraciones, pipelines).
 */
const listeners = new Map()

export const eventBus = {
  on(event, handler) {
    const list = listeners.get(event) || []
    list.push(handler)
    listeners.set(event, list)
  },
  off(event, handler) {
    const list = listeners.get(event) || []
    listeners.set(event, list.filter((h) => h !== handler))
  },
  emit(event, ...args) {
    ;(listeners.get(event) || []).forEach((h) => h(...args))
  }
}

/** Event: onboarding tour progress was updated (step completed, tour finished). Refresh tour UI. */
export const TOUR_PROGRESS_UPDATED = 'tour-progress-updated'

/** Event: open the onboarding tour modal. */
export const OPEN_TOUR = 'open-tour'

/** Event: pipeline run completed or failed. Payload: { runId, pipelineId, success, recordsProcessed, recordsFailed, errorMessage } */
export const PIPELINE_RUN_NOTIFICATION = 'pipeline-run-notification'

/** Active workspace id/name changed (localStorage + JWT). AdminLayout refreshes header and usage. */
export const WORKSPACE_CONTEXT_CHANGED = 'workspace-context-changed'

/** Mis integraciones cambiaron (conectar, editar credenciales, etc.). Payload: `{ my: Integration[] }`. */
export const MY_INTEGRATIONS_UPDATED = 'my-integrations-updated'

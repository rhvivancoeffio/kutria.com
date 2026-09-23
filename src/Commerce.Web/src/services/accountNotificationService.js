/**
 * Global account notification service.
 * Connects to account SSE stream and shows browser notifications for incoming events.
 * Also emits to eventBus for in-app UI updates.
 */
import apiService from './api'
import { eventBus, PIPELINE_RUN_NOTIFICATION } from '../utils/eventBus'

const AUTH_TOKEN_KEY = 'auth_token'

let connection = null
let permissionAsked = false

function getNotificationTitle(type, data) {
  switch (type) {
    case 'pipeline-run-complete':
      return 'Pipeline completado'
    case 'pipeline-run-error':
      return 'Pipeline falló'
    default:
      return 'Notificación'
  }
}

function getNotificationBody(type, data) {
  switch (type) {
    case 'pipeline-run-complete':
      return `${data?.recordsProcessed ?? 0} registros procesados.`
    case 'pipeline-run-error':
      return data?.errorMessage || 'Error al ejecutar el pipeline.'
    default:
      return data ? JSON.stringify(data) : ''
  }
}

function showBrowserNotification(type, data) {
  if (!('Notification' in window)) return
  if (Notification.permission !== 'granted') return

  const title = getNotificationTitle(type, data)
  const body = getNotificationBody(type, data)

  try {
    const n = new Notification(title, {
      body,
      icon: '/gravity-logo.png',
      tag: `account-${type}-${data?.runId ?? Date.now()}`,
      requireInteraction: false
    })
    n.onclick = () => {
      window.focus()
      n.close()
    }
  } catch (e) {
    console.warn('Browser notification failed:', e)
  }
}

async function ensurePermission() {
  if (!('Notification' in window)) return false
  if (Notification.permission === 'granted') return true
  if (Notification.permission === 'denied') return false
  if (permissionAsked) return false

  permissionAsked = true
  try {
    const perm = await Notification.requestPermission()
    return perm === 'granted'
  } catch {
    return false
  }
}

function handleEvent(type, data) {
  showBrowserNotification(type, data)

  if (type === 'pipeline-run-complete' || type === 'pipeline-run-error') {
    eventBus.emit(PIPELINE_RUN_NOTIFICATION, data)
  }
}

export function startAccountNotificationService() {
  if (!localStorage.getItem(AUTH_TOKEN_KEY)) return

  if (connection?.close) {
    connection.close()
    connection = null
  }

  ensurePermission().then(() => {
    connection = apiService.createAccountStreamConnection({
      onEvent(type, data) {
        handleEvent(type, data)
      },
      onClose() {
        connection = null
      }
    })
  })
}

export function stopAccountNotificationService() {
  if (connection?.close) {
    connection.close()
    connection = null
  }
}

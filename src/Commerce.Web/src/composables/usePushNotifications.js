import { ref, computed, onMounted } from 'vue'
import {
  subscribeToPush,
  unsubscribeFromPush,
  getPushStatus
} from '../services/pushNotificationService'

/**
 * Composable for Push Notifications
 *
 * Use in /admin only. Requests permission, subscribes, and registers with backend.
 * Backend must implement:
 * - GET /api/push/vapid-public-key (or set VITE_VAPID_PUBLIC_KEY)
 * - POST /api/push/subscribe (store subscription)
 * - POST /api/push/unsubscribe (optional)
 */
export function usePushNotifications() {
  const permission = ref('default')
  const subscribed = ref(false)
  const loading = ref(false)
  const error = ref(null)

  const supported = computed(() => {
    return 'serviceWorker' in navigator && 'PushManager' in window
  })

  const canSubscribe = computed(
    () => supported.value && permission.value !== 'granted' && !subscribed.value
  )

  const isSubscribed = computed(() => subscribed.value && permission.value === 'granted')

  async function refreshStatus() {
    if (!supported.value) return
    try {
      const status = await getPushStatus()
      permission.value = status.permission || 'default'
      subscribed.value = status.subscribed
      error.value = null
    } catch (e) {
      error.value = e.message || 'Error al obtener estado'
    }
  }

  async function requestPermissionAndSubscribe() {
    if (!supported.value) {
      error.value = 'Las notificaciones push no están soportadas'
      return false
    }
    loading.value = true
    error.value = null
    try {
      const status = await getPushStatus()
      if (status.permission === 'granted' && status.subscribed) {
        subscribed.value = true
        permission.value = 'granted'
        return true
      }
      if (status.permission === 'denied') {
        permission.value = 'denied'
        error.value = 'Permiso denegado'

        return false
      }
      const permissionResult = await Notification.requestPermission()
      permission.value = permissionResult
      if (permissionResult !== 'granted') {
        error.value = permissionResult === 'denied' ? 'Permiso denegado' : 'Permiso cancelado'
        return false
      }
      await subscribeToPush()
      subscribed.value = true
      return true
    } catch (e) {
      error.value = e.message || 'Error al suscribirse'
      return false
    } finally {
      loading.value = false
    }
  }

  async function unsubscribe() {
    if (!supported.value) return
    loading.value = true
    error.value = null
    try {
      await unsubscribeFromPush()
      subscribed.value = false
    } catch (e) {
      error.value = e.message || 'Error al desuscribirse'
    } finally {
      loading.value = false
    }
  }

  onMounted(() => {
    refreshStatus()
  })

  return {
    permission,
    subscribed,
    loading,
    error,
    supported,
    canSubscribe,
    isSubscribed,
    requestPermissionAndSubscribe,
    unsubscribe,
    refreshStatus
  }
}

/**
 * Push Notification Service
 *
 * Handles Web Push subscription and registration with the backend.
 * The backend must implement POST /api/push/subscribe to store the subscription
 * and use Web Push (e.g. web-push library) to send notifications.
 *
 * Required: VAPID public key from backend (GET /api/push/vapid-public-key or env)
 */

import { api } from './api'

const SUBSCRIBE_ENDPOINT = 'push/subscribe'
const UNSUBSCRIBE_ENDPOINT = 'push/unsubscribe'
const VAPID_PUBLIC_KEY_ENDPOINT = 'push/vapid-public-key'

/**
 * Convert VAPID public key (base64url) to Uint8Array for pushManager.subscribe
 */
function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const rawData = atob(base64)
  const outputArray = new Uint8Array(rawData.length)
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i)
  }
  return outputArray
}

/**
 * Get VAPID public key from backend or env
 */
async function getVapidPublicKey() {
  const envKey = import.meta.env.VITE_VAPID_PUBLIC_KEY
  if (envKey) return envKey
  const { data } = await api.get(VAPID_PUBLIC_KEY_ENDPOINT)
  return data?.publicKey || data?.key
}

/**
 * Subscribe to push notifications and register with backend
 * @param {Object} options - { userVisibleOnly, applicationServerKey }
 * @returns {Promise<PushSubscription|null>}
 */
export async function subscribeToPush(options = {}) {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    throw new Error('Push notifications are not supported')
  }
  const registration = await navigator.serviceWorker.ready
  const vapidKey = await getVapidPublicKey()
  const applicationServerKey = urlBase64ToUint8Array(vapidKey)
  const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: options.userVisibleOnly ?? true,
    applicationServerKey
  })
  await api.post(SUBSCRIBE_ENDPOINT, subscription.toJSON())
  return subscription
}

/**
 * Unsubscribe from push and notify backend
 */
export async function unsubscribeFromPush() {
  const registration = await navigator.serviceWorker.ready
  const subscription = await registration.pushManager.getSubscription()
  if (subscription) {
    await subscription.unsubscribe()
    try {
      await api.post(UNSUBSCRIBE_ENDPOINT, subscription.toJSON())
    } catch {
      // Backend might not have the subscription
    }
  }
}

/**
 * Check if push is supported and permission status
 */
export async function getPushStatus() {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    return { supported: false, permission: null, subscribed: false }
  }
  const permission = Notification.permission
  const registration = await navigator.serviceWorker.ready
  const subscription = await registration.pushManager.getSubscription()
  return {
    supported: true,
    permission,
    subscribed: !!subscription
  }
}

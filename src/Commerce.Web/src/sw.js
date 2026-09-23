/* eslint-env serviceworker */
import { cleanupOutdatedCaches, precacheAndRoute } from 'workbox-precaching'
import { registerRoute } from 'workbox-routing'
import { NetworkOnly, CacheFirst } from 'workbox-strategies'
import { ExpirationPlugin } from 'workbox-expiration'

// Vite dev / HMR: never let the SW satisfy module graph URLs (avoids MIME / wrong body on lazy *.vue chunks).
registerRoute(
  ({ url }) =>
    url.pathname.startsWith('/src/') ||
    url.pathname.startsWith('/@') ||
    url.pathname.startsWith('/node_modules/'),
  new NetworkOnly()
)

// Precaching: assets from build
cleanupOutdatedCaches()
precacheAndRoute(self.__WB_MANIFEST)

// API and MCP: always go to network, never cache (avoid serving HTML by mistake)
// En prod el API está en otro origen (backend.meetgravity.io); en dev /api y /mcp van por proxy.
registerRoute(
  ({ url }) => url.pathname.startsWith('/api') || url.pathname.startsWith('/mcp'),
  new NetworkOnly()
)

registerRoute(
  ({ request }) => request.destination === 'image',
  new CacheFirst({
    cacheName: 'images-cache',
    plugins: [new ExpirationPlugin({ maxEntries: 100 })]
  })
)

// Prompt for update: client calls skipWaiting when user clicks "Recargar"
self.addEventListener('message', (event) => {
  if (event.data?.type === 'SKIP_WAITING') {
    self.skipWaiting()
  }
})

// Push notifications: show notification when push received
self.addEventListener('push', (event) => {
  if (!event.data) return
  let payload = { title: 'TemplateProject', body: '' }
  try {
    payload = event.data.json()
  } catch {
    payload.body = event.data.text()
  }
  const options = {
    body: payload.body || '',
    icon: '/icons/icon-192.png',
    badge: '/icons/icon-192.png',
    tag: payload.tag || 'gravity-push',
    data: payload.data || {},
    requireInteraction: payload.requireInteraction ?? false
  }
  event.waitUntil(
    self.registration.showNotification(payload.title || 'TemplateProject', options)
  )
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const url = event.notification?.data?.url || '/admin'
  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      for (const client of clientList) {
        if (client.url.includes(self.location.origin) && 'focus' in client) {
          client.navigate(url)
          return client.focus()
        }
      }
      if (self.clients.openWindow) {
        return self.clients.openWindow(url)
      }
    })
  )
})

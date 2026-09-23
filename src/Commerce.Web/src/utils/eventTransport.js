/**
 * Client transport for waitable server event-streams.
 * Source of truth: API appsettings EventStreams (GET .../event-streams/settings).
 * Values: Sse (default) | TableStorage (short-poll).
 */

let cached = null
let inflight = null

export function getEventTransport() {
  const t = cached?.transport
  if (typeof t === 'string' && t.toLowerCase() === 'tablestorage') return 'poll'
  return 'sse'
}

export function getEventPollIntervalMs() {
  const n = Number(cached?.pollIntervalMs)
  if (!Number.isFinite(n)) return 800
  return Math.min(3000, Math.max(400, Math.round(n)))
}

export function isPollEventTransport() {
  return getEventTransport() === 'poll'
}

/**
 * Load transport settings from Commerce.Api appsettings.
 * @param {{ apiBase: string, tenantPrefix: string, headers?: Record<string, string>, signal?: AbortSignal }} opts
 */
export async function loadEventStreamSettings(opts) {
  const { apiBase, tenantPrefix, headers = {}, signal } = opts
  if (cached) return cached
  if (inflight) return inflight

  inflight = (async () => {
    try {
      const res = await fetch(`${apiBase}${tenantPrefix}/event-streams/settings`, {
        method: 'GET',
        headers,
        signal
      })
      if (!res.ok) throw new Error(`settings ${res.status}`)
      const data = await res.json()
      cached = {
        transport: normalizeTransport(data?.transport),
        pollIntervalMs: getEventPollIntervalMsFrom(data?.pollIntervalMs)
      }
      return cached
    } catch {
      cached = { transport: 'Sse', pollIntervalMs: 800 }
      return cached
    } finally {
      inflight = null
    }
  })()

  return inflight
}

function normalizeTransport(raw) {
  const t = String(raw || 'Sse').trim()
  if (/^(tablestorage|poll)$/i.test(t)) return 'TableStorage'
  return 'Sse'
}

function getEventPollIntervalMsFrom(value) {
  const n = Number(value)
  if (!Number.isFinite(n)) return 800
  return Math.min(3000, Math.max(400, Math.round(n)))
}

/** Test helper / force refresh after config change without reload. */
export function clearEventStreamSettingsCache() {
  cached = null
  inflight = null
}

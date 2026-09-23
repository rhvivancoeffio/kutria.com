import { getEventPollIntervalMs } from '@/utils/eventTransport'

/**
 * Generic short-poll transport against GET /event-streams/{streamId}/events.
 *
 * @param {{
 *   start: () => Promise<{ streamId: string, [key: string]: unknown }>,
 *   eventsUrl: (streamId: string, after: string | null) => string,
 *   headers?: Record<string, string>,
 *   signal?: AbortSignal,
 *   onEvent: (payload: object) => void | Promise<void>,
 *   onStarted?: (startResult: object) => void | Promise<void>,
 *   intervalMs?: number
 * }} opts
 */
export async function runPollEventTransport(opts) {
  const {
    start,
    eventsUrl,
    headers = {},
    signal,
    onEvent,
    onStarted,
    intervalMs = getEventPollIntervalMs()
  } = opts

  const started = await start()
  const streamId = started?.streamId
  if (!streamId) throw new Error('streamId missing from start response.')
  if (onStarted) await onStarted(started)

  let after = null
  const delay = Math.min(3000, Math.max(400, intervalMs))

  while (!signal?.aborted) {
    const res = await fetch(eventsUrl(streamId, after), {
      method: 'GET',
      headers,
      signal
    })
    if (!res.ok) {
      const text = await res.text().catch(() => '')
      throw new Error(text || `Event stream poll failed (${res.status}).`)
    }
    const batch = await res.json()
    const events = Array.isArray(batch?.events) ? batch.events : []
    for (const evt of events) {
      await onEvent(evt)
      if (evt?.type === 'done' || evt?.type === 'error') {
        return started
      }
    }
    if (batch?.nextAfter) after = batch.nextAfter
    if (batch?.completed) return started
    await sleep(delay, signal)
  }

  return started
}

function sleep(ms, signal) {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(new DOMException('Aborted', 'AbortError'))
      return
    }
    const t = setTimeout(resolve, ms)
    signal?.addEventListener(
      'abort',
      () => {
        clearTimeout(t)
        reject(new DOMException('Aborted', 'AbortError'))
      },
      { once: true }
    )
  })
}

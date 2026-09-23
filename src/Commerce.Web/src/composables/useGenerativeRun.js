import { FINBUCKLE_TENANT_HEADER, resolveTenantSlug } from '@/utils/tenant'
import {
  getEventPollIntervalMs,
  loadEventStreamSettings
} from '@/utils/eventTransport'
import { runPollEventTransport } from '@/composables/usePollEventTransport'

function resolveApiBase() {
  return (import.meta.env.VITE_API_URL || '/api').replace(/\/$/, '')
}

/**
 * After POST /generative/.../runs (202 + processId), follow IEventStreamStore via short-poll.
 * Generative has no dedicated SSE wait endpoint — always polls GET /event-streams/{processId}/events.
 *
 * @param {{
 *   processId: string,
 *   signal?: AbortSignal,
 *   onEvent: (evt: object) => void | Promise<void>,
 *   onError?: (err: Error) => void
 * }} opts
 */
export async function watchGenerativeProcess(opts) {
  const { processId, signal, onEvent, onError } = opts
  const id = String(processId || '').trim()
  if (!id) throw new Error('processId is required.')

  const tenant = resolveTenantSlug()
  const prefix = tenant ? `/t/${tenant}` : ''
  const base = resolveApiBase()
  const token =
    localStorage.getItem('auth_token') || localStorage.getItem('_token') || ''
  const headers = {
    Accept: 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(tenant ? { [FINBUCKLE_TENANT_HEADER]: tenant } : {})
  }

  try {
    await loadEventStreamSettings({
      apiBase: base,
      tenantPrefix: prefix,
      headers,
      signal
    })

    await runPollEventTransport({
      headers,
      signal,
      intervalMs: getEventPollIntervalMs(),
      start: async () => ({ streamId: id, processId: id }),
      eventsUrl: (streamId, after) => {
        const q = after ? `?after=${encodeURIComponent(after)}` : ''
        return `${base}${prefix}/event-streams/${encodeURIComponent(streamId)}/events${q}`
      },
      onEvent
    })
  } catch (err) {
    if (err?.name === 'AbortError' || signal?.aborted) return
    if (onError) onError(err instanceof Error ? err : new Error(String(err)))
    else throw err
  }
}

/**
 * Parse agent output JSON for ficha / video payloads.
 * Accepts: `{ ficha }`, `{ type: "products", products: [ficha] }`, or a flat ficha object.
 * @param {object} evt
 * @returns {{ ficha?: object, video?: object, images?: object[], message?: string, raw?: object } | null}
 */
export function parseGenerativeOutputEvent(evt) {
  if (!evt || evt.type !== 'output') return null
  const raw = typeof evt.text === 'string' ? evt.text.trim() : ''
  if (!raw.startsWith('{')) return null
  try {
    const parsed = JSON.parse(raw)
    if (!parsed || typeof parsed !== 'object') return null
    const images = Array.isArray(parsed.images)
      ? parsed.images.filter((x) => x && typeof x === 'object')
      : null
    return {
      ficha: resolveFichaFromParsed(parsed),
      video: parsed.video && typeof parsed.video === 'object' ? parsed.video : null,
      images,
      message: typeof parsed.message === 'string' ? parsed.message : null,
      raw: parsed
    }
  } catch {
    return null
  }
}

/**
 * @param {object} parsed
 * @returns {object | null}
 */
function resolveFichaFromParsed(parsed) {
  if (parsed.ficha && typeof parsed.ficha === 'object' && !Array.isArray(parsed.ficha)) {
    return parsed.ficha
  }

  const products = parsed.products
  if (Array.isArray(products) && products[0] && typeof products[0] === 'object') {
    const first = products[0]
    // Prefer product-shaped cards that look like a generative ficha (name + description).
    if (looksLikeFicha(first)) return first
  }

  if (looksLikeFicha(parsed)) return parsed
  return null
}

/** @param {object} obj */
function looksLikeFicha(obj) {
  if (!obj || typeof obj !== 'object' || Array.isArray(obj)) return false
  const name = typeof obj.name === 'string' ? obj.name.trim() : ''
  const description = typeof obj.description === 'string' ? obj.description.trim() : ''
  return name.length > 0 && description.length > 0
}

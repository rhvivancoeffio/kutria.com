import { fetchEventSource } from '@microsoft/fetch-event-source'

/**
 * Generic SSE transport. Feature supplies absolute URL + fetch init fields.
 * @param {{ url: string, method?: string, headers?: Record<string, string>, body?: BodyInit | null, signal?: AbortSignal, onEvent: (payload: object) => void | Promise<void> }} opts
 */
export async function runSseEventTransport(opts) {
  const { url, method = 'POST', headers = {}, body = null, signal, onEvent } = opts
  await fetchEventSource(url, {
    method,
    headers,
    body,
    signal,
    openWhenHidden: true,
    async onmessage(event) {
      if (!event.data || event.data.startsWith(':')) return
      const payload = JSON.parse(event.data)
      await onEvent(payload)
    },
    onerror(err) {
      throw err
    }
  })
}

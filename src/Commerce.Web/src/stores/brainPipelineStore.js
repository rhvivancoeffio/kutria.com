import { defineStore } from 'pinia'
import { reactive } from 'vue'
import { fetchEventSource } from '@microsoft/fetch-event-source'
import { FINBUCKLE_TENANT_HEADER, resolveTenantSlug, tenantApiPrefix } from '@/utils/tenant'

const TYPES = ['return', 'faq', 'terms', 'privacy', 'shipping']
const TERMINAL = ['ready', 'needs_ocr', 'published', 'failed']

function storageKey() {
  return `brain-policy-jobs:${resolveTenantSlug() || 'tenant'}`
}

function blank(type) {
  return {
    type,
    jobId: '',
    fileName: '',
    status: '',
    events: [],
    text: '',
    summary: '',
    pageCount: null,
    tableCount: null,
    error: ''
  }
}

export const useBrainPipelineStore = defineStore('brainPipeline', () => {
  const sections = reactive(Object.fromEntries(TYPES.map((type) => [type, blank(type)])))
  const streams = new Map()

  function persist() {
    const jobs = {}
    for (const type of TYPES) {
      if (sections[type].jobId) jobs[type] = sections[type].jobId
    }
    sessionStorage.setItem(storageKey(), JSON.stringify(jobs))
  }

  function apply(type, payload) {
    const section = sections[type]
    if (!section || !payload) return
    section.jobId = payload.id || payload.Id || section.jobId
    section.status = payload.status || payload.Status || section.status
    section.fileName = payload.fileName || payload.FileName || section.fileName
    section.pageCount = payload.pageCount ?? payload.PageCount ?? section.pageCount
    section.tableCount = payload.tableCount ?? payload.TableCount ?? section.tableCount
    section.summary = payload.summary || payload.Summary || section.summary
    section.error = payload.error || payload.Error || ''
    const text = payload.text || payload.Text
    if (text) section.text = text
    const message = payload.message || section.status
    const last = section.events[section.events.length - 1]
    if (message && (!last || last.message !== message)) {
      section.events.push({ at: new Date().toISOString(), status: section.status, message })
    }
    persist()
  }

  function watch(type, jobId, restart = false, phase = 'parse') {
    if (!jobId) return
    const key = `${resolveTenantSlug()}:${type}:${jobId}:${phase}`
    if (streams.has(key)) {
      if (!restart) return
      streams.get(key).abort()
      streams.delete(key)
    }
    const controller = new AbortController()
    streams.set(key, controller)
    let terminal = false
    const tenant = resolveTenantSlug()
    const prefix = tenant ? `/t/${tenant}` : tenantApiPrefix()
    const base = (import.meta.env.VITE_API_URL || '/api').replace(/\/$/, '')
    const token = localStorage.getItem('auth_token')
    const query = phase === 'publish' ? '?phase=publish' : ''
    fetchEventSource(`${base}${prefix}/policies/jobs/${jobId}/events${query}`, {
      signal: controller.signal,
      openWhenHidden: true,
      async onopen(response) {
        const type = response.headers.get('content-type') || ''
        if (!response.ok || !type.includes('text/event-stream')) {
          const stop = new Error(`sse ${response.status}`)
          stop.name = 'Finished'
          throw stop
        }
      },
      headers: {
        Accept: 'text/event-stream',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(tenant ? { [FINBUCKLE_TENANT_HEADER]: tenant } : {})
      },
      onmessage(event) {
        if (!event.data) return
        const payload = JSON.parse(event.data)
        apply(type, payload)
        if (TERMINAL.includes(payload.status) || (phase === 'publish' && ['published', 'failed'].includes(payload.status))) {
          terminal = phase === 'publish'
            ? ['published', 'failed'].includes(payload.status)
            : TERMINAL.includes(payload.status)
        }
      },
      onclose() {
        streams.delete(key)
        const stop = new Error(terminal ? 'finished' : 'stream closed')
        stop.name = terminal ? 'Finished' : 'Retry'
        throw stop
      },
      onerror(error) {
        if (error?.name === 'Finished' || controller.signal.aborted) throw error
        return 2000
      }
    }).catch(() => {
      streams.delete(key)
    })
  }

  function reset(type) {
    for (const [key, controller] of streams) {
      if (key.includes(`:${type}:`)) {
        controller.abort()
        streams.delete(key)
      }
    }
    Object.assign(sections[type], blank(type))
    persist()
  }

  function resume() {
    let saved = {}
    try {
      saved = JSON.parse(sessionStorage.getItem(storageKey()) || '{}')
    } catch {
      saved = {}
    }
    for (const type of TYPES) {
      const jobId = sections[type].jobId || saved[type]
      if (!jobId) continue
      sections[type].jobId = jobId
      if (!TERMINAL.includes(sections[type].status)) watch(type, jobId)
    }
  }

  return { sections, apply, watch, resume, reset }
})

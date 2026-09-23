<script setup>
import { computed, nextTick, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useToast } from 'vue-toastification'
import FormToggle from '@/components/FormToggle.vue'
import apiService, { getApiErrorMessage } from '@/services/api'
import { useBrainPipelineStore } from '@/stores/brainPipelineStore'
import { FINBUCKLE_TENANT_HEADER, resolveTenantSlug, tenantApiPrefix } from '@/utils/tenant'
import {
  getEventTransport,
  getEventPollIntervalMs,
  loadEventStreamSettings
} from '@/utils/eventTransport'
import { runSseEventTransport } from '@/composables/useSseEventTransport'
import { runPollEventTransport } from '@/composables/usePollEventTransport'

const types = [
  { id: 'return', label: 'Devolución', description: 'Plazos, excepciones y quién paga el envío de un cambio o devolución.' },
  { id: 'faq', label: 'FAQ', description: 'Preguntas frecuentes de soporte, horarios y casos que no cubre una política.' },
  { id: 'terms', label: 'Términos', description: 'Condiciones de venta, precios y lo que el comprador acepta al comprar.' },
  { id: 'privacy', label: 'Privacidad', description: 'Qué datos se recogen, con quién se comparten y cómo se piden borrar.' },
  { id: 'shipping', label: 'Envío', description: 'Plazos, cobertura, costo y qué pasa si el pedido no llega a tiempo.' }
]

const toast = useToast()
const store = useBrainPipelineStore()
const files = reactive({})
const dates = reactive({})
const source = reactive({})
const pasted = reactive({})
const busy = reactive({})
const dragging = reactive({})
const replacing = reactive({})
const published = ref({})
const pageLoading = ref(true)
const rehydrating = reactive({})
const evalItems = ref([])
const evalVersion = ref('')
const savingTests = ref(false)
const evalSliderOpen = ref(false)
const evalTarget = ref(null)
const evalLive = ref(null)
const evalRunning = ref(false)
const auditSliderOpen = ref(false)
const auditTarget = ref(null)
const auditRuns = ref([])
const auditRunId = ref('')
const auditLoading = ref(false)
const questionsSliderOpen = ref(false)
const inputs = {}
let evalController = null

onMounted(async () => {
  store.resume()
  pageLoading.value = true
  try {
    await Promise.all([loadPublished(), loadEvalItems()])
  } finally {
    pageLoading.value = false
  }
})

onUnmounted(() => evalController?.abort())

watch(
  () => types.map((item) => store.sections[item.id]?.status).join(','),
  async (signature, previous) => {
    const prior = previous?.split(',') ?? []
    for (const [index, item] of types.entries()) {
      const status = signature.split(',')[index]
      if (status === 'published' && prior[index] !== 'published') {
        replacing[item.id] = false
        busy[item.id] = false
        rehydrating[item.id] = true
        try {
          await loadPublished()
        } finally {
          rehydrating[item.id] = false
        }
      }
      if (status === 'failed' && prior[index] === 'publishing') {
        busy[item.id] = false
      }
    }
  }
)

async function loadPublished() {
  try {
    const items = await apiService.listPublishedPolicies()
    const next = {}
    for (const item of items) {
      const type = item.type ?? item.Type
      if (type) next[type] = item
    }
    published.value = next
  } catch {
    published.value = {}
  }
}

async function loadEvalItems() {
  try {
    const data = await apiService.getPolicyEvalItems()
    evalVersion.value = data.version
    evalItems.value = data.items.map((item) => ({
      baseItemId: item.baseItemId ?? item.BaseItemId ?? null,
      extraId: item.extraId ?? item.ExtraId ?? null,
      type: item.type ?? item.Type ?? '',
      question: item.question ?? item.Question ?? '',
      isDisabled: item.isDisabled ?? item.IsDisabled ?? false,
      source: item.source ?? item.Source ?? 'base'
    }))
  } catch {
    evalItems.value = []
  }
}

function publishedDoc(type) {
  return published.value[type] ?? null
}

function showUpload(type) {
  if (isPublishing(type) || rehydrating[type]) return false
  return !publishedDoc(type) || replacing[type] || inProgress(type)
}

function inProgress(type) {
  const status = store.sections[type]?.status
  return Boolean(status) && status !== 'published'
}

function isPublishing(type) {
  return busy[type] || store.sections[type]?.status === 'publishing'
}

function showPreview(type) {
  return store.sections[type]?.status === 'ready' || isPublishing(type)
}

function showSummary(type) {
  return Boolean(publishedDoc(type)) && !replacing[type] && !inProgress(type) && !rehydrating[type]
}

function startReplace(type) {
  replacing[type] = true
  files[type] = null
  source[type] = ''
  pasted[type] = ''
  store.reset(type)
}

function cancelReplace(type) {
  replacing[type] = false
  files[type] = null
  source[type] = ''
  pasted[type] = ''
}

function fileSize(bytes) {
  const value = Number(bytes)
  if (!Number.isFinite(value) || value <= 0) return null
  if (value < 1024) return `${value} B`
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`
  return `${(value / (1024 * 1024)).toFixed(1)} MB`
}

function fileDate(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString('es-PE', { year: 'numeric', month: 'short', day: 'numeric' })
}

function fileTime(value) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('es-PE', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function evalStatus(doc) {
  return doc?.evaluationStatus ?? doc?.EvaluationStatus ?? 'not_run'
}

function evalLabel(doc) {
  const failed = doc?.evaluationFailedCount ?? doc?.EvaluationFailedCount ?? 0
  const status = evalStatus(doc)
  if (status === 'pending') return 'En curso'
  if (status === 'passed') return `Aprobado (${failed} fallos)`
  if (status === 'review_failed') return `Review Failed (${failed} fallos)`
  return 'Sin evaluar'
}

const questionsToRun = computed(() => {
  const type = evalTarget.value?.type ?? evalTarget.value?.Type
  return evalItems.value.filter((item) => !item.isDisabled && item.question.trim() && item.type === type)
})

function typeLabel(type) {
  return types.find((item) => item.id === type)?.label ?? type
}

const questionsByType = computed(() => types.map((type) => ({
  ...type,
  items: evalItems.value
    .map((row, index) => ({ row, index }))
    .filter(({ row }) => row.baseItemId && row.type === type.id)
})))

const extraQuestions = computed(() => evalItems.value
  .map((row, index) => ({ row, index }))
  .filter(({ row }) => !row.baseItemId))

let extraKey = 0

function openQuestions() {
  closeEval()
  closeAudit()
  questionsSliderOpen.value = true
}

function closeQuestions() {
  questionsSliderOpen.value = false
}

function setQuestionActive(row, active) {
  row.isDisabled = !active
}

const auditRun = computed(() => auditRuns.value.find((run) => (run.id ?? run.Id) === auditRunId.value) ?? auditRuns.value[0] ?? null)

const auditAnswers = computed(() => auditRun.value?.answers ?? auditRun.value?.Answers ?? [])

const auditStats = computed(() => {
  const failed = auditAnswers.value.filter(answerInvented).length
  const fromRun = auditRun.value?.failedCount ?? auditRun.value?.FailedCount
  return {
    failed: fromRun ?? failed,
    passed: Math.max(0, auditAnswers.value.length - failed),
    total: auditAnswers.value.length
  }
})

function answerInvented(answer) {
  return Boolean(answer?.invented ?? answer?.Invented)
}

function runStatus(run) {
  return run?.status ?? run?.Status ?? ''
}

function runStatusLabel(status) {
  if (status === 'passed') return 'Aprobado'
  if (status === 'review_failed') return 'Review Failed'
  if (status === 'pending') return 'En curso'
  return 'Sin estado'
}

function runDuration(run) {
  const start = new Date(run?.startedAt ?? run?.StartedAt)
  const end = new Date(run?.finishedAt ?? run?.FinishedAt)
  const ms = end.getTime() - start.getTime()
  if (!Number.isFinite(ms) || ms < 0) return '—'
  const seconds = Math.round(ms / 1000)
  if (seconds < 60) return `${seconds} s`
  return `${Math.floor(seconds / 60)} min ${seconds % 60} s`
}

function openEval(type) {
  closeAudit()
  closeQuestions()
  const doc = publishedDoc(type)
  if (!doc) return
  evalTarget.value = { ...doc, type }
  evalLive.value = {
    status: evalStatus(doc),
    failedCount: doc.evaluationFailedCount ?? doc.EvaluationFailedCount ?? 0,
    answered: 0,
    message: evalStatus(doc) === 'pending' ? 'En curso.' : ''
  }
  evalSliderOpen.value = true
  if (evalStatus(doc) === 'pending') watchEval(doc.id ?? doc.Id)
}

function closeEval() {
  evalSliderOpen.value = false
  if (!evalRunning.value) evalTarget.value = null
}

function closeAudit() {
  auditSliderOpen.value = false
  auditTarget.value = null
}

async function showAudit(type) {
  const doc = publishedDoc(type)
  if (!doc) return
  closeEval()
  closeQuestions()
  auditTarget.value = { ...doc, type }
  auditRuns.value = []
  auditRunId.value = ''
  auditSliderOpen.value = true
  auditLoading.value = true
  try {
    const runs = await apiService.listPolicyJobEvals(doc.id ?? doc.Id)
    auditRuns.value = runs
    auditRunId.value = runs[0]?.id ?? runs[0]?.Id ?? ''
  } catch (err) {
    toast.error(getApiErrorMessage(err) || 'No se pudo leer la auditoría.')
  } finally {
    auditLoading.value = false
  }
}

function applyEvalEvent(payload) {
  evalLive.value = {
    status: payload.evaluationStatus ?? payload.status,
    failedCount: payload.failedCount ?? 0,
    answered: payload.answered ?? 0,
    message: payload.message || ''
  }
  const id = payload.id ?? evalTarget.value?.id ?? evalTarget.value?.Id
  if (!id) return
  const next = { ...published.value }
  for (const type of Object.keys(next)) {
    const doc = next[type]
    if ((doc.id ?? doc.Id) === id) {
      next[type] = {
        ...doc,
        evaluationStatus: evalLive.value.status,
        evaluationFailedCount: evalLive.value.failedCount
      }
    }
  }
  published.value = next
}

function watchEval(id, streamId) {
  evalController?.abort()
  const controller = new AbortController()
  evalController = controller
  const tenant = resolveTenantSlug()
  const prefix = tenant ? `/t/${tenant}` : tenantApiPrefix()
  const base = (import.meta.env.VITE_API_URL || '/api').replace(/\/$/, '')
  const token = localStorage.getItem('auth_token')
  const headers = {
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(tenant ? { [FINBUCKLE_TENANT_HEADER]: tenant } : {})
  }

  const onEvent = (payload) => {
    applyEvalEvent(payload)
    const status = payload.evaluationStatus
    if (status === 'passed' || status === 'review_failed' || payload.type === 'done' || payload.type === 'error') {
      evalRunning.value = false
      controller.abort()
      showAudit({ id })
    }
  }

  const resolvedStreamId = streamId || `policyeval_${String(id).replace(/-/g, '')}`

  void (async () => {
    try {
      await loadEventStreamSettings({
        apiBase: base,
        tenantPrefix: prefix,
        headers,
        signal: controller.signal
      })

      if (getEventTransport() === 'poll') {
        await runPollEventTransport({
          headers,
          signal: controller.signal,
          intervalMs: getEventPollIntervalMs(),
          start: async () => ({ streamId: resolvedStreamId }),
          eventsUrl: (sid, after) => {
            const q = after ? `?after=${encodeURIComponent(after)}` : ''
            return `${base}${prefix}/event-streams/${encodeURIComponent(sid)}/events${q}`
          },
          onEvent: async (evt) => {
            let payload = {
              id,
              type: evt.type,
              text: evt.text,
              evaluationStatus: 'pending',
              failedCount: 0,
              answered: 0,
              message: evt.text || ''
            }
            if (evt?.data && typeof evt.data === 'string') {
              try {
                const parsed = JSON.parse(evt.data)
                payload = { ...payload, ...parsed, type: evt.type }
              } catch {
                /* keep defaults */
              }
            } else if (evt?.evaluationStatus) {
              payload = { ...payload, ...evt }
            }
            if (evt.type === 'done' && !payload.evaluationStatus) payload.evaluationStatus = 'passed'
            if (evt.type === 'error' && !payload.evaluationStatus) payload.evaluationStatus = 'review_failed'
            onEvent(payload)
          }
        })
      } else {
        await runSseEventTransport({
          url: `${base}${prefix}/policies/jobs/${id}/eval/events`,
          method: 'GET',
          headers: {
            Accept: 'text/event-stream',
            ...headers
          },
          signal: controller.signal,
          onEvent
        })
      }
    } catch (err) {
      if (err?.name === 'AbortError' || controller.signal.aborted) return
      evalRunning.value = false
    }
  })()
}

async function runEval() {
  const doc = evalTarget.value
  const id = doc?.id ?? doc?.Id
  if (!id) return
  evalRunning.value = true
  try {
    const started = await apiService.startPolicyEval(id)
    const streamId = started?.streamId ?? started?.StreamId
    applyEvalEvent({ id, evaluationStatus: 'pending', failedCount: 0, answered: 0, message: 'En cola.' })
    watchEval(id, streamId)
  } catch (err) {
    evalRunning.value = false
    toast.error(getApiErrorMessage(err) || 'No se pudo iniciar la evaluación.')
  }
}

function addEvalItem() {
  const clientKey = `new-${++extraKey}`
  evalItems.value = [...evalItems.value, { baseItemId: null, extraId: null, clientKey, type: 'return', question: '', isDisabled: false, source: 'extra' }]
  nextTick(() => {
    const row = document.getElementById(`eval-item-${clientKey}`)
    row?.scrollIntoView({ block: 'nearest' })
    row?.querySelector('textarea')?.focus()
  })
}

async function saveEvalItems() {
  savingTests.value = true
  try {
    await apiService.savePolicyEvalItems(evalItems.value.map((item) => ({
      baseItemId: item.baseItemId,
      extraId: item.extraId,
      type: item.type,
      question: item.question,
      isDisabled: item.isDisabled
    })))
    await loadEvalItems()
    toast.success('Preguntas guardadas.')
  } catch (err) {
    toast.error(getApiErrorMessage(err) || 'No se pudieron guardar las preguntas.')
  } finally {
    savingTests.value = false
  }
}

async function download(doc) {
  const id = doc.id ?? doc.Id
  const name = doc.fileName ?? doc.FileName
  try {
    await apiService.downloadPolicyFile(id, name)
  } catch (err) {
    toast.error(getApiErrorMessage(err) || 'No se pudo descargar el archivo.')
  }
}

function pick(type, event) {
  files[type] = event.target.files?.[0] ?? event.dataTransfer?.files?.[0] ?? null
  dragging[type] = false
}

function selectSource(type, mode) {
  source[type] = mode
}

function textFileName(type) {
  const label = (typeLabel(type) || type)
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
  return `${label || type}.txt`
}

function uploadFile(type) {
  if (source[type] === 'text') {
    const text = (pasted[type] || '').trim()
    if (!text) return null
    return new File([text], textFileName(type), { type: 'text/plain' })
  }
  return files[type] ?? null
}

async function upload(type) {
  if (!dates[type]) {
    toast.error('Elige la fecha de vigencia.')
    return
  }
  if (source[type] !== 'file' && source[type] !== 'text') {
    toast.error('Elige ingresar texto o cargar un archivo.')
    return
  }
  const file = uploadFile(type)
  if (!file) {
    toast.error(source[type] === 'text' ? 'Pega el texto de la política.' : 'Elige un archivo.')
    return
  }
  busy[type] = true
  try {
    const created = await apiService.uploadPolicy(file, type, dates[type])
    const id = created.id ?? created.Id
    store.apply(type, {
      id,
      status: created.status ?? created.Status ?? 'queued',
      fileName: file.name,
      message: 'Archivo recibido. En cola.'
    })
    store.watch(type, id)
    source[type] = ''
    pasted[type] = ''
  } catch (err) {
    toast.error(getApiErrorMessage(err) || 'No se pudo subir el archivo.')
  } finally {
    busy[type] = false
  }
}

async function publish(type) {
  const section = store.sections[type]
  if (!section?.jobId) return
  busy[type] = true
  try {
    await apiService.publishPolicy(section.jobId, section.text)
    store.apply(type, { id: section.jobId, status: 'publishing', message: 'Publicando en el cerebro.' })
    store.watch(type, section.jobId, true, 'publish')
  } catch (err) {
    busy[type] = false
    toast.error(getApiErrorMessage(err) || 'No se pudo publicar.')
  }
}

function time(value) {
  return new Date(value).toLocaleTimeString('es-PE', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 flex items-start justify-between gap-4 sm:mb-8">
        <div>
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Cerebro</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            PDF, Word, JSON o TXT. Excel y Word viejo todavía no se leen. Cada tipo se sube aparte. Puedes ir a otra opción: la carga sigue y los eventos quedan en su sección.
          </p>
        </div>
        <button
          type="button"
          class="inline-flex h-11 w-11 shrink-0 items-center justify-center rounded-lg text-gray-600 hover:bg-gray-200/70 dark:text-gray-300 dark:hover:bg-gray-800"
          aria-label="Preguntas de evaluación"
          @click="openQuestions"
        >
          <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25Z" />
          </svg>
        </button>
      </div>

      <div v-if="pageLoading" class="grid gap-4" aria-busy="true" aria-label="Cargando políticas">
        <article
          v-for="item in types"
          :key="`skel-${item.id}`"
          class="flex flex-col overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-800"
        >
          <header class="border-b border-gray-200 px-5 py-4 dark:border-gray-700 sm:px-6">
            <div class="h-5 w-32 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
            <div class="mt-2 h-4 w-full max-w-md animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
          </header>
          <div class="flex-1 space-y-3 px-5 py-5 sm:px-6">
            <div class="grid gap-3 sm:grid-cols-2">
              <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
              <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
              <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
              <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
            </div>
            <div class="h-16 animate-pulse rounded-lg bg-gray-200 dark:bg-gray-700" />
          </div>
          <footer class="flex justify-end gap-2 border-t border-gray-200 bg-gray-50 px-5 py-3 dark:border-gray-700 dark:bg-gray-900/40 sm:px-6">
            <div class="h-11 w-28 animate-pulse rounded-lg bg-gray-200 dark:bg-gray-700" />
            <div class="h-11 w-36 animate-pulse rounded-lg bg-gray-200 dark:bg-gray-700" />
          </footer>
        </article>
      </div>

      <div v-else class="grid gap-4">
        <article
          v-for="item in types"
          :key="item.id"
          class="flex flex-col overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-800"
        >
          <header class="border-b border-gray-200 px-5 py-4 dark:border-gray-700 sm:px-6">
            <h2 class="text-base font-semibold text-gray-900 dark:text-white sm:text-lg">{{ item.label }}</h2>
            <p class="mt-1 max-w-3xl text-sm leading-5 text-gray-500 dark:text-gray-400">{{ item.description }}</p>
          </header>
          <div class="flex-1 space-y-4 px-5 py-5 sm:px-6">
            <div v-if="rehydrating[item.id]" class="space-y-3" aria-busy="true" aria-label="Actualizando documento">
              <div class="grid gap-3 sm:grid-cols-2">
                <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
                <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
                <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
                <div class="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
              </div>
              <div class="h-16 animate-pulse rounded-lg bg-gray-200 dark:bg-gray-700" />
            </div>

            <div v-else-if="showSummary(item.id)" class="space-y-4">
              <dl class="grid gap-3 sm:grid-cols-2 text-sm">
                <div>
                  <dt class="text-gray-500 dark:text-gray-400">Archivo</dt>
                  <dd class="font-medium text-gray-900 dark:text-white">{{ publishedDoc(item.id).fileName ?? publishedDoc(item.id).FileName }}</dd>
                </div>
                <div>
                  <dt class="text-gray-500 dark:text-gray-400">Tamaño</dt>
                  <dd class="font-medium text-gray-900 dark:text-white">{{ fileSize(publishedDoc(item.id).sizeBytes ?? publishedDoc(item.id).SizeBytes) || '—' }}</dd>
                </div>
                <div>
                  <dt class="text-gray-500 dark:text-gray-400">Vigente desde</dt>
                  <dd class="font-medium text-gray-900 dark:text-white">{{ (publishedDoc(item.id).effectiveFrom ?? publishedDoc(item.id).EffectiveFrom) || '—' }}</dd>
                </div>
                <div>
                  <dt class="text-gray-500 dark:text-gray-400">Publicado</dt>
                  <dd class="font-medium text-gray-900 dark:text-white">{{ fileDate(publishedDoc(item.id).uploadedAt ?? publishedDoc(item.id).UploadedAt) }}</dd>
                </div>
                <div>
                  <dt class="text-gray-500 dark:text-gray-400">Evaluación</dt>
                  <dd class="font-medium text-gray-900 dark:text-white">{{ evalLabel(publishedDoc(item.id)) }}</dd>
                </div>
                <div>
                  <dt class="text-gray-500 dark:text-gray-400">Lectura</dt>
                  <dd class="font-medium text-gray-900 dark:text-white">
                    {{ publishedDoc(item.id).pageCount ?? publishedDoc(item.id).PageCount ?? 0 }} páginas,
                    {{ publishedDoc(item.id).tableCount ?? publishedDoc(item.id).TableCount ?? 0 }} tablas
                  </dd>
                </div>
              </dl>
              <p v-if="publishedDoc(item.id).summary ?? publishedDoc(item.id).Summary" class="rounded-lg bg-gray-50 px-3 py-2 text-sm leading-5 text-gray-700 dark:bg-gray-900/50 dark:text-gray-200">
                {{ publishedDoc(item.id).summary ?? publishedDoc(item.id).Summary }}
              </p>
            </div>

            <div v-if="showUpload(item.id)" class="space-y-4">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                Vigente desde <span class="text-red-500">*</span>
                <input v-model="dates[item.id]" type="date" required class="mt-1 block rounded-lg border border-gray-300 bg-white px-3 py-2 dark:border-gray-600 dark:bg-gray-700" />
              </label>
              <div class="grid gap-3 sm:grid-cols-2">
                <button
                  type="button"
                  class="flex min-h-[88px] flex-col items-start justify-center rounded-lg border px-4 py-3 text-left transition-colors"
                  :class="source[item.id] === 'file'
                    ? 'border-primary-600 bg-primary-50 ring-1 ring-primary-600 dark:bg-primary-900/20'
                    : 'border-gray-200 bg-white hover:border-primary-400 dark:border-gray-700 dark:bg-gray-800'"
                  @click="selectSource(item.id, 'file')"
                >
                  <span class="text-sm font-semibold text-gray-900 dark:text-white">Cargar archivo</span>
                  <span class="mt-1 text-xs text-gray-500 dark:text-gray-400">PDF, Word, JSON o TXT</span>
                </button>
                <button
                  type="button"
                  class="flex min-h-[88px] flex-col items-start justify-center rounded-lg border px-4 py-3 text-left transition-colors"
                  :class="source[item.id] === 'text'
                    ? 'border-primary-600 bg-primary-50 ring-1 ring-primary-600 dark:bg-primary-900/20'
                    : 'border-gray-200 bg-white hover:border-primary-400 dark:border-gray-700 dark:bg-gray-800'"
                  @click="selectSource(item.id, 'text')"
                >
                  <span class="text-sm font-semibold text-gray-900 dark:text-white">Ingresar texto</span>
                  <span class="mt-1 text-xs text-gray-500 dark:text-gray-400">Se sube como archivo .txt</span>
                </button>
              </div>
              <div
                v-if="source[item.id] === 'file'"
                :class="[
                  'rounded-lg border-2 transition-colors flex flex-col items-center justify-center min-h-[140px] p-4 cursor-pointer',
                  dragging[item.id]
                    ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20'
                    : 'border-gray-300 dark:border-gray-600 hover:border-primary-400 hover:bg-gray-50 dark:hover:bg-gray-700/50'
                ]"
                @dragover.prevent="dragging[item.id] = true"
                @dragleave.prevent="dragging[item.id] = false"
                @drop.prevent="pick(item.id, $event)"
                @click="inputs[item.id]?.click()"
              >
                <input
                  :ref="(el) => { inputs[item.id] = el }"
                  type="file"
                  class="hidden"
                  accept=".pdf,.docx,.txt,.json"
                  @change="pick(item.id, $event)"
                />
                <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                </svg>
                <p class="text-sm text-gray-600 dark:text-gray-400 text-center">
                  <template v-if="files[item.id]">{{ files[item.id].name }}</template>
                  <template v-else>Arrastra un archivo aquí o <span class="text-primary-600 dark:text-primary-400">selecciona</span></template>
                </p>
              </div>
              <textarea
                v-if="source[item.id] === 'text'"
                v-model="pasted[item.id]"
                rows="8"
                class="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm dark:border-gray-700 dark:bg-gray-900"
                placeholder="Copia y pega el texto. Se sube como archivo .txt."
              />
            </div>

            <ul v-if="(showUpload(item.id) || isPublishing(item.id)) && store.sections[item.id].events.length" class="space-y-1 text-sm text-gray-700 dark:text-gray-200">
              <li v-for="(event, index) in store.sections[item.id].events" :key="index">
                <span class="text-gray-400">{{ time(event.at) }}</span>
                {{ event.message }}
              </li>
            </ul>

            <p v-if="showUpload(item.id) && store.sections[item.id].summary" class="text-sm text-gray-900 dark:text-white">
              Resumen: {{ store.sections[item.id].summary }}
            </p>

            <div v-if="showPreview(item.id)" class="relative">
              <textarea
                v-model="store.sections[item.id].text"
                rows="6"
                class="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm dark:border-gray-700 dark:bg-gray-900 disabled:cursor-not-allowed disabled:bg-gray-50 disabled:text-gray-500 dark:disabled:bg-gray-900/60 dark:disabled:text-gray-400"
                :disabled="isPublishing(item.id)"
                :aria-busy="isPublishing(item.id)"
              />
              <div
                v-if="isPublishing(item.id)"
                class="pointer-events-none absolute inset-0 flex items-center justify-center rounded-lg bg-white/70 dark:bg-gray-900/70"
              >
                <div class="flex items-center gap-2 rounded-lg bg-white px-3 py-2 text-sm font-medium text-gray-700 shadow-sm dark:bg-gray-800 dark:text-gray-200">
                  <svg class="h-4 w-4 animate-spin text-primary-600" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  Publicando…
                </div>
              </div>
            </div>
          </div>
          <footer class="flex flex-wrap items-center justify-end gap-2 border-t border-gray-200 bg-gray-50 px-5 py-3 dark:border-gray-700 dark:bg-gray-900/40 sm:px-6">
            <template v-if="rehydrating[item.id]">
              <div class="h-11 w-36 animate-pulse rounded-lg bg-gray-200 dark:bg-gray-700" />
            </template>
            <template v-else-if="showSummary(item.id)">
              <button
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg px-4 py-2.5 font-medium text-gray-700 hover:bg-gray-200/80 dark:text-gray-300 dark:hover:bg-gray-700"
                @click="showAudit(item.id)"
              >
                Ver auditoría
              </button>
              <button
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg px-4 py-2.5 font-medium text-gray-700 hover:bg-gray-200/80 dark:text-gray-300 dark:hover:bg-gray-700"
                @click="startReplace(item.id)"
              >
                Cargar nuevo archivo
              </button>
              <button
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg px-4 py-2.5 font-medium text-gray-700 hover:bg-gray-200/80 dark:text-gray-300 dark:hover:bg-gray-700"
                @click="download(publishedDoc(item.id))"
              >
                Descargar archivo
              </button>
              <button
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 font-medium text-white transition-colors hover:bg-primary-700"
                @click="openEval(item.id)"
              >
                Ejecutar evaluación
              </button>
            </template>
            <template v-else-if="isPublishing(item.id)">
              <button
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 font-medium text-white opacity-50"
                disabled
              >
                Publicando…
              </button>
            </template>
            <template v-else-if="showUpload(item.id) && store.sections[item.id].status === 'ready'">
              <button
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-50"
                :disabled="busy[item.id]"
                @click="publish(item.id)"
              >
                Sí, publicar
              </button>
            </template>
            <template v-else-if="showUpload(item.id)">
              <button
                v-if="replacing[item.id] && publishedDoc(item.id)"
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg px-4 py-2.5 font-medium text-gray-700 hover:bg-gray-200/80 dark:text-gray-300 dark:hover:bg-gray-700"
                @click="cancelReplace(item.id)"
              >
                Cancelar
              </button>
              <button
                type="button"
                class="inline-flex min-h-[44px] items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-50"
                :disabled="busy[item.id]"
                @click="upload(item.id)"
              >
                {{ source[item.id] === 'text' ? 'Subir texto' : 'Subir archivo' }}
              </button>
            </template>
          </footer>
        </article>
      </div>

    </main>
  </div>

  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="evalSliderOpen" class="fixed inset-0 z-50 bg-black/50" @click.self="closeEval" />
    </Transition>
    <Transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="evalSliderOpen"
        class="fixed top-0 right-0 z-[51] flex h-full w-full max-w-lg flex-col border-l border-gray-200 bg-white shadow-xl dark:border-gray-700 dark:bg-gray-800"
        role="dialog"
        aria-modal="true"
      >
        <div class="flex items-start justify-between gap-3 border-b border-gray-200 p-4 dark:border-gray-700 sm:p-5">
          <div>
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Ejecutar evaluación</h2>
            <p class="mt-1 text-sm text-gray-600 dark:text-gray-400">
              {{ questionsToRun.length }} preguntas de {{ typeLabel(evalTarget?.type) }}. Las de otros tipos no se ejecutan.
            </p>
          </div>
          <button type="button" class="p-2 -m-2 shrink-0 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300" aria-label="Cerrar" @click="closeEval">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div class="flex-1 space-y-3 overflow-y-auto p-4 sm:p-5">
          <p v-if="evalLive?.message" class="text-sm font-medium text-gray-900 dark:text-white">{{ evalLive.message }}</p>
          <ol class="space-y-2 text-sm text-gray-700 dark:text-gray-200">
            <li v-for="(row, index) in questionsToRun" :key="row.baseItemId || row.extraId || index" class="rounded-lg border border-gray-200 px-3 py-2 dark:border-gray-700">
              {{ index + 1 }}. {{ row.question }}
            </li>
          </ol>
          <p v-if="!questionsToRun.length" class="text-sm text-gray-500">No hay preguntas activas para esta sección.</p>
        </div>
        <div class="flex justify-end gap-2 border-t border-gray-200 p-4 dark:border-gray-700">
          <button type="button" class="inline-flex min-h-[44px] items-center justify-center rounded-lg px-4 py-2.5 font-medium text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700" @click="closeEval">
            Cancelar
          </button>
          <button
            type="button"
            class="inline-flex min-h-[44px] items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-50"
            :disabled="evalRunning || evalLive?.status === 'pending' || !questionsToRun.length"
            @click="runEval"
          >
            Ejecutar
          </button>
        </div>
      </div>
    </Transition>
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="auditSliderOpen" class="fixed inset-0 z-50 bg-black/50" @click.self="closeAudit" />
    </Transition>
    <Transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="auditSliderOpen"
        class="fixed top-0 right-0 z-[51] flex h-full w-full max-w-xl flex-col border-l border-gray-200 bg-white shadow-xl dark:border-gray-700 dark:bg-gray-800"
        role="dialog"
        aria-modal="true"
        aria-labelledby="audit-slider-title"
      >
        <div class="flex items-start justify-between gap-3 border-b border-gray-200 p-4 dark:border-gray-700 sm:p-5">
          <div>
            <h2 id="audit-slider-title" class="text-lg font-semibold text-gray-900 dark:text-white">Auditoría</h2>
            <p class="mt-1 text-sm text-gray-600 dark:text-gray-400">
              {{ typeLabel(auditTarget?.type) }} · {{ auditTarget?.fileName ?? auditTarget?.FileName }}
            </p>
          </div>
          <button type="button" class="p-2 -m-2 shrink-0 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300" aria-label="Cerrar" @click="closeAudit">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div class="flex-1 space-y-4 overflow-y-auto p-4 sm:p-5">
          <p v-if="auditLoading" class="text-sm text-gray-500">Cargando auditoría…</p>
          <p v-else-if="!auditRun" class="text-sm text-gray-500">Aún no hay una ejecución de evaluación.</p>
          <template v-else>
            <label v-if="auditRuns.length > 1" class="block text-sm font-medium text-gray-700 dark:text-gray-300">
              Ejecución
              <select v-model="auditRunId" class="mt-1 block w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700">
                <option v-for="run in auditRuns" :key="run.id ?? run.Id" :value="run.id ?? run.Id">
                  {{ fileTime(run.startedAt ?? run.StartedAt) }} · {{ runStatusLabel(runStatus(run)) }}
                </option>
              </select>
            </label>
            <div
              class="flex items-start gap-3 rounded-lg border p-4"
              :class="runStatus(auditRun) === 'passed'
                ? 'border-emerald-200 bg-emerald-50 dark:border-emerald-900 dark:bg-emerald-950/40'
                : runStatus(auditRun) === 'pending'
                  ? 'border-amber-200 bg-amber-50 dark:border-amber-900 dark:bg-amber-950/40'
                  : 'border-red-200 bg-red-50 dark:border-red-900 dark:bg-red-950/40'"
            >
              <svg v-if="runStatus(auditRun) === 'passed'" class="mt-0.5 h-6 w-6 shrink-0 text-emerald-600 dark:text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
              </svg>
              <svg v-else-if="runStatus(auditRun) === 'pending'" class="mt-0.5 h-6 w-6 shrink-0 text-amber-600 dark:text-amber-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6l4 2m5-2a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
              </svg>
              <svg v-else class="mt-0.5 h-6 w-6 shrink-0 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m9.75 9.75 4.5 4.5m0-4.5-4.5 4.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
              </svg>
              <div>
                <p class="font-semibold text-gray-900 dark:text-white">{{ runStatusLabel(runStatus(auditRun)) }}</p>
                <p class="mt-0.5 text-sm text-gray-600 dark:text-gray-300">
                  {{ auditStats.passed }} correctas · {{ auditStats.failed }} con error · {{ auditStats.total }} preguntas
                </p>
              </div>
            </div>
            <dl class="grid grid-cols-2 gap-3 rounded-lg border border-gray-200 p-4 text-sm dark:border-gray-700">
              <div>
                <dt class="text-gray-500 dark:text-gray-400">Inicio</dt>
                <dd class="font-medium text-gray-900 dark:text-white">{{ fileTime(auditRun.startedAt ?? auditRun.StartedAt) }}</dd>
              </div>
              <div>
                <dt class="text-gray-500 dark:text-gray-400">Fin</dt>
                <dd class="font-medium text-gray-900 dark:text-white">{{ fileTime(auditRun.finishedAt ?? auditRun.FinishedAt) }}</dd>
              </div>
              <div>
                <dt class="text-gray-500 dark:text-gray-400">Duración</dt>
                <dd class="font-medium text-gray-900 dark:text-white">{{ runDuration(auditRun) }}</dd>
              </div>
              <div>
                <dt class="text-gray-500 dark:text-gray-400">Plantilla</dt>
                <dd class="font-medium text-gray-900 dark:text-white">v{{ (auditRun.templateVersion ?? auditRun.TemplateVersion) || '—' }}</dd>
              </div>
            </dl>
            <ol class="space-y-2">
              <li
                v-for="(answer, index) in auditAnswers"
                :key="answer.questionId ?? answer.QuestionId ?? index"
                class="rounded-lg border px-3 py-3"
                :class="answerInvented(answer)
                  ? 'border-red-200 bg-red-50/70 dark:border-red-900 dark:bg-red-950/30'
                  : 'border-emerald-200 bg-emerald-50/70 dark:border-emerald-900 dark:bg-emerald-950/30'"
              >
                <div class="flex items-start gap-2">
                  <svg v-if="!answerInvented(answer)" class="mt-0.5 h-5 w-5 shrink-0 text-emerald-600 dark:text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                  </svg>
                  <svg v-else class="mt-0.5 h-5 w-5 shrink-0 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m9.75 9.75 4.5 4.5m0-4.5-4.5 4.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                  </svg>
                  <div class="min-w-0">
                    <p class="text-sm font-medium text-gray-900 dark:text-white">{{ index + 1 }}. {{ answer.question ?? answer.Question }}</p>
                    <p class="mt-1 text-sm text-gray-700 dark:text-gray-200">{{ answer.answer ?? answer.Answer }}</p>
                    <p v-if="answerInvented(answer) && (answer.judgeReason ?? answer.JudgeReason)" class="mt-1 text-sm text-red-700 dark:text-red-300">
                      {{ answer.judgeReason ?? answer.JudgeReason }}
                    </p>
                  </div>
                </div>
              </li>
            </ol>
          </template>
        </div>
        <div class="flex justify-end border-t border-gray-200 p-4 dark:border-gray-700">
          <button type="button" class="inline-flex min-h-[44px] items-center justify-center rounded-lg px-4 py-2.5 font-medium text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700" @click="closeAudit">
            Cerrar
          </button>
        </div>
      </div>
    </Transition>
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="questionsSliderOpen" class="fixed inset-0 z-50 bg-black/50" @click.self="closeQuestions" />
    </Transition>
    <Transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="questionsSliderOpen"
        class="fixed top-0 right-0 z-[51] flex h-full w-full max-w-xl flex-col border-l border-gray-200 bg-white shadow-xl dark:border-gray-700 dark:bg-gray-800"
        role="dialog"
        aria-modal="true"
        aria-labelledby="questions-slider-title"
      >
        <div class="flex items-start justify-between gap-3 border-b border-gray-200 p-4 dark:border-gray-700 sm:p-5">
          <div>
            <h2 id="questions-slider-title" class="text-lg font-semibold text-gray-900 dark:text-white">Preguntas de evaluación</h2>
            <p class="mt-1 text-sm text-gray-600 dark:text-gray-400">
              Plantilla v{{ evalVersion || '1' }}. Cada pregunta corre solo si ese tipo ya está publicado.
            </p>
          </div>
          <button type="button" class="p-2 -m-2 shrink-0 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300" aria-label="Cerrar" @click="closeQuestions">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div class="flex-1 space-y-6 overflow-y-auto p-4 sm:p-5">
          <p v-if="!evalItems.length" class="text-sm text-gray-500">No hay preguntas cargadas.</p>
          <section v-for="group in questionsByType.filter((item) => item.items.length)" :key="group.id" class="space-y-3">
            <div>
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white">{{ group.label }}</h3>
              <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">{{ group.description }}</p>
            </div>
            <div
              v-for="{ row, index } in group.items"
              :key="row.baseItemId || row.extraId || index"
              class="flex items-start gap-3 rounded-lg border border-gray-200 p-3 dark:border-gray-700"
              :class="row.isDisabled ? 'opacity-60' : ''"
            >
              <div class="min-w-0 flex-1 space-y-2">
                <select v-if="!row.baseItemId" v-model="row.type" class="h-10 rounded-lg border border-gray-300 bg-white px-3 text-sm dark:border-gray-700 dark:bg-gray-900">
                  <option v-for="item in types" :key="item.id" :value="item.id">{{ item.label }}</option>
                </select>
                <textarea v-model="row.question" rows="2" class="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm dark:border-gray-700 dark:bg-gray-900" />
              </div>
              <div class="flex shrink-0 flex-col items-center gap-1 pt-1">
                <FormToggle
                  :model-value="!row.isDisabled"
                  aria-label="Pregunta activa"
                  @update:model-value="setQuestionActive(row, $event)"
                />
                <span class="text-[11px] text-gray-500 dark:text-gray-400">{{ row.isDisabled ? 'Inactiva' : 'Activa' }}</span>
              </div>
            </div>
          </section>
          <div
            v-for="{ row, index } in extraQuestions"
            :id="row.clientKey ? `eval-item-${row.clientKey}` : undefined"
            :key="row.clientKey || row.extraId || index"
            class="flex items-start gap-3 rounded-lg border border-gray-200 p-3 dark:border-gray-700"
            :class="row.isDisabled ? 'opacity-60' : ''"
          >
            <div class="min-w-0 flex-1 space-y-2">
              <select v-model="row.type" class="h-10 rounded-lg border border-gray-300 bg-white px-3 text-sm dark:border-gray-700 dark:bg-gray-900">
                <option v-for="item in types" :key="item.id" :value="item.id">{{ item.label }}</option>
              </select>
              <textarea v-model="row.question" rows="2" class="w-full rounded-lg border border-gray-300 bg-white p-3 text-sm dark:border-gray-700 dark:bg-gray-900" placeholder="Escribe la pregunta" />
            </div>
            <div class="flex shrink-0 flex-col items-center gap-1 pt-1">
              <FormToggle
                :model-value="!row.isDisabled"
                aria-label="Pregunta activa"
                @update:model-value="setQuestionActive(row, $event)"
              />
              <span class="text-[11px] text-gray-500 dark:text-gray-400">{{ row.isDisabled ? 'Inactiva' : 'Activa' }}</span>
            </div>
          </div>
          <button
            type="button"
            class="flex min-h-[44px] w-full items-center gap-2 rounded-lg border border-dashed border-gray-300 px-3 py-3 text-sm font-medium text-gray-700 hover:border-primary-400 hover:bg-gray-50 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-gray-700/50"
            @click="addEvalItem"
          >
            <svg class="h-5 w-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            Agregar pregunta
          </button>
        </div>
        <div class="flex justify-end border-t border-gray-200 p-4 dark:border-gray-700">
          <button
            type="button"
            class="inline-flex min-h-[44px] items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 font-medium text-white transition-colors hover:bg-primary-700 disabled:opacity-50"
            :disabled="savingTests"
            @click="saveEvalItems"
          >
            Guardar
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

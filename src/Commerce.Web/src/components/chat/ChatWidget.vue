<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { FINBUCKLE_TENANT_HEADER, resolveTenantSlug, tenantApiPrefix } from '@/utils/tenant'
import { getEventTransport, getEventPollIntervalMs, loadEventStreamSettings } from '@/utils/eventTransport'
import { runSseEventTransport } from '@/composables/useSseEventTransport'
import { runPollEventTransport } from '@/composables/usePollEventTransport'

const props = defineProps({
  apiBase: { type: String, default: '' },
  tenant: { type: String, default: '' },
  embedded: { type: Boolean, default: false },
  /** Optional lock: buyer | manager. Empty = user picks (admin) or buyer (embed). */
  audience: { type: String, default: '' }
})

const open = ref(false)
const layout = ref('float')
const draft = ref('')
const busy = ref(false)
const error = ref('')
const threadId = ref('')
const threads = ref([])
const threadsLoading = ref(false)
const threadMenuOpen = ref(false)
const selectedAudience = ref(null)
const isFullLayout = computed(() => layout.value === 'full')
const resolvedAudience = computed(() => {
  if (props.audience === 'manager' || props.audience === 'buyer') return props.audience
  if (props.embedded) return 'buyer'
  return selectedAudience.value
})
const awaitingRole = computed(() => !resolvedAudience.value)
const roleLabel = computed(() =>
  resolvedAudience.value === 'manager' ? 'Admin' : resolvedAudience.value === 'buyer' ? 'Buyer' : ''
)
const activeThreadTitle = computed(() => {
  const current = threads.value.find((t) => t.threadId === threadId.value)
  if (current?.title) return current.title
  if (threadId.value) return 'Conversación'
  return 'Nueva conversación'
})
const messages = ref([])
/** Abort in-flight SSE so we don't leave RequestAborted races / retries running. */
let streamAbort = null
const logEl = ref(null)
const inputEl = ref(null)
const fileInput = ref(null)
const catalogView = ref('list')
const hasCatalog = computed(() => messages.value.some((item) => item.products?.length))
const carouselTracks = new Map()
/** Message indexes with expanded product list (float mode "Ver más"). */
const expandedProductIndexes = ref(new Set())
const FLOAT_PRODUCT_PREVIEW = 3

function setCarouselTrack(index, el) {
  if (el) carouselTracks.set(index, el)
  else carouselTracks.delete(index)
}

function scrollCarousel(index, direction) {
  const track = carouselTracks.get(index)
  if (!track) return
  const step = Math.max(260, Math.round(track.clientWidth * 0.75))
  track.scrollBy({ left: step * direction, behavior: 'smooth' })
}

function visibleProducts(item, index) {
  const all = Array.isArray(item?.products) ? item.products : []
  if (isFullLayout.value || expandedProductIndexes.value.has(index)) return all
  return all.slice(0, FLOAT_PRODUCT_PREVIEW)
}

function hiddenProductCount(item, index) {
  const total = Array.isArray(item?.products) ? item.products.length : 0
  if (isFullLayout.value || expandedProductIndexes.value.has(index)) return 0
  return Math.max(0, total - FLOAT_PRODUCT_PREVIEW)
}

function expandProducts(index) {
  const next = new Set(expandedProductIndexes.value)
  next.add(index)
  expandedProductIndexes.value = next
}
const micSupported = typeof MediaRecorder !== 'undefined' && !!navigator.mediaDevices?.getUserMedia
const recording = ref(false)
const recordingMs = ref(0)
const audioMode = ref('voice')
const showAudioMenu = ref(false)
const transcribing = ref(false)
const imageFile = ref(null)
const imagePreviewUrl = ref('')
const canSend = computed(
  () => !!resolvedAudience.value && (!!draft.value.trim() || !!imageFile.value)
)

const roleOptions = [
  {
    id: 'buyer',
    title: 'Buyer',
    blurb: 'Buscar productos, comparar, carrito y checkout. También con foto.'
  },
  {
    id: 'manager',
    title: 'Admin',
    blurb: 'Gestionar catálogo: crear productos (con foto), marcas y categorías.'
  }
]

function welcomeFor(audience) {
  return audience === 'manager'
    ? 'Modo Admin. Puedo ayudarte con el catálogo: crear o editar productos (también con foto), marcas y categorías.'
    : 'Modo Buyer. Preguntame por el catálogo, carrito, checkout o soporte. También podés adjuntar una foto para buscar.'
}

function selectRole(audience) {
  if (audience !== 'buyer' && audience !== 'manager') return
  if (busy.value) return
  selectedAudience.value = audience
  startNewConversation(audience)
  void loadThreads()
}

function changeRole() {
  if (busy.value || props.embedded || props.audience) return
  selectedAudience.value = null
  threadId.value = ''
  threads.value = []
  threadMenuOpen.value = false
  error.value = ''
  clearImage()
  draft.value = ''
  messages.value = []
}

function startNewConversation(audience = resolvedAudience.value) {
  if (busy.value) return
  streamAbort?.abort()
  threadId.value = ''
  threadMenuOpen.value = false
  expandedProductIndexes.value = new Set()
  error.value = ''
  clearImage()
  draft.value = ''
  messages.value = audience
    ? [{ role: 'assistant', text: welcomeFor(audience) }]
    : []
  void nextTick(() => inputEl.value?.focus())
}

function chatApiPrefix() {
  const tenant = props.tenant || resolveTenantSlug()
  return tenant ? `/t/${tenant}` : tenantApiPrefix()
}

function chatHeaders() {
  return {
    [FINBUCKLE_TENANT_HEADER]: props.tenant || resolveTenantSlug()
  }
}

async function loadThreads() {
  if (!resolvedAudience.value) {
    threads.value = []
    return
  }
  threadsLoading.value = true
  try {
    const response = await fetch(
      `${resolveApiBase()}${chatApiPrefix()}/chat/threads?audience=${encodeURIComponent(resolvedAudience.value)}`,
      { headers: chatHeaders() }
    )
    const payload = await response.json().catch(() => ({}))
    if (!response.ok) throw new Error(payload.error || 'No se pudieron cargar las conversaciones.')
    threads.value = Array.isArray(payload.threads)
      ? payload.threads.map((t) => ({
          threadId: t.threadId || t.ThreadId || '',
          title: t.title || t.Title || 'Conversación',
          updatedAt: t.updatedAt || t.UpdatedAt || null,
          messageCount: t.messageCount ?? t.MessageCount ?? 0
        })).filter((t) => t.threadId)
      : []
  } catch (err) {
    error.value = err?.message || 'No se pudieron cargar las conversaciones.'
  } finally {
    threadsLoading.value = false
  }
}

async function selectThread(id) {
  if (!id || busy.value || id === threadId.value) {
    threadMenuOpen.value = false
    return
  }
  if (!resolvedAudience.value) return
  streamAbort?.abort()
  threadMenuOpen.value = false
  busy.value = true
  error.value = ''
  clearImage()
  draft.value = ''
  try {
    const response = await fetch(
      `${resolveApiBase()}${chatApiPrefix()}/chat/threads/${encodeURIComponent(id)}?audience=${encodeURIComponent(resolvedAudience.value)}`,
      { headers: chatHeaders() }
    )
    const payload = await response.json().catch(() => ({}))
    if (!response.ok) throw new Error(payload.error || 'No se pudo abrir la conversación.')
    threadId.value = id
    const history = Array.isArray(payload.messages) ? payload.messages : []
    messages.value = history.length
      ? history.map((m) => hydrateHistoryMessage(m))
      : [{ role: 'assistant', text: welcomeFor(resolvedAudience.value) }]
    expandedProductIndexes.value = new Set()
    await scrollLog()
  } catch (err) {
    error.value = err?.message || 'No se pudo abrir la conversación.'
  } finally {
    busy.value = false
    void nextTick(() => inputEl.value?.focus())
  }
}

function formatThreadTime(value) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return date.toLocaleString('es', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function upsertThreadInList(id, titleHint) {
  if (!id) return
  const existing = threads.value.find((t) => t.threadId === id)
  const title = titleHint || existing?.title || 'Conversación'
  const next = {
    threadId: id,
    title,
    updatedAt: new Date().toISOString(),
    messageCount: existing?.messageCount ?? 0
  }
  threads.value = [next, ...threads.value.filter((t) => t.threadId !== id)]
}
let mediaRecorder = null
let mediaStream = null
let recordChunks = []
let recordTimer = null
let recordStartedAt = 0

function toggleDock(side) {
  layout.value = layout.value === side ? 'float' : side
  threadMenuOpen.value = false
}

function toggleFull() {
  layout.value = layout.value === 'full' ? 'float' : 'full'
  threadMenuOpen.value = false
  if (layout.value === 'full' && resolvedAudience.value) void loadThreads()
}

function toggleThreadMenu() {
  if (!resolvedAudience.value || busy.value) return
  threadMenuOpen.value = !threadMenuOpen.value
  if (threadMenuOpen.value) void loadThreads()
}

function openChat() {
  open.value = !open.value
  threadMenuOpen.value = false
  if (open.value && resolvedAudience.value) void loadThreads()
}

function resolveApiBase() {
  if (props.apiBase) return props.apiBase.replace(/\/$/, '')
  return (import.meta.env.VITE_API_URL || '/api').replace(/\/$/, '')
}

function clearImage() {
  if (imagePreviewUrl.value) URL.revokeObjectURL(imagePreviewUrl.value)
  imagePreviewUrl.value = ''
  imageFile.value = null
  if (fileInput.value) fileInput.value.value = ''
}

function detachImageForSend() {
  const preview = imagePreviewUrl.value
  const file = imageFile.value
  imagePreviewUrl.value = ''
  imageFile.value = null
  if (fileInput.value) fileInput.value.value = ''
  return { file, preview }
}

function setImageFile(file) {
  if (!file || !file.type?.startsWith('image/')) {
    error.value = 'Selecciona un archivo de imagen válido'
    return
  }
  if (file.size > 8 * 1024 * 1024) {
    error.value = 'La imagen debe pesar máximo 8MB'
    return
  }
  clearImage()
  imageFile.value = file
  imagePreviewUrl.value = URL.createObjectURL(file)
  error.value = ''
}

function onImagePick(event) {
  const file = event.target?.files?.[0]
  if (file) setImageFile(file)
}

function openImagePicker() {
  if (busy.value || recording.value || transcribing.value) return
  fileInput.value?.click()
}

async function send(intent) {
  if (!resolvedAudience.value || busy.value) return
  const message = (intent?.message || draft.value).trim()
  let file = intent?.imageFile || null
  let preview = intent?.imagePreview || ''
  if (!intent?.imageFile && imageFile.value) {
    const detached = detachImageForSend()
    file = detached.file
    preview = detached.preview
  }
  const visible = (intent?.display || message || (file ? '📷 Imagen adjunta' : '')).trim()
  if ((!message && !file)) return
  if (!intent?.message) draft.value = ''
  error.value = ''
  messages.value.push({ role: 'user', text: visible, imageUrl: preview || '' })
  const assistant = { role: 'assistant', text: '', tools: [], status: 'Pensando…', statuses: [] }
  messages.value.push(assistant)
  busy.value = true
  await scrollLog()

  streamAbort?.abort()
  streamAbort = new AbortController()

  try {
    const tenant = props.tenant || resolveTenantSlug()
    const prefix = tenant ? `/t/${tenant}` : tenantApiPrefix()
    const headers = {
      [FINBUCKLE_TENANT_HEADER]: props.tenant || resolveTenantSlug()
    }
    let body
    if (file) {
      const form = new FormData()
      form.append('message', message)
      if (threadId.value) form.append('threadId', threadId.value)
      form.append('audience', resolvedAudience.value)
      form.append('image', file, file.name || 'photo.jpg')
      body = form
    } else {
      headers['Content-Type'] = 'application/json'
      body = JSON.stringify({
        message,
        threadId: threadId.value || null,
        audience: resolvedAudience.value
      })
    }

    const onEvent = async (payload) => {
      if (payload.threadId) {
        threadId.value = payload.threadId
        const hint = messages.value.find((m) => m.role === 'user' && m.text)?.text
        upsertThreadInList(payload.threadId, hint)
      }
      if (payload.type === 'status' && payload.text) {
        assistant.status = payload.text
        assistant.statuses = [...(assistant.statuses || []), payload.text]
        await scrollLog()
      } else if (payload.type === 'token' && payload.text) {
        assistant.text += payload.text
        assistant.status = ''
        await scrollLog()
      } else if (payload.type === 'output' && payload.text) {
        assistant.status = ''
        applyAssistantOutput(assistant, payload.text)
      } else if (payload.type === 'tool' && payload.toolName) {
        assistant.tools = [...(assistant.tools || []), payload.toolName]
        if (!assistant.status) {
          assistant.status = toolStatusLabel(payload.toolName)
        }
      } else if (payload.type === 'error') {
        assistant.status = ''
        error.value = payload.text || 'The agent failed.'
      } else if (payload.type === 'done') {
        assistant.status = ''
      }
    }

    await loadEventStreamSettings({
      apiBase: resolveApiBase(),
      tenantPrefix: prefix,
      headers: { [FINBUCKLE_TENANT_HEADER]: props.tenant || resolveTenantSlug() },
      signal: streamAbort.signal
    })

    if (getEventTransport() === 'poll') {
      await runPollEventTransport({
        headers,
        signal: streamAbort.signal,
        intervalMs: getEventPollIntervalMs(),
        onEvent,
        onStarted: (started) => {
          if (started.threadId) {
            threadId.value = started.threadId
            const hint = messages.value.find((m) => m.role === 'user' && m.text)?.text
            upsertThreadInList(started.threadId, hint)
          }
        },
        start: async () => {
          const res = await fetch(`${resolveApiBase()}${prefix}/chat/runs`, {
            method: 'POST',
            headers,
            body,
            signal: streamAbort.signal
          })
          if (!res.ok) {
            const errBody = await res.json().catch(() => ({}))
            throw new Error(errBody.error || `Chat run failed (${res.status}).`)
          }
          return res.json()
        },
        eventsUrl: (streamId, after) => {
          const q = after ? `?after=${encodeURIComponent(after)}` : ''
          return `${resolveApiBase()}${prefix}/event-streams/${encodeURIComponent(streamId)}/events${q}`
        }
      })
    } else {
      await runSseEventTransport({
        url: `${resolveApiBase()}${prefix}/chat/stream`,
        method: 'POST',
        headers,
        body,
        signal: streamAbort.signal,
        onEvent
      })
    }
  } catch (err) {
    if (err?.name === 'AbortError') {
      // User navigated away / new send aborted previous turn.
    } else {
      error.value = err?.message || 'Could not reach the agent.'
    }
  } finally {
    assistant.status = ''
    if (!assistant.text) {
      if (assistant.products?.length) {
        assistant.text = `Encontré ${assistant.products.length} opciones.`
        if (!assistant.outputType || assistant.outputType === 'message') {
          assistant.outputType = 'products'
        }
      } else if (showCartPanel(assistant)) {
        assistant.text = 'Aquí está tu carrito.'
        assistant.outputType = 'cart'
      } else {
        assistant.text = 'No reply.'
      }
    }
    busy.value = false
    if (threadId.value) {
      const hint = messages.value.find((m) => m.role === 'user' && m.text)?.text
      upsertThreadInList(threadId.value, hint)
      void loadThreads()
    }
    await scrollLog()
  }
}
function hydrateHistoryMessage(m) {
  const role = String(m.role || m.Role || 'assistant').toLowerCase() === 'user' ? 'user' : 'assistant'
  const raw = String(m.text || m.Text || '')
  const item = { role, text: raw, tools: [] }
  if (role === 'assistant') applyAssistantOutput(item, raw)
  return item
}

/** Shared by SSE `output` and thread history reload. */
function applyAssistantOutput(item, raw) {
  const text = String(raw || '').trim()
  if (!text) return
  item.output = text
  if (!text.startsWith('{')) return
  try {
    let parsed = JSON.parse(text)
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return
    // Backend may recover truncated agent JSON as a single product card — promote it.
    if (!Array.isArray(parsed.products) && (parsed.sku || parsed.name)) {
      parsed = {
        type: 'products',
        message: typeof parsed.message === 'string' && parsed.message.trim()
          ? parsed.message.trim()
          : 'Encontré 1 opción.',
        products: [parsed],
        cart: null
      }
    }
    if (typeof parsed.message === 'string' && parsed.message.trim()) {
      item.text = parsed.message.trim()
    } else if (!item.text && Array.isArray(parsed.products) && parsed.products.length) {
      item.text = `Encontré ${parsed.products.length} opciones.`
    } else if (!item.text && parsed.cart && typeof parsed.cart === 'object') {
      item.text = 'Aquí está tu carrito.'
    }
    item.products = catalogProducts(parsed)
    item.cart = parsed.cart && typeof parsed.cart === 'object' ? parsed.cart : null
    applyOutputType(item, parsed)
  } catch {
    // Plain text or partial stream payload.
  }
}

function catalogProducts(parsed) {
  if (!Array.isArray(parsed?.products)) return []
  return parsed.products.filter((product) => product && (product.name || product.sku))
}

/** UI contract from Agent Framework JSON output (`type` in agent output.schema). */
function applyOutputType(assistant, parsed) {
  const kind = typeof parsed?.type === 'string' ? parsed.type.trim() : ''
  if (kind === 'cart' || kind === 'products' || kind === 'approval_needed' || kind === 'message') {
    assistant.outputType = kind
  } else if (parsed?.cart && (parsed.cart.item_count > 0 || (parsed.cart.items && parsed.cart.items.length))) {
    assistant.outputType = 'cart'
  } else if (Array.isArray(parsed?.products) && parsed.products.length) {
    assistant.outputType = 'products'
  } else {
    assistant.outputType = 'message'
  }
  assistant.approvalPending = kind === 'approval_needed'
  assistant.draft = parsed?.draft && typeof parsed.draft === 'object' ? parsed.draft : null
  if (!assistant.approvalPending) {
    assistant.approvalResolved = assistant.approvalResolved || null
  }
}

function toolStatusLabel(toolName) {
  const key = String(toolName || '').toLowerCase()
  const map = {
    search_products: 'Buscando en el catálogo…',
    compare_products: 'Comparando productos…',
    get_product_details: 'Cargando detalle…',
    get_cart: 'Revisando tu carrito…',
    create_cart: 'Creando el carrito…',
    add_item: 'Agregando al carrito…',
    update_item_qty: 'Actualizando cantidades…',
    remove_item: 'Quitando del carrito…',
    apply_coupon: 'Aplicando cupón…',
    remove_coupon: 'Quitando cupón…',
    'intent-classifier': 'Analizando tu intención…'
  }
  return map[key] || `Ejecutando ${toolName}…`
}

function formatCartMoney(amount, currency) {
  if (amount == null || amount === '') return ''
  const n = Number(amount)
  if (!Number.isFinite(n)) return ''
  const formatted = n.toLocaleString('es-PE', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
  return currency ? `${currency} ${formatted}` : `S/ ${formatted}`
}

function showCartPanel(item) {
  if (!item?.cart) return false
  if (item.outputType === 'cart') return true
  return Number(item.cart.item_count) > 0 || (Array.isArray(item.cart.items) && item.cart.items.length > 0)
}

function cartPaymentMethods(cart) {
  if (!cart || !Array.isArray(cart.payment_methods)) return []
  return cart.payment_methods.filter((pm) => pm && (pm.name || pm.description || pm.code || pm.payment_method_id))
}

function cartShippingRows(cart) {
  if (!cart || !Array.isArray(cart.shipping_availables)) return []
  return cart.shipping_availables.filter((row) => row && (row.seller_id || row.seller_name))
}

function shippingHint(ship) {
  const count = Number(ship?.options_count)
  if (Number.isFinite(count) && count > 0) {
    return count === 1 ? '1 opción de envío' : `${count} opciones de envío`
  }
  if (ship?.has_shipping) return 'Envío disponible'
  return 'Sin opciones de envío aún'
}

function respondApproval(item, approved) {
  if (!item?.approvalPending || busy.value) return
  item.approvalPending = false
  item.approvalResolved = approved ? 'approved' : 'cancelled'
  if (approved) {
    send({
      display: 'Aprobar',
      message: 'Apruebo. Procede con la acción propuesta (crea o aplica los cambios).'
    })
    return
  }
  send({
    display: 'Cancelar',
    message: 'Cancelo. No ejecutes la acción propuesta ni crees nada.'
  })
}

function draftLines(draft) {
  if (!draft || typeof draft !== 'object') return []
  const lines = []
  if (draft.brand?.name) {
    lines.push(`Marca: ${draft.brand.name}${draft.brand.is_new ? ' (nueva)' : ''}`)
  }
  if (draft.category?.name) {
    lines.push(`Categoría: ${draft.category.name}${draft.category.is_new ? ' (nueva)' : ''}`)
  }
  if (draft.product?.title) {
    lines.push(`Producto: ${draft.product.title}`)
  }
  return lines
}

function productLetter(product) {
  const source = String(product?.name || product?.sku || '?').trim()
  return (source.match(/[A-Za-z0-9]/)?.[0] || '?').toUpperCase()
}

function productImage(product) {
  const url = product?.image_url || product?.imageUrl || ''
  return typeof url === 'string' && url.trim() ? url.trim() : ''
}

function formatPrice(product) {
  const from = Number(product?.price_from ?? product?.price)
  const to = Number(product?.price_to)
  const currency = product?.currency || ''
  if (!Number.isFinite(from)) return ''
  const fmt = (n) => n.toLocaleString('es-PE', { minimumFractionDigits: 0, maximumFractionDigits: 2 })
  if (Number.isFinite(to) && to > from) {
    const range = `${fmt(from)} – ${fmt(to)}`
    return currency ? `${currency} ${range}` : range
  }
  return currency ? `${currency} ${fmt(from)}` : fmt(from)
}

function productSizes(product) {
  if (Array.isArray(product?.sizes) && product.sizes.length) {
    return product.sizes.map((s) => String(s).trim()).filter(Boolean).slice(0, 8)
  }
  if (product?.size) return [String(product.size).trim()].filter(Boolean)
  return []
}

function productColors(product) {
  if (Array.isArray(product?.colors) && product.colors.length) {
    return product.colors.map((c) => String(c).trim()).filter(Boolean).slice(0, 6)
  }
  if (product?.color) return [String(product.color).trim()].filter(Boolean)
  return []
}

function productOptionsSummary(product) {
  if (typeof product?.options_summary === 'string' && product.options_summary.trim()) {
    return product.options_summary.trim()
  }
  return ''
}

function stockLabel(product) {
  if (product?.stock == null || product.stock === '') return ''
  const stock = Number(product.stock)
  if (!Number.isFinite(stock)) return ''
  if (stock <= 0) return 'No disponible'
  return stock === 1 ? '1 en stock' : `${stock} en stock`
}

function productLabel(product) {
  const name = product?.name || product?.sku || 'producto'
  const sku = product?.sku_id || product?.sku
  return sku ? `${name} (SKU ${sku})` : name
}

function canPurchase(product) {
  if (!(product?.sku_id || product?.sku) || busy.value) return false
  if (product.stock == null || product.stock === '') return true
  const stock = Number(product.stock)
  return !Number.isFinite(stock) || stock > 0
}

function askDetail(product) {
  const sku = product?.sku_id || product?.sku || product?.name
  if (!sku || busy.value) return
  send({
    display: `Ver detalle de ${product.name || sku}`,
    message: `Quiero el detalle de ${productLabel(product)}. Usa get_product_details con product_id=${product.sku || sku}. No busques por texto.`
  })
}

function askAddToCart(product) {
  if (!canPurchase(product)) return
  const skuId = product.sku_id || product.sku
  const sellerId = product.seller_id || product.sellerId || ''
  send({
    display: `Agregar al carrito: ${product.name || skuId}`,
    message: sellerId
      ? `Agrega al carrito 1 unidad. add_item con sku_id=${skuId} y seller_id=${sellerId}.`
      : `Agrega al carrito 1 unidad. add_item con sku_id=${skuId}.`
  })
}

function askBuy(product) {
  if (!canPurchase(product)) return
  const skuId = product.sku_id || product.sku
  const sellerId = product.seller_id || product.sellerId || ''
  send({
    display: `Comprar ${product.name || skuId}`,
    message: sellerId
      ? `Quiero comprar y pagar 1 unidad. Primero add_item con sku_id=${skuId} y seller_id=${sellerId}.`
      : `Quiero comprar y pagar 1 unidad de ${productLabel(product)}.`
  })
}

function formatRecordingTime(ms) {
  const total = Math.floor(ms / 1000)
  return `${Math.floor(total / 60)}:${String(total % 60).padStart(2, '0')}`
}

function pickAudioMime() {
  const candidates = ['audio/webm;codecs=opus', 'audio/webm', 'audio/mp4', 'audio/ogg;codecs=opus']
  return candidates.find((candidate) => MediaRecorder.isTypeSupported(candidate)) || ''
}

function stopRecordingCleanup() {
  if (recordTimer) {
    clearInterval(recordTimer)
    recordTimer = null
  }
  mediaRecorder = null
  mediaStream?.getTracks().forEach((track) => track.stop())
  mediaStream = null
  recordChunks = []
  recording.value = false
  recordingMs.value = 0
}

function toggleAudioMenu() {
  if (busy.value || transcribing.value || !micSupported) return
  if (recording.value) {
    void stopAndSendRecording()
    return
  }
  showAudioMenu.value = !showAudioMenu.value
}

async function startRecording(mode) {
  if (busy.value || transcribing.value || recording.value) return
  error.value = ''
  showAudioMenu.value = false
  audioMode.value = mode
  if (!micSupported) {
    error.value = 'Tu navegador no permite grabar audio.'
    return
  }
  try {
    mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true })
    const mime = pickAudioMime()
    recordChunks = []
    mediaRecorder = mime ? new MediaRecorder(mediaStream, { mimeType: mime }) : new MediaRecorder(mediaStream)
    mediaRecorder.ondataavailable = (event) => {
      if (event.data.size > 0) recordChunks.push(event.data)
    }
    mediaRecorder.start(250)
    recording.value = true
    recordStartedAt = Date.now()
    recordingMs.value = 0
    recordTimer = setInterval(() => {
      recordingMs.value = Date.now() - recordStartedAt
      if (recordingMs.value >= 120000) void stopAndSendRecording()
    }, 200)
  } catch {
    stopRecordingCleanup()
    error.value = 'No pudimos acceder al micrófono. Revisa los permisos.'
  }
}

function cancelRecording() {
  stopRecordingCleanup()
  error.value = ''
  audioMode.value = 'voice'
}

async function stopAndSendRecording() {
  if (!mediaRecorder || mediaRecorder.state === 'inactive') {
    stopRecordingCleanup()
    return
  }
  const mode = audioMode.value
  const recorder = mediaRecorder
  const mimeType = recorder.mimeType || pickAudioMime() || 'audio/webm'
  const blob = await new Promise((resolve) => {
    recorder.onstop = () => resolve(new Blob(recordChunks, { type: mimeType }))
    try {
      recorder.stop()
    } catch {
      resolve(new Blob(recordChunks, { type: mimeType }))
    }
  })
  mediaStream?.getTracks().forEach((track) => track.stop())
  mediaStream = null
  mediaRecorder = null
  if (recordTimer) {
    clearInterval(recordTimer)
    recordTimer = null
  }
  const duration = recordingMs.value
  recording.value = false
  recordingMs.value = 0
  recordChunks = []
  if (duration < 800 || blob.size < 200) {
    error.value = 'La nota es muy corta. Mantén la grabación un momento más.'
    return
  }
  await sendAudioBlob(blob, mimeType, mode)
}

async function sendAudioBlob(blob, mimeType, mode) {
  transcribing.value = true
  error.value = ''
  try {
    const text = await transcribe(blob, mimeType)
    if (mode === 'dictation') {
      draft.value = draft.value.trim() ? `${draft.value.trim()} ${text}` : text
      await nextTick()
      inputEl.value?.focus()
      return
    }
    await send({ display: text, message: text })
  } catch (err) {
    error.value = err?.message || 'No se pudo transcribir el audio.'
  } finally {
    transcribing.value = false
  }
}

async function transcribe(blob, mimeType) {
  const tenant = props.tenant || resolveTenantSlug()
  const prefix = tenant ? `/t/${tenant}` : tenantApiPrefix()
  const ext = mimeType.includes('mp4') ? 'm4a' : mimeType.includes('ogg') ? 'ogg' : 'webm'
  const file = new File([blob], `nota-voz.${ext}`, { type: mimeType.split(';')[0] || 'audio/webm' })
  const body = new FormData()
  body.append('audio', file)
  const response = await fetch(`${resolveApiBase()}${prefix}/chat/transcribe`, {
    method: 'POST',
    headers: { [FINBUCKLE_TENANT_HEADER]: tenant },
    body
  })
  const payload = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(payload.error || 'No se pudo transcribir el audio.')
  const text = String(payload.text || '').trim()
  if (!text) throw new Error('No se pudo transcribir el audio.')
  return text
}

onMounted(() => {
  if (resolvedAudience.value) {
    messages.value = [{ role: 'assistant', text: welcomeFor(resolvedAudience.value) }]
    void loadThreads()
  }
  const tenant = props.tenant || resolveTenantSlug()
  const prefix = tenant ? `/t/${tenant}` : tenantApiPrefix()
  void loadEventStreamSettings({
    apiBase: resolveApiBase(),
    tenantPrefix: prefix,
    headers: { [FINBUCKLE_TENANT_HEADER]: props.tenant || resolveTenantSlug() }
  })
})

onBeforeUnmount(() => {
  streamAbort?.abort()
  stopRecordingCleanup()
  clearImage()
})

async function scrollLog() {
  await nextTick()
  if (logEl.value) logEl.value.scrollTop = logEl.value.scrollHeight
}
</script>

<template>
  <div
    class="commerce-chat"
    :data-embedded="embedded ? 'true' : 'false'"
    :data-layout="!open && layout !== 'left' && layout !== 'right' ? 'float' : layout"
    :data-open="open ? 'true' : 'false'"
  >
    <div v-if="open" class="commerce-chat__panel" role="dialog" aria-label="Kutria chat">
      <aside
        v-if="isFullLayout && resolvedAudience"
        class="commerce-chat__sidebar"
        aria-label="Conversaciones"
      >
        <button
          type="button"
          class="commerce-chat__new-chat"
          :disabled="busy"
          @click="startNewConversation()"
        >
          + Nueva conversación
        </button>
        <p v-if="threadsLoading" class="commerce-chat__sidebar-empty">Cargando…</p>
        <p v-else-if="!threads.length" class="commerce-chat__sidebar-empty">Sin conversaciones aún</p>
        <ul v-else class="commerce-chat__thread-list" role="list">
          <li v-for="item in threads" :key="item.threadId">
            <button
              type="button"
              class="commerce-chat__thread-item"
              :class="{ 'is-active': item.threadId === threadId }"
              :disabled="busy"
              @click="selectThread(item.threadId)"
            >
              <span class="commerce-chat__thread-title">{{ item.title || 'Conversación' }}</span>
              <span v-if="formatThreadTime(item.updatedAt)" class="commerce-chat__thread-time">
                {{ formatThreadTime(item.updatedAt) }}
              </span>
            </button>
          </li>
        </ul>
      </aside>

      <div class="commerce-chat__main">
      <header class="commerce-chat__header">
        <span class="commerce-chat__title">
          <img src="/brand/kutria-isotipo.svg" alt="" width="18" height="18" />
          Kutria
          <span v-if="roleLabel" class="commerce-chat__role-badge">{{ roleLabel }}</span>
          <span v-if="resolvedAudience && isFullLayout" class="commerce-chat__thread-label">{{ activeThreadTitle }}</span>
        </span>
        <div class="commerce-chat__header-actions">
          <button
            v-if="resolvedAudience && !isFullLayout"
            type="button"
            class="commerce-chat__icon-btn"
            :disabled="busy"
            aria-label="Nueva conversación"
            title="Nueva conversación"
            @click="startNewConversation()"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
            </svg>
          </button>
          <div v-if="resolvedAudience && !isFullLayout" class="commerce-chat__history">
            <button
              type="button"
              class="commerce-chat__icon-btn"
              :class="{ 'is-active': threadMenuOpen }"
              :disabled="busy"
              aria-label="Historial de conversaciones"
              title="Historial"
              :aria-expanded="threadMenuOpen"
              @click="toggleThreadMenu"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M4 6h16M4 12h16M4 18h10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
              </svg>
            </button>
            <div v-if="threadMenuOpen" class="commerce-chat__history-menu" role="menu">
              <p v-if="threadsLoading" class="commerce-chat__sidebar-empty">Cargando…</p>
              <p v-else-if="!threads.length" class="commerce-chat__sidebar-empty">Sin conversaciones</p>
              <button
                v-for="item in threads"
                :key="item.threadId"
                type="button"
                role="menuitem"
                class="commerce-chat__history-item"
                :class="{ 'is-active': item.threadId === threadId }"
                :disabled="busy"
                @click="selectThread(item.threadId)"
              >
                <span>{{ item.title || 'Conversación' }}</span>
                <span v-if="formatThreadTime(item.updatedAt)">{{ formatThreadTime(item.updatedAt) }}</span>
              </button>
            </div>
          </div>
          <button
            v-if="resolvedAudience && !embedded && !audience"
            type="button"
            class="commerce-chat__icon-btn"
            :disabled="busy"
            aria-label="Cambiar modo"
            title="Cambiar modo"
            @click="changeRole"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M16 4h4v4M8 20H4v-4M20 4l-7.5 7.5M4 20l7.5-7.5" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
          </button>
          <div v-if="hasCatalog" class="commerce-chat__view" role="group" aria-label="Catalog view">
            <button
              type="button"
              :class="{ 'is-active': catalogView === 'list' }"
              :aria-pressed="catalogView === 'list'"
              @click="catalogView = 'list'"
            >Lista</button>
            <button
              type="button"
              :class="{ 'is-active': catalogView === 'text' }"
              :aria-pressed="catalogView === 'text'"
              @click="catalogView = 'text'"
            >Texto</button>
          </div>
          <button
            type="button"
            class="commerce-chat__layout"
            :class="{ 'is-active': layout === 'left' }"
            :aria-pressed="layout === 'left'"
            aria-label="Anclar a la izquierda"
            @click="toggleDock('left')"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M4 5v14M9 7h11v10H9z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round" />
            </svg>
          </button>
          <button
            type="button"
            class="commerce-chat__layout"
            :class="{ 'is-active': layout === 'right' }"
            :aria-pressed="layout === 'right'"
            aria-label="Anclar a la derecha"
            @click="toggleDock('right')"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M20 5v14M4 7h11v10H4z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round" />
            </svg>
          </button>
          <button
            type="button"
            class="commerce-chat__layout"
            :class="{ 'is-active': layout === 'full' }"
            :aria-pressed="layout === 'full'"
            aria-label="Pantalla completa"
            @click="toggleFull"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M8 4H4v4M16 4h4v4M20 16v4h-4M4 16v4h4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
          </button>
          <button type="button" class="commerce-chat__close" aria-label="Close chat" @click="open = false">×</button>
        </div>
      </header>
      <div ref="logEl" class="commerce-chat__log">
        <div v-if="awaitingRole" class="commerce-chat__role-pick" role="group" aria-label="Elegir modo">
          <p class="commerce-chat__role-pick-title">¿Cómo querés usar el chat?</p>
          <p class="commerce-chat__role-pick-hint">Cada modo usa agentes distintos (Buyer vs Admin).</p>
          <div class="commerce-chat__role-cards">
            <button
              v-for="option in roleOptions"
              :key="option.id"
              type="button"
              class="commerce-chat__role-card"
              @click="selectRole(option.id)"
            >
              <span class="commerce-chat__role-card-title">{{ option.title }}</span>
              <span class="commerce-chat__role-card-blurb">{{ option.blurb }}</span>
            </button>
          </div>
        </div>
        <template v-for="(item, index) in messages" :key="index">
          <div :class="['commerce-chat__turn', item.role === 'user' ? 'commerce-chat__turn--user' : 'commerce-chat__turn--assistant']">
            <div class="commerce-chat__avatar" :aria-hidden="true">
              <svg v-if="item.role === 'user'" width="18" height="18" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="8" r="3.5" stroke="currentColor" stroke-width="1.7" />
                <path d="M5 19c1.5-3.2 3.8-4.8 7-4.8s5.5 1.6 7 4.8" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" />
              </svg>
              <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none">
                <rect x="4" y="7" width="16" height="11" rx="3" stroke="currentColor" stroke-width="1.7" />
                <path d="M9 7V5.5A3 3 0 0 1 12 2.5 3 3 0 0 1 15 5.5V7" stroke="currentColor" stroke-width="1.7" />
                <circle cx="9" cy="12.5" r="1.1" fill="currentColor" />
                <circle cx="15" cy="12.5" r="1.1" fill="currentColor" />
                <path d="M9.5 16h5" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" />
              </svg>
            </div>
            <div class="commerce-chat__turn-body">
              <p class="commerce-chat__speaker">{{ item.role === 'user' ? 'Tú' : 'Asistente IA' }}</p>
              <div :class="['commerce-chat__bubble', item.role === 'user' ? 'commerce-chat__bubble--user' : 'commerce-chat__bubble--assistant']">
                <img
                  v-if="item.imageUrl"
                  :src="item.imageUrl"
                  alt=""
                  class="commerce-chat__bubble-image"
                />
                <span v-if="item.text">{{ item.text }}</span>
                <p v-if="item.role === 'assistant' && item.status" class="commerce-chat__thinking">
                  <span class="commerce-chat__thinking-dot" aria-hidden="true" />
                  {{ item.status }}
                </p>
                <ul v-if="item.draft && draftLines(item.draft).length" class="commerce-chat__draft">
                  <li v-for="line in draftLines(item.draft)" :key="line">{{ line }}</li>
                </ul>
              </div>
            <div
              v-if="item.outputType === 'approval_needed'"
              class="commerce-chat__approval"
              role="group"
              aria-label="Confirmación"
            >
              <template v-if="item.approvalPending">
                <button
                  type="button"
                  class="commerce-chat__approval-btn commerce-chat__approval-btn--cancel"
                  :disabled="busy"
                  @click="respondApproval(item, false)"
                >
                  Cancelar
                </button>
                <button
                  type="button"
                  class="commerce-chat__approval-btn commerce-chat__approval-btn--approve"
                  :disabled="busy"
                  @click="respondApproval(item, true)"
                >
                  Aprobar
                </button>
              </template>
              <p v-else-if="item.approvalResolved" class="commerce-chat__approval-status">
                {{ item.approvalResolved === 'approved' ? 'Aprobado' : 'Cancelado' }}
              </p>
            </div>
            <div
              v-if="item.products?.length && catalogView === 'list' && item.outputType !== 'cart'"
              class="commerce-chat__cards"
              :class="{ 'commerce-chat__cards--carousel': isFullLayout }"
              :aria-label="`${item.products.length} productos`"
            >
              <button
                v-if="isFullLayout && item.products.length > 1"
                type="button"
                class="commerce-chat__carousel-nav commerce-chat__carousel-nav--prev"
                aria-label="Productos anteriores"
                @click="scrollCarousel(index, -1)"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M15 6l-6 6 6 6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </button>
              <div
                class="commerce-chat__carousel-track"
                role="list"
                :ref="(el) => setCarouselTrack(index, el)"
              >
                <article
                  v-for="product in visibleProducts(item, index)"
                  :key="product.sku || product.name"
                  class="commerce-chat__card"
                  role="listitem"
                >
                  <button
                    type="button"
                    class="commerce-chat__card-open"
                    :disabled="busy || !(product.sku || product.name)"
                    @click="askDetail(product)"
                  >
                    <img
                      v-if="productImage(product)"
                      class="commerce-chat__card-thumb"
                      :src="productImage(product)"
                      alt=""
                      loading="lazy"
                    />
                    <span v-else class="commerce-chat__card-mark" aria-hidden="true">{{ productLetter(product) }}</span>
                    <span class="commerce-chat__card-body">
                      <span class="commerce-chat__card-name">{{ product.name || product.sku }}</span>
                      <span v-if="productOptionsSummary(product) || product.why" class="commerce-chat__card-why">
                        {{ productOptionsSummary(product) || product.why }}
                      </span>
                      <span v-if="productColors(product).length" class="commerce-chat__card-swatches" aria-label="Colores">
                        <span
                          v-for="color in productColors(product)"
                          :key="`c-${color}`"
                          class="commerce-chat__card-swatch"
                          :title="color"
                        >{{ color }}</span>
                      </span>
                      <span v-if="productSizes(product).length" class="commerce-chat__card-sizes" aria-label="Tallas">
                        <span
                          v-for="size in productSizes(product)"
                          :key="`s-${size}`"
                          class="commerce-chat__card-size"
                          :class="{ 'is-selected': product.size && String(product.size) === String(size) }"
                        >{{ size }}</span>
                      </span>
                      <span class="commerce-chat__card-tags">
                        <span v-if="Number(product.variant_count) > 1" class="commerce-chat__card-tag">
                          {{ product.variant_count }} variantes
                        </span>
                        <span v-if="stockLabel(product)" class="commerce-chat__card-tag" :data-empty="Number(product.stock) <= 0 ? 'true' : 'false'">{{ stockLabel(product) }}</span>
                        <span v-if="product.delivery" class="commerce-chat__card-tag">{{ product.delivery }}</span>
                      </span>
                    </span>
                    <span v-if="formatPrice(product)" class="commerce-chat__card-price">{{ formatPrice(product) }}</span>
                  </button>
                  <div class="commerce-chat__card-actions">
                    <button type="button" class="commerce-chat__card-action" :disabled="!canPurchase(product)" @click="askAddToCart(product)">
                      Agregar al carrito
                    </button>
                    <button type="button" class="commerce-chat__card-action commerce-chat__card-action--buy" :disabled="!canPurchase(product)" @click="askBuy(product)">
                      Comprar
                    </button>
                  </div>
                </article>
              </div>
              <button
                v-if="isFullLayout && item.products.length > 1"
                type="button"
                class="commerce-chat__carousel-nav commerce-chat__carousel-nav--next"
                aria-label="Productos siguientes"
                @click="scrollCarousel(index, 1)"
              >
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M9 6l6 6-6 6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
                </svg>
              </button>
              <button
                v-if="!isFullLayout && hiddenProductCount(item, index) > 0"
                type="button"
                class="commerce-chat__show-more"
                @click="expandProducts(index)"
              >
                Ver más ({{ hiddenProductCount(item, index) }})
              </button>
            </div>
            <aside
              v-if="showCartPanel(item)"
              class="commerce-chat__cart-panel"
              aria-label="Carrito"
            >
              <header class="commerce-chat__cart-panel-head">
                <span class="commerce-chat__cart-panel-title">Tu carrito</span>
                <span class="commerce-chat__cart-panel-count">
                  {{ item.cart.item_count ?? item.cart.items?.length ?? 0 }}
                  {{ (item.cart.item_count ?? item.cart.items?.length ?? 0) === 1 ? 'ítem' : 'ítems' }}
                </span>
              </header>
              <ul v-if="item.cart.items?.length" class="commerce-chat__cart-lines">
                <li v-for="(line, li) in item.cart.items" :key="line.item_id || line.sku_id || li" class="commerce-chat__cart-line">
                  <img
                    v-if="line.image_url"
                    class="commerce-chat__cart-line-thumb"
                    :src="line.image_url"
                    alt=""
                    loading="lazy"
                  />
                  <span class="commerce-chat__cart-line-main">
                    <span class="commerce-chat__cart-line-name">{{ line.name || line.sku_id || 'Producto' }}</span>
                    <span class="commerce-chat__cart-line-qty">× {{ line.quantity ?? 1 }}</span>
                    <span v-if="line.seller_name" class="commerce-chat__cart-line-seller">{{ line.seller_name }}</span>
                  </span>
                  <span v-if="formatCartMoney(line.total, item.cart.currency || item.cart.currency_symbol)" class="commerce-chat__cart-line-total">
                    {{ formatCartMoney(line.total, item.cart.currency || item.cart.currency_symbol) }}
                  </span>
                </li>
              </ul>
              <div v-if="cartPaymentMethods(item.cart).length" class="commerce-chat__cart-section">
                <p class="commerce-chat__cart-section-title">Métodos de pago</p>
                <ul class="commerce-chat__cart-meta-list">
                  <li v-for="(pm, pi) in cartPaymentMethods(item.cart)" :key="pm.payment_method_id || pm.code || pi">
                    {{ pm.name || pm.description || pm.code || 'Pago' }}
                    <span v-if="pm.group_description" class="commerce-chat__cart-meta-hint">{{ pm.group_description }}</span>
                  </li>
                </ul>
              </div>
              <div v-if="cartShippingRows(item.cart).length" class="commerce-chat__cart-section">
                <p class="commerce-chat__cart-section-title">Envío</p>
                <ul class="commerce-chat__cart-meta-list">
                  <li v-for="(ship, si) in cartShippingRows(item.cart)" :key="ship.seller_id || si">
                    {{ ship.seller_name || ship.seller_id || 'Vendedor' }}
                    <span class="commerce-chat__cart-meta-hint">
                      {{ shippingHint(ship) }}
                    </span>
                  </li>
                </ul>
              </div>
              <footer class="commerce-chat__cart-panel-foot">
                <span v-if="item.cart.coupon_code" class="commerce-chat__cart-coupon">Cupón {{ item.cart.coupon_code }}</span>
                <span v-if="formatCartMoney(item.cart.total, item.cart.currency || item.cart.currency_symbol)" class="commerce-chat__cart-total">
                  Total {{ formatCartMoney(item.cart.total, item.cart.currency || item.cart.currency_symbol) }}
                </span>
              </footer>
            </aside>
            </div>
          </div>
          <div v-for="tool in item.tools || []" :key="`${index}-${tool}`" class="commerce-chat__tool">tool · {{ tool }}</div>
        </template>
        <p v-if="error" class="commerce-chat__error">{{ error }}</p>
      </div>
      <div v-if="recording && !awaitingRole" class="commerce-chat__recording">
        <span class="commerce-chat__recording-dot" aria-hidden="true"></span>
        <p>{{ audioMode === 'dictation' ? 'Dictando' : 'Nota de voz' }} {{ formatRecordingTime(recordingMs) }}</p>
        <button type="button" @click="cancelRecording">Cancelar</button>
        <button type="button" class="commerce-chat__recording-stop" @click="stopAndSendRecording">
          {{ audioMode === 'dictation' ? 'Listo' : 'Enviar' }}
        </button>
      </div>
      <p v-if="transcribing && !awaitingRole" class="commerce-chat__transcribing">Transcribiendo audio…</p>
      <div v-if="!awaitingRole" class="commerce-chat__composer">
        <div v-if="imagePreviewUrl" class="commerce-chat__attach-preview">
          <img :src="imagePreviewUrl" alt="" />
          <button type="button" class="commerce-chat__attach-remove" aria-label="Quitar imagen" :disabled="busy" @click="clearImage">×</button>
        </div>
        <form class="commerce-chat__form" @submit.prevent="send()">
          <input
            ref="fileInput"
            type="file"
            accept="image/*"
            class="commerce-chat__file"
            @change="onImagePick"
          />
          <button
            type="button"
            class="commerce-chat__attach"
            :disabled="busy || recording || transcribing"
            aria-label="Adjuntar imagen"
            @click="openImagePicker"
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <path d="M4 7.5A2.5 2.5 0 016.5 5h11A2.5 2.5 0 0120 7.5v9a2.5 2.5 0 01-2.5 2.5h-11A2.5 2.5 0 014 16.5v-9z" stroke="currentColor" stroke-width="1.7" />
              <path d="M8.5 13.5l2.2-2.2a1 1 0 011.4 0L15 14.2l1.3-1.3a1 1 0 011.4 0l2.3 2.3" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" />
              <circle cx="9" cy="9" r="1.2" fill="currentColor" />
            </svg>
          </button>
          <textarea
            ref="inputEl"
            v-model="draft"
            class="commerce-chat__input"
            rows="1"
            :placeholder="recording ? (audioMode === 'dictation' ? 'Dictando…' : 'Grabando nota…') : (resolvedAudience === 'manager' ? 'Mensaje o foto para crear / gestionar…' : 'Message or attach a photo…')"
            :disabled="busy || recording || transcribing"
            @keydown.enter.exact.prevent="send()"
          />
          <div v-if="micSupported" class="commerce-chat__mic">
            <button
              type="button"
              class="commerce-chat__mic-button"
              :class="{ 'is-recording': recording }"
              :disabled="busy || transcribing"
              :aria-expanded="showAudioMenu"
              :aria-pressed="recording"
              :aria-label="recording ? 'Detener audio' : 'Audio'"
              @click="toggleAudioMenu"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M12 14a3 3 0 003-3V7a3 3 0 10-6 0v4a3 3 0 003 3z" stroke="currentColor" stroke-width="1.7" />
                <path d="M19 11a7 7 0 01-14 0M12 18v3" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" />
              </svg>
            </button>
            <div v-if="showAudioMenu" class="commerce-chat__audio-menu" role="menu">
              <button type="button" role="menuitem" @click="startRecording('dictation')">Dictar</button>
              <button type="button" role="menuitem" @click="startRecording('voice')">Nota de voz</button>
            </div>
          </div>
          <button class="commerce-chat__send" type="submit" :disabled="busy || recording || transcribing || !canSend">Send</button>
        </form>
      </div>
      </div>
    </div>
    <button type="button" class="commerce-chat__toggle" aria-label="Open chat" @click="openChat">
      <img v-if="!open" src="/brand/kutria-isotipo.svg" alt="" width="28" height="28" />
      <span v-else>−</span>
    </button>
  </div>
</template>

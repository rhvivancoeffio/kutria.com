import { rememberedTenant, resolveTenantSlug } from '@/utils/tenant'

const STORAGE_PREFIX = 'commerce:product-create:generative:'

function storageKey() {
  const tenant = resolveTenantSlug() || rememberedTenant() || 'default'
  return `${STORAGE_PREFIX}${tenant}`
}

/**
 * Draft of the last generative run on product-create (survives reload).
 * @typedef {{
 *   v: 1,
 *   processId: string,
 *   kind: 'image' | 'content',
 *   phase: 'running' | 'success' | 'error' | 'applied',
 *   ficha?: object | null,
 *   error?: string,
 *   imageUrl?: string | null,
 *   productName?: string | null,
 *   savedAt: string
 * }} GenerativeDraft
 */

function tryGet(store, key) {
  try {
    return store?.getItem(key) ?? null
  } catch {
    return null
  }
}

function trySet(store, key, value) {
  try {
    store?.setItem(key, value)
    return true
  } catch {
    return false
  }
}

function tryRemove(store, key) {
  try {
    store?.removeItem(key)
  } catch {
    /* ignore */
  }
}

/** @returns {GenerativeDraft | null} */
export function loadGenerativeDraft() {
  try {
    const key = storageKey()
    const raw =
      tryGet(window.localStorage, key) ||
      tryGet(window.sessionStorage, key)
    if (!raw) return null
    const parsed = JSON.parse(raw)
    if (!parsed || parsed.v !== 1 || !parsed.processId) return null
    // Heal: keep both stores in sync so DevTools Local Storage always shows it.
    trySet(window.localStorage, key, raw)
    trySet(window.sessionStorage, key, raw)
    return parsed
  } catch {
    return null
  }
}

/** @param {Partial<GenerativeDraft> & { processId?: string, kind?: 'image' | 'content', phase?: string }} patch */
export function saveGenerativeDraft(patch) {
  try {
    const prev = loadGenerativeDraft() || {}
    const next = {
      v: 1,
      processId: String(patch.processId || prev.processId || '').trim(),
      kind: patch.kind || prev.kind || 'content',
      phase: patch.phase || prev.phase || 'running',
      ficha: patch.ficha !== undefined ? patch.ficha : prev.ficha ?? null,
      error: patch.error !== undefined ? patch.error : prev.error,
      imageUrl: patch.imageUrl !== undefined ? patch.imageUrl : prev.imageUrl ?? null,
      productName: patch.productName !== undefined ? patch.productName : prev.productName ?? null,
      savedAt: new Date().toISOString()
    }
    if (!next.processId) {
      console.warn('[product-create] skip generative draft save: missing processId', patch)
      return false
    }
    const key = storageKey()
    const raw = JSON.stringify(next)
    const localOk = trySet(window.localStorage, key, raw)
    const sessionOk = trySet(window.sessionStorage, key, raw)
    if (!localOk && !sessionOk) {
      console.warn('[product-create] generative draft save failed (quota/private mode)', key)
      return false
    }
    // Mirror into IndexedDB (async) so reload still works if web storage is flaky.
    void import('./productCreateImageDraft.js')
      .then(({ saveProductCreateImageDraft }) =>
        saveProductCreateImageDraft({
          imageUrl: next.imageUrl || undefined,
          generative: next
        })
      )
      .catch(() => {})
    return true
  } catch (err) {
    console.warn('[product-create] generative draft save error', err)
    return false
  }
}

export function clearGenerativeDraft() {
  const key = storageKey()
  tryRemove(window.localStorage, key)
  tryRemove(window.sessionStorage, key)
  void import('./productCreateImageDraft.js')
    .then(({ saveProductCreateImageDraft }) => saveProductCreateImageDraft({ generative: null }))
    .catch(() => {})
}

/** Last completed (or ficha-ready) image generation available to reopen without re-running. */
export function hasSuccessfulImageGenerationDraft() {
  const draft = loadGenerativeDraft()
  if (!draft || draft.kind !== 'image' || !draft.ficha) return false
  return draft.phase === 'success' || draft.phase === 'running' || draft.phase === 'applied'
}

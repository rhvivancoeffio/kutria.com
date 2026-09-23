import { rememberedTenant, resolveTenantSlug } from '@/utils/tenant'

const DB_NAME = 'commerce-product-create'
const STORE = 'image'
const DB_VERSION = 1

function recordKey() {
  const tenant = resolveTenantSlug() || rememberedTenant() || 'default'
  return `image:${tenant}`
}

/**
 * @returns {Promise<IDBDatabase>}
 */
function openDb() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION)
    req.onerror = () => reject(req.error || new Error('indexedDB open failed'))
    req.onupgradeneeded = () => {
      const db = req.result
      if (!db.objectStoreNames.contains(STORE)) {
        db.createObjectStore(STORE, { keyPath: 'id' })
      }
    }
    req.onsuccess = () => resolve(req.result)
  })
}

/**
 * Persist photo + optional generative snapshot so reload keeps preview and last ficha.
 * @param {{
 *   blob?: Blob | File | null,
 *   fileName?: string | null,
 *   contentType?: string | null,
 *   imageUrl?: string | null,
 *   imageAttachmentId?: string | null,
 *   generative?: object | null
 * }} payload
 */
export async function saveProductCreateImageDraft(payload = {}) {
  try {
    const db = await openDb()
    const prev = await loadProductCreateImageDraft()
    const blob = payload.blob !== undefined ? payload.blob : prev?.blob ?? null
    const next = {
      id: recordKey(),
      blob: blob || null,
      fileName: payload.fileName !== undefined ? payload.fileName : prev?.fileName ?? null,
      contentType:
        payload.contentType !== undefined
          ? payload.contentType
          : prev?.contentType ?? (blob && 'type' in blob ? blob.type : null) ?? null,
      imageUrl: payload.imageUrl !== undefined ? payload.imageUrl : prev?.imageUrl ?? null,
      imageAttachmentId:
        payload.imageAttachmentId !== undefined
          ? payload.imageAttachmentId
          : prev?.imageAttachmentId ?? null,
      generative:
        payload.generative !== undefined ? payload.generative : prev?.generative ?? null,
      savedAt: new Date().toISOString()
    }
    if (!next.blob && !next.imageUrl && !next.imageAttachmentId && !next.generative) {
      await clearProductCreateImageDraft()
      db.close()
      return
    }
    await new Promise((resolve, reject) => {
      const tx = db.transaction(STORE, 'readwrite')
      tx.oncomplete = () => resolve()
      tx.onerror = () => reject(tx.error)
      tx.objectStore(STORE).put(next)
    })
    db.close()
  } catch (err) {
    console.warn('[product-create] image draft save failed', err)
  }
}

/**
 * @returns {Promise<{
 *   blob: Blob | null,
 *   fileName: string | null,
 *   contentType: string | null,
 *   imageUrl: string | null,
 *   imageAttachmentId: string | null,
 *   generative: object | null
 * } | null>}
 */
export async function loadProductCreateImageDraft() {
  try {
    const db = await openDb()
    const row = await new Promise((resolve, reject) => {
      const tx = db.transaction(STORE, 'readonly')
      tx.onerror = () => reject(tx.error)
      const req = tx.objectStore(STORE).get(recordKey())
      req.onsuccess = () => resolve(req.result || null)
      req.onerror = () => reject(req.error)
    })
    db.close()
    if (!row) return null
    return {
      blob: row.blob || null,
      fileName: row.fileName || null,
      contentType: row.contentType || null,
      imageUrl: row.imageUrl || null,
      imageAttachmentId: row.imageAttachmentId || null,
      generative: row.generative || null
    }
  } catch {
    return null
  }
}

export async function clearProductCreateImageDraft() {
  try {
    const db = await openDb()
    await new Promise((resolve, reject) => {
      const tx = db.transaction(STORE, 'readwrite')
      tx.oncomplete = () => resolve()
      tx.onerror = () => reject(tx.error)
      tx.objectStore(STORE).delete(recordKey())
    })
    db.close()
  } catch {
    /* ignore */
  }
}

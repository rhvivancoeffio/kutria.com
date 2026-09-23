import Dexie from 'dexie'
import { computeCollectionFingerprint } from '@/utils/collectionFingerprint'

class OrderOriginWizardDexie extends Dexie {
  constructor() {
    super('gravity_channel_order_origin_wizard_v1')
    this.version(1).stores({
      drafts: 'draftKey, fingerprint',
      sessions: 'sessionKey'
    })
  }
}

const db = new OrderOriginWizardDexie()

/** @returns {unknown} */
function storableDeep(value) {
  if (value == null) return value
  if (typeof value !== 'object') return value
  try {
    return JSON.parse(JSON.stringify(value))
  } catch {
    return Array.isArray(value) ? [] : {}
  }
}

export function origenIntegrationSegment(integrationId) {
  if (integrationId == null || integrationId === '') return 'no-int'
  const s = String(integrationId).trim()
  return s || 'no-int'
}

export function buildOriginDraftKey(workspaceId, integrationId, fingerprint) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  const i = origenIntegrationSegment(integrationId)
  const f = String(fingerprint || '').trim() || 'no-fp'
  return `${w}::origins::${i}::${f}`
}

export function buildOriginSessionKey(workspaceId) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  return `${w}::origins::__session__`
}

/**
 * @param {{ workspaceId: string, integrationId: string, filters: Record<string, unknown> }} params
 */
export function computeOrderOriginFilterFingerprint(params) {
  return computeCollectionFingerprint({
    pipelineId: null,
    filters: {
      workspaceId: params.workspaceId,
      integrationId: params.integrationId,
      ...params.filters
    }
  })
}

/**
 * @param {{ draftKey: string, fingerprint: string, filters: object, targetPathsByStagingKey: object, updatedAt?: number }} record
 */
export async function saveOrderOriginDraft(record) {
  const { draftKey, fingerprint, filters, targetPathsByStagingKey } = record
  const updatedAt = Date.now()
  await db.drafts.put({
    draftKey,
    fingerprint,
    filters: storableDeep(filters),
    targetPathsByStagingKey: storableDeep(targetPathsByStagingKey || {}),
    updatedAt
  })
}

/** @param {string} draftKey */
export async function loadOrderOriginDraft(draftKey) {
  return db.drafts.get(draftKey)
}

/** @param {string} draftKey */
export async function deleteOrderOriginDraft(draftKey) {
  await db.drafts.delete(draftKey)
}

/**
 * Borradores del asistente de orígenes para un workspace (clave `draftKey` empieza por `{ws}::origins::`).
 * @param {string} workspaceId
 */
export async function listOrderOriginDraftsForWorkspace(workspaceId) {
  const w = String(workspaceId || '').trim() || 'no-ws'
  const prefix = `${w}::origins::`
  const all = await db.drafts.toArray()
  return all
    .filter((d) => typeof d.draftKey === 'string' && d.draftKey.startsWith(prefix))
    .sort((a, b) => (Number(b.updatedAt) || 0) - (Number(a.updatedAt) || 0))
}

/**
 * @param {{ sessionKey: string, integrationId?: string, channelIntegrationId?: string, lastFilterFingerprint?: string, stepIndex?: number, updatedAt?: number }} record
 */
export async function saveOrderOriginSession(record) {
  const { sessionKey, integrationId, channelIntegrationId, lastFilterFingerprint, stepIndex } = record
  await db.sessions.put({
    sessionKey,
    integrationId: integrationId != null ? String(integrationId) : '',
    channelIntegrationId: channelIntegrationId != null ? String(channelIntegrationId) : '',
    lastFilterFingerprint: lastFilterFingerprint != null ? String(lastFilterFingerprint) : '',
    stepIndex: typeof stepIndex === 'number' ? stepIndex : 0,
    updatedAt: Date.now()
  })
}

/** @param {string} sessionKey */
export async function loadOrderOriginSession(sessionKey) {
  return db.sessions.get(sessionKey)
}

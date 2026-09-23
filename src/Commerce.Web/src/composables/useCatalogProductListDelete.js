import { ref, computed } from 'vue'
import { useToast } from 'vue-toastification'
import apiService, { getApiErrorMessage } from '@/services/api'

/** @param {Record<string, unknown> | null | undefined} row */
export function catalogProductDeleteConfirmMessage(row) {
  if (!row) {
    return '¿Eliminar este producto del catálogo? Esta acción no se puede deshacer.'
  }
  const candidates = [row.title, row.sku, row.entityId]
  const labelRaw = candidates.find((x) => x != null && String(x).trim())
  const label = labelRaw != null ? String(labelRaw).trim() : ''
  if (!label) {
    return '¿Eliminar este producto del catálogo? Esta acción no se puede deshacer.'
  }
  const short = label.length > 90 ? `${label.slice(0, 90)}…` : label
  return `¿Eliminar «${short}» del catálogo? Esta acción no se puede deshacer.`
}

/**
 * Borrado desde listados de catálogo (modal + API + toast).
 * Mientras `deletingId` está definido, el modal debe usar `:busy="!!deletingId"` para deshabilitar acciones hasta que termine la petición.
 * @param {{ onAfterDelete?: (deletedId: string) => void | Promise<void> }} options
 */
export function useCatalogProductListDelete(options = {}) {
  const deletingId = ref(null)
  const pendingRow = ref(null)
  const toast = useToast()

  const deleteConfirmMessage = computed(() => catalogProductDeleteConfirmMessage(pendingRow.value))

  /**
   * @param {{ id?: string }} row
   * @param {Event} [event]
   */
  function openDeleteConfirm(row, event) {
    event?.stopPropagation?.()
    event?.preventDefault?.()
    const id = row?.id != null ? String(row.id).trim() : ''
    if (!id || deletingId.value) return
    pendingRow.value = row
  }

  function cancelDeleteConfirm() {
    if (deletingId.value) return
    pendingRow.value = null
  }

  async function applyDeleteConfirm() {
    const row = pendingRow.value
    if (!row || deletingId.value) return
    const id = row.id != null ? String(row.id).trim() : ''
    if (!id) return

    deletingId.value = id
    try {
      await apiService.deleteChannelCatalogProduct(id)
      toast.success('Producto eliminado')
      pendingRow.value = null
      await options.onAfterDelete?.(id)
    } catch (e) {
      toast.error(getApiErrorMessage(e) || 'No se pudo eliminar el producto')
    } finally {
      deletingId.value = null
    }
  }

  function isDeletingRow(row) {
    const rid = row?.id != null ? String(row.id).trim() : ''
    return Boolean(rid && deletingId.value === rid)
  }

  return {
    deletingId,
    pendingRow,
    deleteConfirmMessage,
    openDeleteConfirm,
    cancelDeleteConfirm,
    applyDeleteConfirm,
    isDeletingRow
  }
}

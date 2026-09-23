import { ref, onMounted, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import apiService from '@/services/api'

export const ADMIN_CATALOG_PIPELINE_STORAGE_KEY = 'admin_catalog_pipeline_id'

export function persistAdminCatalogPipelineId(id) {
  const v = String(id || '').trim()
  try {
    if (v) sessionStorage.setItem(ADMIN_CATALOG_PIPELINE_STORAGE_KEY, v)
    else sessionStorage.removeItem(ADMIN_CATALOG_PIPELINE_STORAGE_KEY)
  } catch (_) {}
}

export function readAdminCatalogPipelineIdFromStorage() {
  try {
    return sessionStorage.getItem(ADMIN_CATALOG_PIPELINE_STORAGE_KEY) || ''
  } catch {
    return ''
  }
}

/** Conjunto de ids permitidos según el listado GET /channel-pipelines/lookups (no inventar opciones desde filas de catálogo). */
function allowedPipelineIdSet(pipelines) {
  const list = Array.isArray(pipelines) ? pipelines : []
  return new Set(
    list.map((p) => String(p?.id ?? '').trim().toLowerCase()).filter(Boolean)
  )
}

/** Si el id actual no está en la lista del API, limpia ref y storage (evita UUID viejos en sessionStorage). */
export function reconcileCatalogPipelineSelection(catalogPipelineIdRef, pipelines) {
  const allowed = allowedPipelineIdSet(pipelines)
  const cur = String(catalogPipelineIdRef.value || '').trim().toLowerCase()
  if (cur && !allowed.has(cur)) {
    catalogPipelineIdRef.value = ''
    persistAdminCatalogPipelineId('')
  }
}

/** Claves de query compartidas con `/admin/data/products` (categoría, marca, nombre). */
export const CATALOG_LIST_FILTER_QUERY = {
  category: 'catalogCategory',
  brand: 'catalogBrand',
  name: 'catalogName'
}

function stringQueryParam(query, name) {
  const v = query?.[name]
  return typeof v === 'string' ? v : ''
}

/** Lee categoría/marca/nombre desde la query de ruta hacia refs de la vista. */
export function readCatalogListFiltersFromQuery(query, catalogListFilters) {
  if (!catalogListFilters) return
  catalogListFilters.category.value = stringQueryParam(query, CATALOG_LIST_FILTER_QUERY.category)
  catalogListFilters.brand.value = stringQueryParam(query, CATALOG_LIST_FILTER_QUERY.brand)
  catalogListFilters.name.value = stringQueryParam(query, CATALOG_LIST_FILTER_QUERY.name)
}

export function facetFilterValue(id, name) {
  const i = String(id ?? '').trim()
  if (i) return i
  return String(name ?? '').trim()
}

export function facetRowKey(prefix, id, name) {
  return `${prefix}:${String(id ?? '')}:${String(name ?? '')}`
}

export function facetOptionLabel(primary, secondary, count) {
  const p = String(primary ?? '').trim()
  const s = String(secondary ?? '').trim()
  const label = p || s || '—'
  const suffix = typeof count === 'number' && !Number.isNaN(count) ? ` (${count})` : ''
  return `${label}${suffix}`
}

/** Solo aplica storage si ese id existe en `pipelines`; si no, borra storage obsoleto. */
export function restoreCatalogPipelineFromStorageIfAllowed(catalogPipelineIdRef, pipelines) {
  if (String(catalogPipelineIdRef.value || '').trim()) return
  const stored = readAdminCatalogPipelineIdFromStorage().trim()
  if (!stored) return
  const allowed = allowedPipelineIdSet(pipelines)
  if (!allowed.has(stored.toLowerCase())) {
    persistAdminCatalogPipelineId('')
    return
  }
  const hit = pipelines.find((p) => String(p.id).trim().toLowerCase() === stored.toLowerCase())
  if (hit?.id != null) catalogPipelineIdRef.value = String(hit.id)
}

/**
 * Selector de pipeline de catálogo: el id vive en sessionStorage (`admin_catalog_pipeline_id`).
 * La query de la URL solo lleva paginación (`pageQueryKey`) y extras (`extraQuery`); si llega `?catalogPipeline=` (legacy) se migra a storage y se limpia la URL.
 * Con `catalogListFilters`, también sincroniza `catalogCategory` / `catalogBrand` / `catalogName` (misma convención que Productos).
 * @param {{
 *   pageQueryKey?: string,
 *   extraQuery?: () => Record<string, string>,
 *   catalogListFilters?: { category: import('vue').Ref<string>, brand: import('vue').Ref<string>, name: import('vue').Ref<string> }
 * }} [options]
 */
export function useCatalogPipelinePageContext(options = {}) {
  const { pageQueryKey = null, extraQuery, catalogListFilters } = options
  const route = useRoute()
  const router = useRouter()
  const catalogPipelineId = ref('')
  const catalogPipelines = ref([])
  const catalogPage = ref(1)
  const suppressRouteWatch = ref(true)
  /** True después del `replace` inicial (storage / un solo pipeline) para que la vista cargue datos una sola vez coherente. */
  const catalogPipelineReady = ref(false)

  function readCatalogPipelineIdFromUrlQuery() {
    const q = route.query
    return typeof q.catalogPipeline === 'string' && q.catalogPipeline.trim() ? q.catalogPipeline.trim() : ''
  }

  function readFromRoute() {
    const fromUrl = readCatalogPipelineIdFromUrlQuery()
    if (fromUrl) {
      catalogPipelineId.value = fromUrl
      persistAdminCatalogPipelineId(fromUrl)
    }
    if (pageQueryKey) {
      const raw = route.query[pageQueryKey]
      catalogPage.value = Math.max(1, parseInt(typeof raw === 'string' ? raw : '', 10) || 1)
    }
    readCatalogListFiltersFromQuery(route.query, catalogListFilters)
  }

  function buildQuery(extra = {}) {
    const merged = typeof extraQuery === 'function' ? { ...extraQuery(), ...extra } : { ...extra }
    const out = { ...merged }
    if (pageQueryKey && catalogPage.value > 1) out[pageQueryKey] = String(catalogPage.value)
    if (catalogListFilters) {
      const c = catalogListFilters.category.value?.trim()
      const b = catalogListFilters.brand.value?.trim()
      const n = catalogListFilters.name.value?.trim()
      if (c) out[CATALOG_LIST_FILTER_QUERY.category] = c
      if (b) out[CATALOG_LIST_FILTER_QUERY.brand] = b
      if (n) out[CATALOG_LIST_FILTER_QUERY.name] = n
    }
    return out
  }

  /** True si hay que hacer replace: quitar `catalogPipeline` legacy o alinear solo la query de paginación/extra. */
  function urlNeedsNormalizing() {
    if (readCatalogPipelineIdFromUrlQuery()) return true
    if (pageQueryKey) {
      const wantPage = catalogPage.value > 1 ? String(catalogPage.value) : ''
      const curPage = route.query[pageQueryKey] != null ? String(route.query[pageQueryKey]) : ''
      if (wantPage !== curPage) return true
    }
    if (catalogListFilters) {
      const sq = (k) => stringQueryParam(route.query, k)
      const same = (refVal, key) => (String(refVal || '').trim() === String(sq(key) || '').trim())
      if (!same(catalogListFilters.category.value, CATALOG_LIST_FILTER_QUERY.category)) return true
      if (!same(catalogListFilters.brand.value, CATALOG_LIST_FILTER_QUERY.brand)) return true
      if (!same(catalogListFilters.name.value, CATALOG_LIST_FILTER_QUERY.name)) return true
    }
    return false
  }

  function pushUrl(extra = {}) {
    router.push({ path: route.path, query: buildQuery(extra) })
  }

  function onCatalogPipelineChange() {
    catalogPage.value = 1
    if (catalogListFilters) {
      catalogListFilters.category.value = ''
      catalogListFilters.brand.value = ''
      catalogListFilters.name.value = ''
    }
    persistAdminCatalogPipelineId(catalogPipelineId.value)
    pushUrl()
  }

  async function loadCatalogPipelines() {
    try {
      catalogPipelines.value = await apiService.getDataPipelineLookups()
    } catch {
      catalogPipelines.value = []
    }
  }

  function tryAutoSelectSinglePipeline() {
    if (catalogPipelineId.value?.trim() || catalogPipelines.value.length !== 1) return false
    catalogPipelineId.value = String(catalogPipelines.value[0].id)
    persistAdminCatalogPipelineId(catalogPipelineId.value)
    return true
  }

  /** Base Manual pipeline id equals account id (see ChannelDataPipelineWellKnown). */
  function tryAutoSelectBaseManualPipeline(accountId) {
    const acc = String(accountId || '').trim()
    if (!acc) return false
    const want = acc.toLowerCase()
    const pl = catalogPipelines.value.find((p) => String(p.id).toLowerCase() === want)
    if (!pl?.id) return false
    catalogPipelineId.value = String(pl.id)
    persistAdminCatalogPipelineId(catalogPipelineId.value)
    return true
  }

  const catalogTotalPages = (totalCount, pageSize) =>
    Math.max(1, Math.ceil(totalCount / pageSize))

  onMounted(async () => {
    readFromRoute()
    await loadCatalogPipelines()
    reconcileCatalogPipelineSelection(catalogPipelineId, catalogPipelines.value)
    if (!catalogPipelineId.value.trim()) {
      restoreCatalogPipelineFromStorageIfAllowed(catalogPipelineId, catalogPipelines.value)
    }
    if (!catalogPipelineId.value.trim()) {
      try {
        const me = await apiService.getProfile()
        const aid = me?.accountId ?? me?.AccountId
        tryAutoSelectBaseManualPipeline(aid)
      } catch (_) {}
    }
    tryAutoSelectSinglePipeline()
    if (catalogPipelineId.value.trim()) {
      persistAdminCatalogPipelineId(catalogPipelineId.value)
      if (urlNeedsNormalizing()) {
        await router.replace({ path: route.path, query: buildQuery() })
      }
    }
    await nextTick()
    suppressRouteWatch.value = false
    catalogPipelineReady.value = true
  })

  watch(
    () => route.query,
    async () => {
      if (suppressRouteWatch.value) return
      readFromRoute()
      reconcileCatalogPipelineSelection(catalogPipelineId, catalogPipelines.value)
      if (!catalogPipelineId.value.trim()) {
        restoreCatalogPipelineFromStorageIfAllowed(catalogPipelineId, catalogPipelines.value)
      }
      if (!catalogPipelineId.value.trim() && catalogPipelines.value.length === 1) {
        tryAutoSelectSinglePipeline()
        if (urlNeedsNormalizing()) pushUrl()
        return
      }
      if (readCatalogPipelineIdFromUrlQuery() || urlNeedsNormalizing()) {
        await router.replace({ path: route.path, query: buildQuery() })
      }
    },
    { deep: true }
  )

  return {
    catalogPipelineId,
    catalogPipelines,
    catalogPage,
    catalogPipelineReady,
    suppressRouteWatch,
    readFromRoute,
    pushUrl,
    onCatalogPipelineChange,
    loadCatalogPipelines,
    buildQuery,
    persistAdminCatalogPipelineId,
    catalogTotalPages
  }
}

export function normalizeCatalogListRow(r) {
  const skuIds = r.skuIds ?? r.SkuIds ?? []
  return {
    id: r.id ?? r.Id,
    channelDataPipelineId: r.channelDataPipelineId ?? r.ChannelDataPipelineId,
    pipelineName: r.pipelineName ?? r.PipelineName ?? '',
    entityId: r.entityId ?? r.EntityId ?? '',
    naturalKey: r.naturalKey ?? r.NaturalKey ?? '',
    title: r.title ?? r.Title ?? null,
    sku: r.sku ?? r.Sku ?? null,
    skuIds: Array.isArray(skuIds) ? skuIds : [],
    price: r.price ?? r.Price,
    specialPrice: r.specialPrice ?? r.SpecialPrice ?? null,
    availableQuantity: r.availableQuantity ?? r.AvailableQuantity,
    loadStatus: r.loadStatus ?? r.LoadStatus ?? '',
    sourceUpdatedAt: r.sourceUpdatedAt ?? r.SourceUpdatedAt
  }
}

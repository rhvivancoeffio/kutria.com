import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiService from '../services/api'

const CACHE_TTL_MS = 60_000 // 60 seconds

export const useAppStore = defineStore('app', () => {
  // --- State ---
  const usage = ref(null)
  const usageFetchedAt = ref(null)
  /** null = cuenta del JWT; Guid string = partner viendo cuenta del programa (misma clave que GET /accounts/usage). */
  const lastUsageViewAccountId = ref(null)
const availableIntegrations = ref([])
  const availableIntegrationsFetchedAt = ref(null)
  const billing = ref(null)
  const billingFetchedAt = ref(null)
  const profile = ref(null)
  const profileFetchedAt = ref(null)
  const tour = ref(null)
  const tourFetchedAt = ref(null)

  // --- Computed ---
  const isUsageStale = computed(() => {
    if (!usageFetchedAt.value) return true
    return Date.now() - usageFetchedAt.value > CACHE_TTL_MS
  })
const isAvailableIntegrationsStale = computed(() => {
    if (!availableIntegrationsFetchedAt.value) return true
    return Date.now() - availableIntegrationsFetchedAt.value > CACHE_TTL_MS
  })
  const isBillingStale = computed(() => {
    if (!billingFetchedAt.value) return true
    return Date.now() - billingFetchedAt.value > CACHE_TTL_MS
  })
  const isProfileStale = computed(() => {
    if (!profileFetchedAt.value) return true
    return Date.now() - profileFetchedAt.value > CACHE_TTL_MS
  })
  const isTourStale = computed(() => {
    if (!tourFetchedAt.value) return true
    return Date.now() - tourFetchedAt.value > CACHE_TTL_MS
  })

  // --- In-flight guards (evitar fetches duplicados concurrentes) ---
  let usagePromise = null

  // --- Actions ---
  async function fetchUsage(force = false, viewAccountId = null) {
    const viewAs = viewAccountId && String(viewAccountId).trim() ? String(viewAccountId).trim() : null
    if (!force && usage.value && !isUsageStale.value && lastUsageViewAccountId.value === viewAs) {
      return usage.value
    }
    if (usagePromise && !force) return usagePromise
    usagePromise = (async () => {
      try {
        usage.value = await apiService.getAccountUsage(viewAs)
        usageFetchedAt.value = Date.now()
        lastUsageViewAccountId.value = viewAs
        return usage.value
      } catch (err) {
        console.error('Error fetching account usage:', err)
        return null
      } finally {
        usagePromise = null
      }
    })()
    return usagePromise
  }

  async function fetchAvailableIntegrations(force = false) {
    if (!force && availableIntegrations.value.length > 0 && !isAvailableIntegrationsStale.value) return availableIntegrations.value
    try {
      const data = await apiService.getAvailableIntegrations()
      availableIntegrations.value = Array.isArray(data) ? data : []
      availableIntegrationsFetchedAt.value = Date.now()
      return availableIntegrations.value
    } catch (err) {
      console.error('Error fetching available integrations:', err)
      availableIntegrations.value = []
      return []
    }
  }

  async function fetchBilling(force = false) {
    if (!force && billing.value && !isBillingStale.value) return billing.value
    try {
      billing.value = await apiService.getCurrentBilling()
      billingFetchedAt.value = Date.now()
      return billing.value
    } catch (err) {
      console.error('Error fetching billing:', err)
      billing.value = null
      return null
    }
  }

  async function fetchProfile(force = false) {
    if (!force && profile.value && !isProfileStale.value) return profile.value
    try {
      profile.value = await apiService.getProfile()
      profileFetchedAt.value = Date.now()
      return profile.value
    } catch (err) {
      console.error('Error fetching profile:', err)
      profile.value = null
      return null
    }
  }

  async function fetchTour(force = false) {
    if (!force && tour.value && !isTourStale.value) return tour.value
    try {
      tour.value = await apiService.getTour('onboarding')
      tourFetchedAt.value = Date.now()
      return tour.value
    } catch (err) {
      console.error('Error fetching tour:', err)
      tour.value = null
      return null
    }
  }

/** Load core admin data (usage, profile, billing). Called once from AdminLayout. */
  async function loadCoreData() {
    const [, , billingRes] = await Promise.all([
      fetchUsage(),
      fetchProfile(),
      fetchBilling()
    ])
    return billingRes
  }

  /** Drop in-memory caches so the next login does not reuse stale profile/mode. */
  function clearSessionData() {
    usage.value = null
    usageFetchedAt.value = null
    lastUsageViewAccountId.value = null
    availableIntegrations.value = []
    availableIntegrationsFetchedAt.value = null
    billing.value = null
    billingFetchedAt.value = null
    profile.value = null
    profileFetchedAt.value = null
    tour.value = null
    tourFetchedAt.value = null
  }

return {
    usage,
    availableIntegrations,
    billing,
    profile,
    tour,
    fetchUsage,
    fetchAvailableIntegrations,
    fetchBilling,
    fetchProfile,
    fetchTour,
    loadCoreData,
    clearSessionData,
  }
})

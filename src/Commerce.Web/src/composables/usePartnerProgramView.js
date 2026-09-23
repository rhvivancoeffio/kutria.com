import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAppStore } from '../stores/appStore'
import { isPartnerStaffFromJwt } from '../utils/jwtUtils'

/**
 * Partner staff: vista de cuentas del programa vía query `accountId` en panel de uso (y rutas partner).
 * La API valida acceso; los pipelines y usage usan el mismo filtro que GET /accounts/usage.
 */
export function usePartnerProgramView() {
  const route = useRoute()
  const store = useAppStore()

  const showPartnerPortal = computed(
    () => isPartnerStaffFromJwt() || store.profile?.isPartnerStaff === true || store.profile?.IsPartnerStaff === true
  )

  const partnerViewAccountId = computed(() => {
    if (!showPartnerPortal.value) return ''
    const q = route.query.accountId
    if (q == null || q === '') return ''
    const s = Array.isArray(q) ? q[0] : q
    return String(s || '').trim()
  })

  return {
    showPartnerPortal,
    partnerViewAccountId
  }
}

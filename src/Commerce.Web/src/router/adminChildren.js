import Forbidden from "../views/shared/Forbidden.vue"
import AdminNotFound from "../views/shared/AdminNotFound.vue"
import apiService from "../services/api"
import { isPartnerStaffFromJwt } from "../utils/jwtUtils"

/** Only partner staff (JWT claim or profile) can open partner portal routes. */
export async function partnerStaffBeforeEnter(to, from, next) {
  if (isPartnerStaffFromJwt()) {
    next()
    return
  }
  try {
    const p = await apiService.getProfile()
    if (p?.isPartnerStaff === true || p?.IsPartnerStaff === true) {
      next()
      return
    }
  } catch {
    /* fall through */
  }
  next({ path: "/admin/forbidden", replace: true })
}

/** Native Gravity store admin (Productos/Categories/Marcas) only when tenant mode is Native. */
export async function nativeStoreBeforeEnter(to, from, next) {
  try {
    const p = await apiService.getProfile()
    const mode = String(p?.commerceStoreMode ?? p?.CommerceStoreMode ?? "None")
    if (mode === "Native") {
      next()
      return
    }
  } catch {
    /* fall through */
  }
  next({ path: "/admin/integraciones", replace: true })
}

/** External ingest (Catalog/Ventas) is for connected stores only — not native Gravity tienda. */
export async function connectedStoreIngestBeforeEnter(to, from, next) {
  try {
    const p = await apiService.getProfile()
    const mode = String(p?.commerceStoreMode ?? p?.CommerceStoreMode ?? "None")
    if (mode === "Native") {
      next({ path: "/admin/tienda/productos", replace: true })
      return
    }
  } catch {
    /* fall through — allow if profile unavailable */
  }
  next()
}

import { withMemberPermissionGuards } from "./memberPermissionGuard.js"

/** Child routes for `AdminLayout` under `/admin/*`. */
const adminLayoutChildrenBase = [
  { path: "usage", name: "AdminUsage", component: () => import("../views/admin/AdminUsage.vue") },
  { path: "workspaces", name: "AdminWorkspaces", component: () => import("../views/admin/AdminWorkspaces.vue") },
  { path: "forbidden", name: "AdminForbidden", component: Forbidden },
  { path: "webhook-monitor", name: "AdminWebhookMonitor", component: () => import("../views/monitor/WebhookMonitor.vue") },
  {
    path: "observability/outbox",
    name: "AdminObservabilityOutbox",
    component: () => import("../views/admin/AdminObservabilityOutboxView.vue")
  },
  {
    path: "observability/webhooks-out",
    name: "AdminObservabilityWebhooksOut",
    component: () => import("../views/admin/AdminObservabilityWebhooksOutView.vue")
  },
  {
    path: "observability/chat-events",
    name: "AdminObservabilityChatEvents",
    component: () => import("../views/admin/AdminObservabilityChatEventsView.vue")
  },
  { path: "integraciones", name: "AdminIntegraciones", component: () => import("../views/integrations/Integrations.vue") },
  {
    path: "environment-variables",
    name: "AdminEnvironmentVariables",
    component: () => import("../views/admin/AdminEnvironmentVariables.vue")
  },
  { path: "profile", name: "AdminProfile", component: () => import("../views/admin/AdminProfile.vue") },
  { path: "change-password", name: "AdminChangePassword", component: () => import("../views/admin/AdminChangePassword.vue") },
  { path: "billing", name: "AdminBilling", component: () => import("../views/admin/AdminBilling.vue") },
  { path: "policies", name: "AdminPolicies", component: () => import("../views/admin/AdminPolicies.vue") },
  {
    path: "integraciones/tienda/catalog",
    name: "AdminStoreCatalog",
    beforeEnter: connectedStoreIngestBeforeEnter,
    component: () => import("../views/admin/AdminStoreCatalog.vue")
  },
  {
    path: "integraciones/tienda/ventas",
    name: "AdminStoreOrders",
    beforeEnter: connectedStoreIngestBeforeEnter,
    component: () => import("../views/admin/AdminStoreOrders.vue")
  },
  {
    path: "integraciones/tienda/catalog/integrations",
    name: "AdminStoreCatalogIngestions",
    beforeEnter: connectedStoreIngestBeforeEnter,
    component: () => import("../views/admin/AdminStoreCatalogIngestions.vue")
  },
  {
    path: "integraciones/tienda/ventas/integrations",
    name: "AdminStoreOrdersIngestions",
    beforeEnter: connectedStoreIngestBeforeEnter,
    component: () => import("../views/admin/AdminStoreOrdersIngestions.vue")
  },
  { path: "tienda/productos", name: "AdminStoreProducts", beforeEnter: nativeStoreBeforeEnter, component: () => import("../views/admin/AdminStoreProducts.vue") },
  { path: "tienda/productos/nuevo", name: "AdminStoreProductCreate", beforeEnter: nativeStoreBeforeEnter, component: () => import("../views/admin/AdminStoreProductCreate.vue") },
  { path: "tienda/productos/:productId", name: "AdminStoreProductEdit", beforeEnter: nativeStoreBeforeEnter, component: () => import("../views/admin/AdminStoreProductCreate.vue") },
  { path: "tienda/categories", name: "AdminStoreCategories", beforeEnter: nativeStoreBeforeEnter, component: () => import("../views/admin/AdminStoreCategories.vue") },
  { path: "tienda/marcas", name: "AdminStoreBrands", beforeEnter: nativeStoreBeforeEnter, component: () => import("../views/admin/AdminStoreBrands.vue") },
  // Hidden for now — Attributes UI (re-enable when ready):
  // { path: "tienda/atributos", name: "AdminStoreAttributes", beforeEnter: nativeStoreBeforeEnter, component: () => import("../views/admin/AdminStoreAttributes.vue") },
  { path: "tienda/catalog", redirect: { name: "AdminStoreCatalog" } },
  { path: "tienda/ventas", redirect: { name: "AdminStoreOrders" } },
  { path: "tienda/catalog/integrations", redirect: { name: "AdminStoreCatalogIngestions" } },
  { path: "tienda/ventas/integrations", redirect: { name: "AdminStoreOrdersIngestions" } },
  { path: "embed", name: "AdminEmbed", component: () => import("../views/admin/AdminEmbed.vue") },
  { path: "consumption-history", name: "AdminConsumptionHistory", component: () => import("../views/admin/ConsumptionHistory.vue") },
  { path: "members", name: "AdminMembers", component: () => import("../views/admin/AdminMembers.vue") },
  { path: "api-keys", name: "AdminApiKeys", component: () => import("../views/admin/AdminApiKeys.vue") },
  { path: "mcp", name: "AdminMcpConnect", component: () => import("../views/mcp/AdminMcpConnect.vue") },
  {
    path: "outbound-webhooks",
    name: "AdminOutboundWebhooks",
    component: () => import("../views/admin/AdminOutboundWebhooks.vue")
  },
  { path: "accounts", name: "AdminAccounts", component: () => import("../views/admin/AdminAccounts.vue") },
  { path: "partners", name: "AdminPartners", component: () => import("../views/admin/AdminPartners.vue") },
  {
    path: "partner/accounts",
    name: "PartnerProgramAccounts",
    beforeEnter: partnerStaffBeforeEnter,
    component: () => import("../views/partner/PartnerProgramAccounts.vue")
  },
  {
    path: "partner/accounts/:accountId/metrics",
    name: "PartnerAccountMetrics",
    beforeEnter: partnerStaffBeforeEnter,
    component: () => import("../views/partner/PartnerAccountMetrics.vue")
  },
  {
    path: "partner/accounts/:accountId/consumption",
    name: "PartnerAccountConsumption",
    beforeEnter: partnerStaffBeforeEnter,
    component: () => import("../views/admin/ConsumptionHistory.vue")
  },
  { path: ":pathMatch(.*)*", name: "AdminNotFound", component: AdminNotFound }
]

export const adminLayoutChildren = withMemberPermissionGuards(adminLayoutChildrenBase)

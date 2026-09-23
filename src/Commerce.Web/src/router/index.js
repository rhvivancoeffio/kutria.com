import { createRouter, createWebHistory } from "vue-router"
import { isSuperAdmin, isPartnerStaffFromJwt, canAccessObservabilityAdmin } from "../utils/jwtUtils"
import apiService from "../services/api"
import { useAppStore } from "../stores/appStore"
import Home from "../views/home/Home.vue"
import NotFound from "../views/shared/NotFound.vue"
import AdminLayout from "../layouts/AdminLayout.vue"
import { adminLayoutChildren } from "./adminChildren.js"
import { rememberTenant, rememberedTenant } from "../utils/tenant"

const TOOLS_RETURN_PATH_KEY = 'tools_return_path'

function redirectToCentralIdp(to, next, authPath) {
  const base = (import.meta.env.VITE_AUTH_BASE_URL || '').replace(/\/$/, '')
  if (!base) {
    next()
    return
  }
  const pathToSave = to.query.returnUrl && typeof to.query.returnUrl === 'string'
    ? decodeURIComponent(to.query.returnUrl)
    : '/admin'
  try {
    sessionStorage.setItem(TOOLS_RETURN_PATH_KEY, pathToSave)
  } catch (_) {}
  const callbackUrl = window.location.origin + '/auth/identity/callback'
  const returnUrlParam = encodeURIComponent(callbackUrl)
  let url = `${base}${authPath}?returnUrl=${returnUrlParam}`
  const clientId = import.meta.env.VITE_AUTH_CLIENT_ID
  if (clientId) {
    url += `&clientId=${encodeURIComponent(clientId)}`
  }
  const pr =
    typeof to.query.partnerReservationToken === "string" ? to.query.partnerReservationToken.trim() : ""
  if (pr) {
    url += `&partnerReservationToken=${encodeURIComponent(pr)}`
  }
  const ik =
    typeof to.query.inviteKind === "string" ? to.query.inviteKind.trim().toLowerCase() : ""
  if (ik === "operator" || ik === "program") {
    url += `&inviteKind=${encodeURIComponent(ik)}`
  }
  window.location.href = url
  next(false)
}

async function signInSignUpBeforeEnter(to, next, authPath) {
  try {
    const config = await apiService.getAuthConfig()
    if (config.useCentralIdp) {
      redirectToCentralIdp(to, next, authPath)
    } else {
      next()
    }
  } catch (_) {
    next()
  }
}

const routes = [
  {
    path: "/",
    name: "Home",
    component: Home
  },
  {
    path: "/integrations/shopify/callback",
    name: "ShopifyOAuthCallback",
    component: () => import("../views/integrations/IntegrationOAuthCallback.vue")
  },
  {
    path: "/integrations/meli/callback",
    name: "MercadoLibreOAuthCallback",
    component: () => import("../views/integrations/IntegrationOAuthCallback.vue")
  },
  {
    path: "/integrations/slack/callback",
    name: "SlackOAuthCallback",
    component: () => import("../views/integrations/IntegrationOAuthCallback.vue")
  },
  {
    path: "/integrations/notion/callback",
    name: "NotionOAuthCallback",
    component: () => import("../views/integrations/IntegrationOAuthCallback.vue")
  },
  {
    path: "/integrations/clickup/callback",
    name: "ClickUpOAuthCallback",
    component: () => import("../views/integrations/IntegrationOAuthCallback.vue")
  },
  {
    path: "/integrations/oauth/callback",
    name: "IntegrationOAuthCallback",
    component: () => import("../views/integrations/IntegrationOAuthCallback.vue")
  },
  {
    path: "/mcp/oauth/consent",
    name: "McpOAuthConsent",
    component: () => import("../views/mcp/McpOAuthConsent.vue")
  },
  {
    path: "/mcp/oauth/callback",
    name: "McpOAuthCallback",
    component: () => import("../views/integrations/IntegrationOAuthCallback.vue")
  },
  {
    path: "/precios",
    name: "PricingPlans",
    component: () => import("../views/billing/PricingPlans.vue")
  },
  {
    path: "/docs",
    component: () => import("../layouts/PublicDocsLayout.vue"),
    children: [
      { path: "", name: "Docs", component: () => import("../views/public/docs/DocView.vue") },
      { path: "admin", name: "DocsAdmin", component: () => import("../views/public/docs/DocView.vue") },
      { path: "admin/:slug", name: "DocsAdminSlug", component: () => import("../views/public/docs/DocView.vue") },
      { path: "developers", name: "DocsDevelopers", component: () => import("../views/public/docs/DocView.vue") },
      { path: "developers/:slug", name: "DocsDevelopersSlug", component: () => import("../views/public/docs/DocView.vue") },
      { path: ":slug", name: "DocsSlug", component: () => import("../views/public/docs/DocView.vue") }
    ]
  },
  {
    path: "/sign-up",
    name: "SignUp",
    component: () => import("../views/auth/SignUp.vue"),
    beforeEnter(to, from, next) {
      signInSignUpBeforeEnter(to, next, '/sign-up')
    }
  },
  {
    path: "/sign-in",
    name: "SignIn",
    component: () => import("../views/auth/SignIn.vue"),
    beforeEnter(to, from, next) {
      signInSignUpBeforeEnter(to, next, '/sign-in')
    }
  },
  {
    path: "/forgot-password",
    name: "ForgotPassword",
    component: () => import("../views/auth/ForgotPassword.vue")
  },
  {
    path: "/reset-password",
    name: "ResetPassword",
    component: () => import("../views/auth/ResetPassword.vue")
  },
  {
    path: "/confirm-email",
    name: "ConfirmEmail",
    component: () => import("../views/auth/ConfirmEmail.vue")
  },
  {
    path: "/resend-confirmation",
    name: "ResendConfirmation",
    component: () => import("../views/auth/ResendConfirmation.vue")
  },
  {
    path: "/auth/:provider/callback",
    name: "SocialLoginCallback",
    component: () => import("../views/auth/SocialLoginCallback.vue")
  },
  {
    path: "/invite/:token",
    name: "AcceptInvitation",
    component: () => import("../views/invitations/AcceptInvitation.vue")
  },
  {
    path: "/t/:tenant",
    children: [
      {
        path: "sign-in",
        name: "TenantSignIn",
        component: () => import("../views/auth/SignIn.vue"),
        beforeEnter(to, from, next) {
          signInSignUpBeforeEnter(to, next, "/sign-in")
        }
      },
      {
        path: "admin",
        component: AdminLayout,
        redirect: (to) => {
          const base = `/t/${to.params.tenant}/admin`
          return isPartnerStaffFromJwt()
            ? { path: `${base}/usage`, query: to.query }
            : { path: `${base}/workspaces`, query: to.query }
        },
        children: adminLayoutChildren
      }
    ]
  },
  {
    path: "/admin/:pathMatch(.*)*",
    redirect: (to) => {
      const slug = rememberedTenant()
      if (!slug) return { path: "/sign-in", query: { returnUrl: to.fullPath } }
      return { path: `/t/${slug}${to.path}`, query: to.query, hash: to.hash }
    }
  },
  {
    path: "/:pathMatch(.*)*",
    name: "NotFound",
    component: NotFound
  }
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
})

function stripTenant(path) {
  return path.replace(/^\/t\/[^/]+/, "") || "/"
}

router.beforeEach((to) => {
  if (typeof to.params.tenant === "string" && to.params.tenant) {
    rememberTenant(to.params.tenant)
  }

  const adminPath = stripTenant(to.path)
  const isBareAdmin = to.path === "/admin" || to.path.startsWith("/admin/")
  if (isBareAdmin && !to.params.tenant) {
    const slug = rememberedTenant()
    if (slug) {
      return { path: `/t/${slug}${to.path}`, query: to.query, hash: to.hash, replace: true }
    }
  }

  if (adminPath === "/admin/accounts" && !isSuperAdmin()) {
    return { path: tenantForbidden(to), replace: true }
  }
  if (adminPath === "/admin/partners" && !isSuperAdmin()) {
    return { path: tenantForbidden(to), replace: true }
  }
  if (isObservabilityAdminRoute(adminPath) && !canAccessObservabilityFromSession()) {
    return { path: tenantForbidden(to), replace: true }
  }
})

function tenantForbidden(to) {
  const slug = typeof to.params.tenant === "string" ? to.params.tenant : rememberedTenant()
  return slug ? `/t/${slug}/admin/forbidden` : "/admin/forbidden"
}

function canAccessObservabilityFromSession() {
  if (canAccessObservabilityAdmin()) return true
  const profile = useAppStore().profile
  return profile?.isAccountOwner === true || profile?.IsAccountOwner === true
}

/** Rutas de la sección Observabilidad (SuperAdmin o dueño de cuenta). */
function isObservabilityAdminRoute(path) {
  return path === "/admin/webhook-monitor" || path.startsWith("/admin/observability/")
}

export default router

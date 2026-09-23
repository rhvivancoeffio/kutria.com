/**
 * Finbuckle.MultiTenant resolves the tenant from the path `/t/{identifier}`.
 * Static Web Apps cannot use a tenant subdomain. Never use Account/Workspace as the boundary.
 */

const TENANT_STORAGE_KEY = 'kutria-tenant'
const PATH_RE = /\/t\/([a-z0-9-]+)(?:\/|$)/i

/**
 * @returns {string} Tenant identifier, or '' when the URL has no `/t/{id}` segment.
 */
export function resolveTenantSlug() {
  if (typeof window === 'undefined') return ''
  const match = window.location.pathname.match(PATH_RE)
  return match?.[1] ? match[1].toLowerCase() : ''
}

export function rememberTenant(identifier) {
  const slug = String(identifier || '').trim().toLowerCase()
  if (!slug) return
  try {
    localStorage.setItem(TENANT_STORAGE_KEY, slug)
  } catch (_) {}
}

export function rememberedTenant() {
  try {
    return localStorage.getItem(TENANT_STORAGE_KEY) || ''
  } catch (_) {
    return ''
  }
}

/** SPA path under the current tenant, e.g. tenantPath('/admin') → `/t/acme/admin`. */
export function tenantPath(path = '/') {
  const slug = resolveTenantSlug() || rememberedTenant()
  const suffix = path.startsWith('/') ? path : `/${path}`
  if (!slug) return suffix
  return `/t/${slug}${suffix === '/' ? '' : suffix}`
}

/** API prefix Finbuckle route strategy reads: `/t/{identifier}`. */
export function tenantApiPrefix() {
  const slug = resolveTenantSlug() || rememberedTenant()
  return slug ? `/t/${slug}` : ''
}

export function tenantPathHint(slug = resolveTenantSlug()) {
  return slug ? `/t/${slug}` : '/t/{tienda}'
}

const PUBLIC_AUTH_PREFIXES = [
  '/auth/config',
  '/auth/signup',
  '/auth/signin',
  '/auth/tenants',
  '/auth/forgot-password',
  '/auth/reset-password',
  '/auth/confirm-email',
  '/auth/resend-confirm-email'
]

/** Prefix tenant-scoped API calls. Public auth stays at the API root. */
export function withTenantApiPath(url, tenant) {
  if (!url || url.startsWith('/t/')) return url
  if (PUBLIC_AUTH_PREFIXES.some((prefix) => url.startsWith(prefix))) return url
  const slug = tenant || resolveTenantSlug() || rememberedTenant()
  if (!slug) return url
  return `/t/${slug}${url.startsWith('/') ? url : `/${url}`}`
}

export const FINBUCKLE_TENANT_HEADER = 'X-Tenant'

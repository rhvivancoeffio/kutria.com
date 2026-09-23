const AUTH_TOKEN_KEY = 'auth_token'

/**
 * Parses JWT payload from localStorage token. Returns null if invalid or missing.
 */
export function parseJwtPayload() {
  try {
    const token = localStorage.getItem(AUTH_TOKEN_KEY)
    if (!token) return null
    const parts = token.split('.')
    if (parts.length !== 3) return null
    const payload = parts[1]
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const json = atob(base64)
    return JSON.parse(json)
  } catch {
    return null
  }
}

/**
 * Returns true if the current user has SuperAdmin claim in the JWT.
 */
export function isSuperAdmin() {
  const payload = parseJwtPayload()
  return payload?.super_admin === 'true'
}

/** Claim `partner_id` from JWT (Guid string), if present. */
export function getPartnerIdFromJwt() {
  const payload = parseJwtPayload()
  const id = payload?.partner_id
  return typeof id === 'string' && id.trim() ? id.trim() : null
}

export function isPartnerUserFromJwt() {
  const payload = parseJwtPayload()
  return payload?.partner_user === 'true' || payload?.partner_user === true
}

export function isPartnerStaffFromJwt() {
  const payload = parseJwtPayload()
  return payload?.partner_staff === 'true' || payload?.partner_staff === true
}

/**
 * Returns true if the current user is account owner (JWT claim account_owner).
 */
export function isAccountOwnerFromJwt() {
  const payload = parseJwtPayload()
  return payload?.account_owner === 'true'
}

function parseJwtPayloadFromToken(token) {
  try {
    if (!token) return null
    const parts = token.split('.')
    if (parts.length !== 3) return null
    const payload = parts[1]
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const json = atob(base64)
    return JSON.parse(json)
  } catch {
    return null
  }
}

/**
 * @returns {string[]}
 */
export function getPermissionsFromToken(token) {
  const payload = parseJwtPayloadFromToken(token ?? localStorage.getItem(AUTH_TOKEN_KEY))
  if (!payload?.permissions) return []
  try {
    const raw = payload.permissions
    if (Array.isArray(raw)) return raw.filter((p) => typeof p === 'string')
    if (typeof raw === 'string') {
      const parsed = JSON.parse(raw)
      return Array.isArray(parsed) ? parsed.filter((p) => typeof p === 'string') : []
    }
  } catch {
    return []
  }
  return []
}

/**
 * @param {string} permissionKey
 * @param {{ permissions?: string[], isAccountOwner?: boolean }} [opts]
 */
export function hasPermission(permissionKey, opts = {}) {
  if (opts.isAccountOwner || isSuperAdmin() || isAccountOwnerFromJwt()) return true
  const perms = opts.permissions ?? getPermissionsFromToken()
  return perms.includes(permissionKey)
}

/** SuperAdmin or account owner — access to Observabilidad admin routes and APIs. */
export function canAccessObservabilityAdmin() {
  return isSuperAdmin() || isAccountOwnerFromJwt()
}

/**
 * Menús laterales del admin por rol: Super Admin, Admin (owner), Member, Partner (staff).
 * Un solo `resolveSidebarMenuKey` elige el menú; entradas opcionales usan flags declarativos.
 */

export type SidebarMenuKey = 'superAdmin' | 'admin' | 'member' | 'partner'

export type SidebarNavHeading = {
  kind: 'heading'
  label: string
  /** Solo si el usuario es dueño de cuenta (p. ej. Workspaces). */
  requiresAccountOwner?: boolean
  /** Solo si JWT/perfil indica partner staff (portal programa). */
  requiresPartnerPortal?: boolean
  /** Asignado al renderizar: primer bloque del nav usa padding superior menor. */
  firstInNav?: boolean
}

export type SidebarNavLink = {
  kind: 'link'
  label: string
  title: string
  /** Ruta legacy `/admin/...` para active state con useAdminPaths */
  legacyPath: string
  icon: string
  excludePaths?: string[]
  requiresAccountOwner?: boolean
  requiresPartnerPortal?: boolean
  /** Permission key from member-permissions seed (e.g. integrations.list). Owners bypass. */
  requiresPermission?: string
}

/** Submenú colapsable (Integraciones, o bloque ex-encabezado «Uso diario», etc.). */
export type SidebarNavGroup = {
  kind: 'group'
  label: string
  title: string
  icon: string
  children: SidebarNavLink[]
  /** Menos padding superior cuando reemplaza al primer encabezado del nav. */
  sectionFirst?: boolean
  /** Solo si el tenant eligió tienda nativa Gravity (`commerceStoreMode === Native`). */
  requiresNativeStore?: boolean
  /** Oculto si el tenant es tienda nativa (p. ej. Ingesta de conectores externos). */
  hideWhenNativeStore?: boolean
}

export type SidebarNavEntry = SidebarNavHeading | SidebarNavLink | SidebarNavGroup

const heading = (
  label: string,
  opts: { requiresAccountOwner?: boolean; requiresPartnerPortal?: boolean } = {}
): SidebarNavHeading => ({ kind: 'heading', label, ...opts })

const link = (
  label: string,
  title: string,
  legacyPath: string,
  icon: string,
  opts: { excludePaths?: string[]; requiresAccountOwner?: boolean; requiresPartnerPortal?: boolean; requiresPermission?: string } = {}
): SidebarNavLink => ({ kind: 'link', label, title, legacyPath, icon, ...opts })

const group = (
  label: string,
  title: string,
  icon: string,
  children: SidebarNavLink[],
  opts: { sectionFirst?: boolean; requiresNativeStore?: boolean; hideWhenNativeStore?: boolean } = {}
): SidebarNavGroup => ({
  kind: 'group',
  label,
  title,
  icon,
  children,
  ...(opts.sectionFirst ? { sectionFirst: true } : {}),
  ...(opts.requiresNativeStore ? { requiresNativeStore: true } : {}),
  ...(opts.hideWhenNativeStore ? { hideWhenNativeStore: true } : {})
})

/** Convierte cada `heading` + enlaces contiguos en un `group` colapsable (mismo comportamiento que Integraciones). */
function mergeHeadingSectionsIntoGroups(entries: SidebarNavEntry[]): SidebarNavEntry[] {
  const result: SidebarNavEntry[] = []
  let i = 0
  while (i < entries.length) {
    const cur = entries[i]
    if (cur.kind === 'heading') {
      const heading = cur
      const links: SidebarNavLink[] = []
      i++
      while (i < entries.length && entries[i].kind === 'link') {
        links.push(entries[i] as SidebarNavLink)
        i++
      }
      if (links.length > 0) {
        const icon = links[0]?.icon ?? 'home'
        result.push(
          group(heading.label, heading.label, icon, links, {
            sectionFirst: heading.firstInNav === true
          })
        )
      }
      continue
    }
    result.push(cur)
    i++
  }
  return result
}

const MENU_SUPER_ADMIN: SidebarNavEntry[] = [
  link('Workspaces', 'Workspaces', '/admin/workspaces', 'workspaces', { requiresAccountOwner: true }),
  heading('Uso diario'),
  link('Panel de uso', 'Panel de uso', '/admin/usage', 'chartBars'),
  group('Configurations', 'Cerebro, integraciones y carga de documentos', 'cloudUpload', [
    link('Cerebro', 'Subir una política en PDF, Word, JSON o TXT', '/admin/policies', 'cloudUpload'),
    link('Integraciones', 'Integraciones', '/admin/integraciones', 'link', {
      excludePaths: ['/admin/integraciones/tienda']
    })
  ]),
  group('Tienda', 'Productos, categorías y marcas', 'shoppingBag', [
    link('Productos', 'Productos de la tienda', '/admin/tienda/productos', 'cube'),
    link('Categories', 'Categorías de la tienda', '/admin/tienda/categories', 'tag'),
    link('Marcas', 'Marcas de la tienda', '/admin/tienda/marcas', 'brand')
  ], { requiresNativeStore: true }),
  group('Ingesta', 'Catálogo y ventas ingeridos', 'cube', [
    link('Catalog', 'Preview del catálogo ingerido', '/admin/integraciones/tienda/catalog', 'cube'),
    link('Ventas', 'Preview de pedidos ingeridos', '/admin/integraciones/tienda/ventas', 'chartBars')
  ], { hideWhenNativeStore: true }),
  heading('Partner', { requiresPartnerPortal: true }),
  link('Cuentas programa', 'Cuentas provisionadas del programa', '/admin/partner/accounts', 'buildingProgram', {
    requiresPartnerPortal: true
  }),
  heading('Observabilidad'),
  link('Webhooks', 'Webhooks', '/admin/webhook-monitor', 'webhook'),
  link('Webhooks Out', 'Histórico de webhooks salientes (HTTP)', '/admin/observability/webhooks-out', 'bolt'),
  link('Outbox', 'Outbox transaccional: productos y pedidos', '/admin/observability/outbox', 'outbox'),
  link('Chat', 'Eventos de chat (turnos y agentes)', '/admin/observability/chat-events', 'chat'),
  heading('Cuenta'),
  link('Accounts', 'Accounts', '/admin/accounts', 'building'),
  link('Partners', 'Partners', '/admin/partners', 'partners'),
  link('Miembros', 'Miembros', '/admin/members', 'members', { requiresAccountOwner: true }),
  link('Facturación', 'Facturación', '/admin/billing', 'card', { requiresAccountOwner: true }),
  link('Historial de consumo', 'Historial de consumo', '/admin/consumption-history', 'chartBars', {
    requiresAccountOwner: true
  }),
  link('API Keys', 'API Keys', '/admin/api-keys', 'key', { requiresAccountOwner: true }),
  link('Webhooks salientes', 'Configurar URLs por evento (cuenta)', '/admin/outbound-webhooks', 'bolt', {
    requiresAccountOwner: true
  }),
  link('Variables de entorno', 'Variables de entorno', '/admin/environment-variables', 'variable', {
    requiresAccountOwner: true
  }),
  link('Embed chat', 'Copy the tenant embed snippet', '/admin/embed', 'play', { requiresAccountOwner: true })
]

const MENU_ADMIN: SidebarNavEntry[] = [
  link('Workspaces', 'Workspaces', '/admin/workspaces', 'workspaces', { requiresAccountOwner: true }),
  heading('Uso diario'),
  link('Panel de uso', 'Panel de uso', '/admin/usage', 'chartBars'),
  group('Configurations', 'Cerebro, integraciones y carga de documentos', 'cloudUpload', [
    link('Cerebro', 'Subir una política en PDF, Word, JSON o TXT', '/admin/policies', 'cloudUpload'),
    link('Integraciones', 'Integraciones', '/admin/integraciones', 'link', {
      excludePaths: ['/admin/integraciones/tienda']
    })
  ]),
  group('Tienda', 'Productos, categorías y marcas', 'shoppingBag', [
    link('Productos', 'Productos de la tienda', '/admin/tienda/productos', 'cube'),
    link('Categories', 'Categorías de la tienda', '/admin/tienda/categories', 'tag'),
    link('Marcas', 'Marcas de la tienda', '/admin/tienda/marcas', 'brand')
  ], { requiresNativeStore: true }),
  group('Ingesta', 'Catálogo y ventas ingeridos', 'cube', [
    link('Catalog', 'Preview del catálogo ingerido', '/admin/integraciones/tienda/catalog', 'cube'),
    link('Ventas', 'Preview de pedidos ingeridos', '/admin/integraciones/tienda/ventas', 'chartBars')
  ], { hideWhenNativeStore: true }),
  heading('Partner', { requiresPartnerPortal: true }),
  link('Cuentas programa', 'Cuentas provisionadas del programa', '/admin/partner/accounts', 'buildingProgram', {
    requiresPartnerPortal: true
  }),
  heading('Observabilidad'),
  link('Webhooks', 'Webhooks', '/admin/webhook-monitor', 'webhook'),
  link('Webhooks Out', 'Histórico de webhooks salientes (HTTP)', '/admin/observability/webhooks-out', 'bolt'),
  link('Outbox', 'Outbox transaccional: productos y pedidos', '/admin/observability/outbox', 'outbox'),
  link('Chat', 'Eventos de chat (turnos y agentes)', '/admin/observability/chat-events', 'chat'),
  heading('Cuenta'),
  link('Miembros', 'Miembros', '/admin/members', 'members', { requiresAccountOwner: true }),
  link('Facturación', 'Facturación', '/admin/billing', 'card', { requiresAccountOwner: true }),
  link('Historial de consumo', 'Historial de consumo', '/admin/consumption-history', 'chartBars', {
    requiresAccountOwner: true
  }),
  link('API Keys', 'API Keys', '/admin/api-keys', 'key', { requiresAccountOwner: true }),
  link('Webhooks salientes', 'Configurar URLs por evento (cuenta)', '/admin/outbound-webhooks', 'bolt', {
    requiresAccountOwner: true
  }),
  link('Variables de entorno', 'Variables de entorno', '/admin/environment-variables', 'variable', {
    requiresAccountOwner: true
  }),
  link('Embed chat', 'Copy the tenant embed snippet', '/admin/embed', 'play', { requiresAccountOwner: true })
]

const MENU_MEMBER: SidebarNavEntry[] = [
  heading('Uso diario'),
  link('Panel de uso', 'Panel de uso', '/admin/usage', 'chartBars'),
  group('Configurations', 'Cerebro, integraciones y carga de documentos', 'cloudUpload', [
    link('Cerebro', 'Subir una política en PDF, Word, JSON o TXT', '/admin/policies', 'cloudUpload'),
    link('Integraciones', 'Integraciones', '/admin/integraciones', 'link', {
      requiresPermission: 'integrations.list',
      excludePaths: ['/admin/integraciones/tienda']
    })
  ]),
  group('Tienda', 'Productos, categorías y marcas', 'shoppingBag', [
    link('Productos', 'Productos de la tienda', '/admin/tienda/productos', 'cube'),
    link('Categories', 'Categorías de la tienda', '/admin/tienda/categories', 'tag'),
    link('Marcas', 'Marcas de la tienda', '/admin/tienda/marcas', 'brand')
  ], { requiresNativeStore: true }),
  group('Ingesta', 'Catálogo y ventas ingeridos', 'cube', [
    link('Catalog', 'Preview del catálogo ingerido', '/admin/integraciones/tienda/catalog', 'cube', {
      requiresPermission: 'integrations.list'
    }),
    link('Ventas', 'Preview de pedidos ingeridos', '/admin/integraciones/tienda/ventas', 'chartBars', {
      requiresPermission: 'integrations.list'
    })
  ], { hideWhenNativeStore: true }),
  heading('Partner', { requiresPartnerPortal: true }),
  link('Cuentas programa', 'Cuentas provisionadas del programa', '/admin/partner/accounts', 'buildingProgram', {
    requiresPartnerPortal: true
  }),
  heading('Observabilidad'),
  link('Webhooks', 'Webhooks', '/admin/webhook-monitor', 'webhook'),
  link('Outbox', 'Outbox transaccional: productos y pedidos', '/admin/observability/outbox', 'outbox')
]

/** Partner staff (no SuperAdmin): panel de uso + programa; sin dashboard operador. */
const MENU_PARTNER: SidebarNavEntry[] = [
  heading('Uso diario'),
  link('Inicio', 'Inicio', '/admin/usage', 'home'),
  group('Configurations', 'Cerebro y carga de documentos', 'cloudUpload', [
    link('Cerebro', 'Subir una política en PDF, Word, JSON o TXT', '/admin/policies', 'cloudUpload')
  ]),
  heading('Partner'),
  link('Cuentas programa', 'Cuentas provisionadas del programa', '/admin/partner/accounts', 'buildingProgram')
]

export const ADMIN_SIDEBAR_MENUS: Record<SidebarMenuKey, SidebarNavEntry[]> = {
  superAdmin: MENU_SUPER_ADMIN,
  admin: MENU_ADMIN,
  member: MENU_MEMBER,
  partner: MENU_PARTNER
}

export type SidebarMenuContext = {
  isSuperAdmin: boolean
  isAccountOwner: boolean
  showPartnerPortal: boolean
}

export function resolveSidebarMenuKey(ctx: SidebarMenuContext): SidebarMenuKey {
  if (ctx.isSuperAdmin) return 'superAdmin'
  if (ctx.showPartnerPortal) return 'partner'
  if (ctx.isAccountOwner) return 'admin'
  return 'member'
}

function linkEntryVisible(
  e: SidebarNavLink,
  ctx: {
    isAccountOwner: boolean
    showPartnerPortal: boolean
    canPermission?: (key: string) => boolean
    isNativeStore?: boolean
  }
): boolean {
  if (e.requiresAccountOwner && !ctx.isAccountOwner) return false
  if (e.requiresPartnerPortal && !ctx.showPartnerPortal) return false
  if (e.requiresPermission && ctx.canPermission && !ctx.isAccountOwner && !ctx.canPermission(e.requiresPermission))
    return false
  return true
}

function entryVisible(
  e: SidebarNavEntry,
  ctx: {
    isAccountOwner: boolean
    showPartnerPortal: boolean
    canPermission?: (key: string) => boolean
    isNativeStore?: boolean
  }
): boolean {
  if (e.kind === 'group') {
    if (e.requiresNativeStore && !ctx.isNativeStore) return false
    if (e.hideWhenNativeStore && ctx.isNativeStore) return false
    return e.children.some((c) => linkEntryVisible(c, ctx))
  }
  if (e.kind === 'link') return linkEntryVisible(e, ctx)
  if (e.requiresAccountOwner && !ctx.isAccountOwner) return false
  if (e.requiresPartnerPortal && !ctx.showPartnerPortal) return false
  return true
}

/** Filtra por flags y elimina encabezados sin ningún enlace visible debajo. */
export function buildVisibleSidebarNav(
  entries: SidebarNavEntry[],
  ctx: {
    isAccountOwner: boolean
    showPartnerPortal: boolean
    canPermission?: (key: string) => boolean
    isNativeStore?: boolean
  }
): SidebarNavEntry[] {
  const filtered = entries
    .map((e) => {
      if (e.kind !== 'group') return e
      if (e.requiresNativeStore && !ctx.isNativeStore) return null
      if (e.hideWhenNativeStore && ctx.isNativeStore) return null
      const kids = e.children.filter((c) => linkEntryVisible(c, ctx))
      return kids.length ? { ...e, children: kids } : null
    })
    .filter((e): e is SidebarNavEntry => e != null && entryVisible(e, ctx))
  const out: SidebarNavEntry[] = []
  for (let i = 0; i < filtered.length; i++) {
    const cur = filtered[i]
    if (cur.kind !== 'heading') {
      out.push(cur)
      continue
    }
    let j = i + 1
    let hasLink = false
    while (j < filtered.length && filtered[j].kind !== 'heading') {
      const below = filtered[j]
      if (below.kind === 'link') {
        hasLink = true
        break
      }
      if (below.kind === 'group' && below.children.length > 0) {
        hasLink = true
        break
      }
      j++
    }
    if (hasLink) out.push(cur)
  }
  let firstHeading = true
  const withFirstHeading = out.map((e) => {
    if (e.kind === 'heading' && firstHeading) {
      firstHeading = false
      return { ...e, firstInNav: true }
    }
    return e
  })
  return mergeHeadingSectionsIntoGroups(withFirstHeading)
}

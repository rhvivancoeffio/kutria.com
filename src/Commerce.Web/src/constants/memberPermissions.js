/** Permission keys by resource; aligned with data/seed/identity/member-permissions.yaml */
export const MEMBER_PERM = {
  integrations: {
    list: 'integrations.list',
    create: 'integrations.create'
  }
}

/** Vue route name → permission required to enter (Integraciones only in template). */
export const ROUTE_MEMBER_PERMISSION = {
  AdminIntegraciones: MEMBER_PERM.integrations.list,
  AdminStoreCatalog: MEMBER_PERM.integrations.list,
  AdminStoreOrders: MEMBER_PERM.integrations.list,
  AdminStoreCatalogIngestions: MEMBER_PERM.integrations.list,
  AdminStoreOrdersIngestions: MEMBER_PERM.integrations.list
}

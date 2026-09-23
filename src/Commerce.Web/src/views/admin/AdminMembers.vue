<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Miembros</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Invita miembros a tu cuenta y gestiona el equipo
          </p>
        </div>
        <div class="flex flex-col gap-2 sm:items-end shrink-0">
          <PlanLimitAlert :message="membersLimitMessage" />
          <button
            type="button"
            :disabled="membersAtLimit"
            @click="openInviteModal"
            :class="[
              'inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium transition-colors text-sm sm:text-base min-h-[44px] touch-manipulation',
              membersAtLimit ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white'
            ]"
          >
            <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Invitar miembro
          </button>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="space-y-3 sm:space-y-4">
        <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <!-- Forbidden -->
      <div
        v-else-if="forbidden"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-amber-800 dark:text-amber-300">
          Solo el propietario de la cuenta puede gestionar los miembros.
        </p>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
      </div>

      <template v-else>
        <!-- Invitations section -->
        <div class="mb-6 sm:mb-8">
          <h2 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white mb-3 sm:mb-4">Invitaciones pendientes</h2>
          <div
            v-if="!invitations.length"
            class="text-center py-10 sm:py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
          >
            <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
            <p class="mt-4 text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay invitaciones pendientes</p>
          </div>
          <div v-else class="space-y-4 md:space-y-0">
            <!-- Mobile: cards -->
            <div class="md:hidden space-y-3">
              <div
                v-for="inv in invitations"
                :key="inv.id"
                class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0 flex-1">
                    <p class="font-medium text-gray-900 dark:text-white truncate">{{ inv.email || 'Sin email' }}</p>
                    <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">Expira {{ formatDate(inv.expiresAt) }}</p>
                  </div>
                  <div class="flex items-center gap-1 shrink-0">
                    <button
                      v-if="inv.email"
                      type="button"
                      @click="resendInvite(inv)"
                      :disabled="resendingId === inv.id"
                      class="inline-flex p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-colors disabled:opacity-50 touch-manipulation"
                      title="Reenviar invitación"
                      aria-label="Reenviar invitación"
                    >
                      <svg
                        class="w-4 h-4"
                        :class="{ 'animate-spin': resendingId === inv.id }"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                        />
                      </svg>
                    </button>
                    <button
                      type="button"
                      @click="confirmRevoke(inv)"
                      class="inline-flex p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors touch-manipulation"
                      title="Revocar invitación"
                      aria-label="Revocar invitación"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M6 18L18 6M6 6l12 12"
                        />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
            <!-- Desktop: table -->
            <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-x-auto">
              <table v-resizable class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-700/50">
                  <tr>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Email</th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Expira</th>
                    <th class="px-4 lg:px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Acciones</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                  <tr
                    v-for="inv in invitations"
                    :key="inv.id"
                    class="hover:bg-gray-50 dark:hover:bg-gray-700/30"
                  >
                    <td class="px-4 lg:px-6 py-4 font-medium text-gray-900 dark:text-white">{{ inv.email || 'Sin email' }}</td>
                    <td class="px-4 lg:px-6 py-4 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(inv.expiresAt) }}</td>
                    <td class="px-4 lg:px-6 py-4 text-right">
                      <div class="flex justify-end gap-1">
                        <button
                          v-if="inv.email"
                          type="button"
                          @click="resendInvite(inv)"
                          :disabled="resendingId === inv.id"
                          class="inline-flex p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-colors disabled:opacity-50"
                          title="Reenviar invitación"
                          aria-label="Reenviar invitación"
                        >
                          <svg
                            class="w-4 h-4"
                            :class="{ 'animate-spin': resendingId === inv.id }"
                            fill="none"
                            stroke="currentColor"
                            viewBox="0 0 24 24"
                            aria-hidden="true"
                          >
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
                            />
                          </svg>
                        </button>
                        <button
                          type="button"
                          @click="confirmRevoke(inv)"
                          class="inline-flex p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                          title="Revocar invitación"
                          aria-label="Revocar invitación"
                        >
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M6 18L18 6M6 6l12 12"
                            />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- Members section -->
        <div>
          <h2 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white mb-3 sm:mb-4">Miembros del equipo</h2>
          <div
            v-if="!members.length"
            class="text-center py-10 sm:py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
          >
            <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
            <p class="mt-4 text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay miembros además de ti</p>
          </div>
          <div v-else class="space-y-4 md:space-y-0">
            <!-- Mobile: cards -->
            <div class="md:hidden space-y-3">
              <div
                v-for="m in members"
                :key="m.userId"
                class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0 flex-1">
                    <p class="font-semibold text-gray-900 dark:text-white truncate">{{ m.displayName || m.email }}</p>
                    <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400 truncate">{{ m.email }}</p>
                    <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                      Último acceso: {{ formatDate(m.lastLoginAt) || '-' }}
                    </p>
                  </div>
                  <div class="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      @click="openPermissions(m)"
                      class="inline-flex p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-colors"
                      title="Permisos"
                      aria-label="Permisos"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                        />
                      </svg>
                    </button>
                    <button
                      type="button"
                      @click="confirmRemove(m)"
                      class="inline-flex p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                      title="Eliminar"
                      aria-label="Eliminar"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
            <!-- Desktop: table -->
            <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-x-auto">
              <table v-resizable class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-700/50">
                  <tr>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Nombre</th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Email</th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Último acceso</th>
                    <th class="px-4 lg:px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Acciones</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                  <tr
                    v-for="m in members"
                    :key="m.userId"
                    class="hover:bg-gray-50 dark:hover:bg-gray-700/30"
                  >
                    <td class="px-4 lg:px-6 py-4 font-medium text-gray-900 dark:text-white">{{ m.displayName || m.email }}</td>
                    <td class="px-4 lg:px-6 py-4 text-sm text-gray-600 dark:text-gray-300">{{ m.email }}</td>
                    <td class="px-4 lg:px-6 py-4 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(m.lastLoginAt) || '-' }}</td>
                    <td class="px-4 lg:px-6 py-4 text-right">
                      <div class="flex justify-end gap-2">
                        <button
                          type="button"
                          @click="openPermissions(m)"
                          class="inline-flex p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-colors"
                          title="Permisos"
                          aria-label="Permisos"
                        >
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                            />
                          </svg>
                        </button>
                        <button
                          type="button"
                          @click="confirmRemove(m)"
                          class="inline-flex p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                          title="Eliminar"
                        >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </template>
    </main>

    <!-- Invite slide-over -->
    <Teleport to="body">
      <Transition name="slide-overlay">
        <div
          v-if="inviteModalOpen"
          class="fixed inset-0 z-50 bg-black/50"
          aria-hidden="true"
          @click="closeInviteModal"
        />
      </Transition>
      <Transition name="slide-panel">
        <div
          v-if="inviteModalOpen"
          class="fixed inset-y-0 right-0 z-50 flex h-full w-full max-w-xl flex-col border-l border-gray-200 bg-white shadow-2xl dark:border-gray-700 dark:bg-gray-800"
          role="dialog"
          aria-modal="true"
          aria-labelledby="invite-member-slide-title"
          @click.stop
        >
          <div class="sticky top-0 z-10 flex shrink-0 items-start justify-between gap-3 border-b border-gray-200 bg-white px-4 py-4 dark:border-gray-700 dark:bg-gray-800 sm:px-6">
            <div class="min-w-0 pr-2">
              <h2 id="invite-member-slide-title" class="text-lg font-semibold text-gray-900 dark:text-white sm:text-xl">
                Invitar miembro
              </h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                Genera un enlace de invitación para compartir
              </p>
            </div>
            <button
              type="button"
              class="-m-2 shrink-0 rounded-lg p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300"
              aria-label="Cerrar"
              @click="closeInviteModal"
            >
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="min-h-0 flex-1 overflow-y-auto px-4 py-5 sm:px-6">
            <div class="space-y-6">
            <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Correo <span class="text-red-500">*</span></label>
            <input
              v-model="inviteEmail"
              type="email"
              required
              class="input-field w-full"
              :class="{ '!border-red-500 focus:!ring-red-500': inviteEmailTouched && !inviteEmailValid }"
              placeholder="email@ejemplo.com"
              @blur="inviteEmailTouched = true"
            />
            <p v-if="inviteEmailTouched && !inviteEmailValid" class="mt-1 text-sm text-red-600 dark:text-red-400">
              El correo es obligatorio
            </p>
          </div>
            <section v-if="!inviteLink" aria-labelledby="invite-permissions-heading">
            <h3 id="invite-permissions-heading" class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Permisos</h3>
            <MemberPermissionsChecklist v-model="invitePermissions" />
            </section>
            <div v-if="inviteLink">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Enlace generado</label>
            <div class="flex gap-2">
              <input :value="inviteLink" readonly class="input-field flex-1 min-w-0 text-sm font-mono" />
              <button
                type="button"
                @click="copyInviteLink"
                class="px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-500 text-gray-800 dark:text-gray-200 text-sm font-medium shrink-0"
              >
                Copiar
              </button>
            </div>
            </div>
            </div>
          </div>

          <div class="shrink-0 border-t border-gray-200 dark:border-gray-700 bg-gray-50/80 dark:bg-gray-900/40 px-4 sm:px-6 py-4 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              @click="closeInviteModal"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              {{ inviteLink ? 'Cerrar' : 'Cancelar' }}
            </button>
            <button
              v-if="!inviteLink"
              type="button"
              @click="invite"
              :disabled="inviteLoading || !inviteEmailValid"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
                (inviteLoading || !inviteEmailValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white'
              ]"
            >
              {{ inviteLoading ? 'Generando...' : 'Generar enlace' }}
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Revoke confirmation modal -->
    <Teleport to="body">
      <div
        v-if="revokeTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Revocar invitación</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Revocar la invitación para {{ revokeTarget.email || 'este correo' }}?
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button @click="revokeTarget = null" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">Cancelar</button>
            <button @click="doRevoke" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation bg-red-600 hover:bg-red-700 text-white rounded-lg">Revocar</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Edit permissions slide-over -->
    <Teleport to="body">
      <Transition name="slide-overlay">
        <div
          v-if="permissionsTarget"
          class="fixed inset-0 z-50 bg-black/50"
          aria-hidden="true"
          @click="closePermissions"
        />
      </Transition>
      <Transition name="slide-panel">
        <div
          v-if="permissionsTarget"
          class="fixed inset-y-0 right-0 z-50 flex h-full w-full max-w-xl flex-col border-l border-gray-200 bg-white shadow-2xl dark:border-gray-700 dark:bg-gray-800"
          role="dialog"
          aria-modal="true"
          aria-labelledby="member-permissions-slide-title"
          @click.stop
        >
          <div class="sticky top-0 z-10 flex shrink-0 items-start justify-between gap-3 border-b border-gray-200 bg-white px-4 py-4 dark:border-gray-700 dark:bg-gray-800 sm:px-6">
            <div class="min-w-0 pr-2">
              <h2 id="member-permissions-slide-title" class="text-lg font-semibold text-gray-900 dark:text-white sm:text-xl">
                Permisos de miembro
              </h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400 truncate">
                {{ permissionsTarget.displayName || permissionsTarget.email }}
              </p>
              <p v-if="permissionsTarget.displayName" class="mt-0.5 text-xs text-gray-500 dark:text-gray-400 truncate">
                {{ permissionsTarget.email }}
              </p>
            </div>
            <button
              type="button"
              class="-m-2 shrink-0 rounded-lg p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-300"
              aria-label="Cerrar"
              :disabled="permissionsSaving"
              @click="closePermissions"
            >
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="min-h-0 flex-1 overflow-y-auto px-4 py-5 sm:px-6">
            <MemberPermissionsChecklist v-model="editPermissions" />
          </div>

          <div class="shrink-0 border-t border-gray-200 dark:border-gray-700 bg-gray-50/80 dark:bg-gray-900/40 px-4 sm:px-6 py-4 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              @click="closePermissions"
              :disabled="permissionsSaving"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg disabled:opacity-50"
            >
              Cancelar
            </button>
            <button
              type="button"
              @click="savePermissions"
              :disabled="permissionsSaving"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white font-medium disabled:opacity-50"
            >
              {{ permissionsSaving ? 'Guardando...' : 'Guardar' }}
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Remove member confirmation modal -->
    <Teleport to="body">
      <div
        v-if="removeTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar miembro</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Eliminar a {{ removeTarget.displayName || removeTarget.email }} del equipo?
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button @click="removeTarget = null" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">Cancelar</button>
            <button @click="doRemove" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation bg-red-600 hover:bg-red-700 text-white rounded-lg">Eliminar</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import PlanLimitAlert from '../../components/usage/PlanLimitAlert.vue'
import MemberPermissionsChecklist from '../../components/admin/MemberPermissionsChecklist.vue'
import { useAccountUsage } from '../../composables/useAccountUsage'

const toast = useToast()
const { membersAtLimit, membersLimitMessage, fetchUsage } = useAccountUsage()
const members = ref([])
const invitations = ref([])
const loading = ref(true)
const error = ref(null)
const forbidden = ref(false)
const inviteModalOpen = ref(false)
const inviteEmail = ref('')
const inviteEmailTouched = ref(false)
const inviteLoading = ref(false)
const inviteLink = ref('')
const invitePermissions = ref([])
const revokeTarget = ref(null)
const removeTarget = ref(null)
const permissionsTarget = ref(null)
const editPermissions = ref([])
const permissionsSaving = ref(false)
const resendingId = ref(null)

function formatDate(d) {
  if (!d) return '-'
  const date = typeof d === 'string' ? new Date(d) : d
  return date.toLocaleDateString('es-ES', { dateStyle: 'short' })
}

const inviteEmailValid = computed(() => {
  const e = inviteEmail.value?.trim()
  return !!e && /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(e)
})

function openInviteModal() {
  inviteModalOpen.value = true
  inviteEmailTouched.value = false
}

function closeInviteModal() {
  inviteModalOpen.value = false
  inviteLink.value = ''
  inviteEmail.value = ''
  inviteEmailTouched.value = false
  invitePermissions.value = []
}

function openPermissions(member) {
  permissionsTarget.value = member
  editPermissions.value = Array.isArray(member.permissions) ? [...member.permissions] : []
}

function closePermissions() {
  if (permissionsSaving.value) return
  permissionsTarget.value = null
}

async function savePermissions() {
  if (!permissionsTarget.value) return
  permissionsSaving.value = true
  try {
    const result = await apiService.updateMemberPermissions(
      permissionsTarget.value.userId,
      editPermissions.value
    )
    const idx = members.value.findIndex((m) => m.userId === permissionsTarget.value.userId)
    if (idx >= 0) {
      members.value[idx] = {
        ...members.value[idx],
        permissions: result.permissions ?? editPermissions.value
      }
    }
    toast.success('Permisos actualizados')
    permissionsTarget.value = null
  } catch (err) {
    toast.error(err.response?.data?.error || 'Error al guardar permisos')
  } finally {
    permissionsSaving.value = false
  }
}

async function load() {
  loading.value = true
  error.value = null
  forbidden.value = false
  try {
    const [membersData, invitationsData] = await Promise.all([
      apiService.getAccountMembers(),
      apiService.getPendingInvitations(),
      fetchUsage()
    ])
    members.value = Array.isArray(membersData) ? membersData : []
    invitations.value = Array.isArray(invitationsData) ? invitationsData : []
  } catch (err) {
    if (err.response?.status === 403) {
      forbidden.value = true
    } else {
      error.value = err.response?.data?.error || err.response?.data?.message || 'Error al cargar'
    }
  } finally {
    loading.value = false
  }
}

async function invite() {
  inviteLoading.value = true
  inviteLink.value = ''
  error.value = null
  try {
    const result = await apiService.inviteMember(
      inviteEmail.value?.trim() || null,
      invitePermissions.value?.length ? invitePermissions.value : null
    )
    if (result?.token) {
      inviteLink.value = `${window.location.origin}/invite/${result.token}`
      await load()
    } else {
      error.value = result?.error || 'No se pudo generar el enlace'
    }
  } catch (err) {
    error.value = err.response?.data?.error || 'Error al invitar'
  } finally {
    inviteLoading.value = false
  }
}

function copyInviteLink() {
  navigator.clipboard.writeText(inviteLink.value).then(() => {
    toast.success('Enlace copiado al portapapeles')
  }).catch(() => {})
}

async function resendInvite(inv) {
  if (!inv.email) return
  resendingId.value = inv.id
  try {
    const result = await apiService.resendInvitation(inv.id)
    const link = result?.token ? `${window.location.origin}/invite/${result.token}` : null
    if (link) {
      inviteLink.value = link
      inviteModalOpen.value = true
      toast.success('Enlace reenviado por correo. Puedes copiarlo aquí.')
    } else {
      toast.success('Enlace reenviado por correo')
    }
  } catch (err) {
    const token = err.response?.data?.token
    if (token) {
      inviteLink.value = `${window.location.origin}/invite/${token}`
      inviteModalOpen.value = true
      toast.warning(err.response?.data?.error || 'Error al enviar. Puedes copiar el enlace aquí.')
    } else {
      error.value = err.response?.data?.error || 'Error al reenviar'
    }
  } finally {
    resendingId.value = null
  }
}

function confirmRevoke(inv) {
  revokeTarget.value = inv
}

async function doRevoke() {
  if (!revokeTarget.value) return
  try {
    await apiService.revokeInvitation(revokeTarget.value.id)
    revokeTarget.value = null
    await load()
    toast.success('Invitación revocada')
  } catch (err) {
    error.value = err.response?.data?.error || 'Error al revocar'
  }
}

function confirmRemove(m) {
  removeTarget.value = m
}

async function doRemove() {
  if (!removeTarget.value) return
  try {
    await apiService.removeMember(removeTarget.value.userId)
    removeTarget.value = null
    await load()
    toast.success('Miembro eliminado')
  } catch (err) {
    error.value = err.response?.data?.error || 'Error al eliminar'
  }
}

onMounted(load)
</script>

<style scoped>
.slide-overlay-enter-active,
.slide-overlay-leave-active {
  transition: opacity 0.3s ease;
}
.slide-overlay-enter-from,
.slide-overlay-leave-to {
  opacity: 0;
}
.slide-panel-enter-active {
  transition: transform 0.3s ease-out;
}
.slide-panel-leave-active {
  transition: transform 0.3s ease-in;
}
.slide-panel-enter-from,
.slide-panel-leave-to {
  transform: translateX(100%);
}
</style>

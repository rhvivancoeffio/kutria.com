<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Cuentas del programa</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Cuentas activas e invitaciones de programa: gestiona altas pendientes, reenvía el correo o revoca enlaces.
          </p>
        </div>
        <button type="button" class="btn-primary shrink-0 self-start" :disabled="loading" @click="openInvitePanel">
          Nueva Invitación
        </button>
      </div>

      <div v-if="loading" class="space-y-8">
        <div class="space-y-2">
          <div class="h-5 w-40 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          <div v-for="i in 4" :key="'a' + i" class="h-12 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
        </div>
        <div class="space-y-2">
          <div class="h-5 w-48 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          <div v-for="i in 3" :key="'b' + i" class="h-12 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
        </div>
      </div>
      <div
        v-else-if="error"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-sm text-red-800 dark:text-red-300"
      >
        {{ error }}
      </div>
      <div v-else class="space-y-10">
        <div class="max-w-xl">
          <label for="partner-accounts-search" class="sr-only">Buscar cuentas e invitaciones</label>
          <input
            id="partner-accounts-search"
            v-model="searchQuery"
            type="search"
            autocomplete="off"
            class="input-field w-full"
            placeholder="Cuentas: correo, ID o plan · Invitaciones: correo, ID o estado"
          />
        </div>

        <section>
          <div class="mb-3 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Cuentas activas</h2>
            <button
              type="button"
              class="btn-secondary shrink-0 self-start sm:self-auto text-sm"
              :disabled="accountsTotalCount === 0 || csvBusy"
              @click="downloadAccountsCsv"
            >
              Descargar CSV
            </button>
          </div>
          <div
            v-if="accountsTotalCount === 0"
            class="text-center py-10 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-sm"
          >
            <p class="text-gray-600 dark:text-gray-300">No hay cuentas activas aún.</p>
            <p class="mt-2 text-xs text-gray-500 dark:text-gray-500">
              Usa «Nueva Invitación» arriba para generar un enlace de alta.
            </p>
          </div>
          <div
            v-else-if="!filteredAccountRows.length"
            class="text-center py-10 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-sm"
          >
            <p class="text-gray-600 dark:text-gray-300">Ninguna cuenta coincide con la búsqueda.</p>
            <p class="mt-2 text-xs text-gray-500 dark:text-gray-500">Prueba con otro correo, trozo del GUID o nombre del plan.</p>
          </div>
          <div
            v-else
            class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden"
          >
            <div class="overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-900/50">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                      Account ID
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                      Titular (correo)
                    </th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Plan</th>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Creado</th>
                    <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                      Acciones
                    </th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                  <tr v-for="r in filteredAccountRows" :key="accountRowId(r)" class="hover:bg-gray-50 dark:hover:bg-gray-700/50">
                    <td class="px-4 py-3 text-sm font-mono text-gray-800 dark:text-gray-200">{{ accountRowId(r) }}</td>
                    <td class="px-4 py-3 text-sm text-gray-800 dark:text-gray-200 break-all">
                      {{ accountOwnerEmail(r) || '—' }}
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-700 dark:text-gray-300">{{ r.planKey ?? r.PlanKey }}</td>
                    <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
                      {{ formatDate(r.createdAt ?? r.CreatedAt) }}
                    </td>
                    <td class="px-4 py-3 text-right whitespace-nowrap">
                      <div class="inline-flex flex-wrap items-center justify-end gap-1">
                        <button
                          type="button"
                          class="inline-flex items-center justify-center rounded-lg p-2 text-gray-600 transition-colors hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700/50"
                          title="Panel de uso"
                          aria-label="Abrir panel de uso de la cuenta"
                          @click="openAccountSlide('usage', accountRowId(r))"
                        >
                          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                            />
                          </svg>
                        </button>
                        <button
                          type="button"
                          class="inline-flex items-center justify-center rounded-lg p-2 text-emerald-600 transition-colors hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-900/25"
                          title="Métricas de cuenta"
                          aria-label="Abrir métricas de la cuenta"
                          @click="openAccountSlide('metrics', accountRowId(r))"
                        >
                          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"
                            />
                          </svg>
                        </button>
                        <button
                          type="button"
                          class="inline-flex items-center justify-center rounded-lg p-2 text-sky-600 transition-colors hover:bg-sky-50 dark:text-sky-400 dark:hover:bg-sky-900/25"
                          title="Historial de consumo"
                          aria-label="Abrir historial de consumo de la cuenta"
                          @click="openAccountSlide('consumption', accountRowId(r))"
                        >
                          <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                            />
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div
              v-if="accountsTotalPages > 1"
              class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex flex-wrap items-center justify-between gap-2"
            >
              <p class="text-sm text-gray-600 dark:text-gray-400">
                Mostrando {{ accountsRangeStart }}–{{ accountsRangeEnd }} de {{ accountsTotalCount }} cuentas
              </p>
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  :disabled="accountsPage <= 1 || loading"
                  class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600"
                  @click="goToAccountsPage(accountsPage - 1)"
                >
                  Anterior
                </button>
                <span class="text-sm text-gray-600 dark:text-gray-400"> Página {{ accountsPage }} de {{ accountsTotalPages }} </span>
                <button
                  type="button"
                  :disabled="accountsPage >= accountsTotalPages || loading"
                  class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600"
                  @click="goToAccountsPage(accountsPage + 1)"
                >
                  Siguiente
                </button>
              </div>
            </div>
          </div>
        </section>

        <section>
          <div class="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Invitaciones</h2>
            <button
              type="button"
              class="btn-secondary shrink-0 self-start text-sm"
              :disabled="invitationsTotalCount === 0 || csvBusy"
              @click="downloadInvitationsCsv"
            >
              Descargar CSV
            </button>
          </div>
          <p class="mt-1 text-sm text-gray-600 dark:text-gray-400 mb-3">
            Pendientes: reenvía el correo, copia el enlace (solo en entornos no productivos) o revoca. Sin correo en
            archivo no se puede reenviar por email.
          </p>
          <div
            v-if="invitationsTotalCount === 0"
            class="text-center py-10 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-sm"
          >
            <p class="text-gray-600 dark:text-gray-300">No hay invitaciones registradas.</p>
          </div>
          <div
            v-else-if="!filteredInvitationRows.length"
            class="text-center py-10 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-sm"
          >
            <p class="text-gray-600 dark:text-gray-300">Ninguna invitación coincide con la búsqueda.</p>
          </div>
          <div
            v-else
            class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden"
          >
            <div class="overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-900/50">
                  <tr>
                    <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                      Creada
                    </th>
                  <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    Correo
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    Estado
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    Caduca
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    Completada
                  </th>
                  <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    ID
                  </th>
                  <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="inv in filteredInvitationRows" :key="reservationId(inv)" class="hover:bg-gray-50 dark:hover:bg-gray-700/50">
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
                    {{ formatDate(inv.createdAt ?? inv.CreatedAt) }}
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-800 dark:text-gray-200 break-all">
                    {{ reservationEmail(inv) || '—' }}
                  </td>
                  <td class="px-4 py-3 text-sm whitespace-nowrap">
                    <span
                      class="inline-flex rounded-full px-2 py-0.5 text-xs font-medium"
                      :class="reservationStatusBadgeClass(inv)"
                    >
                      {{ reservationStatusLabel(inv) }}
                    </span>
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
                    {{ formatDate(inv.expiresAt ?? inv.ExpiresAt) }}
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
                    {{ formatDate(inv.consumedAt ?? inv.ConsumedAt) }}
                  </td>
                  <td class="px-4 py-3 text-xs font-mono text-gray-500 dark:text-gray-400 break-all max-w-[140px]">
                    {{ reservationId(inv) }}
                  </td>
                  <td class="px-4 py-3 text-right whitespace-nowrap">
                    <div class="inline-flex flex-wrap items-center justify-end gap-1">
                      <button
                        type="button"
                        class="inline-flex items-center justify-center rounded-lg p-2 text-amber-600 transition-colors hover:bg-amber-50 disabled:opacity-40 dark:text-amber-400 dark:hover:bg-amber-900/25"
                        :disabled="!canResendInvitationEmail(inv) || busyReservationId === reservationId(inv)"
                        title="Reenviar correo"
                        aria-label="Reenviar correo de invitación"
                        @click="resendInvitationEmail(inv)"
                      >
                        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="2"
                            d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                          />
                        </svg>
                      </button>
                      <button
                        type="button"
                        class="inline-flex items-center justify-center rounded-lg p-2 text-sky-600 transition-colors hover:bg-sky-50 disabled:opacity-40 dark:text-sky-400 dark:hover:bg-sky-900/25"
                        :disabled="!canCopyInvitationLink(inv) || busyReservationId === reservationId(inv)"
                        title="Copiar enlace de alta"
                        aria-label="Copiar enlace de invitación"
                        @click="copyInvitationLink(inv)"
                      >
                        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="2"
                            d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                          />
                        </svg>
                      </button>
                      <button
                        type="button"
                        class="inline-flex items-center justify-center rounded-lg p-2 text-red-600 transition-colors hover:bg-red-50 disabled:opacity-40 dark:text-red-400 dark:hover:bg-red-900/25"
                        :disabled="!canRevokeInvitation(inv) || busyReservationId === reservationId(inv)"
                        title="Revocar invitación"
                        aria-label="Revocar invitación pendiente"
                        @click="revokeInvitation(inv)"
                      >
                        <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                          <path
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="2"
                            d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                          />
                        </svg>
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
            </div>
            <div
              v-if="invitationsTotalPages > 1"
              class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex flex-wrap items-center justify-between gap-2"
            >
              <p class="text-sm text-gray-600 dark:text-gray-400">
                Mostrando {{ invitationsRangeStart }}–{{ invitationsRangeEnd }} de {{ invitationsTotalCount }} invitaciones
              </p>
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  :disabled="invitationsPage <= 1 || loading"
                  class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600"
                  @click="goToInvitationsPage(invitationsPage - 1)"
                >
                  Anterior
                </button>
                <span class="text-sm text-gray-600 dark:text-gray-400">
                  Página {{ invitationsPage }} de {{ invitationsTotalPages }}
                </span>
                <button
                  type="button"
                  :disabled="invitationsPage >= invitationsTotalPages || loading"
                  class="px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-700 dark:text-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-600"
                  @click="goToInvitationsPage(invitationsPage + 1)"
                >
                  Siguiente
                </button>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>

    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="inviteOpen"
        class="fixed inset-0 z-[52] bg-black/50"
        aria-hidden="true"
        @click.self="closeInvitePanel"
      />
    </Transition>
    <Transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="inviteOpen"
        class="fixed top-0 right-0 z-[53] flex h-full w-full max-w-lg flex-col border-l border-gray-200 bg-white shadow-xl dark:border-gray-700 dark:bg-gray-800"
        role="dialog"
        aria-modal="true"
        aria-labelledby="partner-staff-invite-title"
        @click.stop
      >
        <div class="flex shrink-0 items-start justify-between gap-3 border-b border-gray-200 p-4 dark:border-gray-700 sm:p-5">
          <h2 id="partner-staff-invite-title" class="text-lg font-semibold text-gray-900 dark:text-white">
            Invitación de cuenta programa
          </h2>
          <button
            type="button"
            class="-m-2 shrink-0 rounded-lg p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
            aria-label="Cerrar"
            :disabled="inviteReserving"
            @click="closeInvitePanel"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div class="min-h-0 flex-1 space-y-4 overflow-y-auto p-4 sm:p-5">
          <p class="text-xs text-gray-600 dark:text-gray-400">
            Indica el correo del cliente: recibirá el enlace por email y solo podrá registrarse con ese mismo correo.
            En producción el enlace no se muestra en pantalla.
          </p>
          <div>
            <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Correo del cliente</label>
            <input
              v-model="inviteEmail"
              type="email"
              autocomplete="email"
              class="input-field w-full"
              placeholder="cliente@empresa.com"
            />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Expira (opcional)</label>
            <input v-model="inviteExpiresLocal" type="datetime-local" class="input-field w-full" />
          </div>
          <div>
            <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Nota interna (opcional)</label>
            <textarea
              v-model="inviteNote"
              rows="2"
              class="input-field w-full resize-y text-sm"
              placeholder="Solo para tu referencia (se guarda en metadatos)"
            />
          </div>
          <button type="button" class="btn-primary w-full sm:w-auto" :disabled="inviteReserving" @click="submitInvite">
            {{ inviteReserving ? 'Enviando…' : 'Enviar invitación' }}
          </button>
          <p v-if="inviteError" class="text-sm text-red-600 dark:text-red-400">{{ inviteError }}</p>
          <div
            v-if="lastInvite"
            class="rounded-lg border border-gray-200 bg-gray-50 p-3 text-xs dark:border-gray-600 dark:bg-gray-900/50"
          >
            <p class="mb-1 text-gray-500 dark:text-gray-400">ID de cuenta reservada</p>
            <p class="break-all font-mono text-gray-900 dark:text-gray-100">{{ lastInvite.reservedAccountId }}</p>
            <template v-if="lastInvite.startUrl">
              <p class="mb-1 mt-2 text-gray-500 dark:text-gray-400">Enlace (solo entornos no productivos)</p>
              <div class="flex flex-col gap-2 sm:flex-row sm:items-stretch">
                <input :value="lastInvite.startUrl" readonly class="input-field min-w-0 flex-1 font-mono text-[11px]" />
                <button
                  type="button"
                  class="inline-flex shrink-0 items-center justify-center rounded-lg border border-gray-200 bg-white p-2.5 text-gray-700 shadow-sm transition-colors hover:bg-gray-50 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-200 dark:hover:bg-gray-700"
                  title="Copiar enlace"
                  aria-label="Copiar enlace al portapapeles"
                  @click="copyInviteUrl"
                >
                  <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                    />
                  </svg>
                </button>
              </div>
            </template>
            <p v-else class="mt-2 text-gray-600 dark:text-gray-400">
              El enlace se envió al correo indicado (no se muestra aquí en producción).
            </p>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Panel de uso / métricas / consumo por cuenta (slide-over) -->
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="accountSlide"
        class="fixed inset-0 z-[60] bg-black/50"
        aria-hidden="true"
        @click.self="closeAccountSlide"
      />
    </Transition>
    <Transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="accountSlide"
        class="fixed top-0 right-0 z-[61] flex h-full w-full max-w-2xl flex-col border-l border-gray-200 bg-white shadow-xl dark:border-gray-700 dark:bg-gray-800"
        role="dialog"
        aria-modal="true"
        aria-labelledby="partner-account-slide-title"
        @click.stop
      >
        <div class="flex shrink-0 items-start justify-between gap-3 border-b border-gray-200 p-4 dark:border-gray-700 sm:p-5">
          <div class="min-w-0 pr-2">
            <h2 id="partner-account-slide-title" class="text-lg font-semibold text-gray-900 dark:text-white">
              {{ accountSlideTitle }}
            </h2>
            <p class="mt-1 break-all font-mono text-xs text-gray-500 dark:text-gray-400">{{ accountSlide.accountId }}</p>
          </div>
          <button
            type="button"
            class="-m-2 shrink-0 rounded-lg p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
            aria-label="Cerrar"
            @click="closeAccountSlide"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        <div class="min-h-0 flex-1 overflow-y-auto p-4 sm:p-5">
          <AdminUsage
            v-if="accountSlide.type === 'usage'"
            :key="'usage-' + accountSlide.accountId"
            :embedded-view-account-id="accountSlide.accountId"
          />
          <PartnerAccountMetrics
            v-else-if="accountSlide.type === 'metrics'"
            :key="'metrics-' + accountSlide.accountId"
            embedded
            :account-id-prop="accountSlide.accountId"
          />
          <ConsumptionHistory
            v-else-if="accountSlide.type === 'consumption'"
            :key="'consumption-' + accountSlide.accountId"
            embedded
            :account-id-prop="accountSlide.accountId"
          />
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { useAdminPaths } from '../../composables/useAdminPaths'
import AdminUsage from '../admin/AdminUsage.vue'
import PartnerAccountMetrics from './PartnerAccountMetrics.vue'
import ConsumptionHistory from '../admin/ConsumptionHistory.vue'

const PAGE_SIZE = 20

const toast = useToast()
const { toPath } = useAdminPaths()
const loading = ref(true)
const error = ref(null)
const rows = ref([])
const invitationRows = ref([])
const accountsTotalCount = ref(0)
const invitationsTotalCount = ref(0)
const accountsPage = ref(1)
const invitationsPage = ref(1)
const csvBusy = ref(false)
const busyReservationId = ref(null)
const searchQuery = ref('')

const accountsTotalPages = computed(() =>
  Math.max(1, Math.ceil(accountsTotalCount.value / PAGE_SIZE))
)
const invitationsTotalPages = computed(() =>
  Math.max(1, Math.ceil(invitationsTotalCount.value / PAGE_SIZE))
)
const accountsRangeStart = computed(() =>
  (accountsPage.value - 1) * PAGE_SIZE + (rows.value.length ? 1 : 0)
)
const accountsRangeEnd = computed(() => (accountsPage.value - 1) * PAGE_SIZE + rows.value.length)
const invitationsRangeStart = computed(() =>
  (invitationsPage.value - 1) * PAGE_SIZE + (invitationRows.value.length ? 1 : 0)
)
const invitationsRangeEnd = computed(
  () => (invitationsPage.value - 1) * PAGE_SIZE + invitationRows.value.length
)

/** @type {import('vue').Ref<{ type: 'usage' | 'metrics' | 'consumption'; accountId: string } | null>} */
const accountSlide = ref(null)

const accountSlideTitle = computed(() => {
  const s = accountSlide.value
  if (!s) return ''
  if (s.type === 'usage') return 'Panel de uso'
  if (s.type === 'metrics') return 'Métricas de cuenta'
  if (s.type === 'consumption') return 'Historial de consumo'
  return ''
})

function openAccountSlide(type, accountId) {
  const id = String(accountId || '').trim()
  if (!id) return
  inviteOpen.value = false
  accountSlide.value = { type, accountId: id }
}

function closeAccountSlide() {
  accountSlide.value = null
}

const inviteOpen = ref(false)
const inviteEmail = ref('')
const inviteExpiresLocal = ref('')
const inviteNote = ref('')
const inviteReserving = ref(false)
const inviteError = ref(null)
const lastInvite = ref(null)

function formatDate(d) {
  if (!d) return '—'
  const date = typeof d === 'string' ? new Date(d) : d
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleString('es-ES', { dateStyle: 'short', timeStyle: 'short' })
}

function accountRowId(r) {
  return r.id ?? r.Id ?? ''
}

function accountOwnerEmail(r) {
  const e = r.ownerEmailNormalized ?? r.OwnerEmailNormalized
  return typeof e === 'string' ? e : ''
}

const filteredAccountRows = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  if (!q) return rows.value
  return rows.value.filter((r) => {
    const id = String(accountRowId(r)).toLowerCase()
    const plan = String(r.planKey ?? r.PlanKey ?? '').toLowerCase()
    const email = accountOwnerEmail(r).toLowerCase()
    return id.includes(q) || plan.includes(q) || email.includes(q)
  })
})

const filteredInvitationRows = computed(() => {
  const q = searchQuery.value.trim().toLowerCase()
  if (!q) return invitationRows.value
  return invitationRows.value.filter((inv) => {
    const id = String(reservationId(inv)).toLowerCase()
    const email = reservationEmail(inv).toLowerCase()
    const status = reservationStatus(inv).toLowerCase()
    const label = reservationStatusLabel(inv).toLowerCase()
    return id.includes(q) || email.includes(q) || status.includes(q) || label.includes(q)
  })
})

function reservationId(inv) {
  return inv.id ?? inv.Id ?? ''
}

function reservationEmail(inv) {
  const e = inv.invitedEmailNormalized ?? inv.InvitedEmailNormalized
  return typeof e === 'string' ? e : ''
}

function reservationStatus(inv) {
  return String(inv.status ?? inv.Status ?? '').trim()
}

function reservationStatusLabel(inv) {
  const s = reservationStatus(inv).toLowerCase()
  if (s === 'pending') return 'Pendiente'
  if (s === 'consumed') return 'Completada'
  if (s === 'revoked') return 'Revocada'
  return reservationStatus(inv) || '—'
}

function reservationStatusBadgeClass(inv) {
  const s = reservationStatus(inv).toLowerCase()
  if (s === 'pending') {
    return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-200'
  }
  if (s === 'consumed') {
    return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200'
  }
  if (s === 'revoked') {
    return 'bg-gray-200 text-gray-700 dark:bg-gray-600 dark:text-gray-200'
  }
  return 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300'
}

function isPendingReservation(inv) {
  return reservationStatus(inv).toLowerCase() === 'pending'
}

function isReservationExpired(inv) {
  const exp = inv.expiresAt ?? inv.ExpiresAt
  if (!exp) return false
  const t = new Date(exp).getTime()
  return !Number.isNaN(t) && t <= Date.now()
}

function canResendInvitationEmail(inv) {
  return isPendingReservation(inv) && !isReservationExpired(inv) && reservationEmail(inv).trim().length > 0
}

function canCopyInvitationLink(inv) {
  return isPendingReservation(inv) && !isReservationExpired(inv)
}

function canRevokeInvitation(inv) {
  return isPendingReservation(inv)
}

function goToAccountsPage(p) {
  const next = Number(p)
  if (!Number.isFinite(next) || next < 1 || next > accountsTotalPages.value) return
  accountsPage.value = next
  load()
}

function goToInvitationsPage(p) {
  const next = Number(p)
  if (!Number.isFinite(next) || next < 1 || next > invitationsTotalPages.value) return
  invitationsPage.value = next
  load()
}

async function load() {
  loading.value = true
  error.value = null
  try {
    for (;;) {
      const [accData, resData] = await Promise.all([
        apiService.getPartnerProgramAccounts({ page: accountsPage.value, pageSize: PAGE_SIZE }),
        apiService.getPartnerProgramInvitations({ page: invitationsPage.value, pageSize: PAGE_SIZE })
      ])
      const accItems = accData?.items ?? accData?.Items ?? []
      rows.value = Array.isArray(accItems) ? accItems : []
      accountsTotalCount.value = Number(accData?.totalCount ?? accData?.TotalCount ?? 0)
      const resItems = resData?.items ?? resData?.Items ?? []
      invitationRows.value = Array.isArray(resItems) ? resItems : []
      invitationsTotalCount.value = Number(resData?.totalCount ?? resData?.TotalCount ?? 0)

      const maxAcc = Math.max(1, Math.ceil(accountsTotalCount.value / PAGE_SIZE))
      const maxInv = Math.max(1, Math.ceil(invitationsTotalCount.value / PAGE_SIZE))
      let changed = false
      if (accountsPage.value > maxAcc) {
        accountsPage.value = maxAcc
        changed = true
      }
      if (invitationsPage.value > maxInv) {
        invitationsPage.value = maxInv
        changed = true
      }
      if (!changed) break
    }
  } catch (err) {
    error.value = err.response?.data?.error || err.message || 'Error al cargar datos'
    rows.value = []
    invitationRows.value = []
    accountsTotalCount.value = 0
    invitationsTotalCount.value = 0
  } finally {
    loading.value = false
  }
}

async function fetchAllProgramAccounts() {
  const all = []
  let page = 1
  for (;;) {
    const data = await apiService.getPartnerProgramAccounts({ page, pageSize: PAGE_SIZE })
    const items = data?.items ?? data?.Items ?? []
    if (!Array.isArray(items) || items.length === 0) break
    all.push(...items)
    const total = Number(data?.totalCount ?? data?.TotalCount ?? 0)
    if (all.length >= total) break
    page += 1
  }
  return all
}

async function fetchAllProgramInvitations() {
  const all = []
  let page = 1
  for (;;) {
    const data = await apiService.getPartnerProgramInvitations({ page, pageSize: PAGE_SIZE })
    const items = data?.items ?? data?.Items ?? []
    if (!Array.isArray(items) || items.length === 0) break
    all.push(...items)
    const total = Number(data?.totalCount ?? data?.TotalCount ?? 0)
    if (all.length >= total) break
    page += 1
  }
  return all
}

async function resendInvitationEmail(inv) {
  const id = reservationId(inv)
  if (!id || !canResendInvitationEmail(inv)) return
  busyReservationId.value = id
  try {
    const dto = await apiService.partnerStaffResendInvitationEmail(id)
    const emailSent = dto.emailSent ?? dto.EmailSent
    const startUrl = dto.startUrl ?? dto.StartUrl
    if (emailSent) {
      toast.success('Correo de invitación enviado (o encolado).')
    } else {
      toast.warning('No se pudo confirmar el envío del correo; revisa los logs o inténtalo de nuevo.')
    }
    if (startUrl) {
      try {
        await navigator.clipboard.writeText(startUrl)
        toast.info('Enlace de prueba copiado al portapapeles.')
      } catch {
        /* ignore */
      }
    }
    await load()
  } catch (err) {
    const d = err.response?.data
    const msg = d?.detail || d?.title || d?.error || err.message || 'Error al reenviar'
    toast.error(msg)
  } finally {
    busyReservationId.value = null
  }
}

async function copyInvitationLink(inv) {
  const id = reservationId(inv)
  if (!id || !canCopyInvitationLink(inv)) return
  busyReservationId.value = id
  try {
    const dto = await apiService.partnerStaffGetInvitationLink(id)
    const url = dto.startUrl ?? dto.StartUrl
    if (!url) {
      toast.info('En producción el enlace solo se envía por correo; usa «Reenviar correo».')
      return
    }
    try {
      await navigator.clipboard.writeText(url)
      toast.success('Enlace copiado al portapapeles.')
    } catch {
      toast.error('No se pudo copiar al portapapeles.')
    }
  } catch (err) {
    const d = err.response?.data
    const msg = d?.detail || d?.title || d?.error || err.message || 'Error al obtener el enlace'
    toast.error(msg)
  } finally {
    busyReservationId.value = null
  }
}

async function revokeInvitation(inv) {
  const id = reservationId(inv)
  if (!id || !canRevokeInvitation(inv)) return
  if (!window.confirm('¿Revocar esta invitación? El enlace dejará de ser válido.')) return
  busyReservationId.value = id
  try {
    await apiService.partnerStaffRevokeInvitation(id)
    toast.success('Invitación revocada.')
    await load()
  } catch (err) {
    const d = err.response?.data
    const msg = d?.detail || d?.title || d?.error || err.message || 'Error al revocar'
    toast.error(msg)
  } finally {
    busyReservationId.value = null
  }
}

function openInvitePanel() {
  accountSlide.value = null
  inviteError.value = null
  lastInvite.value = null
  inviteEmail.value = ''
  inviteExpiresLocal.value = ''
  inviteNote.value = ''
  inviteOpen.value = true
}

function closeInvitePanel() {
  if (inviteReserving.value) return
  inviteOpen.value = false
}

async function submitInvite() {
  inviteError.value = null
  const email = inviteEmail.value.trim()
  if (!email) {
    inviteError.value = 'Indica el correo del cliente.'
    toast.error(inviteError.value)
    return
  }
  inviteReserving.value = true
  try {
    let expiresAt = null
    if (inviteExpiresLocal.value) {
      const dt = new Date(inviteExpiresLocal.value)
      if (!Number.isNaN(dt.getTime())) expiresAt = dt.toISOString()
    }
    const note = inviteNote.value.trim()
    const body = {
      email,
      expiresAt,
      metadataJson: note ? JSON.stringify({ partnerNote: note }) : null
    }
    const dto = await apiService.partnerStaffCreateReservation(body)
    const startUrl = dto.startUrl ?? dto.StartUrl ?? null
    lastInvite.value = {
      reservedAccountId: dto.reservedAccountId ?? dto.ReservedAccountId,
      startUrl,
      startToken: dto.startToken ?? dto.StartToken
    }
    if (startUrl) {
      try {
        await navigator.clipboard.writeText(startUrl)
        toast.success('Invitación creada. Enlace de prueba copiado al portapapeles; en producción solo se envía por correo.')
      } catch {
        toast.success('Invitación creada. Copia el enlace de prueba desde el panel si lo necesitas.')
      }
    } else {
      toast.success('Invitación creada. El cliente recibirá el enlace por correo.')
    }
    await load()
  } catch (err) {
    const d = err.response?.data
    inviteError.value = d?.detail || d?.title || d?.error || err.message || 'Error al crear la invitación'
    toast.error(inviteError.value)
  } finally {
    inviteReserving.value = false
  }
}

async function copyInviteUrl() {
  const url = lastInvite.value?.startUrl
  if (!url) return
  try {
    await navigator.clipboard.writeText(url)
    toast.success('Enlace copiado.')
  } catch {
    toast.error('No se pudo copiar al portapapeles.')
  }
}

function escapeCsvCell(value) {
  if (value == null || value === undefined) return ''
  const s = String(value)
  if (/[",\n\r]/.test(s)) return `"${s.replace(/"/g, '""')}"`
  return s
}

function csvLine(cells) {
  return cells.map(escapeCsvCell).join(',')
}

function csvIsoDate(value) {
  if (value == null || value === '') return ''
  const dt = new Date(value)
  if (Number.isNaN(dt.getTime())) return ''
  return dt.toISOString()
}

function exportCsvFile(filenameBase, headerRow, dataRows) {
  const lines = [csvLine(headerRow), ...dataRows.map((row) => csvLine(row))]
  const content = lines.join('\r\n')
  const bom = '\uFEFF'
  const blob = new Blob([bom + content], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${filenameBase}-${new Date().toISOString().slice(0, 10)}.csv`
  a.rel = 'noopener'
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

async function downloadAccountsCsv() {
  if (accountsTotalCount.value === 0 || csvBusy.value) return
  csvBusy.value = true
  try {
    const allRows = await fetchAllProgramAccounts()
    const header = [
      'accountId',
      'ownerEmailNormalized',
      'planKey',
      'createdAt',
      'trialExpiresAt'
    ]
    const dataRows = allRows.map((r) => [
      accountRowId(r),
      accountOwnerEmail(r),
      r.planKey ?? r.PlanKey ?? '',
      csvIsoDate(r.createdAt ?? r.CreatedAt),
      csvIsoDate(r.trialExpiresAt ?? r.TrialExpiresAt)
    ])
    exportCsvFile('programa-cuentas', header, dataRows)
    toast.success('CSV de cuentas descargado.')
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'Error al generar el CSV')
  } finally {
    csvBusy.value = false
  }
}

async function downloadInvitationsCsv() {
  if (invitationsTotalCount.value === 0 || csvBusy.value) return
  csvBusy.value = true
  try {
    const allInv = await fetchAllProgramInvitations()
    const header = [
      'reservationId',
      'kind',
      'invitedEmailNormalized',
      'status',
      'expiresAt',
      'consumedAt',
      'createdAt'
    ]
    const dataRows = allInv.map((inv) => [
      reservationId(inv),
      inv.kind ?? inv.Kind ?? '',
      reservationEmail(inv),
      reservationStatus(inv),
      csvIsoDate(inv.expiresAt ?? inv.ExpiresAt),
      csvIsoDate(inv.consumedAt ?? inv.ConsumedAt),
      csvIsoDate(inv.createdAt ?? inv.CreatedAt)
    ])
    exportCsvFile('programa-invitaciones', header, dataRows)
    toast.success('CSV de invitaciones descargado.')
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'Error al generar el CSV')
  } finally {
    csvBusy.value = false
  }
}

onMounted(load)
</script>

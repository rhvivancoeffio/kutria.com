<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Partners</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Listado de organizaciones partner, reservas de alta y staff. Solo SuperAdmin.
          </p>
        </div>
        <button
          v-if="!forbidden && !listError"
          type="button"
          class="btn-primary shrink-0"
          @click="openCreateSlider"
        >
          Nuevo partner
        </button>
      </div>

      <div v-if="loading && !partners.length" class="space-y-3 sm:space-y-4">
        <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <div
        v-else-if="forbidden"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-amber-800 dark:text-amber-300">Solo los SuperAdmin pueden acceder a esta página.</p>
      </div>

      <div
        v-else-if="listError"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ listError }}</p>
      </div>

      <template v-else>
        <!-- Móvil: tarjetas -->
        <div class="md:hidden space-y-3">
          <div
            v-for="p in partners"
            :key="p.id"
            class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
          >
            <p class="font-medium text-gray-900 dark:text-white truncate">{{ p.name }}</p>
            <p class="text-xs font-mono text-gray-500 dark:text-gray-400 truncate mt-0.5">{{ p.space }}</p>
            <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">{{ formatDate(p.createdAt) }}</p>
            <p class="mt-2">
              <span
                class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                :class="operatorAccountActive(p) ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300'"
              >
                {{ operatorAccountStatusLabel(p) }}
              </span>
            </p>
            <div class="mt-3 flex flex-wrap items-center gap-2">
              <button
                type="button"
                class="inline-flex items-center justify-center rounded-lg border border-gray-200 bg-white p-2.5 text-primary-600 shadow-sm transition-colors hover:bg-primary-50 dark:border-gray-600 dark:bg-gray-800 dark:text-primary-400 dark:hover:bg-primary-900/25"
                title="Gestionar"
                aria-label="Gestionar partner"
                @click="openManageSlider(p.id)"
              >
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                  />
                </svg>
              </button>
              <button
                type="button"
                class="inline-flex items-center justify-center rounded-lg border border-gray-200 bg-white p-2.5 text-emerald-600 shadow-sm transition-colors hover:bg-emerald-50 dark:border-gray-600 dark:bg-gray-800 dark:text-emerald-400 dark:hover:bg-emerald-900/25"
                title="Reserva de cuenta (enlace de alta)"
                aria-label="Reserva de cuenta, enlace de alta"
                @click="openReservationSlider(p)"
              >
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                  />
                </svg>
              </button>
              <button
                type="button"
                class="inline-flex items-center justify-center rounded-lg border border-red-200 bg-white p-2.5 text-red-600 shadow-sm transition-colors hover:bg-red-50 dark:border-red-900/50 dark:bg-gray-800 dark:text-red-400 dark:hover:bg-red-900/20"
                title="Eliminar"
                aria-label="Eliminar partner"
                @click="deleteTarget = { id: p.id, name: p.name }"
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
          </div>
          <div
            v-if="!partners.length"
            class="text-center py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-sm"
          >
            No hay partners. Crea uno con «Nuevo partner».
          </div>
        </div>

        <!-- Desktop: tabla -->
        <div
          v-if="partners.length"
          class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-x-auto"
        >
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-900/50">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  Nombre
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  Space
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  Creado
                </th>
                <th class="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  Cuenta operador
                </th>
                <th class="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                  Acciones
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="p in partners"
                :key="p.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/50"
              >
                <td class="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white truncate max-w-[200px]">
                  {{ p.name }}
                </td>
                <td class="px-4 py-3 text-sm font-mono text-gray-600 dark:text-gray-300">{{ p.space }}</td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-300 whitespace-nowrap">
                  {{ formatDate(p.createdAt) }}
                </td>
                <td class="px-4 py-3 text-sm whitespace-nowrap">
                  <span
                    class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                    :class="operatorAccountActive(p) ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-200' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300'"
                  >
                    {{ operatorAccountStatusLabel(p) }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right whitespace-nowrap">
                  <div class="inline-flex items-center justify-end gap-1">
                    <button
                      type="button"
                      class="inline-flex items-center justify-center rounded-lg p-2 text-primary-600 transition-colors hover:bg-primary-50 dark:text-primary-400 dark:hover:bg-primary-900/25"
                      title="Gestionar"
                      aria-label="Gestionar partner"
                      @click="openManageSlider(p.id)"
                    >
                      <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                        />
                      </svg>
                    </button>
                    <button
                      type="button"
                      class="inline-flex items-center justify-center rounded-lg p-2 text-emerald-600 transition-colors hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-900/25"
                      title="Reserva de cuenta (enlace de alta)"
                      aria-label="Reserva de cuenta, enlace de alta"
                      @click="openReservationSlider(p)"
                    >
                      <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                        />
                      </svg>
                    </button>
                    <button
                      type="button"
                      class="inline-flex items-center justify-center rounded-lg p-2 text-red-600 transition-colors hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/20"
                      title="Eliminar"
                      aria-label="Eliminar partner"
                      @click="deleteTarget = { id: p.id, name: p.name }"
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
          v-else
          class="hidden md:block text-center py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-sm"
        >
          No hay partners. Crea uno con «Nuevo partner».
        </div>
      </template>
    </main>

    <!-- Crear partner (slider) -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="createSliderOpen"
          class="fixed inset-0 z-50 bg-black/50"
          aria-hidden="true"
          @click.self="closeCreateSlider"
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
          v-if="createSliderOpen"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          role="dialog"
          aria-modal="true"
          aria-labelledby="partner-create-title"
          @click.stop
        >
          <div class="p-4 sm:p-5 border-b border-gray-200 dark:border-gray-700 shrink-0 flex items-start justify-between gap-3">
            <h2 id="partner-create-title" class="text-lg font-semibold text-gray-900 dark:text-white">
              Nuevo partner
            </h2>
            <button
              type="button"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg"
              aria-label="Cerrar"
              :disabled="creating"
              @click="closeCreateSlider"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-5 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Space (slug) <span class="text-red-500">*</span>
                <span class="text-gray-400 font-normal">· máx. 30, solo letras y números</span>
              </label>
              <input
                v-model="createForm.space"
                type="text"
                maxlength="30"
                class="input-field w-full transition-[box-shadow,border-color] duration-150"
                :class="spaceInputClass"
                placeholder="mipartner"
                autocomplete="off"
                @input="onSpaceInput"
                @blur="validateSpaceOnBlur"
              />
              <p v-if="spaceFieldInvalid && spaceFieldMessage" class="mt-1 text-xs text-red-600 dark:text-red-400">
                {{ spaceFieldMessage }}
              </p>
              <p v-else-if="spaceFieldValid && spaceSuccessMessage" class="mt-1 text-xs text-emerald-600 dark:text-emerald-400">
                {{ spaceSuccessMessage }}
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Nombre <span class="text-red-500">*</span>
              </label>
              <input
                v-model="createForm.name"
                type="text"
                class="input-field w-full transition-[box-shadow,border-color] duration-150"
                :class="createNameBlurInvalid ? '!border-red-500 dark:!border-red-500 ring-1 ring-red-500' : ''"
                placeholder="Nombre visible"
                @input="onCreateNameInput"
                @blur="onCreateNameBlur"
              />
              <p v-if="createNameBlurInvalid" class="mt-1 text-xs text-red-600 dark:text-red-400">
                Este campo es obligatorio.
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                % comisión por nueva venta
              </label>
              <input
                v-model.number="createForm.newSalePercent"
                type="number"
                min="0"
                max="100"
                step="0.01"
                class="input-field w-full"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                % comisión cliente activo
              </label>
              <input
                v-model.number="createForm.activeClientPercent"
                type="number"
                min="0"
                max="100"
                step="0.01"
                class="input-field w-full"
              />
            </div>

            <section class="space-y-2 border-t border-gray-200 pt-4 dark:border-gray-700">
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Operador del partner</h3>
              <p class="text-xs text-gray-600 dark:text-gray-400">
                Email de quien abrirá el enlace y se dará de alta; debe coincidir con el correo del registro. Se envía un
                correo con el enlace. En entornos que no son producción la API también devuelve el enlace aquí para
                copiarlo.
              </p>
              <div>
                <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                  Email del operador <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="createForm.operatorInviteEmail"
                  type="email"
                  class="input-field w-full"
                  placeholder="correo@empresa.com"
                  autocomplete="off"
                />
              </div>
            </section>

            <p v-if="createError" class="text-sm text-red-600 dark:text-red-400">{{ createError }}</p>
          </div>
          <div class="p-4 sm:p-5 border-t border-gray-200 dark:border-gray-700 shrink-0 flex justify-end gap-2">
            <button type="button" class="btn-secondary" :disabled="creating" @click="closeCreateSlider">Cancelar</button>
            <button
              type="button"
              class="btn-primary"
              :disabled="
                creating ||
                !createForm.space.trim() ||
                !createForm.name.trim() ||
                !createForm.operatorInviteEmail?.trim() ||
                spaceFieldInvalid ||
                createNameBlurInvalid
              "
              @click="submitCreate"
            >
              {{ creating ? 'Creando…' : 'Crear' }}
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Gestionar partner (slider) -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="manageSliderOpen"
          class="fixed inset-0 z-50 bg-black/50"
          aria-hidden="true"
          @click.self="closeManageSlider"
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
          v-if="manageSliderOpen"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          role="dialog"
          aria-modal="true"
          aria-labelledby="partner-manage-title"
          @click.stop
        >
          <div class="p-4 sm:p-5 border-b border-gray-200 dark:border-gray-700 shrink-0 flex items-start justify-between gap-3">
            <div class="min-w-0">
              <h2 id="partner-manage-title" class="text-lg font-semibold text-gray-900 dark:text-white truncate">
                {{ detail?.name || 'Partner' }}
              </h2>
              <p v-if="detail" class="text-xs font-mono text-gray-500 dark:text-gray-400 truncate mt-0.5">
                {{ detail.space }} · {{ detail.id }}
              </p>
            </div>
            <button
              type="button"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg shrink-0"
              aria-label="Cerrar"
              :disabled="saving || reserving || regeneratingOperatorInvite"
              @click="closeManageSlider"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-5 space-y-6">
            <div v-if="detailLoading" class="flex justify-center py-12">
              <div class="animate-spin rounded-full h-10 w-10 border-2 border-primary-600 border-t-transparent" />
            </div>
            <div
              v-else-if="detailError"
              class="p-3 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-sm text-red-800 dark:text-red-300"
            >
              {{ detailError }}
            </div>
            <template v-else-if="detail">
              <section class="space-y-4">
                <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Datos</h3>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre</label>
                  <input v-model="editForm.name" type="text" class="input-field w-full" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    % comisión por nueva venta
                  </label>
                  <input
                    v-model.number="editForm.newSalePercent"
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    class="input-field w-full"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    % comisión cliente activo
                  </label>
                  <input
                    v-model.number="editForm.activeClientPercent"
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    class="input-field w-full"
                  />
                </div>
                <p v-if="saveError" class="text-sm text-red-600 dark:text-red-400">{{ saveError }}</p>
                <button type="button" class="btn-primary w-full sm:w-auto" :disabled="saving" @click="submitSave">
                  {{ saving ? 'Guardando…' : 'Guardar cambios' }}
                </button>
              </section>

              <section class="space-y-3 border-t border-gray-200 pt-4 dark:border-gray-700">
                <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Reservas e invitaciones</h3>
                <p class="text-xs text-gray-600 dark:text-gray-400">
                  Enlaces de alta (cuenta programa) e invitaciones al operador. El email mostrado es el invitado
                  (normalizado). Generar un nuevo enlace de operador deja en «Revocada» la invitación pendiente anterior.
                  En producción el enlace solo va por correo; fuera de producción también podrás copiarlo desde la
                  respuesta al generar.
                </p>
                <div v-if="reservationsListLoading" class="flex justify-center py-6 text-sm text-gray-500">
                  Cargando historial…
                </div>
                <p v-else-if="reservationsListError" class="text-sm text-red-600 dark:text-red-400">
                  {{ reservationsListError }}
                </p>
                <div
                  v-else-if="partnerReservationsList.length"
                  class="max-h-56 overflow-y-auto rounded-lg border border-gray-200 dark:border-gray-600"
                >
                  <table class="min-w-full divide-y divide-gray-200 text-xs dark:divide-gray-600">
                    <thead class="sticky top-0 bg-gray-50 dark:bg-gray-900/80">
                      <tr>
                        <th class="px-2 py-2 text-left font-medium text-gray-600 dark:text-gray-400">Tipo</th>
                        <th class="px-2 py-2 text-left font-medium text-gray-600 dark:text-gray-400">Email invitado</th>
                        <th class="px-2 py-2 text-left font-medium text-gray-600 dark:text-gray-400">Estado</th>
                        <th class="px-2 py-2 text-left font-medium text-gray-600 dark:text-gray-400">Creado</th>
                        <th class="px-2 py-2 text-left font-medium text-gray-600 dark:text-gray-400">Consumido</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
                      <tr v-for="row in partnerReservationsList" :key="row.id ?? row.Id">
                        <td class="px-2 py-1.5 text-gray-900 dark:text-gray-100">
                          {{ reservationKindLabel(row.kind ?? row.Kind) }}
                        </td>
                        <td class="px-2 py-1.5 font-mono text-[11px] text-gray-700 dark:text-gray-300">
                          {{ formatInvitedEmail(row) }}
                        </td>
                        <td class="px-2 py-1.5 text-gray-700 dark:text-gray-300">
                          {{ reservationStatusLabel(row.status ?? row.Status) }}
                        </td>
                        <td class="whitespace-nowrap px-2 py-1.5 text-gray-600 dark:text-gray-400">
                          {{ formatDate(row.createdAt ?? row.CreatedAt) }}
                        </td>
                        <td class="whitespace-nowrap px-2 py-1.5 text-gray-600 dark:text-gray-400">
                          {{ formatDate(row.consumedAt ?? row.ConsumedAt) }}
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <p v-else class="text-xs text-gray-500 dark:text-gray-400">No hay reservas registradas para este partner.</p>

                <div class="space-y-2 border-t border-gray-100 pt-3 dark:border-gray-700">
                  <h4 class="text-xs font-semibold text-gray-800 dark:text-gray-200">Nuevo enlace de operador</h4>
                  <p class="text-xs text-gray-500 dark:text-gray-400">
                    Reenvía el correo con el enlace; en no-producción se intentará copiar el enlace en el portapapeles.
                  </p>
                  <input
                    v-model="operatorRegenerateEmail"
                    type="email"
                    class="input-field w-full"
                    placeholder="correo@empresa.com"
                    autocomplete="off"
                  />
                  <button
                    type="button"
                    class="btn-secondary w-full sm:w-auto"
                    :disabled="regeneratingOperatorInvite || !operatorRegenerateEmail.trim()"
                    @click="submitOperatorInvite"
                  >
                    {{ regeneratingOperatorInvite ? 'Generando…' : 'Generar enlace' }}
                  </button>
                </div>
              </section>
            </template>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Reserva de cuenta (segundo slider, encima del de gestionar) -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="reservationSliderOpen"
          class="fixed inset-0 z-[52] bg-black/50"
          aria-hidden="true"
          @click.self="closeReservationSlider"
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
          v-if="reservationSliderOpen"
          class="fixed top-0 right-0 z-[53] flex h-full w-full max-w-lg flex-col border-l border-gray-200 bg-white shadow-xl dark:border-gray-700 dark:bg-gray-800"
          role="dialog"
          aria-modal="true"
          aria-labelledby="partner-reservation-title"
          @click.stop
        >
          <div class="flex shrink-0 items-start justify-between gap-3 border-b border-gray-200 p-4 dark:border-gray-700 sm:p-5">
            <div class="min-w-0">
              <h2 id="partner-reservation-title" class="text-lg font-semibold text-gray-900 dark:text-white">
                Reserva de cuenta (enlace de alta)
              </h2>
              <p
                v-if="reservationHeader.name || reservationHeader.space"
                class="mt-0.5 truncate text-xs font-mono text-gray-500 dark:text-gray-400"
              >
                {{ reservationHeader.name }} · {{ reservationHeader.space }}
              </p>
            </div>
            <button
              type="button"
              class="-m-2 shrink-0 rounded-lg p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
              aria-label="Cerrar"
              :disabled="reserving"
              @click="closeReservationSlider"
            >
              <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="min-h-0 flex-1 space-y-4 overflow-y-auto p-4 sm:p-5">
            <p class="text-xs text-gray-600 dark:text-gray-400">
              El cliente abre la URL (validación en API y redirección al registro con el token en el enlace).
            </p>
            <div>
              <label class="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Expira (opcional)</label>
              <input v-model="reservationExpiresLocal" type="datetime-local" class="input-field w-full" />
            </div>
            <button type="button" class="btn-primary w-full sm:w-auto" :disabled="reserving" @click="submitReservation">
              {{ reserving ? 'Generando…' : 'Generar reserva' }}
            </button>
            <p v-if="reservationError" class="text-sm text-red-600 dark:text-red-400">{{ reservationError }}</p>
            <div
              v-if="lastReservation"
              class="rounded-lg border border-gray-200 bg-gray-50 p-3 text-xs dark:border-gray-600 dark:bg-gray-900/50"
            >
              <p class="mb-1 text-gray-500 dark:text-gray-400">AccountId reservado</p>
              <p class="break-all font-mono text-gray-900 dark:text-gray-100">{{ lastReservation.reservedAccountId }}</p>
              <p class="mb-1 mt-2 text-gray-500 dark:text-gray-400">Enlace</p>
              <div class="flex flex-col gap-2 sm:flex-row sm:items-stretch">
                <input
                  :value="lastReservation.startUrl"
                  readonly
                  class="input-field min-w-0 flex-1 font-mono text-[11px]"
                />
                <button
                  type="button"
                  class="inline-flex shrink-0 items-center justify-center rounded-lg border border-gray-200 bg-white p-2.5 text-gray-700 shadow-sm transition-colors hover:bg-gray-50 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-200 dark:hover:bg-gray-700"
                  title="Copiar enlace"
                  aria-label="Copiar enlace al portapapeles"
                  @click="copyStartUrl"
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
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Confirmar eliminación -->
    <Teleport to="body">
      <div
        v-if="deleteTarget"
        class="fixed inset-0 z-[60] flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
        @click.self="deleteTarget = null"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar partner</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Eliminar «{{ deleteTarget.name }}»? Esta acción no se puede deshacer.
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              @click="deleteTarget = null"
            >
              Cancelar
            </button>
            <button
              type="button"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] bg-red-600 hover:bg-red-700 text-white rounded-lg font-medium disabled:opacity-50"
              :disabled="deleting"
              @click="doDelete"
            >
              {{ deleting ? 'Eliminando…' : 'Eliminar' }}
            </button>
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

/** JSON en CommissionsJson para comisiones editadas desde SuperAdmin. */
const COMMISSION_KEYS = {
  newSale: 'newSaleCommissionPercent',
  activeClient: 'activeClientCommissionPercent'
}

const toast = useToast()

const loading = ref(true)
const forbidden = ref(false)
const listError = ref(null)
const partners = ref([])

const createSliderOpen = ref(false)
const createForm = ref({
  space: '',
  name: '',
  newSalePercent: 0,
  activeClientPercent: 0,
  operatorInviteEmail: ''
})
const creating = ref(false)
const createError = ref(null)
/** Estado del input space (crear): tras blur, rojo si falla o verde si está disponible. */
const spaceFieldInvalid = ref(false)
const spaceFieldValid = ref(false)
const spaceFieldMessage = ref('')
const spaceSuccessMessage = ref('')
/** Tras blur del nombre en crear: vacío → borde rojo. */
const createNameBlurInvalid = ref(false)

const SPACE_SLUG_PATTERN = /^[a-zA-Z0-9]{1,30}$/

const spaceInputClass = computed(() => {
  if (spaceFieldInvalid.value) {
    return '!border-red-500 dark:!border-red-500 ring-1 ring-red-500'
  }
  if (spaceFieldValid.value) {
    return '!border-emerald-500 dark:!border-emerald-500 ring-1 ring-emerald-500'
  }
  return ''
})

const manageSliderOpen = ref(false)
const managePartnerId = ref(null)
const detail = ref(null)
const detailLoading = ref(false)
const detailError = ref(null)
const editForm = ref({ name: '', newSalePercent: 0, activeClientPercent: 0 })
const saving = ref(false)
const saveError = ref(null)

const reservationExpiresLocal = ref('')
const reserving = ref(false)
const reservationError = ref(null)
const lastReservation = ref(null)
const reservationSliderOpen = ref(false)
const reservationPartnerId = ref(null)
const reservationHeader = ref({ name: '', space: '' })

const deleteTarget = ref(null)
const deleting = ref(false)

const partnerReservationsList = ref([])
const reservationsListLoading = ref(false)
const reservationsListError = ref(null)
const operatorRegenerateEmail = ref('')
const regeneratingOperatorInvite = ref(false)

function operatorAccountActive(p) {
  const v = p?.operatorAccountActive ?? p?.OperatorAccountActive
  return v === true
}

function operatorAccountStatusLabel(p) {
  return operatorAccountActive(p) ? 'Activa' : 'Pendiente'
}

function formatDate(d) {
  if (!d) return '—'
  const date = typeof d === 'string' ? new Date(d) : d
  if (Number.isNaN(date.getTime())) return '—'
  // toLocaleDateString no acepta timeStyle en varios motores; toLocaleString sí.
  return date.toLocaleString('es-ES', { dateStyle: 'short', timeStyle: 'short' })
}

function reservationKindLabel(kind) {
  const k = String(kind ?? '')
    .replace(/_/g, '')
    .toLowerCase()
  if (k === 'partneroperator') return 'Operador'
  return 'Cuenta programa'
}

function reservationStatusLabel(status) {
  const s = String(status ?? '').toLowerCase()
  if (s === 'pending') return 'Pendiente'
  if (s === 'consumed') return 'Usada'
  if (s === 'revoked') return 'Revocada'
  return status || '—'
}

function formatInvitedEmail(row) {
  const n = row.invitedEmailNormalized ?? row.InvitedEmailNormalized
  if (!n) return '—'
  return String(n).toLowerCase()
}

function parseCommissionPercents(commissionsJson) {
  const out = { newSalePercent: 0, activeClientPercent: 0 }
  if (!commissionsJson || typeof commissionsJson !== 'string') return out
  const t = commissionsJson.trim()
  if (!t) return out
  try {
    const o = JSON.parse(t)
    if (typeof o !== 'object' || o === null) return out
    const n = Number(o[COMMISSION_KEYS.newSale])
    const a = Number(o[COMMISSION_KEYS.activeClient])
    if (Number.isFinite(n)) out.newSalePercent = n
    if (Number.isFinite(a)) out.activeClientPercent = a
    return out
  } catch {
    return out
  }
}

function buildCommissionsJson(newSalePercent, activeClientPercent) {
  const n = Number(newSalePercent)
  const a = Number(activeClientPercent)
  return JSON.stringify({
    [COMMISSION_KEYS.newSale]: Number.isFinite(n) ? n : 0,
    [COMMISSION_KEYS.activeClient]: Number.isFinite(a) ? a : 0
  })
}

async function loadList() {
  loading.value = true
  listError.value = null
  forbidden.value = false
  try {
    const data = await apiService.adminListPartners()
    const items = data?.items ?? data?.Items ?? []
    partners.value = Array.isArray(items) ? items : []
  } catch (err) {
    if (err.response?.status === 401 || err.response?.status === 403) {
      forbidden.value = true
    } else {
      listError.value = err.response?.data?.error || err.message || 'Error al cargar partners'
    }
  } finally {
    loading.value = false
  }
}

function resetSpaceFieldValidation() {
  spaceFieldInvalid.value = false
  spaceFieldValid.value = false
  spaceFieldMessage.value = ''
  spaceSuccessMessage.value = ''
}

function onSpaceInput() {
  resetSpaceFieldValidation()
}

async function validateSpaceOnBlur() {
  resetSpaceFieldValidation()
  const raw = createForm.value.space.trim()
  if (!raw) {
    spaceFieldInvalid.value = true
    spaceFieldMessage.value = 'Este campo es obligatorio.'
    return
  }

  if (!SPACE_SLUG_PATTERN.test(raw)) {
    spaceFieldInvalid.value = true
    spaceFieldMessage.value =
      'Usa solo letras y números (sin espacios ni símbolos), entre 1 y 30 caracteres.'
    return
  }

  try {
    const r = await apiService.adminValidatePartnerSpace(raw)
    const ok = r?.ok ?? r?.Ok
    const err = r?.error ?? r?.Error
    const normalized = r?.normalized ?? r?.Normalized
    if (ok) {
      spaceFieldValid.value = true
      if (normalized && String(normalized) !== raw) {
        spaceSuccessMessage.value = `Disponible. Se guardará como «${normalized}».`
      } else {
        spaceSuccessMessage.value = 'Space disponible.'
      }
    } else {
      spaceFieldInvalid.value = true
      if (err === 'taken') {
        spaceFieldMessage.value = 'Este space ya está en uso. Elige otro.'
      } else if (err === 'invalid_format') {
        spaceFieldMessage.value =
          'Formato no válido: solo letras y números, máximo 30 caracteres.'
      } else {
        spaceFieldMessage.value = 'No se pudo validar el space.'
      }
    }
  } catch {
    spaceFieldInvalid.value = true
    spaceFieldMessage.value = 'No se pudo comprobar disponibilidad. Revisa la conexión.'
  }
}

function onCreateNameInput() {
  createNameBlurInvalid.value = false
}

function onCreateNameBlur() {
  createNameBlurInvalid.value = !createForm.value.name.trim()
}

function openCreateSlider() {
  createError.value = null
  resetSpaceFieldValidation()
  createNameBlurInvalid.value = false
  createForm.value = { space: '', name: '', newSalePercent: 0, activeClientPercent: 0, operatorInviteEmail: '' }
  createSliderOpen.value = true
}

function closeCreateSlider() {
  if (creating.value) return
  createSliderOpen.value = false
}

async function openManageSlider(id) {
  reservationSliderOpen.value = false
  reservationPartnerId.value = null
  reservationHeader.value = { name: '', space: '' }
  managePartnerId.value = id
  manageSliderOpen.value = true
  detail.value = null
  detailError.value = null
  lastReservation.value = null
  reservationExpiresLocal.value = ''
  reservationError.value = null
  saveError.value = null
  partnerReservationsList.value = []
  reservationsListError.value = null
  operatorRegenerateEmail.value = ''
  detailLoading.value = true
  try {
    const d = await apiService.adminGetPartner(id)
    detail.value = d
    const pct = parseCommissionPercents(d.commissionsJson ?? d.CommissionsJson)
    editForm.value = {
      name: d.name ?? '',
      newSalePercent: pct.newSalePercent,
      activeClientPercent: pct.activeClientPercent
    }
    await fetchPartnerReservations(id)
  } catch (err) {
    detailError.value = err.response?.data?.error || err.message || 'Error al cargar el partner'
  } finally {
    detailLoading.value = false
  }
}

async function fetchPartnerReservations(partnerId) {
  reservationsListLoading.value = true
  reservationsListError.value = null
  try {
    const data = await apiService.adminListPartnerReservations(partnerId)
    const items = data?.items ?? data?.Items ?? []
    partnerReservationsList.value = Array.isArray(items) ? items : []
    const op = partnerReservationsList.value.find((r) => (r.kind ?? r.Kind) === 'PartnerOperator')
    const norm = op?.invitedEmailNormalized ?? op?.InvitedEmailNormalized
    if (norm) operatorRegenerateEmail.value = String(norm).toLowerCase()
  } catch (err) {
    reservationsListError.value = err.response?.data?.error || err.message || 'Error al cargar reservas'
    partnerReservationsList.value = []
  } finally {
    reservationsListLoading.value = false
  }
}

async function submitOperatorInvite() {
  if (!managePartnerId.value || !operatorRegenerateEmail.value.trim()) return
  regeneratingOperatorInvite.value = true
  try {
    const dto = await apiService.adminCreatePartnerOperatorInvite(
      managePartnerId.value,
      operatorRegenerateEmail.value.trim()
    )
    const url = dto.startUrl ?? dto.StartUrl
    if (url) {
      try {
        await navigator.clipboard.writeText(url)
        toast.success('Enlace de operador generado y copiado al portapapeles.')
      } catch {
        toast.success('Enlace generado (revisa el portapapeles o copia desde la consola de red si hace falta).')
      }
    } else {
      toast.success('Invitación enviada por correo al operador (producción: el enlace no se muestra en la API).')
    }
    await fetchPartnerReservations(managePartnerId.value)
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'Error al generar enlace')
  } finally {
    regeneratingOperatorInvite.value = false
  }
}

function closeReservationSlider() {
  if (reserving.value) return
  reservationSliderOpen.value = false
  reservationPartnerId.value = null
  reservationHeader.value = { name: '', space: '' }
}

function closeManageSlider() {
  if (saving.value || reserving.value || regeneratingOperatorInvite.value) return
  reservationSliderOpen.value = false
  reservationPartnerId.value = null
  reservationHeader.value = { name: '', space: '' }
  manageSliderOpen.value = false
  managePartnerId.value = null
  detail.value = null
  partnerReservationsList.value = []
  reservationsListError.value = null
  operatorRegenerateEmail.value = ''
}

function openReservationSlider(partner) {
  if (!partner?.id) return
  reservationPartnerId.value = partner.id
  reservationHeader.value = {
    name: partner.name ?? '',
    space: partner.space ?? ''
  }
  reservationError.value = null
  lastReservation.value = null
  reservationExpiresLocal.value = ''
  reservationSliderOpen.value = true
}

async function submitCreate() {
  createError.value = null
  onCreateNameBlur()
  await validateSpaceOnBlur()
  if (spaceFieldInvalid.value) {
    createError.value = 'Corrige el space antes de crear.'
    return
  }
  if (createNameBlurInvalid.value) {
    createError.value = 'Completa el nombre del partner.'
    return
  }
  if (!createForm.value.operatorInviteEmail?.trim()) {
    createError.value = 'Indica el email del operador que usará el enlace de acceso.'
    return
  }
  creating.value = true
  try {
    const body = {
      space: createForm.value.space.trim(),
      name: createForm.value.name.trim(),
      commissionsJson: buildCommissionsJson(createForm.value.newSalePercent, createForm.value.activeClientPercent),
      brandingJson: null,
      operatorInviteEmail: createForm.value.operatorInviteEmail.trim()
    }
    const created = await apiService.adminCreatePartner(body)
    const inviteUrl = created?.operatorInviteStartUrl ?? created?.OperatorInviteStartUrl
    if (inviteUrl) {
      try {
        await navigator.clipboard.writeText(inviteUrl)
        toast.success('Partner creado. Enlace de acceso del operador copiado al portapapeles.')
        createSliderOpen.value = false
      } catch {
        createError.value = `Partner creado. Copia el enlace de acceso del operador y cierra cuando termines:\n${inviteUrl}`
        toast.success('Partner creado.')
      }
    } else {
      toast.success('Partner creado. Se envió la invitación con el enlace de acceso al correo del operador.')
      createSliderOpen.value = false
    }

    await loadList()
  } catch (err) {
    createError.value = err.response?.data?.error || err.message || 'Error al crear'
    toast.error(createError.value)
  } finally {
    creating.value = false
  }
}

async function submitSave() {
  if (!managePartnerId.value) return
  saveError.value = null
  saving.value = true
  try {
    const commissionsJson = buildCommissionsJson(editForm.value.newSalePercent, editForm.value.activeClientPercent)
    const updated = await apiService.adminUpdatePartner(managePartnerId.value, {
      name: editForm.value.name.trim() || null,
      commissionsJson
    })
    detail.value = updated
    toast.success('Partner actualizado.')
    await loadList()
    await fetchPartnerReservations(managePartnerId.value)
  } catch (err) {
    saveError.value = err.response?.data?.error || err.message || 'Error al guardar'
    toast.error(saveError.value)
  } finally {
    saving.value = false
  }
}

async function submitReservation() {
  if (!reservationPartnerId.value) return
  reservationError.value = null
  reserving.value = true
  try {
    let expiresAt = null
    if (reservationExpiresLocal.value) {
      const dt = new Date(reservationExpiresLocal.value)
      if (!Number.isNaN(dt.getTime())) expiresAt = dt.toISOString()
    }
    const dto = await apiService.adminCreatePartnerReservation(reservationPartnerId.value, {
      expiresAt,
      metadataJson: null
    })
    lastReservation.value = {
      reservedAccountId: dto.reservedAccountId ?? dto.ReservedAccountId,
      startUrl: dto.startUrl ?? dto.StartUrl,
      startToken: dto.startToken ?? dto.StartToken
    }
    toast.success('Reserva creada.')
  } catch (err) {
    reservationError.value = err.response?.data?.error || err.message || 'Error al crear reserva'
    toast.error(reservationError.value)
  } finally {
    reserving.value = false
  }
}

async function copyStartUrl() {
  const url = lastReservation.value?.startUrl
  if (!url) return
  try {
    await navigator.clipboard.writeText(url)
    toast.success('Enlace copiado.')
  } catch {
    toast.error('No se pudo copiar al portapapeles.')
  }
}

async function doDelete() {
  if (!deleteTarget.value) return
  const { id } = deleteTarget.value
  deleting.value = true
  try {
    await apiService.adminDeletePartner(id)
    toast.success('Partner eliminado.')
    deleteTarget.value = null
    if (reservationPartnerId.value === id) {
      reservationSliderOpen.value = false
      reservationPartnerId.value = null
      reservationHeader.value = { name: '', space: '' }
      lastReservation.value = null
    }
    if (managePartnerId.value === id) closeManageSlider()
    await loadList()
  } catch (err) {
    toast.error(err.response?.data?.error || err.message || 'Error al eliminar')
  } finally {
    deleting.value = false
  }
}

onMounted(loadList)
</script>

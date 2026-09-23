<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <div class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="mb-6 sm:mb-8">
        <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Facturación</h1>
        <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          Gestiona tu plan y suscripción.
        </p>
      </div>

      <!-- Success/cancel messages from redirect -->
      <div v-if="successMessage" class="mb-4 sm:mb-6 p-4 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-800 dark:text-green-300 text-sm sm:text-base">
        {{ successMessage }}
      </div>
      <div v-if="cancelMessage" class="mb-4 sm:mb-6 p-4 rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 text-amber-800 dark:text-amber-300 text-sm sm:text-base">
        {{ cancelMessage }}
      </div>

      <div v-if="loading" class="space-y-6 sm:space-y-8 animate-pulse">
      <!-- Skeleton: Plan actual -->
      <div class="p-4 sm:p-6 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
        <div class="h-5 w-28 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
        <div class="h-4 w-48 bg-gray-200 dark:bg-gray-700 rounded mb-4" />
        <div class="flex flex-wrap gap-3">
          <div class="h-10 w-40 bg-gray-200 dark:bg-gray-700 rounded-lg" />
          <div class="h-10 w-44 bg-gray-200 dark:bg-gray-700 rounded-lg" />
        </div>
      </div>
      <!-- Skeleton: Planes disponibles -->
      <div>
        <div class="h-5 w-40 bg-gray-200 dark:bg-gray-700 rounded mb-4" />
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
          <div
            v-for="i in 4"
            :key="i"
            class="rounded-xl border-2 border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-6 flex flex-col"
          >
            <div class="mb-4">
              <div class="h-5 w-24 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
              <div class="h-4 w-40 bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
            <div class="mb-6">
              <div class="h-8 w-20 bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
            <div class="mb-6 flex-1 space-y-2">
              <div class="h-3 w-32 bg-gray-200 dark:bg-gray-700 rounded" />
              <div v-for="j in 4" :key="j" class="h-3 w-full bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
            <div class="h-11 w-full bg-gray-200 dark:bg-gray-700 rounded-lg" />
          </div>
        </div>
      </div>
      <!-- Skeleton: Zona de peligro -->
      <div class="p-4 sm:p-6 rounded-xl border-2 border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50">
        <div class="h-5 w-36 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
        <div class="h-4 w-full max-w-md bg-gray-200 dark:bg-gray-700 rounded mb-4" />
        <div class="h-10 w-36 bg-gray-200 dark:bg-gray-700 rounded-lg" />
      </div>
    </div>

    <div v-else-if="forbidden" class="text-center py-12 sm:py-16 px-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl">
      <p class="text-amber-800 dark:text-amber-300 text-sm sm:text-base">
        Solo el propietario de la cuenta puede gestionar la facturación.
      </p>
    </div>

    <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6">
      <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
    </div>

    <template v-else>
      <!-- Trial callout -->
      <div
        v-if="!currentBilling?.hasActiveSubscription && (currentBilling?.trialExpiresAt != null || currentBilling?.isTrialExpired)"
        class="mb-6 sm:mb-8 p-4 sm:p-6 rounded-xl border-2"
        :class="currentBilling?.isTrialExpired
          ? 'bg-amber-50 dark:bg-amber-900/20 border-amber-200 dark:border-amber-800'
          : 'bg-primary-50 dark:bg-primary-900/20 border-primary-200 dark:border-primary-800'"
      >
        <h2 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white mb-2">
          {{ currentBilling?.isTrialExpired ? 'Tu periodo de prueba ha expirado' : 'Estás en periodo de prueba' }}
        </h2>
        <p class="text-sm sm:text-base text-gray-600 dark:text-gray-400 mb-4">
          {{ currentBilling?.isTrialExpired
            ? 'Activa tu plan para seguir usando la plataforma sin interrupciones.'
            : 'Puedes activar tu plan en cualquier momento para empezar a pagar y seguir usando la plataforma sin interrupciones.' }}
        </p>
        <p v-if="!currentBilling?.isTrialExpired && currentBilling?.daysRemaining != null" class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-4">
          {{ currentBilling.daysRemaining === 0
            ? 'Expira hoy.'
            : currentBilling.daysRemaining === 1
              ? '1 día restante.'
              : `${currentBilling.daysRemaining} días restantes.` }}
        </p>
        <p class="text-sm text-gray-600 dark:text-gray-400">
          Elige un plan abajo y haz clic en <strong>Activar plan</strong> o <strong>Cambiar a este plan</strong> para empezar a pagar cuando quieras.
        </p>
      </div>

      <!-- Current plan -->
      <div class="mb-6 sm:mb-8 p-4 sm:p-6 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
        <h2 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white mb-2">Plan actual</h2>
        <p class="text-sm sm:text-base text-gray-600 dark:text-gray-400">
          <span class="font-medium text-gray-900 dark:text-white">{{ currentPlanName }}</span>
          <span v-if="currentBilling?.hasActiveSubscription" class="ml-2 text-sm text-green-600 dark:text-green-400">
            (suscripción activa)
          </span>
          <span v-else-if="isInTrial" class="ml-2 text-sm text-primary-600 dark:text-primary-400">
            (periodo de prueba)
          </span>
          <span v-else class="ml-2 text-sm text-gray-500 dark:text-gray-400">
            (plan gratuito)
          </span>
          <span v-if="currentBilling?.appliedCouponCode" class="ml-2 text-sm text-primary-600 dark:text-primary-400">
            · Cupón {{ currentBilling.appliedCouponCode }} aplicado
          </span>
        </p>
        <div class="mt-4 flex flex-col sm:flex-row sm:flex-wrap gap-3">
          <button
            v-if="currentPlan"
            type="button"
            @click="openApplyCouponModal"
            :disabled="applyCouponLoading"
            class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg font-medium text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/30 transition-colors"
          >
            Aplicar cupón
          </button>
          <button
            v-if="currentBilling?.hasActiveSubscription"
            type="button"
            @click="openPortal"
            :disabled="portalLoading || cancelLoading"
            :class="[
              'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg font-medium transition-colors',
              portalLoading || cancelLoading
                ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                : 'bg-primary-600 hover:bg-primary-700 text-white'
            ]"
          >
            {{ portalLoading ? 'Abriendo...' : 'Gestionar suscripción' }}
          </button>
          <button
            type="button"
            @click="confirmCancel"
            :disabled="portalLoading || cancelLoading"
            :class="[
              'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg font-medium transition-colors',
              portalLoading || cancelLoading
                ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                : 'bg-red-100 dark:bg-red-900/40 hover:bg-red-200 dark:hover:bg-red-900/60 text-red-700 dark:text-red-300'
            ]"
          >
            {{ cancelLoading ? 'Cancelando...' : 'Cancelar suscripción' }}
          </button>
        </div>
      </div>

      <!-- Consumption history link -->
      <div class="mb-6 sm:mb-8 p-4 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
        <h2 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white mb-2">Uso de créditos</h2>
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-3">
          Consulta el detalle de cada consumo con tokens y fechas.
        </p>
        <router-link
          to="/admin/consumption-history"
          class="inline-flex items-center text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
        >
          Ver historial de consumo
          <svg class="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </router-link>
      </div>

      <!-- Plans grid -->
      <h2 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white mb-3 sm:mb-4">Planes disponibles</h2>
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
        <div
          v-for="plan in plans"
          :key="plan.key"
          :class="[
            'rounded-xl border-2 p-4 sm:p-6 flex flex-col',
            plan.key === currentBilling?.planKey
              ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20 dark:border-primary-400'
              : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800'
          ]"
        >
          <div class="mb-4">
            <h3 class="text-lg font-bold text-gray-900 dark:text-white">{{ plan.name }}</h3>
            <p class="text-sm text-gray-600 dark:text-gray-400 mt-1">{{ plan.tagline }}</p>
          </div>
          <div class="mb-4">
            <template v-if="plan.contactSales">
              <span class="text-3xl font-bold text-gray-900 dark:text-white">Contactar</span>
            </template>
            <template v-else>
              <span v-if="plan.strikethroughPriceUsd" class="text-lg text-gray-500 dark:text-gray-400 line-through mr-2">${{ plan.strikethroughPriceUsd }}</span>
              <span class="text-3xl font-bold text-gray-900 dark:text-white">${{ plan.priceUsd }}</span>
              <span class="text-gray-500 dark:text-gray-400">/mes</span>
            </template>
          </div>
          <div class="mb-6 flex-1">
            <p v-if="plan.contactSales" class="text-sm text-gray-600 dark:text-gray-400 mb-3">
              Condiciones y volumen a medida
            </p>
            <p v-else-if="plan.creditsPerMonth != null" class="text-sm text-gray-600 dark:text-gray-400 mb-3">
              {{ plan.creditsPerMonth.toLocaleString() }} / mes
            </p>
            <ul v-if="formatPlanLimits(plan.limits).length" class="space-y-2 mb-3">
              <li
                v-for="limit in formatPlanLimits(plan.limits)"
                :key="limit"
                class="flex items-start gap-2 text-sm text-gray-600 dark:text-gray-400"
              >
                <span class="text-primary-500 shrink-0">•</span>
                {{ limit }}
              </li>
            </ul>
            <ul class="space-y-2">
              <li
                v-for="feature in (plan.features || [])"
                :key="feature"
                class="flex items-start gap-2 text-sm text-gray-700 dark:text-gray-300"
              >
                <span class="text-green-500 shrink-0">✓</span>
                {{ feature }}
              </li>
            </ul>
          </div>
          <button
            v-if="plan.contactSales && plan.key !== currentBilling?.planKey"
            type="button"
            @click="showContactSalesModal = true"
            class="w-full py-3 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors bg-gray-800 dark:bg-gray-700 hover:bg-gray-700 dark:hover:bg-gray-600 text-white"
          >
            Contactar
          </button>
          <button
            v-else-if="plan.key !== currentBilling?.planKey"
            type="button"
            @click="openCheckoutModal(plan)"
            :disabled="checkoutLoading"
            :class="[
              'w-full py-3 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
              plan.key === 'smartops'
                ? 'bg-primary-600 hover:bg-primary-700 text-white'
                : 'bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-900 dark:text-white',
              checkoutLoading && 'opacity-70 cursor-not-allowed'
            ]"
          >
            {{ checkoutLoading ? 'Procesando...' : (isInTrial ? `Activar ${plan.name}` : 'Cambiar a este plan') }}
          </button>
          <div v-else class="flex flex-col gap-2">
            <span class="w-full py-3 rounded-lg font-medium text-center text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-900/30">
              Plan actual
            </span>
            <button
              v-if="isInTrial"
              type="button"
              @click="openCheckoutModal(plan)"
              :disabled="checkoutLoading"
              :class="[
                'w-full py-3 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors bg-primary-600 hover:bg-primary-700 text-white',
                checkoutLoading && 'opacity-70 cursor-not-allowed'
              ]"
            >
              {{ checkoutLoading ? 'Procesando...' : `Activar ${plan.name}` }}
            </button>
            <button
              type="button"
              @click="openApplyCouponModal"
              :disabled="applyCouponLoading"
              class="w-full py-2.5 min-h-[44px] touch-manipulation rounded-lg text-sm font-medium text-primary-600 dark:text-primary-400 hover:bg-primary-100 dark:hover:bg-primary-900/40 transition-colors border border-primary-200 dark:border-primary-800"
            >
              Aplicar cupón
            </button>
          </div>
        </div>
      </div>

      <!-- Historial de facturación -->
      <div class="mt-8 sm:mt-12 p-4 sm:p-6 rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
        <h2 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white mb-4">Historial de facturación</h2>
        <div v-if="invoicesLoading" class="py-8 text-center text-gray-500 dark:text-gray-400">
          Cargando facturas...
        </div>
        <div v-else-if="!invoices.length" class="py-8 text-center text-gray-500 dark:text-gray-400">
          No hay facturas aún. Las facturas aparecerán aquí cuando realices pagos.
        </div>
        <div v-else class="overflow-x-auto">
          <table v-resizable class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead>
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Fecha</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Importe</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Estado</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Período</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Acciones</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="inv in invoices"
                :key="inv.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer"
                @click="openInvoiceDetail(inv)"
              >
                <td class="px-4 py-3 text-sm text-gray-900 dark:text-white">
                  {{ formatDate(inv.paidAt || inv.createdAt) }}
                </td>
                <td class="px-4 py-3 text-sm font-medium text-gray-900 dark:text-white">
                  {{ formatAmount(inv.amount, inv.currency) }}
                </td>
                <td class="px-4 py-3">
                  <span
                    :class="[
                      'inline-flex px-2 py-1 text-xs font-medium rounded-full',
                      inv.status === 'paid'
                        ? 'bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300'
                        : inv.status === 'open' || inv.status === 'past_due'
                          ? 'bg-amber-100 dark:bg-amber-900/40 text-amber-800 dark:text-amber-300'
                          : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'
                    ]"
                  >
                    {{ statusLabel(inv.status) }}
                  </span>
                </td>
                <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                  <span v-if="inv.periodStart && inv.periodEnd">
                    {{ formatDate(inv.periodStart) }} – {{ formatDate(inv.periodEnd) }}
                  </span>
                  <span v-else class="text-gray-400">—</span>
                </td>
                <td class="px-4 py-3 text-right">
            <a
              v-if="inv.invoicePdf"
              :href="inv.invoicePdf"
              target="_blank"
              rel="noopener noreferrer"
              @click.stop
              class="text-primary-600 dark:text-primary-400 hover:underline text-sm"
            >
              PDF
            </a>
                  <button
                    v-else
                    type="button"
                    @click.stop="openInvoiceDetail(inv)"
                    class="text-primary-600 dark:text-primary-400 hover:underline text-sm"
                  >
                    Ver detalle
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Delete account -->
      <div class="mt-8 sm:mt-12 p-4 sm:p-6 rounded-xl border-2 border-red-200 dark:border-red-800 bg-red-50/50 dark:bg-red-900/20">
        <h2 class="text-base sm:text-lg font-semibold text-red-800 dark:text-red-300 mb-2">Zona de peligro</h2>
        <p class="text-sm text-red-700 dark:text-red-400 mb-4">
          Eliminar la cuenta eliminará la suscripción (si existe), revocará las invitaciones pendientes y desvinculará a todos los miembros. Esta acción no se puede deshacer.
        </p>
        <button
          type="button"
          @click="deleteAccountModalOpen = true"
          :disabled="deleteLoading"
          :class="[
            'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg font-medium transition-colors',
            deleteLoading
              ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
              : 'bg-red-600 hover:bg-red-700 text-white'
          ]"
        >
          {{ deleteLoading ? 'Eliminando...' : 'Eliminar cuenta' }}
        </button>
      </div>
    </template>

    <!-- Modal solo aplicar cupón (plan actual) -->
    <Teleport to="body">
      <div
        v-if="applyCouponModalOpen"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6 max-h-[85vh] overflow-y-auto">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Aplicar cupón</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            Introduce tu código para aplicar un descuento a tu plan actual
            <span class="font-medium text-gray-900 dark:text-white"> {{ currentPlanName }}</span>.
          </p>

          <div class="mt-4">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Código de oferta</label>
            <div class="flex gap-2">
              <input
                v-model="applyCouponInput"
                type="text"
                placeholder="Ej: LAUNCH20"
                class="input-field flex-1 min-w-0"
                :disabled="applyCouponLoading"
                @keyup.enter="submitApplyCoupon"
              />
            </div>
            <p v-if="applyCouponError" class="mt-2 text-sm text-red-600 dark:text-red-400">{{ applyCouponError }}</p>
          </div>

          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              @click="closeApplyCouponModal"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              Cancelar
            </button>
            <button
              type="button"
              @click="submitApplyCoupon"
              :disabled="applyCouponLoading || !applyCouponInput?.trim()"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
                applyCouponLoading || !applyCouponInput?.trim()
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-primary-600 hover:bg-primary-700 text-white'
              ]"
            >
              {{ applyCouponLoading ? 'Aplicando...' : 'Aplicar' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Modal cambio de plan con cupón opcional -->
    <Teleport to="body">
      <div
        v-if="checkoutModalOpen"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6 max-h-[85vh] overflow-y-auto">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Cambiar de plan</h3>
          <p v-if="selectedPlanForCheckout" class="mt-2 text-gray-600 dark:text-gray-400">
            Cambiarás a <span class="font-medium text-gray-900 dark:text-white">{{ selectedPlanForCheckout.name }}</span>
            <span v-if="selectedPlanForCheckout.strikethroughPriceUsd"><span class="line-through text-gray-500">${{ selectedPlanForCheckout.strikethroughPriceUsd }}</span> </span>— ${{ selectedPlanForCheckout.priceUsd }}/mes
          </p>

          <div class="mt-4">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Código de oferta (opcional)</label>
            <div class="flex gap-2">
              <input
                v-model="couponInput"
                type="text"
                placeholder="Ej: LAUNCH20"
                class="input-field flex-1 min-w-0"
                :disabled="validateCouponLoading"
                @keyup.enter="applyCoupon"
              />
              <button
                type="button"
                @click="applyCoupon"
                :disabled="validateCouponLoading || !couponInput?.trim()"
                :class="[
                  'px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg font-medium transition-colors shrink-0',
                  validateCouponLoading || !couponInput?.trim()
                    ? 'bg-gray-200 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                    : 'bg-primary-600 hover:bg-primary-700 text-white'
                ]"
              >
                {{ validateCouponLoading ? '...' : 'Aplicar' }}
              </button>
            </div>
            <p v-if="appliedCoupon" class="mt-2 text-sm text-green-600 dark:text-green-400">
              ✓ {{ appliedCoupon.description }}
              <span v-if="appliedCoupon.percentOff"> — {{ appliedCoupon.percentOff }}% descuento</span>
            </p>
            <p v-else-if="couponError" class="mt-2 text-sm text-red-600 dark:text-red-400">{{ couponError }}</p>
          </div>

          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              @click="closeCheckoutModal"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              Cancelar
            </button>
            <button
              type="button"
              @click="confirmCheckout"
              :disabled="checkoutLoading"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
                checkoutLoading
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-primary-600 hover:bg-primary-700 text-white'
              ]"
            >
              {{ checkoutLoading ? 'Procesando...' : (appliedCoupon?.percentOff >= 100 ? 'Activar plan gratis' : 'Continuar a pago') }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <ContactSalesModal v-model="showContactSalesModal" />

    <!-- Modal detalle de factura -->
    <Teleport to="body">
      <div
        v-if="invoiceDetailModalOpen"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6 max-h-[85vh] overflow-y-auto">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Detalle de factura</h3>
          <div v-if="selectedInvoice" class="mt-4 space-y-3 text-sm">
            <div class="flex justify-between">
              <span class="text-gray-500 dark:text-gray-400">ID</span>
              <span class="font-mono text-gray-900 dark:text-white">{{ selectedInvoice.providerInvoiceId }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-gray-500 dark:text-gray-400">Importe</span>
              <span class="font-medium text-gray-900 dark:text-white">{{ formatAmount(selectedInvoice.amount, selectedInvoice.currency) }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-gray-500 dark:text-gray-400">Estado</span>
              <span
                :class="[
                  'inline-flex px-2 py-0.5 text-xs font-medium rounded-full',
                  selectedInvoice.status === 'paid'
                    ? 'bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300'
                    : selectedInvoice.status === 'open' || selectedInvoice.status === 'past_due'
                      ? 'bg-amber-100 dark:bg-amber-900/40 text-amber-800 dark:text-amber-300'
                      : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'
                ]"
              >
                {{ statusLabel(selectedInvoice.status) }}
              </span>
            </div>
            <div class="flex justify-between" v-if="selectedInvoice.paidAt">
              <span class="text-gray-500 dark:text-gray-400">Fecha de pago</span>
              <span class="text-gray-900 dark:text-white">{{ formatDate(selectedInvoice.paidAt) }}</span>
            </div>
            <div class="flex justify-between" v-if="selectedInvoice.periodStart && selectedInvoice.periodEnd">
              <span class="text-gray-500 dark:text-gray-400">Período</span>
              <span class="text-gray-900 dark:text-white">{{ formatDate(selectedInvoice.periodStart) }} – {{ formatDate(selectedInvoice.periodEnd) }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-gray-500 dark:text-gray-400">Creada</span>
              <span class="text-gray-900 dark:text-white">{{ formatDate(selectedInvoice.createdAt) }}</span>
            </div>
          </div>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <a
              v-if="selectedInvoice?.invoicePdf"
              :href="selectedInvoice.invoicePdf"
              target="_blank"
              rel="noopener noreferrer"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white text-center inline-block"
            >
              Descargar PDF
            </a>
            <button
              type="button"
              @click="invoiceDetailModalOpen = false"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] sm:min-h-0 touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              Cerrar
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Modal confirmar eliminar cuenta -->
    <Teleport to="body">
      <div
        v-if="deleteAccountModalOpen"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar cuenta</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Estás seguro de que deseas eliminar tu cuenta? Se cancelará la suscripción, se revocarán las invitaciones y se desvincularán todos los miembros. Esta acción no se puede deshacer.
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              @click="deleteAccountModalOpen = false"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              Cancelar
            </button>
            <button
              type="button"
              @click="doDeleteAccount"
              :disabled="deleteLoading"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors',
                deleteLoading
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-red-600 hover:bg-red-700 text-white'
              ]"
            >
              {{ deleteLoading ? 'Eliminando...' : 'Eliminar cuenta' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { formatPlanLimits } from '../../utils/planLimits'
import ContactSalesModal from '../../components/billing/ContactSalesModal.vue'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const plans = ref([])
const currentBilling = ref(null)
const loading = ref(true)
const error = ref(null)
const forbidden = ref(false)
const checkoutLoading = ref(false)
const portalLoading = ref(false)
const cancelLoading = ref(false)
const deleteLoading = ref(false)
const deleteAccountModalOpen = ref(false)
const showContactSalesModal = ref(false)

const checkoutModalOpen = ref(false)
const selectedPlanForCheckout = ref(null)

const invoices = ref([])
const invoicesLoading = ref(false)
const invoiceDetailModalOpen = ref(false)
const selectedInvoice = ref(null)

const applyCouponModalOpen = ref(false)
const applyCouponInput = ref('')
const applyCouponError = ref(null)
const applyCouponLoading = ref(false)
const couponInput = ref('')
const appliedCoupon = ref(null)
const couponError = ref(null)
const validateCouponLoading = ref(false)

const verifiedPaymentMessage = ref(null)
const successMessage = computed(() => verifiedPaymentMessage.value)
const cancelMessage = computed(() => {
  if (route.query.canceled === 'true') return 'Pago cancelado.'
  return null
})

const currentPlan = computed(() => {
  if (!currentBilling.value?.planKey) return null
  return plans.value.find(p => p.key === currentBilling.value.planKey) ?? null
})

const currentPlanName = computed(() => {
  return currentPlan.value?.name ?? currentBilling.value?.planKey ?? 'Starter'
})

const isInTrial = computed(() => {
  const b = currentBilling.value
  if (!b || b.hasActiveSubscription) return false
  return b.trialExpiresAt != null || b.isTrialExpired === true
})

async function verifyPaymentFromRedirect() {
  const sessionId = route.query.session_id
  if (route.query.success !== 'true' || !sessionId?.trim()) return
  try {
    const result = await apiService.confirmPayment(sessionId.trim())
    if (result?.success && result?.message) {
      verifiedPaymentMessage.value = result.message
      router.replace({ path: '/admin/billing', query: {} })
    }
  } catch {
    // Silently ignore - don't show fake success
  }
}

async function load() {
  loading.value = true
  error.value = null
  forbidden.value = false
  try {
    const plansData = await apiService.getPlans()
    plans.value = plansData?.plans ?? []

    try {
      currentBilling.value = await apiService.getCurrentBilling()
    } catch (err) {
      if (err.response?.status === 403) {
        forbidden.value = true
      } else {
        throw err
      }
    }

    if (!forbidden.value) {
      invoicesLoading.value = true
      try {
        const invoicesData = await apiService.getBillingInvoices(50)
        invoices.value = invoicesData?.invoices ?? []
      } catch {
        invoices.value = []
      } finally {
        invoicesLoading.value = false
      }
    }
  } catch (err) {
    error.value = err.response?.data?.error || err.response?.data?.message || 'Error al cargar la facturación'
  } finally {
    loading.value = false
  }
  await verifyPaymentFromRedirect()
}

function formatAmount(amountCents, currency = 'usd') {
  const amount = (amountCents / 100).toFixed(2)
  const symbols = { usd: '$', eur: '€', gbp: '£' }
  const sym = symbols[currency?.toLowerCase()] ?? currency?.toUpperCase() + ' '
  return `${sym}${amount}`
}

function formatDate(val) {
  if (!val) return '—'
  const d = new Date(val)
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

function statusLabel(status) {
  const labels = { paid: 'Pagada', open: 'Pendiente', past_due: 'Vencida', draft: 'Borrador', void: 'Anulada', uncollectible: 'Incobrable' }
  return labels[status?.toLowerCase()] ?? status ?? '—'
}

function openInvoiceDetail(inv) {
  selectedInvoice.value = inv
  invoiceDetailModalOpen.value = true
}

function openApplyCouponModal() {
  applyCouponInput.value = ''
  applyCouponError.value = null
  applyCouponModalOpen.value = true
}

function closeApplyCouponModal() {
  applyCouponModalOpen.value = false
  applyCouponInput.value = ''
  applyCouponError.value = null
}

async function submitApplyCoupon() {
  const code = applyCouponInput.value?.trim()
  if (!code) return
  applyCouponLoading.value = true
  applyCouponError.value = null
  try {
    await apiService.applyCouponToAccount(code)
    closeApplyCouponModal()
    toast.success('Cupón aplicado correctamente.')
    await load()
  } catch (err) {
    applyCouponError.value = err.response?.data?.error || 'Error al aplicar el cupón'
  } finally {
    applyCouponLoading.value = false
  }
}

function openCheckoutModal(plan) {
  selectedPlanForCheckout.value = plan
  couponInput.value = ''
  appliedCoupon.value = null
  couponError.value = null
  checkoutModalOpen.value = true
}

function closeCheckoutModal() {
  checkoutModalOpen.value = false
  selectedPlanForCheckout.value = null
  couponInput.value = ''
  appliedCoupon.value = null
  couponError.value = null
}

async function applyCoupon() {
  const code = couponInput.value?.trim()
  if (!code) return
  validateCouponLoading.value = true
  couponError.value = null
  try {
    const result = await apiService.validateCoupon(code)
    if (result?.valid) {
      appliedCoupon.value = {
        code: result.code,
        percentOff: result.percentOff,
        description: result.description
      }
    } else {
      appliedCoupon.value = null
      couponError.value = 'Cupón no válido o expirado'
    }
  } catch (err) {
    appliedCoupon.value = null
    couponError.value = err.response?.data?.error || 'Error al validar el cupón'
  } finally {
    validateCouponLoading.value = false
  }
}

async function confirmCheckout() {
  if (!selectedPlanForCheckout.value) return
  checkoutLoading.value = true
  error.value = null
  try {
    // Usar cupón validado o el input si el usuario no pulsó "Aplicar"
    const couponCode = appliedCoupon.value?.code || couponInput.value?.trim() || null
    const result = await apiService.createCheckout(selectedPlanForCheckout.value.key, couponCode)
    if (result?.skipCheckout) {
      closeCheckoutModal()
      toast.success('Plan actualizado correctamente.')
      await load()
    } else if (result?.checkoutUrl) {
      window.location.href = result.checkoutUrl
    } else {
      error.value = 'No se pudo crear la sesión de pago'
      closeCheckoutModal()
    }
  } catch (err) {
    error.value = err.response?.data?.error || 'Error al iniciar el checkout'
    closeCheckoutModal()
  } finally {
    checkoutLoading.value = false
  }
}

async function openPortal() {
  portalLoading.value = true
  error.value = null
  try {
    const result = await apiService.createPortal()
    if (result?.portalUrl) {
      window.location.href = result.portalUrl
    } else {
      error.value = 'No se pudo abrir el portal'
    }
  } catch (err) {
    error.value = err.response?.data?.error || 'Error al abrir el portal'
  } finally {
    portalLoading.value = false
  }
}

function confirmCancel() {
  if (window.confirm('¿Estás seguro de que deseas cancelar tu suscripción? Volverás al plan básico.')) {
    cancelSubscription()
  }
}

async function cancelSubscription() {
  cancelLoading.value = true
  error.value = null
  try {
    const result = await apiService.cancelSubscription()
    if (result?.success) {
      await load()
    } else {
      error.value = result?.message || 'No se pudo cancelar la suscripción'
    }
  } catch (err) {
    error.value = err.response?.data?.error || err.response?.data?.message || 'Error al cancelar la suscripción'
  } finally {
    cancelLoading.value = false
  }
}

async function doDeleteAccount() {
  deleteAccountModalOpen.value = false
  deleteLoading.value = true
  try {
    const result = await apiService.deleteAccount()
    const success = result?.success === true
    if (success) {
      apiService.setToken(null)
      window.location.replace('/sign-in')
      return
    }
    error.value = result?.message || 'No se pudo eliminar la cuenta'
  } catch (err) {
    error.value = err.response?.data?.error || err.response?.data?.message || 'Error al eliminar la cuenta'
  } finally {
    deleteLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="modelValue"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      >
        <Transition
          enter-active-class="transition duration-200 ease-out"
          enter-from-class="opacity-0 scale-95 translate-y-4"
          enter-to-class="opacity-100 scale-100 translate-y-0"
          leave-active-class="transition duration-150 ease-in"
          leave-from-class="opacity-100 scale-100 translate-y-0"
          leave-to-class="opacity-0 scale-95 translate-y-4"
        >
          <div
            v-if="modelValue"
            class="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border border-gray-200/80 dark:border-gray-700 max-w-md w-full overflow-hidden"
          >
            <!-- Header con gradiente sutil -->
            <div class="bg-gradient-to-br from-primary-50 to-primary-100/50 dark:from-primary-900/30 dark:to-primary-800/20 px-6 py-6 border-b border-gray-100 dark:border-gray-700/50">
              <div class="flex items-center gap-4">
                <div class="w-14 h-14 rounded-2xl bg-primary-500 dark:bg-primary-600 flex items-center justify-center shrink-0 shadow-lg shadow-primary-500/20">
                  <span class="text-xl font-bold text-white">RV</span>
                </div>
                <div>
                  <h3 class="text-xl font-bold text-gray-900 dark:text-white">Ricardo Vivanco</h3>
                  <p class="text-sm text-primary-600 dark:text-primary-400 font-medium">Plan Enterprise</p>
                </div>
              </div>
              <p class="mt-4 text-sm text-gray-600 dark:text-gray-400 leading-relaxed">
                ¿Interesado en el plan Enterprise? Te contactaremos para preparar una propuesta a medida.
              </p>
            </div>

            <!-- Datos de contacto -->
            <div class="p-6 space-y-4">
              <a
                href="mailto:ricardo@meetgravity.io"
                class="flex items-center gap-4 p-4 rounded-xl bg-gray-50 dark:bg-gray-700/50 border border-gray-100 dark:border-gray-600/50 card-hover group"
              >
                <div class="w-10 h-10 rounded-lg bg-primary-100 dark:bg-primary-900/40 flex items-center justify-center shrink-0 group-hover:bg-primary-200 dark:group-hover:bg-primary-800/60 transition-colors">
                  <svg class="w-5 h-5 text-primary-600 dark:text-primary-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                  </svg>
                </div>
                <div class="min-w-0 flex-1">
                  <p class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Email</p>
                  <p class="text-base font-medium text-primary-600 dark:text-primary-400 truncate group-hover:underline">ricardo@meetgravity.io</p>
                </div>
                <svg class="w-4 h-4 text-gray-400 group-hover:text-primary-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                </svg>
              </a>

              <a
                href="tel:+51992363245"
                class="flex items-center gap-4 p-4 rounded-xl bg-gray-50 dark:bg-gray-700/50 border border-gray-100 dark:border-gray-600/50 card-hover group"
              >
                <div class="w-10 h-10 rounded-lg bg-primary-100 dark:bg-primary-900/40 flex items-center justify-center shrink-0 group-hover:bg-primary-200 dark:group-hover:bg-primary-800/60 transition-colors">
                  <svg class="w-5 h-5 text-primary-600 dark:text-primary-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                  </svg>
                </div>
                <div class="min-w-0 flex-1">
                  <p class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Teléfono</p>
                  <p class="text-base font-medium text-primary-600 dark:text-primary-400">992 363 245</p>
                </div>
                <svg class="w-4 h-4 text-gray-400 group-hover:text-primary-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                </svg>
              </a>
            </div>

            <div class="px-6 pb-6">
              <button
                type="button"
                @click="$emit('update:modelValue', false)"
                class="w-full py-3 rounded-xl text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
              >
                Cerrar
              </button>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
defineProps({
  modelValue: { type: Boolean, required: true }
})
defineEmits(['update:modelValue'])
</script>

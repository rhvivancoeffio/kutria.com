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
        class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/60"
      >
        <div
          class="bg-white dark:bg-gray-800 rounded-xl shadow-xl border border-gray-200 dark:border-gray-700 max-w-lg w-full max-h-[90vh] overflow-hidden flex flex-col"
          @click.stop
        >
          <div class="p-6 border-b border-gray-200 dark:border-gray-700">
            <template v-if="loading">
              <div class="h-6 w-40 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
              <div class="h-4 w-64 mt-2 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
            </template>
            <template v-else>
              <h2 class="text-xl font-semibold text-gray-900 dark:text-white">{{ tour?.name }}</h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">{{ tour?.description }}</p>
              <p v-if="pendingCount > 0" class="mt-2 text-sm text-amber-600 dark:text-amber-400">
                Te faltan {{ pendingCount }} paso{{ pendingCount !== 1 ? 's' : '' }} por completar.
              </p>
            </template>
          </div>
          <div class="flex-1 overflow-y-auto p-6">
            <!-- Skeleton loading -->
            <div v-if="loading" class="space-y-4">
              <div v-for="i in 4" :key="`step-skeleton-${i}`" class="flex gap-3">
                <div class="w-8 h-8 shrink-0 rounded-full bg-gray-200 dark:bg-gray-700 animate-pulse" />
                <div class="flex-1 min-w-0 space-y-2">
                  <div class="h-4 w-40 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-3 w-56 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-3 w-24 mt-2 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
              </div>
            </div>
            <div v-else-if="tour?.steps?.length" class="space-y-4">
              <div
                v-for="(step, idx) in tour.steps"
                :key="step.id"
                class="flex gap-3"
              >
                <div
                  :class="[
                    'w-8 h-8 rounded-full flex items-center justify-center shrink-0 text-sm font-medium',
                    effectiveCompletedSteps.includes(step.id)
                      ? 'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-300'
                      : idx === currentStepIndex
                        ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300'
                        : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400'
                  ]"
                >
                  <svg v-if="effectiveCompletedSteps.includes(step.id)" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span v-else>{{ idx + 1 }}</span>
                </div>
                <div class="flex-1 min-w-0">
                  <h3 class="font-medium text-gray-900 dark:text-white">{{ step.title }}</h3>
                  <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">{{ step.description }}</p>
                  <router-link
                    :to="step.route"
                    class="mt-2 inline-flex items-center gap-1 text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
                    @click="markStepAndClose(step.id)"
                  >
                    {{ step.routeLabel }}
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                    </svg>
                  </router-link>
                </div>
              </div>
            </div>
            <p v-else-if="!loading && !tour?.steps?.length" class="text-sm text-gray-500 dark:text-gray-400">
              No hay pasos configurados para este tour.
            </p>
          </div>
          <div class="p-4 border-t border-gray-200 dark:border-gray-700 flex justify-between gap-3">
            <button
              type="button"
              class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
              @click="$emit('update:modelValue', false)"
            >
              Cerrar
            </button>
            <button
              type="button"
              class="px-4 py-2 text-sm font-medium text-white bg-primary-600 hover:bg-primary-700 rounded-lg"
              @click="finishAndClose"
            >
              Completar tour
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed, watch } from 'vue'
import { useAccountUsage } from '../../composables/useAccountUsage'
import { useOnboardingTour } from '../../composables/useOnboardingTour'

const props = defineProps({
  modelValue: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue'])

const { usage, fetchUsage } = useAccountUsage()
const accountId = computed(() => usage.value?.accountId ?? '')
const {
  tour,
  loading,
  effectiveCompletedSteps,
  currentStepIndex,
  fetchTour,
  markStepCompleted,
  complete
} = useOnboardingTour(accountId, usage)

const pendingCount = computed(() => {
  const steps = tour.value?.steps ?? []
  const completed = effectiveCompletedSteps.value
  return steps.filter(s => !completed.includes(s.id)).length
})

watch(() => props.modelValue, async (open) => {
  if (open) {
    await fetchUsage()
    await fetchTour()
  }
})

function markStepAndClose(stepId) {
  markStepCompleted(stepId)
  emit('update:modelValue', false)
}

function finishAndClose() {
  complete()
  emit('update:modelValue', false)
}
</script>

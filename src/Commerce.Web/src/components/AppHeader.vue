<template>
  <header
    v-if="!isAdminContext"
    :class="[
      sticky && 'sticky top-0 z-50',
      brandVariant === 'commerce'
        ? 'border-b border-white/5 bg-ink-950/70 backdrop-blur-md'
        : [
            'border-b border-gray-200 dark:border-gray-800',
            backdrop && 'bg-white/80 dark:bg-gray-800/80 backdrop-blur-md',
            !backdrop && 'bg-white dark:bg-gray-800',
            shadow && 'shadow-sm'
          ]
    ]"
  >
    <div :class="['w-full px-4 sm:px-6 lg:px-8', compact ? 'py-3' : 'py-4']">
      <div class="relative flex w-full items-center">
        <div class="flex min-w-0 flex-1 shrink-0 items-center">
          <router-link to="/" class="flex items-center text-gray-900 transition-opacity hover:opacity-90 dark:text-white">
            <KutriaMark :size="brandVariant === 'commerce' ? 'sm' : 'sm'" />
          </router-link>
        </div>

        <nav class="absolute left-1/2 hidden -translate-x-1/2 items-center space-x-6 lg:flex">
          <router-link to="/precios" :class="navLinkClass(activeRoute === '/precios')">
            Precios
          </router-link>
          <router-link to="/docs" :class="navLinkClass(activeRoute.startsWith('/docs'))">
            Documentación
          </router-link>
        </nav>

        <div class="flex min-w-0 flex-1 shrink-0 items-center justify-end gap-3">
          <template v-if="isAuthenticated">
            <router-link
              to="/admin"
              :class="
                brandVariant === 'commerce'
                  ? 'hidden h-9 items-center rounded-full bg-[#00F5FF] px-5 text-sm font-semibold text-black hover:bg-[#7dfff8] sm:inline-flex'
                  : [
                      'hidden text-sm font-medium transition-colors sm:inline',
                      activeRoute.startsWith('/admin')
                        ? 'font-semibold text-primary-600 dark:text-primary-400'
                        : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white'
                    ]
              "
            >
              Panel
            </router-link>
          </template>
          <template v-else>
            <router-link
              to="/sign-in"
              :class="[
                'hidden text-sm font-medium transition-colors sm:inline',
                brandVariant === 'commerce'
                  ? activeRoute === '/sign-in'
                    ? 'font-semibold text-white'
                    : 'text-white/55 hover:text-white'
                  : activeRoute === '/sign-in'
                    ? 'font-semibold text-primary-600 dark:text-primary-400'
                    : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white'
              ]"
            >
              Ingresar
            </router-link>
            <router-link
              to="/sign-up"
              :class="
                brandVariant === 'commerce'
                  ? 'hidden h-9 items-center rounded-full bg-[#00F5FF] px-5 text-sm font-semibold text-black hover:bg-[#7dfff8] sm:inline-flex'
                  : [
                      'hidden text-sm font-medium transition-colors sm:inline',
                      activeRoute === '/sign-up'
                        ? 'font-semibold text-primary-600 dark:text-primary-400'
                        : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white'
                    ]
              "
            >
              Registro
            </router-link>
          </template>
          <button
            type="button"
            class="lg:hidden"
            :class="
              brandVariant === 'commerce'
                ? '-mr-2 rounded-lg p-2 text-white/70 hover:bg-white/5'
                : '-mr-2 rounded-lg p-2 text-gray-600 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-700'
            "
            aria-label="Menú"
            :aria-expanded="mobileMenuOpen"
            @click="mobileMenuOpen = !mobileMenuOpen"
          >
            <svg v-if="!mobileMenuOpen" class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
            <svg v-else class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
          <slot name="actions" />
        </div>
      </div>

      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="-translate-y-2 opacity-0"
        enter-to-class="translate-y-0 opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="translate-y-0 opacity-100"
        leave-to-class="-translate-y-2 opacity-0"
      >
        <nav
          v-show="mobileMenuOpen"
          :class="[
            'mt-4 border-t pb-2 pt-4 lg:hidden',
            brandVariant === 'commerce' ? 'border-white/10' : 'border-gray-200 dark:border-gray-700'
          ]"
        >
          <div class="flex flex-col gap-1">
            <template v-if="isAuthenticated">
              <router-link
                to="/admin"
                :class="mobileLinkClass(activeRoute.startsWith('/admin'))"
                @click="mobileMenuOpen = false"
              >
                Panel
              </router-link>
            </template>
            <template v-else>
              <router-link
                to="/sign-in"
                :class="mobileLinkClass(activeRoute === '/sign-in')"
                @click="mobileMenuOpen = false"
              >
                Ingresar
              </router-link>
              <router-link
                to="/sign-up"
                :class="mobileLinkClass(activeRoute === '/sign-up')"
                @click="mobileMenuOpen = false"
              >
                Registro
              </router-link>
            </template>
            <router-link
              to="/precios"
              :class="mobileLinkClass(activeRoute === '/precios')"
              @click="mobileMenuOpen = false"
            >
              Precios
            </router-link>
            <router-link
              to="/docs"
              :class="mobileLinkClass(activeRoute.startsWith('/docs'))"
              @click="mobileMenuOpen = false"
            >
              Documentación
            </router-link>
          </div>
        </nav>
      </Transition>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import apiService from '../services/api'
import KutriaMark from './kutria/KutriaMark.vue'

const mobileMenuOpen = ref(false)
const route = useRoute()
const isAuthenticated = computed(() => apiService.isAuthenticated())

watch(
  () => route.path,
  () => {
    mobileMenuOpen.value = false
  }
)

const props = defineProps({
  sticky: { type: Boolean, default: false },
  backdrop: { type: Boolean, default: false },
  shadow: { type: Boolean, default: true },
  compact: { type: Boolean, default: false },
  maxWidth: {
    type: String,
    default: 'full',
    validator: (value) => ['full', 'narrow'].includes(value)
  },
  brandVariant: {
    type: String,
    default: 'default',
    validator: (value) => ['default', 'commerce'].includes(value)
  }
})

const activeRoute = computed(() => route.path)
const isAdminContext = computed(() => route.path.startsWith('/admin'))

function navLinkClass(active) {
  if (props.brandVariant === 'commerce') {
    return active ? 'font-semibold text-white' : 'text-white/50 transition-colors hover:text-white'
  }
  return active
    ? 'font-semibold text-gray-900 dark:text-white'
    : 'text-gray-600 transition-colors hover:text-gray-900 dark:text-gray-300 dark:hover:text-white'
}

function mobileLinkClass(active) {
  if (props.brandVariant === 'commerce') {
    return [
      'rounded-xl px-3 py-2.5 text-sm font-medium transition-colors',
      active ? 'bg-primary-500/10 text-primary-500' : 'text-white/70 hover:bg-white/5'
    ]
  }
  return [
    'rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
    active
      ? 'bg-primary-50 text-primary-600 dark:bg-primary-900/20 dark:text-primary-400'
      : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-700'
  ]
}
</script>

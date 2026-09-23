<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-opacity duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="modelValue"
        class="fixed inset-0 z-[110] flex items-end justify-center bg-black/60 p-0 sm:items-center sm:p-4 sm:backdrop-blur-[2px]"
        role="dialog"
        aria-modal="true"
        aria-labelledby="mandatory-store-modal-title"
        aria-describedby="mandatory-store-modal-desc"
      >
        <div
          class="flex h-[min(86vh,30rem)] w-full max-w-3xl flex-col overflow-hidden rounded-t-2xl border border-gray-200 bg-white shadow-2xl dark:border-gray-700 dark:bg-gray-800 sm:h-[min(84vh,40rem)] sm:max-w-5xl sm:rounded-2xl"
          @click.stop
        >
          <div
            class="flex shrink-0 items-start gap-3 border-b border-gray-100 px-5 pb-4 pt-5 dark:border-gray-700/80 sm:px-6 sm:pb-5 sm:pt-6"
          >
            <div
              class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary-100 text-primary-600 dark:bg-primary-900/45 dark:text-primary-300"
              aria-hidden="true"
            >
              <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="1.75"
                  d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                />
              </svg>
            </div>
            <div class="min-w-0 flex-1 pt-0.5">
              <p class="text-[11px] font-semibold uppercase tracking-wide text-amber-600 dark:text-amber-400">
                Paso obligatorio
              </p>
              <h2
                id="mandatory-store-modal-title"
                class="mt-0.5 text-lg font-semibold text-gray-900 dark:text-white sm:text-xl"
              >
                {{
                  step === 1
                    ? 'Conecta tu fuente de comercio'
                    : flow === 'create'
                      ? 'Crea tu tienda'
                      : 'Elige cómo conectar tu tienda'
                }}
              </h2>
              <p
                id="mandatory-store-modal-desc"
                class="mt-1.5 text-sm leading-relaxed text-gray-500 dark:text-gray-400"
              >
                <template v-if="step === 1">
                  Sin una tienda o API conectada no puedes sincronizar catálogo, órdenes ni potenciar a tus agentes.
                  Este paso es obligatorio para continuar usando la plataforma.
                </template>
                <template v-else-if="flow === 'create'">
                  Define cómo se llamará tu tienda nativa. Al crear se conecta Gravity automáticamente.
                </template>
                <template v-else>
                  Selecciona un proveedor. Te llevaremos a Integraciones para terminar la configuración con tus credenciales.
                </template>
              </p>
            </div>
          </div>

          <div class="min-h-0 flex-1 overflow-y-auto overscroll-contain px-5 py-5 sm:px-6 sm:py-6">
            <!-- Step 1: option cards -->
            <div v-if="step === 1" class="grid grid-cols-1 gap-4 sm:grid-cols-2 sm:items-stretch">
              <button
                type="button"
                class="group relative flex h-full flex-col rounded-xl border border-primary-300 bg-white p-5 text-left shadow-sm transition-all hover:border-primary-500 hover:shadow-md dark:border-primary-600/60 dark:bg-gray-800 dark:hover:border-primary-400"
                @click="goToCreateStep"
              >
                <div
                  class="flex h-12 w-12 items-center justify-center rounded-xl bg-primary-100 text-primary-600 dark:bg-primary-900/45 dark:text-primary-300"
                  aria-hidden="true"
                >
                  <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="1.75"
                      d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                    />
                  </svg>
                </div>
                <h3
                  class="mt-4 text-base font-semibold text-gray-900 group-hover:text-primary-700 dark:text-white dark:group-hover:text-primary-300"
                >
                  Crear mi tienda
                </h3>
                <p class="mt-1.5 flex-1 text-sm leading-relaxed text-gray-500 dark:text-gray-400">
                  Crea una tienda nativa en la plataforma con Gravity.
                </p>
                <span
                  class="mt-5 inline-flex min-h-[44px] items-center justify-center gap-1.5 rounded-lg bg-primary-600 px-4 py-2.5 text-sm font-medium text-white group-hover:bg-primary-700"
                >
                  Continuar
                  <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                </span>
              </button>

              <button
                type="button"
                class="group flex h-full flex-col rounded-xl border border-gray-200 bg-white p-5 text-left shadow-sm transition-all hover:border-primary-400 hover:shadow-md dark:border-gray-700 dark:bg-gray-800 dark:hover:border-primary-500"
                @click="goToConnectStep"
              >
                <div
                  class="flex h-12 w-12 items-center justify-center rounded-xl bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300"
                  aria-hidden="true"
                >
                  <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="1.75"
                      d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                    />
                  </svg>
                </div>
                <h3
                  class="mt-4 text-base font-semibold text-gray-900 group-hover:text-primary-700 dark:text-white dark:group-hover:text-primary-300"
                >
                  Conectar mi tienda
                </h3>
                <p class="mt-1.5 flex-1 text-sm leading-relaxed text-gray-500 dark:text-gray-400">
                  Conecta Shopify, una API u otro proveedor para importar catálogo y ventas.
                </p>
                <span
                  class="mt-5 inline-flex min-h-[44px] items-center justify-center gap-1.5 rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 group-hover:border-primary-400 group-hover:text-primary-700 dark:border-gray-600 dark:text-gray-200 dark:group-hover:border-primary-500 dark:group-hover:text-primary-300"
                >
                  Continuar
                  <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                </span>
              </button>
            </div>

            <!-- Step 2 create: store name -->
            <div v-else-if="flow === 'create'" class="mx-auto w-full max-w-2xl">
              <div v-if="loading" class="space-y-4">
                <div class="h-24 animate-pulse rounded-2xl bg-gray-200 dark:bg-gray-700" />
                <div class="h-28 animate-pulse rounded-2xl bg-gray-200 dark:bg-gray-700" />
              </div>
              <div
                v-else-if="error"
                class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-800 dark:bg-red-900/20 dark:text-red-300"
              >
                {{ error }}
              </div>
              <div
                v-else-if="!gravityMeta"
                class="rounded-xl border border-dashed border-gray-300 px-4 py-10 text-center dark:border-gray-600"
              >
                <p class="text-sm text-gray-500 dark:text-gray-400">
                  No está disponible el conector Gravity para crear la tienda.
                </p>
              </div>
              <div v-else class="space-y-5">
                <div
                  class="rounded-2xl border border-gray-200 bg-white p-5 shadow-sm dark:border-gray-700 dark:bg-gray-800/60 sm:p-6"
                >
                  <label for="mandatory-store-name" class="block text-sm font-semibold text-gray-900 dark:text-white">
                    Nombre de tu tienda
                    <span class="text-red-500" aria-hidden="true">*</span>
                  </label>
                  <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    Así la verás en Integraciones y en el panel de uso.
                  </p>
                  <div class="relative mt-4">
                    <div
                      class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-gray-400"
                      aria-hidden="true"
                    >
                      <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="1.75"
                          d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"
                        />
                      </svg>
                    </div>
                    <input
                      id="mandatory-store-name"
                      v-model="storeName"
                      type="text"
                      required
                      maxlength="120"
                      autocomplete="organization"
                      placeholder="Ej. Boutique Norte"
                      class="w-full rounded-xl border border-gray-300 bg-gray-50 py-3.5 pl-11 pr-4 text-base text-gray-900 placeholder:text-gray-400 focus:border-primary-500 focus:bg-white focus:outline-none focus:ring-2 focus:ring-primary-500/30 dark:border-gray-600 dark:bg-gray-900 dark:text-white dark:placeholder:text-gray-500 dark:focus:bg-gray-900"
                      :disabled="creating"
                      @blur="storeNameTouched = true"
                      @keydown.enter.prevent="submitCreateStore"
                    />
                  </div>
                  <p v-if="storeNameTouched && !storeName.trim()" class="mt-2 text-sm text-red-600 dark:text-red-400">
                    El nombre de la tienda es requerido
                  </p>
                  <div
                    v-else-if="storeName.trim()"
                    class="mt-3 flex items-center gap-2 rounded-lg bg-gray-50 px-3 py-2 text-sm text-gray-600 dark:bg-gray-900/60 dark:text-gray-300"
                  >
                    <span class="text-xs font-medium uppercase tracking-wide text-gray-400 dark:text-gray-500">
                      Vista previa
                    </span>
                    <span class="truncate font-semibold text-gray-900 dark:text-white">{{ storeName.trim() }}</span>
                  </div>
                </div>

                <ul class="grid gap-3 sm:grid-cols-3" aria-label="Qué incluye crear la tienda">
                  <li
                    v-for="item in createStoreHighlights"
                    :key="item.title"
                    class="flex gap-3 rounded-xl border border-gray-100 bg-gray-50/80 px-3.5 py-3 dark:border-gray-700/80 dark:bg-gray-900/40"
                  >
                    <span
                      class="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-primary-100 text-primary-600 dark:bg-primary-900/40 dark:text-primary-300"
                      aria-hidden="true"
                    >
                      <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                          :d="item.icon"
                        />
                      </svg>
                    </span>
                    <div class="min-w-0">
                      <p class="text-sm font-semibold text-gray-900 dark:text-white">{{ item.title }}</p>
                      <p class="mt-0.5 text-xs leading-relaxed text-gray-500 dark:text-gray-400">{{ item.body }}</p>
                    </div>
                  </li>
                </ul>
              </div>
            </div>

            <!-- Step 2 connect: integrations tabs -->
            <div v-else>
              <div
                class="mb-5 -mx-1 overflow-x-auto border-b border-gray-200 px-1 dark:border-gray-700"
              >
                <nav class="flex min-w-max gap-1 -mb-px sm:min-w-0" aria-label="Tipo de integración">
                  <button
                    v-for="tab in integrationTabs"
                    :key="tab.id"
                    type="button"
                    :class="[
                      'shrink-0 whitespace-nowrap rounded-t-lg border-b-2 px-3 py-2.5 text-sm font-medium transition-colors sm:px-4',
                      activeTab === tab.id
                        ? 'border-primary-600 text-primary-600 dark:border-primary-400 dark:text-primary-400'
                        : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                    ]"
                    @click="activeTab = tab.id"
                  >
                    <span class="inline-flex items-center gap-2">
                      <span aria-hidden="true">{{ tab.icon }}</span>
                      {{ tab.label }}
                      <span
                        v-if="tab.count !== undefined"
                        :class="[
                          'ml-0.5 rounded-full px-2 py-0.5 text-xs',
                          activeTab === tab.id
                            ? 'bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300'
                            : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                        ]"
                      >
                        {{ tab.count }}
                      </span>
                    </span>
                  </button>
                </nav>
              </div>

              <div v-if="loading" class="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div
                  v-for="i in 4"
                  :key="i"
                  class="h-24 animate-pulse rounded-xl bg-gray-200 dark:bg-gray-700"
                />
              </div>

              <div
                v-else-if="error"
                class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 dark:border-red-800 dark:bg-red-900/20 dark:text-red-300"
              >
                {{ error }}
              </div>

              <div
                v-else-if="filteredAvailable.length === 0"
                class="rounded-xl border border-dashed border-gray-300 px-4 py-10 text-center dark:border-gray-600"
              >
                <p class="text-sm text-gray-500 dark:text-gray-400">
                  No hay integraciones disponibles en esta categoría.
                </p>
              </div>

              <div v-else class="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <button
                  v-for="meta in filteredAvailable"
                  :key="meta.key"
                  type="button"
                  class="group flex w-full gap-3 rounded-xl border border-gray-200 bg-white p-4 text-left transition-all hover:border-primary-400 hover:shadow-md dark:border-gray-700 dark:bg-gray-800/80 dark:hover:border-primary-500"
                  @click="selectIntegration(meta)"
                >
                  <div
                    class="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-gray-50 p-2 dark:bg-gray-700"
                  >
                    <img
                      :src="logoSrc(meta)"
                      :alt="meta.name"
                      class="max-h-full max-w-full object-contain"
                      @error="() => setLogoFailed(meta.logoUrl)"
                    />
                  </div>
                  <div class="min-w-0 flex-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <h3
                        class="font-semibold text-gray-900 group-hover:text-primary-600 dark:text-white dark:group-hover:text-primary-400"
                      >
                        {{ meta.name }}
                      </h3>
                      <span
                        class="rounded-full bg-gray-100 px-2 py-0.5 text-xs font-semibold text-gray-600 dark:bg-gray-700 dark:text-gray-400"
                      >
                        {{ integrationTypeBadge(meta) }}
                      </span>
                    </div>
                    <p class="mt-0.5 line-clamp-2 text-sm text-gray-600 dark:text-gray-400">
                      {{ meta.description }}
                    </p>
                  </div>
                  <svg
                    class="mt-1 h-5 w-5 shrink-0 text-gray-400 group-hover:text-primary-500"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <div
            v-if="step === 2"
            class="shrink-0 border-t border-gray-100 px-5 py-4 dark:border-gray-700/80 sm:px-6"
          >
            <div class="flex flex-col-reverse gap-2 sm:flex-row sm:items-center sm:justify-between">
              <button
                type="button"
                class="inline-flex min-h-[44px] w-full items-center justify-center gap-2 rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100 dark:border-gray-600 dark:text-gray-200 dark:hover:bg-gray-700 sm:w-auto"
                :disabled="creating"
                @click="step = 1"
              >
                <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                </svg>
                Volver
              </button>
              <button
                v-if="flow === 'create'"
                type="button"
                class="inline-flex min-h-[44px] w-full items-center justify-center rounded-lg bg-primary-600 px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50 sm:w-auto"
                :disabled="creating || loading || !gravityMeta"
                @click="submitCreateStore"
              >
                {{ creating ? 'Creando…' : 'Crear' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch, onUnmounted } from 'vue'
import { useToast } from 'vue-toastification'
import { useAppStore } from '../../stores/appStore'
import apiService from '../../services/api'
import { eventBus, MY_INTEGRATIONS_UPDATED, TOUR_PROGRESS_UPDATED } from '../../utils/eventBus'
import {
  buildIntegrationTabs,
  filterAvailableByTab,
  integrationTypeBadge,
  NATIVE_STORE_PROVIDER_KEY,
  resolveTabForMeta
} from '../../utils/integrationCatalogTabs'

const props = defineProps({
  modelValue: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'select', 'created'])

const toast = useToast()
const appStore = useAppStore()
const step = ref(1)
/** @type {import('vue').Ref<'create' | 'connect'>} */
const flow = ref('connect')
const activeTab = ref('stores')
const loading = ref(false)
const creating = ref(false)
const error = ref(null)
const availableIntegrations = ref([])
const failedLogos = ref([])
const storeName = ref('')
const storeNameTouched = ref(false)

const createStoreHighlights = [
  {
    title: 'Catálogo',
    body: 'Productos y variantes listos para sincronizar.',
    icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4'
  },
  {
    title: 'Órdenes',
    body: 'Pedidos y ventas desde el mismo conector.',
    icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2'
  },
  {
    title: 'Agentes',
    body: 'Tu tienda queda disponible para discovery y chat.',
    icon: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z'
  }
]

const gravityMeta = computed(() =>
  availableIntegrations.value.find((m) => m.key === NATIVE_STORE_PROVIDER_KEY) ?? null
)

const integrationTabs = computed(() =>
  buildIntegrationTabs(availableIntegrations.value, []).filter((t) => t.id === 'stores' || t.id === 'apis')
)

const filteredAvailable = computed(() =>
  filterAvailableByTab(availableIntegrations.value, activeTab.value).filter(
    (m) => m.key !== NATIVE_STORE_PROVIDER_KEY
  )
)

function setLogoFailed(url) {
  if (url && !failedLogos.value.includes(url)) {
    failedLogos.value = [...failedLogos.value, url]
  }
}

function logoSrc(meta) {
  const url = meta?.logoUrl
  if (url && !failedLogos.value.includes(url)) return url
  return '/images/avatar-default.svg'
}

async function loadAvailable() {
  loading.value = true
  error.value = null
  try {
    const list = await appStore.fetchAvailableIntegrations()
    availableIntegrations.value = Array.isArray(list) ? list : []
  } catch (e) {
    error.value = e?.message || 'No se pudieron cargar las integraciones'
    availableIntegrations.value = []
  } finally {
    loading.value = false
  }
}

async function goToCreateStep() {
  flow.value = 'create'
  step.value = 2
  storeName.value = ''
  storeNameTouched.value = false
  await loadAvailable()
}

async function goToConnectStep() {
  flow.value = 'connect'
  step.value = 2
  activeTab.value = 'stores'
  await loadAvailable()
}

function selectIntegration(meta) {
  emit('select', { meta, tab: resolveTabForMeta(meta) })
}

/**
 * Same connect path as IntegrationConnectEditSlideOvers.submitConnect for Gravity
 * (settings: [] in gravity.integration.yaml → empty settings object).
 */
async function submitCreateStore() {
  storeNameTouched.value = true
  const name = storeName.value?.trim()
  if (!name) {
    toast.error('El nombre de la tienda es requerido')
    return
  }
  const meta = gravityMeta.value
  if (!meta) {
    toast.error('No está disponible el conector Gravity')
    return
  }
  creating.value = true
  try {
    const result = await apiService.createIntegration({
      provider: meta.key,
      name,
      settings: {}
    })
    let my = null
    try {
      my = await apiService.getMyIntegrations()
    } catch {
      /* list refresh is best-effort */
    }
    eventBus.emit(MY_INTEGRATIONS_UPDATED, { my })
    eventBus.emit(TOUR_PROGRESS_UPDATED)
    toast.success('Integración conectada correctamente')
    emit('created', { meta, name, result })
  } catch (e) {
    const msg = e.response?.data?.error || e.message || 'Error al conectar'
    toast.error(msg)
  } finally {
    creating.value = false
  }
}

function lockBodyScroll(lock) {
  if (typeof document === 'undefined') return
  document.body.style.overflow = lock ? 'hidden' : ''
}

watch(
  () => props.modelValue,
  (open) => {
    lockBodyScroll(open)
    if (open) {
      step.value = 1
      flow.value = 'connect'
      activeTab.value = 'stores'
      storeName.value = ''
      storeNameTouched.value = false
      creating.value = false
      error.value = null
    }
  },
  { immediate: true }
)

onUnmounted(() => {
  lockBodyScroll(false)
})
</script>

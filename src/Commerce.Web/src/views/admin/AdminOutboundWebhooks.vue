<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Webhooks</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Notificaciones HTTPS salientes cuando ocurre un evento (p. ej. pedido creado)
            <span
              v-if="currentWorkspaceLabel"
              class="block sm:inline sm:before:content-['·'] sm:before:mx-1 text-gray-500 dark:text-gray-500"
            >
              {{ currentWorkspaceLabel }}
            </span>
          </p>
          <p class="mt-1 text-xs text-gray-500 dark:text-gray-500">
            Distinto del monitor de webhooks entrantes: aquí configuras URLs a las que enviamos el payload del evento.
          </p>
        </div>
        <button
          v-if="workspaceId"
          type="button"
          @click="openCreate"
          class="inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white transition-colors min-h-[44px] touch-manipulation shrink-0"
        >
          <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Nuevo webhook
        </button>
      </div>

      <div
        v-if="!workspaceId"
        class="p-6 rounded-xl border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-900/20 text-amber-900 dark:text-amber-200"
      >
        <p class="font-medium">Selecciona un workspace</p>
        <p class="mt-1 text-sm opacity-90">
          Los webhooks salientes se definen por workspace. Usa el selector de workspace en la barra superior y vuelve a
          esta página.
        </p>
      </div>

      <template v-else>
        <div v-if="loading" class="space-y-3 sm:space-y-4">
          <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
        </div>

        <div
          v-else-if="error"
          class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
        >
          <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
        </div>

        <div
          v-else-if="!hooks.length"
          class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
        >
          <p class="text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay webhooks configurados</p>
          <p class="mt-1 text-sm text-gray-400 dark:text-gray-500">
            Añade una URL HTTPS para notificaciones de pedido (creado y/o actualizado, etc.):
            <code class="text-xs">ChannelSaleOrder.*.v1</code>
          </p>
          <button
            type="button"
            @click="openCreate"
            class="mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white"
          >
            Nuevo webhook
          </button>
        </div>

        <div v-else class="space-y-3 md:space-y-0">
          <div class="md:hidden space-y-3">
            <div
              v-for="h in hooks"
              :key="h.id"
              class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm"
            >
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0 flex-1">
                  <p class="font-semibold text-gray-900 dark:text-white truncate">{{ h.name }}</p>
                  <p class="mt-0.5 text-xs font-mono text-primary-600 dark:text-primary-400 truncate">{{ h.domainEventType }}</p>
                  <p class="mt-1 text-xs text-gray-500 dark:text-gray-400 break-all">{{ h.targetUrl }}</p>
                  <span
                    :class="[
                      'inline-block mt-2 px-2 py-0.5 text-xs font-medium rounded-full',
                      h.isEnabled
                        ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                        : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                    ]"
                  >
                    {{ h.isEnabled ? 'Activo' : 'Desactivado' }}
                  </span>
                </div>
                <div class="inline-flex items-center gap-1 shrink-0">
                  <ActionIconButton action="edit" title="Editar webhook" @click="openEdit(h)" />
                  <ActionIconButton action="delete" title="Eliminar webhook" @click="confirmDelete(h)" />
                </div>
              </div>
            </div>
          </div>

          <div class="hidden md:block bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-700/50">
                <tr>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Nombre
                  </th>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Evento
                  </th>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    URL
                  </th>
                  <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Estado
                  </th>
                  <th class="px-4 lg:px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Acciones
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="h in hooks" :key="h.id" class="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                  <td class="px-4 lg:px-6 py-3 text-sm font-medium text-gray-900 dark:text-white">{{ h.name }}</td>
                  <td class="px-4 lg:px-6 py-3 text-xs font-mono text-primary-600 dark:text-primary-400">{{ h.domainEventType }}</td>
                  <td class="px-4 lg:px-6 py-3 text-xs text-gray-600 dark:text-gray-300 max-w-md truncate" :title="h.targetUrl">
                    {{ h.targetUrl }}
                  </td>
                  <td class="px-4 lg:px-6 py-3">
                    <span
                      :class="[
                        'inline-flex px-2 py-0.5 text-xs font-medium rounded-full',
                        h.isEnabled
                          ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                          : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                      ]"
                    >
                      {{ h.isEnabled ? 'Activo' : 'Desactivado' }}
                    </span>
                  </td>
                  <td class="px-4 lg:px-6 py-3 text-right">
                    <div class="inline-flex items-center justify-end gap-1">
                      <ActionIconButton action="edit" title="Editar webhook" @click="openEdit(h)" />
                      <ActionIconButton action="delete" title="Eliminar webhook" @click="confirmDelete(h)" />
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </template>
    </main>

    <Teleport to="body">
      <Transition name="wh-slide-overlay">
        <div
          v-if="formOpen"
          class="fixed inset-0 bg-black/50 z-50"
          aria-hidden="true"
          @click="closeForm"
        />
      </Transition>

      <Transition name="wh-slide-panel">
        <aside
          v-if="formOpen"
          class="fixed inset-y-0 right-0 z-[60] h-full w-full max-w-2xl bg-white dark:bg-gray-800 shadow-2xl flex flex-col border-l border-gray-200 dark:border-gray-700"
          role="dialog"
          aria-modal="true"
          aria-labelledby="wh-form-title"
          @click.stop
        >
          <div
            class="shrink-0 px-4 sm:px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between gap-3"
          >
            <h3 id="wh-form-title" class="text-lg font-semibold text-gray-900 dark:text-white truncate pr-2">
              {{ editingId ? 'Editar webhook' : 'Nuevo webhook' }}
            </h3>
            <button
              type="button"
              class="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors shrink-0"
              aria-label="Cerrar"
              @click="closeForm"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="flex-1 overflow-y-auto px-4 sm:px-6 py-5 space-y-5">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300"
                >Nombre <span class="text-red-500" aria-hidden="true">*</span></label
              >
              <input
                v-model="form.name"
                type="text"
                :class="inputClassName"
                placeholder="Ej. ERP producción"
                autocomplete="off"
                @blur="onTouched('name')"
              />
              <p
                v-if="showNameError"
                class="mt-1 text-xs text-red-600 dark:text-red-400"
                role="status"
              >
                Requerido
              </p>
            </div>
            <div>
              <label
                class="block text-sm font-medium text-gray-700 dark:text-gray-300"
                for="wh-events-group"
                >Eventos de item a enviar a esta URL
                <span class="text-red-500" aria-hidden="true">*</span></label
              >
              <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-500">
                Puedes combinar creado, actualizado y eliminación. La API acepta alias
                <code class="text-[11px]">item_created</code>, <code class="text-[11px]">item_updated</code>,
                <code class="text-[11px]">item_deleted</code> y nombres canónicos
                <code class="text-[11px]">ChannelSaleOrder.*.v1</code>.
              </p>
              <div
                id="wh-events-group"
                :class="eventsGroupClass"
                tabindex="-1"
                @focusout="onEventsFocusOut"
              >
                <label
                  v-for="opt in orderEventCheckboxes"
                  :key="opt.value"
                  class="flex items-start gap-2.5 text-sm text-gray-800 dark:text-gray-200 cursor-pointer"
                >
                  <input
                    v-model="form.orderEventTypes"
                    type="checkbox"
                    :value="opt.value"
                    class="mt-0.5 h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                    @change="touchEventsField"
                  />
                  <span
                    >{{ opt.label
                    }}<span v-if="opt.hint" class="block text-xs text-gray-500 dark:text-gray-500 font-mono mt-0.5">{{
                      opt.hint
                    }}</span></span
                  >
                </label>
              </div>
              <p
                v-if="showEventsError"
                class="mt-1.5 text-xs text-red-600 dark:text-red-400"
                role="status"
              >
                Requerido: elegí al menos un evento
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300" for="wh-target-url"
                >URL destino <span class="text-red-500" aria-hidden="true">*</span></label
              >
              <input
                id="wh-target-url"
                v-model="form.targetUrl"
                type="text"
                inputmode="url"
                autocapitalize="none"
                spellcheck="false"
                :class="inputClassUrl"
                placeholder="https://api.tudominio.com/hooks/orders"
                @blur="onTouched('url')"
              />
              <p
                v-if="showUrlError"
                class="mt-1 text-xs text-red-600 dark:text-red-400"
                role="status"
              >
                {{ urlErrorText }}
              </p>
              <p
                v-else-if="urlHttpsWarning"
                class="mt-1 text-xs text-amber-700 dark:text-amber-400/90"
                role="status"
              >
                {{ urlHttpsWarning }}
              </p>
            </div>

            <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
              <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50">
                <h4 class="text-sm font-semibold text-gray-900 dark:text-white">Headers personalizados</h4>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                  Headers HTTP adicionales que se enviarán en cada petición a la API (nombre y valor).
                </p>
              </div>
              <div class="p-4 space-y-4">
                <div
                  v-for="(row, idx) in headerRows"
                  :key="idx"
                  class="flex flex-wrap items-center gap-3 p-4 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50"
                >
                  <input
                    v-model="row.name"
                    type="text"
                    placeholder="Nombre (ej. X-API-Key)"
                    autocomplete="off"
                    class="flex-1 min-w-[120px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                  />
                  <input
                    v-model="row.value"
                    type="text"
                    placeholder="Valor"
                    autocomplete="off"
                    class="flex-1 min-w-[120px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                  />
                  <button
                    type="button"
                    class="p-1.5 text-gray-500 hover:text-red-600 dark:hover:text-red-400 rounded transition-colors shrink-0"
                    :disabled="headerRows.length <= 1"
                    :class="{ 'opacity-50 cursor-not-allowed': headerRows.length <= 1 }"
                    title="Eliminar header"
                    aria-label="Eliminar header"
                    @click="removeHeaderRow(idx)"
                  >
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                      />
                    </svg>
                  </button>
                </div>
                <button
                  type="button"
                  class="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-dashed border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors"
                  @click="addHeaderRow"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                  </svg>
                  Agregar header
                </button>
              </div>
            </div>

            <div class="flex items-center justify-between gap-4 pt-1 border-t border-gray-100 dark:border-gray-700/80">
              <div class="min-w-0 pr-2">
                <p class="text-sm font-medium text-gray-900 dark:text-white">Webhook activo</p>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Si está desactivado, no se enviarán eventos a esta URL.</p>
              </div>
              <label
                class="inline-flex h-8 w-14 shrink-0 cursor-pointer flex-row items-center rounded-full p-0.5 transition-colors focus-within:outline focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-primary-500"
                :class="form.isEnabled ? 'bg-primary-600' : 'bg-gray-300 dark:bg-gray-600'"
              >
                <input v-model="form.isEnabled" type="checkbox" class="sr-only" />
                <span
                  class="pointer-events-none h-7 w-7 rounded-full bg-white shadow transition-[margin] duration-200 ease-out"
                  :class="form.isEnabled ? 'ml-auto' : 'ml-0'"
                />
              </label>
            </div>
          </div>

          <div
            class="shrink-0 px-4 sm:px-6 py-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50/80 dark:bg-gray-900/40 flex flex-col-reverse sm:flex-row sm:justify-end gap-3"
          >
            <button
              type="button"
              class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] text-gray-700 dark:text-gray-300 hover:bg-gray-200/80 dark:hover:bg-gray-700 rounded-lg font-medium"
              @click="closeForm"
            >
              Cancelar
            </button>
            <button
              type="button"
              :disabled="formBusy || !isFormValid"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg font-medium text-white',
                formBusy || !isFormValid
                  ? 'bg-gray-400 cursor-not-allowed'
                  : 'bg-primary-600 hover:bg-primary-700'
              ]"
              :title="!isFormValid ? 'Completá los campos requeridos correctamente' : ''"
              @click="submitForm"
            >
              {{ formBusy ? 'Guardando…' : 'Guardar' }}
            </button>
          </div>
        </aside>
      </Transition>
    </Teleport>

    <Teleport to="body">
      <div
        v-if="deleteTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar webhook</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Eliminar "{{ deleteTarget?.name }}"? No se podrá deshacer.
          </p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button
              type="button"
              class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              @click="deleteTarget = null"
            >
              Cancelar
            </button>
            <button
              type="button"
              :disabled="deleteBusy"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg font-medium text-white',
                deleteBusy ? 'bg-gray-400' : 'bg-red-600 hover:bg-red-700'
              ]"
              @click="doDelete"
            >
              {{ deleteBusy ? 'Eliminando…' : 'Eliminar' }}
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
import ActionIconButton from '../../components/common/ActionIconButton.vue'

const ACTIVE_WORKSPACE_KEY = 'active_workspace_id'
const ACTIVE_WORKSPACE_NAME_KEY = 'active_workspace_name'

const ORDER_V1 = {
  created: 'ChannelSaleOrder.Created.v1',
  updated: 'ChannelSaleOrder.Updated.v1',
  deleted: 'ChannelSaleOrder.Deleted.v1'
}

const orderEventCheckboxes = [
  { value: ORDER_V1.created, label: 'Item creado', hint: 'item_created' },
  { value: ORDER_V1.updated, label: 'Item actualizado', hint: 'item_updated' },
  { value: ORDER_V1.deleted, label: 'Item eliminado', hint: 'item_deleted' }
]

/** Altura y estilo unificados para input/select del panel (el nativo select suele verse distinto con solo py-2). */
const whFieldClass =
  'box-border h-10 w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 text-sm text-gray-900 dark:text-gray-100 shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500/30 focus:border-primary-500'
const whFieldMonoClass = `${whFieldClass} font-mono`

const toast = useToast()
const hooks = ref([])
const currentWorkspaceLabel = ref('')
const workspaceId = computed(() => {
  if (typeof localStorage === 'undefined') return ''
  return localStorage.getItem(ACTIVE_WORKSPACE_KEY)?.trim() || ''
})

const loading = ref(true)
const error = ref(null)
const formOpen = ref(false)
const editingId = ref(null)
const formBusy = ref(false)
const deleteTarget = ref(null)
const deleteBusy = ref(false)

const form = ref({
  name: '',
  orderEventTypes: [ORDER_V1.created],
  legacyOtherDomainEvents: '',
  targetUrl: '',
  isEnabled: true
})

/** Marca qué campos ya perdieron foco (para bordes rojos y "Requerido"). */
const formTouched = ref({
  name: false,
  url: false,
  events: false
})

function resetFormTouched() {
  formTouched.value = { name: false, url: false, events: false }
}

function onTouched(field) {
  if (field === 'name') formTouched.value.name = true
  if (field === 'url') formTouched.value.url = true
}

function touchEventsField() {
  formTouched.value.events = true
}

/** Al salir del bloque de checkboxes: marcar “eventos” tocados. */
function onEventsFocusOut(e) {
  const g = e.currentTarget
  if (g && !g.contains(e.relatedTarget)) {
    formTouched.value.events = true
  }
}

/**
 * @returns {{ state: 'empty' | 'invalid' | 'http' | 'ok', href?: string }}
 */
function analyzeTargetUrl(raw) {
  const t = (raw ?? '').trim()
  if (!t) return { state: 'empty' }
  let u
  try {
    u = new URL(t)
  } catch {
    const looksLikeProtocol = /^[a-z][a-z0-9+.-]*:\/\//i.test(t)
    if (looksLikeProtocol) {
      return { state: 'invalid' }
    }
    try {
      u = new URL('https://' + t.replace(/^\/+/, ''))
    } catch {
      return { state: 'invalid' }
    }
  }
  if (u.protocol === 'http:') return { state: 'http', href: u.href }
  if (u.protocol !== 'https:') return { state: 'invalid' }
  return { state: 'ok', href: u.href }
}

const targetUrlAnalysis = computed(() => analyzeTargetUrl(form.value.targetUrl))

const nameIsValid = computed(() => (form.value.name ?? '').trim().length > 0)
const eventsAreValid = computed(() => {
  const s = buildDomainEventTypeFromForm()
  return Boolean(s && s.trim().length)
})
const urlIsValid = computed(() => {
  const s = targetUrlAnalysis.value.state
  return s === 'ok' || s === 'http'
})

const isFormValid = computed(
  () => nameIsValid.value && urlIsValid.value && eventsAreValid.value
)

const showNameError = computed(() => formTouched.value.name && !nameIsValid.value)
const showEventsError = computed(() => formTouched.value.events && !eventsAreValid.value)
const showUrlError = computed(() => {
  if (!formTouched.value.url) return false
  const s = targetUrlAnalysis.value.state
  return s === 'empty' || s === 'invalid'
})

const urlErrorText = computed(() => {
  const s = targetUrlAnalysis.value.state
  if (s === 'empty') return 'Requerido'
  if (s === 'invalid') return 'Ingresá una URL válida (http:// o https://)'
  return ''
})

const urlHttpsWarning = computed(() => {
  if (targetUrlAnalysis.value.state !== 'http') return ''
  return 'Se recomienda usar https:// en producción. HTTP también está permitido (entornos internos o de prueba).'
})

const inputClassName = computed(() => {
  const base = whFieldClass
  if (showNameError.value) {
    return `${base} border-red-500 dark:border-red-500 focus:border-red-500 focus:ring-red-500/30`
  }
  return base
})

const inputClassUrl = computed(() => {
  const base = whFieldMonoClass
  if (showUrlError.value) {
    return `${base} border-red-500 dark:border-red-500 focus:border-red-500 focus:ring-red-500/30`
  }
  if (targetUrlAnalysis.value.state === 'http') {
    return `${base} border-amber-500 dark:border-amber-500/70 focus:border-amber-500 focus:ring-amber-500/30`
  }
  return base
})

const eventsGroupClass = computed(() => {
  const base = 'mt-2 space-y-2 rounded-lg border p-3 outline-none'
  return showEventsError.value
    ? `${base} border-red-500 dark:border-red-500`
    : `${base} border-gray-200 dark:border-gray-600`
})

/** Filas nombre/valor para headers HTTP (se serializan a objeto JSON en guardar). */
const headerRows = ref([{ name: '', value: '' }])

function refreshWorkspaceLabel() {
  if (typeof localStorage === 'undefined') {
    currentWorkspaceLabel.value = ''
    return
  }
  const name = localStorage.getItem(ACTIVE_WORKSPACE_NAME_KEY)?.trim()
  const id = localStorage.getItem(ACTIVE_WORKSPACE_KEY)?.trim()
  currentWorkspaceLabel.value = name || id || ''
}

async function loadData() {
  refreshWorkspaceLabel()
  const wid = workspaceId.value
  if (!wid) {
    hooks.value = []
    loading.value = false
    error.value = null
    return
  }
  loading.value = true
  error.value = null
  try {
    hooks.value = await apiService.listOutboundEventHooks(wid)
  } catch (e) {
    error.value = e.response?.data?.detail || e.response?.data?.title || e.message || 'Error al cargar webhooks'
  } finally {
    loading.value = false
  }
}

function rowsFromHeadersJson(jsonStr) {
  const empty = [{ name: '', value: '' }]
  if (!jsonStr || !String(jsonStr).trim() || String(jsonStr).trim() === '{}') {
    return empty
  }
  try {
    const obj = JSON.parse(String(jsonStr))
    if (!obj || typeof obj !== 'object' || Array.isArray(obj)) {
      return empty
    }
    const pairs = Object.entries(obj).map(([name, value]) => ({
      name: String(name),
      value: value == null ? '' : String(value)
    }))
    return pairs.length ? pairs : empty
  } catch {
    return empty
  }
}

function addHeaderRow() {
  headerRows.value.push({ name: '', value: '' })
}

function removeHeaderRow(index) {
  if (headerRows.value.length <= 1) return
  headerRows.value.splice(index, 1)
}

function buildHeadersJsonFromRows() {
  const o = {}
  for (const r of headerRows.value) {
    const k = r.name?.trim()
    if (!k) continue
    o[k] = r.value ?? ''
  }
  return JSON.stringify(o)
}

function tokenizeEventField(str) {
  if (!str || !String(str).trim()) return []
  return String(str)
    .split(/[,|]/)
    .map(s => s.trim())
    .filter(Boolean)
}

function mapOrderToken(t) {
  const s = String(t)
  const k = s.toLowerCase()
  if (k === 'item_created' || k === 'order_created' || s === ORDER_V1.created) return ORDER_V1.created
  if (k === 'item_updated' || k === 'order_updated' || s === ORDER_V1.updated) return ORDER_V1.updated
  if (k === 'item_deleted' || k === 'order_deleted' || s === ORDER_V1.deleted) return ORDER_V1.deleted
  if (s === ORDER_V1.created || s === ORDER_V1.updated || s === ORDER_V1.deleted) return s
  return null
}

function parseDomainEventTypeField(raw) {
  const order = []
  const other = []
  for (const t of tokenizeEventField(raw)) {
    const o = mapOrderToken(t)
    if (o) {
      if (!order.includes(o)) order.push(o)
    } else {
      other.push(t)
    }
  }
  return { orderTokens: order, otherTokens: other }
}

function buildDomainEventTypeFromForm() {
  const order = (form.value.orderEventTypes && form.value.orderEventTypes.length
    ? form.value.orderEventTypes
    : [])
  const legacy = tokenizeEventField(form.value.legacyOtherDomainEvents)
  const all = []
  for (const x of order) {
    if (x && !all.includes(x)) all.push(x)
  }
  for (const x of legacy) {
    if (x && !all.includes(x)) all.push(x)
  }
  return all.join(', ')
}

function openCreate() {
  editingId.value = null
  form.value = {
    name: '',
    orderEventTypes: [ORDER_V1.created],
    legacyOtherDomainEvents: '',
    targetUrl: '',
    isEnabled: true
  }
  headerRows.value = [{ name: '', value: '' }]
  resetFormTouched()
  formOpen.value = true
}

function openEdit(h) {
  const { orderTokens, otherTokens } = parseDomainEventTypeField(h.domainEventType)
  editingId.value = h.id
  const orderEventTypes = orderTokens.length
    ? orderTokens
    : otherTokens.length
      ? []
      : [ORDER_V1.created]
  form.value = {
    name: h.name,
    orderEventTypes,
    legacyOtherDomainEvents: otherTokens.join(', '),
    targetUrl: h.targetUrl,
    isEnabled: h.isEnabled
  }
  headerRows.value = rowsFromHeadersJson(h.headersJson)
  resetFormTouched()
  formOpen.value = true
}

function closeForm() {
  formOpen.value = false
  editingId.value = null
  headerRows.value = [{ name: '', value: '' }]
  resetFormTouched()
}

async function submitForm() {
  if (!isFormValid.value) return
  const wid = workspaceId.value
  if (!wid) return
  const name = form.value.name?.trim()
  const url = form.value.targetUrl?.trim()
  const domainEventType = buildDomainEventTypeFromForm()
  const headersJson = buildHeadersJsonFromRows()
  formBusy.value = true
  try {
    if (editingId.value) {
      await apiService.updateOutboundEventHook({
        id: editingId.value,
        workspaceId: wid,
        domainEventType,
        name,
        targetUrl: url,
        headersJson,
        isEnabled: form.value.isEnabled
      })
      toast.success('Webhook actualizado')
    } else {
      const created = await apiService.createOutboundEventHook({
        workspaceId: wid,
        domainEventType,
        name,
        targetUrl: url,
        headersJson,
        isEnabled: form.value.isEnabled
      })
      toast.success('Webhook creado')
      if (created.pingWarning) {
        toast.warning(created.pingWarning)
      }
    }
    closeForm()
    await loadData()
  } catch (e) {
    toast.error(e.response?.data?.detail || e.response?.data?.title || e.message || 'Error al guardar')
  } finally {
    formBusy.value = false
  }
}

function confirmDelete(h) {
  deleteTarget.value = h
}

async function doDelete() {
  if (!deleteTarget.value || deleteBusy.value) return
  const wid = workspaceId.value
  if (!wid) return
  deleteBusy.value = true
  try {
    await apiService.deleteOutboundEventHook(deleteTarget.value.id, wid)
    deleteTarget.value = null
    await loadData()
    toast.success('Webhook eliminado')
  } catch (e) {
    toast.error(e.response?.data?.detail || e.response?.data?.title || e.message || 'Error al eliminar')
  } finally {
    deleteBusy.value = false
  }
}

onMounted(loadData)
</script>

<style scoped>
.wh-slide-overlay-enter-active,
.wh-slide-overlay-leave-active {
  transition: opacity 0.25s ease;
}

.wh-slide-overlay-enter-from,
.wh-slide-overlay-leave-to {
  opacity: 0;
}

.wh-slide-panel-enter-active {
  transition: transform 0.28s ease-out;
}

.wh-slide-panel-leave-active {
  transition: transform 0.22s ease-in;
}

.wh-slide-panel-enter-from,
.wh-slide-panel-leave-to {
  transform: translateX(100%);
}
</style>

<template>
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
        v-if="modelValue"
        class="fixed inset-0 z-50 bg-black/50"
        aria-hidden="true"
        @click="close"
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
        v-if="modelValue && row"
        class="fixed top-0 right-0 z-[51] h-full min-h-0 w-full max-w-lg sm:max-w-xl lg:max-w-2xl bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
        role="dialog"
        aria-modal="true"
        aria-labelledby="outbox-detail-title"
        @click.stop
      >
        <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <h2 id="outbox-detail-title" class="text-lg font-semibold text-gray-900 dark:text-white">
                Detalle outbox
              </h2>
              <p class="mt-1 text-xs font-mono text-gray-500 dark:text-gray-400 break-all">
                {{ row.id }}
              </p>
              <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">
                {{ kindLabel }}
              </p>
            </div>
            <button
              type="button"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
              aria-label="Cerrar"
              @click="close"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-6 space-y-4">
          <dl class="grid grid-cols-1 gap-3 text-sm">
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Tipo de mensaje</dt>
              <dd class="mt-0.5 font-mono text-xs text-gray-900 dark:text-gray-100 break-all">
                {{ row.messageType || '—' }}
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                {{ entityLabel }}
              </dt>
              <dd class="mt-0.5 font-mono text-xs text-gray-900 dark:text-gray-100 break-all">
                {{ entityId || '—' }}
              </dd>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Estado</dt>
                <dd class="mt-0.5">
                  <span
                    :class="[
                      'inline-flex px-2 py-0.5 rounded-full text-xs font-medium',
                      row.processedAt
                        ? 'bg-emerald-100 dark:bg-emerald-900/40 text-emerald-800 dark:text-emerald-200'
                        : 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300'
                    ]"
                  >
                    {{ row.processedAt ? 'Procesado' : 'Pendiente' }}
                  </span>
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Intentos</dt>
                <dd class="mt-0.5 text-gray-900 dark:text-gray-100">{{ row.attemptCount }}</dd>
              </div>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Procesado</dt>
              <dd class="mt-0.5 text-gray-900 dark:text-gray-100">{{ formatDate(row.processedAt) }}</dd>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Dispatch después (UTC)</dt>
              <dd class="mt-0.5 text-gray-900 dark:text-gray-100">{{ formatDate(row.dispatchAfterUtc) }}</dd>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bloqueado hasta (UTC)</dt>
              <dd class="mt-0.5 text-gray-900 dark:text-gray-100">{{ formatDate(row.lockedUntilUtc) }}</dd>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Creado</dt>
              <dd class="mt-0.5 text-gray-900 dark:text-gray-100">{{ formatDate(row.createdAt) }}</dd>
            </div>
            <div>
              <dt class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Actualizado</dt>
              <dd class="mt-0.5 text-gray-900 dark:text-gray-100">{{ formatDate(row.updatedAt) }}</dd>
            </div>
            <div v-if="row.lastError">
              <dt class="text-xs font-medium text-red-600 dark:text-red-400 uppercase tracking-wide">Último error</dt>
              <dd class="mt-0.5 text-sm text-red-800 dark:text-red-200 whitespace-pre-wrap break-words">
                {{ row.lastError }}
              </dd>
            </div>
          </dl>

          <div>
            <div class="flex items-center justify-between gap-2 mb-2">
              <h3 class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Payload (JSON)</h3>
              <button
                type="button"
                class="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline"
                @click="copyPayload"
              >
                Copiar
              </button>
            </div>
            <pre
              class="text-xs font-mono p-3 rounded-lg bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-700 overflow-x-auto max-h-[min(50vh,24rem)] text-gray-800 dark:text-gray-200"
            ><code>{{ formattedPayload }}</code></pre>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed } from 'vue'
import { useToast } from 'vue-toastification'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  /** Fila normalizada del API (catalog u orders outbox) */
  row: { type: Object, default: null },
  /** 'catalog' | 'orders' */
  outboxKind: { type: String, default: 'catalog' }
})

const emit = defineEmits(['update:modelValue'])

const toast = useToast()

const kindLabel = computed(() =>
  props.outboxKind === 'orders' ? 'Outbox de pedidos' : 'Outbox de productos (catálogo)'
)

const entityLabel = computed(() =>
  props.outboxKind === 'orders' ? 'TemplateProject sale order id' : 'TemplateProject catalog product id'
)

const entityId = computed(() => {
  if (!props.row) return null
  return props.outboxKind === 'orders'
    ? props.row.channelSaleOrderId
    : props.row.channelCatalogProductId
})

const formattedPayload = computed(() => {
  const raw = props.row?.payloadJson
  if (raw == null || raw === '') return '—'
  const s = String(raw)
  try {
    return JSON.stringify(JSON.parse(s), null, 2)
  } catch {
    return s
  }
})

function formatDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString()
}

function close() {
  emit('update:modelValue', false)
}

async function copyPayload() {
  const text = formattedPayload.value
  if (text === '—') return
  try {
    await navigator.clipboard.writeText(text)
    toast.success('Payload copiado')
  } catch {
    toast.error('No se pudo copiar')
  }
}
</script>

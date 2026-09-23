<template>
  <section
    class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm overflow-hidden"
  >
    <!-- Toggle header -->
    <div class="flex items-center justify-between gap-4 p-4 sm:p-5">
      <div class="min-w-0">
        <p class="text-sm font-semibold text-gray-900 dark:text-white">
          ¿Tu producto tiene varios colores o tallas?
        </p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
          Ej: Misma zapatilla en negro y blanco, o en varias tallas.
        </p>
      </div>
      <FormToggle v-model="enabled" aria-label="Activar variaciones" />
    </div>

    <!-- Simple inventory when no variations -->
    <div
      v-if="!enabled"
      class="border-t border-gray-100 dark:border-gray-700 px-4 sm:px-5 py-4 space-y-4"
    >
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">SKU</label>
          <input
            v-model="simple.sku"
            type="text"
            maxlength="64"
            class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2.5 text-gray-900 dark:text-white font-mono"
            placeholder="Opcional"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Stock</label>
          <input
            v-model="simple.stock"
            type="number"
            min="0"
            class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2.5 text-gray-900 dark:text-white"
            placeholder="0"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Precio S/</label>
          <input
            v-model="simple.price"
            type="text"
            inputmode="decimal"
            class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2.5 text-gray-900 dark:text-white font-semibold"
            placeholder="0.00"
          />
        </div>
      </div>
    </div>

    <!-- Variations editor -->
    <div v-else class="border-t border-gray-100 dark:border-gray-700 px-4 sm:px-5 py-4 space-y-5">
      <div>
        <p class="text-sm font-medium text-gray-800 dark:text-gray-200 mb-2">¿Qué varía? Elige:</p>
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
          <button
            v-for="opt in kindOptions"
            :key="opt.id"
            type="button"
            class="rounded-xl border px-3 py-3 text-left transition-colors touch-manipulation"
            :class="
              kind === opt.id
                ? 'bg-gray-900 dark:bg-white border-gray-900 dark:border-white text-white dark:text-gray-900'
                : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-600 text-gray-900 dark:text-white hover:border-gray-300'
            "
            @click="kind = opt.id"
          >
            <span class="block text-sm font-semibold">{{ opt.label }}</span>
            <span
              class="block text-[11px] mt-0.5"
              :class="kind === opt.id ? 'text-gray-300 dark:text-gray-500' : 'text-gray-500'"
            >
              {{ opt.hint }}
            </span>
          </button>
        </div>
      </div>

      <!-- Colors editor -->
      <div v-if="kind === 'colors' || kind === 'both'">
        <div class="flex items-center justify-between gap-2 mb-3">
          <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Tus colores</h3>
          <span class="text-xs text-gray-500">{{ colors.length }} color{{ colors.length === 1 ? '' : 'es' }}</span>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div
            v-for="(c, idx) in colors"
            :key="c.id"
            class="rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-900/40 p-3 space-y-3"
          >
            <div class="flex gap-3">
              <div
                class="w-14 h-14 shrink-0 rounded-lg border border-dashed border-gray-300 dark:border-gray-600 flex items-center justify-center overflow-hidden bg-white dark:bg-gray-800"
              >
                <img
                  v-if="c.imageUrl || productImageUrl"
                  :src="c.imageUrl || productImageUrl"
                  alt=""
                  class="w-full h-full object-cover"
                />
                <svg v-else class="w-5 h-5 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="1.5"
                    d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"
                  />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </div>
              <div class="flex-1 min-w-0 space-y-2">
                <div class="flex items-start gap-2">
                  <div class="relative flex-1">
                    <span
                      class="absolute left-2.5 top-1/2 -translate-y-1/2 h-3 w-3 rounded-full border border-gray-300 dark:border-gray-500"
                      :style="{ backgroundColor: c.swatch }"
                    />
                    <input
                      v-model="c.name"
                      type="text"
                      maxlength="40"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm pl-8 pr-3 py-2 text-gray-900 dark:text-white"
                      placeholder="Nombre del color"
                    />
                  </div>
                  <button
                    type="button"
                    class="p-2 rounded-lg text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                    :disabled="colors.length <= 1"
                    aria-label="Eliminar color"
                    @click="removeColor(idx)"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                      />
                    </svg>
                  </button>
                </div>
                <div v-if="kind === 'colors'" class="grid grid-cols-2 gap-2">
                  <div>
                    <label class="block text-[11px] text-gray-500 mb-0.5">Stock</label>
                    <input
                      v-model="c.stock"
                      type="number"
                      min="0"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-2.5 py-2 text-gray-900 dark:text-white"
                    />
                  </div>
                  <div>
                    <label class="block text-[11px] text-gray-500 mb-0.5">Precio S/</label>
                    <input
                      v-model="c.price"
                      type="text"
                      inputmode="decimal"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-2.5 py-2 text-gray-900 dark:text-white"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <button
          type="button"
          class="mt-3 w-full min-h-[44px] rounded-xl border border-dashed border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-600 dark:text-gray-300 hover:border-gray-400 hover:bg-gray-50 dark:hover:bg-gray-900/50 touch-manipulation"
          @click="addColor"
        >
          + Agregar otro color
        </button>
      </div>

      <!-- Sizes editor -->
      <div v-if="kind === 'sizes' || kind === 'both'">
        <div class="flex items-center justify-between gap-2 mb-3">
          <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Tus tallas</h3>
          <span class="text-xs text-gray-500">{{ sizes.length }} talla{{ sizes.length === 1 ? '' : 's' }}</span>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div
            v-for="(s, idx) in sizes"
            :key="s.id"
            class="rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-900/40 p-3"
          >
            <div class="flex items-start gap-2 mb-2">
              <input
                v-model="s.name"
                type="text"
                maxlength="16"
                class="flex-1 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2 text-gray-900 dark:text-white font-semibold"
                placeholder="Talla"
              />
              <button
                type="button"
                class="p-2 rounded-lg text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                :disabled="sizes.length <= 1"
                aria-label="Eliminar talla"
                @click="removeSize(idx)"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                  />
                </svg>
              </button>
            </div>
            <div v-if="kind === 'sizes'" class="grid grid-cols-2 gap-2">
              <div>
                <label class="block text-[11px] text-gray-500 mb-0.5">Stock</label>
                <input
                  v-model="s.stock"
                  type="number"
                  min="0"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-2.5 py-2 text-gray-900 dark:text-white"
                />
              </div>
              <div>
                <label class="block text-[11px] text-gray-500 mb-0.5">Precio S/</label>
                <input
                  v-model="s.price"
                  type="text"
                  inputmode="decimal"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-2.5 py-2 text-gray-900 dark:text-white"
                />
              </div>
            </div>
          </div>
        </div>
        <button
          type="button"
          class="mt-3 w-full min-h-[44px] rounded-xl border border-dashed border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-600 dark:text-gray-300 hover:border-gray-400 hover:bg-gray-50 dark:hover:bg-gray-900/50 touch-manipulation"
          @click="addSize"
        >
          + Agregar otra talla
        </button>
      </div>

      <!-- Summary -->
      <div v-if="versions.length" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden">
        <div class="flex flex-wrap items-center justify-between gap-2 px-3 py-2.5 bg-gray-50 dark:bg-gray-900/60 border-b border-gray-200 dark:border-gray-600">
          <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Resumen de versiones</h3>
          <span
            class="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-gray-900 dark:bg-white text-white dark:text-gray-900"
          >
            Tienes {{ versions.length }} versión{{ versions.length === 1 ? '' : 'es' }} de tu producto
          </span>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-sm min-w-[520px]">
            <thead>
              <tr class="text-left text-[11px] uppercase tracking-wide text-gray-400 border-b border-gray-100 dark:border-gray-700">
                <th class="px-3 py-2 font-medium">Foto</th>
                <th class="px-3 py-2 font-medium">Color</th>
                <th class="px-3 py-2 font-medium">Talla</th>
                <th class="px-3 py-2 font-medium">Stock disponible</th>
                <th class="px-3 py-2 font-medium">Precio</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="row in versions"
                :key="row.key"
                class="border-b border-gray-100 dark:border-gray-700 last:border-0"
              >
                <td class="px-3 py-2.5">
                  <div
                    class="w-10 h-10 rounded-lg overflow-hidden bg-gray-100 dark:bg-gray-700 flex items-center justify-center"
                  >
                    <img
                      v-if="row.imageUrl"
                      :src="row.imageUrl"
                      alt=""
                      class="w-full h-full object-cover"
                    />
                    <span v-else class="text-gray-300 text-xs">—</span>
                  </div>
                </td>
                <td class="px-3 py-2.5">
                  <span v-if="row.color" class="inline-flex items-center gap-1.5 text-gray-800 dark:text-gray-200">
                    <span
                      class="h-2.5 w-2.5 rounded-full border border-gray-300 dark:border-gray-500"
                      :style="{ backgroundColor: row.swatch }"
                    />
                    {{ row.color }}
                  </span>
                  <span v-else class="text-gray-400">—</span>
                </td>
                <td class="px-3 py-2.5 text-gray-800 dark:text-gray-200">
                  {{ row.size || '—' }}
                </td>
                <td class="px-3 py-2.5">
                  <input
                    :value="row.stock"
                    type="number"
                    min="0"
                    class="w-20 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-2 py-1.5 text-gray-900 dark:text-white"
                    @input="onVersionStock(row, $event.target.value)"
                  />
                </td>
                <td class="px-3 py-2.5">
                  <div class="relative w-28">
                    <span class="absolute left-2 top-1/2 -translate-y-1/2 text-xs text-gray-400">S/</span>
                    <input
                      :value="row.price"
                      type="text"
                      inputmode="decimal"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm pl-7 pr-2 py-1.5 text-gray-900 dark:text-white"
                      @input="onVersionPrice(row, $event.target.value)"
                    />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue'
import FormToggle from '../FormToggle.vue'

const props = defineProps({
  productImageUrl: { type: String, default: '' }
})

const kindOptions = [
  { id: 'colors', label: 'Colores', hint: 'Ej: Negro, Blanco' },
  { id: 'sizes', label: 'Tallas', hint: 'Ej: S, M, L' },
  { id: 'both', label: 'Colores y Tallas', hint: 'Ambos' }
]

const SWATCHES = ['#111827', '#f3f4f6', '#6b7280', '#1d4ed8', '#b91c1c', '#15803d', '#ca8a04', '#7c3aed']

let seq = 0
function uid() {
  seq += 1
  return `v-${Date.now()}-${seq}`
}

function makeColor(name = '', swatch, index = 0) {
  return {
    id: uid(),
    name,
    swatch: swatch || SWATCHES[index % SWATCHES.length],
    imageUrl: '',
    stock: '',
    price: ''
  }
}

function makeSize(name = '') {
  return { id: uid(), name, stock: '', price: '' }
}

const enabled = ref(false)
const kind = ref('colors')
const colors = ref([makeColor('Negro', '#111827', 0), makeColor('Blanco', '#f3f4f6', 1)])
const sizes = ref([makeSize('S'), makeSize('M'), makeSize('L')])
const simple = reactive({ sku: '', stock: '', price: '' })
const combo = reactive({})

function ensureCombo(key) {
  if (!combo[key]) combo[key] = { stock: '', price: '' }
  return combo[key]
}

const versions = computed(() => {
  if (!enabled.value) return []
  if (kind.value === 'colors') {
    return colors.value.map((c) => ({
      key: c.id,
      color: c.name || 'Sin nombre',
      size: null,
      swatch: c.swatch,
      imageUrl: c.imageUrl || props.productImageUrl,
      stock: c.stock,
      price: c.price,
      source: 'color',
      colorId: c.id,
      sizeId: null
    }))
  }
  if (kind.value === 'sizes') {
    return sizes.value.map((s) => ({
      key: s.id,
      color: null,
      size: s.name || '—',
      swatch: null,
      imageUrl: props.productImageUrl,
      stock: s.stock,
      price: s.price,
      source: 'size',
      colorId: null,
      sizeId: s.id
    }))
  }
  const rows = []
  for (const c of colors.value) {
    for (const s of sizes.value) {
      const key = `${c.id}__${s.id}`
      const cell = combo[key] || { stock: '', price: '' }
      rows.push({
        key,
        color: c.name || 'Sin nombre',
        size: s.name || '—',
        swatch: c.swatch,
        imageUrl: c.imageUrl || props.productImageUrl,
        stock: cell.stock,
        price: cell.price,
        source: 'combo',
        colorId: c.id,
        sizeId: s.id
      })
    }
  }
  return rows
})

watch(
  [colors, sizes, kind],
  () => {
    if (kind.value !== 'both') return
    for (const c of colors.value) {
      for (const s of sizes.value) {
        ensureCombo(`${c.id}__${s.id}`)
      }
    }
  },
  { deep: true, immediate: true }
)
function addColor() {
  colors.value.push(makeColor('', null, colors.value.length))
}

function removeColor(idx) {
  if (colors.value.length <= 1) return
  colors.value.splice(idx, 1)
}

function addSize() {
  sizes.value.push(makeSize(''))
}

function removeSize(idx) {
  if (sizes.value.length <= 1) return
  sizes.value.splice(idx, 1)
}

function onVersionStock(row, value) {
  if (row.source === 'color') {
    const c = colors.value.find((x) => x.id === row.colorId)
    if (c) c.stock = value
  } else if (row.source === 'size') {
    const s = sizes.value.find((x) => x.id === row.sizeId)
    if (s) s.stock = value
  } else if (row.source === 'combo') {
    ensureCombo(row.key).stock = value
  }
}

function onVersionPrice(row, value) {
  if (row.source === 'color') {
    const c = colors.value.find((x) => x.id === row.colorId)
    if (c) c.price = value
  } else if (row.source === 'size') {
    const s = sizes.value.find((x) => x.id === row.sizeId)
    if (s) s.price = value
  } else if (row.source === 'combo') {
    ensureCombo(row.key).price = value
  }
}

watch(enabled, (on) => {
  if (on && !kind.value) kind.value = 'colors'
})

function parseMoney(raw) {
  if (raw == null || raw === '') return 0
  const n = Number(String(raw).replace(',', '.').trim())
  return Number.isFinite(n) && n >= 0 ? n : 0
}

function parseStock(raw) {
  if (raw == null || raw === '') return 0
  const n = parseInt(String(raw).trim(), 10)
  return Number.isFinite(n) && n >= 0 ? n : 0
}

/**
 * Payload for create product — always ≥1 variation.
 * When variations toggle is off, returns a single default SKU from simple inventory.
 */
function toPayload(productName = '') {
  const fallbackName = String(productName || '').trim() || 'Default'
  const productImg = props.productImageUrl || null

  if (!enabled.value) {
    return [
      {
        name: fallbackName,
        variantName: 'Default',
        sku: simple.sku.trim() || null,
        basePrice: parseMoney(simple.price),
        stock: parseStock(simple.stock),
        imageUrl: productImg,
        options: []
      }
    ]
  }

  return versions.value.map((row) => {
    const options = []
    if (row.color) {
      options.push({ key: 'COLOR', name: 'Color', value: row.color })
    }
    if (row.size) {
      options.push({ key: 'SIZE', name: 'Talla', value: row.size })
    }
    const label = [row.color, row.size].filter(Boolean).join(' / ') || fallbackName
    return {
      name: label,
      variantName: label,
      sku: null,
      basePrice: parseMoney(row.price),
      stock: parseStock(row.stock),
      imageUrl: row.imageUrl || productImg,
      options
    }
  })
}

defineExpose({ toPayload })
</script>

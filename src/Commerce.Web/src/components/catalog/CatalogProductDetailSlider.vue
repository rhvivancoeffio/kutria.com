<template>
  <Teleport to="body">
    <!-- Backdrop -->
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
      />
    </Transition>

    <!-- Panel -->
    <Transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="translate-x-full"
      enter-to-class="translate-x-0"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="translate-x-0"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="modelValue"
        class="fixed top-0 right-0 z-[51] h-full min-h-0 w-full max-w-2xl sm:max-w-3xl lg:max-w-4xl xl:max-w-5xl bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
        @click.stop
      >
        <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <h2 class="text-lg font-semibold text-gray-900 dark:text-white truncate">Detalle de producto</h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400 truncate">
                {{ headerLine }}
              </p>
            </div>
            <button
              type="button"
              @click="close"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
              aria-label="Cerrar"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <!-- Tabs: mismo patrón que MeliFilteredCollectionMapping (slider «Categoría …» al mapear taxonomía) -->
        <div
          v-if="product && !loading && !error"
          class="shrink-0 border-b border-gray-200 dark:border-gray-700 px-4 sm:px-6"
          role="tablist"
          aria-label="Vistas del detalle"
        >
          <nav class="flex flex-wrap gap-4">
            <button
              id="tab-catalog-detail-data"
              type="button"
              role="tab"
              :aria-selected="activeDetailTab === 'data'"
              :class="[
                'pb-3 pt-3 px-1 border-b-2 font-medium text-sm transition-colors',
                activeDetailTab === 'data'
                  ? 'border-primary-500 text-primary-600 dark:text-primary-400'
                  : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
              ]"
              @click="activeDetailTab = 'data'"
            >
              Datos de producto y SKUs
            </button>
            <button
              id="tab-catalog-detail-channels"
              type="button"
              role="tab"
              :aria-selected="activeDetailTab === 'channels'"
              :class="[
                'pb-3 pt-3 px-1 border-b-2 font-medium text-sm transition-colors',
                activeDetailTab === 'channels'
                  ? 'border-primary-500 text-primary-600 dark:text-primary-400'
                  : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
              ]"
              @click="activeDetailTab = 'channels'"
            >
              Canales
            </button>
            <button
              id="tab-catalog-detail-json"
              type="button"
              role="tab"
              :aria-selected="activeDetailTab === 'json'"
              :class="[
                'pb-3 pt-3 px-1 border-b-2 font-medium text-sm transition-colors inline-flex items-center gap-1.5',
                activeDetailTab === 'json'
                  ? 'border-primary-500 text-primary-600 dark:text-primary-400'
                  : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
              ]"
              @click="activeDetailTab = 'json'"
            >
              <span>JSON canónico</span>
              <span
                v-if="jsonDirty"
                class="h-1.5 w-1.5 rounded-full bg-amber-500 shrink-0"
                title="Hay cambios locales respecto al JSON cargado"
                aria-hidden="true"
              />
            </button>
          </nav>
        </div>

        <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-6 space-y-4">
          <div v-if="loading" class="space-y-2">
            <div class="h-6 w-2/3 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
            <div class="h-4 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
            <div class="h-48 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          </div>

          <div v-else-if="error" class="admin-surface p-4 text-sm text-red-700 dark:text-red-300 border border-red-200 dark:border-red-900/30 bg-red-50/70 dark:bg-red-900/20">
            {{ error }}
          </div>

          <template v-else-if="product">
            <div
              v-show="activeDetailTab === 'data'"
              role="tabpanel"
              :aria-hidden="activeDetailTab !== 'data'"
              aria-labelledby="tab-catalog-detail-data"
              class="space-y-4"
            >
            <p
              v-if="jsonDirty"
              class="text-xs text-amber-800 dark:text-amber-200 bg-amber-50 dark:bg-amber-900/25 border border-amber-200/80 dark:border-amber-800/40 rounded-lg px-3 py-2"
            >
              Cambios sin guardar: usá «Guardar cambios» en la barra inferior para persistir en el catálogo (y staging si aplica).
            </p>

            <div class="admin-surface p-4">
              <div class="flex items-start gap-3">
                <div class="shrink-0 flex flex-col items-stretch gap-1.5 max-w-[6.5rem] sm:max-w-[7rem]">
                  <template v-if="productImageUrls.length">
                    <div class="relative w-12 h-12 mx-auto rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900">
                      <img
                        :src="productImageUrls[productCarouselClipIndex()]"
                        alt=""
                        class="w-full h-full object-cover"
                        loading="lazy"
                      />
                      <template v-if="productImageUrls.length > 1">
                        <button
                          type="button"
                          class="absolute inset-y-0 left-0 w-4 flex items-center justify-center text-[10px] font-bold text-white bg-black/40 hover:bg-black/55 rounded-r"
                          aria-label="Imagen anterior"
                          @click.stop="productCarouselPrev"
                        >
                          ‹
                        </button>
                        <button
                          type="button"
                          class="absolute inset-y-0 right-0 w-4 flex items-center justify-center text-[10px] font-bold text-white bg-black/40 hover:bg-black/55 rounded-l"
                          aria-label="Imagen siguiente"
                          @click.stop="productCarouselNext"
                        >
                          ›
                        </button>
                      </template>
                    </div>
                    <div
                      v-if="productImageUrls.length > 1"
                      class="flex gap-0.5 overflow-x-auto max-w-full pb-0.5 justify-start"
                    >
                      <button
                        v-for="(u, ti) in productImageUrls"
                        :key="'pthumb-' + ti"
                        type="button"
                        class="h-7 w-7 shrink-0 rounded overflow-hidden border transition-shadow"
                        :class="
                          ti === productCarouselClipIndex()
                            ? 'border-primary-500 ring-2 ring-primary-400/50'
                            : 'border-gray-200 dark:border-gray-600 opacity-80 hover:opacity-100'
                        "
                        @click.stop="productCarouselGo(ti)"
                      >
                        <img :src="u" alt="" class="w-full h-full object-cover" loading="lazy" />
                      </button>
                    </div>
                  </template>
                  <div
                    v-else
                    class="w-12 h-12 mx-auto rounded-lg border border-dashed border-gray-200 dark:border-gray-600 shrink-0 bg-gray-50 dark:bg-gray-900/50 flex items-center justify-center text-[10px] text-center text-gray-400 px-1 leading-tight"
                  >
                    Sin imagen
                  </div>
                </div>
                <div class="min-w-0">
                  <div class="flex items-center gap-2 flex-wrap">
                    <p class="font-semibold text-gray-900 dark:text-white truncate">{{ product.title || '—' }}</p>
                    <span
                      :class="[
                        'px-2 py-0.5 rounded text-xs font-medium',
                        loadOk ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300' : 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300'
                      ]"
                    >{{ product.loadStatus }}</span>
                  </div>
                  <p class="mt-1 text-xs text-gray-500 dark:text-gray-400 font-mono truncate">
                    {{ product.sku || product.entityId || product.naturalKey }}
                  </p>
                  <p v-if="product.loadError" class="mt-2 text-xs text-red-600 dark:text-red-400">
                    {{ product.loadError }}
                  </p>
                </div>
              </div>
            </div>

            <div class="admin-surface p-4">
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">Categoría y marca</h3>
              <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <dt class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Categoría</dt>
                  <dd class="mt-1 text-gray-900 dark:text-white">
                    <span v-if="product.categoryName">{{ product.categoryName }}</span>
                    <span v-else class="text-gray-400">—</span>
                  </dd>
                  <dd v-if="product.categoryId" class="mt-0.5 font-mono text-xs text-gray-500 dark:text-gray-400 break-all">
                    {{ product.categoryId }}
                  </dd>
                </div>
                <div>
                  <dt class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Marca</dt>
                  <dd class="mt-1 text-gray-900 dark:text-white">
                    <span v-if="product.brandName">{{ product.brandName }}</span>
                    <span v-else class="text-gray-400">—</span>
                  </dd>
                  <dd v-if="product.brandId" class="mt-0.5 font-mono text-xs text-gray-500 dark:text-gray-400 break-all">
                    {{ product.brandId }}
                  </dd>
                </div>
                <div class="sm:col-span-2">
                  <dt class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Moneda y precios</dt>
                  <dd class="mt-1 flex flex-row flex-nowrap items-stretch gap-3">
                    <input
                      type="text"
                      inputmode="text"
                      autocomplete="off"
                      spellcheck="false"
                      placeholder="USD"
                      aria-label="Moneda ISO"
                      class="w-[5.5rem] shrink-0 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm font-mono text-gray-900 dark:text-white uppercase"
                      :value="productCurrencyFieldValue()"
                      @input="onProductCurrencyInput($event.target.value)"
                    />
                    <input
                      type="text"
                      inputmode="decimal"
                      autocomplete="off"
                      aria-label="Precio de lista"
                      placeholder="Lista"
                      class="min-w-0 flex-1 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                      :value="productPriceFieldValue()"
                      @input="onProductPriceInput($event.target.value)"
                    />
                  </dd>
                  <dd class="mt-2">
                    <input
                      type="text"
                      inputmode="decimal"
                      autocomplete="off"
                      aria-label="Precio promocional"
                      placeholder="Promoción (opcional)"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                      :value="productSpecialPriceFieldValue()"
                      @input="onProductSpecialPriceInput($event.target.value)"
                    />
                  </dd>
                </div>
                <div>
                  <dt class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Stock</dt>
                  <dd class="mt-1">
                    <input
                      type="text"
                      inputmode="numeric"
                      autocomplete="off"
                      aria-label="Stock disponible"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                      :value="productStockFieldValue()"
                      @input="onProductStockInput($event.target.value)"
                    />
                  </dd>
                </div>
              </dl>
              <p v-if="categoryMetaLoading" class="mt-3 text-xs text-gray-500 dark:text-gray-400">Cargando atributos esperados para esta categoría…</p>
              <p v-else-if="categoryMetaError" class="mt-3 text-xs text-red-600 dark:text-red-400">{{ categoryMetaError }}</p>
              <p v-else-if="categoryMetaItems.length" class="mt-3 text-xs text-gray-500 dark:text-gray-400">
                Atributos de referencia (pipeline + categoría): {{ categorySpecCount }} especificaciones ·
                {{ categoryMetaItems.filter((x) => x.type === 'canonical').length }} campos canónicos.
              </p>
            </div>

            <div class="admin-surface p-4">
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">Especificaciones del producto</h3>
              <p v-if="!productSpecKeys.length" class="text-sm text-gray-500 dark:text-gray-400">Sin especificaciones a nivel producto.</p>
              <div v-else class="space-y-3">
                <div v-for="key in productSpecKeys" :key="'p-' + key" class="grid grid-cols-1 sm:grid-cols-3 gap-2 sm:gap-3 sm:items-center">
                  <label class="text-xs font-medium text-gray-600 dark:text-gray-300 sm:pt-2">{{ specLabel(key) }}</label>
                  <div class="sm:col-span-2">
                    <input
                      type="text"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                      :value="productSpecDisplay(key)"
                      @input="onProductSpecInput(key, $event.target.value)"
                    />
                  </div>
                </div>
              </div>
            </div>

            <div class="admin-surface p-4 overflow-hidden">
              <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">Variaciones</h3>
              <p v-if="!variationsRows.length" class="text-sm text-gray-500 dark:text-gray-400">Este producto no declara variaciones en el JSON canónico.</p>
              <template v-else>
                <!-- Tabla: lg+ -->
                <div class="hidden lg:block overflow-x-auto -mx-4 px-4">
                  <table class="min-w-full text-sm border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden">
                    <thead class="bg-gray-50 dark:bg-gray-900/80">
                      <tr>
                        <th class="text-left font-medium text-gray-600 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-gray-600 whitespace-nowrap">
                          skuId
                        </th>
                        <th class="text-left font-medium text-gray-600 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-gray-600 whitespace-nowrap">
                          Imágenes
                        </th>
                        <th class="text-left font-medium text-gray-600 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-gray-600 whitespace-nowrap min-w-[10rem]">
                          Dimensiones
                        </th>
                        <th class="text-left font-medium text-gray-600 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-gray-600 whitespace-nowrap">
                          Lista
                        </th>
                        <th class="text-left font-medium text-gray-600 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-gray-600 whitespace-nowrap">
                          Promo
                        </th>
                        <th class="text-left font-medium text-gray-600 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-gray-600 whitespace-nowrap">
                          Stock
                        </th>
                        <th
                          v-for="col in variationSpecKeys"
                          :key="'th-' + col"
                          class="text-left font-medium text-gray-600 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-gray-600 whitespace-nowrap"
                        >
                          {{ specLabel(col) }}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(row, idx) in variationsRows" :key="'tr-' + idx" class="border-b border-gray-100 dark:border-gray-700/80 last:border-0">
                        <td class="px-3 py-2 font-mono text-xs text-gray-700 dark:text-gray-200 whitespace-nowrap align-top">
                          {{ row.skuId || row.sku || '—' }}
                        </td>
                        <td class="px-3 py-2 align-middle">
                          <div v-if="variationImageUrlsFor(idx).length" class="flex flex-col items-stretch gap-1 max-w-[5.5rem]">
                            <div class="relative w-9 h-9 mx-auto shrink-0 rounded overflow-hidden border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900">
                              <img
                                :src="variationImageUrlsFor(idx)[variationCarouselClipIndex(idx)]"
                                alt=""
                                class="w-full h-full object-cover"
                                loading="lazy"
                              />
                              <template v-if="variationImageUrlsFor(idx).length > 1">
                                <button
                                  type="button"
                                  class="absolute inset-y-0 left-0 w-4 flex items-center justify-center text-[10px] font-bold text-white bg-black/40 hover:bg-black/55 rounded-r"
                                  aria-label="Imagen anterior"
                                  @click.stop="variationCarouselPrev(idx)"
                                >
                                  ‹
                                </button>
                                <button
                                  type="button"
                                  class="absolute inset-y-0 right-0 w-4 flex items-center justify-center text-[10px] font-bold text-white bg-black/40 hover:bg-black/55 rounded-l"
                                  aria-label="Imagen siguiente"
                                  @click.stop="variationCarouselNext(idx)"
                                >
                                  ›
                                </button>
                              </template>
                            </div>
                            <div
                              v-if="variationImageUrlsFor(idx).length > 1"
                              class="flex gap-0.5 overflow-x-auto max-w-[5.5rem] pb-0.5 justify-start"
                            >
                              <button
                                v-for="(u, ti) in variationImageUrlsFor(idx)"
                                :key="'vthumb-' + idx + '-' + ti"
                                type="button"
                                class="h-7 w-7 shrink-0 rounded overflow-hidden border transition-shadow"
                                :class="
                                  ti === variationCarouselClipIndex(idx)
                                    ? 'border-primary-500 ring-2 ring-primary-400/50'
                                    : 'border-gray-200 dark:border-gray-600 opacity-80 hover:opacity-100'
                                "
                                @click.stop="variationCarouselGo(idx, ti)"
                              >
                                <img :src="u" alt="" class="w-full h-full object-cover" loading="lazy" />
                              </button>
                            </div>
                          </div>
                          <span v-else class="text-xs text-gray-400">—</span>
                        </td>
                        <td class="px-3 py-2 align-top min-w-[9rem]">
                          <div class="flex flex-col gap-1">
                            <div
                              v-for="df in VARIATION_DIMENSION_FIELDS"
                              :key="'dim-' + idx + '-' + df.key"
                              class="flex items-center gap-1.5"
                            >
                              <span class="text-[10px] text-gray-500 dark:text-gray-400 w-8 shrink-0">{{ df.shortLabel }}</span>
                              <input
                                type="text"
                                inputmode="decimal"
                                class="flex-1 min-w-0 rounded border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-1.5 py-0.5 text-xs text-gray-900 dark:text-white"
                                :value="variationDimensionFieldValue(idx, df.key)"
                                @input="onVariationDimensionInput(idx, df.key, $event.target.value)"
                              />
                            </div>
                          </div>
                        </td>
                        <td class="px-3 py-2 align-top min-w-[6.5rem]">
                          <input
                            type="text"
                            inputmode="decimal"
                            class="w-full min-w-[5rem] rounded border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-2 py-1 text-xs text-gray-900 dark:text-white"
                            :value="variationPriceFieldValue(idx)"
                            @input="onVariationPriceInput(idx, $event.target.value)"
                          />
                          <p v-if="variationCurrencyForRow(idx)" class="mt-0.5 text-[10px] text-gray-500 dark:text-gray-400 font-mono">
                            {{ variationCurrencyForRow(idx) }}
                          </p>
                        </td>
                        <td class="px-3 py-2 align-top min-w-[5.5rem]">
                          <input
                            type="text"
                            inputmode="decimal"
                            class="w-full min-w-[4.5rem] rounded border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-2 py-1 text-xs text-gray-900 dark:text-white"
                            :value="variationSpecialPriceFieldValue(idx)"
                            @input="onVariationSpecialPriceInput(idx, $event.target.value)"
                          />
                        </td>
                        <td class="px-3 py-2 align-top w-[5.5rem]">
                          <input
                            type="text"
                            inputmode="numeric"
                            class="w-full rounded border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-2 py-1 text-xs text-gray-900 dark:text-white"
                            :value="variationStockFieldValue(idx)"
                            @input="onVariationStockInput(idx, $event.target.value)"
                          />
                        </td>
                        <td v-for="col in variationSpecKeys" :key="'td-' + idx + '-' + col" class="px-3 py-2 align-top min-w-[8rem]">
                          <input
                            type="text"
                            class="w-full min-w-[6rem] rounded border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-2 py-1 text-xs text-gray-900 dark:text-white"
                            :value="variationSpecDisplay(idx, col)"
                            @input="onVariationSpecInput(idx, col, $event.target.value)"
                          />
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <!-- Lista: pantallas menores a lg -->
                <div class="lg:hidden space-y-4">
                  <div
                    v-for="(row, idx) in variationsRows"
                    :key="'card-' + idx"
                    class="rounded-lg border border-gray-200 dark:border-gray-600 p-3 space-y-2 bg-gray-50/50 dark:bg-gray-900/30"
                  >
                    <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Variación {{ idx + 1 }}</p>
                    <div>
                      <span class="text-xs text-gray-500 dark:text-gray-400">skuId</span>
                      <p class="font-mono text-sm text-gray-900 dark:text-white break-all">{{ row.skuId || row.sku || '—' }}</p>
                    </div>
                    <div>
                      <span class="text-xs text-gray-500 dark:text-gray-400">Imágenes</span>
                      <div v-if="variationImageUrlsFor(idx).length" class="mt-2 space-y-2">
                        <div
                          class="relative mx-auto max-h-48 max-w-full aspect-square rounded-lg overflow-hidden border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900"
                        >
                          <img
                            :src="variationImageUrlsFor(idx)[variationCarouselClipIndex(idx)]"
                            alt=""
                            class="w-full h-full object-contain"
                            loading="lazy"
                          />
                          <template v-if="variationImageUrlsFor(idx).length > 1">
                            <button
                              type="button"
                              class="absolute inset-y-0 left-0 w-9 flex items-center justify-center text-lg font-bold text-white bg-black/35 hover:bg-black/50"
                              aria-label="Imagen anterior"
                              @click.stop="variationCarouselPrev(idx)"
                            >
                              ‹
                            </button>
                            <button
                              type="button"
                              class="absolute inset-y-0 right-0 w-9 flex items-center justify-center text-lg font-bold text-white bg-black/35 hover:bg-black/50"
                              aria-label="Imagen siguiente"
                              @click.stop="variationCarouselNext(idx)"
                            >
                              ›
                            </button>
                          </template>
                        </div>
                        <div
                          v-if="variationImageUrlsFor(idx).length > 1"
                          class="flex gap-1.5 overflow-x-auto pb-1 -mx-1 px-1"
                        >
                          <button
                            v-for="(u, ti) in variationImageUrlsFor(idx)"
                            :key="'mthumb-' + idx + '-' + ti"
                            type="button"
                            class="h-12 w-12 shrink-0 rounded-lg overflow-hidden border transition-shadow"
                            :class="
                              ti === variationCarouselClipIndex(idx)
                                ? 'border-primary-500 ring-2 ring-primary-400/50'
                                : 'border-gray-200 dark:border-gray-600 opacity-85 hover:opacity-100'
                            "
                            @click.stop="variationCarouselGo(idx, ti)"
                          >
                            <img :src="u" alt="" class="w-full h-full object-cover" loading="lazy" />
                          </button>
                        </div>
                      </div>
                      <p v-else class="mt-1 text-sm text-gray-400">—</p>
                    </div>
                    <div>
                      <span class="text-xs text-gray-500 dark:text-gray-400">Dimensiones</span>
                      <div class="mt-2 grid grid-cols-2 gap-2">
                        <div v-for="df in VARIATION_DIMENSION_FIELDS" :key="'mdim-' + idx + '-' + df.key">
                          <label class="text-xs font-medium text-gray-500 dark:text-gray-400">{{ df.label }}</label>
                          <input
                            type="text"
                            inputmode="decimal"
                            class="mt-1 w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                            :value="variationDimensionFieldValue(idx, df.key)"
                            @input="onVariationDimensionInput(idx, df.key, $event.target.value)"
                          />
                        </div>
                      </div>
                    </div>
                    <div class="grid grid-cols-2 gap-3">
                      <div>
                        <label class="text-xs font-medium text-gray-500 dark:text-gray-400">Precio lista</label>
                        <input
                          type="text"
                          inputmode="decimal"
                          class="mt-1 w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                          :value="variationPriceFieldValue(idx)"
                          @input="onVariationPriceInput(idx, $event.target.value)"
                        />
                        <p v-if="variationCurrencyForRow(idx)" class="mt-0.5 text-[10px] text-gray-500 font-mono">{{ variationCurrencyForRow(idx) }}</p>
                      </div>
                      <div>
                        <label class="text-xs font-medium text-gray-500 dark:text-gray-400">Promo</label>
                        <input
                          type="text"
                          inputmode="decimal"
                          class="mt-1 w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                          :value="variationSpecialPriceFieldValue(idx)"
                          @input="onVariationSpecialPriceInput(idx, $event.target.value)"
                        />
                      </div>
                      <div class="col-span-2">
                        <label class="text-xs font-medium text-gray-500 dark:text-gray-400">Stock</label>
                        <input
                          type="text"
                          inputmode="numeric"
                          class="mt-1 w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                          :value="variationStockFieldValue(idx)"
                          @input="onVariationStockInput(idx, $event.target.value)"
                        />
                      </div>
                    </div>
                    <div v-for="col in variationSpecKeys" :key="'c-' + idx + '-' + col" class="grid grid-cols-1 gap-1">
                      <label class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ specLabel(col) }}</label>
                      <input
                        type="text"
                        class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 px-3 py-2 text-sm text-gray-900 dark:text-white"
                        :value="variationSpecDisplay(idx, col)"
                        @input="onVariationSpecInput(idx, col, $event.target.value)"
                      />
                    </div>
                  </div>
                </div>
              </template>
            </div>
            </div>

            <div
              v-show="activeDetailTab === 'channels'"
              role="tabpanel"
              :aria-hidden="activeDetailTab !== 'channels'"
              aria-labelledby="tab-catalog-detail-channels"
              class="space-y-4"
            >
              <div class="admin-surface p-4">
                <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-1">Publicaciones por canal</h3>
                <p class="text-xs text-gray-500 dark:text-gray-400 mb-4">
                  Una publicación por canal de venta donde ya hay listado. Expandí «Eventos» para ver los envíos
                  (create, update, stock, precios, etc.).
                </p>
                <div v-if="channelsLoading" class="space-y-2 py-2">
                  <div class="h-10 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-10 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
                <p v-else-if="channelsError" class="text-sm text-red-600 dark:text-red-400">{{ channelsError }}</p>
                <p v-else-if="!channelPublications.length" class="text-sm text-gray-500 dark:text-gray-400">
                  Sin publicaciones en canales todavía (no hay estado de listado para este producto en ningún destino).
                </p>
                <div v-else class="space-y-2">
                  <div
                    v-for="pub in channelPublications"
                    :key="'pub-' + pub.publicationId"
                    class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-900/30"
                  >
                    <div class="px-3 py-3 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
                      <div class="min-w-0 flex-1 space-y-1">
                        <p class="text-sm font-semibold text-gray-900 dark:text-white">
                          {{ pub.channelDestinationName || 'Canal' }}
                          <span class="text-gray-400 font-normal">·</span>
                          <span class="font-mono text-xs text-gray-500 dark:text-gray-400">{{ pub.marketplaceKey }}</span>
                        </p>
                        <p class="text-xs text-gray-500 dark:text-gray-400">
                          ID listado:
                          <span class="font-mono text-gray-700 dark:text-gray-200">{{ pub.externalListingId || '—' }}</span>
                        </p>
                        <div class="flex flex-wrap items-center gap-2">
                          <span
                            class="inline-flex px-2 py-0.5 rounded text-xs font-medium"
                            :class="syncStateBadgeClass(pub.syncState)"
                          >
                            {{ listingSyncStateLabel(pub.syncState) }}
                          </span>
                          <span v-if="pub.marketplaceStatus" class="text-xs text-gray-600 dark:text-gray-300 truncate max-w-full">
                            Canal: {{ pub.marketplaceStatus }}
                          </span>
                        </div>
                        <p v-if="pub.lastError" class="text-xs text-red-600 dark:text-red-400 break-words">{{ pub.lastError }}</p>
                        <p v-if="pub.lastWarning" class="text-xs text-amber-700 dark:text-amber-300 break-words">{{ pub.lastWarning }}</p>
                      </div>
                      <div class="shrink-0 flex flex-wrap items-center gap-2">
                        <RouterLink
                          v-if="pub.channelDestinationId"
                          class="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline whitespace-nowrap"
                          :to="paths.toPath(`channels/destinations/${pub.channelDestinationId}/publication`)"
                        >
                          Ver canal
                        </RouterLink>
                      </div>
                    </div>
                    <div class="border-t border-gray-200 dark:border-gray-700">
                      <button
                        type="button"
                        class="w-full flex items-center justify-between gap-2 px-3 py-2 text-left text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800/60"
                        :aria-expanded="isPublicationExpanded(pub.publicationId)"
                        @click="togglePublicationAccordion(pub.publicationId)"
                      >
                        <span>Eventos de publicación</span>
                        <span class="text-gray-400 text-xs shrink-0" aria-hidden="true">
                          {{ isPublicationExpanded(pub.publicationId) ? '▼' : '▶' }}
                        </span>
                      </button>
                      <div
                        v-show="isPublicationExpanded(pub.publicationId)"
                        class="px-3 pb-3 pt-0 border-t border-gray-100 dark:border-gray-800/80 bg-gray-50/50 dark:bg-gray-900/50"
                      >
                        <p v-if="eventsLoadingPublicationId === pub.publicationId" class="text-xs text-gray-500 dark:text-gray-400 py-2">
                          Cargando eventos…
                        </p>
                        <p
                          v-else-if="eventsErrorByPublicationId[pub.publicationId]"
                          class="text-xs text-red-600 dark:text-red-400 py-2"
                        >
                          {{ eventsErrorByPublicationId[pub.publicationId] }}
                        </p>
                        <ul
                          v-else-if="(publicationEventsById[pub.publicationId] || []).length"
                          class="divide-y divide-gray-200 dark:divide-gray-700"
                        >
                          <li
                            v-for="ev in publicationEventsById[pub.publicationId]"
                            :key="'ev-' + pub.publicationId + '-' + ev.id"
                            class="py-2 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2 text-xs"
                          >
                            <div class="min-w-0 space-y-0.5">
                              <p class="font-mono text-gray-700 dark:text-gray-200">
                                {{ ev.serviceKind || '—' }}
                                <span class="text-gray-400">·</span>
                                {{ formatPublicationDate(ev.createdAt) }}
                              </p>
                              <p v-if="ev.errorMessage" class="text-red-600 dark:text-red-400 break-words">{{ ev.errorMessage }}</p>
                              <p v-if="ev.warningMessage" class="text-amber-700 dark:text-amber-300 break-words">
                                {{ ev.warningMessage }}
                              </p>
                            </div>
                            <span
                              class="shrink-0 inline-flex px-2 py-0.5 rounded text-[11px] font-medium self-start"
                              :class="publishItemStatusBadgeClass(ev.status)"
                            >
                              {{ publishItemStatusLabel(ev.status) }}
                            </span>
                          </li>
                        </ul>
                        <p v-else class="text-xs text-gray-500 dark:text-gray-400 py-2">Sin eventos registrados.</p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div
              v-show="activeDetailTab === 'json'"
              role="tabpanel"
              :aria-hidden="activeDetailTab !== 'json'"
              aria-labelledby="tab-catalog-detail-json"
              class="space-y-4"
            >
              <p
                v-if="jsonDirty"
                class="text-xs text-amber-800 dark:text-amber-200 bg-amber-50 dark:bg-amber-900/25 border border-amber-200/80 dark:border-amber-800/40 rounded-lg px-3 py-2"
              >
                Vista del borrador JSON. Guardá desde la barra inferior.
              </p>
              <div class="admin-surface overflow-hidden flex flex-col min-h-[min(70vh,calc(100vh-14rem))]">
                <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex flex-wrap items-center justify-between gap-3 shrink-0">
                  <h3 class="text-sm font-semibold text-gray-900 dark:text-white">JSON canónico</h3>
                  <div class="flex flex-wrap items-center gap-2">
                    <button
                      type="button"
                      class="text-sm font-medium text-primary-600 dark:text-primary-400 hover:underline"
                      @click="copyJsonEdited"
                    >
                      Copiar JSON editado
                    </button>
                    <button
                      type="button"
                      class="text-sm font-medium text-gray-600 dark:text-gray-400 hover:underline"
                      @click="copyJsonOriginal"
                    >
                      Copiar original
                    </button>
                  </div>
                </div>
                <pre class="p-4 text-xs font-mono overflow-auto flex-1 min-h-0 bg-gray-900 text-gray-100">{{ prettyDraftJson }}</pre>
              </div>
            </div>
          </template>
        </div>

        <div
          class="shrink-0 border-t border-gray-200 dark:border-gray-700 px-4 py-3 sm:px-6 bg-gray-50/90 dark:bg-gray-900/80 flex flex-col sm:flex-row sm:items-center gap-3 sm:gap-4"
        >
          <div class="min-w-0 flex-1 text-xs order-2 sm:order-1">
            <p v-if="saveError" class="text-red-600 dark:text-red-400">{{ saveError }}</p>
            <template v-else>
              <p v-if="product && jsonDirty" class="text-amber-800 dark:text-amber-200">
                Cambios pendientes de guardar.
              </p>
              <p v-else-if="product" class="text-gray-500 dark:text-gray-400">Sin cambios locales.</p>
              <p v-else-if="loading" class="text-gray-500 dark:text-gray-400">Cargando…</p>
              <p v-else-if="error" class="text-red-600 dark:text-red-400">{{ error }}</p>
            </template>
          </div>
          <div
            class="flex flex-col-reverse sm:flex-row sm:items-center gap-2 w-full sm:w-auto shrink-0 order-1 sm:order-2"
          >
            <button
              v-if="product && !loading && !error"
              type="button"
              class="text-sm py-2.5 px-5 w-full sm:w-auto rounded-lg border border-red-300 dark:border-red-800 text-red-700 dark:text-red-300 hover:bg-red-50 dark:hover:bg-red-950/40 font-medium disabled:opacity-40 disabled:cursor-not-allowed disabled:pointer-events-none"
              :disabled="deleteFooterDisabled"
              @click="openDeleteProductConfirm"
            >
              Eliminar producto
            </button>
            <button
              type="button"
              class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white shrink-0 disabled:opacity-40 disabled:cursor-not-allowed disabled:pointer-events-none"
              :disabled="saveFooterDisabled"
              @click="saveCanonical"
            >
              {{ saveBusy ? 'Guardando…' : 'Guardar cambios' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>

  <CatalogProductDeleteConfirmModal
    :open="deleteConfirmOpen"
    :message="deleteConfirmMessage"
    :busy="deleteBusy"
    @cancel="closeDeleteConfirm"
    @confirm="executeDeleteProduct"
  />
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import { useToast } from 'vue-toastification'
import { useAdminPaths } from '@/composables/useAdminPaths'
import apiService, { getApiErrorMessage } from '@/services/api'
import { catalogProductDeleteConfirmMessage } from '@/composables/useCatalogProductListDelete'
import CatalogProductDeleteConfirmModal from '@/components/catalog/CatalogProductDeleteConfirmModal.vue'
import {
  attributeNamesFromMetadata,
  collectVariationSpecKeys,
  dimensionKeyAsPascal,
  displaySpecValue,
  ensureVariationsArray,
  parseCanonicalRoot,
  parseSpecInput,
  readProductImageUrls,
  readSpecifications,
  readVariationCurrencyId,
  readVariationDimensionValue,
  readVariationImageUrls,
  readVariationPrice,
  readVariationSpecialPrice,
  readVariationStock,
  readVariations,
  unionKeys,
  VARIATION_DIMENSION_FIELDS,
  variationDimensionsRowIsEmpty
} from '@/utils/catalogCanonicalDraft.js'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  catalogProductId: { type: [String, Object], default: null }
})

const emit = defineEmits(['update:modelValue', 'deleted'])

const toast = useToast()
const paths = useAdminPaths()

const loading = ref(false)
const error = ref('')
const product = ref(null)
const draftRoot = ref({})
const baselinePrettyCanonical = ref('')
const rawCanonicalFromApi = ref('')
const categoryMetaItems = ref([])
const categoryMetaLoading = ref(false)
const categoryMetaError = ref('')
const activeDetailTab = ref('data')
const saveBusy = ref(false)
const saveError = ref('')
const deleteBusy = ref(false)
const deleteConfirmOpen = ref(false)

const channelPublications = ref([])
const channelsLoading = ref(false)
const channelsError = ref('')
const expandedPublicationIds = ref([])
const publicationEventsById = reactive({})
const eventsErrorByPublicationId = reactive({})
const eventsLoadingPublicationId = ref(null)

const deleteConfirmMessage = computed(() => catalogProductDeleteConfirmMessage(product.value))

/** Índice de imagen visible por índice de variación (carrusel). */
const variationCarouselIndex = reactive({})

/** Carrusel de imágenes a nivel producto (root.images + primary). */
const productImageCarouselIndex = ref(0)

const loadOk = computed(() => {
  const s = product.value?.loadStatus
  return s === 'Ok' || s === 'OK'
})

const headerLine = computed(() => {
  const p = product.value
  if (!p) return ''
  const parts = []
  if (p.categoryName) parts.push(p.categoryName)
  if (p.brandName) parts.push(p.brandName)
  if (p.sku) parts.push(`SKU ${p.sku}`)
  if (p.entityId) parts.push(`ID ${p.entityId}`)
  return parts.join(' · ')
})

const prettyDraftJson = computed(() => {
  try {
    return JSON.stringify(draftRoot.value || {}, null, 2)
  } catch {
    return ''
  }
})

const jsonDirty = computed(() => {
  try {
    return prettyDraftJson.value !== baselinePrettyCanonical.value
  } catch {
    return false
  }
})

const saveFooterDisabled = computed(
  () =>
    saveBusy.value ||
    deleteBusy.value ||
    !product.value ||
    loading.value ||
    error.value ||
    !jsonDirty.value
)

const deleteFooterDisabled = computed(
  () => deleteBusy.value || saveBusy.value || !product.value || loading.value || Boolean(error.value)
)

const categorySpecCount = computed(() => categoryMetaItems.value.filter((x) => x.type === 'specification').length)

const variationsRows = computed(() => readVariations(draftRoot.value))

const productImageUrls = computed(() => {
  const fromDraft = readProductImageUrls(draftRoot.value)
  if (fromDraft.length) return fromDraft
  const u = product.value?.primaryImageUrl
  return u && String(u).trim() ? [String(u).trim()] : []
})

const productSpecKeys = computed(() => {
  const meta = attributeNamesFromMetadata(categoryMetaItems.value, 'Product')
  const fromDraft = Object.keys(readSpecifications(draftRoot.value))
  return unionKeys(meta, fromDraft)
})

const variationSpecKeys = computed(() => {
  const meta = attributeNamesFromMetadata(categoryMetaItems.value, 'Sku')
  const fromDraft = collectVariationSpecKeys(variationsRows.value)
  return unionKeys(meta, fromDraft)
})

function formatCurrency(val, currencyId) {
  if (val == null || val === undefined) return '-'
  const currency = String(currencyId || 'USD')
  return new Intl.NumberFormat('es', { style: 'currency', currency }).format(val)
}

function listingSyncStateLabel(syncState) {
  const t = String(syncState || '').trim()
  const map = {
    Pending: 'Pendiente',
    Processing: 'Procesando',
    Succeeded: 'Sincronizado',
    Failed: 'Error',
    AwaitingRemote: 'Esperando respuesta del canal'
  }
  return map[t] || t || '—'
}

function syncStateBadgeClass(syncState) {
  const t = String(syncState || '').trim()
  if (t === 'Succeeded') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300'
  if (t === 'Failed') return 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300'
  if (t === 'Processing') return 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300'
  if (t === 'AwaitingRemote') return 'bg-amber-100 text-amber-900 dark:bg-amber-900/30 dark:text-amber-200'
  if (t === 'Pending') return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300'
  return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300'
}

function publishItemStatusLabel(status) {
  const t = String(status || '').trim()
  const map = {
    Pending: 'Pendiente',
    Processing: 'Procesando',
    Succeeded: 'Correcto',
    Failed: 'Falló'
  }
  return map[t] || t || '—'
}

function publishItemStatusBadgeClass(status) {
  const t = String(status || '').trim()
  if (t === 'Succeeded') return 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300'
  if (t === 'Failed') return 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300'
  if (t === 'Processing') return 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300'
  if (t === 'Pending') return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300'
  return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300'
}

function formatPublicationDate(iso) {
  if (iso == null || iso === '') return '—'
  try {
    const d = new Date(iso)
    if (Number.isNaN(d.getTime())) return String(iso)
    return new Intl.DateTimeFormat('es', {
      dateStyle: 'short',
      timeStyle: 'short'
    }).format(d)
  } catch {
    return String(iso)
  }
}

function resetChannelsPanel() {
  channelPublications.value = []
  channelsError.value = ''
  channelsLoading.value = false
  expandedPublicationIds.value = []
  eventsLoadingPublicationId.value = null
  Object.keys(publicationEventsById).forEach((k) => delete publicationEventsById[k])
  Object.keys(eventsErrorByPublicationId).forEach((k) => delete eventsErrorByPublicationId[k])
}

function isPublicationExpanded(publicationId) {
  const id = String(publicationId || '').trim()
  return id && expandedPublicationIds.value.includes(id)
}

async function loadChannelPublications() {
  const id = String(props.catalogProductId || '').trim()
  if (!id || channelsLoading.value) return
  channelsLoading.value = true
  channelsError.value = ''
  try {
    const { publications } = await apiService.getPipelineCatalogProductChannels(id)
    channelPublications.value = publications || []
  } catch (e) {
    channelsError.value = getApiErrorMessage(e) || 'No se pudieron cargar las publicaciones por canal.'
    channelPublications.value = []
  } finally {
    channelsLoading.value = false
  }
}

async function ensurePublicationEventsLoaded(publicationId) {
  const pubId = String(publicationId || '').trim()
  const catalogId = String(props.catalogProductId || '').trim()
  if (!pubId || !catalogId) return
  if (Object.prototype.hasOwnProperty.call(publicationEventsById, pubId)) return
  eventsLoadingPublicationId.value = pubId
  delete eventsErrorByPublicationId[pubId]
  try {
    const { events } = await apiService.getPipelineCatalogProductPublicationEvents(catalogId, pubId)
    publicationEventsById[pubId] = events || []
  } catch (e) {
    eventsErrorByPublicationId[pubId] = getApiErrorMessage(e) || 'No se pudieron cargar los eventos.'
    publicationEventsById[pubId] = []
  } finally {
    eventsLoadingPublicationId.value = null
  }
}

function togglePublicationAccordion(publicationId) {
  const id = String(publicationId || '').trim()
  if (!id) return
  const cur = expandedPublicationIds.value
  const i = cur.indexOf(id)
  if (i >= 0) {
    expandedPublicationIds.value = cur.filter((x) => x !== id)
    return
  }
  expandedPublicationIds.value = [...cur, id]
  void ensurePublicationEventsLoaded(id)
}

function specLabel(attributeName) {
  const spec = categoryMetaItems.value.find(
    (x) => x.type === 'specification' && String(x.attributeName || '').toLowerCase() === String(attributeName || '').toLowerCase()
  )
  if (spec?.displayName) return spec.displayName
  return attributeName || '—'
}

function productSpecDisplay(key) {
  const specs = readSpecifications(draftRoot.value)
  return displaySpecValue(specs[key])
}

function onProductSpecInput(key, text) {
  const root = draftRoot.value
  if (!root.specifications || typeof root.specifications !== 'object' || Array.isArray(root.specifications)) {
    root.specifications = {}
  }
  const v = parseSpecInput(text)
  const t = String(text ?? '').trim()
  if (t === '' && (v === '' || v === undefined)) {
    delete root.specifications[key]
    return
  }
  root.specifications[key] = v
}

function productPriceFieldValue() {
  const n = readVariationPrice(draftRoot.value)
  if (n != null) return String(n)
  const p = product.value?.price
  return p != null && p !== '' ? String(p) : ''
}

function productStockFieldValue() {
  const n = readVariationStock(draftRoot.value)
  if (n != null) return String(n)
  const q = product.value?.availableQuantity
  return q != null && q !== '' ? String(q) : ''
}

function productCurrencyFieldValue() {
  const c = readVariationCurrencyId(draftRoot.value)
  if (c) return c
  const p = product.value?.currencyId
  return p != null && String(p).trim() ? String(p).trim() : ''
}

function onProductPriceInput(text) {
  const root = draftRoot.value
  const t = String(text ?? '').trim().replace(',', '.')
  if (t === '') {
    delete root.price
    delete root.Price
    return
  }
  const v = Number.parseFloat(t)
  if (Number.isFinite(v)) root.price = v
}

function productSpecialPriceFieldValue() {
  const n = readVariationSpecialPrice(draftRoot.value)
  if (n != null) return String(n)
  const p = product.value?.specialPrice
  return p != null && p !== '' ? String(p) : ''
}

function onProductSpecialPriceInput(text) {
  const root = draftRoot.value
  const t = String(text ?? '').trim().replace(',', '.')
  if (t === '') {
    delete root.specialPrice
    delete root.SpecialPrice
    return
  }
  const v = Number.parseFloat(t)
  if (!Number.isFinite(v)) return
  if (v > 0) root.specialPrice = v
  else {
    delete root.specialPrice
    delete root.SpecialPrice
  }
}

function onProductStockInput(text) {
  const root = draftRoot.value
  const t = String(text ?? '').trim()
  if (t === '') {
    delete root.availableQuantity
    delete root.AvailableQuantity
    return
  }
  const v = Number.parseInt(t, 10)
  if (Number.isFinite(v)) root.availableQuantity = v
}

function onProductCurrencyInput(text) {
  const root = draftRoot.value
  const t = String(text ?? '').trim().toUpperCase()
  if (t === '') {
    delete root.currencyId
    delete root.CurrencyId
    return
  }
  root.currencyId = t
  delete root.CurrencyId
}

function variationSpecDisplay(index, key) {
  const row = variationsRows.value[index]
  const specs = readSpecifications(row)
  return displaySpecValue(specs[key])
}

function onVariationSpecInput(index, key, text) {
  const vars = ensureVariationsArray(draftRoot.value)
  while (vars.length <= index) vars.push({ specifications: {} })
  const row = vars[index]
  if (!row.specifications || typeof row.specifications !== 'object' || Array.isArray(row.specifications)) {
    row.specifications = {}
  }
  const v = parseSpecInput(text)
  const t = String(text ?? '').trim()
  if (t === '' && (v === '' || v === undefined)) {
    delete row.specifications[key]
    return
  }
  row.specifications[key] = v
}

function variationRowAt(index) {
  return variationsRows.value[index]
}

function variationImageUrlsFor(index) {
  return readVariationImageUrls(variationRowAt(index))
}

function clearVariationCarouselState() {
  Object.keys(variationCarouselIndex).forEach((k) => delete variationCarouselIndex[k])
}

function clearProductImageCarouselState() {
  productImageCarouselIndex.value = 0
}

function productCarouselClipIndex() {
  const urls = productImageUrls.value
  if (!urls.length) return 0
  let i = productImageCarouselIndex.value
  if (i >= urls.length || i < 0) {
    i = 0
    productImageCarouselIndex.value = 0
  }
  return i
}

function productCarouselGo(targetIndex) {
  const urls = productImageUrls.value
  if (!urls.length || targetIndex < 0 || targetIndex >= urls.length) return
  productImageCarouselIndex.value = targetIndex
}

function productCarouselPrev() {
  const urls = productImageUrls.value
  if (urls.length < 2) return
  const cur = productCarouselClipIndex()
  productCarouselGo(cur <= 0 ? urls.length - 1 : cur - 1)
}

function productCarouselNext() {
  const urls = productImageUrls.value
  if (urls.length < 2) return
  const cur = productCarouselClipIndex()
  productCarouselGo(cur >= urls.length - 1 ? 0 : cur + 1)
}

function variationCarouselClipIndex(idx) {
  const urls = variationImageUrlsFor(idx)
  if (!urls.length) return 0
  const k = String(idx)
  let i = variationCarouselIndex[k] ?? 0
  if (i >= urls.length || i < 0) {
    i = 0
    variationCarouselIndex[k] = 0
  }
  return i
}

function variationCarouselGo(idx, targetIndex) {
  const urls = variationImageUrlsFor(idx)
  if (!urls.length || targetIndex < 0 || targetIndex >= urls.length) return
  variationCarouselIndex[String(idx)] = targetIndex
}

function variationCarouselPrev(idx) {
  const urls = variationImageUrlsFor(idx)
  if (urls.length < 2) return
  const cur = variationCarouselClipIndex(idx)
  variationCarouselGo(idx, cur <= 0 ? urls.length - 1 : cur - 1)
}

function variationCarouselNext(idx) {
  const urls = variationImageUrlsFor(idx)
  if (urls.length < 2) return
  const cur = variationCarouselClipIndex(idx)
  variationCarouselGo(idx, cur >= urls.length - 1 ? 0 : cur + 1)
}

function variationDimensionFieldValue(index, key) {
  const n = readVariationDimensionValue(variationRowAt(index), key)
  return n == null ? '' : String(n)
}

function onVariationDimensionInput(index, key, text) {
  const vars = ensureVariationsArray(draftRoot.value)
  while (vars.length <= index) vars.push({ specifications: {} })
  const row = vars[index]
  const t = String(text ?? '').trim().replace(',', '.')
  if (t === '') {
    if (row.dimensions && typeof row.dimensions === 'object' && !Array.isArray(row.dimensions)) {
      delete row.dimensions[key]
      delete row.dimensions[dimensionKeyAsPascal(key)]
    }
    if (variationDimensionsRowIsEmpty(row.dimensions)) {
      delete row.dimensions
      delete row.Dimensions
    }
    return
  }
  const v = Number.parseFloat(t)
  if (!Number.isFinite(v)) return
  if (!row.dimensions || typeof row.dimensions !== 'object' || Array.isArray(row.dimensions)) {
    row.dimensions = {}
  }
  delete row.Dimensions
  row.dimensions[key] = v
  delete row.dimensions[dimensionKeyAsPascal(key)]
}

function variationCurrencyForRow(index) {
  return readVariationCurrencyId(variationRowAt(index)) || product.value?.currencyId || null
}

function variationPriceFieldValue(index) {
  const n = readVariationPrice(variationRowAt(index))
  return n == null ? '' : String(n)
}

function variationSpecialPriceFieldValue(index) {
  const n = readVariationSpecialPrice(variationRowAt(index))
  return n == null ? '' : String(n)
}

function variationStockFieldValue(index) {
  const n = readVariationStock(variationRowAt(index))
  return n == null ? '' : String(n)
}

function onVariationPriceInput(index, text) {
  const vars = ensureVariationsArray(draftRoot.value)
  while (vars.length <= index) vars.push({ specifications: {} })
  const row = vars[index]
  const t = String(text ?? '').trim().replace(',', '.')
  if (t === '') {
    delete row.price
    delete row.Price
    return
  }
  const v = Number.parseFloat(t)
  if (Number.isFinite(v)) row.price = v
}

function onVariationSpecialPriceInput(index, text) {
  const vars = ensureVariationsArray(draftRoot.value)
  while (vars.length <= index) vars.push({ specifications: {} })
  const row = vars[index]
  const t = String(text ?? '').trim().replace(',', '.')
  if (t === '') {
    delete row.specialPrice
    delete row.SpecialPrice
    return
  }
  const v = Number.parseFloat(t)
  if (!Number.isFinite(v)) return
  if (v > 0) row.specialPrice = v
  else {
    delete row.specialPrice
    delete row.SpecialPrice
  }
}

function onVariationStockInput(index, text) {
  const vars = ensureVariationsArray(draftRoot.value)
  while (vars.length <= index) vars.push({ specifications: {} })
  const row = vars[index]
  const t = String(text ?? '').trim()
  if (t === '') {
    delete row.availableQuantity
    delete row.AvailableQuantity
    return
  }
  const v = Number.parseInt(t, 10)
  if (Number.isFinite(v)) row.availableQuantity = v
}

function closeDeleteConfirm() {
  if (deleteBusy.value) return
  deleteConfirmOpen.value = false
}

function openDeleteProductConfirm() {
  const p = product.value
  if (!p?.id || deleteBusy.value || saveBusy.value) return
  deleteConfirmOpen.value = true
}

async function executeDeleteProduct() {
  const p = product.value
  if (!p?.id || deleteBusy.value) return
  deleteBusy.value = true
  saveError.value = ''
  try {
    await apiService.deleteChannelCatalogProduct(p.id)
    toast.success('Producto eliminado')
    deleteConfirmOpen.value = false
    emit('deleted')
    close()
  } catch (e) {
    saveError.value = getApiErrorMessage(e) || 'No se pudo eliminar el producto'
  } finally {
    deleteBusy.value = false
  }
}

async function saveCanonical() {
  const p = product.value
  if (!p?.id || saveBusy.value) return
  saveBusy.value = true
  saveError.value = ''
  try {
    let jsonText = prettyDraftJson.value || ''
    try {
      JSON.parse(jsonText)
    } catch {
      saveError.value = 'El borrador no es JSON válido. Revisá la pestaña JSON canónico.'
      return
    }
    const updated = await apiService.updateChannelCatalogProductCanonical(p.id, jsonText)
    product.value = updated
    resetDraftFromProduct()
    await loadCategoryMetadata()
    if (activeDetailTab.value === 'channels') {
      resetChannelsPanel()
      void loadChannelPublications()
    }
    toast.success('Cambios guardados')
  } catch (e) {
    saveError.value = getApiErrorMessage(e) || 'Error al guardar'
  } finally {
    saveBusy.value = false
  }
}

function close() {
  activeDetailTab.value = 'data'
  saveError.value = ''
  deleteConfirmOpen.value = false
  resetChannelsPanel()
  clearVariationCarouselState()
  clearProductImageCarouselState()
  emit('update:modelValue', false)
}

async function copyJsonEdited() {
  try {
    await navigator.clipboard.writeText(prettyDraftJson.value || '')
  } catch {
    /* ignore */
  }
}

async function copyJsonOriginal() {
  try {
    await navigator.clipboard.writeText(rawCanonicalFromApi.value || '')
  } catch {
    /* ignore */
  }
}

async function loadCategoryMetadata() {
  const p = product.value
  if (!p?.channelDataPipelineId || (!p.categoryId && !p.categoryName)) {
    categoryMetaItems.value = []
    categoryMetaError.value = ''
    return
  }
  categoryMetaLoading.value = true
  categoryMetaError.value = ''
  try {
    const r = await apiService.getMarketplaceTaxonomyCanonicalCategoryAttributes(
      p.channelDataPipelineId,
      p.categoryId || null,
      p.categoryName || null
    )
    categoryMetaItems.value = r.items || []
  } catch (e) {
    categoryMetaError.value = getApiErrorMessage(e) || 'No se pudieron cargar los atributos de la categoría'
    categoryMetaItems.value = []
  } finally {
    categoryMetaLoading.value = false
  }
}

function resetDraftFromProduct() {
  const p = product.value
  if (!p) {
    draftRoot.value = {}
    baselinePrettyCanonical.value = ''
    rawCanonicalFromApi.value = ''
    return
  }
  const raw = p.canonicalJson
  rawCanonicalFromApi.value =
    typeof raw === 'string' ? raw : (() => { try { return JSON.stringify(raw ?? {}, null, 2) } catch { return '' } })()
  const root = parseCanonicalRoot(p.canonicalJson)
  draftRoot.value = root && typeof root === 'object' ? root : {}
  try {
    baselinePrettyCanonical.value = JSON.stringify(draftRoot.value || {}, null, 2)
  } catch {
    baselinePrettyCanonical.value = ''
  }
}

async function load() {
  const id = props.catalogProductId
  if (!props.modelValue || !id) return
  loading.value = true
  error.value = ''
  saveError.value = ''
  resetChannelsPanel()
  clearVariationCarouselState()
  clearProductImageCarouselState()
  product.value = null
  categoryMetaItems.value = []
  categoryMetaError.value = ''
  draftRoot.value = {}
  baselinePrettyCanonical.value = ''
  rawCanonicalFromApi.value = ''
  try {
    const res = await apiService.getChannelCatalogProductDetail(id)
    product.value = res
    activeDetailTab.value = 'data'
    resetDraftFromProduct()
    await loadCategoryMetadata()
  } catch (e) {
    error.value = getApiErrorMessage(e) || 'Error al cargar el detalle'
  } finally {
    loading.value = false
  }
}

watch(() => [props.modelValue, props.catalogProductId], load, { immediate: true })

watch(
  () => [props.modelValue, props.catalogProductId, activeDetailTab.value],
  ([open, catalogId, tab]) => {
    if (!open || String(tab) !== 'channels') return
    const id = String(catalogId || '').trim()
    if (!id) return
    void loadChannelPublications()
  }
)
</script>

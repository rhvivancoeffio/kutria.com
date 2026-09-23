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
        v-if="modelValue"
        class="fixed top-0 right-0 z-[51] h-full min-h-0 w-full max-w-2xl sm:max-w-3xl lg:max-w-4xl xl:max-w-5xl bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
        role="dialog"
        aria-modal="true"
        aria-labelledby="catalog-create-product-heading"
        @click.stop
      >
        <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
          <div class="flex items-start justify-between gap-4">
            <div class="min-w-0">
              <h2
                id="catalog-create-product-heading"
                class="text-lg font-semibold text-gray-900 dark:text-white truncate"
              >
                Nuevo producto
              </h2>
              <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                Se guarda en el catálogo del pipeline seleccionado. JSON canónico compatible con el slider de detalle
                (<code class="text-xs">variations[]</code>).
              </p>
            </div>
            <button
              type="button"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
              aria-label="Cerrar"
              :disabled="createSaving"
              @click="close"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto min-h-0 p-4 sm:p-6 space-y-4">
          <div class="admin-surface p-4">
            <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">Datos del producto</h3>
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-3">
              Al menos título o SKU a nivel producto. Categoría y marca son opcionales (id y nombre para filtros y canales).
            </p>
            <div class="space-y-3">
              <div>
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Título</label>
                <input
                  v-model="newTitle"
                  type="text"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  placeholder="Nombre del producto"
                  autocomplete="off"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">SKU</label>
                <input
                  v-model="newSku"
                  type="text"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  placeholder="Código interno"
                  autocomplete="off"
                />
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
                    ID categoría <span class="text-gray-400 font-normal">(opcional)</span>
                  </label>
                  <input
                    v-model="newCategoryId"
                    type="text"
                    maxlength="256"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2 font-mono"
                    placeholder="Ej. taxonomía tienda / Mercado Libre"
                    autocomplete="off"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Nombre categoría <span class="text-gray-400 font-normal">(opcional)</span>
                  </label>
                  <input
                    v-model="newCategoryName"
                    type="text"
                    maxlength="1024"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                    placeholder="Ej. Indumentaria"
                    autocomplete="off"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
                    ID marca <span class="text-gray-400 font-normal">(opcional)</span>
                  </label>
                  <input
                    v-model="newBrandId"
                    type="text"
                    maxlength="256"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2 font-mono"
                    placeholder="Ej. brand-123"
                    autocomplete="off"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
                    Nombre marca <span class="text-gray-400 font-normal">(opcional)</span>
                  </label>
                  <input
                    v-model="newBrandName"
                    type="text"
                    maxlength="512"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                    placeholder="Ej. Mi marca"
                    autocomplete="off"
                  />
                </div>
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Precio lista (raíz)</label>
                  <input
                    v-model="newPrice"
                    type="number"
                    step="any"
                    min="0"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                    placeholder="—"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Precio promo (raíz)</label>
                  <input
                    v-model="newSpecialPrice"
                    type="number"
                    step="any"
                    min="0"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                    placeholder="Commerceional"
                  />
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Moneda</label>
                  <input
                    v-model="newCurrencyId"
                    type="text"
                    maxlength="16"
                    class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2 uppercase"
                    placeholder="USD"
                  />
                </div>
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Stock (raíz)</label>
                <input
                  v-model="newStock"
                  type="number"
                  step="1"
                  min="0"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                  placeholder="—"
                />
              </div>
            </div>
          </div>

          <div class="admin-surface p-4">
            <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-3">
              <div>
                <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Variaciones</h3>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                  Cada variación genera <code class="text-[11px]">skuId</code> automático si lo dejás vacío. Precio y stock
                  por fila opcionales. Cada SKU requiere entre 1 y 4 imágenes (subidas o URL).
                </p>
              </div>
              <button
                type="button"
                class="inline-flex items-center justify-center gap-1.5 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700/50 shrink-0"
                :disabled="createSaving || variationRows.length >= 120"
                @click="addVariation"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                Agregar variación
              </button>
            </div>

            <p v-if="!variationRows.length" class="text-sm text-gray-500 dark:text-gray-400">
              Sin variaciones: el producto queda solo con los datos de arriba. Agregá filas para publicar múltiples SKUs.
            </p>

            <div v-else class="space-y-4">
              <div
                v-for="(row, vIdx) in variationRows"
                :key="row._key"
                class="rounded-lg border border-gray-200 dark:border-gray-600 p-3 sm:p-4 space-y-3 bg-gray-50/50 dark:bg-gray-900/30"
              >
                <div class="flex items-center justify-between gap-2">
                  <span class="text-xs font-semibold text-gray-600 dark:text-gray-300 uppercase tracking-wide">
                    Variación {{ vIdx + 1 }}
                  </span>
                  <button
                    type="button"
                    class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/25 border border-transparent hover:border-red-200 dark:hover:border-red-900/40 disabled:opacity-50 touch-manipulation"
                    :disabled="createSaving"
                    title="Quitar variación"
                    aria-label="Quitar variación"
                    @click="removeVariation(vIdx)"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                      />
                    </svg>
                  </button>
                </div>

                <div class="rounded-lg border border-gray-200 dark:border-gray-600 bg-white/60 dark:bg-gray-900/40 p-2 space-y-1.5">
                  <div class="flex flex-wrap items-baseline justify-between gap-2">
                    <span class="text-xs font-semibold text-gray-700 dark:text-gray-200">Imágenes</span>
                    <span
                      class="text-[11px] font-medium tabular-nums"
                      :class="
                        variationImageCountOk(row)
                          ? 'text-gray-500 dark:text-gray-400'
                          : 'text-amber-600 dark:text-amber-400'
                      "
                    >
                      {{ filledImageCount(row) }}/{{ MAX_IMAGES_PER_VARIATION }} (mín. {{ MIN_IMAGES_PER_VARIATION }})
                    </span>
                  </div>
                  <p class="text-xs text-gray-500 dark:text-gray-400 leading-snug">
                    JPEG, PNG, GIF o WebP (máx. 5 MB) vía blob storage, o pegá URLs https. Deslizá horizontalmente para ver
                    todos los slots.
                  </p>
                  <input
                    :ref="(el) => setVariationFileInput(vIdx, el)"
                    type="file"
                    class="hidden"
                    tabindex="-1"
                    accept="image/jpeg,image/png,image/gif,image/webp"
                    @change="onVariationImageFile(vIdx, $event)"
                  />
                  <div
                    class="flex gap-2 overflow-x-auto pb-0.5 pt-0.5 snap-x snap-mandatory scroll-pl-1 -mx-0.5 px-0.5 [scrollbar-width:thin] isolate touch-manipulation"
                  >
                    <div
                      v-for="(imgUrl, imgIdx) in row.imageUrls"
                      :key="'img-' + row._key + '-' + imgIdx"
                      class="snap-start shrink-0 w-[6.25rem] sm:w-28 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 shadow-sm overflow-hidden flex flex-col relative z-0"
                    >
                      <div class="relative aspect-square bg-gray-100 dark:bg-gray-800">
                        <img
                          v-if="isHttpUrl(imgUrl)"
                          :src="imgUrl"
                          alt=""
                          class="absolute inset-0 w-full h-full object-cover pointer-events-none select-none"
                          @error="(ev) => ev.target && (ev.target.style.visibility = 'hidden')"
                        />
                        <button
                          v-if="!isHttpUrl(imgUrl)"
                          type="button"
                          class="absolute inset-0 z-0 flex flex-col items-center justify-center gap-0.5 p-1 text-center rounded-none cursor-pointer hover:bg-gray-200/60 dark:hover:bg-gray-700/50 active:bg-gray-200 dark:active:bg-gray-700 disabled:opacity-40 disabled:pointer-events-none touch-manipulation"
                          :disabled="
                            createSaving || imageUploadingIdx === vIdx || !catalogPipelineId || !canAddMoreVariationImages(row)
                          "
                          :aria-label="'Subir imagen en slot ' + (imgIdx + 1)"
                          @click.stop="triggerVariationImageUpload(vIdx)"
                        >
                          <span class="text-[9px] font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide leading-tight">
                            {{ imgIdx + 1 }}
                          </span>
                          <span class="text-[9px] text-gray-400 dark:text-gray-500">Subir</span>
                        </button>
                        <button
                          v-else
                          type="button"
                          class="absolute inset-0 z-0 cursor-pointer rounded-none bg-transparent hover:bg-black/10 dark:hover:bg-white/10 active:bg-black/15 dark:active:bg-white/15 touch-manipulation"
                          :disabled="createSaving"
                          aria-label="Editar URL de la imagen"
                          @click.stop="focusVariationImageUrlInput(row._key, imgIdx)"
                        ></button>
                        <button
                          type="button"
                          class="absolute top-0.5 right-0.5 z-10 p-1 rounded-md bg-white/90 dark:bg-gray-900/90 text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 shadow-sm border border-gray-200/80 dark:border-gray-600 disabled:opacity-40 touch-manipulation"
                          :disabled="createSaving || !canRemoveImageSlot(row, imgIdx)"
                          title="Quitar imagen"
                          aria-label="Quitar imagen"
                          @click.stop="removeImageUrl(vIdx, imgIdx)"
                        >
                          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                            <path
                              stroke-linecap="round"
                              stroke-linejoin="round"
                              stroke-width="2"
                              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                            />
                          </svg>
                        </button>
                      </div>
                      <input
                        :id="'cci-url-' + row._key + '-' + imgIdx"
                        v-model="row.imageUrls[imgIdx]"
                        type="url"
                        class="w-full min-h-[1.625rem] text-[9px] leading-tight px-1 py-0.5 border-t border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white font-mono placeholder:text-gray-400 relative z-[1]"
                        placeholder="https…"
                        autocomplete="off"
                      />
                    </div>

                    <button
                      type="button"
                      class="snap-start shrink-0 w-[6.25rem] sm:w-28 min-h-[8.125rem] sm:min-h-[9rem] rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-600 flex flex-col items-center justify-center gap-1 p-1.5 text-center hover:border-primary-400 dark:hover:border-primary-500 hover:bg-primary-50/40 dark:hover:bg-primary-900/10 disabled:opacity-50 touch-manipulation transition-colors cursor-pointer relative z-[1]"
                      :disabled="
                        createSaving ||
                        imageUploadingIdx === vIdx ||
                        !catalogPipelineId ||
                        !canAddMoreVariationImages(row)
                      "
                      @click.stop="triggerVariationImageUpload(vIdx)"
                    >
                      <span
                        v-if="imageUploadingIdx === vIdx"
                        class="text-[10px] font-medium text-primary-600 dark:text-primary-400 leading-tight"
                      >
                        Subiendo…
                      </span>
                      <template v-else>
                        <svg
                          class="w-5 h-5 text-gray-400 dark:text-gray-500 shrink-0"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                          aria-hidden="true"
                        >
                          <path
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="1.5"
                            d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
                          />
                        </svg>
                        <span class="text-[10px] font-medium text-gray-600 dark:text-gray-300 leading-tight">Subir</span>
                      </template>
                    </button>

                    <button
                      type="button"
                      class="snap-start shrink-0 w-[6.25rem] sm:w-28 min-h-[8.125rem] sm:min-h-[9rem] rounded-lg border-2 border-dashed border-gray-300 dark:border-gray-600 flex flex-col items-center justify-center gap-1 p-1.5 text-center hover:border-primary-400 dark:hover:border-primary-500 hover:bg-primary-50/40 dark:hover:bg-primary-900/10 disabled:opacity-50 touch-manipulation transition-colors cursor-pointer relative z-[1]"
                      :disabled="createSaving || row.imageUrls.length >= MAX_IMAGES_PER_VARIATION"
                      @click.stop="addImageUrlRow(vIdx)"
                    >
                      <svg
                        class="w-5 h-5 text-gray-400 dark:text-gray-500 shrink-0"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 4v16m8-8H4" />
                      </svg>
                      <span class="text-[10px] font-medium text-gray-600 dark:text-gray-300 leading-tight">Más URL</span>
                    </button>
                  </div>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div class="sm:col-span-2">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
                      skuId <span class="text-gray-400 font-normal">(opcional)</span>
                    </label>
                    <input
                      v-model="row.skuId"
                      type="text"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2 font-mono"
                      placeholder="Se asigna solo si está vacío"
                      autocomplete="off"
                    />
                  </div>
                  <div class="sm:col-span-2">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">SKU</label>
                    <input
                      v-model="row.sku"
                      type="text"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                      placeholder="Código de la variación"
                      autocomplete="off"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Precio lista</label>
                    <input
                      v-model="row.price"
                      type="number"
                      step="any"
                      min="0"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                      placeholder="—"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Precio promo</label>
                    <input
                      v-model="row.specialPrice"
                      type="number"
                      step="any"
                      min="0"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                      placeholder="—"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Stock</label>
                    <input
                      v-model="row.stock"
                      type="number"
                      step="1"
                      min="0"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2"
                      placeholder="—"
                    />
                  </div>
                  <div class="sm:col-span-2">
                    <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Moneda</label>
                    <input
                      v-model="row.currencyId"
                      type="text"
                      maxlength="16"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2 uppercase"
                      placeholder="Hereda del producto si vacío"
                    />
                  </div>
                </div>

                <div>
                  <div class="flex items-center justify-between gap-2 mb-2">
                    <span class="text-xs font-medium text-gray-600 dark:text-gray-300">Especificaciones</span>
                    <button
                      type="button"
                      class="inline-flex items-center justify-center gap-1.5 px-2.5 py-1.5 rounded-lg border border-gray-300 dark:border-gray-600 text-xs font-medium text-gray-700 dark:text-gray-200 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700/80 disabled:opacity-50 touch-manipulation"
                      :disabled="createSaving"
                      @click="addSpecRow(vIdx)"
                    >
                      <svg class="w-3.5 h-3.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                      </svg>
                      Atributo
                    </button>
                  </div>
                  <div class="space-y-2">
                    <div
                      v-for="(sp, sIdx) in row.specRows"
                      :key="sp._key"
                      class="flex flex-col sm:flex-row gap-2 sm:items-center"
                    >
                      <input
                        v-model="sp.key"
                        type="text"
                        class="flex-1 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2 text-gray-900 dark:text-white"
                        placeholder="Clave (ej. Color)"
                      />
                      <input
                        v-model="sp.value"
                        type="text"
                        class="flex-1 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2 text-gray-900 dark:text-white"
                        placeholder="Valor"
                      />
                      <button
                        type="button"
                        class="p-2 rounded-lg text-gray-500 dark:text-gray-400 hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 shrink-0 disabled:opacity-40 touch-manipulation"
                        :disabled="createSaving || row.specRows.length <= 1"
                        title="Quitar atributo"
                        aria-label="Quitar atributo"
                        @click="removeSpecRow(vIdx, sIdx)"
                      >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
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
                </div>
              </div>
            </div>
          </div>
        </div>

        <div
          class="shrink-0 border-t border-gray-200 dark:border-gray-700 px-4 py-3 sm:px-6 bg-gray-50/90 dark:bg-gray-900/80 flex flex-col sm:flex-row sm:items-center gap-3 sm:gap-4"
        >
          <p v-if="submitError" class="text-xs text-red-600 dark:text-red-400 order-2 sm:order-1 sm:flex-1 min-w-0">
            {{ submitError }}
          </p>
          <div v-else class="text-xs text-gray-500 dark:text-gray-400 order-2 sm:order-1 sm:flex-1 min-w-0">
            Los datos se serializan al JSON canónico del catálogo al guardar.
          </div>
          <div class="flex flex-col-reverse sm:flex-row gap-2 sm:gap-2 order-1 sm:order-2 w-full sm:w-auto">
            <button
              type="button"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
              :disabled="createSaving"
              @click="close"
            >
              Cancelar
            </button>
            <button
              type="button"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium disabled:opacity-50"
              :disabled="createSaving || !catalogPipelineId"
              @click="submit"
            >
              {{ createSaving ? 'Creando…' : 'Crear producto' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { nextTick, ref, watch } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '@/services/api'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  catalogPipelineId: { type: String, default: '' }
})

const emit = defineEmits(['update:modelValue', 'created'])

const toast = useToast()

const createSaving = ref(false)
const submitError = ref('')

const newTitle = ref('')
const newSku = ref('')
const newCategoryId = ref('')
const newCategoryName = ref('')
const newBrandId = ref('')
const newBrandName = ref('')
const newPrice = ref('')
const newSpecialPrice = ref('')
const newCurrencyId = ref('')
const newStock = ref('')

const variationRows = ref([])
const variationFileInputs = ref({})
const imageUploadingIdx = ref(null)

const MIN_IMAGES_PER_VARIATION = 1
const MAX_IMAGES_PER_VARIATION = 4

function rowKey() {
  return typeof crypto !== 'undefined' && crypto.randomUUID
    ? crypto.randomUUID()
    : `k-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

function emptySpecRow() {
  return { _key: rowKey(), key: '', value: '' }
}

function emptyVariation() {
  return {
    _key: rowKey(),
    skuId: '',
    sku: '',
    price: '',
    specialPrice: '',
    stock: '',
    currencyId: '',
    specRows: [emptySpecRow()],
    imageUrls: ['']
  }
}

function resetForm() {
  submitError.value = ''
  newTitle.value = ''
  newSku.value = ''
  newCategoryId.value = ''
  newCategoryName.value = ''
  newBrandId.value = ''
  newBrandName.value = ''
  newPrice.value = ''
  newSpecialPrice.value = ''
  newCurrencyId.value = ''
  newStock.value = ''
  variationRows.value = []
}

function close() {
  if (createSaving.value) return
  emit('update:modelValue', false)
}

function addVariation() {
  variationRows.value.push(emptyVariation())
}

function removeVariation(idx) {
  variationRows.value.splice(idx, 1)
}

function addSpecRow(vIdx) {
  variationRows.value[vIdx].specRows.push(emptySpecRow())
}

function removeSpecRow(vIdx, sIdx) {
  const rows = variationRows.value[vIdx].specRows
  if (rows.length <= 1) return
  rows.splice(sIdx, 1)
}

function setVariationFileInput(vIdx, el) {
  if (el) variationFileInputs.value[vIdx] = el
  else delete variationFileInputs.value[vIdx]
}

function triggerVariationImageUpload(vIdx) {
  variationFileInputs.value[vIdx]?.click()
}

function focusVariationImageUrlInput(rowKey, imgIdx) {
  nextTick(() => {
    document.getElementById(`cci-url-${rowKey}-${imgIdx}`)?.focus()
  })
}

function filledImageCount(row) {
  return (row.imageUrls || []).filter((u) => String(u || '').trim()).length
}

function variationImageCountOk(row) {
  const n = filledImageCount(row)
  return n >= MIN_IMAGES_PER_VARIATION && n <= MAX_IMAGES_PER_VARIATION
}

function canAddMoreVariationImages(row) {
  return filledImageCount(row) < MAX_IMAGES_PER_VARIATION
}

function canRemoveImageSlot(row, imgIdx) {
  const urls = row.imageUrls
  if (!urls || imgIdx < 0 || imgIdx >= urls.length) return false
  if (urls.length > 1) return true
  return Boolean(String(urls[imgIdx] || '').trim())
}

function addImageUrlRow(vIdx) {
  const row = variationRows.value[vIdx]
  if (!row.imageUrls) row.imageUrls = ['']
  if (row.imageUrls.length >= MAX_IMAGES_PER_VARIATION) return
  row.imageUrls.push('')
}

function removeImageUrl(vIdx, imgIdx) {
  const row = variationRows.value[vIdx]
  const urls = row.imageUrls
  if (!urls || imgIdx < 0 || imgIdx >= urls.length) return
  if (urls.length > 1) {
    urls.splice(imgIdx, 1)
  } else {
    urls[imgIdx] = ''
  }
}

/** @returns {boolean} false si ya hay 4 URLs y no hay slot vacío */
function placeImageUrlInVariation(row, url) {
  if (!row.imageUrls?.length) row.imageUrls = ['']
  const urls = row.imageUrls
  const emptyIdx = urls.findIndex((u) => !String(u || '').trim())
  if (emptyIdx >= 0) {
    urls[emptyIdx] = url
    return true
  }
  if (urls.length < MAX_IMAGES_PER_VARIATION) {
    urls.push(url)
    return true
  }
  return false
}

function isHttpUrl(s) {
  const t = String(s || '').trim()
  return /^https?:\/\//i.test(t)
}

async function onVariationImageFile(vIdx, event) {
  const input = event.target
  const file = input.files?.[0]
  input.value = ''
  if (!file || !String(props.catalogPipelineId || '').trim()) return
  imageUploadingIdx.value = vIdx
  try {
    const url = await apiService.uploadChannelCatalogProductMedia(props.catalogPipelineId, file)
    if (url) {
      const row = variationRows.value[vIdx]
      if (!placeImageUrlInVariation(row, url)) {
        toast.warning(`Máximo ${MAX_IMAGES_PER_VARIATION} imágenes por variación.`)
      }
    }
  } catch (e) {
    const d = e.response?.data
    const msg = d?.error || d?.detail || e.message || 'No se pudo subir la imagen'
    toast.error(typeof msg === 'string' ? msg : 'No se pudo subir la imagen')
  } finally {
    imageUploadingIdx.value = null
  }
}

function buildSpecifications(specRows) {
  const o = {}
  for (const r of specRows) {
    const k = String(r.key || '').trim()
    if (!k) continue
    o[k] = String(r.value ?? '').trim()
  }
  return Object.keys(o).length ? o : null
}

watch(
  () => props.modelValue,
  (open) => {
    if (open) resetForm()
  }
)

async function submit() {
  submitError.value = ''
  if (!String(newTitle.value).trim() && !String(newSku.value).trim()) {
    submitError.value = 'Indica al menos título o SKU del producto.'
    return
  }
  if (!String(props.catalogPipelineId || '').trim()) {
    submitError.value = 'Seleccioná un pipeline en la pantalla principal.'
    return
  }

  if (variationRows.value.length > 0) {
    for (let i = 0; i < variationRows.value.length; i++) {
      const n = filledImageCount(variationRows.value[i])
      if (n < MIN_IMAGES_PER_VARIATION) {
        submitError.value = `Variación ${i + 1}: al menos ${MIN_IMAGES_PER_VARIATION} imagen por SKU (${n}).`
        return
      }
      if (n > MAX_IMAGES_PER_VARIATION) {
        submitError.value = `Variación ${i + 1}: como máximo ${MAX_IMAGES_PER_VARIATION} imágenes por SKU (${n}).`
        return
      }
    }
  }

  const variations = variationRows.value.map((row) => ({
    skuId: row.skuId.trim() || null,
    sku: row.sku.trim() || null,
    price: row.price,
    specialPrice: row.specialPrice,
    availableQuantity: row.stock,
    currencyId: row.currencyId.trim() || null,
    specifications: buildSpecifications(row.specRows),
    images: (row.imageUrls || [])
      .map((u) => String(u).trim())
      .filter(Boolean)
      .slice(0, MAX_IMAGES_PER_VARIATION)
  }))

  createSaving.value = true
  try {
    const p = await apiService.createChannelCatalogProduct({
      pipelineId: props.catalogPipelineId,
      title: newTitle.value,
      sku: newSku.value,
      categoryId: newCategoryId.value,
      categoryName: newCategoryName.value,
      brandId: newBrandId.value,
      brandName: newBrandName.value,
      price: newPrice.value,
      specialPrice: newSpecialPrice.value,
      currencyId: newCurrencyId.value,
      availableQuantity: newStock.value,
      variations
    })
    emit('update:modelValue', false)
    toast.success('Producto creado')
    emit('created', p)
  } catch (e) {
    const d = e.response?.data
    const msg = d?.error || d?.detail || d?.title || e.message || 'Error al crear el producto'
    const text = typeof msg === 'string' ? msg : 'Error al crear el producto'
    toast.error(text)
  } finally {
    createSaving.value = false
  }
}
</script>

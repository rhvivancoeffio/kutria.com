<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-[1400px] mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-6">
      <!-- Header -->
      <div class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between mb-6">
        <div class="min-w-0">
          <button
            type="button"
            class="inline-flex items-center gap-1.5 text-sm font-medium text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white min-h-[40px] touch-manipulation -ml-1 px-1"
            :disabled="saving"
            @click="goBackToList"
          >
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
            Productos
          </button>
          <h1 class="mt-1 text-2xl sm:text-[1.75rem] font-bold tracking-tight text-gray-900 dark:text-white">
            {{ isEditMode ? 'Editar producto' : 'Creador de productos' }}
          </h1>
          <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
            {{
              isEditMode
                ? 'Actualiza la ficha en Gravity.'
                : 'Crea fichas que venden. Imagen primero, luego confirmas los datos.'
            }}
          </p>
        </div>

        <div
          v-if="!isEditMode"
          class="flex flex-wrap items-center gap-2 p-1 rounded-xl bg-gray-100/80 dark:bg-gray-800 border border-gray-200 dark:border-gray-700"
        >
          <button
            type="button"
            class="inline-flex items-center gap-2 px-3.5 py-2.5 rounded-lg text-sm font-medium min-h-[44px] touch-manipulation transition-colors"
            :class="modePillClass('image')"
            @click="setMode('image')"
          >
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
            <span class="text-left leading-tight">
              <span class="block">Rápido con foto</span>
              <span class="block text-[11px] font-normal text-gray-500 dark:text-gray-400">Empieza por la imagen</span>
            </span>
          </button>
          <button
            type="button"
            class="inline-flex items-center gap-2 px-3.5 py-2.5 rounded-lg text-sm font-medium min-h-[44px] touch-manipulation transition-colors"
            :class="modePillClass('manual')"
            @click="setMode('manual')"
          >
            <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
            Crear manual
          </button>
        </div>
      </div>

      <!-- Mini stepper -->
      <div v-if="!isEditMode" class="flex items-center gap-2 sm:gap-3 mb-5">
        <div class="flex items-center gap-2">
          <span class="flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold" :class="stepBadgeClass(1)">
            <svg v-if="step > 1" class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
            </svg>
            <template v-else>1</template>
          </span>
          <span class="text-[13px] font-semibold" :class="step === 1 ? 'text-gray-900 dark:text-white' : 'text-gray-400'">
            Foto
          </span>
        </div>
        <div class="w-8 sm:w-10 h-px bg-gray-200 dark:bg-gray-600" aria-hidden="true" />
        <div class="flex items-center gap-2">
          <span class="flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold" :class="stepBadgeClass(2)">
            2
          </span>
          <span class="text-[13px] font-semibold" :class="step === 2 ? 'text-gray-900 dark:text-white' : 'text-gray-400'">
            Confirma
          </span>
        </div>
      </div>

      <div v-if="loadingProduct" class="py-16 text-center">
        <p class="text-sm text-gray-500 dark:text-gray-400">Cargando producto…</p>
      </div>

      <div
        v-else-if="loadProductError"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm">{{ loadProductError }}</p>
        <button
          type="button"
          class="mt-3 text-sm font-medium text-red-700 dark:text-red-300 hover:underline"
          @click="loadProductForEdit"
        >
          Reintentar
        </button>
      </div>

      <template v-else>
      <!-- ========== STEP 1: Foto ========== -->
      <div v-if="step === 1" class="flex flex-col lg:flex-row gap-4 lg:gap-5 items-start">
        <div class="flex-1 min-w-0 w-full">
          <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 shadow-sm overflow-hidden p-4 sm:p-6">
            <span
              class="inline-flex items-center px-2.5 py-1 rounded-md text-[10px] font-bold tracking-wider uppercase bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 mb-4"
            >
              Modo rápido • 1 imagen = ficha
            </span>

            <div
              v-if="imagePreviewOk && imagePreviewSrc"
              class="relative rounded-[20px] border-2 border-primary-400 dark:border-primary-500 bg-primary-50/30 dark:bg-primary-900/10 overflow-hidden min-h-[200px]"
            >
              <img
                :src="imagePreviewSrc"
                alt="Vista previa"
                class="absolute inset-0 w-full h-full object-contain p-4"
              />
              <div class="pointer-events-none absolute inset-x-0 bottom-0 h-20 bg-gradient-to-t from-black/55 to-transparent" />
              <div class="absolute inset-x-0 bottom-0 p-3 flex items-end justify-between gap-2">
                <p class="min-w-0 text-xs sm:text-sm font-medium text-white truncate drop-shadow">
                  {{ imageFile?.name || '1 imagen lista' }}
                </p>
                <div class="shrink-0 flex items-center gap-2">
                  <button
                    type="button"
                    class="inline-flex items-center min-h-[36px] px-2.5 py-1.5 rounded-lg bg-white/95 dark:bg-gray-900/95 text-xs font-medium text-gray-700 dark:text-gray-200 border border-gray-200/80 dark:border-gray-600 shadow-sm hover:bg-white dark:hover:bg-gray-800 disabled:opacity-50"
                    :disabled="imageUploading"
                    @click="!imageUploading && fileInput?.click()"
                  >
                    Cambiar
                  </button>
                  <button
                    type="button"
                    class="inline-flex items-center min-h-[36px] px-2.5 py-1.5 rounded-lg bg-white/95 dark:bg-gray-900/95 text-xs font-medium text-red-600 dark:text-red-400 border border-gray-200/80 dark:border-gray-600 shadow-sm hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-50"
                    :disabled="imageUploading"
                    @click="clearImage"
                  >
                    Quitar imagen
                  </button>
                </div>
              </div>
              <input
                ref="fileInput"
                type="file"
                accept="image/*"
                class="hidden"
                :disabled="imageUploading"
                @change="onFileChange"
              />
            </div>

            <div
              v-else
              :class="[
                'rounded-[20px] border-2 transition-colors flex flex-col items-center justify-center min-h-[200px] px-4 py-12 sm:py-16',
                imageUploading ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer',
                dragging
                  ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20'
                  : 'border-gray-300 dark:border-gray-600 bg-gray-50/80 dark:bg-gray-900/40 hover:border-primary-400 hover:bg-gray-50 dark:hover:bg-gray-700/50'
              ]"
              @dragover.prevent="!imageUploading && (dragging = true)"
              @dragleave.prevent="dragging = false"
              @drop.prevent="onDrop"
              @click="!imageUploading && fileInput?.click()"
            >
              <input
                ref="fileInput"
                type="file"
                accept="image/*"
                class="hidden"
                :disabled="imageUploading"
                @change="onFileChange"
              />
              <span
                class="flex h-12 w-12 items-center justify-center rounded-xl bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 mb-3"
              >
                <svg class="w-6 h-6 text-gray-600 dark:text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="1.75"
                    d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
                  />
                </svg>
              </span>
              <p class="text-[15px] font-semibold text-gray-900 dark:text-white text-center">
                Arrastra una imagen aquí o <span class="text-primary-600 dark:text-primary-400">selecciona</span>
              </p>
              <p class="text-[13px] text-gray-500 dark:text-gray-400 mt-1">JPG, PNG o WEBP • máx. 8 MB</p>
            </div>

            <label
              class="mt-3 inline-flex items-center gap-1.5 text-sm text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 cursor-pointer"
            >
              <input
                type="file"
                accept="image/*"
                capture="environment"
                class="hidden"
                :disabled="imageUploading"
                @change="onFileChange"
              />
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"
                />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              Tomar foto
            </label>

            <div class="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div
                v-for="card in featureCards"
                :key="card.title"
                class="rounded-xl bg-gray-50 dark:bg-gray-900/50 border border-gray-100 dark:border-gray-700 px-3.5 py-3"
              >
                <p class="text-[10px] font-bold tracking-wider uppercase text-primary-600 dark:text-primary-400">
                  {{ card.title }}
                </p>
                <p class="mt-1 text-xs text-gray-600 dark:text-gray-400">{{ card.body }}</p>
              </div>
            </div>

            <p v-if="formError" class="mt-4 text-sm text-red-600 dark:text-red-400">{{ formError }}</p>
            <div class="mt-5 flex justify-end">
              <button
                type="button"
                class="min-h-[44px] px-5 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-40"
                :disabled="!canContinueFromPhoto || imageUploading || generativeBusy"
                @click="openImageAnalysisReady"
              >
                {{ hasLastImageGeneration ? 'Ver resultado de IA' : 'Iniciar análisis' }}
              </button>
            </div>
          </div>
        </div>

        <CompletenessPanel
          class="w-full lg:w-[20.5rem] shrink-0"
          :score-percent="scorePercent"
          :score-dash="scoreDash"
          :score-hint="scoreHint"
          :checklist="checklist"
          :tip-text="tipText"
          :can-submit="false"
          :saving="saving"
          submit-disabled
        />
      </div>

      <!-- ========== STEP 2: Confirma ========== -->
      <div v-else class="space-y-4">
        <form class="flex flex-col xl:flex-row gap-4 xl:gap-5 items-start" @submit.prevent="submit">
          <!-- Form cards -->
          <div class="flex-1 min-w-0 w-full space-y-4 order-1">
            <!-- Título + clasificación (adapts "Atributos y variantes") -->
            <section
              class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm p-4 sm:p-5 space-y-4"
            >
              <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Atributos y clasificación</h2>

              <div>
                <div class="flex items-center justify-between gap-2 mb-1">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Título del producto *</label>
                  <span class="text-[11px] text-gray-400">{{ form.name.length }}/120 · SEO ~60–80</span>
                </div>
                <div class="flex flex-col sm:flex-row gap-2">
                  <input
                    v-model="form.name"
                    type="text"
                    required
                    maxlength="120"
                    class="flex-1 min-w-0 w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2.5"
                    placeholder="Ej: Zapatillas urbanas negras…"
                    autocomplete="off"
                  />
                  <button
                    v-if="mode === 'manual'"
                    type="button"
                    class="shrink-0 inline-flex items-center justify-center gap-1.5 min-h-[42px] px-3 rounded-lg border border-gray-300 dark:border-gray-600 text-xs font-semibold text-gray-800 dark:text-gray-100 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="generativeBusy || !hasName"
                    title="Mejorar contenido con IA a partir del título"
                    @click="startContentByName"
                  >
                    <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                      <path
                        d="M12 2l1.6 5.2L19 9l-5.2 1.6L12 16l-1.6-5.4L5 9l5.4-1.8L12 2z"
                      />
                    </svg>
                    Mejorar con IA
                  </button>
                </div>
              </div>

              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <div class="flex items-center justify-between gap-2 mb-1">
                    <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Categoría</label>
                    <button
                      type="button"
                      class="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline"
                      @click="openCategoryPicker"
                    >
                      {{ form.categoryId ? 'Cambiar' : form.categoryPath ? 'Elegir del árbol' : '+ Añadir' }}
                    </button>
                  </div>
                  <div
                    class="rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900/60 px-3 py-2.5 text-sm min-h-[42px]"
                    :class="form.categoryPath ? 'text-gray-800 dark:text-gray-200' : 'text-gray-400'"
                  >
                    <span
                      v-if="form.categoryPath"
                      class="inline-flex items-center px-2 py-0.5 rounded-md bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 text-xs font-medium"
                    >
                      {{ form.categoryPath }}
                      <span v-if="!form.categoryId" class="ml-1 text-gray-400 font-normal">(sugerida)</span>
                    </span>
                    <template v-else>Sin categoría</template>
                  </div>
                  <div
                    v-if="categoryPickerOpen"
                    class="mt-2 rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden"
                  >
                    <div class="flex justify-between px-3 py-2 bg-gray-50 dark:bg-gray-800/80 border-b border-gray-100 dark:border-gray-700">
                      <span class="text-xs font-medium text-gray-600 dark:text-gray-300">Árbol</span>
                      <button type="button" class="text-xs text-gray-500" @click="categoryPickerOpen = false">
                        Cerrar
                      </button>
                    </div>
                    <p v-if="categoryLoading" class="px-3 py-3 text-xs text-gray-500">Cargando…</p>
                    <p v-else-if="categoryError" class="px-3 py-3 text-xs text-red-600">{{ categoryError }}</p>
                    <ul v-else class="max-h-48 overflow-y-auto" role="tree">
                      <ProductCategoryTreePick
                        :nodes="categoryTree"
                        :depth="0"
                        :expanded="categoryExpanded"
                        :selected-id="form.categoryId"
                        @toggle="toggleCategory"
                        @select="selectCategory"
                      />
                    </ul>
                  </div>
                </div>

                <div class="relative" @keydown.escape="brandOpen = false">
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Marca</label>
                  <div
                    v-if="form.brandId || form.brandName"
                    class="flex items-center gap-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-900/60 px-3 py-2"
                  >
                    <span
                      class="inline-flex items-center px-2 py-0.5 rounded-md bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 text-xs font-medium text-gray-900 dark:text-white truncate"
                    >
                      {{ form.brandName }}
                      <span v-if="!form.brandId" class="ml-1 text-gray-400 font-normal">(sugerida)</span>
                    </span>
                    <button type="button" class="ml-auto p-1 text-gray-500" aria-label="Quitar" @click="clearBrand">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                  <template v-else>
                    <input
                      v-model="brandQuery"
                      type="search"
                      class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2.5 text-gray-900 dark:text-white"
                      placeholder="Buscar marca…"
                      @focus="brandOpen = true"
                      @input="onBrandInput"
                    />
                    <div
                      v-if="brandOpen && (brandLoading || brandResults.length || brandQuery.trim())"
                      class="absolute z-20 mt-1 w-full max-h-40 overflow-y-auto rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 shadow-lg"
                    >
                      <p v-if="brandLoading" class="px-3 py-2 text-xs text-gray-500">Buscando…</p>
                      <button
                        v-for="b in brandResults"
                        :key="b.brandId"
                        type="button"
                        class="w-full text-left px-3 py-2 text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                        @click="selectBrand(b)"
                      >
                        {{ b.name }}
                      </button>
                    </div>
                  </template>
                </div>
              </div>
            </section>

            <!-- Contenido -->
            <section
              class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm p-4 sm:p-5 space-y-4"
            >
              <div class="flex items-center justify-between gap-2">
                <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Contenido</h2>
                <span
                  class="text-[10px] font-bold uppercase tracking-wide px-2 py-0.5 rounded-full bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300"
                >
                  Catálogo
                </span>
              </div>

              <!-- Generative tools (ambos modos): contenido, por imagen, Video UGC -->
              <div
                class="rounded-xl border border-dashed border-gray-200 dark:border-gray-600 bg-gray-50/80 dark:bg-gray-900/40 p-3 sm:p-4 space-y-3"
              >
                <div class="flex flex-wrap items-center gap-2">
                  <span class="text-xs font-medium text-gray-600 dark:text-gray-300">Generar con IA</span>
                  <button
                    type="button"
                    class="text-xs font-semibold px-2.5 py-1.5 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="generativeBusy || !hasName"
                    @click="startContentByName"
                  >
                    Desde título
                  </button>
                  <button
                    type="button"
                    class="text-xs font-semibold px-2.5 py-1.5 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="generativeBusy || !hasImage"
                    title="Genera ficha a partir de la foto"
                    @click="startContentByImage"
                  >
                    Desde foto
                  </button>
                  <button
                    type="button"
                    class="text-xs font-semibold px-2.5 py-1.5 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="generativeBusy || !hasName"
                    title="Genera una foto de catálogo con IA"
                    @click="startAiImages"
                  >
                    Generar imágenes
                  </button>
                  <button
                    type="button"
                    class="text-xs font-semibold px-2.5 py-1.5 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="generativeBusy || !hasName"
                    @click="startVideoStub"
                  >
                    Video UGC
                  </button>
                </div>
                <div class="flex flex-wrap gap-1.5">
                  <button
                    v-for="m in contentModeOptions"
                    :key="m.id"
                    type="button"
                    class="text-[11px] font-medium px-2 py-1 rounded-md border transition-colors"
                    :class="
                      contentModes.includes(m.id)
                        ? 'border-primary-500 bg-primary-50 text-primary-800 dark:bg-primary-900/40 dark:text-primary-200'
                        : 'border-gray-200 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
                    "
                    :disabled="generativeBusy"
                    @click="toggleContentMode(m.id)"
                  >
                    {{ m.label }}
                  </button>
                </div>
                <p v-if="!hasName" class="text-[11px] text-amber-700 dark:text-amber-400">
                  Escribe un título para habilitar título, Generar imágenes y Video UGC.
                </p>
                <p v-else-if="!hasImage" class="text-[11px] text-amber-700 dark:text-amber-400">
                  Añade una foto en la galería para habilitar Desde foto.
                </p>
                <p v-else-if="generativeBusy" class="text-xs text-gray-500 dark:text-gray-400">
                  {{ generativeStatus || 'Generando…' }}
                  <template v-if="lastProcessId">
                    (<code class="text-[11px]">{{ lastProcessId }}</code>)
                  </template>
                </p>
                <p v-else-if="lastProcessId" class="text-xs text-gray-600 dark:text-gray-300">
                  Última generación: <code class="text-[11px]">{{ lastProcessId }}</code>
                </p>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción *</label>
                <textarea
                  v-model="form.description"
                  rows="5"
                  required
                  maxlength="8000"
                  class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white text-sm px-3 py-2.5"
                  placeholder="Beneficios, materiales, uso…"
                />
                <p class="mt-1 text-[11px] text-gray-400">{{ form.description.length }}/8000</p>
              </div>

              <div>
                <div class="flex items-center justify-between gap-2 mb-1.5">
                  <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Bullets / puntos clave</label>
                  <span class="text-[11px] text-gray-400">{{ filledBullets.length }}/6</span>
                </div>
                <div class="space-y-2">
                  <div v-for="(bullet, idx) in bullets" :key="idx" class="flex items-center gap-2">
                    <span class="text-gray-400 shrink-0 select-none" aria-hidden="true">•</span>
                    <input
                      v-model="bullets[idx]"
                      type="text"
                      maxlength="120"
                      class="flex-1 min-w-0 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2 text-gray-900 dark:text-white"
                      :placeholder="bulletPlaceholders[idx] || 'Punto clave…'"
                    />
                    <button
                      type="button"
                      class="p-2 rounded-lg text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 disabled:opacity-30"
                      :disabled="bullets.length <= 1"
                      aria-label="Quitar bullet"
                      @click="removeBullet(idx)"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                </div>
                <button
                  v-if="bullets.length < 6"
                  type="button"
                  class="mt-2 text-xs font-semibold text-primary-600 dark:text-primary-400 hover:underline"
                  @click="addBullet"
                >
                  + Añadir bullet
                </button>
              </div>
            </section>

            <!-- Variaciones y stock -->
            <ProductCreateVariationsPanel
              ref="variationsPanelRef"
              :product-image-url="imagePreviewSrc && imagePreviewOk ? imagePreviewSrc : ''"
            />

            <!-- Publicación -->
            <section
              class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm p-4 sm:p-5 space-y-4"
            >
              <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Publicación</h2>
              <div class="flex items-center justify-between gap-3">
                <div>
                  <p class="text-sm text-gray-800 dark:text-gray-200">Activo</p>
                  <p class="text-xs text-gray-500">Disponible en la tienda</p>
                </div>
                <FormToggle v-model="form.isActive" aria-label="Activo" />
              </div>
              <div class="flex items-center justify-between gap-3">
                <div>
                  <p class="text-sm text-gray-800 dark:text-gray-200">Mostrar en catálogo</p>
                  <p class="text-xs text-gray-500">Visible en listados y búsqueda</p>
                </div>
                <FormToggle v-model="form.showInCatalog" aria-label="Mostrar en catálogo" />
              </div>
            </section>

            <!-- Galería -->
            <section
              class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm p-4 sm:p-5 space-y-3"
            >
              <div class="flex flex-wrap items-center justify-between gap-2">
                <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Galería de fotos</h2>
                <div class="flex flex-wrap items-center gap-1.5">
                  <button
                    type="button"
                    class="text-xs font-semibold px-2.5 py-1.5 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="generativeBusy || !hasName"
                    @click="startAiImages"
                  >
                    Generar imágenes
                  </button>
                  <button
                    type="button"
                    class="text-xs font-semibold px-2.5 py-1.5 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-800 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                    :disabled="generativeBusy || !hasName"
                    @click="startVideoStub"
                  >
                    Video UGC
                  </button>
                </div>
              </div>
              <div class="flex gap-2.5 overflow-x-auto pb-1">
                <button
                  type="button"
                  class="relative w-[4.5rem] h-[4.5rem] shrink-0 rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50 dark:bg-gray-900 hover:border-primary-400"
                  :disabled="imageUploading"
                  @click="!imageUploading && galleryFileInput?.click()"
                >
                  <img
                    v-if="imagePreviewSrc && imagePreviewOk"
                    :src="imagePreviewSrc"
                    alt=""
                    class="w-full h-full object-cover"
                  />
                  <div v-else class="absolute inset-0 flex flex-col items-center justify-center text-gray-400 gap-0.5">
                    <span class="text-lg leading-none">+</span>
                    <span class="text-[10px]">Añadir</span>
                  </div>
                </button>
                <button
                  v-for="(g, i) in generatedGallery"
                  :key="g.attachmentId || i"
                  type="button"
                  class="relative w-[4.5rem] h-[4.5rem] shrink-0 rounded-xl border overflow-hidden bg-gray-50 dark:bg-gray-900"
                  :class="
                    form.imageUrl === g.displayUrl
                      ? 'border-primary-500 ring-2 ring-primary-500/30'
                      : 'border-gray-200 dark:border-gray-600 hover:border-primary-400'
                  "
                  title="Usar como principal"
                  @click="applyGeneratedImage(g)"
                >
                  <img :src="g.displayUrl" alt="" class="w-full h-full object-cover" />
                </button>
                <input
                  ref="galleryFileInput"
                  type="file"
                  accept="image/*"
                  class="hidden"
                  :disabled="imageUploading"
                  @change="onGalleryFileChange"
                />
                <div
                  v-for="n in Math.max(0, 4 - generatedGallery.length)"
                  :key="'slot-' + n"
                  class="w-[4.5rem] h-[4.5rem] shrink-0 rounded-xl border border-dashed border-gray-300 dark:border-gray-600 flex flex-col items-center justify-center text-gray-300 dark:text-gray-600 gap-0.5"
                  title="Más fotos próximamente"
                >
                  <span class="text-lg leading-none">+</span>
                  <span class="text-[10px]">Añadir</span>
                </div>
              </div>
              <input
                v-if="mode === 'manual' && !imageFile"
                v-model="form.imageUrl"
                type="url"
                maxlength="2000"
                class="w-full rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm px-3 py-2.5 text-gray-900 dark:text-white"
                placeholder="URL de imagen principal…"
                @input="onImageUrlInput"
              />
              <p v-else-if="form.imageUrl" class="text-xs text-gray-400 truncate">{{ form.imageUrl }}</p>
            </section>

            <p v-if="formError" class="text-sm text-red-600 dark:text-red-400">{{ formError }}</p>

            <div class="flex flex-col-reverse sm:flex-row sm:justify-between gap-2">
              <button
                type="button"
                class="min-h-[44px] px-4 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200"
                :disabled="saving"
                @click="onConfirmBack"
              >
                {{ mode === 'image' ? '← Volver a foto' : 'Cancelar' }}
              </button>
              <button
                type="submit"
                class="xl:hidden min-h-[44px] px-4 py-2.5 rounded-lg bg-primary-600 text-white text-sm font-medium disabled:opacity-40"
                :disabled="saving || !canSubmit"
              >
                {{ saving ? (isEditMode ? 'Guardando…' : 'Creando…') : isEditMode ? 'Guardar cambios' : 'Validar y publicar' }}
              </button>
            </div>
          </div>

          <!-- Right: vista previa + validación -->
          <aside class="w-full xl:w-[20.5rem] shrink-0 space-y-4 order-2">
            <div class="space-y-3">
              <div class="flex items-center justify-between gap-2">
                <p class="text-[10px] font-bold tracking-wider uppercase text-gray-400">Vista previa</p>
                <span
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-[11px] font-medium text-gray-500 dark:text-gray-400"
                >
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                    />
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                    />
                  </svg>
                  En vivo
                </span>
              </div>
              <div
                class="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-sm overflow-hidden"
              >
                <div class="relative aspect-square bg-gray-100 dark:bg-gray-900">
                  <img
                    v-if="imagePreviewSrc && imagePreviewOk"
                    :src="imagePreviewSrc"
                    alt=""
                    class="absolute inset-0 w-full h-full object-cover"
                    @error="imagePreviewOk = false"
                  />
                  <div v-else class="absolute inset-0 flex items-center justify-center text-gray-300 dark:text-gray-600">
                    <svg class="w-12 h-12" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="1.5"
                        d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
                      />
                    </svg>
                  </div>
                  <span
                    class="absolute top-3 left-3 px-2 py-0.5 rounded-md text-[10px] font-bold uppercase bg-primary-600 text-white"
                  >
                    Nuevo
                  </span>
                </div>
                <div class="p-4 space-y-2">
                  <p class="text-sm font-bold text-gray-900 dark:text-white leading-snug line-clamp-2">
                    {{ form.name.trim() || 'Título del producto' }}
                  </p>
                  <p v-if="form.brandName" class="text-xs text-gray-500 dark:text-gray-400">
                    {{ form.brandName }}
                  </p>
                  <p v-if="form.categoryPath" class="text-xs text-gray-400 truncate">{{ form.categoryPath }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400 line-clamp-3">
                    {{ form.description.trim() || 'La descripción aparecerá aquí…' }}
                  </p>
                  <ul v-if="filledBullets.length" class="space-y-1 pt-1">
                    <li
                      v-for="(b, i) in filledBullets.slice(0, 4)"
                      :key="i"
                      class="flex items-start gap-1.5 text-xs text-gray-600 dark:text-gray-300"
                    >
                      <span class="text-primary-600 shrink-0 leading-5">•</span>
                      <span class="line-clamp-2 leading-5">{{ b }}</span>
                    </li>
                  </ul>
                  <div class="flex flex-wrap gap-1.5 pt-1">
                    <span
                      v-if="form.isActive"
                      class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-50 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300"
                    >
                      Activo
                    </span>
                    <span
                      v-if="form.showInCatalog"
                      class="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300"
                    >
                      En catálogo
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <CompletenessPanel
              :score-percent="scorePercent"
              :score-dash="scoreDash"
              :score-hint="scoreHint"
              :checklist="checklist"
              :tip-text="tipPlain"
              :title-chars="form.name.length"
              :description-chars="form.description.length"
              :has-image="hasImage"
              :can-submit="canSubmit"
              :saving="saving"
              :submit-label="isEditMode ? 'Guardar cambios' : 'Validar y publicar'"
              :saving-label="isEditMode ? 'Guardando…' : 'Creando…'"
              @submit="submit"
            />
          </aside>
        </form>

        <div
          class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 rounded-2xl bg-gray-900 dark:bg-gray-950 text-white px-4 sm:px-5 py-4"
        >
          <div class="flex items-start gap-3 min-w-0">
            <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary-500/20 text-primary-300">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M5 3v4M3 5h4M6 17v4m-2-2h4m5-16l2.286 6.857L21 12l-5.714 2.143L13 21l-2.286-6.857L5 12l5.714-2.143L13 3z"
                />
              </svg>
            </span>
            <div class="min-w-0">
              <p class="text-sm font-semibold">Revisa y completa tu ficha</p>
              <p class="text-xs text-gray-400 mt-0.5">
                Vista previa en vivo · Los datos se guardan en Gravity al crear
              </p>
            </div>
          </div>
          <button
            type="button"
            class="inline-flex items-center justify-center gap-2 min-h-[44px] px-4 py-2 rounded-lg bg-white text-gray-900 text-sm font-semibold hover:bg-gray-100 touch-manipulation shrink-0"
            :disabled="saving || !canSubmit"
            @click="submit"
          >
            {{ saving ? (isEditMode ? 'Guardando…' : 'Publicando…') : isEditMode ? 'Guardar cambios' : 'Validar y publicar' }}
          </button>
        </div>
      </div>
      </template>
    </main>

    <ProductCreateImageGenerateModal
      :open="imageGenModalOpen"
      :phase="imageGenPhase"
      :image-src="modalUserImageSrc"
      :steps="imageGenSteps"
      :progress="imageGenProgress"
      :detected-name="imageGenDetectedName"
      :preview-description="imageGenPreviewDescription"
      :preview-brand-hint="imageGenPreviewBrandHint"
      :preview-category-hint="imageGenPreviewCategoryHint"
      :error-message="imageGenError"
      @start="onImageGenStart"
      @use-content="onImageGenUseContent"
      @regenerate="onImageGenRegenerate"
      @change-photo="onImageGenChangePhoto"
      @close="imageGenModalOpen = false"
    />

    <ProductCreateContentImproveModal
      :open="contentImproveModalOpen"
      :phase="contentImprovePhase"
      :product-name="form.name.trim()"
      :steps="contentImproveSteps"
      :progress="contentImproveProgress"
      :detected-name="contentImproveDetectedName"
      :preview-description="contentImprovePreviewDescription"
      :preview-brand-hint="contentImprovePreviewBrandHint"
      :preview-category-hint="contentImprovePreviewCategoryHint"
      :error-message="contentImproveError"
      @use-content="onContentImproveUse"
      @regenerate="onContentImproveRegenerate"
      @close="contentImproveModalOpen = false"
    />

    <ProductCreateVideoUgcModal
      :open="videoUgcModalOpen"
      :phase="videoUgcPhase"
      :product-name="form.name.trim()"
      :status-text="generativeStatus"
      :error-message="videoUgcError"
      :video="videoUgcResult"
      @close="videoUgcModalOpen = false"
      @regenerate="onVideoUgcRegenerate"
      @copy="onVideoUgcCopy"
    />

    <ProductCreateAiImagesModal
      :open="aiImagesModalOpen"
      :phase="aiImagesPhase"
      :product-name="form.name.trim()"
      :status-text="generativeStatus"
      :error-message="aiImagesError"
      :images="aiImagesResult"
      :selected-id="aiImagesSelectedId"
      @close="aiImagesModalOpen = false"
      @regenerate="onAiImagesRegenerate"
      @select="onAiImagesSelect"
      @use="onAiImagesUse"
    />
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import FormToggle from '../../components/FormToggle.vue'
import ProductCategoryTreePick from '../../components/store/ProductCategoryTreePick.vue'
import CompletenessPanel from '../../components/store/ProductCreateCompletenessPanel.vue'
import ProductCreateVariationsPanel from '../../components/store/ProductCreateVariationsPanel.vue'
import ProductCreateImageGenerateModal from '../../components/store/ProductCreateImageGenerateModal.vue'
import ProductCreateContentImproveModal from '../../components/store/ProductCreateContentImproveModal.vue'
import ProductCreateVideoUgcModal from '../../components/store/ProductCreateVideoUgcModal.vue'
import ProductCreateAiImagesModal from '../../components/store/ProductCreateAiImagesModal.vue'
import { parseGenerativeOutputEvent, watchGenerativeProcess } from '../../composables/useGenerativeRun'
import {
  clearGenerativeDraft,
  hasSuccessfulImageGenerationDraft,
  loadGenerativeDraft,
  saveGenerativeDraft
} from '../../utils/productCreateGenerativeDraft'
import {
  clearProductCreateImageDraft,
  loadProductCreateImageDraft,
  saveProductCreateImageDraft
} from '../../utils/productCreateImageDraft'

const featureCards = [
  { title: 'Imagen', body: 'Define la foto principal de la ficha en Gravity.' },
  { title: 'Ficha', body: 'Confirma nombre, descripción, marca y categoría.' },
  { title: 'Catálogo', body: 'Activa visibilidad en listados y búsqueda.' }
]

const route = useRoute()
const router = useRouter()
const toast = useToast()
const saving = ref(false)
const loadProductError = ref(null)
const editingProductId = ref('')

const isEditMode = computed(() => {
  const id = route.params.productId
  return typeof id === 'string' ? id.trim().length > 0 : Array.isArray(id) && !!id[0]
})

const routeProductId = computed(() => {
  const id = route.params.productId
  if (typeof id === 'string') return id.trim()
  if (Array.isArray(id) && id[0]) return String(id[0]).trim()
  return ''
})

const loadingProduct = ref(!!routeProductId.value)
const formError = ref(null)
const mode = ref('image')
const step = ref(1)
const imagePreviewOk = ref(false)
const imageFile = ref(null)
const imageObjectUrl = ref('')
const imageAttachmentId = ref(null)
const fileInput = ref(null)
const galleryFileInput = ref(null)
const dragging = ref(false)
const imageUploading = ref(false)

const form = reactive({
  name: '',
  description: '',
  brandId: '',
  brandName: '',
  categoryId: '',
  categoryPath: '',
  imageUrl: '',
  isActive: true,
  showInCatalog: true
})

const imagePreviewSrc = computed(() => imageObjectUrl.value || form.imageUrl.trim() || '')
const canContinueFromPhoto = computed(() => !!imageFile.value || (!!form.imageUrl.trim() && imagePreviewOk.value))

const brandQuery = ref('')
const brandOpen = ref(false)
const brandLoading = ref(false)
const brandResults = ref([])
let brandTimer = null

const variationsPanelRef = ref(null)

const categoryPickerOpen = ref(false)
const categoryTree = ref([])
const categoryLoading = ref(false)
const categoryError = ref(null)
const categoryExpanded = ref({})

const bulletPlaceholders = [
  'Ej: Ultraligeras y cómodas',
  'Ej: Malla transpirable',
  'Ej: Suela antideslizante',
  'Ej: Ideal para uso diario'
]
const bullets = ref(['', '', ''])

const contentModeOptions = [
  { id: 'all', label: 'Todo' },
  { id: 'description', label: 'Descripción' },
  { id: 'bullets', label: 'Bullets' },
  { id: 'seo', label: 'SEO' }
]
const contentModes = ref(['all'])
const generativeBusy = ref(false)
const generativeStatus = ref('')
const lastProcessId = ref(null)
let generativeController = null

const videoUgcModalOpen = ref(false)
const videoUgcPhase = ref('running')
const videoUgcError = ref('')
const videoUgcResult = ref(null)

const aiImagesModalOpen = ref(false)
const aiImagesPhase = ref('running')
const aiImagesError = ref('')
const aiImagesResult = ref([])
const aiImagesSelectedId = ref('')
const generatedGallery = ref([])

const imageGenModalOpen = ref(false)
const imageGenPhase = ref('ready')
const imageGenError = ref('')
const imageGenProgress = ref(8)
const pendingFicha = ref(null)
const imageGenSteps = ref(createImageGenSteps())

/** True when we can reopen the last successful image generation instead of starting a new one. */
const hasLastImageGeneration = computed(
  () => !!(pendingFicha.value && imageGenPhase.value === 'success') || hasSuccessfulImageGenerationDraft()
)

/** Prefer local blob; fall back to durable server URL after reload. */
const modalUserImageSrc = computed(() => imagePreviewSrc.value)

function createImageGenSteps() {
  return [
    {
      id: 1,
      title: 'Detectando categoría y atributos',
      detail: 'Identificando tipo de producto…',
      status: 'pending'
    },
    {
      id: 2,
      title: 'Extrayendo color, material y estilo',
      detail: 'Leyendo detalles visuales…',
      status: 'pending'
    },
    {
      id: 3,
      title: 'Generando copy vendedor optimizado SEO',
      detail: 'Título + bullets + descripción que convierte',
      status: 'pending'
    }
  ]
}

const imageGenDetectedName = computed(() => {
  const n = pendingFicha.value?.name
  return typeof n === 'string' && n.trim() ? n.trim() : 'tu producto'
})

const imageGenPreviewDescription = computed(() => {
  const d = pendingFicha.value?.description
  return typeof d === 'string' ? d.trim() : ''
})

const imageGenPreviewBrandHint = computed(() => {
  const f = pendingFicha.value
  const b = f?.brandHint
  if (typeof b !== 'string' || !b.trim()) return ''
  const hasId = typeof f?.brandId === 'string' && !!f.brandId.trim()
  return hasId ? `${b.trim()} (existente)` : `${b.trim()} (nueva)`
})

const imageGenPreviewCategoryHint = computed(() => {
  const f = pendingFicha.value
  const c = f?.categoryHint
  if (typeof c !== 'string' || !c.trim()) return ''
  const hasId = typeof f?.categoryId === 'string' && !!f.categoryId.trim()
  return hasId ? `${c.trim()} (existente)` : `${c.trim()} (nueva)`
})

const contentImproveModalOpen = ref(false)
const contentImprovePhase = ref('running')
const contentImproveError = ref('')
const contentImproveProgress = ref(0)
const contentPendingFicha = ref(null)
const contentImproveSteps = ref(createContentImproveSteps())

function createContentImproveSteps() {
  return [
    {
      id: 1,
      title: 'Analizando título y contexto',
      detail: 'Entendiendo el producto a mejorar…',
      status: 'pending'
    },
    {
      id: 2,
      title: 'Generando descripción y bullets',
      detail: 'Redacción comercial en español…',
      status: 'pending'
    },
    {
      id: 3,
      title: 'Optimizando SEO y ficha',
      detail: 'Título SEO + meta description',
      status: 'pending'
    }
  ]
}

const contentImproveDetectedName = computed(() => {
  const n = contentPendingFicha.value?.name || form.name
  return typeof n === 'string' && n.trim() ? n.trim() : 'tu producto'
})

const contentImprovePreviewDescription = computed(() => {
  const d = contentPendingFicha.value?.description
  return typeof d === 'string' ? d.trim() : ''
})

const contentImprovePreviewBrandHint = computed(() => {
  const f = contentPendingFicha.value
  const b = f?.brandHint
  if (typeof b !== 'string' || !b.trim()) return ''
  const hasId = typeof f?.brandId === 'string' && !!f.brandId.trim()
  return hasId ? `${b.trim()} (existente)` : `${b.trim()} (nueva)`
})

const contentImprovePreviewCategoryHint = computed(() => {
  const f = contentPendingFicha.value
  const c = f?.categoryHint
  if (typeof c !== 'string' || !c.trim()) return ''
  const hasId = typeof f?.categoryId === 'string' && !!f.categoryId.trim()
  return hasId ? `${c.trim()} (existente)` : `${c.trim()} (nueva)`
})

onBeforeUnmount(() => {
  generativeController?.abort()
})

const filledBullets = computed(() => bullets.value.map((b) => b.trim()).filter(Boolean))
const hasBullets = computed(() => filledBullets.value.length >= 2)

const hasName = computed(() => !!form.name.trim())
const hasDescription = computed(() => !!form.description.trim())
const hasImage = computed(() => !!imagePreviewSrc.value && imagePreviewOk.value)
const hasBrand = computed(() => !!form.brandId || !!form.brandName.trim())
const hasCategory = computed(() => !!form.categoryId || !!form.categoryPath.trim())
const canSubmit = computed(() => hasName.value && hasDescription.value && hasBrand.value && hasCategory.value)

const checklist = computed(() => [
  { id: 'name', label: 'Título del producto', done: hasName.value },
  { id: 'desc', label: 'Descripción', done: hasDescription.value },
  { id: 'bullets', label: 'Bullets / puntos clave', done: hasBullets.value },
  { id: 'image', label: 'Imagen principal', done: hasImage.value },
  { id: 'brand', label: 'Marca', done: hasBrand.value },
  { id: 'category', label: 'Categoría', done: hasCategory.value }
])

const scorePercent = computed(() => {
  const items = checklist.value
  return Math.round((items.filter((i) => i.done).length / items.length) * 100)
})

const scoreDash = computed(() => {
  const c = 2 * Math.PI * 15.5
  return `${((c * scorePercent.value) / 100).toFixed(2)} ${c.toFixed(2)}`
})

const scoreHint = computed(() => {
  if (scorePercent.value >= 100) return 'Ficha completa. Ya puedes crear.'
  if (scorePercent.value >= 40) return 'Vamos bien, sigue completando.'
  return 'Necesita más datos para destacar en catálogo.'
})

const tipText = computed(() => {
  if (!hasName.value) return 'Tip: usa un título claro (ideal 60–80 caracteres).'
  if (!hasDescription.value) return 'Tip: describe beneficios y materiales en la descripción.'
  if (!hasBullets.value) return 'Tip: añade al menos 2 bullets con beneficios concretos.'
  if (!hasImage.value) return 'Tip: una imagen con fondo claro mejora la ficha.'
  if (!hasBrand.value && !hasCategory.value) return 'Tip: marca y categoría ayudan a filtrar el catálogo.'
  return 'Tip: revisa activo / visible en catálogo antes de crear.'
})

const tipPlain = computed(() => tipText.value.replace(/^Tip:\s*/i, ''))

function addBullet() {
  if (bullets.value.length >= 6) return
  bullets.value.push('')
}

function removeBullet(idx) {
  if (bullets.value.length <= 1) return
  bullets.value.splice(idx, 1)
}

function buildDescriptionPayload() {
  const base = form.description.trim()
  const points = filledBullets.value
  if (!points.length) return base
  const block = points.map((p) => `• ${p}`).join('\n')
  return `${base}\n\n${block}`.slice(0, 8000)
}

function modePillClass(m) {
  return mode.value === m
    ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm border border-gray-200 dark:border-gray-600'
    : 'text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200'
}

function stepBadgeClass(n) {
  if (step.value === n || (n === 1 && step.value > 1)) {
    return 'bg-primary-600 text-white border border-primary-600'
  }
  return 'bg-white dark:bg-gray-800 text-gray-400 border border-gray-200 dark:border-gray-600'
}

function setMode(m) {
  mode.value = m
  formError.value = null
  step.value = m === 'image' ? 1 : 2
}

function goBackToList() {
  if (saving.value) return
  router.push({ name: 'AdminStoreProducts' })
}

function onConfirmBack() {
  if (saving.value) return
  formError.value = null
  if (isEditMode.value) {
    goBackToList()
    return
  }
  if (mode.value === 'image') {
    step.value = 1
    return
  }
  goBackToList()
}

function goConfirm() {
  if (mode.value === 'image') {
    openImageAnalysisReady()
    return
  }
  formError.value = null
  if (form.imageUrl.trim()) imagePreviewOk.value = true
  step.value = 2
}

function openImageAnalysisReady() {
  if (!canContinueFromPhoto.value || imageUploading.value || generativeBusy.value) return
  formError.value = null
  if (!imageFile.value && !imageObjectUrl.value && !form.imageUrl.trim() && !imageAttachmentId.value) {
    formError.value = 'Agrega una imagen para continuar.'
    return
  }

  // Prefer last generation: never force a new run when we already have a usable ficha.
  if (reopenLastImageGenerationIfAny()) return

  resetImageGenSteps()
  imageGenPhase.value = 'ready'
  imageGenModalOpen.value = true
}

/**
 * Reopen success/error/running from draft or in-memory pending ficha.
 * @returns {boolean} true if a previous generation was shown
 */
function reopenLastImageGenerationIfAny() {
  if (pendingFicha.value && (imageGenPhase.value === 'success' || imageGenPhase.value === 'error')) {
    if (imageGenPhase.value === 'success') finalizeImageGenSteps(pendingFicha.value)
    imageGenModalOpen.value = true
    return true
  }

  const draft = loadGenerativeDraft()
  if (!draft || draft.kind !== 'image' || !draft.processId) return false

  lastProcessId.value = draft.processId
  if (draft.imageUrl && !form.imageUrl.trim()) {
    form.imageUrl = draft.imageUrl
    imagePreviewOk.value = true
  }

  if ((draft.phase === 'success' || draft.phase === 'running' || draft.phase === 'applied') && draft.ficha) {
    pendingFicha.value = draft.ficha
    finalizeImageGenSteps(draft.ficha)
    imageGenPhase.value = 'success'
    imageGenModalOpen.value = true
    if (draft.phase === 'running') {
      saveGenerativeDraft({
        processId: draft.processId,
        kind: 'image',
        phase: 'success',
        ficha: draft.ficha,
        imageUrl: form.imageUrl.trim() || draft.imageUrl || null
      })
    }
    return true
  }

  if (draft.phase === 'error') {
    imageGenPhase.value = 'error'
    imageGenError.value = draft.error || 'La generación anterior falló'
    if (draft.ficha) pendingFicha.value = draft.ficha
    imageGenModalOpen.value = true
    return true
  }

  if (draft.phase === 'running' && draft.processId) {
    resetImageGenSteps()
    setActiveStep(0)
    imageGenPhase.value = 'running'
    imageGenModalOpen.value = true
    void followImageGeneration(draft.processId)
    return true
  }

  return false
}

async function continueFromPhoto() {
  openImageAnalysisReady()
}

function onImageGenStart() {
  void startImageGenerationFlow({ regenerate: false })
}

function resetImageGenSteps() {
  imageGenSteps.value = createImageGenSteps()
  imageGenProgress.value = 0
  imageGenError.value = ''
  pendingFicha.value = null
}

/** Mark steps 0..throughIndex as done (green). */
function completeStepsThrough(throughIndex, detailsByIndex = {}) {
  imageGenSteps.value = imageGenSteps.value.map((s, i) => {
    if (i > throughIndex) return s
    return {
      ...s,
      status: 'done',
      detail: detailsByIndex[i] != null ? detailsByIndex[i] : s.detail
    }
  })
}

function setActiveStep(index) {
  imageGenSteps.value = imageGenSteps.value.map((s, i) => {
    if (i < index && s.status !== 'done') return { ...s, status: 'done' }
    if (i === index && s.status !== 'done') return { ...s, status: 'active' }
    return s
  })
}

/**
 * Drive plomo → verde from IEventStreamStore job events:
 * started → status → tool → output → done | error
 */
function applyJobEventToSteps(evt) {
  const type = String(evt?.type || '').toLowerCase()

  if (type === 'started') {
    // Job partition created — first step lights green.
    completeStepsThrough(0)
    setActiveStep(1)
    imageGenProgress.value = 33
    return
  }

  if (type === 'status') {
    completeStepsThrough(0)
    if (imageGenSteps.value[1]?.status !== 'done') setActiveStep(1)
    imageGenProgress.value = Math.max(imageGenProgress.value, 50)
    return
  }

  if (type === 'tool') {
    completeStepsThrough(1)
    setActiveStep(2)
    imageGenProgress.value = Math.max(imageGenProgress.value, 66)
    return
  }

  if (type === 'output') {
    const parsed = parseGenerativeOutputEvent(evt)
    if (parsed?.ficha) {
      pendingFicha.value = parsed.ficha
      finalizeImageGenSteps(parsed.ficha)
      imageGenPhase.value = 'success'
      saveGenerativeDraft({
        processId: lastProcessId.value || '',
        kind: 'image',
        phase: 'success',
        ficha: parsed.ficha,
        imageUrl: form.imageUrl.trim() || null
      })
    } else {
      completeStepsThrough(2)
      imageGenProgress.value = 90
    }
    return
  }

  if (type === 'done') {
    completeStepsThrough(2)
    imageGenProgress.value = 100
  }
}

function finalizeImageGenSteps(ficha) {
  const cat = [ficha?.categoryHint, ficha?.brandHint].filter((x) => typeof x === 'string' && x.trim()).join(' • ')
  const styleBits = []
  if (ficha?.brandHint) styleBits.push(ficha.brandHint)
  if (Array.isArray(ficha?.bullets) && ficha.bullets[0]) styleBits.push(String(ficha.bullets[0]).slice(0, 48))
  const copyBits = []
  if (ficha?.name) copyBits.push(ficha.name)
  if (ficha?.seo?.title) copyBits.push(ficha.seo.title)

  completeStepsThrough(2, {
    0: cat || ficha?.name || 'Categoría detectada',
    1: styleBits.join(' • ') || 'Atributos visuales listos',
    2: copyBits.join(' · ') || 'Copy SEO listo'
  })
  imageGenProgress.value = 100
}

async function startImageGenerationFlow({ regenerate = false } = {}) {
  if (!imageFile.value && !form.imageUrl.trim() && !imageAttachmentId.value) {
    formError.value = 'Agrega una imagen para continuar.'
    return
  }

  resetImageGenSteps()
  imageGenPhase.value = 'running'
  imageGenModalOpen.value = true
  // Steps stay plomo until job events arrive.
  imageUploading.value = true
  generativeBusy.value = true
  generativeStatus.value = 'Subiendo imagen…'

  try {
    const res = await apiService.startGenerativeContentByImageRun({
      imageFile: imageFile.value,
      imageUrl: !imageFile.value ? form.imageUrl.trim() || null : null,
      imageAttachmentId: !imageFile.value ? imageAttachmentId.value : null,
      hint: form.name.trim() || null,
      ...catalogContextForGenerative()
    })
    lastProcessId.value = res.processId || null
    if (res.imageUrl) form.imageUrl = res.imageUrl
    if (res.imageAttachmentId) imageAttachmentId.value = res.imageAttachmentId
    imagePreviewOk.value = true
    imageUploading.value = false
    void saveProductCreateImageDraft({
      blob: imageFile.value || undefined,
      fileName: imageFile.value?.name || undefined,
      contentType: imageFile.value?.type || undefined,
      imageUrl: form.imageUrl.trim() || res.imageUrl || null,
      imageAttachmentId: imageAttachmentId.value
    })
    saveGenerativeDraft({
      processId: res.processId,
      kind: 'image',
      phase: 'running',
      ficha: null,
      imageUrl: form.imageUrl.trim() || res.imageUrl || null
    })
    await followImageGeneration(res.processId)
  } catch (err) {
    const msg = err.response?.data?.error || err.message || 'No se pudo iniciar la generación'
    imageUploading.value = false
    generativeBusy.value = false
    generativeStatus.value = ''
    imageGenPhase.value = 'error'
    imageGenError.value = msg
    imageGenModalOpen.value = true
    if (lastProcessId.value) {
      saveGenerativeDraft({
        processId: lastProcessId.value,
        kind: 'image',
        phase: 'error',
        error: msg,
        imageUrl: form.imageUrl.trim() || null
      })
    }
  }
}

function followImageGeneration(processId) {
  const id = String(processId || '').trim()
  if (!id) {
    imageGenPhase.value = 'error'
    imageGenError.value = 'processId ausente'
    generativeBusy.value = false
    return Promise.resolve()
  }

  generativeController?.abort()
  const controller = new AbortController()
  generativeController = controller
  generativeBusy.value = true
  lastProcessId.value = id

  return watchGenerativeProcess({
    processId: id,
    signal: controller.signal,
    onEvent: (evt) => {
      applyJobEventToSteps(evt)
      if (evt?.type === 'status' && evt.text) generativeStatus.value = String(evt.text)
      if (evt?.type === 'tool' && evt.toolName) {
        generativeStatus.value = `Herramienta: ${evt.toolName}`
      }
      if (evt?.type === 'error') {
        imageGenPhase.value = 'error'
        imageGenError.value = evt.text || 'La generación falló'
        saveGenerativeDraft({
          processId: id,
          kind: 'image',
          phase: 'error',
          error: imageGenError.value,
          ficha: pendingFicha.value,
          imageUrl: form.imageUrl.trim() || null
        })
      }
      if (evt?.type === 'done') {
        if (!pendingFicha.value) {
          const draftFicha = loadGenerativeDraft()?.ficha
          if (draftFicha) pendingFicha.value = draftFicha
        }
        if (pendingFicha.value) {
          imageGenPhase.value = 'success'
          saveGenerativeDraft({
            processId: id,
            kind: 'image',
            phase: 'success',
            ficha: pendingFicha.value,
            imageUrl: form.imageUrl.trim() || null
          })
        } else if (imageGenPhase.value !== 'error') {
          imageGenPhase.value = 'error'
          imageGenError.value = 'La IA no devolvió una ficha usable.'
          saveGenerativeDraft({
            processId: id,
            kind: 'image',
            phase: 'error',
            error: imageGenError.value,
            imageUrl: form.imageUrl.trim() || null
          })
        }
      }
    },
    onError: (err) => {
      imageGenPhase.value = 'error'
      imageGenError.value = err.message || 'No se pudo seguir la generación'
      saveGenerativeDraft({
        processId: id,
        kind: 'image',
        phase: 'error',
        error: imageGenError.value,
        imageUrl: form.imageUrl.trim() || null
      })
    }
  }).finally(() => {
    if (generativeController === controller) {
      generativeBusy.value = false
      generativeStatus.value = ''
      imageUploading.value = false
    }
  })
}

function onImageGenUseContent() {
  if (pendingFicha.value) applyFicha(pendingFicha.value)
  imageGenModalOpen.value = false
  step.value = 2
  // Keep draft as "applied" so reload restores the form — do not clear the key.
  saveGenerativeDraft({
    processId: lastProcessId.value || loadGenerativeDraft()?.processId || '',
    kind: 'image',
    phase: 'applied',
    ficha: pendingFicha.value,
    imageUrl: form.imageUrl.trim() || null
  })
  toast.success('Contenido aplicado a la ficha')
}

function onImageGenRegenerate() {
  clearGenerativeDraft()
  void startImageGenerationFlow({ regenerate: true })
}

function onImageGenChangePhoto() {
  generativeController?.abort()
  imageGenModalOpen.value = false
  imageGenPhase.value = 'ready'
  pendingFicha.value = null
  generativeBusy.value = false
  imageUploading.value = false
  clearGenerativeDraft()
  clearImage()
  step.value = 1
  nextTick(() => fileInput.value?.click())
}

function catalogContextForGenerative() {
  return {
    brandId: form.brandId.trim() || null,
    brandName: form.brandName.trim() || null,
    categoryId: form.categoryId.trim() || null,
    categoryPath: form.categoryPath.trim() || null
  }
}

function applyFicha(ficha) {
  if (!ficha || typeof ficha !== 'object') return
  if (typeof ficha.name === 'string' && ficha.name.trim()) form.name = ficha.name.trim().slice(0, 120)
  if (typeof ficha.description === 'string' && ficha.description.trim()) {
    form.description = ficha.description.trim().slice(0, 8000)
  }
  if (Array.isArray(ficha.bullets)) {
    const next = ficha.bullets
      .map((b) => (typeof b === 'string' ? b.trim() : ''))
      .filter(Boolean)
      .slice(0, 6)
    if (next.length) bullets.value = next.length >= 3 ? next : [...next, ...Array(3 - next.length).fill('')]
  }
  if (typeof ficha.brandHint === 'string' && ficha.brandHint.trim()) {
    const brandName = ficha.brandHint.trim()
    const brandId = typeof ficha.brandId === 'string' ? ficha.brandId.trim() : ''
    // Reuse only when we have a real id (never trust isNewBrand=false alone).
    if (brandId) {
      form.brandId = brandId
      form.brandName = brandName
      brandQuery.value = brandName
    } else if (!form.brandId) {
      form.brandId = ''
      form.brandName = brandName
      brandQuery.value = brandName
    }
  }
  if (typeof ficha.categoryHint === 'string' && ficha.categoryHint.trim()) {
    const path = ficha.categoryHint.trim()
    const categoryId = typeof ficha.categoryId === 'string' ? ficha.categoryId.trim() : ''
    if (categoryId) {
      form.categoryId = categoryId
      form.categoryPath = path
    } else if (!form.categoryId) {
      form.categoryId = ''
      form.categoryPath = path
    }
  }
}

function followGenerativeProcess(processId, kind = 'content') {
  const id = String(processId || '').trim()
  if (!id) {
    generativeBusy.value = false
    generativeStatus.value = ''
    return Promise.resolve()
  }

  generativeController?.abort()
  const controller = new AbortController()
  generativeController = controller
  generativeBusy.value = true
  generativeStatus.value =
    kind === 'video' ? 'Generando guion UGC…' : kind === 'images' ? 'Generando imagen…' : 'Generando ficha…'
  lastProcessId.value = id

  return watchGenerativeProcess({
    processId: id,
    signal: controller.signal,
    onEvent: (evt) => {
      if (evt?.type === 'status' && evt.text) {
        generativeStatus.value = String(evt.text)
      }
      if (evt?.type === 'tool' && evt.toolName) {
        generativeStatus.value = `Herramienta: ${evt.toolName}`
      }
      const parsed = parseGenerativeOutputEvent(evt)
      if (parsed?.ficha) {
        applyFicha(parsed.ficha)
        generativeStatus.value = parsed.message || 'Ficha lista'
      }
      if (parsed?.video) {
        videoUgcResult.value = parsed.video
        videoUgcPhase.value = 'success'
        videoUgcModalOpen.value = true
        generativeStatus.value = parsed.message || 'Guion UGC listo'
      }
      if (parsed?.images?.length) {
        const normalized = normalizeGeneratedImages(parsed.images)
        aiImagesResult.value = normalized
        aiImagesSelectedId.value = normalized[0]?.attachmentId || ''
        aiImagesPhase.value = 'success'
        aiImagesModalOpen.value = true
        generativeStatus.value = parsed.message || 'Imágenes listas'
        for (const img of normalized) {
          const exists = generatedGallery.value.some(
            (g) => g.attachmentId && g.attachmentId === img.attachmentId
          )
          if (!exists) generatedGallery.value = [img, ...generatedGallery.value].slice(0, 4)
        }
      }
      if (evt?.type === 'error') {
        const msg = evt.text || 'La generación falló'
        if (kind === 'video') {
          videoUgcPhase.value = 'error'
          videoUgcError.value = msg
          videoUgcModalOpen.value = true
        } else if (kind === 'images') {
          aiImagesPhase.value = 'error'
          aiImagesError.value = msg
          aiImagesModalOpen.value = true
        } else {
          toast.error(msg)
        }
      }
      if (evt?.type === 'done') {
        if (kind === 'content') toast.success('Ficha generada')
        if (kind === 'video' && videoUgcPhase.value === 'running') {
          videoUgcPhase.value = videoUgcResult.value ? 'success' : 'error'
          if (!videoUgcResult.value) {
            videoUgcError.value = 'La IA no devolvió un guion UGC usable.'
          }
          videoUgcModalOpen.value = true
        }
        if (kind === 'images' && aiImagesPhase.value === 'running') {
          aiImagesPhase.value = aiImagesResult.value.length ? 'success' : 'error'
          if (!aiImagesResult.value.length) {
            aiImagesError.value =
              'La IA no devolvió imágenes. Configura AzureOpenAI:ImageDeployment (p. ej. dall-e-3).'
          }
          aiImagesModalOpen.value = true
        }
      }
    },
    onError: (err) => {
      const msg = err.message || 'No se pudo seguir la generación'
      if (kind === 'video') {
        videoUgcPhase.value = 'error'
        videoUgcError.value = msg
        videoUgcModalOpen.value = true
      } else if (kind === 'images') {
        aiImagesPhase.value = 'error'
        aiImagesError.value = msg
        aiImagesModalOpen.value = true
      } else {
        toast.error(msg)
      }
    }
  }).finally(() => {
    if (generativeController === controller) {
      generativeBusy.value = false
      generativeStatus.value = ''
    }
  })
}

async function startContentByImage() {
  if (generativeBusy.value) return
  if (!imageFile.value && !imageObjectUrl.value && !form.imageUrl.trim() && !imageAttachmentId.value) return
  openImageAnalysisReady()
}

async function startContentByName() {
  if (!hasName.value || generativeBusy.value) return
  await startContentImproveFlow()
}

function resetContentImproveSteps() {
  contentImproveSteps.value = createContentImproveSteps()
  contentImproveProgress.value = 0
  contentImproveError.value = ''
  contentPendingFicha.value = null
}

function completeContentStepsThrough(throughIndex, detailsByIndex = {}) {
  contentImproveSteps.value = contentImproveSteps.value.map((s, i) => {
    if (i > throughIndex) return s
    return {
      ...s,
      status: 'done',
      detail: detailsByIndex[i] != null ? detailsByIndex[i] : s.detail
    }
  })
}

function setContentActiveStep(index) {
  contentImproveSteps.value = contentImproveSteps.value.map((s, i) => {
    if (i < index && s.status !== 'done') return { ...s, status: 'done' }
    if (i === index && s.status !== 'done') return { ...s, status: 'active' }
    return s
  })
}

function finalizeContentImproveSteps(ficha) {
  completeContentStepsThrough(2, {
    0: ficha?.name || form.name.trim() || 'Título listo',
    1: Array.isArray(ficha?.bullets) && ficha.bullets.length
      ? `${ficha.bullets.length} bullets generados`
      : 'Descripción lista',
    2: ficha?.seo?.title || 'SEO listo'
  })
  contentImproveProgress.value = 100
}

function applyContentJobEventToSteps(evt) {
  const type = String(evt?.type || '').toLowerCase()
  if (type === 'started') {
    completeContentStepsThrough(0)
    setContentActiveStep(1)
    contentImproveProgress.value = 33
    return
  }
  if (type === 'status') {
    completeContentStepsThrough(0)
    if (contentImproveSteps.value[1]?.status !== 'done') setContentActiveStep(1)
    contentImproveProgress.value = Math.max(contentImproveProgress.value, 50)
    return
  }
  if (type === 'tool') {
    completeContentStepsThrough(1)
    setContentActiveStep(2)
    contentImproveProgress.value = Math.max(contentImproveProgress.value, 66)
    return
  }
  if (type === 'output') {
    const parsed = parseGenerativeOutputEvent(evt)
    if (parsed?.ficha) {
      contentPendingFicha.value = parsed.ficha
      finalizeContentImproveSteps(parsed.ficha)
      saveGenerativeDraft({
        processId: lastProcessId.value || '',
        kind: 'content',
        phase: 'running',
        ficha: parsed.ficha,
        productName: form.name.trim() || null
      })
    } else {
      completeContentStepsThrough(2)
      contentImproveProgress.value = 90
    }
    return
  }
  if (type === 'done') {
    completeContentStepsThrough(2)
    contentImproveProgress.value = 100
  }
}

async function startContentImproveFlow() {
  if (!hasName.value) return

  resetContentImproveSteps()
  setContentActiveStep(0)
  contentImproveProgress.value = 8
  contentImprovePhase.value = 'running'
  contentImproveModalOpen.value = true
  generativeBusy.value = true
  generativeStatus.value = 'Mejorando contenido…'

  try {
    const res = await apiService.startGenerativeContentRun({
      name: form.name.trim(),
      modes: contentModes.value,
      ...catalogContextForGenerative()
    })
    lastProcessId.value = res.processId || null
    saveGenerativeDraft({
      processId: res.processId,
      kind: 'content',
      phase: 'running',
      ficha: null,
      productName: form.name.trim() || null
    })
    await followContentImprove(res.processId)
  } catch (err) {
    contentImprovePhase.value = 'error'
    contentImproveError.value = err.response?.data?.error || err.message || 'No se pudo mejorar el contenido'
    generativeBusy.value = false
    generativeStatus.value = ''
    if (lastProcessId.value) {
      saveGenerativeDraft({
        processId: lastProcessId.value,
        kind: 'content',
        phase: 'error',
        error: contentImproveError.value,
        productName: form.name.trim() || null
      })
    }
  }
}

function followContentImprove(processId) {
  const id = String(processId || '').trim()
  if (!id) {
    contentImprovePhase.value = 'error'
    contentImproveError.value = 'processId ausente'
    generativeBusy.value = false
    return Promise.resolve()
  }

  generativeController?.abort()
  const controller = new AbortController()
  generativeController = controller
  generativeBusy.value = true
  lastProcessId.value = id

  return watchGenerativeProcess({
    processId: id,
    signal: controller.signal,
    onEvent: (evt) => {
      applyContentJobEventToSteps(evt)
      if (evt?.type === 'status' && evt.text) generativeStatus.value = String(evt.text)
      if (evt?.type === 'error') {
        contentImprovePhase.value = 'error'
        contentImproveError.value = evt.text || 'La generación falló'
        saveGenerativeDraft({
          processId: id,
          kind: 'content',
          phase: 'error',
          error: contentImproveError.value,
          ficha: contentPendingFicha.value,
          productName: form.name.trim() || null
        })
      }
      if (evt?.type === 'done') {
        if (contentPendingFicha.value) {
          contentImprovePhase.value = 'success'
          saveGenerativeDraft({
            processId: id,
            kind: 'content',
            phase: 'success',
            ficha: contentPendingFicha.value,
            productName: form.name.trim() || null
          })
        } else if (contentImprovePhase.value !== 'error') {
          contentImprovePhase.value = 'error'
          contentImproveError.value = 'La IA no devolvió una ficha usable.'
          saveGenerativeDraft({
            processId: id,
            kind: 'content',
            phase: 'error',
            error: contentImproveError.value,
            productName: form.name.trim() || null
          })
        }
      }
    },
    onError: (err) => {
      contentImprovePhase.value = 'error'
      contentImproveError.value = err.message || 'No se pudo seguir la generación'
      saveGenerativeDraft({
        processId: id,
        kind: 'content',
        phase: 'error',
        error: contentImproveError.value,
        productName: form.name.trim() || null
      })
    }
  }).finally(() => {
    if (generativeController === controller) {
      generativeBusy.value = false
      generativeStatus.value = ''
    }
  })
}

function onContentImproveUse() {
  if (contentPendingFicha.value) applyFicha(contentPendingFicha.value)
  contentImproveModalOpen.value = false
  saveGenerativeDraft({
    processId: lastProcessId.value || loadGenerativeDraft()?.processId || '',
    kind: 'content',
    phase: 'applied',
    ficha: contentPendingFicha.value,
    productName: form.name.trim() || null
  })
  toast.success('Contenido aplicado a la ficha')
}

function onContentImproveRegenerate() {
  clearGenerativeDraft()
  void startContentImproveFlow()
}

async function startVideoStub() {
  if (!hasName.value || generativeBusy.value) return
  videoUgcResult.value = null
  videoUgcError.value = ''
  videoUgcPhase.value = 'running'
  videoUgcModalOpen.value = true
  generativeBusy.value = true
  generativeStatus.value = 'Iniciando…'
  try {
    const res = await apiService.startGenerativeVideoRun({
      name: form.name.trim(),
      description: form.description.trim() || null,
      imageUrl: form.imageUrl.trim() || null
    })
    await followGenerativeProcess(res.processId, 'video')
  } catch (err) {
    videoUgcPhase.value = 'error'
    videoUgcError.value = err.response?.data?.error || err.message || 'No se pudo iniciar el video'
    videoUgcModalOpen.value = true
    generativeBusy.value = false
    generativeStatus.value = ''
  }
}

function resolveAttachmentDisplayUrl(urlOrPath) {
  const raw = String(urlOrPath || '').trim()
  if (!raw) return ''
  if (/^https?:\/\//i.test(raw) || raw.startsWith('blob:') || raw.startsWith('data:')) return raw
  const base = (import.meta.env.VITE_API_URL || '/api').replace(/\/$/, '')
  return raw.startsWith('/') ? `${base}${raw}` : `${base}/${raw}`
}

function normalizeGeneratedImages(list) {
  if (!Array.isArray(list)) return []
  return list
    .map((img) => {
      if (!img || typeof img !== 'object') return null
      const attachmentId = typeof img.attachmentId === 'string' ? img.attachmentId.trim() : ''
      const url = typeof img.url === 'string' ? img.url.trim() : ''
      const displayUrl = resolveAttachmentDisplayUrl(url)
      if (!displayUrl) return null
      return {
        attachmentId,
        url,
        displayUrl,
        prompt: typeof img.prompt === 'string' ? img.prompt : ''
      }
    })
    .filter(Boolean)
}

async function startAiImages() {
  if (!hasName.value || generativeBusy.value) return
  aiImagesResult.value = []
  aiImagesSelectedId.value = ''
  aiImagesError.value = ''
  aiImagesPhase.value = 'running'
  aiImagesModalOpen.value = true
  generativeBusy.value = true
  generativeStatus.value = 'Iniciando…'
  try {
    const res = await apiService.startGenerativeImagesRun({
      name: form.name.trim(),
      description: form.description.trim() || null,
      count: 1
    })
    await followGenerativeProcess(res.processId, 'images')
  } catch (err) {
    aiImagesPhase.value = 'error'
    aiImagesError.value = err.response?.data?.error || err.message || 'No se pudo iniciar la generación'
    aiImagesModalOpen.value = true
    generativeBusy.value = false
    generativeStatus.value = ''
  }
}

function onAiImagesRegenerate() {
  void startAiImages()
}

function onAiImagesSelect(img) {
  if (img?.attachmentId) aiImagesSelectedId.value = img.attachmentId
}

function applyGeneratedImage(img) {
  if (!img?.displayUrl) return
  if (imageObjectUrl.value) {
    URL.revokeObjectURL(imageObjectUrl.value)
    imageObjectUrl.value = ''
  }
  imageFile.value = null
  form.imageUrl = img.displayUrl
  imageAttachmentId.value = img.attachmentId || null
  imagePreviewOk.value = true
  const exists = generatedGallery.value.some((g) => g.attachmentId && g.attachmentId === img.attachmentId)
  if (!exists) {
    generatedGallery.value = [img, ...generatedGallery.value].slice(0, 4)
  }
}

function onAiImagesUse(img) {
  applyGeneratedImage(img)
  aiImagesModalOpen.value = false
  toast.success('Imagen aplicada a la ficha')
}

function onVideoUgcRegenerate() {
  void startVideoStub()
}

async function onVideoUgcCopy() {
  const text = [videoUgcResult.value?.script, ...(Array.isArray(videoUgcResult.value?.hooks) ? videoUgcResult.value.hooks : [])]
    .filter((x) => typeof x === 'string' && x.trim())
    .join('\n\n')
  if (!text) {
    toast.error('No hay guion para copiar')
    return
  }
  try {
    await navigator.clipboard.writeText(text)
    toast.success('Guion copiado')
  } catch {
    toast.error('No se pudo copiar al portapapeles')
  }
}

function onGalleryFileChange(e) {
  const file = e?.target?.files?.[0]
  if (e?.target) e.target.value = ''
  if (file) setImageFile(file)
}

function onImageUrlInput() {
  imagePreviewOk.value = !!form.imageUrl.trim()
}

function setImageFile(file) {
  if (!file || !file.type?.startsWith('image/')) {
    formError.value = 'Selecciona un archivo de imagen válido'
    return
  }
  if (file.size > 8 * 1024 * 1024) {
    formError.value = 'La imagen debe pesar máximo 8MB.'
    return
  }
  imageFile.value = file
  if (imageObjectUrl.value) URL.revokeObjectURL(imageObjectUrl.value)
  imageObjectUrl.value = URL.createObjectURL(file)
  imagePreviewOk.value = true
  formError.value = null
  dragging.value = false
  void saveProductCreateImageDraft({
    blob: file,
    fileName: file.name || 'product.jpg',
    contentType: file.type || 'image/jpeg',
    imageUrl: form.imageUrl.trim() || null,
    imageAttachmentId: imageAttachmentId.value
  })
  // Do not start analysis automatically — user clicks "Iniciar análisis".
}

function onFileChange(event) {
  const file = event.target?.files?.[0]
  if (!file) return
  setImageFile(file)
  event.target.value = ''
}

function onDrop(event) {
  dragging.value = false
  if (imageUploading.value) return
  const file = event.dataTransfer?.files?.[0]
  if (file) setImageFile(file)
}

function clearImage() {
  imageFile.value = null
  if (imageObjectUrl.value) URL.revokeObjectURL(imageObjectUrl.value)
  imageObjectUrl.value = ''
  imageAttachmentId.value = null
  form.imageUrl = ''
  imagePreviewOk.value = false
  dragging.value = false
  void clearProductCreateImageDraft()
}

function onBrandInput() {
  brandOpen.value = true
  if (brandTimer) clearTimeout(brandTimer)
  const q = brandQuery.value.trim()
  if (q.length < 1) {
    brandResults.value = []
    return
  }
  brandTimer = setTimeout(async () => {
    brandLoading.value = true
    try {
      brandResults.value = await apiService.autocompleteCatalogBrands(q)
    } catch {
      brandResults.value = []
    } finally {
      brandLoading.value = false
    }
  }, 280)
}

function selectBrand(b) {
  form.brandId = b.brandId
  form.brandName = b.name || ''
  brandQuery.value = ''
  brandResults.value = []
  brandOpen.value = false
}

function clearBrand() {
  form.brandId = ''
  form.brandName = ''
}

function collectExpandIds(nodes, acc = {}) {
  for (const n of nodes || []) {
    if (n.children?.length) {
      acc[n.categoryId] = true
      collectExpandIds(n.children, acc)
    }
  }
  return acc
}

async function openCategoryPicker() {
  categoryPickerOpen.value = true
  if (categoryTree.value.length) return
  categoryLoading.value = true
  categoryError.value = null
  try {
    categoryTree.value = await apiService.listCatalogCategories()
    categoryExpanded.value = collectExpandIds(categoryTree.value, {})
  } catch (err) {
    categoryError.value = err.response?.data?.error || err.message || 'Error al cargar categorías'
    categoryTree.value = []
  } finally {
    categoryLoading.value = false
  }
}

function toggleCategory(id) {
  categoryExpanded.value = { ...categoryExpanded.value, [id]: !categoryExpanded.value[id] }
}

function findPath(nodes, targetId, trail = []) {
  for (const n of nodes || []) {
    const next = [...trail, n.name || n.categoryId]
    if (n.categoryId === targetId) return { node: n, path: next.join(' > ') }
    const hit = findPath(n.children, targetId, next)
    if (hit) return hit
  }
  return null
}

function selectCategory(node) {
  const hit = findPath(categoryTree.value, node.categoryId)
  form.categoryId = node.categoryId
  form.categoryPath = hit?.path || node.name || node.categoryId
  categoryPickerOpen.value = false
}

function toggleContentMode(id) {
  if (id === 'all') {
    contentModes.value = ['all']
    return
  }
  let next = contentModes.value.filter((m) => m !== 'all')
  if (next.includes(id)) next = next.filter((m) => m !== id)
  else next = [...next, id]
  contentModes.value = next.length ? next : ['all']
}

async function submit() {
  if (!canSubmit.value) return
  if (step.value !== 2) {
    goConfirm()
    return
  }
  saving.value = true
  formError.value = null
  try {
    const payload = {
      name: form.name.trim(),
      description: buildDescriptionPayload(),
      brandId: form.brandId.trim() || null,
      brandName: form.brandName.trim() || null,
      categoryId: form.categoryId.trim() || null,
      categoryPath: form.categoryPath.trim() || null,
      imageUrl: form.imageUrl.trim() || null,
      isActive: form.isActive,
      showInCatalog: form.showInCatalog
    }
    if (isEditMode.value && editingProductId.value) {
      await apiService.updateCatalogProduct(editingProductId.value, payload)
      toast.success('Producto actualizado')
    } else {
      const variations =
        typeof variationsPanelRef.value?.toPayload === 'function'
          ? variationsPanelRef.value.toPayload(form.name.trim())
          : []
      await apiService.createCatalogProduct({ ...payload, variations })
      toast.success('Producto creado')
    }
    clearGenerativeDraft()
    void clearProductCreateImageDraft()
    router.push({ name: 'AdminStoreProducts' })
  } catch (err) {
    const msg =
      err.response?.data?.error ||
      err.message ||
      (isEditMode.value ? 'No se pudo actualizar el producto' : 'No se pudo crear el producto')
    formError.value = msg
    toast.error(msg)
  } finally {
    saving.value = false
  }
}

function pickProductImageUrl(parsed) {
  if (!parsed || typeof parsed !== 'object') return ''
  if (typeof parsed.imageUrl === 'string' && parsed.imageUrl.trim()) return parsed.imageUrl.trim()
  const images = parsed.images || parsed.Images
  if (Array.isArray(images)) {
    for (const img of images) {
      const link = img?.link || img?.Link || img?.url || img?.Url
      if (typeof link === 'string' && link.trim()) return link.trim()
    }
  }
  const skus = parsed.skus || parsed.Skus || parsed.variations || parsed.Variations
  if (Array.isArray(skus)) {
    for (const sku of skus) {
      const list = sku?.imageList || sku?.ImageList || sku?.images || sku?.Images
      if (!Array.isArray(list)) continue
      for (const img of list) {
        const link = img?.link || img?.Link || img?.url || img?.Url
        if (typeof link === 'string' && link.trim()) return link.trim()
      }
    }
  }
  return ''
}

function applyProductDetail(detail) {
  let parsed = {}
  if (detail?.payloadJson) {
    try {
      parsed = JSON.parse(detail.payloadJson)
    } catch {
      parsed = {}
    }
  }

  editingProductId.value = detail.productId || routeProductId.value
  form.name = (detail.name || parsed.name || '').trim().slice(0, 120)
  const desc = parsed.description || parsed.Description || ''
  form.description = typeof desc === 'string' ? desc.trim().slice(0, 8000) : ''
  form.brandId = String(parsed.brand?.id || parsed.brandId || '').trim()
  form.brandName = String(parsed.brand?.name || parsed.brandName || '').trim()
  form.categoryId = String(parsed.category?.id || parsed.categoryId || '').trim()
  form.categoryPath = String(parsed.category?.path || parsed.categoryPath || '').trim()
  form.imageUrl = pickProductImageUrl(parsed)
  form.isActive = parsed.isActive !== false && parsed.IsActive !== false
  form.showInCatalog = parsed.showInCatalog !== false && parsed.ShowInCatalog !== false

  if (form.imageUrl) {
    imagePreviewOk.value = true
  }

  // Prefer manual confirm view when editing an existing product.
  mode.value = 'manual'
  step.value = 2
}

async function loadProductForEdit() {
  const id = routeProductId.value
  if (!id) return
  loadingProduct.value = true
  loadProductError.value = null
  try {
    const detail = await apiService.getCatalogProduct(id)
    if (!detail?.productId) {
      loadProductError.value = 'Producto no encontrado.'
      return
    }
    applyProductDetail(detail)
  } catch (err) {
    loadProductError.value =
      err.response?.data?.error || err.message || 'No se pudo cargar el producto'
  } finally {
    loadingProduct.value = false
  }
}

onMounted(() => {
  if (isEditMode.value) {
    void loadProductForEdit()
    return
  }
  void restoreCreatePageDrafts()
})

async function restoreCreatePageDrafts() {
  await restoreImageDraftIfAny()
  // Prefer web-storage draft; fall back to IndexedDB generative snapshot.
  if (!loadGenerativeDraft()) {
    const imageDraft = await loadProductCreateImageDraft()
    if (imageDraft?.generative?.processId) {
      saveGenerativeDraft(imageDraft.generative)
    }
  }
  restoreGenerativeDraftIfAny()
}

/**
 * Restore uploaded photo (IndexedDB blob and/or server URL) after reload.
 */
async function restoreImageDraftIfAny() {
  const draft = await loadProductCreateImageDraft()
  if (!draft) return

  if (draft.blob) {
    const type = draft.contentType || draft.blob.type || 'image/jpeg'
    const name = draft.fileName || 'product.jpg'
    const file = new File([draft.blob], name, { type })
    imageFile.value = file
    if (imageObjectUrl.value) URL.revokeObjectURL(imageObjectUrl.value)
    imageObjectUrl.value = URL.createObjectURL(file)
    imagePreviewOk.value = true
  }

  if (draft.imageUrl && !form.imageUrl.trim()) {
    form.imageUrl = draft.imageUrl
    imagePreviewOk.value = true
  }
  if (draft.imageAttachmentId && !imageAttachmentId.value) {
    imageAttachmentId.value = draft.imageAttachmentId
  }
  if (imagePreviewOk.value) mode.value = 'image'
}

/**
 * After reload: reopen last generation (success) or resume polling (running).
 * Does not start a new generative run.
 */
function restoreGenerativeDraftIfAny() {
  const draft = loadGenerativeDraft()
  if (!draft?.processId) return

  lastProcessId.value = draft.processId

  if (draft.kind === 'image') {
    if (draft.imageUrl && !form.imageUrl.trim()) {
      form.imageUrl = draft.imageUrl
      imagePreviewOk.value = true
    }
    mode.value = 'image'

    // Already applied to the form previously — restore fields and stay on confirm step (no modal).
    if (draft.phase === 'applied' && draft.ficha) {
      pendingFicha.value = draft.ficha
      applyFicha(draft.ficha)
      imageGenPhase.value = 'success'
      step.value = 2
      toast.info('Se recuperó el contenido aplicado de la última generación')
      return
    }

    // If we already have a ficha (success, or running that already emitted output), show it — do not ask to generate again.
    if (draft.ficha && (draft.phase === 'success' || draft.phase === 'running')) {
      pendingFicha.value = draft.ficha
      finalizeImageGenSteps(draft.ficha)
      imageGenPhase.value = 'success'
      imageGenModalOpen.value = true
      if (draft.phase !== 'success') {
        saveGenerativeDraft({
          processId: draft.processId,
          kind: 'image',
          phase: 'success',
          ficha: draft.ficha,
          imageUrl: form.imageUrl.trim() || draft.imageUrl || null
        })
      }
      toast.info('Se recuperó la última generación con IA')
      return
    }

    if (draft.phase === 'error') {
      imageGenPhase.value = 'error'
      imageGenError.value = draft.error || 'La generación anterior falló'
      if (draft.ficha) pendingFicha.value = draft.ficha
      imageGenModalOpen.value = true
      return
    }

    if (draft.phase === 'running') {
      resetImageGenSteps()
      setActiveStep(0)
      imageGenPhase.value = 'running'
      imageGenModalOpen.value = true
      toast.info('Reanudando la generación en curso…')
      void followImageGeneration(draft.processId)
    }
    return
  }

  if (draft.kind === 'content') {
    mode.value = 'manual'
    step.value = 2
    if (draft.productName && !form.name.trim()) {
      form.name = String(draft.productName).slice(0, 120)
    }
    if (draft.phase === 'applied' && draft.ficha) {
      contentPendingFicha.value = draft.ficha
      applyFicha(draft.ficha)
      contentImprovePhase.value = 'success'
      toast.info('Se recuperó el contenido aplicado de la última generación')
      return
    }
    if ((draft.phase === 'success' || draft.phase === 'running') && draft.ficha) {
      contentPendingFicha.value = draft.ficha
      finalizeContentImproveSteps(draft.ficha)
      contentImprovePhase.value = 'success'
      contentImproveModalOpen.value = true
      toast.info('Se recuperó la última generación con IA')
      return
    }
    if (draft.phase === 'error') {
      contentImprovePhase.value = 'error'
      contentImproveError.value = draft.error || 'La generación anterior falló'
      contentImproveModalOpen.value = true
      return
    }
    if (draft.phase === 'running') {
      resetContentImproveSteps()
      setContentActiveStep(0)
      contentImproveProgress.value = 8
      contentImprovePhase.value = 'running'
      contentImproveModalOpen.value = true
      if (draft.ficha) {
        contentPendingFicha.value = draft.ficha
        finalizeContentImproveSteps(draft.ficha)
      }
      toast.info('Reanudando la generación en curso…')
      void followContentImprove(draft.processId)
    }
  }
}
</script>

<template>
  <div class="space-y-4">
    <!-- OpenAPI Schema Definition -->
    <div
      v-if="isOpenApi && (modelValue.schemaSource === 'inline' || modelValue.schemaSource === 'url')"
      class="space-y-4"
    >
      <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
        <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50">
          <h4 class="text-sm font-semibold text-gray-900 dark:text-white">OpenAPI Schema Definition</h4>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
            <template v-if="modelValue.schemaSource === 'inline'">Especifique un schema OpenAPI en formato JSON.</template>
            <template v-else>Cargue el schema desde la URL para obtener las acciones.</template>
            <a href="https://spec.openapis.org/oas/latest.html" target="_blank" rel="noopener" class="text-primary-600 dark:text-primary-400 hover:underline ml-1">Más sobre OpenAPI</a>
          </p>
        </div>
        <div v-if="modelValue.schemaSource === 'url'" class="p-4 space-y-2">
          <p class="text-xs text-amber-600 dark:text-amber-400">
            Ejecute <strong>Obtener Acciones</strong> para descargar y guardar el schema. Así podrá usar Agregar acción correctamente.
          </p>
          <div class="flex gap-2">
            <button
              type="button"
              :disabled="!modelValue.openApiSchemaUrl?.trim() || openApiSchemaLoading"
              class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shadow-sm"
              @click="$emit('fetch-openapi')"
            >
              <svg v-if="openApiSchemaLoading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
              </svg>
              <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" /></svg>
              Obtener Acciones
            </button>
          </div>
        </div>
        <div v-if="modelValue.schemaSource === 'inline'" class="p-4 space-y-4">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Schema JSON <span class="text-red-500">*</span></span>
          <template v-if="inlineMode === 'file'">
            <div class="relative">
              <div v-if="inlineFileLoading" class="absolute inset-0 z-10 flex items-center justify-center rounded-xl bg-white/90 dark:bg-gray-900/90">
                <div class="flex flex-col items-center gap-2">
                  <svg class="w-8 h-8 animate-spin text-primary-600 dark:text-primary-400" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Cargando archivo...</span>
                </div>
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div
                  class="relative flex flex-col items-center justify-center p-6 rounded-xl border-2 border-gray-300 dark:border-gray-600 card-add-hover cursor-pointer"
                  @click="$emit('request-file-load', { formType: mode, key: 'openApiSchema' })"
                  @dragover.prevent
                  @drop.prevent="$emit('request-file-drop', $event, 'openApiSchema')"
                >
                  <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 16m4-4V4" /></svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Commerceión 1: Cargar archivo</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">Arrastra o haz clic para subir .json</span>
                </div>
                <div
                  class="flex flex-col items-center justify-center p-6 rounded-xl border-2 border-gray-300 dark:border-gray-600 card-add-hover cursor-pointer"
                  @click="$emit('update:inlineMode', 'textarea')"
                >
                  <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Commerceión 2: Escribir manualmente</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">Pega o escribe el schema en el textarea</span>
                </div>
              </div>
            </div>
          </template>
          <template v-else>
            <div class="space-y-4">
              <div class="flex flex-wrap items-center gap-x-4 gap-y-3">
                <button
                  type="button"
                  class="inline-flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors shrink-0"
                  @click="$emit('update:inlineMode', 'file')"
                >
                  <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" /></svg>
                  Cambiar a cargar archivo
                </button>
                <div class="h-5 w-px bg-gray-200 dark:bg-gray-600 shrink-0" aria-hidden="true" />
                <div class="flex flex-wrap items-center gap-3">
                  <button type="button" class="inline-flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors border border-gray-300 dark:border-gray-500 shrink-0" title="Copiar" @click="$emit('copy-openapi')">
                    <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
                    Copiar
                  </button>
                  <button type="button" class="inline-flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors border border-gray-300 dark:border-gray-500 shrink-0" title="Formatear y validar JSON" @click="$emit('format-openapi')">
                    <span class="font-mono text-sm">{}</span> Formatear
                  </button>
                  <button type="button" class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium transition-colors shadow-sm shrink-0" @click="$emit('fetch-openapi')">
                    <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" /></svg>
                    Obtener Acciones
                  </button>
                </div>
              </div>
              <JsonEditor
                :model-value="modelValue.openApiSchema"
                placeholder='{"openapi":"3.0.0","info":{"title":"Mi API"},"paths":{...}}'
                height="320px"
                :invalid="!!(openApiValidationError || (touched.openApiSchema && !modelValue.openApiSchema?.trim()))"
                @update:model-value="emitSetting('openApiSchema', $event)"
                @blur="$emit('blur-field', 'openApiSchema')"
              />
            </div>
          </template>
        </div>
      </div>
      <p v-if="openApiValidationError" class="flex items-center gap-2 text-sm text-red-600 dark:text-red-400">
        <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
        {{ openApiValidationError }}
      </p>
      <div v-if="openApiParsed?.info || openApiParsed?.actions?.length" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/50 shadow-sm">
        <div class="p-4 space-y-4">
          <div v-if="openApiParsed.info" class="flex gap-3">
            <div class="p-2.5 rounded-lg bg-primary-50 dark:bg-primary-900/20 shrink-0">
              <svg class="w-5 h-5 text-primary-600 dark:text-primary-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" /></svg>
            </div>
            <div class="min-w-0">
              <div class="font-semibold text-gray-900 dark:text-white">{{ openApiParsed.info.title || 'API' }}</div>
              <div v-if="openApiParsed.info.description" class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ openApiParsed.info.description }}</div>
            </div>
          </div>
          <div v-if="openApiParsed.servers?.length" class="flex flex-wrap gap-x-2 gap-y-1 items-center">
            <span class="text-xs text-gray-500 dark:text-gray-400">URL en schema:</span>
            <span class="text-sm font-mono text-amber-600 dark:text-amber-400 break-all">{{ openApiParsed.servers[0]?.url }}</span>
            <p class="text-xs text-amber-600/90 dark:text-amber-400/90 w-full">Solo conecte a servidores de confianza.</p>
          </div>
          <div v-if="openApiParsed.actions?.length">
            <div class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Acciones encontradas ({{ openApiParsed.actions.length }})</div>
            <div class="space-y-2 max-h-52 overflow-y-auto pr-1">
              <div
                v-for="a in openApiParsed.actions"
                :key="a.operationId || a.path + a.method"
                class="flex items-center justify-between gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700 card-hover"
              >
                <div class="flex items-center gap-3 min-w-0 flex-1">
                  <span class="text-gray-400 shrink-0">{{ a.method === 'GET' ? '←' : '→' }}</span>
                  <div class="min-w-0">
                    <span class="font-mono text-sm font-medium text-gray-900 dark:text-white block truncate">{{ a.operationId || a.path }}</span>
                    <ExpandableDescription v-if="a.summary || a.path" :text="a.summary || a.path" :max-length="100" />
                  </div>
                </div>
                <span class="px-2.5 py-1 rounded-md text-xs font-semibold shrink-0" :class="methodBadgeClass(a.method)">{{ a.method }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Postman Collection Definition -->
    <div
      v-if="isPostman && (modelValue.collectionSource === 'inline' || modelValue.collectionSource === 'url')"
      class="space-y-4"
    >
      <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
        <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50">
          <h4 class="text-sm font-semibold text-gray-900 dark:text-white">Postman Collection Definition</h4>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
            <template v-if="modelValue.collectionSource === 'inline'">Especifique la colección Postman en formato JSON.</template>
            <template v-else>Cargue la colección desde la URL para obtener las acciones.</template>
            <a href="https://learning.postman.com/docs/getting-started/importing-and-exporting-data/" target="_blank" rel="noopener" class="text-primary-600 dark:text-primary-400 hover:underline ml-1">Más sobre Postman</a>
          </p>
        </div>
        <div v-if="modelValue.collectionSource === 'url'" class="p-4 flex gap-2">
          <button
            type="button"
            :disabled="!modelValue.postmanCollectionUrl?.trim() || postmanCollectionLoading"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shadow-sm"
            @click="$emit('fetch-postman')"
          >
            <svg v-if="postmanCollectionLoading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" /></svg>
            Obtener Acciones
          </button>
        </div>
        <div v-if="modelValue.collectionSource === 'inline'" class="p-4 space-y-4">
          <span class="block text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Colección JSON <span class="text-red-500">*</span></span>
          <template v-if="inlineMode === 'file'">
            <div class="relative">
              <div v-if="inlineFileLoading" class="absolute inset-0 z-10 flex items-center justify-center rounded-xl bg-white/90 dark:bg-gray-900/90">
                <div class="flex flex-col items-center gap-2">
                  <svg class="w-8 h-8 animate-spin text-primary-600 dark:text-primary-400" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Cargando archivo...</span>
                </div>
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div
                  class="relative flex flex-col items-center justify-center p-6 rounded-xl border-2 border-gray-300 dark:border-gray-600 card-add-hover cursor-pointer"
                  @click="$emit('request-file-load', { formType: mode, key: 'postmanCollection' })"
                  @dragover.prevent
                  @drop.prevent="$emit('request-file-drop', $event, 'postmanCollection')"
                >
                  <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 16m4-4V4" /></svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Commerceión 1: Cargar archivo</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">Arrastra o haz clic para subir .json</span>
                </div>
                <div
                  class="flex flex-col items-center justify-center p-6 rounded-xl border-2 border-gray-300 dark:border-gray-600 card-add-hover cursor-pointer"
                  @click="$emit('update:inlineMode', 'textarea')"
                >
                  <svg class="w-10 h-10 text-gray-400 dark:text-gray-500 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Commerceión 2: Escribir manualmente</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 mt-1">Pega o escribe la colección en el textarea</span>
                </div>
              </div>
            </div>
          </template>
          <template v-else>
            <div class="flex flex-wrap items-center gap-x-4 gap-y-3 mb-4">
              <button
                type="button"
                class="inline-flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors shrink-0"
                @click="$emit('update:inlineMode', 'file')"
              >
                <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" /></svg>
                Cambiar a cargar archivo
              </button>
              <div class="h-5 w-px bg-gray-200 dark:bg-gray-600 shrink-0" aria-hidden="true" />
              <button
                type="button"
                class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium transition-colors shadow-sm shrink-0"
                @click="$emit('fetch-postman')"
              >
                <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" /></svg>
                Obtener Acciones
              </button>
            </div>
            <JsonEditor
              :model-value="modelValue.postmanCollection"
              placeholder='{"info":{"name":"Mi colección"},"item":[...]}'
              height="240px"
              :invalid="isFieldInvalid('postmanCollection', { key: 'postmanCollection', required: true }, modelValue.postmanCollection)"
              @update:model-value="emitSetting('postmanCollection', $event)"
              @blur="$emit('blur-field', 'postmanCollection')"
            />
          </template>
        </div>
      </div>
      <div v-if="postmanParsed" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/50 shadow-sm">
        <div class="p-4 space-y-4">
          <div v-if="postmanParsed.info && postmanParsed.info.name" class="flex gap-3">
            <div class="p-2.5 rounded-lg bg-primary-50 dark:bg-primary-900/20 shrink-0">
              <svg class="w-5 h-5 text-primary-600 dark:text-primary-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" /></svg>
            </div>
            <div class="min-w-0">
              <div class="font-semibold text-gray-900 dark:text-white">{{ postmanParsed.info.name || 'Colección' }}</div>
            </div>
          </div>
          <div>
            <div class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Acciones encontradas ({{ postmanParsed.actions?.length ?? 0 }})</div>
            <div v-if="postmanParsed.actions?.length" class="space-y-2 max-h-52 overflow-y-auto pr-1">
              <div
                v-for="a in postmanParsed.actions"
                :key="a.name + a.path + a.method"
                class="flex items-center justify-between gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700 card-hover"
              >
                <div class="flex items-center gap-3 min-w-0 flex-1">
                  <span class="text-gray-400 shrink-0">{{ a.method === 'GET' ? '←' : '→' }}</span>
                  <div class="min-w-0">
                    <span class="font-mono text-sm font-medium text-gray-900 dark:text-white block truncate">{{ a.name || a.path }}</span>
                    <ExpandableDescription v-if="a.summary || a.path" :text="a.summary || a.path" :max-length="100" />
                  </div>
                </div>
                <span class="px-2.5 py-1 rounded-md text-xs font-semibold shrink-0" :class="methodBadgeClass(a.method)">{{ a.method }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import JsonEditor from '../JsonEditor.vue'
import ExpandableDescription from '../ExpandableDescription.vue'

const props = defineProps({
  meta: { type: Object, default: null },
  modelValue: { type: Object, required: true },
  mode: { type: String, validator: (v) => v === 'connect' || v === 'edit', default: 'connect' },
  openApiParsed: { type: Object, default: null },
  postmanParsed: { type: Object, default: null },
  openApiValidationError: { type: String, default: '' },
  openApiSchemaLoading: { type: Boolean, default: false },
  postmanCollectionLoading: { type: Boolean, default: false },
  inlineMode: { type: String, validator: (v) => v === 'file' || v === 'textarea', default: 'file' },
  inlineFileLoading: { type: Boolean, default: false },
  touched: { type: Object, default: () => ({}) },
  isFieldInvalid: { type: Function, default: () => false }
})

const emit = defineEmits([
  'update:modelValue',
  'update:inlineMode',
  'fetch-openapi',
  'fetch-postman',
  'request-file-load',
  'request-file-drop',
  'copy-openapi',
  'format-openapi',
  'blur-field'
])

const isOpenApi = computed(() => {
  const m = props.meta
  return m?.name === 'OpenAPI' || m?.key === 'Dynamic'
})

const isPostman = computed(() => props.meta?.key === 'Postman')

function emitSetting(key, value) {
  emit('update:modelValue', { ...props.modelValue, [key]: value })
}

function methodBadgeClass(method) {
  const m = (method || '').toUpperCase()
  if (m === 'GET') return 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300'
  if (m === 'POST') return 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300'
  if (m === 'PUT' || m === 'PATCH') return 'bg-amber-100 dark:bg-amber-900/30 text-amber-800 dark:text-amber-300'
  if (m === 'DELETE') return 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300'
  return 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
}
</script>

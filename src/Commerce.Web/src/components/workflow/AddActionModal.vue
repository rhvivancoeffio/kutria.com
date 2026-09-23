<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="modelValue" class="fixed inset-0 z-50 flex">
        <div class="absolute inset-0 bg-black/50" />
        <div
          class="relative ml-auto w-full max-w-lg lg:max-w-2xl h-full bg-white dark:bg-gray-800 shadow-xl flex flex-col animate-slide-in"
          @click.stop
        >
          <!-- Header -->
          <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ editStep ? 'Editar acción' : 'Añadir acción' }}</h2>
            <button
              type="button"
              @click="$emit('update:modelValue', false)"
              class="p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>

          <!-- Content: pasos 1=Integración, 2=Acción, 3=Output -->
          <div class="flex-1 min-h-0 overflow-y-auto p-4 space-y-6">
            <!-- Paso 1: Integración -->
            <div v-show="modalStep === 1" class="space-y-3">
              <h3 class="text-sm font-medium text-gray-900 dark:text-white">Paso 1 — Integración</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400">Selecciona la integración de la que proviene la acción.</p>
              <div v-if="integrations.length === 0" class="py-6 text-center">
                <NoIntegrationsMessage class-names="text-sm text-amber-600 dark:text-amber-400 m-0" />
              </div>
              <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <button
                  v-for="int in integrations"
                  :key="int.id"
                  type="button"
                  @click="selectIntegration(int)"
                  :class="[
                    'flex items-center gap-3 p-4 rounded-xl border-2 text-left transition-all',
                    selectedIntegration?.id === int.id
                      ? 'border-primary-600 bg-primary-50/50 dark:bg-primary-900/20'
                      : 'border-gray-200 dark:border-gray-600 card-hover bg-white dark:bg-gray-800'
                  ]"
                >
                  <div class="w-10 h-10 rounded-lg bg-gray-100 dark:bg-gray-700 flex items-center justify-center p-1.5 shrink-0 overflow-hidden">
                    <img
                      :src="(int.logoUrl || int.LogoUrl) || '/images/avatar-default.svg'"
                      :alt="int.provider"
                      class="max-h-full max-w-full object-contain"
                      @error="$event.target.src = '/images/avatar-default.svg'"
                    />
                  </div>
                  <div class="min-w-0 flex-1">
                    <p class="font-medium text-gray-900 dark:text-white truncate">{{ int.name }}</p>
                    <p class="text-sm text-gray-500 dark:text-gray-400">{{ int.provider }}</p>
                  </div>
                  <span v-if="selectedIntegration?.id === int.id" class="shrink-0 w-6 h-6 rounded-full bg-primary-600 flex items-center justify-center">
                    <svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" /></svg>
                  </span>
                </button>
              </div>
            </div>

            <!-- Paso 2: Acción -->
            <div v-show="modalStep === 2" class="space-y-3">
              <div v-if="selectedIntegration" class="flex items-center gap-2 py-2 px-3 rounded-lg bg-gray-50 dark:bg-gray-800/50 border border-gray-200 dark:border-gray-600 text-sm text-gray-700 dark:text-gray-300">
                <span class="font-medium">Integración:</span>
                <span>{{ selectedIntegration.name }}</span>
                <button type="button" @click="setModalStep(1)" class="ml-auto text-xs text-primary-600 dark:text-primary-400 hover:underline">Cambiar</button>
              </div>
              <h3 class="text-sm font-medium text-gray-900 dark:text-white">Paso 2 — Acción</h3>
              <p class="text-xs text-gray-500 dark:text-gray-400">Selecciona una o varias acciones. Si eliges una, en el siguiente paso podrás configurar output e input.</p>
              <div v-if="operationsLoading" class="space-y-3">
                <div class="h-10 w-full bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
                <div class="space-y-2 max-h-[28rem] overflow-hidden">
                  <div v-for="i in 6" :key="i" class="flex items-start gap-3 p-3 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50">
                    <div class="w-14 h-6 shrink-0 rounded bg-gray-200 dark:bg-gray-700 animate-pulse" />
                    <div class="flex-1 min-w-0 space-y-2">
                      <div class="h-4 w-4/5 max-w-sm bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                      <div class="h-3 w-2/3 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                    </div>
                  </div>
                </div>
              </div>
              <div v-else-if="operations.length === 0" class="py-8 text-center text-amber-600 dark:text-amber-400">
                <p class="text-sm">No se encontraron acciones en esta integración.</p>
              </div>
              <div v-else class="space-y-3">
                <div v-if="!hideMethodFilter" class="flex flex-wrap gap-2">
                  <button
                    v-for="m in actionMethodFilters"
                    :key="m.value"
                    type="button"
                    @click="actionMethodFilter = m.value"
                    :class="[
                      'px-3 py-1.5 rounded-lg text-xs font-mono font-semibold transition-colors',
                      actionMethodFilter === m.value
                        ? m.activeClass
                        : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600'
                    ]"
                  >{{ m.label }}</button>
                </div>
                <div class="relative">
                  <input
                    v-model="actionSearchQuery"
                    type="text"
                    placeholder="Buscar por nombre, path o descripción..."
                    class="w-full pl-9 pr-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm placeholder-gray-500"
                  />
                  <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                  </svg>
                </div>
                <div v-if="filteredOperations.length > 0" class="flex items-center justify-between gap-2">
                  <span class="text-xs text-gray-500 dark:text-gray-400">{{ selectedOperations.length }} seleccionada(s)</span>
                  <div class="flex gap-2">
                    <button type="button" @click="selectAllOperations" class="text-primary-600 dark:text-primary-400 hover:underline font-medium text-xs">Seleccionar todo</button>
                    <span class="text-gray-400">|</span>
                    <button type="button" @click="selectNoOperations" class="text-gray-500 dark:text-gray-400 hover:underline text-xs">Ninguno</button>
                  </div>
                </div>
                <div class="space-y-2 max-h-[28rem] overflow-y-auto">
                  <button
                    v-for="op in filteredOperations"
                    :key="op.id"
                    type="button"
                    @click="toggleOperation(op)"
                    :class="[
                      'w-full flex items-start gap-3 p-3 rounded-lg border-2 text-left transition-all',
                      isOperationSelected(op)
                        ? 'border-primary-600 bg-primary-50/50 dark:bg-primary-900/20'
                        : 'border-gray-200 dark:border-gray-600 card-hover bg-white dark:bg-gray-800'
                    ]"
                  >
                    <span class="shrink-0 flex items-center justify-center mt-0.5">
                      <input
                        type="checkbox"
                        :checked="isOperationSelected(op)"
                        @click.stop
                        class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 w-4 h-4"
                      />
                    </span>
                    <span
                      :class="[
                        'inline-flex items-center justify-center shrink-0 w-14 py-0.5 rounded text-xs font-mono font-semibold',
                        methodBadgeClass(op.method)
                      ]"
                    >{{ op.method }}</span>
                    <div class="min-w-0 flex-1">
                      <p class="text-sm text-gray-600 dark:text-gray-400 break-all font-mono">{{ op.path }}</p>
                      <p v-if="op.summary" class="mt-1 text-xs text-gray-500 dark:text-gray-400 line-clamp-2">{{ op.summary }}</p>
                    </div>
                    <span v-if="isOperationSelected(op)" class="shrink-0 w-5 h-5 rounded-full bg-primary-600 flex items-center justify-center mt-0.5">
                      <svg class="w-3 h-3 text-white" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" /></svg>
                    </span>
                  </button>
                </div>
                <p v-if="filteredOperations.length === 0 && operations.length > 0" class="text-sm text-amber-600 dark:text-amber-400">Ninguna acción coincide con el método o la búsqueda.</p>
              </div>
              <!-- Referencia: Input Schema (solo cuando hay una acción seleccionada) -->
              <div v-if="selectedOperations.length === 1 && selectedOperation" class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden">
                <div class="px-3 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50">Input Schema (referencia)</div>
                <div class="p-3 space-y-2 max-h-48 overflow-y-auto">
                  <div
                    v-for="(field, idx) in inputSchemaFields"
                    :key="field.name || idx"
                    class="flex items-center justify-between gap-2 p-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 text-sm"
                  >
                    <span class="font-mono text-gray-900 dark:text-white">{{ field.name }}</span>
                    <span class="px-1.5 py-0.5 text-xs rounded bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300">{{ field.type }}</span>
                    <span v-if="field.required" class="text-xs text-amber-600 dark:text-amber-400">requerido</span>
                  </div>
                  <p v-if="inputSchemaFields.length === 0" class="text-xs text-gray-500 dark:text-gray-400 py-2">Sin parámetros definidos</p>
                </div>
                <p class="px-3 py-1.5 text-xs text-gray-500 dark:text-gray-400 border-t border-gray-200 dark:border-gray-600">En el siguiente paso elegirás el output y podrás configurar valores por defecto del input si lo necesitas.</p>
              </div>
            </div>

            <!-- Paso 3: resumen (varias acciones) o Output + Input (una acción) -->
            <div v-show="modalStep === 3" class="space-y-4">
              <!-- Varias acciones: solo resumen -->
              <template v-if="selectedOperations.length > 1">
                <div class="flex flex-wrap items-center gap-2 shrink-0">
                  <span class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Paso 3</span>
                  <span class="text-gray-400">·</span>
                  <span class="text-sm font-medium text-gray-900 dark:text-white">Resumen</span>
                </div>
                <div class="flex flex-wrap items-center gap-2 shrink-0">
                  <span class="inline-flex items-center gap-1.5 py-1.5 px-2.5 rounded-md bg-gray-100 dark:bg-gray-700/80 text-gray-700 dark:text-gray-300 text-xs">
                    <span class="min-w-0 truncate">{{ selectedIntegration?.name }}</span>
                    <button type="button" @click="setModalStep(2)" class="shrink-0 text-primary-600 dark:text-primary-400 hover:underline whitespace-nowrap">Cambiar</button>
                  </span>
                </div>
                <p class="text-xs text-gray-500 dark:text-gray-400">Se añadirán {{ selectedOperations.length }} acciones al flujo con la configuración por defecto.</p>
                <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/30">
                  <div class="px-3 py-2.5 text-xs font-semibold text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/70">Operaciones seleccionadas</div>
                  <ul class="p-3 space-y-2 max-h-64 overflow-y-auto">
                    <li
                      v-for="op in selectedOperations"
                      :key="op.id"
                      class="flex items-start gap-2 py-2 px-3 rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 text-sm"
                    >
                      <span
                        :class="[
                          'inline-flex items-center justify-center shrink-0 w-14 py-0.5 rounded text-xs font-mono font-semibold',
                          methodBadgeClass(op.method)
                        ]"
                      >{{ op.method }}</span>
                      <div class="min-w-0 flex-1">
                        <p class="text-gray-700 dark:text-gray-300 break-all font-mono">{{ op.path }}</p>
                        <p v-if="op.summary" class="mt-0.5 text-xs text-gray-500 dark:text-gray-400 line-clamp-1">{{ op.summary }}</p>
                      </div>
                    </li>
                  </ul>
                </div>
              </template>
              <!-- Una acción: vista completa (Output + Input + comportamiento) -->
              <template v-else-if="selectedOperations.length === 1">
              <div class="flex flex-wrap items-center gap-2 shrink-0">
                <span class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Paso 3</span>
                <span class="text-gray-400">·</span>
                <span class="text-sm font-medium text-gray-900 dark:text-white">Output</span>
              </div>
              <div class="flex flex-col sm:flex-row sm:flex-wrap gap-2 shrink-0">
                <span class="inline-flex items-center gap-1.5 py-1.5 px-2.5 rounded-md bg-gray-100 dark:bg-gray-700/80 text-gray-700 dark:text-gray-300 text-xs">
                  <span class="min-w-0 truncate">{{ selectedIntegration?.name }}</span>
                  <button type="button" @click="setModalStep(2)" class="shrink-0 text-primary-600 dark:text-primary-400 hover:underline whitespace-nowrap">Cambiar</button>
                </span>
                <span v-if="selectedOperation" class="inline-flex items-center gap-1.5 py-1.5 px-2.5 rounded-md bg-gray-100 dark:bg-gray-700/80 text-gray-700 dark:text-gray-300 text-xs font-mono min-w-0 max-w-full">
                  <span class="min-w-0 truncate" :title="selectedOperation.method + ' ' + selectedOperation.path">{{ selectedOperation.method }} {{ selectedOperation.path }}</span>
                  <button type="button" @click="setModalStep(2)" class="shrink-0 text-primary-600 dark:text-primary-400 hover:underline whitespace-nowrap">Cambiar</button>
                </span>
              </div>
              <p class="text-xs text-gray-500 dark:text-gray-400 shrink-0">Marca las propiedades que debe devolver el tool en la respuesta.</p>
              <ToolOutputPropertiesSection
                :schema-tree="outputSchemaTree"
                :schema-fields="outputSchemaFields"
                v-model:selected-paths="selectedOutputPaths"
                v-model:manual-paths="manualOutputPaths"
                enable-preview-toolbar
                v-model:inline-preview-open="outputPreviewInline"
                v-model:preview-full-schema="outputPreviewFullSchema"
                v-model:expanded-overlay="outputPropsExpanded"
                :expanded-teleport-visible="modelValue && modalStep === 3 && selectedOperations.length === 1"
                :preview-text="outputSelectionPreviewText"
              />
              <!-- Comportamiento del paso: OnFail / OnSuccess -->
              <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/30">
                <div class="px-3 py-2.5 text-xs font-semibold text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/70">Comportamiento del paso</div>
                <div class="p-3 space-y-4">
                  <div>
                    <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">Al fallar</label>
                    <select
                      v-model="onFailBehavior"
                      class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
                    >
                      <option value="stop">Detener ejecución</option>
                      <option value="continue">Continuar</option>
                    </select>
                    <label v-if="onFailBehavior === 'continue'" class="flex items-center gap-2 mt-2 text-sm text-gray-600 dark:text-gray-400">
                      <input v-model="onFailRecordFailure" type="checkbox" class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500" />
                      <span>Registrar fallo en logs</span>
                    </label>
                  </div>
                  <div>
                    <label class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 cursor-pointer">
                      <input v-model="onSuccessLog" type="checkbox" class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500" />
                      <span>Al éxito: registrar en logs</span>
                    </label>
                  </div>
                </div>
              </div>
              <!-- Input schema (opcional: input del tool o output de pasos anteriores) -->
              <template v-if="inputSchemaFields.length > 0 || toolInputFieldNames.length > 0 || previousStepPlaceholders.length > 0">
                <h3 class="text-sm font-medium text-gray-900 dark:text-white pt-2">Input (opcional)</h3>
                <div v-if="toolInputFieldNames.length > 0" class="rounded-lg border border-blue-200 dark:border-blue-800 bg-blue-50/30 dark:bg-blue-900/20 px-3 py-2 text-xs text-blue-700 dark:text-blue-300">
                  Input del tool: <span class="font-mono">{{ toolInputFieldNames.join(', ') }}</span>. Usa «← input» o <code class="px-0.5 rounded bg-blue-100 dark:bg-blue-900/50">{input.nombre}</code>.
                </div>
                <div v-if="previousStepPlaceholders.length > 0" class="rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50/30 dark:bg-amber-900/20 px-3 py-2 text-xs text-amber-700 dark:text-amber-300">
                  Pasos anteriores: usa «← paso» para rellenar con <code class="px-0.5 rounded bg-amber-100 dark:bg-amber-900/50">{stepId.output.path}</code>.
                </div>
                <div class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden">
                  <div class="px-3 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50">Valores por defecto</div>
                  <div class="p-3 space-y-2 max-h-40 overflow-y-auto">
                    <div v-for="(field, idx) in inputSchemaFields" :key="field.name || idx" class="flex flex-wrap items-center gap-2">
                      <label class="text-xs font-mono text-gray-600 dark:text-gray-400 w-24 shrink-0">{{ field.name }}</label>
                      <input
                        v-model="inputSchemaDefaults[field.name]"
                        type="text"
                        :placeholder="defaultPlaceholder(field.type)"
                        class="flex-1 min-w-[120px] px-2 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700"
                      />
                      <button v-if="suggestToolInputForField(field.name)" type="button" class="text-xs px-2 py-1 rounded border border-blue-500/50 text-blue-600 dark:text-blue-400" @click="inputSchemaDefaults[field.name] = suggestToolInputForField(field.name)">← input</button>
                      <select
                        v-if="previousStepPlaceholders.length > 0"
                        class="text-xs px-2 py-1 rounded border border-amber-500/50 text-amber-700 dark:text-amber-300 bg-white dark:bg-gray-700"
                        :value="''"
                        @change="e => { const v = e.target.value; if (v) { inputSchemaDefaults[field.name] = v; e.target.value = ''; } }"
                      >
                        <option value="">← paso</option>
                        <option v-for="opt in previousStepPlaceholders" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                      </select>
                    </div>
                    <p v-if="inputSchemaFields.length === 0" class="text-xs text-gray-500 dark:text-gray-400">Sin parámetros</p>
                  </div>
                </div>
              </template>
              </template>
            </div>
          </div>

          <!-- Footer: según paso -->
          <div class="shrink-0 px-4 py-3 border-t border-gray-200 dark:border-gray-600 flex flex-col gap-3 sm:flex-row sm:justify-between sm:gap-3">
            <template v-if="modalStep === 1">
              <button type="button" @click="$emit('update:modelValue', false)" class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Cancelar</button>
              <button type="button" @click="setModalStep(2)" :disabled="!selectedIntegration" :class="['w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg font-medium', selectedIntegration ? 'bg-primary-600 hover:bg-primary-700 text-white' : 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed']">Siguiente</button>
            </template>
            <template v-else-if="modalStep === 2">
              <button type="button" @click="setModalStep(1)" class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Anterior</button>
              <button type="button" @click="setModalStep(3)" :disabled="selectedOperations.length === 0" :class="['w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg font-medium', selectedOperations.length > 0 ? 'bg-primary-600 hover:bg-primary-700 text-white' : 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed']">Siguiente</button>
            </template>
            <template v-else>
              <button type="button" @click="setModalStep(2)" class="w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Anterior</button>
              <button type="button" @click="handleAdd" :disabled="!canAdd" :class="['w-full sm:w-auto px-4 py-2.5 min-h-[44px] rounded-lg font-medium', canAdd ? 'bg-green-600 hover:bg-green-700 text-white' : 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed']">{{ editStep ? 'Guardar' : 'Añadir' }}</button>
            </template>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import apiService from '../../services/api'
import NoIntegrationsMessage from '../integrations/NoIntegrationsMessage.vue'
import ToolOutputPropertiesSection from '../mcp/ToolOutputPropertiesSection.vue'
import { buildMinimalOutputSchemaFromPaths, buildOutputSchemaFromSelection, computeOutputSelectionPreviewText } from '../../utils/outputSchema'
import { buildOutputSchemaTreeFromResponseSchema } from '../../utils/outputSchemaTree'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  integrations: { type: Array, default: () => [] },
  editStep: { type: Object, default: null },
  /** Step padre (action) cuando añadimos hijo; usado para sugerir output como input */
  parentStep: { type: Object, default: null },
  /** JSON del workflow para resolver parentStep */
  workflowJson: { type: String, default: '{}' },
  /** Input schema del tool (nodo inicio); usado para sugerir {input.X} */
  toolInputSchema: { type: String, default: '{}' },
  /** Si true, no se muestra el filtro por método HTTP (p. ej. en contexto MCP donde se prefiere solo búsqueda por nombre) */
  hideMethodFilter: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'add', 'update'])

const selectedIntegration = ref(null)
/** Una o varias acciones seleccionadas (Paso 2). Para Paso 3 con una sola se usa selectedOperation. */
const selectedOperations = ref([])
const operations = ref([])
const operationsLoading = ref(false)
const actionSearchQuery = ref('')
const actionMethodFilter = ref('')

const actionMethodFilters = [
  { value: '', label: 'Todos', activeClass: 'bg-gray-200 dark:bg-gray-600 text-gray-800 dark:text-gray-200' },
  { value: 'GET', label: 'GET', activeClass: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300' },
  { value: 'POST', label: 'POST', activeClass: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
  { value: 'PUT', label: 'PUT', activeClass: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300' },
  { value: 'PATCH', label: 'PATCH', activeClass: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
  { value: 'DELETE', label: 'DELETE', activeClass: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' },
  { value: 'HEAD', label: 'HEAD', activeClass: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300' },
  { value: 'OPTIONS', label: 'OPTIONS', activeClass: 'bg-gray-100 text-gray-700 dark:bg-gray-700/40 dark:text-gray-300' }
]
const inputSchemaDefaults = ref({})
/** Rutas de propiedades del output schema que el usuario quiere devolver en el tool (por defecto todas) */
const selectedOutputPaths = ref([])
/** Cuando no hay schema de respuesta, el usuario puede indicar propiedades a mano: { path, type } */
const manualOutputPaths = ref([])
/** Preview JSON debajo del árbol (vista normal) */
const outputPreviewInline = ref(false)
/** Vista maximizada con split schema | preview */
const outputPropsExpanded = ref(false)
/** Preview: activado = JSON Schema completo; desactivado = forma resumida (solo tipos / anidación) */
const outputPreviewFullSchema = ref(true)
/** Paso del modal: 1 = Integración, 2 = Acción, 3 = Output */
const modalStep = ref(1)

watch(() => modalStep.value, (s) => {
  if (s !== 3) outputPropsExpanded.value = false
})
/** OnFail: 'stop' | 'continue' */
const onFailBehavior = ref('stop')
const onFailRecordFailure = ref(false)
const onSuccessLog = ref(false)
function setModalStep(step) {
  modalStep.value = step
  if (step === 3 && selectedOperations.value.length === 1 && outputSchemaFields.value.length === 0 && manualOutputPaths.value.length === 0) {
    manualOutputPaths.value = [{ path: '', type: 'string', itemType: 'string' }]
  }
}

const filteredOperations = computed(() => {
  let list = operations.value
  const methodFilter = (actionMethodFilter.value || '').toUpperCase()
  if (methodFilter) {
    list = list.filter(op => (op.method || '').toUpperCase() === methodFilter)
  }
  const q = (actionSearchQuery.value || '').trim().toLowerCase()
  if (!q) return list
  return list.filter(op => {
    const method = (op.method || '').toLowerCase()
    const path = (op.path || '').toLowerCase()
    const summary = (op.summary || '').toLowerCase()
    const id = (op.id || '').toLowerCase()
    const desc = (op.description || '').toLowerCase()
    return method.includes(q) || path.includes(q) || summary.includes(q) || id.includes(q) || desc.includes(q)
  })
})

/** Cuando hay una sola acción seleccionada, referencia a ella (para Paso 3 detalle). */
const selectedOperation = computed(() =>
  selectedOperations.value.length === 1 ? selectedOperations.value[0] : null
)
const canAdd = computed(() => {
  if (!selectedIntegration.value || selectedOperations.value.length === 0) return false
  if (selectedOperations.value.length > 1) return true
  return !!selectedOperation.value
})

/** Nombres de parámetros/headers HTTP que no deben formar parte del input schema del tool */
const HTTP_PARAM_NAMES = new Set([
  'content-type', 'accept', 'accept-language', 'accept-encoding', 'accept-charset',
  'authorization', 'cookie', 'user-agent', 'x-requested-with', 'cache-control',
  'pragma', 'origin', 'referer', 'referrer', 'content-length', 'content-encoding',
  'content-language', 'content-location', 'content-range', 'content-disposition',
  'expires', 'last-modified', 'if-modified-since', 'if-none-match', 'etag',
  'host', 'connection', 'upgrade', 'x-api-key', 'x-auth-token', 'api-key',
  'x-correlation-id', 'x-request-id', 'x-forwarded-for', 'x-forwarded-proto',
  'x-real-ip', 'sec-fetch-dest', 'sec-fetch-mode', 'sec-fetch-site', 'sec-ch-ua',
  'sec-ch-ua-mobile', 'sec-ch-ua-platform'
])

function isHttpOnlyParam(name) {
  if (!name || typeof name !== 'string') return false
  const lower = name.toLowerCase().trim()
  if (HTTP_PARAM_NAMES.has(lower)) return true
  if (lower.startsWith('x-') && (lower.endsWith('-key') || lower.endsWith('-token') || lower.endsWith('-id'))) return true
  return false
}

const inputSchemaFields = computed(() => {
  if (!selectedOperation.value) return []
  const op = selectedOperation.value
  const fields = []
  const seen = new Set()
  for (const p of op.parameters || []) {
    const paramIn = (p.in || 'query').toLowerCase()
    if (!p.name || seen.has(p.name) || paramIn === 'header' || isHttpOnlyParam(p.name)) continue
    seen.add(p.name)
    fields.push({
      name: p.name,
      type: p.type || 'string',
      required: !!p.required,
      description: p.description || null,
      in: p.in || 'query'
    })
  }
  if (op.requestBody?.schema) {
    try {
      const body = typeof op.requestBody.schema === 'string' ? JSON.parse(op.requestBody.schema) : op.requestBody.schema
      const props = body.properties || {}
      const requiredSet = new Set(body.required || [])
      for (const [name, prop] of Object.entries(props)) {
        if (seen.has(name) || isHttpOnlyParam(name)) continue
        seen.add(name)
        fields.push({
          name,
          type: prop?.type || 'string',
          required: requiredSet.has(name),
          description: prop?.description || null,
          in: 'body'
        })
      }
    } catch {
      if (!seen.has('body') && !isHttpOnlyParam('body')) {
        fields.push({ name: 'body', type: 'object', required: false, description: op.requestBody?.description || null })
      }
    }
  }
  return fields
})

/** Raw response schema (string or object) from the selected operation; supports camelCase and PascalCase from API */
function getResponseSchemaRaw() {
  const rb = selectedOperation.value?.responseBody
  if (!rb) return null
  return rb.schema ?? rb.Schema ?? null
}

const outputSchemaFields = computed(() => {
  const raw = getResponseSchemaRaw()
  if (raw == null || raw === '') return []
  try {
    const schema = typeof raw === 'string' ? JSON.parse(raw) : raw
    const props = schema?.properties
    if (!props || typeof props !== 'object') return []
    const fields = []
    function flatten(prefix, obj) {
      for (const [name, prop] of Object.entries(obj)) {
        if (name == null) continue
        const path = prefix ? `${prefix}.${name}` : name
        const type = prop?.type || 'string'
        const nested = prop?.properties
        if (type === 'object' && nested && typeof nested === 'object' && Object.keys(nested).length > 0) {
          flatten(path, nested)
        } else if (type === 'array' && prop?.items) {
          const itemProps = prop.items?.properties
          if (itemProps && typeof itemProps === 'object' && Object.keys(itemProps).length > 0) {
            flatten(path, itemProps)
          } else {
            const itemType = prop.items?.type || 'any'
            fields.push({
              name: path,
              type: `array of ${itemType}`,
              description: prop?.description ?? null
            })
          }
        } else {
          fields.push({
            name: path,
            type,
            description: prop?.description ?? null
          })
        }
      }
    }
    flatten('', props)
    return fields
  } catch {
    return []
  }
})

const outputSchemaTree = computed(() => buildOutputSchemaTreeFromResponseSchema(getResponseSchemaRaw()))

/** True when the operation has a response schema payload but it produced no output fields (e.g. $ref not resolved or empty schema) */
const outputSchemaPresentButEmpty = computed(() => {
  const raw = getResponseSchemaRaw()
  if (raw == null || raw === '') return false
  return outputSchemaFields.value.length === 0
})

/** Paths a devolver: desde checkboxes (si hay schema) o desde el listado manual (si no hay schema) */
const effectiveSelectedOutputPaths = computed(() => {
  if (outputSchemaFields.value.length > 0) return selectedOutputPaths.value
  return manualOutputPaths.value.map(m => (m.path || '').trim()).filter(Boolean)
})

/** Items manuales con path, type y itemType (solo los que tienen path) para construir el schema */
const effectiveManualOutputItems = computed(() => {
  return manualOutputPaths.value
    .filter(m => (m.path || '').trim())
    .map(m => ({ path: m.path.trim(), type: m.type || 'string', itemType: m.type === 'array' ? (m.itemType || 'string') : undefined }))
})

/** JSON formateado: schema completo o vista lite según `outputPreviewFullSchema` */
const outputSelectionPreviewText = computed(() => {
  if (selectedOperations.value.length !== 1) return '—'
  const op = selectedOperations.value[0]
  const fullResponseSchema = op.responseBody?.schema ?? op.responseBody?.Schema ?? null
  const hasStructured = outputSchemaFields.value.length > 0 || outputSchemaTree.value.length > 0
  return computeOutputSelectionPreviewText({
    fullSchema: fullResponseSchema,
    selectedPaths: selectedOutputPaths.value,
    manualItems: effectiveManualOutputItems.value,
    previewFullSchema: outputPreviewFullSchema.value,
    hasSelectablePaths: hasStructured,
    emptyMessage: 'Añade propiedades manualmente o elige una operación con schema de respuesta.'
  })
})

async function loadOperations() {
  if (!selectedIntegration.value?.id) {
    operations.value = []
    return
  }
  operationsLoading.value = true
  try {
    const result = await apiService.getIntegrationOperations(selectedIntegration.value.id)
    operations.value = result.operations ?? []
  } catch {
    operations.value = []
  } finally {
    operationsLoading.value = false
  }
}

/** Nombres del input global del tool (nodo inicio) */
const toolInputFieldNames = computed(() => {
  try {
    const schema = typeof props.toolInputSchema === 'string' ? JSON.parse(props.toolInputSchema || '{}') : props.toolInputSchema
    return Object.keys(schema?.properties || {}).filter(Boolean)
  } catch {
    return []
  }
})

/** Placeholders desde pasos anteriores del workflow: {stepId.output.path} para usar en input mapping */
const previousStepPlaceholders = computed(() => {
  let steps = []
  try {
    const def = JSON.parse(props.workflowJson || '{}')
    steps = def.steps || []
  } catch {
    /* ignore */
  }
  const currentStepId = props.editStep?.stepId
  const previous = currentStepId
    ? steps.filter(s => s.type === 'action' && s.id !== currentStepId)
    : steps.filter(s => s.type === 'action')
  const list = []
  for (const s of previous) {
    const paths = s.outputAvailablePaths || []
    const label = s.summary || s.operationId || s.id || 'step'
    for (const path of paths) {
      list.push({ value: `{${s.id}.output.${path}}`, label: `${label}: ${path}` })
    }
  }
  return list
})

/** Sugerencia desde input global del tool: {input.fieldName} cuando hay coincidencia */
function suggestToolInputForField(actionFieldName) {
  const names = toolInputFieldNames.value
  if (!names.length) return null
  const inName = (actionFieldName || '').toLowerCase()
  const exact = names.find(n => n.toLowerCase() === inName)
  if (exact) return `{input.${exact}}`
  const similar = names.find(n => n.toLowerCase().includes(inName) || inName.includes(n.toLowerCase()))
  if (similar) return `{input.${similar}}`
  return null
}

function selectIntegration(int) {
  selectedIntegration.value = int
  selectedOperations.value = []
  loadOperations()
}

function isOperationSelected(op) {
  return selectedOperations.value.some(o => o.id === op.id)
}

function toggleOperation(op) {
  const idx = selectedOperations.value.findIndex(o => o.id === op.id)
  if (idx >= 0) {
    selectedOperations.value = selectedOperations.value.filter(o => o.id !== op.id)
  } else {
    selectedOperations.value = [...selectedOperations.value, op]
  }
  if (selectedOperations.value.length === 1) {
    inputSchemaDefaults.value = {}
    manualOutputPaths.value = []
    nextTick(() => {
      selectedOutputPaths.value = (outputSchemaFields.value || []).map(f => f.name)
    })
  }
}

function selectAllOperations() {
  selectedOperations.value = [...filteredOperations.value]
  if (selectedOperations.value.length === 1) {
    inputSchemaDefaults.value = {}
    manualOutputPaths.value = []
    nextTick(() => {
      selectedOutputPaths.value = (outputSchemaFields.value || []).map(f => f.name)
    })
  }
}

function selectNoOperations() {
  selectedOperations.value = []
}

function defaultPlaceholder(type) {
  const t = (type || '').toLowerCase()
  if (t === 'integer' || t === 'number') return '42'
  if (t === 'boolean') return 'true'
  if (t === 'array') return '[]'
  if (t === 'object') return '{}'
  return '"valor"'
}

function methodBadgeClass(method) {
  const m = (method || '').toUpperCase()
  switch (m) {
    case 'GET': return 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300'
    case 'POST': return 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300'
    case 'PUT': return 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300'
    case 'PATCH': return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300'
    case 'DELETE': return 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300'
    case 'HEAD': return 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300'
    case 'OPTIONS': return 'bg-gray-100 text-gray-700 dark:bg-gray-700/40 dark:text-gray-300'
    default: return 'bg-gray-100 text-gray-700 dark:bg-gray-700/40 dark:text-gray-300'
  }
}

function parseDefaultValue(raw, type) {
  const s = (raw || '').trim()
  if (!s) return undefined
  const t = (type || '').toLowerCase()
  if (t === 'integer' || t === 'number') {
    const n = Number(s)
    return Number.isFinite(n) ? n : undefined
  }
  if (t === 'boolean') return s === 'true' || s === '1'
  if (t === 'array' || t === 'object') {
    try {
      return JSON.parse(s)
    } catch {
      return undefined
    }
  }
  return s
}

/** Extrae rutas aplanadas de un JSON Schema (misma lógica que outputSchemaFields) */
function extractPathsFromSchema(schema) {
  if (!schema || typeof schema !== 'object') return []
  try {
    const props = schema.properties || {}
    const paths = []
    function flatten(prefix, obj) {
      for (const [name, prop] of Object.entries(obj)) {
        const path = prefix ? `${prefix}.${name}` : name
        const type = prop?.type || 'string'
        const nested = prop?.properties
        if (type === 'object' && nested && Object.keys(nested).length > 0) {
          flatten(path, nested)
        } else if (type === 'array' && prop?.items?.properties && Object.keys(prop.items.properties).length > 0) {
          flatten(path, prop.items.properties)
        } else {
          paths.push(path)
        }
      }
    }
    flatten('', props)
    return paths
  } catch {
    return []
  }
}

/** Construye actionData para una operación (usa estado actual: inputSchemaDefaults, selectedOutputPaths, onFail, onSuccess). */
function buildActionDataForOperation(op) {
  const method = (op.method || 'GET').toUpperCase()
  const path = (op.path || '').startsWith('/') ? op.path : '/' + (op.path || '')
  const schemaProps = {}
  const required = []
  for (const p of op.parameters || []) {
    const paramIn = (p.in || 'query').toLowerCase()
    if (paramIn === 'header' || isHttpOnlyParam(p.name)) continue
    const def = parseDefaultValue(inputSchemaDefaults.value[p.name], p.type || 'string')
    schemaProps[p.name] = { type: p.type || 'string', description: p.description, _actionParam: p.name }
    if (def !== undefined) schemaProps[p.name].default = def
    if (p.required) required.push(p.name)
  }
  if (op.requestBody?.schema) {
    try {
      const body = typeof op.requestBody.schema === 'string' ? JSON.parse(op.requestBody.schema) : op.requestBody.schema
      for (const [name, prop] of Object.entries(body.properties || {})) {
        if (isHttpOnlyParam(name)) continue
        const def = parseDefaultValue(inputSchemaDefaults.value[name], prop?.type || 'string')
        schemaProps[name] = { ...prop, type: prop?.type || 'string', _actionParam: name }
        if (def !== undefined) schemaProps[name].default = def
      }
      if (body.required) required.push(...body.required.filter(n => !isHttpOnlyParam(n)))
    } catch {
      const def = parseDefaultValue(inputSchemaDefaults.value.body, 'object')
      schemaProps.body = { type: 'object', _actionParam: 'body' }
      if (def !== undefined) schemaProps.body.default = def
    }
  }
  const inputSchema = Object.keys(schemaProps).length > 0 ? JSON.stringify({ type: 'object', properties: schemaProps, required }) : '{}'

  const fullResponseSchema = op.responseBody?.schema ?? op.responseBody?.Schema ?? null
  const availablePathsFromSchema = getOutputSchemaFieldsForOp(op).map(f => f.name)
  const effectivePaths = effectiveSelectedOutputPaths.value
  let availablePaths = availablePathsFromSchema
  let outputSchema = null
  let outputSchemaFull = null
  if (fullResponseSchema) {
    const selected = effectivePaths.length > 0 ? effectivePaths : availablePathsFromSchema
    outputSchema = buildOutputSchemaFromSelection(fullResponseSchema, selected) || (typeof fullResponseSchema === 'string' ? fullResponseSchema : JSON.stringify(fullResponseSchema))
    outputSchemaFull = typeof fullResponseSchema === 'string' ? fullResponseSchema : JSON.stringify(fullResponseSchema || {})
  } else if (effectiveManualOutputItems.value.length > 0) {
    outputSchema = buildMinimalOutputSchemaFromPaths(effectiveManualOutputItems.value)
    availablePaths = effectivePaths
    outputSchemaFull = outputSchema
  }

  return {
    integrationId: selectedIntegration.value.id,
    integrationLogoUrl: selectedIntegration.value.logoUrl ?? selectedIntegration.value.LogoUrl ?? null,
    method,
    path,
    operationId: op.id,
    inputMapping: buildInputMappingForOp(op),
    summary: op.summary,
    inputSchema,
    outputSchema,
    outputSchemaAvailablePaths: availablePaths,
    outputSchemaFull,
    onFail: {
      behavior: onFailBehavior.value || 'stop',
      recordFailure: !!onFailRecordFailure.value
    },
    onSuccess: { log: !!onSuccessLog.value }
  }
}

/** actionData mínimo para una op (varias seleccionadas: sin configurar output/input en el modal). */
function buildMinimalActionDataForOperation(op) {
  const method = (op.method || 'GET').toUpperCase()
  const path = (op.path || '').startsWith('/') ? op.path : '/' + (op.path || '')
  const schemaProps = {}
  const required = []
  for (const p of op.parameters || []) {
    const paramIn = (p.in || 'query').toLowerCase()
    if (paramIn === 'header' || isHttpOnlyParam(p.name)) continue
    schemaProps[p.name] = { type: p.type || 'string', description: p.description, _actionParam: p.name }
    if (p.required) required.push(p.name)
  }
  if (op.requestBody?.schema) {
    try {
      const body = typeof op.requestBody.schema === 'string' ? JSON.parse(op.requestBody.schema) : op.requestBody.schema
      for (const [name, prop] of Object.entries(body.properties || {})) {
        if (isHttpOnlyParam(name)) continue
        schemaProps[name] = { ...prop, type: prop?.type || 'string', _actionParam: name }
      }
      if (body.required) required.push(...body.required.filter(n => !isHttpOnlyParam(n)))
    } catch {
      schemaProps.body = { type: 'object', _actionParam: 'body' }
    }
  }
  const inputSchema = Object.keys(schemaProps).length > 0 ? JSON.stringify({ type: 'object', properties: schemaProps, required }) : '{}'
  const fullResponseSchema = op.responseBody?.schema ?? op.responseBody?.Schema ?? null
  const availablePaths = getOutputSchemaFieldsForOp(op).map(f => f.name)
  const allPaths = availablePaths.length > 0 ? availablePaths : []
  let outputSchema = null
  if (fullResponseSchema && allPaths.length > 0) {
    outputSchema = buildOutputSchemaFromSelection(fullResponseSchema, allPaths) || (typeof fullResponseSchema === 'string' ? fullResponseSchema : JSON.stringify(fullResponseSchema))
  }
  const outputSchemaFull = fullResponseSchema ? (typeof fullResponseSchema === 'string' ? fullResponseSchema : JSON.stringify(fullResponseSchema)) : null
  return {
    integrationId: selectedIntegration.value.id,
    integrationLogoUrl: selectedIntegration.value.logoUrl ?? selectedIntegration.value.LogoUrl ?? null,
    method,
    path,
    operationId: op.id,
    inputMapping: buildInputMappingForOp(op, true),
    summary: op.summary,
    inputSchema,
    outputSchema: outputSchema || (fullResponseSchema ? (typeof fullResponseSchema === 'string' ? fullResponseSchema : JSON.stringify(fullResponseSchema)) : null),
    outputSchemaAvailablePaths: availablePaths,
    outputSchemaFull: outputSchemaFull || undefined,
    onFail: { behavior: 'stop', recordFailure: false },
    onSuccess: { log: false }
  }
}

function getOutputSchemaFieldsForOp(op) {
  const raw = op?.responseBody?.schema ?? op?.responseBody?.Schema ?? null
  if (raw == null || raw === '') return []
  try {
    const schema = typeof raw === 'string' ? JSON.parse(raw) : raw
    const props = schema?.properties
    if (!props || typeof props !== 'object') return []
    const fields = []
    function flatten(prefix, obj) {
      for (const [name, prop] of Object.entries(obj)) {
        if (name == null) continue
        const path = prefix ? `${prefix}.${name}` : name
        const type = prop?.type || 'string'
        const nested = prop?.properties
        if (type === 'object' && nested && typeof nested === 'object' && Object.keys(nested).length > 0) {
          flatten(path, nested)
        } else if (type === 'array' && prop?.items) {
          const itemProps = prop.items?.properties
          if (itemProps && typeof itemProps === 'object' && Object.keys(itemProps).length > 0) {
            flatten(path, itemProps)
          } else {
            fields.push({ name: path, type: `array of ${prop.items?.type || 'any'}`, description: prop?.description ?? null })
          }
        } else {
          fields.push({ name: path, type, description: prop?.description ?? null })
        }
      }
    }
    flatten('', props)
    return fields
  } catch {
    return []
  }
}

function buildInputMappingForOp(op, usePlaceholdersOnly = false) {
  if (!op) return {}
  const path = op.path || ''
  const method = (op.method || 'GET').toUpperCase()
  const mapping = {}
  const defs = usePlaceholdersOnly ? {} : inputSchemaDefaults.value
  for (const p of op.parameters || []) {
    const paramIn = (p.in || 'query').toLowerCase()
    if (!p.name || paramIn === 'header' || isHttpOnlyParam(p.name)) continue
    const val = defs[p.name]?.trim()
    const placeholder = val && (val.startsWith('{') && val.endsWith('}')) ? val : `{input.${p.name}}`
    if ((p.in || 'query') === 'path') {
      mapping[`pathParams.${p.name}`] = placeholder
    } else if ((p.in || 'query') === 'query') {
      mapping[`queryParams.${p.name}`] = placeholder
    }
  }
  if (op.requestBody?.schema) {
    try {
      const body = typeof op.requestBody.schema === 'string' ? JSON.parse(op.requestBody.schema) : op.requestBody.schema
      const props = body.properties || {}
      const bodyObj = {}
      for (const [name] of Object.entries(props)) {
        if (isHttpOnlyParam(name)) continue
        const val = defs[name]?.trim()
        bodyObj[name] = val && (val.startsWith('{') && val.endsWith('}')) ? val : `{input.${name}}`
      }
      mapping.body = Object.keys(bodyObj).length > 0 ? JSON.stringify(bodyObj) : '{}'
    } catch {
      mapping.body = defs.body ? String(defs.body) : '{}'
    }
  } else if (path.includes('{id}') || path.includes('{Id}')) {
    if (!mapping['pathParams.id']) mapping['pathParams.id'] = defs.id?.trim() && defs.id?.startsWith('{') ? defs.id : '{input.id}'
  }
  if (['POST', 'PUT', 'PATCH'].includes(method) && !mapping.body) {
    mapping.body = defs.body ? String(defs.body) : '{input}'
  }
  if (Object.keys(mapping).length === 0) mapping.body = '{}'
  return mapping
}

function handleAdd() {
  if (!selectedIntegration.value || selectedOperations.value.length === 0) return
  if (props.editStep?.stepId) {
    if (selectedOperations.value.length !== 1) return
    const actionData = buildActionDataForOperation(selectedOperations.value[0])
    emit('update', { stepId: props.editStep.stepId, actionData })
  } else {
    const list = selectedOperations.value.length === 1
      ? [buildActionDataForOperation(selectedOperations.value[0])]
      : selectedOperations.value.map(op => buildMinimalActionDataForOperation(op))
    emit('add', list)
  }
  emit('update:modelValue', false)
  reset()
}

function reset() {
  selectedIntegration.value = null
  selectedOperations.value = []
  operations.value = []
  actionSearchQuery.value = ''
  actionMethodFilter.value = ''
  inputSchemaDefaults.value = {}
  selectedOutputPaths.value = []
  manualOutputPaths.value = []
  modalStep.value = 1
  onFailBehavior.value = 'stop'
  onFailRecordFailure.value = false
  onSuccessLog.value = false
  outputPreviewInline.value = false
  outputPropsExpanded.value = false
  outputPreviewFullSchema.value = true
}

function loadInputSchemaDefaultsFromStep(step) {
  if (!step?.inputSchema) return
  try {
    const schema = typeof step.inputSchema === 'string' ? JSON.parse(step.inputSchema) : step.inputSchema
    const schemaProps = schema.properties || {}
    const defaults = {}
    for (const [name, prop] of Object.entries(schemaProps)) {
      const def = prop?.default
      if (def !== undefined) {
        defaults[name] = typeof def === 'object' ? JSON.stringify(def) : String(def)
      }
    }
    inputSchemaDefaults.value = defaults
  } catch {
    inputSchemaDefaults.value = {}
  }
}

/** Rellena inputSchemaDefaults desde step.inputMapping (pathParams.xxx, queryParams.xxx, body) para edición */
function loadInputSchemaDefaultsFromInputMapping(step) {
  const mapping = step?.inputMapping
  if (!mapping || typeof mapping !== 'object') return
  const defaults = { ...inputSchemaDefaults.value }
  for (const [key, value] of Object.entries(mapping)) {
    if (typeof value !== 'string') continue
    if (key.startsWith('pathParams.')) {
      defaults[key.slice('pathParams.'.length)] = value
    } else if (key.startsWith('queryParams.')) {
      defaults[key.slice('queryParams.'.length)] = value
    } else if (key === 'body') {
      defaults.body = value
    }
  }
  inputSchemaDefaults.value = defaults
}

watch(() => props.modelValue, (open) => {
  if (open) {
    if (props.editStep?.step) {
      const step = props.editStep.step
      const int = props.integrations.find(i => i.id === step.integrationId)
      if (int) {
        selectedIntegration.value = int
        if (props.integrations?.length === 1) {
          modalStep.value = 2
        }
        loadOperations().then(() => {
          const op = operations.value.find(o => o.id === step.operationId)
          if (op) {
            selectedOperations.value = [op]
            loadInputSchemaDefaultsFromStep(step)
            loadInputSchemaDefaultsFromInputMapping(step)
            nextTick(() => {
              if (outputSchemaFields.value.length > 0) {
                manualOutputPaths.value = []
                if (step.outputSchema) {
                  try {
                    const parsed = typeof step.outputSchema === 'string' ? JSON.parse(step.outputSchema) : step.outputSchema
                    const paths = extractPathsFromSchema(parsed)
                    if (paths.length > 0) selectedOutputPaths.value = paths
                  } catch {
                    selectedOutputPaths.value = (outputSchemaFields.value || []).map(f => f.name)
                  }
                } else {
                  selectedOutputPaths.value = (outputSchemaFields.value || []).map(f => f.name)
                }
              } else {
                selectedOutputPaths.value = []
                manualOutputPaths.value = (step.outputSchemaAvailablePaths?.length)
                  ? step.outputSchemaAvailablePaths.map(p => ({ path: p, type: 'string', itemType: 'string' }))
                  : [{ path: '', type: 'string', itemType: 'string' }]
              }
              const onFail = step.onFail
              onFailBehavior.value = (onFail?.behavior === 'continue' ? 'continue' : 'stop')
              onFailRecordFailure.value = !!onFail?.recordFailure
              onSuccessLog.value = !!step.onSuccess?.log
              modalStep.value = props.integrations?.length === 1 ? 2 : 3
            })
          }
        })
      } else {
        selectedOperations.value = []
        selectedIntegration.value = null
        if (props.integrations?.length === 1) {
          selectIntegration(props.integrations[0])
          modalStep.value = 2
        }
      }
      } else {
        selectedOperations.value = []
        if (props.integrations?.length === 1) {
          selectIntegration(props.integrations[0])
          modalStep.value = 2
        } else {
          if (!selectedIntegration.value) operations.value = []
          else loadOperations()
        }
      }
    } else {
      reset()
  }
})
</script>

<style scoped>
.animate-slide-in {
  animation: slideIn 0.25s ease-out;
}
@keyframes slideIn {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}
.modal-enter-active, .modal-leave-active {
  transition: opacity 0.2s;
}
.modal-enter-from, .modal-leave-to {
  opacity: 0;
}
</style>

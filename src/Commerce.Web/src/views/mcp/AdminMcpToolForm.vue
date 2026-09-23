<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6">
      <div v-if="loading" class="space-y-4">
        <div class="h-8 w-48 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        <div class="h-64 bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse" />
      </div>

      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <p class="text-red-800 dark:text-red-300">{{ error }}</p>
        <router-link :to="backUrl" class="mt-2 inline-block text-sm text-primary-600 dark:text-primary-400">← Volver</router-link>
      </div>

      <div v-else class="space-y-6">
        <div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between sm:gap-4">
          <div class="space-y-1 min-w-0 flex-1">
            <router-link :to="backUrl" class="text-sm text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400">← Volver a server</router-link>
            <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ isEdit ? 'Editar tool' : 'Nuevo tool' }}</h1>
            <p class="text-sm text-gray-500 dark:text-gray-400">{{ isEdit ? 'Modifica el tool definido.' : 'Creación básica (una acción) o por flujo (diagrama de workflow).' }}</p>
          </div>
          <button
            v-if="isEdit"
            type="button"
            @click="openExecuteSlider"
            class="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-lg font-medium bg-emerald-600 hover:bg-emerald-700 text-white transition-colors shrink-0 w-full sm:w-auto"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
            Ejecutar tool
          </button>
        </div>

        <!-- Step indicator (only for new) -->
        <div v-if="!isEdit" class="flex items-center gap-2">
          <span class="text-sm font-medium text-gray-500 dark:text-gray-400">Paso {{ currentStep }} de 2</span>
          <div class="flex-1 h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
            <div class="h-full bg-primary-600 rounded-full transition-all duration-300" :style="{ width: `${(currentStep / 2) * 100}%` }" />
          </div>
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden min-h-[420px] flex flex-col">
          <!-- Step 1: Choose mode (only for new) -->
          <div v-show="!isEdit && currentStep === 1" class="p-4 sm:p-6 flex flex-col flex-1 min-h-0">
            <div class="flex-1">
              <PlanLimitAlert
                v-if="atLimitTools"
                :message="toolsLimitMessage"
                billing-link-text="Actualiza tu plan para más"
                class="mb-4 text-sm"
              />
              <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">Elige una forma de empezar. Las plantillas son el camino más rápido si ya conectaste tu tienda.</p>
              <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                <button
                  type="button"
                  @click="!atLimitTools && selectMode('templates')"
                  :disabled="atLimitTools"
                  class="flex flex-col items-center gap-3 p-6 rounded-xl border-2 transition-all text-left"
                  :class="atLimitTools ? 'border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 cursor-not-allowed opacity-75' : (toolMode === 'templates' ? 'border-primary-600 bg-primary-50/50 dark:bg-primary-900/20 ring-1 ring-primary-500/30' : 'border-gray-200 dark:border-gray-600 card-hover')"
                >
                  <div class="w-12 h-12 rounded-full flex items-center justify-center bg-primary-100 dark:bg-primary-900/40">
                    <svg class="w-6 h-6 text-primary-600 dark:text-primary-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
                    </svg>
                  </div>
                  <div>
                    <p class="font-semibold text-gray-900 dark:text-white">Plantillas de caso de uso</p>
                    <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Varias tools listas (contenido, pedidos, marketing) según tu integración VTEX, Shopify, etc.</p>
                  </div>
                </button>
                <button
                  type="button"
                  @click="!atLimitTools && selectMode('basic')"
                  :disabled="atLimitTools"
                  class="flex flex-col items-center gap-3 p-6 rounded-xl border-2 transition-all text-left"
                  :class="atLimitTools ? 'border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 cursor-not-allowed opacity-75' : (toolMode === 'basic' ? 'border-primary-600 bg-primary-50/50 dark:bg-primary-900/20' : 'border-gray-200 dark:border-gray-600 card-hover')"
                >
                  <div class="w-12 h-12 rounded-full flex items-center justify-center bg-primary-100 dark:bg-primary-900/40">
                    <svg class="w-6 h-6 text-primary-600 dark:text-primary-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </div>
                  <div>
                    <p class="font-semibold text-gray-900 dark:text-white">Creación básica</p>
                    <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Una o más acciones sueltas; tú eliges cada operación del catálogo de la integración.</p>
                  </div>
                </button>
                <button
                  type="button"
                  @click="!atLimitTools && !atLimitWorkflowTools && selectMode('flow')"
                  :disabled="atLimitTools || atLimitWorkflowTools"
                  class="flex flex-col items-center gap-3 p-6 rounded-xl border-2 transition-all text-left"
                  :class="(atLimitTools || atLimitWorkflowTools) ? 'border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 cursor-not-allowed opacity-75' : (toolMode === 'flow' ? 'border-primary-600 bg-primary-50/50 dark:bg-primary-900/20' : 'border-gray-200 dark:border-gray-600 card-hover')"
                >
                  <div class="w-12 h-12 rounded-full flex items-center justify-center bg-primary-100 dark:bg-primary-900/40">
                    <svg class="w-6 h-6 text-primary-600 dark:text-primary-300" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                      <circle cx="5" cy="12" r="2.5" />
                      <path d="M7.5 12h2M13 12h4" />
                      <rect x="9" y="9.5" width="4" height="5" rx="1" />
                      <rect x="17" y="9.5" width="4" height="5" rx="1" />
                    </svg>
                  </div>
                  <div>
                    <p class="font-semibold text-gray-900 dark:text-white">Creación de flujo</p>
                    <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Diagrama de workflow con varios pasos y ramas.</p>
                    <p v-if="atLimitWorkflowTools && workflowLimitMessage" class="text-xs text-amber-600 dark:text-amber-400 mt-1">{{ workflowLimitMessage }}</p>
                  </div>
                </button>
              </div>
              <div v-if="toolMode" class="mt-6 p-4 rounded-lg bg-gray-50 dark:bg-gray-700/50 border border-gray-200 dark:border-gray-600">
                <p v-if="toolMode === 'templates'" class="text-sm text-gray-700 dark:text-gray-300">
                  <span class="font-medium text-gray-900 dark:text-white">Plantillas</span> — En el siguiente paso eliges la integración de tienda y una plantilla; se crearán varias <span class="font-medium">tools MCP manuales</span> enlazadas a operaciones reales de tu catálogo (OpenAPI/Postman embebido). Tu agente las invocará como cualquier otra tool.
                </p>
                <p v-else-if="toolMode === 'basic'" class="text-sm text-gray-700 dark:text-gray-300">
                  <span class="font-medium text-gray-900 dark:text-white">Creación básica</span> — Agrega una acción desde tus integraciones; el nombre del tool, la descripción y el input schema se rellenan automáticamente. Puedes editarlos antes de crear el tool.
                </p>
                <p v-else-if="toolMode === 'flow'" class="text-sm text-gray-700 dark:text-gray-300">
                  <span class="font-medium text-gray-900 dark:text-white">Creación de flujo</span> — Construye un tool con el diseñador de workflow. Añade acciones desde tus integraciones OpenAPI o CustomAPI en un flujo guiado.
                </p>
              </div>
            </div>
            <div class="shrink-0 pt-4 border-t border-gray-200 dark:border-gray-700 flex justify-between items-center mt-4">
              <router-link :to="backUrl" class="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors">
                Cancelar
              </router-link>
              <button
                type="button"
                @click="goToStep2"
                :disabled="!toolMode || atLimitTools || (toolMode === 'flow' && atLimitWorkflowTools)"
                :class="['inline-flex items-center gap-1.5 px-4 py-2.5 rounded-lg font-medium transition-colors', (toolMode && !atLimitTools && !(toolMode === 'flow' && atLimitWorkflowTools)) ? 'bg-primary-600 hover:bg-primary-700 text-white' : 'bg-gray-200 dark:bg-gray-600 text-gray-500 cursor-not-allowed']"
              >
                Siguiente
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
              </button>
            </div>
          </div>

          <!-- Step 2: Plantillas de caso de uso (pantalla dedicada) -->
          <div v-show="!isEdit && currentStep === 2 && toolMode === 'templates'" class="p-4 sm:p-6 flex flex-col flex-1 min-h-0">
            <div class="flex-1 min-h-0 overflow-y-auto">
              <PlanLimitAlert
                v-if="atLimitTools"
                :message="toolsLimitMessage"
                billing-link-text="Actualiza tu plan para más"
                class="mb-4 text-sm"
              />
              <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Plantillas de caso de uso</h2>
              <p class="text-sm text-gray-600 dark:text-gray-400 mt-1 max-w-3xl">
                Selecciona la integración de tu tienda y una plantilla. Cada plantilla crea varias tools manuales que el agente usará para llamar a la API (mismas operaciones que ves en el catálogo embebido).
              </p>
              <div class="mt-6 space-y-5">
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-2">Integración</label>
                  <div
                    v-if="storeIntegrationsForTemplates.length"
                    class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3"
                  >
                    <button
                      v-for="int in storeIntegrationsForTemplates"
                      :key="int.id"
                      type="button"
                      @click="toggleUseCaseIntegration(int)"
                      class="flex items-center gap-3 p-4 rounded-xl border-2 text-left transition-all"
                      :class="useCaseIntegrationId === int.id
                        ? 'border-primary-600 bg-primary-50/50 dark:bg-primary-900/20 ring-1 ring-primary-500/30'
                        : 'border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/80 hover:border-gray-300 dark:hover:border-gray-500 card-hover'"
                    >
                      <div class="w-11 h-11 rounded-xl bg-white dark:bg-gray-700 flex items-center justify-center p-1.5 shrink-0 overflow-hidden border border-gray-200 dark:border-gray-600">
                        <img
                          :src="(int.logoUrl || int.LogoUrl) || '/images/avatar-default.svg'"
                          :alt="int.provider || 'Integración'"
                          class="max-h-full max-w-full object-contain"
                          @error="$event.target.src = '/images/avatar-default.svg'"
                        />
                      </div>
                      <div class="min-w-0 flex-1">
                        <span class="font-semibold text-gray-900 dark:text-white truncate block">{{ int.name || int.provider }}</span>
                        <span
                          v-if="int.name"
                          class="text-xs font-medium text-primary-600 dark:text-primary-400"
                        >{{ int.provider }}</span>
                      </div>
                    </button>
                  </div>
                </div>
                <div>
                  <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-2">Equipo / área</label>
                  <div class="flex flex-wrap gap-2" role="group" aria-label="Filtrar por equipo o área">
                    <button
                      v-for="opt in useCaseTeamFilterOptions"
                      :key="opt.value === '' ? '__all' : opt.value"
                      type="button"
                      @click="useCaseTeamFilter = opt.value"
                      class="px-3 py-1.5 rounded-full text-sm font-medium border transition-colors"
                      :class="useCaseTeamFilter === opt.value
                        ? 'border-primary-600 bg-primary-600 text-white shadow-sm dark:bg-primary-600 dark:border-primary-500'
                        : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:border-primary-400 dark:hover:border-primary-500'"
                    >
                      {{ opt.label }}
                    </button>
                  </div>
                </div>
              </div>
              <p v-if="!storeIntegrationsForTemplates.length" class="mt-4 text-sm text-amber-700 dark:text-amber-300">
                Conecta primero una integración VTEX, Shopify, WooCommerce, Magento o Gravity para ver plantillas.
              </p>
              <div v-else-if="useCaseTemplatesLoading" class="mt-4 text-sm text-gray-500 dark:text-gray-400">Cargando plantillas…</div>
              <ul v-else-if="useCaseIntegrationId && filteredUseCaseTemplates.length" class="mt-4 grid grid-cols-1 sm:grid-cols-2 gap-3 max-h-[min(52vh,420px)] overflow-y-auto pr-1 list-none p-0 m-0">
                <li
                  v-for="tpl in filteredUseCaseTemplates"
                  :key="tpl.id"
                  :class="[
                    'rounded-lg border p-4 transition-colors flex flex-col min-h-0',
                    selectedUseCaseTemplateId === tpl.id
                      ? 'border-primary-500 dark:border-primary-400 ring-2 ring-primary-500/40 bg-primary-50/40 dark:bg-primary-950/30'
                      : 'border-gray-200 dark:border-gray-600 bg-gray-50/80 dark:bg-gray-800/80'
                  ]"
                >
                  <div class="flex flex-col flex-1 gap-2 min-h-0">
                    <div class="min-w-0">
                      <p class="font-medium text-gray-900 dark:text-white">{{ tpl.name }}</p>
                      <p v-if="tpl.description" class="text-sm text-gray-600 dark:text-gray-400 mt-0.5">{{ tpl.description }}</p>
                      <p class="text-xs text-gray-500 dark:text-gray-500 mt-1">{{ tpl.team }} · {{ tpl.tools?.length ?? 0 }} tools</p>
                      <ul class="mt-2 text-xs text-gray-500 dark:text-gray-400 font-mono space-y-0.5 max-h-28 overflow-y-auto">
                        <li v-for="(t, i) in (tpl.tools || []).slice(0, 12)" :key="i">· {{ t.title || t.name }}</li>
                        <li v-if="(tpl.tools || []).length > 12">…</li>
                      </ul>
                    </div>
                    <button
                      type="button"
                      :disabled="!canApplyUseCaseTemplate(tpl)"
                      class="shrink-0 w-full sm:w-auto mt-auto inline-flex items-center justify-center px-4 py-2.5 rounded-lg text-sm font-medium transition-colors"
                      :class="!canApplyUseCaseTemplate(tpl)
                        ? 'bg-gray-200 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                        : selectedUseCaseTemplateId === tpl.id
                          ? 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200 hover:bg-gray-200 dark:hover:bg-gray-600 border border-gray-300 dark:border-gray-500'
                          : 'bg-primary-600 hover:bg-primary-700 text-white'"
                      @click="toggleUseCaseTemplateSelection(tpl)"
                    >
                      {{ selectedUseCaseTemplateId === tpl.id ? 'Quitar' : 'Usar' }}
                    </button>
                  </div>
                  <p v-if="!canApplyUseCaseTemplate(tpl) && mcpToolsLimit != null" class="mt-2 text-xs text-amber-600 dark:text-amber-400">
                    No hay cupo suficiente en el plan ({{ toolsUsedCount }}/{{ mcpToolsLimit }}; esta plantilla añade {{ tpl.tools?.length ?? 0 }}).
                  </p>
                </li>
              </ul>
              <p v-else-if="useCaseIntegrationId && !useCaseTemplatesLoading" class="mt-4 text-sm text-gray-500 dark:text-gray-400">No hay plantillas para los filtros seleccionados.</p>
            </div>
            <div class="shrink-0 pt-4 border-t border-gray-200 dark:border-gray-700 flex justify-between items-center mt-4">
              <button type="button" @click="currentStep = 1" class="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors flex items-center gap-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" /></svg>
                Anterior
              </button>
              <button
                type="button"
                @click="handleSaveUseCaseTemplates"
                :disabled="useCaseTemplateSaveLoading || !useCaseTemplateSaveValid"
                :class="['inline-flex items-center gap-1.5 px-4 py-2.5 rounded-lg font-medium transition-colors', (useCaseTemplateSaveLoading || !useCaseTemplateSaveValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']"
              >
                {{ useCaseTemplateSaveLoading ? 'Guardando…' : 'Guardar' }}
              </button>
            </div>
          </div>

          <!-- Step 2: Creación Básica (batch: listado de cards, agregar / editar / quitar, guardar batch) -->
          <div v-show="!isEdit && currentStep === 2 && toolMode === 'basic'" class="p-4 sm:p-6 flex flex-col flex-1 min-h-0">
            <div class="flex-1 min-h-0 overflow-y-auto">
              <PlanLimitAlert
                v-if="!canAddMoreToBatch && mcpToolsLimit != null"
                :message="batchAtLimitMessage"
                billing-link-text="Actualiza tu plan para más"
                class="mb-4 text-sm"
              />
              <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">Añade una o más acciones; cada una se convertirá en un tool. Edita o elimina antes de guardar.</p>
              <div v-if="batchTools.length >= 2" class="mb-4 flex flex-wrap items-center gap-2">
                <span v-if="hasDuplicateActionsInBatch" class="text-sm text-amber-700 dark:text-amber-300">Hay acciones duplicadas (misma integración y operación).</span>
                <span v-else class="text-sm text-gray-600 dark:text-gray-400">Si añadiste la misma acción varias veces, puedes eliminar duplicados.</span>
                <button
                  type="button"
                  @click="removeDuplicateActionsFromBatch"
                  class="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-medium text-amber-800 dark:text-amber-200 bg-amber-100 dark:bg-amber-900/40 hover:bg-amber-200 dark:hover:bg-amber-900/60 border border-amber-300 dark:border-amber-700 transition-colors"
                >
                  Eliminar duplicados
                </button>
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
                <button
                  v-if="canAddMoreToBatch"
                  type="button"
                  @click="openAddActionForBasic"
                  class="p-4 rounded-xl border-2 border-gray-300 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/50 text-gray-600 dark:text-gray-400 card-add-hover font-medium flex flex-col items-center justify-center gap-2 min-h-[140px]"
                >
                  <svg class="w-8 h-8 text-gray-400 dark:text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                  <span>Agregar Acción</span>
                </button>
                <div
                  v-else
                  class="p-4 rounded-xl border-2 border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 flex flex-col items-center justify-center gap-2 min-h-[140px] cursor-not-allowed"
                >
                  <svg class="w-8 h-8 text-gray-400 dark:text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                  <span class="font-medium text-gray-500 dark:text-gray-400">Agregar Acción</span>
                  <p class="text-xs text-amber-600 dark:text-amber-400 text-center">Límite alcanzado ({{ toolsUsedCount + batchTools.length }}/{{ mcpToolsLimit }}). <router-link to="/admin/billing" class="underline hover:no-underline">Actualiza tu plan para más</router-link></p>
                </div>
                <div
                  v-for="(item, idx) in batchTools"
                  :key="item.id"
                  :class="[
                    'p-4 rounded-xl border flex flex-col',
                    batchToolDuplicateNames.has((item.name || '').trim())
                      ? 'border-red-500 dark:border-red-400 ring-1 ring-red-500/30 dark:ring-red-400/30 bg-red-50/30 dark:bg-red-900/10'
                      : 'border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/50'
                  ]"
                >
                  <div v-if="batchToolDuplicateNames.has((item.name || '').trim())" class="flex items-center gap-1.5 mb-2 text-red-600 dark:text-red-400 text-xs font-medium">
                    <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
                    <span>Nombre duplicado — edita o elimina para poder guardar</span>
                  </div>
                  <div class="min-w-0 flex-1">
                    <p class="font-medium text-gray-900 dark:text-white truncate">{{ item.title || item.name || 'Sin título' }}</p>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 font-mono">{{ item.name || '—' }}</p>
                    <p v-if="item.description" class="mt-2 text-sm text-gray-600 dark:text-gray-300 line-clamp-2">{{ item.description }}</p>
                  </div>
                  <div class="flex justify-end gap-1 mt-3 pt-3 border-t border-gray-200 dark:border-gray-600">
                    <button type="button" @click="openEditBatchTool(idx)" class="p-2 text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 rounded" title="Editar">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                    </button>
                    <button type="button" @click="removeBatchTool(idx)" class="p-2 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded" title="Quitar">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
            <div class="shrink-0 pt-4 border-t border-gray-200 dark:border-gray-700 flex justify-between items-center mt-4">
              <button type="button" @click="currentStep = 1" class="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors flex items-center gap-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" /></svg>
                Anterior
              </button>
              <button
                type="button"
                @click="handleCreateBasicToolBatch"
                :disabled="basicToolLoading || !basicBatchFormValid"
                :class="['inline-flex items-center gap-1.5 px-4 py-2.5 rounded-lg font-medium transition-colors', (basicToolLoading || !basicBatchFormValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']"
              >
                {{ basicToolLoading ? 'Guardando…' : 'Guardar' }}
              </button>
            </div>
          </div>

          <!-- Slider: editar tool del batch -->
          <Teleport to="body">
            <div
              v-show="showEditBatchSlider"
              class="fixed inset-0 z-[100] flex"
              aria-modal="true"
              role="dialog"
            >
              <div class="fixed inset-0 bg-black/50" aria-hidden="true" />
              <div class="relative ml-auto w-full max-w-lg lg:max-w-2xl h-full bg-white dark:bg-gray-900 shadow-xl flex flex-col overflow-hidden animate-slide-in">
                <div class="shrink-0 px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                  <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Editar tool</h3>
                  <button type="button" @click="closeEditBatchSlider" class="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
                  </button>
                </div>
                <div class="flex-1 overflow-y-auto p-4 space-y-4">
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
                    <input v-model="editBatchForm.name" type="text" placeholder="get_weather" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" @input="editBatchForm.name = (editBatchForm.name || '').replace(/[^a-zA-Z0-9_]/g, '')" />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Título <span class="text-red-500">*</span></label>
                    <input v-model="editBatchForm.title" type="text" placeholder="Obtener clima" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción</label>
                    <textarea v-model="editBatchForm.description" rows="2" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" placeholder="Commerceional" />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Parámetros</label>
                    <ToolInputSchemaFieldsForm v-model="editBatchForm.schemaFields" />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Output Schema</label>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">Selecciona las propiedades que debe devolver el tool.</p>
                    <ToolOutputPropertiesSection
                      :schema-tree="editBatchOutputSchemaTree"
                      :available-paths="editBatchOutputPathsFlatOnly"
                      v-model:selected-paths="selectedEditOutputPaths"
                      path-list-variant="stacked"
                      :allow-manual-when-empty="false"
                      :readonly-fallback-preview="editBatchOutputSchemaPreview"
                      enable-preview-toolbar
                      v-model:inline-preview-open="editBatchOutputPreviewInline"
                      v-model:preview-full-schema="editBatchOutputPreviewFullSchema"
                      v-model:expanded-overlay="editBatchOutputPropsExpanded"
                      :expanded-teleport-visible="showEditBatchSlider"
                      :preview-text="editBatchOutputSelectionPreviewText"
                    />
                  </div>
                </div>
                <div class="shrink-0 p-4 border-t border-gray-200 dark:border-gray-700 flex flex-col sm:flex-row gap-3 sm:gap-3">
                  <button type="button" @click="closeEditBatchSlider" class="w-full sm:flex-1 py-2.5 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 font-medium transition-colors">Cancelar</button>
                  <button type="button" @click="saveEditBatchTool" class="w-full sm:flex-1 py-2.5 rounded-lg bg-primary-600 hover:bg-primary-700 text-white font-medium transition-colors">Guardar</button>
                </div>
              </div>
            </div>
          </Teleport>

          <!-- Step 2: Manual form (solo al editar tool manual) -->
          <div v-show="isEdit && !isFlowToolFromData" class="p-4 sm:p-6 flex flex-col flex-1 min-h-0">
            <div class="flex-1 min-h-0 overflow-y-auto">
              <form id="manual-tool-form" @submit.prevent="handleAddTool" class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Título <span class="text-red-500">*</span></label>
                <input
                  v-model="addToolForm.title"
                  type="text"
                  required
                  placeholder="Obtener clima"
                  :class="[
                    'w-full px-3 py-2 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white transition-colors',
                    titleTouched && !(addToolForm.title?.trim())
                      ? 'border-2 border-red-500 focus:border-red-500 focus:ring-red-500'
                      : 'border border-gray-300 dark:border-gray-600 focus:border-primary-500 focus:ring-primary-500'
                  ]"
                  @blur="titleTouched = true"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
                <input
                  v-model="addToolForm.name"
                  type="text"
                  required
                  placeholder="get_weather"
                  :class="[
                    'w-full px-3 py-2 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white transition-colors',
                    nameTouched && !isNameValid
                      ? 'border-2 border-red-500 focus:border-red-500 focus:ring-red-500'
                      : 'border border-gray-300 dark:border-gray-600 focus:border-primary-500 focus:ring-primary-500'
                  ]"
                  @blur="nameTouched = true"
                  @input="addToolForm.name = (addToolForm.name || '').replace(/[^a-zA-Z0-9_]/g, '')"
                />
                <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Alfanumérico y sin espacios (ej: get_weather)</p>
                <p v-if="nameTouched && addToolForm.name && !isNameValid" class="mt-1 text-xs text-red-500">Solo letras, números y guión bajo. Sin espacios.</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción (opcional)</label>
                <textarea v-model="addToolForm.description" rows="2" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Input Schema (parámetros del tool)</label>
                <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">Define los parámetros que recibirá el tool. Cada uno se guardará como JSON Schema.</p>
                <ToolInputSchemaFieldsForm v-model="inputSchemaFields" />
              </div>
              <!-- Comportamiento del paso (tool manual tiene 1 step internamente) -->
              <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/30">
                <div class="px-3 py-2.5 text-xs font-semibold text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/70">Comportamiento del paso</div>
                <div class="p-3 space-y-4">
                  <div>
                    <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1.5">Al fallar</label>
                    <select
                      v-model="manualStepOnFailBehavior"
                      class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
                    >
                      <option value="stop">Detener ejecución</option>
                      <option value="continue">Continuar</option>
                    </select>
                    <label v-if="manualStepOnFailBehavior === 'continue'" class="flex items-center gap-2 mt-2 text-sm text-gray-600 dark:text-gray-400">
                      <input v-model="manualStepOnFailRecordFailure" type="checkbox" class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500" />
                      <span>Registrar fallo en logs</span>
                    </label>
                  </div>
                  <div>
                    <label class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 cursor-pointer">
                      <input v-model="manualStepOnSuccessLog" type="checkbox" class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500" />
                      <span>Al éxito: registrar en logs</span>
                    </label>
                  </div>
                </div>
              </div>
              <ToolOutputPropertiesSection
                v-if="isEdit"
                :schema-tree="editToolOutputSchemaTree"
                :available-paths="editToolOutputPathsFlatOnly"
                v-model:selected-paths="editToolSelectedOutputPaths"
                v-model:manual-paths="editToolManualOutputPaths"
                path-list-variant="inline"
                path-description="Propiedades de la respuesta que expone el tool. Marca las que quieras incluir."
                manual-intro="Sin schema de respuesta. Define a mano las propiedades que debe devolver el tool."
                manual-hint="Indica path y tipo (ej. id, data.name). Para array: path + tipo de elemento; para objeto anidado: address.street, address.city."
                enable-preview-toolbar
                v-model:inline-preview-open="editToolOutputPreviewInline"
                v-model:preview-full-schema="editToolOutputPreviewFullSchema"
                v-model:expanded-overlay="editToolOutputPropsExpanded"
                :expanded-teleport-visible="isEdit && !isFlowToolFromData"
                :preview-text="editToolOutputSelectionPreviewText"
              />
              <div v-if="isEdit" class="flex items-center gap-3">
                <label class="relative inline-flex items-center cursor-pointer select-none">
                  <input v-model="addToolForm.isEnabled" type="checkbox" class="sr-only peer" />
                  <div class="w-12 h-7 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[3px] after:left-[3px] after:bg-white after:rounded-full after:h-6 after:w-6 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5" />
                  <span class="ml-3 text-sm font-medium text-gray-700 dark:text-gray-300">Habilitado</span>
                </label>
                <p class="text-xs text-gray-500 dark:text-gray-400">Si está deshabilitado, el tool no se expondrá a clientes MCP.</p>
              </div>
              <div class="rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 overflow-hidden">
                <div class="px-3 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600">Preview: Tool Definition (MCP spec)</div>
                <pre class="p-3 text-xs font-mono text-gray-800 dark:text-gray-200 overflow-x-auto max-h-48 overflow-y-auto">{{ toolDefinitionPreviewJson }}</pre>
              </div>
            </form>
            </div>
            <div class="shrink-0 pt-4 border-t border-gray-200 dark:border-gray-700 flex flex-wrap justify-between items-center gap-3 mt-4">
              <router-link v-if="isEdit" :to="backUrl" class="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors">
                Cancelar
              </router-link>
              <button v-else type="button" @click="currentStep = 1" class="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors flex items-center gap-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" /></svg>
                Anterior
              </button>
              <button type="submit" form="manual-tool-form" :disabled="addToolLoading || !manualFormValid" :class="['inline-flex items-center gap-1.5 px-4 py-2.5 rounded-lg font-medium transition-colors', (addToolLoading || !manualFormValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']">
                Guardar
              </button>
            </div>
          </div>

          <!-- Execute tool slider (reutilizable) -->
          <ExecuteToolSlider
            v-model="showExecuteModal"
            :tool="executeToolForSlider"
            :mcp-id="mcpId"
          />

          <!-- Creación de flujo o edición de tool workflow: canvas + nombre/título/descripción -->
          <div v-show="showFlowCanvas" class="p-4 sm:p-6 flex flex-col flex-1 min-h-0">
            <div class="flex-1 min-h-0 overflow-y-auto space-y-4">
              <p class="text-sm text-gray-600 dark:text-gray-400">Construye el workflow añadiendo acciones desde tus integraciones. Haz clic en el nodo Inicio o arrastra un conector para elegir integración y operación.</p>
              <!-- Diseñador de workflow (más altura) -->
              <WorkflowEditor
                v-model="importWorkflowJson"
                add-action-mode="modal"
                min-height="min(70vh, 640px)"
                :integrations="myIntegrations"
                :load-test-workflow="loadTestWorkflowFromQuery"
                :show-execute-in-canvas="isEdit"
                @add-action-request="(e) => { addActionSourceNodeId = e?.sourceNodeId ?? null; addActionBranch = e?.branch ?? null; editActionStep = null; editConditionalStep = null; showAddActionModal = true }"
                @edit-action-request="(e) => { editActionStep = e; addActionSourceNodeId = null; addActionBranch = null; editConditionalStep = null; showAddActionModal = true }"
                @edit-conditional-request="(e) => { editConditionalStep = e; editActionStep = null; addActionSourceNodeId = null; addActionBranch = null; showAddActionModal = false; showEditConditionalModal = true }"
                @edit-start-input-request="showEditStartInputModal = true"
                @set-output-step="handleSetOutputStep"
                @execute-request="openExecuteSlider"
              />
              <!-- Resultado del tool: un solo paso (cuando no hay resultado agrupado) -->
              <div v-if="!hasAggregatedOutput && outputStepFromWorkflow" class="rounded-lg border border-emerald-200 dark:border-emerald-800 bg-emerald-50/50 dark:bg-emerald-900/20 p-3 flex flex-wrap items-center gap-2">
                <span class="text-sm font-medium text-emerald-800 dark:text-emerald-200">Resultado del tool:</span>
                <span class="text-sm text-emerald-700 dark:text-emerald-300">{{ outputStepFromWorkflow.summary || outputStepFromWorkflow.operationId || outputStepFromWorkflow.id }}</span>
                <button
                  type="button"
                  class="text-xs px-3 py-1.5 rounded-lg font-medium bg-emerald-600 hover:bg-emerald-700 text-white"
                  @click="openEditOutputStepForDto"
                >
                  Personalizar resultado
                </button>
              </div>
              <p v-else-if="!hasAggregatedOutput && importWorkflowSteps.length > 0" class="text-sm text-amber-600 dark:text-amber-400">
                Haz clic en «Resultado» en un nodo para marcar qué paso devuelve la respuesta del tool.
              </p>
              <!-- Resultado agrupado: combinar varios pasos en un solo resultado -->
              <div v-if="importWorkflowActionSteps.length >= 2" class="rounded-lg border border-slate-200 dark:border-slate-600 bg-slate-50/50 dark:bg-slate-900/20 p-3 space-y-3">
                <p class="text-sm font-medium text-slate-800 dark:text-slate-200">Resultado agrupado (varios pasos)</p>
                <p class="text-xs text-slate-600 dark:text-slate-400">Combina los resultados de distintos pasos en un solo objeto. Abre el editor para elegir qué campos tendrá la respuesta.</p>
                <div v-if="hasAggregatedOutput" class="rounded-lg border border-slate-200 dark:border-slate-600 overflow-hidden bg-white dark:bg-gray-800/50">
                  <div class="px-3 py-2 text-xs font-medium text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-600 bg-slate-50 dark:bg-slate-800/50">Vista previa del resultado</div>
                  <pre class="p-3 text-xs font-mono text-slate-800 dark:text-slate-200 overflow-x-auto max-h-32 overflow-y-auto">{{ aggregatedOutputPreviewJson }}</pre>
                </div>
                <button
                  type="button"
                  class="text-sm px-4 py-2 rounded-lg font-medium bg-slate-600 hover:bg-slate-700 text-white"
                  @click="showEditAggregatedOutputModal = true"
                >
                  {{ hasAggregatedOutput ? 'Editar resultado agrupado' : 'Configurar resultado agrupado' }}
                </button>
              </div>
              <!-- Nombre, título y descripción debajo del workflow -->
              <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
                  <input v-model="importToolForm.name" type="text" placeholder="get_product" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" @input="importToolForm.name = (importToolForm.name || '').replace(/[^a-zA-Z0-9_]/g, '')" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Título <span class="text-red-500">*</span></label>
                  <input v-model="importToolForm.title" type="text" placeholder="Obtener producto" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción</label>
                  <input v-model="importToolForm.description" type="text" placeholder="Commerceional" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                </div>
              </div>
              <div v-if="isEdit" class="flex items-center gap-3">
                <label class="relative inline-flex items-center cursor-pointer select-none">
                  <input v-model="addToolForm.isEnabled" type="checkbox" class="sr-only peer" />
                  <div class="w-12 h-7 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[3px] after:left-[3px] after:bg-white after:rounded-full after:h-6 after:w-6 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5" />
                  <span class="ml-3 text-sm font-medium text-gray-700 dark:text-gray-300">Habilitado</span>
                </label>
                <p class="text-xs text-gray-500 dark:text-gray-400">Si está deshabilitado, el tool no se expondrá a clientes MCP.</p>
              </div>
              <div class="rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 overflow-hidden">
                <div class="px-3 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600">Preview: Tool Definition (MCP spec)</div>
                <pre class="p-3 text-xs font-mono text-gray-800 dark:text-gray-200 overflow-x-auto max-h-48 overflow-y-auto">{{ importToolDefinitionPreviewJson }}</pre>
              </div>
            </div>
            <div class="shrink-0 pt-4 border-t border-gray-200 dark:border-gray-700 flex flex-wrap justify-between items-center gap-3 mt-4">
              <router-link v-if="isEdit" :to="backUrl" class="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors">
                Cancelar
              </router-link>
              <button v-else type="button" @click="currentStep = 1" class="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 transition-colors flex items-center gap-1">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" /></svg>
                Anterior
              </button>
              <PlanLimitAlert
                v-if="!isEdit && atLimitTools"
                :message="toolsLimitMessage"
                billing-link-text="Actualiza tu plan para más"
                class="text-sm"
              />
              <button
                type="button"
                @click="handleSaveFlowTool"
                :disabled="importToolLoading || !importFormValid || (!isEdit && atLimitTools)"
                :class="['inline-flex items-center gap-1.5 px-4 py-2.5 rounded-lg font-medium transition-colors', (importToolLoading || !importFormValid || (!isEdit && atLimitTools)) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']"
              >
                {{ isEdit ? 'Guardar' : 'Crear tool' }}
              </button>
            </div>
          </div>

          <!-- Modal slider: Añadir acción (Paso 1: integración, Paso 2: acción, Paso 3: schema) -->
          <AddActionModal
            v-model="showAddActionModal"
            :integrations="myIntegrations"
            :edit-step="toolMode === 'flow' ? editActionStep : null"
            :parent-step="toolMode === 'flow' ? addActionParentStep : null"
            :workflow-json="toolMode === 'flow' ? importWorkflowJson : '{}'"
            :tool-input-schema="toolMode === 'flow' ? importToolInputSchema : '{}'"
            @add="onAddActionFromModal"
            @update="handleUpdateActionFromModal"
          />
          <!-- Modal: Input schema del nodo inicio (global para el tool) -->
          <EditStartInputModal
            v-model="showEditStartInputModal"
            :input-schema-json="importToolInputSchema"
            @save="handleSaveStartInput"
          />
          <!-- Modal: Editar reglas del condicional -->
          <EditConditionalModal
            v-model="showEditConditionalModal"
            :conditional-step="editConditionalStep"
            :workflow-json="importWorkflowJson"
            :input-schema-json="importToolInputSchema"
            :integrations="myIntegrations"
            @save="handleSaveConditionalRules"
          />
          <EditAggregatedOutputModal
            v-model="showEditAggregatedOutputModal"
            :workflow-json="importWorkflowJson"
            :output-mapping="importWorkflowOutputMapping"
            @save="handleSaveAggregatedOutput"
          />
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService, { getApiErrorMessage } from '../../services/api'
import { buildBatchItemsFromTemplate } from '../../services/useCaseTemplateBatch'
import { normalizeWorkflowDefinitionForSubmit } from '../../services/toolPayload'
import { isFlowTool } from '../../utils/workflow'
import WorkflowEditor from '../../components/workflow/WorkflowEditor.vue'
import AddActionModal from '../../components/workflow/AddActionModal.vue'
import EditConditionalModal from '../../components/workflow/EditConditionalModal.vue'
import EditStartInputModal from '../../components/workflow/EditStartInputModal.vue'
import EditAggregatedOutputModal from '../../components/workflow/EditAggregatedOutputModal.vue'
import ExecuteToolSlider from '../../components/mcp/ExecuteToolSlider.vue'
import ToolInputSchemaFieldsForm from '../../components/mcp/ToolInputSchemaFieldsForm.vue'
import ToolOutputPropertiesSection from '../../components/mcp/ToolOutputPropertiesSection.vue'
import { buildMinimalOutputSchemaFromPaths, buildOutputSchemaFromSelection, computeOutputSelectionPreviewText } from '../../utils/outputSchema'
import { buildOutputSchemaTreeFromResponseSchema } from '../../utils/outputSchemaTree'
import PlanLimitAlert from '../../components/usage/PlanLimitAlert.vue'
import { useAccountUsage } from '../../composables/useAccountUsage'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const mcpId = computed(() => route.params.mcpId)
const toolId = computed(() => route.params.toolId)
const isEdit = computed(() => !!toolId.value)

const { usage, isAtLimitForMcpTools, getLimitMessageForMcpTools, isAtLimitForMcpWorkflowTools, getLimitMessageForMcpWorkflowTools, fetchUsage } = useAccountUsage()
const toolsUsedCount = computed(() => mcp.value?.tools?.length ?? 0)
const atLimitTools = computed(() => isAtLimitForMcpTools(toolsUsedCount.value))
const toolsLimitMessage = computed(() => getLimitMessageForMcpTools(toolsUsedCount.value))
const mcpToolsLimit = computed(() => usage.value?.usage?.mcpToolsLimit ?? null)
const mcpWorkflowToolsLimit = computed(() => usage.value?.usage?.mcpWorkflowToolsLimit ?? null)
const workflowToolsCount = computed(() => mcp.value?.tools?.filter(t => t.toolType === 'Workflow').length ?? 0)
const currentToolIsWorkflow = computed(() => isEdit.value && mcp.value?.tools?.some(t => t.id === toolId.value && t.toolType === 'Workflow'))
const atLimitWorkflowTools = computed(() => {
  const limit = mcpWorkflowToolsLimit.value
  if (limit == null) return false
  if (isEdit.value && currentToolIsWorkflow.value) return false
  return workflowToolsCount.value >= limit
})
const workflowLimitMessage = computed(() => getLimitMessageForMcpWorkflowTools(workflowToolsCount.value))
const canAddMoreToBatch = computed(() => {
  const limit = mcpToolsLimit.value
  if (limit == null) return true
  return (toolsUsedCount.value + batchTools.value.length) < limit
})
const batchAtLimitMessage = computed(() => {
  const total = toolsUsedCount.value + batchTools.value.length
  return getLimitMessageForMcpTools(total)
})
/** Si la URL tiene load-test-workflow=true, el WorkflowEditor carga el grafo de prueba al iniciar. */
const loadTestWorkflowFromQuery = computed(() => route.query['load-test-workflow'] === 'true')
const mcp = ref(null)
const loading = ref(true)
const error = ref(null)
const currentStep = ref(1)
const toolMode = ref(null) // 'basic' | 'flow' | 'templates' | 'manual' (edición)
const addToolLoading = ref(false)
/** Creación Básica: form y acción seleccionada (legacy, ya no usado en Step 2) */
const basicToolForm = ref({ name: '', title: '', description: '' })
const basicSelectedAction = ref(null)
const basicInputSchemaFields = ref([])
const basicToolLoading = ref(false)
/** Creación Básica en batch: lista de tools en memoria */
const batchTools = ref([])
const showEditBatchSlider = ref(false)
const editBatchIndex = ref(null)
const editBatchForm = ref({ name: '', title: '', description: '', schemaFields: [] })
/** Rutas disponibles para el output schema en edición (al abrir el slider) */
const editBatchOutputAvailablePaths = ref([])
/** Rutas seleccionadas para el output schema en edición */
const selectedEditOutputPaths = ref([])
const editBatchOutputPreviewInline = ref(false)
const editBatchOutputPreviewFullSchema = ref(true)
const editBatchOutputPropsExpanded = ref(false)
const addToolForm = ref({ integrationIds: [], name: '', title: '', description: '', executionConfig: '', workflowDefinitionJson: '{}', isEnabled: true })
const inputSchemaFields = ref([])
/** Output properties en vista editar tool: paths disponibles y seleccionados */
const editToolOutputAvailablePaths = ref([])
const editToolSelectedOutputPaths = ref([])
const editToolOutputSchemaFull = ref(null)
/** Cuando no hay output schema/paths, el usuario define propiedades a mano (path + type + itemType). */
const editToolManualOutputPaths = ref([{ path: '', type: 'string', itemType: 'string' }])
const editToolOutputPreviewInline = ref(false)
const editToolOutputPreviewFullSchema = ref(true)
const editToolOutputPropsExpanded = ref(false)
const nameTouched = ref(false)
const titleTouched = ref(false)
const importToolForm = ref({ name: '', title: '', description: '' })
const importWorkflowJson = ref('{}')
const importToolInputSchema = ref('{}')
const showAddActionModal = ref(false)
const showEditConditionalModal = ref(false)
const showEditStartInputModal = ref(false)
const showEditAggregatedOutputModal = ref(false)
const addActionSourceNodeId = ref(null)
const addActionBranch = ref(null) // 'success' | 'failure' when adding to conditional branch
const editActionStep = ref(null)
const editConditionalStep = ref(null)
const importToolLoading = ref(false)
const myIntegrations = ref([])

const useCaseTemplateProviders = new Set(['VTEX', 'Shopify', 'WooCommerce', 'Magento', 'Gravity'])
const useCaseIntegrationId = ref('')
const useCaseTeamFilter = ref('')
const useCaseTeamFilterOptions = [
  { value: '', label: 'Todos' },
  { value: 'Content', label: 'Content' },
  { value: 'Sales & Growth', label: 'Sales & Growth' },
  { value: 'Operations / Fulfillment', label: 'Operations / Fulfillment' },
  { value: 'Customer Support', label: 'Customer Support' }
]
const useCaseTemplatesRaw = ref([])
const useCaseTemplatesLoading = ref(false)
const selectedUseCaseTemplateId = ref('')
const useCaseTemplateSaveLoading = ref(false)

const storeIntegrationsForTemplates = computed(() =>
  (myIntegrations.value || []).filter(i => i?.provider && useCaseTemplateProviders.has(i.provider))
)

const filteredUseCaseTemplates = computed(() => {
  const team = (useCaseTeamFilter.value || '').trim()
  const items = useCaseTemplatesRaw.value || []
  if (!team) return items
  return items.filter(t => (t.team || '') === team)
})

const selectedUseCaseTemplate = computed(() => {
  const id = selectedUseCaseTemplateId.value
  if (!id) return null
  return filteredUseCaseTemplates.value.find(t => t.id === id) ?? null
})

const useCaseTemplateSaveValid = computed(() => {
  if (!useCaseIntegrationId.value || !selectedUseCaseTemplateId.value) return false
  const tpl = selectedUseCaseTemplate.value
  if (!tpl) return false
  return canApplyUseCaseTemplate(tpl)
})

watch(useCaseIntegrationId, async (id) => {
  selectedUseCaseTemplateId.value = ''
  useCaseTemplatesRaw.value = []
  if (!id) return
  const int = myIntegrations.value.find(i => i.id === id)
  if (!int?.provider) return
  useCaseTemplatesLoading.value = true
  try {
    const res = await apiService.getUseCaseTemplates({ provider: int.provider })
    useCaseTemplatesRaw.value = res?.items ?? []
  } catch (e) {
    toast.error(getApiErrorMessage(e) || 'No se pudieron cargar las plantillas')
  } finally {
    useCaseTemplatesLoading.value = false
  }
})

watch(filteredUseCaseTemplates, (list) => {
  const id = selectedUseCaseTemplateId.value
  if (!id) return
  if (!list.some(t => t.id === id)) selectedUseCaseTemplateId.value = ''
})

/** En paso 2 plantillas: mantener una integración de tienda seleccionada si hay alguna. */
function ensureUseCaseTemplateIntegration() {
  if (toolMode.value !== 'templates' || currentStep.value !== 2) return
  const list = storeIntegrationsForTemplates.value
  if (!list.length) {
    useCaseIntegrationId.value = ''
    return
  }
  const cur = useCaseIntegrationId.value
  if (!cur || !list.some(i => i.id === cur)) {
    useCaseIntegrationId.value = list[0].id
  }
}

watch(
  () => [
    currentStep.value,
    toolMode.value,
    (storeIntegrationsForTemplates.value || []).map(i => i.id).join(',')
  ],
  () => {
    ensureUseCaseTemplateIntegration()
  }
)

function canApplyUseCaseTemplate(tpl) {
  const n = tpl?.tools?.length ?? 0
  if (n === 0) return false
  const limit = mcpToolsLimit.value
  if (limit == null) return true
  return toolsUsedCount.value + n <= limit
}

function toggleUseCaseIntegration(int) {
  if (!int?.id) return
  if (useCaseIntegrationId.value === int.id) return
  useCaseIntegrationId.value = int.id
}

function toggleUseCaseTemplateSelection(tpl) {
  if (!tpl?.id || !canApplyUseCaseTemplate(tpl)) return
  if (selectedUseCaseTemplateId.value === tpl.id) {
    selectedUseCaseTemplateId.value = ''
  } else {
    selectedUseCaseTemplateId.value = tpl.id
  }
}

async function handleSaveUseCaseTemplates() {
  const intId = useCaseIntegrationId.value
  const tplId = selectedUseCaseTemplateId.value
  if (!intId || !tplId) return
  const tpl = selectedUseCaseTemplate.value
  if (!tpl || !canApplyUseCaseTemplate(tpl)) return
  useCaseTemplateSaveLoading.value = true
  try {
    const result = await apiService.getIntegrationOperations(intId)
    const ops = result?.operations ?? result?.items ?? []
    const { batchItems, unresolved } = buildBatchItemsFromTemplate(tpl, intId, ops)
    if (unresolved.length) {
      const msg = unresolved.slice(0, 5).join('; ')
      toast.error(`No se pudieron enlazar operaciones: ${msg}${unresolved.length > 5 ? '…' : ''}`)
      return
    }
    if (!batchItems.length) {
      toast.error('La plantilla no tiene tools válidas')
      return
    }
    const limit = mcpToolsLimit.value
    if (limit != null && (toolsUsedCount.value + batchItems.length) > limit) {
      toast.error(getLimitMessageForMcpTools(toolsUsedCount.value + batchItems.length))
      return
    }
    const payload = batchItems.map(t => ({
      name: (t.name || '').trim(),
      title: (t.title || '').trim(),
      description: t.description,
      inputSchema: t.inputSchema ?? '{}',
      workflowDefinitionJson: normalizeWorkflowDefinitionForSubmit(t.workflowDefinitionJson),
      integrationIds: t.integrationIds || []
    }))
    await apiService.addMcpToolsBatch(mcpId.value, payload)
    toast.success(`Se crearon ${batchItems.length} tools`)
    router.push(backUrl.value)
  } catch (e) {
    toast.error(getApiErrorMessage(e) || 'Error al crear')
  } finally {
    useCaseTemplateSaveLoading.value = false
  }
}

const showExecuteModal = ref(false)
/** Tool payload para el slider de ejecutar (id, name, title, inputSchema) */
const executeToolForSlider = ref(null)
/** Comportamiento del paso (solo tool manual en edición): onFail / onSuccess */
const manualStepOnFailBehavior = ref('stop')
const manualStepOnFailRecordFailure = ref(false)
const manualStepOnSuccessLog = ref(false)

function openExecuteSlider() {
  if (!toolId.value) return
  executeToolForSlider.value = {
    id: toolId.value,
    name: importToolForm.value?.name || addToolForm.value?.name || '',
    title: importToolForm.value?.title || addToolForm.value?.title || '',
    inputSchema: addToolForm.value?.inputSchema || importToolInputSchema.value || '{}'
  }
  showExecuteModal.value = true
}

const backUrl = computed(() => `/admin/mcps/${mcpId.value}`)

/** En edición: true si el tool cargado es workflow (muestra canvas). */
const isFlowToolFromData = computed(() => isEdit.value && toolMode.value === 'flow')

/** Mostrar panel del canvas (creación flow o edición de tool workflow). */
const showFlowCanvas = computed(() =>
  (!isEdit.value && currentStep.value === 2 && toolMode.value === 'flow') || (isEdit.value && isFlowToolFromData.value)
)

/** Step padre cuando añadimos acción como hijo (para sugerir output como input) */
const addActionParentStep = computed(() => {
  const sourceId = addActionSourceNodeId.value
  if (!sourceId) return null
  try {
    const w = JSON.parse(importWorkflowJson.value || '{}')
    const step = (w.steps || []).find(s => s.id === sourceId)
    return step?.type === 'action' && step?.integrationId && step?.operationId ? step : null
  } catch {
    return null
  }
})

/** Pasos del workflow (modo crear tool por flujo) */
const importWorkflowSteps = computed(() => {
  try {
    const w = JSON.parse(importWorkflowJson.value || '{}')
    return Array.isArray(w.steps) ? w.steps : []
  } catch {
    return []
  }
})

/** Pasos de tipo action del workflow (para dropdown de resultado agrupado) */
const importWorkflowActionSteps = computed(() => {
  const steps = importWorkflowSteps.value
  return steps.filter(s => s.type === 'action' && s.id)
})

/** outputMapping del workflow parseado: { key -> placeholder } */
const importWorkflowOutputMapping = computed(() => {
  try {
    const w = JSON.parse(importWorkflowJson.value || '{}')
    const om = w.outputMapping
    return om && typeof om === 'object' && !Array.isArray(om) ? om : {}
  } catch {
    return {}
  }
})

const OUTPUT_MAPPING_PLACEHOLDER_REGEX = /^\{([^.]+)\.output\.?(.*)\}$/
/** Vista previa del resultado agrupado (solo estructura, para mostrar en la página) */
const aggregatedOutputPreviewJson = computed(() => {
  const mapping = importWorkflowOutputMapping.value
  const steps = importWorkflowActionSteps.value
  const obj = {}
  for (const [key, placeholder] of Object.entries(mapping)) {
    const v = typeof placeholder === 'string' ? placeholder.trim() : ''
    const m = v.match(OUTPUT_MAPPING_PLACEHOLDER_REGEX)
    if (m) {
      const step = steps.find(s => s.id === m[1])
      const label = step ? (step.summary || step.operationId || m[1]) : m[1]
      obj[key] = m[2] ? `<${label}: ${m[2]}>` : `<output: ${label}>`
    } else {
      obj[key] = '?'
    }
  }
  return Object.keys(obj).length ? JSON.stringify(obj, null, 2) : '{}'
})

function handleSaveAggregatedOutput(mapping) {
  try {
    const def = JSON.parse(importWorkflowJson.value || '{}')
    def.outputMapping = mapping ?? undefined
    importWorkflowJson.value = JSON.stringify(def, null, 2)
  } catch {
    /* ignore */
  }
}

/** Si el resultado del tool es agrupado (varios pasos) */
const hasAggregatedOutput = computed(() => Object.keys(importWorkflowOutputMapping.value).length > 0)

/** Paso marcado como resultado del tool en el workflow de creación */
const outputStepFromWorkflow = computed(() => {
  try {
    const w = JSON.parse(importWorkflowJson.value || '{}')
    const ids = Array.isArray(w.outputStepIds) ? w.outputStepIds : []
    const firstId = ids[0]
    if (!firstId) return null
    const step = (w.steps || []).find(s => s.id === firstId && s.type === 'action')
    return step || null
  } catch {
    return null
  }
})

function handleSetOutputStep(stepId) {
  try {
    const def = JSON.parse(importWorkflowJson.value || '{}')
    def.outputStepIds = [stepId]
    importWorkflowJson.value = JSON.stringify(def, null, 2)
  } catch {
    /* ignore */
  }
}

function openEditOutputStepForDto() {
  const step = outputStepFromWorkflow.value
  if (!step) return
  editActionStep.value = { stepId: step.id, step }
  addActionSourceNodeId.value = null
  addActionBranch.value = null
  editConditionalStep.value = null
  showAddActionModal.value = true
}

const importFormValid = computed(() => {
  const name = (importToolForm.value.name ?? '').trim()
  const title = (importToolForm.value.title ?? '').trim()
  if (!name || !nameRegex.test(name) || !title) return false
  try {
    const w = JSON.parse(importWorkflowJson.value || '{}')
    return Array.isArray(w.steps) && w.steps.length > 0
  } catch {
    return false
  }
})

const nameRegex = /^[a-zA-Z0-9_]+$/
const isNameValid = computed(() => {
  const name = addToolForm.value.name?.trim() ?? ''
  return name.length > 0 && nameRegex.test(name)
})

const isTitleValid = computed(() => {
  const title = addToolForm.value.title?.trim() ?? ''
  return title.length > 0
})

const manualFormValid = computed(() => isNameValid.value && isTitleValid.value)

const toolHasIntegrations = computed(() => (addToolForm.value.integrationIds ?? []).length > 0)

/** Preview del tool definition estándar MCP (name, description, inputSchema) */
const toolDefinitionPreview = computed(() => {
  const name = addToolForm.value.name?.trim() ?? ''
  const title = addToolForm.value.title?.trim() ?? ''
  const description = addToolForm.value.description?.trim() ?? ''
  const fields = inputSchemaFields.value.filter(f => f.name?.trim())
  const properties = {}
  const required = []
  for (const f of fields) {
    const n = f.name.trim()
    if (!n) continue
    properties[n] = { type: f.type || 'string' }
    const defVal = f.defaultValue?.trim()
    if (defVal !== '') {
      if (f.type === 'number' || f.type === 'integer') properties[n].default = Number(defVal)
      else if (f.type === 'boolean') properties[n].default = defVal === 'true'
      else properties[n].default = defVal
    }
    if (f.required) required.push(n)
  }
  const inputSchema = fields.length > 0 ? { type: 'object', properties, required } : { type: 'object', properties: {}, required: [] }
  const def = { name, description: description || undefined }
  if (title) def.title = title
  def.inputSchema = inputSchema
  return def
})

const toolDefinitionPreviewJson = computed(() => JSON.stringify(toolDefinitionPreview.value, null, 2))

/** Preview del tool definition para el flujo de creación por integración */
const importToolDefinitionPreview = computed(() => {
  const name = (importToolForm.value.name ?? '').trim()
  const title = (importToolForm.value.title ?? '').trim()
  const description = (importToolForm.value.description ?? '').trim()
  let inputSchema = { type: 'object', properties: {}, required: [] }
  try {
    const parsed = JSON.parse(importToolInputSchema.value || '{}')
    if (parsed?.properties && Object.keys(parsed.properties).length > 0) {
      inputSchema = { type: 'object', properties: parsed.properties, required: parsed.required || [] }
    }
  } catch {
    /* ignore */
  }
  const def = { name: name || undefined, description: description || undefined }
  if (title) def.title = title
  def.inputSchema = inputSchema
  try {
    const w = JSON.parse(importWorkflowJson.value || '{}')
    if (w?.steps?.length) def.executionConfig = { workflowDefinition: w }
  } catch {
    /* ignore */
  }
  return def
})

const importToolDefinitionPreviewJson = computed(() => JSON.stringify(importToolDefinitionPreview.value, null, 2))

function selectMode(mode) {
  toolMode.value = mode
  nameTouched.value = false
  titleTouched.value = false
  if (mode === 'flow') {
    importToolForm.value = { name: '', title: '', description: '' }
    importWorkflowJson.value = '{}'
    importToolInputSchema.value = '{}'
  } else if (mode === 'basic') {
    basicToolForm.value = { name: '', title: '', description: '' }
    basicSelectedAction.value = null
    basicInputSchemaFields.value = []
    batchTools.value = []
    showEditBatchSlider.value = false
    editBatchIndex.value = null
  } else if (mode === 'templates') {
    // Pantalla dedicada en paso 2; no tocar formularios de básico/flujo.
  } else {
    addToolForm.value = { integrationIds: [], name: '', title: '', description: '', executionConfig: '' }
    inputSchemaFields.value = []
  }
}

function goToStep2() {
  if (!toolMode.value) return
  if (atLimitTools.value) {
    toast.error(toolsLimitMessage.value)
    return
  }
  currentStep.value = 2
  if (toolMode.value === 'templates') {
    nextTick(() => ensureUseCaseTemplateIntegration())
  }
}

async function loadMcp() {
  loading.value = true
  error.value = null
  try {
    if (isEdit.value) {
      const tool = await apiService.getMcpTool(mcpId.value, toolId.value)
      if (!tool) {
        error.value = 'Tool no encontrado'
        return
      }
      addToolForm.value = {
        integrationIds: (tool.integrations ?? []).map(i => i.id),
        name: tool.name,
        title: tool.title ?? '',
        description: tool.description ?? '',
        executionConfig: tool.executionConfig ?? '',
        workflowDefinitionJson: tool.workflowDefinitionJson ?? '{}',
        isEnabled: tool.isEnabled !== false
      }
      const isFlow = tool.toolType === 'Workflow' || (tool.toolType == null && isFlowTool(tool.workflowDefinitionJson ?? '{}'))
      toolMode.value = isFlow ? 'flow' : 'manual'
      currentStep.value = 2
      if (!isFlow) {
        try {
          const w = JSON.parse(tool.workflowDefinitionJson || '{}')
          const step = (w.steps || [])[0]
          if (step) {
            const onFail = step.onFail
            manualStepOnFailBehavior.value = onFail?.behavior === 'continue' ? 'continue' : 'stop'
            manualStepOnFailRecordFailure.value = !!onFail?.recordFailure
            manualStepOnSuccessLog.value = !!step.onSuccess?.log
          } else {
            manualStepOnFailBehavior.value = 'stop'
            manualStepOnFailRecordFailure.value = false
            manualStepOnSuccessLog.value = false
          }
        } catch {
          manualStepOnFailBehavior.value = 'stop'
          manualStepOnFailRecordFailure.value = false
          manualStepOnSuccessLog.value = false
        }
      }
      if (isFlow) {
        importWorkflowJson.value = tool.workflowDefinitionJson ?? '{}'
        importToolForm.value = {
          name: tool.name ?? '',
          title: tool.title ?? '',
          description: tool.description ?? ''
        }
      }
      // Always set so the Execute slider has the tool inputSchema in edit mode (flow and basic).
      importToolInputSchema.value = tool.inputSchema ?? '{}'
      try {
        const schema = JSON.parse(tool.inputSchema || '{}')
        const props = schema.properties ?? {}
        const required = new Set(schema.required ?? [])
        inputSchemaFields.value = Object.entries(props).map(([name, p]) => ({
          name,
          type: p?.type || 'string',
          required: required.has(name),
          defaultValue: p?.default !== undefined ? String(p.default) : '',
          fromAction: !!(p && p._actionParam),
          actionParam: p?._actionParam ?? name
        }))
        if (inputSchemaFields.value.length === 0) inputSchemaFields.value.push({ name: '', type: 'string', required: false, defaultValue: '' })
      } catch {
        inputSchemaFields.value = [{ name: '', type: 'string', required: false, defaultValue: '' }]
      }
      editToolOutputSchemaFull.value = tool.outputSchema || null
      const outParsed = parseOutputSchemaWithFlags(tool.outputSchema)
      const hasOutputFlags = outParsed.paths.length > 0 && (() => {
        try {
          const raw = typeof tool.outputSchema === 'string' ? tool.outputSchema : JSON.stringify(tool.outputSchema || {})
          return raw.includes('"_enabled"')
        } catch { return false }
      })()
      if (hasOutputFlags && outParsed.paths.length > 0) {
        editToolOutputAvailablePaths.value = outParsed.paths
        editToolSelectedOutputPaths.value = [...outParsed.enabled]
      } else {
        const outPaths = extractPathsFromSchema(tool.outputSchema || '{}')
        editToolOutputAvailablePaths.value = outPaths
        editToolSelectedOutputPaths.value = outPaths.length > 0 ? [...outPaths] : []
        if (outPaths.length === 0) {
          editToolManualOutputPaths.value = [{ path: '', type: 'string', itemType: 'string' }]
        }
      }
      editToolOutputPreviewInline.value = false
      editToolOutputPropsExpanded.value = false
      editToolOutputPreviewFullSchema.value = true
    } else {
      mcp.value = await apiService.getMcpById(mcpId.value)
    }
  } catch (e) {
    if (e.response?.status === 404) error.value = 'Tool no encontrado'
    else error.value = getApiErrorMessage(e) || 'Error al cargar'
  } finally {
    loading.value = false
  }
}

/** Parsea JSON schema a lista de campos (para Creación Básica). Preserva _actionParam para mapping from→to. */
function parseSchemaToFieldsForBasic(json) {
  try {
    const schema = typeof json === 'string' ? JSON.parse(json || '{}') : json
    const propsObj = schema.properties || {}
    const requiredSet = new Set(schema.required || [])
    const result = []
    for (const [name, prop] of Object.entries(propsObj)) {
      result.push({
        name,
        type: prop?.type || 'string',
        required: requiredSet.has(name),
        defaultValue: prop?.default !== undefined ? (typeof prop.default === 'object' ? JSON.stringify(prop.default) : String(prop.default)) : '',
        actionParam: prop?._actionParam ?? name
      })
    }
    return result.length > 0 ? result : [{ name: '', type: 'string', required: false, defaultValue: '', actionParam: '' }]
  } catch {
    return [{ name: '', type: 'string', required: false, defaultValue: '', actionParam: '' }]
  }
}

/** Construye inputKeyMapping (to → from) desde inputSchema: cuando el usuario renombra una propiedad, _actionParam es el nombre de la acción. */
function buildInputKeyMappingFromSchema(inputSchemaJson) {
  try {
    const schema = typeof inputSchemaJson === 'string' ? JSON.parse(inputSchemaJson || '{}') : inputSchemaJson
    const props = schema.properties || {}
    const mapping = {}
    for (const [fromKey, prop] of Object.entries(props)) {
      const toKey = prop?._actionParam ?? fromKey
      if (toKey && toKey !== fromKey) mapping[toKey] = fromKey
    }
    return mapping
  } catch {
    return {}
  }
}

/** True si el action no tiene mapeo personalizado: inputMapping vacío o todos los valores son placeholders {input.xxx}. */
function actionHasNoCustomMapping(actionData) {
  const m = actionData?.inputMapping || {}
  for (const [key, value] of Object.entries(m)) {
    if (key === 'body') {
      if (!value || value === '{}') continue
      try {
        const obj = JSON.parse(value)
        for (const v of Object.values(obj)) {
          const s = typeof v === 'string' ? v.trim() : ''
          if (s && !/^\{input\.[^}]+\}$/.test(s)) return false
        }
      } catch {
        return false
      }
    } else {
      const s = typeof value === 'string' ? value.trim() : ''
      if (s && !/^\{input\.[^}]+\}$/.test(s)) return false
    }
  }
  return true
}

/** Fusiona las propiedades del inputSchema de la operación en el schema del nodo start. Solo añade propiedades nuevas; no borra ni pisa las existentes. */
function mergeOperationInputSchemaIntoStart(currentStartSchemaJson, operationInputSchemaJson) {
  if (!operationInputSchemaJson || operationInputSchemaJson === '{}') return currentStartSchemaJson || '{}'
  let current
  let operation
  try {
    current = typeof currentStartSchemaJson === 'string' ? JSON.parse(currentStartSchemaJson || '{}') : (currentStartSchemaJson || {})
  } catch {
    return currentStartSchemaJson || '{}'
  }
  try {
    operation = typeof operationInputSchemaJson === 'string' ? JSON.parse(operationInputSchemaJson || '{}') : (operationInputSchemaJson || {})
  } catch {
    return currentStartSchemaJson || '{}'
  }
  const currentProps = current.properties || {}
  const operationProps = operation.properties || {}
  const currentRequired = new Set(current.required || [])
  const operationRequired = new Set(operation.required || [])
  const mergedProps = { ...currentProps }
  for (const [name, prop] of Object.entries(operationProps)) {
    if (!(name in mergedProps)) mergedProps[name] = prop
    else if (operationRequired.has(name)) currentRequired.add(name)
  }
  const mergedRequired = [...currentRequired]
  for (const r of operationRequired) {
    if (!currentRequired.has(r) && (r in mergedProps)) mergedRequired.push(r)
  }
  const merged = { type: 'object', properties: mergedProps }
  if (mergedRequired.length > 0) merged.required = mergedRequired
  return JSON.stringify(merged)
}

function slugFromSummary(summary) {
  if (!summary || typeof summary !== 'string') return 'tool'
  return summary
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^a-z0-9_]/g, '')
    .slice(0, 50) || 'tool'
}

function openAddActionForBasic() {
  addActionSourceNodeId.value = null
  addActionBranch.value = null
  editActionStep.value = null
  showAddActionModal.value = true
}

function onAddActionFromModal(payload) {
  const list = Array.isArray(payload) ? payload : [payload]
  if (toolMode.value === 'basic') {
    for (const actionData of list) {
      const inputSchema = actionData.inputSchema || '{}'
      const stepId = 'step1'
      const step = {
        id: stepId,
        type: 'action',
        integrationId: actionData.integrationId,
        method: actionData.method,
        path: actionData.path,
        operationId: actionData.operationId,
        summary: actionData.summary,
        inputMapping: actionData.inputMapping || {},
        inputKeyMapping: buildInputKeyMappingFromSchema(inputSchema),
        inputSchema,
        nextStepId: null
      }
      const def = {
        steps: [step],
        entryStepId: stepId,
        outputStepIds: [stepId]
      }
      let outputSchema = actionData.outputSchema || undefined
      if (actionData.outputSchemaFull && (actionData.outputSchemaAvailablePaths?.length ?? 0) > 0) {
        const selectedPaths = extractPathsFromSchema(actionData.outputSchema || '{}')
        const withFlags = buildOutputSchemaWithSelectionFlags(actionData.outputSchemaFull, selectedPaths)
        if (withFlags) outputSchema = withFlags
      }
      batchTools.value.push({
        id: crypto.randomUUID(),
        name: slugFromSummary(actionData.summary),
        title: actionData.summary || 'Tool',
        description: actionData.summary || '',
        inputSchema,
        outputSchema,
        outputSchemaAvailablePaths: actionData.outputSchemaAvailablePaths || [],
        outputSchemaFull: actionData.outputSchemaFull || undefined,
        workflowDefinitionJson: JSON.stringify(def, null, 2),
        integrationIds: actionData.integrationId ? [actionData.integrationId] : []
      })
    }
    showAddActionModal.value = false
  } else {
    for (let i = 0; i < list.length; i++) {
      handleAddActionFromModal(list[i])
      if (i < list.length - 1) {
        let def
        try {
          def = JSON.parse(importWorkflowJson.value || '{}')
        } catch {
          def = { steps: [] }
        }
        const lastStep = def.steps?.[def.steps.length - 1]
        if (lastStep) addActionSourceNodeId.value = lastStep.id
      }
    }
    showAddActionModal.value = false
  }
}

const basicFormValid = computed(() => {
  const name = (basicToolForm.value.name ?? '').trim()
  const title = (basicToolForm.value.title ?? '').trim()
  return !!basicSelectedAction.value && name.length > 0 && nameRegex.test(name) && title.length > 0
})

const basicBatchFormValid = computed(() => {
  if (batchTools.value.length === 0) return false
  const allValid = batchTools.value.every(
    t => nameRegex.test((t.name || '').trim()) && (t.title || '').trim().length > 0
  )
  return allValid && batchToolDuplicateNames.value.size === 0
})

/** Nombres de tools que están duplicados en el batch (para marcar con borde rojo) */
const batchToolDuplicateNames = computed(() => {
  const names = batchTools.value.map(t => (t.name || '').trim()).filter(Boolean)
  const count = {}
  for (const n of names) count[n] = (count[n] || 0) + 1
  return new Set(Object.keys(count).filter(n => count[n] > 1))
})

/** Normaliza operationId quitando sufijo _N (ej. CreateCoupon_2 → CreateCoupon) para detectar duplicados. */
function normalizeOperationIdForDedup(opId) {
  if (!opId || typeof opId !== 'string') return opId || ''
  return opId.replace(/_\d+$/, '')
}

/** Clave para detectar acción duplicada en un ítem del batch (misma integración + operación). */
function batchItemActionKey(item) {
  if (!item?.workflowDefinitionJson) return null
  try {
    const w = JSON.parse(item.workflowDefinitionJson || '{}')
    const step = (w.steps || [])[0]
    if (!step || step.type !== 'action') return null
    const intId = step.integrationId ?? ''
    const opId = step.operationId ?? ''
    if (opId) return `${intId}|${normalizeOperationIdForDedup(opId)}`
    const method = (step.method || 'GET').toString().toUpperCase()
    const path = (step.path || '').toString().replace(/^\/+/, '')
    return `${intId}|${method}|${path}`
  } catch {
    return null
  }
}

/** True si en el batch hay al menos dos ítems con la misma acción (integración+operación). */
const hasDuplicateActionsInBatch = computed(() => {
  const keys = batchTools.value.map(batchItemActionKey).filter(Boolean)
  const count = {}
  for (const k of keys) count[k] = (count[k] || 0) + 1
  return Object.values(count).some(c => c > 1)
})

/** Elimina del batch los ítems duplicados por acción; deja solo el primero de cada grupo. */
function removeDuplicateActionsFromBatch() {
  console.log('[removeDuplicateActionsFromBatch] batchTools count:', batchTools.value.length)
  const seen = new Map()
  const toRemove = []
  for (let i = 0; i < batchTools.value.length; i++) {
    const k = batchItemActionKey(batchTools.value[i])
    console.log('[removeDuplicateActionsFromBatch] item', i, 'key:', k)
    if (!k) continue
    if (seen.has(k)) toRemove.push(i)
    else seen.set(k, i)
  }
  console.log('[removeDuplicateActionsFromBatch] toRemove indices:', toRemove)
  const removedSet = new Set(toRemove)
  if (editBatchIndex.value != null && removedSet.has(editBatchIndex.value)) {
    showEditBatchSlider.value = false
    editBatchIndex.value = null
  }
  for (let j = toRemove.length - 1; j >= 0; j--) batchTools.value.splice(toRemove[j], 1)
  console.log('[removeDuplicateActionsFromBatch] after remove, batchTools count:', batchTools.value.length)
}

/** Output schema del ítem en edición (formateado para preview cuando no hay paths seleccionables) */
const editBatchOutputSchemaPreview = computed(() => {
  const idx = editBatchIndex.value
  if (idx == null || idx < 0 || idx >= batchTools.value.length) return null
  const raw = batchTools.value[idx]?.outputSchema
  if (raw == null || raw === '') return null
  if (typeof raw === 'string') {
    try {
      const parsed = JSON.parse(raw)
      return JSON.stringify(parsed, null, 2)
    } catch {
      return raw
    }
  }
  if (typeof raw === 'object') {
    try {
      return JSON.stringify(raw, null, 2)
    } catch {
      return null
    }
  }
  return null
})

const editBatchFullOutputSchemaRaw = computed(() => {
  const idx = editBatchIndex.value
  if (idx == null || idx < 0 || idx >= batchTools.value.length) return null
  return batchTools.value[idx]?.outputSchema ?? null
})

const editBatchOutputSchemaTree = computed(() =>
  buildOutputSchemaTreeFromResponseSchema(editBatchFullOutputSchemaRaw.value)
)

/** Lista plana solo si no hay árbol (schema sin `properties` anidables en raíz). */
const editBatchOutputPathsFlatOnly = computed(() =>
  editBatchOutputSchemaTree.value.length > 0 ? [] : editBatchOutputAvailablePaths.value
)

const editBatchOutputSelectionPreviewText = computed(() =>
  computeOutputSelectionPreviewText({
    fullSchema: editBatchFullOutputSchemaRaw.value,
    selectedPaths: selectedEditOutputPaths.value,
    manualItems: [],
    previewFullSchema: editBatchOutputPreviewFullSchema.value,
    hasSelectablePaths:
      editBatchOutputSchemaTree.value.length > 0 || editBatchOutputAvailablePaths.value.length > 0,
    emptyMessage: 'Sin output schema definido o sin propiedades seleccionables.'
  })
)

const editToolEffectiveManualOutputItems = computed(() =>
  (editToolManualOutputPaths.value || [])
    .filter(m => (m.path || '').trim())
    .map(m => ({
      path: m.path.trim(),
      type: m.type || 'string',
      itemType: m.type === 'array' ? (m.itemType || 'string') : undefined
    }))
)

const editToolOutputSchemaTree = computed(() =>
  buildOutputSchemaTreeFromResponseSchema(editToolOutputSchemaFull.value)
)

const editToolOutputPathsFlatOnly = computed(() =>
  editToolOutputSchemaTree.value.length > 0 ? [] : editToolOutputAvailablePaths.value
)

const editToolOutputSelectionPreviewText = computed(() =>
  computeOutputSelectionPreviewText({
    fullSchema: editToolOutputSchemaFull.value,
    selectedPaths: editToolSelectedOutputPaths.value,
    manualItems: editToolEffectiveManualOutputItems.value,
    previewFullSchema: editToolOutputPreviewFullSchema.value,
    hasSelectablePaths:
      editToolOutputSchemaTree.value.length > 0 || editToolOutputAvailablePaths.value.length > 0,
    emptyMessage: 'Añade propiedades manualmente o espera un schema de respuesta con propiedades.'
  })
)

/** Extrae rutas aplanadas de un JSON Schema (acepta string o objeto). Expande arrays de objetos (ej. list → list.orderId, list.creationDate). */
function extractPathsFromSchema(schema) {
  if (schema == null) return []
  try {
    const s = typeof schema === 'string' ? JSON.parse(schema || '{}') : schema
    if (!s || typeof s !== 'object') return []
    const props = s.properties || {}
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

/**
 * Builds full output schema with _enabled: true|false on each leaf property.
 * Used so we persist all outputs and can re-open edit to enable/disable more.
 */
function buildOutputSchemaWithSelectionFlags(fullSchema, selectedPaths) {
  const schema = typeof fullSchema === 'string' ? (() => { try { return JSON.parse(fullSchema) } catch { return {} } })() : (fullSchema || {})
  const props = schema.properties || {}
  if (Object.keys(props).length === 0) return null
  const selectedSet = new Set((selectedPaths || []).filter(Boolean))

  function visit(prefix, obj) {
    if (!obj || typeof obj !== 'object') return obj
    const nextProps = obj.properties
    if (!nextProps || typeof nextProps !== 'object') return { ...obj, _enabled: selectedSet.has(prefix) }
    const next = {}
    for (const [name, prop] of Object.entries(nextProps)) {
      const path = prefix ? `${prefix}.${name}` : name
      const type = prop?.type || 'string'
      const nested = prop?.properties
      if (type === 'object' && nested && Object.keys(nested).length > 0) {
        next[name] = visit(path, prop)
      } else if (type === 'array' && prop?.items?.properties && Object.keys(prop.items.properties).length > 0) {
        next[name] = { ...prop, items: visit(path, prop.items) }
      } else {
        next[name] = { ...prop, _enabled: selectedSet.has(path) }
      }
    }
    return { ...obj, properties: next }
  }

  const root = visit('', schema)
  return root?.properties && Object.keys(root.properties).length > 0 ? JSON.stringify(root) : null
}

/**
 * Parses output schema that may have _enabled on each property.
 * Returns all paths and the set of enabled path names (for edit UI).
 */
function parseOutputSchemaWithFlags(schemaJson) {
  if (!schemaJson) return { paths: [], enabled: new Set() }
  try {
    const schema = typeof schemaJson === 'string' ? JSON.parse(schemaJson || '{}') : schemaJson
    const paths = []
    const enabled = new Set()

    function visit(prefix, obj) {
      const props = obj?.properties
      if (!props || typeof props !== 'object') return
      for (const [name, prop] of Object.entries(props)) {
        const path = prefix ? `${prefix}.${name}` : name
        const type = prop?.type || 'string'
        const nested = prop?.properties
        if (type === 'object' && nested && Object.keys(nested).length > 0) {
          visit(path, prop)
        } else if (type === 'array' && prop?.items?.properties && Object.keys(prop.items.properties).length > 0) {
          visit(path, prop.items)
        } else {
          paths.push(path)
          if (prop != null && typeof prop === 'object' && '_enabled' in prop) {
            if (prop._enabled === true) enabled.add(path)
          } else {
            enabled.add(path)
          }
        }
      }
    }
    visit('', schema)
    return { paths, enabled }
  } catch {
    return { paths: [], enabled: new Set() }
  }
}

function buildBasicInputSchemaJson() {
  const fields = basicInputSchemaFields.value.filter(f => (f.name || '').trim())
  if (fields.length === 0) return '{}'
  const properties = {}
  const required = []
  for (const f of fields) {
    const name = f.name.trim()
    if (!name) continue
    properties[name] = { type: f.type || 'string', _actionParam: f.actionParam ?? name }
    const defVal = (f.defaultValue || '').trim()
    if (defVal !== '') {
      if (f.type === 'number' || f.type === 'integer') properties[name].default = Number(defVal)
      else if (f.type === 'boolean') properties[name].default = defVal === 'true'
      else properties[name].default = defVal
    }
    if (f.required) required.push(name)
  }
  return JSON.stringify({ type: 'object', properties, required }, null, 0)
}

const basicToolDefinitionPreview = computed(() => {
  const name = (basicToolForm.value.name ?? '').trim()
  const title = (basicToolForm.value.title ?? '').trim()
  const description = (basicToolForm.value.description ?? '').trim()
  let inputSchema = { type: 'object', properties: {}, required: [] }
  try {
    const parsed = JSON.parse(buildBasicInputSchemaJson())
    if (parsed?.properties && Object.keys(parsed.properties).length > 0) {
      inputSchema = { type: 'object', properties: parsed.properties, required: parsed.required || [] }
    }
  } catch {
    /* ignore */
  }
  return { name: name || undefined, title: title || undefined, description: description || undefined, inputSchema }
})

const basicToolDefinitionPreviewJson = computed(() => JSON.stringify(basicToolDefinitionPreview.value, null, 2))

function addBasicInputSchemaField() {
  basicInputSchemaFields.value.push({ name: '', type: 'string', required: false, defaultValue: '' })
}

function removeBasicInputSchemaField(idx) {
  const field = basicInputSchemaFields.value[idx]
  if (field?.required) return
  basicInputSchemaFields.value.splice(idx, 1)
}

function openEditBatchTool(idx) {
  const item = batchTools.value[idx]
  if (!item) return
  editBatchIndex.value = idx
  const fields = parseSchemaToFieldsForBasic(item.inputSchema || '{}')
  editBatchForm.value = {
    name: item.name || '',
    title: item.title || '',
    description: item.description || '',
    schemaFields: fields.map(f => ({ ...f, fromAction: !!(f.actionParam && f.actionParam !== f.name) }))
  }
  const parsed = parseOutputSchemaWithFlags(item.outputSchema)
  const hasFlags = parsed.paths.length > 0 && (() => {
    try {
      const raw = typeof item.outputSchema === 'string' ? item.outputSchema : JSON.stringify(item.outputSchema || {})
      return raw.includes('"_enabled"')
    } catch { return false }
  })()
  if (hasFlags && parsed.paths.length > 0) {
    editBatchOutputAvailablePaths.value = parsed.paths
    selectedEditOutputPaths.value = [...parsed.enabled]
  } else {
    const available = (item.outputSchemaAvailablePaths && item.outputSchemaAvailablePaths.length > 0)
      ? item.outputSchemaAvailablePaths
      : extractPathsFromSchema(item.outputSchema || '{}')
    editBatchOutputAvailablePaths.value = available
    const currentSelected = extractPathsFromSchema(item.outputSchema || '{}')
    selectedEditOutputPaths.value = currentSelected.length > 0 ? currentSelected : (available.length > 0 ? [...available] : [])
  }
  showEditBatchSlider.value = true
}

function closeEditBatchSlider() {
  showEditBatchSlider.value = false
  editBatchIndex.value = null
  editBatchOutputAvailablePaths.value = []
  selectedEditOutputPaths.value = []
  editBatchOutputPreviewInline.value = false
  editBatchOutputPropsExpanded.value = false
  editBatchOutputPreviewFullSchema.value = true
}

function buildEditBatchInputSchemaJson() {
  const fields = editBatchForm.value.schemaFields.filter(f => (f.name || '').trim())
  if (fields.length === 0) return '{}'
  const properties = {}
  const required = []
  for (const f of fields) {
    const name = f.name.trim()
    if (!name) continue
    properties[name] = { type: f.type || 'string', _actionParam: f.actionParam ?? name }
    const defVal = (f.defaultValue || '').trim()
    if (defVal !== '') {
      if (f.type === 'number' || f.type === 'integer') properties[name].default = Number(defVal)
      else if (f.type === 'boolean') properties[name].default = defVal === 'true'
      else properties[name].default = defVal
    }
    if (f.required) required.push(name)
  }
  return JSON.stringify({ type: 'object', properties, required }, null, 0)
}

function saveEditBatchTool() {
  const idx = editBatchIndex.value
  if (idx == null || idx < 0 || idx >= batchTools.value.length) return
  const name = (editBatchForm.value.name || '').trim()
  const title = (editBatchForm.value.title || '').trim()
  if (!name || !nameRegex.test(name) || !title) return
  const inputSchema = buildEditBatchInputSchemaJson()
  const item = batchTools.value[idx]
  const fullSchema = item.outputSchemaFull || item.outputSchema || '{}'
  const withFlags = buildOutputSchemaWithSelectionFlags(fullSchema, selectedEditOutputPaths.value)
  const outputSchema = withFlags || (selectedEditOutputPaths.value.length > 0
    ? buildOutputSchemaFromSelection(fullSchema, selectedEditOutputPaths.value)
    : null)
  batchTools.value[idx] = {
    ...item,
    name,
    title: title,
    description: (editBatchForm.value.description || '').trim() || '',
    inputSchema,
    outputSchema: outputSchema || undefined
  }
  closeEditBatchSlider()
}

function removeBatchTool(idx) {
  batchTools.value.splice(idx, 1)
}

async function handleCreateBasicToolBatch() {
  if (!basicBatchFormValid.value || batchTools.value.length === 0) return
  const limit = mcpToolsLimit.value
  if (limit != null && (toolsUsedCount.value + batchTools.value.length) > limit) {
    toast.error(getLimitMessageForMcpTools(toolsUsedCount.value + batchTools.value.length))
    return
  }
  basicToolLoading.value = true
  try {
    const payload = batchTools.value.map(t => ({
      name: (t.name || '').trim(),
      title: (t.title || '').trim(),
      description: (t.description || '').trim() || undefined,
      inputSchema: t.inputSchema ?? '{}',
      outputSchema: t.outputSchema || undefined,
      workflowDefinitionJson: normalizeWorkflowDefinitionForSubmit(t.workflowDefinitionJson),
      integrationIds: t.integrationIds || []
    }))
    await apiService.addMcpToolsBatch(mcpId.value, payload)
    toast.success('Tools creados')
    router.push(backUrl.value)
  } catch (e) {
    toast.error(getApiErrorMessage(e) || 'Error al crear')
  } finally {
    basicToolLoading.value = false
  }
}

async function handleCreateBasicTool() {
  if (!basicFormValid.value || !basicSelectedAction.value) return
  basicToolLoading.value = true
  try {
    const stepId = 'step1'
    const basicSchemaJson = buildBasicInputSchemaJson()
    const step = {
      id: stepId,
      type: 'action',
      integrationId: basicSelectedAction.value.integrationId,
      method: basicSelectedAction.value.method,
      path: basicSelectedAction.value.path,
      operationId: basicSelectedAction.value.operationId,
      summary: basicSelectedAction.value.summary,
      inputMapping: basicSelectedAction.value.inputMapping || {},
      inputKeyMapping: buildInputKeyMappingFromSchema(basicSchemaJson),
      inputSchema: basicSchemaJson,
      nextStepId: null
    }
    const def = {
      steps: [step],
      entryStepId: stepId,
      outputStepIds: [stepId]
    }
    const integrationIds = basicSelectedAction.value.integrationId ? [basicSelectedAction.value.integrationId] : undefined
    await apiService.addMcpTool(mcpId.value, {
      name: basicToolForm.value.name.trim(),
      title: basicToolForm.value.title.trim(),
      description: (basicToolForm.value.description || '').trim() || undefined,
      inputSchema: buildBasicInputSchemaJson(),
      integrationIds,
      workflowDefinitionJson: normalizeWorkflowDefinitionForSubmit(def),
      toolType: 'Manual'
    })
    toast.success('Tool creado')
    router.push(backUrl.value)
  } catch (e) {
    toast.error(getApiErrorMessage(e) || 'Error al crear')
  } finally {
    basicToolLoading.value = false
  }
}

function buildInputSchemaJson() {
  const fields = inputSchemaFields.value.filter(f => f.name?.trim())
  if (fields.length === 0) return '{}'
  const properties = {}
  const required = []
  for (const f of fields) {
    const name = f.name.trim()
    if (!name) continue
    properties[name] = { type: f.type || 'string' }
    const defVal = f.defaultValue?.trim()
    if (defVal !== '') {
      if (f.type === 'number' || f.type === 'integer') properties[name].default = Number(defVal)
      else if (f.type === 'boolean') properties[name].default = defVal === 'true'
      else properties[name].default = defVal
    }
    if (f.fromAction && (f.actionParam || name)) properties[name]._actionParam = f.actionParam || name
    if (f.required) required.push(name)
  }
  return JSON.stringify({ type: 'object', properties, required }, null, 0)
}

function getNextStepId() {
  let def
  try {
    def = JSON.parse(importWorkflowJson.value || '{}')
  } catch {
    return 'step1'
  }
  const steps = def.steps || []
  const ids = new Set(steps.map(s => s.id).filter(Boolean))
  let n = 1
  while (ids.has(`step${n}`)) n++
  return `step${n}`
}

function computeOutputStepIds(def) {
  const stepMap = new Map((def.steps || []).map(s => [s.id, s]))
  const getNextIds = (s) => {
    if (!s) return []
    if (s.type === 'conditional') return [s.branches?.success, s.branches?.failure].filter(Boolean)
    return s.nextStepId ? [s.nextStepId] : []
  }
  return (def.steps || []).filter(s => getNextIds(s).length === 0).map(s => s.id)
}

function actionKeyForDedup(step) {
  if (!step || step.type !== 'action') return null
  const intId = step.integrationId ?? ''
  const opId = step.operationId ?? ''
  if (opId) return `${intId}|${normalizeOperationIdForDedup(opId)}`
  const method = (step.method || 'GET').toString().toUpperCase()
  const path = (step.path || '').toString().replace(/^\/+/, '')
  return `${intId}|${method}|${path}`
}

function handleAddActionFromModal(actionData) {
  const stepId = getNextStepId()
  let def
  try {
    def = JSON.parse(importWorkflowJson.value || '{}')
  } catch {
    def = { steps: [], entryStepId: '', entryStepIds: [], outputStepIds: [] }
  }
  def.steps = def.steps || []
  def.entryStepIds = def.entryStepIds ?? (def.entryStepId ? [def.entryStepId] : [])
  const newStepKey = actionKeyForDedup({
    type: 'action',
    integrationId: actionData.integrationId,
    operationId: actionData.operationId,
    method: actionData.method,
    path: actionData.path
  })
  const sameOpCount = newStepKey ? def.steps.filter(s => actionKeyForDedup(s) === newStepKey).length : 0
  const baseLabel = (actionData.summary || 'Acción').trim() || 'Acción'
  const displayName = sameOpCount >= 1 ? `${baseLabel} (${sameOpCount + 1})` : baseLabel
  const step = {
    id: stepId,
    type: 'action',
    integrationId: actionData.integrationId,
    integrationLogoUrl: actionData.integrationLogoUrl ?? null,
    method: actionData.method,
    path: actionData.path,
    operationId: actionData.operationId,
    summary: actionData.summary,
    displayName,
    inputMapping: actionData.inputMapping || {},
    inputKeyMapping: buildInputKeyMappingFromSchema(actionData.inputSchema || '{}'),
    inputSchema: actionData.inputSchema || '{}',
    outputAvailablePaths: actionData.outputSchemaAvailablePaths || [],
    outputSchema: actionData.outputSchema || undefined,
    onFail: actionData.onFail ?? undefined,
    onSuccess: actionData.onSuccess ?? undefined,
    nextStepId: null
  }
  const sourceId = addActionSourceNodeId.value
  const branch = addActionBranch.value
  const isFromStart = sourceId === 'start'
  if (branch && sourceId && !isFromStart) {
    const condStep = def.steps.find(s => s.id === sourceId && s.type === 'conditional')
    if (condStep) {
      condStep.branches = condStep.branches || { success: '', failure: '' }
      condStep.branches[branch] = stepId
    }
  } else if (!isFromStart) {
    const sourceStep = sourceId ? def.steps.find(s => s.id === sourceId) : null
    const prevStep = sourceStep ?? def.steps[def.steps.length - 1]
    if (prevStep) {
      step.nextStepId = prevStep.nextStepId
      prevStep.nextStepId = stepId
    }
  }
  def.steps.push(step)
  if (isFromStart) {
    def.entryStepIds = [...def.entryStepIds, stepId]
    def.entryStepId = def.entryStepId || stepId
  } else if (!def.entryStepId) {
    def.entryStepId = def.steps[0]?.id ?? stepId
  }
  def.outputStepIds = computeOutputStepIds(def)
  importWorkflowJson.value = JSON.stringify(def, null, 2)
  addActionSourceNodeId.value = null
  addActionBranch.value = null
  editActionStep.value = null
  if (actionData.inputSchema && actionHasNoCustomMapping(actionData)) {
    if (def.steps.length === 1) {
      importToolInputSchema.value = actionData.inputSchema
    } else {
      importToolInputSchema.value = mergeOperationInputSchemaIntoStart(importToolInputSchema.value, actionData.inputSchema)
    }
  }
}

function handleUpdateActionFromModal({ stepId, actionData }) {
  let def
  try {
    def = JSON.parse(importWorkflowJson.value || '{}')
  } catch {
    def = { steps: [], entryStepId: '', outputStepIds: [] }
  }
  const step = def.steps?.find(s => s.id === stepId)
  if (!step) return
  step.integrationId = actionData.integrationId
  step.integrationLogoUrl = actionData.integrationLogoUrl ?? null
  step.method = actionData.method
  step.path = actionData.path
  step.operationId = actionData.operationId
  step.summary = actionData.summary
  step.inputMapping = actionData.inputMapping || {}
  step.inputKeyMapping = buildInputKeyMappingFromSchema(actionData.inputSchema || '{}')
  step.inputSchema = actionData.inputSchema || '{}'
  step.outputAvailablePaths = actionData.outputSchemaAvailablePaths || []
  step.outputSchema = actionData.outputSchema || undefined
  step.onFail = actionData.onFail ?? undefined
  step.onSuccess = actionData.onSuccess ?? undefined
  importWorkflowJson.value = JSON.stringify(def, null, 2)
  editActionStep.value = null
  if (actionData.inputSchema && actionHasNoCustomMapping(actionData)) {
    importToolInputSchema.value = mergeOperationInputSchemaIntoStart(importToolInputSchema.value, actionData.inputSchema)
  }
}

function handleSaveStartInput(json) {
  importToolInputSchema.value = json || '{}'
}

function handleSaveConditionalRules({ stepId, rulesJson, inputKey }) {
  let def
  try {
    def = JSON.parse(importWorkflowJson.value || '{}')
  } catch {
    def = { steps: [] }
  }
  const step = def.steps?.find(s => s.id === stepId)
  if (!step || step.type !== 'conditional') return
  step.rulesJson = rulesJson || '[]'
  step.inputKey = inputKey || ''
  importWorkflowJson.value = JSON.stringify(def, null, 2)
  editConditionalStep.value = null
}

/** Crear o actualizar tool desde el panel de flujo (canvas). */
async function handleSaveFlowTool() {
  if (!importFormValid.value) return
  if (!isEdit.value && atLimitTools.value) {
    toast.error(toolsLimitMessage.value)
    return
  }
  if (atLimitWorkflowTools.value && workflowLimitMessage.value) {
    toast.error(workflowLimitMessage.value)
    return
  }
  importToolLoading.value = true
  try {
    let def
    try {
      def = JSON.parse(importWorkflowJson.value || '{}')
    } catch {
      def = { steps: [] }
    }
    const integrationIds = [...new Set((def.steps || []).map(s => s.integrationId).filter(Boolean))]
    const inputSchema = importToolInputSchema.value || '{}'
    const hasAggregatedOutput = Object.keys(importWorkflowOutputMapping.value).length > 0
    const outStep = outputStepFromWorkflow.value
    const outputSchema = hasAggregatedOutput
      ? undefined
      : (outStep?.outputSchema
          ? (typeof outStep.outputSchema === 'string' ? outStep.outputSchema : JSON.stringify(outStep.outputSchema))
          : undefined)
    const payload = {
      name: importToolForm.value.name.trim(),
      title: importToolForm.value.title.trim(),
      description: importToolForm.value.description?.trim() || undefined,
      inputSchema,
      outputSchema,
      workflowDefinitionJson: normalizeWorkflowDefinitionForSubmit(importWorkflowJson.value || '{}'),
      toolType: 'Workflow'
    }
    if (isEdit.value) {
      await apiService.updateMcpTool(mcpId.value, toolId.value, {
        ...payload,
        isEnabled: addToolForm.value.isEnabled
      })
      toast.success('Tool actualizado')
    } else {
      const result = await apiService.addMcpTool(mcpId.value, {
        ...payload,
        integrationIds: integrationIds.length > 0 ? integrationIds : undefined
      })
      toast.success('Tool creado')
      router.push(`/admin/mcps/${mcpId.value}/tools/${result.id}`)
    }
  } catch (e) {
    toast.error(getApiErrorMessage(e) || (isEdit.value ? 'Error al actualizar' : 'Error al crear'))
  } finally {
    importToolLoading.value = false
  }
}

async function handleAddTool() {
  if (toolMode.value === 'flow' && atLimitWorkflowTools.value && workflowLimitMessage.value) {
    toast.error(workflowLimitMessage.value)
    return
  }
  addToolLoading.value = true
  try {
    const payload = {
      name: addToolForm.value.name,
      title: addToolForm.value.title || undefined,
      description: addToolForm.value.description || undefined,
      inputSchema: buildInputSchemaJson()
    }
    if (isEdit.value) {
      payload.isEnabled = addToolForm.value.isEnabled
      let workflowJson = addToolForm.value.workflowDefinitionJson || '{}'
      if (toolMode.value === 'manual') {
        try {
          const w = JSON.parse(workflowJson)
          const step = (w.steps || [])[0]
          if (step) {
            step.onFail = {
              behavior: manualStepOnFailBehavior.value || 'stop',
              recordFailure: !!manualStepOnFailRecordFailure.value
            }
            step.onSuccess = { log: !!manualStepOnSuccessLog.value }
            workflowJson = JSON.stringify(w)
          }
        } catch {
          /* keep original */
        }
      }
      payload.workflowDefinitionJson = normalizeWorkflowDefinitionForSubmit(workflowJson)
      payload.toolType = toolMode.value === 'flow' ? 'Workflow' : 'Manual'
      if (editToolOutputAvailablePaths.value.length > 0 && editToolOutputSchemaFull.value) {
        const withFlags = buildOutputSchemaWithSelectionFlags(editToolOutputSchemaFull.value, editToolSelectedOutputPaths.value)
        if (withFlags) payload.outputSchema = withFlags
        else if (editToolSelectedOutputPaths.value.length > 0) {
          const reduced = buildOutputSchemaFromSelection(editToolOutputSchemaFull.value, editToolSelectedOutputPaths.value)
          if (reduced) payload.outputSchema = reduced
        }
      } else if (editToolOutputAvailablePaths.value.length === 0) {
        const manualItems = (editToolManualOutputPaths.value || []).filter(item => (item.path || '').trim() !== '')
        const manualSchema = buildMinimalOutputSchemaFromPaths(manualItems)
        if (manualSchema) payload.outputSchema = manualSchema
      }
      await apiService.updateMcpTool(mcpId.value, toolId.value, payload)
      toast.success('Tool actualizado')
    } else {
      const result = await apiService.addMcpTool(mcpId.value, {
        ...payload,
        toolType: toolMode.value === 'flow' ? 'Workflow' : 'Manual',
        integrationIds: addToolForm.value.integrationIds?.length > 0 ? addToolForm.value.integrationIds : undefined
      })
      toast.success('Tool añadido')
      if (toolMode.value === 'flow') {
        router.push(`/admin/mcps/${mcpId.value}/tools/${result.id}`)
      } else {
        router.push(backUrl.value)
      }
    }
  } catch (e) {
    toast.error(getApiErrorMessage(e) || (isEdit.value ? 'Error al actualizar' : 'Error al añadir'))
  } finally {
    addToolLoading.value = false
  }
}

onMounted(async () => {
  fetchUsage()
  await loadMcp()
  try {
    myIntegrations.value = await apiService.getMyIntegrations()
  } catch (e) {
    console.warn('Failed to load integrations', e)
  }
})
</script>

<style scoped>
.animate-slide-in {
  animation: slideIn 0.25s ease-out;
}
@keyframes slideIn {
  from {
    transform: translateX(100%);
  }
  to {
    transform: translateX(0);
  }
}
</style>

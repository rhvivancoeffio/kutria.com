<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6">
      <div v-if="loading" class="space-y-4">
        <div class="h-10 w-64 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        <div class="h-32 bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse" />
      </div>

      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <p class="text-red-800 dark:text-red-300">{{ error }}</p>
        <router-link to="/admin/mcps" class="mt-2 inline-block text-sm text-primary-600 dark:text-primary-400">← Volver a server</router-link>
      </div>

      <div v-else-if="mcp" class="space-y-4">
        <div class="space-y-1">
          <div>
            <router-link to="/admin/mcps" class="text-sm text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400">← Servers</router-link>
          </div>
          <div class="flex flex-wrap items-center gap-x-4 gap-y-1">
            <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ mcp.name }}</h1>
            <span class="text-sm text-gray-500 dark:text-gray-400">v{{ mcp.version }} · {{ securityTypeLabel(mcp.securityType) }}</span>
            <span
              :class="[
                'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium',
                mcp.isEnabled
                  ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                  : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-400'
              ]"
            >
              {{ mcp.isEnabled ? 'Habilitado' : 'Deshabilitado' }}
            </span>
            <button
              v-if="mcp.securityType === 'ApiKey' || mcp.securityType === 'OAuth'"
              type="button"
              @click="showHowToConnectSlider = true"
              class="inline-flex items-center gap-1.5 text-sm font-medium text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" /></svg>
              Cómo conectar
            </button>
            <p v-if="mcp.description && mcp.description !== mcp.name" class="w-full text-sm text-gray-600 dark:text-gray-300">{{ mcp.description }}</p>
          </div>
        </div>

        <!-- Sin integraciones: mensaje reutilizable -->
        <div
          v-if="!loadingIntegrations && myIntegrations.length === 0"
          class="rounded-lg border border-amber-200 dark:border-amber-800 bg-amber-50 dark:bg-amber-900/20 px-4 py-3"
        >
          <NoIntegrationsMessage class-names="text-sm text-amber-800 dark:text-amber-200 m-0" />
        </div>

        <!-- Tabs + Buscador: barra única -->
        <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 overflow-hidden">
          <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 p-3 sm:px-4 sm:py-2.5">
            <nav class="flex gap-1 -mb-px min-w-0 overflow-x-auto pb-px" aria-label="Tabs">
              <button
                v-for="tab in mcpTabs"
                :key="tab.id"
                type="button"
                :class="[
                  'px-3 py-2 text-sm font-medium rounded-t-lg border-b-2 transition-colors whitespace-nowrap shrink-0',
                  activeTab === tab.id
                    ? 'border-primary-600 text-primary-600 dark:text-primary-400 dark:border-primary-400 bg-white dark:bg-gray-800 shadow-[0_-1px_0_0_rgba(0,0,0,0.05)] dark:shadow-[0_-1px_0_0_rgba(255,255,255,0.05)]'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-300'
                ]"
                @click="activeTab = tab.id"
              >
                <span class="inline-flex items-center gap-2">
                  <span>{{ tab.icon }}</span>
                  {{ tab.label }}
                  <span
                    :class="[
                      'ml-1 px-2 py-0.5 rounded-full text-xs',
                      activeTab === tab.id
                        ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300'
                        : 'bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400'
                    ]"
                  >
                    {{ tab.count }}
                  </span>
                </span>
              </button>
            </nav>
            <div class="flex items-center gap-2 shrink-0 sm:min-w-[200px] lg:min-w-[240px]">
              <div class="relative flex-1 min-w-0">
                <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-gray-400 dark:text-gray-500">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
                </span>
                <input
                  v-model.trim="tabSearchQuery"
                  type="search"
                  :placeholder="tabSearchPlaceholder"
                  class="w-full text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 pl-9 pr-9 py-2 focus:ring-2 focus:ring-primary-500 focus:border-primary-500 dark:focus:ring-primary-400 dark:focus:border-primary-400"
                  autocomplete="off"
                  aria-label="Buscar en el listado"
                />
                <button
                  v-if="tabSearchQuery"
                  type="button"
                  @click="tabSearchQuery = ''"
                  class="absolute inset-y-0 right-0 flex items-center pr-2.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded-r-lg hover:bg-gray-100 dark:hover:bg-gray-700/50 transition-colors"
                  aria-label="Limpiar búsqueda"
                >
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
                </button>
              </div>
              <span v-if="tabSearchQuery" class="hidden sm:inline-flex items-center px-2 py-1 rounded-md bg-gray-200/80 dark:bg-gray-700/80 text-xs font-medium text-gray-600 dark:text-gray-400 tabular-nums">
                {{ filteredTabCount }}/{{ mcpTabs.find(t => t.id === activeTab)?.count ?? 0 }}
              </span>
            </div>
          </div>
        </div>

        <!-- Tab: Tools -->
        <section v-show="activeTab === 'tools'" class="space-y-4">
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-4 py-2 border-b border-gray-200 dark:border-gray-700 flex flex-wrap items-center justify-between gap-2">
              <h2 class="text-sm font-medium text-gray-900 dark:text-white">Tools</h2>
              <PlanLimitAlert
                v-if="limitMessageTools"
                :message="limitMessageTools"
                billing-link-text="Actualiza tu plan para más"
                class="text-xs"
              />
            </div>
            <div v-if="loadingTools" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 p-3">
              <div
                v-for="n in 6"
                :key="n"
                class="flex flex-col p-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50"
              >
                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0 flex-1">
                      <div class="h-4 w-3/4 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                      <div class="mt-1 h-3 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                    </div>
                    <div class="h-6 w-10 bg-gray-200 dark:bg-gray-700 rounded-full animate-pulse shrink-0" />
                  </div>
                  <div class="mt-2 h-3 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="mt-1 h-3 w-2/3 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="mt-2 h-5 w-20 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
                <div class="flex justify-end gap-1 mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
              </div>
            </div>
            <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 p-3">
              <div
                v-for="tool in filteredTools"
                :key="tool.id"
                class="flex flex-col p-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 card-hover"
              >
                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0 flex-1">
                      <p class="font-medium text-gray-900 dark:text-white truncate">{{ tool.name }}</p>
                      <span class="text-xs text-gray-500 dark:text-gray-400">v{{ tool.version ?? 1 }}</span>
                    </div>
                    <label class="relative inline-flex items-center cursor-pointer select-none shrink-0" :class="{ 'opacity-50 pointer-events-none': togglingToolId === tool.id }">
                      <input
                        type="checkbox"
                        :checked="tool.isEnabled !== false"
                        @change="toggleToolEnabled(tool)"
                        class="sr-only peer"
                      />
                      <div class="relative w-10 h-6 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-4 rtl:peer-checked:after:-translate-x-4" />
                    </label>
                  </div>
                  <p v-if="tool.description" class="mt-0.5 text-sm text-gray-500 dark:text-gray-400 line-clamp-2">{{ tool.description }}</p>
                  <span
                    :class="[
                      'inline-block mt-2 px-2 py-0.5 text-xs font-medium rounded',
                      (tool.integrations?.length ?? 0) > 0
                        ? 'bg-primary-100 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                        : 'bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400'
                    ]"
                  >
                    {{ (tool.integrations?.length ?? 0) > 0 ? `${tool.integrations.length} integración${tool.integrations.length !== 1 ? 'es' : ''}` : 'Manual' }}
                  </span>
                  <!-- Workflow: botón Ejecutar dentro del área tipo canvas -->
                  <div v-if="isToolFlow(tool)" class="mt-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-white/50 dark:bg-gray-800/50 p-2 flex items-center justify-between gap-2">
                    <span class="text-xs font-medium text-gray-500 dark:text-gray-400">Flujo</span>
                    <button type="button" @click="openExecuteSlider(tool)" class="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs font-medium bg-green-600 hover:bg-green-700 text-white" title="Ejecutar">
                      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                      Ejecutar
                    </button>
                  </div>
                </div>
                <div class="flex justify-end gap-1 mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                  <button type="button" @click="openVersionsModal('tool', tool)" class="p-2 text-gray-400 hover:text-amber-600 dark:hover:text-amber-400 rounded" title="Versiones">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  </button>
                  <button v-if="!isToolFlow(tool)" type="button" @click="openExecuteSlider(tool)" class="p-2 text-gray-400 hover:text-green-600 dark:hover:text-green-400 rounded" title="Ejecutar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  </button>
                  <router-link :to="`/admin/mcps/${mcpId}/tools/${tool.id}`" class="p-2 text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 rounded" title="Editar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  </router-link>
                  <button type="button" @click="duplicateTool(tool)" :disabled="atLimitTools" :class="['p-2 rounded', atLimitTools ? 'text-gray-300 dark:text-gray-600 cursor-not-allowed' : 'text-gray-400 hover:text-primary-600 dark:hover:text-primary-400']" title="Duplicar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
                  </button>
                  <button
                    type="button"
                    @click="confirmDeleteTool(tool)"
                    class="p-2 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded"
                    title="Eliminar tool"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                  </button>
                </div>
              </div>
              <router-link
                v-if="!atLimitTools"
                :to="`/admin/mcps/${mcpId}/tools/new`"
                class="flex flex-col items-center justify-center min-h-[120px] p-4 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 card-add-hover"
              >
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                <span class="mt-2 text-sm font-medium">Agregar</span>
                <span class="mt-0.5 text-xs text-gray-400 dark:text-gray-500">Nuevo tool</span>
              </router-link>
              <div
                v-else
                class="flex flex-col items-center justify-center min-h-[120px] p-4 rounded-lg border-2 border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 cursor-not-allowed"
              >
                <svg class="w-8 h-8 text-gray-400 dark:text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                <span class="mt-2 text-sm font-medium text-gray-500 dark:text-gray-400">Agregar</span>
                <p class="mt-1 text-xs text-amber-600 dark:text-amber-400 text-center">
                  Llegaste al límite ({{ toolsUsedCount }}/{{ usage?.usage?.mcpToolsLimit ?? 0 }}).
                  <router-link to="/admin/billing" class="underline hover:no-underline">Actualiza tu plan para más</router-link>
                </p>
              </div>
            </div>
          </div>
        </section>

        <!-- Tab: Prompts -->
        <section v-show="activeTab === 'prompts'" class="space-y-4">
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-4 py-2 border-b border-gray-200 dark:border-gray-700 flex flex-wrap items-center justify-between gap-2">
              <h2 class="text-sm font-medium text-gray-900 dark:text-white">Prompts</h2>
              <PlanLimitAlert
                v-if="limitMessagePrompts"
                :message="limitMessagePrompts"
                billing-link-text="Actualiza tu plan para más"
                class="text-xs"
              />
            </div>
            <div v-if="loadingPrompts" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 p-3">
              <div
                v-for="n in 6"
                :key="n"
                class="flex flex-col p-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50"
              >
                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0 flex-1">
                      <div class="h-4 w-3/4 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                      <div class="mt-1 h-3 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                    </div>
                    <div class="h-6 w-10 bg-gray-200 dark:bg-gray-700 rounded-full animate-pulse shrink-0" />
                  </div>
                  <div class="mt-2 h-3 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="mt-1 h-3 w-3/4 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
                <div class="flex justify-end gap-1 mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
              </div>
            </div>
            <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 p-3">
              <div
                v-for="prompt in filteredPrompts"
                :key="prompt.id"
                class="flex flex-col p-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 card-hover"
              >
                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0 flex-1">
                      <p class="font-medium text-gray-900 dark:text-white truncate">{{ prompt.name }}</p>
                      <span class="text-xs text-gray-500 dark:text-gray-400">v{{ prompt.version ?? 1 }}</span>
                    </div>
                    <label class="relative inline-flex items-center cursor-pointer select-none shrink-0" :class="{ 'opacity-50 pointer-events-none': togglingPromptId === prompt.id }">
                      <input
                        type="checkbox"
                        :checked="prompt.isEnabled !== false"
                        @change="togglePromptEnabled(prompt)"
                        class="sr-only peer"
                      />
                      <div class="relative w-10 h-6 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-4 rtl:peer-checked:after:-translate-x-4" />
                    </label>
                  </div>
                  <p v-if="prompt.description" class="mt-0.5 text-sm text-gray-500 dark:text-gray-400 line-clamp-2">{{ prompt.description }}</p>
                </div>
                <div class="flex justify-end gap-1 mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                  <button type="button" @click="openPromptPreview(prompt)" class="p-2 text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 rounded" title="Vista previa">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                  </button>
                  <button type="button" @click="openVersionsModal('prompt', prompt)" class="p-2 text-gray-400 hover:text-amber-600 dark:hover:text-amber-400 rounded" title="Versiones">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  </button>
                  <router-link :to="`/admin/mcps/${mcpId}/prompts/${prompt.id}`" class="p-2 text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 rounded" title="Editar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  </router-link>
                  <button type="button" @click="duplicatePrompt(prompt)" :disabled="atLimitPrompts" :class="['p-2 rounded', atLimitPrompts ? 'text-gray-300 dark:text-gray-600 cursor-not-allowed' : 'text-gray-400 hover:text-primary-600 dark:hover:text-primary-400']" title="Duplicar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
                  </button>
                  <button type="button" @click="confirmDeletePrompt(prompt)" class="p-2 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded" title="Eliminar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                  </button>
                </div>
              </div>
              <router-link
                v-if="!atLimitPrompts"
                :to="`/admin/mcps/${mcpId}/prompts/new`"
                class="flex flex-col items-center justify-center min-h-[120px] p-4 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 card-add-hover"
              >
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                <span class="mt-2 text-sm font-medium">Agregar</span>
                <span class="mt-0.5 text-xs text-gray-400 dark:text-gray-500">Nuevo prompt</span>
              </router-link>
              <div
                v-else
                class="flex flex-col items-center justify-center min-h-[120px] p-4 rounded-lg border-2 border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 cursor-not-allowed"
              >
                <svg class="w-8 h-8 text-gray-400 dark:text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                <span class="mt-2 text-sm font-medium text-gray-500 dark:text-gray-400">Agregar</span>
                <p class="mt-1 text-xs text-amber-600 dark:text-amber-400 text-center">
                  Llegaste al límite ({{ promptsUsedCount }}/{{ usage?.usage?.mcpPromptsLimit ?? 0 }}).
                  <router-link to="/admin/billing" class="underline hover:no-underline">Actualiza tu plan para más</router-link>
                </p>
              </div>
            </div>
          </div>
        </section>

        <!-- Tab: Resources -->
        <section v-show="activeTab === 'resources'" class="space-y-4">
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-4 py-2 border-b border-gray-200 dark:border-gray-700 flex flex-wrap items-center justify-between gap-2">
              <h2 class="text-sm font-medium text-gray-900 dark:text-white">Resources</h2>
              <PlanLimitAlert
                v-if="limitMessageResources"
                :message="limitMessageResources"
                billing-link-text="Actualiza tu plan para más"
                class="text-xs"
              />
            </div>
            <div v-if="loadingResources" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 p-3">
              <div
                v-for="n in 6"
                :key="n"
                class="flex flex-col p-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50"
              >
                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0 flex-1">
                      <div class="h-4 w-3/4 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                      <div class="mt-1 h-3 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                    </div>
                    <div class="h-6 w-10 bg-gray-200 dark:bg-gray-700 rounded-full animate-pulse shrink-0" />
                  </div>
                  <div class="mt-2 h-3 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse font-mono" />
                  <div class="mt-1 h-3 w-1/2 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
                <div class="flex justify-end gap-1 mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  <div class="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                </div>
              </div>
            </div>
            <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 p-3">
              <div
                v-for="resource in filteredResources"
                :key="resource.id"
                class="flex flex-col p-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 card-hover"
              >
                <div class="min-w-0 flex-1">
                  <div class="flex items-start justify-between gap-2">
                    <div class="min-w-0 flex-1">
                      <p class="font-medium text-gray-900 dark:text-white truncate">{{ resource.name }}</p>
                      <span class="text-xs text-gray-500 dark:text-gray-400">v{{ resource.version ?? 1 }}</span>
                    </div>
                    <label class="relative inline-flex items-center cursor-pointer select-none shrink-0" :class="{ 'opacity-50 pointer-events-none': togglingResourceId === resource.id }">
                      <input
                        type="checkbox"
                        :checked="resource.isEnabled !== false"
                        @change="toggleResourceEnabled(resource)"
                        class="sr-only peer"
                      />
                      <div class="relative w-10 h-6 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-4 rtl:peer-checked:after:-translate-x-4" />
                    </label>
                  </div>
                  <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400 truncate font-mono">{{ resource.uri }}</p>
                </div>
                <div class="flex justify-end gap-1 mt-2 pt-2 border-t border-gray-200 dark:border-gray-700">
                  <button type="button" @click="openVersionsModal('resource', resource)" class="p-2 text-gray-400 hover:text-amber-600 dark:hover:text-amber-400 rounded" title="Versiones">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                  </button>
                  <router-link :to="`/admin/mcps/${mcpId}/resources/${resource.id}`" class="p-2 text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 rounded" title="Editar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  </router-link>
                  <button type="button" @click="duplicateResource(resource)" :disabled="atLimitResources" :class="['p-2 rounded', atLimitResources ? 'text-gray-300 dark:text-gray-600 cursor-not-allowed' : 'text-gray-400 hover:text-primary-600 dark:hover:text-primary-400']" title="Duplicar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
                  </button>
                  <button type="button" @click="confirmDeleteResource(resource)" class="p-2 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded" title="Eliminar">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                  </button>
                </div>
              </div>
              <router-link
                v-if="!atLimitResources"
                :to="`/admin/mcps/${mcpId}/resources/new`"
                class="flex flex-col items-center justify-center min-h-[120px] p-4 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 card-add-hover"
              >
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                <span class="mt-2 text-sm font-medium">Agregar</span>
                <span class="mt-0.5 text-xs text-gray-400 dark:text-gray-500">Nuevo resource</span>
              </router-link>
              <div
                v-else
                class="flex flex-col items-center justify-center min-h-[120px] p-4 rounded-lg border-2 border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800/50 cursor-not-allowed"
              >
                <svg class="w-8 h-8 text-gray-400 dark:text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                <span class="mt-2 text-sm font-medium text-gray-500 dark:text-gray-400">Agregar</span>
                <p class="mt-1 text-xs text-amber-600 dark:text-amber-400 text-center">
                  Llegaste al límite ({{ resourcesUsedCount }}/{{ usage?.usage?.mcpResourcesLimit ?? 0 }}).
                  <router-link to="/admin/billing" class="underline hover:no-underline">Actualiza tu plan para más</router-link>
                </p>
              </div>
            </div>
          </div>
        </section>
      </div>
    </main>

    <!-- Delete Confirmation Modal -->
    <Teleport to="body">
      <div
        v-if="showDeleteModal"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">{{ deleteModalTitle }}</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400" v-html="deleteModalMessage" />
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button type="button" @click="showDeleteModal = false" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">
              Cancelar
            </button>
            <button type="button" @click="handleDelete" :disabled="deleteLoading" :class="['w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors', deleteLoading ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-red-600 hover:bg-red-700 text-white']">
              Eliminar
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Cómo conectar - Slider Panel -->
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
          v-if="showHowToConnectSlider"
          class="fixed inset-0 z-50 bg-black/50"
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
          v-if="showHowToConnectSlider && mcp"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-full sm:max-w-md lg:max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Cómo conectar</h2>
                <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  {{ mcp.name }} · {{ securityTypeLabel(mcp.securityType) }}
                </p>
              </div>
              <button
                type="button"
                @click="showHowToConnectSlider = false"
                class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
                aria-label="Cerrar"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          </div>
          <div class="flex-1 overflow-y-auto p-4 sm:p-6">
            <McpHowToConnect
              :security-type="mcp.securityType"
              :server-id="mcp.id"
              :base-url="mcpBaseUrl"
              :show-title="false"
            />
          </div>
          <div class="shrink-0 p-4 sm:p-6 border-t border-gray-200 dark:border-gray-700 flex justify-end bg-gray-50 dark:bg-gray-800/50">
            <button
              type="button"
              @click="showHowToConnectSlider = false"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              Cerrar
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Execute Tool Slider (componente reutilizable) -->
    <ExecuteToolSlider
      v-model="showExecuteSlider"
      :tool="executeTool"
      :mcp-id="mcpId"
    />

    <!-- Prompt Preview Slider -->
    <McpPromptPreviewSlider
      v-model="showPromptPreviewSlider"
      :prompt="previewPrompt"
      :tools="toolsList ?? []"
    />

    <!-- Versions Modal -->
    <Teleport to="body">
      <div
        v-if="showVersionsModal"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div
          class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl w-full flex flex-col max-w-4xl min-h-[70vh] max-h-[90vh] sm:min-h-[420px] sm:max-h-[85vh] h-[88vh] sm:h-auto"
          @click.stop
        >
          <div class="px-3 sm:px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between gap-2 shrink-0 min-h-[52px]">
            <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white truncate min-w-0">
              Versiones – {{ versionsModalItem?.name ?? '' }}
            </h3>
            <button type="button" @click="closeVersionsModal" class="p-2 shrink-0 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded touch-manipulation" aria-label="Cerrar">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>
          <div class="flex-1 min-h-0 overflow-hidden flex flex-col sm:flex-row">
            <div class="w-full sm:w-56 lg:w-64 border-b sm:border-b-0 sm:border-r border-gray-200 dark:border-gray-700 overflow-y-auto shrink-0 min-h-[180px] sm:min-h-0">
              <p class="px-3 py-2 text-xs font-medium text-gray-500 dark:text-gray-400 sticky top-0 bg-white dark:bg-gray-800">Historial</p>
              <div v-if="versionsLoading" class="px-3 py-6 text-sm text-gray-500">Cargando…</div>
              <ul v-else-if="versionsList.length === 0" class="px-3 py-6 text-sm text-gray-500">No hay versiones anteriores.</ul>
              <ul v-else class="divide-y divide-gray-200 dark:divide-gray-700 pb-2">
                <li
                  v-for="v in versionsList"
                  :key="v.version"
                  class="flex items-center justify-between gap-2 px-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                  :class="{ 'bg-primary-50 dark:bg-primary-900/20': selectedVersionNum === v.version }"
                >
                  <button type="button" @click="selectVersion(v.version)" class="flex-1 text-left min-w-0 py-0.5">
                    <span class="font-medium text-gray-900 dark:text-white block truncate">v{{ v.version }}</span>
                    <span class="block text-xs text-gray-500 dark:text-gray-400">{{ formatVersionDate(v.createdAt) }}</span>
                  </button>
                  <button
                    v-if="v.version !== (versionsModalItem?.version ?? 1)"
                    type="button"
                    @click="restoreVersion(v.version)"
                    :disabled="restoreLoading"
                    class="text-xs px-2 py-1.5 rounded bg-primary-600 text-white hover:bg-primary-700 disabled:opacity-50 shrink-0 touch-manipulation"
                  >
                    Restaurar
                  </button>
                </li>
              </ul>
            </div>
            <div class="flex-1 min-h-[200px] sm:min-h-0 overflow-auto p-3 sm:p-4">
              <template v-if="selectedVersionNum != null">
                <div v-if="snapshotLoading" class="py-8 text-sm text-gray-500 text-center">Cargando versión…</div>
                <div v-else-if="versionSnapshot" class="space-y-3 sm:space-y-4">
                  <p class="text-sm font-medium text-gray-700 dark:text-gray-300">Cambio: versión {{ selectedVersionNum }} ({{ formatVersionDate(versionSnapshot.createdAt) }}) — Actual vs versión seleccionada</p>
                  <div class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden min-h-[200px] max-h-[50vh] sm:max-h-[400px]">
                    <Diff
                      :prev="snapshotPreview"
                      :current="currentVersionPreview"
                      mode="split"
                      :theme="diffTheme"
                      language="json"
                      class="vue-diff-versions-modal"
                    />
                  </div>
                </div>
              </template>
              <p v-else class="text-sm text-gray-500 dark:text-gray-400 py-6 sm:py-8">Haz clic en una versión para ver el cambio.</p>
            </div>
          </div>
          <footer class="px-3 sm:px-4 py-3 border-t border-gray-200 dark:border-gray-700 flex justify-end shrink-0 bg-gray-50/50 dark:bg-gray-800/50 rounded-b-2xl sm:rounded-b-xl pb-safe">
            <button
              type="button"
              @click="closeVersionsModal"
              class="w-full sm:w-auto min-h-[44px] px-4 py-2.5 rounded-lg font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600 transition-colors touch-manipulation"
            >
              Cerrar
            </button>
          </footer>
        </div>
      </div>
    </Teleport>

  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import { useTheme } from '../../composables/useTheme'
import apiService from '../../services/api'
import { isFlowTool } from '../../utils/workflow'
import McpHowToConnect from '../../components/mcp/McpHowToConnect.vue'
import ExecuteToolSlider from '../../components/mcp/ExecuteToolSlider.vue'
import McpPromptPreviewSlider from '../../components/mcp/McpPromptPreviewSlider.vue'
import PlanLimitAlert from '../../components/usage/PlanLimitAlert.vue'
import NoIntegrationsMessage from '../../components/integrations/NoIntegrationsMessage.vue'
import { useAccountUsage } from '../../composables/useAccountUsage'

function isToolFlow(tool) {
  if (tool?.toolType === 'Workflow') return true
  if (tool?.toolType === 'Manual') return false
  return isFlowTool(tool?.workflowDefinitionJson)
}

const route = useRoute()
const toast = useToast()
const { theme } = useTheme()
const {
  usage,
  isAtLimitForMcpTools,
  getLimitMessageForMcpTools,
  isAtLimitForMcpPrompts,
  getLimitMessageForMcpPrompts,
  isAtLimitForMcpResources,
  getLimitMessageForMcpResources,
  fetchUsage
} = useAccountUsage()
const diffTheme = computed(() => (theme.value === 'dark' ? 'dark' : 'light'))
const mcpId = computed(() => route.params.id)
const mcp = ref(null)
// MCP URL: in production use backend base (e.g. https://backend.meetgravity.io/channels); in dev use origin (proxy serves /mcp).
const apiUrl = import.meta.env.VITE_API_URL || ''
const mcpBaseUrl = computed(() => {
  if (typeof window === 'undefined') return ''
  if (apiUrl && apiUrl !== '/api') return apiUrl.replace(/\/api\/?$/, '')
  return window.location.origin
})
const loading = ref(true)
const error = ref(null)
const myIntegrations = ref([])
const loadingIntegrations = ref(false)
const activeTab = ref('tools')
/** Tab data loaded on demand (null = not loaded yet). */
const toolsList = ref(null)
const promptsList = ref(null)
const resourcesList = ref(null)
const loadingTools = ref(false)
const loadingPrompts = ref(false)
const loadingResources = ref(false)
/** Buscador en memoria: filtra tools, prompts o resources del tab activo */
const tabSearchQuery = ref('')

const mcpTabs = computed(() => {
  const tools = toolsList.value !== null ? toolsList.value.length : (mcp.value?.toolsCount ?? 0)
  const prompts = promptsList.value !== null ? promptsList.value.length : (mcp.value?.promptsCount ?? 0)
  const resources = resourcesList.value !== null ? resourcesList.value.length : (mcp.value?.resourcesCount ?? 0)
  return [
    { id: 'tools', label: 'Tools', icon: '⚙', count: tools },
    { id: 'prompts', label: 'Prompts', icon: '💬', count: prompts },
    { id: 'resources', label: 'Resources', icon: '📄', count: resources }
  ]
})

/** Filtra en memoria por nombre y descripción (y uri en resources). Case-insensitive. */
function matchesSearch(item, query, fields) {
  if (!query) return true
  const q = query.toLowerCase()
  return fields.some(f => (item[f] ?? '').toString().toLowerCase().includes(q))
}

const filteredTools = computed(() => {
  const list = toolsList.value ?? []
  const q = tabSearchQuery.value
  if (!q) return list
  return list.filter(t => matchesSearch(t, q, ['name', 'title', 'description']))
})

const filteredPrompts = computed(() => {
  const list = promptsList.value ?? []
  const q = tabSearchQuery.value
  if (!q) return list
  return list.filter(p => matchesSearch(p, q, ['name', 'title', 'description']))
})

const filteredResources = computed(() => {
  const list = resourcesList.value ?? []
  const q = tabSearchQuery.value
  if (!q) return list
  return list.filter(r => matchesSearch(r, q, ['name', 'title', 'description', 'uri']))
})

const tabSearchPlaceholder = computed(() => {
  const t = activeTab.value
  if (t === 'tools') return 'Buscar tools…'
  if (t === 'prompts') return 'Buscar prompts…'
  if (t === 'resources') return 'Buscar resources…'
  return 'Buscar…'
})

const filteredTabCount = computed(() => {
  if (activeTab.value === 'tools') return filteredTools.value.length
  if (activeTab.value === 'prompts') return filteredPrompts.value.length
  if (activeTab.value === 'resources') return filteredResources.value.length
  return 0
})

const toolsUsedCount = computed(() => (toolsList.value !== null ? toolsList.value.length : (mcp.value?.toolsCount ?? 0)))
const promptsUsedCount = computed(() => (promptsList.value !== null ? promptsList.value.length : (mcp.value?.promptsCount ?? 0)))
const resourcesUsedCount = computed(() => (resourcesList.value !== null ? resourcesList.value.length : (mcp.value?.resourcesCount ?? 0)))
const atLimitTools = computed(() => isAtLimitForMcpTools(toolsUsedCount.value))
const atLimitPrompts = computed(() => isAtLimitForMcpPrompts(promptsUsedCount.value))
const atLimitResources = computed(() => isAtLimitForMcpResources(resourcesUsedCount.value))
const limitMessageTools = computed(() => getLimitMessageForMcpTools(toolsUsedCount.value))
const limitMessagePrompts = computed(() => getLimitMessageForMcpPrompts(promptsUsedCount.value))
const limitMessageResources = computed(() => getLimitMessageForMcpResources(resourcesUsedCount.value))

const showDeleteModal = ref(false)
const deleteLoading = ref(false)
const deleteTarget = ref(null) // { type: 'tool'|'prompt'|'resource', item: {...} }
const togglingToolId = ref(null)

const showHowToConnectSlider = ref(false)

const showExecuteSlider = ref(false)
const executeTool = ref(null)

function openExecuteSlider(tool) {
  executeTool.value = tool
  showExecuteSlider.value = true
}

watch(showExecuteSlider, (v) => {
  if (!v) executeTool.value = null
})

const showPromptPreviewSlider = ref(false)
const previewPrompt = ref(null)

function openPromptPreview(prompt) {
  previewPrompt.value = prompt
  showPromptPreviewSlider.value = true
}

watch(showPromptPreviewSlider, (v) => {
  if (!v) previewPrompt.value = null
})
const togglingPromptId = ref(null)
const togglingResourceId = ref(null)

const showVersionsModal = ref(false)
const versionsModalType = ref('tool') // 'tool' | 'prompt' | 'resource'
const versionsModalItem = ref(null)
const versionsList = ref([])
const versionsLoading = ref(false)
const selectedVersionNum = ref(null)
const versionSnapshot = ref(null)
const snapshotLoading = ref(false)
const restoreLoading = ref(false)

function formatVersionDate(createdAt) {
  if (!createdAt) return ''
  const d = new Date(createdAt)
  return d.toLocaleString('es-ES', { dateStyle: 'short', timeStyle: 'short' })
}

function safeJson(s, fallback) {
  if (s == null) return fallback
  if (typeof s !== 'string') return s
  try {
    return JSON.parse(s || (fallback === [] ? '[]' : '{}'))
  } catch {
    return fallback
  }
}

function buildVersionPreview(type, item) {
  if (!item) return ''
  try {
    if (type === 'tool') {
      return JSON.stringify({
        name: item.name,
        title: item.title,
        description: item.description,
        inputSchema: safeJson(item.inputSchema, {}),
        workflowDefinitionJson: safeJson(item.workflowDefinitionJson, {})
      }, null, 2)
    }
    if (type === 'prompt') {
      return JSON.stringify({
        name: item.name,
        title: item.title,
        description: item.description,
        messageBlocks: typeof item.messageBlocks === 'string' ? safeJson(item.messageBlocks, []) : (item.messageBlocks ?? []),
        argumentsSchema: safeJson(item.argumentsSchema, []),
        allowedToolIds: item.allowedToolIds ?? [],
        temperature: item.temperature,
        topP: item.topP,
        topK: item.topK,
        maxTokens: item.maxTokens,
        outputFormat: item.outputFormat,
        outputSchema: item.outputSchema
      }, null, 2)
    }
    if (type === 'resource') {
      return JSON.stringify({
        uri: item.uri,
        name: item.name,
        title: item.title,
        description: item.description,
        mimeType: item.mimeType,
        content: item.content ? (String(item.content).length > 500 ? String(item.content).slice(0, 500) + '…' : item.content) : null
      }, null, 2)
    }
  } catch {
    return String(item?.name ?? item?.uri ?? '')
  }
  return ''
}

const currentVersionPreview = computed(() => buildVersionPreview(versionsModalType.value, versionsModalItem.value))

const snapshotPreview = computed(() => {
  const s = versionSnapshot.value
  if (!s) return ''
  try {
    if (versionsModalType.value === 'tool') {
      return JSON.stringify({
        name: s.name,
        title: s.title,
        description: s.description,
        inputSchema: safeJson(s.inputSchema, {}),
        workflowDefinitionJson: safeJson(s.workflowDefinitionJson, {})
      }, null, 2)
    }
    if (versionsModalType.value === 'prompt') {
      return JSON.stringify({
        name: s.name,
        title: s.title,
        description: s.description,
        messageBlocks: typeof s.messageBlocks === 'string' ? safeJson(s.messageBlocks, []) : (s.messageBlocks ?? []),
        argumentsSchema: safeJson(s.argumentsSchema, []),
        allowedToolIds: s.allowedToolIds ?? [],
        temperature: s.temperature,
        topP: s.topP,
        topK: s.topK,
        maxTokens: s.maxTokens,
        outputFormat: s.outputFormat,
        outputSchema: s.outputSchema
      }, null, 2)
    }
    if (versionsModalType.value === 'resource') {
      return JSON.stringify({
        uri: s.uri,
        name: s.name,
        title: s.title,
        description: s.description,
        mimeType: s.mimeType,
        content: s.content ? (String(s.content).length > 500 ? String(s.content).slice(0, 500) + '…' : s.content) : null
      }, null, 2)
    }
  } catch {
    return ''
  }
  return ''
})

async function openVersionsModal(type, item) {
  versionsModalType.value = type
  versionsModalItem.value = item
  showVersionsModal.value = true
  versionsList.value = []
  selectedVersionNum.value = null
  versionSnapshot.value = null
  versionsLoading.value = true
  try {
    if (type === 'tool') {
      const res = await apiService.getMcpToolVersionHistory(mcpId.value, item.id)
      versionsList.value = res?.versions ?? []
    } else if (type === 'prompt') {
      const res = await apiService.getMcpPromptVersionHistory(mcpId.value, item.id)
      versionsList.value = res?.versions ?? []
    } else {
      const res = await apiService.getMcpResourceVersionHistory(mcpId.value, item.id)
      versionsList.value = res?.versions ?? []
    }
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al cargar versiones')
  } finally {
    versionsLoading.value = false
  }
}

function closeVersionsModal() {
  showVersionsModal.value = false
  versionsModalItem.value = null
  versionsList.value = []
  selectedVersionNum.value = null
  versionSnapshot.value = null
}

async function selectVersion(version) {
  selectedVersionNum.value = version
  versionSnapshot.value = null
  snapshotLoading.value = true
  try {
    const type = versionsModalType.value
    const item = versionsModalItem.value
    if (type === 'tool') {
      versionSnapshot.value = await apiService.getMcpToolVersionSnapshot(mcpId.value, item.id, version)
    } else if (type === 'prompt') {
      versionSnapshot.value = await apiService.getMcpPromptVersionSnapshot(mcpId.value, item.id, version)
    } else {
      versionSnapshot.value = await apiService.getMcpResourceVersionSnapshot(mcpId.value, item.id, version)
    }
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al cargar versión')
  } finally {
    snapshotLoading.value = false
  }
}

async function restoreVersion(version) {
  restoreLoading.value = true
  try {
    const type = versionsModalType.value
    const item = versionsModalItem.value
    if (type === 'tool') {
      await apiService.restoreMcpToolFromVersion(mcpId.value, item.id, version)
      toast.success('Tool restaurado a la versión ' + version)
    } else if (type === 'prompt') {
      await apiService.restoreMcpPromptFromVersion(mcpId.value, item.id, version)
      toast.success('Prompt restaurado a la versión ' + version)
    } else {
      await apiService.restoreMcpResourceFromVersion(mcpId.value, item.id, version)
      toast.success('Resource restaurado a la versión ' + version)
    }
    closeVersionsModal()
    await loadMcp()
    loadTabData(activeTab.value, true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al restaurar')
  } finally {
    restoreLoading.value = false
  }
}

const deleteModalTitle = computed(() => {
  if (!deleteTarget.value) return 'Eliminar'
  const t = deleteTarget.value.type
  if (t === 'tool') return 'Eliminar tool'
  if (t === 'prompt') return 'Eliminar prompt'
  if (t === 'resource') return 'Eliminar resource'
  return 'Eliminar'
})

const deleteModalMessage = computed(() => {
  if (!deleteTarget.value) return ''
  const name = deleteTarget.value.item?.name ?? deleteTarget.value.item?.title ?? 'este elemento'
  return `¿Eliminar <strong class="text-gray-900 dark:text-white">${escapeHtml(name)}</strong>? Esta acción no se puede deshacer.`
})

function escapeHtml(s) {
  const div = document.createElement('div')
  div.textContent = s
  return div.innerHTML
}

function securityTypeLabel(type) {
  const labels = { None: 'Sin seguridad', ApiKey: 'ApiKey', OAuth: 'OAuth' }
  return labels[type] ?? type
}

async function loadMcp() {
  loading.value = true
  error.value = null
  try {
    mcp.value = await apiService.getMcpById(mcpId.value)
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar'
  } finally {
    loading.value = false
  }
}

async function loadToolsTab(force = false) {
  if (!force && toolsList.value !== null && !loadingTools.value) return
  loadingTools.value = true
  try {
    toolsList.value = await apiService.getMcpTools(mcpId.value)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al cargar tools')
    toolsList.value = []
  } finally {
    loadingTools.value = false
  }
}

async function loadPromptsTab(force = false) {
  if (!force && promptsList.value !== null && !loadingPrompts.value) return
  loadingPrompts.value = true
  try {
    promptsList.value = await apiService.getMcpPrompts(mcpId.value)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al cargar prompts')
    promptsList.value = []
  } finally {
    loadingPrompts.value = false
  }
}

async function loadResourcesTab(force = false) {
  if (!force && resourcesList.value !== null && !loadingResources.value) return
  loadingResources.value = true
  try {
    resourcesList.value = await apiService.getMcpResources(mcpId.value)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al cargar resources')
    resourcesList.value = []
  } finally {
    loadingResources.value = false
  }
}

function loadTabData(tab, force = false) {
  if (tab === 'tools') loadToolsTab(force)
  else if (tab === 'prompts') loadPromptsTab(force)
  else if (tab === 'resources') loadResourcesTab(force)
}

function confirmDeleteTool(tool) {
  deleteTarget.value = { type: 'tool', item: tool }
  showDeleteModal.value = true
}

function confirmDeletePrompt(prompt) {
  deleteTarget.value = { type: 'prompt', item: prompt }
  showDeleteModal.value = true
}

function confirmDeleteResource(resource) {
  deleteTarget.value = { type: 'resource', item: resource }
  showDeleteModal.value = true
}

async function handleDelete() {
  if (!deleteTarget.value) return
  deleteLoading.value = true
  try {
    const { type, item } = deleteTarget.value
    if (type === 'tool') {
      await apiService.deleteMcpTool(mcpId.value, item.id)
      toast.success('Tool eliminado')
    } else if (type === 'prompt') {
      await apiService.deleteMcpPrompt(mcpId.value, item.id)
      toast.success('Prompt eliminado')
    } else if (type === 'resource') {
      await apiService.deleteMcpResource(mcpId.value, item.id)
      toast.success('Resource eliminado')
    }
    showDeleteModal.value = false
    deleteTarget.value = null
    await loadMcp()
    loadTabData(activeTab.value, true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al eliminar')
  } finally {
    deleteLoading.value = false
  }
}

async function toggleToolEnabled(tool) {
  togglingToolId.value = tool.id
  try {
    await apiService.setMcpToolEnabled(mcpId.value, tool.id, !(tool.isEnabled !== false))
    toast.success(tool.isEnabled !== false ? 'Tool deshabilitado' : 'Tool habilitado')
    await loadMcp()
    await loadToolsTab(true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al actualizar')
    await loadMcp()
    await loadToolsTab(true)
  } finally {
    togglingToolId.value = null
  }
}

async function togglePromptEnabled(prompt) {
  togglingPromptId.value = prompt.id
  try {
    await apiService.setMcpPromptEnabled(mcpId.value, prompt.id, !(prompt.isEnabled !== false))
    toast.success(prompt.isEnabled !== false ? 'Prompt deshabilitado' : 'Prompt habilitado')
    await loadMcp()
    await loadPromptsTab(true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al actualizar')
    await loadMcp()
    await loadPromptsTab(true)
  } finally {
    togglingPromptId.value = null
  }
}

async function toggleResourceEnabled(resource) {
  togglingResourceId.value = resource.id
  try {
    await apiService.setMcpResourceEnabled(mcpId.value, resource.id, !(resource.isEnabled !== false))
    toast.success(resource.isEnabled !== false ? 'Resource deshabilitado' : 'Resource habilitado')
    await loadMcp()
    await loadResourcesTab(true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al actualizar')
    await loadMcp()
    await loadResourcesTab(true)
  } finally {
    togglingResourceId.value = null
  }
}

async function duplicateTool(tool) {
  try {
    await apiService.duplicateMcpTool(mcpId.value, tool.id)
    toast.success('Tool duplicado')
    await loadMcp()
    await loadToolsTab(true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al duplicar')
  }
}

async function duplicatePrompt(prompt) {
  try {
    await apiService.duplicateMcpPrompt(mcpId.value, prompt.id)
    toast.success('Prompt duplicado')
    await loadMcp()
    await loadPromptsTab(true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al duplicar')
  }
}

async function duplicateResource(resource) {
  try {
    await apiService.duplicateMcpResource(mcpId.value, resource.id)
    toast.success('Resource duplicado')
    await loadMcp()
    await loadResourcesTab(true)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al duplicar')
  }
}

watch(activeTab, (tab) => {
  loadTabData(tab)
})

async function loadMyIntegrations() {
  loadingIntegrations.value = true
  try {
    myIntegrations.value = await apiService.getMyIntegrations()
  } catch {
    myIntegrations.value = []
  } finally {
    loadingIntegrations.value = false
  }
}

onMounted(() => {
  fetchUsage()
  loadMyIntegrations()
  loadMcp().then(() => {
    loadTabData(activeTab.value)
  })
})
</script>

<style scoped>
@keyframes slideInRight {
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
}
.animate-slide-in-right {
  animation: slideInRight 0.25s ease-out;
}
</style>

<style>
/* Vue-diff: make the component fill the modal container and scroll */
.vue-diff-versions-modal {
  height: 100%;
  min-height: 200px;
}
.vue-diff-versions-modal .vue-diff {
  height: 100%;
}
</style>

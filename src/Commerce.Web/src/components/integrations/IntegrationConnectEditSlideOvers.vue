<template>
<div class="integration-connect-edit-modals-host" aria-hidden="true">
    <input
      ref="inlineFileInputRef"
      type="file"
      accept=".json"
      class="hidden"
      @change="onInlineFileLoaded"
    />
    <!-- Slider Conectar -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="connectModalOpen"
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
          v-if="connectModalOpen"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-full sm:max-w-md lg:max-w-2xl bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 flex items-start justify-between shrink-0 gap-3">
            <div class="min-w-0 flex-1">
              <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white">
                {{ isMcpConnect ? 'Agregar servidor MCP' : isChannelConnect || isSupportConnect ? `Conectar ${selectedMeta?.name}` : `Conectar ${selectedMeta?.name}` }}
              </h3>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1 line-clamp-2">{{ selectedMeta?.description }}</p>
            </div>
            <button
              type="button"
              @click="closeConnectModal"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              aria-label="Cerrar"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <form @submit.prevent="submitConnect" class="flex flex-col flex-1 min-h-0">
            <div class="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ isMcpConnect ? 'Nombre del servidor' : (isChannelConnect ? 'Nombre del canal' : (isSupportConnect ? 'Nombre de la herramienta' : (isOpenApiConnect ? 'Nombre del servicio' : 'Nombre de la tienda'))) }} <span class="text-red-500">*</span>
                </label>
                <input
                  v-model="connectForm.name"
                  type="text"
                  required
                  :placeholder="isMcpConnect ? 'Nombre del servidor MCP' : (isChannelConnect ? (selectedMeta ? `Ej: Mi ${selectedMeta.name} principal` : 'Ej: Mi canal principal') : (isSupportConnect ? (selectedMeta ? `Ej: Mi ${selectedMeta.name} principal` : 'Ej: Mi herramienta principal') : (isOpenApiConnect ? 'Ej: Mi servicio OpenAPI principal' : (selectedMeta ? `Ej: Mi tienda ${selectedMeta.name} principal` : 'Ej: Mi tienda principal'))))"
                  :class="[
                    'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                    isConnectFieldInvalid('name') ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                  ]"
                  @blur="setConnectTouched('name')"
                />
                <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                  {{ isMcpConnect ? 'Identificador para distinguir este servidor de otros' : (isChannelConnect ? 'Identificador para distinguir este canal de otros' : (isSupportConnect ? 'Identificador para distinguir esta herramienta de otras' : (isOpenApiConnect ? 'Identificador para distinguir este servicio de otros' : 'Identificador para distinguir esta tienda de otras del mismo proveedor'))) }}
                </p>
              </div>

              <template v-for="setting in getConnectSettingsPhase1OrAll()" :key="setting.key">
              <div v-if="isOpenApiSettingVisible(setting, connectForm.settings, selectedMeta)">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ setting.label }}
                  <span v-if="setting.required" class="text-red-500">*</span>
                </label>
                <div class="relative">
                  <div
                    v-if="(selectedMeta?.key === 'VTEX' || selectedMeta?.key === 'Shopify') && setting.key === 'accountName'"
                    class="flex flex-col sm:flex-row rounded-lg border overflow-hidden"
                    :class="isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500' : 'border-gray-300 dark:border-gray-600'"
                  >
                    <input
                      v-model="connectForm.settings[setting.key]"
                      type="text"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      class="flex-1 min-w-0 px-3 py-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 border-0 focus:border-0 rounded-none"
                      @blur="setConnectTouched(setting.key)"
                    />
                    <span class="px-3 py-2 bg-gray-100 dark:bg-gray-600 text-gray-600 dark:text-gray-400 text-xs sm:text-sm shrink-0 border-t sm:border-t-0 sm:border-l border-gray-200 dark:border-gray-500 truncate">
                      {{ selectedMeta?.key === 'VTEX' ? '.vtexcommercestable.com.br' : '.myshopify.com' }}
                    </span>
                  </div>
                  <div
                    v-else-if="setting.type === 'radio' && (setting.options?.length ?? 0) > 0"
                    class="flex flex-col gap-2"
                  >
                    <label
                      v-for="opt in setting.options"
                      :key="opt.value"
                      class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                      :class="[
                        connectForm.settings[setting.key] === opt.value ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20' : 'border-gray-300 dark:border-gray-600',
                        isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? '!border-red-500 dark:!border-red-500' : ''
                      ]"
                    >
                      <input
                        v-model="connectForm.settings[setting.key]"
                        type="radio"
                        :value="opt.value"
                        :required="setting.required"
                        :disabled="setting.readonly"
                        class="w-4 h-4 text-primary-600"
                        @blur="setConnectTouched(setting.key)"
                      />
                      <span class="font-medium text-gray-900 dark:text-white">{{ opt.label || opt.value }}</span>
                    </label>
                  </div>
                  <select
                    v-else-if="(setting.type === 'select' || setting.type === 'list') && (setting.options?.length ?? 0) > 0"
                    v-model="connectForm.settings[setting.key]"
                    :required="setting.required"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                    ]"
                    @blur="setConnectTouched(setting.key)"
                  >
                    <option value="">-- Seleccionar --</option>
                    <option
                      v-for="opt in setting.options"
                      :key="opt.value"
                      :value="opt.value"
                    >
                      {{ opt.label || opt.value }}
                    </option>
                  </select>
                  <template v-else-if="setting.type === 'textarea'">
                    <textarea
                      v-model="connectForm.settings[setting.key]"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      rows="6"
                      :class="[
                        'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 font-mono text-sm',
                        isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                      ]"
                      @blur="setConnectTouched(setting.key)"
                    />
                  </template>
                  <input
                    v-else
                    v-model="connectForm.settings[setting.key]"
                    :type="getInputType(setting)"
                    :required="setting.required"
                    :placeholder="setting.placeholder"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500',
                      setting.type === 'password' ? 'pr-10' : ''
                    ]"
                    @blur="setConnectTouched(setting.key)"
                  />
                  <button
                    v-if="setting.type === 'password'"
                    type="button"
                    @click="togglePasswordVisibility(setting.key)"
                    class="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 rounded"
                    :title="passwordVisible[setting.key] ? 'Ocultar' : 'Mostrar'"
                    aria-label="Mostrar u ocultar credencial"
                  >
                    <svg v-if="passwordVisible[setting.key]" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                    <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  </button>
                </div>
                <p v-if="setting.help" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ setting.help }}</p>
                <p v-if="isMcpConnect && setting.key === 'mcpServerUrl'" class="mt-2 text-xs text-amber-600 dark:text-amber-400">
                  Solo conecte a servidores de confianza. Usted es responsable de las acciones realizadas con esta conexión y de mantener sus herramientas actualizadas.
                </p>
              </div>
              </template>

              <!-- Custom API Actions (connect) -->
              <div v-if="isCustomApiConnect" class="space-y-4">
                <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
                  <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50">
                    <h4 class="text-sm font-semibold text-gray-900 dark:text-white">Acciones personalizadas</h4>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                      Defina las acciones que la API expone. Cada acción tiene un nombre, método HTTP y ruta.
                    </p>
                  </div>
                  <div class="p-4 space-y-4">
                    <div
                      v-for="(action, idx) in customActionsForm"
                      :key="idx"
                      class="flex flex-col gap-3 p-4 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50"
                    >
                      <div class="flex items-center justify-between">
                        <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Acción {{ idx + 1 }}</span>
                        <button
                          type="button"
                          @click="removeCustomAction('connect', idx)"
                          class="p-1.5 text-gray-500 hover:text-red-600 dark:hover:text-red-400 rounded transition-colors"
                          :disabled="customActionsForm.length <= 1"
                          :class="{ 'opacity-50 cursor-not-allowed': customActionsForm.length <= 1 }"
                          title="Eliminar acción"
                        >
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                        </button>
                      </div>
                      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Nombre</label>
                          <input
                            v-model="action.key"
                            type="text"
                            placeholder="ej. getProducts"
                            class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                          />
                        </div>
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Método</label>
                          <select
                            v-model="action.method"
                            class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                          >
                            <option value="GET">GET</option>
                            <option value="POST">POST</option>
                            <option value="PATCH">PATCH</option>
                            <option value="DELETE">DELETE</option>
                          </select>
                        </div>
                      </div>
                      <div>
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Ruta (path)</label>
                        <input
                          v-model="action.path"
                          type="text"
                          placeholder="/api/products"
                          class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                        />
                      </div>
                      <div v-if="action.method === 'POST' || action.method === 'PATCH'">
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Payload (JSON opcional)</label>
                        <textarea
                          v-model="action.body"
                          rows="3"
                          placeholder='{"key": "value"}'
                          class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm font-mono"
                        />
                      </div>
                    </div>
                    <button
                      type="button"
                      @click="addCustomAction('connect')"
                      class="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                      Agregar acción
                    </button>
                  </div>
                </div>
              </div>

              <!-- Custom Headers (connect) -->
              <div v-if="selectedMeta?.hasCustomHeaders" class="space-y-4">
                <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
                  <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50">
                    <h4 class="text-sm font-semibold text-gray-900 dark:text-white">Headers personalizados</h4>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                      Headers HTTP adicionales que se enviarán en cada petición a la API (nombre y valor).
                    </p>
                  </div>
                  <div class="p-4 space-y-4">
                    <div
                      v-for="(row, idx) in customHeadersForm"
                      :key="idx"
                      class="flex flex-wrap items-center gap-3 p-4 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50"
                    >
                      <input
                        v-model="row.key"
                        type="text"
                        placeholder="Nombre (ej. X-API-Key)"
                        class="flex-1 min-w-[120px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                      />
                      <input
                        v-model="row.value"
                        type="text"
                        placeholder="Valor"
                        class="flex-1 min-w-[120px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                      />
                      <button
                        type="button"
                        @click="removeCustomHeader('connect', idx)"
                        class="p-1.5 text-gray-500 hover:text-red-600 dark:hover:text-red-400 rounded transition-colors shrink-0"
                        :disabled="customHeadersForm.length <= 1"
                        :class="{ 'opacity-50 cursor-not-allowed': customHeadersForm.length <= 1 }"
                        title="Eliminar header"
                      >
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                      </button>
                    </div>
                    <button
                      type="button"
                      @click="addCustomHeader('connect')"
                      class="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-dashed border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                      Agregar header
                    </button>
                  </div>
                </div>
              </div>

              <!-- MCP Obtener Herramientas -->
              <div v-if="isMcpConnect" class="flex gap-2">
                <button
                  type="button"
                  :disabled="!connectForm.settings.mcpServerUrl?.trim() || mcpToolsLoading"
                  @click="fetchMcpTools('connect')"
                  class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shadow-sm"
                >
                  <svg v-if="mcpToolsLoading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
                  Obtener Herramientas
                </button>
              </div>
              <div v-if="isMcpConnect && mcpToolsLoading" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/50 shadow-sm">
                <div class="p-4">
                  <div class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Herramientas encontradas</div>
                  <div class="space-y-2 max-h-52 overflow-y-auto pr-1">
                    <div v-for="i in 4" :key="`mcp-skeleton-connect-${i}`" class="flex items-start gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700">
                      <div class="w-5 h-5 rounded bg-gray-200 dark:bg-gray-700 animate-pulse shrink-0 mt-0.5" />
                      <div class="min-w-0 flex-1 space-y-2">
                        <div class="h-4 w-40 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                        <div class="h-3 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else-if="isMcpConnect && mcpTools.length > 0" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/50 shadow-sm">
                <div class="p-4">
                  <div class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Herramientas encontradas ({{ mcpTools.length }})</div>
                  <div class="space-y-2 max-h-52 overflow-y-auto pr-1">
                    <div
                      v-for="t in mcpTools"
                      :key="t.name"
                      class="flex items-start gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700"
                    >
                      <span class="text-primary-500 mt-0.5">⚙</span>
                      <div class="min-w-0 flex-1">
                        <span class="font-mono text-sm font-medium text-gray-900 dark:text-white block">{{ t.name }}</span>
                        <ExpandableDescription v-if="t.description" :text="t.description" :max-length="120" />
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- MCP OAuth hint -->
              <div v-if="isMcpConnect && connectForm.settings.authType === 'oauth'" class="rounded-lg border border-gray-200 dark:border-gray-600 p-4 bg-gray-50 dark:bg-gray-800/50">
                <p class="text-xs text-gray-500 dark:text-gray-400">
                  Client ID y Client Secret son opcionales si el servidor soporta Dynamic Client Registration (DCR). Los endpoints OAuth se descubren automáticamente.
                </p>
              </div>

              <!-- OpenAPI / Postman schema (IntegrationApiForm) -->
              <IntegrationApiForm
                v-if="isOpenApiConnect || selectedMeta?.key === postmanProviderKey"
                :meta="selectedMeta"
                v-model:model-value="connectForm.settings"
                mode="connect"
                :open-api-parsed="openApiParsed"
                :postman-parsed="postmanParsed"
                :open-api-validation-error="openApiValidationError"
                :open-api-schema-loading="openApiSchemaLoading"
                :postman-collection-loading="postmanCollectionLoading"
                :inline-mode="inlineSchemaModeConnect"
                :inline-file-loading="inlineFileLoading"
                :touched="connectSettingsTouched"
                :is-field-invalid="(key, setting, value) => isConnectFieldInvalid(key, setting, value)"
                @update:inline-mode="inlineSchemaModeConnect = $event"
                @fetch-openapi="fetchOpenApiActions('connect')"
                @fetch-postman="fetchPostmanActions('connect')"
                @request-file-load="({ formType, key }) => triggerInlineFileLoad(formType, key)"
                @request-file-drop="(ev, key) => onInlineFileDrop(ev, 'connect', key)"
                @copy-openapi="copyOpenApiSchema('connect')"
                @format-openapi="formatAndValidateOpenApiSchema('connect')"
                @blur-field="setConnectTouched"
              />

              <!-- Base URL y Auth (después del schema) -->
              <template v-for="setting in getConnectSettingsForPhase2()" :key="setting.key">
              <div v-if="isOpenApiSettingVisible(setting, connectForm.settings, selectedMeta)">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ setting.label }}
                  <span v-if="setting.required" class="text-red-500">*</span>
                </label>
                <div class="relative">
                  <div
                    v-if="(selectedMeta?.key === 'VTEX' || selectedMeta?.key === 'Shopify') && setting.key === 'accountName'"
                    class="flex flex-col sm:flex-row rounded-lg border overflow-hidden"
                    :class="isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500' : 'border-gray-300 dark:border-gray-600'"
                  >
                    <input
                      v-model="connectForm.settings[setting.key]"
                      type="text"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      class="flex-1 min-w-0 px-3 py-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 border-0 focus:border-0 rounded-none"
                      @blur="setConnectTouched(setting.key)"
                    />
                    <span class="px-3 py-2 bg-gray-100 dark:bg-gray-600 text-gray-600 dark:text-gray-400 text-xs sm:text-sm shrink-0 border-t sm:border-t-0 sm:border-l border-gray-200 dark:border-gray-500 truncate">
                      {{ selectedMeta?.key === 'VTEX' ? '.vtexcommercestable.com.br' : '.myshopify.com' }}
                    </span>
                  </div>
                  <div
                    v-else-if="setting.type === 'radio' && (setting.options?.length ?? 0) > 0"
                    class="flex flex-col gap-2"
                  >
                    <label
                      v-for="opt in setting.options"
                      :key="opt.value"
                      class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                      :class="[
                        connectForm.settings[setting.key] === opt.value ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20' : 'border-gray-300 dark:border-gray-600',
                        isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? '!border-red-500 dark:!border-red-500' : ''
                      ]"
                    >
                      <input
                        v-model="connectForm.settings[setting.key]"
                        type="radio"
                        :value="opt.value"
                        :required="setting.required"
                        :disabled="setting.readonly"
                        class="w-4 h-4 text-primary-600"
                        @blur="setConnectTouched(setting.key)"
                      />
                      <span class="font-medium text-gray-900 dark:text-white">{{ opt.label || opt.value }}</span>
                    </label>
                  </div>
                  <select
                    v-else-if="(setting.type === 'select' || setting.type === 'list') && (setting.options?.length ?? 0) > 0"
                    v-model="connectForm.settings[setting.key]"
                    :required="setting.required"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                    ]"
                    @blur="setConnectTouched(setting.key)"
                  >
                    <option value="">-- Seleccionar --</option>
                    <option
                      v-for="opt in setting.options"
                      :key="opt.value"
                      :value="opt.value"
                    >
                      {{ opt.label || opt.value }}
                    </option>
                  </select>
                  <template v-else-if="setting.type === 'textarea'">
                    <textarea
                      v-model="connectForm.settings[setting.key]"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      rows="6"
                      :class="[
                        'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 font-mono text-sm',
                        isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                      ]"
                      @blur="setConnectTouched(setting.key)"
                    />
                  </template>
                  <input
                    v-else
                    v-model="connectForm.settings[setting.key]"
                    :type="getInputType(setting)"
                    :required="setting.required"
                    :placeholder="setting.placeholder"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isConnectFieldInvalid(setting.key, setting, connectForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500',
                      setting.type === 'password' ? 'pr-10' : ''
                    ]"
                    @blur="setConnectTouched(setting.key)"
                  />
                  <button
                    v-if="setting.type === 'password'"
                    type="button"
                    @click="togglePasswordVisibility(setting.key)"
                    class="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 text-gray-500 hover:text-gray-700 dark:hover:text-gray-400 dark:hover:text-gray-300 rounded"
                    :title="passwordVisible[setting.key] ? 'Ocultar' : 'Mostrar'"
                    aria-label="Mostrar u ocultar credencial"
                  >
                    <svg v-if="passwordVisible[setting.key]" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                    <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  </button>
                </div>
                <p v-if="setting.help" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ setting.help }}</p>
              </div>
              </template>
            </div>
            <div class="shrink-0 px-4 sm:px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-2 sm:gap-2">
                <button
                  type="button"
                  @click="closeConnectModal"
                  class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  :disabled="!isConnectFormValid || saving"
                  class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {{ saving ? (isMcpConnect ? 'Agregando...' : 'Conectando...') : (isMcpConnect ? 'Agregar servidor' : (isChannelConnect ? 'Conectar canal' : (isSupportConnect ? 'Conectar herramienta' : 'Conectar'))) }}
                </button>
            </div>
          </form>
        </div>
      </Transition>
    </Teleport>

    <!-- Slider Editar credenciales -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="editCredentialsModalOpen"
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
          v-if="editCredentialsModalOpen"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-full sm:max-w-md lg:max-w-2xl bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 flex items-start justify-between shrink-0 gap-3">
            <div class="min-w-0 flex-1">
              <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white truncate">
                Editar credenciales — {{ editIntegration?.name }}
              </h3>
              <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Actualiza la URL, API Key u otras credenciales si te equivocaste al configurar
              </p>
            </div>
            <button
              type="button"
              @click="closeEditCredentialsModal"
              class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
              aria-label="Cerrar"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <form @submit.prevent="submitEditCredentials" class="flex flex-col flex-1 min-h-0">
            <div class="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
              <!-- Skeleton mientras se cargan las credenciales (GET por id) -->
              <template v-if="loadingEditId === editIntegration?.id">
                <div class="space-y-4" aria-busy="true" aria-label="Cargando formulario">
                  <div v-for="i in 6" :key="i" class="space-y-2">
                    <div class="h-4 w-24 rounded bg-gray-200 dark:bg-gray-600 animate-pulse" />
                    <div class="h-10 w-full rounded-lg bg-gray-200 dark:bg-gray-600 animate-pulse" />
                  </div>
                  <div class="pt-2 flex gap-2">
                    <div class="h-10 flex-1 rounded-lg bg-gray-200 dark:bg-gray-600 animate-pulse" />
                    <div class="h-10 w-24 rounded-lg bg-gray-200 dark:bg-gray-600 animate-pulse" />
                  </div>
                </div>
              </template>
              <template v-else>
              <template v-for="setting in getEditSettingsPhase1OrAll()" :key="setting.key">
              <div v-if="isOpenApiSettingVisible(setting, editForm.settings, editMeta)">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ setting.label }}
                  <span v-if="setting.required" class="text-red-500">*</span>
                </label>
                <div class="relative">
                  <div
                    v-if="(editMeta?.key === 'VTEX' || editMeta?.key === 'Shopify') && setting.key === 'accountName'"
                    class="flex flex-col sm:flex-row rounded-lg border overflow-hidden"
                    :class="isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500' : 'border-gray-300 dark:border-gray-600'"
                  >
                    <input
                      v-model="editForm.settings[setting.key]"
                      type="text"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      class="flex-1 min-w-0 px-3 py-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 border-0 focus:border-0 rounded-none"
                      @blur="setEditTouched(setting.key)"
                    />
                    <span class="px-3 py-2 bg-gray-100 dark:bg-gray-600 text-gray-600 dark:text-gray-400 text-xs sm:text-sm shrink-0 border-t sm:border-t-0 sm:border-l border-gray-200 dark:border-gray-500 truncate">
                      {{ editMeta?.key === 'VTEX' ? '.vtexcommercestable.com.br' : '.myshopify.com' }}
                    </span>
                  </div>
                  <div
                    v-else-if="setting.type === 'radio' && (setting.options?.length ?? 0) > 0"
                    class="flex flex-col gap-2"
                  >
                    <label
                      v-for="opt in setting.options"
                      :key="opt.value"
                      class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                      :class="[
                        editForm.settings[setting.key] === opt.value ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20' : 'border-gray-300 dark:border-gray-600',
                        isEditFieldInvalid(setting, editForm.settings[setting.key]) ? '!border-red-500 dark:!border-red-500' : ''
                      ]"
                    >
                      <input
                        v-model="editForm.settings[setting.key]"
                        type="radio"
                        :value="opt.value"
                        :required="setting.required"
                        :disabled="setting.readonly"
                        class="w-4 h-4 text-primary-600"
                        @blur="setEditTouched(setting.key)"
                      />
                      <span class="font-medium text-gray-900 dark:text-white">{{ opt.label || opt.value }}</span>
                    </label>
                  </div>
                  <select
                    v-else-if="(setting.type === 'select' || setting.type === 'list') && (setting.options?.length ?? 0) > 0"
                    v-model="editForm.settings[setting.key]"
                    :required="setting.required"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                    ]"
                    @blur="setEditTouched(setting.key)"
                  >
                    <option value="">-- Seleccionar --</option>
                    <option
                      v-for="opt in setting.options"
                      :key="opt.value"
                      :value="opt.value"
                    >
                      {{ opt.label || opt.value }}
                    </option>
                  </select>
                  <template v-else-if="setting.type === 'textarea'">
                    <textarea
                      v-model="editForm.settings[setting.key]"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      rows="6"
                      :class="[
                        'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 font-mono text-sm',
                        isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                      ]"
                      @blur="setEditTouched(setting.key)"
                    />
                  </template>
                  <input
                    v-else
                    v-model="editForm.settings[setting.key]"
                    :type="getInputType(setting)"
                    :required="setting.required"
                    :placeholder="setting.placeholder"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500',
                      setting.type === 'password' ? 'pr-10' : ''
                    ]"
                    @blur="setEditTouched(setting.key)"
                  />
                  <button
                    v-if="setting.type === 'password'"
                    type="button"
                    @click="togglePasswordVisibility(setting.key)"
                    class="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 rounded"
                    :title="passwordVisible[setting.key] ? 'Ocultar' : 'Mostrar'"
                    aria-label="Mostrar u ocultar credencial"
                  >
                    <svg v-if="passwordVisible[setting.key]" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                    <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  </button>
                </div>
                <p v-if="setting.help" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ setting.help }}</p>
                <p v-if="isMcpEdit && setting.key === 'mcpServerUrl'" class="mt-2 text-xs text-amber-600 dark:text-amber-400">
                  Solo conecte a servidores de confianza. Usted es responsable de las acciones realizadas con esta conexión.
                </p>
              </div>
              </template>

              <!-- Custom API Actions (edit) -->
              <div v-if="isCustomApiEdit" class="space-y-4">
                <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
                  <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50">
                    <h4 class="text-sm font-semibold text-gray-900 dark:text-white">Acciones personalizadas</h4>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                      Defina las acciones que la API expone. Cada acción tiene un nombre, método HTTP y ruta.
                    </p>
                  </div>
                  <div class="p-4 space-y-4">
                    <div
                      v-for="(action, idx) in editCustomActionsForm"
                      :key="idx"
                      class="flex flex-col gap-3 p-4 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50"
                    >
                      <div class="flex items-center justify-between">
                        <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Acción {{ idx + 1 }}</span>
                        <button
                          type="button"
                          @click="removeCustomAction('edit', idx)"
                          class="p-1.5 text-gray-500 hover:text-red-600 dark:hover:text-red-400 rounded transition-colors"
                          :disabled="editCustomActionsForm.length <= 1"
                          :class="{ 'opacity-50 cursor-not-allowed': editCustomActionsForm.length <= 1 }"
                          title="Eliminar acción"
                        >
                          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                        </button>
                      </div>
                      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Nombre</label>
                          <input
                            v-model="action.key"
                            type="text"
                            placeholder="ej. getProducts"
                            class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                          />
                        </div>
                        <div>
                          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Método</label>
                          <select
                            v-model="action.method"
                            class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                          >
                            <option value="GET">GET</option>
                            <option value="POST">POST</option>
                            <option value="PATCH">PATCH</option>
                            <option value="DELETE">DELETE</option>
                          </select>
                        </div>
                      </div>
                      <div>
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Ruta (path)</label>
                        <input
                          v-model="action.path"
                          type="text"
                          placeholder="/api/products"
                          class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                        />
                      </div>
                      <div v-if="action.method === 'POST' || action.method === 'PATCH'">
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Payload (JSON opcional)</label>
                        <textarea
                          v-model="action.body"
                          rows="3"
                          placeholder='{"key": "value"}'
                          class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm font-mono"
                        />
                      </div>
                    </div>
                    <button
                      type="button"
                      @click="addCustomAction('edit')"
                      class="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                      Agregar acción
                    </button>
                  </div>
                </div>
              </div>

              <!-- Custom Headers (edit) -->
              <div v-if="editMeta?.hasCustomHeaders" class="space-y-4">
                <div class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-gray-50/50 dark:bg-gray-800/30">
                  <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50">
                    <h4 class="text-sm font-semibold text-gray-900 dark:text-white">Headers personalizados</h4>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                      Headers HTTP adicionales que se enviarán en cada petición a la API (nombre y valor).
                    </p>
                  </div>
                  <div class="p-4 space-y-4">
                    <div
                      v-for="(row, idx) in editCustomHeadersForm"
                      :key="idx"
                      class="flex flex-wrap items-center gap-3 p-4 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800/50"
                    >
                      <input
                        v-model="row.key"
                        type="text"
                        placeholder="Nombre (ej. X-API-Key)"
                        class="flex-1 min-w-[120px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                      />
                      <input
                        v-model="row.value"
                        type="text"
                        placeholder="Valor"
                        class="flex-1 min-w-[120px] px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 text-sm"
                      />
                      <button
                        type="button"
                        @click="removeCustomHeader('edit', idx)"
                        class="p-1.5 text-gray-500 hover:text-red-600 dark:hover:text-red-400 rounded transition-colors shrink-0"
                        :disabled="editCustomHeadersForm.length <= 1"
                        :class="{ 'opacity-50 cursor-not-allowed': editCustomHeadersForm.length <= 1 }"
                        title="Eliminar header"
                      >
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                      </button>
                    </div>
                    <button
                      type="button"
                      @click="addCustomHeader('edit')"
                      class="inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-dashed border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-primary-500 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors"
                    >
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                      Agregar header
                    </button>
                  </div>
                </div>
              </div>

              <!-- MCP Obtener Herramientas (edit) -->
              <div v-if="isMcpEdit" class="flex gap-2">
                <button
                  type="button"
                  :disabled="!editForm.settings.mcpServerUrl?.trim() || mcpToolsLoading"
                  @click="fetchMcpTools('edit')"
                  class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium transition-colors shadow-sm"
                >
                  <svg v-if="mcpToolsLoading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                  </svg>
                  <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" /><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
                  Obtener Herramientas
                </button>
              </div>
              <div v-if="isMcpEdit && mcpToolsLoading" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/50 shadow-sm">
                <div class="p-4">
                  <div class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Herramientas encontradas</div>
                  <div class="space-y-2 max-h-52 overflow-y-auto pr-1">
                    <div v-for="i in 4" :key="`mcp-skeleton-edit-${i}`" class="flex items-start gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700">
                      <div class="w-5 h-5 rounded bg-gray-200 dark:bg-gray-700 animate-pulse shrink-0 mt-0.5" />
                      <div class="min-w-0 flex-1 space-y-2">
                        <div class="h-4 w-40 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                        <div class="h-3 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else-if="isMcpEdit && mcpTools.length > 0" class="rounded-xl border border-gray-200 dark:border-gray-600 overflow-hidden bg-white dark:bg-gray-800/50 shadow-sm">
                <div class="p-4">
                  <div class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Herramientas encontradas ({{ mcpTools.length }})</div>
                  <div class="space-y-2 max-h-52 overflow-y-auto pr-1">
                    <div v-for="t in mcpTools" :key="t.name" class="flex items-start gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700">
                      <span class="text-primary-500 mt-0.5">⚙</span>
                      <div class="min-w-0 flex-1">
                        <span class="font-mono text-sm font-medium text-gray-900 dark:text-white block">{{ t.name }}</span>
                        <ExpandableDescription v-if="t.description" :text="t.description" :max-length="120" />
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- OpenAPI / Postman schema (IntegrationApiForm) -->
              <IntegrationApiForm
                v-if="isOpenApiEdit || editMeta?.key === postmanProviderKey"
                :meta="editMeta"
                v-model:model-value="editForm.settings"
                mode="edit"
                :open-api-parsed="openApiParsed"
                :postman-parsed="postmanParsed"
                :open-api-validation-error="openApiValidationError"
                :open-api-schema-loading="openApiSchemaLoading"
                :postman-collection-loading="postmanCollectionLoading"
                :inline-mode="inlineSchemaModeEdit"
                :inline-file-loading="inlineFileLoading"
                :touched="editSettingsTouched"
                :is-field-invalid="(key, setting, value) => isEditFieldInvalid(setting, value)"
                @update:inline-mode="inlineSchemaModeEdit = $event"
                @fetch-openapi="fetchOpenApiActions('edit')"
                @fetch-postman="fetchPostmanActions('edit')"
                @request-file-load="({ formType, key }) => triggerInlineFileLoad(formType, key)"
                @request-file-drop="(ev, key) => onInlineFileDrop(ev, 'edit', key)"
                @copy-openapi="copyOpenApiSchema('edit')"
                @format-openapi="formatAndValidateOpenApiSchema('edit')"
                @blur-field="setEditTouched"
              />

              <!-- Base URL y Auth (después del schema) -->
              <template v-for="setting in getEditSettingsPhase2()" :key="setting.key">
              <div v-if="isOpenApiSettingVisible(setting, editForm.settings, editMeta)">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ setting.label }}
                  <span v-if="setting.required" class="text-red-500">*</span>
                </label>
                <div class="relative">
                  <div
                    v-if="(editMeta?.key === 'VTEX' || editMeta?.key === 'Shopify') && setting.key === 'accountName'"
                    class="flex flex-col sm:flex-row rounded-lg border overflow-hidden"
                    :class="isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500' : 'border-gray-300 dark:border-gray-600'"
                  >
                    <input
                      v-model="editForm.settings[setting.key]"
                      type="text"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      class="flex-1 min-w-0 px-3 py-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 border-0 focus:border-0 rounded-none"
                      @blur="setEditTouched(setting.key)"
                    />
                    <span class="px-3 py-2 bg-gray-100 dark:bg-gray-600 text-gray-600 dark:text-gray-400 text-xs sm:text-sm shrink-0 border-t sm:border-t-0 sm:border-l border-gray-200 dark:border-gray-500 truncate">
                      {{ editMeta?.key === 'VTEX' ? '.vtexcommercestable.com.br' : '.myshopify.com' }}
                    </span>
                  </div>
                  <div
                    v-else-if="setting.type === 'radio' && (setting.options?.length ?? 0) > 0"
                    class="flex flex-col gap-2"
                  >
                    <label
                      v-for="opt in setting.options"
                      :key="opt.value"
                      class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                      :class="[
                        editForm.settings[setting.key] === opt.value ? 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20' : 'border-gray-300 dark:border-gray-600',
                        isEditFieldInvalid(setting, editForm.settings[setting.key]) ? '!border-red-500 dark:!border-red-500' : ''
                      ]"
                    >
                      <input
                        v-model="editForm.settings[setting.key]"
                        type="radio"
                        :value="opt.value"
                        :required="setting.required"
                        :disabled="setting.readonly"
                        class="w-4 h-4 text-primary-600"
                        @blur="setEditTouched(setting.key)"
                      />
                      <span class="font-medium text-gray-900 dark:text-white">{{ opt.label || opt.value }}</span>
                    </label>
                  </div>
                  <select
                    v-else-if="(setting.type === 'select' || setting.type === 'list') && (setting.options?.length ?? 0) > 0"
                    v-model="editForm.settings[setting.key]"
                    :required="setting.required"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                    ]"
                    @blur="setEditTouched(setting.key)"
                  >
                    <option value="">-- Seleccionar --</option>
                    <option
                      v-for="opt in setting.options"
                      :key="opt.value"
                      :value="opt.value"
                    >
                      {{ opt.label || opt.value }}
                    </option>
                  </select>
                  <template v-else-if="setting.type === 'textarea'">
                    <textarea
                      v-model="editForm.settings[setting.key]"
                      :required="setting.required"
                      :placeholder="setting.placeholder"
                      :disabled="setting.readonly"
                      rows="6"
                      :class="[
                        'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 font-mono text-sm',
                        isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500'
                      ]"
                      @blur="setEditTouched(setting.key)"
                    />
                  </template>
                  <input
                    v-else
                    v-model="editForm.settings[setting.key]"
                    :type="getInputType(setting)"
                    :required="setting.required"
                    :placeholder="setting.placeholder"
                    :disabled="setting.readonly"
                    :class="[
                      'w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2',
                      isEditFieldInvalid(setting, editForm.settings[setting.key]) ? 'border-red-500 dark:border-red-500 focus:ring-red-500' : 'border-gray-300 dark:border-gray-600 focus:ring-primary-500',
                      setting.type === 'password' ? 'pr-10' : ''
                    ]"
                    @blur="setEditTouched(setting.key)"
                  />
                  <button
                    v-if="setting.type === 'password'"
                    type="button"
                    @click="togglePasswordVisibility(setting.key)"
                    class="absolute right-2 top-1/2 -translate-y-1/2 p-1.5 text-gray-500 hover:text-gray-700 dark:hover:text-gray-400 dark:hover:text-gray-300 rounded"
                    :title="passwordVisible[setting.key] ? 'Ocultar' : 'Mostrar'"
                    aria-label="Mostrar u ocultar credencial"
                  >
                    <svg v-if="passwordVisible[setting.key]" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                    <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  </button>
                </div>
                <p v-if="setting.help" class="mt-1 text-xs text-gray-500 dark:text-gray-400">{{ setting.help }}</p>
              </div>
              </template>
              </template>
            </div>
            <div class="shrink-0 px-4 sm:px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-2 sm:gap-2">
              <button
                type="button"
                @click="closeEditCredentialsModal"
                class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 disabled:opacity-50"
              >
                Cancelar
              </button>
              <button
                type="submit"
                :disabled="loadingEditId === editIntegration?.id || !isEditFormValid || savingEdit"
                class="w-full sm:w-auto min-h-[44px] touch-manipulation px-4 py-2.5 sm:py-2 rounded-lg bg-primary-600 hover:bg-primary-700 text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {{ savingEdit ? 'Guardando...' : (loadingEditId === editIntegration?.id ? 'Cargando...' : 'Guardar credenciales') }}
              </button>
            </div>
          </form>
        </div>
      </Transition>
    </Teleport>

    <!-- Modal Preview OAuth MCP (Conectar) - Estilo ChatGPT/estándar -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="mcpOAuthPreviewModalOpen"
          :key="mcpOAuthPreviewIntegration?.id ?? 'oauth-preview'"
          class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
        >
          <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 max-w-lg w-full overflow-hidden">
            <!-- Header con X -->
            <div class="relative p-6 pb-4">
              <button
                type="button"
                @click="closeMcpOAuthPreviewModal"
                class="absolute top-4 right-4 p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg transition-colors"
                aria-label="Cerrar"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
              <!-- Logos + título -->
              <div class="flex flex-col items-center text-center">
                <div class="flex items-center justify-center gap-3 mb-3">
                  <div class="w-10 h-10 rounded-lg overflow-hidden flex items-center justify-center shrink-0">
                    <img
                      :src="(platformBrandLogoUrl && !logoFailed(platformBrandLogoUrl)) ? platformBrandLogoUrl : '/images/avatar-default.svg'"
                      alt="Gravity"
                      class="w-full h-full object-cover"
                      @error="() => setLogoFailed(platformBrandLogoUrl)"
                    />
                  </div>
                  <span class="text-gray-400 dark:text-gray-500">⋯</span>
                  <div class="w-10 h-10 rounded-lg bg-gray-200 dark:bg-gray-700 flex items-center justify-center overflow-hidden p-1.5">
                    <img
                      :src="integrationListLogoSrc(mcpOAuthPreviewIntegration)"
                      :alt="mcpOAuthPreviewIntegration?.provider || 'Integration'"
                      class="w-full h-full object-contain"
                      @error="() => onIntegrationListLogoError(mcpOAuthPreviewIntegration)"
                    />
                  </div>
                </div>
                <h3 class="text-xl font-bold text-gray-900 dark:text-white">
                  Conectar {{ mcpOAuthPreviewDisplayName }}
                </h3>
              </div>
            </div>
            <!-- Secciones informativas -->
            <div class="px-6 pb-6 space-y-5">
              <section>
                <h4 class="text-sm font-semibold text-gray-900 dark:text-white mb-1">Permisos siempre respetados</h4>
                <p class="text-sm text-gray-600 dark:text-gray-400">
                  La plataforma está limitada a los permisos que autorices explícitamente. Puedes revocar el acceso en cualquier momento desde esta página.
                </p>
              </section>
              <section>
                <h4 class="text-sm font-semibold text-gray-900 dark:text-white mb-1">Cómo usamos los datos</h4>
                <p class="text-sm text-gray-600 dark:text-gray-400">
                  {{ mcpOAuthPreviewDataUsageText }}
                </p>
              </section>
              <section>
                <h4 class="text-sm font-semibold text-gray-900 dark:text-white mb-1">Los conectores pueden introducir riesgos</h4>
                <p class="text-sm text-gray-600 dark:text-gray-400">
                  {{ mcpOAuthPreviewRisksText }}
                </p>
              </section>
            </div>
            <!-- Botón principal -->
            <div class="px-6 pb-6">
              <button
                type="button"
                @click="confirmMcpOAuthFromPreview"
                :disabled="mcpOAuthLoadingForId === mcpOAuthPreviewIntegration?.id"
                class="w-full inline-flex items-center justify-center gap-2 py-3.5 px-6 rounded-xl font-semibold bg-gray-900 dark:bg-gray-100 hover:bg-gray-800 dark:hover:bg-gray-200 text-white dark:text-gray-900 transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <svg v-if="mcpOAuthLoadingForId === mcpOAuthPreviewIntegration?.id" class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                </svg>
                <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 11V7a4 4 0 118 0m-4 8v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2z" /></svg>
                {{ mcpOAuthLoadingForId === mcpOAuthPreviewIntegration?.id ? 'Redirigiendo...' : `Conectar ${mcpOAuthPreviewDisplayName}` }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
</div>
</template>
<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { storeToRefs } from 'pinia'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { useAppStore } from '../../stores/appStore'
import { useAccountUsage } from '../../composables/useAccountUsage'
import {
  isOpenApiSchemaConfigured as isOpenApiSchemaConfiguredValidation,
  isPostmanCollectionConfigured as isPostmanCollectionConfiguredValidation,
  isApiSettingVisible as isApiSettingVisibleValidation,
  isSettingValueEmpty as isSettingValueEmptyValidation,
  isConnectFormValid as isConnectFormValidFromComposable,
  isEditFormValid as isEditFormValidFromComposable,
  getConnectSettingsPhase1OrAll as getConnectSettingsPhase1OrAllFromComposable,
  getConnectSettingsForPhase2 as getConnectSettingsForPhase2FromComposable,
  getEditSettingsPhase1OrAll as getEditSettingsPhase1OrAllFromComposable,
  getEditSettingsPhase2 as getEditSettingsPhase2FromComposable
} from '../../composables/useIntegrationFormValidation'
import { eventBus, TOUR_PROGRESS_UPDATED, MY_INTEGRATIONS_UPDATED } from '../../utils/eventBus'
import ExpandableDescription from '../../components/ExpandableDescription.vue'
import {
  registerIntegrationConnectEditApi,
  unregisterIntegrationConnectEditApi
} from '../../utils/integrationConnectEditRegistry'
import IntegrationApiForm from './IntegrationApiForm.vue'

const toast = useToast()
const appStore = useAppStore()

async function notifyIntegrationsListUpdated() {
  await appStore.fetchAvailableIntegrations(true)
  const my = await apiService.getMyIntegrations()
  eventBus.emit(MY_INTEGRATIONS_UPDATED, { my })
}

const saving = ref(false)
const { availableIntegrations } = storeToRefs(appStore)
const connectModalOpen = ref(false)
const editCredentialsModalOpen = ref(false)
/** ID of integration whose credentials are being loaded for edit (shows spinner on Editar button). */
const loadingEditId = ref(null)
const selectedMeta = ref(null)
const editIntegration = ref(null)
const editMeta = ref(null)
const savingEdit = ref(false)
const inlineFileInputRef = ref(null)
const inlineFileLoadTarget = ref(null) // { formType: 'connect'|'edit', key: 'openApiSchema'|'postmanCollection' }
const inlineFileLoading = ref(false)
/** 'file' = drop zone, 'textarea' = textarea. For connect/edit, OpenAPI/Postman inline. */
const inlineSchemaModeConnect = ref('file')
const inlineSchemaModeEdit = ref('file')

const connectForm = ref({
  name: '',
  settings: {}
})

const editForm = ref({
  settings: {}
})

const passwordVisible = ref({})
const openApiValidationError = ref('')
const connectTouched = ref({ name: false })
const connectSettingsTouched = ref({})
const editSettingsTouched = ref({})
const openApiParsed = ref(null)
const mcpTools = ref([])
const mcpToolsLoading = ref(false)
const mcpOAuthLoading = ref(false)
const mcpOAuthLoadingForId = ref(null)
const mcpOAuthPreviewModalOpen = ref(false)

/** Providers que soportan webhooks (VTEX, Shopify) */
const mcpOAuthPreviewIntegration = ref(null)
const platformBrandLogoUrl = computed(() => getLogoUrl('Gravity'))

const mcpOAuthPreviewDisplayName = computed(() => {
  const int = mcpOAuthPreviewIntegration.value
  if (!int) return ''
  const meta = availableIntegrations.value?.find(m => m.key === int.provider)
  return meta?.name || int.provider || 'Integración'
})

const mcpOAuthPreviewDataUsageText = computed(() => {
  const int = mcpOAuthPreviewIntegration.value
  if (!int) return ''
  const meta = availableIntegrations.value?.find(m => m.key === int.provider)
  const type = meta?.type ?? 'api'
  const integrationType = meta?.integrationType ?? ''
  let source = 'de la integración'
  if (type === 'mcp') source = 'del servidor MCP'
  else if (integrationType === 'Shops' && !meta?.channelMarketplace) source = 'de la tienda'
  else if (integrationType === 'Shops' && meta?.channelMarketplace) source = 'de la tienda y del canal'
  else if (integrationType === 'Channel' || meta?.channelMarketplace) source = 'del canal'
  else if (integrationType === 'Support') source = 'de la herramienta de soporte'
  else if (type === 'api') source = 'de la API'
  return `Los datos ${source} se usan únicamente para proporcionarte información relevante a través de los agentes. No entrenamos modelos con tu información.`
})

const mcpOAuthPreviewRisksText = computed(() => {
  const int = mcpOAuthPreviewIntegration.value
  if (!int) return 'Solo conecta integraciones en las que confíes.'
  const meta = availableIntegrations.value?.find(m => m.key === int.provider)
  const type = meta?.type ?? 'api'
  const phrase = type === 'mcp'
    ? 'Solo conecta servidores en los que confíes.'
    : 'Solo conecta integraciones en las que confíes.'
  return `Los conectores están diseñados para respetar tu privacidad, pero sitios externos podrían intentar acceder a tus datos. ${phrase}`
})
const openApiSchemaLoading = ref(false)
const postmanCollectionLoading = ref(false)
const postmanParsed = ref(null)
const postmanParsedRef = ref(null)
const customActionsForm = ref([{ key: '', method: 'GET', path: '', body: '' }])
const editCustomActionsForm = ref([{ key: '', method: 'GET', path: '', body: '' }])
const customHeadersForm = ref([{ key: '', value: '' }])
const editCustomHeadersForm = ref([{ key: '', value: '' }])

const isOpenApiConnect = computed(() =>
  selectedMeta.value?.name === 'OpenAPI' || selectedMeta.value?.key === 'Dynamic' || selectedMeta.value?.key === 'CustomAPI'
)
const isOpenApiEdit = computed(() =>
  editMeta.value?.name === 'OpenAPI' || editMeta.value?.key === 'Dynamic' || editMeta.value?.key === 'CustomAPI'
)
const isMcpConnect = computed(() => selectedMeta.value?.type === 'mcp')
const isMcpEdit = computed(() => editMeta.value?.type === 'mcp')
const isChannelConnect = computed(() =>
  selectedMeta.value?.integrationType === 'Channel' || selectedMeta.value?.channelMarketplace === true
)
const isChannelEdit = computed(() =>
  editMeta.value?.integrationType === 'Channel' || editMeta.value?.channelMarketplace === true
)
const isSupportConnect = computed(() => selectedMeta.value?.integrationType === 'Support')
const isSupportEdit = computed(() => editMeta.value?.integrationType === 'Support')
const isCustomApiConnect = computed(() => selectedMeta.value?.key === 'CustomAPI')
const isCustomApiEdit = computed(() => editMeta.value?.key === 'CustomAPI')

/** Keys de proveedores por tab (usa integrationType: Shops, Channel, Support; type: api/mcp para APIs/MCP) */
const storeProviderKeys = computed(() =>
  (availableIntegrations.value ?? [])
    .filter(m => m.integrationType === 'Shops')
    .map(m => m.key)
)
const openApiProviderKey = 'Dynamic'
const customApiProviderKey = 'CustomAPI'
const postmanProviderKey = 'Postman'

// Validación unificada (composable)
function isOpenApiSchemaConfigured(settings) {
  return isOpenApiSchemaConfiguredValidation(settings)
}
function isPostmanCollectionConfigured(settings) {
  return isPostmanCollectionConfiguredValidation(settings)
}
function isOpenApiSettingVisible(setting, settings, meta) {
  return isApiSettingVisibleValidation(setting, settings, meta, postmanProviderKey)
}
function isSettingValueEmpty(setting, value) {
  return isSettingValueEmptyValidation(setting, value)
}
function getConnectSettingsPhase1OrAll() {
  return getConnectSettingsPhase1OrAllFromComposable(selectedMeta.value)
}
function getConnectSettingsForPhase2() {
  return getConnectSettingsForPhase2FromComposable(selectedMeta.value)
}
function getEditSettingsPhase1OrAll() {
  return getEditSettingsPhase1OrAllFromComposable(editMeta.value)
}
function getEditSettingsPhase2() {
  return getEditSettingsPhase2FromComposable(editMeta.value)
}
const mcpProviderKeys = computed(() =>
  (availableIntegrations.value ?? [])
    .filter(m => m.type === 'mcp' && m.integrationType !== 'Support')
    .map(m => m.key)
)
const channelProviderKeys = computed(() =>
  (availableIntegrations.value ?? [])
    .filter(m => m.integrationType === 'Channel' || m.channelMarketplace)
    .map(m => m.key)
)
const supportProviderKeys = computed(() =>
  (availableIntegrations.value ?? [])
    .filter(m => m.integrationType === 'Support')
    .map(m => m.key)
)

/** Providers that support OAuth (Shopify, etc.) */
const oauthProviderKeys = computed(() =>
  (availableIntegrations.value ?? [])
    .filter(m => m.authType === 'oauth')
    .map(m => m.key)
)

/** Show OAuth Conectar button for MCP or OAuth providers (e.g. Shopify) */
const oauthConnectProviderKeys = computed(() =>
  [...new Set([...mcpProviderKeys.value, ...oauthProviderKeys.value])]
)

/** OAuth conectado: backend sets mcpOAuthConnected for all OAuth providers (MCP, Shopify) */
function isOAuthConnected(int) {
  if (!int || !oauthConnectProviderKeys.value.includes(int.provider)) return false
  return int.mcpoAuthConnected === true || int.mcpOAuthConnected === true
}

/** Proveedor que usa OAuth */
function isOAuthProvider(providerKey) {
  const meta = availableIntegrations.value?.find(m => m.key === providerKey)
  return meta?.authType === 'oauth'
}

const failedLogos = ref([])

function setLogoFailed(url) {
  if (url && !failedLogos.value.includes(url)) {
    failedLogos.value = [...failedLogos.value, url]
  }
}

function logoFailed(url) {
  return url && failedLogos.value.includes(url)
}

function getLogoUrl(providerKey) {
  if (providerKey == null || providerKey === '') return null
  const k = String(providerKey).trim().toLowerCase()
  const meta = availableIntegrations.value.find(m => (m.key || '').toLowerCase() === k)
  return meta?.logoUrl ?? null
}

/** Prefer logo from GET /integrations (list item); fallback to catálogo disponible. */
function integrationListLogoSrc(int) {
  if (!int) return '/images/avatar-default.svg'
  const url = int.logoUrl ?? int.LogoUrl ?? getLogoUrl(int.provider)
  if (!url || logoFailed(url)) return '/images/avatar-default.svg'
  return url
}

function onIntegrationListLogoError(int) {
  const url = int?.logoUrl ?? int?.LogoUrl ?? getLogoUrl(int?.provider)
  if (url) setLogoFailed(url)
}

function getInputType(setting) {
  if (setting.type === 'password')
    return passwordVisible.value[setting.key] ? 'text' : 'password'
  return setting.type === 'url' ? 'url' : 'text'
}

function isConnectFieldInvalid(field, setting, value) {
  if (field === 'name') return connectTouched.value.name && !(connectForm.value.name ?? '').trim()
  const touched = connectSettingsTouched.value[setting?.key ?? field]
  return touched && setting?.required && isSettingValueEmpty(setting, value)
}

function isEditFieldInvalid(setting, value) {
  const touched = editSettingsTouched.value[setting?.key]
  return touched && setting?.required && isSettingValueEmpty(setting, value)
}

function setConnectTouched(field) {
  if (field === 'name') connectTouched.value.name = true
  else connectSettingsTouched.value = { ...connectSettingsTouched.value, [field]: true }
}

function setEditTouched(field) {
  editSettingsTouched.value = { ...editSettingsTouched.value, [field]: true }
}

const ALLOWED_INLINE_FILE_EXT = ['.json']

function triggerInlineFileLoad(formType, key) {
  inlineFileLoadTarget.value = { formType, key }
  nextTick(() => inlineFileInputRef.value?.click())
}

async function onInlineFileLoaded(event) {
  const target = inlineFileLoadTarget.value
  if (!target) return
  const file = event.target?.files?.[0]
  if (!file) return
  event.target.value = ''
  inlineFileLoadTarget.value = null

  const ext = '.' + (file.name.split('.').pop() ?? '').toLowerCase()
  if (!ALLOWED_INLINE_FILE_EXT.includes(ext)) {
    toast.error('Solo se permiten archivos .json')
    return
  }

  inlineFileLoading.value = true
  try {
    const text = await file.text()
    const form = target.formType === 'connect' ? connectForm.value : editForm.value
    if (!form.settings) form.settings = {}
    form.settings[target.key] = text
    if (target.formType === 'connect') {
      setConnectTouched(target.key)
      inlineSchemaModeConnect.value = 'textarea'
    } else {
      setEditTouched(target.key)
      inlineSchemaModeEdit.value = 'textarea'
    }
    toast.success('Archivo cargado correctamente')
  } catch (e) {
    toast.error(e.message || 'Error al leer el archivo')
  } finally {
    inlineFileLoading.value = false
  }
}

async function onInlineFileDrop(event, formType, key) {
  event.preventDefault()
  event.stopPropagation()
  const file = event.dataTransfer?.files?.[0]
  if (!file) return
  const ext = '.' + (file.name.split('.').pop() ?? '').toLowerCase()
  if (!ALLOWED_INLINE_FILE_EXT.includes(ext)) {
    toast.error('Solo se permiten archivos .json')
    return
  }
  inlineFileLoading.value = true
  try {
    const text = await file.text()
    const form = formType === 'connect' ? connectForm.value : editForm.value
    if (!form.settings) form.settings = {}
    form.settings[key] = text
    if (formType === 'connect') {
      setConnectTouched(key)
      inlineSchemaModeConnect.value = 'textarea'
    } else {
      setEditTouched(key)
      inlineSchemaModeEdit.value = 'textarea'
    }
    toast.success('Archivo cargado correctamente')
  } catch (e) {
    toast.error(e.message || 'Error al leer el archivo')
  } finally {
    inlineFileLoading.value = false
  }
}

function onInlineFileDragOver(event) {
  event.preventDefault()
  event.stopPropagation()
  event.dataTransfer.dropEffect = 'copy'
}

function addCustomAction(formType) {
  const arr = formType === 'connect' ? customActionsForm : editCustomActionsForm
  arr.value = [...arr.value, { key: '', method: 'GET', path: '', body: '' }]
}

function removeCustomAction(formType, idx) {
  const arr = formType === 'connect' ? customActionsForm : editCustomActionsForm
  if (arr.value.length <= 1) return
  arr.value = arr.value.filter((_, i) => i !== idx)
}

function addCustomHeader(formType) {
  const arr = formType === 'connect' ? customHeadersForm : editCustomHeadersForm
  arr.value = [...arr.value, { key: '', value: '' }]
}

function removeCustomHeader(formType, idx) {
  const arr = formType === 'connect' ? customHeadersForm : editCustomHeadersForm
  if (arr.value.length <= 1) return
  arr.value = arr.value.filter((_, i) => i !== idx)
}

const isConnectFormValid = computed(() => {
  if (!connectForm.value.name?.trim()) return false
  return isConnectFormValidFromComposable(
    connectForm.value.settings ?? {},
    selectedMeta.value,
    customActionsForm.value,
    postmanProviderKey
  )
})

const isEditFormValid = computed(() => {
  return isEditFormValidFromComposable(
    editForm.value.settings ?? {},
    editMeta.value,
    editCustomActionsForm.value,
    postmanProviderKey
  )
})

function getSchemaForForm(formType) {
  return formType === 'connect' ? connectForm.value.settings : editForm.value.settings
}

function parseOpenApiSchema(jsonStr) {
  openApiValidationError.value = ''
  if (!jsonStr?.trim()) return null
  try {
    const parsed = JSON.parse(jsonStr)
    if (!parsed.openapi && !parsed.swagger) {
      openApiValidationError.value = 'El JSON debe ser un schema OpenAPI válido (debe incluir "openapi" o "swagger")'
      return null
    }
    return parsed
  } catch (e) {
    openApiValidationError.value = `JSON inválido: ${e.message}`
    return null
  }
}

function extractOpenApiActions(schema) {
  if (!schema?.paths) return []
  const actions = []
  for (const [path, pathItem] of Object.entries(schema.paths)) {
    const methods = ['get', 'post', 'put', 'patch', 'delete', 'options', 'head']
    for (const m of methods) {
      const op = pathItem[m]
      if (op) {
        actions.push({
          path,
          method: m.toUpperCase(),
          operationId: op.operationId,
          summary: op.summary || op.description || path
        })
      }
    }
  }
  return actions
}

function copyToClipboard(text) {
  if (!text) return
  navigator.clipboard.writeText(text).then(() => toast.success('Copiado al portapapeles'))
}

function copyOpenApiSchema(formType) {
  const settings = getSchemaForForm(formType)
  const schema = settings.openApiSchema
  if (!schema) {
    toast.info('No hay schema para copiar')
    return
  }
  copyToClipboard(schema)
}

function applyServerUrlToBaseUrl(settings, servers) {
  const url = servers?.[0]?.url?.trim()
  if (url) {
    settings.baseUrl = url
  }
}

function formatAndValidateOpenApiSchema(formType) {
  const settings = getSchemaForForm(formType)
  const schemaStr = settings.openApiSchema
  const parsed = parseOpenApiSchema(schemaStr)
  if (!parsed) return
  try {
    settings.openApiSchema = JSON.stringify(parsed, null, 2)
    openApiParsed.value = {
      info: parsed.info || {},
      servers: parsed.servers || [],
      actions: extractOpenApiActions(parsed)
    }
    applyServerUrlToBaseUrl(settings, parsed.servers)
    toast.success('JSON válido y formateado')
  } catch {
    toast.error(openApiValidationError.value)
  }
}

async function fetchOpenApiActions(formType) {
  const settings = getSchemaForForm(formType)
  let schema = null

  if (settings.schemaSource === 'url') {
    const url = settings.openApiSchemaUrl?.trim()
    if (!url) {
      toast.error('Ingrese la URL del schema OpenAPI')
      return
    }
    openApiSchemaLoading.value = true
    openApiValidationError.value = ''
    try {
      const data = await apiService.fetchOpenApiSchema(url)
      const text = data?.schema ?? ''
      schema = parseOpenApiSchema(text)
      if (!schema) return
      // Persistir el schema en el formulario para que al guardar (crear/editar) quede guardado y no se rompa Agregar acción
      settings.openApiSchema = text
    } catch (e) {
      const msg = e.response?.data?.error || e.message || 'Error al cargar'
      openApiValidationError.value = msg
      toast.error(msg)
      return
    } finally {
      openApiSchemaLoading.value = false
    }
  } else {
    schema = parseOpenApiSchema(settings.openApiSchema)
    if (!schema) {
      toast.error(openApiValidationError.value)
      return
    }
  }

  openApiParsed.value = {
    info: schema.info || {},
    servers: schema.servers || [],
    actions: extractOpenApiActions(schema)
  }
  applyServerUrlToBaseUrl(settings, schema.servers)
  toast.success(`Se encontraron ${openApiParsed.value.actions.length} acciones`)
}

function extractPostmanActions(collection) {
  if (!collection?.item) return []
  const actions = []
  function collect(items, prefix = '') {
    if (!Array.isArray(items)) return
    items.forEach((item, idx) => {
      const name = item?.name ?? `item_${idx}`
      if (item?.request) {
        const method = (item.request?.method ?? 'GET').toUpperCase()
        let path = '/'
        const url = item.request?.url
        if (url) {
          if (typeof url === 'string') {
            try {
              path = new URL(url).pathname || '/'
            } catch {
              path = '/'
            }
          } else if (url?.path) {
            path = '/' + (Array.isArray(url.path) ? url.path.join('/') : String(url.path || ''))
          } else if (url?.raw) {
            try {
              path = new URL(url.raw).pathname || '/'
            } catch {
              path = '/'
            }
          }
        }
        actions.push({
          name,
          method,
          path,
          summary: item.request?.description || item?.description || `${method} ${path}`
        })
      } else if (item?.item) {
        collect(item.item, prefix + name + '_')
      }
    })
  }
  collect(collection.item)
  return actions
}

async function fetchPostmanActions(formType) {
  const settings = formType === 'connect' ? connectForm.value.settings : editForm.value.settings
  let collection = null

  if (settings?.collectionSource === 'url') {
    const url = settings?.postmanCollectionUrl?.trim()
    if (!url) {
      toast.error('Ingrese la URL de la colección Postman')
      return
    }
    postmanCollectionLoading.value = true
    try {
      const data = await apiService.fetchPostmanCollection(url)
      const text = data?.collection ?? ''
      if (!text) {
        toast.error('La colección recibida está vacía')
        return
      }
      try {
        collection = JSON.parse(text)
      } catch {
        toast.error('La colección no es JSON válido')
        return
      }
    } catch (e) {
      const msg = e.response?.data?.error || e.message || 'Error al cargar'
      toast.error(msg)
      return
    } finally {
      postmanCollectionLoading.value = false
    }
  } else {
    const jsonStr = settings?.postmanCollection?.trim()
    if (!jsonStr) {
      toast.error('Ingrese la colección Postman')
      return
    }
    try {
      collection = JSON.parse(jsonStr)
    } catch {
      toast.error('JSON inválido')
      return
    }
  }

  const info = collection?.info ? { name: collection.info.name } : {}
  const actions = extractPostmanActions(collection)
  postmanParsed.value = { info, actions }
  toast.success(`Se encontraron ${actions.length} acciones`)
  nextTick(() => postmanParsedRef.value?.scrollIntoView?.({ behavior: 'smooth', block: 'nearest' }))
}

async function fetchMcpTools(formType) {
  const settings = formType === 'connect' ? connectForm.value.settings : editForm.value.settings
  const url = settings?.mcpServerUrl?.trim()
  if (!url) {
    toast.error('Ingrese la URL del servidor MCP')
    return
  }
  const integrationId = formType === 'edit' && editIntegration.value?.id ? editIntegration.value.id : null
  mcpToolsLoading.value = true
  mcpTools.value = []
  try {
    const data = await apiService.fetchMcpTools(settings, integrationId)
    mcpTools.value = data?.tools ?? []
    toast.success(`Se encontraron ${mcpTools.value.length} herramientas`)
  } catch (e) {
    const msg = e.response?.data?.error || e.message || 'Error al obtener herramientas'
    toast.error(msg)
  } finally {
    mcpToolsLoading.value = false
  }
}

function methodBadgeClass(method) {
  const m = (method || '').toUpperCase()
  if (m === 'GET') return 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300'
  if (m === 'POST') return 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300'
  if (m === 'PUT' || m === 'PATCH') return 'bg-amber-100 dark:bg-amber-900/30 text-amber-800 dark:text-amber-300'
  if (m === 'DELETE') return 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300'
  return 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
}

function togglePasswordVisibility(key) {
  passwordVisible.value = { ...passwordVisible.value, [key]: !passwordVisible.value[key] }
}

function openConnectModal(meta) {
  if (isAtLimitForProvider(meta)) {
    toast.error(getLimitMessageForProvider(meta))
    return
  }
  selectedMeta.value = meta
  passwordVisible.value = {}
  openApiValidationError.value = ''
  openApiParsed.value = null
  postmanParsed.value = null
  mcpTools.value = []
  connectTouched.value = { name: false }
  connectSettingsTouched.value = {}
  const baseSettings = Object.fromEntries((meta.settings ?? []).map(s => [s.key, s.default ?? '']))
  if (meta.authType === 'oauth') {
    baseSettings.authType = 'oauth'
  }
  if (meta.key === postmanProviderKey && !baseSettings.collectionSource) {
    baseSettings.collectionSource = 'url'
  }
  connectForm.value = {
    name: '',
    settings: baseSettings
  }
  inlineSchemaModeConnect.value = 'file'
  if (meta.key === 'CustomAPI') {
    const raw = baseSettings.customActions ?? ''
    try {
      const parsed = raw?.trim() ? JSON.parse(raw) : []
      customActionsForm.value = Array.isArray(parsed) && parsed.length > 0
        ? parsed.map(a => ({ key: a.key ?? '', method: a.method ?? 'GET', path: a.path ?? '', body: a.body ?? '' }))
        : [{ key: '', method: 'GET', path: '', body: '' }]
    } catch {
      customActionsForm.value = [{ key: '', method: 'GET', path: '', body: '' }]
    }
  }
  if (meta.hasCustomHeaders) {
    const raw = baseSettings.customHeaders ?? ''
    try {
      const parsed = raw?.trim() ? JSON.parse(raw) : []
      customHeadersForm.value = Array.isArray(parsed) && parsed.length > 0
        ? parsed.map(h => ({ key: h.key ?? '', value: h.value ?? '' }))
        : [{ key: '', value: '' }]
    } catch {
      customHeadersForm.value = [{ key: '', value: '' }]
    }
  }
  connectModalOpen.value = true
}

function closeConnectModal() {
  connectModalOpen.value = false
  selectedMeta.value = null
}

function openEditCredentialsModal(int) {
  editIntegration.value = int
  editMeta.value = availableIntegrations.value.find(m => m.key === int.provider)
    ?? availableIntegrations.value.find(m => (m.name && m.name === int.provider))
    ?? null
  if (!editMeta.value) {
    toast.error('No se encontró la configuración del proveedor')
    return
  }
  // Siempre abrir modal de edición (nombre, descripción, etc.). Para OAuth, reautorizar con el botón "Conectar" en la tarjeta.
  passwordVisible.value = {}
  openApiValidationError.value = ''
  openApiParsed.value = null
  postmanParsed.value = null
  mcpTools.value = []
  editSettingsTouched.value = {}
  editForm.value = { settings: {} }
  loadingEditId.value = int.id
  editCredentialsModalOpen.value = true
  loadEditFormData(int)
}

async function loadEditFormData(int) {
  try {
    const data = await apiService.getIntegrationById(int.id, { revealSensitiveSettings: true })
    const settings = data?.settings ?? {}
    const meta = editMeta.value
    if (!meta) return
    const formSettings = Object.fromEntries(
      (meta.settings ?? []).map(s => [s.key, settings[s.key] ?? ''])
    )
    if (meta.authType === 'oauth' || settings.authType === 'oauth') {
      formSettings.authType = 'oauth'
    }
    if (meta.key === 'VTEX' && !formSettings.accountName && settings.url) {
      const m = String(settings.url).match(/https?:\/\/([^.]+)\.vtexcommercestable\.com\.br/)
      if (m) formSettings.accountName = m[1]
    }
    if (meta.key === 'Shopify' && !formSettings.accountName && settings.url) {
      const m = String(settings.url).match(/https?:\/\/([^.]+)\.myshopify\.com/)
      if (m) formSettings.accountName = m[1]
    }
    if (meta.key === postmanProviderKey && !formSettings.collectionSource) {
      formSettings.collectionSource = formSettings.postmanCollection?.trim() ? 'inline' : 'url'
    }
    if (meta.hasCustomHeaders) {
      formSettings.customHeaders = settings.customHeaders ?? ''
    }
    editForm.value = { settings: formSettings }
    const hasInlineContent = !!(formSettings.openApiSchema?.trim() || formSettings.postmanCollection?.trim())
    inlineSchemaModeEdit.value = hasInlineContent ? 'textarea' : 'file'
    if (meta.key === 'CustomAPI') {
      const raw = formSettings.customActions ?? ''
      try {
        const parsed = raw?.trim() ? JSON.parse(raw) : []
        editCustomActionsForm.value = Array.isArray(parsed) && parsed.length > 0
          ? parsed.map(a => ({ key: a.key ?? '', method: a.method ?? 'GET', path: a.path ?? '', body: a.body ?? '' }))
          : [{ key: '', method: 'GET', path: '', body: '' }]
      } catch {
        editCustomActionsForm.value = [{ key: '', method: 'GET', path: '', body: '' }]
      }
    }
    if (meta.hasCustomHeaders) {
      const raw = formSettings.customHeaders ?? ''
      try {
        const parsed = raw?.trim() ? JSON.parse(raw) : []
        editCustomHeadersForm.value = Array.isArray(parsed) && parsed.length > 0
          ? parsed.map(h => ({ key: h.key ?? '', value: h.value ?? '' }))
          : [{ key: '', value: '' }]
      } catch {
        editCustomHeadersForm.value = [{ key: '', value: '' }]
      }
    }
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al cargar credenciales')
    closeEditCredentialsModal()
  } finally {
    loadingEditId.value = null
  }
}

function closeEditCredentialsModal() {
  editCredentialsModalOpen.value = false
  editIntegration.value = null
  editMeta.value = null
}

async function submitEditCredentials() {
  if (!editIntegration.value) return
  if (editIntegration.value.provider === 'CustomAPI') {
    const actions = editCustomActionsForm.value
      .filter(a => (a.key ?? '').trim() && (a.path ?? '').trim())
      .map(a => ({ key: (a.key ?? '').trim(), method: a.method ?? 'GET', path: (a.path ?? '').trim(), body: (a.body ?? '').trim() || undefined }))
    editForm.value.settings.customActions = JSON.stringify(actions)
  }
  if (editMeta.value?.hasCustomHeaders) {
    const headers = editCustomHeadersForm.value
      .filter(h => (h.key ?? '').trim())
      .map(h => ({ key: (h.key ?? '').trim(), value: (h.value ?? '').trim() }))
    editForm.value.settings.customHeaders = JSON.stringify(headers)
  }
  savingEdit.value = true
  try {
    const settings = buildSettingsForSubmit(editIntegration.value.provider, editForm.value.settings, editMeta.value)
    await apiService.updateIntegration(editIntegration.value.id, {
      settings
    })
    closeEditCredentialsModal()
    await notifyIntegrationsListUpdated()
    toast.success('Credenciales actualizadas correctamente')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al actualizar credenciales')
  } finally {
    savingEdit.value = false
  }
}

async function startMcpOAuth() {
  const id = editIntegration.value?.id
  if (!id) return
  mcpOAuthLoading.value = true
  try {
    const { redirectUrl } = await apiService.getMcpOAuthAuthorizeUrl(id)
    if (redirectUrl) window.location.href = redirectUrl
    else toast.error('No se pudo obtener la URL de autorización')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al iniciar OAuth')
  } finally {
    mcpOAuthLoading.value = false
  }
}

function openOAuthConnectModal(int) {
  mcpOAuthPreviewIntegration.value = int
  mcpOAuthPreviewModalOpen.value = true
}

function openMcpOAuthPreviewModal(int) {
  openOAuthConnectModal(int)
}

function closeMcpOAuthPreviewModal() {
  mcpOAuthPreviewModalOpen.value = false
  mcpOAuthPreviewIntegration.value = null
}

async function confirmMcpOAuthFromPreview() {
  const int = mcpOAuthPreviewIntegration.value
  if (!int?.id) return
  mcpOAuthLoadingForId.value = int.id
  try {
    const { redirectUrl } = await apiService.getMcpOAuthAuthorizeUrl(int.id)
    if (redirectUrl) {
      closeMcpOAuthPreviewModal()
      window.location.href = redirectUrl
    } else {
      toast.error('No se pudo obtener la URL de autorización. Verifique que la integración tenga OAuth configurado.')
    }
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al iniciar OAuth')
  } finally {
    mcpOAuthLoadingForId.value = null
  }
}

async function startMcpOAuthForIntegration(int) {
  if (!int?.id) return
  mcpOAuthLoadingForId.value = int.id
  try {
    const { redirectUrl } = await apiService.getMcpOAuthAuthorizeUrl(int.id)
    if (redirectUrl) window.location.href = redirectUrl
    else toast.error('No se pudo obtener la URL de autorización. Verifique que la integración tenga OAuth configurado.')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al iniciar OAuth')
  } finally {
    mcpOAuthLoadingForId.value = null
  }
}

/** Redirect URI para OAuth: se obtiene de los metadatos (oauthCallbackPath). Solo MCP genérico no tiene path en YAML. */
function getRedirectUriForProvider(provider) {
  if (!provider) return null
  const origin = window.location.origin
  const meta = availableIntegrations.value?.find(m => m.key === provider)
  const path = meta?.oauthCallbackPath
  if (path) return `${origin}${path.startsWith('/') ? path : '/' + path}`
  if (provider === 'MCP') return `${origin}/mcp/oauth/callback`
  return null
}

function buildSettingsForSubmit(provider, settings, meta) {
  const base = { ...(settings ?? {}) }
  const redirectUri = getRedirectUriForProvider(provider)
  if (redirectUri) base.redirectUri = redirectUri
  if (meta?.authType === 'oauth') base.authType = 'oauth'
  if (provider === 'VTEX' || provider === 'Shopify') delete base.url
  return base
}

async function submitConnect() {
  if (!selectedMeta.value) return
  const name = connectForm.value.name?.trim()
  if (!name) {
    toast.error(isMcpConnect.value ? 'El nombre del servidor es requerido' : (isChannelConnect.value ? 'El nombre del canal es requerido' : (isSupportConnect.value ? 'El nombre de la herramienta es requerido' : (isOpenApiConnect.value ? 'El nombre del servicio es requerido' : 'El nombre de la tienda es requerido'))))
    return
  }
  if (selectedMeta.value.key === 'CustomAPI') {
    const actions = customActionsForm.value
      .filter(a => (a.key ?? '').trim() && (a.path ?? '').trim())
      .map(a => ({ key: (a.key ?? '').trim(), method: a.method ?? 'GET', path: (a.path ?? '').trim(), body: (a.body ?? '').trim() || undefined }))
    connectForm.value.settings.customActions = JSON.stringify(actions)
  }
  if (selectedMeta.value.hasCustomHeaders) {
    const headers = customHeadersForm.value
      .filter(h => (h.key ?? '').trim())
      .map(h => ({ key: (h.key ?? '').trim(), value: (h.value ?? '').trim() }))
    connectForm.value.settings.customHeaders = JSON.stringify(headers)
  }
  saving.value = true
  try {
    const settings = buildSettingsForSubmit(selectedMeta.value.key, connectForm.value.settings, selectedMeta.value)
    const meta = selectedMeta.value
    const result = await apiService.createIntegration({
      provider: meta.key,
      name,
      settings
    })
    closeConnectModal()
    await notifyIntegrationsListUpdated()
    eventBus.emit(TOUR_PROGRESS_UPDATED)
    toast.success('Integración conectada correctamente')
    const skipOAuth =
      meta?.key === 'ClickUp' && String(connectForm.value.settings?.connectionAuth ?? '').toLowerCase() === 'personal'
    if (meta?.authType === 'oauth' && result?.id && !skipOAuth) {
      let newInt = { id: result.id, name, provider: meta.key }
      try {
        const my = await apiService.getMyIntegrations()
        newInt = my?.find((i) => i.id === result.id) ?? newInt
      } catch (_) {
        /* keep fallback */
      }
      nextTick(() => openOAuthConnectModal(newInt))
    }
  } catch (e) {
    const msg = e.response?.data?.error || e.message || 'Error al conectar'
    toast.error(msg)
  } finally {
    saving.value = false
  }
}

const { fetchUsage, isAtLimitForProvider, getLimitMessageForProvider } = useAccountUsage()

const modalApi = {
  openConnectModal,
  openEditCredentialsModal,
  openOAuthConnectModal
}

onMounted(() => {
  registerIntegrationConnectEditApi(modalApi)
  fetchUsage()
})

onUnmounted(() => {
  unregisterIntegrationConnectEditApi(modalApi)
})

</script>

<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6 sm:py-8">
      <div class="mb-6">
        <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Integraciones</h1>
        <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
          Conecta tiendas, APIs (OpenAPI o Custom) y servidores MCP para potenciar tus agentes.
        </p>
      </div>

      <!-- Tabs: Tiendas | APIs | MCP -->
      <div class="mb-6 sm:mb-8 border-b border-gray-200 dark:border-gray-700 -mx-4 sm:mx-0 px-4 sm:px-0 overflow-x-auto">
        <nav class="flex gap-1 -mb-px min-w-max sm:min-w-0" aria-label="Tabs">
          <button
            v-for="tab in integrationTabs"
            :key="tab.id"
            type="button"
            :class="[
              'px-3 sm:px-4 py-3 text-sm font-medium rounded-t-lg border-b-2 transition-colors whitespace-nowrap shrink-0',
              activeTab === tab.id
                ? 'border-primary-600 text-primary-600 dark:text-primary-400 dark:border-primary-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-300'
            ]"
            @click="activeTab = tab.id"
          >
            <span class="inline-flex items-center gap-2">
              <span>{{ tab.icon }}</span>
              {{ tab.label }}
              <span
                v-if="tab.count !== undefined"
                :class="[
                  'ml-1 px-2 py-0.5 rounded-full text-xs',
                  activeTab === tab.id
                    ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-700 dark:text-primary-300'
                    : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400'
                ]"
              >
                {{ tab.count }}
              </span>
            </span>
          </button>
        </nav>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="space-y-8">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div v-for="i in 6" :key="i" class="h-32 sm:h-36 bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse" />
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
        <p class="text-red-800 dark:text-red-300">{{ error }}</p>
      </div>

      <div v-else class="space-y-10">
        <!-- Mis integraciones conectadas (filtradas por tab) -->
        <section>
          <h2 class="text-lg sm:text-xl font-semibold text-gray-900 dark:text-white mb-4">{{ connectedSectionTitle }}</h2>
          <div v-if="filteredMyIntegrations.length === 0" class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6 sm:p-8 text-center">
            <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
            </svg>
            <p class="mt-4 text-gray-500 dark:text-gray-400">{{ emptyConnectedMessage }}</p>
            <p class="text-sm text-gray-400 dark:text-gray-500">{{ emptyConnectedHint }}</p>
          </div>
          <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <article
              v-for="int in filteredMyIntegrations"
              :key="int.id"
              class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 sm:p-5 shadow-sm card-hover flex flex-col h-full min-w-0"
            >
              <div class="flex items-start gap-3 sm:gap-4">
                <div class="w-12 h-12 sm:w-14 sm:h-14 rounded-xl bg-gray-50 dark:bg-gray-700 flex items-center justify-center p-2 shrink-0 overflow-hidden">
                  <img
                    :src="integrationListLogoSrc(int)"
                    :alt="int.provider"
                    class="max-h-full max-w-full object-contain"
                    @error="() => onIntegrationListLogoError(int)"
                  />
                </div>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2">
                    <input
                      v-if="editingNameId === int.id"
                      ref="nameInputRef"
                      v-model="editNameValue"
                      type="text"
                      class="flex-1 px-2 py-1 text-sm font-semibold rounded border border-primary-500 dark:bg-gray-700 dark:border-primary-500 focus:ring-2 focus:ring-primary-500"
                      @blur="saveNameEdit"
                      @keydown.enter="saveNameEdit"
                      @keydown.escape="cancelNameEdit"
                    />
                    <template v-else>
                      <h3 class="font-semibold text-gray-900 dark:text-white truncate">{{ int.name }}</h3>
                      <button
                        v-if="integrationPerms.canCreate"
                        type="button"
                        @click="startEditName(int)"
                        class="p-1 text-gray-400 hover:text-primary-600 dark:hover:text-gray-500 dark:hover:text-primary-400 rounded"
                        title="Editar nombre"
                      >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                        </svg>
                      </button>
                    </template>
                  </div>
                  <p class="text-sm text-gray-500 dark:text-gray-400">{{ int.provider }}</p>
                  <div class="flex flex-wrap gap-1 mt-2">
                    <span
                      v-if="int.isActive === false"
                      class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300"
                    >
                      Inactiva
                    </span>
                    <span
                      v-if="oauthConnectProviderKeys.includes(int.provider) && isOAuthConnected(int)"
                      class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300"
                    >
                      OAuth conectado
                    </span>
                    <span
                      v-else-if="oauthConnectProviderKeys.includes(int.provider) && isOAuthProvider(int.provider)"
                      class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300"
                    >
                      Pendiente de conectar
                    </span>
                  </div>
                </div>
              </div>

              <p
                v-if="webhookProviderKeys.includes(int.provider)"
                class="mt-3 text-xs text-gray-500 dark:text-gray-400 leading-relaxed"
              >
                Configura el webhook en {{ int.provider }} para recibir notificaciones en tiempo real de órdenes y productos.
              </p>

              <div
                v-if="hasConnectedIntegrationActions(int)"
                class="mt-auto pt-4 border-t border-gray-100 dark:border-gray-700/80 grid grid-cols-2 gap-2"
              >
                <button
                  v-if="canShowActionsModal(int)"
                  type="button"
                  class="inline-flex items-center justify-center gap-1.5 text-sm py-2 px-3 w-full min-h-[2.25rem] rounded-lg bg-gray-200 text-gray-800 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600 transition-colors disabled:opacity-50"
                  :disabled="actionsModalLoading && actionsModalIntegration?.id === int.id"
                  @click="openActionsModal(int)"
                >
                  <span>{{ actionsModalLoading && actionsModalIntegration?.id === int.id ? 'Cargando…' : 'Acciones' }}</span>
                </button>
                <button
                  v-if="webhookProviderKeys.includes(int.provider)"
                  type="button"
                  class="inline-flex items-center justify-center gap-1.5 text-sm py-2 px-3 w-full min-h-[2.25rem] rounded-lg bg-gray-200 text-gray-800 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600 transition-colors disabled:opacity-50"
                  :title="webhookActivateTooltip"
                  :disabled="webhookActivatingId === int.id"
                  @click="activateWebhook(int)"
                >
                  <span>{{ webhookActivatingId === int.id ? 'Activando…' : 'Webhook' }}</span>
                </button>
                <button
                  v-if="integrationPerms.canCreate && oauthConnectProviderKeys.includes(int.provider)"
                  type="button"
                  class="inline-flex items-center justify-center gap-1.5 text-sm py-2 px-3 w-full min-h-[2.25rem] rounded-lg bg-gray-200 text-gray-800 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600 transition-colors disabled:opacity-50"
                  :disabled="mcpOAuthLoadingForId === int.id"
                  @click="openIntegrationOAuthConnectModal(int)"
                >
                  <span>{{ mcpOAuthLoadingForId === int.id ? 'Conectando…' : 'Conectar OAuth' }}</span>
                </button>
                <button
                  v-if="integrationPerms.canCreate"
                  type="button"
                  class="inline-flex items-center justify-center gap-1.5 text-sm py-2 px-3 w-full min-h-[2.25rem] rounded-lg bg-gray-200 text-gray-800 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600 transition-colors disabled:opacity-50"
                  :disabled="togglingActiveId === int.id"
                  @click="toggleIntegrationActive(int)"
                >
                  <span>{{ togglingActiveId === int.id ? '…' : (int.isActive !== false ? 'Desactivar' : 'Activar') }}</span>
                </button>
                <button
                  v-if="integrationPerms.canCreate"
                  type="button"
                  class="inline-flex items-center justify-center gap-1.5 text-sm py-2 px-3 w-full min-h-[2.25rem] rounded-lg bg-gray-200 text-gray-800 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600 transition-colors"
                  @click="openIntegrationEditCredentialsModal(int)"
                >
                  Editar
                </button>
                <button
                  v-if="integrationPerms.canCreate"
                  type="button"
                  class="inline-flex items-center justify-center gap-1.5 text-sm py-2 px-3 w-full min-h-[2.25rem] rounded-lg border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                  :class="connectedActionButtonSpan(int) === 2 ? 'col-span-2' : ''"
                  @click="confirmDeleteIntegration(int)"
                >
                  Desconectar
                </button>
              </div>
            </article>
          </div>
        </section>

        <!-- Integraciones disponibles (filtradas por tab) -->
        <section>
          <h2 class="text-lg sm:text-xl font-semibold text-gray-900 dark:text-white mb-4">{{ availableSectionTitle }}</h2>
          <PlanLimitAlert
            v-if="limitMessageForActiveTab"
            :message="limitMessageForActiveTab"
            billing-link-text="Actualiza tu plan para más"
            class-names="mb-4 text-sm text-amber-600 dark:text-amber-400"
          />
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <button
              v-for="meta in filteredAvailableIntegrations"
              :key="meta.key"
              type="button"
              :disabled="!integrationPerms.canCreate || isAtLimitForProvider(meta)"
              :aria-label="connectButtonLabel(meta)"
              class="group flex gap-4 p-5 rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 card-hover hover:shadow-md duration-200 min-w-0 w-full text-left disabled:opacity-60 disabled:cursor-not-allowed disabled:hover:shadow-none"
              @click="openIntegrationConnectModal(meta)"
            >
              <div class="shrink-0 w-12 h-12 sm:w-14 sm:h-14 rounded-xl bg-gray-50 dark:bg-gray-700 flex items-center justify-center p-2 overflow-hidden">
                <img
                  :src="(meta.logoUrl && !logoFailed(meta.logoUrl)) ? meta.logoUrl : '/images/avatar-default.svg'"
                  :alt="meta.name"
                  class="max-h-full max-w-full object-contain"
                  @error="() => setLogoFailed(meta.logoUrl)"
                />
              </div>
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-2 flex-wrap">
                  <h3 class="font-semibold text-gray-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
                    {{ meta.name }}
                  </h3>
                  <span
                    v-if="meta.type"
                    class="px-2 py-0.5 rounded-full text-xs font-semibold bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400"
                  >
                    {{ integrationTypeBadge(meta) }}
                  </span>
                </div>
                <p class="text-sm text-gray-600 dark:text-gray-400 mt-0.5 line-clamp-2">{{ meta.description }}</p>
                <p
                  v-if="isAtLimitForProvider(meta) && getLimitMessageForProvider(meta)"
                  class="mt-2 text-xs text-amber-700 dark:text-amber-300"
                >
                  {{ getLimitMessageForProvider(meta) }}
                </p>
              </div>
              <div class="shrink-0 self-center flex items-center gap-1.5 ml-1">
                <span class="hidden sm:inline text-sm font-medium text-primary-600 dark:text-primary-400 group-hover:underline">
                  {{ connectButtonLabel(meta) }}
                </span>
                <svg
                  class="w-5 h-5 text-gray-400 group-hover:text-primary-500 dark:group-hover:text-primary-400 transition-colors"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </button>
          </div>
        </section>
      </div>
    </main>


    <!-- Modal Ver Acciones -->
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
          v-if="actionsModalOpen"
          class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
        >
          <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 max-w-2xl w-full max-h-[85vh] flex flex-col overflow-hidden">
            <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
              <div class="flex items-center justify-between gap-2">
                <h3 class="text-base sm:text-lg font-semibold text-gray-900 dark:text-white truncate">
                  Acciones de {{ actionsModalIntegration?.name }}
                </h3>
                <button
                  type="button"
                  @click="closeActionsModal"
                  class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg transition-colors"
                  aria-label="Cerrar"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
                </button>
              </div>
              <div class="mt-4 flex flex-col sm:flex-row gap-2">
                <input
                  v-model="actionsSearchFilter"
                  type="text"
                  placeholder="Buscar acciones..."
                  class="input-field flex-1 min-w-0"
                />
                <button
                  type="button"
                  @click="refreshActionsModal"
                  :disabled="actionsModalLoading || !actionsModalIntegration"
                  class="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  title="Actualizar desde el servidor"
                >
                  <svg
                    :class="['w-5 h-5', actionsModalLoading && 'animate-spin']"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                  <span class="hidden sm:inline">Actualizar</span>
                </button>
              </div>
              <p v-if="!actionsModalLoading && actionsModalData.items.length > 0" class="mt-2 text-sm text-gray-500 dark:text-gray-400">
                {{ filteredActionsModalItems.length }} de {{ actionsModalData.items.length }} acciones
              </p>
            </div>
            <div class="flex-1 overflow-y-auto p-4 sm:p-6">
              <div v-if="actionsModalLoading" class="space-y-2">
                <div
                  v-for="i in 6"
                  :key="`actions-skeleton-${i}`"
                  class="flex items-start gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700"
                >
                  <div class="w-5 h-5 rounded bg-gray-200 dark:bg-gray-700 animate-pulse shrink-0 mt-0.5" />
                  <div class="min-w-0 flex-1 space-y-2">
                    <div class="h-4 w-3/4 max-w-[200px] bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                    <div class="h-3 w-full bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                    <div class="h-3 w-2/3 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
                  </div>
                </div>
              </div>
              <div v-else-if="actionsModalData.items.length === 0" class="py-12 text-center text-gray-500 dark:text-gray-400">
                <svg class="mx-auto w-12 h-12 text-gray-400 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <p>No hay acciones disponibles</p>
              </div>
              <div v-else-if="filteredActionsModalItems.length === 0" class="py-12 text-center text-gray-500 dark:text-gray-400">
                <p>{{ actionsSearchFilter ? 'No se encontraron acciones' : 'No hay acciones disponibles' }}</p>
              </div>
              <div v-else class="space-y-2">
                <!-- MCP tools -->
                <template v-if="actionsModalData.type === 'mcp'">
                  <div
                    v-for="t in filteredActionsModalItems"
                    :key="t.name"
                    class="flex items-start gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700"
                  >
                    <span class="text-primary-500 mt-0.5">⚙</span>
                    <div class="min-w-0 flex-1">
                      <span class="font-mono text-sm font-medium text-gray-900 dark:text-white block">{{ t.name }}</span>
                      <ExpandableDescription v-if="t.description" :text="t.description" :max-length="120" />
                    </div>
                  </div>
                </template>
                <!-- OpenAPI actions -->
                <template v-else-if="actionsModalData.type === 'openapi'">
                  <div
                    v-for="a in filteredActionsModalItems"
                    :key="(a.operationId || a.path) + (a.method || '')"
                    class="flex items-center justify-between gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/80 border border-gray-100 dark:border-gray-700"
                  >
                    <div class="min-w-0 flex-1">
                      <span class="font-mono text-sm font-medium text-gray-900 dark:text-white block">{{ a.operationId || a.path }}</span>
                      <ExpandableDescription v-if="a.summary" :text="a.summary" :max-length="100" />
                    </div>
                    <span
                      :class="['px-2 py-0.5 rounded text-xs font-medium shrink-0', methodBadgeClass(a.method)]"
                    >
                      {{ a.method || '?' }}
                    </span>
                  </div>
                </template>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Modal Confirmar eliminar -->
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
          v-if="deleteModalOpen"
          class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50"
        >
          <div class="bg-white dark:bg-gray-800 rounded-xl shadow-xl border border-gray-200 dark:border-gray-700 max-w-md w-full p-6">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Desconectar integración</h3>
            <p class="mt-2 text-gray-600 dark:text-gray-400">
              ¿Desconectar <strong>{{ integrationToDelete?.name }}</strong>? Esta acción no se puede deshacer.
            </p>
            <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
              <button type="button" @click="deleteModalOpen = false" class="w-full sm:w-auto min-h-[44px] touch-manipulation btn-secondary">Cancelar</button>
              <button
                type="button"
                @click="doDeleteIntegration"
                :disabled="deleting"
                class="w-full sm:w-auto px-6 py-2.5 sm:py-3 min-h-[44px] touch-manipulation rounded-lg font-semibold bg-red-600 hover:bg-red-700 text-white transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {{ deleting ? 'Eliminando...' : 'Desconectar' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { useAppStore } from '../../stores/appStore'
import PlanLimitAlert from '../../components/usage/PlanLimitAlert.vue'
import { useAccountUsage } from '../../composables/useAccountUsage'
import { useResourcePermissions } from '../../composables/useMemberPermissions'
import { eventBus, TOUR_PROGRESS_UPDATED, MY_INTEGRATIONS_UPDATED } from '../../utils/eventBus'
import {
  openIntegrationConnectModal,
  openIntegrationEditCredentialsModal,
  openIntegrationOAuthConnectModal
} from '../../utils/integrationConnectEditRegistry'
import ExpandableDescription from '../../components/ExpandableDescription.vue'
import {
  defaultIntegrationTab,
  excludeGravityWhenNativeStore,
  isGravityNativeProvider
} from '../../utils/integrationCatalogTabs'

const toast = useToast()
const route = useRoute()
const appStore = useAppStore()
const integrationPerms = useResourcePermissions('integrations')

/** Native Gravity store (`commerceStoreMode === Native`) — Tiendas/APIs tabs are hidden. */
const isNativeStore = computed(() => {
  const raw = appStore.profile?.commerceStoreMode ?? appStore.profile?.CommerceStoreMode ?? 'None'
  return String(raw || 'None') === 'Native'
})

const loading = ref(true)
const error = ref(null)
const deleting = ref(false)
const availableIntegrations = ref([])
const myIntegrations = ref([])

const VALID_TABS = ['stores', 'apis', 'channels', 'support', 'mcp']
const activeTab = ref(defaultIntegrationTab(false))
const deleteModalOpen = ref(false)
const integrationToDelete = ref(null)
const editingNameId = ref(null)
const editNameValue = ref('')
const nameInputRef = ref(null)

const actionsModalOpen = ref(false)
const actionsModalIntegration = ref(null)
const actionsModalLoading = ref(false)
const actionsModalData = ref({ type: null, items: [] }) // { type: 'mcp'|'openapi', items: [...] }
const actionsSearchFilter = ref('')
const openApiSchemaLoading = ref(false)
const postmanCollectionLoading = ref(false)
const postmanParsed = ref(null)
const postmanParsedRef = ref(null)
const customActionsForm = ref([{ key: '', method: 'GET', path: '', body: '' }])
const editCustomActionsForm = ref([{ key: '', method: 'GET', path: '', body: '' }])
const customHeadersForm = ref([{ key: '', value: '' }])
const editCustomHeadersForm = ref([{ key: '', value: '' }])

const filteredActionsModalItems = computed(() => {
  const items = actionsModalData.value.items ?? []
  const q = (actionsSearchFilter.value || '').toLowerCase().trim()
  if (!q) return items
  const type = actionsModalData.value.type
  if (type === 'mcp') {
    return items.filter(t => {
      const name = (t.name || '').toLowerCase()
      const desc = (t.description || '').toLowerCase()
      return name.includes(q) || desc.includes(q)
    })
  }
  if (type === 'openapi') {
    return items.filter(a => {
      const opId = (a.operationId || '').toLowerCase()
      const path = (a.path || '').toLowerCase()
      const summary = (a.summary || '').toLowerCase()
      return opId.includes(q) || path.includes(q) || summary.includes(q)
    })
  }
  return items
})


/** Keys de proveedores por tab (usa integrationType: Shops, Channel, Support; type: api/mcp para APIs/MCP) */
const storeProviderKeys = computed(() =>
  (availableIntegrations.value ?? [])
    .filter(m => m.integrationType === 'Shops')
    .map(m => m.key)
)
const openApiProviderKey = 'Dynamic'
const customApiProviderKey = 'CustomAPI'
const postmanProviderKey = 'Postman'
const gravityApiProviderKey = 'GravityAPI'
const apiCatalogProviderKeys = [openApiProviderKey, customApiProviderKey, postmanProviderKey, gravityApiProviderKey]

function isApiCatalogProvider(key) {
  return apiCatalogProviderKeys.includes(key)
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

/** Providers with POST /integrations/{id}/activate-webhook — disabled in v1. */
const webhookProviderKeys = []
const webhookActivatingId = ref(null)
const webhookActivateTooltip = 'Registrar el webhook en el proveedor para recibir eventos en tu cuenta'
const togglingActiveId = ref(null)

const mcpOAuthLoadingForId = ref(null)

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

function belongsToTab(provider, tab) {
  if (tab === 'stores') return storeProviderKeys.value.includes(provider)
  if (tab === 'apis') return isApiCatalogProvider(provider)
  if (tab === 'mcp') return mcpProviderKeys.value.includes(provider)
  if (tab === 'channels') return channelProviderKeys.value.includes(provider)
  if (tab === 'support') return supportProviderKeys.value.includes(provider)
  return false
}

const filteredMyIntegrations = computed(() => {
  const tab = activeTab.value
  const list = (myIntegrations.value ?? []).filter((i) => belongsToTab(i.provider, tab))
  return excludeGravityWhenNativeStore(list, isNativeStore.value)
})

const filteredAvailableIntegrations = computed(() => {
  const tab = activeTab.value
  const list = availableIntegrations.value ?? []
  let filtered = []
  if (tab === 'stores') filtered = list.filter((m) => m.integrationType === 'Shops')
  else if (tab === 'apis') filtered = list.filter((m) => isApiCatalogProvider(m.key))
  else if (tab === 'mcp') filtered = list.filter((m) => m.type === 'mcp' && m.integrationType !== 'Support')
  else if (tab === 'channels') {
    filtered = list.filter((m) => m.integrationType === 'Channel' || m.channelMarketplace)
  } else if (tab === 'support') filtered = list.filter((m) => m.integrationType === 'Support')
  return excludeGravityWhenNativeStore(filtered, isNativeStore.value)
})

const integrationTabs = computed(() => {
  const available = excludeGravityWhenNativeStore(availableIntegrations.value ?? [], isNativeStore.value)
  const mine = excludeGravityWhenNativeStore(myIntegrations.value ?? [], isNativeStore.value)
  const storesCount =
    mine.filter((i) => belongsToTab(i.provider, 'stores')).length +
    available.filter((m) => m.integrationType === 'Shops').length
  const apisCount =
    mine.filter((i) => belongsToTab(i.provider, 'apis')).length +
    available.filter((m) => isApiCatalogProvider(m.key)).length
  const channelsCount =
    mine.filter((i) => channelProviderKeys.value.includes(i.provider) && !isGravityNativeProvider(i.provider)).length +
    available.filter((m) => m.integrationType === 'Channel' || m.channelMarketplace).length
  const supportCount =
    mine.filter((i) => supportProviderKeys.value.includes(i.provider)).length +
    available.filter((m) => m.integrationType === 'Support').length
  const mcpCount =
    mine.filter((i) => mcpProviderKeys.value.includes(i.provider)).length +
    available.filter((m) => m.type === 'mcp' && m.integrationType !== 'Support').length
  const tabs = [
    { id: 'stores', label: 'Tiendas', icon: '🛒', count: storesCount },
    { id: 'apis', label: 'APIs', icon: '🔌', count: apisCount },
    { id: 'channels', label: 'Canales', icon: '💬', count: channelsCount },
    { id: 'support', label: 'Soporte', icon: '📋', count: supportCount },
    { id: 'mcp', label: 'MCP', icon: '🔗', count: mcpCount }
  ]
  if (isNativeStore.value) {
    return tabs.filter((t) => t.id !== 'stores' && t.id !== 'apis')
  }
  return tabs
})

function isTabVisible(tab) {
  if (!tab || !VALID_TABS.includes(tab)) return false
  if (isNativeStore.value && (tab === 'stores' || tab === 'apis')) return false
  return true
}

function ensureActiveTabVisible() {
  if (isTabVisible(activeTab.value)) return
  activeTab.value = defaultIntegrationTab(isNativeStore.value)
}

const connectedSectionTitle = computed(() => {
  const t = activeTab.value
  if (t === 'stores') return 'Mis tiendas conectadas'
  if (t === 'apis') return 'Mis APIs conectadas'
  if (t === 'channels') return 'Mis canales conectados'
  if (t === 'support') return 'Mis herramientas de soporte conectadas'
  if (t === 'mcp') return 'Mis servidores MCP conectados'
  return 'Mis integraciones conectadas'
})

const availableSectionTitle = computed(() => {
  const t = activeTab.value
  if (t === 'stores') return 'Tiendas disponibles'
  if (t === 'apis') return 'Conectar API'
  if (t === 'channels') return 'Conectar canal'
  if (t === 'support') return 'Conectar herramienta de soporte'
  if (t === 'mcp') return 'Conectar servidor MCP'
  return 'Disponibles'
})

const emptyConnectedMessage = computed(() => {
  const t = activeTab.value
  if (t === 'stores') return 'Aún no has conectado ninguna tienda'
  if (t === 'apis') return 'Aún no has conectado ninguna API'
  if (t === 'channels') return 'Aún no has conectado ningún canal'
  if (t === 'support') return 'Aún no has conectado ninguna herramienta de soporte'
  if (t === 'mcp') return 'Aún no has conectado ningún servidor MCP'
  return 'Aún no has conectado ninguna integración'
})

const emptyConnectedHint = computed(() => {
  const t = activeTab.value
  if (t === 'stores') return 'Selecciona un proveedor abajo y dale un nombre para identificarla'
  if (t === 'apis') return 'Conecta una API OpenAPI o Custom para exponer acciones a tus agentes'
  if (t === 'channels') return 'Conecta canales de venta e integraciones según tu plan'
  if (t === 'support') return 'Conecta Slack, WhatsApp, Notion u otras herramientas de soporte y mensajería'
  if (t === 'mcp') return 'Agrega un servidor MCP para usar sus herramientas en tus agentes'
  return 'Selecciona una integración abajo para conectarla'
})

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


function methodBadgeClass(method) {
  const m = (method || '').toUpperCase()
  if (m === 'GET') return 'bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300'
  if (m === 'POST') return 'bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300'
  if (m === 'PUT' || m === 'PATCH') return 'bg-amber-100 dark:bg-amber-900/30 text-amber-800 dark:text-amber-300'
  if (m === 'DELETE') return 'bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300'
  return 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
}


function isConnected(providerKey) {
  return myIntegrations.value.some(i => i.provider === providerKey)
}

function integrationTypeBadge(meta) {
  if (meta.type === 'mcp') return 'MCP'
  if (meta.integrationType === 'Channel' || meta.channelMarketplace) return 'Canal'
  if (meta.integrationType === 'Support') return 'Soporte'
  return 'API'
}

function connectButtonLabel(meta) {
  if (meta.type === 'mcp') return isConnected(meta.key) ? 'Agregar otro servidor' : 'Agregar servidor'
  if (meta.integrationType === 'Channel' || meta.channelMarketplace) {
    return isConnected(meta.key) ? 'Conectar otro canal' : 'Conectar canal'
  }
  if (meta.integrationType === 'Support') {
    return isConnected(meta.key) ? 'Conectar otra herramienta' : 'Conectar herramienta'
  }
  if (meta.key === 'Dynamic') return isConnected(meta.key) ? 'Conectar otro servicio' : 'Conectar servicio'
  if (meta.key === gravityApiProviderKey) return isConnected(meta.key) ? 'Conectar otra API' : 'Conectar API'
  if (meta.integrationType === 'Shops') return isConnected(meta.key) ? 'Conectar otra tienda' : 'Conectar tienda'
  return isConnected(meta.key) ? 'Conectar otra' : 'Conectar'
}

function canShowActionsModal(int) {
  return (
    mcpProviderKeys.value.includes(int.provider)
    || supportProviderKeys.value.includes(int.provider)
    || int.provider === openApiProviderKey
    || int.provider === customApiProviderKey
    || int.provider === postmanProviderKey
    || int.provider === 'OpenAPI'
  )
}

function hasConnectedIntegrationActions(int) {
  if (canShowActionsModal(int)) return true
  if (webhookProviderKeys.includes(int.provider)) return true
  if (integrationPerms.canCreate && oauthConnectProviderKeys.value.includes(int.provider)) return true
  if (integrationPerms.canCreate) return true
  return false
}

function connectedActionButtonSpan(int) {
  let count = 0
  if (canShowActionsModal(int)) count++
  if (webhookProviderKeys.includes(int.provider)) count++
  if (integrationPerms.canCreate && oauthConnectProviderKeys.value.includes(int.provider)) count++
  if (integrationPerms.canCreate) count += 3 // Activar/Desactivar, Editar, Desconectar
  return count % 2 === 1 ? 2 : 1
}

function startEditName(int) {
  editingNameId.value = int.id
  editNameValue.value = int.name
  nextTick(() => nameInputRef.value?.focus())
}

function cancelNameEdit() {
  editingNameId.value = null
}

async function saveNameEdit() {
  const id = editingNameId.value
  const newName = editNameValue.value?.trim()
  if (!id || !newName) {
    editingNameId.value = null
    return
  }
  try {
    await apiService.updateIntegration(id, { name: newName })
    myIntegrations.value = myIntegrations.value.map(i =>
      i.id === id ? { ...i, name: newName } : i
    )
    toast.success('Nombre actualizado')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al actualizar')
  } finally {
    editingNameId.value = null
  }
}

async function toggleIntegrationActive(int) {
  if (!int?.id) return
  const next = int.isActive === false
  togglingActiveId.value = int.id
  try {
    await apiService.updateIntegration(int.id, { isActive: next })
    myIntegrations.value = myIntegrations.value.map(i =>
      i.id === int.id ? { ...i, isActive: next } : i
    )
    toast.success(next ? 'Integración activada' : 'Integración desactivada')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al actualizar estado')
  } finally {
    togglingActiveId.value = null
  }
}


async function activateWebhook(int) {
  if (!int?.id || !webhookProviderKeys.includes(int.provider)) return
  webhookActivatingId.value = int.id
  try {
    await apiService.activateWebhook(int.id)
    toast.success('Webhook activado correctamente')
  } catch (e) {
    const msg = e.response?.data?.error || e.message || 'Error al activar webhook'
    toast.error(msg)
  } finally {
    webhookActivatingId.value = null
  }
}

async function fetchActionsForModal() {
  const int = actionsModalIntegration.value
  if (!int?.id) return
  actionsModalLoading.value = true
  try {
    const detail = await apiService.getIntegrationById(int.id)
    const settings = detail?.settings ?? {}
    if (mcpProviderKeys.value.includes(int.provider)) {
      const url = settings.mcpServerUrl?.trim()
      if (!url) {
        toast.error('La integración no tiene URL del servidor MCP configurada')
        actionsModalData.value = { type: 'mcp', items: [] }
        return
      }
      const data = await apiService.fetchMcpTools(settings, int.id)
      actionsModalData.value = { type: 'mcp', items: data?.tools ?? [] }
      if (actionsModalData.value.items.length === 0) toast.info('No se encontraron herramientas')
    } else if (supportProviderKeys.value.includes(int.provider)) {
      const result = await apiService.getIntegrationOperations(int.id)
      const ops = result?.operations ?? []
      actionsModalData.value = { type: 'openapi', items: ops.map(o => ({ operationId: o.operationId ?? o.id, path: o.path, method: o.method, summary: o.summary ?? o.path })) }
      if (ops.length === 0) toast.info('No se encontraron acciones')
    } else if (int.provider === 'CustomAPI') {
      const customActionsStr = settings.customActions?.trim()
      if (!customActionsStr) {
        toast.error('La integración no tiene Custom Actions configuradas')
        actionsModalData.value = { type: 'openapi', items: [] }
        return
      }
      try {
        const arr = JSON.parse(customActionsStr)
        const actions = (Array.isArray(arr) ? arr : []).map(a => ({
          path: a.path ?? '',
          method: (a.method ?? 'GET').toUpperCase(),
          operationId: a.key ?? a.operationId,
          summary: a.path || a.key || ''
        }))
        actionsModalData.value = { type: 'openapi', items: actions }
        if (actions.length === 0) toast.info('No se encontraron acciones en Custom Actions')
      } catch (e) {
        toast.error('Custom Actions: JSON inválido')
        actionsModalData.value = { type: 'openapi', items: [] }
      }
    } else if (int.provider === openApiProviderKey || int.provider === 'OpenAPI') {
      let schema = null
      if (settings.schemaSource === 'url') {
        const schemaUrl = settings.openApiSchemaUrl?.trim()
        if (!schemaUrl) {
          toast.error('La integración no tiene URL del schema OpenAPI configurada')
          actionsModalData.value = { type: 'openapi', items: [] }
          return
        }
        const data = await apiService.fetchOpenApiSchema(schemaUrl)
        schema = parseOpenApiSchema(data?.schema ?? '')
      } else {
        schema = parseOpenApiSchema(settings.openApiSchema ?? '')
      }
      const actions = schema ? extractOpenApiActions(schema) : []
      actionsModalData.value = { type: 'openapi', items: actions }
      if (actions.length === 0) toast.info('No se encontraron acciones en el schema')
    } else if (int.provider === postmanProviderKey) {
      let collection = null
      if (settings.collectionSource === 'url') {
        const collectionUrl = settings.postmanCollectionUrl?.trim()
        if (!collectionUrl) {
          toast.error('La integración no tiene URL de la colección Postman configurada')
          actionsModalData.value = { type: 'openapi', items: [] }
          return
        }
        const data = await apiService.fetchPostmanCollection(collectionUrl)
        const text = data?.collection ?? ''
        if (!text) {
          actionsModalData.value = { type: 'openapi', items: [] }
          return
        }
        try {
          collection = JSON.parse(text)
        } catch {
          toast.error('La colección Postman no es JSON válido')
          actionsModalData.value = { type: 'openapi', items: [] }
          return
        }
      } else {
        const jsonStr = settings.postmanCollection?.trim()
        if (!jsonStr) {
          toast.error('La integración no tiene colección Postman configurada')
          actionsModalData.value = { type: 'openapi', items: [] }
          return
        }
        try {
          collection = JSON.parse(jsonStr)
        } catch {
          toast.error('Colección Postman: JSON inválido')
          actionsModalData.value = { type: 'openapi', items: [] }
          return
        }
      }
      const rawActions = extractPostmanActions(collection)
      const actions = rawActions.map(a => ({ operationId: a.name, path: a.path, method: a.method, summary: a.summary }))
      actionsModalData.value = { type: 'openapi', items: actions }
      if (actions.length === 0) toast.info('No se encontraron acciones en la colección')
    } else {
      actionsModalData.value = { type: null, items: [] }
    }
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al cargar acciones')
    actionsModalData.value = { type: null, items: [] }
  } finally {
    actionsModalLoading.value = false
  }
}

async function openActionsModal(int) {
  if (!int?.id) return
  actionsModalIntegration.value = int
  actionsModalOpen.value = true
  actionsModalData.value = { type: null, items: [] }
  actionsSearchFilter.value = ''
  await fetchActionsForModal()
}

async function refreshActionsModal() {
  await fetchActionsForModal()
}

function closeActionsModal() {
  actionsModalOpen.value = false
  actionsModalIntegration.value = null
  actionsModalData.value = { type: null, items: [] }
  actionsSearchFilter.value = ''
}


function confirmDeleteIntegration(int) {
  integrationToDelete.value = int
  deleteModalOpen.value = true
}

async function doDeleteIntegration() {
  if (!integrationToDelete.value) return
  deleting.value = true
  try {
    const res = await apiService.deleteIntegration(integrationToDelete.value.id)
    deleteModalOpen.value = false
    integrationToDelete.value = null
    await loadData()
    toast.success(res?.message || 'Integración desconectada')
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al desconectar')
  } finally {
    deleting.value = false
  }
}

const {
  fetchUsage,
  isAtLimitForTab,
  getLimitMessageForTab,
  isAtLimitForProvider,
  getLimitMessageForProvider
} = useAccountUsage()

const isAtLimitForActiveTab = computed(() => isAtLimitForTab(activeTab.value))
const limitMessageForActiveTab = computed(() => getLimitMessageForTab(activeTab.value))

async function loadData() {
  loading.value = true
  error.value = null
  try {
    const [available, my] = await Promise.all([
      appStore.fetchAvailableIntegrations(),
      apiService.getMyIntegrations(),
      fetchUsage()
    ])
    availableIntegrations.value = available ?? []
    myIntegrations.value = my ?? []
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar integraciones'
  } finally {
    loading.value = false
  }
}

function syncActiveTabFromRoute() {
  let tab = route.query.tab
  if (tab === 'openapi' || tab === 'custom') tab = 'apis'
  if (tab && isTabVisible(tab)) {
    activeTab.value = tab
  } else {
    ensureActiveTabVisible()
  }
}

function onMyIntegrationsUpdated({ my }) {
  if (Array.isArray(my)) myIntegrations.value = my
}

onMounted(() => {
  loadData()
  syncActiveTabFromRoute()
  eventBus.on(MY_INTEGRATIONS_UPDATED, onMyIntegrationsUpdated)
})

onUnmounted(() => {
  eventBus.off(MY_INTEGRATIONS_UPDATED, onMyIntegrationsUpdated)
})

watch(() => route.query.tab, (tab) => {
  if (tab === 'openapi' || tab === 'custom') tab = 'apis'
  if (tab && isTabVisible(tab)) {
    activeTab.value = tab
  } else {
    ensureActiveTabVisible()
  }
})

watch(isNativeStore, () => {
  ensureActiveTabVisible()
})
</script>

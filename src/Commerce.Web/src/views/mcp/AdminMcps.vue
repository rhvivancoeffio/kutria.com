<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">MCP Servers</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Define tus MCP Servers y asocia tools, prompts y resources a cada uno.
          </p>
        </div>
        <div class="flex flex-col gap-2 sm:items-end shrink-0">
          <PlanLimitAlert
            v-if="mcpServersLimitMessage"
            :message="mcpServersLimitMessage"
            billing-link-text="Actualiza tu plan para más"
            class="text-sm"
          />
          <button
            type="button"
            :disabled="isAtLimitForMcpServers"
            @click="onClickCreateMcpServer"
            :class="[
              'inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium transition-colors text-sm sm:text-base min-h-[44px] touch-manipulation',
              isAtLimitForMcpServers
                ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                : 'bg-primary-600 hover:bg-primary-700 text-white'
            ]"
          >
            <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Crear MCP Server
          </button>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="space-y-3 sm:space-y-4">
        <div v-for="i in 5" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
      </div>

      <!-- Empty state -->
      <div
        v-else-if="mcps.length === 0"
        class="text-center py-10 sm:py-12 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
        </svg>
        <p class="mt-4 text-sm sm:text-base text-gray-500 dark:text-gray-400">Aún no tienes MCP Servers definidos.</p>
        <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Crea uno para empezar a asociar tools, prompts y resources.</p>
        <PlanLimitAlert
          v-if="mcpServersLimitMessage"
          :message="mcpServersLimitMessage"
          billing-link-text="Actualiza tu plan para más"
          class="mt-4 text-sm"
        />
        <button
          type="button"
          :disabled="isAtLimitForMcpServers"
          @click="onClickCreateMcpServer"
          :class="[
            'mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium',
            isAtLimitForMcpServers
              ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
              : 'bg-primary-600 hover:bg-primary-700 text-white'
          ]"
        >
          <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Crear MCP Server
        </button>
      </div>

      <!-- Grid -->
      <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4">
        <div
          v-for="mcp in mcps"
          :key="mcp.id"
          class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4 shadow-sm card-hover"
        >
          <div class="flex items-start justify-between gap-3">
            <router-link :to="`/admin/mcps/${mcp.id}`" class="min-w-0 flex-1" :title="mcp.name">
              <h3 class="font-semibold text-gray-900 dark:text-white line-clamp-2 break-words">{{ mcp.name }}</h3>
              <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400 truncate" :title="`v${mcp.version} · ${mcpIntegrationsLabel(mcp)}`">v{{ mcp.version }} · {{ mcpIntegrationsLabel(mcp) }}</p>
              <div class="mt-2 flex flex-wrap gap-2 items-center">
                <span
                  :class="[
                    'inline-block px-2 py-0.5 text-xs font-medium rounded-full',
                    securityTypeBadgeClass(mcp.securityType)
                  ]"
                >
                  {{ securityTypeLabel(mcp.securityType) }}
                </span>
                <span
                  :class="[
                    'inline-block px-2 py-0.5 text-xs font-medium rounded-full',
                    mcp.isEnabled ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                  ]"
                >
                  {{ mcp.isEnabled ? 'Activo' : 'Inactivo' }}
                </span>
              </div>
              <div class="mt-2 flex flex-wrap gap-x-3 gap-y-1 text-xs text-gray-500 dark:text-gray-400">
                <span>{{ mcp.toolCount }} tools</span>
                <span>{{ mcp.promptCount }} prompts</span>
                <span>{{ mcp.resourceCount }} resources</span>
              </div>
            </router-link>
            <div class="flex gap-1 shrink-0">
              <button
                v-if="mcp.securityType === 'ApiKey' || mcp.securityType === 'OAuth'"
                type="button"
                @click.stop="openHelpSlider(mcp)"
                class="p-2 rounded-lg text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700 active:bg-gray-200 dark:active:bg-gray-600 transition-colors touch-manipulation"
                title="Cómo conectar"
                aria-label="Cómo conectar"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
              </button>
              <button
                type="button"
                @click.stop="openEditSlider(mcp)"
                class="p-2 rounded-lg text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700 active:bg-gray-200 dark:active:bg-gray-600 transition-colors touch-manipulation"
                title="Editar"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
              </button>
              <button
                type="button"
                @click.stop="confirmDelete(mcp)"
                class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 active:bg-red-100 dark:active:bg-red-900/30 transition-colors touch-manipulation"
                title="Eliminar"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
              </button>
            </div>
          </div>
        </div>
    </div>
    </main>

    <!-- Create MCP - Slider Panel -->
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
          v-if="showCreateModal"
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
          v-if="showCreateModal"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-md sm:max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Crear MCP</h2>
                <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  Define la configuración de tu <strong>MCP Server</strong>. Un MCP Definition describe qué tools, prompts y resources expondrá el servidor a clientes como Cursor o Claude.
                </p>
              </div>
              <button
                type="button"
                @click="closeCreateModal"
                class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
                aria-label="Cerrar"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          </div>
          <form @submit.prevent="handleCreate" class="flex flex-col flex-1 min-h-0">
            <div class="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
                <input
                  v-model="createForm.name"
                  type="text"
                  required
                  placeholder="Mi MCP"
                  class="w-full px-3 py-2 rounded-lg border bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  :class="(createFormTouched.name && !createForm.name?.trim()) ? 'border-red-500 dark:border-red-500 ring-1 ring-red-500/20' : 'border-gray-300 dark:border-gray-600'"
                  @blur="createFormTouched.name = true"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción (opcional)</label>
                <textarea v-model="createForm.description" rows="2" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Seguridad</label>
                <select v-model="createForm.securityType" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white">
                  <option value="ApiKey">ApiKey</option>
                  <option value="OAuth">OAuth Flow</option>
                </select>
              </div>
            </div>
            <div class="shrink-0 p-4 sm:p-6 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 bg-gray-50 dark:bg-gray-800/50">
              <button type="button" @click="closeCreateModal" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700">Cancelar</button>
              <button type="submit" :disabled="createLoading || !createForm.name?.trim()" :class="['w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors', (createLoading || !createForm.name?.trim()) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']">Crear</button>
            </div>
          </form>
        </div>
      </Transition>
    </Teleport>

    <!-- Edit MCP - Slider Panel -->
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
          v-if="showEditSlider"
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
          v-if="showEditSlider"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-md sm:max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Editar MCP</h2>
                <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  Modifica la configuración de tu <strong>MCP Server</strong>.
                </p>
              </div>
              <button
                type="button"
                @click="showEditSlider = false"
                class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg shrink-0"
                aria-label="Cerrar"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
          </div>
          <form v-if="editingMcp" @submit.prevent="handleEdit" class="flex flex-col flex-1 min-h-0">
            <div class="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre</label>
                <input v-model="editForm.name" type="text" required class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción (opcional)</label>
                <textarea v-model="editForm.description" rows="2" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Versión</label>
                <input v-model="editForm.version" type="text" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Seguridad</label>
                <select v-model="editForm.securityType" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white">
                  <option value="None">Ninguna</option>
                  <option value="ApiKey">ApiKey</option>
                  <option value="OAuth">OAuth Flow</option>
                </select>
              </div>
              <div class="flex items-center gap-3">
                <label class="relative inline-flex items-center cursor-pointer">
                  <input v-model="editForm.isEnabled" type="checkbox" class="sr-only peer" />
                  <div class="w-11 h-6 bg-gray-200 dark:bg-gray-700 peer-focus:ring-4 peer-focus:ring-primary-300 dark:peer-focus:ring-primary-800 rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600" />
                  <span class="ms-3 text-sm font-medium text-gray-700 dark:text-gray-300">Habilitado</span>
                </label>
              </div>
            </div>
            <div class="shrink-0 p-4 sm:p-6 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 bg-gray-50 dark:bg-gray-800/50">
              <button type="button" @click="showEditSlider = false" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700">Cancelar</button>
              <button type="submit" :disabled="editLoading" :class="['w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors', editLoading ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']">Guardar</button>
            </div>
          </form>
        </div>
      </Transition>
    </Teleport>

    <!-- Help: Cómo conectar - Slider Panel -->
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
          v-if="showHelpSlider"
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
          v-if="showHelpSlider && helpMcp"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-md sm:max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
          @click.stop
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 shrink-0">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white">Cómo conectar</h2>
            <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
              {{ helpMcp.name }} · {{ securityTypeLabel(helpMcp.securityType) }}
            </p>
          </div>
          <div class="flex-1 overflow-y-auto p-4 sm:p-6">
            <McpHowToConnect
              :security-type="helpMcp.securityType"
              :server-id="helpMcp.id"
              :base-url="mcpBaseUrl"
              :show-title="false"
            />
          </div>
          <div class="shrink-0 p-4 sm:p-6 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 bg-gray-50 dark:bg-gray-800/50">
            <button
              type="button"
              @click="showHelpSlider = false"
              class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 font-medium"
            >
              Cerrar
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Delete Confirmation Modal -->
    <Teleport to="body">
      <div
        v-if="showDeleteModal"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar MCP</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">
            ¿Eliminar el MCP <strong class="text-gray-900 dark:text-white">{{ mcpToDelete?.name }}</strong> y todos sus tools, prompts y resources? Esta acción no se puede deshacer.
          </p>
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
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import McpHowToConnect from '../../components/mcp/McpHowToConnect.vue'
import PlanLimitAlert from '../../components/usage/PlanLimitAlert.vue'
import { useAccountUsage } from '../../composables/useAccountUsage'

const toast = useToast()
const { isAtLimitForMcpServers, getLimitMessageForMcpServers, fetchUsage } = useAccountUsage()
const mcpServersLimitMessage = computed(() => getLimitMessageForMcpServers())

function onClickCreateMcpServer() {
  if (isAtLimitForMcpServers.value) {
    toast.error(getLimitMessageForMcpServers())
    return
  }
  showCreateModal.value = true
}
const mcps = ref([])
const loading = ref(true)
const error = ref(null)
const showCreateModal = ref(false)
const showEditSlider = ref(false)
const showHelpSlider = ref(false)
const helpMcp = ref(null)
const showDeleteModal = ref(false)
// MCP URL: in production use backend base (e.g. https://backend.meetgravity.io/channels); in dev use origin (proxy serves /mcp).
const apiUrl = import.meta.env.VITE_API_URL || ''
const mcpBaseUrl = computed(() => {
  if (typeof window === 'undefined') return ''
  if (apiUrl && apiUrl !== '/api') return apiUrl.replace(/\/api\/?$/, '')
  return window.location.origin
})
const createLoading = ref(false)
const editLoading = ref(false)
const deleteLoading = ref(false)

const mcpToDelete = ref(null)

const editingMcp = ref(null)
const createForm = ref({ name: '', description: '', securityType: 'ApiKey' })
const createFormTouched = ref({ name: false })
const editForm = ref({ name: '', description: '', version: '', isEnabled: true, securityType: 'None' })

function securityTypeLabel(type) {
  const labels = { None: 'Sin seguridad', ApiKey: 'ApiKey', OAuth: 'OAuth' }
  return labels[type] ?? type
}

function securityTypeBadgeClass(type) {
  const t = (type || '').toString()
  if (t === 'OAuth') return 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400'
  if (t === 'ApiKey') return 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400'
  return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
}

function mcpIntegrationsLabel(mcp) {
  const ints = mcp.integrations ?? []
  if (ints.length === 0) return 'Sin integraciones'
  return ints.map(i => `${i.name} (${i.provider})`).join(', ')
}

async function loadMcps() {
  loading.value = true
  error.value = null
  try {
    mcps.value = await apiService.getMyMcps()
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar MCPs'
  } finally {
    loading.value = false
  }
}

function closeCreateModal() {
  showCreateModal.value = false
  createForm.value = { name: '', description: '', securityType: 'ApiKey' }
  createFormTouched.value = { name: false }
}

function openHelpSlider(mcp) {
  helpMcp.value = mcp
  showHelpSlider.value = true
}

function openEditSlider(mcp) {
  editingMcp.value = mcp
  editForm.value = {
    name: mcp.name,
    description: mcp.description ?? '',
    version: mcp.version ?? '',
    isEnabled: mcp.isEnabled ?? true,
    securityType: mcp.securityType ?? 'None'
  }
  showEditSlider.value = true
}

function confirmDelete(mcp) {
  mcpToDelete.value = mcp
  showDeleteModal.value = true
}

async function handleDelete() {
  if (!mcpToDelete.value) return
  deleteLoading.value = true
  try {
    await apiService.deleteMcp(mcpToDelete.value.id)
    toast.success('MCP eliminado')
    showDeleteModal.value = false
    mcpToDelete.value = null
    await loadMcps()
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al eliminar')
  } finally {
    deleteLoading.value = false
  }
}

async function handleEdit() {
  if (!editingMcp.value) return
  editLoading.value = true
  try {
    await apiService.updateMcp(editingMcp.value.id, {
      name: editForm.value.name,
      description: editForm.value.description || undefined,
      version: editForm.value.version || undefined,
      isEnabled: editForm.value.isEnabled,
      securityType: editForm.value.securityType
    })
    toast.success('MCP actualizado')
    showEditSlider.value = false
    editingMcp.value = null
    await loadMcps()
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al guardar')
  } finally {
    editLoading.value = false
  }
}

async function handleCreate() {
  createLoading.value = true
  try {
    const result = await apiService.createMcp({
      name: createForm.value.name,
      description: createForm.value.description || undefined,
      securityType: createForm.value.securityType || 'ApiKey'
    })
    toast.success('MCP creado')
    closeCreateModal()
    await loadMcps()
    window.location.href = `/admin/mcps/${result.id}`
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al crear')
  } finally {
    createLoading.value = false
  }
}

onMounted(() => {
  fetchUsage()
  loadMcps()
})
</script>

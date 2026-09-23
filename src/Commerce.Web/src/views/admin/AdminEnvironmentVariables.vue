<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between mb-6 sm:mb-8">
        <div class="min-w-0">
          <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white truncate">Variables de entorno</h1>
          <p class="mt-1 text-sm sm:text-base text-gray-600 dark:text-gray-400">
            Secretos por cuenta, agrupados por etiqueta. Los valores se guardan cifrados.
          </p>
          <p class="mt-2 text-xs sm:text-sm text-gray-500 dark:text-gray-400 max-w-3xl">
            <span class="font-medium text-gray-700 dark:text-gray-300">Referencia en MCP / plantillas:</span>
            formato <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-700 font-mono text-xs">{{ refFormat }}</code>
            — p. ej. <code class="font-mono text-xs">{{ exampleRefDefault }}</code> o
            <code class="font-mono text-xs">{{ exampleRefProd }}</code>.
            Grupo y nombre en UPPER_SNAKE; grupo vacío en el formulario equivale a <code class="font-mono text-xs">{{ defaultGroupName }}</code>.
          </p>
        </div>
        <button
          type="button"
          @click="openCreateModal"
          class="inline-flex items-center justify-center gap-2 px-4 py-2.5 sm:py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white transition-colors min-h-[44px] touch-manipulation shrink-0"
        >
          <svg class="w-4 h-4 sm:w-5 sm:h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Nueva variable
        </button>
      </div>

      <div v-if="loading" class="space-y-3 sm:space-y-4">
        <div v-for="i in 4" :key="i" class="h-14 sm:h-16 bg-gray-200 dark:bg-gray-700 rounded-lg animate-pulse" />
      </div>

      <div
        v-else-if="error"
        class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 mb-6"
      >
        <p class="text-red-800 dark:text-red-300 text-sm sm:text-base">{{ error }}</p>
      </div>

      <div
        v-else-if="!items.length"
        class="text-center py-12 sm:py-16 px-4 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700"
      >
        <p class="text-sm sm:text-base text-gray-500 dark:text-gray-400">No hay variables configuradas</p>
        <button
          type="button"
          @click="openCreateModal"
          class="mt-6 inline-flex items-center justify-center gap-2 px-4 py-2.5 min-h-[44px] touch-manipulation rounded-lg bg-primary-600 hover:bg-primary-700 text-white"
        >
          Crear variable
        </button>
      </div>

      <div v-else class="space-y-8">
        <section v-for="{ key: groupKey, label: groupLabel, rows } in groupedRows" :key="groupKey">
          <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between mb-3">
            <h2 class="text-sm font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              {{ groupLabel }}
            </h2>
            <button
              type="button"
              @click="openCreateModalForGroup(groupKey)"
              class="inline-flex items-center justify-center gap-1.5 px-3 py-2 text-sm font-medium rounded-lg border border-primary-200 dark:border-primary-800 text-primary-700 dark:text-primary-300 bg-primary-50 dark:bg-primary-900/20 hover:bg-primary-100 dark:hover:bg-primary-900/40 transition-colors min-h-[40px] touch-manipulation shrink-0 self-start sm:self-auto"
            >
              <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Agregar variable
            </button>
          </div>
          <div class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="md:hidden divide-y divide-gray-200 dark:divide-gray-700">
              <div v-for="row in rows" :key="row.id" class="p-4 space-y-2">
                <div class="flex justify-between gap-2">
                  <div class="min-w-0">
                    <span class="font-semibold text-gray-900 dark:text-white">{{ row.name }}</span>
                    <div class="mt-1 flex items-center gap-2 flex-wrap">
                      <span class="text-xs text-gray-500 dark:text-gray-400">Referencia</span>
                      <code class="text-xs font-mono px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-900 text-primary-700 dark:text-primary-300">{{ envRefToken(row) }}</code>
                      <button
                        type="button"
                        class="text-xs text-primary-600 dark:text-primary-400 hover:underline"
                        title="Copiar referencia"
                        @click="copyRef(row)"
                      >
                        Copiar
                      </button>
                    </div>
                  </div>
                  <div class="flex gap-1 shrink-0">
                    <button
                      v-if="canReveal"
                      type="button"
                      class="p-2 rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700"
                      :title="valueVisible[row.id] ? 'Ocultar valor' : 'Ver valor'"
                      @click="toggleReveal(row.id)"
                    >
                      <svg v-if="!valueVisible[row.id]" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                      <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      class="p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20"
                      title="Editar"
                      @click="openEditModal(row)"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      class="p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                      title="Eliminar"
                      @click="confirmDelete(row)"
                    >
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </div>
                <p class="font-mono text-sm text-gray-600 dark:text-gray-300 break-all">
                  {{ displayValue(row.id) }}
                </p>
                <p class="text-xs text-gray-500">{{ formatDate(row.createdAt) }}</p>
              </div>
            </div>

            <div class="hidden md:block overflow-x-auto">
              <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-700/50">
                  <tr>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Nombre</th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Referencia</th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Valor</th>
                    <th class="px-4 lg:px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Creada</th>
                    <th class="px-4 lg:px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Acciones</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                  <tr v-for="row in rows" :key="row.id" class="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                    <td class="px-4 lg:px-6 py-4 font-medium text-gray-900 dark:text-white">{{ row.name }}</td>
                    <td class="px-4 lg:px-6 py-4">
                      <div class="flex items-center gap-2 flex-wrap">
                        <code class="text-sm font-mono px-2 py-1 rounded bg-gray-100 dark:bg-gray-900 text-primary-800 dark:text-primary-300">{{ envRefToken(row) }}</code>
                        <button
                          type="button"
                          class="text-xs font-medium text-primary-600 dark:text-primary-400 hover:underline shrink-0"
                          title="Copiar"
                          @click="copyRef(row)"
                        >
                          Copiar
                        </button>
                      </div>
                    </td>
                    <td class="px-4 lg:px-6 py-4">
                      <div class="flex items-center gap-2 min-w-0">
                        <span class="font-mono text-sm text-gray-700 dark:text-gray-300 truncate">{{ displayValue(row.id) }}</span>
                        <button
                          v-if="canReveal"
                          type="button"
                          class="p-1.5 rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 shrink-0"
                          :title="valueVisible[row.id] ? 'Ocultar' : 'Mostrar'"
                          @click="toggleReveal(row.id)"
                        >
                          <svg v-if="!valueVisible[row.id]" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                          </svg>
                          <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                          </svg>
                        </button>
                      </div>
                    </td>
                    <td class="px-4 lg:px-6 py-4 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(row.createdAt) }}</td>
                    <td class="px-4 lg:px-6 py-4 text-right">
                      <button
                        type="button"
                        class="inline-flex p-2 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20 mr-1"
                        title="Editar"
                        @click="openEditModal(row)"
                      >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                        </svg>
                      </button>
                      <button
                        type="button"
                        class="inline-flex p-2 rounded-lg text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                        title="Eliminar"
                        @click="confirmDelete(row)"
                      >
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </section>
      </div>
    </main>

    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="formModalOpen" class="fixed inset-0 z-50 bg-black/50" />
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
          v-if="formModalOpen"
          class="fixed top-0 right-0 z-[51] h-full w-full max-w-md sm:max-w-lg bg-white dark:bg-gray-800 shadow-xl border-l border-gray-200 dark:border-gray-700 flex flex-col"
        >
          <div class="p-4 sm:p-6 border-b border-gray-200 dark:border-gray-700 flex items-start justify-between shrink-0">
            <div>
              <h3 class="text-lg font-semibold text-gray-900 dark:text-white">
                {{ editingId ? 'Editar variable' : 'Nueva variable' }}
              </h3>
              <p v-if="editingId" class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Puedes cambiar el grupo, el nombre o el valor (deja el valor vacío para no cambiar el secreto).
              </p>
              <p v-else-if="groupFieldLocked" class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Se añadirá al grupo «{{ formGroup || defaultGroupName }}». Completa nombre y valor.
              </p>
              <p v-else class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Orden: grupo, nombre y valor. Si dejas el grupo vacío se guardará como «{{ defaultGroupName }}».
              </p>
            </div>
            <button type="button" class="p-2 -m-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg" aria-label="Cerrar" @click="closeFormModal">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Grupo</label>
              <input
                v-model="formGroup"
                type="text"
                autocomplete="off"
                class="w-full px-3 py-2 rounded-lg border text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 font-mono text-sm uppercase"
                :class="[
                  groupFieldLocked && !editingId
                    ? 'border-gray-200 dark:border-gray-600 bg-gray-100 dark:bg-gray-900/60 cursor-not-allowed'
                    : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700',
                  formGroupTouched && !isFormGroupValid && !(groupFieldLocked && !editingId) ? 'border-red-500 dark:border-red-600' : ''
                ]"
                :readonly="groupFieldLocked && !editingId"
                :placeholder="groupFieldLocked ? '' : `Vacío → ${defaultGroupName}`"
                :aria-readonly="groupFieldLocked && !editingId ? 'true' : undefined"
                maxlength="200"
                @blur="normalizeFormGroupCase"
                @input="formGroupTouched = true"
              />
              <p v-if="groupFieldLocked && !editingId" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                Grupo fijo (desde la sección donde pulsaste Agregar variable).
              </p>
              <p v-else class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                UPPER_SNAKE como el nombre (p. ej. PRODUCTION). Vacío → <code class="font-mono">{{ defaultGroupName }}</code>. Mismo nombre puede repetirse en otro grupo.
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre (clave) <span class="text-red-500">*</span></label>
              <input
                v-model="formName"
                type="text"
                autocomplete="off"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 font-mono text-sm uppercase"
                :class="formNameTouched && !isFormNameValid ? 'border-red-500 dark:border-red-600' : ''"
                placeholder="ej. STRIPE_SECRET_KEY"
                maxlength="200"
                @blur="normalizeFormNameCase"
                @input="formNameTouched = true"
              />
              <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                UPPER_SNAKE: letras, números y guión bajo; debe empezar por letra. Único dentro del mismo grupo.
              </p>
              <p v-if="isFormNameValid && isFormGroupValid" class="mt-1 text-xs text-gray-600 dark:text-gray-300">
                Referencia MCP:
                <code class="font-mono px-1 rounded bg-gray-100 dark:bg-gray-900">{{ envMcpRefPreview() }}</code>
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Valor <span v-if="!editingId" class="text-red-500">*</span>
              </label>
              <input
                v-model="formValue"
                type="password"
                autocomplete="off"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-primary-500 font-mono text-sm"
                :placeholder="editingId ? 'Dejar vacío para no cambiar el secreto' : ''"
              />
            </div>
          </div>
          <div class="shrink-0 px-4 sm:px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex flex-col-reverse sm:flex-row sm:justify-end gap-3 bg-gray-50 dark:bg-gray-700/50">
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700" @click="closeFormModal">
              Cancelar
            </button>
            <button
              type="button"
              :disabled="formSubmitting || !isFormNameValid || !isFormGroupValid || (!editingId && !formValue?.trim())"
              :class="[
                'w-full sm:w-auto px-4 py-2.5 rounded-lg font-medium',
                formSubmitting || !isFormNameValid || !isFormGroupValid || (!editingId && !formValue?.trim())
                  ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed'
                  : 'bg-primary-600 hover:bg-primary-700 text-white'
              ]"
              @click="submitForm"
            >
              {{ formSubmitting ? 'Guardando...' : 'Guardar' }}
            </button>
          </div>
        </div>
      </Transition>
    </Teleport>

    <Teleport to="body">
      <div
        v-if="deleteTarget"
        class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4 bg-black/50"
      >
        <div class="bg-white dark:bg-gray-800 rounded-t-2xl sm:rounded-xl shadow-xl max-w-md w-full p-5 pb-8 sm:p-6">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Eliminar variable</h3>
          <p class="mt-2 text-gray-600 dark:text-gray-400">¿Eliminar "{{ deleteTarget?.name }}"? Esta acción no se puede deshacer.</p>
          <div class="mt-6 flex flex-col-reverse sm:flex-row sm:justify-end gap-3">
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg" @click="deleteTarget = null">
              Cancelar
            </button>
            <button type="button" class="w-full sm:w-auto px-4 py-2.5 bg-red-600 hover:bg-red-700 text-white rounded-lg font-medium" @click="doDelete">Eliminar</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, reactive } from 'vue'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import { useAppStore } from '../../stores/appStore'

const toast = useToast()
const store = useAppStore()

const loading = ref(true)
const error = ref('')
const items = ref([])

const formModalOpen = ref(false)
const editingId = ref(null)
const formName = ref('')
const formGroup = ref('')
const formValue = ref('')
const formSubmitting = ref(false)
/** true = crear desde «Agregar variable» de un grupo; el campo Grupo va relleno y solo lectura */
const groupFieldLocked = ref(false)
const formNameTouched = ref(false)
const formGroupTouched = ref(false)

const nameConventionMaxLen = 200
/** Alineado con EnvironmentVariableNameConvention en backend (UPPER_SNAKE). */
const nameConventionPattern = /^[A-Z][A-Z0-9_]*$/

const isFormNameValid = computed(() => {
  const t = (formName.value ?? '').trim().toUpperCase()
  return t.length > 0 && t.length <= nameConventionMaxLen && nameConventionPattern.test(t)
})

/** Vacío → DEFAULT (válido); si hay texto, misma regla que el nombre. */
const isFormGroupValid = computed(() => {
  if (groupFieldLocked.value) return true
  const t = (formGroup.value ?? '').trim().toUpperCase()
  if (t.length === 0) return true
  return t.length <= nameConventionMaxLen && nameConventionPattern.test(t)
})

function normalizeFormNameCase() {
  const t = (formName.value ?? '').trim().toUpperCase()
  formName.value = t
}

function normalizeFormGroupCase() {
  if (groupFieldLocked.value) return
  const t = (formGroup.value ?? '').trim().toUpperCase()
  formGroup.value = t
}

const deleteTarget = ref(null)

const valueVisible = reactive({})
const plainValues = reactive({})

const canReveal = computed(() => store.profile?.isAccountOwner === true || store.profile?.isSuperAdmin === true)

/** Coincide con DefaultGroupCanonical en backend. */
const defaultGroupName = 'DEFAULT'

const refFormat = '{GRUPO.NOMBRE}'
const exampleRefDefault = '{DEFAULT.DB}'
const exampleRefProd = '{PRODUCTION.DB}'

function canonicalGroupForRef(rawGroup) {
  const t = (rawGroup ?? '').trim().toUpperCase()
  return t || defaultGroupName
}

function envRefToken(row) {
  const n = (row?.name ?? '').trim()
  if (!n) return ''
  const g = canonicalGroupForRef(row?.group)
  return `{${g}.${n}}`
}

function envMcpRefPreview() {
  const n = (formName.value ?? '').trim().toUpperCase()
  if (!n || !isFormNameValid.value) return ''
  const g = canonicalGroupForRef(formGroup.value)
  return `{${g}.${n}}`
}

async function copyRef(row) {
  const t = envRefToken(row)
  if (!t) return
  try {
    await navigator.clipboard.writeText(t)
    toast.success('Referencia copiada')
  } catch {
    toast.error('No se pudo copiar')
  }
}

const groupedRows = computed(() => {
  const map = new Map()
  for (const row of items.value) {
    const raw = (row.group ?? '').trim()
    const key = canonicalGroupForRef(raw)
    if (!map.has(key)) map.set(key, [])
    map.get(key).push(row)
  }
  return Array.from(map.entries())
    .sort((a, b) => a[0].localeCompare(b[0], 'es'))
    .map(([key, rows]) => ({ key, label: key, rows }))
})

/** Grupo que se envía al API: vacío → DEFAULT (normalizado a mayúsculas). */
function resolvedGroupForApi(trimmedFormGroup) {
  const t = (trimmedFormGroup ?? '').trim().toUpperCase()
  return t || defaultGroupName
}

function formatDate(iso) {
  if (!iso) return ''
  try {
    return new Date(iso).toLocaleString()
  } catch {
    return String(iso)
  }
}

function displayValue(id) {
  if (valueVisible[id] && plainValues[id] != null) return plainValues[id]
  const row = items.value.find((r) => r.id === id)
  return row?.valueMasked ?? '••••••••'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    await store.fetchProfile()
    const data = await apiService.getEnvironmentVariables()
    items.value = Array.isArray(data) ? data : []
    Object.keys(valueVisible).forEach((k) => delete valueVisible[k])
    Object.keys(plainValues).forEach((k) => delete plainValues[k])
  } catch (e) {
    error.value = e.response?.data?.error || e.message || 'Error al cargar'
  } finally {
    loading.value = false
  }
}

async function toggleReveal(id) {
  if (!canReveal.value) return
  if (valueVisible[id]) {
    valueVisible[id] = false
    return
  }
  if (plainValues[id] == null) {
    try {
      const data = await apiService.revealEnvironmentVariableValue(id)
      plainValues[id] = data?.value ?? ''
      valueVisible[id] = true
    } catch (e) {
      const msg = e.response?.data?.detail || e.response?.data?.error || e.message || 'No se pudo obtener el valor'
      toast.error(msg)
    }
    return
  }
  valueVisible[id] = true
}

function openCreateModal() {
  editingId.value = null
  groupFieldLocked.value = false
  formNameTouched.value = false
  formGroupTouched.value = false
  formName.value = ''
  formGroup.value = ''
  formValue.value = ''
  formModalOpen.value = true
}

function openCreateModalForGroup(groupKey) {
  editingId.value = null
  groupFieldLocked.value = true
  formNameTouched.value = false
  formGroupTouched.value = false
  formName.value = ''
  formGroup.value = groupKey
  formValue.value = ''
  formModalOpen.value = true
}

function openEditModal(row) {
  editingId.value = row.id
  groupFieldLocked.value = false
  formNameTouched.value = false
  formGroupTouched.value = false
  formName.value = (row.name ?? '').trim().toUpperCase()
  formGroup.value = canonicalGroupForRef(row.group)
  formValue.value = ''
  formModalOpen.value = true
}

function closeFormModal() {
  formModalOpen.value = false
  groupFieldLocked.value = false
}

async function submitForm() {
  formSubmitting.value = true
  try {
    const groupForApi = resolvedGroupForApi(formGroup.value)
    if (editingId.value) {
      await apiService.updateEnvironmentVariable(editingId.value, {
        name: formName.value.trim(),
        group: groupForApi,
        value: formValue.value.trim() || undefined
      })
      toast.success('Variable actualizada')
    } else {
      await apiService.createEnvironmentVariable({
        name: formName.value.trim(),
        group: groupForApi,
        value: formValue.value
      })
      toast.success('Variable creada')
    }
    closeFormModal()
    await load()
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al guardar')
  } finally {
    formSubmitting.value = false
  }
}

function confirmDelete(row) {
  deleteTarget.value = row
}

async function doDelete() {
  const row = deleteTarget.value
  if (!row) return
  try {
    await apiService.deleteEnvironmentVariable(row.id)
    toast.success('Eliminada')
    deleteTarget.value = null
    await load()
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al eliminar')
  }
}

onMounted(() => {
  load()
})
</script>

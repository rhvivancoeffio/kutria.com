<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-6">
      <div v-if="loading" class="space-y-4">
        <div class="h-8 w-48 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        <div class="h-64 bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse" />
      </div>

      <div v-else-if="error" class="p-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800">
        <p class="text-red-800 dark:text-red-300">{{ error }}</p>
        <router-link :to="backUrl" class="mt-2 inline-block text-sm text-primary-600 dark:text-primary-400">← Volver a server</router-link>
      </div>

      <div v-else class="space-y-6">
        <div class="space-y-1">
          <router-link :to="backUrl" class="text-sm text-gray-500 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400">← Volver a server</router-link>
          <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ isEdit ? 'Editar Prompt' : 'Nuevo prompt' }}</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400">Instrucciones por roles (System, User, Assistant, Tool), tools permitidas y parámetros de inferencia.</p>
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <form @submit.prevent="handleSave" class="p-4 sm:p-6 space-y-6">
            <div v-if="!isEdit">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Empezar desde plantilla (opcional)</label>
              <SearchableSelect
                :model-value="selectedTemplateId == null ? '' : selectedTemplateId"
                :options="templateSelectOptions"
                placeholder="— Sin plantilla —"
                search-placeholder="Buscar plantilla..."
                empty-message="No hay coincidencias"
                :always-show-empty-option="true"
                teleport
                @update:model-value="onTemplateSelectValue"
              />
              <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                Si eliges una plantilla, se rellenan mensajes y argumentos. Puedes editarlos después.
                <template v-if="templatesList.length === 0">
                  <router-link to="/admin/templates" class="text-primary-600 dark:text-primary-400 hover:underline">Crear plantillas</router-link> para usarlas aquí.
                </template>
              </p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre <span class="text-red-500">*</span></label>
              <input
                v-model="form.name"
                type="text"
                required
                placeholder="my_prompt"
                :class="[
                  'w-full px-3 py-2 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white transition-colors',
                  nameTouched && !isNameValid
                    ? 'border-2 border-red-500 focus:border-red-500 focus:ring-red-500'
                    : 'border border-gray-300 dark:border-gray-600 focus:border-primary-500 focus:ring-primary-500'
                ]"
                @blur="nameTouched = true"
                @input="form.name = (form.name || '').replace(/[^a-zA-Z0-9_]/g, '')"
              />
              <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Alfanumérico y sin espacios (ej: my_prompt)</p>
              <p v-if="nameTouched && form.name && !isNameValid" class="mt-1 text-xs text-red-500">Solo letras, números y guión bajo.</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Título (opcional)</label>
              <input v-model="form.title" type="text" placeholder="Mi prompt" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Descripción (opcional)</label>
              <textarea v-model="form.description" rows="2" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
            </div>

            <!-- Split view: Mensajes por rol (izq, más ancho) | Argumentos (der) -->
            <div class="grid grid-cols-1 lg:grid-cols-[3fr_2fr] gap-4 lg:gap-6 lg:items-start">
              <!-- Izquierda: Mensajes por rol -->
              <div class="flex flex-col min-h-0 rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/30 dark:bg-gray-800/30 overflow-hidden">
                <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Mensajes por rol <span class="text-red-500">*</span></label>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Orden: system, user, assistant, tool. Argumentos: <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600">&#123;&#123;nombre&#125;&#125;</code> (o &#123;&#123;...&#125;&#125; para crearlos). Snippets: <code class="px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600">&#123;&#123;snippet:Nombre&#125;&#125;</code> inserta el contenido del snippet (crea snippets en Admin → Snippets).</p>
                </div>
                <div class="p-3 flex-1 min-h-[280px] lg:min-h-[360px] overflow-y-auto space-y-3">
                  <div
                    v-for="(block, idx) in form.messageBlocks"
                    :key="idx"
                    class="p-3 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700/50 transition-colors"
                    :class="{ 'opacity-50': dragFromIndex === idx, 'ring-2 ring-primary-400 dark:ring-primary-500 border-primary-400 dark:border-primary-500': dragOverIndex === idx }"
                    @dragover.prevent="onBlockDragOver($event, idx)"
                    @dragleave="onBlockDragLeave(idx)"
                    @drop.prevent="onBlockDrop($event, idx)"
                  >
                    <div class="flex items-center justify-between gap-2 mb-2">
                      <div class="flex items-center gap-2 shrink-0">
                        <span
                          draggable="true"
                          class="p-1.5 rounded cursor-grab active:cursor-grabbing text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 touch-none"
                          title="Arrastrar para reordenar"
                          @dragstart="onBlockDragStart($event, idx)"
                          @dragend="onBlockDragEnd"
                        >
                          <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path d="M8 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm8-12a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4zm0 6a2 2 0 1 0 0 4 2 2 0 0 0 0-4z" /></svg>
                        </span>
                        <select
                          v-model="block.role"
                          class="px-2.5 py-1 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                        >
                          <option value="system">System</option>
                          <option value="user">User</option>
                          <option value="assistant">Assistant</option>
                          <option value="tool">Tool</option>
                        </select>
                      </div>
                      <button type="button" @click="removeMessageBlock(idx)" class="p-1.5 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded hover:bg-gray-200 dark:hover:bg-gray-600 shrink-0" title="Quitar bloque">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                      </button>
                    </div>
                    <textarea
                      v-model="block.content"
                      rows="3"
                      :placeholder="placeholderForRole(block.role)"
                      class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                      @input="syncArgumentsFromMessageBlocks"
                    />
                    <div v-if="block.role === 'tool' && (mcp?.tools?.length ?? 0) > 0" class="mt-2 space-y-2">
                      <div class="flex flex-wrap items-center gap-1.5">
                        <span class="text-xs text-gray-500 dark:text-gray-400 shrink-0">Seleccionadas (este bloque):</span>
                        <template v-if="(getBlockAllowedToolIds(block) || []).length === 0">
                          <span class="text-xs text-gray-500 dark:text-gray-400 italic">Todas las tools del MCP</span>
                        </template>
                        <template v-else>
                          <span
                            v-for="tid in (getBlockAllowedToolIds(block) || [])"
                            :key="tid"
                            class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 text-xs text-gray-700 dark:text-gray-300"
                          >
                            {{ getToolNameById(tid) }}
                            <button type="button" @click="removeBlockAllowedTool(idx, tid)" class="p-0.5 -mr-0.5 rounded hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-500 hover:text-red-600 dark:hover:text-red-400" title="Quitar">×</button>
                          </span>
                        </template>
                      </div>
                      <div>
                        <button type="button" @click="openAllowedToolsModalForBlock(idx)" class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg border-2 border-primary-500 dark:border-primary-400 text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/30 font-medium text-xs transition-colors">
                          <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                          Agregar tools permitidos
                        </button>
                      </div>
                    </div>
                  </div>
                  <button type="button" @click="addMessageBlock" class="w-full py-2 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors">
                    + Añadir mensaje
                  </button>
                </div>
                <div class="px-4 py-2 border-t border-gray-200 dark:border-gray-600 shrink-0 bg-white/50 dark:bg-gray-800/50">
                  <p v-if="messageBlocksTouched && !hasValidMessageBlocks" class="text-xs text-red-500">Al menos un bloque debe tener contenido no vacío.</p>
                </div>
              </div>

              <!-- Derecha: Argumentos -->
              <ArgumentsFormArray
                v-model="argumentsFields"
                title="Argumentos"
                hint="En los mensajes usa <code class='px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600'>&#123;&#123;nombre&#125;&#125;</code> (ej: <code class='px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600'>&#123;&#123;name&#125;&#125;</code>, <code class='px-1 py-0.5 rounded bg-gray-200 dark:bg-gray-600'>&#123;&#123;order_id&#125;&#125;</code>). Al escribir &#123;&#123;...&#125;&#125; se añade aquí."
                item-label="Argumento"
                add-button-label="+ Añadir argumento"
              />
            </div>

            <!-- Tools permitidas: resumen por bloque (solo lectura) -->
            <div v-if="(mcp?.tools?.length ?? 0) > 0 && hasToolRoleBlock" class="p-4 rounded-xl border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tools permitidas por bloque (resumen)</label>
              <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">Cada bloque con rol Tool tiene su propia lista. Para cambiar, usa el botón «Agregar tools permitidos» en ese bloque.</p>
              <div class="space-y-2">
                <template v-for="(block, idx) in form.messageBlocks" :key="idx">
                  <div v-if="block.role === 'tool'" class="flex flex-wrap items-center gap-2 py-1.5 px-2 rounded-lg bg-white dark:bg-gray-800/50 border border-gray-200 dark:border-gray-600">
                    <span class="text-xs font-medium text-gray-600 dark:text-gray-400 shrink-0">Bloque {{ idx + 1 }} (Tool):</span>
                    <template v-if="(getBlockAllowedToolIds(block) || []).length === 0">
                      <span class="text-xs text-gray-500 dark:text-gray-400 italic">Todas las tools</span>
                    </template>
                    <template v-else>
                      <span
                        v-for="tid in (getBlockAllowedToolIds(block) || [])"
                        :key="tid"
                        class="inline-flex items-center px-2 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs text-gray-700 dark:text-gray-300"
                      >
                        {{ getToolNameById(tid) }}
                      </span>
                    </template>
                  </div>
                </template>
              </div>
            </div>

            <!-- Inference params -->
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Temperature</label>
                <input v-model.number="form.temperature" type="number" min="0" max="2" step="0.1" placeholder="—" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">0–2, opcional</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Top P</label>
                <input v-model.number="form.topP" type="number" min="0" max="1" step="0.05" placeholder="—" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">0–1, opcional</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Top K</label>
                <input v-model.number="form.topK" type="number" min="0" placeholder="—" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">Commerceional</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Max Tokens</label>
                <input v-model.number="form.maxTokens" type="number" min="0" placeholder="—" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                <p class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">Commerceional</p>
              </div>
            </div>

            <!-- Output format -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Formato de salida</label>
              <div class="flex gap-4">
                <label class="inline-flex items-center gap-2 cursor-pointer">
                  <input v-model="form.outputFormat" type="radio" :value="0" class="text-primary-600 focus:ring-primary-500" />
                  <span class="text-sm text-gray-700 dark:text-gray-300">Texto</span>
                </label>
                <label class="inline-flex items-center gap-2 cursor-pointer">
                  <input v-model="form.outputFormat" type="radio" :value="1" class="text-primary-600 focus:ring-primary-500" />
                  <span class="text-sm text-gray-700 dark:text-gray-300">Estructurado (JSON Schema)</span>
                </label>
              </div>
              <div v-if="form.outputFormat === 1" class="mt-3 space-y-2">
                <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Propiedades del objeto de salida (JSON Schema)</label>
                <div class="space-y-2 max-h-[320px] overflow-y-auto">
                  <div
                    v-for="(field, idx) in outputSchemaFields"
                    :key="idx"
                    class="rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700/50 overflow-hidden"
                  >
                    <button
                      type="button"
                      class="w-full flex items-center justify-between gap-2 px-3 py-2.5 text-left hover:bg-gray-50 dark:hover:bg-gray-700/70 transition-colors"
                      @click="toggleOutputSchemaFieldExpanded(idx)"
                    >
                      <span class="text-xs font-medium text-gray-600 dark:text-gray-300">Propiedad {{ idx + 1 }}{{ field.name ? ' — ' + field.name : '' }}</span>
                      <span class="flex items-center gap-1">
                        <svg
                          class="w-4 h-4 text-gray-400 transition-transform"
                          :class="{ 'rotate-180': isOutputSchemaFieldExpanded(idx) }"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                        </svg>
                        <button type="button" @click.stop="removeOutputSchemaField(idx)" class="p-1.5 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded hover:bg-gray-200 dark:hover:bg-gray-600" title="Quitar">
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                        </button>
                      </span>
                    </button>
                    <div v-show="isOutputSchemaFieldExpanded(idx)" class="px-3 pb-3 pt-0 space-y-2 border-t border-gray-100 dark:border-gray-600">
                      <div class="space-y-0.5 pt-2">
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Nombre (clave JSON)</label>
                        <input v-model="field.name" type="text" placeholder="ej: result" class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                      </div>
                      <div class="space-y-0.5">
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Tipo</label>
                        <select v-model="field.type" class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white">
                          <option value="string">string</option>
                          <option value="number">number</option>
                          <option value="boolean">boolean</option>
                          <option value="array">array</option>
                          <option value="object">object</option>
                        </select>
                      </div>
                      <div class="space-y-0.5">
                        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Descripción</label>
                        <input v-model="field.description" type="text" placeholder="(opcional)" class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white" />
                      </div>
                      <div class="flex items-center gap-2">
                        <label class="relative inline-flex items-center cursor-pointer select-none">
                          <input v-model="field.required" type="checkbox" class="sr-only peer" />
                          <div class="w-11 h-6 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-5 after:w-5 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5" />
                          <span class="ml-2 text-sm text-gray-700 dark:text-gray-300">Requerido</span>
                        </label>
                      </div>
                    </div>
                  </div>
                </div>
                <button type="button" @click="addOutputSchemaField" class="w-full py-2 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-primary-400 hover:text-primary-600 dark:hover:text-primary-400 text-sm font-medium transition-colors">
                  + Añadir propiedad
                </button>
              </div>
            </div>

            <div v-if="isEdit" class="flex items-center gap-3">
              <label class="relative inline-flex items-center cursor-pointer select-none">
                <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                <div class="w-12 h-7 bg-gray-300 dark:bg-gray-600 rounded-full shadow-inner peer peer-checked:bg-primary-600 peer-checked:shadow-none transition-colors duration-200 after:content-[''] after:absolute after:top-[3px] after:left-[3px] after:bg-white after:rounded-full after:h-6 after:w-6 after:shadow after:transition-transform after:duration-200 peer-checked:after:translate-x-5 rtl:peer-checked:after:-translate-x-5" />
                <span class="ml-3 text-sm font-medium text-gray-700 dark:text-gray-300">Habilitado</span>
              </label>
              <p class="text-xs text-gray-500 dark:text-gray-400">Si está deshabilitado, el prompt no se expondrá a clientes MCP.</p>
            </div>

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end sm:items-center gap-3 pt-2">
              <router-link :to="backUrl" class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 text-center">Cancelar</router-link>
              <button
                v-if="isEdit"
                type="button"
                class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium text-gray-600 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600"
                @click="showSaveAsTemplateModal = true"
              >
                Guardar como plantilla
              </button>
              <button
                type="button"
                class="w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-600"
                @click="showPromptPreviewSlider = true"
              >
                Vista previa
              </button>
              <button type="submit" :disabled="saving || !formValid" :class="['w-full sm:w-auto px-4 py-2.5 sm:py-2 min-h-[44px] touch-manipulation rounded-lg font-medium transition-colors', (saving || !formValid) ? 'bg-gray-300 dark:bg-gray-600 text-gray-500 cursor-not-allowed' : 'bg-primary-600 hover:bg-primary-700 text-white']">Guardar</button>
            </div>
          </form>
        </div>
      </div>

      <!-- Slider de vista previa (mismo componente que en detalle MCP) -->
      <McpPromptPreviewSlider
        v-model="showPromptPreviewSlider"
        :prompt="previewPromptFromForm"
        :tools="mcp?.tools ?? []"
      />

      <!-- Modal Guardar como plantilla -->
      <Teleport to="body">
        <div v-if="showSaveAsTemplateModal" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-4 bg-black/50">
          <div class="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md p-5" @click.stop>
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Guardar como plantilla</h3>
            <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Crea una plantilla reutilizable con los mensajes y argumentos actuales de este prompt.</p>
            <div class="mt-4">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Nombre de la plantilla <span class="text-red-500">*</span></label>
              <input v-model.trim="saveAsTemplateName" type="text" placeholder="ej: Soporte básico" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm" />
            </div>
            <div class="mt-6 flex justify-end gap-3">
              <button type="button" @click="showSaveAsTemplateModal = false" class="px-4 py-2 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Cancelar</button>
              <button type="button" @click="doSaveAsTemplate" :disabled="savingTemplate || !saveAsTemplateName" class="px-4 py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white disabled:opacity-50">{{ savingTemplate ? 'Guardando…' : 'Guardar plantilla' }}</button>
            </div>
          </div>
        </div>
      </Teleport>

      <!-- Slider: seleccionar tools permitidas -->
      <Teleport to="body">
        <Transition name="allowed-tools-slider">
          <div v-if="showAllowedToolsModal" class="fixed inset-0 z-50 flex">
            <div class="absolute inset-0 bg-black/50" />
            <div class="relative ml-auto w-full max-w-lg h-full bg-white dark:bg-gray-800 shadow-xl flex flex-col animate-slide-in-panel" @click.stop>
              <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
                <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Tools permitidas{{ allowedToolsModalBlockIndex != null ? ` (Bloque ${allowedToolsModalBlockIndex + 1})` : '' }}</h3>
                <button type="button" @click="showAllowedToolsModal = false" class="p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700" aria-label="Cerrar">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
                </button>
              </div>
              <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-600 shrink-0">
                <p class="text-xs text-gray-500 dark:text-gray-400">Marca las tools que este prompt podrá usar. Sin selección = todas permitidas.</p>
                <div class="mt-3 flex items-center gap-2">
                  <input
                    v-model="allowedToolsModalSearch"
                    type="text"
                    placeholder="Buscar por nombre..."
                    class="flex-1 px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
                  />
                  <div class="flex gap-2 shrink-0">
                    <button type="button" @click="selectAllFilteredToolsInModal" class="text-xs px-2 py-1.5 rounded-lg text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/30 font-medium">Todo</button>
                    <button type="button" @click="selectNoFilteredToolsInModal" class="text-xs px-2 py-1.5 rounded-lg text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700">Ninguno</button>
                  </div>
                </div>
              </div>
              <div class="flex-1 min-h-0 overflow-y-auto p-4">
                <div class="space-y-1">
                  <label
                    v-for="tool in filteredToolsForModal"
                    :key="tool.id"
                    class="flex items-center gap-3 px-3 py-2.5 rounded-lg border border-gray-200 dark:border-gray-600 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
                    :class="{ 'border-primary-500 bg-primary-50/50 dark:bg-primary-900/20': selectedToolIdsSet.has(String(tool.id)) }"
                  >
                    <input v-model="allowedToolsModalSelection" type="checkbox" :value="tool.id" class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500" />
                    <div class="min-w-0 flex-1">
                      <span class="text-sm font-medium text-gray-900 dark:text-white">{{ tool.name }}</span>
                      <p v-if="tool.description" class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ tool.description }}</p>
                    </div>
                  </label>
                </div>
                <p v-if="filteredToolsForModal.length === 0" class="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">No hay tools que coincidan con la búsqueda.</p>
              </div>
              <div class="px-4 py-3 border-t border-gray-200 dark:border-gray-600 flex justify-end gap-3 shrink-0">
                <button type="button" @click="showAllowedToolsModal = false" class="px-4 py-2 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700">Cancelar</button>
                <button type="button" @click="applyAllowedToolsSelection(); showAllowedToolsModal = false" class="px-4 py-2 rounded-lg font-medium bg-primary-600 hover:bg-primary-700 text-white">Aplicar</button>
              </div>
            </div>
          </div>
        </Transition>
      </Teleport>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import apiService from '../../services/api'
import McpPromptPreviewSlider from '../../components/mcp/McpPromptPreviewSlider.vue'
import SearchableSelect from '../../components/SearchableSelect.vue'
import ArgumentsFormArray from '../../components/mcp/ArgumentsFormArray.vue'
import { syncMcpArgumentsFromMessageBlocks } from '../../utils/mcpPromptArgumentSync'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const mcpId = computed(() => route.params.mcpId)
const promptId = computed(() => route.params.promptId)
const isEdit = computed(() => !!promptId.value)
const mcp = ref(null)
const loading = ref(true)
const error = ref(null)
const saving = ref(false)
const form = ref({
  name: '',
  title: '',
  description: '',
  messageBlocks: [{ role: 'user', content: '' }],
  temperature: null,
  topP: null,
  topK: null,
  maxTokens: null,
  outputFormat: 0,
  outputSchema: '',
  isEnabled: true
})
const argumentsFields = ref([{ name: '', description: '', required: false, fromTemplate: false }])
/** Form array para formato de salida JSON Schema: propiedades del objeto de salida */
const outputSchemaFields = ref([{ name: '', type: 'string', description: '', required: false }])
const outputSchemaExpanded = ref([false])
const nameTouched = ref(false)
const messageBlocksTouched = ref(false)
const showPromptPreviewSlider = ref(false)
const templatesList = ref([])
const selectedTemplateId = ref(null)
const showSaveAsTemplateModal = ref(false)
const saveAsTemplateName = ref('')
const savingTemplate = ref(false)
const showAllowedToolsModal = ref(false)
const allowedToolsModalBlockIndex = ref(null)
const allowedToolsModalSearch = ref('')
const allowedToolsModalSelection = ref([])
const dragFromIndex = ref(null)
const dragOverIndex = ref(null)

const backUrl = computed(() => `/admin/mcps/${mcpId.value}`)

const templateSelectOptions = computed(() => [
  { value: '', label: '— Sin plantilla —' },
  ...(templatesList.value || []).map((t) => ({
    value: t.id,
    label: `${t.name}${t.title ? ` — ${t.title}` : ''}`
  }))
])

/** Objeto tipo prompt para el slider de vista previa (create/edit): refleja el estado actual del formulario */
const previewPromptFromForm = computed(() => ({
  name: form.value.name?.trim() || '(sin nombre)',
  title: form.value.title?.trim() || null,
  description: form.value.description?.trim() || null,
  argumentsSchema: buildArgumentsSchemaJson(),
  messageBlocks: buildMessageBlocksJson(),
  allowedToolIds: allBlocksAllowedToolIdsUnion.value,
  temperature: form.value.temperature != null && form.value.temperature !== '' ? Number(form.value.temperature) : null,
  topP: form.value.topP != null && form.value.topP !== '' ? Number(form.value.topP) : null,
  topK: form.value.topK != null && form.value.topK !== '' ? Number(form.value.topK) : null,
  maxTokens: form.value.maxTokens != null && form.value.maxTokens !== '' ? Number(form.value.maxTokens) : null,
  outputFormat: form.value.outputFormat,
  outputSchema: form.value.outputFormat === 1 ? (buildOutputSchemaJson() ?? null) : (form.value.outputSchema?.trim() || null),
  isEnabled: form.value.isEnabled,
  version: isEdit.value ? 1 : 0
}))

const nameRegex = /^[a-zA-Z0-9_]+$/
const isNameValid = computed(() => {
  const name = form.value.name?.trim() ?? ''
  return name.length > 0 && nameRegex.test(name)
})

const hasValidMessageBlocks = computed(() => {
  const blocks = form.value.messageBlocks ?? []
  return blocks.some(b => (b.content ?? '').trim().length > 0)
})

/** Hay al menos un bloque de mensaje con rol Tool (mostrar sección tools permitidas + botón). */
const hasToolRoleBlock = computed(() => (form.value.messageBlocks ?? []).some(b => b.role === 'tool'))

/** Unión de todos los allowedToolIds de bloques con rol tool (para API y preview). */
const allBlocksAllowedToolIdsUnion = computed(() => {
  const blocks = form.value.messageBlocks ?? []
  const set = new Set()
  for (const b of blocks) {
    if (b.role !== 'tool') continue
    const ids = getBlockAllowedToolIds(b)
    if (ids) ids.forEach(id => set.add(id))
  }
  return Array.from(set)
})

const formValid = computed(() => {
  return isNameValid.value && hasValidMessageBlocks.value
})

const toolsList = computed(() => mcp.value?.tools ?? [])
const filteredToolsForModal = computed(() => {
  const list = toolsList.value
  const q = (allowedToolsModalSearch.value || '').trim().toLowerCase()
  if (!q) return list
  return list.filter(t => (t.name || '').toLowerCase().includes(q) || (t.description || '').toLowerCase().includes(q))
})

const selectedToolIdsSet = computed(() => new Set((allowedToolsModalSelection.value || []).map(id => String(id))))

function addMessageBlock() {
  form.value.messageBlocks.push({ role: 'user', content: '' })
}

function onBlockDragStart(e, idx) {
  dragFromIndex.value = idx
  e.dataTransfer.effectAllowed = 'move'
  e.dataTransfer.setData('text/plain', String(idx))
  e.dataTransfer.setData('application/json', JSON.stringify({ index: idx }))
}

function onBlockDragEnd() {
  dragFromIndex.value = null
  dragOverIndex.value = null
}

function onBlockDragOver(e, idx) {
  if (dragFromIndex.value === null) return
  if (dragFromIndex.value !== idx) dragOverIndex.value = idx
}

function onBlockDragLeave(idx) {
  if (dragOverIndex.value === idx) dragOverIndex.value = null
}

function onBlockDrop(e, toIndex) {
  const fromIndex = dragFromIndex.value
  dragFromIndex.value = null
  dragOverIndex.value = null
  if (fromIndex == null || fromIndex === toIndex) return
  const blocks = [...(form.value.messageBlocks ?? [])]
  const [moved] = blocks.splice(fromIndex, 1)
  blocks.splice(toIndex, 0, moved)
  form.value.messageBlocks = blocks
}

function removeMessageBlock(idx) {
  form.value.messageBlocks.splice(idx, 1)
  if (form.value.messageBlocks.length === 0) {
    form.value.messageBlocks.push({ role: 'user', content: '' })
  }
}

/** allowedToolIds del bloque (solo para rol tool). Inicializa array si no existe. */
function getBlockAllowedToolIds(block) {
  if (!block || block.role !== 'tool') return []
  if (!Array.isArray(block.allowedToolIds)) return []
  return block.allowedToolIds
}

function ensureBlockAllowedToolIds(block) {
  if (block.role === 'tool' && !Array.isArray(block.allowedToolIds)) {
    block.allowedToolIds = []
  }
}

function getToolNameById(toolId) {
  const t = toolsList.value.find(tool => String(tool.id) === String(toolId))
  return t?.name ?? toolId
}

function removeBlockAllowedTool(blockIdx, toolId) {
  const block = form.value.messageBlocks?.[blockIdx]
  if (!block || block.role !== 'tool') return
  ensureBlockAllowedToolIds(block)
  const ids = block.allowedToolIds.filter(id => String(id) !== String(toolId))
  block.allowedToolIds = ids
}

function openAllowedToolsModalForBlock(blockIdx) {
  const block = form.value.messageBlocks?.[blockIdx]
  if (!block || block.role !== 'tool') return
  ensureBlockAllowedToolIds(block)
  allowedToolsModalBlockIndex.value = blockIdx
  allowedToolsModalSearch.value = ''
  allowedToolsModalSelection.value = [...block.allowedToolIds]
  showAllowedToolsModal.value = true
}

function applyAllowedToolsSelection() {
  const idx = allowedToolsModalBlockIndex.value
  if (idx == null || idx < 0) return
  const block = form.value.messageBlocks?.[idx]
  if (block && block.role === 'tool') {
    ensureBlockAllowedToolIds(block)
    block.allowedToolIds = [...allowedToolsModalSelection.value]
  }
}

function selectAllFilteredToolsInModal() {
  const ids = filteredToolsForModal.value.map(t => t.id)
  const current = new Set(allowedToolsModalSelection.value)
  ids.forEach(id => current.add(id))
  allowedToolsModalSelection.value = Array.from(current)
}

function selectNoFilteredToolsInModal() {
  const toRemove = new Set(filteredToolsForModal.value.map(t => t.id))
  allowedToolsModalSelection.value = allowedToolsModalSelection.value.filter(id => !toRemove.has(id))
}

watch(showAllowedToolsModal, (open) => {
  if (!open) allowedToolsModalBlockIndex.value = null
})

/** Placeholder del textarea según el rol: breve guía de cómo escribir el prompt */
function placeholderForRole(role) {
  const hints = {
    system: 'Instrucciones globales para el modelo (comportamiento, tono, reglas). Ej: "Eres un asistente útil. Responde siempre en español." Usa {{nombre}} para argumentos.',
    user: 'Mensaje del usuario o pregunta. Ej: "Hola {{name}}, ¿cuál es el estado del pedido {{order_id}}?"',
    assistant: 'Ejemplo de respuesta del asistente (few-shot). Ej: "El pedido {{order_id}} está en camino."',
    tool: 'Instrucciones sobre cómo o cuándo usar las tools. Usa {{arg}} si el mensaje depende de argumentos.'
  }
  return hints[role] ?? 'Contenido del mensaje. Usa {{nombre}} para argumentos.'
}

let mcpArgSyncTimer = null
/** Debounced: reconcilia argumentos con `{{nombre}}` cerrados sin basura al editar a medias. */
function syncArgumentsFromMessageBlocks() {
  if (mcpArgSyncTimer != null) clearTimeout(mcpArgSyncTimer)
  mcpArgSyncTimer = setTimeout(() => {
    mcpArgSyncTimer = null
    argumentsFields.value = syncMcpArgumentsFromMessageBlocks(
      argumentsFields.value,
      form.value.messageBlocks ?? []
    )
  }, 400)
}

function buildArgumentsSchemaJson() {
  const args = argumentsFields.value.filter(a => a.name?.trim())
  if (args.length === 0) return null
  const arr = args.map(a => ({
    name: a.name.trim(),
    description: a.description?.trim() || undefined,
    required: a.required
  }))
  return JSON.stringify(arr)
}

function isOutputSchemaFieldExpanded(idx) {
  return outputSchemaExpanded.value[idx] !== false
}
function toggleOutputSchemaFieldExpanded(idx) {
  const arr = [...outputSchemaExpanded.value]
  arr[idx] = !arr[idx]
  outputSchemaExpanded.value = arr
}
function addOutputSchemaField() {
  outputSchemaFields.value.push({ name: '', type: 'string', description: '', required: false })
  outputSchemaExpanded.value.push(false)
}
function removeOutputSchemaField(idx) {
  outputSchemaFields.value.splice(idx, 1)
  outputSchemaExpanded.value.splice(idx, 1)
  if (outputSchemaFields.value.length === 0) {
    outputSchemaFields.value.push({ name: '', type: 'string', description: '', required: false })
    outputSchemaExpanded.value = [false]
  }
}
/** Construye JSON Schema object desde el form array (solo propiedades con nombre). */
function buildOutputSchemaJson() {
  const fields = outputSchemaFields.value.filter(f => (f.name ?? '').trim())
  if (fields.length === 0) return null
  const properties = {}
  const required = []
  for (const f of fields) {
    const key = f.name.trim()
    if (!key) continue
    properties[key] = {
      type: (f.type || 'string').toLowerCase(),
      ...(f.description?.trim() ? { description: f.description.trim() } : {})
    }
    if (f.required) required.push(key)
  }
  const schema = { type: 'object', properties }
  if (required.length > 0) schema.required = required
  return JSON.stringify(schema)
}
/** Parsea outputSchema (string JSON) y rellena outputSchemaFields. */
function parseOutputSchemaToFields(jsonStr) {
  if (!jsonStr || typeof jsonStr !== 'string' || !jsonStr.trim()) {
    outputSchemaFields.value = [{ name: '', type: 'string', description: '', required: false }]
    outputSchemaExpanded.value = [false]
    return
  }
  try {
    const obj = JSON.parse(jsonStr)
    if (!obj || typeof obj !== 'object' || !obj.properties || typeof obj.properties !== 'object') {
      outputSchemaFields.value = [{ name: '', type: 'string', description: '', required: false }]
      outputSchemaExpanded.value = [false]
      return
    }
    const requiredSet = new Set(Array.isArray(obj.required) ? obj.required : [])
    const arr = Object.entries(obj.properties).map(([key, prop]) => ({
      name: key,
      type: (prop && typeof prop.type === 'string' ? prop.type : 'string').toLowerCase(),
      description: (prop && prop.description) ? String(prop.description) : '',
      required: requiredSet.has(key)
    }))
    outputSchemaFields.value = arr.length > 0 ? arr : [{ name: '', type: 'string', description: '', required: false }]
    outputSchemaExpanded.value = outputSchemaFields.value.map(() => true)
  } catch {
    outputSchemaFields.value = [{ name: '', type: 'string', description: '', required: false }]
    outputSchemaExpanded.value = [false]
  }
}

function buildMessageBlocksJson() {
  const blocks = (form.value.messageBlocks ?? []).map(b => {
    const role = (b.role || 'user').toLowerCase()
    const out = { role, content: (b.content ?? '').trim() }
    if (role === 'tool' && Array.isArray(b.allowedToolIds)) {
      out.allowedToolIds = b.allowedToolIds
    }
    return out
  })
  return JSON.stringify(blocks)
}

function fillFormFromPrompt(prompt) {
  let messageBlocks = [{ role: 'user', content: '' }]
  const promptAllowedIds = Array.isArray(prompt.allowedToolIds) ? prompt.allowedToolIds : []
  try {
    const raw = prompt.messageBlocks
    if (raw && typeof raw === 'string' && raw.trim() !== '' && raw.trim() !== '[]') {
      const parsed = JSON.parse(raw)
      if (Array.isArray(parsed) && parsed.length > 0) {
        let firstToolBlockIdx = -1
        messageBlocks = parsed.map((b) => {
          const role = (b.role || 'user').toLowerCase()
          const block = { role, content: b.content ?? '' }
          if (role === 'tool') {
            block.allowedToolIds = Array.isArray(b.allowedToolIds) ? [...b.allowedToolIds] : (firstToolBlockIdx === -1 ? [...promptAllowedIds] : [])
            if (firstToolBlockIdx === -1) firstToolBlockIdx = 0
          }
          return block
        })
      }
    }
  } catch {
    messageBlocks = [{ role: 'user', content: '' }]
  }
  form.value = {
    name: prompt.name,
    title: prompt.title ?? '',
    description: prompt.description ?? '',
    messageBlocks,
    temperature: prompt.temperature ?? null,
    topP: prompt.topP ?? null,
    topK: prompt.topK ?? null,
    maxTokens: prompt.maxTokens ?? null,
    outputFormat: typeof prompt.outputFormat === 'number' ? prompt.outputFormat : (prompt.outputFormat === 'Structured' ? 1 : 0),
    outputSchema: prompt.outputSchema ?? '',
    isEnabled: prompt.isEnabled !== false
  }
  try {
    const schema = prompt.argumentsSchema?.trim()
    if (schema) {
      const parsed = JSON.parse(schema)
      const arr = Array.isArray(parsed) ? parsed : []
      argumentsFields.value = arr.length > 0
        ? arr.map(a => ({
            name: a.name ?? '',
            description: a.description ?? '',
            required: !!a.required,
            fromTemplate: true
          }))
        : [{ name: '', description: '', required: false, fromTemplate: false }]
    } else {
      argumentsFields.value = [{ name: '', description: '', required: false, fromTemplate: false }]
    }
  } catch {
    argumentsFields.value = [{ name: '', description: '', required: false, fromTemplate: false }]
  }
  parseOutputSchemaToFields(prompt.outputSchema ?? '')
}

async function loadMcp() {
  loading.value = true
  error.value = null
  try {
    if (isEdit.value) {
      const [promptRes, toolsRes] = await Promise.all([
        apiService.getMcpPrompt(mcpId.value, promptId.value),
        apiService.getMcpTools(mcpId.value)
      ])
      mcp.value = { id: mcpId.value, tools: toolsRes ?? [] }
      if (!promptRes) {
        error.value = 'Prompt no encontrado'
        return
      }
      fillFormFromPrompt(promptRes)
    } else {
      mcp.value = { id: mcpId.value, tools: [] }
      loading.value = false
      apiService.getMcpTools(mcpId.value).then((tools) => {
        if (mcp.value) mcp.value.tools = tools ?? []
      }).catch(() => {
        if (mcp.value) mcp.value.tools = []
      })
      try {
        const res = await apiService.getMcpPromptTemplates()
        templatesList.value = res?.items ?? []
      } catch {
        templatesList.value = []
      }
      return
    }
  } catch (e) {
    const status = e.response?.status
    error.value = status === 404 ? 'Prompt no encontrado' : (e.response?.data?.error || e.message || 'Error al cargar')
  } finally {
    loading.value = false
  }
}

function onTemplateSelectValue(val) {
  selectedTemplateId.value = val === '' || val == null ? null : val
  const id = selectedTemplateId.value
  if (id) {
    const t = templatesList.value.find((x) => String(x.id) === String(id))
    if (t) applyTemplate(t)
  }
}

function applyTemplate(tpl) {
  try {
    const blocks = typeof tpl.template === 'string' ? JSON.parse(tpl.template || '[]') : (tpl.template ?? [])
    form.value.title = tpl.title ?? form.value.title
    form.value.description = tpl.description ?? form.value.description
    form.value.messageBlocks = Array.isArray(blocks) && blocks.length > 0
      ? blocks.map(b => ({ role: b.role ?? 'user', content: b.content ?? '' }))
      : [{ role: 'user', content: '' }]
  } catch {
    form.value.messageBlocks = [{ role: 'user', content: '' }]
  }
  try {
    const schema = (tpl.argumentsSchema ?? '').trim()
    if (schema) {
      const parsed = JSON.parse(schema)
      const arr = Array.isArray(parsed) ? parsed : []
      argumentsFields.value = arr.length > 0
        ? arr.map(a => ({
            name: a.name ?? '',
            description: a.description ?? '',
            required: !!a.required,
            fromTemplate: true
          }))
        : [{ name: '', description: '', required: false, fromTemplate: false }]
    }
  } catch {
    argumentsFields.value = [{ name: '', description: '', required: false, fromTemplate: false }]
  }
}

async function doSaveAsTemplate() {
  if (!saveAsTemplateName.value) return
  savingTemplate.value = true
  try {
    await apiService.saveMcpPromptAsTemplate(mcpId.value, promptId.value, { templateName: saveAsTemplateName.value })
    toast.success('Plantilla guardada')
    showSaveAsTemplateModal.value = false
    saveAsTemplateName.value = ''
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al guardar plantilla')
  } finally {
    savingTemplate.value = false
  }
}

async function handleSave() {
  messageBlocksTouched.value = true
  if (!formValid.value) return
  saving.value = true
  try {
    const payload = {
      name: form.value.name?.trim(),
      title: form.value.title?.trim() || undefined,
      description: form.value.description?.trim() || undefined,
      messageBlocks: buildMessageBlocksJson(),
      argumentsSchema: buildArgumentsSchemaJson(),
      allowedToolIds: allBlocksAllowedToolIdsUnion.value.length ? allBlocksAllowedToolIdsUnion.value : undefined,
      temperature: form.value.temperature != null && form.value.temperature !== '' ? Number(form.value.temperature) : undefined,
      topP: form.value.topP != null && form.value.topP !== '' ? Number(form.value.topP) : undefined,
      topK: form.value.topK != null && form.value.topK !== '' ? Number(form.value.topK) : undefined,
      maxTokens: form.value.maxTokens != null && form.value.maxTokens !== '' ? Number(form.value.maxTokens) : undefined,
      outputFormat: form.value.outputFormat,
      outputSchema: form.value.outputFormat === 1 ? (buildOutputSchemaJson() ?? undefined) : (form.value.outputSchema?.trim() || undefined)
    }
    if (isEdit.value) {
      payload.isEnabled = form.value.isEnabled
      await apiService.updateMcpPrompt(mcpId.value, promptId.value, payload)
      toast.success('Prompt actualizado')
    } else {
      if (selectedTemplateId.value) payload.templateId = selectedTemplateId.value
      await apiService.addMcpPrompt(mcpId.value, payload)
      toast.success('Prompt añadido')
    }
    router.push(backUrl.value)
  } catch (e) {
    toast.error(e.response?.data?.error || e.message || 'Error al guardar')
  } finally {
    saving.value = false
  }
}

onUnmounted(() => {
  if (mcpArgSyncTimer != null) clearTimeout(mcpArgSyncTimer)
})

onMounted(() => loadMcp())
</script>

<style scoped>
.allowed-tools-slider-enter-active,
.allowed-tools-slider-leave-active {
  transition: opacity 0.2s ease;
}
.allowed-tools-slider-enter-from,
.allowed-tools-slider-leave-to {
  opacity: 0;
}
.animate-slide-in-panel {
  animation: slideInPanel 0.25s ease-out;
}
@keyframes slideInPanel {
  from {
    transform: translateX(100%);
  }
  to {
    transform: translateX(0);
  }
}
</style>

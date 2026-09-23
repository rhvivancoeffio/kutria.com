<template>
  <div class="action-node group">
    <Handle id="target" type="target" position="left" class="action-node__handle" :connectable-start="false" />
    <div class="action-node__connector action-node__connector--input" aria-hidden="true">
      <span class="action-node__connector-line" />
    </div>
    <div
      class="action-node__body"
      :class="{
        'action-node__body--duplicate-name': hasDuplicateName,
        'action-node__body--selected': isSelected
      }"
    >
      <div class="action-node__title-row">
        <template v-if="!isEditingName">
          <span class="action-node__label">{{ displayLabel }}</span>
          <button
            type="button"
            class="action-node__edit-name nodrag"
            title="Editar nombre del nodo"
            @click.stop="startEditName"
          >
            <svg class="action-node__edit-name-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
        </template>
        <input
          v-else
          ref="nameInputRef"
          v-model="editingNameValue"
          type="text"
          class="action-node__name-input nodrag"
          @blur="commitEditName"
          @keydown.enter.prevent="commitEditName"
          @keydown.esc="cancelEditName"
        >
      </div>
      <span v-if="node.data?.isOutputStep" class="action-node__output-badge" title="Este paso es el resultado que devuelve el tool">Resultado</span>
      <span
        v-if="hasDuplicateName"
        class="action-node__error-icon"
        title="Nombre duplicado: no puede haber dos nodos con el mismo nombre"
      >
        <svg fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>
    <div v-if="node.data?.step?.integrationLogoUrl || node.data?.step?.integrationName" class="action-node__icon-wrap">
      <img
        v-if="node.data?.step?.integrationLogoUrl"
        :src="node.data.step.integrationLogoUrl"
        alt=""
        class="action-node__icon"
        @error="$event.target.style.display = 'none'"
      />
      <span
        v-if="integrationName"
        class="action-node__integration-tag"
        :title="integrationName"
      >{{ integrationName }}</span>
    </div>
    <div class="action-node__outputs">
      <div
        v-for="handleId in outputHandleIds"
        :key="handleId"
        class="action-node__output-row"
      >
        <div class="action-node__connector action-node__connector--output" aria-hidden="true">
          <span class="action-node__connector-line" />
        </div>
        <Handle
          :id="handleId"
          type="source"
          position="right"
          connectable="single"
          class="action-node__handle"
        />
      </div>
    </div>
    <button
      v-if="setOutputStep && !node.data?.isOutputStep"
      type="button"
      class="action-node__set-output"
      title="Usar este paso como resultado del tool"
      @click.stop="setOutputStep(node.id)"
    >
      Resultado
    </button>
    <button
      type="button"
      class="action-node__delete"
      title="Eliminar nodo"
      @click.stop="onDelete"
    >
      <svg class="action-node__delete-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
      </svg>
    </button>
  </div>
</template>

<script setup>
import { inject, computed, ref, nextTick } from 'vue'
import { Handle, useNode, useVueFlow } from '@vue-flow/core'

const { id, node } = useNode()
const { removeNodes, getNodes, setNodes } = useVueFlow()
const setOutputStep = inject('setOutputStep', null)
const updateNodeLabel = inject('updateNodeLabel', null)
const duplicateNodeIds = inject('duplicateNodeIds', ref(new Set()))
const selectedNodeId = inject('selectedNodeId', ref(null))

const integrationName = computed(() => node.data?.step?.integrationName ?? '')
const hasDuplicateName = computed(() => duplicateNodeIds.value && duplicateNodeIds.value.has(id))
const isSelected = computed(() => selectedNodeId?.value === id)
const displayLabel = computed(() => node.data?.label ?? node.id)

/** Ids de los source handles (outputs). Por defecto out-1, out-2, out-3. */
const outputHandleIds = computed(() => {
  const handles = node.data?.step?.outputHandles
  if (Array.isArray(handles) && handles.length > 0) return handles
  return ['out-1', 'out-2', 'out-3']
})

const isEditingName = ref(false)
const editingNameValue = ref('')
const nameInputRef = ref(null)

function startEditName() {
  editingNameValue.value = displayLabel.value || ''
  isEditingName.value = true
  nextTick(() => nameInputRef.value?.focus())
}

function defaultActionLabel() {
  const s = node.data?.step
  return s?.summary || (s ? `${s.method || 'GET'} ${(s.path || '').replace(/^\/+/, '')}` : null) || `Acción: ${id}`
}

function commitEditName() {
  isEditingName.value = false
  const name = (editingNameValue.value || '').trim()
  const prevLabel = node.data?.label ?? defaultActionLabel()
  if (name === prevLabel) return
  if (updateNodeLabel) {
    updateNodeLabel(id, name || undefined, defaultActionLabel())
  } else if (getNodes && setNodes) {
    const newLabel = name || defaultActionLabel()
    const currentNodes = typeof getNodes === 'function' ? getNodes() : getNodes.value ?? []
    const updated = currentNodes.map((n) => {
      if (n.id !== id) return n
      return {
        ...n,
        data: {
          ...n.data,
          label: newLabel,
          step: { ...(n.data?.step || {}), displayName: name || undefined }
        }
      }
    })
    setNodes(updated)
  }
}

function cancelEditName() {
  isEditingName.value = false
  editingNameValue.value = displayLabel.value || ''
}

function onDelete() {
  removeNodes([id])
}
</script>

<style scoped>
.action-node {
  position: relative;
  display: flex;
  align-items: center;
}
.action-node__body {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-wrap: wrap;
  gap: 8px;
  min-width: 80px;
  max-width: 160px;
  min-height: 52px;
  padding: 14px 14px;
  border-radius: 8px;
  background: #374151;
  color: #f3f4f6;
  font-size: 12px;
  text-align: center;
  border: 1px solid #4b5563;
  box-shadow:
    0 1px 0 rgba(255, 255, 255, 0.05) inset,
    0 2px 4px rgba(0, 0, 0, 0.3);
}
.action-node__icon-wrap {
  position: absolute;
  top: -10px;
  left: 10px;
  display: flex;
  align-items: center;
  gap: 6px;
  z-index: 1;
}
.action-node__icon {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  object-fit: contain;
  background: #374151;
  border: 2px solid #4b5563;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3);
  flex-shrink: 0;
}
.action-node__integration-tag {
  max-width: 100px;
  padding: 2px 6px;
  font-size: 10px;
  font-weight: 500;
  color: #9ca3af;
  background: #1f2937;
  border: 1px solid #4b5563;
  border-radius: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
}
.group:hover .action-node__integration-tag {
  max-width: 140px;
}
.action-node__output-badge {
  font-size: 10px;
  font-weight: 600;
  padding: 2px 5px;
  border-radius: 4px;
  background: #059669;
  color: white;
  white-space: nowrap;
}
.action-node__set-output {
  position: absolute;
  bottom: -8px;
  left: 50%;
  transform: translateX(-50%);
  font-size: 10px;
  padding: 2px 6px;
  border-radius: 4px;
  background: #059669;
  color: white;
  border: none;
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s;
  white-space: nowrap;
}
.group:hover .action-node__set-output {
  opacity: 1;
}
.group:focus-within .action-node__set-output {
  opacity: 1;
}
.action-node__set-output:hover {
  background: #047857;
}
.action-node:hover .action-node__body {
  background: #3d4a5c;
}
.action-node__body--duplicate-name {
  border-color: #ef4444;
  box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.4);
}
.action-node__body--selected {
  border-width: 2px;
  border-color: var(--color-primary-500, #6366f1);
  box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.35);
}
.action-node__error-icon {
  position: absolute;
  top: 4px;
  right: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  color: #ef4444;
  pointer-events: none; /* no capturar clics para que el botón editar siga funcionando */
}
.action-node__error-icon svg {
  width: 14px;
  height: 14px;
}
.action-node__title-row {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  min-width: 0;
}
.action-node__label {
  font-weight: 500;
  line-height: 1.35;
  word-wrap: break-word;
  overflow-wrap: break-word;
  min-width: 0;
}
.action-node__edit-name {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  padding: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: #9ca3af;
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s, color 0.15s, background 0.15s;
}
.action-node__edit-name:hover {
  color: #f3f4f6;
  background: rgba(255, 255, 255, 0.1);
}
.group:hover .action-node__edit-name,
.group:focus-within .action-node__edit-name,
.action-node__edit-name:focus {
  opacity: 1;
}
.action-node__edit-name-icon {
  width: 12px;
  height: 12px;
}
.action-node__name-input {
  width: 100%;
  min-width: 60px;
  max-width: 140px;
  padding: 2px 6px;
  font-size: 12px;
  font-weight: 500;
  color: #f3f4f6;
  background: #1f2937;
  border: 1px solid #4b5563;
  border-radius: 4px;
  outline: none;
}
.action-node__name-input:focus {
  border-color: #60a5fa;
}
/* □── y ──□: línea desde el borde del nodo (fuera del recuadro) */
.action-node__connector {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  width: 22px;
}
.action-node__connector--input {
  justify-content: flex-start;
}
.action-node__outputs {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
}
.action-node__output-row {
  display: flex;
  align-items: center;
  gap: 0;
}
.action-node__output-row .action-node__connector--output {
  justify-content: flex-end;
}
.action-node__connector--output {
  justify-content: flex-end;
}
.action-node__connector-line {
  display: block;
  width: 20px;
  height: 2px;
  background: #9ca3af;
  border-radius: 1px;
}
.action-node__handle {
  flex-shrink: 0;
}
.action-node__delete {
  position: absolute;
  top: -8px;
  right: -8px;
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: #ef4444;
  color: white;
  border: 2px solid white;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.2);
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s, transform 0.15s;
}
.action-node__delete:hover {
  background: #dc2626;
  transform: scale(1.1);
}
.group:hover .action-node__delete {
  opacity: 1;
}
.group:focus-within .action-node__delete {
  opacity: 1;
}
.action-node__delete-icon {
  width: 14px;
  height: 14px;
}
</style>

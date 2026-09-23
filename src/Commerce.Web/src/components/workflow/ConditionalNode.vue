<template>
  <div class="conditional-node group">
    <div class="conditional-node__input-wrap">
      <Handle id="target" type="target" position="left" class="conditional-node__handle conditional-node__handle--target" :connectable-start="false" />
      <div class="conditional-node__connector conditional-node__connector--input" aria-hidden="true">
        <span class="conditional-node__connector-line" />
      </div>
    </div>
    <div
      class="conditional-node__body"
      :class="{
        'conditional-node__body--duplicate-name': hasDuplicateName,
        'conditional-node__body--selected': isSelected
      }"
    >
      <FloatingTooltip :content="conditionTooltip" placement="top">
        <span class="conditional-node__body-trigger">
          <template v-if="showHint">
            <span class="conditional-node__hint">Doble click</span>
            <span class="conditional-node__hint">Crear regla</span>
          </template>
          <template v-else>
            <div class="conditional-node__title-row">
              <template v-if="!isEditingName">
                <span class="conditional-node__label">{{ displayLabel }}</span>
                <button
                  type="button"
                  class="conditional-node__edit-name nodrag"
                  title="Editar nombre del nodo"
                  @click.stop="startEditName"
                >
                  <svg class="conditional-node__edit-name-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
              </template>
              <input
                v-else
                ref="nameInputRef"
                v-model="editingNameValue"
                type="text"
                class="conditional-node__name-input nodrag"
                @blur="commitEditName"
                @keydown.enter.prevent="commitEditName"
                @keydown.esc="cancelEditName"
              >
            </div>
          </template>
          <span
            v-if="hasDuplicateName"
            class="conditional-node__error-icon"
            title="Nombre duplicado: no puede haber dos nodos con el mismo nombre"
          >
            <svg fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
          </span>
        </span>
      </FloatingTooltip>
    </div>
    <div class="conditional-node__outputs">
      <div class="conditional-node__output-row conditional-node__output-row--true">
        <span class="conditional-node__output-label">True</span>
        <div class="conditional-node__connector conditional-node__connector--output" aria-hidden="true">
          <span class="conditional-node__connector-line conditional-node__connector-line--true" />
        </div>
        <Handle id="success" type="source" position="right" connectable="single" class="conditional-node__handle conditional-node__handle--true" />
      </div>
      <div class="conditional-node__output-row conditional-node__output-row--false">
        <span class="conditional-node__output-label">False</span>
        <div class="conditional-node__connector conditional-node__connector--output" aria-hidden="true">
          <span class="conditional-node__connector-line conditional-node__connector-line--false" />
        </div>
        <Handle id="failure" type="source" position="right" connectable="single" class="conditional-node__handle conditional-node__handle--false" />
      </div>
    </div>
    <button
      type="button"
      class="conditional-node__delete"
      title="Eliminar nodo"
      @click.stop="onDelete"
    >
      <svg class="conditional-node__delete-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
      </svg>
    </button>
  </div>
</template>

<script setup>
import { computed, ref, nextTick, inject } from 'vue'
import { Handle, useNode, useVueFlow } from '@vue-flow/core'
import FloatingTooltip from '../FloatingTooltip.vue'

const { id, node } = useNode()
const { removeNodes, getNodes, setNodes } = useVueFlow()
const updateNodeLabel = inject('updateNodeLabel', null)
const duplicateNodeIds = inject('duplicateNodeIds', ref(new Set()))
const selectedNodeId = inject('selectedNodeId', ref(null))
const hasDuplicateName = computed(() => duplicateNodeIds.value && duplicateNodeIds.value.has(id))
const isSelected = computed(() => selectedNodeId?.value === id)

const showHint = computed(() => {
  const rulesJson = node.data?.step?.rulesJson
  if (rulesJson == null || rulesJson === '') return true
  try {
    const arr = typeof rulesJson === 'string' ? JSON.parse(rulesJson) : rulesJson
    return !Array.isArray(arr) || arr.length === 0
  } catch {
    return true
  }
})

/** Texto para el tooltip: lista de reglas (RuleName: Expression). */
const conditionTooltip = computed(() => {
  const rulesJson = node.data?.step?.rulesJson
  if (rulesJson == null || rulesJson === '') return 'Sin reglas definidas'
  try {
    const arr = typeof rulesJson === 'string' ? JSON.parse(rulesJson) : rulesJson
    const workflow = Array.isArray(arr) ? arr[0] : arr
    const rules = workflow?.Rules ?? []
    if (rules.length === 0) return 'Sin reglas definidas'
    return rules.map((r, i) => `${r.RuleName || `Rule${i + 1}`}: ${r.Expression ?? ''}`).join('\n')
  } catch {
    return 'Reglas no válidas'
  }
})

const displayLabel = computed(() => node.data?.label ?? 'Condicional')
const isEditingName = ref(false)
const editingNameValue = ref('')
const nameInputRef = ref(null)

function startEditName() {
  editingNameValue.value = displayLabel.value || ''
  isEditingName.value = true
  nextTick(() => nameInputRef.value?.focus())
}

function commitEditName() {
  isEditingName.value = false
  const name = (editingNameValue.value || '').trim()
  const prevLabel = node.data?.label ?? 'Condicional'
  if (name === prevLabel) return
  if (updateNodeLabel) {
    updateNodeLabel(id, name || undefined, 'Condicional')
  } else if (getNodes && setNodes) {
    const newLabel = name || 'Condicional'
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
.conditional-node {
  position: relative;
  display: flex;
  align-items: stretch;
}
.conditional-node__body {
  position: relative;
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  min-width: 80px;
  padding: 12px 16px;
  border-radius: 8px;
  background: #374151;
  color: #f3f4f6;
  font-size: 12px;
  text-align: center;
  border: 1px solid #6b4b2e;
  border-left: 3px solid #f59e0b;
  box-shadow:
    0 1px 0 rgba(255, 255, 255, 0.05) inset,
    0 2px 4px rgba(0, 0, 0, 0.3);
}
.conditional-node__body-trigger {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  min-width: min-content;
}
.conditional-node:hover .conditional-node__body {
  background: #3d4a5c;
}
.conditional-node__body--duplicate-name {
  border-color: #ef4444;
  box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.4);
}
.conditional-node__body--selected {
  border-width: 2px;
  border-color: var(--color-primary-500, #6366f1);
  box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.35);
}
.conditional-node__error-icon {
  position: absolute;
  top: 4px;
  right: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  color: #ef4444;
  pointer-events: none;
}
.conditional-node__error-icon svg {
  width: 14px;
  height: 14px;
}
.conditional-node__hint {
  font-size: 11px;
  color: #9ca3af;
  line-height: 1.3;
}
.conditional-node__title-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 4px;
  min-width: 0;
}
.conditional-node__label {
  font-weight: 500;
  line-height: 1.35;
}
.conditional-node__edit-name {
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
.conditional-node__edit-name:hover {
  color: #f3f4f6;
  background: rgba(255, 255, 255, 0.1);
}
.group:hover .conditional-node__edit-name,
.group:focus-within .conditional-node__edit-name,
.conditional-node__edit-name:focus {
  opacity: 1;
}
.conditional-node__edit-name-icon {
  width: 12px;
  height: 12px;
}
.conditional-node__name-input {
  width: 100%;
  min-width: 60px;
  max-width: 120px;
  padding: 2px 6px;
  font-size: 12px;
  font-weight: 500;
  color: #f3f4f6;
  background: #1f2937;
  border: 1px solid #4b5563;
  border-radius: 4px;
  outline: none;
}
.conditional-node__name-input:focus {
  border-color: #60a5fa;
}
/* Contenedor entrada: centra verticalmente el handle [ ] y la línea - */
.conditional-node__input-wrap {
  display: flex;
  align-items: center;
  align-self: stretch;
  flex-shrink: 0;
}
.conditional-node__input-wrap :deep(.vue-flow__handle) {
  position: relative !important;
  top: auto !important;
  transform: none !important;
}
/* Líneas desde el borde (fuera del recuadro) */
.conditional-node__connector {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  width: 22px;
}
.conditional-node__connector--input {
  justify-content: flex-start;
}
.conditional-node__connector--output {
  justify-content: flex-end;
}
.conditional-node__connector-line {
  display: block;
  width: 20px;
  height: 2px;
  background: #9ca3af;
  border-radius: 1px;
}
.conditional-node__outputs {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: 4px;
  padding: 8px 0;
  pointer-events: none;
  min-height: 56px;
}
.conditional-node__output-row {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  min-height: 28px;
  gap: 4px;
}
/* Evitar que los 2 handles se superpongan: cada uno anclado a su fila */
.conditional-node__output-row > * {
  pointer-events: auto;
}
.conditional-node__output-row :deep(.vue-flow__handle) {
  position: absolute !important;
  right: 0;
  top: 50%;
  transform: translateY(-50%);
  margin: 0;
}
.conditional-node__output-label {
  font-size: 10px;
  font-weight: 600;
  min-width: 28px;
  text-align: right;
  color: #9ca3af;
}
.conditional-node__output-row--true .conditional-node__output-label {
  color: #22c55e;
}
.conditional-node__output-row--false .conditional-node__output-label {
  color: #ef4444;
}
.conditional-node__connector-line--true {
  background: #22c55e;
}
.conditional-node__connector-line--false {
  background: #ef4444;
}
.conditional-node__handle {
  flex-shrink: 0;
  position: relative;
  z-index: 1;
}
/* True = verde, False = rojo (por clase, no por orden DOM) */
.conditional-node__handle--true,
.conditional-node__handle--true :deep(.vue-flow__handle) {
  background: #22c55e !important;
  border-color: #16a34a !important;
}
.conditional-node__handle--false,
.conditional-node__handle--false :deep(.vue-flow__handle) {
  background: #ef4444 !important;
  border-color: #dc2626 !important;
}
.conditional-node__output-row--true .conditional-node__handle {
  z-index: 2;
}
.conditional-node__output-row--false .conditional-node__handle {
  z-index: 2;
}
.conditional-node__delete {
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
.conditional-node__delete:hover {
  background: #dc2626;
  transform: scale(1.1);
}
.group:hover .conditional-node__delete {
  opacity: 1;
}
.group:focus-within .conditional-node__delete {
  opacity: 1;
}
.conditional-node__delete-icon {
  width: 14px;
  height: 14px;
}
</style>

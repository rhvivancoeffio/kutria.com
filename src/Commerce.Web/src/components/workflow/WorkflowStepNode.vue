<template>
  <div
    class="workflow-step-node group"
    :class="node.data?.step?.type === 'conditional' ? 'workflow-step-node--conditional' : 'workflow-step-node--action'"
  >
    <Handle id="target" type="target" position="left" />
    <!-- Action: label + handle -->
    <template v-if="node.data?.step?.type !== 'conditional'">
      <span class="workflow-step-node__label">{{ node.data?.label ?? node.id }}</span>
      <Handle type="source" position="right" />
    </template>
    <!-- Conditional: icono If + 2 handles en el borde derecho (como los otros nodos) -->
    <template v-else>
      <div class="workflow-step-node__conditional-body">
        <div class="workflow-step-node__conditional-header">
          <div class="workflow-step-node__branch-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M4 12h8M12 12l4-4M12 12l4 4" />
            </svg>
          </div>
          <span class="workflow-step-node__label">If</span>
        </div>
      </div>
      <div class="workflow-step-node__handles">
        <div class="workflow-step-node__handle-row workflow-step-node__handle-row--true">
          <span class="workflow-step-node__handle-label">true</span>
          <Handle id="success" type="source" position="right" connectable="single" :style="{ top: '25%', bottom: 'auto' }" />
        </div>
        <div class="workflow-step-node__handle-row workflow-step-node__handle-row--false">
          <span class="workflow-step-node__handle-label">false</span>
          <Handle id="failure" type="source" position="right" connectable="single" :style="{ top: '75%', bottom: 'auto' }" />
        </div>
      </div>
    </template>
    <button
      type="button"
      class="workflow-step-node__delete"
      title="Eliminar nodo"
      @click.stop="onDelete"
    >
      <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
      </svg>
    </button>
  </div>
</template>

<script setup>
import { Handle } from '@vue-flow/core'
import { useNode } from '@vue-flow/core'
import { useVueFlow } from '@vue-flow/core'

const { id, node } = useNode()
const { removeNodes } = useVueFlow()

function onDelete() {
  removeNodes([id])
}
</script>

<style scoped>
.workflow-step-node {
  position: relative;
  min-width: 160px;
  padding: 12px 24px;
  font-size: 12px;
  border-radius: 8px;
  text-align: center;
  border: 1px solid;
  box-shadow:
    0 1px 0 rgba(255,255,255,0.05) inset,
    0 2px 4px rgba(0,0,0,0.3);
}
.workflow-step-node--action {
  background: #374151;
  color: #f3f4f6;
  border-color: #4b5563;
}
.workflow-step-node--conditional {
  background: #374151;
  color: #f3f4f6;
  border-color: #6b4b2e;
  border-left: 3px solid #f59e0b;
}
.workflow-step-node__label {
  display: block;
  font-weight: 500;
}
.workflow-step-node__conditional-body {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 100px;
}
.workflow-step-node__conditional-header {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}
.workflow-step-node__branch-icon {
  width: 24px;
  height: 24px;
  color: #22c55e;
  flex-shrink: 0;
}
.workflow-step-node__branch-icon svg {
  width: 100%;
  height: 100%;
}
.workflow-step-node__handles {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  display: flex;
  flex-direction: column;
  justify-content: space-around;
  padding: 8px 0;
  pointer-events: none;
}
.workflow-step-node__handle-row {
  position: relative;
  display: flex;
  align-items: center;
  gap: 4px;
  min-height: 24px;
}
.workflow-step-node__handle-row > *:last-child {
  pointer-events: auto;
}
.workflow-step-node__handle-label {
  font-size: 10px;
  font-weight: 600;
  pointer-events: none;
}
.workflow-step-node__handle-row--true .workflow-step-node__handle-label { color: #22c55e; }
.workflow-step-node__handle-row--false .workflow-step-node__handle-label { color: #ef4444; }
.workflow-step-node__delete {
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
  box-shadow: 0 1px 3px rgba(0,0,0,0.2);
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s, transform 0.15s;
}
.workflow-step-node__delete:hover {
  background: #dc2626;
  transform: scale(1.1);
}
.group:hover .workflow-step-node__delete {
  opacity: 1;
}
.group:focus-within .workflow-step-node__delete {
  opacity: 1;
}
</style>

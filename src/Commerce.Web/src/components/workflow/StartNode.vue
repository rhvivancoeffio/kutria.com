<template>
  <div class="start-node cursor-pointer" title="Doble clic para configurar los parámetros del tool">
    <div class="start-node__body">
      <div class="start-node__content">
        <span class="start-node__label">Inicio</span>
        <span class="start-node__hint">Doble click<br />Configurar parámetros</span>
      </div>
    </div>
    <div class="start-node__connector" aria-hidden="true">
      <span class="start-node__connector-line" />
    </div>
    <Handle id="start" type="source" position="right" class="start-node__handle" :connectable="true" />
  </div>
</template>

<script setup>
import { Handle } from '@vue-flow/core'
</script>

<style scoped>
/* Wrapper sin fondo: solo ordena [body][connector][handle] */
.start-node {
  position: relative;
  display: flex;
  align-items: center;
}
/* Recuadro del nodo: solo esto tiene fondo y borde. La línea sale del borde. */
.start-node__body {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 80px;
  max-width: 100px;
  padding: 12px 14px;
  border-radius: 8px;
  background: #374151;
  color: #f3f4f6;
  font-size: 12px;
  font-weight: 500;
  text-align: center;
  border: 1px solid #4b5563;
  box-shadow:
    0 1px 0 rgba(255, 255, 255, 0.05) inset,
    0 2px 4px rgba(0, 0, 0, 0.3);
}
.start-node:hover .start-node__body {
  background: #3d4a5c;
}
.start-node__content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}
.start-node__label {
  font-weight: 500;
  line-height: 1.35;
}
.start-node__hint {
  font-size: 10px;
  font-weight: 500;
  color: #9ca3af;
  line-height: 1.25;
  text-align: center;
}
/* ──□ Línea desde el borde del nodo (fuera del recuadro) */
.start-node__connector {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  width: 22px;
  justify-content: flex-end;
}
.start-node__connector-line {
  display: block;
  width: 20px;
  height: 2px;
  background: #9ca3af;
  border-radius: 1px;
}
.start-node__handle {
  flex-shrink: 0;
}
</style>

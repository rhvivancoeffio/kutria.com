<template>
  <Panel position="bottom-left" class="workflow-controls">
    <div class="workflow-controls__group">
      <button
        type="button"
        class="workflow-controls__btn"
        title="Zoom to fit"
        @click="fitView()"
      >
        <svg class="workflow-controls__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
        </svg>
      </button>
      <button
        type="button"
        class="workflow-controls__btn"
        title="Zoom in"
        @click="zoomIn()"
      >
        <svg class="workflow-controls__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM10 7v6m3-3H7" />
        </svg>
      </button>
      <button
        type="button"
        class="workflow-controls__btn"
        title="Zoom out"
        @click="zoomOut()"
      >
        <svg class="workflow-controls__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM13 10H7" />
        </svg>
      </button>
      <button
        type="button"
        class="workflow-controls__btn"
        title="Tidy up"
        @click="tidyUp()"
      >
        <svg class="workflow-controls__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
        </svg>
      </button>
    </div>
  </Panel>
</template>

<script setup>
import { Panel } from '@vue-flow/core'
import { useVueFlow } from '@vue-flow/core'

const { fitView, zoomIn, zoomOut, getNodes, setNodes, getEdges } = useVueFlow()

const NODE_SPACING_X = 240
const START_NODE_ID = 'start'
const START_X = 100
const START_Y = 120

function tidyUp() {
  const nodes = [...getNodes.value]
  const edges = [...getEdges.value]
  const startNode = nodes.find(n => n.id === START_NODE_ID)
  const stepNodes = nodes.filter(n => n.id !== START_NODE_ID)

  if (stepNodes.length === 0) {
    fitView()
    return
  }

  // Ordenar nodos siguiendo el grafo: start -> siguiente por edge
  const order = [START_NODE_ID]
  let current = START_NODE_ID
  while (true) {
    const outEdge = edges.find(e => e.source === current)
    if (!outEdge) break
    order.push(outEdge.target)
    current = outEdge.target
  }

  const stepOrder = order.filter(id => id !== START_NODE_ID)
  // Nodos no conectados van al final
  const extra = stepNodes.filter(n => !stepOrder.includes(n.id))
  const allOrdered = [...stepOrder, ...extra.map(n => n.id)]
  const updated = nodes.map(n => {
    if (n.id === START_NODE_ID) {
      return { ...n, position: { x: START_X, y: START_Y } }
    }
    const idx = allOrdered.indexOf(n.id)
    const x = idx >= 0 ? START_X + NODE_SPACING_X + idx * NODE_SPACING_X : (n.position?.x ?? START_X + NODE_SPACING_X)
    const y = n.position?.y ?? START_Y
    return { ...n, position: { x, y } }
  })

  setNodes(updated)
  fitView({ padding: 0.2 })
}
</script>

<style scoped>
.workflow-controls {
  margin: 12px;
}
.workflow-controls__group {
  display: flex;
  gap: 2px;
  padding: 4px;
  background: rgba(31, 41, 55, 0.9);
  border: 1px solid #4b5563;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}
.workflow-controls__btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  color: #9ca3af;
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: color 0.15s, background 0.15s;
}
.workflow-controls__btn:hover {
  color: #f3f4f6;
  background: rgba(75, 85, 99, 0.6);
}
.workflow-controls__icon {
  width: 16px;
  height: 16px;
}
</style>

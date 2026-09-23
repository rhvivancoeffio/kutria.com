<template>
  <span class="floating-tooltip">
    <span
      ref="triggerRef"
      class="floating-tooltip__trigger"
      @mouseenter="onTriggerEnter"
      @mouseleave="onTriggerLeave"
      @focus="onTriggerEnter"
      @blur="onTriggerLeave"
    >
      <slot />
    </span>
    <Teleport to="body">
      <div
        v-show="visible"
        ref="tooltipRef"
        class="floating-tooltip__popup"
        :style="popupStyle"
        role="tooltip"
      >
        <template v-if="$slots.content">
          <slot name="content" />
        </template>
        <template v-else>
          <span class="floating-tooltip__text">{{ content }}</span>
        </template>
      </div>
    </Teleport>
  </span>
</template>

<script setup>
import { ref, computed, watch, nextTick, onUnmounted } from 'vue'
import { computePosition, offset, flip, shift, autoUpdate } from '@floating-ui/dom'

const props = defineProps({
  /** Texto del tooltip. Saltos de línea con \n se muestran correctamente. */
  content: {
    type: String,
    default: ''
  },
  /** Posición preferida: 'top' | 'bottom' | 'left' | 'right' */
  placement: {
    type: String,
    default: 'top'
  },
  /** Retraso en ms antes de mostrar el tooltip */
  showDelay: {
    type: Number,
    default: 200
  },
  /** Retraso en ms antes de ocultar al salir del trigger */
  hideDelay: {
    type: Number,
    default: 0
  }
})

const triggerRef = ref(null)
const tooltipRef = ref(null)
const visible = ref(false)
const position = ref({ x: 0, y: 0 })
let showTimeout = null
let hideTimeout = null
let autoUpdateCleanup = null

const popupStyle = computed(() => ({
  position: 'fixed',
  left: `${position.value.x}px`,
  top: `${position.value.y}px`
}))

async function updatePosition() {
  const refEl = triggerRef.value
  const floatEl = tooltipRef.value
  if (!refEl || !floatEl) return
  const { x, y } = await computePosition(refEl, floatEl, {
    placement: props.placement,
    strategy: 'fixed',
    middleware: [
      offset(8),
      flip({ padding: 8 }),
      shift({ padding: 8 })
    ]
  })
  position.value = { x, y }
}

function show() {
  if (hideTimeout) {
    clearTimeout(hideTimeout)
    hideTimeout = null
  }
  if (visible.value) return
  showTimeout = setTimeout(() => {
    showTimeout = null
    visible.value = true
    nextTick(() => {
      updatePosition()
      const refEl = triggerRef.value
      const floatEl = tooltipRef.value
      if (refEl && floatEl) {
        autoUpdateCleanup?.()
        autoUpdateCleanup = autoUpdate(refEl, floatEl, updatePosition)
      }
    })
  }, props.showDelay)
}

function hide() {
  if (showTimeout) {
    clearTimeout(showTimeout)
    showTimeout = null
  }
  hideTimeout = setTimeout(() => {
    hideTimeout = null
    visible.value = false
    autoUpdateCleanup?.()
    autoUpdateCleanup = null
  }, props.hideDelay)
}

function onTriggerEnter() {
  show()
}

function onTriggerLeave() {
  hide()
}

watch(visible, (v) => {
  if (v) nextTick(updatePosition)
})

onUnmounted(() => {
  showTimeout && clearTimeout(showTimeout)
  hideTimeout && clearTimeout(hideTimeout)
  autoUpdateCleanup?.()
})
</script>

<style scoped>
.floating-tooltip {
  display: inline-flex;
}
.floating-tooltip__trigger {
  display: inherit;
  cursor: inherit;
}
.floating-tooltip__popup {
  z-index: 9999;
  max-width: 360px;
  padding: 8px 12px;
  font-size: 12px;
  line-height: 1.45;
  color: #f3f4f6;
  background: #1f2937;
  border: 1px solid #374151;
  border-radius: 6px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25);
  white-space: pre-line;
  word-break: break-word;
  pointer-events: none;
}
.floating-tooltip__text {
  white-space: pre-line;
}
</style>

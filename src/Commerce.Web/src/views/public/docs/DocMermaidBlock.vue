<template>
  <div
    ref="root"
    class="doc-mermaid mt-4 rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-950 p-4 overflow-x-auto text-gray-800 dark:text-gray-100 [&_svg]:max-w-full"
    aria-hidden="true"
  />
</template>

<script setup>
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import mermaid from 'mermaid'

const props = defineProps({
  source: { type: String, required: true }
})

const root = ref(null)
let observer
let renderToken = 0

function diagramTheme() {
  return document.documentElement.classList.contains('dark') ? 'dark' : 'default'
}

async function draw() {
  const el = root.value
  if (!el || !props.source?.trim()) return
  const token = ++renderToken
  const id = `doc-mmd-${token}-${Math.random().toString(36).slice(2, 10)}`
  mermaid.initialize({
    startOnLoad: false,
    theme: diagramTheme(),
    securityLevel: 'strict',
    fontFamily: 'ui-sans-serif, system-ui, sans-serif'
  })
  try {
    const { svg, bindFunctions } = await mermaid.render(id, props.source)
    if (token !== renderToken || el !== root.value) return
    el.innerHTML = svg
    bindFunctions?.(el)
  } catch (e) {
    console.warn('[DocMermaid]', e)
    if (token !== renderToken || el !== root.value) return
    el.innerHTML =
      '<p class="text-sm text-amber-700 dark:text-amber-300">No se pudo dibujar el diagrama.</p>'
  }
}

onMounted(() => {
  draw()
  observer = new MutationObserver(() => {
    draw()
  })
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })
})

watch(() => props.source, draw)

onBeforeUnmount(() => {
  observer?.disconnect()
})
</script>

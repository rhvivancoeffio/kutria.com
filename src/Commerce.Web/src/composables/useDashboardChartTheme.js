import { ref, onMounted, onUnmounted } from 'vue'

/** Colores de leyenda/grid para Chart.js alineados con `class="dark"` en documentElement. */
export function useDashboardChartTheme() {
  const legendColor = ref('#64748b')
  const tickColor = ref('#64748b')
  const gridColor = ref('rgba(100, 116, 139, 0.2)')

  function sync() {
    const dark = document.documentElement.classList.contains('dark')
    legendColor.value = dark ? '#94a3b8' : '#64748b'
    tickColor.value = dark ? '#94a3b8' : '#64748b'
    gridColor.value = dark ? 'rgba(148, 163, 184, 0.12)' : 'rgba(100, 116, 139, 0.2)'
  }

  onMounted(() => {
    sync()
    const obs = new MutationObserver(sync)
    obs.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })
    onUnmounted(() => obs.disconnect())
  })

  return { legendColor, tickColor, gridColor }
}

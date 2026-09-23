<template>
  <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4">
    <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">{{ title }}</h3>
    <div v-if="!hasData" class="h-52 flex items-center justify-center text-sm text-gray-500 dark:text-gray-400">
      Sin datos para mostrar
    </div>
    <div v-else class="h-56 relative" role="img" :aria-label="ariaLabel || title">
      <Doughnut :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { Chart as ChartJS, ArcElement, Tooltip, Legend } from 'chart.js'
import { Doughnut } from 'vue-chartjs'
import { useDashboardChartTheme } from '../../composables/useDashboardChartTheme'

ChartJS.register(ArcElement, Tooltip, Legend)

const props = defineProps({
  title: { type: String, required: true },
  labels: { type: Array, default: () => [] },
  values: { type: Array, default: () => [] },
  ariaLabel: { type: String, default: '' }
})

const { legendColor } = useDashboardChartTheme()

const palette = [
  'rgba(14, 165, 233, 0.9)',
  'rgba(59, 130, 246, 0.9)',
  'rgba(99, 102, 241, 0.9)',
  'rgba(168, 85, 247, 0.9)',
  'rgba(244, 63, 94, 0.9)',
  'rgba(245, 158, 11, 0.9)',
  'rgba(34, 197, 94, 0.9)',
  'rgba(100, 116, 139, 0.85)'
]

const hasData = computed(
  () =>
    Array.isArray(props.labels) &&
    props.labels.length > 0 &&
    Array.isArray(props.values) &&
    props.values.some((v) => Number(v) > 0)
)

const chartData = computed(() => {
  const labels = props.labels || []
  const values = props.values || []
  const bg = labels.map((_, i) => palette[i % palette.length])
  return {
    labels,
    datasets: [
      {
        data: values,
        backgroundColor: bg,
        borderWidth: 0
      }
    ]
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom',
      labels: {
        color: legendColor.value,
        boxWidth: 12,
        padding: 12,
        font: { size: 11 }
      }
    },
    tooltip: {
      callbacks: {
        label(ctx) {
          const total = ctx.dataset.data.reduce((a, b) => a + b, 0) || 1
          const v = ctx.raw
          const pct = ((v / total) * 100).toFixed(1)
          return `${ctx.label}: ${v} (${pct}%)`
        }
      }
    }
  }
}))

</script>

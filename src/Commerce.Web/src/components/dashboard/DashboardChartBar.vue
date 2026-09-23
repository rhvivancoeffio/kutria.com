<template>
  <div class="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4">
    <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">{{ title }}</h3>
    <div v-if="!hasData" class="h-52 flex items-center justify-center text-sm text-gray-500 dark:text-gray-400">
      Sin datos para mostrar
    </div>
    <div v-else class="h-64 relative" role="img" :aria-label="ariaLabel || title">
      <Bar :data="chartData" :options="chartOptions" />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Tooltip, Legend } from 'chart.js'
import { Bar } from 'vue-chartjs'
import { useDashboardChartTheme } from '../../composables/useDashboardChartTheme'

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend)

const props = defineProps({
  title: { type: String, required: true },
  labels: { type: Array, default: () => [] },
  values: { type: Array, default: () => [] },
  ariaLabel: { type: String, default: '' },
  horizontal: { type: Boolean, default: true }
})

const { tickColor, gridColor } = useDashboardChartTheme()

const hasData = computed(
  () =>
    Array.isArray(props.labels) &&
    props.labels.length > 0 &&
    Array.isArray(props.values) &&
    props.values.some((v) => Number(v) > 0)
)

const chartData = computed(() => ({
  labels: props.labels,
  datasets: [
    {
      label: props.title,
      data: props.values,
      backgroundColor: 'rgba(14, 165, 233, 0.75)',
      borderColor: 'rgba(14, 165, 233, 1)',
      borderWidth: 1,
      borderRadius: 4
    }
  ]
}))

const chartOptions = computed(() => {
  const axisColor = tickColor.value
  const grid = gridColor.value
  return {
    indexAxis: props.horizontal ? 'y' : 'x',
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: { enabled: true }
    },
    scales: props.horizontal
      ? {
          x: {
            beginAtZero: true,
            grid: { color: grid },
            ticks: { color: axisColor, font: { size: 11 } }
          },
          y: {
            grid: { display: false },
            ticks: { color: axisColor, font: { size: 11 } }
          }
        }
      : {
          x: {
            grid: { color: grid },
            ticks: { color: axisColor, font: { size: 11 } }
          },
          y: {
            beginAtZero: true,
            grid: { color: grid },
            ticks: { color: axisColor, font: { size: 11 } }
          }
        }
  }
})
</script>

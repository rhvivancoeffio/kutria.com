<template>
  <button
    @click="toggleShowJson"
    :class="[
      'p-2 rounded-lg transition-colors',
      showJson 
        ? 'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300' 
        : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600'
    ]"
    :title="showJson ? 'Ocultar JSON' : 'Mostrar JSON'"
  >
    <svg 
      xmlns="http://www.w3.org/2000/svg" 
      class="h-5 w-5" 
      fill="none" 
      viewBox="0 0 24 24" 
      stroke="currentColor"
    >
      <path 
        stroke-linecap="round" 
        stroke-linejoin="round" 
        stroke-width="2" 
        d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" 
      />
    </svg>
  </button>
</template>

<script setup>
import { useSettings } from '../composables/useSettings'

const { showJson, toggleShowJson } = useSettings()
</script>

<template>
  <button
    @click="toggleTheme"
    class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
    :title="currentTheme === 'dark' ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'"
  >
    <span v-if="currentTheme === 'dark'" class="text-xl">🌙</span>
    <span v-else class="text-xl">☀️</span>
  </button>
</template>

<script setup>
import { computed } from 'vue'
import { useTheme } from '../composables/useTheme'

const { theme, toggleTheme } = useTheme()

// Create a computed property to ensure reactivity
const currentTheme = computed(() => theme.value)
</script>

<template>
  <div id="app" class="min-h-screen transition-colors duration-200 relative">
    <router-view />
    <ChatWidget v-if="!isLanding" />
    <!-- Portal para dropdowns/modales: siempre encima del contenido -->
    <div id="portal-root" class="fixed inset-0 z-[2147483647] pointer-events-none" aria-hidden="true" />
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useTheme } from './composables/useTheme'
import ChatWidget from './components/chat/ChatWidget.vue'
import './components/chat/chat-widget.css'

const route = useRoute()
const isLanding = computed(() => route.name === 'Home')

const { setTheme } = useTheme()

onMounted(() => {
  if (!localStorage.getItem('kutria-theme')) {
    localStorage.setItem('theme', 'dark')
    localStorage.setItem('kutria-theme', '1')
  }
  const savedTheme = localStorage.getItem('theme')
  const themeToApply = savedTheme === 'light' || savedTheme === 'dark' ? savedTheme : 'dark'
  if (!savedTheme) {
    localStorage.setItem('theme', 'dark')
  }
  setTheme(themeToApply)
})
</script>

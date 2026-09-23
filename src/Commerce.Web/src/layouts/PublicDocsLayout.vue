<template>
  <div class="min-h-screen bg-white dark:bg-gray-900 flex flex-col">
    <header class="sticky top-0 z-40 flex items-center justify-between h-14 px-4 lg:px-6 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 shrink-0">
      <div class="flex items-center gap-3">
        <button
          @click="sidebarOpen = !sidebarOpen"
          class="p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 lg:hidden"
          aria-label="Toggle menú"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <router-link to="/" class="flex items-center text-gray-900 dark:text-white">
          <KutriaMark />
        </router-link>
      </div>
      <div class="flex items-center gap-4">
        <router-link
          v-if="isAuthenticated"
          to="/admin"
          class="text-sm font-medium text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400"
        >
          Panel
        </router-link>
        <router-link to="/" class="text-sm text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400">
          ← Inicio
        </router-link>
      </div>
    </header>

    <div class="flex flex-1 min-w-0 overflow-hidden">
      <aside
        :class="[
          'fixed z-30 flex flex-col w-64 bg-gray-50 dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 transition-transform duration-200 ease-in-out',
          'top-14 left-0 bottom-0',
          'lg:relative lg:top-0 lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'
        ]"
      >
        <nav class="flex-1 overflow-y-auto py-6 px-3">
          <template v-for="(items, sectionKey) in nav" :key="sectionKey">
            <div class="mb-3">
              <span class="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400 px-3">
                {{ sectionLabel(sectionKey) }}
              </span>
            </div>
            <ul class="space-y-0.5 mb-6">
              <li v-for="slug in items" :key="slug">
                <router-link
                  :to="docPath(sectionKey, slug)"
                  :class="[
                    'flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                    isActive(sectionKey, slug)
                      ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400'
                      : 'text-gray-600 dark:text-gray-400 hover:bg-gray-200/60 dark:hover:bg-gray-800 hover:text-gray-900 dark:hover:text-gray-200'
                  ]"
                >
                  {{ docTitle(sectionKey, slug) }}
                </router-link>
              </li>
            </ul>
          </template>
        </nav>
      </aside>

      <div
        v-if="sidebarOpen"
        class="fixed inset-0 z-20 bg-black/50 lg:hidden"
        @click="sidebarOpen = false"
        aria-hidden="true"
      />

      <main class="flex-1 min-w-0 overflow-auto bg-white dark:bg-gray-900">
        <div class="max-w-3xl mx-auto px-6 lg:px-10 py-10 lg:py-14">
          <router-view />
        </div>
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import apiService from '../services/api'
import docsData from '../data/docs.json'
import KutriaMark from '../components/kutria/KutriaMark.vue'

const sidebarOpen = ref(false)
const isAuthenticated = computed(() => apiService.isAuthenticated())
const route = useRoute()
const nav = docsData.nav

const sectionLabel = (key) => {
  const labels = { docs: 'Guías', admin: 'Admin', developers: 'Developers' }
  return labels[key] || key
}

const docPath = (section, slug) => {
  if (section === 'docs') return slug ? `/docs/${slug}` : '/docs'
  const base = `/docs/${section}`
  return slug ? `${base}/${slug}` : base
}

const docTitle = (section, slug) => {
  const sectionData = docsData[section]
  if (!sectionData) return 'Introducción'
  const key = slug || 'intro'
  const page = sectionData[key]
  return page?.title || 'Introducción'
}

const isActive = (section, slug) => {
  const path = route.path.replace(/^\/docs\/?/, '').split('/').filter(Boolean)
  if (section === 'docs') {
    const expected = slug || ''
    const actual = path[0] || ''
    return expected === actual
  }
  if (path[0] !== section) return false
  const expectedSlug = slug || 'intro'
  const actualSlug = path[1] || 'intro'
  return expectedSlug === actualSlug
}
</script>

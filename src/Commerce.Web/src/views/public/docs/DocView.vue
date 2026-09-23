<template>
  <article v-if="page" class="max-w-none">
    <!-- Tagline (blockquote style, Onyx-like) -->
    <blockquote
      v-if="page.tagline"
      class="border-l-4 border-primary-500 dark:border-primary-400 pl-4 my-6 text-lg text-gray-600 dark:text-gray-400 italic"
    >
      {{ page.tagline }}
    </blockquote>

    <h1 class="text-4xl font-bold text-gray-900 dark:text-white mb-4 tracking-tight">
      {{ page.title }}
    </h1>
    <p v-if="page.description" class="text-lg text-gray-600 dark:text-gray-300 leading-relaxed mb-6">
      {{ page.description }}
    </p>

    <div
      v-if="page.resourceLinks && page.resourceLinks.length"
      class="mb-8 rounded-xl border border-primary-200/80 dark:border-primary-800/60 bg-primary-50/40 dark:bg-primary-950/30 px-4 py-3"
    >
      <p class="text-sm font-medium text-gray-800 dark:text-gray-200 mb-2">
        Enlaces relacionados
      </p>
      <ul class="list-disc list-inside space-y-1.5 text-sm text-gray-700 dark:text-gray-300">
        <li v-for="(lnk, i) in page.resourceLinks" :key="i">
          <router-link
            :to="lnk.href"
            class="text-primary-600 dark:text-primary-400 hover:underline font-medium"
          >
            {{ lnk.text }}
          </router-link>
        </li>
      </ul>
    </div>

    <!-- Cards grid -->
    <div
      v-if="page.cards && page.cards.length"
      class="grid gap-4 sm:grid-cols-2 mb-12"
    >
      <router-link
        v-for="(card, i) in page.cards"
        :key="i"
        :to="card.href"
        class="group flex gap-4 p-5 rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 card-hover hover:shadow-md duration-200"
      >
        <div class="shrink-0 w-10 h-10 rounded-lg bg-primary-50 dark:bg-primary-900/30 flex items-center justify-center text-primary-600 dark:text-primary-400">
          <component :is="iconComponent(card.icon)" class="w-5 h-5" />
        </div>
        <div class="min-w-0">
          <h3 class="font-semibold text-gray-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
            {{ card.title }}
          </h3>
          <p class="text-sm text-gray-600 dark:text-gray-400 mt-0.5">
            {{ card.description }}
          </p>
        </div>
        <svg class="w-5 h-5 shrink-0 text-gray-400 group-hover:text-primary-500 transition-colors self-center ml-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
      </router-link>
    </div>

    <!-- Sections -->
    <div class="space-y-10">
      <section
        v-for="(section, idx) in page.sections"
        :key="idx"
        class="scroll-mt-24"
      >
        <h2 class="text-2xl font-semibold text-gray-900 dark:text-white mb-4 pb-2 border-b border-gray-200 dark:border-gray-700">
          {{ section.title }}
        </h2>
        <p v-if="section.content" class="text-gray-600 dark:text-gray-300 leading-relaxed">
          {{ section.content }}
        </p>
        <DocJsonSchemaBlock
          v-if="section.jsonSchema"
          :schema="section.jsonSchema"
        />
        <ul v-if="section.items" class="list-disc list-inside space-y-2 text-gray-600 dark:text-gray-300">
          <li v-for="(item, i) in section.items" :key="i">{{ item }}</li>
        </ul>
        <pre
          v-if="section.code"
          class="p-4 rounded-xl bg-gray-100 dark:bg-gray-800/80 text-sm overflow-x-auto mt-3 border border-gray-200 dark:border-gray-700"
        ><code class="text-gray-800 dark:text-gray-200">{{ section.code }}</code></pre>
        <DocMermaidBlock
          v-if="section.mermaid"
          :source="section.mermaid"
        />
        <figure v-if="section.image" class="mt-4">
          <button
            type="button"
            class="block w-full text-left rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900 cursor-zoom-in group"
            @click="openImageExpand(section.image, section.imageCaption)"
          >
            <img
              :src="section.image"
              :alt="section.imageAlt || section.title"
              class="max-w-full h-auto w-full group-hover:opacity-95 transition-opacity"
            />
            <figcaption v-if="section.imageCaption" class="mt-2 px-1 text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2">
              <span>{{ section.imageCaption }}</span>
              <span class="text-xs text-primary-600 dark:text-primary-400 opacity-0 group-hover:opacity-100 transition-opacity">Expandir</span>
            </figcaption>
          </button>
        </figure>
      </section>
    </div>

    <!-- Callout (Tip / Info / Warning) -->
    <div
      v-if="page.callout"
      :class="[
        'mt-10 p-4 rounded-xl border',
        calloutClass(page.callout.type)
      ]"
    >
      <div class="flex gap-3">
        <component :is="calloutIcon(page.callout.type)" class="w-5 h-5 shrink-0 mt-0.5" />
        <p class="text-sm">{{ page.callout.text }}</p>
      </div>
    </div>

    <!-- Prev/Next page navigation (admin & developers sections) - card style -->
    <nav
      v-if="prevNext && (prevNext.prev || prevNext.next)"
      class="mt-14 pt-8 border-t border-gray-200 dark:border-gray-700 grid grid-cols-1 sm:grid-cols-2 gap-4"
    >
      <router-link
        v-if="prevNext.prev"
        :to="prevNext.prev.href"
        class="group flex items-start gap-4 p-5 rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 card-hover duration-200"
      >
        <div class="shrink-0 w-10 h-10 rounded-lg bg-gray-200/80 dark:bg-gray-700/80 group-hover:bg-primary-100 dark:group-hover:bg-primary-900/40 flex items-center justify-center text-gray-600 dark:text-gray-400 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
        </div>
        <div class="min-w-0">
          <span class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Anterior</span>
          <p class="mt-1 font-semibold text-gray-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">{{ prevNext.prev.title }}</p>
        </div>
      </router-link>
      <router-link
        v-if="prevNext.next"
        :to="prevNext.next.href"
        class="group flex flex-row-reverse items-start gap-4 p-5 rounded-xl border border-gray-200 dark:border-gray-700 bg-gray-50/50 dark:bg-gray-800/50 card-hover duration-200"
      >
        <div class="shrink-0 w-10 h-10 rounded-lg bg-gray-200/80 dark:bg-gray-700/80 group-hover:bg-primary-100 dark:group-hover:bg-primary-900/40 flex items-center justify-center text-gray-600 dark:text-gray-400 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
          </svg>
        </div>
        <div class="min-w-0 flex-1 text-right">
          <span class="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Siguiente</span>
          <p class="mt-1 font-semibold text-gray-900 dark:text-white group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">{{ prevNext.next.title }}</p>
        </div>
      </router-link>
    </nav>

    <!-- Modal expandir imagen -->
    <Teleport to="body">
      <div
        v-if="expandedImage.src"
        class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/80 dark:bg-black/90"
        role="dialog"
        aria-modal="true"
        aria-label="Imagen ampliada"
      >
        <div class="relative max-w-[95vw] max-h-[95vh] flex flex-col items-center">
          <button
            type="button"
            class="absolute -top-12 right-0 p-2 rounded-lg text-white hover:bg-white/20 transition-colors focus:outline-none focus:ring-2 focus:ring-white z-10"
            aria-label="Cerrar"
            @click="closeImageExpand"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
          <img
            :src="expandedImage.src"
            :alt="expandedImage.caption || 'Imagen ampliada'"
            class="max-w-full max-h-[85vh] w-auto h-auto object-contain rounded-lg shadow-2xl"
            @click.stop
          />
          <p v-if="expandedImage.caption" class="mt-3 text-center text-sm text-gray-300">
            {{ expandedImage.caption }}
          </p>
        </div>
      </div>
    </Teleport>
  </article>
  <article v-else class="max-w-none">
    <h1 class="text-4xl font-bold text-gray-900 dark:text-white mb-6">No encontrado</h1>
    <p class="text-gray-600 dark:text-gray-300 mb-6">La página solicitada no existe.</p>
    <router-link
      to="/docs"
      class="inline-flex items-center gap-2 text-primary-600 dark:text-primary-400 hover:underline font-medium"
    >
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
      </svg>
      Volver a Documentación
    </router-link>
  </article>
</template>

<script setup>
import { ref, computed, h, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import docsData from '../../../data/docs.json'
import DocMermaidBlock from './DocMermaidBlock.vue'
import DocJsonSchemaBlock from './DocJsonSchemaBlock.vue'

const route = useRoute()
const expandedImage = ref({ src: null, caption: null })

function openImageExpand(src, caption) {
  expandedImage.value = { src, caption: caption || null }
  document.body.style.overflow = 'hidden'
}

function closeImageExpand() {
  expandedImage.value = { src: null, caption: null }
  document.body.style.overflow = ''
}

function onKeydown(e) {
  if (e.key === 'Escape') closeImageExpand()
}

onMounted(() => {
  window.addEventListener('keydown', onKeydown)
})
onUnmounted(() => {
  window.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})

const page = computed(() => {
  const path = route.path.replace(/^\/docs\/?/, '').split('/').filter(Boolean)
  let section = 'docs'
  let slug = 'intro'

  if (path[0] && ['admin', 'developers', 'mcp'].includes(path[0])) {
    section = path[0]
    slug = path[1] || 'intro'
  } else {
    slug = path[0] || 'intro'
  }

  const sectionData = docsData[section]
  if (!sectionData) return null
  return sectionData[slug] || null
})

const prevNext = computed(() => {
  const path = route.path.replace(/^\/docs\/?/, '').split('/').filter(Boolean)
  let section = 'docs'
  let currentSlug = ''
  if (path[0] && ['admin', 'developers', 'mcp'].includes(path[0])) {
    section = path[0]
    currentSlug = path[1] || ''
  } else {
    currentSlug = path[0] || ''
  }

  const navItems = docsData.nav[section] || []
  const slugKey = currentSlug || 'intro'
  const idx = navItems.findIndex(s => (s || 'intro') === slugKey)
  if (idx < 0) return null

  const sectionData = docsData[section]
  if (!sectionData) return null

  const getTitle = (s) => {
    const key = s || 'intro'
    return sectionData[key]?.title || 'Introducción'
  }
  const getHref = (s) => {
    if (section === 'docs') return s ? `/docs/${s}` : '/docs'
    if (section === 'admin') return s ? `/docs/admin/${s}` : '/docs/admin'
    if (section === 'mcp') return s ? `/docs/mcp/${s}` : '/docs/mcp'
    return s ? `/docs/developers/${s}` : '/docs/developers'
  }

  const prev = idx > 0 ? { href: getHref(navItems[idx - 1]), title: getTitle(navItems[idx - 1]) } : null
  const next = idx < navItems.length - 1 && idx >= 0 ? { href: getHref(navItems[idx + 1]), title: getTitle(navItems[idx + 1]) } : null
  return { prev, next }
})

const iconComponent = (icon) => {
  const icons = {
    settings: () => h('svg', { fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24', class: 'w-5 h-5' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z' }),
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z' })
    ]),
    code: () => h('svg', { fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24', class: 'w-5 h-5' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4' })
    ]),
    rocket: () => h('svg', { fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24', class: 'w-5 h-5' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M15.59 14.37a6 6 0 01-5.84 7.38v-4.8m5.84-2.58a14.98 14.98 0 006.16-12.12A14.98 14.98 0 009.631 8.41m5.96 5.96a14.926 14.926 0 01-5.841 2.58m-.119-8.54a6 6 0 00-7.381 5.84h4.8m2.581-5.84a14.927 14.927 0 00-2.58 5.84m2.699 2.7c-.103.021-.207.041-.311.06a15.081 15.081 0 01-2.448-2.448 14.9 14.9 0 01.06-.312m-2.24 2.39a4.493 4.493 0 00-1.757 4.306 4.493 4.493 0 004.306-1.758M16.5 9a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0z' })
    ]),
    book: () => h('svg', { fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24', class: 'w-5 h-5' }, [
      h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: 'M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253' })
    ])
  }
  return icons[icon] || icons.book
}

const calloutClass = (type) => {
  const classes = {
    tip: 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-200',
    info: 'bg-primary-50 dark:bg-primary-900/20 border-primary-200 dark:border-primary-800 text-primary-800 dark:text-primary-200',
    warning: 'bg-amber-50 dark:bg-amber-900/20 border-amber-200 dark:border-amber-800 text-amber-800 dark:text-amber-200'
  }
  return classes[type] || classes.info
}

const calloutIcon = (type) => {
  const path = type === 'tip'
    ? 'M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z'
    : type === 'warning'
      ? 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z'
      : 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
  return () => h('svg', { class: 'w-5 h-5 shrink-0', fill: 'none', stroke: 'currentColor', viewBox: '0 0 24 24' }, [
    h('path', { 'stroke-linecap': 'round', 'stroke-linejoin': 'round', 'stroke-width': '2', d: path })
  ])
}
</script>

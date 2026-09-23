<script setup>
import { computed, ref } from 'vue'
import { resolveTenantSlug } from '@/utils/tenant'

const copied = ref(false)
const tenant = computed(() => resolveTenantSlug())
const webOrigin = computed(() => (typeof window === 'undefined' ? '' : window.location.origin))
const apiOrigin = computed(() => {
  const configured = import.meta.env.VITE_API_URL
  if (configured && /^https?:\/\//i.test(configured)) return configured.replace(/\/$/, '')
  return 'http://localhost:5050'
})

const snippet = computed(() => `<script
  src="${webOrigin.value}/embed.js"
  data-api="${apiOrigin.value}"
  data-tenant="${tenant.value}"
  async
><\/script>`)

async function copy() {
  await navigator.clipboard.writeText(snippet.value)
  copied.value = true
  window.setTimeout(() => {
    copied.value = false
  }, 1600)
}
</script>

<template>
  <section class="max-w-3xl space-y-4">
    <header>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Embed chat</h1>
      <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">
        Paste this snippet on a site that is listed in Chat:AllowedOrigins. The tenant comes from data-tenant, not cookies.
      </p>
    </header>
    <pre class="overflow-auto rounded-xl bg-ink-950 p-4 text-sm text-primary-500">{{ snippet }}</pre>
    <button type="button" class="btn-primary" @click="copy">
      {{ copied ? 'Copied' : 'Copy embed code' }}
    </button>
  </section>
</template>

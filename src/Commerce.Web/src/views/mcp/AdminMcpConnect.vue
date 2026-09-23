<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900">
    <main class="max-w-3xl mx-auto px-3 sm:px-6 lg:px-8 py-4 sm:py-8">
      <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">Conectar MCP</h1>
      <p class="mt-2 text-sm text-gray-600 dark:text-gray-400">
        Expón las tools de Commerce a Claude, ChatGPT, Cursor y otros clientes MCP.
      </p>

      <div class="mt-6 rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5 space-y-4">
        <div>
          <p class="text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">URL del servidor</p>
          <code class="mt-1 block text-sm break-all text-gray-900 dark:text-white">{{ mcpUrl }}</code>
        </div>

        <div>
          <h2 class="text-sm font-semibold text-gray-900 dark:text-white">API Key</h2>
          <ol class="mt-2 list-decimal list-inside space-y-1 text-sm text-gray-700 dark:text-gray-300">
            <li>
              Crea una clave en
              <router-link to="/admin/api-keys" class="text-primary-600 dark:text-primary-400 hover:underline">API Keys</router-link>.
            </li>
            <li>Configura el cliente MCP con la URL anterior.</li>
            <li>
              Envía el header
              <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">x-api-key</code>
              con el valor secreto de la clave.
            </li>
          </ol>
        </div>

        <div>
          <h2 class="text-sm font-semibold text-gray-900 dark:text-white">OAuth 2.1</h2>
          <p class="mt-2 text-sm text-gray-700 dark:text-gray-300">
            Clientes como ChatGPT o Claude usan Dynamic Client Registration contra este host
            (<code class="text-xs">POST {{ mcpUrl }}/oauth/register</code>) y el flujo de consentimiento en Kutria.
          </p>
        </div>

        <details class="rounded-lg border border-gray-200 dark:border-gray-600 overflow-hidden">
          <summary class="px-4 py-3 cursor-pointer text-sm font-medium text-gray-900 dark:text-white bg-gray-50 dark:bg-gray-800/50">
            Ejemplo Cursor (<code class="text-xs">mcp.json</code>)
          </summary>
          <pre class="p-4 text-xs overflow-x-auto bg-gray-900 text-gray-100"><code>{{ cursorConfig }}</code></pre>
        </details>
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const mcpUrl = computed(() =>
  (import.meta.env.VITE_MCP_URL || 'http://localhost:5055').replace(/\/$/, '')
)

const cursorConfig = computed(() =>
  JSON.stringify(
    {
      mcpServers: {
        kutria: {
          url: mcpUrl.value,
          headers: {
            'x-api-key': 'YOUR_API_KEY'
          }
        }
      }
    },
    null,
    2
  )
)
</script>

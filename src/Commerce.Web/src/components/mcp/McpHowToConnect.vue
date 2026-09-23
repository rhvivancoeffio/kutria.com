<template>
  <div v-if="showContent" class="space-y-4">
    <h2 v-if="showTitle" class="text-sm font-medium text-gray-900 dark:text-white">Cómo conectar</h2>
    <div class="space-y-4">
      <!-- ApiKey -->
      <template v-if="securityType === 'ApiKey'">
        <p class="text-sm text-gray-600 dark:text-gray-400">
          Este servidor requiere autenticación por <strong>API Key</strong>. Sigue estos pasos para conectarte desde Cursor, Claude Desktop u otro cliente MCP:
        </p>
        <ol class="list-decimal list-inside space-y-2 text-sm text-gray-700 dark:text-gray-300">
          <li>Crea una API Key en <router-link to="/admin/api-keys" class="text-primary-600 dark:text-primary-400 hover:underline">API Keys</router-link> (puedes restringirla a este servidor si lo deseas).</li>
          <li>Usa la <strong>URL del servidor MCP</strong>: <code class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs break-all">{{ mcpUrl }}</code></li>
          <li>Envía estos headers en cada petición:
            <ul class="mt-2 ml-4 list-disc list-inside space-y-0.5 text-gray-600 dark:text-gray-400">
              <li><code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">x-api-key</code>: tu API Key (el valor secreto que copiaste al crear la key).</li>
            </ul>
          </li>
        </ol>
        <p class="text-xs text-gray-500 dark:text-gray-500">
          En Cursor: añade el servidor con la URL anterior y configura los headers en la definición del servidor MCP, o usa un cliente que permita cabeceras personalizadas.
        </p>
      </template>
      <!-- OAuth -->
      <template v-else-if="securityType === 'OAuth'">
        <p class="text-sm text-gray-600 dark:text-gray-400">
          Este servidor usa <strong>OAuth 2.1</strong> para autorización. Los clientes deben registrar una aplicación (DCR), iniciar el flujo de autorización y usar el token obtenido.
        </p>
        <ol class="list-decimal list-inside space-y-2 text-sm text-gray-700 dark:text-gray-300">
          <li><strong>URL del servidor MCP</strong>: <code class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs break-all">{{ mcpUrl }}</code></li>
          <li>Registra el cliente con <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">POST {{ baseUrl }}/oauth/register</code> (Dynamic Client Registration).</li>
          <li>Inicia el flujo: redirige al usuario a <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs break-all">GET {{ baseUrl }}/oauth/authorize?client_id=...&redirect_uri=...&response_type=code&...</code> (usa PKCE).</li>
          <li>El usuario verá la pantalla de consentimiento en Kutria (<code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">/mcp/oauth/consent</code>), iniciará sesión si hace falta y aceptará el acceso.</li>
          <li>Tras autorizar, intercambia el <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">code</code> por un <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">access_token</code> con <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">POST {{ baseUrl }}/oauth/token</code>.</li>
          <li>Envía en cada petición MCP: <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">Authorization: Bearer &lt;access_token&gt;</code>.</li>
        </ol>
        <p class="text-xs text-gray-500 dark:text-gray-500">
          Más detalles en <router-link to="/docs/mcp/oauth" class="text-primary-600 dark:text-primary-400 hover:underline">Documentación → Flujo OAuth</router-link>.
        </p>
      </template>

      <!-- Ejemplos por cliente (estilo Notion: https://developers.notion.com/guides/mcp/get-started-with-mcp) -->
      <div class="border-t border-gray-200 dark:border-gray-600 pt-4 mt-4">
        <h3 class="text-sm font-semibold text-gray-900 dark:text-white mb-3">Conectar desde tu cliente</h3>
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-4">
          Ejemplos para los clientes MCP más usados. Sustituye la URL por <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-xs">{{ mcpUrl }}</code>.
        </p>
        <div class="space-y-3">
          <!-- Cursor -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="cursor" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>Cursor</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar configuración" @click.stop="copyConfig(cursorConfig)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>Abre <strong>Cursor Settings</strong> → <strong>MCP</strong> → <strong>Add new global MCP server</strong>.</li>
                <li>Pega esta configuración (o en <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">.cursor/mcp.json</code> en la raíz del proyecto para compartir con el equipo):</li>
              </ol>
              <pre class="mt-2 p-3 rounded-lg bg-gray-900 dark:bg-gray-950 text-gray-100 text-xs overflow-x-auto"><code>{{ cursorConfig }}</code></pre>
              <p class="text-xs">Guarda y reinicia Cursor. Si usas <strong>ApiKey</strong>, sustituye <code class="px-1 py-0.5 rounded bg-gray-700 text-gray-300">YOUR_API_KEY</code> por tu API Key. Si usas OAuth, completa el flujo cuando te lo pida.</p>
            </div>
          </details>

          <!-- Claude Code -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="claude-code" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>Claude Code</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar comando" @click.stop="copyConfig(claudeCodeCommand)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <p class="text-xs">En la terminal:</p>
              <pre class="p-3 rounded-lg bg-gray-900 dark:bg-gray-950 text-gray-100 text-xs overflow-x-auto"><code>claude mcp add --transport http gravity {{ mcpUrl }}</code></pre>
              <p class="text-xs">Luego ejecuta <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">/mcp</code> en Claude Code. Con <strong>OAuth</strong> sigue el flujo cuando te lo pida. Con <strong>ApiKey</strong> el CLI no permite headers; usa Cursor o VS Code con la config que incluye headers.</p>
            </div>
          </details>

          <!-- Copilot (VS Code / GitHub Copilot) -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="vscode" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>Copilot</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar configuración" @click.stop="copyConfig(vscodeConfig)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>Crea <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">.vscode/mcp.json</code> en tu workspace:</li>
              </ol>
              <pre class="mt-2 p-3 rounded-lg bg-gray-900 dark:bg-gray-950 text-gray-100 text-xs overflow-x-auto"><code>{{ vscodeConfig }}</code></pre>
              <ol class="list-decimal list-inside space-y-1 text-xs mt-2" start="2">
                <li>Abre la Command Palette (<kbd class="px-1.5 py-0.5 rounded border border-gray-300 dark:border-gray-600 text-xs">Cmd+Shift+P</kbd> / <kbd class="px-1.5 py-0.5 rounded border border-gray-300 dark:border-gray-600 text-xs">Ctrl+Shift+P</kbd>) y ejecuta <strong>MCP: List Servers</strong>.</li>
                <li>Inicia el servidor Gravity. Con <strong>ApiKey</strong>, VS Code te pedirá la API Key la primera vez (variable <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">gravity-api-key</code>). Con OAuth, completa el flujo si se solicita.</li>
              </ol>
            </div>
          </details>

          <!-- Claude Desktop -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="claude-desktop" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>Claude Desktop</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar URL" @click.stop="copyConfig(mcpUrl)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>Abre <strong>Settings</strong> → <strong>Connectors</strong>.</li>
                <li>Pulsa <strong>Add Connector</strong> e introduce la URL: <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 break-all">{{ mcpUrl }}</code></li>
                <li v-if="securityType === 'OAuth'">Completa el flujo OAuth para conectar tu workspace.</li>
                <li v-else class="text-amber-600 dark:text-amber-400">Con <strong>ApiKey</strong> Claude Desktop no permite cabeceras personalizadas. Usa <strong>Cursor</strong> o <strong>VS Code</strong> con la config que incluye headers.</li>
              </ol>
            </div>
          </details>

          <!-- ChatGPT -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="chatgpt" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>ChatGPT</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar URL" @click.stop="copyConfig(mcpUrl)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>Ve a <a href="https://chatgpt.com/#settings/Connectors" target="_blank" rel="noopener noreferrer" class="text-primary-600 dark:text-primary-400 hover:underline">chatgpt.com → Settings → Connectors</a> (requiere inicio de sesión).</li>
                <li>Pulsa <strong>Add Connector</strong> e introduce: <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700 break-all">{{ mcpUrl }}</code></li>
                <li v-if="securityType === 'OAuth'">Completa el flujo OAuth.</li>
                <li v-else class="text-amber-600 dark:text-amber-400">Con <strong>ApiKey</strong> ChatGPT Connectors no permiten cabeceras. Usa <strong>Cursor</strong> o <strong>VS Code</strong> con la config con headers.</li>
              </ol>
            </div>
          </details>

          <!-- Windsurf -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="windsurf" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>Windsurf</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar configuración" @click.stop="copyConfig(windsurfConfig)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>Abre <strong>Windsurf Settings</strong> (<kbd class="px-1.5 py-0.5 rounded border border-gray-300 dark:border-gray-600 text-xs">Cmd+,</kbd> en Mac) → busca <strong>MCP</strong>.</li>
                <li>Haz clic en <strong>View raw config</strong> para abrir <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">mcp_config.json</code> y añade:</li>
              </ol>
              <pre class="mt-2 p-3 rounded-lg bg-gray-900 dark:bg-gray-950 text-gray-100 text-xs overflow-x-auto"><code>{{ windsurfConfig }}</code></pre>
              <p class="text-xs">Guarda y reinicia Windsurf. Con <strong>ApiKey</strong>, sustituye <code class="px-1 py-0.5 rounded bg-gray-700 text-gray-300">YOUR_API_KEY</code> en <code class="px-1 py-0.5 rounded bg-gray-700 text-gray-300">headers</code> si tu versión lo soporta. Completa el OAuth cuando se solicite.</p>
            </div>
          </details>

          <!-- Codex -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="codex" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>Codex</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar configuración" @click.stop="copyConfig(codexConfig)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>Añade el servidor en <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">~/.codex/config.toml</code>:</li>
              </ol>
              <pre class="mt-2 p-3 rounded-lg bg-gray-900 dark:bg-gray-950 text-gray-100 text-xs overflow-x-auto"><code>{{ codexConfig }}</code></pre>
              <ol class="list-decimal list-inside space-y-1 text-xs mt-2" start="2">
                <li>Autentica con: <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">codex mcp login gravity</code> y completa el OAuth.</li>
              </ol>
            </div>
          </details>

          <!-- Otros clientes -->
          <details class="group rounded-lg border border-gray-200 dark:border-gray-600 bg-gray-50/50 dark:bg-gray-800/30 overflow-hidden">
            <summary class="flex items-center gap-2 px-4 py-3 cursor-pointer list-none text-sm font-medium text-gray-900 dark:text-white hover:bg-gray-100 dark:hover:bg-gray-700/50 [&::-webkit-details-marker]:hidden">
              <ClientIcon client-id="other" size="sm" />
              <span class="text-primary-500">▸</span>
              <span>Otros clientes (JSON)</span>
              <button type="button" class="ml-auto p-1.5 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-200 dark:hover:text-gray-300 dark:hover:bg-gray-600 transition-colors" title="Copiar configuración HTTP" @click.stop="copyConfig(genericHttpConfig)">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" /></svg>
              </button>
            </summary>
            <div class="px-4 pb-4 pt-0 space-y-3 text-sm text-gray-600 dark:text-gray-400">
              <p class="text-xs">La mayoría de clientes MCP aceptan JSON. <strong>Streamable HTTP</strong> (recomendado):</p>
              <pre class="p-3 rounded-lg bg-gray-900 dark:bg-gray-950 text-gray-100 text-xs overflow-x-auto"><code>{{ genericHttpConfig }}</code></pre>
              <p v-if="securityType === 'ApiKey'" class="text-xs text-amber-600 dark:text-amber-400">Con ApiKey, sustituye <code class="px-1 py-0.5 rounded bg-gray-700 text-gray-300">YOUR_API_KEY</code> en <code class="px-1 py-0.5 rounded bg-gray-700 text-gray-300">headers</code> por tu API Key.</p>
              <p class="text-xs mt-2">Si tu herramienta solo soporta stdio, puedes usar el bridge <code class="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-700">mcp-remote</code>:</p>
              <pre class="p-3 rounded-lg bg-gray-900 dark:bg-gray-950 text-gray-100 text-xs overflow-x-auto"><code>{{ stdioConfig }}</code></pre>
              <p v-if="securityType === 'ApiKey'" class="text-xs text-amber-600 dark:text-amber-400">Con <strong>ApiKey</strong>, el bridge stdio no suele enviar cabeceras HTTP. Usa un cliente con transporte HTTP que soporte <code class="px-1 py-0.5 rounded bg-gray-700 text-gray-300">headers</code> (Cursor, VS Code) o un proxy que inyecte los headers.</p>
            </div>
          </details>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useToast } from 'vue-toastification'
import ClientIcon from './ClientIcon.vue'

const toast = useToast()

function copyConfig(text) {
  if (!text) return
  navigator.clipboard.writeText(text).then(
    () => toast.success('Copiado al portapapeles'),
    () => toast.error('No se pudo copiar')
  )
}

const props = defineProps({
  /** Tipo de seguridad del servidor: 'ApiKey' | 'OAuth' */
  securityType: { type: String, default: '' },
  /** Id del MCP Server (para X-Gravity-MCPServer-Id) */
  serverId: { type: String, default: '' },
  /** Origen de la API (ej. https://mcp.meetgravity.io) */
  baseUrl: { type: String, default: '' },
  /** Mostrar el título "Cómo conectar" (útil ocultarlo si el padre ya lo muestra) */
  showTitle: { type: Boolean, default: true }
})

const showContent = computed(() =>
  props.securityType === 'ApiKey' || props.securityType === 'OAuth'
)

const mcpUrl = computed(() => props.baseUrl.replace(/\/$/, ''))

const claudeCodeCommand = computed(() => `claude mcp add --transport http kutria ${mcpUrl.value}`)

const isApiKey = computed(() => props.securityType === 'ApiKey')

// Headers para ApiKey
const apiKeyHeadersJson = computed(() =>
  isApiKey.value
    ? `,
      "headers": {
        "x-api-key": "YOUR_API_KEY"
      }`
    : '')

// Configuraciones de ejemplo por cliente; con ApiKey se añaden headers donde el cliente lo soporta
const cursorConfig = computed(() =>
  `{
  "mcpServers": {
    "kutria": {
      "url": "${mcpUrl.value}"${apiKeyHeadersJson.value}
    }
  }
}`)

const vscodeConfig = computed(() => {
  const headersBlock = isApiKey.value
    ? `,
      "headers": {
        "x-api-key": "\${input:kutria-api-key}"
      }`
    : ''
  const inputsBlock = isApiKey.value
    ? `,
  "inputs": [
    {
      "type": "promptString",
      "id": "kutria-api-key",
      "description": "Kutria MCP API Key (desde Admin → API Keys)",
      "password": true
    }
  ]`
    : ''
  return `{
  "servers": {
    "kutria": {
      "type": "http",
      "url": "${mcpUrl.value}"${headersBlock}
    }
  }${inputsBlock}
}`
})

const windsurfConfig = computed(() => {
  const headersBlock = isApiKey.value
    ? `,
      "headers": {
        "x-api-key": "YOUR_API_KEY"
      }`
    : ''
  return `{
  "mcpServers": {
    "kutria": {
      "serverUrl": "${mcpUrl.value}"${headersBlock}
    }
  }
}`
})

const codexConfig = computed(() => {
  if (isApiKey.value) {
    return `[mcp_servers.kutria]
url = "${mcpUrl.value}"

# Headers para ApiKey (si tu cliente los soporta; revisa la doc de Codex)
# En muchos clientes TOML no hay headers; usa Cursor o VS Code con la config JSON.`
  }
  return `[mcp_servers.kutria]
url = "${mcpUrl.value}"`
})

const genericHttpConfig = computed(() =>
  `{
  "mcpServers": {
    "kutria": {
      "url": "${mcpUrl.value}"${apiKeyHeadersJson.value}
    }
  }
}`)

const stdioConfig = computed(() =>
  `{
  "mcpServers": {
    "kutria": {
      "command": "npx",
      "args": ["-y", "mcp-remote", "${mcpUrl.value}"]
    }
  }
}`)
</script>

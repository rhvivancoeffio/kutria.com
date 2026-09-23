import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'
import path from 'path'

const apiProxyTarget = process.env.VITE_API_PROXY_TARGET || 'http://localhost:5050'

function apiProxy() {
  return {
    target: apiProxyTarget,
    changeOrigin: false,
    secure: false,
    ws: true
  }
}

export default defineConfig({
  base: '/',
  plugins: [
    vue(),
    {
      name: 'commerce-embed-entry',
      configureServer(server) {
        server.middlewares.use((req, _res, next) => {
          if (req.url === '/embed.js' || req.url?.startsWith('/embed.js?')) {
            req.url = '/src/embed.js'
          }
          next()
        })
      }
    },
    // SPA is served on one host. Tenant is the path `/t/{identifier}`, not a subdomain.
    VitePWA({
      strategies: 'injectManifest',
      srcDir: 'src',
      filename: 'sw.js',
      registerType: 'prompt',
      devOptions: { enabled: process.env.VITE_PWA_IN_DEV === 'true' },
      includeAssets: ['favicon.ico', 'favicon.png', 'apple-touch-icon.png', 'robots.txt', 'icons/icon-192.png', 'icons/icon-512.png', 'screenshots/wide.png', 'screenshots/narrow.png'],
      manifest: {
        id: '/',
        name: 'Kutria',
        short_name: 'Kutria',
        description: 'AI Commerce OS — escala tus ventas y mejora tus operaciones',
        theme_color: '#0A0A0A',
        background_color: '#0A0A0A',
        display: 'standalone',
        display_override: ['window-controls-overlay', 'standalone'],
        scope: '/',
        start_url: '/',
        screenshots: [
          { src: '/screenshots/wide.png', sizes: '1280x720', type: 'image/png', form_factor: 'wide', label: 'Desktop' },
          { src: '/screenshots/narrow.png', sizes: '390x844', type: 'image/png', form_factor: 'narrow', label: 'Mobile' }
        ],
        icons: [
          { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: '/icons/icon-maskable-192.png', sizes: '192x192', type: 'image/png', purpose: 'maskable' },
          { src: '/icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' }
        ]
      },
      injectManifest: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}']
      }
    })
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  },
  server: {
    port: 3000,
    strictPort: true,
    host: true,
    allowedHosts: true,
    proxy: {
      '/api': {
        ...apiProxy(),
        rewrite: (p) => p.replace(/^\/api/, '')
      },
      '/auth': {
        ...apiProxy(),
        bypass(req) {
          if (/^\/auth\/[^/]+\/callback/.test(req.url || '')) {
            return req.url
          }
        }
      },
      '/oauth': apiProxy(),
      '/.well-known': apiProxy()
    }
  }
})

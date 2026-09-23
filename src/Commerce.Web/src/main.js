import { createApp } from 'vue'
import { createPinia } from 'pinia'
import Toast from 'vue-toastification'
import 'vue-toastification/dist/index.css'
import VueDiff from 'vue-diff'
import 'vue-diff/dist/index.css'
import { vResizable } from 'vue-resizable-table-column'
import App from './App.vue'
import router from './router'
import './style.css'
import { registerPwa } from './pwa/register'
import { usePwaStore } from './stores/pwaStore'

const pinia = createPinia()
const app = createApp(App)
app.directive('resizable', vResizable)
app.use(pinia)
app.use(router)

// PWA + Vite dev: a registered SW (e.g. from a prior prod/preview session) often breaks dynamic imports
// (TypeError: Failed to fetch dynamically imported module …/*.vue). Only register SW in prod or when explicitly testing PWA.
const pwaInDev = import.meta.env.VITE_PWA_IN_DEV === 'true'
if (import.meta.env.PROD || pwaInDev) {
  registerPwa(usePwaStore())
} else if ('serviceWorker' in navigator) {
  void navigator.serviceWorker.getRegistrations().then((regs) => {
    for (const r of regs) void r.unregister()
  })
}
app.use(Toast, {
  position: 'top-right',
  timeout: 4000,
  closeOnClick: true,
  pauseOnHover: true
})
app.use(VueDiff)
app.mount('#app')

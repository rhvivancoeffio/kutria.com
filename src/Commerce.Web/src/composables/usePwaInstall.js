import { ref, computed, onMounted, onUnmounted } from 'vue'

const DISMISS_STORAGE_KEY = 'pwa-install-dismissed'
const DISMISS_COOLDOWN_MS = 7 * 24 * 60 * 60 * 1000 // 7 days

export function usePwaInstall() {
  const canInstall = ref(false)
  const isInstalled = ref(false)
  const deferredPrompt = ref(null)

  function checkIfInstalled() {
    return (
      window.matchMedia('(display-mode: standalone)').matches ||
      window.navigator.standalone === true ||
      document.referrer.includes('android-app://')
    )
  }

  function wasDismissedRecently() {
    try {
      const dismissed = localStorage.getItem(DISMISS_STORAGE_KEY)
      if (!dismissed) return false
      const ts = parseInt(dismissed, 10)
      return Date.now() - ts < DISMISS_COOLDOWN_MS
    } catch {
      return false
    }
  }

  function dismissInstall() {
    try {
      localStorage.setItem(DISMISS_STORAGE_KEY, String(Date.now()))
    } catch {
      // ignore
    }
    canInstall.value = false
  }

  async function promptInstall() {
    if (!deferredPrompt.value) return false
    deferredPrompt.value.prompt()
    const { outcome } = await deferredPrompt.value.userChoice
    if (outcome === 'accepted') {
      canInstall.value = false
      deferredPrompt.value = null
      return true
    }
    return false
  }

  const shouldShowPrompt = computed(
    () => canInstall.value && !isInstalled.value && !wasDismissedRecently()
  )

  const handleBeforeInstallPrompt = (e) => {
    e.preventDefault()
    deferredPrompt.value = e
    canInstall.value = true
  }

  const handleAppInstalled = () => {
    isInstalled.value = true
    canInstall.value = false
    deferredPrompt.value = null
  }

  onMounted(() => {
    isInstalled.value = checkIfInstalled()
    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    window.addEventListener('appinstalled', handleAppInstalled)
  })

  onUnmounted(() => {
    window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt)
    window.removeEventListener('appinstalled', handleAppInstalled)
  })

  return {
    canInstall,
    isInstalled,
    shouldShowPrompt,
    promptInstall,
    dismissInstall
  }
}

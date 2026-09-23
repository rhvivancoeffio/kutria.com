import { registerSW } from 'virtual:pwa-register'

let pwaStore = null

export function registerPwa(store) {
  pwaStore = store
  const updateSW = registerSW({
    immediate: true,
    onNeedRefresh() {
      if (pwaStore) {
        pwaStore.setNeedRefresh(() => {
          updateSW(true)
        })
      }
    },
    onOfflineReady() {
      if (pwaStore) {
        pwaStore.setOfflineReady()
      }
    }
  })
  return updateSW
}

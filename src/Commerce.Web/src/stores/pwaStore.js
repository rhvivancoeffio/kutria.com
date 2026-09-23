import { defineStore } from 'pinia'
import { ref } from 'vue'

export const usePwaStore = defineStore('pwa', () => {
  const needRefresh = ref(false)
  const offlineReady = ref(false)
  let updateSWFn = null

  function setNeedRefresh(fn) {
    needRefresh.value = true
    updateSWFn = fn
  }

  function setOfflineReady() {
    offlineReady.value = true
  }

  function applyUpdate() {
    if (typeof updateSWFn === 'function') {
      updateSWFn()
    }
    needRefresh.value = false
    updateSWFn = null
  }

  function dismissOfflineReady() {
    offlineReady.value = false
  }

  function dismissUpdate() {
    needRefresh.value = false
    updateSWFn = null
  }

  return {
    needRefresh,
    offlineReady,
    setNeedRefresh,
    setOfflineReady,
    applyUpdate,
    dismissOfflineReady,
    dismissUpdate
  }
})

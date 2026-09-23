import { ref } from 'vue'

// Settings state
const showJson = ref(false)

// Initialize settings on module load
if (typeof window !== 'undefined' && typeof localStorage !== 'undefined') {
  const savedShowJson = localStorage.getItem('showJson')
  if (savedShowJson !== null) {
    showJson.value = savedShowJson === 'true'
  }
}

export function useSettings() {
  const setShowJson = (value) => {
    showJson.value = value
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('showJson', value.toString())
    }
  }

  const toggleShowJson = () => {
    setShowJson(!showJson.value)
  }

  return {
    showJson,
    setShowJson,
    toggleShowJson
  }
}

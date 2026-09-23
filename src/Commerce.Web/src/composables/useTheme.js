import { ref } from 'vue'

const theme = ref('dark')

function updateDocumentClass(themeValue) {
  if (typeof document === 'undefined') return
  const dark = themeValue === 'dark'
  const root = document.documentElement
  root.classList.toggle('dark', dark)
  root.style.backgroundColor = dark ? '#0A0A0A' : '#f4f4f5'
  root.style.colorScheme = dark ? 'dark' : 'light'
  document.querySelector('meta[name="color-scheme"]')?.setAttribute('content', dark ? 'dark' : 'light')
  document.querySelector('meta[name="theme-color"]')?.setAttribute('content', dark ? '#0A0A0A' : '#f4f4f5')
}

if (typeof document !== 'undefined') {
  document.documentElement.classList.remove('dark')
  let savedTheme = localStorage.getItem('theme')
  if (!savedTheme || (savedTheme !== 'dark' && savedTheme !== 'light')) {
    // Kutria default: dark (#0A0A0A).
    savedTheme = 'dark'
    localStorage.setItem('theme', 'dark')
  }
  theme.value = savedTheme
  updateDocumentClass(savedTheme)
}

export function useTheme() {
  const setTheme = (newTheme) => {
    theme.value = newTheme
    updateDocumentClass(newTheme)
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('theme', newTheme)
    }
  }

  const toggleTheme = () => {
    setTheme(theme.value === 'dark' ? 'light' : 'dark')
  }

  return {
    theme,
    setTheme,
    toggleTheme
  }
}

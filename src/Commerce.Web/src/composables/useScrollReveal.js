import { onMounted, onUnmounted } from 'vue'

const VISIBLE_CLASS = 'is-visible'
const REVEAL_SELECTOR = '.scroll-reveal'

/**
 * Observa elementos con clase .scroll-reveal y les añade .is-visible cuando entran en el viewport.
 * @param {import('vue').Ref<HTMLElement | null>} rootRef - Contenedor (ej. <main>) donde están las secciones
 * @param {IntersectionObserverInit} [options] - Commerceiones del observer (threshold, rootMargin)
 */
export function useScrollReveal(rootRef, options = {}) {
  let observer = null

  const defaultOptions = {
    threshold: 0.08,
    rootMargin: '0px 0px -40px 0px',
    ...options
  }

  onMounted(() => {
    const root = rootRef?.value
    if (!root) return

    observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add(VISIBLE_CLASS)
        }
      })
    }, defaultOptions)

    root.querySelectorAll(REVEAL_SELECTOR).forEach((el) => observer.observe(el))
  })

  onUnmounted(() => {
    observer?.disconnect()
  })
}

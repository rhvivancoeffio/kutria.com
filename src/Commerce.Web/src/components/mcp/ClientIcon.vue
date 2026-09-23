<template>
  <div
    :class="[
      'shrink-0 rounded-xl flex items-center justify-center overflow-hidden',
      bgClass,
      sizeClass
    ]"
    aria-hidden="true"
  >
    <img
      v-if="logoUrl && !logoError"
      :src="logoUrl"
      :alt="''"
      class="w-full h-full object-contain"
      @error="logoError = true"
    />
    <span
      v-else
      class="font-bold text-white select-none"
      :class="letterSizeClass"
    >{{ letter }}</span>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { getClientLogo, getClientById, HOW_TO_CONNECT_NAMES } from '../../data/mcpClients'

const props = defineProps({
  /** One of: cursor, claude, windsurf, vscode, chatgpt, codex, other, claude-code, claude-desktop */
  clientId: { type: String, required: true },
  /** 'sm' | 'md' | 'lg' – sm=8, md=10, lg=14 (tailwind w-/h-) */
  size: { type: String, default: 'md' }
})

const logoError = ref(false)
const logoUrl = computed(() => getClientLogo(props.clientId))
const client = computed(() => getClientById(props.clientId))
const bgClass = computed(() => client.value.bgClass ?? 'bg-gray-100 dark:bg-gray-700')
const letter = computed(() => {
  const name = HOW_TO_CONNECT_NAMES[props.clientId] ?? client.value.name
  return (name && name[0]) ? name[0].toUpperCase() : '?'
})

const sizeMap = {
  sm: 'w-8 h-8',
  md: 'w-10 h-10',
  lg: 'w-14 h-14'
}
const sizeClass = computed(() => sizeMap[props.size] ?? sizeMap.md)
const letterSizeMap = { sm: 'text-sm', md: 'text-base', lg: 'text-2xl' }
const letterSizeClass = computed(() => letterSizeMap[props.size] ?? letterSizeMap.md)

watch(() => props.clientId, () => { logoError.value = false })
</script>

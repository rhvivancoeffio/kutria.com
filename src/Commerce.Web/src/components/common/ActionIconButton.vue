<template>
  <button
    type="button"
    :disabled="disabled"
    :title="titleText"
    :aria-label="titleText"
    :class="buttonClass"
    @click="$emit('click', $event)"
  >
    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path
        v-if="action === 'edit'"
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
      />
      <path
        v-else-if="action === 'revoke'"
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636"
      />
      <path
        v-else
        stroke-linecap="round"
        stroke-linejoin="round"
        stroke-width="2"
        d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
      />
    </svg>
  </button>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  action: {
    type: String,
    default: 'edit'
  },
  title: {
    type: String,
    default: ''
  },
  disabled: {
    type: Boolean,
    default: false
  },
  compact: {
    type: Boolean,
    default: false
  }
})

defineEmits(['click'])

const titleText = computed(() => {
  if (props.title) return props.title
  if (props.action === 'delete') return 'Eliminar'
  if (props.action === 'revoke') return 'Revocar'
  return 'Editar'
})

const toneClass = computed(() => {
  if (props.action === 'delete') {
    return 'text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20'
  }
  if (props.action === 'revoke') {
    return 'text-amber-600 dark:text-amber-400 hover:bg-amber-50 dark:hover:bg-amber-900/20'
  }
  return 'text-primary-600 dark:text-primary-400 hover:bg-primary-50 dark:hover:bg-primary-900/20'
})

const buttonClass = computed(() => ([
  'inline-flex items-center justify-center rounded-lg transition-colors touch-manipulation disabled:opacity-40 disabled:cursor-not-allowed',
  props.compact ? 'p-1.5' : 'p-2',
  toneClass.value
]))
</script>

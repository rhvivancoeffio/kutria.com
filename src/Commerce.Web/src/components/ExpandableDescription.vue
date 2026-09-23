<template>
  <div :class="textClass">
    <template v-if="!text">
      <slot name="empty"></slot>
    </template>
    <template v-else-if="isLong && !expanded">
      {{ truncate(text) }}
      <button
        type="button"
        @click.stop="expanded = true"
        class="text-primary-600 dark:text-primary-400 hover:underline ml-1 font-medium"
      >
        Leer más
      </button>
    </template>
    <template v-else>
      {{ text }}
      <button
        v-if="isLong"
        type="button"
        @click.stop="expanded = false"
        class="text-primary-600 dark:text-primary-400 hover:underline ml-1 font-medium"
      >
        Ver menos
      </button>
    </template>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  text: { type: String, default: '' },
  maxLength: { type: Number, default: 120 },
  textClass: { type: String, default: 'text-xs text-gray-500 dark:text-gray-400' }
})

const expanded = ref(false)
const isLong = computed(() => (props.text || '').length > props.maxLength)
const truncate = (s) => (s || '').slice(0, props.maxLength) + '...'
</script>

<template>
  <button
    type="button"
    role="switch"
    :aria-checked="modelValue"
    :aria-label="ariaLabel"
    :class="[
      'relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-gray-900',
      modelValue ? 'bg-primary-600 dark:bg-primary-500' : 'bg-gray-200 dark:bg-gray-600'
    ]"
    @click="$emit('update:modelValue', !modelValue)"
  >
    <span
      class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow transition duration-200 ease-in-out"
      :class="modelValue ? 'translate-x-5' : 'translate-x-0.5'"
      aria-hidden="true"
    />
  </button>
</template>

<script setup>
defineProps({
  modelValue: { type: Boolean, default: false },
  /** Accesible cuando no hay etiqueta visible junto al control. */
  ariaLabel: { type: String, default: '' }
})
defineEmits(['update:modelValue'])
</script>

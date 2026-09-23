<script setup>
defineProps({
  wordmark: { type: Boolean, default: true },
  size: { type: String, default: 'sm' }
})
</script>

<template>
  <span class="inline-flex items-center gap-2.5 text-current">
    <!-- Intenta SVG de tu brand, fallback a PNG real -->
    <img
      src="/brand/kutria-isotipo.svg"
      alt="Kutria"
      class="shrink-0 object-contain"
      :class="size === 'lg' ? 'h-10 w-10' : size === 'md' ? 'h-8 w-8' : 'h-7 w-7'"
      @error="(e) => { e.target.src = '/brand/kutria_k_isotipo.png' }"
    />
    <span
      v-if="wordmark"
      class="font-extrabold leading-none tracking-[-0.06em] text-current"
      :class="size === 'lg' ? 'text-[22px]' : 'text-[15px]'"
    >KUTRIA</span>
  </span>
</template>

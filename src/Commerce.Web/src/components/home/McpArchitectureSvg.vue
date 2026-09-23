<template>
  <svg
    viewBox="-8 -2 336 254"
    class="mcp-arch-svg w-full h-auto"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    xmlns:xlink="http://www.w3.org/1999/xlink"
    aria-hidden="true"
  >
    <!-- Flechas de conexión: líneas finas y puntas pequeñas -->
    <path d="M98 100 H126 M122 97 L128 100 L122 103" class="arrow-line" stroke-width="1.25" stroke-linecap="round" stroke-linejoin="round" fill="none" />
    <path d="M236 100 H264 M260 97 L266 100 L260 103" class="arrow-line" stroke-width="1.25" stroke-linecap="round" stroke-linejoin="round" fill="none" />
    <!-- Animación: punto que fluye -->
    <circle r="2.5" class="flow-dot">
      <animateMotion dur="2s" repeatCount="indefinite" path="M 98 100 L 126 100" keyPoints="0;1" keyTimes="0;1" />
    </circle>
    <circle r="2.5" class="flow-dot">
      <animateMotion dur="2s" repeatCount="indefinite" path="M 236 100 L 264 100" keyPoints="0;1" keyTimes="0;1" />
    </circle>
    <!-- Línea de flujo animada (más fina) -->
    <path class="flow-line flow-line-1" d="M98 100 H126" stroke-width="1.5" stroke-linecap="round" fill="none" />
    <path class="flow-line flow-line-2" d="M236 100 H264" stroke-width="1.5" stroke-linecap="round" fill="none" />
    <!-- MCP Client: icono computador/host -->
    <g class="client-host-icon" transform="translate(5, 70)">
      <rect x="8" y="0" width="74" height="44" rx="4" class="fill-gray-100 dark:fill-gray-800 stroke-gray-400 dark:stroke-gray-500" stroke-width="1.25" />
      <rect x="14" y="6" width="62" height="32" rx="2" class="fill-white dark:fill-gray-700 stroke-gray-300 dark:stroke-gray-600" stroke-width="1" />
      <rect x="38" y="44" width="14" height="6" rx="1" class="fill-gray-200 dark:fill-gray-600 stroke-gray-400 dark:stroke-gray-500" stroke-width="1" />
      <rect x="42" y="50" width="6" height="4" rx="0.5" class="fill-gray-300 dark:fill-gray-500" />
    </g>
    <text x="45" y="136" text-anchor="middle" class="text-[9px] font-medium fill-gray-600 dark:fill-gray-400" style="font-size: 9px">MCP Client</text>
    <!-- MCP Server: logo mcp.png -->
    <image href="/logos/mcp.png" x="148" y="75" width="70" height="50" preserveAspectRatio="xMidYMid meet" />
    <text x="183" y="136" text-anchor="middle" class="text-[9px] font-medium fill-gray-600 dark:fill-gray-400" style="font-size: 9px">MCP Server</text>
    <!-- Data Source: label con margen para que no se corte -->
    <text x="268" y="18" text-anchor="start" class="font-medium fill-gray-600 dark:fill-gray-400" style="font-size: 10px">Data Source</text>
    <image href="/logos/vtex.png" x="275" y="32" width="36" height="36" preserveAspectRatio="xMidYMid meet" />
    <image href="/logos/shopify.png" x="275" y="76" width="36" height="36" preserveAspectRatio="xMidYMid meet" />
    <image href="/logos/magento.png" x="275" y="120" width="36" height="36" preserveAspectRatio="xMidYMid meet" />
    <image href="/logos/gravity.jpeg" x="275" y="164" width="36" height="36" preserveAspectRatio="xMidYMid meet" />
    <image href="/logos/woocommerce.png" x="275" y="198" width="36" height="36" preserveAspectRatio="xMidYMid meet" />
  </svg>
</template>

<script setup>
</script>

<style scoped>
.mcp-arch-svg :deep(.arrow-line) {
  stroke: rgb(14 165 233);
}
.dark .mcp-arch-svg :deep(.arrow-line) {
  stroke: rgb(56 189 248);
}
.mcp-arch-svg :deep(.flow-dot) {
  fill: rgb(14 165 233);
}
.dark .mcp-arch-svg :deep(.flow-dot) {
  fill: rgb(56 189 248);
}
.mcp-arch-svg :deep(.flow-line) {
  stroke: rgb(14 165 233);
  stroke-dasharray: 4 8;
  animation: flow-dash 1.2s linear infinite;
}
.dark .mcp-arch-svg :deep(.flow-line) {
  stroke: rgb(56 189 248);
}
.mcp-arch-svg :deep(.flow-line-2) {
  animation-delay: 0.6s;
}
@keyframes flow-dash {
  to {
    stroke-dashoffset: -12;
  }
}
</style>

<template>
  <svg
    viewBox="0 0 280 200"
    class="w-full h-auto"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    xmlns:xlink="http://www.w3.org/1999/xlink"
    aria-hidden="true"
  >
    <defs>
      <!-- Clip circle: centro (0,0) en el espacio de cada grupo para que el logo quede centrado -->
      <clipPath id="clientNodeClip" clipPathUnits="userSpaceOnUse">
        <circle cx="0" cy="0" r="16" />
      </clipPath>
    </defs>
    <!-- Central node: logo MCP -->
    <image href="/logos/mcp.png" x="108" y="68" width="64" height="64" preserveAspectRatio="xMidYMid meet" />
    <!-- Connection lines (behind nodes) -->
    <line x1="140" y1="68" x2="140" y2="35" class="stroke-gray-300 dark:stroke-gray-600" stroke-width="1.5" stroke-dasharray="4 3" />
    <line x1="168" y1="82" x2="208" y2="42" class="stroke-gray-300 dark:stroke-gray-600" stroke-width="1.5" stroke-dasharray="4 3" />
    <line x1="172" y1="100" x2="218" y2="100" class="stroke-gray-300 dark:stroke-gray-600" stroke-width="1.5" stroke-dasharray="4 3" />
    <line x1="168" y1="118" x2="208" y2="158" class="stroke-gray-300 dark:stroke-gray-600" stroke-width="1.5" stroke-dasharray="4 3" />
    <line x1="140" y1="132" x2="140" y2="165" class="stroke-gray-300 dark:stroke-gray-600" stroke-width="1.5" stroke-dasharray="4 3" />
    <line x1="108" y1="100" x2="62" y2="100" class="stroke-gray-300 dark:stroke-gray-600" stroke-width="1.5" stroke-dasharray="4 3" />
    <!-- Client nodes: logos completos, recorte circular centrado, meet para no cortar -->
    <g transform="translate(140, 22)">
      <image href="/logos/claude.jpeg" x="-16" y="-16" width="32" height="32" clip-path="url(#clientNodeClip)" preserveAspectRatio="xMidYMid meet" />
    </g>
    <g transform="translate(230, 28)">
      <image href="/logos/cursor.jpeg" x="-16" y="-16" width="32" height="32" clip-path="url(#clientNodeClip)" preserveAspectRatio="xMidYMid meet" />
    </g>
    <g transform="translate(248, 100)">
      <image href="/logos/copilot.jpeg" x="-16" y="-16" width="32" height="32" clip-path="url(#clientNodeClip)" preserveAspectRatio="xMidYMid meet" />
    </g>
    <g transform="translate(230, 172)">
      <image href="/logos/chatgpt.png" x="-16" y="-16" width="32" height="32" clip-path="url(#clientNodeClip)" preserveAspectRatio="xMidYMid meet" />
    </g>
    <g transform="translate(140, 178)">
      <image href="/logos/windsurf.png" x="-16" y="-16" width="32" height="32" clip-path="url(#clientNodeClip)" preserveAspectRatio="xMidYMid meet" />
    </g>
    <g transform="translate(32, 100)">
      <image href="/logos/codex.jpeg" x="-16" y="-16" width="32" height="32" clip-path="url(#clientNodeClip)" preserveAspectRatio="xMidYMid meet" />
    </g>
  </svg>
</template>

<script setup>
</script>

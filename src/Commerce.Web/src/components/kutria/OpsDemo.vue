<script setup>
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const events = ref([])
const proposal = ref(false)
const steps = ref([])
const done = ref(false)
const seconds = ref(16)
const booting = ref(true)
const log = ref(null)
let timers = []
let stopped = false

function clearTimers() {
  timers.forEach((id) => {
    clearTimeout(id)
    clearInterval(id)
  })
  timers = []
}

function wait(ms) {
  return new Promise((resolve) => {
    timers.push(setTimeout(resolve, ms))
  })
}

async function pinLog() {
  await nextTick()
  const el = log.value
  if (!el) return
  el.scrollTop = el.scrollHeight
}

async function play() {
  while (!stopped) {
    clearTimers()
    events.value = []
    proposal.value = false
    steps.value = []
    done.value = false
    seconds.value = 16
    booting.value = true
    await wait(860)
    if (stopped) return
    booting.value = false
    const tick = setInterval(() => {
      if (seconds.value > 0) seconds.value -= 1
    }, 1000)
    timers.push(tick)

    await wait(200)
    if (stopped) return
    events.value = [{
      id: 1,
      role: 'system',
      time: '09:14',
      icon: '📊',
      tone: 'neutral',
      text: 'Tasa entrega a tiempo 89% (meta 95%) - 23 pedidos Shippit Lima +48h sin movimiento'
    }]
    await wait(1800)
    if (stopped) return
    events.value = [...events.value, {
      id: 2,
      role: 'system',
      time: '09:15',
      icon: '⚠️',
      tone: 'warn',
      text: 'Devoluciones por talla +40% en Falabella - 12 SKUs afectados - tabla EU vs US'
    }]
    await wait(2000)
    if (stopped) return
    events.value = [...events.value, {
      id: 3,
      role: 'advisor',
      name: 'Kutria - Asesor',
      time: '09:16',
      text: 'Detecté el patrón. Propuesta:'
    }]
    await wait(1000)
    if (stopped) return
    proposal.value = true
    await wait(2000)
    if (stopped) return
    steps.value = ['Aprobado por jefe de operaciones - Ejecutando...']
    await wait(1000)
    if (stopped) return
    steps.value = [...steps.value, 'Actualizando 12 fichas en VTEX...']
    await wait(800)
    if (stopped) return
    steps.value = [...steps.value, 'Sincronizando Falabella...']
    await wait(1400)
    if (stopped) return
    steps.value = [...steps.value, 'Hecho. Fichas actualizadas. Notificación a CX enviada.']
    done.value = true
    await wait(4200)
  }
}

watch([events, proposal, steps, done], pinLog, { deep: true })
onMounted(play)
onBeforeUnmount(() => {
  stopped = true
  clearTimers()
})
</script>

<template>
  <div class="relative rounded-[20px] border border-white/[0.08] bg-[#121212] p-px shadow-[0_20px_80px_rgba(0,0,0,0.6)]">
    <div class="overflow-hidden rounded-[18px] bg-[#0F0F0F]">
      <div class="flex items-center justify-between border-b border-white/[0.06] bg-[#0F0F0F] px-4 py-3.5 sm:px-5">
        <div class="flex items-center gap-3">
          <div class="hidden items-center gap-1.5 sm:flex">
            <span class="h-2.5 w-2.5 rounded-full bg-white/10" />
            <span class="h-2.5 w-2.5 rounded-full bg-white/10" />
            <span class="h-2.5 w-2.5 rounded-full bg-white/10" />
          </div>
          <span class="text-[13px] font-medium tracking-tight text-white/80">Panel de operaciones - kutria.com • en vivo</span>
        </div>
        <span class="inline-flex items-center gap-1.5 rounded-full border border-emerald-400/20 bg-emerald-400/[0.08] px-2.5 py-1">
          <span class="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-400 shadow-[0_0_8px_rgba(16,185,129,0.8)]" />
          <span class="font-mono text-[10px] font-medium uppercase tracking-wide text-emerald-300">LIVE • Simulación automática</span>
        </span>
      </div>

      <div
        ref="log"
        class="h-[420px] space-y-3 overflow-y-auto overscroll-contain bg-[#0A0A0A] px-4 py-5 [overflow-anchor:none] sm:h-[460px] sm:px-5"
      >
        <div v-if="booting" class="space-y-3">
          <div class="mb-1 flex items-center gap-2">
            <div class="kutria-skel h-2 w-2 rounded-full" />
            <div class="kutria-skel h-2.5 w-56 rounded-full" />
            <div class="kutria-skel ml-auto h-4 w-10 rounded-full" />
          </div>
          <div v-for="n in 2" :key="n" class="flex gap-2.5 rounded-[12px] border border-white/[0.07] bg-white/[0.03] px-3.5 py-3">
            <div class="kutria-skel h-4 w-4 shrink-0 rounded" />
            <div class="min-w-0 flex-1 space-y-2">
              <div class="flex gap-2">
                <div class="kutria-skel h-3 w-10 rounded-full" />
                <div class="kutria-skel h-3 w-14 rounded-full" />
              </div>
              <div class="kutria-skel h-2.5 w-full rounded-full" />
              <div class="kutria-skel h-2.5 w-4/5 rounded-full" />
            </div>
          </div>
          <div class="rounded-[14px] border border-white/[0.07] bg-white/[0.03] px-4 py-4">
            <div class="flex items-center gap-2">
              <div class="kutria-skel h-7 w-7 rounded-[8px]" />
              <div class="kutria-skel h-3 w-3/4 rounded-full" />
            </div>
            <div class="mt-3 space-y-2 rounded-[10px] border border-white/[0.06] bg-[#0A0A0A] px-3.5 py-3">
              <div class="kutria-skel h-2.5 w-24 rounded-full" />
              <div class="kutria-skel h-2.5 w-full rounded-full" />
              <div class="kutria-skel h-8 w-full rounded-[8px]" />
            </div>
          </div>
        </div>
        <template v-else>
        <div class="mb-1 flex items-center gap-2 text-[11px] text-white/30">
          <span class="h-2 w-2 animate-pulse rounded-full bg-emerald-400" />
          Live feed • VTEX + Falabella + Shippit conectados
          <span class="ml-auto rounded-full border border-white/[0.06] bg-white/[0.06] px-2 py-0.5 text-[10px]">auto</span>
        </div>

        <template v-for="event in events" :key="event.id">
          <div
            v-if="event.role === 'system'"
            class="flex gap-2.5 rounded-[12px] border px-3.5 py-3"
            :class="event.tone === 'warn' ? 'border-amber-500/20 bg-amber-500/[0.06]' : 'border-white/[0.07] bg-white/[0.03]'"
          >
            <span class="mt-0.5 text-[14px] leading-none">{{ event.icon }}</span>
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <span class="font-mono text-[10px] text-white/30">{{ event.time }}</span>
                <span
                  class="rounded-full border px-1.5 py-0.5 text-[10px]"
                  :class="event.tone === 'warn' ? 'border-amber-500/20 bg-amber-500/10 text-amber-300' : 'border-white/[0.06] bg-white/[0.05] text-white/40'"
                >SISTEMA</span>
              </div>
              <p class="mt-1.5 text-[12.5px] leading-[1.5] text-white/70">{{ event.text }}</p>
            </div>
          </div>
          <div v-else class="flex gap-2.5">
            <div class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full border border-[#00F5FF]/20 bg-[#00F5FF]/15 text-[11px]">🤖</div>
            <div class="max-w-[85%] rounded-[12px] rounded-tl-[4px] border border-[#00F5FF]/15 bg-[#151A1F] px-3.5 py-2.5">
              <div class="mb-1 text-[11px] font-medium text-[#00F5FF]/70">{{ event.name }}</div>
              <p class="text-[13px] font-medium text-white">{{ event.text }}</p>
              <span class="mt-1 block text-[10px] text-white/30">{{ event.time }}</span>
            </div>
          </div>
        </template>

        <div v-if="proposal" class="overflow-hidden rounded-[14px] border border-amber-400/25 bg-amber-400/[0.06]">
          <div class="px-4 py-4">
            <div class="flex items-start justify-between gap-3">
              <div class="flex items-center gap-2">
                <div class="flex h-7 w-7 items-center justify-center rounded-[8px] border border-amber-400/20 bg-amber-400/15 text-amber-300">
                  <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v4m0 4h.01M10.3 4.3 2.6 18a2 2 0 0 0 1.7 3h15.4a2 2 0 0 0 1.7-3L13.7 4.3a2 2 0 0 0-3.4 0Z" /></svg>
                </div>
                <p class="text-[13px] font-semibold leading-tight text-white">Editar 12 fichas, corregir tabla tallas US→EU, agregar nota 'Horma pequeña'</p>
              </div>
              <span class="whitespace-nowrap rounded-full bg-amber-400 px-2 py-0.5 text-[10px] font-bold text-black">AUTO</span>
            </div>
            <div class="mt-3 space-y-2.5 rounded-[10px] border border-white/[0.06] bg-[#0A0A0A] px-3.5 py-3">
              <p class="text-[11px] text-white/40">SKUs detectados</p>
              <p class="font-mono text-[11px] text-white/70">NIKE-PEG-40-42, ADIDAS-UB-22-42, ASICS-NIM-25-42 +9 más</p>
              <div class="h-px bg-white/[0.06]" />
              <p class="text-[11px] text-white/40">Ejecución propuesta</p>
              <p class="text-[12px] leading-[1.5] text-white/70">Cambiar tabla de tallas de US a EU y agregar nota <span class="text-white">“Horma pequeña, pide 1 talla más”</span></p>
              <div class="flex items-center gap-2 rounded-[8px] border border-emerald-500/15 bg-emerald-500/10 px-3 py-2">
                <span class="text-[11px] font-medium text-emerald-300">Impacto:</span>
                <span class="text-[11px] text-white/70">-30% devoluciones +S/ 2,400/mes</span>
              </div>
            </div>
            <div class="mt-4 space-y-2">
              <p v-if="!steps.length" class="font-mono text-[11px] text-white/30">Esperando aprobación automática...</p>
              <div v-for="(step, index) in steps" :key="step" class="flex gap-2 text-[12px] leading-[1.4]">
                <span
                  class="mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center rounded-full"
                  :class="done && index === steps.length - 1 ? 'bg-emerald-400 text-black' : index === 0 ? 'bg-white text-black' : 'bg-white/[0.08] text-white/60'"
                >
                  <svg v-if="index === 0 || (done && index === steps.length - 1)" class="h-3 w-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><path stroke-linecap="round" stroke-linejoin="round" d="m5 13 4 4L19 7" /></svg>
                  <svg v-else class="h-3 w-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" d="M5 12h14" /></svg>
                </span>
                <span :class="index === 0 ? 'font-medium text-white' : done && index === steps.length - 1 ? 'font-medium text-emerald-300' : 'text-white/60'">{{ step }}</span>
              </div>
            </div>
          </div>
        </div>
        </template>
      </div>

      <div class="border-t border-white/[0.06] bg-[#111111] px-4 py-3">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2 text-[11px] text-white/40">
            <span class="h-1.5 w-1.5 rounded-full" :class="booting ? 'kutria-skel h-1.5 w-1.5' : 'animate-pulse bg-emerald-400'" />
            <span v-if="booting" class="kutria-skel h-2.5 w-44 rounded-full" />
            <span v-else class="font-mono">Simulación automática • reinicio en {{ seconds }}s</span>
          </div>
          <div class="h-1.5 w-16 overflow-hidden rounded-full bg-white/[0.06]">
            <div v-if="booting" class="kutria-skel h-full w-1/3 rounded-full" />
            <div v-else class="h-full bg-emerald-400/60 transition-all duration-1000" :style="{ width: `${Math.max(0, ((16 - seconds) / 16) * 100)}%` }" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.kutria-skel {
  background: linear-gradient(90deg, rgba(255, 255, 255, 0.05) 0%, rgba(255, 255, 255, 0.14) 50%, rgba(255, 255, 255, 0.05) 100%);
  background-size: 200% 100%;
  animation: kutria-shimmer 1.25s ease-in-out infinite;
}

@keyframes kutria-shimmer {
  0% { background-position: 100% 0; }
  100% { background-position: -100% 0; }
}
</style>

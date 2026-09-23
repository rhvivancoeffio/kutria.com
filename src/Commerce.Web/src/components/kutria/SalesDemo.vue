<script setup>
import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const products = [
  { id: 'pegasus', name: 'Pegasus 40', brand: 'Nike', price: 349, desc: 'Amortiguación para asfalto, ideal para 10K', tags: ['Talla 42', 'Stock 12', 'Entrega 24h'], letter: 'N' },
  { id: 'ultraboost', name: 'Ultraboost 22', brand: 'Adidas', price: 429, desc: 'Más retorno de energía, para entrenos largos', tags: ['Talla 42', 'Stock 5', 'Entrega 48h'], letter: 'A' },
  { id: 'nimbus', name: 'Gel Nimbus 25', brand: 'Asics', price: 389, desc: 'Máximo confort para pisada neutra', tags: ['Talla 42', 'Stock 8', 'Entrega 24h'], letter: 'AS' }
]

const messages = ref([])
const typing = ref(false)
const showProducts = ref(false)
const chosen = ref(null)
const reserved = ref(false)
const seconds = ref(13)
const draft = ref('')
const composing = ref(false)
const sending = ref(false)
const booting = ref(true)
const fading = ref(false)
const productSkeleton = ref(false)
const log = ref(null)
let timers = []
let stopped = false
let nextId = 1

function clearTimers() {
  timers.forEach((id) => {
    clearTimeout(id)
    clearInterval(id)
  })
  timers = []
}

async function pinLog() {
  await nextTick()
  const el = log.value
  if (!el) return
  const top = booting.value || fading.value ? 0 : el.scrollHeight
  el.scrollTo({ top, behavior: 'smooth' })
}

function wait(ms) {
  return new Promise((resolve) => {
    const id = setTimeout(resolve, ms)
    timers.push(id)
  })
}

async function typeAndSend(text, time) {
  composing.value = true
  draft.value = ''
  for (let i = 1; i <= text.length; i += 1) {
    if (stopped) return
    draft.value = text.slice(0, i)
    await wait(i % 7 === 0 ? 70 : 34)
  }
  if (stopped) return
  composing.value = false
  sending.value = true
  await wait(520)
  if (stopped) return
  messages.value.push({ id: nextId++, role: 'user', name: 'Cliente', text, time })
  draft.value = ''
  sending.value = false
}

async function play() {
  while (!stopped) {
    clearTimers()
    messages.value = []
    typing.value = false
    showProducts.value = false
    chosen.value = null
    reserved.value = false
    seconds.value = 13
    draft.value = ''
    composing.value = false
    sending.value = false
    fading.value = false
    productSkeleton.value = false
    booting.value = true
    await wait(720)
    if (stopped) return
    booting.value = false

    await wait(280)
    if (stopped) return
    await typeAndSend('zapatillas negras talla 42 para correr', '09:10')
    if (stopped) return

    typing.value = true
    await wait(800)
    if (stopped) return
    typing.value = false
    messages.value.push({
      id: nextId++,
      role: 'agent',
      name: 'Kutria',
      text: 'Encontré 3 opciones que te sirven para maratón, talla 42 con stock inmediato:',
      time: '09:10'
    })
    await wait(520)
    if (stopped) return
    productSkeleton.value = true
    await wait(860)
    if (stopped) return
    productSkeleton.value = false
    showProducts.value = true
    await wait(1400)
    if (stopped) return

    await typeAndSend('Me quedo con las Nike Pegasus', '09:11')
    if (stopped) return
    chosen.value = 'pegasus'
    typing.value = true
    await wait(700)
    if (stopped) return
    typing.value = false
    messages.value.push({
      id: nextId++,
      role: 'agent',
      name: 'Kutria',
      text: 'Perfecto. Ya reservé tu talla.',
      time: '09:11'
    })
    await wait(600)
    if (stopped) return
    reserved.value = true
    const tick = setInterval(() => {
      if (seconds.value > 0) seconds.value -= 1
    }, 1000)
    timers.push(tick)
    await wait(2600)
    if (stopped) return
    fading.value = true
    await wait(460)
  }
}

watch([messages, typing, showProducts, chosen, reserved, productSkeleton, booting, fading], pinLog, { deep: true })

onMounted(play)
onBeforeUnmount(() => {
  stopped = true
  clearTimers()
})
</script>

<template>
  <div class="relative rounded-[20px] border border-white/[0.08] bg-[#121212] p-px shadow-[0_20px_80px_rgba(0,0,0,0.6)]">
    <div class="flex flex-col overflow-hidden rounded-[18px] bg-[#0F0F0F]">
      <div class="flex items-center justify-between border-b border-white/[0.06] bg-[#0F0F0F] px-4 py-3.5 sm:px-5">
        <div class="flex items-center gap-3">
          <div class="hidden items-center gap-1.5 sm:flex">
            <span class="h-2.5 w-2.5 rounded-full bg-white/10" />
            <span class="h-2.5 w-2.5 rounded-full bg-white/10" />
            <span class="h-2.5 w-2.5 rounded-full bg-white/10" />
          </div>
          <span class="text-[13px] font-medium tracking-tight text-white/80">Chat de tienda - kutria.com • en vivo</span>
        </div>
        <span class="inline-flex items-center gap-1.5 rounded-full border border-emerald-400/20 bg-emerald-400/[0.08] px-2.5 py-1 text-[10px] text-emerald-300">LIVE • Simulación</span>
      </div>
      <div
        ref="log"
        class="h-[440px] overflow-y-auto overscroll-contain px-4 py-4 [overflow-anchor:none] sm:px-5"
      >
        <div class="flex flex-col gap-3 transition-opacity duration-500 ease-out" :class="fading ? 'opacity-0' : 'opacity-100'">
        <div v-if="booting" class="flex flex-col gap-3">
          <div class="kutria-skel h-9 w-3/5 self-end rounded-2xl" />
          <div class="kutria-skel h-16 w-4/5 rounded-2xl" />
          <div class="kutria-skel h-[72px] w-full rounded-xl" />
        </div>
        <TransitionGroup name="kutria-in" tag="div" class="flex flex-col gap-3">
        <div
          v-for="item in messages"
          :key="item.id"
          :class="item.role === 'user' ? 'items-end' : 'items-start'"
          class="flex flex-col"
        >
          <span class="mb-1 text-[10px] text-white/25">{{ item.name }} · {{ item.time }}</span>
          <p
            :class="item.role === 'user' ? 'rounded-tr-[4px] bg-white text-black' : 'rounded-tl-[4px] border border-white/[0.06] bg-[#1A1A1A] text-white/80'"
            class="max-w-[90%] rounded-2xl px-3.5 py-2.5 text-[13px] leading-relaxed"
          >{{ item.text }}</p>
        </div>
        </TransitionGroup>
        <Transition name="kutria-in">
          <div v-if="typing" class="flex max-w-[78%] flex-col items-start">
            <span class="mb-1 text-[10px] text-white/25">Kutria</span>
            <div class="w-full space-y-2 rounded-2xl rounded-tl-[4px] border border-white/[0.06] bg-[#1A1A1A] px-3.5 py-3">
              <div class="kutria-skel h-2 w-full rounded-full" />
              <div class="kutria-skel h-2 w-4/5 rounded-full" />
              <div class="kutria-skel h-2 w-2/3 rounded-full" />
            </div>
          </div>
        </Transition>
        <Transition name="kutria-in">
          <div v-if="productSkeleton" class="grid gap-2">
            <div v-for="n in 3" :key="n" class="flex items-start gap-3 rounded-xl border border-white/[0.06] bg-[#151515] p-3">
              <div class="kutria-skel h-9 w-9 shrink-0 rounded-lg" />
              <div class="min-w-0 flex-1 space-y-2 pt-1">
                <div class="kutria-skel h-2.5 w-2/5 rounded-full" />
                <div class="kutria-skel h-2 w-4/5 rounded-full" />
                <div class="flex gap-1.5 pt-1">
                  <div class="kutria-skel h-4 w-12 rounded-full" />
                  <div class="kutria-skel h-4 w-14 rounded-full" />
                </div>
              </div>
            </div>
          </div>
        </Transition>
        <Transition name="kutria-in">
        <div v-if="showProducts" class="grid gap-2">
          <div
            v-for="(product, index) in products"
            :key="product.id"
            :style="{ animationDelay: `${index * 90}ms` }"
            :class="chosen === product.id ? 'border-[#00F5FF]/40' : 'border-white/[0.08]'"
            class="kutria-card flex items-start justify-between gap-3 rounded-xl border bg-[#151515] p-3"
          >
            <div class="flex gap-3">
              <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-white text-[11px] font-bold text-black">{{ product.letter }}</div>
              <div>
                <p class="text-[13px] font-medium text-white">{{ product.brand }} {{ product.name }}</p>
                <p class="text-[12px] text-white/40">{{ product.desc }}</p>
                <div class="mt-2 flex flex-wrap gap-1">
                  <span v-for="tag in product.tags" :key="tag" class="rounded-full border border-white/[0.08] px-2 py-0.5 text-[10px] text-white/45">{{ tag }}</span>
                </div>
              </div>
            </div>
            <div class="text-right">
              <p class="text-[13px] font-semibold">S/ {{ product.price }}</p>
              <p class="mt-1 text-[10px]" :class="chosen === product.id ? 'text-[#00F5FF]' : 'text-white/30'">{{ chosen === product.id ? '✓ Elegida' : 'Disponible' }}</p>
            </div>
          </div>
        </div>
        </Transition>
        <Transition name="kutria-in">
          <div v-if="reserved" class="rounded-xl border border-emerald-400/20 bg-emerald-400/[0.06] px-3 py-2 text-[12px] text-emerald-200">
            ✓ Pedido #K-2847 creado · Stock reservado {{ seconds }}s · kutria.com/pay/K-2847
          </div>
        </Transition>
        </div>
      </div>
      <form class="border-t border-[#27272A] bg-[#0A0A0A] px-3 py-3 sm:px-4" @submit.prevent>
        <div class="flex items-center gap-2 rounded-full border border-[#27272A] bg-[#111111] px-3 py-1.5">
          <p class="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-[13px] leading-8" :class="draft ? 'text-right text-white' : 'text-[#A1A1AA]'">
            {{ draft || 'Escribe un mensaje…' }}<span v-if="composing" class="kutria-caret" />
          </p>
          <button
            type="button"
            class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full transition"
            :class="draft || sending ? 'bg-[#00F5FF] text-black' : 'bg-white/10 text-white/30'"
            :aria-label="sending ? 'Enviando' : 'Enviar'"
            disabled
          >
            <span v-if="sending" class="h-3.5 w-3.5 animate-spin rounded-full border-2 border-black/20 border-t-black" />
            <svg v-else class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" d="M5 12h14M13 6l6 6-6 6" />
            </svg>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.kutria-caret {
  display: inline-block;
  width: 1px;
  height: 0.9em;
  margin-left: 1px;
  background: #00f5ff;
  vertical-align: -0.1em;
  animation: kutria-blink 0.9s steps(1) infinite;
}

@keyframes kutria-blink {
  50% { opacity: 0; }
}

.kutria-skel {
  background: linear-gradient(90deg, rgba(255, 255, 255, 0.05) 0%, rgba(255, 255, 255, 0.14) 50%, rgba(255, 255, 255, 0.05) 100%);
  background-size: 200% 100%;
  animation: kutria-shimmer 1.25s ease-in-out infinite;
}

.kutria-card {
  animation: kutria-rise 0.55s ease both;
  transition: border-color 0.45s ease, background-color 0.45s ease;
}

.kutria-in-enter-active,
.kutria-in-leave-active {
  transition: opacity 0.48s ease, transform 0.48s ease;
}

.kutria-in-leave-active {
  transition-duration: 0.28s;
}

.kutria-in-enter-from,
.kutria-in-leave-to {
  opacity: 0;
  transform: translateY(10px);
}

@keyframes kutria-shimmer {
  0% { background-position: 100% 0; }
  100% { background-position: -100% 0; }
}

@keyframes kutria-rise {
  from {
    opacity: 0;
    transform: translateY(8px);
  }
  to {
    opacity: 1;
    transform: none;
  }
}
</style>

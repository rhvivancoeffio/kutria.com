<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import SalesDemo from '@/components/kutria/SalesDemo.vue'
import OpsDemo from '@/components/kutria/OpsDemo.vue'
import KutriaMark from '@/components/kutria/KutriaMark.vue'
import apiService from '@/services/api'

const year = new Date().getFullYear()
const isAuthenticated = computed(() => apiService.isAuthenticated())

const nav = [
  { id: 'ventas', label: 'Ventas' },
  { id: 'operaciones', label: 'Operaciones' },
  { id: 'agentes', label: 'Agentes' },
  { id: 'voz', label: 'Voz' },
  { id: 'mcp', label: 'MCP' },
  { id: 'precio', label: 'Precio' }
]

const agents = [
  { n: '01', title: 'Descubrimiento y Carrito', text: 'Ayuda a tu cliente a encontrar y comprar, como tu mejor vendedor.', tags: ['Intención real', 'Al carrito'] },
  { n: '02', title: 'Compra', text: 'Convierte carritos en ventas reales, con stock validado.', tags: ['Stock validado', 'Sin errores'] },
  { n: '03', title: 'Post-Venta', text: 'Resuelve seguimiento y devoluciones para que tu equipo crezca.', tags: ['Tracking auto', 'Devoluciones'] }
]

const stores = [
  { n: '01 • Web propia', title: 'Tiendas online con web propia', text: 'Vendes en VTEX, Shopify o WooCommerce y tu equipo hace todo manual. Kutria se conecta a tu tienda actual, entiende tu catálogo y convierte búsquedas en compras.' },
  { n: '02 • Marketplaces', title: 'Retail que vende en marketplaces', text: 'Vendes en Mercado Libre, Falabella y Amazon. Pedidos duplicados, stock desactualizado y reclamos. Tus agentes unifican operación y mantienen la venta andando.' },
  { n: '03 • Catálogo grande', title: 'Marcas y distribuidores saturados', text: 'Catálogo grande, equipo saturado entre pedidos, reclamos y reportes. Kutria automatiza post-venta y te dice dónde pierdes margen.' }
]

const steps = [
  { n: '01', title: 'Se conecta a lo que ya usas', text: 'Tu tienda, tus marketplaces y tu ERP. No migras nada.' },
  { n: '02', title: 'Aprende de tu negocio', text: 'Entiende tu catálogo, tus clientes y por qué compran o devuelven.' },
  { n: '03', title: 'Mejora sola, cada semana', text: 'Tus agentes atienden 24/7 y tu operación se optimiza sola. Tú solo apruebas.' }
]

// Nuevos features para los chats separados
const buyerFeatures = [
  { title: 'Entiende foto, audio, "lo mismo de antes"', desc: 'No es un chatbot con botones 1/2/3. Memoria de cliente real.' },
  { title: 'Valida stock real en tu OMS vía MCP', desc: 'Consulta tu OMS, VTEX, Shopify antes de prometer. No alucina stock.' },
  { title: 'Crea pedido + cobra con link local', desc: 'Shopify/VTEX/Woo + tu OMS + métodos de pago LATAM. 42s de chat a pedido.' }
]

const opsFeatures = [
  { title: 'Se embebe en tu Sistema Operativo', desc: 'Vive dentro de Shopify Admin, VTEX o tu OMS. No es otro dashboard.' },
  { title: 'Fulfillment y tickets automático', desc: 'Reserva stock, genera guía con tu courier, resuelve WISMO con tracking real.' },
  { title: 'No rompe operación si vendes más', desc: 'Escala 0→n agentes. Tu equipo solo en excepciones. 80% resuelto solo.' }
]

function scrollTo(id) {
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}
</script>

<template>
  <div class="kutria-landing min-h-screen bg-[#0A0A0A] text-white antialiased selection:bg-cyan-500/20 selection:text-cyan-200">
    <header class="sticky top-0 z-50 border-b border-[#27272A] bg-[#0A0A0A]/80 backdrop-blur-xl">
      <div class="mx-auto flex h-16 max-w-[1200px] items-center justify-between px-6 lg:px-8">
        <div class="flex items-center gap-8">
          <a href="#top" class="flex items-center gap-3 text-white" @click.prevent="scrollTo('top')">
            <KutriaMark />
            <span class="hidden text-[13px] font-normal text-[#A1A1AA] sm:inline">— AI Commerce OS</span>
          </a>
          <nav class="hidden items-center gap-7 md:flex">
            <button
              v-for="item in nav"
              :key="item.id"
              type="button"
              class="text-[13px] text-[#A1A1AA] transition-colors hover:text-white"
              @click="scrollTo(item.id)"
            >{{ item.label }}</button>
          </nav>
        </div>
        <div class="flex items-center gap-2 sm:gap-3">
          <template v-if="isAuthenticated">
            <RouterLink to="/admin" class="inline-flex h-8 items-center rounded-full bg-[#00F5FF] px-4 text-[13px] font-semibold text-black transition-colors hover:bg-[#7dfff8]">Ir a Panel</RouterLink>
          </template>
          <template v-else>
            <RouterLink to="/sign-in" class="inline-flex h-8 items-center px-2 text-[13px] font-medium text-[#A1A1AA] transition-colors hover:text-white">Ingresar</RouterLink>
            <RouterLink to="/sign-up" class="inline-flex h-8 items-center rounded-full bg-[#00F5FF] px-4 text-[13px] font-semibold text-black transition-colors hover:bg-[#7dfff8]">Registrar</RouterLink>
          </template>
        </div>
      </div>
    </header>

    <!-- HERO NUEVO CON TU HEADLINE GANADOR -->
    <section id="top" class="relative overflow-hidden">
      <div class="kutria-grid absolute inset-0 opacity-60" />
      <div class="pointer-events-none absolute -top-[260px] left-1/2 h-[800px] w-[1000px] -translate-x-1/2 rounded-full bg-[radial-gradient(ellipse_at_center,rgba(0,245,255,0.14),transparent_60%)] blur-3xl" />
      <div class="pointer-events-none absolute left-[8%] top-[18%] h-[420px] w-[420px] rounded-full bg-[radial-gradient(circle,rgba(139,92,246,0.10),transparent_70%)]" />
      <div class="relative mx-auto max-w-[1200px] px-6 pb-16 pt-20 lg:px-8 lg:pb-24 lg:pt-28">
        <div class="mx-auto max-w-[900px] text-center">
          
          <!-- HEADLINE QUE MATA EL MIEDO -->
          <h1 class="mt-8 text-[36px] font-[600] leading-[0.92] tracking-[-0.045em] sm:text-[54px] lg:text-[68px]">
            <span class="block font-[400] text-white/50">No necesitas otro chatbot.</span>
            <span class="block mt-2">Necesitas un canal que venda solo</span>
            <span class="block">y una operación que no se rompa.</span>
          </h1>

          <p class="mx-auto mt-7 max-w-[640px] text-[16px] font-[400] leading-[1.55] text-white/55 sm:text-[18px]">
            Agentic Commerce OS. Un canal de venta <span class="text-white">Chat de tu tienda, WhatsApp e IG</span> + agentes que operan tu fulfillment y resuelven tickets conectados a tu OMS y stock real vía <span class="text-white font-medium">MCP</span>.
          </p>

          <div class="mt-6 flex flex-wrap items-center justify-center gap-2">
            <span class="rounded-full border border-white/[0.08] bg-white/[0.04] px-3 py-1 text-[11px] text-white/60">Ecommerce OS</span>
            <span class="rounded-full border border-white/[0.08] bg-white/[0.04] px-3 py-1 text-[11px] text-white/60">4 agentes</span>
            <span class="rounded-full border border-white/[0.08] bg-white/[0.04] px-3 py-1 text-[11px] text-white/60">MCP HTTP</span>
            <span class="rounded-full border border-white/[0.08] bg-white/[0.04] px-3 py-1 text-[11px] text-white/60">BYOI</span>
            <span class="rounded-full border border-cyan-500/20 bg-cyan-500/10 px-3 py-1 text-[11px] text-cyan-200">LATAM ES/PT</span>
          </div>

          <div class="mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row">
            <button type="button" class="flex h-11 w-full items-center justify-center rounded-full bg-[#00F5FF] px-6 text-[14px] font-semibold text-black transition hover:bg-[#7dfff8] sm:w-auto" @click="scrollTo('ventas')">Ver demo en vivo</button>
            <button type="button" class="h-11 w-full rounded-full border border-[#27272A] bg-transparent px-6 text-[14px] font-medium text-white transition hover:border-[#3f3f46] sm:w-auto" @click="scrollTo('precio')">Hablar con fundador</button>
          </div>
          <p class="mt-4 font-mono text-[11px] text-white/30">Español, Portugués • Métodos de pago y envíos locales • Respuesta &lt;2h</p>
        </div>
      </div>
    </section>

    <!-- SECCION INTRO EN VIVO CON TITULO Y SEPARACION -->
    <section class="border-t border-[#27272A] bg-[#0A0A0A]">
      <div class="mx-auto max-w-[1200px] px-6 py-16 lg:px-8">
        <div class="max-w-[720px]">
          <p class="font-mono text-[11px] uppercase tracking-widest text-white/40">01 • EN VIVO</p>
          <h2 class="mt-3 text-[28px] font-semibold leading-[1.05] tracking-[-0.03em] sm:text-[36px]">Dos canales en vivo, un mismo OS</h2>
          <p class="mt-3 text-[15px] leading-relaxed text-white/50">Buyer a la izquierda vende con stock real. Operaciones a la derecha embebido en tu OMS, sin romper operación cuando vendes más.</p>
        </div>
      </div>
    </section>

    <!-- CHAT DEL BUYER - IZQUIERDA CON TITULO -->
    <section id="ventas" class="relative border-t border-[#27272A] bg-[#0A0A0A]">
      <div class="mx-auto max-w-[1200px] px-6 py-10 lg:px-8">
        <div class="mb-10 flex flex-col gap-3">
          <div class="inline-flex items-center gap-2">
            <span class="font-mono text-[11px] tracking-widest text-cyan-300/80">CANAL DE VENTA • CHAT + WHATSAPP + IG</span>
            <span class="h-1 w-1 rounded-full bg-zinc-700" />
            <span class="font-mono text-[11px] text-white/30">Ventas hoy: 23 • En vivo</span>
          </div>
          <h3 class="text-[26px] font-semibold tracking-[-0.02em] sm:text-[32px]">Chat del Buyer — Vende</h3>
          <p class="max-w-[520px] text-[14px] leading-relaxed text-white/50">Un canal que cierra ventas solo, con tu stock real validado vía MCP. Mismo agente en tu web, WhatsApp e IG. Conversación → Pedido en 42s.</p>
        </div>
      </div>

      <div class="mx-auto grid max-w-[1200px] items-start gap-12 px-6 pb-20 lg:grid-cols-[1.15fr_0.85fr] lg:px-8">
        <div>
          <SalesDemo />
          <div class="mt-3 flex flex-wrap gap-2">
            <span class="rounded-full border border-white/10 px-3 py-1 text-[11px] text-white/50">Foto, audio, "lo mismo de antes"</span>
            <span class="rounded-full border border-cyan-500/20 bg-cyan-500/10 px-3 py-1 text-[11px] text-cyan-200">Stock real MCP</span>
            <span class="rounded-full border border-white/10 px-3 py-1 text-[11px] text-white/50">42s a pedido</span>
          </div>
        </div>

        <div class="lg:sticky lg:top-24">
          <p class="font-mono text-[11px] uppercase tracking-widest text-white/40">Demo • Ventas • No chatbot</p>
          <h2 class="mt-4 max-w-md text-[30px] font-semibold leading-[1.05] tracking-[-0.03em] sm:text-[36px]">Tus clientes encuentran y compran solos</h2>
          <p class="mt-4 max-w-md text-[15px] leading-relaxed text-white/50">Sin que tu equipo intervenga. El agente entiende lenguaje natural, confirma stock real en tu OMS y manda el link de pago local.</p>

          <div class="mt-8 space-y-3">
            <div v-for="f in buyerFeatures" :key="f.title" class="rounded-2xl border border-white/[0.06] bg-white/[0.03] p-4">
              <p class="text-[13px] font-medium text-white">{{ f.title }}</p>
              <p class="mt-1 text-[12px] leading-relaxed text-white/50">{{ f.desc }}</p>
            </div>
          </div>

          <div class="mt-8 grid gap-3 sm:grid-cols-2">
            <div class="rounded-2xl border border-white/[0.06] bg-white/[0.03] p-4">
              <p class="text-[11px] uppercase tracking-widest text-white/35">Problema</p>
              <p class="mt-2 text-[14px] text-white/70">Búsquedas que no convierten y carritos abandonados.</p>
            </div>
            <div class="rounded-2xl border border-[#00F5FF]/20 bg-[#00F5FF]/[0.05] p-4">
              <p class="text-[11px] uppercase tracking-widest text-[#00F5FF]">Beneficio</p>
              <p class="mt-2 text-[14px]">+34% conversión. Mejora continua.</p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- CHAT DE OPERACIONES - DERECHA CON TITULO Y SEPARACION GRANDE -->
    <section id="operaciones" class="relative border-t border-[#27272A] bg-[#0C0C0C]">
      <div class="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top,rgba(139,92,246,0.08),transparent_60%)]" />

      <div class="relative mx-auto max-w-[1200px] px-6 pt-16 lg:px-8">
        <div class="mb-10 flex flex-col gap-3">
          <div class="inline-flex items-center gap-2">
            <span class="font-mono text-[11px] tracking-widest text-[#8B5CF6]/80">OPERACIONES + SOPORTE • EMBEBIDO EN TU OMS</span>
            <span class="h-1 w-1 rounded-full bg-zinc-700" />
            <span class="font-mono text-[11px] text-white/30">Tickets hoy: 41 • En vivo</span>
          </div>
          <h3 class="text-[26px] font-semibold tracking-[-0.02em] sm:text-[32px]">Chat de Operaciones — Fulfillment & Soporte</h3>
          <p class="max-w-[520px] text-[14px] leading-relaxed text-white/50">Vive dentro de tu OMS / Shopify Admin. Opera fulfillment, genera guías y resuelve reclamos conectado a tu servicio de envíos. No rompe operación si vendes más.</p>
        </div>
      </div>

      <div class="relative mx-auto max-w-[1200px] px-6 pb-20 lg:px-8 lg:pb-28">
        <div class="grid items-start gap-12 lg:grid-cols-[0.9fr_1.1fr] lg:gap-16">
          <div class="order-2 lg:order-1">
            <OpsDemo />
            <div class="mt-3 flex flex-wrap gap-2">
              <span class="rounded-full border border-white/[0.06] bg-white/[0.05] px-3 py-1 text-[11px] text-white/50">Embebido en tu OMS</span>
              <span class="rounded-full border border-[#8B5CF6]/20 bg-[#8B5CF6]/10 px-3 py-1 text-[11px] text-[#8B5CF6]/70">Fulfillment auto</span>
              <span class="rounded-full border border-white/[0.06] bg-white/[0.05] px-3 py-1 text-[11px] text-white/50">80% sin humano</span>
            </div>
          </div>

          <div class="order-1 lg:sticky lg:top-24 lg:order-2">
            <div class="inline-flex items-center gap-2 rounded-full border border-[#8B5CF6]/20 bg-[#8B5CF6]/[0.06] px-3 py-1">
              <span class="h-1.5 w-1.5 animate-pulse rounded-full bg-[#8B5CF6]" />
              <span class="font-mono text-[11px] uppercase tracking-widest text-[#8B5CF6]/80">Demo • Operaciones • Embebido</span>
            </div>
            <h2 class="mt-5 text-[30px] font-semibold leading-[1.05] tracking-[-0.03em] sm:text-[36px]">Tu operación mejora sola cada semana</h2>

            <div class="mt-8 space-y-3">
              <div v-for="f in opsFeatures" :key="f.title" class="rounded-[14px] border border-white/[0.06] bg-white/[0.03] p-4">
                <p class="text-[13px] font-medium text-white">{{ f.title }}</p>
                <p class="mt-1 text-[12px] leading-relaxed text-white/50">{{ f.desc }}</p>
              </div>
            </div>

            <div class="mt-8 space-y-4">
              <div class="rounded-[14px] border border-red-500/20 bg-red-500/[0.06] p-4">
                <div class="flex items-center gap-2">
                  <span class="flex h-6 w-6 items-center justify-center rounded-full border border-red-500/20 bg-red-500/15 text-[12px] text-red-300">✕</span>
                  <span class="font-mono text-[11px] font-bold uppercase tracking-widest text-red-300">Sin Kutria</span>
                </div>
                <p class="mt-3 text-[13.5px] leading-[1.6] text-white/70">Suben devoluciones, se demoran entregas, se pausan SKUs y te enteras 1 semana después. <span class="font-medium text-white">Pierdes margen sin saber por qué.</span></p>
              </div>
              <div class="rounded-[14px] border border-[#8B5CF6]/20 bg-[#8B5CF6]/[0.08] p-4">
                <div class="flex items-center gap-2">
                  <span class="flex h-6 w-6 items-center justify-center rounded-full border border-[#8B5CF6]/20 bg-[#8B5CF6]/15 text-[#8B5CF6]">
                    <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M13 2 4 14h7l-1 8 9-12h-7l1-8Z" /></svg>
                  </span>
                  <span class="font-mono text-[11px] font-bold uppercase tracking-widest text-[#8B5CF6]/80">Con Kutria OS</span>
                </div>
                <p class="mt-3 text-[13.5px] leading-[1.6] text-white/70">El Asesor detecta el problema, analiza causa y propone solución para que apruebes en 1 click. <span class="font-medium text-white">Tu margen mejora solo.</span></p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Mantengo todo lo demás igual - conceptos base -->
    <section id="agentes" class="border-t border-white/[0.06]">
      <div class="mx-auto max-w-[1200px] px-6 py-20 lg:px-8">
        <div class="flex flex-col justify-between gap-6 md:flex-row md:items-end">
          <div>
            <span class="inline-flex rounded-full border border-white/10 bg-white/[0.03] px-3 py-1 font-mono text-[11px] uppercase tracking-widest text-white/40">Sistema</span>
            <h2 class="mt-5 max-w-[520px] text-[30px] font-semibold leading-[1.05] tracking-[-0.03em] sm:text-[42px]">4 agentes que trabajan para tu retail</h2>
          </div>
          <p class="max-w-[380px] text-[14px] leading-[1.6] text-white/50">No son chatbots. Cada agente tiene acceso real a tu tienda y operación para vender y resolver.</p>
        </div>
        <div class="mt-12 grid grid-cols-1 gap-px overflow-hidden rounded-[20px] border border-white/[0.06] bg-white/[0.07] p-px md:grid-cols-3">
          <article v-for="agent in agents" :key="agent.n" class="flex flex-col bg-[#101010] p-7 transition-colors hover:bg-[#121212] lg:p-8">
            <div class="flex items-start justify-between">
              <div class="flex h-9 w-9 items-center justify-center rounded-[10px] border border-white/[0.06] bg-white/[0.04] text-[11px] text-white/50">K</div>
              <span class="font-mono text-[11px] tracking-widest text-white/20">{{ agent.n }}</span>
            </div>
            <h3 class="mt-6 text-[16px] font-semibold tracking-tight">{{ agent.title }}</h3>
            <p class="mt-3 text-[13px] leading-[1.6] text-white/50">{{ agent.text }}</p>
            <div class="mt-auto flex flex-wrap gap-1.5 pt-6">
              <span v-for="tag in agent.tags" :key="tag" class="rounded-full border border-white/[0.08] bg-white/[0.03] px-2.5 py-1 text-[10px] text-white/50">{{ tag }}</span>
            </div>
          </article>
        </div>
      </div>
    </section>

    <section id="mcp" class="border-t border-zinc-800 bg-[#0C0C0C]">
      <div class="mx-auto max-w-6xl px-6 py-20 lg:px-8">
        <div class="flex flex-col justify-between gap-6 md:flex-row md:items-end">
          <div>
            <span class="inline-flex rounded-full border border-zinc-800 bg-white/5 px-3 py-1 font-mono text-xs uppercase tracking-widest text-cyan-400">ESTANDAR • MCP</span>
            <h2 class="mt-5 max-w-xl text-3xl font-semibold leading-tight tracking-tight sm:text-4xl">Tu tienda habla MCP. Tus agentes ya entienden tu negocio.</h2>
          </div>
          <p class="max-w-sm text-sm leading-relaxed text-white/50">No es un plugin cerrado. Es el estandar abierto de Anthropic. Tus sistemas se exponen como herramientas y tus 4 agentes las usan con contexto real.</p>
        </div>
        <div class="mt-10 grid gap-4 md:grid-cols-3">
          <article class="rounded-2xl border border-zinc-800 p-6"><p class="font-mono text-xs text-cyan-400">01 • CONEXION</p><h3 class="mt-3 font-semibold">Conecta lo que ya usas como MCP Server</h3><p class="mt-2 text-sm leading-relaxed text-white/50">Shopify, VTEX, Woo, tu ERP y WMS como tools. El agente lee stock real.</p></article>
          <article class="rounded-2xl border border-zinc-800 p-6"><p class="font-mono text-xs text-cyan-400">02 • CONTROL</p><h3 class="mt-3 font-semibold">Sin lock-in. Tú eres dueño</h3><p class="mt-2 text-sm leading-relaxed text-white/50">MCP es open. Si usas Claude, Cursor o tu LLM, tus conectores siguen.</p></article>
          <article class="rounded-2xl border border-zinc-800 bg-white/[0.02] p-6"><p class="font-mono text-xs text-cyan-400">03 • ESCALA</p><h3 class="mt-3 font-semibold">De 4 tools a 40 sin reescribir</h3><p class="mt-2 text-sm leading-relaxed text-white/50">Agrega pasarela, OMS, BI como MCP Server. Empieza en nuestra infra, luego BYOI.</p></article>
        </div>
      </div>
    </section>

    <section id="voz" class="border-t border-white/[0.06]">
      <div class="mx-auto grid max-w-[1200px] items-center gap-10 px-6 py-20 lg:grid-cols-2 lg:px-8">
        <div>
          <p class="font-mono text-[11px] uppercase tracking-widest text-white/40">Secundario • Incluido</p>
          <h2 class="mt-4 text-[30px] font-semibold tracking-[-0.03em] sm:text-[40px]">Tus clientes compran como prefieran</h2>
          <p class="mt-4 text-[15px] leading-relaxed text-white/50">Escriben o hablan. Tu widget entiende ambos. Mismo agente • no es bot separado.</p>
        </div>
        <div class="rounded-[20px] border border-white/[0.08] bg-[#0F0F0F] p-6">
          <p class="text-[12px] text-white/40">kutria • chat · escuchando</p>
          <p class="mt-4 text-[18px] text-white/80">“zapatillas negras talla 42...”</p>
          <p class="mt-4 text-[13px] text-[#00F5FF]">→ Entendido como texto • 3 resultados • agregado al carrito</p>
        </div>
      </div>
    </section>

    <section id="tiendas" class="border-t border-white/[0.06]">
      <div class="mx-auto max-w-[1200px] px-6 py-20 lg:px-8">
        <p class="font-mono text-[11px] uppercase tracking-widest text-white/40">Para quién es</p>
        <h2 class="mt-4 max-w-xl text-[30px] font-semibold tracking-[-0.03em] sm:text-[42px]">Hecho para tiendas que venden en serio</h2>
        <div class="mt-10 grid gap-4 md:grid-cols-3">
          <article v-for="store in stores" :key="store.n" class="rounded-2xl border border-white/[0.06] bg-[#101010] p-6">
            <p class="font-mono text-[11px] text-white/30">{{ store.n }}</p>
            <h3 class="mt-3 text-[16px] font-semibold">{{ store.title }}</h3>
            <p class="mt-2 text-[13px] leading-relaxed text-white/50">{{ store.text }}</p>
          </article>
        </div>
      </div>
    </section>

    <section id="funciona" class="border-t border-white/[0.06]">
      <div class="mx-auto max-w-[1200px] px-6 py-20 lg:px-8">
        <p class="font-mono text-[11px] uppercase tracking-widest text-white/40">Implementación</p>
        <h2 class="mt-4 text-[30px] font-semibold tracking-[-0.03em] sm:text-[42px]">Sin migraciones.</h2>
        <div class="mt-10 grid gap-4 md:grid-cols-3">
          <article v-for="step in steps" :key="step.n" class="rounded-2xl border border-white/[0.06] p-6">
            <p class="font-mono text-[12px] text-[#00F5FF]">{{ step.n }}</p>
            <h3 class="mt-3 font-semibold">{{ step.title }}</h3>
            <p class="mt-2 text-[13px] leading-relaxed text-white/50">{{ step.text }}</p>
          </article>
        </div>
      </div>
    </section>

    <section id="precio" class="border-t border-zinc-800">
      <div class="mx-auto max-w-6xl px-6 py-20 lg:px-8">
        <p class="font-mono text-xs uppercase tracking-widest text-white/40">CONTACTO • DEMO TECNICA</p>
        <h2 class="mt-4 max-w-xl text-3xl font-semibold tracking-tight sm:text-4xl">Te montamos tu AI Commerce OS en tu dominio.</h2>
        <p class="mt-4 max-w-2xl text-sm leading-relaxed text-white/50">Configuramos tus 4 agentes, conectamos tu catálogo y tu pasarela. Infra incluida, multi-tenant listo, con tu marca. Agenda 20 min y lo ves con tu catálogo real.</p>
        <div class="mt-8 flex gap-3"><a href="https://wa.me/51992363245" target="_blank" class="inline-flex h-11 items-center justify-center rounded-full border border-zinc-800 bg-transparent px-6 text-sm font-medium text-white">Escribir por WhatsApp</a></div>
      </div>
    </section>

    <footer class="border-t border-[#27272A]">
      <div class="mx-auto flex max-w-[1200px] flex-col gap-4 px-6 py-10 sm:flex-row sm:items-center sm:justify-between lg:px-8">
        <div class="flex items-center gap-3 text-white"><KutriaMark /><p class="text-[12px] text-[#A1A1AA]">AI Commerce OS · Lima, Perú</p></div>
        <p class="text-[12px] text-white/30">© {{ year }} Kutria</p>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.kutria-grid {
  background-image: linear-gradient(to right, rgba(255,255,255,0.04) 1px, transparent 1px), linear-gradient(to bottom, rgba(255,255,255,0.04) 1px, transparent 1px);
  background-size: 4rem 4rem;
}
</style>

<template>
  <div class="h-screen bg-gray-50 dark:bg-ink-950 flex flex-col overflow-hidden">
    <!-- PWA Install Banner: visible cuando no está instalada -->
    <div
      v-if="!pwaIsInstalled && pwaInstallBannerVisible"
      class="flex items-center justify-between gap-4 px-4 py-2 text-sm shrink-0 bg-primary-50 dark:bg-primary-900/20 border-b border-primary-200 dark:border-primary-800"
    >
      <div class="flex items-center gap-2">
        <svg class="w-5 h-5 text-primary-600 dark:text-primary-400 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
        </svg>
        <span class="text-gray-700 dark:text-gray-300">
          <template v-if="pwaShouldShowPrompt">
            Instala la app para usarla sin el navegador.
          </template>
          <template v-else>
            Para instalar: menú del navegador (⋮) → <strong>Instalar aplicación</strong>.
          </template>
        </span>
      </div>
      <div class="flex items-center gap-2 shrink-0">
        <button
          v-if="pwaShouldShowPrompt"
          type="button"
          @click="pwaPromptInstall"
          class="px-3 py-1.5 rounded-lg font-semibold text-sm bg-primary-600 hover:bg-primary-700 text-white"
        >
          Instalar
        </button>
        <button
          type="button"
          @click="dismissPwaInstallBanner"
          class="p-1.5 rounded text-gray-500 hover:bg-gray-200 dark:hover:bg-gray-700"
          aria-label="Cerrar"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </div>
    <!-- Trial Banner -->
    <div
      v-if="trialBannerVisible"
      class="flex flex-wrap items-center justify-center gap-2 sm:gap-3 px-4 py-2 text-sm shrink-0"
      :class="trialBannerClass"
    >
      <span>{{ trialBannerMessage }}</span>
      <span v-if="!billingInfo?.isTrialExpired && isAccountOwner" class="hidden sm:inline text-gray-600 dark:text-gray-400">
        ¿Quieres activar tu plan ya? Empieza a pagar cuando quieras.
      </span>
      <router-link
        v-if="isAccountOwner"
        :to="toPath('billing')"
        :class="[
          'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg font-semibold text-sm transition-colors',
          billingInfo?.isTrialExpired
            ? 'bg-amber-600 hover:bg-amber-700 text-white'
            : 'bg-primary-600 hover:bg-primary-700 text-white'
        ]"
      >
        {{ billingInfo?.isTrialExpired ? 'Activar plan' : 'Activar plan ahora' }}
      </router-link>
    </div>
    <!-- Admin Header -->
    <header class="sticky top-0 z-50 flex items-center justify-between h-14 min-h-[3.5rem] px-4 bg-white dark:bg-ink-800 border-b border-gray-200 dark:border-ink-600 shrink-0 shadow-sm">
      <div class="flex items-center gap-3">
        <button
          @click="sidebarOpen = !sidebarOpen"
          class="p-2 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"
          aria-label="Toggle menú"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </button>
        <router-link :to="toPath('')" class="flex items-center text-gray-900 dark:text-white">
          <KutriaMark />
        </router-link>
        <button
          v-if="!showPartnerPortal"
          type="button"
          class="flex items-center gap-1.5 min-w-0 max-w-[min(42vw,10rem)] sm:max-w-[12rem] lg:max-w-[15rem] border-l border-gray-200 dark:border-gray-600 pl-3 ml-1 py-1 pr-2 rounded-r-lg text-left transition-colors hover:bg-gray-100 dark:hover:bg-gray-700/80"
          :title="activeWorkspaceTooltip"
          aria-label="Cambiar workspace"
          @click="openWorkspaceModal"
        >
          <div class="flex flex-col min-w-0 flex-1">
            <span class="text-[10px] font-semibold uppercase tracking-wide text-gray-400 dark:text-gray-500 leading-none mb-0.5">Workspace</span>
            <span class="text-sm font-medium text-gray-900 dark:text-white truncate">{{ activeWorkspaceHeaderLabel }}</span>
          </div>
          <svg
            class="w-4 h-4 shrink-0 text-gray-500 dark:text-gray-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4" />
          </svg>
        </button>
      </div>
      <div class="flex items-center gap-2">
        <router-link
          to="/docs"
          target="_blank"
          rel="noopener noreferrer"
          class="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
          title="Documentación"
        >
          <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
          </svg>
          <span class="hidden sm:inline">Documentación</span>
        </router-link>
        <button
          v-if="onboardingTourEligible"
          type="button"
          @click="tourModalOpen = true"
          :class="[
            'relative flex items-center gap-2 px-3 py-1.5 text-sm font-medium rounded-lg transition-all duration-200',
            pendingCount > 0
              ? 'text-amber-600 dark:text-amber-400 hover:text-amber-700 dark:hover:text-amber-300 hover:bg-amber-50 dark:hover:bg-amber-900/20 ring-2 ring-amber-400/30 ring-offset-2 ring-offset-white dark:ring-offset-gray-800'
              : 'text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 hover:bg-gray-100 dark:hover:bg-gray-700'
          ]"
          title="Ver qué te falta configurar"
        >
          <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
          </svg>
          <span class="hidden sm:inline">{{ pendingCount > 0 ? (totalSteps ? `Te faltan ${pendingCount}/${totalSteps} pasos para configurar la plataforma` : `Faltan ${pendingCount}`) : 'Guía' }}</span>
          <span
            v-if="pendingCount > 0"
            class="absolute -top-1 -right-1 flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-amber-500 px-1.5 text-[10px] font-bold text-white"
          >
            {{ pendingCount }}
          </span>
        </button>
        <div class="relative">
          <button
            @click="userMenuOpen = !userMenuOpen"
            class="p-2 rounded-full text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700"
            aria-label="Commerceiones de usuario"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
          </button>
          <Transition
            enter-active-class="transition duration-150 ease-out"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-100 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div
              v-show="userMenuOpen"
              ref="userMenuRef"
              class="absolute right-0 mt-2 w-56 overflow-hidden rounded-lg border border-gray-200 bg-white py-1 shadow-lg dark:border-ink-600 dark:bg-ink-800"
            >
              <router-link
                :to="toPath('profile')"
                @click="userMenuOpen = false"
                class="flex items-center gap-2 px-4 py-2.5 text-sm text-gray-700 no-underline hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-ink-700"
              >
                <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
                Mi Perfil
              </router-link>
              <router-link
                :to="toPath('change-password')"
                @click="userMenuOpen = false"
                class="flex items-center gap-2 px-4 py-2.5 text-sm text-gray-700 no-underline hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-ink-700"
              >
                <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
                </svg>
                Cambiar Contraseña
              </router-link>
              <template v-if="pwaShouldShowPrompt">
                <div class="my-1 border-t border-gray-200 dark:border-ink-600" />
                <button
                  type="button"
                  @click="pwaPromptInstall(); userMenuOpen = false"
                  class="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-primary-600 hover:bg-gray-100 dark:text-primary-400 dark:hover:bg-ink-700"
                >
                  <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                  </svg>
                  Instalar app
                </button>
                <button
                  type="button"
                  @click="pwaDismissInstall(); userMenuOpen = false"
                  class="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-gray-500 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-ink-700"
                >
                  No, gracias
                </button>
              </template>
              <template v-if="push.supported">
                <div class="my-1 border-t border-gray-200 dark:border-ink-600" />
                <button
                  v-if="!push.isSubscribed && !push.loading"
                  type="button"
                  @click="handleEnablePush"
                  class="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-ink-700"
                >
                  <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
                  </svg>
                  Activar notificaciones push
                </button>
                <button
                  v-else-if="push.isSubscribed"
                  type="button"
                  @click="push.unsubscribe()"
                  :disabled="push.loading"
                  class="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-gray-700 hover:bg-gray-100 disabled:opacity-50 dark:text-gray-200 dark:hover:bg-ink-700"
                >
                  <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5.636 18.364a9 9 0 010-12.728m12.728 0a9 9 0 010 12.728m-9.9-2.829a5 5 0 010-7.07m7.072 0a5 5 0 010 7.07M13 12a1 1 0 11-2 0 1 1 0 012 0z" />
                  </svg>
                  Desactivar notificaciones
                </button>
              </template>
              <div class="my-1 border-t border-gray-200 dark:border-ink-600" />
              <button
                type="button"
                @click="handleSignOut"
                class="flex w-full items-center gap-2 px-4 py-2.5 text-left text-sm text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-ink-700"
              >
                <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                </svg>
                Cerrar Sesión
              </button>
            </div>
          </Transition>
        </div>
      </div>
    </header>

    <!-- Content row: sidebar + main -->
    <div class="flex flex-1 min-w-0 min-h-0 overflow-hidden">
      <!-- Sidebar: desktop = fijo en el flujo (no hace scroll con el contenido). Mobile = overlay -->
      <aside
        :class="[
          'fixed z-40 flex flex-col shrink-0 bg-white dark:bg-ink-800 border-r border-gray-200 dark:border-ink-600 transition-all duration-200 ease-in-out',
          // Mobile: below banners + header
          'left-0 bottom-0 lg:top-0',
          // Desktop: dentro del flex, no hace scroll
          'lg:relative lg:left-0 lg:bottom-0',
          'lg:translate-x-0',
          sidebarOpen ? 'translate-x-0 w-64' : '-translate-x-full lg:translate-x-0 lg:w-16'
        ]"
        :style="isMobile ? { top: `${sidebarTopOffset}px` } : {}"
      >
        <div class="flex flex-col h-full overflow-hidden">
          <!-- Nav -->
          <nav class="flex-1 overflow-y-auto py-4 px-2 lg:px-2" @click="closeSidebarOnNavClick">
            <ul class="space-y-1">
              <template v-for="(entry, navIdx) in sidebarNavEntries" :key="'nav-' + navIdx">
                <li v-if="entry.kind === 'link'">
                  <router-link
                    :to="sidebarLinkTo(entry.legacyPath)"
                    :class="navLinkClass(entry.legacyPath, { excludePaths: entry.excludePaths })"
                    :title="entry.title"
                  >
                    <AdminNavIcon :name="entry.icon" />
                    <span :class="['whitespace-nowrap overflow-hidden transition-all duration-200', sidebarOpen ? 'opacity-100 w-auto ml-0' : 'opacity-0 w-0 overflow-hidden']">{{ entry.label }}</span>
                  </router-link>
                </li>
                <template v-else-if="entry.kind === 'group'">
                  <li
                    v-if="sidebarOpen"
                    :class="entry.sectionFirst ? 'px-2 pt-2 pb-0' : 'px-2 pt-4 pb-0'"
                  >
                    <button
                      type="button"
                      class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs font-semibold uppercase tracking-wider text-gray-500 transition-colors hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-700/80"
                      :aria-expanded="isNavGroupExpanded(entry)"
                      :aria-controls="'nav-group-children-' + navIdx"
                      @click.stop="toggleNavGroup(entry)"
                    >
                      <svg
                        class="h-4 w-4 shrink-0 text-gray-400 transition-transform dark:text-gray-500"
                        :class="{ '-rotate-90': !isNavGroupExpanded(entry) }"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                      >
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                      </svg>
                      <span class="min-w-0 flex-1 truncate">{{ entry.label }}</span>
                    </button>
                  </li>
                  <li class="list-none">
                    <ul
                      :id="'nav-group-children-' + navIdx"
                      class="m-0 list-none space-y-1 p-0"
                      :class="sidebarOpen ? 'border-l border-gray-200 dark:border-gray-600 ml-3 pl-2' : ''"
                      v-show="isNavGroupExpanded(entry)"
                    >
                      <li v-for="(child, ci) in entry.children" :key="'nav-group-' + navIdx + '-' + ci">
                        <router-link
                          :to="sidebarLinkTo(child.legacyPath)"
                          :class="navLinkClass(child.legacyPath, { excludePaths: child.excludePaths })"
                          :title="child.title"
                        >
                          <AdminNavIcon :name="child.icon" />
                          <span
                            :class="[
                              'whitespace-nowrap overflow-hidden transition-all duration-200',
                              sidebarOpen ? 'opacity-100 w-auto ml-0' : 'opacity-0 w-0 overflow-hidden'
                            ]"
                          >{{ child.label }}</span>
                        </router-link>
                      </li>
                    </ul>
                  </li>
                </template>
              </template>
            </ul>
          </nav>

          <!-- Footer: icon-only when collapsed -->
          <div :class="['border-t border-gray-200 dark:border-gray-700 shrink-0 flex items-center gap-2 transition-all', sidebarOpen ? 'p-3' : 'p-2 justify-center lg:flex-col']">
            <ThemeToggle />
            <JsonToggle />
            <router-link
              to="/"
              :class="['text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400', sidebarOpen ? 'flex-1 text-sm' : 'p-2 lg:p-2']"
              title="Inicio"
            >
              <span v-if="sidebarOpen">← Inicio</span>
              <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
            </router-link>
          </div>
        </div>
      </aside>

      <!-- Overlay (mobile only, when sidebar open) -->
      <div
        v-if="sidebarOpen"
        class="fixed inset-0 z-30 bg-black/50 lg:hidden"
        @click="sidebarOpen = false"
        aria-hidden="true"
      />

      <!-- Main content -->
      <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
        <main class="flex-1 overflow-auto">
          <router-view />
        </main>
      </div>
    </div>

    <!-- Tour de onboarding (componente reutilizable) -->
    <IntegrationConnectEditSlideOvers />
    <MandatoryStoreConnectionModal
      v-if="mandatoryStoreModalEligible"
      v-model="mandatoryStoreModalOpen"
      @select="onMandatoryStoreIntegrationSelected"
      @created="onMandatoryStoreCreated"
    />
    <OnboardingTourModal v-if="onboardingTourEligible" v-model="tourModalOpen" />
    <Teleport v-if="!showPartnerPortal" to="body">
      <Transition
        enter-active-class="transition-opacity duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition-opacity duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="workspaceModalOpen"
          class="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-0 sm:items-center sm:p-4 sm:backdrop-blur-[2px]"
          role="dialog"
          aria-modal="true"
          aria-labelledby="workspace-modal-title"
          aria-describedby="workspace-modal-desc"
          @click.self="closeWorkspaceModalIfAllowed"
        >
          <div
            class="flex max-h-[min(88vh,36rem)] w-full max-w-lg flex-col rounded-t-2xl border border-gray-200 bg-white shadow-2xl dark:border-gray-700 dark:bg-gray-800 sm:max-h-[min(82vh,30rem)] sm:rounded-xl"
            @click.stop
          >
            <div
              class="flex shrink-0 items-start gap-3 border-b border-gray-100 px-5 pb-4 pt-5 dark:border-gray-700/80 sm:px-6 sm:pb-4 sm:pt-6"
            >
              <div
                class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary-100 text-primary-600 dark:bg-primary-900/45 dark:text-primary-300"
                aria-hidden="true"
              >
                <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="1.75"
                    d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"
                  />
                </svg>
              </div>
              <div class="min-w-0 flex-1 pt-0.5">
                <h3 id="workspace-modal-title" class="text-lg font-semibold text-gray-900 dark:text-white">
                  Seleccionar workspace
                </h3>
                <p id="workspace-modal-desc" class="mt-1 text-sm leading-relaxed text-gray-500 dark:text-gray-400">
                  El panel muestra catálogo, integraciones y reglas según el workspace activo.
                </p>
              </div>
              <button
                v-if="workspaceModalDismissible"
                type="button"
                class="shrink-0 rounded-lg p-2 text-gray-400 transition-colors hover:bg-gray-100 hover:text-gray-700 dark:hover:bg-gray-700 dark:hover:text-gray-200"
                aria-label="Cerrar"
                @click="closeWorkspaceModalIfAllowed"
              >
                <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div class="min-h-0 flex-1 overflow-y-auto px-5 py-4 sm:px-6 sm:py-5">
              <template v-if="workspaces.length === 0">
                <p class="text-sm leading-relaxed text-gray-600 dark:text-gray-400">
                  No hay workspaces en esta cuenta o no pudimos cargarlos. Si eres propietario, créalos o revísalos en
                  Workspaces.
                </p>
                <router-link
                  :to="toPath('workspaces')"
                  class="mt-4 inline-flex items-center gap-2 rounded-lg bg-primary-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-primary-700"
                  @click="closeWorkspaceModalIfAllowed"
                >
                  Ir a Workspaces
                  <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                  </svg>
                </router-link>
              </template>
              <ul v-else class="space-y-2" role="listbox" aria-labelledby="workspace-modal-title">
                <li v-for="ws in workspaces" :key="ws.id">
                  <button
                    type="button"
                    role="option"
                    class="flex w-full items-center gap-3 rounded-xl border px-4 py-3 text-left transition-colors"
                    :class="
                      String(ws.id) === activeWorkspaceId
                        ? 'border-primary-400 bg-primary-50 ring-1 ring-primary-400/30 dark:border-primary-500 dark:bg-primary-900/35 dark:ring-primary-500/25'
                        : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50 dark:border-gray-600 dark:hover:border-gray-500 dark:hover:bg-gray-700/50'
                    "
                    :aria-selected="String(ws.id) === activeWorkspaceId"
                    @click="selectWorkspace(ws.id)"
                  >
                    <span
                      class="min-w-0 flex-1 truncate font-medium"
                      :class="
                        String(ws.id) === activeWorkspaceId
                          ? 'text-primary-800 dark:text-primary-200'
                          : 'text-gray-900 dark:text-gray-100'
                      "
                    >
                      {{ ws.name }}
                    </span>
                    <span
                      v-if="String(ws.id) === activeWorkspaceId"
                      class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary-600 text-white dark:bg-primary-500"
                      aria-hidden="true"
                    >
                      <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                      </svg>
                    </span>
                    <span
                      v-else
                      class="flex h-8 w-8 shrink-0 items-center justify-center text-gray-300 dark:text-gray-600"
                      aria-hidden="true"
                    >
                      <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                      </svg>
                    </span>
                  </button>
                </li>
              </ul>
            </div>

            <div
              v-if="workspaceModalDismissible"
              class="flex shrink-0 justify-end gap-2 border-t border-gray-100 px-5 py-3 dark:border-gray-700/80 sm:px-6"
            >
              <button
                type="button"
                class="rounded-lg px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-gray-700"
                @click="closeWorkspaceModalIfAllowed"
              >
                Cancelar
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
    <!-- PWA: actualización y offline (solo en /admin) -->
    <PwaUpdateBanner />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch, provide, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import ThemeToggle from '../components/ThemeToggle.vue'
import KutriaMark from '../components/kutria/KutriaMark.vue'
import JsonToggle from '../components/JsonToggle.vue'
import IntegrationConnectEditSlideOvers from '../components/integrations/IntegrationConnectEditSlideOvers.vue'
import MandatoryStoreConnectionModal from '../components/integrations/MandatoryStoreConnectionModal.vue'
import OnboardingTourModal from '../components/onboarding/OnboardingTourModal.vue'
import PwaUpdateBanner from '../components/PwaUpdateBanner.vue'
import apiService from '../services/api'
import { eventBus, TOUR_PROGRESS_UPDATED, OPEN_TOUR, WORKSPACE_CONTEXT_CHANGED, MY_INTEGRATIONS_UPDATED } from '../utils/eventBus'
import { openIntegrationConnectModal } from '../utils/integrationConnectEditRegistry'
import { useAppStore } from '../stores/appStore'
import { useTenantStore } from '../stores/tenantStore'
import { useAdminPaths } from '../composables/useAdminPaths'
import { useAccountUsage } from '../composables/useAccountUsage'
import { useOnboardingTour } from '../composables/useOnboardingTour'
import { usePwaInstall } from '../composables/usePwaInstall'
import { usePushNotifications } from '../composables/usePushNotifications'
import { startAccountNotificationService, stopAccountNotificationService } from '../services/accountNotificationService'
import { isPartnerStaffFromJwt } from '../utils/jwtUtils'
import AdminNavIcon from './adminNav/AdminNavIcon.vue'
import {
  ADMIN_SIDEBAR_MENUS,
  buildVisibleSidebarNav,
  resolveSidebarMenuKey
} from './adminNav/adminSidebarMenus'
import { usePartnerProgramView } from '../composables/usePartnerProgramView'
import { useMemberPermissions } from '../composables/useMemberPermissions'
const STORAGE_KEY = 'admin-sidebar-open'
const NAV_GROUP_EXPANDED_KEY = 'admin-sidebar-nav-groups-expanded'

function readNavGroupExpandedMap() {
  try {
    const raw = localStorage.getItem(NAV_GROUP_EXPANDED_KEY)
    if (raw) {
      const o = JSON.parse(raw)
      if (o && typeof o === 'object' && !Array.isArray(o)) return o
    }
  } catch {
    /* ignore */
  }
  return {}
}

const navGroupExpanded = ref(readNavGroupExpandedMap())

function loadSidebarState() {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    return stored !== null ? stored === 'true' : true
  } catch {
    return true
  }
}

const sidebarOpen = ref(loadSidebarState())
const userMenuOpen = ref(false)
const userMenuRef = ref(null)
const route = useRoute()
const router = useRouter()
const { toPath } = useAdminPaths()

const isMobile = ref(typeof window !== 'undefined' && window.innerWidth < 1024)

const ACTIVE_WORKSPACE_KEY = 'active_workspace_id'
const ACTIVE_WORKSPACE_NAME_KEY = 'active_workspace_name'
const workspaceModalOpen = ref(false)
/** Si es false (p. ej. obligatorio tras login), no se cierra con overlay, Esc ni Cancelar. */
const workspaceModalDismissible = ref(true)
const workspaces = ref([])
const activeWorkspaceId = ref('')
const activeWorkspaceNameStored = ref('')

const store = useAppStore()
const tenantStore = useTenantStore()
tenantStore.refreshFromPath()
const { usage, fetchUsage } = useAccountUsage()
const accountId = computed(() => usage.value?.accountId ?? '')

function readWorkspaceContextFromStorage() {
  try {
    activeWorkspaceId.value = localStorage.getItem(ACTIVE_WORKSPACE_KEY) || ''
    activeWorkspaceNameStored.value = localStorage.getItem(ACTIVE_WORKSPACE_NAME_KEY) || ''
  } catch {
    activeWorkspaceId.value = ''
    activeWorkspaceNameStored.value = ''
  }
}

function workspaceListContainsStoredId(storedId) {
  const s = String(storedId || '').trim()
  if (!s || !workspaces.value.length) return false
  return workspaces.value.some((w) => String(w.id) === s)
}

function clearStoredWorkspaceContext() {
  try {
    localStorage.removeItem(ACTIVE_WORKSPACE_KEY)
    localStorage.removeItem(ACTIVE_WORKSPACE_NAME_KEY)
  } catch {
    /* ignore */
  }
  activeWorkspaceId.value = ''
  activeWorkspaceNameStored.value = ''
}

function syncActiveWorkspaceNameFromList() {
  const id = activeWorkspaceId.value
  if (!id || !workspaces.value.length) return
  const w = workspaces.value.find((x) => String(x.id) === id)
  const n = (w?.name && String(w.name).trim()) || ''
  if (!n) return
  activeWorkspaceNameStored.value = n
  try {
    localStorage.setItem(ACTIVE_WORKSPACE_NAME_KEY, n)
  } catch {
    /* ignore */
  }
}

const activeWorkspaceName = computed(() => {
  const id = activeWorkspaceId.value
  if (!id) return ''
  const fromList = workspaces.value.find((w) => String(w.id) === id)?.name
  return (fromList || activeWorkspaceNameStored.value || '').trim()
})

const activeWorkspaceHeaderLabel = computed(() => activeWorkspaceName.value || 'Workspace')

const activeWorkspaceTooltip = computed(() => {
  const id = activeWorkspaceId.value
  const name = activeWorkspaceName.value
  if (!id) return 'Seleccionar workspace'
  return name ? `Workspace: ${name}` : `Workspace (${id})`
})

async function loadWorkspaces() {
  try {
    workspaces.value = await apiService.getWorkspaceLookups()
    syncActiveWorkspaceNameFromList()
  } catch {
    workspaces.value = []
  }
}

/**
 * After loading workspaces: clear stale localStorage ids; auto-select when there is
 * only one workspace (or partner portal); otherwise open the mandatory picker modal.
 */
async function ensureWorkspaceMatchesLoadedList() {
  readWorkspaceContextFromStorage()
  const storedId = String(activeWorkspaceId.value || '').trim()

  if (workspaces.value.length === 0) {
    if (storedId) {
      clearStoredWorkspaceContext()
      await fetchUsage(true, usageRefreshViewAccountId())
    }
    return
  }

  const matches = workspaceListContainsStoredId(storedId)
  if (storedId && !matches) {
    clearStoredWorkspaceContext()
    await fetchUsage(true, usageRefreshViewAccountId())
  }

  const needsPick =
    !String(activeWorkspaceId.value || '').trim() ||
    !workspaceListContainsStoredId(activeWorkspaceId.value)
  if (needsPick) {
    if (showPartnerPortal.value || workspaces.value.length === 1) {
      const def = workspaces.value[0]
      if (def) await selectWorkspace(def.id)
      return
    }
    await openWorkspaceModal({ dismissible: false })
  }
}

function onWorkspaceModalEscape(e) {
  if (e.key !== 'Escape') return
  if (!workspaceModalOpen.value || !workspaceModalDismissible.value) return
  e.preventDefault()
  workspaceModalOpen.value = false
}

watch(workspaceModalOpen, (open) => {
  if (typeof document === 'undefined') return
  if (open) {
    document.addEventListener('keydown', onWorkspaceModalEscape)
    document.body.style.overflow = 'hidden'
  } else {
    document.removeEventListener('keydown', onWorkspaceModalEscape)
    document.body.style.overflow = ''
  }
})

async function openWorkspaceModal(opts = {}) {
  if (showPartnerPortal.value) return
  await loadWorkspaces()
  workspaceModalDismissible.value = opts.dismissible !== false
  workspaceModalOpen.value = true
}

function closeWorkspaceModalIfAllowed() {
  if (!workspaceModalDismissible.value) return
  workspaceModalOpen.value = false
}

async function selectWorkspace(workspaceId) {
  const id = String(workspaceId)
  const ws = workspaces.value.find((w) => String(w.id) === id)
  const name = (ws?.name && String(ws.name).trim()) || ''
  workspaceModalOpen.value = false
  try {
    await apiService.selectWorkspace(id, name || null)
  } catch {
    /* token may not refresh; still sync local context */
  }
  try {
    localStorage.setItem(ACTIVE_WORKSPACE_KEY, id)
    if (name) localStorage.setItem(ACTIVE_WORKSPACE_NAME_KEY, name)
  } catch {
    /* ignore */
  }
  activeWorkspaceId.value = id
  if (name) activeWorkspaceNameStored.value = name
  // Handler refreshes workspace-scoped integrations then opens MandatoryStoreConnectionModal if empty.
  eventBus.emit(WORKSPACE_CONTEXT_CHANGED)
}

/**
 * Active workspace changed (picker, AdminWorkspaces create/select, etc.).
 * Reload integrations for the new X-Workspace-Id so needsMandatoryStoreConnection()
 * (myIntegrations.length === 0) can open MandatoryStoreConnectionModal with the real flow.
 */
async function handleWorkspaceContextChanged() {
  readWorkspaceContextFromStorage()
  await loadWorkspaces()
  await fetchUsage(true, usageRefreshViewAccountId())
  mandatoryStoreSetupInProgress.value = false
  await refreshMyIntegrations()
  evaluateMandatoryStoreModal()
}

readWorkspaceContextFromStorage()

const isAccountOwner = computed(() => store.profile?.isAccountOwner === true)
const isSuperAdmin = computed(() => store.profile?.isSuperAdmin === true)
const { can: canPermission } = useMemberPermissions({ profile: computed(() => store.profile) })
provide('memberCan', canPermission)
const showPartnerPortal = computed(
  () => isPartnerStaffFromJwt() || store.profile?.isPartnerStaff === true || store.profile?.IsPartnerStaff === true
)
const { partnerViewAccountId } = usePartnerProgramView()
function usageRefreshViewAccountId() {
  return showPartnerPortal.value && partnerViewAccountId.value ? partnerViewAccountId.value : null
}
/** Super admin y staff partner: sin tour de onboarding de cuenta. */
const onboardingTourEligible = computed(() => !isSuperAdmin.value && !showPartnerPortal.value)

/** Same audience as tour: operator accounts must connect a store or API. */
const mandatoryStoreModalEligible = computed(() => !isSuperAdmin.value && !showPartnerPortal.value)

// Tour de onboarding: botón "Guía" y auto-apertura (solo cuentas operador, no partner ni super admin).
const { shouldShowTour, pendingCount, totalSteps, fetchTour, refreshProgress } = useOnboardingTour(accountId, usage)
const PWA_INSTALL_BANNER_KEY = 'pwa-install-banner-dismissed'
const { shouldShowPrompt: pwaShouldShowPrompt, isInstalled: pwaIsInstalled, promptInstall: pwaPromptInstall, dismissInstall: pwaDismissInstall } = usePwaInstall()
const pwaInstallBannerVisible = ref(!localStorage.getItem(PWA_INSTALL_BANNER_KEY))

function dismissPwaInstallBanner() {
  localStorage.setItem(PWA_INSTALL_BANNER_KEY, '1')
  pwaInstallBannerVisible.value = false
}
const push = usePushNotifications()
const toast = useToast()
const tourModalOpen = ref(false)
const mandatoryStoreModalOpen = ref(false)
/** After the user picks a provider, stay on Integraciones without reopening this modal. */
const mandatoryStoreSetupInProgress = ref(false)
/** Tenant integrations — modal closes when length > 0 (any PC / invited members). */
const myIntegrations = ref([])

function isOnIntegracionesRoute() {
  return /\/admin\/integraciones(?:\/|$)/.test(route.path)
}

async function refreshMyIntegrations() {
  try {
    const list = await apiService.getMyIntegrations()
    myIntegrations.value = Array.isArray(list) ? list : []
  } catch (err) {
    console.error('Error fetching my integrations:', err)
    myIntegrations.value = []
  }
  return myIntegrations.value
}

function needsMandatoryStoreConnection() {
  if (!mandatoryStoreModalEligible.value) return false
  return myIntegrations.value.length === 0
}

/**
 * Open the mandatory store/API modal when the account has neither.
 * Suppressed while the workspace picker is open, or while the user is on Integraciones finishing setup.
 */
function evaluateMandatoryStoreModal() {
  if (!needsMandatoryStoreConnection()) {
    mandatoryStoreModalOpen.value = false
    mandatoryStoreSetupInProgress.value = false
    return
  }
  if (workspaceModalOpen.value) return
  if (isOnIntegracionesRoute() || mandatoryStoreSetupInProgress.value) {
    mandatoryStoreModalOpen.value = false
    return
  }
  mandatoryStoreModalOpen.value = true
  if (tourModalOpen.value) tourModalOpen.value = false
}

async function onMandatoryStoreIntegrationSelected({ meta, tab }) {
  mandatoryStoreSetupInProgress.value = true
  mandatoryStoreModalOpen.value = false
  const query = {}
  if (tab) query.tab = tab
  await router.push({ path: toPath('integraciones'), query })
  await nextTick()
  if (meta) openIntegrationConnectModal(meta)
}

async function onMandatoryStoreCreated() {
  mandatoryStoreSetupInProgress.value = false
  mandatoryStoreModalOpen.value = false
  await Promise.all([
    refreshMyIntegrations(),
    store.fetchProfile(true),
    fetchUsage(true, usageRefreshViewAccountId())
  ])
  await router.push({ path: toPath('integraciones'), query: { tab: 'channels' } })
  evaluateMandatoryStoreModal()
}

async function initTour() {
  if (!onboardingTourEligible.value) return
  if (mandatoryStoreModalOpen.value || needsMandatoryStoreConnection()) return
  await fetchTour()
  if (shouldShowTour.value) {
    tourModalOpen.value = true
  }
}

watch(workspaceModalOpen, (open) => {
  if (!open) evaluateMandatoryStoreModal()
})

const commerceStoreMode = computed(() => {
  const raw = store.profile?.commerceStoreMode ?? store.profile?.CommerceStoreMode ?? 'None'
  return String(raw || 'None')
})
const isNativeStore = computed(() => commerceStoreMode.value === 'Native')

const sidebarMenuKey = computed(() =>
  resolveSidebarMenuKey({
    isSuperAdmin: isSuperAdmin.value,
    isAccountOwner: isAccountOwner.value,
    showPartnerPortal: showPartnerPortal.value
  })
)

const sidebarNavEntries = computed(() =>
  buildVisibleSidebarNav(ADMIN_SIDEBAR_MENUS[sidebarMenuKey.value], {
    isAccountOwner: isAccountOwner.value,
    showPartnerPortal: showPartnerPortal.value,
    canPermission: canPermission,
    isNativeStore: isNativeStore.value
  })
)

function navGroupStorageId(menuKey, label) {
  return `${menuKey}:${label}`
}

function persistNavGroupExpanded() {
  try {
    localStorage.setItem(NAV_GROUP_EXPANDED_KEY, JSON.stringify(navGroupExpanded.value))
  } catch {
    /* ignore */
  }
}

/** Con la barra colapsada (solo iconos) siempre se muestran los ítems del grupo. */
function isNavGroupExpanded(entry) {
  if (entry.kind !== 'group') return true
  if (!sidebarOpen.value) return true
  const id = navGroupStorageId(sidebarMenuKey.value, entry.label)
  return navGroupExpanded.value[id] !== false
}

function toggleNavGroup(entry) {
  if (entry.kind !== 'group') return
  const id = navGroupStorageId(sidebarMenuKey.value, entry.label)
  const expanded = navGroupExpanded.value[id] !== false
  navGroupExpanded.value = { ...navGroupExpanded.value, [id]: !expanded }
  persistNavGroupExpanded()
}

function expandGroupsContainingActiveRoute() {
  const entries = sidebarNavEntries.value
  const menuKey = sidebarMenuKey.value
  const patch = { ...navGroupExpanded.value }
  let changed = false
  for (const entry of entries) {
    if (entry.kind !== 'group') continue
    const id = navGroupStorageId(menuKey, entry.label)
    const anyActive = entry.children.some((c) =>
      isSidebarNavLinkActive(c.legacyPath, { excludePaths: c.excludePaths })
    )
    if (anyActive && patch[id] !== true) {
      patch[id] = true
      changed = true
    }
  }
  if (changed) {
    navGroupExpanded.value = patch
    persistNavGroupExpanded()
  }
}

watch(
  () => [route.path, sidebarMenuKey.value],
  () => expandGroupsContainingActiveRoute(),
  { immediate: true }
)

function sidebarLinkTo(legacyPath) {
  const tail = legacyPath === '/admin' ? '' : legacyPath.replace(/^\/admin\//, '')
  return toPath(tail)
}

const billingInfo = computed(() => store.billing)

function closeUserMenuOnClickOutside(e) {
  if (userMenuOpen.value && userMenuRef.value && !userMenuRef.value.contains(e.target) && !e.target.closest('[aria-label="Commerceiones de usuario"]')) {
    userMenuOpen.value = false
  }
}

function closeSidebarOnNavClick() {
  if (window.innerWidth < 1024) sidebarOpen.value = false
}

async function handleEnablePush() {
  const ok = await push.requestPermissionAndSubscribe()
  userMenuOpen.value = false
  if (ok) {
    toast.success('Notificaciones push activadas')
  } else if (push.error) {
    toast.error(push.error)
  }
}

function handleSignOut() {
  userMenuOpen.value = false
  stopAccountNotificationService()
  store.clearSessionData()
  apiService.signOut()
}

watch(sidebarOpen, (open) => {
  try {
    localStorage.setItem(STORAGE_KEY, String(open))
  } catch {}
})

const trialBannerVisible = computed(() => {
  if (isSuperAdmin.value || showPartnerPortal.value) return false
  const b = billingInfo.value
  if (!b || b.hasActiveSubscription) return false
  return b.trialExpiresAt != null || b.isTrialExpired === true
})

const trialBannerMessage = computed(() => {
  const b = billingInfo.value
  if (!b) return ''
  if (b.isTrialExpired) return 'Tu periodo de prueba ha expirado.'
  if (b.daysRemaining != null) {
    if (b.daysRemaining === 0) return 'Estás en periodo de prueba – expira hoy.'
    if (b.daysRemaining === 1) return 'Estás en periodo de prueba – 1 día restante.'
    return `Estás en periodo de prueba – ${b.daysRemaining} días restantes.`
  }
  return 'Estás en periodo de prueba.'
})

const trialBannerClass = computed(() => {
  const b = billingInfo.value
  const expired = b?.isTrialExpired === true
  return expired
    ? 'bg-amber-100 dark:bg-amber-900/40 text-amber-900 dark:text-amber-100'
    : 'bg-primary-50 dark:bg-primary-900/30 text-primary-800 dark:text-primary-200'
})

/** Altura desde el top del viewport donde empieza el sidebar (mobile). Incluye banners + header. */
const sidebarTopOffset = computed(() => {
  const headerH = 56 // h-14
  let bannersH = 0
  if (!pwaIsInstalled.value && pwaInstallBannerVisible.value) bannersH += 40 // py-2 + text
  if (trialBannerVisible.value) bannersH += 40
  return headerH + bannersH
})

async function handleTourProgressUpdated() {
  await fetchUsage(true, usageRefreshViewAccountId())
  evaluateMandatoryStoreModal()
  if (onboardingTourEligible.value) {
    refreshProgress()
  }
  if (!mandatoryStoreModalOpen.value && !needsMandatoryStoreConnection()) {
    await initTour()
  }
}

function handleOpenTour() {
  if (!onboardingTourEligible.value) return
  if (mandatoryStoreModalOpen.value) return
  tourModalOpen.value = true
}

function handleMyIntegrationsUpdated() {
  Promise.all([
    refreshMyIntegrations(),
    store.fetchProfile(true),
    fetchUsage(true, usageRefreshViewAccountId())
  ]).then(() => {
    evaluateMandatoryStoreModal()
  })
}

function updateIsMobile() {
  isMobile.value = window.innerWidth < 1024
}

watch(
  () => route.path,
  () => {
    if (!isOnIntegracionesRoute()) {
      mandatoryStoreSetupInProgress.value = false
    }
    evaluateMandatoryStoreModal()
  }
)

watch(
  () => myIntegrations.value.length,
  () => evaluateMandatoryStoreModal()
)

onMounted(async () => {
  document.addEventListener('click', closeUserMenuOnClickOutside)
  window.addEventListener('resize', updateIsMobile)
  updateIsMobile()
  eventBus.on(TOUR_PROGRESS_UPDATED, handleTourProgressUpdated)
  eventBus.on(OPEN_TOUR, handleOpenTour)
  eventBus.on(WORKSPACE_CONTEXT_CHANGED, handleWorkspaceContextChanged)
  eventBus.on(MY_INTEGRATIONS_UPDATED, handleMyIntegrationsUpdated)
  startAccountNotificationService()
  try {
    await store.loadCoreData()
    await Promise.all([loadWorkspaces(), refreshMyIntegrations()])
    await ensureWorkspaceMatchesLoadedList()
    evaluateMandatoryStoreModal()
    if (!mandatoryStoreModalOpen.value) {
      await initTour()
    }
  } catch (err) {
    console.error('AdminLayout loadCoreData error', err)
  }
})
onUnmounted(() => {
  document.removeEventListener('keydown', onWorkspaceModalEscape)
  document.body.style.overflow = ''
  document.removeEventListener('click', closeUserMenuOnClickOutside)
  window.removeEventListener('resize', updateIsMobile)
  eventBus.off(TOUR_PROGRESS_UPDATED, handleTourProgressUpdated)
  eventBus.off(OPEN_TOUR, handleOpenTour)
  eventBus.off(WORKSPACE_CONTEXT_CHANGED, handleWorkspaceContextChanged)
  eventBus.off(MY_INTEGRATIONS_UPDATED, handleMyIntegrationsUpdated)
  stopAccountNotificationService()
})

function isSidebarNavLinkActive(legacyAdminPath, opts = {}) {
  const resolved =
    legacyAdminPath === '/admin'
      ? toPath('')
      : toPath(String(legacyAdminPath || '').replace(/^\/admin\//, ''))
  let isActive =
    legacyAdminPath === '/admin'
      ? route.path === resolved
      : route.path === resolved || route.path.startsWith(`${resolved}/`)
  if (opts.excludePaths?.length) {
    const excluded = opts.excludePaths.some((ex) => {
      const full =
        ex === '/admin'
          ? toPath('')
          : toPath(String(ex || '').replace(/^\/admin\//, ''))
      return route.path === full || route.path.startsWith(`${full}/`)
    })
    if (excluded) isActive = false
  }
  return isActive
}

const navLinkClass = (legacyAdminPath, opts = {}) => {
  const isActive = isSidebarNavLinkActive(legacyAdminPath, opts)
  return [
    'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
    !sidebarOpen.value && 'lg:justify-center lg:px-2',
    isActive
      ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400'
      : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
  ]
}
</script>

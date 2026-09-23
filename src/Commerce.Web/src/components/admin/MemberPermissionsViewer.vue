<template>
  <MemberPermissionsSkeleton v-if="loading" variant="viewer" />
  <div v-else-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</div>
  <div v-else class="space-y-4">
    <p
      v-if="fullAccess"
      class="rounded-lg border border-primary-200 dark:border-primary-800 bg-primary-50/80 dark:bg-primary-950/40 px-4 py-3 text-sm text-primary-900 dark:text-primary-100"
    >
      {{ fullAccessMessage }}
    </p>
    <p
      v-else-if="!grantedSet.size"
      class="rounded-lg border border-dashed border-gray-300 dark:border-gray-600 px-4 py-3 text-sm text-gray-600 dark:text-gray-400"
    >
      No tenés permisos asignados en esta cuenta. Pedile al propietario que te habilite acciones desde Miembros.
    </p>
    <div
      v-for="resource in catalog.resources"
      :key="resource.key"
      class="border border-gray-200 dark:border-gray-600 rounded-lg p-3"
    >
      <p class="text-sm font-medium text-gray-900 dark:text-white mb-3">{{ resource.label }}</p>
      <ul class="space-y-2.5">
        <li
          v-for="action in resource.actions"
          :key="action.key"
          class="flex items-start gap-2.5 text-sm"
          :class="
            grantedSet.has(action.key)
              ? 'text-gray-900 dark:text-gray-100'
              : 'text-gray-400 dark:text-gray-500'
          "
        >
          <span
            class="mt-0.5 shrink-0 inline-flex h-4 w-4 items-center justify-center rounded-full border"
            :class="
              grantedSet.has(action.key)
                ? 'border-green-500 bg-green-500 text-white'
                : 'border-gray-300 dark:border-gray-600 bg-transparent'
            "
            aria-hidden="true"
          >
            <svg
              v-if="grantedSet.has(action.key)"
              class="h-2.5 w-2.5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
            </svg>
          </span>
          <span class="min-w-0">
            <span class="font-medium">{{ action.label }}</span>
            <p
              v-if="action.description"
              class="mt-0.5 text-xs leading-relaxed"
              :class="
                grantedSet.has(action.key)
                  ? 'text-gray-500 dark:text-gray-400'
                  : 'text-gray-400 dark:text-gray-600'
              "
            >
              {{ action.description }}
            </p>
          </span>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import apiService from '../../services/api'
import MemberPermissionsSkeleton from './MemberPermissionsSkeleton.vue'

const props = defineProps({
  permissions: {
    type: Array,
    default: () => []
  },
  isAccountOwner: {
    type: Boolean,
    default: false
  },
  isSuperAdmin: {
    type: Boolean,
    default: false
  }
})

const loading = ref(true)
const error = ref(null)
const catalog = ref({ resources: [] })

const fullAccess = computed(() => props.isAccountOwner || props.isSuperAdmin)

const fullAccessMessage = computed(() => {
  if (props.isSuperAdmin) return 'Super administrador: acceso completo a todas las funciones de la plataforma.'
  return 'Propietario de la cuenta: tenés acceso completo a todos los permisos listados abajo.'
})

const grantedSet = computed(() => new Set(props.permissions ?? []))

function integrationsOnlyCatalog(raw) {
  const resources = (raw?.resources ?? []).filter((r) => r.key === 'integrations')
  return { ...raw, resources }
}

onMounted(async () => {
  try {
    catalog.value = integrationsOnlyCatalog(await apiService.getMemberPermissionsCatalog())
  } catch (e) {
    error.value = e.response?.data?.error || 'No se pudo cargar el catálogo de permisos'
  } finally {
    loading.value = false
  }
})
</script>

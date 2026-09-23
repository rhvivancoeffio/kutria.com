<template>
  <MemberPermissionsSkeleton v-if="loading" />
  <div v-else-if="error" class="text-sm text-red-600 dark:text-red-400">{{ error }}</div>
  <div v-else class="space-y-4">
    <section
      v-for="resource in catalog.resources"
      :key="resource.key"
      class="border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden"
    >
      <label
        class="flex items-center gap-3 px-3 py-3 sm:px-4 bg-gray-50/90 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-600 cursor-pointer group"
      >
        <input
          :ref="(el) => syncGroupCheckbox(el, resource)"
          type="checkbox"
          class="rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 shrink-0"
          :aria-label="`Seleccionar todos los permisos de ${resource.label}`"
          @change="toggleGroup(resource, $event.target.checked)"
        />
        <span class="min-w-0 flex-1">
          <span class="text-sm font-semibold text-gray-900 dark:text-white group-hover:text-primary-700 dark:group-hover:text-primary-300">
            {{ resource.label }}
          </span>
          <span class="ml-2 text-xs font-medium tabular-nums text-gray-500 dark:text-gray-400">
            {{ groupSelectionSummary(resource) }}
          </span>
        </span>
      </label>

      <div class="p-3 sm:p-4 space-y-3">
        <label
          v-for="action in resource.actions"
          :key="action.key"
          class="flex items-start gap-3 text-sm cursor-pointer group pl-1"
        >
          <input
            type="checkbox"
            :checked="selectedSet.has(action.key)"
            class="mt-0.5 rounded border-gray-300 dark:border-gray-600 text-primary-600 focus:ring-primary-500 shrink-0"
            @change="toggle(action.key, $event.target.checked)"
          />
          <span class="min-w-0">
            <span class="font-medium text-gray-800 dark:text-gray-200 group-hover:text-gray-900 dark:group-hover:text-white">
              {{ action.label }}
            </span>
            <p
              v-if="action.description"
              class="mt-0.5 text-xs leading-relaxed text-gray-500 dark:text-gray-400"
            >
              {{ action.description }}
            </p>
          </span>
        </label>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import apiService from '../../services/api'
import MemberPermissionsSkeleton from './MemberPermissionsSkeleton.vue'

const props = defineProps({
  modelValue: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['update:modelValue'])

const loading = ref(true)
const error = ref(null)
const catalog = ref({ resources: [], defaultPermissions: [] })

const selectedSet = computed(() => new Set(props.modelValue ?? []))

function groupKeys(resource) {
  return (resource.actions ?? []).map((a) => a.key)
}

function groupSelectionSummary(resource) {
  const keys = groupKeys(resource)
  if (!keys.length) return '0/0'
  const selected = keys.filter((k) => selectedSet.value.has(k)).length
  return `${selected}/${keys.length}`
}

function syncGroupCheckbox(el, resource) {
  if (!el) return
  const keys = groupKeys(resource)
  const selected = keys.filter((k) => selectedSet.value.has(k)).length
  el.checked = keys.length > 0 && selected === keys.length
  el.indeterminate = selected > 0 && selected < keys.length
}

function toggleGroup(resource, checked) {
  const next = new Set(props.modelValue ?? [])
  for (const action of resource.actions ?? []) {
    if (checked) next.add(action.key)
    else next.delete(action.key)
  }
  emit('update:modelValue', [...next])
}

const INTEGRATIONS_RESOURCE_KEY = 'integrations'

function integrationsOnlyCatalog(raw) {
  const resources = (raw?.resources ?? []).filter((r) => r.key === INTEGRATIONS_RESOURCE_KEY)
  return { ...raw, resources }
}

onMounted(async () => {
  try {
    catalog.value = integrationsOnlyCatalog(await apiService.getMemberPermissionsCatalog())
    if (!props.modelValue?.length && catalog.value.defaultPermissions?.length) {
      emit('update:modelValue', [...catalog.value.defaultPermissions])
    }
  } catch (e) {
    error.value = e.response?.data?.error || 'No se pudo cargar el catálogo de permisos'
  } finally {
    loading.value = false
  }
})

watch(
  () => props.modelValue,
  (v) => {
    if (!v?.length && catalog.value.defaultPermissions?.length && !loading.value) {
      emit('update:modelValue', [...catalog.value.defaultPermissions])
    }
  }
)

function toggle(key, checked) {
  const next = new Set(props.modelValue ?? [])
  if (checked) next.add(key)
  else next.delete(key)
  emit('update:modelValue', [...next])
}
</script>

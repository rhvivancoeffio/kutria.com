<template>
  <slot v-if="allowed" />
</template>

<script setup>
import { computed } from 'vue'
import { useAppResourcePermissions } from '@/composables/useMemberPermissions'

const props = defineProps({
  /** Recurso del catálogo (products, orders, channels, …). */
  resource: {
    type: String,
    required: true
  },
  /** Acción: list | create | delete | purge | bulkUpload | changeStatus | respond */
  action: {
    type: String,
    required: true
  }
})

const ACTION_FLAG = {
  list: 'canList',
  create: 'canCreate',
  delete: 'canDelete',
  purge: 'canPurge',
  bulkUpload: 'canBulkUpload',
  changeStatus: 'canChangeStatus',
  respond: 'canRespond'
}

const perms = useAppResourcePermissions(props.resource)

const allowed = computed(() => {
  const flag = ACTION_FLAG[props.action]
  return flag ? perms[flag].value === true : false
})
</script>

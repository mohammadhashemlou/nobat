<script setup lang="ts">
import type { AppointmentStatus } from '~/types/api'

const props = defineProps<{ status: AppointmentStatus | string }>()

const map: Record<string, { label: string; color: 'success' | 'warning' | 'error' | 'neutral' | 'info' }> = {
  Pending: { label: 'در انتظار', color: 'warning' },
  Confirmed: { label: 'تأیید شده', color: 'success' },
  Completed: { label: 'انجام شده', color: 'info' },
  Cancelled: { label: 'لغو شده', color: 'error' },
  NoShow: { label: 'عدم حضور', color: 'neutral' }
}

const meta = computed(() => map[props.status] ?? { label: props.status, color: 'neutral' as const })
</script>

<template>
  <UBadge :color="meta.color" variant="subtle" size="sm">
    {{ meta.label }}
  </UBadge>
</template>

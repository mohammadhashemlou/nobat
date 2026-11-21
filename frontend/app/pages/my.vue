<script setup lang="ts">
definePageMeta({ layout: 'default', middleware: 'auth' })

useHead({ title: 'نوبت‌های من | نوبت' })

const api = useApi()
const { formatFullDate, formatTime, persianDigits } = useJalali()
const toast = useToast()

const appointments = ref<Appointment[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const activeTab = ref<'upcoming' | 'past'>('upcoming')
const cancellingId = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    appointments.value = await api<Appointment[]>('/api/v1/appointments')
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری نوبت‌ها ناموفق بود'
  } finally {
    loading.value = false
  }
}

onMounted(load)

const now = Date.now()

const upcoming = computed(() =>
  appointments.value
    .filter((a) => new Date(a.startAt).getTime() >= now && a.status !== 'Cancelled')
    .sort((a, b) => +new Date(a.startAt) - +new Date(b.startAt))
)

const past = computed(() =>
  appointments.value
    .filter((a) => new Date(a.startAt).getTime() < now || a.status === 'Cancelled')
    .sort((a, b) => +new Date(b.startAt) - +new Date(a.startAt))
)

const visible = computed(() => (activeTab.value === 'upcoming' ? upcoming.value : past.value))

const canCancel = (a: Appointment) =>
  new Date(a.startAt).getTime() > Date.now() &&
  (a.status === 'Confirmed' || a.status === 'Pending')

async function cancel(a: Appointment) {
  cancellingId.value = a.id
  try {
    await api(`/api/v1/appointments/${a.id}/cancel`, { method: 'POST' })
    toast.add({ title: 'نوبت لغو شد', color: 'success', icon: 'i-lucide-check' })
    await load()
  } catch (e: any) {
    toast.add({ title: e?.message || 'لغو نوبت ناموفق بود', color: 'error', icon: 'i-lucide-circle-alert' })
  } finally {
    cancellingId.value = null
  }
}

const priceLabel = (p: number) => `${persianDigits(p.toLocaleString('en-US'))} تومان`
</script>

<template>
  <div class="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-12">
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-2xl font-extrabold">نوبت‌های من</h1>
        <p class="mt-1 text-sm text-muted">نوبت‌های آینده و گذشته خود را مدیریت کنید.</p>
      </div>
      <UButton to="/booking" icon="i-lucide-calendar-plus">رزرو نوبت جدید</UButton>
    </div>

    <UTabs
      v-model="activeTab"
      :items="[
        { label: `آینده (${persianDigits(upcoming.length)})`, value: 'upcoming', icon: 'i-lucide-calendar-clock' },
        { label: `گذشته (${persianDigits(past.length)})`, value: 'past', icon: 'i-lucide-history' }
      ]"
      class="mb-5"
    />

    <div v-if="loading" class="space-y-3">
      <USkeleton v-for="i in 3" :key="i" class="h-28 w-full rounded-2xl" />
    </div>

    <ErrorState v-else-if="error" :message="error" @retry="load" />

    <EmptyState
      v-else-if="!visible.length"
      :icon="activeTab === 'upcoming' ? 'i-lucide-calendar-plus' : 'i-lucide-history'"
      :title="activeTab === 'upcoming' ? 'نوبت آینده‌ای ندارید' : 'نوبتی در گذشته ثبت نشده'"
      :description="activeTab === 'upcoming' ? 'همین حالا یک نوبت جدید رزرو کنید.' : undefined"
      action-label="رزرو نوبت"
      action-to="/booking"
    />

    <div v-else class="space-y-3">
      <article v-for="a in visible" :key="a.id" class="panel p-4 sm:p-5">
        <div class="flex flex-wrap items-start justify-between gap-3">
          <div class="min-w-0">
            <div class="flex flex-wrap items-center gap-2">
              <h2 class="font-bold">{{ a.serviceName }}</h2>
              <StatusBadge :status="a.status" />
            </div>
            <p class="mt-1 flex items-center gap-1.5 text-sm text-muted">
              <UIcon name="i-lucide-user" class="size-4" /> {{ a.barberName }}
            </p>
          </div>

          <div class="text-end">
            <p class="text-sm font-semibold">{{ formatFullDate(a.startAt) }}</p>
            <p class="mt-0.5 text-xs text-muted tabular-nums">
              {{ formatTime(a.startAt) }} تا {{ formatTime(a.endAt) }}
            </p>
          </div>
        </div>

        <div class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1 border-t border-gray-100 pt-3 text-xs text-muted dark:border-gray-800">
          <span class="flex items-center gap-1">
            <UIcon name="i-lucide-clock" class="size-3.5" />
            {{ persianDigits(a.durationMinutes) }} دقیقه
          </span>
          <span class="flex items-center gap-1 font-semibold text-brand-600 dark:text-brand-400">
            <UIcon name="i-lucide-banknote" class="size-3.5" />
            {{ priceLabel(a.price) }}
          </span>
          <span v-if="a.notes" class="flex items-center gap-1">
            <UIcon name="i-lucide-message-square" class="size-3.5" /> {{ a.notes }}
          </span>

          <UButton
            v-if="canCancel(a)"
            color="error"
            variant="ghost"
            size="xs"
            icon="i-lucide-x"
            class="ms-auto"
            :loading="cancellingId === a.id"
            @click="cancel(a)"
          >
            لغو نوبت
          </UButton>
        </div>
      </article>
    </div>
  </div>
</template>

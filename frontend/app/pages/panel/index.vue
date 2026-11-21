<script setup lang="ts">
definePageMeta({ layout: 'panel', middleware: 'admin' })

useHead({ title: 'نمای کلی | پنل مدیریت' })

const api = useApi()
const auth = useAuthStore()
const { ensure, salon } = useSalon()
const { formatTime, formatFullDate, persianDigits, todayApiDate } = useJalali()

const loading = ref(true)
const error = ref<string | null>(null)
const appointments = ref<Appointment[]>([])

async function load() {
  loading.value = true
  error.value = null
  try {
    const s = await ensure()
    appointments.value = await api<Appointment[]>('/api/v1/appointments', {
      query: { salonId: s.id }
    })
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری ناموفق بود'
  } finally {
    loading.value = false
  }
}

onMounted(load)

const todayIso = todayApiDate()

const todayAppointments = computed(() =>
  appointments.value
    .filter((a) => a.startAt.slice(0, 10) === todayIso && a.status !== 'Cancelled')
    .sort((a, b) => +new Date(a.startAt) - +new Date(b.startAt))
)

const upcomingCount = computed(
  () =>
    appointments.value.filter(
      (a) => new Date(a.startAt) > new Date() && a.status === 'Confirmed'
    ).length
)

const todayRevenue = computed(() =>
  todayAppointments.value
    .filter((a) => a.status === 'Completed' || a.status === 'Confirmed')
    .reduce((sum, a) => sum + a.price, 0)
)

const stats = computed(() => [
  {
    label: 'نوبت‌های امروز',
    value: persianDigits(todayAppointments.value.length),
    icon: 'i-lucide-calendar-days',
    tone: 'brand'
  },
  {
    label: 'نوبت‌های پیش‌رو',
    value: persianDigits(upcomingCount.value),
    icon: 'i-lucide-calendar-clock',
    tone: 'emerald'
  },
  {
    label: 'درآمد امروز',
    value: `${persianDigits(todayRevenue.value.toLocaleString('en-US'))} ت`,
    icon: 'i-lucide-banknote',
    tone: 'amber'
  },
  {
    label: 'کل نوبت‌ها',
    value: persianDigits(appointments.value.length),
    icon: 'i-lucide-list-checks',
    tone: 'sky'
  }
])

const toneClass: Record<string, string> = {
  brand: 'bg-brand-50 text-brand-600 dark:bg-brand-500/10 dark:text-brand-300',
  emerald: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-300',
  amber: 'bg-amber-50 text-amber-600 dark:bg-amber-500/10 dark:text-amber-300',
  sky: 'bg-sky-50 text-sky-600 dark:bg-sky-500/10 dark:text-sky-300'
}

const priceLabel = (p: number) => `${persianDigits(p.toLocaleString('en-US'))} تومان`
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-xl font-extrabold sm:text-2xl">سلام {{ auth.user?.fullName }}</h1>
      <p class="mt-1 text-sm text-muted">
        نمای کلی سالن «{{ salon?.name || '—' }}» در {{ formatFullDate(todayIso) }}
      </p>
    </div>

    <div v-if="loading" class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <USkeleton v-for="i in 4" :key="i" class="h-28 rounded-2xl" />
    </div>

    <ErrorState v-else-if="error" :message="error" @retry="load" />

    <template v-else>
      <!-- Stats -->
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div v-for="s in stats" :key="s.label" class="panel p-4">
          <div class="flex items-center justify-between">
            <span class="grid size-10 place-items-center rounded-xl" :class="toneClass[s.tone]">
              <UIcon :name="s.icon" class="size-5" />
            </span>
          </div>
          <p class="mt-3 text-2xl font-extrabold tabular-nums">{{ s.value }}</p>
          <p class="mt-0.5 text-xs text-muted">{{ s.label }}</p>
        </div>
      </div>

      <!-- Today's schedule -->
      <section class="mt-6">
        <div class="mb-3 flex items-center justify-between">
          <h2 class="text-base font-bold">برنامه امروز</h2>
          <UButton to="/panel/appointments" variant="link" size="sm" trailing-icon="i-lucide-arrow-left">
            همه نوبت‌ها
          </UButton>
        </div>

        <EmptyState
          v-if="!todayAppointments.length"
          icon="i-lucide-calendar-check"
          title="امروز نوبتی ثبت نشده"
          description="برنامه امروز شما خالی است."
        />

        <div v-else class="panel divide-y divide-gray-100 dark:divide-gray-800">
          <div
            v-for="a in todayAppointments"
            :key="a.id"
            class="flex flex-wrap items-center gap-3 p-4"
          >
            <div class="flex w-16 shrink-0 flex-col items-center rounded-lg bg-gray-50 py-1.5 dark:bg-gray-800">
              <span class="text-sm font-bold tabular-nums">{{ formatTime(a.startAt) }}</span>
              <span class="text-[10px] text-muted">{{ formatTime(a.endAt) }}</span>
            </div>
            <div class="min-w-0 flex-1">
              <p class="font-semibold">{{ a.customerName }}</p>
              <p class="text-xs text-muted">{{ a.serviceName }} — {{ a.barberName }}</p>
            </div>
            <StatusBadge :status="a.status" />
            <span class="text-sm font-semibold text-brand-600 dark:text-brand-400">
              {{ priceLabel(a.price) }}
            </span>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

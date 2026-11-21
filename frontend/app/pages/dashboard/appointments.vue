<script setup lang="ts">
definePageMeta({ layout: 'dashboard', middleware: 'admin' })

useHead({ title: 'نوبت‌ها | پنل مدیریت' })

const api = useApi()
const { ensure } = useSalon()
const { formatFullDate, formatTime, persianDigits, todayApiDate } = useJalali()
const toast = useToast()

const loading = ref(true)
const error = ref<string | null>(null)
const appointments = ref<Appointment[]>([])
const filter = ref<'today' | 'upcoming' | 'all'>('today')
const statusFilter = ref<string>('all')
const busyId = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    const s = await ensure()
    const query: Record<string, unknown> = { salonId: s.id }
    if (filter.value === 'upcoming') query.upcomingOnly = true
    if (statusFilter.value !== 'all') query.status = statusFilter.value
    appointments.value = await api<Appointment[]>('/api/v1/appointments', { query })
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری ناموفق بود'
  } finally {
    loading.value = false
  }
}

onMounted(load)
watch([filter, statusFilter], load)

const todayIso = todayApiDate()

const rows = computed(() => {
  let list = appointments.value
  if (filter.value === 'today') list = list.filter((a) => a.startAt.slice(0, 10) === todayIso)
  return [...list].sort((a, b) => +new Date(a.startAt) - +new Date(b.startAt))
})

async function changeStatus(a: Appointment, status: string) {
  busyId.value = a.id
  try {
    await api(`/api/v1/appointments/${a.id}/status`, {
      method: 'PATCH',
      body: { status }
    })
    toast.add({ title: 'وضعیت به‌روزرسانی شد', color: 'success', icon: 'i-lucide-check' })
    await load()
  } catch (e: any) {
    toast.add({ title: e?.message || 'تغییر وضعیت ناموفق بود', color: 'error', icon: 'i-lucide-circle-alert' })
  } finally {
    busyId.value = null
  }
}

const statusOptions = [
  { label: 'تأیید شده', value: 'Confirmed' },
  { label: 'انجام شده', value: 'Completed' },
  { label: 'عدم حضور', value: 'NoShow' },
  { label: 'لغو شده', value: 'Cancelled' }
]

const priceLabel = (p: number) => `${persianDigits(p.toLocaleString('en-US'))} تومان`
</script>

<template>
  <div>
    <div class="mb-5 flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-extrabold sm:text-2xl">نوبت‌ها</h1>
        <p class="mt-1 text-sm text-muted">مدیریت و پیگیری نوبت‌های سالن.</p>
      </div>
      <UButton to="/booking" icon="i-lucide-plus">نوبت جدید</UButton>
    </div>

    <div class="mb-4 flex flex-wrap gap-2">
      <UButton
        v-for="f in [
          { label: 'امروز', value: 'today' },
          { label: 'پیش‌رو', value: 'upcoming' },
          { label: 'همه', value: 'all' }
        ]"
        :key="f.value"
        size="sm"
        :variant="filter === f.value ? 'solid' : 'outline'"
        :color="filter === f.value ? 'primary' : 'neutral'"
        @click="filter = f.value as any"
      >
        {{ f.label }}
      </UButton>

      <USelect
        v-model="statusFilter"
        :items="[{ label: 'همه وضعیت‌ها', value: 'all' }, ...statusOptions]"
        size="sm"
        class="ms-auto w-40"
      />
    </div>

    <div v-if="loading" class="space-y-2">
      <USkeleton v-for="i in 5" :key="i" class="h-16 w-full rounded-xl" />
    </div>

    <ErrorState v-else-if="error" :message="error" @retry="load" />

    <EmptyState
      v-else-if="!rows.length"
      icon="i-lucide-calendar-x"
      title="نوبتی یافت نشد"
      description="با فیلترهای انتخابی نوبتی وجود ندارد."
    />

    <div v-else class="panel overflow-hidden">
      <!-- Desktop table -->
      <table class="hidden w-full text-sm md:table">
        <thead class="border-b border-gray-200 bg-gray-50 text-xs text-muted dark:border-gray-800 dark:bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-start font-medium">تاریخ و ساعت</th>
            <th class="px-4 py-3 text-start font-medium">مشتری</th>
            <th class="px-4 py-3 text-start font-medium">خدمت</th>
            <th class="px-4 py-3 text-start font-medium">آرایشگر</th>
            <th class="px-4 py-3 text-start font-medium">مبلغ</th>
            <th class="px-4 py-3 text-start font-medium">وضعیت</th>
            <th class="px-4 py-3" />
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
          <tr v-for="a in rows" :key="a.id" class="hover:bg-gray-50/60 dark:hover:bg-gray-800/40">
            <td class="px-4 py-3">
              <p class="font-medium">{{ formatFullDate(a.startAt) }}</p>
              <p class="text-xs text-muted tabular-nums">
                <span dir="ltr">{{ formatTime(a.startAt) }}–{{ formatTime(a.endAt) }}</span>
              </p>
            </td>
            <td class="px-4 py-3">
              <p class="font-medium">{{ a.customerName }}</p>
              <p v-if="a.customerPhone" dir="ltr" class="text-xs text-muted">{{ a.customerPhone }}</p>
            </td>
            <td class="px-4 py-3">{{ a.serviceName }}</td>
            <td class="px-4 py-3">{{ a.barberName }}</td>
            <td class="px-4 py-3 font-semibold">{{ priceLabel(a.price) }}</td>
            <td class="px-4 py-3"><StatusBadge :status="a.status" /></td>
            <td class="px-4 py-3 text-end">
              <UDropdownMenu
                :items="[[
                  ...statusOptions.map((o) => ({
                    label: o.label,
                    onSelect: () => changeStatus(a, o.value)
                  }))
                ]]"
              >
                <UButton
                  variant="ghost"
                  color="neutral"
                  size="xs"
                  icon="i-lucide-ellipsis-vertical"
                  :loading="busyId === a.id"
                />
              </UDropdownMenu>
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Mobile cards -->
      <div class="divide-y divide-gray-100 md:hidden dark:divide-gray-800">
        <div v-for="a in rows" :key="a.id" class="p-4">
          <div class="flex items-start justify-between gap-2">
            <div class="min-w-0">
              <p class="font-semibold">{{ a.customerName }}</p>
              <p class="text-xs text-muted">{{ a.serviceName }} — {{ a.barberName }}</p>
            </div>
            <StatusBadge :status="a.status" />
          </div>
          <div class="mt-2 flex items-center justify-between text-xs text-muted">
            <span>{{ formatFullDate(a.startAt) }} — <span dir="ltr">{{ formatTime(a.startAt) }}</span></span>
            <span class="font-semibold text-brand-600 dark:text-brand-400">{{ priceLabel(a.price) }}</span>
          </div>
          <div class="mt-2 flex gap-1.5">
            <UButton
              v-for="o in statusOptions"
              :key="o.value"
              size="xs"
              variant="soft"
              color="neutral"
              :loading="busyId === a.id"
              @click="changeStatus(a, o.value)"
            >
              {{ o.label }}
            </UButton>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ layout: 'panel', middleware: 'admin' })

useHead({ title: 'ساعات کاری | پنل مدیریت' })

const api = useApi()
const route = useRoute()
const { ensure, salonId } = useSalon()
const { IRANIAN_WEEKDAYS } = useJalali()
const toast = useToast()

const loading = ref(true)
const saving = ref(false)
const error = ref<string | null>(null)
const barbers = ref<BarberAdmin[]>([])
const selectedBarberId = ref<string>('')

interface DayRow {
  enabled: boolean
  startTime: string
  endTime: string
  breakStart: string
  breakEnd: string
}

const rows = ref<DayRow[]>(
  Array.from({ length: 7 }, () => ({
    enabled: false,
    startTime: '09:00',
    endTime: '18:00',
    breakStart: '13:00',
    breakEnd: '14:00'
  }))
)

async function loadBarbers() {
  const id = salonId.value || (await ensure()).id
  barbers.value = await api<BarberAdmin[]>('/api/v1/barbers', {
    query: { salonId: id, includeInactive: false }
  })

  const fromQuery = route.query.barber as string | undefined
  selectedBarberId.value =
    fromQuery && barbers.value.some((b) => b.id === fromQuery)
      ? fromQuery
      : barbers.value[0]?.id || ''
}

async function loadHours() {
  if (!selectedBarberId.value) return
  loading.value = true
  error.value = null
  try {
    const data = await api<WorkingHours[]>(`/api/v1/barbers/${selectedBarberId.value}/working-hours`)
    const fresh: DayRow[] = Array.from({ length: 7 }, () => ({
      enabled: false,
      startTime: '09:00',
      endTime: '18:00',
      breakStart: '13:00',
      breakEnd: '14:00'
    }))
    for (const w of data) {
      fresh[w.dayOfWeekValue] = {
        enabled: true,
        startTime: w.startTime,
        endTime: w.endTime,
        breakStart: w.breakStart || '',
        breakEnd: w.breakEnd || ''
      }
    }
    rows.value = fresh
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری ساعات کاری ناموفق بود'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    await loadBarbers()
    await loadHours()
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری ناموفق بود'
    loading.value = false
  }
})

watch(selectedBarberId, loadHours)

async function save() {
  const invalid = rows.value.findIndex(
    (r) => r.enabled && (!r.startTime || !r.endTime || r.startTime >= r.endTime)
  )
  if (invalid >= 0) {
    toast.add({
      title: `ساعت شروع و پایان ${IRANIAN_WEEKDAYS[invalid]} نامعتبر است`,
      color: 'error',
      icon: 'i-lucide-circle-alert'
    })
    return
  }

  const items = rows.value
    .map((r, i) => ({ ...r, dayOfWeek: i }))
    .filter((r) => r.enabled)
    .map((r) => ({
      dayOfWeek: r.dayOfWeek,
      startTime: r.startTime,
      endTime: r.endTime,
      breakStart: r.breakStart || null,
      breakEnd: r.breakEnd || null
    }))

  saving.value = true
  try {
    await api(`/api/v1/barbers/${selectedBarberId.value}/working-hours`, {
      method: 'PUT',
      body: { items }
    })
    toast.add({ title: 'ساعات کاری ذخیره شد', color: 'success', icon: 'i-lucide-check' })
    await loadHours()
  } catch (e: any) {
    toast.add({ title: e?.message || 'ذخیره ناموفق بود', color: 'error', icon: 'i-lucide-circle-alert' })
  } finally {
    saving.value = false
  }
}

function copyToAll() {
  const first = rows.value.find((r) => r.enabled)
  if (!first) return
  rows.value = rows.value.map((r) => ({
    ...first,
    enabled: r.enabled || true
  }))
}
</script>

<template>
  <div>
    <div class="mb-5">
      <h1 class="text-xl font-extrabold sm:text-2xl">ساعات کاری</h1>
      <p class="mt-1 text-sm text-muted">برای هر آرایشگر روزها و ساعات فعالیت را تعیین کنید.</p>
    </div>

    <div v-if="!barbers.length && !loading" class="panel p-6 text-center">
      <EmptyState
        icon="i-lucide-users"
        title="آرایشگری وجود ندارد"
        description="ابتدا از بخش آرایشگران، آرایشگر اضافه کنید."
        action-label="افزودن آرایشگر"
        action-to="/panel/barbers"
      />
    </div>

    <template v-else>
      <div class="mb-4 flex flex-wrap items-end gap-3">
        <UFormField label="آرایشگر" class="min-w-56">
          <USelect
            v-model="selectedBarberId"
            :items="barbers.map((b) => ({ label: b.fullName, value: b.id }))"
            class="w-56"
          />
        </UFormField>
        <UButton variant="outline" color="neutral" icon="i-lucide-copy" class="mb-0.5" @click="copyToAll">
          کپی روز اول به همه
        </UButton>
      </div>

      <ErrorState v-if="error" :message="error" @retry="loadHours" />

      <div v-else-if="loading" class="space-y-2">
        <USkeleton v-for="i in 7" :key="i" class="h-16 w-full rounded-xl" />
      </div>

      <div v-else class="panel divide-y divide-gray-100 dark:divide-gray-800">
        <div
          v-for="(row, i) in rows"
          :key="i"
          class="grid items-center gap-3 p-4 sm:grid-cols-[140px_1fr]"
        >
          <div class="flex items-center gap-2">
            <USwitch v-model="row.enabled" size="sm" />
            <span class="text-sm font-medium" :class="!row.enabled ? 'text-muted' : ''">
              {{ IRANIAN_WEEKDAYS[i] }}
            </span>
          </div>

          <div
            class="grid gap-2 transition sm:grid-cols-4"
            :class="!row.enabled ? 'pointer-events-none opacity-40' : ''"
          >
            <UFormField label="شروع" size="xs">
              <UInput v-model="row.startTime" type="time" dir="ltr" lang="en-GB" class="w-full" />
            </UFormField>
            <UFormField label="پایان" size="xs">
              <UInput v-model="row.endTime" type="time" dir="ltr" lang="en-GB" class="w-full" />
            </UFormField>
            <UFormField label="شروع استراحت" size="xs">
              <UInput v-model="row.breakStart" type="time" dir="ltr" lang="en-GB" class="w-full" placeholder="—" />
            </UFormField>
            <UFormField label="پایان استراحت" size="xs">
              <UInput v-model="row.breakEnd" type="time" dir="ltr" lang="en-GB" class="w-full" placeholder="—" />
            </UFormField>
          </div>
        </div>
      </div>

      <div v-if="!loading && !error" class="mt-4 flex justify-end">
        <UButton :loading="saving" icon="i-lucide-save" @click="save">ذخیره ساعات کاری</UButton>
      </div>
    </template>
  </div>
</template>

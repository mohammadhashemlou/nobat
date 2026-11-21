<script setup lang="ts">
definePageMeta({ layout: 'panel', middleware: 'admin' })

useHead({ title: 'تعطیلات | پنل مدیریت' })

const api = useApi()
const { ensure, salonId } = useSalon()
const { formatLongDate, persianDigits } = useJalali()
const toast = useToast()

const loading = ref(true)
const error = ref<string | null>(null)
const holidays = ref<Holiday[]>([])
const saving = ref(false)
const removingId = ref<string | null>(null)

const selectedDate = ref<string | null>(null)
const description = ref('')

async function load() {
  loading.value = true
  error.value = null
  try {
    const id = salonId.value || (await ensure()).id
    holidays.value = await api<Holiday[]>('/api/v1/holidays', { query: { salonId: id } })
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری تعطیلات ناموفق بود'
  } finally {
    loading.value = false
  }
}

onMounted(load)

const disabledDates = computed(() => holidays.value.map((h) => h.date))

async function add() {
  if (!selectedDate.value) {
    toast.add({ title: 'یک تاریخ انتخاب کنید', color: 'warning', icon: 'i-lucide-alert-circle' })
    return
  }
  saving.value = true
  try {
    const id = salonId.value || (await ensure()).id
    await api('/api/v1/holidays', {
      method: 'POST',
      body: {
        salonId: id,
        date: selectedDate.value,
        description: description.value.trim() || null
      }
    })
    toast.add({ title: 'تعطیلی ثبت شد', color: 'success', icon: 'i-lucide-check' })
    selectedDate.value = null
    description.value = ''
    await load()
  } catch (e: any) {
    toast.add({ title: e?.message || 'ثبت ناموفق بود', color: 'error', icon: 'i-lucide-circle-alert' })
  } finally {
    saving.value = false
  }
}

async function remove(h: Holiday) {
  removingId.value = h.id
  try {
    await api(`/api/v1/holidays/${h.id}`, { method: 'DELETE' })
    toast.add({ title: 'تعطیلی حذف شد', color: 'success', icon: 'i-lucide-check' })
    await load()
  } catch (e: any) {
    toast.add({ title: e?.message || 'حذف ناموفق بود', color: 'error', icon: 'i-lucide-circle-alert' })
  } finally {
    removingId.value = null
  }
}
</script>

<template>
  <div>
    <div class="mb-5">
      <h1 class="text-xl font-extrabold sm:text-2xl">تعطیلات</h1>
      <p class="mt-1 text-sm text-muted">روزهایی که سالن تعطیل است را مشخص کنید.</p>
    </div>

    <div class="grid gap-5 lg:grid-cols-[380px_1fr]">
      <div class="space-y-3">
        <JalaliDatePicker v-model="selectedDate" :disabled-dates="disabledDates" />
        <div class="panel space-y-3 p-4">
          <UFormField label="توضیح (اختیاری)">
            <UInput v-model="description" class="w-full" placeholder="مثلاً تعطیلی رسمی" />
          </UFormField>
          <UButton block :loading="saving" icon="i-lucide-calendar-off" @click="add">
            ثبت تعطیلی
          </UButton>
        </div>
      </div>

      <div>
        <div v-if="loading" class="space-y-2">
          <USkeleton v-for="i in 4" :key="i" class="h-14 w-full rounded-xl" />
        </div>

        <ErrorState v-else-if="error" :message="error" @retry="load" />

        <EmptyState
          v-else-if="!holidays.length"
          icon="i-lucide-calendar-check"
          title="تعطیلی ثبت نشده"
          description="سالن در همه روزهای کاری باز است."
        />

        <div v-else class="panel divide-y divide-gray-100 dark:divide-gray-800">
          <div v-for="h in holidays" :key="h.id" class="flex items-center gap-3 p-4">
            <span class="grid size-10 place-items-center rounded-xl bg-rose-50 text-rose-600 dark:bg-rose-500/10 dark:text-rose-300">
              <UIcon name="i-lucide-calendar-off" class="size-5" />
            </span>
            <div class="min-w-0 flex-1">
              <p class="font-medium">{{ formatLongDate(h.date) }}</p>
              <p v-if="h.description" class="text-xs text-muted">{{ h.description }}</p>
            </div>
            <UButton
              variant="ghost"
              color="error"
              size="xs"
              icon="i-lucide-trash-2"
              :loading="removingId === h.id"
              @click="remove(h)"
            />
          </div>
        </div>

        <p class="mt-3 text-xs text-muted">
          {{ persianDigits(holidays.length) }} روز تعطیل ثبت شده است.
        </p>
      </div>
    </div>
  </div>
</template>

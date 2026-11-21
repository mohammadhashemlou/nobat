<script setup lang="ts">
definePageMeta({ layout: 'default' })

useHead({ title: 'رزرو نوبت | نوبت' })

const config = useRuntimeConfig()
const api = useApi()
const auth = useAuthStore()
const { formatLongDate, formatTime, persianDigits, todayApiDate, formatFullDate } = useJalali()

const slug = config.public.salonSlug

// ---- State ----------------------------------------------------------------
const salon = ref<PublicSalon | null>(null)
const loadingSalon = ref(true)
const salonError = ref<string | null>(null)

const step = ref(1)
const selectedService = ref<PublicService | null>(null)
const selectedBarber = ref<PublicBarber | null>(null)
const selectedDate = ref<string>(todayApiDate())
const selectedSlot = ref<TimeSlot | null>(null)

const availability = ref<Availability | null>(null)
const loadingSlots = ref(false)
const slotError = ref<string | null>(null)

const notes = ref('')
const booking = ref(false)
const bookingError = ref<string | null>(null)
const created = ref<Appointment | null>(null)

// ---- Derived -------------------------------------------------------------
const eligibleBarbers = computed(() => {
  if (!salon.value || !selectedService.value) return []
  return salon.value.barbers.filter((b) => b.serviceIds.includes(selectedService.value!.id))
})

const availableWeekdays = computed<number[]>(() => []) // server decides per-day availability

const canGoStep2 = computed(() => !!selectedService.value)
const canGoStep3 = computed(() => !!selectedService.value && !!selectedBarber.value)
const canConfirm = computed(() => !!selectedSlot.value)

const priceLabel = (p: number) => `${persianDigits(p.toLocaleString('en-US'))} تومان`

// ---- Load salon ----------------------------------------------------------
async function loadSalon() {
  loadingSalon.value = true
  salonError.value = null
  try {
    salon.value = await api<PublicSalon>(`/api/v1/salons/${slug}`, { skipAuth: true })
  } catch (e: any) {
    salonError.value = e?.message || 'بارگذاری اطلاعات سالن ناموفق بود'
  } finally {
    loadingSalon.value = false
  }
}

onMounted(loadSalon)

// ---- Availability --------------------------------------------------------
async function loadSlots() {
  if (!selectedBarber.value || !selectedService.value) return
  loadingSlots.value = true
  slotError.value = null
  availability.value = null
  selectedSlot.value = null
  try {
    const q = new URLSearchParams({
      barberId: selectedBarber.value.id,
      serviceId: selectedService.value.id,
      date: selectedDate.value
    })
    availability.value = await api<Availability>(`/api/v1/appointments/availability?${q}`, {
      skipAuth: true
    })
  } catch (e: any) {
    slotError.value = e?.message || 'دریافت زمان‌های آزاد ناموفق بود'
  } finally {
    loadingSlots.value = false
  }
}

watch([selectedDate, selectedBarber, selectedService], () => {
  if (step.value >= 3 && selectedBarber.value && selectedService.value) loadSlots()
})

function goStep(n: number) {
  if (n === 2 && !canGoStep2.value) return
  if (n === 3 && !canGoStep3.value) return
  step.value = n
  if (n === 3) loadSlots()
}

function pickService(s: PublicService) {
  selectedService.value = s
  // Reset dependent selections
  selectedBarber.value = null
  selectedSlot.value = null
  availability.value = null
  step.value = 2
}

function pickBarber(b: PublicBarber) {
  selectedBarber.value = b
  selectedSlot.value = null
  availability.value = null
  step.value = 3
  loadSlots()
}

function pickSlot(slot: TimeSlot) {
  selectedSlot.value = slot
}

// ---- Book ----------------------------------------------------------------
async function confirmBooking() {
  if (!selectedSlot.value || !selectedBarber.value || !selectedService.value) return

  if (!auth.isAuthenticated) {
    await navigateTo({ path: '/login', query: { redirect: '/booking' } })
    return
  }

  booking.value = true
  bookingError.value = null
  try {
    created.value = await api<Appointment>('/api/v1/appointments', {
      method: 'POST',
      body: {
        barberId: selectedBarber.value.id,
        serviceId: selectedService.value.id,
        startAtUtc: selectedSlot.value.startUtc,
        notes: notes.value.trim() || null
      }
    })
    step.value = 4
  } catch (e: any) {
    bookingError.value = e?.message || 'ثبت نوبت ناموفق بود'
    // Slot likely taken — refresh availability
    if (e?.status === 409) {
      await loadSlots()
      selectedSlot.value = null
    }
  } finally {
    booking.value = false
  }
}

function resetFlow() {
  step.value = 1
  selectedService.value = null
  selectedBarber.value = null
  selectedSlot.value = null
  availability.value = null
  notes.value = ''
  created.value = null
  bookingError.value = null
}
</script>

<template>
  <div class="mx-auto w-full max-w-5xl px-4 py-8 sm:px-6 sm:py-12">
    <div class="mb-6">
      <h1 class="text-2xl font-extrabold sm:text-3xl">رزرو نوبت</h1>
      <p class="mt-1.5 text-sm text-muted">
        خدمت، آرایشگر و زمان دلخواه را انتخاب کنید.
      </p>
    </div>

    <!-- Loading salon -->
    <div v-if="loadingSalon" class="space-y-3">
      <USkeleton class="h-10 w-64" />
      <USkeleton class="h-40 w-full rounded-2xl" />
      <USkeleton class="h-40 w-full rounded-2xl" />
    </div>

    <ErrorState
      v-else-if="salonError"
      title="بارگذاری ناموفق"
      :message="salonError"
      @retry="loadSalon"
    />

    <template v-else-if="salon">
      <!-- Stepper -->
      <ol class="mb-6 grid grid-cols-4 gap-2 text-center text-xs sm:text-sm">
        <li
          v-for="(label, i) in ['خدمت', 'آرایشگر', 'زمان', 'تأیید']"
          :key="label"
          class="flex flex-col items-center gap-1.5"
        >
          <span
            class="grid size-8 place-items-center rounded-full font-bold transition"
            :class="step > i + 1
              ? 'bg-emerald-500 text-white'
              : step === i + 1
                ? 'bg-brand-600 text-white dark:bg-brand-500'
                : 'bg-gray-200 text-gray-500 dark:bg-gray-800 dark:text-gray-400'"
          >
            <UIcon v-if="step > i + 1" name="i-lucide-check" class="size-4" />
            <template v-else>{{ persianDigits(i + 1) }}</template>
          </span>
          <span :class="step === i + 1 ? 'font-bold' : 'text-muted'">{{ label }}</span>
        </li>
      </ol>

      <!-- STEP 1: service -->
      <section v-if="step === 1">
        <EmptyState
          v-if="!salon.services.length"
          icon="i-lucide-scissors"
          title="خدمتی ثبت نشده"
          description="هنوز خدمتی برای این سالن ثبت نشده است."
        />

        <div v-else class="grid gap-3 sm:grid-cols-2">
          <button
            v-for="s in salon.services"
            :key="s.id"
            type="button"
            class="panel flex items-start gap-4 p-4 text-start transition hover:-translate-y-0.5 hover:shadow-md"
            @click="pickService(s)"
          >
            <span class="grid size-11 shrink-0 place-items-center rounded-xl bg-brand-50 text-brand-600 dark:bg-brand-500/10 dark:text-brand-300">
              <UIcon name="i-lucide-scissors" class="size-5" />
            </span>
            <span class="min-w-0 flex-1">
              <span class="flex items-center justify-between gap-2">
                <span class="font-bold">{{ s.name }}</span>
                <span class="shrink-0 text-sm font-semibold text-brand-600 dark:text-brand-400">
                  {{ priceLabel(s.price) }}
                </span>
              </span>
              <span v-if="s.description" class="mt-1 block line-clamp-2 text-xs text-muted">
                {{ s.description }}
              </span>
              <span class="mt-2 inline-flex items-center gap-1 text-xs text-muted">
                <UIcon name="i-lucide-clock" class="size-3.5" />
                {{ persianDigits(s.durationMinutes) }} دقیقه
              </span>
            </span>
          </button>
        </div>
      </section>

      <!-- STEP 2: barber -->
      <section v-else-if="step === 2">
        <div class="mb-4 flex items-center justify-between">
          <p class="text-sm text-muted">
            خدمت انتخابی:
            <span class="font-semibold text-gray-900 dark:text-gray-100">{{ selectedService?.name }}</span>
          </p>
          <UButton variant="ghost" color="neutral" size="sm" icon="i-lucide-arrow-right" @click="goStep(1)">
            تغییر خدمت
          </UButton>
        </div>

        <EmptyState
          v-if="!eligibleBarbers.length"
          icon="i-lucide-user-x"
          title="آرایشگری برای این خدمت نیست"
          description="در حال حاضر هیچ آرایشگری این خدمت را ارائه نمی‌دهد."
        />

        <div v-else class="grid gap-3 sm:grid-cols-2">
          <button
            v-for="b in eligibleBarbers"
            :key="b.id"
            type="button"
            class="panel flex items-center gap-4 p-4 text-start transition hover:-translate-y-0.5 hover:shadow-md"
            @click="pickBarber(b)"
          >
            <UAvatar :alt="b.fullName" :src="b.avatarUrl || undefined" size="lg" />
            <span class="min-w-0 flex-1">
              <span class="block font-bold">{{ b.fullName }}</span>
              <span v-if="b.bio" class="mt-0.5 block line-clamp-2 text-xs text-muted">{{ b.bio }}</span>
            </span>
            <UIcon name="i-lucide-chevron-left" class="size-5 shrink-0 text-gray-400" />
          </button>
        </div>
      </section>

      <!-- STEP 3: date & time -->
      <section v-else-if="step === 3">
        <div class="mb-4 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted">
          <span>
            خدمت: <span class="font-semibold text-gray-900 dark:text-gray-100">{{ selectedService?.name }}</span>
          </span>
          <span>
            آرایشگر: <span class="font-semibold text-gray-900 dark:text-gray-100">{{ selectedBarber?.fullName }}</span>
          </span>
          <UButton variant="ghost" color="neutral" size="sm" icon="i-lucide-arrow-right" @click="goStep(2)">
            تغییر
          </UButton>
        </div>

        <div class="grid gap-4 lg:grid-cols-[380px_1fr]">
          <JalaliDatePicker v-model="selectedDate" :available-weekdays="availableWeekdays" />

          <div class="panel p-4">
            <div class="mb-3 flex items-center justify-between">
              <p class="text-sm font-bold">{{ formatLongDate(selectedDate) }}</p>
              <UBadge v-if="availability?.isHoliday" color="error" variant="subtle" size="sm">
                تعطیل
              </UBadge>
            </div>

            <div v-if="loadingSlots" class="grid grid-cols-3 gap-2 sm:grid-cols-4">
              <USkeleton v-for="i in 8" :key="i" class="h-10 rounded-xl" />
            </div>

            <ErrorState
              v-else-if="slotError"
              title="دریافت زمان‌ها ناموفق"
              :message="slotError"
              @retry="loadSlots"
            />

            <EmptyState
              v-else-if="availability?.isHoliday"
              icon="i-lucide-calendar-off"
              title="این روز تعطیل است"
              :description="availability.holidayDescription || 'سالن در این تاریخ تعطیل است.'"
            />

            <EmptyState
              v-else-if="!availability?.isWorkingDay"
              icon="i-lucide-moon"
              title="روز غیرکاری"
              description="آرایشگر در این روز فعالیت ندارد. روز دیگری را انتخاب کنید."
            />

            <EmptyState
              v-else-if="!availability?.slots.length"
              icon="i-lucide-clock-alert"
              title="زمان آزادی نیست"
              description="برای این روز زمان خالی باقی نمانده است. روز دیگری را امتحان کنید."
            />

            <div v-else class="grid grid-cols-3 gap-2 sm:grid-cols-4">
              <button
                v-for="slot in availability.slots"
                :key="slot.startUtc"
                type="button"
                class="slot-btn"
                :class="{ 'slot-btn--active': selectedSlot?.startUtc === slot.startUtc }"
                @click="pickSlot(slot)"
              >
                {{ formatTime(slot.startUtc) }}
              </button>
            </div>
          </div>
        </div>

        <!-- Confirm bar -->
        <div
          v-if="selectedSlot"
          class="sticky bottom-4 z-20 mt-4 flex flex-wrap items-center gap-3 rounded-2xl border
                 border-brand-200 bg-white p-4 shadow-lg dark:border-brand-800 dark:bg-gray-900"
        >
          <div class="flex items-center gap-2 text-sm">
            <UIcon name="i-lucide-calendar-check" class="size-5 text-brand-600 dark:text-brand-400" />
            <span class="font-semibold">{{ formatFullDate(selectedDate) }}</span>
            <span class="text-muted">— ساعت {{ formatTime(selectedSlot.startUtc) }}</span>
          </div>
          <UButton class="ms-auto" icon="i-lucide-arrow-left" @click="step = 4">
            ادامه
          </UButton>
        </div>
      </section>

      <!-- STEP 4: confirm -->
      <section v-else-if="step === 4">
        <div v-if="!created" class="grid gap-4 lg:grid-cols-[1fr_340px]">
          <div class="panel space-y-4 p-5">
            <h2 class="text-lg font-bold">تأیید نهایی</h2>

            <UAlert
              v-if="bookingError"
              color="error"
              variant="subtle"
              icon="i-lucide-circle-alert"
              :title="bookingError"
            />

            <dl class="divide-y divide-gray-100 text-sm dark:divide-gray-800">
              <div class="flex justify-between py-2.5">
                <dt class="text-muted">خدمت</dt>
                <dd class="font-semibold">{{ selectedService?.name }}</dd>
              </div>
              <div class="flex justify-between py-2.5">
                <dt class="text-muted">آرایشگر</dt>
                <dd class="font-semibold">{{ selectedBarber?.fullName }}</dd>
              </div>
              <div class="flex justify-between py-2.5">
                <dt class="text-muted">تاریخ</dt>
                <dd class="font-semibold">{{ formatFullDate(selectedDate) }}</dd>
              </div>
              <div class="flex justify-between py-2.5">
                <dt class="text-muted">ساعت</dt>
                <dd class="font-semibold tabular-nums">
                  {{ selectedSlot ? formatTime(selectedSlot.startUtc) : '-' }}
                  تا
                  {{ selectedSlot ? formatTime(selectedSlot.endUtc) : '-' }}
                </dd>
              </div>
              <div class="flex justify-between py-2.5">
                <dt class="text-muted">مدت</dt>
                <dd class="font-semibold">{{ persianDigits(selectedService?.durationMinutes || 0) }} دقیقه</dd>
              </div>
              <div class="flex justify-between py-2.5">
                <dt class="text-muted">مبلغ</dt>
                <dd class="font-bold text-brand-600 dark:text-brand-400">
                  {{ priceLabel(selectedService?.price || 0) }}
                </dd>
              </div>
            </dl>

            <UFormField label="یادداشت (اختیاری)">
              <UTextarea v-model="notes" :rows="3" placeholder="توضیحاتی برای آرایشگر..." class="w-full" />
            </UFormField>

            <div v-if="!auth.isAuthenticated" class="rounded-xl bg-amber-50 p-3 text-sm text-amber-800 dark:bg-amber-950/40 dark:text-amber-200">
              برای ثبت نوبت باید وارد حساب خود شوید.
            </div>

            <div class="flex gap-2">
              <UButton variant="ghost" color="neutral" icon="i-lucide-arrow-right" @click="step = 3">
                بازگشت
              </UButton>
              <UButton
                class="ms-auto"
                size="lg"
                icon="i-lucide-check"
                :loading="booking"
                :disabled="!canConfirm"
                @click="confirmBooking"
              >
                {{ auth.isAuthenticated ? 'ثبت نوبت' : 'ورود و ثبت نوبت' }}
              </UButton>
            </div>
          </div>

          <aside class="panel h-fit p-5">
            <h3 class="text-sm font-bold">خلاصه رزرو</h3>
            <div class="mt-3 space-y-2 text-sm">
              <p class="flex items-center gap-2 text-muted">
                <UIcon name="i-lucide-store" class="size-4" /> {{ salon.name }}
              </p>
              <p class="flex items-center gap-2 text-muted">
                <UIcon name="i-lucide-map-pin" class="size-4" /> {{ salon.address || '—' }}
              </p>
              <p class="flex items-center gap-2 text-muted">
                <UIcon name="i-lucide-phone" class="size-4" /> {{ salon.phone || '—' }}
              </p>
            </div>
            <p class="mt-4 rounded-xl bg-brand-50 p-3 text-xs leading-relaxed text-brand-800 dark:bg-brand-500/10 dark:text-brand-200">
              با ثبت نوبت، زمان انتخابی برای شما رزرو می‌شود. در صورت نیاز می‌توانید از بخش
              «نوبت‌های من» آن را لغو کنید.
            </p>
          </aside>
        </div>

        <!-- Success -->
        <div v-else class="mx-auto max-w-lg">
          <div class="panel p-8 text-center">
            <span class="mx-auto grid size-16 place-items-center rounded-full bg-emerald-100 dark:bg-emerald-500/15">
              <UIcon name="i-lucide-check" class="size-9 text-emerald-600 dark:text-emerald-400" />
            </span>
            <h2 class="mt-5 text-xl font-extrabold">نوبت شما ثبت شد!</h2>
            <p class="mt-2 text-sm text-muted">
              {{ created.barberName }} — {{ created.serviceName }}
            </p>
            <div class="mt-4 rounded-xl bg-gray-50 p-4 text-sm dark:bg-gray-800/60">
              <p class="font-semibold">{{ formatFullDate(created.startAt) }}</p>
              <p class="mt-1 text-muted">
                ساعت {{ formatTime(created.startAt) }} تا {{ formatTime(created.endAt) }}
              </p>
            </div>
            <div class="mt-6 flex flex-col gap-2 sm:flex-row">
              <UButton to="/my" block icon="i-lucide-list">مشاهده نوبت‌های من</UButton>
              <UButton variant="outline" color="neutral" block @click="resetFlow">
                رزرو نوبت دیگر
              </UButton>
            </div>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

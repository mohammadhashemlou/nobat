<script setup lang="ts">
definePageMeta({ layout: 'dashboard', middleware: 'admin' })

useHead({ title: 'آرایشگران | پنل مدیریت' })

const api = useApi()
const { ensure, salonId } = useSalon()
const { persianDigits } = useJalali()
const toast = useToast()

const loading = ref(true)
const error = ref<string | null>(null)
const barbers = ref<BarberAdmin[]>([])
const services = ref<ServiceAdmin[]>([])
const saving = ref(false)

const modalOpen = ref(false)
const editing = ref<BarberAdmin | null>(null)
const formError = ref<string | null>(null)

const form = reactive({
  fullName: '',
  email: '',
  password: '',
  phoneNumber: '',
  bio: '',
  isActive: true,
  serviceIds: [] as string[]
})

async function load() {
  loading.value = true
  error.value = null
  try {
    const id = salonId.value || (await ensure()).id
    const [b, s] = await Promise.all([
      api<BarberAdmin[]>('/api/v1/barbers', { query: { salonId: id, includeInactive: true } }),
      api<ServiceAdmin[]>('/api/v1/services', { query: { salonId: id, includeInactive: true } })
    ])
    barbers.value = b
    services.value = s
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری ناموفق بود'
  } finally {
    loading.value = false
  }
}

onMounted(load)

function openCreate() {
  editing.value = null
  Object.assign(form, {
    fullName: '',
    email: '',
    password: '',
    phoneNumber: '',
    bio: '',
    isActive: true,
    serviceIds: []
  })
  formError.value = null
  modalOpen.value = true
}

function openEdit(b: BarberAdmin) {
  editing.value = b
  Object.assign(form, {
    fullName: b.fullName,
    email: b.email || '',
    password: '',
    phoneNumber: b.phoneNumber || '',
    bio: b.bio || '',
    isActive: b.isActive,
    serviceIds: [...b.serviceIds]
  })
  formError.value = null
  modalOpen.value = true
}

function toggleService(id: string) {
  const i = form.serviceIds.indexOf(id)
  if (i >= 0) form.serviceIds.splice(i, 1)
  else form.serviceIds.push(id)
}

async function save() {
  formError.value = null
  if (!form.fullName.trim()) {
    formError.value = 'نام آرایشگر الزامی است'
    return
  }

  saving.value = true
  try {
    const id = salonId.value || (await ensure()).id
    if (editing.value) {
      await api(`/api/v1/barbers/${editing.value.id}`, {
        method: 'PUT',
        body: {
          fullName: form.fullName.trim(),
          bio: form.bio.trim() || null,
          avatarUrl: editing.value.avatarUrl,
          isActive: form.isActive,
          serviceIds: form.serviceIds
        }
      })
      toast.add({ title: 'آرایشگر به‌روزرسانی شد', color: 'success', icon: 'i-lucide-check' })
    } else {
      if (!form.email.trim() || !form.password) {
        formError.value = 'ایمیل و رمز عبور برای ساخت حساب آرایشگر الزامی است'
        saving.value = false
        return
      }
      await api('/api/v1/barbers', {
        method: 'POST',
        body: {
          salonId: id,
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          password: form.password,
          phoneNumber: form.phoneNumber.trim() || null,
          bio: form.bio.trim() || null,
          avatarUrl: null,
          serviceIds: form.serviceIds
        }
      })
      toast.add({ title: 'آرایشگر ایجاد شد', color: 'success', icon: 'i-lucide-check' })
    }
    modalOpen.value = false
    await load()
  } catch (e: any) {
    formError.value = e?.message || 'ذخیره ناموفق بود'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <div class="mb-5 flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-extrabold sm:text-2xl">آرایشگران</h1>
        <p class="mt-1 text-sm text-muted">آرایشگران و خدمات هرکدام را مدیریت کنید.</p>
      </div>
      <UButton icon="i-lucide-plus" @click="openCreate">آرایشگر جدید</UButton>
    </div>

    <div v-if="loading" class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <USkeleton v-for="i in 3" :key="i" class="h-28 rounded-2xl" />
    </div>

    <ErrorState v-else-if="error" :message="error" @retry="load" />

    <EmptyState
      v-else-if="!barbers.length"
      icon="i-lucide-users"
      title="آرایشگری ثبت نشده"
      description="اولین آرایشگر سالن را اضافه کنید."
    >
      <UButton class="mt-4" icon="i-lucide-plus" @click="openCreate">افزودن آرایشگر</UButton>
    </EmptyState>

    <div v-else class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <article v-for="b in barbers" :key="b.id" class="panel p-4">
        <div class="flex items-center gap-3">
          <UAvatar :alt="b.fullName" :src="b.avatarUrl || undefined" size="lg" />
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-1.5">
              <h2 class="truncate font-bold">{{ b.fullName }}</h2>
              <UBadge v-if="!b.isActive" color="neutral" variant="subtle" size="sm">غیرفعال</UBadge>
            </div>
            <p v-if="b.email" dir="ltr" class="truncate text-xs text-muted">{{ b.email }}</p>
          </div>
        </div>

        <p v-if="b.bio" class="mt-2 line-clamp-2 text-xs text-muted">{{ b.bio }}</p>

        <p class="mt-3 flex items-center gap-1 text-xs text-muted">
          <UIcon name="i-lucide-scissors" class="size-3.5" />
          {{ persianDigits(b.serviceIds.length) }} خدمت
        </p>

        <div class="mt-3 flex gap-2 border-t border-gray-100 pt-3 dark:border-gray-800">
          <UButton size="xs" variant="soft" color="neutral" icon="i-lucide-pencil" @click="openEdit(b)">
            ویرایش
          </UButton>
          <UButton
            size="xs"
            variant="ghost"
            color="neutral"
            icon="i-lucide-clock"
            :to="`/dashboard/schedule?barber=${b.id}`"
          >
            ساعات کاری
          </UButton>
        </div>
      </article>
    </div>

    <!-- Modal -->
    <UModal v-model:open="modalOpen" :title="editing ? 'ویرایش آرایشگر' : 'آرایشگر جدید'">
      <template #body>
        <form class="space-y-4" @submit.prevent="save">
          <UAlert
            v-if="formError"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="formError"
          />

          <UFormField label="نام و نام خانوادگی" required>
            <UInput v-model="form.fullName" class="w-full" placeholder="مثلاً رضا محمدی" />
          </UFormField>

          <template v-if="!editing">
            <UFormField label="ایمیل (برای ورود آرایشگر)" required>
              <UInput v-model="form.email" type="email" dir="ltr" class="w-full" placeholder="barber@example.com" />
            </UFormField>
            <UFormField label="رمز عبور" required>
              <UInput v-model="form.password" type="password" class="w-full" placeholder="حداقل ۸ کاراکتر شامل حرف بزرگ و عدد" />
            </UFormField>
            <UFormField label="شماره موبایل">
              <UInput v-model="form.phoneNumber" dir="ltr" class="w-full" placeholder="09xxxxxxxxx" />
            </UFormField>
          </template>

          <UFormField label="بیوگرافی">
            <UTextarea v-model="form.bio" :rows="2" class="w-full" placeholder="اختیاری" />
          </UFormField>

          <UFormField label="خدمات قابل ارائه">
            <div v-if="!services.length" class="text-xs text-muted">
              ابتدا از بخش «خدمات» خدمت اضافه کنید.
            </div>
            <div v-else class="flex flex-wrap gap-1.5">
              <UButton
                v-for="s in services"
                :key="s.id"
                size="xs"
                :variant="form.serviceIds.includes(s.id) ? 'solid' : 'outline'"
                :color="form.serviceIds.includes(s.id) ? 'primary' : 'neutral'"
                @click="toggleService(s.id)"
              >
                {{ s.name }}
              </UButton>
            </div>
          </UFormField>

          <UFormField v-if="editing" label="وضعیت">
            <USwitch v-model="form.isActive" label="فعال باشد" />
          </UFormField>
        </form>
      </template>

      <template #footer>
        <div class="flex w-full justify-end gap-2">
          <UButton variant="ghost" color="neutral" @click="modalOpen = false">انصراف</UButton>
          <UButton :loading="saving" icon="i-lucide-save" @click="save">ذخیره</UButton>
        </div>
      </template>
    </UModal>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ layout: 'panel', middleware: 'admin' })

useHead({ title: 'خدمات | پنل مدیریت' })

const api = useApi()
const { ensure, salonId } = useSalon()
const { persianDigits } = useJalali()
const toast = useToast()

const loading = ref(true)
const error = ref<string | null>(null)
const services = ref<ServiceAdmin[]>([])
const saving = ref(false)
const busyId = ref<string | null>(null)

const modalOpen = ref(false)
const editing = ref<ServiceAdmin | null>(null)
const form = reactive({
  name: '',
  description: '',
  durationMinutes: 30,
  price: 0,
  isActive: true
})
const formError = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    const id = salonId.value || (await ensure()).id
    services.value = await api<ServiceAdmin[]>('/api/v1/services', {
      query: { salonId: id, includeInactive: true }
    })
  } catch (e: any) {
    error.value = e?.message || 'بارگذاری خدمات ناموفق بود'
  } finally {
    loading.value = false
  }
}

onMounted(load)

function openCreate() {
  editing.value = null
  form.name = ''
  form.description = ''
  form.durationMinutes = 30
  form.price = 0
  form.isActive = true
  formError.value = null
  modalOpen.value = true
}

function openEdit(s: ServiceAdmin) {
  editing.value = s
  form.name = s.name
  form.description = s.description || ''
  form.durationMinutes = s.durationMinutes
  form.price = s.price
  form.isActive = s.isActive
  formError.value = null
  modalOpen.value = true
}

async function save() {
  formError.value = null
  if (!form.name.trim()) {
    formError.value = 'نام خدمت الزامی است'
    return
  }
  if (form.durationMinutes <= 0) {
    formError.value = 'مدت زمان باید بیشتر از صفر باشد'
    return
  }

  saving.value = true
  try {
    const id = salonId.value || (await ensure()).id
    if (editing.value) {
      await api(`/api/v1/services/${editing.value.id}`, {
        method: 'PUT',
        body: {
          name: form.name.trim(),
          description: form.description.trim() || null,
          durationMinutes: form.durationMinutes,
          price: form.price,
          isActive: form.isActive,
          barberIds: editing.value.barberIds
        }
      })
      toast.add({ title: 'خدمت به‌روزرسانی شد', color: 'success', icon: 'i-lucide-check' })
    } else {
      await api('/api/v1/services', {
        method: 'POST',
        body: {
          salonId: id,
          name: form.name.trim(),
          description: form.description.trim() || null,
          durationMinutes: form.durationMinutes,
          price: form.price
        }
      })
      toast.add({ title: 'خدمت ایجاد شد', color: 'success', icon: 'i-lucide-check' })
    }
    modalOpen.value = false
    await load()
  } catch (e: any) {
    formError.value = e?.message || 'ذخیره ناموفق بود'
  } finally {
    saving.value = false
  }
}

async function remove(s: ServiceAdmin) {
  busyId.value = s.id
  try {
    await api(`/api/v1/services/${s.id}`, { method: 'DELETE' })
    toast.add({ title: 'خدمت غیرفعال شد', color: 'success', icon: 'i-lucide-check' })
    await load()
  } catch (e: any) {
    toast.add({ title: e?.message || 'حذف ناموفق بود', color: 'error', icon: 'i-lucide-circle-alert' })
  } finally {
    busyId.value = null
  }
}

const priceLabel = (p: number) => `${persianDigits(p.toLocaleString('en-US'))} تومان`
</script>

<template>
  <div>
    <div class="mb-5 flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-extrabold sm:text-2xl">خدمات</h1>
        <p class="mt-1 text-sm text-muted">خدمات سالن، مدت زمان و قیمت‌ها را مدیریت کنید.</p>
      </div>
      <UButton icon="i-lucide-plus" @click="openCreate">خدمت جدید</UButton>
    </div>

    <div v-if="loading" class="space-y-2">
      <USkeleton v-for="i in 4" :key="i" class="h-16 w-full rounded-xl" />
    </div>

    <ErrorState v-else-if="error" :message="error" @retry="load" />

    <EmptyState
      v-else-if="!services.length"
      icon="i-lucide-scissors"
      title="خدمتی ثبت نشده"
      description="اولین خدمت سالن را اضافه کنید."
    >
      <UButton class="mt-4" icon="i-lucide-plus" @click="openCreate">افزودن خدمت</UButton>
    </EmptyState>

    <div v-else class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <article v-for="s in services" :key="s.id" class="panel flex flex-col p-4">
        <div class="flex items-start justify-between gap-2">
          <h2 class="font-bold">{{ s.name }}</h2>
          <UBadge :color="s.isActive ? 'success' : 'neutral'" variant="subtle" size="sm">
            {{ s.isActive ? 'فعال' : 'غیرفعال' }}
          </UBadge>
        </div>
        <p v-if="s.description" class="mt-1 line-clamp-2 text-xs text-muted">{{ s.description }}</p>

        <div class="mt-3 flex items-center gap-3 text-xs text-muted">
          <span class="flex items-center gap-1">
            <UIcon name="i-lucide-clock" class="size-3.5" />
            {{ persianDigits(s.durationMinutes) }} دقیقه
          </span>
          <span class="flex items-center gap-1 font-semibold text-brand-600 dark:text-brand-400">
            <UIcon name="i-lucide-banknote" class="size-3.5" />
            {{ priceLabel(s.price) }}
          </span>
        </div>

        <div class="mt-4 flex gap-2 border-t border-gray-100 pt-3 dark:border-gray-800">
          <UButton size="xs" variant="soft" color="neutral" icon="i-lucide-pencil" @click="openEdit(s)">
            ویرایش
          </UButton>
          <UButton
            v-if="s.isActive"
            size="xs"
            variant="ghost"
            color="error"
            icon="i-lucide-trash-2"
            :loading="busyId === s.id"
            @click="remove(s)"
          >
            غیرفعال
          </UButton>
        </div>
      </article>
    </div>

    <!-- Modal -->
    <UModal v-model:open="modalOpen" :title="editing ? 'ویرایش خدمت' : 'خدمت جدید'">
      <template #body>
        <form class="space-y-4" @submit.prevent="save">
          <UAlert
            v-if="formError"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="formError"
          />

          <UFormField label="نام خدمت" required>
            <UInput v-model="form.name" placeholder="مثلاً اصلاح مو" class="w-full" />
          </UFormField>

          <UFormField label="توضیحات">
            <UTextarea v-model="form.description" :rows="2" class="w-full" placeholder="اختیاری" />
          </UFormField>

          <div class="grid grid-cols-2 gap-3">
            <UFormField label="مدت (دقیقه)" required>
              <UInput v-model.number="form.durationMinutes" type="number" min="5" step="5" class="w-full" />
            </UFormField>
            <UFormField label="قیمت (تومان)" required>
              <UInput v-model.number="form.price" type="number" min="0" step="1000" class="w-full" />
            </UFormField>
          </div>

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

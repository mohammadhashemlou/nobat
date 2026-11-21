<script setup lang="ts">
definePageMeta({ layout: 'default' })

useHead({ title: 'ثبت‌نام | نوبت' })

const auth = useAuthStore()

const form = reactive({
  fullName: '',
  phoneNumber: '',
  password: '',
  confirmPassword: '',
  role: 'Customer' as 'Customer' | 'Barber'
})

const loading = ref(false)
const error = ref<string | null>(null)
const fieldErrors = ref<Record<string, string[]>>({})
const success = ref(false)

const passwordHint = computed(() => ({
  length: form.password.length >= 8,
  upper: /[A-Z]/.test(form.password),
  lower: /[a-z]/.test(form.password),
  digit: /[0-9]/.test(form.password)
}))

const passwordValid = computed(() => Object.values(passwordHint.value).every(Boolean))
const phoneValid = computed(() => isValidMobile(form.phoneNumber))

const canSubmit = computed(
  () =>
    form.fullName.trim().length > 1 &&
    phoneValid.value &&
    passwordValid.value &&
    form.password === form.confirmPassword
)

async function submit() {
  if (!canSubmit.value) return

  error.value = null
  fieldErrors.value = {}
  loading.value = true

  try {
    await auth.register({
      fullName: form.fullName.trim(),
      phoneNumber: form.phoneNumber,
      password: form.password,
      confirmPassword: form.confirmPassword,
      role: form.role
    })

    success.value = true
    await auth.login(form.phoneNumber, form.password)
    await navigateTo(form.role === 'Barber' ? '/panel' : '/my')
  } catch (e: any) {
    error.value = e?.message || 'ثبت‌نام ناموفق بود'
    if (e?.details?.errors) fieldErrors.value = e.details.errors
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="mx-auto flex w-full max-w-md flex-col justify-center px-4 py-14 sm:py-20">
    <div class="mb-7 text-center">
      <span class="mx-auto grid size-12 place-items-center rounded-2xl bg-brand-600 text-white dark:bg-brand-500">
        <UIcon name="i-lucide-user-plus" class="size-6" />
      </span>
      <h1 class="mt-4 text-2xl font-extrabold">ساخت حساب کاربری</h1>
      <p class="mt-1.5 text-sm text-muted">در چند ثانیه ثبت‌نام کنید و نوبت بگیرید.</p>
    </div>

    <form class="panel space-y-4 p-6" @submit.prevent="submit">
      <UAlert
        v-if="error"
        color="error"
        variant="subtle"
        icon="i-lucide-circle-alert"
        :title="error"
      />
      <UAlert
        v-if="success"
        color="success"
        variant="subtle"
        icon="i-lucide-check-circle"
        title="ثبت‌نام موفق! در حال انتقال..."
      />

      <UFormField label="نام و نام خانوادگی" :error="fieldErrors.FullName?.[0]" required>
        <UInput v-model="form.fullName" placeholder="مثلاً علی رضایی" icon="i-lucide-user" size="lg" class="w-full" />
      </UFormField>

      <UFormField
        label="شماره موبایل"
        :error="fieldErrors.PhoneNumber?.[0] || (form.phoneNumber && !phoneValid ? 'شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود' : undefined)"
        required
      >
        <PhoneInput v-model="form.phoneNumber" size="lg" />
      </UFormField>

      <UFormField label="رمز عبور" :error="fieldErrors.Password?.[0]" required>
        <PasswordInput v-model="form.password" autocomplete="new-password" />
      </UFormField>

      <div v-if="form.password" class="grid grid-cols-2 gap-1.5 text-xs">
        <span :class="passwordHint.length ? 'text-emerald-600 dark:text-emerald-400' : 'text-muted'">
          <UIcon :name="passwordHint.length ? 'i-lucide-check' : 'i-lucide-x'" class="size-3.5" /> حداقل ۸ کاراکتر
        </span>
        <span :class="passwordHint.upper ? 'text-emerald-600 dark:text-emerald-400' : 'text-muted'">
          <UIcon :name="passwordHint.upper ? 'i-lucide-check' : 'i-lucide-x'" class="size-3.5" /> حرف بزرگ
        </span>
        <span :class="passwordHint.lower ? 'text-emerald-600 dark:text-emerald-400' : 'text-muted'">
          <UIcon :name="passwordHint.lower ? 'i-lucide-check' : 'i-lucide-x'" class="size-3.5" /> حرف کوچک
        </span>
        <span :class="passwordHint.digit ? 'text-emerald-600 dark:text-emerald-400' : 'text-muted'">
          <UIcon :name="passwordHint.digit ? 'i-lucide-check' : 'i-lucide-x'" class="size-3.5" /> عدد
        </span>
      </div>

      <UFormField
        label="تکرار رمز عبور"
        :error="form.confirmPassword && form.password !== form.confirmPassword ? 'رمزها مطابقت ندارند' : undefined"
        required
      >
        <PasswordInput v-model="form.confirmPassword" autocomplete="new-password" />
      </UFormField>

      <UButton type="submit" size="lg" block :loading="loading" :disabled="!canSubmit">
        ثبت‌نام
      </UButton>

      <p class="pt-1 text-center text-sm text-muted">
        حساب دارید؟
        <NuxtLink to="/login" class="font-semibold text-brand-600 hover:underline dark:text-brand-400">
          وارد شوید
        </NuxtLink>
      </p>
    </form>
  </div>
</template>

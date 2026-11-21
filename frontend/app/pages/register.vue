<script setup lang="ts">
definePageMeta({ layout: 'default' })

useHead({ title: 'ثبت‌نام | نوبت' })

const auth = useAuthStore()

const form = reactive({
  fullName: '',
  email: '',
  phoneNumber: '',
  password: '',
  confirmPassword: '',
  role: 'Customer' as 'Customer' | 'Barber'
})

const loading = ref(false)
const error = ref<string | null>(null)
const fieldErrors = ref<Record<string, string[]>>({})
const success = ref(false)

const passwordHint = computed(() => {
  const p = form.password
  return {
    length: p.length >= 8,
    upper: /[A-Z]/.test(p),
    lower: /[a-z]/.test(p),
    digit: /[0-9]/.test(p)
  }
})

const passwordValid = computed(() => Object.values(passwordHint.value).every(Boolean))
const canSubmit = computed(
  () =>
    form.fullName.trim().length > 1 &&
    /.+@.+\..+/.test(form.email) &&
    passwordValid.value &&
    form.password === form.confirmPassword
)

async function submit() {
  error.value = null
  fieldErrors.value = {}
  loading.value = true
  try {
    await auth.register({
      fullName: form.fullName.trim(),
      email: form.email.trim(),
      password: form.password,
      confirmPassword: form.confirmPassword,
      phoneNumber: form.phoneNumber.trim() || undefined,
      role: form.role
    })
    success.value = true
    // Auto-login after successful registration
    await auth.login(form.email.trim(), form.password)
    setTimeout(() => navigateTo(form.role === 'Barber' ? '/dashboard' : '/my'), 700)
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

      <UFormField label="ایمیل" :error="fieldErrors.Email?.[0]" required>
        <UInput
          v-model="form.email"
          type="email"
          dir="ltr"
          placeholder="you@example.com"
          icon="i-lucide-mail"
          size="lg"
          class="w-full"
        />
      </UFormField>

      <UFormField label="شماره موبایل (اختیاری)" :error="fieldErrors.PhoneNumber?.[0]">
        <UInput
          v-model="form.phoneNumber"
          dir="ltr"
          placeholder="09xxxxxxxxx"
          icon="i-lucide-phone"
          size="lg"
          class="w-full"
        />
      </UFormField>

      <UFormField label="رمز عبور" :error="fieldErrors.Password?.[0]" required>
        <UInput
          v-model="form.password"
          type="password"
          placeholder="••••••••"
          icon="i-lucide-lock"
          size="lg"
          class="w-full"
        />
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
        <UInput
          v-model="form.confirmPassword"
          type="password"
          placeholder="••••••••"
          icon="i-lucide-lock"
          size="lg"
          class="w-full"
        />
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

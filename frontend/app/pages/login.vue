<script setup lang="ts">
definePageMeta({ layout: 'default' })

useHead({ title: 'ورود | نوبت' })

const auth = useAuthStore()
const route = useRoute()

const form = reactive({ email: '', password: '' })
const loading = ref(false)
const error = ref<string | null>(null)

const redirectTarget = computed(() => {
  const r = route.query.redirect as string | undefined
  if (r) return r
  return auth.isCustomer ? '/my' : '/dashboard'
})

onMounted(() => {
  if (auth.isAuthenticated) navigateTo(redirectTarget.value)
})

async function submit() {
  error.value = null
  loading.value = true
  try {
    await auth.login(form.email.trim(), form.password)
    await navigateTo(redirectTarget.value)
  } catch (e: any) {
    error.value = e?.message || 'ورود ناموفق بود'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="mx-auto flex w-full max-w-md flex-col justify-center px-4 py-14 sm:py-20">
    <div class="mb-7 text-center">
      <span class="mx-auto grid size-12 place-items-center rounded-2xl bg-brand-600 text-white dark:bg-brand-500">
        <UIcon name="i-lucide-log-in" class="size-6" />
      </span>
      <h1 class="mt-4 text-2xl font-extrabold">ورود به حساب</h1>
      <p class="mt-1.5 text-sm text-muted">برای رزرو و مدیریت نوبت‌ها وارد شوید.</p>
    </div>

    <form class="panel space-y-4 p-6" @submit.prevent="submit">
      <UAlert
        v-if="error"
        color="error"
        variant="subtle"
        icon="i-lucide-circle-alert"
        :title="error"
      />

      <UFormField label="ایمیل" name="email" required>
        <UInput
          v-model="form.email"
          type="email"
          dir="ltr"
          placeholder="you@example.com"
          icon="i-lucide-mail"
          autocomplete="email"
          size="lg"
          class="w-full"
        />
      </UFormField>

      <UFormField label="رمز عبور" name="password" required>
        <UInput
          v-model="form.password"
          type="password"
          placeholder="••••••••"
          icon="i-lucide-lock"
          autocomplete="current-password"
          size="lg"
          class="w-full"
        />
      </UFormField>

      <UButton
        type="submit"
        size="lg"
        block
        :loading="loading"
        :disabled="!form.email || !form.password"
      >
        ورود
      </UButton>

      <p class="pt-1 text-center text-sm text-muted">
        حساب ندارید؟
        <NuxtLink to="/register" class="font-semibold text-brand-600 hover:underline dark:text-brand-400">
          ثبت‌نام کنید
        </NuxtLink>
      </p>
    </form>

    <div class="mt-5 rounded-xl bg-gray-100 p-3 text-center text-xs text-muted dark:bg-gray-900">
      حساب نمونه مدیر:
      <span dir="ltr" class="font-mono">admin@nobat.local / Admin@123456</span>
    </div>
  </div>
</template>

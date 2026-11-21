<script setup lang="ts">
const auth = useAuthStore()
const route = useRoute()

interface NavItem {
  label: string
  to: string
  icon: string
  roles?: Array<'Admin' | 'Barber'>
}

const items: NavItem[] = [
  { label: 'نمای کلی', to: '/dashboard', icon: 'i-lucide-layout-dashboard' },
  { label: 'نوبت‌ها', to: '/dashboard/appointments', icon: 'i-lucide-calendar-check' },
  { label: 'خدمات', to: '/dashboard/services', icon: 'i-lucide-scissors', roles: ['Admin'] },
  { label: 'آرایشگران', to: '/dashboard/barbers', icon: 'i-lucide-users', roles: ['Admin'] },
  { label: 'ساعات کاری', to: '/dashboard/schedule', icon: 'i-lucide-clock', roles: ['Admin'] },
  { label: 'تعطیلات', to: '/dashboard/holidays', icon: 'i-lucide-calendar-off', roles: ['Admin'] }
]

const visibleItems = computed(() =>
  items.filter((i) => !i.roles || (auth.role && i.roles.includes(auth.role as any)))
)

const isActive = (to: string) =>
  to === '/dashboard' ? route.path === '/dashboard' : route.path.startsWith(to)

const sidebarOpen = ref(false)
</script>

<template>
  <div class="min-h-dvh bg-gray-50 dark:bg-gray-950">
    <!-- Top bar (mobile) -->
    <div
      class="sticky top-0 z-40 flex h-14 items-center gap-3 border-b border-gray-200 bg-white px-4
             lg:hidden dark:border-gray-800 dark:bg-gray-900"
    >
      <UButton
        variant="ghost"
        color="neutral"
        :icon="sidebarOpen ? 'i-lucide-x' : 'i-lucide-menu'"
        @click="sidebarOpen = !sidebarOpen"
      />
      <span class="font-bold">پنل مدیریت</span>
      <div class="ms-auto"><UColorModeButton /></div>
    </div>

    <div class="mx-auto flex w-full max-w-7xl">
      <!-- Sidebar -->
      <aside
        class="fixed inset-y-0 start-0 z-50 w-72 shrink-0 border-e border-gray-200 bg-white
               transition-transform lg:sticky lg:top-0 lg:h-dvh lg:translate-x-0
               dark:border-gray-800 dark:bg-gray-900"
        :class="sidebarOpen ? 'translate-x-0' : 'translate-x-full lg:translate-x-0'"
      >
        <div class="flex h-16 items-center gap-2.5 border-b border-gray-200 px-5 dark:border-gray-800">
          <span class="grid size-9 place-items-center rounded-xl bg-brand-600 text-white dark:bg-brand-500">
            <UIcon name="i-lucide-scissors" class="size-5" />
          </span>
          <div class="leading-tight">
            <p class="text-sm font-extrabold">نوبت</p>
            <p class="text-[11px] text-muted">پنل مدیریت</p>
          </div>
        </div>

        <nav class="flex flex-col gap-1 p-3">
          <NuxtLink
            v-for="item in visibleItems"
            :key="item.to"
            :to="item.to"
            class="flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium transition"
            :class="isActive(item.to)
              ? 'bg-brand-600 text-white shadow-sm dark:bg-brand-500'
              : 'text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'"
            @click="sidebarOpen = false"
          >
            <UIcon :name="item.icon" class="size-4.5" />
            {{ item.label }}
          </NuxtLink>
        </nav>

        <div class="absolute inset-x-3 bottom-3 space-y-1 border-t border-gray-200 pt-3 dark:border-gray-800">
          <NuxtLink
            to="/"
            class="flex items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium text-gray-600
                   hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            <UIcon name="i-lucide-arrow-right" class="size-4.5" />
            بازگشت به سایت
          </NuxtLink>
          <button
            type="button"
            class="flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium
                   text-rose-600 hover:bg-rose-50 dark:text-rose-400 dark:hover:bg-rose-500/10"
            @click="auth.logout()"
          >
            <UIcon name="i-lucide-log-out" class="size-4.5" />
            خروج از حساب
          </button>
        </div>
      </aside>

      <!-- Overlay -->
      <div
        v-if="sidebarOpen"
        class="fixed inset-0 z-40 bg-gray-900/40 lg:hidden"
        @click="sidebarOpen = false"
      />

      <!-- Content -->
      <div class="min-w-0 flex-1">
        <div class="hidden items-center justify-between border-b border-gray-200 bg-white px-8 py-4
                    lg:flex dark:border-gray-800 dark:bg-gray-900">
          <div>
            <h1 class="text-base font-bold">پنل مدیریت</h1>
            <p class="text-xs text-muted">{{ auth.user?.fullName }}</p>
          </div>
          <UColorModeButton />
        </div>

        <div class="p-4 sm:p-6 lg:p-8">
          <slot />
        </div>
      </div>
    </div>
  </div>
</template>

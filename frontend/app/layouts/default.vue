<script setup lang="ts">
const auth = useAuthStore()
const route = useRoute()

const navItems = [
  { label: 'خانه', to: '/', icon: 'i-lucide-home' },
  { label: 'رزرو نوبت', to: '/booking', icon: 'i-lucide-calendar-plus' }
]

const isActive = (to: string) =>
  to === '/' ? route.path === '/' : route.path.startsWith(to)

const panelLink = computed(() =>
  auth.isCustomer ? '/my' : '/panel'
)

const mobileOpen = ref(false)
</script>

<template>
  <div class="flex min-h-dvh flex-col">
    <header
      class="sticky top-0 z-40 border-b border-gray-200/70 bg-white/80 backdrop-blur-lg
             dark:border-gray-800/70 dark:bg-gray-950/80"
    >
      <div class="mx-auto flex h-16 w-full max-w-6xl items-center gap-3 px-4 sm:px-6">
        <NuxtLink to="/" class="flex items-center gap-2.5 shrink-0">
          <span
            class="grid size-9 place-items-center rounded-xl bg-brand-600 text-white shadow-sm
                   dark:bg-brand-500"
          >
            <UIcon name="i-lucide-scissors" class="size-5" />
          </span>
          <span class="text-lg font-extrabold tracking-tight">نوبت</span>
        </NuxtLink>

        <nav class="hidden items-center gap-1 md:flex">
          <NuxtLink
            v-for="item in navItems"
            :key="item.to"
            :to="item.to"
            class="rounded-lg px-3 py-2 text-sm font-medium transition"
            :class="isActive(item.to)
              ? 'bg-brand-50 text-brand-700 dark:bg-brand-500/10 dark:text-brand-300'
              : 'text-gray-600 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'"
          >
            {{ item.label }}
          </NuxtLink>
        </nav>

        <div class="ms-auto flex items-center gap-2">
          <UColorModeButton />

          <template v-if="auth.isAuthenticated">
            <UButton
              :to="panelLink"
              variant="soft"
              color="neutral"
              icon="i-lucide-layout-dashboard"
              class="hidden sm:inline-flex"
            >
              {{ auth.isCustomer ? 'نوبت‌های من' : 'پنل مدیریت' }}
            </UButton>

            <UDropdownMenu
              :items="[[
                { label: auth.user?.fullName || '', type: 'label' },
                { label: auth.isCustomer ? 'نوبت‌های من' : 'پنل مدیریت', icon: 'i-lucide-layout-dashboard', to: panelLink },
                { label: 'خروج', icon: 'i-lucide-log-out', onSelect: () => auth.logout() }
              ]]"
            >
              <UButton variant="ghost" color="neutral" class="gap-2">
                <UAvatar :alt="auth.user?.fullName" size="sm" />
                <span class="hidden max-w-28 truncate text-sm sm:inline">
                  {{ auth.user?.fullName }}
                </span>
              </UButton>
            </UDropdownMenu>
          </template>

          <template v-else>
            <UButton to="/login" variant="ghost" color="neutral" class="hidden sm:inline-flex">
              ورود
            </UButton>
            <UButton to="/register">ثبت‌نام</UButton>
          </template>

          <UButton
            class="md:hidden"
            variant="ghost"
            color="neutral"
            :icon="mobileOpen ? 'i-lucide-x' : 'i-lucide-menu'"
            @click="mobileOpen = !mobileOpen"
          />
        </div>
      </div>

      <div v-if="mobileOpen" class="border-t border-gray-200 px-4 py-3 md:hidden dark:border-gray-800">
        <NuxtLink
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          class="flex items-center gap-2 rounded-lg px-3 py-2.5 text-sm font-medium"
          :class="isActive(item.to)
            ? 'bg-brand-50 text-brand-700 dark:bg-brand-500/10 dark:text-brand-300'
            : 'text-gray-700 dark:text-gray-200'"
          @click="mobileOpen = false"
        >
          <UIcon :name="item.icon" class="size-4" />
          {{ item.label }}
        </NuxtLink>
      </div>
    </header>

    <main class="flex-1">
      <slot />
    </main>

    <footer class="border-t border-gray-200 bg-white dark:border-gray-800 dark:bg-gray-950">
      <div
        class="mx-auto flex w-full max-w-6xl flex-col items-center justify-between gap-3
               px-4 py-6 text-sm text-gray-500 sm:flex-row sm:px-6 dark:text-gray-400"
      >
        <p>© {{ new Date().getFullYear() }} نوبت — رزرو آنلاین آرایشگاه</p>
        <p class="flex items-center gap-1.5">
          ساخته‌شده با
          <UIcon name="i-lucide-heart" class="size-4 text-rose-500" />
          برای آرایشگران ایران
        </p>
      </div>
    </footer>
  </div>
</template>

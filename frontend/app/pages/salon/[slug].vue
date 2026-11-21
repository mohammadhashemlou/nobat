<script setup lang="ts">
definePageMeta({ layout: 'default' })

const route = useRoute()
const api = useApi()
const { persianDigits } = useJalali()

const slug = String(route.params.slug || '')

const salon = ref<PublicSalon | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    salon.value = await api<PublicSalon>(`/api/v1/salons/${slug}`, { skipAuth: true })
    useHead({ title: `${salon.value.name} | نوبت` })
  } catch (e: any) {
    error.value = e?.status === 404 ? 'سالنی با این آدرس یافت نشد' : e?.message
  } finally {
    loading.value = false
  }
}

onMounted(load)

const priceLabel = (p: number) => `${persianDigits(p.toLocaleString('en-US'))} تومان`
</script>

<template>
  <div class="mx-auto w-full max-w-5xl px-4 py-8 sm:px-6 sm:py-12">
    <div v-if="loading" class="space-y-4">
      <USkeleton class="h-36 w-full rounded-2xl" />
      <USkeleton class="h-64 w-full rounded-2xl" />
    </div>

    <ErrorState v-else-if="error" :message="error" @retry="load" />

    <template v-else-if="salon">
      <!-- Salon header -->
      <header class="panel overflow-hidden">
        <div class="h-28 bg-gradient-to-l from-brand-600 to-brand-400" />
        <div class="px-5 pb-5">
          <div class="flex items-end gap-4">
            <span
              class="-mt-10 grid size-20 shrink-0 place-items-center rounded-2xl border-4 border-white
                     bg-brand-600 text-white shadow-lg dark:border-gray-900 dark:bg-brand-500"
            >
              <UIcon name="i-lucide-scissors" class="size-9" />
            </span>
          </div>

          <div class="mt-3">
            <h1 class="text-xl font-extrabold sm:text-2xl">{{ salon.name }}</h1>
            <p v-if="salon.description" class="mt-1 max-w-2xl text-sm text-muted">
              {{ salon.description }}
            </p>
          </div>

          <div class="mt-4 flex flex-wrap gap-x-5 gap-y-2 text-sm text-muted">
            <span v-if="salon.address" class="flex items-center gap-1.5">
              <UIcon name="i-lucide-map-pin" class="size-4" /> {{ salon.address }}
            </span>
            <span v-if="salon.phone" class="flex items-center gap-1.5">
              <UIcon name="i-lucide-phone" class="size-4" />
              <span dir="ltr">{{ salon.phone }}</span>
            </span>
          </div>

          <UButton to="/booking" class="mt-4" icon="i-lucide-calendar-plus" size="lg">
            رزرو نوبت
          </UButton>
        </div>
      </header>

      <!-- Services -->
      <section class="mt-8">
        <h2 class="mb-3 flex items-center gap-2 text-lg font-bold">
          <UIcon name="i-lucide-scissors" class="size-5 text-brand-600 dark:text-brand-400" />
          خدمات و قیمت‌ها
        </h2>

        <EmptyState
          v-if="!salon.services.length"
          icon="i-lucide-scissors"
          title="خدمتی ثبت نشده"
          description="به‌زودی خدمات این سالن اضافه می‌شود."
        />

        <div v-else class="panel divide-y divide-gray-100 dark:divide-gray-800">
          <div
            v-for="s in salon.services"
            :key="s.id"
            class="flex items-center justify-between gap-4 p-4"
          >
            <div class="min-w-0">
              <p class="font-semibold">{{ s.name }}</p>
              <p v-if="s.description" class="mt-0.5 line-clamp-1 text-xs text-muted">{{ s.description }}</p>
              <p class="mt-1 flex items-center gap-1 text-xs text-muted">
                <UIcon name="i-lucide-clock" class="size-3.5" />
                {{ persianDigits(s.durationMinutes) }} دقیقه
              </p>
            </div>
            <div class="shrink-0 text-end">
              <p class="font-bold text-brand-600 dark:text-brand-400">{{ priceLabel(s.price) }}</p>
              <UButton to="/booking" variant="link" size="xs" class="mt-1 p-0">رزرو</UButton>
            </div>
          </div>
        </div>
      </section>

      <!-- Barbers -->
      <section class="mt-8">
        <h2 class="mb-3 flex items-center gap-2 text-lg font-bold">
          <UIcon name="i-lucide-users" class="size-5 text-brand-600 dark:text-brand-400" />
          آرایشگران
        </h2>

        <EmptyState
          v-if="!salon.barbers.length"
          icon="i-lucide-user-x"
          title="آرایشگری ثبت نشده"
          description="به‌زودی آرایشگران این سالن اضافه می‌شوند."
        />

        <div v-else class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <div v-for="b in salon.barbers" :key="b.id" class="panel flex items-center gap-3 p-4">
            <UAvatar :alt="b.fullName" :src="b.avatarUrl || undefined" size="lg" />
            <div class="min-w-0">
              <p class="font-semibold">{{ b.fullName }}</p>
              <p v-if="b.bio" class="line-clamp-2 text-xs text-muted">{{ b.bio }}</p>
              <p v-else class="text-xs text-muted">
                {{ persianDigits(b.serviceIds.length) }} خدمت
              </p>
            </div>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

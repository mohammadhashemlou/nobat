<script setup lang="ts">
const config = useRuntimeConfig()
const auth = useAuthStore()
const slug = config.public.salonSlug

const features = [
  {
    icon: 'i-lucide-calendar-check',
    title: 'رزرو آنلاین ۲۴ ساعته',
    description: 'در هر ساعت از شبانه‌روز نوبت بگیرید؛ بدون تماس تلفنی و بدون انتظار.'
  },
  {
    icon: 'i-lucide-clock',
    title: 'زمان‌های آزاد واقعی',
    description: 'سیستم به‌صورت هوشمند ساعات کاری، استراحت و نوبت‌های موجود را در نظر می‌گیرد.'
  },
  {
    icon: 'i-lucide-shield-check',
    title: 'بدون تداخل نوبت',
    description: 'هر زمان فقط یک بار قابل رزرو است — در سطح اپلیکیشن و دیتابیس تضمین شده.'
  },
  {
    icon: 'i-lucide-scissors',
    title: 'انتخاب آرایشگر و خدمت',
    description: 'آرایشگر مورد نظر و خدمات دلخواه را انتخاب کنید و قیمت را از قبل بدانید.'
  },
  {
    icon: 'i-lucide-history',
    title: 'تاریخچه نوبت‌ها',
    description: 'همه نوبت‌های گذشته و آینده در یک پنل ساده و مرتب در دسترس شماست.'
  },
  {
    icon: 'i-lucide-smartphone',
    title: 'موبایل‌پسند و فارسی',
    description: 'طراحی راست‌چین با تقویم شمسی و پشتیبانی از حالت روشن و تاریک.'
  }
]

const steps = [
  { n: 1, title: 'خدمت را انتخاب کنید', text: 'از میان خدمات سالن، خدمت مورد نظر و مدت آن را ببینید.' },
  { n: 2, title: 'آرایشگر را برگزینید', text: 'آرایشگری که آن خدمت را ارائه می‌دهد انتخاب کنید.' },
  { n: 3, title: 'زمان را رزرو کنید', text: 'روز و ساعت آزاد را انتخاب کنید و نوبت را ثبت کنید.' }
]

const primaryCta = computed(() => (auth.isAuthenticated ? '/booking' : '/login'))
</script>

<template>
  <div>
    <!-- Hero -->
    <section class="relative overflow-hidden">
      <div
        class="pointer-events-none absolute inset-0 -z-10 opacity-70
               [background-image:radial-gradient(circle_at_20%_10%,theme(colors.brand.200),transparent_45%),radial-gradient(circle_at_80%_0%,theme(colors.brand.100),transparent_40%)]
               dark:[background-image:radial-gradient(circle_at_20%_10%,theme(colors.brand.900),transparent_45%),radial-gradient(circle_at_80%_0%,theme(colors.brand.950),transparent_40%)]"
      />

      <div class="mx-auto w-full max-w-6xl px-4 pb-16 pt-14 sm:px-6 sm:pt-20 lg:pb-24 lg:pt-28">
        <div class="mx-auto max-w-3xl text-center">
          <UBadge variant="subtle" size="lg" class="mb-5 gap-1.5">
            <UIcon name="i-lucide-sparkles" class="size-4" />
            نوبت‌دهی آنلاین آرایشگاه، ساده و سریع
          </UBadge>

          <h1 class="text-3xl font-extrabold leading-tight tracking-tight sm:text-5xl lg:text-6xl">
            نوبت آرایشگاه را
            <span class="bg-gradient-to-l from-brand-600 to-brand-400 bg-clip-text text-transparent">
              آنلاین
            </span>
            بگیرید
          </h1>

          <p class="mx-auto mt-5 max-w-2xl text-base leading-relaxed text-muted sm:text-lg">
            بدون تماس تلفنی، بدون انتظار در صف. خدمت و آرایشگر را انتخاب کنید،
            زمان آزاد را ببینید و در چند ثانیه نوبت خود را قطعی کنید.
          </p>

          <div class="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
            <UButton :to="primaryCta" size="xl" icon="i-lucide-calendar-plus" class="w-full sm:w-auto">
              رزرو نوبت
            </UButton>
            <UButton
              :to="`/salon/${slug}`"
              size="xl"
              variant="outline"
              color="neutral"
              icon="i-lucide-store"
              class="w-full sm:w-auto"
            >
              مشاهده سالن
            </UButton>
          </div>

          <div class="mt-10 flex flex-wrap items-center justify-center gap-x-8 gap-y-3 text-sm text-muted">
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-check" class="size-4 text-emerald-500" /> بدون تداخل نوبت
            </span>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-check" class="size-4 text-emerald-500" /> تقویم شمسی
            </span>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-check" class="size-4 text-emerald-500" /> لغو آسان
            </span>
          </div>
        </div>
      </div>
    </section>

    <!-- Features -->
    <section class="border-t border-gray-200 bg-white py-16 dark:border-gray-800 dark:bg-gray-900/40">
      <div class="mx-auto w-full max-w-6xl px-4 sm:px-6">
        <div class="mx-auto max-w-2xl text-center">
          <h2 class="text-2xl font-extrabold sm:text-3xl">همه‌چیز برای یک رزرو بی‌دردسر</h2>
          <p class="mt-3 text-muted">
            تجربه‌ای مدرن برای مشتریان و ابزاری قدرتمند برای مدیریت سالن.
          </p>
        </div>

        <div class="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="f in features"
            :key="f.title"
            class="panel group p-5 transition hover:-translate-y-0.5 hover:shadow-md"
          >
            <span
              class="grid size-11 place-items-center rounded-xl bg-brand-50 text-brand-600 transition
                     group-hover:bg-brand-600 group-hover:text-white dark:bg-brand-500/10 dark:text-brand-300"
            >
              <UIcon :name="f.icon" class="size-5.5" />
            </span>
            <h3 class="mt-4 text-base font-bold">{{ f.title }}</h3>
            <p class="mt-1.5 text-sm leading-relaxed text-muted">{{ f.description }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- Steps -->
    <section class="py-16">
      <div class="mx-auto w-full max-w-6xl px-4 sm:px-6">
        <div class="mx-auto max-w-2xl text-center">
          <h2 class="text-2xl font-extrabold sm:text-3xl">در سه قدم نوبت بگیرید</h2>
          <p class="mt-3 text-muted">کل فرآیند کمتر از یک دقیقه طول می‌کشد.</p>
        </div>

        <div class="mt-10 grid gap-5 md:grid-cols-3">
          <div v-for="s in steps" :key="s.n" class="relative panel p-6">
            <span
              class="grid size-10 place-items-center rounded-full bg-brand-600 text-base font-extrabold text-white
                     dark:bg-brand-500"
            >
              {{ s.n }}
            </span>
            <h3 class="mt-4 text-base font-bold">{{ s.title }}</h3>
            <p class="mt-1.5 text-sm leading-relaxed text-muted">{{ s.text }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- CTA -->
    <section class="pb-20">
      <div class="mx-auto w-full max-w-6xl px-4 sm:px-6">
        <div
          class="relative overflow-hidden rounded-3xl bg-gradient-to-l from-brand-700 to-brand-500 px-6 py-12
                 text-center text-white shadow-xl sm:px-12 sm:py-16"
        >
          <div class="pointer-events-none absolute -end-10 -top-10 size-48 rounded-full bg-white/10" />
          <div class="pointer-events-none absolute -bottom-16 -start-10 size-56 rounded-full bg-white/10" />

          <h2 class="relative text-2xl font-extrabold sm:text-3xl">همین حالا نوبت خود را رزرو کنید</h2>
          <p class="relative mx-auto mt-3 max-w-xl text-white/85">
            زمان‌های آزاد همین امروز محدود است. سریع‌تر از همیشه نوبت بگیرید.
          </p>
          <UButton
            :to="primaryCta"
            size="xl"
            color="neutral"
            variant="solid"
            class="relative mt-7 bg-white text-brand-700 hover:bg-white/90"
            icon="i-lucide-calendar-plus"
          >
            شروع رزرو
          </UButton>
        </div>
      </div>
    </section>
  </div>
</template>

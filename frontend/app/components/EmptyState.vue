<script setup lang="ts">
withDefaults(
  defineProps<{
    icon?: string
    title: string
    description?: string
    actionLabel?: string
    actionTo?: string
  }>(),
  { icon: 'i-lucide-inbox' }
)
</script>

<template>
  <div class="flex flex-col items-center justify-center rounded-2xl border border-dashed
              border-gray-300 px-6 py-14 text-center dark:border-gray-700">
    <span class="grid size-14 place-items-center rounded-2xl bg-gray-100 dark:bg-gray-800">
      <UIcon :name="icon" class="size-7 text-gray-400 dark:text-gray-500" />
    </span>
    <h3 class="mt-4 text-base font-semibold">{{ title }}</h3>
    <p v-if="description" class="mt-1.5 max-w-sm text-sm text-muted">{{ description }}</p>
    <UButton v-if="actionLabel && actionTo" :to="actionTo" class="mt-5" variant="soft">
      {{ actionLabel }}
    </UButton>
    <slot />
  </div>
</template>

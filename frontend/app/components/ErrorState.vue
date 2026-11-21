<script setup lang="ts">
defineProps<{
  title?: string
  message?: string
}>()

const emit = defineEmits<{ retry: [] }>()
</script>

<template>
  <div class="flex flex-col items-center justify-center rounded-2xl border border-rose-200
              bg-rose-50/60 px-6 py-12 text-center dark:border-rose-900/60 dark:bg-rose-950/30">
    <span class="grid size-14 place-items-center rounded-2xl bg-rose-100 dark:bg-rose-900/50">
      <UIcon name="i-lucide-triangle-alert" class="size-7 text-rose-600 dark:text-rose-400" />
    </span>
    <h3 class="mt-4 text-base font-semibold text-rose-900 dark:text-rose-200">
      {{ title || 'مشکلی پیش آمد' }}
    </h3>
    <p class="mt-1.5 max-w-sm text-sm text-rose-700/80 dark:text-rose-300/80">
      {{ message || 'ارتباط با سرور برقرار نشد. لطفاً دوباره تلاش کنید.' }}
    </p>
    <UButton class="mt-5" color="error" variant="soft" icon="i-lucide-refresh-cw" @click="emit('retry')">
      تلاش دوباره
    </UButton>
  </div>
</template>

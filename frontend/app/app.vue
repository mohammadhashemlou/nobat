<script setup lang="ts">
import { fa_ir } from '@nuxt/ui/locale'
</script>

<template>
  <UApp :locale="fa_ir">
    <RouteProgress />
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </UApp>
</template>

<script setup lang="ts">
defineOptions({ inheritAttrs: false })

const props = withDefaults(
  defineProps<{
    modelValue: string
    placeholder?: string
    icon?: string
  }>(),
  { placeholder: '••••••••', icon: 'i-lucide-lock' }
)

const emit = defineEmits<{ 'update:modelValue': [string] }>()

const revealed = ref(false)
</script>

<template>
  <UInput
    :model-value="props.modelValue"
    :type="revealed ? 'text' : 'password'"
    :placeholder="props.placeholder"
    :icon="props.icon"
    size="lg"
    class="w-full"
    v-bind="$attrs"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <template #trailing>
      <UButton
        color="neutral"
        variant="link"
        size="xs"
        :icon="revealed ? 'i-lucide-eye-off' : 'i-lucide-eye'"
        :aria-label="revealed ? 'پنهان کردن گذرواژه' : 'نمایش گذرواژه'"
        tabindex="-1"
        @click="revealed = !revealed"
      />
    </template>
  </UInput>
</template>

<script setup lang="ts">
const props = defineProps<{ modelValue: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

function handleInput(value: string | number) {
  emit('update:modelValue', normalizePhoneInput(value))
}
</script>

<template>
  <UInput
    :model-value="props.modelValue"
    dir="ltr"
    inputmode="numeric"
    autocomplete="tel"
    maxlength="11"
    placeholder="09xxxxxxxxx"
    icon="i-lucide-smartphone"
    class="w-full"
    @keydown="blockNonDigitKeys"
    @beforeinput="blockNonDigitInput"
    @update:model-value="handleInput"
  />
</template>

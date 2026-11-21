<script setup lang="ts">
const routeLoading = useState('route-progress', () => false)
const inFlight = useNetworkActivity()

const busy = computed(() => routeLoading.value || inFlight.value > 0)

const visible = ref(false)
let revealTimer: ReturnType<typeof setTimeout> | null = null

/**
 * Requests that finish in a few milliseconds would only flash the bar, which
 * reads as a glitch. Waiting a beat means the indicator appears exactly when
 * the wait is long enough to need one.
 */
watch(busy, (running) => {
  if (revealTimer) {
    clearTimeout(revealTimer)
    revealTimer = null
  }

  if (running) {
    revealTimer = setTimeout(() => {
      visible.value = true
    }, 180)
    return
  }

  visible.value = false
})

onUnmounted(() => {
  if (revealTimer) clearTimeout(revealTimer)
})
</script>

<template>
  <div
    class="pointer-events-none fixed inset-x-0 top-0 z-[100] h-0.5 overflow-hidden transition-opacity duration-200"
    :class="visible ? 'opacity-100' : 'opacity-0'"
    role="progressbar"
    aria-label="در حال بارگذاری"
  >
    <div class="route-progress-bar h-full bg-brand-600 dark:bg-brand-400" />
  </div>
</template>

<style scoped>
.route-progress-bar {
  transform-origin: right center;
  animation: route-progress 1.3s ease-in-out infinite;
}

@keyframes route-progress {
  0% {
    transform: scaleX(0.04);
    opacity: 1;
  }
  55% {
    transform: scaleX(0.5);
    opacity: 1;
  }
  100% {
    transform: scaleX(1);
    opacity: 0.15;
  }
}

@media (prefers-reduced-motion: reduce) {
  .route-progress-bar {
    animation: none;
    opacity: 0.6;
  }
}
</style>

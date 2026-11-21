// Re-runs a loader when the tab regains focus or visibility, and optionally on a
// timer. The panel and the customer area are often open side by side, so without
// this a change made in one window stays invisible in the other.
export function useAutoRefresh(
  load: (silent?: boolean) => unknown,
  options: { intervalMs?: number } = {}
) {
  const { intervalMs = 0 } = options
  let timer: ReturnType<typeof setInterval> | null = null

  function revalidate() {
    if (document.visibilityState === 'visible') {
      void load(true)
    }
  }

  onMounted(() => {
    window.addEventListener('focus', revalidate)
    document.addEventListener('visibilitychange', revalidate)

    if (intervalMs > 0) {
      timer = setInterval(revalidate, intervalMs)
    }
  })

  onUnmounted(() => {
    window.removeEventListener('focus', revalidate)
    document.removeEventListener('visibilitychange', revalidate)
    if (timer) clearInterval(timer)
  })
}

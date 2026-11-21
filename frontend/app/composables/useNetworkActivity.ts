/**
 * How many API calls are in flight right now. A plain module-level ref on
 * purpose: the counter is read from the layout and written from store actions,
 * where a Nuxt context is not guaranteed to be available.
 */
const inFlight = ref(0)

export function useNetworkActivity() {
  return inFlight
}

export function trackRequest<T>(run: () => Promise<T>): Promise<T> {
  inFlight.value++

  return run().finally(() => {
    inFlight.value = Math.max(0, inFlight.value - 1)
  })
}

/**
 * Feeds the route progress bar. Nuxt's Suspense hooks are the only place that
 * knows a page component is still resolving, which is what we want to surface.
 */
export default defineNuxtPlugin((nuxtApp) => {
  const active = useState('route-progress', () => false)

  nuxtApp.hook('page:start', () => {
    active.value = true
  })

  nuxtApp.hook('page:finish', () => {
    active.value = false
  })

  nuxtApp.hook('app:error', () => {
    active.value = false
  })
})

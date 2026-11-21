/**
 * Marks the document once Vue has hydrated the server-rendered markup.
 *
 * Until hydration runs, the SSR HTML looks interactive but is inert: `v-model`
 * bindings and click handlers do not exist yet, and the first client render resets
 * every input to the reactive state — silently discarding anything typed in
 * between. E2E tests wait for `html[data-hydrated="true"]` so they never act on a
 * page that is still booting.
 */
export default defineNuxtPlugin((nuxtApp) => {
  nuxtApp.hook('app:mounted', () => {
    document.documentElement.dataset.hydrated = 'true'
  })
})

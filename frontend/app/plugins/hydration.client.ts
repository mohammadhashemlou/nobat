// Flags the document once Vue has taken over the server-rendered markup. Before
// that the HTML looks interactive but is inert, and the first client render resets
// inputs to the reactive state. E2E tests wait for this flag before typing.
export default defineNuxtPlugin((nuxtApp) => {
  nuxtApp.hook('app:mounted', () => {
    document.documentElement.dataset.hydrated = 'true'
  })
})

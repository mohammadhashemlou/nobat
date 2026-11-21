import * as Vue from 'vue'

// Nuxt auto-imports Vue APIs into <script setup> blocks. Outside Nuxt those are free
// variables, so expose the ones our components use on the global scope.
const globals = globalThis as unknown as Record<string, unknown>
globals.computed = Vue.computed
globals.ref = Vue.ref
globals.reactive = Vue.reactive
globals.watch = Vue.watch
globals.onMounted = Vue.onMounted

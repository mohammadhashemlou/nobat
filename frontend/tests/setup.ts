import * as Vue from 'vue'
import dayjs from 'dayjs'
import utc from 'dayjs/plugin/utc'
import timezone from 'dayjs/plugin/timezone'
import jalaliday from 'jalaliday'
import fa from 'dayjs/locale/fa'

// Mirror app/plugins/dayjs.ts exactly — the Jalali behaviour under test depends on it.
dayjs.extend(utc)
dayjs.extend(timezone)
dayjs.extend(jalaliday)
dayjs.calendar('jalali')
dayjs.locale('fa')

// Nuxt auto-imports Vue APIs into <script setup> blocks. Outside Nuxt those are free
// variables, so expose the ones our components use on the global scope.
const globals = globalThis as unknown as Record<string, unknown>
globals.computed = Vue.computed
globals.ref = Vue.ref
globals.reactive = Vue.reactive
globals.watch = Vue.watch
globals.onMounted = Vue.onMounted

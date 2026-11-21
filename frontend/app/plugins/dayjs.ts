import dayjs from 'dayjs'
import utc from 'dayjs/plugin/utc'
import timezone from 'dayjs/plugin/timezone'
import jalaliday from 'jalaliday'
import fa from 'dayjs/locale/fa'

dayjs.extend(utc)
dayjs.extend(timezone)
dayjs.extend(jalaliday)

// Default calendar for all new instances is Jalali.
dayjs.calendar('jalali')
dayjs.locale('fa')

export default defineNuxtPlugin(() => {
  return {
    provide: {
      dayjs
    }
  }
})

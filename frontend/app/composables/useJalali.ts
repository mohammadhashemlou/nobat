import dayjs from 'dayjs'

export const BUSINESS_TZ = 'Asia/Tehran'

/** Iranian week order: Saturday first. */
export const IRANIAN_WEEKDAYS = [
  'شنبه',
  'یکشنبه',
  'دوشنبه',
  'سه‌شنبه',
  'چهارشنبه',
  'پنجشنبه',
  'جمعه'
] as const

export const IRANIAN_WEEKDAYS_SHORT = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'] as const

export const JALALI_MONTHS = [
  'فروردین',
  'اردیبهشت',
  'خرداد',
  'تیر',
  'مرداد',
  'شهریور',
  'مهر',
  'آبان',
  'آذر',
  'دی',
  'بهمن',
  'اسفند'
] as const

const PERSIAN_DIGITS = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹']

export interface JalaliParts {
  year: number
  month: number // 1..12
  day: number // 1..31
}

export function useJalali() {
  /** Convert any date-ish input to a dayjs instance in the business timezone. */
  function toBusiness(date: string | number | Date) {
    return dayjs(date).tz(BUSINESS_TZ)
  }

  /** Convert to a Jalali-calendar dayjs instance (Tehran timezone). */
  function toJalali(date: string | number | Date) {
    return toBusiness(date).calendar('jalali').locale('fa')
  }

  /** Convert to a Gregorian-calendar dayjs instance (Tehran timezone). */
  function toGregorian(date: string | number | Date) {
    return toBusiness(date).calendar('gregory')
  }

  function persianDigits(input: string | number): string {
    return String(input).replace(/\d/g, (d) => PERSIAN_DIGITS[Number(d)]!)
  }

  function latinDigits(input: string): string {
    return input.replace(/[۰-۹]/g, (d) => String(PERSIAN_DIGITS.indexOf(d)))
  }

  /** { year, month (1-12), day } in Jalali. */
  function parts(date: string | number | Date): JalaliParts {
    const j = toJalali(date)
    return { year: j.year(), month: j.month() + 1, day: j.date() }
  }

  /** 1405/09/02 */
  function formatDate(date: string | number | Date, withPersianDigits = true): string {
    const out = toJalali(date).format('YYYY/MM/DD')
    return withPersianDigits ? persianDigits(out) : out
  }

  /** ۲ آذر ۱۴۰۵ */
  function formatLongDate(date: string | number | Date): string {
    const j = toJalali(date)
    const out = `${j.date()} ${JALALI_MONTHS[j.month()]} ${j.year()}`
    return persianDigits(out)
  }

  /** دوشنبه ۲ آذر ۱۴۰۵ */
  function formatFullDate(date: string | number | Date): string {
    const j = toJalali(date)
    const weekday = IRANIAN_WEEKDAYS[toIranianDow(j.day())]
    const out = `${weekday} ${j.date()} ${JALALI_MONTHS[j.month()]} ${j.year()}`
    return persianDigits(out)
  }

  /** 09:30 in Tehran time (24h), Persian digits. */
  function formatTime(date: string | number | Date): string {
    return persianDigits(toBusiness(date).format('HH:mm'))
  }

  /** Relative label: امروز / فردا / weekday + date. */
  function relativeDayLabel(date: string | number | Date): string {
    const target = toBusiness(date).startOf('day')
    const today = dayjs().tz(BUSINESS_TZ).startOf('day')
    const diff = target.diff(today, 'day')
    if (diff === 0) return 'امروز'
    if (diff === 1) return 'فردا'
    if (diff === -1) return 'دیروز'
    return formatFullDate(date)
  }

  /** dayjs .day() (0=Sunday) -> Iranian index (Sat=0 .. Fri=6). */
  function toIranianDow(dayjsDay: number): number {
    return (dayjsDay + 1) % 7
  }

  /** Number of days in a Jalali month. */
  function daysInJalaliMonth(year: number, month: number): number {
    if (month <= 6) return 31
    if (month <= 11) return 30
    // Esfand: leap year check via conversion round-trip
    const first = jalaliToGregorian(year, 12, 1)
    const next = dayjs(first).add(1, 'day')
    const nextJ = parts(next.toDate())
    // If adding a day lands in month 12 still, Esfand has 30 days
    return nextJ.month === 12 ? 30 : 29
  }

  /** Iranian weekday index (Sat=0) of the 1st day of a Jalali month. */
  function firstDowOfJalaliMonth(year: number, month: number): number {
    const g = jalaliToGregorian(year, month, 1)
    return toIranianDow(dayjs(g).day())
  }

  /** Convert a Jalali date to a JS Date (Tehran midnight). */
  function jalaliToGregorian(year: number, month: number, day: number): Date {
    // Build the Jalali wall-clock, then read it back in the Gregorian calendar.
    const base = dayjs()
      .calendar('jalali')
      .locale('fa')
      .year(year)
      .month(month - 1)
      .date(day)
    const iso = base.calendar('gregory').format('YYYY-MM-DD')
    return new Date(`${iso}T00:00:00+03:30`)
  }

  /** ISO date (YYYY-MM-DD, Gregorian) for the API, from a JS Date in Tehran. */
  function toApiDate(date: string | number | Date): string {
    return toGregorian(date).format('YYYY-MM-DD')
  }

  /** Today's ISO date (Gregorian) in Tehran. */
  function todayApiDate(): string {
    return dayjs().tz(BUSINESS_TZ).calendar('gregory').format('YYYY-MM-DD')
  }

  return {
    BUSINESS_TZ,
    IRANIAN_WEEKDAYS,
    IRANIAN_WEEKDAYS_SHORT,
    JALALI_MONTHS,
    toBusiness,
    toJalali,
    toGregorian,
    persianDigits,
    latinDigits,
    parts,
    formatDate,
    formatLongDate,
    formatFullDate,
    formatTime,
    relativeDayLabel,
    toIranianDow,
    daysInJalaliMonth,
    firstDowOfJalaliMonth,
    jalaliToGregorian,
    toApiDate,
    todayApiDate
  }
}

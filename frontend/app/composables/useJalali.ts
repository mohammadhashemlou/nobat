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

// ---------------------------------------------------------------------------
// Jalali <-> Gregorian conversion.
//
// Deliberately implemented arithmetically (the classic jalaali algorithm) rather
// than by chaining dayjs setters. dayjs + jalaliday clamps the day-of-month when
// moving between months of different lengths, which silently produced wrong
// dates for Esfand — and the whole booking flow depends on these conversions
// being exact. This version is pure, deterministic and timezone-independent.
// ---------------------------------------------------------------------------

const JALALI_BREAKS = [
  -61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210, 1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178
] as const

const div = (a: number, b: number) => Math.trunc(a / b)
const mod = (a: number, b: number) => a - Math.trunc(a / b) * b

interface JalaliYearInfo {
  /** 0 == leap year (Esfand has 30 days). */
  leap: number
  gy: number
  march: number
}

function jalaliYearInfo(jy: number): JalaliYearInfo {
  const bl = JALALI_BREAKS.length
  const gy = jy + 621
  let leapJ = -14
  let jp: number = JALALI_BREAKS[0]!
  let jump = 0

  if (jy < jp || jy >= JALALI_BREAKS[bl - 1]!) {
    throw new Error(`Invalid Jalali year ${jy}`)
  }

  for (let i = 1; i < bl; i += 1) {
    const jm = JALALI_BREAKS[i]!
    jump = jm - jp
    if (jy < jm) break
    leapJ += div(jump, 33) * 8 + div(mod(jump, 33), 4)
    jp = jm
  }

  let n = jy - jp
  leapJ += div(n, 33) * 8 + div(mod(n, 33) + 3, 4)
  if (mod(jump, 33) === 4 && jump - n === 4) leapJ += 1

  const leapG = div(gy, 4) - div((div(gy, 100) + 1) * 3, 4) - 150
  const march = 20 + leapJ - leapG

  if (jump - n < 6) n = n - jump + div(jump + 4, 33) * 33

  let leap = mod(mod(n + 1, 33) - 1, 4)
  if (leap === -1) leap = 4

  return { leap, gy, march }
}

/** Gregorian (y, m, d) -> Julian day number. */
function gregorianToJdn(gy: number, gm: number, gd: number): number {
  let d =
    div((gy + div(gm - 8, 6) + 100100) * 1461, 4) +
    div(153 * mod(gm + 9, 12) + 2, 5) +
    gd -
    34840408
  d = d - div(div(gy + 100100 + div(gm - 8, 6), 100) * 3, 4) + 752
  return d
}

/** Julian day number -> Gregorian (y, m, d). */
function jdnToGregorian(jdn: number): { gy: number; gm: number; gd: number } {
  let j = 4 * jdn + 139361631
  j = j + div(div(4 * jdn + 183187720, 146097) * 3, 4) * 4 - 3908
  const i = div(mod(j, 1461), 4) * 5 + 308
  const gd = div(mod(i, 153), 5) + 1
  const gm = mod(div(i, 153), 12) + 1
  const gy = div(j, 1461) - 100100 + div(8 - gm, 6)
  return { gy, gm, gd }
}

function jalaliToJdn(jy: number, jm: number, jd: number): number {
  const r = jalaliYearInfo(jy)
  return gregorianToJdn(r.gy, 3, r.march) + (jm - 1) * 31 - div(jm, 7) * (jm - 7) + jd - 1
}

/** True when Esfand of the given Jalali year has 30 days. */
export function isJalaliLeapYear(jy: number): boolean {
  return jalaliYearInfo(jy).leap === 0
}

/** Convert Jalali (y, m, d) to Gregorian parts, without going through dayjs. */
export function jalaliToGregorianParts(jy: number, jm: number, jd: number): {
  gy: number
  gm: number
  gd: number
} {
  return jdnToGregorian(jalaliToJdn(jy, jm, jd))
}

/** Convert Gregorian (y, m, d) to Jalali parts, without going through dayjs. */
export function gregorianToJalaliParts(gy: number, gm: number, gd: number): {
  jy: number
  jm: number
  jd: number
} {
  const jdn = gregorianToJdn(gy, gm, gd)
  let jy = gy - 621
  const r = jalaliYearInfo(jy)
  const firstFarvardin = gregorianToJdn(r.gy, 3, r.march)

  let k = jdn - firstFarvardin

  if (k >= 0) {
    // First six months are 31 days each (186 days total).
    if (k <= 185) {
      return { jy, jm: 1 + div(k, 31), jd: mod(k, 31) + 1 }
    }
    k -= 186
  } else {
    // Belongs to the previous Jalali year.
    jy -= 1
    k += 179
    if (r.leap === 1) k += 1
  }

  return { jy, jm: 7 + div(k, 30), jd: mod(k, 30) + 1 }
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

  /**
   * Gregorian Y/M/D of an instant as seen in Tehran.
   * Reads the explicitly-Gregorian formatted string so it can never pick up
   * Jalali getters from the global calendar.
   */
  function tehranGregorianParts(date: string | number | Date) {
    const [y, m, d] = toGregorian(date).format('YYYY-MM-DD').split('-').map(Number)
    return { gy: y!, gm: m!, gd: d! }
  }

  /** { year, month (1-12), day } in Jalali. */
  function parts(date: string | number | Date): JalaliParts {
    const g = tehranGregorianParts(date)
    const j = gregorianToJalaliParts(g.gy, g.gm, g.gd)
    return { year: j.jy, month: j.jm, day: j.jd }
  }

  /** 1405/09/02 */
  function formatDate(date: string | number | Date, withPersianDigits = true): string {
    const p = parts(date)
    const out = `${p.year}/${String(p.month).padStart(2, '0')}/${String(p.day).padStart(2, '0')}`
    return withPersianDigits ? persianDigits(out) : out
  }

  /** ۲ آذر ۱۴۰۵ */
  function formatLongDate(date: string | number | Date): string {
    const p = parts(date)
    return persianDigits(`${p.day} ${JALALI_MONTHS[p.month - 1]} ${p.year}`)
  }

  /** دوشنبه ۲ آذر ۱۴۰۵ */
  function formatFullDate(date: string | number | Date): string {
    const p = parts(date)
    const g = tehranGregorianParts(date)
    const weekday = IRANIAN_WEEKDAYS[toIranianDow(new Date(Date.UTC(g.gy, g.gm - 1, g.gd)).getUTCDay())]
    return persianDigits(`${weekday} ${p.day} ${JALALI_MONTHS[p.month - 1]} ${p.year}`)
  }

  /** 09:30 in Tehran time (24h), Persian digits. */
  function formatTime(date: string | number | Date): string {
    return persianDigits(toBusiness(date).format('HH:mm'))
  }

  /** Relative label: امروز / فردا / دیروز, otherwise the full Jalali date. */
  function relativeDayLabel(date: string | number | Date): string {
    // Compare Gregorian day strings instead of calling .startOf('day') on a
    // timezone-shifted Jalali instance — that combination miscomputes the date.
    const targetDay = toGregorian(date).format('YYYY-MM-DD')
    const todayDay = dayjs().tz(BUSINESS_TZ).calendar('gregory').format('YYYY-MM-DD')

    const midnightMs = (isoDay: string) => new Date(`${isoDay}T00:00:00+03:30`).getTime()
    const diffDays = Math.round((midnightMs(targetDay) - midnightMs(todayDay)) / 86_400_000)

    if (diffDays === 0) return 'امروز'
    if (diffDays === 1) return 'فردا'
    if (diffDays === -1) return 'دیروز'
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
    return isJalaliLeapYear(year) ? 30 : 29
  }

  /** Iranian weekday index (Sat=0) of the 1st day of a Jalali month. */
  function firstDowOfJalaliMonth(year: number, month: number): number {
    const g = jalaliToGregorianParts(year, month, 1)
    const dow = new Date(Date.UTC(g.gy, g.gm - 1, g.gd)).getUTCDay()
    return toIranianDow(dow)
  }

  /** Convert a Jalali date to a JS Date (Tehran midnight). */
  function jalaliToGregorian(year: number, month: number, day: number): Date {
    const g = jalaliToGregorianParts(year, month, day)
    const iso = `${g.gy}-${String(g.gm).padStart(2, '0')}-${String(g.gd).padStart(2, '0')}`
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

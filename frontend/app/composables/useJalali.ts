export const BUSINESS_TZ = 'Asia/Tehran'

// Iran dropped daylight saving in 2022, so +03:30 is a constant. Using a fixed
// offset instead of a tz database keeps conversions independent of the host zone.
const BUSINESS_OFFSET_MS = 210 * 60_000

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
// Done arithmetically (classic jalaali algorithm) instead of through a calendar
// plugin: jalaliday drifts by a day on some dates, and chained dayjs setters
// clamp the day-of-month when crossing months of different lengths.
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

/** Convert Jalali (y, m, d) to Gregorian parts. */
export function jalaliToGregorianParts(
  jy: number,
  jm: number,
  jd: number
): { gy: number; gm: number; gd: number } {
  return jdnToGregorian(jalaliToJdn(jy, jm, jd))
}

/** Convert Gregorian (y, m, d) to Jalali parts. */
export function gregorianToJalaliParts(
  gy: number,
  gm: number,
  gd: number
): { jy: number; jm: number; jd: number } {
  const jdn = gregorianToJdn(gy, gm, gd)
  let jy = gy - 621
  const r = jalaliYearInfo(jy)
  const firstFarvardin = gregorianToJdn(r.gy, 3, r.march)

  let k = jdn - firstFarvardin

  if (k >= 0) {
    // The first six months are 31 days each (186 days total).
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

// ---------------------------------------------------------------------------
// Instant handling
// ---------------------------------------------------------------------------

export type DateInput = string | number | Date

/** Milliseconds since epoch for any accepted input. */
function toInstant(date: DateInput): number {
  if (typeof date === 'number') return date
  if (date instanceof Date) return date.getTime()

  // Native ISO parsing honours any explicit offset. A bare date is interpreted as
  // Tehran midnight, which is what every API payload means by "a day".
  const iso = /^\d{4}-\d{2}-\d{2}$/.test(date) ? `${date}T00:00:00+03:30` : date
  const ms = Date.parse(iso)
  if (Number.isNaN(ms)) throw new Error(`Invalid date: ${String(date)}`)
  return ms
}

interface TehranWallClock {
  gy: number
  gm: number
  gd: number
  hour: number
  minute: number
}

/** The Gregorian wall-clock in Tehran for an instant. */
function tehranWallClock(date: DateInput): TehranWallClock {
  const shifted = new Date(toInstant(date) + BUSINESS_OFFSET_MS)
  return {
    gy: shifted.getUTCFullYear(),
    gm: shifted.getUTCMonth() + 1,
    gd: shifted.getUTCDate(),
    hour: shifted.getUTCHours(),
    minute: shifted.getUTCMinutes()
  }
}

const pad2 = (n: number) => String(n).padStart(2, '0')

export function useJalali() {
  /** Tehran midnight of a Jalali date, as a Date. */
  function jalaliToGregorian(year: number, month: number, day: number): Date {
    const g = jalaliToGregorianParts(year, month, day)
    return new Date(`${g.gy}-${pad2(g.gm)}-${pad2(g.gd)}T00:00:00+03:30`)
  }

  function persianDigits(input: string | number): string {
    return String(input).replace(/\d/g, (d) => PERSIAN_DIGITS[Number(d)]!)
  }

  function latinDigits(input: string): string {
    return input.replace(/[۰-۹]/g, (d) => String(PERSIAN_DIGITS.indexOf(d)))
  }

  /** { year, month (1-12), day } in Jalali. */
  function parts(date: DateInput): JalaliParts {
    const c = tehranWallClock(date)
    const j = gregorianToJalaliParts(c.gy, c.gm, c.gd)
    return { year: j.jy, month: j.jm, day: j.jd }
  }

  /** 1405/09/02 */
  function formatDate(date: DateInput, withPersianDigits = true): string {
    const p = parts(date)
    const out = `${p.year}/${pad2(p.month)}/${pad2(p.day)}`
    return withPersianDigits ? persianDigits(out) : out
  }

  function formatLongDate(date: DateInput): string {
    const p = parts(date)
    return persianDigits(`${p.day} ${JALALI_MONTHS[p.month - 1]} ${p.year}`)
  }

  function formatFullDate(date: DateInput): string {
    const p = parts(date)
    const c = tehranWallClock(date)
    const weekday = IRANIAN_WEEKDAYS[toIranianDow(new Date(Date.UTC(c.gy, c.gm - 1, c.gd)).getUTCDay())]
    return persianDigits(`${weekday} ${p.day} ${JALALI_MONTHS[p.month - 1]} ${p.year}`)
  }

  /** 09:30 in Tehran time (24-hour), Persian digits. */
  function formatTime(date: DateInput): string {
    const c = tehranWallClock(date)
    return persianDigits(`${pad2(c.hour)}:${pad2(c.minute)}`)
  }

  /** Today / tomorrow / yesterday, otherwise the full date. */
  function relativeDayLabel(date: DateInput): string {
    const dayMs = (d: DateInput) => {
      const c = tehranWallClock(d)
      return Date.UTC(c.gy, c.gm - 1, c.gd)
    }

    const diffDays = Math.round((dayMs(date) - dayMs(Date.now())) / 86_400_000)

    if (diffDays === 0) return 'امروز'
    if (diffDays === 1) return 'فردا'
    if (diffDays === -1) return 'دیروز'
    return formatFullDate(date)
  }

  /** JS weekday (0 = Sunday) -> Iranian index (0 = Saturday .. 6 = Friday). */
  function toIranianDow(jsDay: number): number {
    return (jsDay + 1) % 7
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
    return toIranianDow(new Date(Date.UTC(g.gy, g.gm - 1, g.gd)).getUTCDay())
  }

  /** ISO date (YYYY-MM-DD, Gregorian) in Tehran — the format the API expects. */
  function toApiDate(date: DateInput): string {
    const c = tehranWallClock(date)
    return `${c.gy}-${pad2(c.gm)}-${pad2(c.gd)}`
  }

  /** Today's ISO date (Gregorian) in Tehran. */
  function todayApiDate(): string {
    return toApiDate(Date.now())
  }

  return {
    BUSINESS_TZ,
    IRANIAN_WEEKDAYS,
    IRANIAN_WEEKDAYS_SHORT,
    JALALI_MONTHS,
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

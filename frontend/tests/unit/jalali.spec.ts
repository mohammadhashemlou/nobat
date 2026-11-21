import { describe, expect, it } from 'vitest'
import {
  useJalali,
  JALALI_MONTHS,
  IRANIAN_WEEKDAYS,
  isJalaliLeapYear,
  jalaliToGregorianParts,
  gregorianToJalaliParts
} from '~/composables/useJalali'

const j = useJalali()

/** 1405/09/02 == 2026-11-23 (Tehran). */
const SAMPLE_ISO = '2026-11-23T00:00:00+03:30'

describe('persianDigits', () => {
  it('converts every latin digit', () => {
    expect(j.persianDigits('0123456789')).toBe('۰۱۲۳۴۵۶۷۸۹')
  })

  it('accepts numbers', () => {
    expect(j.persianDigits(1405)).toBe('۱۴۰۵')
  })

  it('leaves non-digits untouched', () => {
    expect(j.persianDigits('09:30')).toBe('۰۹:۳۰')
    expect(j.persianDigits('abc')).toBe('abc')
  })
})

describe('latinDigits', () => {
  it('is the inverse of persianDigits', () => {
    expect(j.latinDigits(j.persianDigits('1405/09/02'))).toBe('1405/09/02')
  })
})

describe('Jalali conversion', () => {
  it('maps 2026-11-23 to 1405/09/02', () => {
    expect(j.parts(SAMPLE_ISO)).toEqual({ year: 1405, month: 9, day: 2 })
  })

  it('formats a Jalali date with Persian digits', () => {
    expect(j.formatDate(SAMPLE_ISO)).toBe('۱۴۰۵/۰۹/۰۲')
  })

  it('can format with latin digits when asked', () => {
    expect(j.formatDate(SAMPLE_ISO, false)).toBe('1405/09/02')
  })

  it('formats a long date using the Jalali month name', () => {
    expect(j.formatLongDate(SAMPLE_ISO)).toBe('۲ آذر ۱۴۰۵')
    expect(JALALI_MONTHS[8]).toBe('آذر')
  })

  it('formats a full date with the Iranian weekday name', () => {
    // 2026-11-23 falls on a Monday.
    expect(j.formatFullDate(SAMPLE_ISO)).toBe('دوشنبه ۲ آذر ۱۴۰۵')
    expect(IRANIAN_WEEKDAYS[2]).toBe('دوشنبه')
  })
})

describe('time formatting', () => {
  it('always renders 24-hour time in Tehran', () => {
    expect(j.formatTime('2026-11-23T09:30:00+03:30')).toBe('۰۹:۳۰')
    expect(j.formatTime('2026-11-23T17:05:00+03:30')).toBe('۱۷:۰۵')
  })

  it('never emits an AM/PM marker', () => {
    expect(j.formatTime('2026-11-23T13:00:00+03:30')).not.toMatch(/[AaPp][Mm]/)
  })
})

describe('API date helpers (regression: Jalali leaked into ISO payloads)', () => {
  it('toApiDate returns a Gregorian ISO date', () => {
    expect(j.toApiDate(SAMPLE_ISO)).toBe('2026-11-23')
  })

  it('toApiDate never returns a Jalali year', () => {
    const iso = j.toApiDate(SAMPLE_ISO)
    const year = Number(iso.slice(0, 4))
    expect(year).toBeGreaterThan(2000)
    expect(iso).toMatch(/^\d{4}-\d{2}-\d{2}$/)
  })

  it('todayApiDate returns a plausible Gregorian date', () => {
    const iso = j.todayApiDate()
    expect(iso).toMatch(/^\d{4}-\d{2}-\d{2}$/)
    expect(Number(iso.slice(0, 4))).toBeGreaterThanOrEqual(new Date().getUTCFullYear() - 1)
  })

  it('toApiDate agrees with the Tehran business day', () => {
    // 22:30 UTC is already the next day in Tehran (+03:30).
    expect(j.toApiDate('2026-11-22T22:30:00Z')).toBe('2026-11-23')
  })
})

describe('jalaliToGregorian', () => {
  it('round-trips a Jalali date back to the same Gregorian day', () => {
    const date = j.jalaliToGregorian(1405, 9, 2)
    expect(j.toApiDate(date)).toBe('2026-11-23')
  })

  it('handles the first day of the year', () => {
    const date = j.jalaliToGregorian(1405, 1, 1)
    expect(j.toApiDate(date)).toBe('2026-03-21')
  })

  // Authoritative anchors (Nowruz and well-known national dates).
  it.each([
    [1402, 10, 1, '2023-12-22'],
    [1402, 11, 22, '2024-02-11'],
    [1403, 1, 1, '2024-03-20'],
    [1404, 1, 1, '2025-03-21'],
    [1405, 1, 1, '2026-03-21'],
    [1405, 9, 2, '2026-11-23']
  ])('converts %i/%i/%i to %s', (y, m, d, expected) => {
    const g = jalaliToGregorianParts(y, m, d)
    const iso = `${g.gy}-${String(g.gm).padStart(2, '0')}-${String(g.gd).padStart(2, '0')}`
    expect(iso).toBe(expected)
  })

  it('round-trips every month boundary of a decade', () => {
    for (let year = 1400; year <= 1410; year++) {
      for (let month = 1; month <= 12; month++) {
        const lastDay = j.daysInJalaliMonth(year, month)

        for (const day of [1, 15, lastDay]) {
          const g = jalaliToGregorianParts(year, month, day)
          const back = gregorianToJalaliParts(g.gy, g.gm, g.gd)
          expect(back).toEqual({ jy: year, jm: month, jd: day })
        }
      }
    }
  })

  it('marks leap years correctly', () => {
    expect(isJalaliLeapYear(1403)).toBe(true)
    expect(isJalaliLeapYear(1399)).toBe(true)
    expect(isJalaliLeapYear(1404)).toBe(false)
    expect(isJalaliLeapYear(1405)).toBe(false)
  })
})

describe('Gregorian -> Jalali display', () => {
  // Regression: jalaliday itself drifts by a day in some ranges
  // (2024-01-05 was rendered as 1402/10/16). The arithmetic path must be exact.
  it.each([
    ['2023-12-22T12:00:00+03:30', '۱۴۰۲/۱۰/۰۱'],
    ['2024-01-05T12:00:00+03:30', '۱۴۰۲/۱۰/۱۵'],
    ['2024-02-11T12:00:00+03:30', '۱۴۰۲/۱۱/۲۲'],
    ['2024-03-20T12:00:00+03:30', '۱۴۰۳/۰۱/۰۱'],
    ['2026-11-23T12:00:00+03:30', '۱۴۰۵/۰۹/۰۲']
  ])('formats %s as %s', (iso, expected) => {
    expect(j.formatDate(iso)).toBe(expected)
  })

  it('keeps month lengths consistent with the conversion', () => {
    for (let month = 1; month <= 12; month++) {
      const len = j.daysInJalaliMonth(1405, month)
      const first = jalaliToGregorianParts(1405, month, 1)
      const nextMonth = month === 12 ? 1 : month + 1
      const nextYear = month === 12 ? 1406 : 1405
      const next = jalaliToGregorianParts(nextYear, nextMonth, 1)

      const days = (Date.UTC(next.gy, next.gm - 1, next.gd) - Date.UTC(first.gy, first.gm - 1, first.gd)) / 86_400_000
      expect(days).toBe(len)
    }
  })
})

describe('Iranian weekday mapping', () => {
  it('maps JS Sunday (0) to Iranian Sunday (1)', () => {
    expect(j.toIranianDow(0)).toBe(1)
  })

  it('maps JS Saturday (6) to Iranian Saturday (0)', () => {
    expect(j.toIranianDow(6)).toBe(0)
  })

  it('covers all seven days exactly once', () => {
    const mapped = [0, 1, 2, 3, 4, 5, 6].map(j.toIranianDow)
    expect(new Set(mapped).size).toBe(7)
  })
})

describe('daysInJalaliMonth', () => {
  it('gives 31 days to the first six months', () => {
    for (let m = 1; m <= 6; m++) expect(j.daysInJalaliMonth(1405, m)).toBe(31)
  })

  it('gives 30 days to months 7-11', () => {
    for (let m = 7; m <= 11; m++) expect(j.daysInJalaliMonth(1405, m)).toBe(30)
  })

  it('gives Esfand 30 days in a leap year and 29 otherwise', () => {
    expect(j.daysInJalaliMonth(1403, 12)).toBe(30)
    expect(j.daysInJalaliMonth(1404, 12)).toBe(29)
    expect(j.daysInJalaliMonth(1405, 12)).toBe(29)
  })
})

describe('firstDowOfJalaliMonth', () => {
  it('returns an Iranian weekday index', () => {
    const dow = j.firstDowOfJalaliMonth(1405, 9)
    expect(dow).toBeGreaterThanOrEqual(0)
    expect(dow).toBeLessThanOrEqual(6)
  })
})

describe('relativeDayLabel', () => {
  /**
   * Builds a plain ISO timestamp at noon Tehran, N days from today.
   * Computed with native Date maths so the result never depends on the host
   * timezone or on a calendar plugin's idea of "midnight".
   */
  const isoFromToday = (offsetDays: number) => {
    const ms = Date.now() + offsetDays * 86_400_000 + 3.5 * 3_600_000
    const d = new Date(ms)
    const pad = (n: number) => String(n).padStart(2, '0')
    return `${d.getUTCFullYear()}-${pad(d.getUTCMonth() + 1)}-${pad(d.getUTCDate())}T12:00:00+03:30`
  }

  it('labels today', () => {
    expect(j.relativeDayLabel(isoFromToday(0))).toBe('امروز')
  })

  it('labels tomorrow', () => {
    expect(j.relativeDayLabel(isoFromToday(1))).toBe('فردا')
  })

  it('labels yesterday', () => {
    expect(j.relativeDayLabel(isoFromToday(-1))).toBe('دیروز')
  })

  it('falls back to a full date for anything further out', () => {
    const label = j.relativeDayLabel(isoFromToday(5))
    expect(['امروز', 'فردا', 'دیروز']).not.toContain(label)
    // A full Jalali date: weekday + day + month name + year, in Persian digits.
    expect(label).toMatch(/[۰-۹]/)
    expect(JALALI_MONTHS.some((m) => label.includes(m))).toBe(true)
  })
})

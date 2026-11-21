const PERSIAN_DIGITS = /[۰-۹]/g
const ARABIC_DIGITS = /[٠-٩]/g

/**
 * Keeps a mobile field to Latin digits so the value always matches the API rule.
 * Persian and Arabic digits are what people actually type, so they are converted
 * rather than rejected.
 */
export function normalizePhoneInput(value: string | number | null | undefined): string {
  if (value === null || value === undefined) return ''

  return String(value)
    .replace(PERSIAN_DIGITS, (d) => String(d.charCodeAt(0) - 0x06f0))
    .replace(ARABIC_DIGITS, (d) => String(d.charCodeAt(0) - 0x0660))
    .replace(/\D/g, '')
    .slice(0, 11)
}

export function isValidMobile(value: string): boolean {
  return /^09\d{9}$/.test(value)
}

/** Formats 09121234567 as 0912 123 4567 for display. */
export function formatMobile(value: string): string {
  if (value.length !== 11) return value
  return `${value.slice(0, 4)} ${value.slice(4, 7)} ${value.slice(7)}`
}

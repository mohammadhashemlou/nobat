const PERSIAN_DIGITS = /[۰-۹]/g
const ARABIC_DIGITS = /[٠-٩]/g
const DIGIT_KEY = /^[0-9۰-۹٠-٩]$/
const DIGIT_RUN = /^[0-9۰-۹٠-٩]+$/

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

/**
 * Stops the keystroke before it lands in the field. Normalising after the fact
 * still lets a letter flash on screen for a frame, and that looks broken.
 * Keys that carry no character (arrows, Backspace, F-keys) are left alone.
 */
export function blockNonDigitKeys(event: KeyboardEvent): void {
  if (event.ctrlKey || event.metaKey || event.altKey) return
  if (event.key.length !== 1) return
  if (DIGIT_KEY.test(event.key)) return

  event.preventDefault()
}

/**
 * Covers what keydown cannot see: paste, drag-and-drop, autofill and IME
 * composition. Deletions arrive without a payload and must pass through.
 */
export function blockNonDigitInput(event: Event): void {
  const data = (event as InputEvent).data
  if (data === null || data === undefined) return
  if (DIGIT_RUN.test(data)) return

  event.preventDefault()
}

export function isValidMobile(value: string): boolean {
  return /^09\d{9}$/.test(value)
}

/** Formats 09121234567 as 0912 123 4567 for display. */
export function formatMobile(value: string): string {
  if (value.length !== 11) return value
  return `${value.slice(0, 4)} ${value.slice(4, 7)} ${value.slice(7)}`
}

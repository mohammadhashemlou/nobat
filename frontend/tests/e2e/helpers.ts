import { expect, request as playwrightRequest, test, type APIRequestContext } from '@playwright/test'

export const API_BASE = process.env.E2E_API_URL ?? 'http://localhost:5080'

export const ADMIN_PHONE = '09120000000'
export const ADMIN_PASSWORD = 'Admin@123456'

/** A mobile number that is unique per run — it is the account's username. */
export function uniquePhone(): string {
  return '09' + String(Date.now()).slice(-9)
}

/** Fixed +03:30 offset — Iran has not observed DST since 2022. */
export const TEHRAN_OFFSET_MS = 3.5 * 3_600_000

export function pad(n: number): string {
  return String(n).padStart(2, '0')
}

/** Gregorian YYYY-MM-DD of a Tehran day, `offsetDays` from today. */
export function tehranIsoDay(offsetDays = 0): string {
  const d = new Date(Date.now() + offsetDays * 86_400_000 + TEHRAN_OFFSET_MS)
  return `${d.getUTCFullYear()}-${pad(d.getUTCMonth() + 1)}-${pad(d.getUTCDate())}`
}

export interface SeededCatalog {
  salonId: string
  serviceId: string
  serviceName: string
  barberId: string
  barberName: string
}

/** Authenticates against the API and returns an authorised request context. */
export async function adminApi(): Promise<{ api: APIRequestContext; token: string }> {
  const api = await playwrightRequest.newContext({ baseURL: API_BASE })

  const login = await api.post('/api/v1/auth/login', {
    data: { phoneNumber: ADMIN_PHONE, password: ADMIN_PASSWORD }
  })
  expect(login.ok(), 'admin login must succeed — is the API seeded and running?').toBeTruthy()

  const { accessToken } = await login.json()
  return { api, token: accessToken }
}

// Creates a service and a barber with a 7-day shift, so any date in the picker is
// bookable. Names are unique per run so repeated runs do not collide.
export async function seedCatalog(api: APIRequestContext, token: string): Promise<SeededCatalog> {
  const auth = { Authorization: `Bearer ${token}` }
  const salon = await (await api.get('/api/v1/salons/default')).json()

  const suffix = Date.now().toString().slice(-6)
  const serviceName = `خدمت تست ${suffix}`
  const barberName = `آرایشگر تست ${suffix}`

  const serviceRes = await api.post('/api/v1/services', {
    headers: auth,
    data: {
      salonId: salon.id,
      name: serviceName,
      description: 'خدمت ساخته‌شده برای تست خودکار',
      durationMinutes: 45,
      price: 250000
    }
  })
  expect(serviceRes.ok()).toBeTruthy()
  const service = await serviceRes.json()

  const barberRes = await api.post('/api/v1/barbers', {
    headers: auth,
    data: {
      salonId: salon.id,
      fullName: barberName,
      phoneNumber: uniquePhone(),
      password: 'Barber@12345',
      bio: 'آرایشگر تست',
      serviceIds: [service.id]
    }
  })
  expect(barberRes.ok()).toBeTruthy()
  const barber = await barberRes.json()

  const items = Array.from({ length: 7 }, (_, dayOfWeek) => ({
    dayOfWeek,
    startTime: '09:00',
    endTime: '18:00',
    breakStart: '13:00',
    breakEnd: '14:00'
  }))

  const hoursRes = await api.put(`/api/v1/barbers/${barber.id}/working-hours`, {
    headers: auth,
    data: { items }
  })
  expect(hoursRes.ok()).toBeTruthy()

  return {
    salonId: salon.id,
    serviceId: service.id,
    serviceName,
    barberId: barber.id,
    barberName
  }
}

/** Registers a brand-new customer through the API and returns their credentials. */
export async function createCustomer(
  api: APIRequestContext
): Promise<{ phoneNumber: string; password: string }> {
  const phoneNumber = uniquePhone()
  const password = 'Test@12345'

  const res = await api.post('/api/v1/auth/register', {
    data: {
      fullName: 'مشتری تست',
      phoneNumber,
      password,
      confirmPassword: password,
      role: 2
    }
  })
  expect(res.ok()).toBeTruthy()

  return { phoneNumber, password }
}

// Waits for the hydration flag set by app/plugins/hydration.client.ts. Acting on
// server-rendered markup too early is the classic Nuxt E2E flake: clicks have no
// listeners and typed text is discarded by the first client render.
export async function waitForApp(
  page: import('@playwright/test').Page,
  timeout = 60_000
): Promise<void> {
  await page.locator('html[data-hydrated="true"]').waitFor({ state: 'attached', timeout })
}

/** Navigates and waits for hydration — use this instead of a bare `page.goto`. */
export async function gotoApp(
  page: import('@playwright/test').Page,
  path: string
): Promise<void> {
  await page.goto(path)
  await waitForApp(page)
}

/** Signs in through the UI and waits for the post-login redirect (panel or /my). */
export async function uiLogin(
  page: import('@playwright/test').Page,
  phoneNumber: string,
  password: string
): Promise<void> {
  await gotoApp(page, '/login')
  await page.getByLabel(/شماره موبایل/).fill(phoneNumber)
  await page.getByLabel(/رمز عبور/).fill(password)
  await page.getByRole('button', { name: /ورود/ }).click()
  await expect(page).toHaveURL(/\/panel|\/my/, { timeout: 30_000 })
}

// Picks a Gregorian ISO day in the Jalali picker, advancing the month if needed.
export async function pickDay(page: import('@playwright/test').Page, iso: string): Promise<void> {
  const cell = page.locator(`[data-testid="day-cell"][data-iso="${iso}"]`)

  for (let attempt = 0; attempt < 3; attempt++) {
    if ((await cell.count()) > 0) break
    await page.getByRole('button', { name: 'ماه بعد' }).click()
    await page.waitForTimeout(300)
  }

  await expect(cell).toBeVisible({ timeout: 20_000 })
  await cell.click()
}

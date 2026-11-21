import { expect, request as playwrightRequest, test, type APIRequestContext } from '@playwright/test'

export const API_BASE = process.env.E2E_API_URL ?? 'http://localhost:5080'

export const ADMIN_EMAIL = 'admin@nobat.local'
export const ADMIN_PASSWORD = 'Admin@123456'

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
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD }
  })
  expect(login.ok(), 'admin login must succeed — is the API seeded and running?').toBeTruthy()

  const { accessToken } = await login.json()
  return { api, token: accessToken }
}

/**
 * Creates a service + barber with a 7-day shift so any date in the picker is bookable.
 * Names are unique per run, which keeps repeated runs from colliding.
 */
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
      email: `barber-${suffix}-${Math.floor(Math.random() * 1000)}@test.local`,
      password: 'Barber@12345',
      phoneNumber: '09121111111',
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
export async function createCustomer(api: APIRequestContext): Promise<{ email: string; password: string }> {
  const email = `e2e-${Date.now()}-${Math.floor(Math.random() * 1000)}@test.local`
  const password = 'Test@12345'

  const res = await api.post('/api/v1/auth/register', {
    data: {
      fullName: 'مشتری تست',
      email,
      password,
      confirmPassword: password,
      phoneNumber: '09120000000',
      role: 2
    }
  })
  expect(res.ok()).toBeTruthy()

  return { email, password }
}

/**
 * Waits until Nuxt has hydrated the page.
 *
 * Server-rendered markup is visible before Vue takes over. Acting on it too early is
 * the classic source of flaky Nuxt E2E tests: clicks hit elements with no listeners
 * yet, and text typed into a `v-model` input is thrown away when the first client
 * render resets it to the still-empty reactive state. `app/plugins/hydration.client.ts`
 * sets the flag this waits for.
 */
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

/** Signs in through the UI and waits for the post-login redirect (dashboard or /my). */
export async function uiLogin(
  page: import('@playwright/test').Page,
  email: string,
  password: string
): Promise<void> {
  await gotoApp(page, '/login')
  await page.getByLabel(/ایمیل/).fill(email)
  await page.getByLabel(/رمز عبور/).fill(password)
  await page.getByRole('button', { name: /ورود/ }).click()
  await expect(page).toHaveURL(/\/dashboard|\/my/, { timeout: 30_000 })
}

/**
 * Selects a Gregorian ISO day in the Jalali date picker.
 * The calendar opens on the current Jalali month, so advance it when needed.
 */
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

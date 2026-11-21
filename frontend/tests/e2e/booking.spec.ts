import { expect, test } from '@playwright/test'
import {
  adminApi,
  gotoApp,
  createCustomer,
  seedCatalog,
  pickDay,
  tehranIsoDay,
  uiLogin,
  type SeededCatalog
} from './helpers'

test.describe.configure({ mode: 'serial' })

let catalog: SeededCatalog
let customer: { email: string; password: string }

test.beforeAll(async () => {
  const { api, token } = await adminApi()
  catalog = await seedCatalog(api, token)
  customer = await createCustomer(api)
  await api.dispose()
})

test.describe('booking flow', () => {
  test('a customer books a slot end to end', async ({ page }) => {
    const consoleErrors: string[] = []
    page.on('pageerror', (e) => consoleErrors.push(e.message))
    page.on('console', (m) => {
      if (m.type() === 'error') consoleErrors.push(m.text())
    })

    await uiLogin(page, customer.email, customer.password)
    await gotoApp(page, '/booking')

    // Step 1 — picking a service advances the wizard automatically.
    await page.getByText(catalog.serviceName, { exact: false }).first().click()

    // Step 2 — same for the barber.
    await page.getByText(catalog.barberName, { exact: false }).first().click()

    // Step 3 — date + slot
    await pickDay(page, tehranIsoDay(3))

    const slots = page.locator('.slot-btn')
    await expect(slots.first()).toBeVisible({ timeout: 30_000 })
    const slotCount = await slots.count()
    expect(slotCount).toBeGreaterThan(0)

    const chosenSlot = await slots.nth(1).innerText()
    await slots.nth(1).click()
    await page.getByRole('button', { name: 'ادامه' }).click()

    // Step 4 — confirm
    await page.getByRole('button', { name: /ثبت نوبت/ }).first().click()

    // Success screen
    await expect(page.getByText('نوبت شما ثبت شد!')).toBeVisible({ timeout: 30_000 })
    await expect(page.locator('body')).toContainText(catalog.barberName)
    await expect(page.locator('body')).toContainText(catalog.serviceName)
    await expect(page.locator('body')).toContainText(chosenSlot.trim())

    // No runtime errors anywhere in the journey.
    expect(consoleErrors, `console errors: ${consoleErrors.join(' | ')}`).toHaveLength(0)
  })

  test('the booked slot is no longer offered', async ({ page }) => {
    await uiLogin(page, customer.email, customer.password)
    await gotoApp(page, '/booking')

    await page.getByText(catalog.serviceName, { exact: false }).first().click()
    await page.getByText(catalog.barberName, { exact: false }).first().click()

    await pickDay(page, tehranIsoDay(3))

    const slots = page.locator('.slot-btn')
    await expect(slots.first()).toBeVisible({ timeout: 30_000 })

    // The earlier booking consumed one slot, so fewer than a full day remains.
    await expect.poll(async () => slots.count(), { timeout: 20_000 }).toBeLessThan(28)
  })

  test('the appointment appears in the customer history', async ({ page }) => {
    await uiLogin(page, customer.email, customer.password)
    await gotoApp(page, '/my')

    await expect(page.locator('body')).toContainText(catalog.serviceName, { timeout: 30_000 })
    await expect(page.locator('body')).toContainText(catalog.barberName)
  })

  test('the customer can cancel an upcoming appointment', async ({ page }) => {
    await uiLogin(page, customer.email, customer.password)
    await gotoApp(page, '/my')

    // Cancelling is a single click — the page confirms with a toast, no dialog.
    const cancelButton = page.getByRole('button', { name: 'لغو نوبت' }).first()
    await expect(cancelButton).toBeVisible({ timeout: 30_000 })
    await cancelButton.click()

    await expect(page.locator('body')).toContainText('نوبت لغو شد', { timeout: 20_000 })

    // Once cancelled the appointment moves out of "upcoming", so its action is gone.
    await expect(page.getByRole('button', { name: 'لغو نوبت' })).toHaveCount(0, { timeout: 20_000 })
  })
})

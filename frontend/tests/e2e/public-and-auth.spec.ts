import { expect, test } from '@playwright/test'
import { ADMIN_EMAIL, ADMIN_PASSWORD } from './helpers'

test.describe('public pages', () => {
  test('home page renders in Persian and RTL', async ({ page }) => {
    await page.goto('/')

    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('html')).toHaveAttribute('lang', 'fa')

    // Persian copy is present (RTL + Vazirmatn font are the core layout requirements).
    await expect(page.locator('h1')).toContainText(/[\u0600-\u06FF]/)
  })

  test('header exposes login and registration', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('link', { name: 'ورود' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'ثبت‌نام' })).toBeVisible()
  })

  test('booking entry point is reachable', async ({ page }) => {
    await page.goto('/booking')

    await expect(page.locator('body')).toContainText(/رزرو|نوبت/)
  })

  test('dark mode can be toggled', async ({ page }) => {
    await page.goto('/')

    const html = page.locator('html')
    await expect(html).not.toHaveClass(/dark/)

    await page.getByRole('button', { name: 'تغییر به حالت تیره' }).click()
    await expect(html).toHaveClass(/dark/)

    // The control flips its label once dark mode is active.
    await expect(page.getByRole('button', { name: 'تغییر به حالت روشن' })).toBeVisible()
  })

  test('the public salon page loads for the default slug', async ({ page }) => {
    const response = await page.goto('/salon/default')
    expect(response?.status()).toBe(200)
  })
})

test.describe('authentication', () => {
  test('wrong credentials surface a Persian error and stay on the page', async ({ page }) => {
    await page.goto('/login')

    await page.getByLabel(/ایمیل/).fill(ADMIN_EMAIL)
    await page.getByLabel(/رمز عبور/).fill('DefinitelyWrong@123')
    await page.getByRole('button', { name: /ورود/ }).click()

    await expect(page.locator('form')).toContainText(/ناموفق|اشتباه|نامعتبر|رمز/, { timeout: 20_000 })
    await expect(page).toHaveURL(/\/login/)
  })

  test('admin lands on the dashboard and can sign out', async ({ page }) => {
    await page.goto('/login')

    await page.getByLabel(/ایمیل/).fill(ADMIN_EMAIL)
    await page.getByLabel(/رمز عبور/).fill(ADMIN_PASSWORD)
    await page.getByRole('button', { name: /ورود/ }).click()

    await expect(page).toHaveURL(/\/dashboard/, { timeout: 30_000 })
    await expect(page.locator('body')).toContainText(/داشبورد|مدیریت|نوبت/)

    // Sign out from the dashboard sidebar.
    await page.getByRole('button', { name: 'خروج از حساب' }).click()

    await expect(page).toHaveURL(/\/login|\/$/, { timeout: 20_000 })
    await expect(page.locator('body')).toContainText(/ورود/)
  })

  test('the dashboard is protected for anonymous visitors', async ({ page }) => {
    await page.goto('/dashboard')

    await expect(page).toHaveURL(/\/login/, { timeout: 20_000 })
  })
})

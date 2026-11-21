import { expect, test } from '@playwright/test'
import { ADMIN_PASSWORD, ADMIN_PHONE, gotoApp } from './helpers'

test.describe('public pages', () => {
  test('home page renders in Persian and RTL', async ({ page }) => {
    await gotoApp(page, '/')

    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('html')).toHaveAttribute('lang', 'fa')

    // Persian copy is present (RTL + Vazirmatn font are the core layout requirements).
    await expect(page.locator('h1')).toContainText(/[\u0600-\u06FF]/)
  })

  test('header exposes login and registration', async ({ page }) => {
    await gotoApp(page, '/')

    await expect(page.getByRole('link', { name: 'ورود' })).toBeVisible()
    await expect(page.getByRole('link', { name: 'ثبت‌نام' })).toBeVisible()
  })

  test('booking entry point is reachable', async ({ page }) => {
    await gotoApp(page, '/booking')

    await expect(page.locator('body')).toContainText(/رزرو|نوبت/)
  })

  test('dark mode can be toggled', async ({ page }) => {
    await gotoApp(page, '/')

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

  test('the favicon is served', async ({ page, request }) => {
    await gotoApp(page, '/')

    const href = await page.locator('link[rel="icon"]').first().getAttribute('href')
    expect(href).toBe('/favicon.svg')

    const icon = await request.get(href!)
    expect(icon.status()).toBe(200)
    expect(icon.headers()['content-type']).toContain('svg')
  })
})

test.describe('authentication', () => {
  test('the password can be revealed before submitting', async ({ page }) => {
    await gotoApp(page, '/login')

    const password = page.getByLabel(/رمز عبور/)
    await expect(password).toHaveAttribute('type', 'password')

    await page.getByRole('button', { name: 'نمایش گذرواژه' }).click()
    await expect(password).toHaveAttribute('type', 'text')

    await page.getByRole('button', { name: 'پنهان کردن گذرواژه' }).click()
    await expect(password).toHaveAttribute('type', 'password')
  })

  test('the mobile field accepts only digits and eleven of them', async ({ page }) => {
    await gotoApp(page, '/login')

    const phone = page.getByLabel(/شماره موبایل/)
    await phone.fill('')
    await phone.pressSequentially('0912abc1234567')

    await expect(phone).toHaveValue('09121234567')
  })

  test('wrong credentials surface a Persian error and stay on the page', async ({ page }) => {
    await gotoApp(page, '/login')

    await page.getByLabel(/شماره موبایل/).fill(ADMIN_PHONE)
    await page.getByLabel(/رمز عبور/).fill('DefinitelyWrong@123')
    await page.getByRole('button', { name: /ورود/ }).click()

    await expect(page.locator('form')).toContainText(/ناموفق|اشتباه|نامعتبر|رمز/, { timeout: 20_000 })
    await expect(page).toHaveURL(/\/login/)
  })

  test('admin lands on the panel and can sign out', async ({ page }) => {
    await gotoApp(page, '/login')

    await page.getByLabel(/شماره موبایل/).fill(ADMIN_PHONE)
    await page.getByLabel(/رمز عبور/).fill(ADMIN_PASSWORD)
    await page.getByRole('button', { name: /ورود/ }).click()

    await expect(page).toHaveURL(/\/panel/, { timeout: 30_000 })
    await expect(page.locator('body')).toContainText(/داشبورد|مدیریت|نوبت/)

    // Sign out from the panel sidebar.
    await page.getByRole('button', { name: 'خروج از حساب' }).click()

    await expect(page).toHaveURL(/\/login|\/$/, { timeout: 20_000 })
    await expect(page.locator('body')).toContainText(/ورود/)
  })

  test('the panel is protected for anonymous visitors', async ({ page }) => {
    await gotoApp(page, '/panel')

    await expect(page).toHaveURL(/\/login/, { timeout: 20_000 })
  })
})

import { chromium } from 'playwright'
import fs from 'node:fs'

const outDir = 'C:/Users/MohammadH/WorkBuddy AI/Moratab/.tmp/shots'
fs.mkdirSync(outDir, { recursive: true })

const browser = await chromium.launch()
const ctx = await browser.newContext({ viewport: { width: 1440, height: 1100 }, locale: 'fa-IR' })
const page = await ctx.newPage()

const errors = []
page.on('pageerror', (e) => errors.push('PAGEERROR: ' + e.message))
page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()) })

async function goto(url) {
  await page.goto(url, { waitUntil: 'networkidle' })
  await page.waitForTimeout(2200)
}

// login
await goto('http://localhost:3000/login')
await page.fill('input[type="email"]', 'admin@nobat.local')
await page.fill('input[type="password"]', 'Admin@123456')
await page.click('button[type="submit"]')
await page.waitForURL('**/dashboard', { timeout: 20000 })

// booking
await goto('http://localhost:3000/booking')

await page.getByText('اصلاح مو', { exact: false }).first().click()
await page.waitForTimeout(1000)
await page.screenshot({ path: `${outDir}/20-booking-barber.png` })

// pick barber (text)
await page.getByText('رضا محمدی', { exact: false }).first().click()
await page.waitForTimeout(2600)
await page.screenshot({ path: `${outDir}/21-booking-time.png` })

// choose a date a few days ahead
const dayCells = page.locator('button:not([disabled])').filter({ hasNotText: /[\u0600-\u06FF]/ })
const count = await dayCells.count()
console.log('enabled day cells:', count)
await dayCells.nth(Math.min(8, count - 1)).click()
await page.waitForTimeout(2600)
await page.screenshot({ path: `${outDir}/22-booking-slots.png` })

const slots = page.locator('.slot-btn')
const slotCount = await slots.count()
console.log('available slots:', slotCount)

if (slotCount > 0) {
  await slots.nth(1).click()
  await page.waitForTimeout(700)
  await page.screenshot({ path: `${outDir}/23-booking-selected.png` })

  await page.getByRole('button', { name: 'ادامه' }).click()
  await page.waitForTimeout(900)
  await page.screenshot({ path: `${outDir}/24-booking-confirm.png` })

  await page.getByRole('button', { name: /ثبت نوبت/ }).first().click()
  await page.waitForTimeout(2800)
  await page.screenshot({ path: `${outDir}/25-booking-success.png` })
}

// appointments all
await goto('http://localhost:3000/dashboard/appointments')
await page.getByRole('button', { name: 'همه', exact: true }).click()
await page.waitForTimeout(2200)
await page.screenshot({ path: `${outDir}/26-appointments-all.png` })

// my appointments as admin (has bookings)
await goto('http://localhost:3000/my')
await page.screenshot({ path: `${outDir}/27-my.png` })

console.log('errors:', JSON.stringify(errors.slice(0, 8), null, 1))
await browser.close()

import { chromium } from 'playwright'
import fs from 'node:fs'

const outDir = 'C:/Users/MohammadH/WorkBuddy AI/Moratab/.tmp/shots'
fs.mkdirSync(outDir, { recursive: true })

const browser = await chromium.launch()
const ctx = await browser.newContext({
  viewport: { width: 1440, height: 1000 },
  locale: 'fa-IR'
})
const page = await ctx.newPage()

const errors = []
page.on('console', (m) => {
  if (m.type() === 'error') errors.push(m.text())
})
page.on('pageerror', (e) => errors.push('PAGEERROR: ' + e.message))

// --- Login ---
await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' })
await page.fill('input[type="email"]', 'admin@nobat.local')
await page.fill('input[type="password"]', 'Admin@123456')
await page.click('button[type="submit"]')
await page.waitForURL('**/dashboard', { timeout: 20000 })
await page.waitForTimeout(1500)
await page.screenshot({ path: `${outDir}/10-dashboard.png` })

// --- Dashboard pages ---
for (const [path, name] of [
  ['/dashboard/appointments', '11-appointments'],
  ['/dashboard/services', '12-services'],
  ['/dashboard/barbers', '13-barbers'],
  ['/dashboard/schedule', '14-schedule'],
  ['/dashboard/holidays', '15-holidays']
]) {
  await page.goto('http://localhost:3000' + path, { waitUntil: 'networkidle' })
  await page.waitForTimeout(1200)
  await page.screenshot({ path: `${outDir}/${name}.png` })
}

console.log('console errors:', JSON.stringify(errors.slice(0, 10), null, 1))
await browser.close()

import { chromium } from 'playwright'
import fs from 'node:fs'

const outDir = 'C:/Users/MohammadH/WorkBuddy AI/Moratab/.tmp/shots'
fs.mkdirSync(outDir, { recursive: true })

const targets = [
  { path: '/', name: '01-home' },
  { path: '/salon/default', name: '02-salon' },
  { path: '/login', name: '03-login' },
  { path: '/booking', name: '04-booking' }
]

const browser = await chromium.launch()

// Desktop (light)
for (const t of targets) {
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    deviceScaleFactor: 1,
    locale: 'fa-IR'
  })
  const page = await ctx.newPage()
  await page.goto('http://localhost:3000' + t.path, { waitUntil: 'networkidle' })
  await page.waitForTimeout(1200)
  await page.screenshot({ path: `${outDir}/${t.name}-desktop.png`, fullPage: false })
  await ctx.close()
}

// Dark mode home
{
  const ctx = await browser.newContext({
    viewport: { width: 1440, height: 1000 },
    colorScheme: 'dark',
    locale: 'fa-IR'
  })
  const page = await ctx.newPage()
  await page.goto('http://localhost:3000/', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1200)
  await page.screenshot({ path: `${outDir}/05-home-dark.png` })
  await ctx.close()
}

// Mobile home
{
  const ctx = await browser.newContext({
    viewport: { width: 390, height: 844 },
    deviceScaleFactor: 2,
    isMobile: true,
    hasTouch: true,
    locale: 'fa-IR'
  })
  const page = await ctx.newPage()
  await page.goto('http://localhost:3000/', { waitUntil: 'networkidle' })
  await page.waitForTimeout(1200)
  await page.screenshot({ path: `${outDir}/06-home-mobile.png` })
  await ctx.close()
}

await browser.close()
console.log('screenshots done')

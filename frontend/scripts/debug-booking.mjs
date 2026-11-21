import { chromium } from 'playwright'

const browser = await chromium.launch()
const ctx = await browser.newContext({ viewport: { width: 1440, height: 1100 }, locale: 'fa-IR' })
const page = await ctx.newPage()

const logs = []
page.on('pageerror', (e) => logs.push('PAGEERROR: ' + e.message))
page.on('console', (m) => logs.push(`[${m.type()}] ${m.text()}`))

await page.goto('http://localhost:3000/login', { waitUntil: 'networkidle' })
await page.fill('input[type="email"]', 'admin@nobat.local')
await page.fill('input[type="password"]', 'Admin@123456')
await page.click('button[type="submit"]')
await page.waitForURL('**/dashboard', { timeout: 20000 })

await page.goto('http://localhost:3000/booking', { waitUntil: 'networkidle' })
await page.waitForTimeout(2000)

const info = await page.evaluate(() => {
  const sections = document.querySelectorAll('section')
  const buttons = document.querySelectorAll('section button')
  return {
    sections: sections.length,
    buttons: buttons.length,
    firstButtonText: buttons[0]?.textContent?.trim().slice(0, 40),
    hydrated: !!document.querySelector('#__nuxt')?.__vue_app__,
    bodyLen: document.body.innerText.length
  }
})
console.log('DOM:', JSON.stringify(info, null, 1))

// try clicking by text
try {
  await page.getByText('اصلاح مو', { exact: false }).first().click({ timeout: 5000 })
  await page.waitForTimeout(1200)
  const after = await page.evaluate(() => document.body.innerText.includes('آرایشگر را انتخاب') || document.body.innerText.includes('آرایشگر'))
  console.log('after text click, step2 visible:', after)
  const stepText = await page.evaluate(() => {
    const m = document.body.innerText.match(/خدمت انتخابی|آرایشگر|زمان/g)
    return m ? m.slice(0, 6) : []
  })
  console.log('markers:', stepText)
} catch (e) {
  console.log('click failed:', e.message)
}

console.log('logs:', JSON.stringify(logs.filter(l => !l.includes('[vite]') && !l.includes('DevTools')).slice(0, 12), null, 1))
await browser.close()

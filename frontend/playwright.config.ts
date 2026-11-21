import { defineConfig, devices } from '@playwright/test'

// Point E2E_BASE_URL / E2E_API_URL at an already-running stack, or let Playwright
// build the app and serve the production output on :3000 against the API on :5080.
// The production build matters: the dev server ships unbundled ES modules, which on
// a cold CI runner can arrive later than the actionability timeout.
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:3000'

export default defineConfig({
  testDir: './tests/e2e',
  outputDir: './test-results',
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 90_000,
  expect: { timeout: 20_000 },
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : [['list']],

  use: {
    baseURL,
    locale: 'fa-IR',
    viewport: { width: 1440, height: 1000 },
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    actionTimeout: 20_000
  },

  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],

  webServer: process.env.E2E_BASE_URL
    ? undefined
    : {
        command: 'npm run build && npm run preview',
        url: 'http://localhost:3000',
        reuseExistingServer: !process.env.CI,
        timeout: 420_000
      }
})

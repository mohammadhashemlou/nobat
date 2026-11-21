import { defineConfig, devices } from '@playwright/test'

/**
 * End-to-end configuration.
 *
 * The suite drives the real Nuxt app against the real API, so both must be reachable.
 * Set E2E_BASE_URL / E2E_API_URL to point at an already-running stack (CI, staging);
 * otherwise Playwright builds the app and serves the production output on :3000 and
 * expects the API on :5080.
 *
 * The production build is deliberate: the dev server ships hundreds of unbundled
 * ES modules, so on a cold CI runner the client bundle can take longer to arrive
 * than the actionability timeout. Serving the real artifact keeps hydration in the
 * sub-second range — and it is what actually ships, so it is what should be tested.
 */
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

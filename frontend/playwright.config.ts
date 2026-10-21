import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  timeout: 30000,
  expect: {
    timeout: 5000
  },
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  workers: process.env['CI'] ? 2 : undefined,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }]
  ],
  use: {
    baseURL: process.env['E2E_BASE_URL'] || 'http://localhost:4200',
    launchOptions: {
      executablePath: process.env['PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH'] || '/usr/bin/chromium',
      args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage', '--disable-gpu']
    },
    trace: 'on-first-retry',
    video: 'off',
    screenshot: 'only-on-failure'
  },
  projects: [
    {
      name: 'Desktop Arabic RTL',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1440, height: 900 },
        locale: 'ar-EG'
      }
    },
    {
      name: 'Mobile Arabic RTL',
      use: {
        ...devices['Pixel 5'],
        viewport: { width: 390, height: 844 },
        locale: 'ar-EG'
      }
    }
  ]
});

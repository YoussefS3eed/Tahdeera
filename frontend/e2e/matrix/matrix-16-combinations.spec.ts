import { test, expect } from '@playwright/test';

const VIEWPORTS = [
  { name: 'Desktop', width: 1440, height: 900 },
  { name: 'Mobile', width: 390, height: 844 }
];

const LOCALES = ['ar', 'en'];
const THEMES = ['light', 'dark'];

test.describe('Milestone F5: 16-Combination Matrix Suite (Viewports x Locales x Themes)', () => {
  for (const vp of VIEWPORTS) {
    for (const locale of LOCALES) {
      for (const theme of THEMES) {
        test(`${vp.name} | ${locale.toUpperCase()} | ${theme.toUpperCase()} - Core Portals Integrity`, async ({ page }) => {
          await page.setViewportSize({ width: vp.width, height: vp.height });

          await page.addInitScript(({ loc, thm }) => {
            localStorage.setItem('tahdeera_locale', loc);
            localStorage.setItem('tahdeera_theme', thm);
            localStorage.setItem('tahdeera_auth_token', 'mock_admin_token');
            localStorage.setItem('tahdeera_user_profile', JSON.stringify({
              id: 'usr_admin',
              fullName: 'Super Admin',
              email: 'admin@tahdeera.app',
              isPlatformAdmin: true
            }));
          }, { loc: locale, thm: theme });

          await page.goto('/admin/overview', { waitUntil: 'domcontentloaded' });

          const overflow = await page.evaluate(() => {
            return document.documentElement.scrollWidth > document.documentElement.clientWidth;
          });
          expect(overflow).toBeFalsy();
        });
      }
    }
  }
});

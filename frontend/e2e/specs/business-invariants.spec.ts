import { test, expect } from '@playwright/test';

test.describe('Milestone F5 Business Invariants & Security Gates', () => {
  test('SCR-04: Non-admin user accessing /admin/* is redirected to /forbidden', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('tahdeera_auth_token', 'teacher_token');
      localStorage.setItem('tahdeera_user_profile', JSON.stringify({
        id: 'usr_teacher',
        fullName: 'أ. أحمد',
        email: 'teacher@tahdeera.app',
        isPlatformAdmin: false
      }));
    });

    await page.goto('/admin/overview');
    await expect(page).toHaveURL(/.*\/forbidden/);
    await expect(page.locator('.forbidden-title')).toContainText('عفواً، هذه الصفحة مخصصة لمدير النظام العام للمنصة فقط');
  });

  test('SCR-20: Impersonation banner displays across workspace tabs and exits back to admin', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('tahdeera_auth_token', 'mock_admin_token');
      localStorage.setItem('tahdeera_user_profile', JSON.stringify({
        id: 'usr_admin',
        fullName: 'مدير المنصة',
        isPlatformAdmin: true
      }));
    });

    await page.goto('/admin/accounts');
    const impersonateBtn = page.locator('.impersonate-btn').first();
    await impersonateBtn.click();

    // Verify redirected to workspace dashboard with sticky amber banner
    await expect(page).toHaveURL(/.*\/workspace\/dashboard/);
    const banner = page.locator('.impersonation-banner');
    await expect(banner).toBeVisible();
    await expect(banner).toContainText('أنت تتصفح حالياً بصلاحية مدير سنتر');

    // Click Exit Impersonation CTA
    const exitBtn = page.locator('.exit-btn');
    await exitBtn.click();

    // Verify redirected back to /admin/accounts and banner dismissed
    await expect(page).toHaveURL(/.*\/admin\/accounts/);
    await expect(page.locator('.impersonation-banner')).not.toBeVisible();
  });

  test('SCR-25 & SCR-03: Support hotlines propagate to lockout screen', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('tahdeera_auth_token', 'mock_teacher_token');
      localStorage.setItem('tahdeera_user_profile', JSON.stringify({
        id: 'usr_teacher',
        fullName: 'أ. محمد',
        email: 'teacher@tahdeera.app',
        isPlatformAdmin: false
      }));
    });
    await page.goto('/locked');
    await expect(page.locator('body')).toContainText('01272785618');
    await expect(page.locator('body')).toContainText('01019482687');
  });
});

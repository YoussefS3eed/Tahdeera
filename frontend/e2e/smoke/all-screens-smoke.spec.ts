import { test, expect } from '@playwright/test';

const SCREENS = [
  // Public Portal (SCR-01 .. SCR-04)
  { id: 'SCR-01', path: '/', name: 'Landing Page' },
  { id: 'SCR-02', path: '/login', name: 'Authentication / Login' },
  { id: 'SCR-03', path: '/locked', name: 'Lockout Screen' },
  { id: 'SCR-04', path: '/forbidden', name: '403 Forbidden Stub' },

  // Center / Teacher Workspace (SCR-05 .. SCR-18)
  { id: 'SCR-05', path: '/workspace/dashboard', name: 'Workspace Dashboard' },
  { id: 'SCR-06', path: '/workspace/students', name: 'Students Roster' },
  { id: 'SCR-07', path: '/workspace/attendance', name: 'Attendance Scanner' },
  { id: 'SCR-08', path: '/workspace/groups', name: 'Groups & Schedules' },
  { id: 'SCR-09', path: '/workspace/fees', name: 'Student Fees & Payments' },
  { id: 'SCR-10', path: '/workspace/expenses', name: 'Center Expenses' },
  { id: 'SCR-11', path: '/workspace/staff', name: 'Staff Management' },
  { id: 'SCR-12', path: '/workspace/whatsapp', name: 'Workspace WhatsApp' },
  { id: 'SCR-13', path: '/workspace/exams', name: 'Exams & Question Bank' },
  { id: 'SCR-14', path: '/workspace/homework', name: 'Homework & Assignments' },
  { id: 'SCR-15', path: '/workspace/library', name: 'Educational Library' },
  { id: 'SCR-16', path: '/workspace/reports', name: 'Reports & Analytics' },
  { id: 'SCR-17', path: '/workspace/settings', name: 'Center Settings' },
  { id: 'SCR-18', path: '/workspace/profile', name: 'User Profile' },

  // Super Admin Platform Portal (SCR-19 .. SCR-26)
  { id: 'SCR-19', path: '/admin/overview', name: 'Platform Overview' },
  { id: 'SCR-20', path: '/admin/accounts', name: 'Accounts Roster' },
  { id: 'SCR-21', path: '/admin/subscriptions', name: 'Subscriptions & Renewals' },
  { id: 'SCR-22', path: '/admin/packages', name: 'Packages & Feature Flags' },
  { id: 'SCR-23', path: '/admin/revenue', name: 'Platform Revenue Ledger' },
  { id: 'SCR-24', path: '/admin/whatsapp', name: 'Central WhatsApp Gateway' },
  { id: 'SCR-25', path: '/admin/settings', name: 'Global Settings & Hotlines' },
  { id: 'SCR-26', path: '/admin/audit-logs', name: 'Immutable Audit Log' }
];

test.describe('Milestone F5: 26-Screen Comprehensive Smoke Suite', () => {
  for (const screen of SCREENS) {
    test(`Verify ${screen.id} (${screen.name}) loads cleanly with zero horizontal overflow`, async ({ page }) => {
      const consoleErrors: string[] = [];
      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });

      // Emulate platform admin session for admin routes
      if (screen.path.startsWith('/admin')) {
        await page.addInitScript(() => {
          localStorage.setItem('tahdeera_auth_token', 'mock_admin_token');
          localStorage.setItem('tahdeera_user_profile', JSON.stringify({
            id: 'usr_admin',
            fullName: 'مدير المنصة العام',
            email: 'admin@tahdeera.app',
            phone: '01272785618',
            initials: 'م.ع',
            isPlatformAdmin: true
          }));
        });
      }

      await page.goto(screen.path, { waitUntil: 'domcontentloaded' });

      // Invariant: Zero uncaught page errors
      expect(consoleErrors.length).toBe(0);

      // Invariant: Zero horizontal scroll overflow
      const hasHorizontalOverflow = await page.evaluate(() => {
        return document.documentElement.scrollWidth > document.documentElement.clientWidth;
      });
      expect(hasHorizontalOverflow).toBeFalsy();
    });
  }
});

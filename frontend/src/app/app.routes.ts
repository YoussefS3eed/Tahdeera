import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { lockoutGuard } from './core/guards/lockout.guard';
import { superAdminGuard } from './core/guards/super-admin.guard';
import { WorkspaceRole } from './core/models/auth.models';
import { FoundationHarnessComponent } from './features/harness/foundation-harness.component';
import { ForbiddenStubComponent } from './features/stubs/forbidden-stub.component';

export const routes: Routes = [
  // Public Portal Layout
  {
    path: '',
    loadComponent: () =>
      import('./core/layout/public-layout/public-layout.component').then(
        (m) => m.PublicLayoutComponent
      ),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/public/landing/landing.component').then(
            (m) => m.LandingComponent
          ),
        pathMatch: 'full',
        title: 'تحضيرة — حضورك الذكي'
      },
      {
        path: 'login',
        loadComponent: () =>
          import('./features/public/login/login.component').then(
            (m) => m.LoginComponent
          ),
        title: 'تسجيل الدخول — تحضيرة'
      }
    ]
  },

  // Student Exam Portal (DEC-02)
  {
    path: 'portal/exam/:id',
    loadComponent: () =>
      import('./features/portal/exam/portal-exam.component').then(
        (m) => m.PortalExamComponent
      ),
    title: 'بوابة الاختبارات الإلكترونية — تحضيرة'
  },

  // Foundation Test Harness
  {
    path: 'harness',
    component: FoundationHarnessComponent,
    title: 'Tahdeera Foundation Harness'
  },

  // Top-level locked alias
  {
    path: 'locked',
    redirectTo: 'workspace/lockout',
    pathMatch: 'full'
  },

  // Workspace Shell Layout
  {
    path: 'workspace',
    loadComponent: () =>
      import('./core/layout/workspace-layout/workspace-layout.component').then(
        (m) => m.WorkspaceLayoutComponent
      ),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/workspace/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        pathMatch: 'full',
        title: 'لوحة التحكم — تحضيرة'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/workspace/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'لوحة التحكم — تحضيرة'
      },
      {
        path: 'lockout',
        loadComponent: () =>
          import('./features/workspace/lockout/lockout.component').then(
            (m) => m.LockoutComponent
          ),
        canActivate: [authGuard],
        title: 'حالة الحساب — تحضيرة'
      },
      // SCR-05: Live NFC Attendance Station
      {
        path: 'nfc-station',
        loadComponent: () =>
          import('./features/workspace/nfc-station/nfc-station.component').then(
            (m) => m.NfcStationComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'محطة الحضور المباشر — تحضيرة'
      },
      // SCR-06: Daily Attendance Log & Overrides
      {
        path: 'attendance',
        loadComponent: () =>
          import('./features/workspace/attendance/attendance.component').then(
            (m) => m.AttendanceComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الحضور والغياب اليومي — تحضيرة'
      },
      // SCR-07: Students Directory
      {
        path: 'students',
        loadComponent: () =>
          import('./features/workspace/students/students.component').then(
            (m) => m.StudentsComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'سجل الطلاب — تحضيرة'
      },
      // SCR-08: Unified 360° Student Profile
      {
        path: 'students/:id',
        loadComponent: () =>
          import('./features/workspace/student-profile/student-profile.component').then(
            (m) => m.StudentProfileComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الملف الأكاديمي الشامل 360° — تحضيرة'
      },
      // SCR-09: NFC Cards Registry
      {
        path: 'cards',
        loadComponent: () =>
          import('./features/workspace/cards/cards.component').then(
            (m) => m.CardsComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'بطاقات الحضور NFC — تحضيرة'
      },
      // SCR-10: Study Groups & Assistant Delegation
      {
        path: 'groups',
        loadComponent: () =>
          import('./features/workspace/groups/groups.component').then(
            (m) => m.GroupsComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'المجموعات الدراسية — تحضيرة'
      },
      // SCR-11: Weekly Schedule & Lifecycle
      {
        path: 'schedule',
        loadComponent: () =>
          import('./features/workspace/schedule/schedule.component').then(
            (m) => m.ScheduleComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الجدول والمواعيد — تحضيرة'
      },
      // SCR-12: Payments & Invoicing
      {
        path: 'payments',
        loadComponent: () =>
          import('./features/workspace/payments/payments.component').then(
            (m) => m.PaymentsComponent
          ),
        canActivate: [authGuard, lockoutGuard, roleGuard],
        data: { requiredRoles: ['Owner', 'Admin', 'Accountant'] as WorkspaceRole[] },
        title: 'الرسوم والمدفوعات — تحضيرة'
      },
      // SCR-13: WhatsApp Communication Center
      {
        path: 'whatsapp',
        loadComponent: () =>
          import('./features/workspace/whatsapp/whatsapp.component').then(
            (m) => m.WhatsAppComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'أولياء الأمور وواتساب — تحضيرة'
      },
      // SCR-14: LMS & Question Bank
      {
        path: 'lms',
        loadComponent: () =>
          import('./features/workspace/lms/lms.component').then(
            (m) => m.LmsComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الامتحانات والواجبات — تحضيرة'
      },
      // SCR-16: Standard Reports
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/workspace/reports/reports.component').then(
            (m) => m.ReportsComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'التقارير والإحصائيات — تحضيرة'
      },
      // SCR-17: Team RBAC
      {
        path: 'team',
        loadComponent: () =>
          import('./features/workspace/team/team.component').then(
            (m) => m.TeamComponent
          ),
        canActivate: [authGuard, lockoutGuard, roleGuard],
        data: { requiredRoles: ['Owner', 'Admin'] as WorkspaceRole[] },
        title: 'فريق العمل والصلاحيات — تحضيرة'
      },
      // SCR-18: Workspace Settings
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/workspace/settings/settings.component').then(
            (m) => m.SettingsComponent
          ),
        canActivate: [authGuard, lockoutGuard, roleGuard],
        data: { requiredRoles: ['Owner', 'Admin'] as WorkspaceRole[] },
        title: 'إعدادات السنتر — تحضيرة'
      },
      // SCR-15: AI Assistant Business Intelligence
      {
        path: 'ai-assistant',
        loadComponent: () =>
          import('./features/workspace/ai-assistant/ai-assistant.component').then(
            (m) => m.AiAssistantComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'مساعد الذكاء الاصطناعي — تحضيرة'
      },
      // Guard & Security Test Routes
      {
        path: 'forbidden',
        component: ForbiddenStubComponent,
        title: 'Access Forbidden - Tahdeera'
      },
      {
        path: 'protected',
        canActivate: [authGuard],
        component: FoundationHarnessComponent
      },
      {
        path: 'teachers-only',
        canActivate: [roleGuard],
        data: { requiredRoles: ['Teacher', 'Owner', 'Admin'] as WorkspaceRole[] },
        component: FoundationHarnessComponent
      },
      {
        path: 'subscribed-only',
        canActivate: [lockoutGuard],
        component: FoundationHarnessComponent
      }
    ]
  },


  // Top-level Forbidden Route (Clarification A5)
  {
    path: 'forbidden',
    loadComponent: () =>
      import('./features/forbidden/forbidden-stub.component').then(
        (m) => m.ForbiddenStubComponent
      ),
    title: '403 — غير مصرح بالوصول'
  },

  // Super Admin Platform Management (SCR-19 through SCR-26)
  {
    path: 'admin',
    loadComponent: () =>
      import('./core/layout/admin-layout/admin-layout.component').then(
        (m) => m.AdminLayoutComponent
      ),
    canActivate: [superAdminGuard],
    children: [
      {
        path: '',
        redirectTo: 'overview',
        pathMatch: 'full'
      },
      {
        path: 'overview',
        loadComponent: () =>
          import('./features/admin/overview/overview.component').then(
            (m) => m.AdminOverviewComponent
          ),
        title: 'نظرة عامة — إدارة المنصة'
      },
      {
        path: 'accounts',
        loadComponent: () =>
          import('./features/admin/accounts/accounts.component').then(
            (m) => m.AccountsComponent
          ),
        title: 'السناتر والمدرسين — إدارة المنصة'
      },
      {
        path: 'subscriptions',
        loadComponent: () =>
          import('./features/admin/subscriptions/subscriptions.component').then(
            (m) => m.SubscriptionsComponent
          ),
        title: 'الاشتراكات والتجديدات — إدارة المنصة'
      },
      {
        path: 'packages',
        loadComponent: () =>
          import('./features/admin/packages/packages.component').then(
            (m) => m.PackagesComponent
          ),
        title: 'الباقات والميزات — إدارة المنصة'
      },
      {
        path: 'revenue',
        loadComponent: () =>
          import('./features/admin/revenue/revenue.component').then(
            (m) => m.RevenueComponent
          ),
        title: 'الإيرادات والتحصيل — إدارة المنصة'
      },
      {
        path: 'whatsapp',
        loadComponent: () =>
          import('./features/admin/whatsapp/whatsapp-admin.component').then(
            (m) => m.WhatsAppAdminComponent
          ),
        title: 'بوابة الواتساب — إدارة المنصة'
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/admin/settings/admin-settings.component').then(
            (m) => m.AdminSettingsComponent
          ),
        title: 'إعدادات المنصة — إدارة المنصة'
      },
      {
        path: 'audit-logs',
        loadComponent: () =>
          import('./features/admin/audit-logs/audit-logs.component').then(
            (m) => m.AuditLogComponent
          ),
        title: 'سجل العمليات — إدارة المنصة'
      }
    ]
  },

  // Fallback Wildcard Route
  {
    path: '**',
    redirectTo: ''
  }
];

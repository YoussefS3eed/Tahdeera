import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { lockoutGuard } from './core/guards/lockout.guard';
import { WorkspaceRole } from './core/enums/domain.enums';
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
      {
        path: 'nfc-station',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'محطة الحضور المباشر — تحضيرة'
      },
      {
        path: 'attendance',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الحضور والغياب اليومي — تحضيرة'
      },
      {
        path: 'students',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'سجل الطلاب — تحضيرة'
      },
      {
        path: 'cards',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'بطاقات الحضور NFC — تحضيرة'
      },
      {
        path: 'groups',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'المجموعات الدراسية — تحضيرة'
      },
      {
        path: 'schedule',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الجدول والمواعيد — تحضيرة'
      },
      {
        path: 'payments',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الرسوم والمدفوعات — تحضيرة'
      },
      {
        path: 'whatsapp',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'أولياء الأمور وواتساب — تحضيرة'
      },
      {
        path: 'lms',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'الامتحانات والواجبات — تحضيرة'
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'التقارير والإحصائيات — تحضيرة'
      },
      {
        path: 'team',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'فريق العمل والصلاحيات — تحضيرة'
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
          ),
        canActivate: [authGuard, lockoutGuard],
        title: 'إعدادات السنتر — تحضيرة'
      },
      {
        path: 'ai-assistant',
        loadComponent: () =>
          import('./features/workspace/placeholder/workspace-placeholder.component').then(
            (m) => m.WorkspacePlaceholderComponent
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
        data: { requiredRoles: [WorkspaceRole.Teacher, WorkspaceRole.Owner, WorkspaceRole.Admin] },
        component: FoundationHarnessComponent
      },
      {
        path: 'subscribed-only',
        canActivate: [lockoutGuard],
        component: FoundationHarnessComponent
      }
    ]
  },

  // Fallback Wildcard Route
  {
    path: '**',
    redirectTo: ''
  }
];

import { Routes } from '@angular/router';
import { FoundationHarnessComponent } from './features/harness/foundation-harness.component';
import { LoginStubComponent } from './features/stubs/login-stub.component';
import { ForbiddenStubComponent } from './features/stubs/forbidden-stub.component';
import { LockoutStubComponent } from './features/stubs/lockout-stub.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { lockoutGuard } from './core/guards/lockout.guard';
import { WorkspaceRole } from './core/enums/domain.enums';

export const routes: Routes = [
  {
    path: '',
    component: FoundationHarnessComponent,
    title: 'Tahdeera Foundation Harness'
  },
  {
    path: 'harness',
    component: FoundationHarnessComponent,
    title: 'Tahdeera Foundation Harness'
  },
  {
    path: 'login',
    component: LoginStubComponent,
    title: 'Login - Tahdeera'
  },
  {
    path: 'workspace/forbidden',
    component: ForbiddenStubComponent,
    title: 'Access Forbidden - Tahdeera'
  },
  {
    path: 'workspace/lockout',
    component: LockoutStubComponent,
    title: 'Workspace Suspended - Tahdeera'
  },
  // Test routes for route guard verification
  {
    path: 'workspace/protected',
    canActivate: [authGuard],
    component: FoundationHarnessComponent
  },
  {
    path: 'workspace/teachers-only',
    canActivate: [roleGuard],
    data: { requiredRoles: [WorkspaceRole.Teacher, WorkspaceRole.Owner, WorkspaceRole.Admin] },
    component: FoundationHarnessComponent
  },
  {
    path: 'workspace/subscribed-only',
    canActivate: [lockoutGuard],
    component: FoundationHarnessComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];

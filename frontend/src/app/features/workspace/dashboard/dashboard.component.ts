import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { DashboardService } from './services/dashboard.service';
import { AuthStore } from '../../../core/stores/auth.store';
import { LocalizationService } from '../../../core/services/localization.service';
import { DashboardOverviewResponse } from '../../../core/models/dashboard.models';
import { AccessDeniedNotificationState } from '../../../core/models/route.models';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, SkeletonLoaderComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: []
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private router = inject(Router);
  readonly authStore = inject(AuthStore);
  readonly localization = inject(LocalizationService);

  readonly isLoading = signal<boolean>(true);
  readonly isRefreshing = signal<boolean>(false);
  readonly dashboardData = signal<DashboardOverviewResponse | null>(null);
  readonly errorMessage = signal<string | null>(null);

  // Client-side one-time notification state for unauthorized role redirects
  readonly unauthorizedAlertVisible = signal<boolean>(false);
  readonly unauthorizedAlertMessageKey = signal<string>('DASHBOARD.UNAUTHORIZED_ROLE_ACCESS');

  readonly canViewFinancials = computed(() => this.authStore.canViewFinancials());
  readonly activeWorkspace = computed(() => this.authStore.activeWorkspace());
  readonly currentUser = computed(() => this.authStore.user());

  get unauthorizedAlertMessage(): string {
    return (
      this.localization.translate(this.unauthorizedAlertMessageKey()) ||
      (this.localization.currentLang() === 'ar'
        ? 'عذرًا، هذا القسم مخصص لمالك الحساب والإدارة فقط.'
        : 'Sorry, this section is restricted to account owners and administrators only.')
    );
  }

  constructor() {
    this.consumeRouterNotificationState();
  }

  ngOnInit(): void {
    this.loadData();
  }

  /**
   * Consumes one-time Router Navigation State delivered by roleGuard.
   * Immediately clears state via history.replaceState so page refresh or back/forward
   * history navigation never re-displays the banner.
   */
  private consumeRouterNotificationState(): void {
    const nav = this.router.getCurrentNavigation();
    const state = (nav?.extras?.state ||
      (typeof history !== 'undefined' ? history.state : undefined)) as AccessDeniedNotificationState | undefined;

    if (state?.accessDenied) {
      this.unauthorizedAlertVisible.set(true);
      if (state.messageKey) {
        this.unauthorizedAlertMessageKey.set(state.messageKey);
      }

      // Immediately scrub state from history to ensure strictly one-time display
      if (typeof history !== 'undefined' && history.replaceState) {
        try {
          const cleanState = { ...history.state };
          delete cleanState.accessDenied;
          delete cleanState.messageKey;
          delete cleanState.attemptedUrl;
          delete cleanState.timestamp;
          history.replaceState(cleanState, '');
        } catch {
          // Graceful fallback in environments with restricted history access
        }
      }
    }
  }

  dismissUnauthorizedAlert(): void {
    this.unauthorizedAlertVisible.set(false);
  }

  loadData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dashboardService.getOverview().subscribe({
      next: data => {
        this.dashboardData.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set(
          this.localization.currentLang() === 'ar'
            ? 'حدث خطأ أثناء تحميل بيانات لوحة التحكم'
            : 'An error occurred while loading dashboard data'
        );
        this.isLoading.set(false);
      }
    });
  }

  refresh(): void {
    if (this.isRefreshing()) return;
    this.isRefreshing.set(true);

    this.dashboardService.getOverview().subscribe({
      next: data => {
        this.dashboardData.set(data);
        this.isRefreshing.set(false);
      },
      error: () => {
        this.isRefreshing.set(false);
      }
    });
  }

  openGlobalSearch(): void {
    // Navigate to students search
    this.router.navigate(['/workspace/students']);
  }

  getAttendanceBadgeClass(status: string): string {
    switch (status) {
      case 'Present':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'Late':
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case 'Absent':
        return 'bg-red-50 text-red-700 border-red-200';
      case 'Excused':
        return 'bg-blue-50 text-blue-700 border-blue-200';
      default:
        return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }

  getWhatsappBadgeClass(status: string): string {
    switch (status) {
      case 'Delivered':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200';
      case 'Pending':
        return 'bg-amber-50 text-amber-700 border-amber-200';
      case 'Failed':
        return 'bg-red-50 text-red-700 border-red-200';
      default:
        return 'bg-slate-100 text-slate-600 border-slate-200';
    }
  }
}

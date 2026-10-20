import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { DashboardService } from './services/dashboard.service';
import { AuthStore } from '../../../core/stores/auth.store';
import { LocalizationService } from '../../../core/services/localization.service';
import { DashboardOverviewResponse } from '../../../core/models/dashboard.models';
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

  readonly canViewFinancials = computed(() => this.authStore.canViewFinancials());
  readonly activeWorkspace = computed(() => this.authStore.activeWorkspace());
  readonly currentUser = computed(() => this.authStore.user());

  ngOnInit(): void {
    this.loadData();
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

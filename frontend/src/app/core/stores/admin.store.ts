import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, catchError, of, Observable } from 'rxjs';
import {
  AdminWorkspaceSummary,
  PlatformOverviewKpis,
  RenewalWatchlistItem,
  PackageTemplate,
  PlatformSubscriptionPayment,
  WhatsAppProviderConfig,
  WhatsAppPlatformStats,
  PlatformGlobalSettings,
  AdminAuditLogEntry,
  GranularFeatureFlags
} from '../models/admin.models';
import {
  CreateWorkspaceRequest,
  SuspendWorkspaceRequest,
  RenewSubscriptionRequest,
  RecordPlatformPaymentRequest,
  ReversePlatformPaymentRequest,
  UpdatePlatformSettingsRequest
} from '../models/admin.requests';

@Injectable({
  providedIn: 'root'
})
export class AdminStore {
  private http = inject(HttpClient);

  readonly overviewKpis = signal<PlatformOverviewKpis | null>(null);
  readonly renewalWatchlist = signal<RenewalWatchlistItem[]>([]);
  readonly workspaces = signal<AdminWorkspaceSummary[]>([]);
  readonly selectedWorkspace = signal<AdminWorkspaceSummary | null>(null);
  readonly packages = signal<PackageTemplate[]>([]);
  readonly subscriptionPayments = signal<PlatformSubscriptionPayment[]>([]);
  readonly whatsappConfig = signal<WhatsAppProviderConfig | null>(null);
  readonly whatsappStats = signal<WhatsAppPlatformStats | null>(null);
  readonly globalSettings = signal<PlatformGlobalSettings | null>(null);
  readonly auditLogs = signal<AdminAuditLogEntry[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  readonly totalWorkspacesCount = computed(() => this.workspaces().length);
  readonly activeWorkspacesCount = computed(() => this.workspaces().filter(w => w.status === 'Active').length);
  readonly totalRevenueEgp = computed(() =>
    this.subscriptionPayments()
      .filter(p => p.status === 'Completed')
      .reduce((sum, p) => sum + p.amount, 0)
  );

  loadOverview(): Observable<any> {
    this.isLoading.set(true);
    return this.http.get<{ kpis: PlatformOverviewKpis; renewalWatchlist: RenewalWatchlistItem[] }>('/api/v1/admin/overview').pipe(
      tap(res => {
        this.overviewKpis.set(res.kpis);
        this.renewalWatchlist.set(res.renewalWatchlist);
        this.isLoading.set(false);
      }),
      catchError(err => {
        this.error.set(err.message || 'Failed to load overview');
        this.isLoading.set(false);
        return of(null);
      })
    );
  }

  loadWorkspaces(query?: string, status?: string): Observable<any> {
    this.isLoading.set(true);
    let params: any = {};
    if (query) params.query = query;
    if (status) params.status = status;

    return this.http.get<AdminWorkspaceSummary[]>('/api/v1/admin/workspaces', { params }).pipe(
      tap(res => {
        this.workspaces.set(res);
        this.isLoading.set(false);
      }),
      catchError(err => {
        this.error.set(err.message || 'Failed to load workspaces');
        this.isLoading.set(false);
        return of([]);
      })
    );
  }

  createWorkspace(req: CreateWorkspaceRequest): Observable<AdminWorkspaceSummary | null> {
    this.isLoading.set(true);
    return this.http.post<AdminWorkspaceSummary>('/api/v1/admin/workspaces', req).pipe(
      tap(created => {
        this.workspaces.update(list => [created, ...list]);
        this.isLoading.set(false);
      }),
      catchError(err => {
        this.error.set(err.message || 'Failed to create workspace');
        this.isLoading.set(false);
        return of(null);
      })
    );
  }

  suspendWorkspace(id: string, req: SuspendWorkspaceRequest): Observable<any> {
    return this.http.post<any>(`/api/v1/admin/workspaces/${id}/suspend`, req).pipe(
      tap(updated => {
        this.workspaces.update(list =>
          list.map(w => w.id === id ? { ...w, status: 'Suspended', suspensionReason: req.suspensionReason } : w)
        );
      })
    );
  }

  reactivateWorkspace(id: string): Observable<any> {
    return this.http.post<any>(`/api/v1/admin/workspaces/${id}/reactivate`, {}).pipe(
      tap(updated => {
        this.workspaces.update(list =>
          list.map(w => w.id === id ? { ...w, status: 'Active', suspensionReason: undefined } : w)
        );
      })
    );
  }

  renewSubscription(id: string, req: RenewSubscriptionRequest): Observable<any> {
    return this.http.post<any>(`/api/v1/admin/workspaces/${id}/renew`, req).pipe(
      tap(updated => {
        this.workspaces.update(list =>
          list.map(w => w.id === id ? { ...w, ...updated } : w)
        );
      })
    );
  }

  loadPackages(): Observable<any> {
    return this.http.get<PackageTemplate[]>('/api/v1/admin/packages').pipe(
      tap(pkgs => this.packages.set(pkgs))
    );
  }

  updatePackageOverrides(workspaceId: string, overrides: GranularFeatureFlags): Observable<any> {
    return this.http.put<any>(`/api/v1/admin/workspaces/${workspaceId}/overrides`, overrides).pipe(
      tap(updated => {
        this.workspaces.update(list =>
          list.map(w => w.id === workspaceId ? { ...w, featureOverrides: overrides } : w)
        );
      })
    );
  }

  resetPackageOverrides(workspaceId: string): Observable<any> {
    return this.http.delete<any>(`/api/v1/admin/workspaces/${workspaceId}/overrides`).pipe(
      tap(() => {
        this.workspaces.update(list =>
          list.map(w => w.id === workspaceId ? { ...w, featureOverrides: undefined } : w)
        );
      })
    );
  }

  loadPayments(): Observable<any> {
    return this.http.get<PlatformSubscriptionPayment[]>('/api/v1/admin/payments').pipe(
      tap(payments => this.subscriptionPayments.set(payments))
    );
  }

  recordPayment(req: RecordPlatformPaymentRequest): Observable<any> {
    return this.http.post<PlatformSubscriptionPayment>('/api/v1/admin/payments', req).pipe(
      tap(payment => {
        this.subscriptionPayments.update(list => [payment, ...list]);
      })
    );
  }

  reversePayment(id: string, req: ReversePlatformPaymentRequest): Observable<any> {
    return this.http.post<any>(`/api/v1/admin/payments/${id}/reverse`, req).pipe(
      tap(updated => {
        this.subscriptionPayments.update(list =>
          list.map(p => p.id === id ? { ...p, status: 'Reversed', reversalReason: req.reversalReason, reversedAt: new Date().toISOString() } : p)
        );
      })
    );
  }

  loadWhatsAppConfig(): Observable<any> {
    return this.http.get<any>('/api/v1/admin/whatsapp').pipe(
      tap(res => {
        this.whatsappConfig.set(res.config);
        this.whatsappStats.set(res.stats);
      })
    );
  }

  testWhatsAppPing(): Observable<any> {
    return this.http.post<any>('/api/v1/admin/whatsapp/ping', {});
  }

  loadSettings(): Observable<any> {
    return this.http.get<PlatformGlobalSettings>('/api/v1/admin/settings').pipe(
      tap(settings => this.globalSettings.set(settings))
    );
  }

  updateSettings(req: UpdatePlatformSettingsRequest): Observable<any> {
    return this.http.put<PlatformGlobalSettings>('/api/v1/admin/settings', req).pipe(
      tap(updated => this.globalSettings.set(updated))
    );
  }

  loadAuditLogs(action?: string, search?: string): Observable<any> {
    let params: any = {};
    if (action) params.action = action;
    if (search) params.search = search;
    return this.http.get<AdminAuditLogEntry[]>('/api/v1/admin/audit-logs', { params }).pipe(
      tap(logs => this.auditLogs.set(logs))
    );
  }

  appendAuditLog(entry: AdminAuditLogEntry): void {
    this.auditLogs.update(logs => [entry, ...logs]);
  }
}

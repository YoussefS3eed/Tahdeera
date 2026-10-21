import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  StandardReportId,
  ReportCatalogItem,
  ReportFilterCriteria,
  ReportResult
} from '../models/reports.models';

@Injectable({ providedIn: 'root' })
export class ReportsStore {
  private http = inject(HttpClient);

  readonly catalog = signal<ReportCatalogItem[]>([]);
  readonly activeReportId = signal<StandardReportId>('payments-ledger');
  readonly currentFilters = signal<ReportFilterCriteria>({ reportId: 'payments-ledger' });
  readonly reportResult = signal<ReportResult | null>(null);
  readonly isLoading = signal<boolean>(false);

  readonly activeReportMeta = computed(() =>
    this.catalog().find(r => r.id === this.activeReportId()) || null
  );

  async loadCatalog(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/reports/catalog')
    );
    this.catalog.set(res || []);
    if (!this.activeReportId() && res && res.length > 0) {
      this.activeReportId.set(res[0].id);
    }
  }

  selectReport(id: string): void {
    this.activeReportId.set(id);
    this.currentFilters.set({ reportId: id });
    this.runReport({ reportId: id });
  }

  async runReport(criteria: ReportFilterCriteria): Promise<ReportResult> {
    this.isLoading.set(true);
    try {
      const res: any = await firstValueFrom(
        this.http.post('/api/v1/workspaces/ws-main/reports/execute', criteria)
      );
      this.reportResult.set(res);
      return res;
    } finally {
      this.isLoading.set(false);
    }
  }
}

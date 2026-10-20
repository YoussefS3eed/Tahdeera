import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthStore } from '../../../../core/stores/auth.store';
import { DashboardOverviewResponse } from '../../../../core/models/dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private authStore = inject(AuthStore);

  getOverview(): Observable<DashboardOverviewResponse> {
    const wsId = this.authStore.activeWorkspaceId() || 'ws_01';
    return this.http.get<DashboardOverviewResponse>(`/api/v1/workspaces/${wsId}/dashboard`);
  }
}

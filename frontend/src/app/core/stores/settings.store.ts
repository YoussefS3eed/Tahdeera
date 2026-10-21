import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  WorkspaceSettings,
  WorkspaceCenterProfile,
  WorkspaceAttendancePolicies,
  ReadOnlySubscriptionView
} from '../models/team-settings.models';

@Injectable({ providedIn: 'root' })
export class SettingsStore {
  private http = inject(HttpClient);

  readonly settings = signal<WorkspaceSettings | null>(null);
  readonly subscription = signal<ReadOnlySubscriptionView | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly backupProgress = signal<boolean>(false);
  readonly lastBackupTimestamp = signal<string | null>(null);

  async loadSettings(): Promise<void> {
    this.isLoading.set(true);
    try {
      const res: any = await firstValueFrom(
        this.http.get('/api/v1/workspaces/ws-main/settings')
      );
      this.settings.set(res);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadSubscription(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/subscription')
    );
    this.subscription.set(res);
  }

  async updateProfile(profile: Partial<WorkspaceCenterProfile>): Promise<void> {
    this.isSaving.set(true);
    try {
      const current = this.settings();
      if (!current) return;
      const updatedProfile = { ...current.profile, ...profile };
      const res: any = await firstValueFrom(
        this.http.put('/api/v1/workspaces/ws-main/settings/profile', updatedProfile)
      );
      this.settings.set({
        ...current,
        profile: res
      });
    } finally {
      this.isSaving.set(false);
    }
  }

  async updateAttendancePolicies(policies: Partial<WorkspaceAttendancePolicies>): Promise<void> {
    this.isSaving.set(true);
    try {
      const current = this.settings();
      if (!current) return;
      const updatedPolicies = { ...current.attendancePolicies, ...policies };
      const res: any = await firstValueFrom(
        this.http.put('/api/v1/workspaces/ws-main/settings/attendance-policies', updatedPolicies)
      );
      this.settings.set({
        ...current,
        attendancePolicies: res
      });
    } finally {
      this.isSaving.set(false);
    }
  }

  async triggerBackupDownload(): Promise<Blob> {
    this.backupProgress.set(true);
    try {
      const blob: any = await firstValueFrom(
        this.http.post('/api/v1/workspaces/ws-main/settings/backup', {}, { responseType: 'blob' })
      );
      this.lastBackupTimestamp.set(new Date().toISOString());
      return blob;
    } finally {
      this.backupProgress.set(false);
    }
  }
}

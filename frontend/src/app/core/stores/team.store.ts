import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  WorkspaceStaffMember,
  CreateStaffMemberRequest,
  WorkspaceRole
} from '../models/team-settings.models';

@Injectable({ providedIn: 'root' })
export class TeamStore {
  private http = inject(HttpClient);

  readonly members = signal<WorkspaceStaffMember[]>([]);
  readonly maxStaffQuota = signal<number>(8);
  readonly roleFilter = signal<string>('all');
  readonly isLoading = signal<boolean>(false);
  readonly latestCreatedMember = signal<WorkspaceStaffMember | null>(null);

  readonly currentMemberCount = computed(() => this.members().length);
  readonly isQuotaReached = computed(() => this.currentMemberCount() >= this.maxStaffQuota());

  readonly filteredMembers = computed(() => {
    const role = this.roleFilter();
    if (role === 'all') return this.members();
    return this.members().filter(m => m.role === role);
  });

  async loadMembers(): Promise<void> {
    this.isLoading.set(true);
    try {
      const res: any = await firstValueFrom(
        this.http.get('/api/v1/workspaces/ws-main/members')
      );
      this.members.set(res?.members || res || []);
    } finally {
      this.isLoading.set(false);
    }
  }

  async addMember(request: CreateStaffMemberRequest): Promise<WorkspaceStaffMember> {
    if (this.isQuotaReached()) {
      throw new Error('QUOTA_MAX_STAFF_REACHED: تم بلوغ الحد الأقصى لعدد الموظفين في هذه الباقة');
    }
    this.isLoading.set(true);
    try {
      const res: any = await firstValueFrom(
        this.http.post('/api/v1/workspaces/ws-main/members', request)
      );
      this.members.update(list => [...list, res]);
      this.latestCreatedMember.set(res);
      return res;
    } finally {
      this.isLoading.set(false);
    }
  }

  async deactivateMember(memberId: string): Promise<WorkspaceStaffMember> {
    this.isLoading.set(true);
    try {
      const res: any = await firstValueFrom(
        this.http.post(`/api/v1/workspaces/ws-main/members/${memberId}/deactivate`, {})
      );
      this.members.update(list => list.map(m => (m.id === memberId ? res : m)));
      return res;
    } finally {
      this.isLoading.set(false);
    }
  }

  async updateMemberRole(memberId: string, role: WorkspaceRole): Promise<WorkspaceStaffMember> {
    this.isLoading.set(true);
    try {
      const res: any = await firstValueFrom(
        this.http.put(`/api/v1/workspaces/ws-main/members/${memberId}/role`, { role })
      );
      this.members.update(list => list.map(m => (m.id === memberId ? res : m)));
      return res;
    } finally {
      this.isLoading.set(false);
    }
  }
}

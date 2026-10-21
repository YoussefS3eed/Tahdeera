import { Injectable, signal, computed, inject, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  WhatsAppMessageType,
  WhatsAppDeliveryStatus,
  WhatsAppTemplate,
  WhatsAppMessageLog,
  GuardianConsentRecord
} from '../models/whatsapp.models';

@Injectable({ providedIn: 'root' })
export class WhatsAppStore implements OnDestroy {
  private http = inject(HttpClient);
  private timerInterval: any = null;

  readonly activeSubTab = signal<'template' | 'logs' | 'parents'>('template');
  readonly activeTemplateType = signal<WhatsAppMessageType>('AttendanceCheckIn');
  readonly templates = signal<Record<string, WhatsAppTemplate>>({});
  readonly outboxLogs = signal<WhatsAppMessageLog[]>([]);
  readonly consents = signal<GuardianConsentRecord[]>([]);
  readonly filterStatus = signal<string>('all');
  readonly searchPhone = signal<string>('');
  readonly isLoading = signal<boolean>(false);

  readonly filteredLogs = computed(() => {
    const st = this.filterStatus();
    const q = this.searchPhone().trim().toLowerCase();
    return this.outboxLogs().filter(log => {
      if (st !== 'all' && log.status !== st) return false;
      if (q && !log.guardianPhone.includes(q) && !log.studentName.toLowerCase().includes(q)) return false;
      return true;
    });
  });

  readonly currentTemplate = computed(() => {
    const t = this.templates();
    const type = this.activeTemplateType();
    return t[type] || null;
  });

  constructor() {
    this.startCountdownEngine();
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }

  private startCountdownEngine(): void {
    if (this.timerInterval) clearInterval(this.timerInterval);
    this.timerInterval = setInterval(() => {
      this.outboxLogs.update(logs =>
        logs.map(log => {
          if (log.status === 'Pending' && log.bufferSecondsRemaining !== undefined) {
            const nextSec = log.bufferSecondsRemaining - 1;
            if (nextSec <= 0) {
              return {
                ...log,
                bufferSecondsRemaining: 0,
                status: 'Delivered' // Transition after buffer expiry
              };
            }
            return {
              ...log,
              bufferSecondsRemaining: nextSec
            };
          }
          return log;
        })
      );
    }, 1000);
  }

  async loadTemplates(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/whatsapp/templates')
    );
    this.templates.set(res || {});
  }

  async updateTemplate(type: WhatsAppMessageType, rawText: string): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.put(`/api/v1/workspaces/ws-main/whatsapp/templates/${type}`, { rawText })
    );
    this.templates.update(t => ({
      ...t,
      [type]: res
    }));
  }

  async loadOutboxLogs(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/whatsapp/outbox')
    );
    this.outboxLogs.set(res || []);
  }

  async cancelPendingMessage(id: string): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.post(`/api/v1/workspaces/ws-main/whatsapp/outbox/${id}/cancel`, {})
    );
    this.outboxLogs.update(logs => logs.map(l => (l.id === id ? res : l)));
  }

  async retryFailedMessage(id: string): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.post(`/api/v1/workspaces/ws-main/whatsapp/outbox/${id}/retry`, {})
    );
    this.outboxLogs.update(logs => logs.map(l => (l.id === id ? res : l)));
  }

  async loadConsents(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/whatsapp/consents')
    );
    this.consents.set(res || []);
  }

  async toggleConsent(studentId: string, optIn: boolean): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.put(`/api/v1/workspaces/ws-main/whatsapp/consents/${studentId}`, { whatsappOptIn: optIn })
    );
    this.consents.update(list => list.map(c => (c.studentId === studentId ? res : c)));
  }
}

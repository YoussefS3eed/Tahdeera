import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  PromptChip,
  AiQueryResponse,
  AiDailyQuota
} from '../models/ai-assistant.models';

@Injectable({ providedIn: 'root' })
export class AiAssistantStore {
  private http = inject(HttpClient);

  readonly currentResponse = signal<AiQueryResponse | null>(null);
  readonly queryHistory = signal<AiQueryResponse[]>([]);
  readonly promptChips = signal<PromptChip[]>([]);
  readonly dailyQuota = signal<AiDailyQuota>({ used: 14, limit: 100, resetAt: '2026-10-22T00:00:00Z' });
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  async loadPromptChips(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/ai/chips')
    );
    this.promptChips.set(res || []);
  }

  async loadQuota(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/ai/quota')
    );
    if (res) {
      this.dailyQuota.set(res);
    }
  }

  async askAi(questionText: string): Promise<AiQueryResponse> {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    try {
      const res: any = await firstValueFrom(
        this.http.post('/api/v1/workspaces/ws-main/ai/query', { question: questionText })
      );
      this.currentResponse.set(res);
      this.queryHistory.update(h => [res, ...h]);
      this.dailyQuota.update(q => ({ ...q, used: q.used + 1 }));
      return res;
    } catch (err: any) {
      this.errorMessage.set(err.message || 'حدث خطأ أثناء معالجة السؤال');
      throw err;
    } finally {
      this.isLoading.set(false);
    }
  }

  clearResponse(): void {
    this.currentResponse.set(null);
  }
}

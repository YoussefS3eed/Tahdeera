import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  ExamItem,
  QuestionItem,
  HomeworkItem,
  LibraryItem,
  ExamSubmission
} from '../models/lms.models';

export type LmsTab = 'exams' | 'question_bank' | 'homework' | 'library';

@Injectable({ providedIn: 'root' })
export class LmsStore {
  private http = inject(HttpClient);

  readonly activeTab = signal<LmsTab>('exams');
  readonly exams = signal<ExamItem[]>([]);
  readonly questionBank = signal<QuestionItem[]>([]);
  readonly homeworkList = signal<HomeworkItem[]>([]);
  readonly libraryList = signal<LibraryItem[]>([]);
  readonly selectedExam = signal<ExamItem | null>(null);
  readonly examSubmissions = signal<ExamSubmission[]>([]);
  readonly isLoading = signal<boolean>(false);

  // Computeds
  readonly publishedExams = computed(() =>
    this.exams().filter(e => e.status === 'Published')
  );

  readonly draftExams = computed(() =>
    this.exams().filter(e => e.status === 'Draft')
  );

  readonly pendingAiDraftQuestions = computed(() =>
    this.questionBank().filter(q => !q.isApproved)
  );

  readonly approvedQuestions = computed(() =>
    this.questionBank().filter(q => q.isApproved)
  );

  async loadAll(): Promise<void> {
    this.isLoading.set(true);
    try {
      await Promise.all([
        this.loadExams(),
        this.loadQuestionBank(),
        this.loadHomeworkList(),
        this.loadLibrary()
      ]);
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadExams(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/lms/exams')
    );
    this.exams.set(res || []);
  }

  async createExam(payload: Partial<ExamItem>): Promise<ExamItem> {
    const res: any = await firstValueFrom(
      this.http.post('/api/v1/workspaces/ws-main/lms/exams', payload)
    );
    this.exams.update(list => [res, ...list]);
    return res;
  }

  async loadQuestionBank(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/lms/questions')
    );
    this.questionBank.set(res || []);
  }

  async createQuestion(payload: Partial<QuestionItem>): Promise<QuestionItem> {
    const res: any = await firstValueFrom(
      this.http.post('/api/v1/workspaces/ws-main/lms/questions', payload)
    );
    this.questionBank.update(list => [res, ...list]);
    return res;
  }

  async approveAiDraftQuestion(id: string): Promise<QuestionItem> {
    const res: any = await firstValueFrom(
      this.http.post(`/api/v1/workspaces/ws-main/lms/questions/${id}/approve`, {})
    );
    this.questionBank.update(list => list.map(q => (q.id === id ? res : q)));
    return res;
  }

  async loadHomeworkList(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/lms/homework')
    );
    this.homeworkList.set(res || []);
  }

  async createHomework(payload: Partial<HomeworkItem>): Promise<HomeworkItem> {
    const res: any = await firstValueFrom(
      this.http.post('/api/v1/workspaces/ws-main/lms/homework', payload)
    );
    this.homeworkList.update(list => [res, ...list]);
    return res;
  }

  async loadLibrary(): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get('/api/v1/workspaces/ws-main/lms/library')
    );
    this.libraryList.set(res || []);
  }

  async uploadLibraryItem(payload: Partial<LibraryItem>): Promise<LibraryItem> {
    const res: any = await firstValueFrom(
      this.http.post('/api/v1/workspaces/ws-main/lms/library', payload)
    );
    this.libraryList.update(list => [res, ...list]);
    return res;
  }

  async loadExamSubmissions(examId: string): Promise<void> {
    const res: any = await firstValueFrom(
      this.http.get(`/api/v1/workspaces/ws-main/lms/exams/${examId}/submissions`)
    );
    this.examSubmissions.set(res || []);
  }

  async submitExam(examId: string, submission: {
    studentCode: string;
    studentName?: string;
    answers: { questionId: string; selectedOptionId?: string; openAnswerText?: string }[];
  }): Promise<ExamSubmission> {
    const res: any = await firstValueFrom(
      this.http.post(`/api/v1/portal/exams/${examId}/submit`, submission)
    );
    this.examSubmissions.update(list => [res, ...list]);
    return res;
  }
}

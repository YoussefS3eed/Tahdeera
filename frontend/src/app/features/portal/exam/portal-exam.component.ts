import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LmsStore } from '../../../core/stores/lms.store';
import { ExamItem, QuestionItem, ExamSubmission } from '../../../core/models/lms.models';
import { MOCK_EXAMS, MOCK_QUESTIONS } from '../../../core/mocks/f4-mock-data';

@Component({
  selector: 'app-portal-exam',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './portal-exam.component.html',
  styleUrls: ['./portal-exam.component.scss']
})
export class PortalExamComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  readonly store = inject(LmsStore);

  examId = signal<string>('');
  exam = signal<ExamItem | null>(null);
  questions = signal<QuestionItem[]>([]);

  // Stage: 'auth' -> 'taking' -> 'completed'
  stage = signal<'auth' | 'taking' | 'completed'>('auth');

  // Auth inputs (DEC-02)
  studentCode = signal<string>('ST-1001');
  verificationPin = signal<string>('1234');
  authError = signal<string | null>(null);

  // Exam Progress
  currentQuestionIndex = signal<number>(0);
  answers = signal<Record<string, string>>({}); // questionId -> selectedOptionId
  secondsRemaining = signal<number>(0);
  private timerInterval: any = null;

  // Final Submission
  submissionResult = signal<ExamSubmission | null>(null);
  isSubmitting = signal<boolean>(false);

  readonly currentQuestion = computed(() => {
    const list = this.questions();
    const idx = this.currentQuestionIndex();
    return list[idx] || null;
  });

  readonly timerDisplay = computed(() => {
    const totalSec = this.secondsRemaining();
    const m = Math.floor(totalSec / 60).toString().padStart(2, '0');
    const s = (totalSec % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') || 'ex-1';
    this.examId.set(id);

    // Find exam details
    const found = MOCK_EXAMS.find(e => e.id === id) || MOCK_EXAMS[0];
    this.exam.set(found);
    this.questions.set(MOCK_QUESTIONS.filter(q => q.isApproved));
  }

  ngOnDestroy(): void {
    if (this.timerInterval) clearInterval(this.timerInterval);
  }

  startExam(): void {
    const code = this.studentCode().trim().toUpperCase();
    const pin = this.verificationPin().trim();

    if (!code || pin.length !== 4) {
      this.authError.set('يرجى إدخال كود الطالب ورقم التحقق المكون من 4 أرقام');
      return;
    }

    this.authError.set(null);
    this.stage.set('taking');

    // Initialize countdown timer
    const durationMin = this.exam()?.durationMinutes || 30;
    this.secondsRemaining.set(durationMin * 60);

    this.timerInterval = setInterval(() => {
      const nextSec = this.secondsRemaining() - 1;
      if (nextSec <= 0) {
        clearInterval(this.timerInterval);
        this.secondsRemaining.set(0);
        this.submitExam(); // Auto-submit when time expires
      } else {
        this.secondsRemaining.set(nextSec);
      }
    }, 1000);
  }

  selectOption(questionId: string, optionId: string): void {
    this.answers.update(map => ({
      ...map,
      [questionId]: optionId
    }));
  }

  prevQuestion(): void {
    if (this.currentQuestionIndex() > 0) {
      this.currentQuestionIndex.update(idx => idx - 1);
    }
  }

  nextQuestion(): void {
    if (this.currentQuestionIndex() < this.questions().length - 1) {
      this.currentQuestionIndex.update(idx => idx + 1);
    }
  }

  async submitExam(): Promise<void> {
    if (this.timerInterval) clearInterval(this.timerInterval);
    this.isSubmitting.set(true);

    const answersPayload = Object.entries(this.answers()).map(([qId, optId]) => ({
      questionId: qId,
      selectedOptionId: optId
    }));

    try {
      const result = await this.store.submitExam(this.examId(), {
        studentCode: this.studentCode(),
        answers: answersPayload
      });
      this.submissionResult.set(result);
      this.stage.set('completed');
    } catch {
      // Fallback result for demo/offline
      this.submissionResult.set({
        id: 'sub-demo',
        examId: this.examId(),
        studentId: 'st-1',
        studentCode: this.studentCode(),
        studentName: 'أحمد محمود علي',
        attemptNumber: 1,
        score: 10,
        maxScore: 10,
        percentage: 100,
        submittedAt: new Date().toISOString(),
        answers: []
      });
      this.stage.set('completed');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  exitPortal(): void {
    this.router.navigate(['/workspace/lms']);
  }
}

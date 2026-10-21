import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { LmsStore } from './lms.store';
import {
  MOCK_EXAMS,
  MOCK_QUESTIONS,
  MOCK_HOMEWORK,
  MOCK_LIBRARY
} from '../mocks/f4-mock-data';

describe('LmsStore', () => {
  let store: LmsStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [LmsStore]
    });

    store = TestBed.inject(LmsStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should initialize with default signals', () => {
    expect(store.activeTab()).toBe('exams');
    expect(store.exams()).toEqual([]);
    expect(store.questionBank()).toEqual([]);
    expect(store.homeworkList()).toEqual([]);
    expect(store.libraryList()).toEqual([]);
    expect(store.isLoading()).toBeFalse();
  });

  it('should load all LMS sub-collections via loadAll()', async () => {
    const loadPromise = store.loadAll();

    const examsReq = httpMock.expectOne('/api/v1/workspaces/ws-main/lms/exams');
    expect(examsReq.request.method).toBe('GET');
    examsReq.flush(MOCK_EXAMS);

    const qReq = httpMock.expectOne('/api/v1/workspaces/ws-main/lms/questions');
    expect(qReq.request.method).toBe('GET');
    qReq.flush(MOCK_QUESTIONS);

    const hwReq = httpMock.expectOne('/api/v1/workspaces/ws-main/lms/homework');
    expect(hwReq.request.method).toBe('GET');
    hwReq.flush(MOCK_HOMEWORK);

    const libReq = httpMock.expectOne('/api/v1/workspaces/ws-main/lms/library');
    expect(libReq.request.method).toBe('GET');
    libReq.flush(MOCK_LIBRARY);

    await loadPromise;

    expect(store.exams().length).toBe(MOCK_EXAMS.length);
    expect(store.questionBank().length).toBe(MOCK_QUESTIONS.length);
    expect(store.homeworkList().length).toBe(MOCK_HOMEWORK.length);
    expect(store.libraryList().length).toBe(MOCK_LIBRARY.length);
    expect(store.publishedExams().length).toBeGreaterThan(0);
    expect(store.pendingAiDraftQuestions().length).toBe(1);
    expect(store.approvedQuestions().length).toBe(3);
  });

  it('should approve an AI draft question', async () => {
    store.questionBank.set([...MOCK_QUESTIONS]);
    const draft = MOCK_QUESTIONS.find(q => !q.isApproved)!;

    const approvePromise = store.approveAiDraftQuestion(draft.id);
    const req = httpMock.expectOne(`/api/v1/workspaces/ws-main/lms/questions/${draft.id}/approve`);
    expect(req.request.method).toBe('POST');

    const approvedQ = {
      ...draft,
      sourceType: 'AiApproved' as const,
      isApproved: true
    };
    req.flush(approvedQ);

    await approvePromise;
    const updated = store.questionBank().find(q => q.id === draft.id);
    expect(updated?.isApproved).toBeTrue();
    expect(updated?.sourceType).toBe('AiApproved');
    expect(store.pendingAiDraftQuestions().length).toBe(0);
  });

  it('should create an exam and prepend to exams list', async () => {
    const newExam = {
      title: 'امتحان تجريبي',
      subject: 'لغة عربية',
      durationMinutes: 45,
      totalPoints: 50,
      status: 'Draft' as const
    };

    const createPromise = store.createExam(newExam);
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/lms/exams');
    expect(req.request.method).toBe('POST');

    const created = { id: 'ex-new', ...newExam, createdAt: '2026-10-21T10:00:00Z' };
    req.flush(created);

    await createPromise;
    expect(store.exams()[0].id).toBe('ex-new');
  });

  it('should submit an exam from student portal', async () => {
    const examId = 'ex-1';
    const submissionPayload = {
      studentCode: 'ST-1001',
      studentName: 'أحمد محمود علي',
      answers: [
        { questionId: 'q-1', selectedOptionId: 'opt-1b' },
        { questionId: 'q-2', selectedOptionId: 'opt-2a' }
      ]
    };

    const submitPromise = store.submitExam(examId, submissionPayload);
    const req = httpMock.expectOne(`/api/v1/portal/exams/${examId}/submit`);
    expect(req.request.method).toBe('POST');

    const mockResult = {
      id: 'sub-new',
      examId,
      studentId: 'st-1',
      studentCode: 'ST-1001',
      studentName: 'أحمد محمود علي',
      attemptNumber: 1,
      score: 10,
      maxScore: 10,
      percentage: 100,
      submittedAt: '2026-10-21T10:15:00Z',
      answers: []
    };
    req.flush(mockResult);

    const res = await submitPromise;
    expect(res.score).toBe(10);
    expect(res.percentage).toBe(100);
    expect(store.examSubmissions()[0].id).toBe('sub-new');
  });
});

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AiAssistantStore } from './ai-assistant.store';
import { MOCK_PROMPT_CHIPS, MOCK_AI_RESPONSES } from '../mocks/f4-mock-data';

describe('AiAssistantStore', () => {
  let store: AiAssistantStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AiAssistantStore]
    });

    store = TestBed.inject(AiAssistantStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should initialize with default signals', () => {
    expect(store.currentResponse()).toBeNull();
    expect(store.queryHistory()).toEqual([]);
    expect(store.promptChips()).toEqual([]);
    expect(store.isLoading()).toBeFalse();
    expect(store.dailyQuota().limit).toBe(100);
  });

  it('should load prompt chips', async () => {
    const loadPromise = store.loadPromptChips();
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/ai/chips');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_PROMPT_CHIPS);

    await loadPromise;
    expect(store.promptChips().length).toBe(MOCK_PROMPT_CHIPS.length);
  });

  it('should ask AI and update currentResponse, queryHistory, and quota', async () => {
    const question = 'من هم الطلاب الأكثر غياباً؟';
    const mockAns = MOCK_AI_RESPONSES['من هم الطلاب الأكثر غياباً؟'];

    const askPromise = store.askAi(question);
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/ai/query');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ question });
    req.flush(mockAns);

    const initialUsed = store.dailyQuota().used;
    const res = await askPromise;

    expect(res.isOutOfScope).toBeFalse();
    expect(store.currentResponse()).toEqual(mockAns);
    expect(store.queryHistory().length).toBe(1);
    expect(store.dailyQuota().used).toBe(initialUsed + 1);
  });

  it('should handle out-of-scope question with safe fallback and suggestions', async () => {
    const oosQuestion = 'ما هي عاصمة فرنسا؟';
    const oosResponse = {
      id: 'res-oos',
      question: oosQuestion,
      isOutOfScope: true,
      answerAr: 'عذراً، أنا المساعد الذكي الخاص بمنصة تحضيرة لسنتر الأوائل التعليمي فقط.',
      suggestedChips: MOCK_PROMPT_CHIPS,
      createdAt: '2026-10-21T10:00:00Z'
    };

    const askPromise = store.askAi(oosQuestion);
    const req = httpMock.expectOne('/api/v1/workspaces/ws-main/ai/query');
    req.flush(oosResponse);

    const res = await askPromise;
    expect(res.isOutOfScope).toBeTrue();
    expect(res.suggestedChips?.length).toBeGreaterThan(0);
    expect(store.currentResponse()?.isOutOfScope).toBeTrue();
  });

  it('should clear current response', () => {
    store.currentResponse.set({
      id: 'test',
      question: 'q',
      isOutOfScope: false,
      answerAr: 'a',
      createdAt: 'now'
    });
    expect(store.currentResponse()).not.toBeNull();
    store.clearResponse();
    expect(store.currentResponse()).toBeNull();
  });
});

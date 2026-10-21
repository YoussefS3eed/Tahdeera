import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { LmsComponent } from './lms.component';
import { LmsStore } from '../../../core/stores/lms.store';
import { MOCK_EXAMS, MOCK_QUESTIONS, MOCK_HOMEWORK, MOCK_LIBRARY } from '../../../core/mocks/f4-mock-data';

describe('LmsComponent', () => {
  let component: LmsComponent;
  let fixture: ComponentFixture<LmsComponent>;
  let store: LmsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LmsComponent, HttpClientTestingModule],
      providers: [LmsStore]
    }).compileComponents();

    fixture = TestBed.createComponent(LmsComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(LmsStore);

    // Mock store data
    store.exams.set([...MOCK_EXAMS]);
    store.questionBank.set([...MOCK_QUESTIONS]);
    store.homeworkList.set([...MOCK_HOMEWORK]);
    store.libraryList.set([...MOCK_LIBRARY]);

    fixture.detectChanges();
  });

  it('should create the LMS component', () => {
    expect(component).toBeTruthy();
    expect(store.activeTab()).toBe('exams');
  });

  it('should switch between tabs', () => {
    component.setTab('question_bank');
    expect(store.activeTab()).toBe('question_bank');

    component.setTab('homework');
    expect(store.activeTab()).toBe('homework');

    component.setTab('library');
    expect(store.activeTab()).toBe('library');

    component.setTab('exams');
    expect(store.activeTab()).toBe('exams');
  });

  it('should show pending AI draft questions alert count in tab', () => {
    expect(store.pendingAiDraftQuestions().length).toBe(1);
  });
});

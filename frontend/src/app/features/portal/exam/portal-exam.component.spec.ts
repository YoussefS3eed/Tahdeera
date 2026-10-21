import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { PortalExamComponent } from './portal-exam.component';
import { LmsStore } from '../../../core/stores/lms.store';
import { MOCK_EXAMS, MOCK_QUESTIONS } from '../../../core/mocks/f4-mock-data';

describe('PortalExamComponent', () => {
  let component: PortalExamComponent;
  let fixture: ComponentFixture<PortalExamComponent>;
  let store: LmsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PortalExamComponent, HttpClientTestingModule],
      providers: [
        LmsStore,
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => 'ex-101'
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PortalExamComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(LmsStore);

    fixture.detectChanges();
  });

  afterEach(() => {
    component.ngOnDestroy();
  });

  it('should initialize on stage auth with target exam', () => {
    expect(component).toBeTruthy();
    expect(component.stage()).toBe('auth');
    expect(component.exam()?.id).toBe('ex-101');
  });

  it('should reject invalid PIN or empty code', () => {
    component.studentCode.set('');
    component.verificationPin.set('12');
    component.startExam();

    expect(component.authError()).not.toBeNull();
    expect(component.stage()).toBe('auth');
  });

  it('should start exam with valid code and 4-digit PIN (DEC-02)', () => {
    component.studentCode.set('ST-1001');
    component.verificationPin.set('1234');
    component.startExam();

    expect(component.authError()).toBeNull();
    expect(component.stage()).toBe('taking');
    expect(component.secondsRemaining()).toBeGreaterThan(0);
  });

  it('should allow option selection and submitting exam', async () => {
    spyOn(store, 'submitExam').and.returnValue(Promise.resolve({
      id: 'sub-test',
      examId: 'ex-101',
      studentId: 'st-1',
      studentCode: 'ST-1001',
      studentName: 'أحمد محمود',
      attemptNumber: 1,
      score: 10,
      maxScore: 10,
      percentage: 100,
      submittedAt: new Date().toISOString(),
      submittedAtUtc: new Date().toISOString(),
      answers: []
    }));

    component.startExam();
    const q1 = component.questions()[0];
    component.selectOption(q1.id, q1.options![0].id);
    expect(component.answers()[q1.id]).toBe(q1.options![0].id);

    await component.submitExam();
    expect(component.stage()).toBe('completed');
    expect(component.submissionResult()).not.toBeNull();
  });
});

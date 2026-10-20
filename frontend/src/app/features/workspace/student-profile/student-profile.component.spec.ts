import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StudentProfileComponent } from './student-profile.component';
import { AcademicService } from '../../../core/services/academic.service';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { Student360Profile } from '../../../core/models/academic.models';

describe('StudentProfileComponent (SCR-08 Unified 360° Profile)', () => {
  let component: StudentProfileComponent;
  let fixture: ComponentFixture<StudentProfileComponent>;
  let academicServiceMock: jasmine.SpyObj<AcademicService>;
  let routerMock: jasmine.SpyObj<Router>;

  const mockProfile: Student360Profile = {
    id: 'std-1042',
    studentCode: 'ST-1042',
    fullName: 'يوسف أحمد علي',
    grade: 'Grade1Secondary',
    guardianName: 'أحمد علي',
    guardianPhone: '01019482687',
    whatsappOptIn: true,
    activeNfcCardUid: '04:A2:7C:19',
    qrIdentifierToken: 'QR-ST-1042',
    status: 'Active',
    tuitionBalance: 0,
    attendanceRate: 95,
    enrolledGroupIds: ['grp-1'],
    createdAt: '2026-09-01T08:00:00Z',
    withdrawnAt: null,
    attendanceHistory: [
      {
        id: 'att-1',
        sessionId: 'sess-1',
        studentId: 'std-1042',
        studentName: 'يوسف أحمد علي',
        studentCode: 'ST-1042',
        status: 'Present',
        scannedAt: '2026-10-21T08:02:00Z',
        source: 'NfcCard',
        autoEvaluationLocked: false,
        whatsappNotificationStatus: 'Sent'
      }
    ],
    financialLedger: [
      {
        id: 'fin-1',
        receiptNumber: 'REC-2026-10-001',
        amountPaid: 350,
        billingMonth: 'October 2026',
        paidAt: '2026-10-01T10:00:00Z',
        remainingBalance: 0,
        receivedByStaffName: 'الأستاذ محمد'
      }
    ],
    examScores: [],
    homeworkSubmissions: [],
    whatsappNotificationLogs: []
  };

  beforeEach(async () => {
    academicServiceMock = jasmine.createSpyObj('AcademicService', [
      'getStudent360',
      'revokeCard',
      'linkCard'
    ]);
    routerMock = jasmine.createSpyObj('Router', ['navigate']);

    academicServiceMock.getStudent360.and.returnValue(of(mockProfile));

    await TestBed.configureTestingModule({
      imports: [StudentProfileComponent],
      providers: [
        { provide: AcademicService, useValue: academicServiceMock },
        { provide: Router, useValue: routerMock },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(new Map([['id', 'std-1042']]))
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StudentProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load student profile on init and display header', () => {
    expect(component.profile()).toEqual(mockProfile);
    expect(component.profile()?.studentCode).toBe('ST-1042');
    expect(component.profile()?.activeNfcCardUid).toBe('04:A2:7C:19');
  });

  it('should permit Owner and Teacher to view finances tab (CONF-09)', () => {
    component.currentUserRole.set('Teacher');
    expect(component.canViewFinances()).toBeTrue();

    component.currentUserRole.set('Owner');
    expect(component.canViewFinances()).toBeTrue();
  });

  it('should strictly omit and forbid finances tab when role is Assistant or Receptionist (CONF-09)', () => {
    component.currentUserRole.set('Assistant');
    expect(component.canViewFinances()).toBeFalse();

    component.currentUserRole.set('Receptionist');
    expect(component.canViewFinances()).toBeFalse();

    // Trying to switch to finances tab should be blocked
    component.setTab('finances');
    expect(component.activeTab()).not.toBe('finances');
  });

  it('should revoke card and set activeNfcCardUid to null', () => {
    academicServiceMock.revokeCard.and.returnValue(of({} as any));
    component.onCardRevoked({ reason: 'Lost' });

    expect(academicServiceMock.revokeCard).toHaveBeenCalledWith('04:A2:7C:19', 'Lost');
    expect(component.profile()?.activeNfcCardUid).toBeNull();
  });
});

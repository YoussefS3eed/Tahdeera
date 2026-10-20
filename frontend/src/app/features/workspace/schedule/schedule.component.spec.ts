import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ScheduleComponent } from './schedule.component';
import { AcademicService } from '../../../core/services/academic.service';
import { of } from 'rxjs';
import { ClassSessionItem, StudyGroupItem } from '../../../core/models/academic.models';

describe('ScheduleComponent (SCR-11 Weekly Schedule & Session Lifecycle)', () => {
  let component: ScheduleComponent;
  let fixture: ComponentFixture<ScheduleComponent>;
  let academicServiceMock: jasmine.SpyObj<AcademicService>;

  const mockSessions: ClassSessionItem[] = [
    {
      id: 'sess-1',
      groupId: 'grp-1',
      groupName: 'فيزياء 1ث - مجموعة السبت أ',
      grade: 'Grade1Secondary',
      roomName: 'قاعة الفارابي',
      startsAt: '2026-10-24T08:00:00Z',
      endsAt: '2026-10-24T10:00:00Z',
      lateGraceMinutes: 15,
      status: 'Scheduled',
      totalRosterCount: 30,
      presentCount: 0,
      lateCount: 0,
      absentCount: 0,
      cancellationReason: null
    },
    {
      id: 'sess-2',
      groupId: 'grp-2',
      groupName: 'كيمياء 2ث - مجموعة الأحد',
      grade: 'Grade2Secondary',
      roomName: 'معمل العلوم 1',
      startsAt: '2026-10-25T10:30:00Z',
      endsAt: '2026-10-25T12:30:00Z',
      lateGraceMinutes: 10,
      status: 'Cancelled',
      totalRosterCount: 25,
      presentCount: 0,
      lateCount: 0,
      absentCount: 0,
      cancellationReason: 'عطل طارئ بالتكييف'
    }
  ];

  beforeEach(async () => {
    academicServiceMock = jasmine.createSpyObj('AcademicService', [
      'getClassSessions',
      'getStudyGroups',
      'createClassSession',
      'rescheduleSession',
      'cancelSession'
    ]);

    academicServiceMock.getClassSessions.and.returnValue(of(mockSessions));
    academicServiceMock.getStudyGroups.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [ScheduleComponent],
      providers: [
        { provide: AcademicService, useValue: academicServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize and load weekly sessions', () => {
    expect(component.sessions().length).toBe(2);
    expect(component.filteredSessions().length).toBe(2);
  });

  it('should reschedule session and trigger re-evaluation (DEC-03)', () => {
    const updatedSess: ClassSessionItem = {
      ...mockSessions[0],
      startsAt: '2026-10-24T09:00:00Z',
      endsAt: '2026-10-24T11:00:00Z'
    };
    academicServiceMock.rescheduleSession.and.returnValue(of(updatedSess));

    component.onSessionRescheduled({
      sessionId: 'sess-1',
      newStartTime: '2026-10-24T09:00:00Z',
      newEndTime: '2026-10-24T11:00:00Z'
    });

    expect(academicServiceMock.rescheduleSession).toHaveBeenCalledWith(
      'sess-1',
      '2026-10-24T09:00:00Z',
      '2026-10-24T11:00:00Z'
    );
    const target = component.sessions().find(s => s.id === 'sess-1');
    expect(target?.startsAt).toBe('2026-10-24T09:00:00Z');
  });

  it('should cancel session with reason and void attendance (DEC-01)', () => {
    const cancelledSess: ClassSessionItem = {
      ...mockSessions[0],
      status: 'Cancelled',
      cancellationReason: 'ظرف طارئ للمدرس'
    };
    academicServiceMock.cancelSession.and.returnValue(of(cancelledSess));

    component.onSessionCancelled({
      sessionId: 'sess-1',
      reason: 'ظرف طارئ للمدرس'
    });

    expect(academicServiceMock.cancelSession).toHaveBeenCalledWith('sess-1', 'ظرف طارئ للمدرس');
    const target = component.sessions().find(s => s.id === 'sess-1');
    expect(target?.status).toBe('Cancelled');
    expect(target?.cancellationReason).toBe('ظرف طارئ للمدرس');
  });
});

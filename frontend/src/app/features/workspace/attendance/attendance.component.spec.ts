import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { AttendanceComponent } from './attendance.component';
import { AcademicService } from '../../../core/services/academic.service';
import { AttendanceSyncService } from '../../../core/services/attendance-sync.service';
import { of } from 'rxjs';
import { AttendanceRecordItem, StudentListItem } from '../../../core/models/academic.models';

describe('AttendanceComponent (SCR-06 Daily Attendance Log)', () => {
  let component: AttendanceComponent;
  let fixture: ComponentFixture<AttendanceComponent>;
  let academicServiceMock: jasmine.SpyObj<AcademicService>;
  let syncServiceMock: jasmine.SpyObj<AttendanceSyncService>;

  const mockStudents: StudentListItem[] = [
    {
      id: 'std-1',
      studentCode: 'ST-1001',
      fullName: 'أحمد محمود',
      grade: 'Grade1Secondary',
      guardianName: 'محمود',
      guardianPhone: '01011111111',
      whatsappOptIn: true,
      activeNfcCardUid: '04:A1:B2:C3',
      qrIdentifierToken: 'QR-1',
      status: 'Active',
      tuitionBalance: 0,
      attendanceRate: 95,
      enrolledGroupIds: ['grp-1'],
      createdAt: '2026-09-01T08:00:00Z',
      withdrawnAt: null
    },
    {
      id: 'std-withdrawn',
      studentCode: 'ST-1002',
      fullName: 'سارة طارق',
      grade: 'Grade1Secondary',
      guardianName: 'طارق',
      guardianPhone: '01022222222',
      whatsappOptIn: true,
      activeNfcCardUid: '04:D4:E5:F6',
      qrIdentifierToken: 'QR-2',
      status: 'Withdrawn',
      tuitionBalance: 0,
      attendanceRate: 50,
      enrolledGroupIds: ['grp-1'],
      createdAt: '2026-09-01T08:00:00Z',
      withdrawnAt: '2026-10-10T12:00:00Z'
    }
  ];

  const mockRecords: AttendanceRecordItem[] = [
    {
      id: 'att-1',
      sessionId: 'sess-1',
      studentId: 'std-1',
      studentName: 'أحمد محمود',
      studentCode: 'ST-1001',
      status: 'Present',
      scannedAt: '2026-10-21T08:05:00Z',
      source: 'NfcCard',
      autoEvaluationLocked: false,
      whatsappNotificationStatus: 'Sent'
    },
    {
      id: 'att-2',
      sessionId: 'sess-1',
      studentId: 'std-withdrawn',
      studentName: 'سارة طارق',
      studentCode: 'ST-1002',
      status: 'Late',
      scannedAt: '2026-10-21T08:25:00Z',
      source: 'QrCode',
      autoEvaluationLocked: false,
      whatsappNotificationStatus: 'Queued'
    },
    {
      id: 'att-3',
      sessionId: 'sess-cancelled',
      studentId: 'std-1',
      studentName: 'أحمد محمود',
      studentCode: 'ST-1001',
      status: 'Voided',
      scannedAt: '2026-10-21T09:00:00Z',
      source: 'ManualOverride',
      autoEvaluationLocked: false,
      whatsappNotificationStatus: 'Cancelled'
    }
  ];

  beforeEach(async () => {
    academicServiceMock = jasmine.createSpyObj('AcademicService', [
      'getStudyGroups',
      'getClassSessions',
      'getStudents',
      'getAttendanceRecords',
      'overrideAttendance'
    ]);
    syncServiceMock = jasmine.createSpyObj('AttendanceSyncService', [
      'syncPending',
      'resolveConflict'
    ], {
      pendingCount: signal(0),
      conflictCount: signal(0)
    } as any);

    academicServiceMock.getStudyGroups.and.returnValue(of([]));
    academicServiceMock.getClassSessions.and.returnValue(of([]));
    academicServiceMock.getStudents.and.returnValue(of(mockStudents));
    academicServiceMock.getAttendanceRecords.and.returnValue(of(mockRecords));

    await TestBed.configureTestingModule({
      imports: [AttendanceComponent],
      providers: [
        { provide: AcademicService, useValue: academicServiceMock },
        { provide: AttendanceSyncService, useValue: syncServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AttendanceComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize and load attendance records and students', () => {
    expect(component.allRecords().length).toBe(3);
    expect(academicServiceMock.getAttendanceRecords).toHaveBeenCalled();
    expect(academicServiceMock.getStudents).toHaveBeenCalled();
  });

  it('should filter table rows by active tab', () => {
    component.setTab('Present');
    expect(component.filteredRecords().length).toBe(1);
    expect(component.filteredRecords()[0].status).toBe('Present');

    component.setTab('Late');
    expect(component.filteredRecords().length).toBe(1);
    expect(component.filteredRecords()[0].status).toBe('Late');

    component.setTab('Voided');
    expect(component.filteredRecords().length).toBe(1);
    expect(component.filteredRecords()[0].status).toBe('Voided');
  });

  it('should open Withdrawn Retroactive Modal when editing a withdrawn student (DEC-07)', () => {
    const withdrawnRecord = mockRecords[1]; // student std-withdrawn
    component.openEditModal(withdrawnRecord);

    expect(component.activeWithdrawnRecord()).toEqual(withdrawnRecord);
    expect(component.activeOverrideRecord()).toBeNull();
  });

  it('should open standard Manual Override Modal when editing an active student', () => {
    const activeRecord = mockRecords[0]; // student std-1
    component.openEditModal(activeRecord);

    expect(component.activeOverrideRecord()).toEqual(activeRecord);
    expect(component.activeWithdrawnRecord()).toBeNull();
  });

  it('should lock record upon manual override save', () => {
    const updatedRecord: AttendanceRecordItem = {
      ...mockRecords[0],
      status: 'Late',
      autoEvaluationLocked: true,
      justificationReason: 'عذر مروري مقبول'
    };
    academicServiceMock.overrideAttendance.and.returnValue(of(updatedRecord));

    component.activeOverrideRecord.set(mockRecords[0]);
    component.saveOverride({ status: 'Late', reason: 'عذر مروري مقبول' });

    expect(academicServiceMock.overrideAttendance).toHaveBeenCalledWith('att-1', 'Late', 'عذر مروري مقبول');
    const saved = component.allRecords().find(r => r.id === 'att-1');
    expect(saved?.autoEvaluationLocked).toBeTrue();
  });
});

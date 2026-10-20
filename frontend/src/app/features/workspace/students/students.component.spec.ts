import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StudentsComponent } from './students.component';
import { AcademicService } from '../../../core/services/academic.service';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { StudentListItem, ACADEMIC_VALIDATION_REGEX } from '../../../core/models/academic.models';

describe('StudentsComponent (SCR-07 Students Directory & Registration Wizards)', () => {
  let component: StudentsComponent;
  let fixture: ComponentFixture<StudentsComponent>;
  let academicServiceMock: jasmine.SpyObj<AcademicService>;
  let routerMock: jasmine.SpyObj<Router>;

  const mockStudents: StudentListItem[] = [
    {
      id: 'std-1',
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
      attendanceRate: 92,
      enrolledGroupIds: ['grp-1'],
      createdAt: '2026-09-01T08:00:00Z',
      withdrawnAt: null
    },
    {
      id: 'std-2',
      studentCode: 'ST-1043',
      fullName: 'مريم شريف فوزي',
      grade: 'Grade2Secondary',
      guardianName: 'شريف فوزي',
      guardianPhone: '01123456789',
      whatsappOptIn: true,
      activeNfcCardUid: '04:B3:8D:2A',
      qrIdentifierToken: 'QR-ST-1043',
      status: 'Active',
      tuitionBalance: 150,
      attendanceRate: 88,
      enrolledGroupIds: ['grp-2'],
      createdAt: '2026-09-01T08:00:00Z',
      withdrawnAt: null
    }
  ];

  beforeEach(async () => {
    academicServiceMock = jasmine.createSpyObj('AcademicService', [
      'getStudents',
      'getStudyGroups',
      'createStudent',
      'linkExistingStudent'
    ]);
    routerMock = jasmine.createSpyObj('Router', ['navigate']);

    academicServiceMock.getStudents.and.returnValue(of(mockStudents));
    academicServiceMock.getStudyGroups.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [StudentsComponent],
      providers: [
        { provide: AcademicService, useValue: academicServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StudentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should validate Egyptian phone numbers correctly with regex', () => {
    expect(ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test('01019482687')).toBeTrue();
    expect(ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test('01123456789')).toBeTrue();
    expect(ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test('01234567890')).toBeTrue();
    expect(ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test('01512345678')).toBeTrue();

    // Invalid: wrong prefix 019
    expect(ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test('01912345678')).toBeFalse();
    // Invalid: too short or non-numeric
    expect(ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test('0101234567')).toBeFalse();
    expect(ACADEMIC_VALIDATION_REGEX.EGYPTIAN_PHONE.test('010123456789')).toBeFalse();
  });

  it('should validate 4-digit secret PIN for student linking (DEC-02)', () => {
    expect(ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test('1234')).toBeTrue();
    expect(ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test('0000')).toBeTrue();

    // Invalid PINs
    expect(ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test('123')).toBeFalse();
    expect(ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test('12345')).toBeFalse();
    expect(ACADEMIC_VALIDATION_REGEX.SECRET_PIN.test('abcd')).toBeFalse();
  });

  it('should toggle view mode between grid and table', () => {
    component.setViewMode('table');
    expect(component.viewMode()).toBe('table');
    expect(localStorage.getItem('tahdeera_students_view_mode')).toBe('table');

    component.setViewMode('grid');
    expect(component.viewMode()).toBe('grid');
    expect(localStorage.getItem('tahdeera_students_view_mode')).toBe('grid');
  });

  it('should filter students by search query and grade', () => {
    component.searchQuery.set('مريم');
    expect(component.filteredStudents().length).toBe(1);
    expect(component.filteredStudents()[0].fullName).toContain('مريم');

    component.searchQuery.set('');
    component.selectedGrade.set('Grade1Secondary');
    expect(component.filteredStudents().length).toBe(1);
    expect(component.filteredStudents()[0].studentCode).toBe('ST-1042');
  });
});

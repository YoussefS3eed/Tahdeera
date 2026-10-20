import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GroupsComponent } from './groups.component';
import { AcademicService } from '../../../core/services/academic.service';
import { of } from 'rxjs';
import { StudyGroupItem } from '../../../core/models/academic.models';

describe('GroupsComponent (SCR-10 Study Groups & Assistant Delegation)', () => {
  let component: GroupsComponent;
  let fixture: ComponentFixture<GroupsComponent>;
  let academicServiceMock: jasmine.SpyObj<AcademicService>;

  const mockGroups: StudyGroupItem[] = [
    {
      id: 'grp-1',
      name: 'فيزياء 1ث - مجموعة السبت أ',
      grade: 'Grade1Secondary',
      subject: 'الفيزياء',
      defaultMonthlyFee: 350,
      lateGraceMinutes: 15,
      maxCapacity: 30,
      currentEnrollmentCount: 28,
      teacherInChargeName: 'أ. حسام الدين',
      delegatedAssistantStaffId: 'staff-1',
      delegatedAssistantName: 'أحمد نبيل (مساعد أول)',
      weeklyScheduleSummary: 'السبت 08:00 - 10:00'
    },
    {
      id: 'grp-2',
      name: 'كيمياء 2ث - مجموعة الأحد',
      grade: 'Grade2Secondary',
      subject: 'الكيمياء',
      defaultMonthlyFee: 400,
      lateGraceMinutes: 10,
      maxCapacity: 25,
      currentEnrollmentCount: 15,
      teacherInChargeName: 'أ. حسام الدين',
      delegatedAssistantStaffId: null,
      delegatedAssistantName: null,
      weeklyScheduleSummary: 'الأحد 10:30 - 12:30'
    }
  ];

  beforeEach(async () => {
    academicServiceMock = jasmine.createSpyObj('AcademicService', [
      'getStudyGroups',
      'createStudyGroup',
      'delegateGroupAssistant'
    ]);

    academicServiceMock.getStudyGroups.and.returnValue(of(mockGroups));

    await TestBed.configureTestingModule({
      imports: [GroupsComponent],
      providers: [
        { provide: AcademicService, useValue: academicServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize and load study groups', () => {
    expect(component.groups().length).toBe(2);
    expect(component.filteredGroups().length).toBe(2);
  });

  it('should filter groups by academic grade', () => {
    component.selectedGrade.set('Grade1Secondary');
    expect(component.filteredGroups().length).toBe(1);
    expect(component.filteredGroups()[0].id).toBe('grp-1');

    component.selectedGrade.set('Grade2Secondary');
    expect(component.filteredGroups().length).toBe(1);
    expect(component.filteredGroups()[0].id).toBe('grp-2');
  });

  it('should create group and append to list', () => {
    const newGroup: StudyGroupItem = {
      id: 'grp-new',
      name: 'أحياء 3ث',
      grade: 'Grade3Secondary',
      subject: 'الأحياء',
      defaultMonthlyFee: 500,
      lateGraceMinutes: 15,
      maxCapacity: 20,
      currentEnrollmentCount: 0,
      teacherInChargeName: 'أ. حسام الدين',
      delegatedAssistantStaffId: null,
      delegatedAssistantName: null,
      weeklyScheduleSummary: 'الثلاثاء 14:00'
    };
    academicServiceMock.createStudyGroup.and.returnValue(of(newGroup));

    component.onGroupCreated(newGroup);

    expect(academicServiceMock.createStudyGroup).toHaveBeenCalled();
    expect(component.groups().find(g => g.id === 'grp-new')).toBeDefined();
  });

  it('should delegate single assistant to group per CONF-09', () => {
    const updatedGroup: StudyGroupItem = {
      ...mockGroups[1],
      delegatedAssistantStaffId: 'staff-2',
      delegatedAssistantName: 'سلمى مجدي (مساعد تدريس)'
    };
    academicServiceMock.delegateGroupAssistant.and.returnValue(of(updatedGroup));

    component.onAssistantDelegated({ groupId: 'grp-2', assistantId: 'staff-2' });

    expect(academicServiceMock.delegateGroupAssistant).toHaveBeenCalledWith('grp-2', 'staff-2');
    const target = component.groups().find(g => g.id === 'grp-2');
    expect(target?.delegatedAssistantName).toBe('سلمى مجدي (مساعد تدريس)');
  });
});

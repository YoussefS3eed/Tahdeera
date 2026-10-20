import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CardsComponent } from './cards.component';
import { AcademicService } from '../../../core/services/academic.service';
import { of } from 'rxjs';
import { NfcCardItem, StudentListItem } from '../../../core/models/academic.models';

describe('CardsComponent (SCR-09 NFC Cards Registry)', () => {
  let component: CardsComponent;
  let fixture: ComponentFixture<CardsComponent>;
  let academicServiceMock: jasmine.SpyObj<AcademicService>;

  const mockCards: NfcCardItem[] = [
    {
      cardUid: '04:A2:7C:19',
      label: 'كارت يوسف',
      assignedStudentId: 'std-1042',
      assignedStudentName: 'يوسف أحمد علي',
      status: 'Active',
      issuedAt: '2026-09-01T08:00:00Z',
      revocationReason: null
    },
    {
      cardUid: '04:FF:EE:DD',
      label: 'كارت احتياطي',
      assignedStudentId: null,
      assignedStudentName: null,
      status: 'Unassigned',
      issuedAt: '2026-10-01T08:00:00Z',
      revocationReason: null
    }
  ];

  beforeEach(async () => {
    academicServiceMock = jasmine.createSpyObj('AcademicService', [
      'getNfcCards',
      'getStudents',
      'linkCard',
      'revokeCard'
    ]);

    academicServiceMock.getNfcCards.and.returnValue(of(mockCards));
    academicServiceMock.getStudents.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [CardsComponent],
      providers: [
        { provide: AcademicService, useValue: academicServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CardsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize and load cards inventory', () => {
    expect(component.cards().length).toBe(2);
    expect(component.filteredCards().length).toBe(2);
  });

  it('should filter cards by status', () => {
    component.selectedStatus.set('Active');
    expect(component.filteredCards().length).toBe(1);
    expect(component.filteredCards()[0].cardUid).toBe('04:A2:7C:19');

    component.selectedStatus.set('Unassigned');
    expect(component.filteredCards().length).toBe(1);
    expect(component.filteredCards()[0].cardUid).toBe('04:FF:EE:DD');
  });

  it('should link card to a student and update local list', () => {
    const updatedCard: NfcCardItem = {
      cardUid: '04:FF:EE:DD',
      label: 'كارت جديد',
      assignedStudentId: 'std-999',
      assignedStudentName: 'عمر خالد',
      status: 'Active',
      issuedAt: '2026-10-21T08:00:00Z',
      revocationReason: null
    };
    academicServiceMock.linkCard.and.returnValue(of(updatedCard));

    component.onCardLinked({ cardUid: '04:FF:EE:DD', studentId: 'std-999' });

    expect(academicServiceMock.linkCard).toHaveBeenCalledWith('04:FF:EE:DD', 'std-999');
    const target = component.cards().find(c => c.cardUid === '04:FF:EE:DD');
    expect(target?.status).toBe('Active');
    expect(target?.assignedStudentName).toBe('عمر خالد');
  });

  it('should revoke card with reason and update status to Revoked', () => {
    const revokedCard: NfcCardItem = {
      ...mockCards[0],
      status: 'Revoked',
      revocationReason: 'Lost'
    };
    academicServiceMock.revokeCard.and.returnValue(of(revokedCard));

    component.onCardRevoked({ cardUid: '04:A2:7C:19', reason: 'Lost' });

    expect(academicServiceMock.revokeCard).toHaveBeenCalledWith('04:A2:7C:19', 'Lost');
    const target = component.cards().find(c => c.cardUid === '04:A2:7C:19');
    expect(target?.status).toBe('Revoked');
  });
});

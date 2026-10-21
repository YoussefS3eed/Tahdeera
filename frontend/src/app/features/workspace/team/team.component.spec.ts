import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { TeamComponent } from './team.component';
import { TeamStore } from '../../../core/stores/team.store';
import { MOCK_STAFF_MEMBERS } from '../../../core/mocks/f4-mock-data';

describe('TeamComponent', () => {
  let component: TeamComponent;
  let fixture: ComponentFixture<TeamComponent>;
  let store: TeamStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TeamComponent, HttpClientTestingModule],
      providers: [TeamStore]
    }).compileComponents();

    fixture = TestBed.createComponent(TeamComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(TeamStore);

    // Mock store data
    store.members.set([...MOCK_STAFF_MEMBERS]);

    fixture.detectChanges();
  });

  it('should create team component and show members', () => {
    expect(component).toBeTruthy();
    expect(store.members().length).toBe(MOCK_STAFF_MEMBERS.length);
  });

  it('should filter by role', () => {
    store.roleFilter.set('Teacher');
    expect(store.filteredMembers().length).toBe(1);
    expect(store.filteredMembers()[0].role).toBe('Teacher');
  });

  it('should open credential card modal with selected member', () => {
    const member = MOCK_STAFF_MEMBERS[1];
    component.openCredentialCard(member);

    expect(component.selectedMemberForCard()).toEqual(member);
    expect(component.showCredentialCard()).toBeTrue();
  });

  it('should open deactivate modal with target member', () => {
    const member = MOCK_STAFF_MEMBERS[1];
    component.openDeactivateModal(member);

    expect(component.selectedMemberForDeactivation()).toEqual(member);
    expect(component.showDeactivateModal()).toBeTrue();
  });
});

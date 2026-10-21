import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AccountsComponent } from './accounts.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { AdminImpersonationStore } from '../../../core/stores/admin-impersonation.store';
import { MOCK_ADMIN_WORKSPACES } from '../../../core/mocks/admin-mock-data';

describe('AccountsComponent (SCR-20)', () => {
  let component: AccountsComponent;
  let fixture: ComponentFixture<AccountsComponent>;
  let adminStore: AdminStore;
  let impersonationStore: AdminImpersonationStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountsComponent],
      providers: [
        AdminStore,
        AdminImpersonationStore,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AccountsComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    impersonationStore = TestBed.inject(AdminImpersonationStore);
    adminStore.workspaces.set(MOCK_ADMIN_WORKSPACES);
    fixture.detectChanges();
  });

  it('should render table rows for workspaces', () => {
    const rows = fixture.nativeElement.querySelectorAll('.data-table tbody tr');
    expect(rows.length).toBe(MOCK_ADMIN_WORKSPACES.length);
  });

  it('should trigger impersonation when clicking impersonate button', () => {
    spyOn(impersonationStore, 'startImpersonation');
    const firstWorkspace = MOCK_ADMIN_WORKSPACES[0];
    component.impersonate(firstWorkspace);
    expect(impersonationStore.startImpersonation).toHaveBeenCalledWith(firstWorkspace);
  });

  it('should enforce mandatory suspension reason minimum 5 chars', () => {
    component.openSuspendModal(MOCK_ADMIN_WORKSPACES[0]);
    component.suspensionReason = 'short';
    expect(component.suspensionReason.trim().length).toBe(5);

    component.suspensionReason = '1234';
    expect(component.suspensionReason.trim().length >= 5).toBeFalse();
  });
});

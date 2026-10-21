import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AuditLogComponent } from './audit-logs.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { MOCK_ADMIN_AUDIT_LOGS } from '../../../core/mocks/admin-mock-data';

describe('AuditLogComponent (SCR-26)', () => {
  let component: AuditLogComponent;
  let fixture: ComponentFixture<AuditLogComponent>;
  let adminStore: AdminStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditLogComponent],
      providers: [
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AuditLogComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    adminStore.auditLogs.set(MOCK_ADMIN_AUDIT_LOGS);
    fixture.detectChanges();
  });

  it('should render audit log items without delete buttons', () => {
    const rows = fixture.nativeElement.querySelectorAll('.data-table tbody tr');
    expect(rows.length).toBe(MOCK_ADMIN_AUDIT_LOGS.length);
    const deleteBtn = fixture.nativeElement.querySelector('.btn-danger');
    expect(deleteBtn).toBeNull();
  });

  it('should open diff modal when clicking inspect diff button', () => {
    const firstWithDiff = MOCK_ADMIN_AUDIT_LOGS.find(l => l.beforeState || l.afterState);
    if (firstWithDiff) {
      component.openDiffModal(firstWithDiff);
      fixture.detectChanges();
      expect(component.selectedEntry).toEqual(firstWithDiff);
      expect(fixture.nativeElement.querySelector('.modal-card')).toBeTruthy();
    }
  });
});

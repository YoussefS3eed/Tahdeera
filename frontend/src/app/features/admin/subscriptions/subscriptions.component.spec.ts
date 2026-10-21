import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { SubscriptionsComponent } from './subscriptions.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { MOCK_ADMIN_WORKSPACES } from '../../../core/mocks/admin-mock-data';

describe('SubscriptionsComponent (SCR-21)', () => {
  let component: SubscriptionsComponent;
  let fixture: ComponentFixture<SubscriptionsComponent>;
  let adminStore: AdminStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubscriptionsComponent],
      providers: [
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SubscriptionsComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    adminStore.workspaces.set(MOCK_ADMIN_WORKSPACES);
    fixture.detectChanges();
  });

  it('should render subscriptions list', () => {
    const rows = fixture.nativeElement.querySelectorAll('.data-table tbody tr');
    expect(rows.length).toBe(MOCK_ADMIN_WORKSPACES.length);
  });

  it('should calculate expiry preview correctly when selecting presets', () => {
    component.openRenewModal(MOCK_ADMIN_WORKSPACES[0]);
    component.applyPreset('90D', 90);
    expect(component.renewRequest.durationDays).toBe(90);
    expect(component.calculatedExpiryDate.getTime()).toBeGreaterThan(Date.now());
  });
});

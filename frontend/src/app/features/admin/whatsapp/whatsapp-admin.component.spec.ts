import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { WhatsAppAdminComponent } from './whatsapp-admin.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { MOCK_WHATSAPP_CONFIG, MOCK_WHATSAPP_STATS } from '../../../core/mocks/admin-mock-data';

describe('WhatsAppAdminComponent (SCR-24)', () => {
  let component: WhatsAppAdminComponent;
  let fixture: ComponentFixture<WhatsAppAdminComponent>;
  let adminStore: AdminStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WhatsAppAdminComponent],
      providers: [
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WhatsAppAdminComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    adminStore.whatsappConfig.set(MOCK_WHATSAPP_CONFIG);
    adminStore.whatsappStats.set(MOCK_WHATSAPP_STATS);
    fixture.detectChanges();
  });

  it('should mask sensitive tokens by default', () => {
    expect(component.showToken).toBeFalse();
    expect(component.showSecret).toBeFalse();
  });

  it('should trigger test provider ping and update status', () => {
    component.testPing();
    expect(component.pingLoading).toBeTrue();
  });
});

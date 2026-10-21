import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { WhatsAppComponent } from './whatsapp.component';
import { WhatsAppStore } from '../../../core/stores/whatsapp.store';
import { MOCK_WHATSAPP_TEMPLATES, MOCK_OUTBOX_LOGS, MOCK_CONSENTS } from '../../../core/mocks/f4-mock-data';

describe('WhatsAppHubComponent', () => {
  let component: WhatsAppComponent;
  let fixture: ComponentFixture<WhatsAppComponent>;
  let store: WhatsAppStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WhatsAppComponent, HttpClientTestingModule],
      providers: [WhatsAppStore]
    }).compileComponents();

    fixture = TestBed.createComponent(WhatsAppComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(WhatsAppStore);

    // Seed mock data
    store.templates.set({ ...MOCK_WHATSAPP_TEMPLATES });
    store.outboxLogs.set([...MOCK_OUTBOX_LOGS]);
    store.consents.set([...MOCK_CONSENTS]);

    fixture.detectChanges();
  });

  afterEach(() => {
    store.ngOnDestroy();
  });

  it('should create the WhatsApp hub component', () => {
    expect(component).toBeTruthy();
    expect(store.activeSubTab()).toBe('template');
  });

  it('should switch tabs', () => {
    component.setTab('logs');
    expect(store.activeSubTab()).toBe('logs');

    component.setTab('parents');
    expect(store.activeSubTab()).toBe('parents');

    component.setTab('template');
    expect(store.activeSubTab()).toBe('template');
  });

  it('should display count of outbox logs and consents in tabs', () => {
    expect(store.outboxLogs().length).toBe(MOCK_OUTBOX_LOGS.length);
    expect(store.consents().length).toBe(MOCK_CONSENTS.length);
  });
});

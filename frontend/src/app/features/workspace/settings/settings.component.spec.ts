import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { SettingsComponent } from './settings.component';
import { SettingsStore } from '../../../core/stores/settings.store';
import { MOCK_WORKSPACE_SETTINGS, MOCK_SUBSCRIPTION_VIEW } from '../../../core/mocks/f4-mock-data';

describe('SettingsComponent', () => {
  let component: SettingsComponent;
  let fixture: ComponentFixture<SettingsComponent>;
  let store: SettingsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SettingsComponent, HttpClientTestingModule],
      providers: [SettingsStore]
    }).compileComponents();

    fixture = TestBed.createComponent(SettingsComponent);
    component = fixture.componentInstance;
    store = TestBed.inject(SettingsStore);

    // Mock store data
    store.settings.set({ ...MOCK_WORKSPACE_SETTINGS });
    store.subscription.set({ ...MOCK_SUBSCRIPTION_VIEW });

    fixture.detectChanges();
  });

  it('should create settings component and load initial state', () => {
    expect(component).toBeTruthy();
    expect(store.settings()?.profile?.centerNameAr || store.settings()?.centerName).toBe('سنتر الأوائل التعليمي');
    expect(store.subscription()?.packageNameAr || store.subscription()?.planName).toBeDefined();
  });
});

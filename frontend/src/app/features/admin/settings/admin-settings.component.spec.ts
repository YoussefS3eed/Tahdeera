import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminSettingsComponent } from './admin-settings.component';
import { AdminStore } from '../../../core/stores/admin.store';
import { MOCK_GLOBAL_SETTINGS } from '../../../core/mocks/admin-mock-data';

describe('AdminSettingsComponent (SCR-25)', () => {
  let component: AdminSettingsComponent;
  let fixture: ComponentFixture<AdminSettingsComponent>;
  let adminStore: AdminStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminSettingsComponent],
      providers: [
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminSettingsComponent);
    component = fixture.componentInstance;
    adminStore = TestBed.inject(AdminStore);
    adminStore.globalSettings.set(MOCK_GLOBAL_SETTINGS);
    fixture.detectChanges();
  });

  it('should display support hotlines in preview box', () => {
    expect(fixture.nativeElement.textContent).toContain('01272785618');
    expect(fixture.nativeElement.textContent).toContain('01019482687');
  });

  it('should save settings when form is submitted', () => {
    spyOn(adminStore, 'updateSettings').and.callThrough();
    component.saveSettings();
    expect(adminStore.updateSettings).toHaveBeenCalled();
  });
});

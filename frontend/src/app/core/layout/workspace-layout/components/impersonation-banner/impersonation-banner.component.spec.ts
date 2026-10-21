import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ImpersonationBannerComponent } from './impersonation-banner.component';
import { AdminImpersonationStore } from '../../../../stores/admin-impersonation.store';
import { AuthStore } from '../../../../stores/auth.store';
import { AdminStore } from '../../../../stores/admin.store';
import { MOCK_ADMIN_WORKSPACES } from '../../../../mocks/admin-mock-data';

describe('ImpersonationBannerComponent', () => {
  let component: ImpersonationBannerComponent;
  let fixture: ComponentFixture<ImpersonationBannerComponent>;
  let impersonationStore: AdminImpersonationStore;

  beforeEach(async () => {
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [ImpersonationBannerComponent],
      providers: [
        AdminImpersonationStore,
        AuthStore,
        AdminStore,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ImpersonationBannerComponent);
    component = fixture.componentInstance;
    impersonationStore = TestBed.inject(AdminImpersonationStore);
  });

  it('should not render banner when not impersonating', () => {
    impersonationStore.isImpersonating.set(false);
    fixture.detectChanges();
    const banner = fixture.nativeElement.querySelector('.impersonation-banner');
    expect(banner).toBeNull();
  });

  it('should render banner and target workspace name when impersonating', () => {
    impersonationStore.isImpersonating.set(true);
    impersonationStore.targetWorkspace.set(MOCK_ADMIN_WORKSPACES[0]);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.impersonation-banner');
    expect(banner).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain(MOCK_ADMIN_WORKSPACES[0].name);
    expect(fixture.nativeElement.textContent).toContain('إنهاء التصفح والعودة للإدارة');
  });

  it('should call exitImpersonation when exit button is clicked', () => {
    spyOn(impersonationStore, 'exitImpersonation');
    impersonationStore.isImpersonating.set(true);
    impersonationStore.targetWorkspace.set(MOCK_ADMIN_WORKSPACES[0]);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.exit-btn') as HTMLButtonElement;
    button.click();
    expect(impersonationStore.exitImpersonation).toHaveBeenCalled();
  });
});

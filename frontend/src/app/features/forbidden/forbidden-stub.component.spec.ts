import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ForbiddenStubComponent } from './forbidden-stub.component';
import { AuthStore } from '../../core/stores/auth.store';
import { LocalizationService } from '../../core/services/localization.service';
import { SupportedLanguage } from '../../core/enums/domain.enums';

describe('ForbiddenStubComponent', () => {
  let component: ForbiddenStubComponent;
  let fixture: ComponentFixture<ForbiddenStubComponent>;
  let router: jasmine.SpyObj<Router>;
  let authStore: AuthStore;
  let localizationService: LocalizationService;

  beforeEach(async () => {
    localStorage.clear();
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [ForbiddenStubComponent],
      providers: [
        AuthStore,
        LocalizationService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    localizationService = TestBed.inject(LocalizationService);
    localizationService.setLanguage(SupportedLanguage.Arabic);

    fixture = TestBed.createComponent(ForbiddenStubComponent);
    component = fixture.componentInstance;
    authStore = TestBed.inject(AuthStore);
    fixture.detectChanges();
  });

  it('should create and display 403 message', () => {
    expect(component).toBeTruthy();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('403');
    expect(compiled.textContent).toContain('عفواً، هذه الصفحة مخصصة لمدير النظام العام للمنصة فقط');
  });

  it('should navigate to /workspace/dashboard when primary CTA is clicked', () => {
    component.returnToDashboard();
    expect(router.navigate).toHaveBeenCalledWith(['/workspace/dashboard']);
  });

  it('should call authStore.logout and navigate to /login when secondary CTA is clicked', () => {
    spyOn(authStore, 'logout');
    component.logout();
    expect(authStore.logout).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});

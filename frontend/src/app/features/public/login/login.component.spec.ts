import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { AuthStore } from '../../../core/stores/auth.store';
import { LocalizationService } from '../../../core/services/localization.service';
import { LoginResponse } from '../../../core/models/auth.models';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let httpTestingController: HttpTestingController;
  let authStore: AuthStore;
  let router: Router;

  const mockTeacherResponse: LoginResponse = {
    user: {
      id: 'usr_01',
      fullName: 'أ. محمد عبد الله',
      email: 'teacher@tahdeera.app',
      phone: '01012345670',
      initials: 'م.ع'
    },
    tokens: {
      accessToken: 'token_teacher_123',
      refreshToken: 'refresh_teacher_123',
      tokenType: 'Bearer',
      expiresIn: 900
    },
    workspaces: [
      {
        id: 'ws_01',
        name: 'سنتر الأوائل',
        code: 'AWAEL',
        status: 'Active',
        role: 'Teacher',
        studentCount: 248,
        activeGroupCount: 12,
        subscriptionDaysRemaining: 45,
        subscriptionExpiresAt: '2026-12-05T00:00:00Z'
      }
    ],
    activeWorkspaceId: 'ws_01'
  };

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        AuthStore,
        {
          provide: LocalizationService,
          useValue: {
            currentLang: () => 'ar',
            isRtl: () => true
          }
        },
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    httpTestingController = TestBed.inject(HttpTestingController);
    authStore = TestBed.inject(AuthStore);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
    spyOn(router, 'navigateByUrl').and.returnValue(Promise.resolve(true));

    fixture.detectChanges();
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('should create and initialize form controls', () => {
    expect(component).toBeTruthy();
    expect(component.loginForm).toBeDefined();
    expect(component.loginForm.get('identifier')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
    expect(component.loginForm.get('rememberMe')?.value).toBe(true);
  });

  it('should validate Egyptian phone format correctly', () => {
    const identifier = component.loginForm.get('identifier');

    identifier?.setValue('01012345678');
    expect(identifier?.valid).toBeTrue();

    identifier?.setValue('01123456789');
    expect(identifier?.valid).toBeTrue();

    identifier?.setValue('01234567890');
    expect(identifier?.valid).toBeTrue();

    identifier?.setValue('01598765432');
    expect(identifier?.valid).toBeTrue();

    // Invalid Egyptian phones
    identifier?.setValue('01312345678'); // 013 is not a mobile prefix
    expect(identifier?.valid).toBeFalse();

    identifier?.setValue('0101234567'); // 10 digits
    expect(identifier?.valid).toBeFalse();

    identifier?.setValue('010123456789'); // 12 digits
    expect(identifier?.valid).toBeFalse();
  });

  it('should normalize Arabic-Indic digits in identifier validation', () => {
    const identifier = component.loginForm.get('identifier');
    identifier?.setValue('٠١٠١٢٣٤٥٦٧٨');
    expect(identifier?.valid).toBeTrue();
  });

  it('should validate email format correctly', () => {
    const identifier = component.loginForm.get('identifier');
    identifier?.setValue('teacher@tahdeera.app');
    expect(identifier?.valid).toBeTrue();

    identifier?.setValue('not-an-email');
    expect(identifier?.valid).toBeFalse();
  });

  it('should toggle password visibility signal', () => {
    expect(component.showPassword()).toBeFalse();
    component.togglePasswordVisibility();
    expect(component.showPassword()).toBeTrue();
    component.togglePasswordVisibility();
    expect(component.showPassword()).toBeFalse();
  });

  it('should toggle forgot password modal visibility', () => {
    expect(component.isForgotPasswordOpen()).toBeFalse();
    component.openForgotPasswordModal();
    expect(component.isForgotPasswordOpen()).toBeTrue();
    component.closeForgotPasswordModal();
    expect(component.isForgotPasswordOpen()).toBeFalse();
  });

  it('should authenticate user and navigate to dashboard on successful login', () => {
    component.loginForm.patchValue({
      identifier: 'teacher@tahdeera.app',
      password: 'Password123'
    });

    component.submit();
    expect(component.isLoading()).toBeTrue();

    const req = httpTestingController.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.identifier).toBe('teacher@tahdeera.app');
    req.flush(mockTeacherResponse);

    expect(component.isLoading()).toBeFalse();
    expect(authStore.isAuthenticated()).toBeTrue();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/workspace/dashboard');
  });

  it('should set error message and increment failure count on HTTP 401', () => {
    component.loginForm.patchValue({
      identifier: 'teacher@tahdeera.app',
      password: 'WrongPassword'
    });

    component.submit();
    const req = httpTestingController.expectOne('/api/v1/auth/login');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(component.isLoading()).toBeFalse();
    expect(component.errorMessage()).toContain('اسم المستخدم أو كلمة المرور');
    expect(localStorage.getItem('tahdeera_login_failures')).toBe('1');
  });

  it('should activate rate lock after 5 failed login attempts', fakeAsync(() => {
    localStorage.setItem('tahdeera_login_failures', '4');

    component.loginForm.patchValue({
      identifier: 'teacher@tahdeera.app',
      password: 'WrongPassword'
    });

    component.submit();
    const req = httpTestingController.expectOne('/api/v1/auth/login');
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(component.isRateLocked()).toBeTrue();
    expect(component.remainingLockoutSeconds()).toBe(60);

    // Fast-forward 10 seconds
    tick(10000);
    expect(component.remainingLockoutSeconds()).toBe(50);

    // Fast-forward remaining 50 seconds
    tick(50000);
    expect(component.isRateLocked()).toBeFalse();
    expect(component.remainingLockoutSeconds()).toBe(0);
  }));

  it('should populate form and submit when demo preset is selected', () => {
    component.selectDemoPreset('teacher');
    expect(component.loginForm.value.identifier).toBe('teacher@tahdeera.app');

    const req = httpTestingController.expectOne('/api/v1/auth/login');
    req.flush(mockTeacherResponse);

    expect(authStore.isAuthenticated()).toBeTrue();
  });
});

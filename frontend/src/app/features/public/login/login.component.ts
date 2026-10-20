import {
  Component,
  OnInit,
  OnDestroy,
  signal,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { AuthStore } from '../../../core/stores/auth.store';
import { LoginResponse } from '../../../core/models/auth.models';
import { LocalizationService } from '../../../core/services/localization.service';

const STORAGE_KEY_FAILURES = 'tahdeera_login_failures';
const STORAGE_KEY_LOCKOUT_UNTIL = 'tahdeera_login_lockout_until';
const MAX_FAILURES = 5;
const LOCKOUT_DURATION_MS = 60000; // 60 seconds

// Egyptian phone regex: 11 digits starting with 010, 011, 012, or 015
const EGYPTIAN_PHONE_REGEX = /^01[0125][0-9]{8}$/;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function identifierValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) {
    return null;
  }
  let val = String(control.value).trim();
  // Normalize Arabic-Indic numerals to ASCII
  val = val.replace(/[٠-٩]/g, d => '٠١٢٣٤٥٦٧٨٩'.indexOf(d).toString());

  const isEmail = EMAIL_REGEX.test(val);
  const isPhone = EGYPTIAN_PHONE_REGEX.test(val);

  if (isEmail || isPhone) {
    return null;
  }
  return { invalidIdentifier: true };
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: []
})
export class LoginComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  readonly authStore = inject(AuthStore);
  readonly localization = inject(LocalizationService);

  loginForm!: FormGroup;

  readonly showPassword = signal<boolean>(false);
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isRateLocked = signal<boolean>(false);
  readonly remainingLockoutSeconds = signal<number>(0);
  readonly isForgotPasswordOpen = signal<boolean>(false);

  private countdownSubscription?: Subscription;

  ngOnInit(): void {
    // If already authenticated and active, redirect to dashboard (or lockout if locked)
    if (this.authStore.isAuthenticated()) {
      if (this.authStore.isLockedOut()) {
        this.router.navigate(['/workspace/lockout']);
      } else {
        this.router.navigate(['/workspace/dashboard']);
      }
      return;
    }

    this.loginForm = this.fb.group({
      identifier: ['', [Validators.required, identifierValidator]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [true]
    });

    this.checkPersistedRateLimit();
  }

  ngOnDestroy(): void {
    this.countdownSubscription?.unsubscribe();
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  openForgotPasswordModal(): void {
    this.isForgotPasswordOpen.set(true);
  }

  closeForgotPasswordModal(): void {
    this.isForgotPasswordOpen.set(false);
  }

  selectDemoPreset(
    preset: 'teacher' | 'assistant' | 'receptionist' | 'expired' | 'suspended'
  ): void {
    if (this.isRateLocked()) return;

    let identifier = '';
    const password = 'Password123';

    switch (preset) {
      case 'teacher':
        identifier = 'teacher@tahdeera.app';
        break;
      case 'assistant':
        identifier = 'assistant@tahdeera.app';
        break;
      case 'receptionist':
        identifier = 'receptionist@tahdeera.app';
        break;
      case 'expired':
        identifier = 'expired@tahdeera.app';
        break;
      case 'suspended':
        identifier = 'suspended@tahdeera.app';
        break;
    }

    this.loginForm.patchValue({
      identifier,
      password,
      rememberMe: true
    });

    this.submit();
  }

  submit(): void {
    if (this.isRateLocked() || this.loginForm.invalid || this.isLoading()) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    let identifier = this.loginForm.value.identifier.trim();
    // Normalize Arabic numerals
    identifier = identifier.replace(/[٠-٩]/g, (d: string) =>
      '٠١٢٣٤٥٦٧٨٩'.indexOf(d).toString()
    );

    const payload = {
      identifier,
      password: this.loginForm.value.password,
      rememberMe: this.loginForm.value.rememberMe
    };

    this.http.post<LoginResponse>('/api/v1/auth/login', payload).subscribe({
      next: res => {
        this.isLoading.set(false);
        this.resetFailureCount();
        this.authStore.login(res);

        const returnUrl =
          this.route.snapshot.queryParams['returnUrl'] || '/workspace/dashboard';

        if (this.authStore.isLockedOut()) {
          this.router.navigate(['/workspace/lockout']);
        } else {
          this.router.navigateByUrl(returnUrl);
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set(
          this.localization.currentLang() === 'ar'
            ? 'اسم المستخدم أو كلمة المرور غير صحيحة'
            : 'Invalid username or password'
        );
        this.recordFailedAttempt();
      }
    });
  }

  private checkPersistedRateLimit(): void {
    try {
      const lockoutUntilStr = localStorage.getItem(STORAGE_KEY_LOCKOUT_UNTIL);
      if (lockoutUntilStr) {
        const lockoutUntil = parseInt(lockoutUntilStr, 10);
        const now = Date.now();
        if (lockoutUntil > now) {
          const remainingSec = Math.ceil((lockoutUntil - now) / 1000);
          this.startRateLock(remainingSec);
        } else {
          this.resetFailureCount();
        }
      }
    } catch {
      // Ignored
    }
  }

  private recordFailedAttempt(): void {
    try {
      const currentFailures =
        parseInt(localStorage.getItem(STORAGE_KEY_FAILURES) || '0', 10) + 1;
      localStorage.setItem(STORAGE_KEY_FAILURES, currentFailures.toString());

      if (currentFailures >= MAX_FAILURES) {
        const lockoutUntil = Date.now() + LOCKOUT_DURATION_MS;
        localStorage.setItem(STORAGE_KEY_LOCKOUT_UNTIL, lockoutUntil.toString());
        this.startRateLock(LOCKOUT_DURATION_MS / 1000);
      }
    } catch {
      // Ignored
    }
  }

  private startRateLock(seconds: number): void {
    this.isRateLocked.set(true);
    this.remainingLockoutSeconds.set(seconds);
    this.countdownSubscription?.unsubscribe();

    this.countdownSubscription = interval(1000).subscribe(() => {
      const sec = this.remainingLockoutSeconds() - 1;
      if (sec <= 0) {
        this.countdownSubscription?.unsubscribe();
        this.resetFailureCount();
        this.isRateLocked.set(false);
        this.remainingLockoutSeconds.set(0);
      } else {
        this.remainingLockoutSeconds.set(sec);
      }
    });
  }

  private resetFailureCount(): void {
    try {
      localStorage.removeItem(STORAGE_KEY_FAILURES);
      localStorage.removeItem(STORAGE_KEY_LOCKOUT_UNTIL);
    } catch {
      // Ignored
    }
  }
}

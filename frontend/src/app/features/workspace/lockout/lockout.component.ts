import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AuthStore } from '../../../core/stores/auth.store';
import { LocalizationService } from '../../../core/services/localization.service';

@Component({
  selector: 'app-lockout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './lockout.component.html',
  styleUrls: []
})
export class LockoutComponent {
  private router = inject(Router);
  readonly authStore = inject(AuthStore);
  readonly localization = inject(LocalizationService);

  readonly hotline1 = '01272785618';
  readonly hotline2 = '01019482687';

  readonly lockoutReason = computed(() => this.authStore.lockoutReason());
  readonly activeWorkspace = computed(() => this.authStore.activeWorkspace());

  readonly studentCount = computed(() => this.activeWorkspace()?.studentCount ?? 248);
  readonly groupCount = computed(() => this.activeWorkspace()?.activeGroupCount ?? 12);

  logout(): void {
    this.authStore.logout();
    this.router.navigate(['/login']);
  }
}

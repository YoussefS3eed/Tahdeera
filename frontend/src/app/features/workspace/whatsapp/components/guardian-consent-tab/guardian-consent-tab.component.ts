import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WhatsAppStore } from '../../../../../core/stores/whatsapp.store';

@Component({
  selector: 'app-guardian-consent-tab',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './guardian-consent-tab.component.html',
  styleUrls: ['./guardian-consent-tab.component.scss']
})
export class GuardianConsentTabComponent {
  readonly store = inject(WhatsAppStore);
  readonly searchQuery = signal<string>('');

  readonly filteredConsents = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    return this.store.consents().filter(c => {
      if (q) {
        const matchName = c.studentName.toLowerCase().includes(q);
        const matchPhone = c.guardianPhone.includes(q);
        const matchParent = c.guardianName ? c.guardianName.toLowerCase().includes(q) : false;
        if (!matchName && !matchPhone && !matchParent) return false;
      }
      return true;
    });
  });

  toggle(studentId: string, currentStatus: boolean): void {
    this.store.toggleConsent(studentId, !currentStatus);
  }
}

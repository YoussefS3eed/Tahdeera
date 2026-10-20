import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CredentialScanResult } from '../../../../../core/models/academic.models';

@Component({
  selector: 'app-last-scan-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './last-scan-card.component.html',
  styleUrls: ['./last-scan-card.component.scss']
})
export class LastScanCardComponent {
  readonly scan = input<CredentialScanResult | null>(null);

  getStatusColorClass(outcome: string | undefined): string {
    switch (outcome) {
      case 'Present':
        return 'badge-present';
      case 'Late':
        return 'badge-late';
      case 'Duplicate':
        return 'badge-duplicate';
      case 'WrongGroup':
        return 'badge-wrong-group';
      case 'Revoked':
      case 'Unregistered':
      default:
        return 'badge-error';
    }
  }
}
